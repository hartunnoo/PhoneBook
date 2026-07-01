using Microsoft.EntityFrameworkCore;
using PhoneBook.Common.Constants;
using PhoneBook.Domain.Entities;
using PhoneBook.Domain.Interfaces;
using PhoneBook.Infrastructure.Data;

namespace PhoneBook.Infrastructure.Repositories;

public class ContactRepository : IContactRepository
{
    private readonly PhoneBookDbContext _db;

    public ContactRepository(PhoneBookDbContext db) => _db = db;

    public async Task<Contact?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _db.Contacts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<List<Contact>> GetAllAsync(string? search = null, CancellationToken ct = default)
    {
        var query = BuildSearchQuery(_db.Contacts.AsNoTracking(), search);
        return await query.OrderBy(c => c.Name).ToListAsync(ct);
    }

    public async Task<(List<Contact> Items, int TotalCount)> GetPagedAsync(
        string? search, string? sort, List<string>? allowedMinistries,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = BuildSearchQuery(_db.Contacts.AsNoTracking(), search);

        // RLS filter at DB level
        query = ApplyRls(query, allowedMinistries);

        // Sort at DB level
        query = ApplySort(query, sort);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task<ContactStats> GetStatsAsync(List<string>? allowedMinistries, CancellationToken ct = default)
    {
        var query = ApplyRls(_db.Contacts.AsNoTracking(), allowedMinistries);

        var total = await query.CountAsync(ct);
        var favorites = await query.CountAsync(c => c.IsFavorite, ct);
        var ministries = await query
            .Where(c => !string.IsNullOrWhiteSpace(c.Kementerian))
            .GroupBy(c => c.Kementerian!)
            .Select(g => new MinistryCount { Name = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync(ct);

        return new ContactStats { Total = total, Favorites = favorites, Ministries = ministries };
    }

    public async Task<List<Contact>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default)
        => await _db.Contacts.AsNoTracking().Where(c => ids.Contains(c.Id)).ToListAsync(ct);

    public async Task<Contact> AddAsync(Contact contact, CancellationToken ct = default)
    {
        _db.Contacts.Add(contact);
        await _db.SaveChangesAsync(ct);
        return contact;
    }

    public async Task AddRangeAsync(IEnumerable<Contact> contacts, CancellationToken ct = default)
    {
        _db.Contacts.AddRange(contacts);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<Contact> UpdateAsync(Contact contact, CancellationToken ct = default)
    {
        _db.Contacts.Update(contact);
        await _db.SaveChangesAsync(ct);
        return contact;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var contact = await _db.Contacts.FindAsync([id], ct);
        if (contact is null) return false;
        _db.Contacts.Remove(contact);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task DeleteRangeAsync(IEnumerable<int> ids, CancellationToken ct = default)
    {
        var contacts = await _db.Contacts.Where(c => ids.Contains(c.Id)).ToListAsync(ct);
        if (contacts.Any())
        {
            _db.Contacts.RemoveRange(contacts);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<DateTime?> GetLastModifiedAsync(CancellationToken ct = default)
        => await _db.Contacts.MaxAsync(c => (DateTime?)c.UpdatedAt, ct);

    // ── Query builders ──

    static IQueryable<Contact> BuildSearchQuery(IQueryable<Contact> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search)) return query;

        var term = search.Trim().ToLower();
        return query.Where(c =>
            c.Name.ToLower().Contains(term)
            || (c.Mobile1 != null && c.Mobile1.Contains(term))
            || (c.Mobile2 != null && c.Mobile2.Contains(term))
            || (c.Company != null && c.Company.ToLower().Contains(term))
            || (c.Tags != null && c.Tags.ToLower().Contains(term))
            || (c.Kementerian != null && c.Kementerian.ToLower().Contains(term))
            || (c.Department != null && c.Department.ToLower().Contains(term)));
    }

    static IQueryable<Contact> ApplyRls(IQueryable<Contact> query, List<string>? allowedMinistries)
    {
        if (allowedMinistries is null) return query; // Admin — no filter
        if (!allowedMinistries.Any()) return query.Where(c => false); // No access — empty
        return query.Where(c => c.Kementerian != null && allowedMinistries.Contains(c.Kementerian));
    }

    static IQueryable<Contact> ApplySort(IQueryable<Contact> query, string? sort) => sort switch
    {
        AppConstants.SortByName => query.OrderBy(c => c.Name),
        AppConstants.SortByDate => query.OrderByDescending(c => c.CreatedAt),
        AppConstants.SortByDept => query.OrderBy(c => c.Department).ThenBy(c => c.Name),
        _ => query.OrderByDescending(c => c.IsFavorite).ThenBy(c => c.Name) // default: fav first
    };
}
