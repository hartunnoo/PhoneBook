using PhoneBook.Domain.Entities;

namespace PhoneBook.Domain.Interfaces;

public interface IContactRepository
{
    Task<Contact?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<Contact>> GetAllAsync(string? search = null, CancellationToken ct = default);
    Task<Contact> AddAsync(Contact contact, CancellationToken ct = default);
    Task<Contact> UpdateAsync(Contact contact, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
