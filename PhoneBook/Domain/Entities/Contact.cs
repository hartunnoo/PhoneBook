using System.ComponentModel.DataAnnotations;
using PhoneBook.Common.Constants;

namespace PhoneBook.Domain.Entities;

public class Contact
{
    public int Id { get; set; }

    [Required, MaxLength(DbConfig.MaxNameLength)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(30)] public string? Honorific { get; set; }  // YB, YM, Dato, Datuk, etc.
    [MaxLength(DbConfig.MaxCompanyLength)] public string? Jawatan { get; set; }
    [MaxLength(DbConfig.MaxCompanyLength)] public string? Department { get; set; }
    [MaxLength(DbConfig.MaxCompanyLength)] public string? Bahagian { get; set; }
    [MaxLength(DbConfig.MaxCompanyLength)] public string? Kementerian { get; set; }
    [MaxLength(DbConfig.MaxCompanyLength)] public string? Company { get; set; }

    // Office location
    [MaxLength(DbConfig.MaxCompanyLength)] public string? Building { get; set; }
    [MaxLength(10)] public string? Floor { get; set; }
    [MaxLength(10)] public string? Room { get; set; }

    // Assistant / Secretary
    [MaxLength(DbConfig.MaxNameLength)] public string? PAName { get; set; }
    [MaxLength(DbConfig.MaxMobileLength)] public string? PAMobile { get; set; }
    [MaxLength(DbConfig.MaxEmailLength)] public string? PAEmail { get; set; }

    // Contact numbers
    [MaxLength(DbConfig.MaxMobileLength)] public string? Mobile1 { get; set; }
    [MaxLength(DbConfig.MaxMobileLength)] public string? Mobile2 { get; set; }
    [MaxLength(DbConfig.MaxMobileLength)] public string? Mobile3 { get; set; }
    [MaxLength(DbConfig.MaxMobileLength)] public string? Mobile4 { get; set; }
    [MaxLength(DbConfig.MaxMobileLength)] public string? Phone1 { get; set; }
    [MaxLength(DbConfig.MaxMobileLength)] public string? Phone2 { get; set; }
    [MaxLength(DbConfig.MaxMobileLength)] public string? Phone3 { get; set; }
    [MaxLength(DbConfig.MaxMobileLength)] public string? Phone4 { get; set; }

    // Emails
    [MaxLength(DbConfig.MaxEmailLength)] public string? Email1 { get; set; }
    [MaxLength(DbConfig.MaxEmailLength)] public string? Email2 { get; set; }
    [MaxLength(DbConfig.MaxEmailLength)] public string? Email3 { get; set; }

    [MaxLength(DbConfig.MaxNotesLength)] public string? Notes { get; set; }
    public string? PhotoPath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    [MaxLength(100)] public string? CreatedBy { get; set; }
}
