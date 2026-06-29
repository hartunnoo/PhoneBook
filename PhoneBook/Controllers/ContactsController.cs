using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhoneBook.Application.DTOs;
using PhoneBook.Application.Services;
using PhoneBook.Common.Constants;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using ILogger = Serilog.ILogger;

namespace PhoneBook.Controllers;

[ApiController]
[Route(ApiRoutes.Contacts)]
[Authorize]
[IgnoreAntiforgeryToken]
public class ContactsController : ControllerBase
{
    private readonly ContactService _service;
    private readonly ILogger _log = Log.ForContext<ContactsController>();

    public ContactsController(ContactService service) => _service = service;

    [HttpGet]
    public async Task<List<ContactResponseDto>> GetAll([FromQuery] string? search, CancellationToken ct)
        => await _service.GetAllAsync(search, ct);

    [HttpGet("{id}")]
    public async Task<ActionResult<ContactResponseDto>> Get(int id, CancellationToken ct)
    {
        var contact = await _service.GetByIdAsync(id, ct);
        return contact is null ? NotFound() : contact;
    }

    [HttpPost]
    public async Task<ActionResult<ContactResponseDto>> Create(CreateContactDto dto, CancellationToken ct)
    {
        var result = await _service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateContactDto dto, CancellationToken ct)
    {
        var result = await _service.UpdateAsync(id, dto, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await _service.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{id}/photo")]
    public async Task<IActionResult> UploadPhoto(int id, IFormFile photo, CancellationToken ct)
    {
        _log.Information("Photo upload requested for contact {Id}, file: {Name}, size: {Size}", id, photo?.FileName, photo?.Length);

        if (photo is null || photo.Length == 0) { _log.Warning("No file provided"); return BadRequest("No file"); }
        if (photo.Length > 5 * 1024 * 1024) { _log.Warning("File too large: {Size}", photo.Length); return BadRequest("Max 5MB"); }

        var contact = await _service.GetByIdAsync(id, ct);
        if (contact is null) { _log.Warning("Contact {Id} not found", id); return NotFound(); }

        // Delete old photo
        if (!string.IsNullOrWhiteSpace(contact.PhotoPath))
        {
            var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", contact.PhotoPath.TrimStart('/'));
            if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
        }

        // Ensure directory
        var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "photos");
        Directory.CreateDirectory(dir);

        // Resize + save with ImageSharp
        var fileName = $"{id}_{DateTime.UtcNow:yyyyMMddHHmmss}.jpg";
        var filePath = Path.Combine(dir, fileName);
        using var stream = photo.OpenReadStream();
        using var image = await Image.LoadAsync(stream, ct);
        image.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(300, 300), Mode = ResizeMode.Crop }));
        await image.SaveAsJpegAsync(filePath, ct);

        // Update contact
        var photoPath = $"/photos/{fileName}";
        await _service.UpdatePhotoAsync(id, photoPath, ct);

        _log.Information("Photo saved for contact {Id}: {Path}", id, photoPath);
        return Ok(new { photoPath });
    }

    [HttpDelete("{id}/photo")]
    public async Task<IActionResult> DeletePhoto(int id, CancellationToken ct)
    {
        var contact = await _service.GetByIdAsync(id, ct);
        if (contact is null) return NotFound();

        if (!string.IsNullOrWhiteSpace(contact.PhotoPath))
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", contact.PhotoPath.TrimStart('/'));
            if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
        }

        await _service.UpdatePhotoAsync(id, null, ct);
        _log.Information("Photo deleted for contact {Id}", id);
        return NoContent();
    }
}
