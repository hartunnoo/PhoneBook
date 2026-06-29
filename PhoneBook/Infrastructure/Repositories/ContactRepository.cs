using Microsoft.EntityFrameworkCore;
using PhoneBook.Domain.Entities;
using PhoneBook.Domain.Interfaces;
using PhoneBook.Infrastructure.Data;

namespace PhoneBook.Infrastructure.Repositories;

public class ContactRepository : IContactRepository
{
    private readonly PhoneBookDbContext _db;

    public ContactRepository(PhoneBookDbContext db) => _db = db;

    public async Task<Contact?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _db.Contacts.FindAsync([id], ct);

    public async Task<List<Contact>> GetAllAsync(string? search = null, CancellationToken ct = default)
    {
        var query = _db.Contacts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(term)
                || (c.Mobile1 != null && c.Mobile1.Contains(term))
                || (c.Mobile2 != null && c.Mobile2.Contains(term))
                || (c.Company != null && c.Company.ToLower().Contains(term)));
        }

        return await query.OrderBy(c => c.Name).ToListAsync(ct);
    }

    public async Task<Contact> AddAsync(Contact contact, CancellationToken ct = default)
    {
        _db.Contacts.Add(contact);
        await _db.SaveChangesAsync(ct);
        return contact;
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
}
