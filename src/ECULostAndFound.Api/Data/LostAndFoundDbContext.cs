using ECULostAndFound.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ECULostAndFound.Api.Data;

public class LostAndFoundDbContext : DbContext
{
    public LostAndFoundDbContext(
        DbContextOptions<LostAndFoundDbContext> options)
        : base(options)
    {
    }

    public DbSet<LostAndFoundItem> LostAndFoundItems =>
        Set<LostAndFoundItem>();

    public DbSet<ProcessedChange> ProcessedChanges =>
        Set<ProcessedChange>();

    public DbSet<AuditEntry> AuditEntries =>
        Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProcessedChange>()
            .HasIndex(change => change.ChangeId)
            .IsUnique();

        modelBuilder.Entity<AuditEntry>()
            .HasIndex(entry => entry.ChangeId)
            .IsUnique();
    }
}