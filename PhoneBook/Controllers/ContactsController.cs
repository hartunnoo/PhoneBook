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

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var all = await _service.GetAllAsync(null, ct);
        return Ok(new { total = all.Count, favorites = all.Count(c => c.IsFavorite), ministries = all.Where(c => !string.IsNullOrWhiteSpace(c.Kementerian)).GroupBy(c => c.Kementerian!).Select(g => new { name = g.Key, count = g.Count() }).OrderByDescending(x => x.count).Take(5) });
    }

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
        await _service.LogAuditAsync(id, "Foto profil dikemaskini", user: User.Identity?.Name);
        return Ok(new { photoPath });
    }

    [HttpGet("{id}/vcard")]
    public async Task<IActionResult> GetVCard(int id, CancellationToken ct)
    {
        var c = await _service.GetByIdAsync(id, ct);
        if (c is null) return NotFound();

        var vcard = new System.Text.StringBuilder();
        vcard.AppendLine("BEGIN:VCARD"); vcard.AppendLine("VERSION:3.0");
        vcard.AppendLine($"FN:{c.Honorific} {c.Name}".Trim());
        vcard.AppendLine($"N:{c.Name};;;{c.Honorific};");
        if (!string.IsNullOrWhiteSpace(c.Jawatan)) vcard.AppendLine($"TITLE:{c.Jawatan}");
        if (!string.IsNullOrWhiteSpace(c.Kementerian)) vcard.AppendLine($"ORG:{c.Kementerian}");
        if (!string.IsNullOrWhiteSpace(c.Mobile1)) vcard.AppendLine($"TEL;TYPE=CELL:{c.Mobile1}");
        if (!string.IsNullOrWhiteSpace(c.Phone1)) vcard.AppendLine($"TEL;TYPE=WORK:{c.Phone1}");
        if (!string.IsNullOrWhiteSpace(c.Email1)) vcard.AppendLine($"EMAIL:{c.Email1}");
        if (!string.IsNullOrWhiteSpace(c.PhotoPath)) vcard.AppendLine($"PHOTO;VALUE=URI:{Request.Scheme}://{Request.Host}{c.PhotoPath}");
        vcard.AppendLine("END:VCARD");
        return File(System.Text.Encoding.UTF8.GetBytes(vcard.ToString()), "text/vcard", $"{c.Name}.vcf");
    }

    [HttpPut("{id}/favorite")]
    public async Task<IActionResult> ToggleFavorite(int id, CancellationToken ct)
    {
        var c = await _service.GetByIdAsync(id, ct);
        if (c is null) return NotFound();
        await _service.UpdateFavoriteAsync(id, !c.IsFavorite, ct);
        return Ok(new { isFavorite = !c.IsFavorite });
    }

    [HttpPost("import")]
    public async Task<IActionResult> ImportCsv(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest("No file");
        using var reader = new StreamReader(file.OpenReadStream());
        var text = await reader.ReadToEndAsync(ct);
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var count = 0;
        foreach (var line in lines.Skip(1)) // Skip header
        {
            var cols = line.Split(',');
            if (cols.Length < 1 || string.IsNullOrWhiteSpace(cols[0])) continue;
            var dto = new CreateContactDto { Name = cols[0].Trim().Trim('"') };
            if (cols.Length > 1) dto.Mobile1 = cols[1].Trim().Trim('"');
            if (cols.Length > 2) dto.Email1 = cols[2].Trim().Trim('"');
            if (cols.Length > 3) dto.Jawatan = cols[3].Trim().Trim('"');
            if (cols.Length > 4) dto.Kementerian = cols[4].Trim().Trim('"');
            await _service.CreateAsync(dto, ct);
            count++;
        }
        _log.Information("CSV import: {Count} contacts", count);
        return Ok(new { count });
    }

    [HttpGet("{id}/audit")]
    public async Task<IActionResult> GetAuditLogs(int id, CancellationToken ct)
    {
        var logs = await _service.GetAuditLogsAsync(id, ct);
        return Ok(logs.Select(l => new { l.Action, l.Detail, l.ByUser, Timestamp = l.Timestamp.ToString("o") }));
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
