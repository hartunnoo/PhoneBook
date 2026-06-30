using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PhoneBook.Domain.Entities;

namespace PhoneBook.Infrastructure.Data;

public class PhoneBookDbContext : IdentityDbContext<IdentityUser>
{
    public PhoneBookDbContext(DbContextOptions<PhoneBookDbContext> options) : base(options) { }

    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<UserAccess> UserAccesses => Set<UserAccess>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.ToTable("Contacts");
            entity.HasIndex(c => c.Name);
            entity.HasIndex(c => c.Mobile1);
        });
    }
}
