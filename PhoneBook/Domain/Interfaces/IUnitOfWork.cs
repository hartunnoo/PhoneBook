namespace PhoneBook.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IContactRepository Contacts { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
