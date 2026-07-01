using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PhoneBook.Application.DTOs;
using PhoneBook.Common.Constants;
using PhoneBook.Domain.Entities;
using PhoneBook.Domain.Interfaces;
using PhoneBook.Infrastructure.Data;

namespace PhoneBook.Application.Services;

public class ContactService
{
    private readonly IUnitOfWork _uow;
    private readonly PhoneBookDbContext _db;
    private readonly IMemoryCache _cache;

    public ContactService(IUnitOfWork uow, PhoneBookDbContext db, IMemoryCache cache)
    { _uow = uow; _db = db; _cache = cache; }

    public async Task<List<ContactResponseDto>> GetAllAsync(string? search = null, CancellationToken ct = default)
    {
        var cacheKey = CacheKeys.AllContacts;
        if (!_cache.TryGetValue(cacheKey, out List<Contact>? cached))
        {
            cached = await _uow.Contacts.GetAllAsync(search, ct);
            _cache.Set(cacheKey, cached, new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(15)));
        }
        return cached!.Select(Map).ToList();
    }

    public async Task<(List<ContactResponseDto> Items, int TotalCount)> GetPagedAsync(
        string? search, string? sort, List<string>? allowedMinistries,
        int page, int pageSize, CancellationToken ct = default)
    {
        var cacheKey = $"{CacheKeys.AllContacts}:paged:{search}:{sort}:{page}:{pageSize}";
        if (!_cache.TryGetValue(cacheKey, out (List<ContactResponseDto> Items, int TotalCount) cached))
        {
            var (items, total) = await _uow.Contacts.GetPagedAsync(search, sort, allowedMinistries, page, pageSize, ct);
            cached = (items.Select(Map).ToList(), total);
            _cache.Set(cacheKey, cached, new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(15)));
        }
        return cached;
    }

    public async Task<ContactStats> GetStatsAsync(List<string>? allowedMinistries, CancellationToken ct = default)
    {
        var ministriesKey = allowedMinistries is null ? "admin" : string.Join(",", allowedMinistries.OrderBy(x => x));
        var cacheKey = $"{CacheKeys.AllContacts}:stats:{ministriesKey}";
        if (!_cache.TryGetValue(cacheKey, out ContactStats? cached))
        {
            cached = await _uow.Contacts.GetStatsAsync(allowedMinistries, ct);
            _cache.Set(cacheKey, cached, new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(15)));
        }
        return cached!;
    }

    public async Task<ContactResponseDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var cacheKey = CacheKeys.ContactById(id);
        if (!_cache.TryGetValue(cacheKey, out Contact? cached))
        {
            cached = await _uow.Contacts.GetByIdAsync(id, ct);
            if (cached is not null)
                _cache.Set(cacheKey, cached, new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(15)));
        }
        return cached is { } c ? Map(c) : null;
    }

    public async Task<List<ContactResponseDto>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default)
        => (await _uow.Contacts.GetByIdsAsync(ids, ct)).Select(Map).ToList();

    public async Task<ContactResponseDto> CreateAsync(CreateContactDto dto, CancellationToken ct = default)
    {
        var result = Map(await _uow.Contacts.AddAsync(MapToEntity(dto, new Contact { CreatedAt = DateTime.UtcNow }), ct));
        InvalidateCache();
        return result;
    }

    public async Task AddRangeAsync(IEnumerable<CreateContactDto> dtos, CancellationToken ct = default)
    {
        var contacts = dtos.Select(dto => MapToEntity(dto, new Contact { CreatedAt = DateTime.UtcNow }));
        await _uow.Contacts.AddRangeAsync(contacts, ct);
        InvalidateCache();
    }

    public async Task<ContactResponseDto?> UpdateAsync(int id, UpdateContactDto dto, CancellationToken ct = default)
    {
        var c = await _uow.Contacts.GetByIdAsync(id, ct);
        if (c is null) return null;
        var result = Map(await _uow.Contacts.UpdateAsync(MapToEntity(dto, c), ct));
        InvalidateCache();
        return result;
    }

    public Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        InvalidateCache();
        return _uow.Contacts.DeleteAsync(id, ct);
    }

    public async Task DeleteRangeAsync(IEnumerable<int> ids, CancellationToken ct = default)
    {
        await _uow.Contacts.DeleteRangeAsync(ids, ct);
        InvalidateCache();
    }

    public async Task UpdatePhotoAsync(int id, string? photoPath, CancellationToken ct = default)
    {
        var c = await _uow.Contacts.GetByIdAsync(id, ct);
        if (c is not null) { c.PhotoPath = photoPath; await _uow.Contacts.UpdateAsync(c, ct); }
        InvalidateCache();
    }

    public async Task UpdateFavoriteAsync(int id, bool isFavorite, CancellationToken ct = default)
    {
        var c = await _uow.Contacts.GetByIdAsync(id, ct);
        if (c is not null) { c.IsFavorite = isFavorite; await _uow.Contacts.UpdateAsync(c, ct); }
        InvalidateCache();
    }

    static Contact MapToEntity(CreateContactDto d, Contact c)
    {
        c.Name = d.Name; c.Gender = d.Gender; c.Honorific = d.Honorific;
        c.Jawatan = d.Jawatan; c.Department = d.Department; c.Bahagian = d.Bahagian;
        c.Kementerian = d.Kementerian; c.Company = d.Company;
        c.Building = d.Building; c.Floor = d.Floor; c.Room = d.Room;
        c.PAName = d.PAName; c.PAMobile = d.PAMobile; c.PAEmail = d.PAEmail;
        c.Mobile1 = d.Mobile1; c.Mobile2 = d.Mobile2; c.Mobile3 = d.Mobile3; c.Mobile4 = d.Mobile4;
        c.Phone1 = d.Phone1; c.Phone2 = d.Phone2; c.Phone3 = d.Phone3; c.Phone4 = d.Phone4;
        c.Email1 = d.Email1; c.Email2 = d.Email2; c.Email3 = d.Email3;
        c.Tags = d.Tags; c.IsFavorite = d.IsFavorite;
        c.Notes = d.Notes; c.UpdatedAt = DateTime.UtcNow;
        return c;
    }

    static ContactResponseDto Map(Contact c) => new()
    {
        Id = c.Id, Name = c.Name, Gender = c.Gender, Honorific = c.Honorific,
        Jawatan = c.Jawatan, Department = c.Department, Bahagian = c.Bahagian,
        Kementerian = c.Kementerian, Company = c.Company,
        Building = c.Building, Floor = c.Floor, Room = c.Room,
        PAName = c.PAName, PAMobile = c.PAMobile, PAEmail = c.PAEmail,
        Mobile1 = c.Mobile1, Mobile2 = c.Mobile2, Mobile3 = c.Mobile3, Mobile4 = c.Mobile4,
        Phone1 = c.Phone1, Phone2 = c.Phone2, Phone3 = c.Phone3, Phone4 = c.Phone4,
        Email1 = c.Email1, Email2 = c.Email2, Email3 = c.Email3,
        Tags = c.Tags, IsFavorite = c.IsFavorite, Notes = c.Notes,
        PhotoPath = c.PhotoPath, CreatedBy = c.CreatedBy,
        CreatedAt = c.CreatedAt, UpdatedAt = c.UpdatedAt
    };

    public async Task<List<AuditLog>> GetAuditLogsAsync(int contactId, CancellationToken ct = default)
        => await _db.AuditLogs.Where(a => a.ContactId == contactId).OrderByDescending(a => a.Timestamp).Take(10).ToListAsync(ct);

    public async Task LogAuditAsync(int contactId, string action, string? detail = null, string? user = null, CancellationToken ct = default)
    {
        _db.AuditLogs.Add(new AuditLog { ContactId = contactId, Action = action, Detail = detail, ByUser = user, Timestamp = DateTime.UtcNow });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<DateTime?> GetLastModifiedAsync(CancellationToken ct = default)
        => await _uow.Contacts.GetLastModifiedAsync(ct);

    void InvalidateCache()
    {
        // Remove the main "all contacts" cache entry to force refresh on next read.
        // Individual contact cache entries will naturally expire or be overwritten.
        _cache.Remove(CacheKeys.AllContacts);
    }
}
