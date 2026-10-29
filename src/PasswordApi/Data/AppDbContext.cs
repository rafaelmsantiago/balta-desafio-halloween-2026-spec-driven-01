using Microsoft.EntityFrameworkCore;

namespace PasswordApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Password> Passwords => Set<Password>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Password>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Value).IsRequired().HasMaxLength(128);
            entity.Property(p => p.CreatedAtUtc).IsRequired();
        });
    }
}
