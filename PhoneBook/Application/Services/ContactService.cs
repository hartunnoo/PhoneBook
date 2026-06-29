using PhoneBook.Application.DTOs;
using PhoneBook.Domain.Entities;
using PhoneBook.Domain.Interfaces;

namespace PhoneBook.Application.Services;

public class ContactService
{
    private readonly IUnitOfWork _uow;
    public ContactService(IUnitOfWork uow) => _uow = uow;

    public async Task<List<ContactResponseDto>> GetAllAsync(string? search = null, CancellationToken ct = default)
        => (await _uow.Contacts.GetAllAsync(search, ct)).Select(Map).ToList();

    public async Task<ContactResponseDto?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _uow.Contacts.GetByIdAsync(id, ct) is { } c ? Map(c) : null;

    public async Task<ContactResponseDto> CreateAsync(CreateContactDto dto, CancellationToken ct = default)
        => Map(await _uow.Contacts.AddAsync(MapToEntity(dto, new Contact { CreatedAt = DateTime.UtcNow }), ct));

    public async Task<ContactResponseDto?> UpdateAsync(int id, UpdateContactDto dto, CancellationToken ct = default)
        => await _uow.Contacts.GetByIdAsync(id, ct) is { } c ? Map(await _uow.Contacts.UpdateAsync(MapToEntity(dto, c), ct)) : null;

    public Task<bool> DeleteAsync(int id, CancellationToken ct = default)
        => _uow.Contacts.DeleteAsync(id, ct);

    public async Task UpdatePhotoAsync(int id, string? photoPath, CancellationToken ct = default)
    {
        var c = await _uow.Contacts.GetByIdAsync(id, ct);
        if (c is not null) { c.PhotoPath = photoPath; await _uow.Contacts.UpdateAsync(c, ct); }
    }

    public async Task UpdateFavoriteAsync(int id, bool isFavorite, CancellationToken ct = default)
    {
        var c = await _uow.Contacts.GetByIdAsync(id, ct);
        if (c is not null) { c.IsFavorite = isFavorite; await _uow.Contacts.UpdateAsync(c, ct); }
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
}
