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
    /// Smart semantic search — understands meaning beyond keywords
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SmartSearch([FromQuery] string q)
    {
        try
        {
        if (string.IsNullOrWhiteSpace(q)) return BadRequest("Query required");

        var all = await _contacts.GetAllAsync();
        var contacts = all.Select(c => new ContactInfo
        {
            Id = c.Id, Name = c.Name, Honorific = c.Honorific,
            Jawatan = c.Jawatan, Kementerian = c.Kementerian,
            Department = c.Department, Tags = c.Tags
        }).ToList();

        var matches = await _ai.SmartSearchAsync(contacts, q);
        var ids = matches.Select(i => contacts[i].Id).ToArray();
        return Ok(new { results = ids });
        }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    /// <summary>
    /// Parse raw text (email signature, business card) into structured contact fields
    /// </summary>
    [HttpPost("parse")]
    public async Task<IActionResult> ParseText([FromBody] ParseRequest req)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(req.Text)) return BadRequest("Text required");
            var result = await _ai.ParseContactAsync(req.Text);
            return result is null ? BadRequest(new { error = "Gagal parse teks" }) : Ok(result);
        }
        catch (Exception ex) { return StatusCode(500, new { error = ex.Message }); }
    }

    public class ParseRequest { public string Text { get; set; } = ""; }
}
