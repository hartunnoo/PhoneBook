using PhoneBook.Domain.Interfaces;
using PhoneBook.Infrastructure.Data;

namespace PhoneBook.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly PhoneBookDbContext _db;
    private IContactRepository? _contacts;

    public UnitOfWork(PhoneBookDbContext db) => _db = db;

    public IContactRepository Contacts =>
        _contacts ??= new ContactRepository(_db);

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);

    public void Dispose() => _db.Dispose();
}
