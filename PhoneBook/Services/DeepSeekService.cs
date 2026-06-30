using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PhoneBook.Services;

public class DeepSeekService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<DeepSeekService> _log;

    public DeepSeekService(HttpClient http, IConfiguration config, ILogger<DeepSeekService> log)
    {
        _http = http;
        _config = config;
        _log = log;
    }

    /// <summary>
    /// Smart semantic search — understands meaning, not just keywords
    /// </summary>
    public async Task<List<int>> SmartSearchAsync(List<ContactInfo> contacts, string query)
    {
        var contactList = contacts.Select((c, i) => $"[{i}] {c.Honorific} {c.Name} | {c.Jawatan} | {c.Kementerian} | {c.Department} | Tags: {c.Tags}").ToList();
        var prompt = $@"Kamu adalah pembantu direktori kenalan. Pengguna mencari: ""{query}""

Berikut adalah senarai kenalan:
{string.Join("\n", contactList)}

Kembalikan HANYA nombor indeks kenalan yang paling relevan dengan carian, dipisahkan dengan koma. Contoh: 0,3,5
Jika tiada yang relevan, kembalikan: none
Jangan sertakan penjelasan. Hanya nombor indeks atau 'none'.";

        var response = await ChatAsync(prompt);
        if (string.IsNullOrWhiteSpace(response) || response.Trim().ToLower() == "none")
            return new List<int>();

        try
        {
            return response.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var i) ? i : -1)
                .Where(i => i >= 0 && i < contacts.Count)
                .ToList();
        }
        catch { return new List<int>(); }
    }

    /// <summary>
    /// Auto-enrich: parse email signature / business card text into structured fields
    /// </summary>
    public async Task<EnrichedContact?> ParseContactAsync(string rawText)
    {
        var prompt = $@"Kamu adalah pembantu yang mengekstrak maklumat kenalan dari teks.

Teks: ""{rawText}""

Ekstrak maklumat berikut dan kembalikan SEBAGAI JSON SAHAJA (tiada teks lain):
{{
  ""name"": ""nama penuh"",
  ""honorific"": ""gelaran seperti YB, YM, Dato, Dk, Hjh, Dr, Pg, Awg, Dyg"",
  ""gender"": ""male atau female"",
  ""jawatan"": ""jawatan"",
  ""kementerian"": ""kementerian"",
  ""department"": ""jabatan"",
  ""mobile"": ""nombor mobile pertama dijumpai"",
  ""phone"": ""nombor telefon pejabat"",
  ""email"": ""emel"",
  ""building"": ""bangunan/alamat"",
  ""paname"": ""nama PA atau setiausaha jika ada"",
  ""pamobile"": ""nombor mobile PA"",
  ""notes"": ""nota tambahan""
}}

Jika sesuatu tiada, gunakan null. Kembalikan JSON SAHAJA.";

        var response = await ChatAsync(prompt);
        if (string.IsNullOrWhiteSpace(response)) return null;

        try
        {
            var json = response.Trim();
            if (json.StartsWith("```")) json = json.Split("\n", 2)[1..].ToString()?.Replace("```", "") ?? json;
            json = json.Replace("```json", "").Replace("```", "").Trim();
            return JsonSerializer.Deserialize<EnrichedContact>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to parse AI response: {Response}", response);
            return null;
        }
    }

    private async Task<string?> ChatAsync(string prompt)
    {
        try
        {
            var apiKey = _config["DeepSeek:ApiKey"] ?? Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _log.LogWarning("DeepSeek API key not configured");
                return null;
            }

            var request = new
            {
                model = "deepseek-chat",
                messages = new[] { new { role = "user", content = prompt } },
                temperature = 0.1,
                max_tokens = 500
            };

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var response = await _http.PostAsync("https://api.deepseek.com/v1/chat/completions", content);
            if (!response.IsSuccessStatusCode)
            {
                _log.LogWarning("DeepSeek API returned {Code}", response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<DeepSeekResponse>();
            return result?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "DeepSeek API call failed");
            return null;
        }
    }
}

public class ContactInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Honorific { get; set; }
    public string? Jawatan { get; set; }
    public string? Kementerian { get; set; }
    public string? Department { get; set; }
    public string? Tags { get; set; }
}

public class EnrichedContact
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("honorific")] public string? Honorific { get; set; }
    [JsonPropertyName("gender")] public string? Gender { get; set; }
    [JsonPropertyName("jawatan")] public string? Jawatan { get; set; }
    [JsonPropertyName("kementerian")] public string? Kementerian { get; set; }
    [JsonPropertyName("department")] public string? Department { get; set; }
    [JsonPropertyName("mobile")] public string? Mobile { get; set; }
    [JsonPropertyName("phone")] public string? Phone { get; set; }
    [JsonPropertyName("email")] public string? Email { get; set; }
    [JsonPropertyName("building")] public string? Building { get; set; }
    [JsonPropertyName("paname")] public string? PAName { get; set; }
    [JsonPropertyName("pamobile")] public string? PAMobile { get; set; }
    [JsonPropertyName("notes")] public string? Notes { get; set; }
}

public class DeepSeekResponse
{
    [JsonPropertyName("choices")] public List<DeepSeekChoice>? Choices { get; set; }
}

public class DeepSeekChoice
{
    [JsonPropertyName("message")] public DeepSeekMessage? Message { get; set; }
}

public class DeepSeekMessage
{
    [JsonPropertyName("content")] public string? Content { get; set; }
}
