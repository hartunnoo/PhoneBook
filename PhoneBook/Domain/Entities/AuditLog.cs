using System.ComponentModel.DataAnnotations;

namespace PhoneBook.Domain.Entities;

public class AuditLog
{
    public int Id { get; set; }
    public int ContactId { get; set; }
    [MaxLength(200)] public string Action { get; set; } = string.Empty;
    [MaxLength(100)] public string? Detail { get; set; }
    [MaxLength(100)] public string? ByUser { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
