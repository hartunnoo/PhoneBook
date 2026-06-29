using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace PhoneBook.Controllers;

[ApiController]
[Route("api/[controller]")]
[IgnoreAntiforgeryToken]
public class AuthController : ControllerBase
{
    private readonly SignInManager<IdentityUser> _signIn;
    private readonly UserManager<IdentityUser> _user;

    public AuthController(SignInManager<IdentityUser> signIn, UserManager<IdentityUser> user)
    {
        _signIn = signIn;
        _user = user;
    }

    [HttpGet("status")]
    public IActionResult Status()
        => Ok(new { IsAuthenticated = User.Identity?.IsAuthenticated ?? false, User.Identity?.Name });

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

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return Ok(new { message = "Logged out" });
    }
}

public class AuthRequest
{
    [Required, MinLength(3)] public string Username { get; set; } = "";
    [Required, MinLength(6)] public string Password { get; set; } = "";
}
