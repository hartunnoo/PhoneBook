using Microsoft.EntityFrameworkCore;
using PhoneBook.Domain.Entities;
using PhoneBook.Infrastructure.Data;

namespace PhoneBook.Services;

public class RowLevelSecurityService
{
    private readonly PhoneBookDbContext _db;

    public RowLevelSecurityService(PhoneBookDbContext db) => _db = db;

    /// <summary>
    /// Returns list of Kementerian names the user can access.
    /// Returns null if user is admin (access to ALL).
    /// </summary>
    public async Task<List<string>?> GetAllowedMinistriesAsync(string? userName)
    {
        if (string.IsNullOrWhiteSpace(userName)) return null;

        var accesses = await _db.UserAccesses
            .Where(a => a.UserName == userName)
            .ToListAsync();

        if (!accesses.Any()) return new List<string>(); // No access
        if (accesses.Any(a => a.IsAdmin)) return null;   // Admin = all access

        return accesses
            .Where(a => !string.IsNullOrWhiteSpace(a.Kementerian))
            .Select(a => a.Kementerian!)
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// Grant access to a ministry for a user
    /// </summary>
    public async Task GrantAccessAsync(string userName, string? kementerian = null, bool isAdmin = false)
    {
        // Check if already exists
        var existing = await _db.UserAccesses
            .FirstOrDefaultAsync(a => a.UserName == userName && a.Kementerian == kementerian);
        if (existing is not null) return;

        _db.UserAccesses.Add(new UserAccess
        {
            UserName = userName,
            Kementerian = kementerian,
            IsAdmin = isAdmin
        });
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Remove access
    /// </summary>
    public async Task RevokeAccessAsync(string userName, string? kementerian = null)
    {
        var toRemove = await _db.UserAccesses
            .Where(a => a.UserName == userName && a.Kementerian == kementerian)
            .ToListAsync();
        _db.UserAccesses.RemoveRange(toRemove);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Get current user's access list for display
    /// </summary>
    public async Task<List<UserAccess>> GetUserAccessAsync(string? userName)
    {
        if (string.IsNullOrWhiteSpace(userName)) return new List<UserAccess>();
        return await _db.UserAccesses.Where(a => a.UserName == userName).ToListAsync();
    }
}
