using PhoneBook.Domain.Entities;

namespace PhoneBook.Domain.Interfaces;

public interface IContactRepository
{
    Task<Contact?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<Contact>> GetAllAsync(string? search = null, CancellationToken ct = default);
    Task<(List<Contact> Items, int TotalCount)> GetPagedAsync(string? search, string? sort, List<string>? allowedMinistries, int page, int pageSize, CancellationToken ct = default);
    Task<ContactStats> GetStatsAsync(List<string>? allowedMinistries, CancellationToken ct = default);
    Task<List<Contact>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default);
    Task<Contact> AddAsync(Contact contact, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Contact> contacts, CancellationToken ct = default);
    Task<Contact> UpdateAsync(Contact contact, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task DeleteRangeAsync(IEnumerable<int> ids, CancellationToken ct = default);
    Task<DateTime?> GetLastModifiedAsync(CancellationToken ct = default);
}

public class ContactStats
{
    public int Total { get; set; }
    public int Favorites { get; set; }
    public List<MinistryCount> Ministries { get; set; } = new();
}

public class MinistryCount
{
    public string Name { get; set; } = "";
    public int Count { get; set; }
}
