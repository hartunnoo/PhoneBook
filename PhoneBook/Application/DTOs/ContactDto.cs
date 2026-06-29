using System.ComponentModel.DataAnnotations;
using PhoneBook.Common.Constants;

namespace PhoneBook.Application.DTOs;

public class CreateContactDto
{
    [Required(ErrorMessage = "Nama diperlukan")]
    [MaxLength(DbConfig.MaxNameLength)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(DbConfig.MaxMobileLength)] public string? Mobile1 { get; set; }
    [MaxLength(DbConfig.MaxMobileLength)] public string? Mobile2 { get; set; }
    [MaxLength(DbConfig.MaxMobileLength)] public string? Mobile3 { get; set; }
    [MaxLength(DbConfig.MaxMobileLength)] public string? Mobile4 { get; set; }
    [MaxLength(DbConfig.MaxMobileLength)] public string? Phone1 { get; set; }
    [MaxLength(DbConfig.MaxMobileLength)] public string? Phone2 { get; set; }
    [MaxLength(DbConfig.MaxMobileLength)] public string? Phone3 { get; set; }
    [MaxLength(DbConfig.MaxMobileLength)] public string? Phone4 { get; set; }
    [MaxLength(DbConfig.MaxEmailLength)] public string? Email1 { get; set; }
    [MaxLength(DbConfig.MaxEmailLength)] public string? Email2 { get; set; }
    [MaxLength(DbConfig.MaxEmailLength)] public string? Email3 { get; set; }
    [MaxLength(DbConfig.MaxCompanyLength)] public string? Company { get; set; }
    [MaxLength(DbConfig.MaxCompanyLength)] public string? Kementerian { get; set; }
    [MaxLength(DbConfig.MaxCompanyLength)] public string? Department { get; set; }
    [MaxLength(DbConfig.MaxCompanyLength)] public string? Bahagian { get; set; }
    [MaxLength(DbConfig.MaxCompanyLength)] public string? Jawatan { get; set; }
    [MaxLength(DbConfig.MaxNotesLength)] public string? Notes { get; set; }
}

public class UpdateContactDto : CreateContactDto { }

public class ContactResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Mobile1 { get; set; } public string? Mobile2 { get; set; }
    public string? Mobile3 { get; set; } public string? Mobile4 { get; set; }
    public string? Phone1 { get; set; } public string? Phone2 { get; set; }
    public string? Phone3 { get; set; } public string? Phone4 { get; set; }
    public string? Email1 { get; set; } public string? Email2 { get; set; }
    public string? Email3 { get; set; }
    public string? Company { get; set; } public string? Kementerian { get; set; }
    public string? Department { get; set; } public string? Bahagian { get; set; }
    public string? Jawatan { get; set; } public string? Notes { get; set; }
    public string? PhotoPath { get; set; }
    public DateTime CreatedAt { get; set; }
}
