using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhoneBook.Services;

namespace PhoneBook.Controllers;

[ApiController]
[Route("api/rls")]
[Authorize]
public class RlsController : ControllerBase
{
    private readonly RowLevelSecurityService _rls;

    public RlsController(RowLevelSecurityService rls) => _rls = rls;

    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        // Get all unique users with access grants
        var all = await _rls.GetAllAccessGrantsAsync();
        var users = all.GroupBy(a => a.UserName).Select(g => new
        {
            userName = g.Key,
            isAdmin = g.Any(a => a.IsAdmin),
            ministries = g.Where(a => !string.IsNullOrWhiteSpace(a.Kementerian)).Select(a => a.Kementerian).ToList(),
            ids = g.Select(a => a.Id).ToList()
        });
        return Ok(users);
    }

    [HttpPost("grant")]
    public async Task<IActionResult> Grant([FromBody] RlsGrantRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.UserName)) return BadRequest("Username required");
        await _rls.GrantAccessAsync(req.UserName, req.Kementerian, req.IsAdmin);
        return Ok(new { message = "Access granted" });
    }

    [HttpDelete("revoke/{userName}")]
    public async Task<IActionResult> Revoke(string userName, [FromQuery] string? kementerian = null)
    {
        await _rls.RevokeAccessAsync(userName, kementerian);
        return Ok(new { message = "Access revoked" });
    }
}

public class RlsGrantRequest
{
    public string UserName { get; set; } = "";
    public string? Kementerian { get; set; }
    public bool IsAdmin { get; set; }
}
