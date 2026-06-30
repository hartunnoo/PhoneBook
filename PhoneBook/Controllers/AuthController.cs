using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PhoneBook.Services;

namespace PhoneBook.Controllers;

[ApiController]
[Route("api/[controller]")]
[IgnoreAntiforgeryToken]
public class AuthController : ControllerBase
{
    private readonly SignInManager<IdentityUser> _signIn;
    private readonly UserManager<IdentityUser> _user;
    private readonly RowLevelSecurityService _rls;

    public AuthController(SignInManager<IdentityUser> signIn, UserManager<IdentityUser> user, RowLevelSecurityService rls)
    {
        _signIn = signIn;
        _user = user;
        _rls = rls;
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        var userName = User.Identity?.Name;
        bool isAdmin = false;
        if (User.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(userName))
        {
            var accesses = await _rls.GetAllowedMinistriesAsync(userName);
            isAdmin = accesses is null; // null = admin (all access)
        }
        return Ok(new { IsAuthenticated = User.Identity?.IsAuthenticated ?? false, User.Identity?.Name, IsAdmin = isAdmin });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] AuthRequest req)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = new IdentityUser { UserName = req.Username, Email = req.Username };
        var result = await _user.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await _signIn.SignInAsync(user, isPersistent: true);
        return Ok(new { message = "Registered" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] AuthRequest req)
    {
        var result = await _signIn.PasswordSignInAsync(req.Username, req.Password, true, false);
        if (!result.Succeeded)
            return Unauthorized(new { message = "Invalid credentials" });

        return Ok(new { message = "Logged in", req.Username });
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var user = await _user.GetUserAsync(User);
        if (user is null) return Unauthorized();
        var result = await _user.ChangePasswordAsync(user, req.CurrentPassword, req.NewPassword);
        if (!result.Succeeded) return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        return Ok(new { message = "Password changed" });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return Ok(new { message = "Logged out" });
    }
}

public class ChangePasswordRequest
{
    [Required] public string CurrentPassword { get; set; } = "";
    [Required, MinLength(4)] public string NewPassword { get; set; } = "";
}

public class AuthRequest
{
    [Required, MinLength(3)] public string Username { get; set; } = "";
    [Required, MinLength(6)] public string Password { get; set; } = "";
}
