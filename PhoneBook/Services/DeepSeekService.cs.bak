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
    public async Task<List<int>> SmartSearchAsync(List<ContactInfo> contacts, string query, CancellationToken ct = default)
    {
        // Token budget: estimate and cap contacts list to stay within ~3000 tokens (~4000 chars)
        var contactList = contacts
            .Select((c, i) => $"[{i}] {c.Honorific} {c.Name} | {c.Jawatan} | {c.Kementerian}")
            .ToList();

        // Truncate list if estimated token count exceeds budget
        const int maxChars = 4000;
        var totalChars = 0;
        var truncated = new List<string>();
        foreach (var entry in contactList)
        {
            if (totalChars + entry.Length > maxChars) break;
            truncated.Add(entry);
            totalChars += entry.Length + 1; // +1 for newline
        }

        var prompt = $@"Kamu adalah pembantu direktori kenalan. Pengguna mencari: ""{query}""

Berikut adalah senarai kenalan:
{string.Join("\n", truncated)}

Kembalikan HANYA nombor indeks kenalan yang paling relevan dengan carian, dipisahkan dengan koma. Contoh: 0,3,5
Jika tiada yang relevan, kembalikan: none
Jangan sertakan penjelasan. Hanya nombor indeks atau 'none'.";

        var response = await ChatAsync(prompt, ct);
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
    public async Task<EnrichedContact?> ParseContactAsync(string rawText, CancellationToken ct = default)
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

        var response = await ChatAsync(prompt, ct);
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

    /// <summary>
    /// Conversational search — understand natural language queries and return filters + explanation
    /// </summary>
    public async Task<ChatSearchResult?> ConversationalSearchAsync(
        string query, List<string> availableMinistries, CancellationToken ct = default)
    {
        var ministryList = string.Join(", ", availableMinistries);
        var prompt = $@"Kamu adalah pembantu carian direktori kerajaan. Pengguna bertanya dalam Bahasa Melayu atau Inggeris.

Soalan: ""{query}""

Kementerian yang ada dalam direktori: {ministryList}

Analisa soalan pengguna. Fahami apa yang mereka CARI. Kemudian kembalikan JSON SAHAJA dengan format ini:
{{  ""explanation"": ""Jawapan ringkas dalam Bahasa Melayu menerangkan apa yang anda jumpa dan kenapa."",
  ""kementerian"": ""nama kementerian tepat dari senarai jika pengguna menyebut kementerian spesifik, jika tidak null"",
  ""department_keywords"": ""kata kunci untuk cari di ruangan department/jabatan, pisahkan dengan koma, null jika tiada"",
  ""jawatan_keywords"": ""kata kunci untuk cari di ruangan jawatan, pisahkan dengan koma, null jika tiada"",
  ""name_keywords"": ""nama yang mungkin disebut, pisahkan dengan koma, null jika tiada"",
  ""honorific_filter"": ""YB, YM, Dato, Datin, Dr, Prof jika disebut, jika tidak null"",
  ""tag_filter"": ""VIP, VVIP, IT, Media, Kewangan jika relevant, null jika tiada"",
  ""is_general"": true jika soalan umum atau tidak spesifik, false jika spesifik
}}
PENTING: Jangan reka kementerian yang tiada dalam senarai. Kembalikan JSON SAHAJA, tiada teks lain.";

        var response = await ChatAsync(prompt, ct);
        if (string.IsNullOrWhiteSpace(response)) return null;
        try
        {
            var json = response.Trim();
            if (json.StartsWith("```")) json = json.Split("\n", 2).Last().Replace("```", "");
            json = json.Replace("```json", "").Replace("```", "").Trim();
            return JsonSerializer.Deserialize<ChatSearchResult>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex) { _log.LogWarning(ex, "Failed to parse chat response: {Response}", response); return null; }
    }

    /// <summary>
    /// Suggest tags for a contact based on jawatan, kementerian, department
    /// </summary>
    public async Task<List<string>?> SuggestTagsAsync(
        string name, string? jawatan, string? kementerian, string? department,
        string? existingTags, CancellationToken ct = default)
    {
        var prompt = $@"Kamu adalah pembantu direktori kerajaan. Cadangkan tag yang sesuai untuk kenalan ini.

Nama: {name}
Jawatan: {jawatan ?? "tiada"}
Kementerian: {kementerian ?? "tiada"}
Department: {department ?? "tiada"}
Tag sedia ada: {existingTags ?? "tiada"}

Tag yang biasa digunakan: VIP, VVIP, IT, ICT, Kewangan, Protokol, PS, PTTK, Pentadbiran, Media, Perubatan, Pendidikan, Undang-Undang, Keselamatan

Berdasarkan jawatan dan kementerian, cadangkan 1-3 tag yang PALING sesuai. Kembalikan tag yang dipisahkan dengan koma SAHAJA. Contoh: VIP, PTTK, Kewangan
Jangan cadangkan tag yang sudah ada. Jangan beri penjelasan.";

        var response = await ChatAsync(prompt, ct);
        if (string.IsNullOrWhiteSpace(response)) return null;
        return response.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim()).Where(t => t.Length > 0).ToList();
    }

    /// <summary>
    /// Bulk extract multiple contacts from unstructured text (email threads, PDF, meeting notes)
    /// </summary>
    public async Task<List<EnrichedContact>?> BulkParseContactsAsync(string rawText, CancellationToken ct = default)
    {
        var prompt = $@"Kamu adalah pembantu yang mengekstrak maklumat kenalan dari teks. Mungkin ada LEBIH DARI SATU kenalan dalam teks ini.

Teks: ""{rawText}""

Ekstrak SEMUA kenalan yang dijumpai. Untuk setiap kenalan, kembalikan:
{{
  ""name"": ""nama penuh"",
  ""honorific"": ""YB, YM, Dato, Datin, Dk, Hjh, Dr, Pg, Awg, Dyg"",
  ""gender"": ""male atau female"",
  ""jawatan"": ""jawatan"",
  ""kementerian"": ""kementerian"",
  ""department"": ""jabatan"",
  ""mobile"": ""nombor mobile"",
  ""phone"": ""nombor telefon pejabat"",
  ""email"": ""emel""
}}

Kembalikan SEBAGAI JSON ARRAY SAHAJA. Contoh: [{{ ... }}, {{ ... }}]
Jika tiada kenalan, kembalikan []. Jangan beri penjelasan lain.";

        var response = await ChatAsync(prompt, ct);
        if (string.IsNullOrWhiteSpace(response)) return null;

        try
        {
            var json = response.Trim();
            if (json.StartsWith("```")) json = json.Split("\n", 2).Last().Replace("```", "");
            json = json.Replace("```json", "").Replace("```", "").Trim();
            return JsonSerializer.Deserialize<List<EnrichedContact>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to parse bulk contacts: {Response}", response);
            return null;
        }
    }

    /// <summary>
    /// Suggest enrichment for missing fields based on patterns
    /// </summary>
    public async Task<EnrichedContact?> SuggestEnrichmentAsync(
        string name, string? jawatan, string? kementerian, string? department,
        string? existingBuilding, string? existingBahagian, CancellationToken ct = default)
    {
        var prompt = $@"Kamu adalah pembantu direktori kerajaan. Lengkapkan maklumat yang hilang berdasarkan pola biasa.

Nama: {name}
Jawatan: {jawatan ?? "tiada"}
Kementerian: {kementerian ?? "tiada"}
Department: {department ?? "tiada"}
Building: {existingBuilding ?? "tiada"}
Bahagian: {existingBahagian ?? "tiada"}

Berdasarkan pola biasa di kementerian dan jabatan kerajaan, cadangkan maklumat yang mungkin hilang. Kembalikan JSON SAHAJA:
{{
  ""department"": ""jabatan yang mungkin jika tiada"",
  ""bahagian"": ""bahagian yang mungkin jika tiada"",
  ""building"": ""bangunan yang mungkin jika tiada"",
  ""floor"": ""tingkat jika relevan"",
  ""email"": ""emel format biasa jika boleh dijangka""
}}

Hanya isi ruang yang anda YAKIN berdasarkan pola biasa. Gunakan null jika tidak pasti. Kembalikan JSON SAHAJA.";

        var response = await ChatAsync(prompt, ct);
        if (string.IsNullOrWhiteSpace(response)) return null;

        try
        {
            var json = response.Trim();
            if (json.StartsWith("```")) json = json.Split("\n", 2).Last().Replace("```", "");
            json = json.Replace("```json", "").Replace("```", "").Trim();
            return JsonSerializer.Deserialize<EnrichedContact>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to parse enrichment: {Response}", response);
            return null;
        }
    }

    private async Task<string?> ChatAsync(string prompt, CancellationToken ct = default)
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

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(15)); // 15s timeout for AI calls

            var response = await _http.PostAsync("https://api.deepseek.com/v1/chat/completions", content, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                _log.LogWarning("DeepSeek API returned {Code}", response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<DeepSeekResponse>(ct);
            return result?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();
        }
        catch (OperationCanceledException)
        {
            _log.LogWarning("DeepSeek API call timed out or was cancelled");
            return null;
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

public class ChatSearchResult
{
    [JsonPropertyName("explanation")] public string Explanation { get; set; } = "";
    [JsonPropertyName("kementerian")] public string? Kementerian { get; set; }
    [JsonPropertyName("department_keywords")] public string? DepartmentKeywords { get; set; }
    [JsonPropertyName("jawatan_keywords")] public string? JawatanKeywords { get; set; }
    [JsonPropertyName("name_keywords")] public string? NameKeywords { get; set; }
    [JsonPropertyName("honorific_filter")] public string? HonorificFilter { get; set; }
    [JsonPropertyName("tag_filter")] public string? TagFilter { get; set; }
    [JsonPropertyName("is_general")] public bool IsGeneral { get; set; }
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
