using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhoneBook.Application.DTOs;
using PhoneBook.Application.Services;
using PhoneBook.Common.Constants;
using PhoneBook.Domain.Interfaces;
using PhoneBook.Services;
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
    private readonly RowLevelSecurityService _rls;
    private readonly ILogger _log = Log.ForContext<ContactsController>();

    public ContactsController(ContactService service, RowLevelSecurityService rls) { _service = service; _rls = rls; }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? sort, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        // RLS: Get user's allowed ministries
        var allowedMinistries = await _rls.GetAllowedMinistriesAsync(User.Identity?.Name);

        // ETag support — check if client has current version
        if (string.IsNullOrWhiteSpace(search) && string.IsNullOrWhiteSpace(sort))
        {
            var lastMod = await _service.GetLastModifiedAsync(ct);
            if (lastMod.HasValue)
            {
                var etag = $"\"{lastMod.Value.Ticks}\"";
                Response.Headers["ETag"] = etag;
                if (Request.Headers.IfNoneMatch == etag) return StatusCode(304);
            }
        }

        var (items, total) = await _service.GetPagedAsync(search, sort, allowedMinistries, page, pageSize, ct);
        return Ok(new { items, total, page, pageSize, maxPages = (int)Math.Ceiling(total / (double)pageSize) });
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var allowedMinistries = await _rls.GetAllowedMinistriesAsync(User.Identity?.Name);
        var stats = await _service.GetStatsAsync(allowedMinistries, ct);
        return Ok(new {
            total = stats.Total,
            favorites = stats.Favorites,
            ministries = stats.Ministries.Select(m => new { name = m.Name, count = m.Count }),
            maxPages = (int)Math.Ceiling(stats.Total / 50.0)
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ContactResponseDto>> Get(int id, CancellationToken ct)
    {
        var contact = await _service.GetByIdAsync(id, ct);
        return contact is null ? NotFound() : contact;
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportCsv(CancellationToken ct)
    {
        var all = await _service.GetAllAsync(null, ct);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Name,Mobile,Phone,Email,Jawatan,Kementerian,Department,Bahagian,Company,Tags");
        foreach (var c in all.OrderBy(c => c.Name))
            sb.AppendLine($"\"{c.Name}\",{c.Mobile1},{c.Phone1},{c.Email1},{c.Jawatan},{c.Kementerian},{c.Department},{c.Bahagian},{c.Company},{c.Tags}");
        return File(System.Text.Encoding.UTF8.GetBytes(sb.ToString()), AppConstants.ContentCsv, AppConstants.CsvFileName);
    }

    [HttpPost("bulk-delete")]
    public async Task<IActionResult> BulkDelete([FromBody] int[] ids, CancellationToken ct)
    {
        await _service.DeleteRangeAsync(ids, ct);
        await _service.LogAuditAsync(0, $"{ids.Length} kenalan dipadam secara pukal", user: User.Identity?.Name, ct: ct);
        return Ok(new { deleted = ids.Length });
    }

    [HttpPost("merge")]
    public async Task<IActionResult> Merge([FromBody] int[] ids, CancellationToken ct)
    {
        if (ids.Length < 2) return BadRequest("Need at least 2 IDs");

        // Fetch all contacts in a single query
        var contacts = await _service.GetByIdsAsync(ids, ct);
        if (!contacts.Any()) return NotFound();

        var primary = contacts.First();
        var toDelete = new List<int>();

        foreach (var c in contacts.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(primary.Mobile1) && !string.IsNullOrWhiteSpace(c.Mobile1)) primary.Mobile1 = c.Mobile1;
            if (string.IsNullOrWhiteSpace(primary.Email1) && !string.IsNullOrWhiteSpace(c.Email1)) primary.Email1 = c.Email1;
            toDelete.Add(c.Id);
        }

        await _service.UpdateAsync(primary.Id, new UpdateContactDto { Name = primary.Name, Mobile1 = primary.Mobile1, Email1 = primary.Email1, Jawatan = primary.Jawatan }, ct);
        if (toDelete.Any()) await _service.DeleteRangeAsync(toDelete, ct);
        await _service.LogAuditAsync(primary.Id, $"{ids.Length} kenalan digabungkan", user: User.Identity?.Name, ct: ct);
        return Ok(new { merged = ids.Length });
    }

    [HttpPost]
    public async Task<ActionResult<ContactResponseDto>> Create(CreateContactDto dto, CancellationToken ct)
    {
        var result = await _service.CreateAsync(dto, ct);
        await _service.LogAuditAsync(result.Id, "Kenalan dicipta", user: User.Identity?.Name, ct: ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateContactDto dto, CancellationToken ct)
    {
        var result = await _service.UpdateAsync(id, dto, ct);
        if (result is not null) await _service.LogAuditAsync(id, "Kenalan dikemaskini", user: User.Identity?.Name, ct: ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var contact = await _service.GetByIdAsync(id, ct);
        var deleted = await _service.DeleteAsync(id, ct);
        if (deleted) await _service.LogAuditAsync(id, $"Kenalan dipadam: {contact?.Name}", user: User.Identity?.Name, ct: ct);
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
            var oldPath = Path.Combine(Directory.GetCurrentDirectory(), AppConstants.WwwRoot, contact.PhotoPath.TrimStart('/'));
            if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
        }

        // Ensure directory
        var dir = Path.Combine(Directory.GetCurrentDirectory(), AppConstants.WwwRoot, AppConstants.PhotosDir);
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
        return File(System.Text.Encoding.UTF8.GetBytes(vcard.ToString()), AppConstants.ContentVCard, $"{c.Name}.vcf");
    }

    [HttpPut("{id}/favorite")]
    public async Task<IActionResult> ToggleFavorite(int id, CancellationToken ct)
    {
        var c = await _service.GetByIdAsync(id, ct);
        if (c is null) return NotFound();
        var newVal = !c.IsFavorite;
        await _service.UpdateFavoriteAsync(id, newVal, ct);
        await _service.LogAuditAsync(id, newVal ? "Ditanda favorit" : "Favorit dibuang", user: User.Identity?.Name, ct: ct);
        return Ok(new { isFavorite = newVal });
    }

    [HttpPost("import")]
    public async Task<IActionResult> ImportCsv(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest("No file");
        using var reader = new StreamReader(file.OpenReadStream());
        var text = await reader.ReadToEndAsync(ct);
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var dtos = new List<CreateContactDto>();
        foreach (var line in lines.Skip(1)) // Skip header
        {
            var cols = line.Split(',');
            if (cols.Length < 1 || string.IsNullOrWhiteSpace(cols[0])) continue;
            var dto = new CreateContactDto { Name = cols[0].Trim().Trim('"') };
            if (cols.Length > 1) dto.Mobile1 = cols[1].Trim().Trim('"');
            if (cols.Length > 2) dto.Email1 = cols[2].Trim().Trim('"');
            if (cols.Length > 3) dto.Jawatan = cols[3].Trim().Trim('"');
            if (cols.Length > 4) dto.Kementerian = cols[4].Trim().Trim('"');
            dtos.Add(dto);
        }
        await _service.AddRangeAsync(dtos, ct);
        var count = dtos.Count;
        _log.Information("CSV import: {Count} contacts", count);
        await _service.LogAuditAsync(0, $"{count} kenalan diimport melalui CSV", user: User.Identity?.Name, ct: ct);
        return Ok(new { count });
    }

    [HttpGet("{id}/audit")]
    public async Task<IActionResult> GetAuditLogs(int id, CancellationToken ct)
    {
        var logs = await _service.GetAuditLogsAsync(id, ct);
        return Ok(logs.Select(l => new { l.Action, l.Detail, l.ByUser, Timestamp = l.Timestamp.ToString(DateTimeFormat.RoundTrip) }));
    }

    [HttpDelete("{id}/photo")]
    public async Task<IActionResult> DeletePhoto(int id, CancellationToken ct)
    {
        var contact = await _service.GetByIdAsync(id, ct);
        if (contact is null) return NotFound();

        if (!string.IsNullOrWhiteSpace(contact.PhotoPath))
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), AppConstants.WwwRoot, contact.PhotoPath.TrimStart('/'));
            if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
        }

        await _service.UpdatePhotoAsync(id, null, ct);
        await _service.LogAuditAsync(id, "Foto profil dipadam", user: User.Identity?.Name, ct: ct);
        _log.Information("Photo deleted for contact {Id}", id);
        return NoContent();
    }
}
