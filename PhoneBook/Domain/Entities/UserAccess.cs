using System.ComponentModel.DataAnnotations;

namespace PhoneBook.Domain.Entities;

public class UserAccess
{
    public int Id { get; set; }
    [MaxLength(256)] public string UserName { get; set; } = string.Empty;
    [MaxLength(100)] public string? Kementerian { get; set; } // null = ALL (admin)
    public bool IsAdmin { get; set; }
}
