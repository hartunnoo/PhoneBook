using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhoneBook.Application.Services;
using PhoneBook.Services;

namespace PhoneBook.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly DeepSeekService _ai;
    private readonly ContactService _contacts;

    public AiController(DeepSeekService ai, ContactService contacts)
    {
        _ai = ai;
        _contacts = contacts;
    }

    /// <summary>
    /// Smart semantic search — understands meaning beyond keywords.
    /// Pre-filters with a cheap keyword match before sending to AI to reduce token cost.
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SmartSearch([FromQuery] string q, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q)) return BadRequest("Query required");

            // Pre-filter with cheap keyword search to reduce AI token cost
            var all = await _contacts.GetAllAsync(q, ct);
            var contacts = all.Take(200) // Cap at 200 to avoid token explosion
                .Select(c => new ContactInfo
                {
                    Id = c.Id, Name = c.Name, Honorific = c.Honorific,
                    Jawatan = c.Jawatan, Kementerian = c.Kementerian,
                    Department = c.Department, Tags = Truncate(c.Tags, 60)
                }).ToList();

            var matches = await _ai.SmartSearchAsync(contacts, q);
            var ids = matches.Select(i => contacts[i].Id).ToArray();
            return Ok(new { results = ids });
        }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    static string? Truncate(string? s, int max) =>
        s is { Length: > 0 } && s.Length > max ? s[..max] : s;

    /// <summary>
    /// Parse raw text (email signature, business card) into structured contact fields
    /// </summary>
    [HttpPost("parse")]
    public async Task<IActionResult> ParseText([FromBody] ParseRequest req, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(req.Text)) return BadRequest("Text required");
            var result = await _ai.ParseContactAsync(req.Text, ct);
            return result is null ? BadRequest(new { error = "Gagal parse teks" }) : Ok(result);
        }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    public class ParseRequest { public string Text { get; set; } = ""; }

    /// <summary>
    /// Conversational search — natural language queries in BM/English
    /// </summary>
    [HttpGet("chat")]
    public async Task<IActionResult> ConversationalSearch([FromQuery] string q, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(q)) return BadRequest(new { error = "Sila taip soalan anda" });
            var all = await _contacts.GetAllAsync(null, ct);
            var ministries = all.Select(c => c.Kementerian).Where(k => !string.IsNullOrWhiteSpace(k)).Distinct().OrderBy(k => k).ToList();
            var ai = await _ai.ConversationalSearchAsync(q, ministries!, ct);
            if (ai is null) return StatusCode(500, new { error = "AI tidak dapat memproses soalan. Cuba lagi." });

            var filtered = all.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(ai.Kementerian))
                filtered = filtered.Where(c => (c.Kementerian ?? "").Contains(ai.Kementerian, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(ai.DepartmentKeywords))
            { var words = ai.DepartmentKeywords.Split(',').Select(w => w.Trim()).Where(w => w.Length > 0).ToList();
              filtered = filtered.Where(c => words.Any(w => (c.Department ?? "").Contains(w, StringComparison.OrdinalIgnoreCase))); }
            if (!string.IsNullOrWhiteSpace(ai.JawatanKeywords))
            { var words = ai.JawatanKeywords.Split(',').Select(w => w.Trim()).Where(w => w.Length > 0).ToList();
              filtered = filtered.Where(c => words.Any(w => (c.Jawatan ?? "").Contains(w, StringComparison.OrdinalIgnoreCase))); }
            if (!string.IsNullOrWhiteSpace(ai.NameKeywords))
            { var words = ai.NameKeywords.Split(',').Select(w => w.Trim()).Where(w => w.Length > 0).ToList();
              filtered = filtered.Where(c => words.Any(w => (c.Name ?? "").Contains(w, StringComparison.OrdinalIgnoreCase))); }
            if (!string.IsNullOrWhiteSpace(ai.TagFilter))
            { var words = ai.TagFilter.Split(',').Select(t => t.Trim()).ToList();
              filtered = filtered.Where(c => words.Any(t => (c.Tags ?? "").Contains(t, StringComparison.OrdinalIgnoreCase))); }

            var results = filtered.Take(100).ToList();
            return Ok(new { explanation = ai.Explanation, total = results.Count, isGeneral = ai.IsGeneral,
                contacts = results.Select(c => new { c.Id, c.Name, c.Honorific, c.Jawatan, c.Department, c.Kementerian, c.Mobile1, c.Phone1, c.Email1, c.Building, c.Tags, c.IsFavorite }) });
        }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    /// <summary>Suggest tags based on jawatan + kementerian patterns</summary>
    [HttpPost("suggest-tags")]
    public async Task<IActionResult> SuggestTags([FromBody] TagRequest req, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { error = "Name required" });
            var tags = await _ai.SuggestTagsAsync(req.Name, req.Jawatan, req.Kementerian, req.Department, req.ExistingTags, ct);
            return tags is null ? Ok(new { tags = Array.Empty<string>() }) : Ok(new { tags });
        }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    /// <summary>Extract multiple contacts from unstructured text</summary>
    [HttpPost("bulk-parse")]
    public async Task<IActionResult> BulkParse([FromBody] ParseRequest req, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(req.Text)) return BadRequest("Text required");
            var result = await _ai.BulkParseContactsAsync(req.Text, ct);
            return result is null ? BadRequest(new { error = "Gagal parse teks" }) : Ok(new { contacts = result });
        }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    /// <summary>Suggest missing fields based on patterns</summary>
    [HttpPost("suggest-enrich")]
    public async Task<IActionResult> SuggestEnrich([FromBody] EnrichRequest req, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { error = "Name required" });
            var result = await _ai.SuggestEnrichmentAsync(req.Name, req.Jawatan, req.Kementerian, req.Department, req.Building, req.Bahagian, ct);
            return result is null ? Ok(new {}) : Ok(result);
        }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    public class TagRequest { public string Name { get; set; } = ""; public string? Jawatan { get; set; } public string? Kementerian { get; set; } public string? Department { get; set; } public string? ExistingTags { get; set; } }
    public class EnrichRequest { public string Name { get; set; } = ""; public string? Jawatan { get; set; } public string? Kementerian { get; set; } public string? Department { get; set; } public string? Building { get; set; } public string? Bahagian { get; set; } }
}
