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
}