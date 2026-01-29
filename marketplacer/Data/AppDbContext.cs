using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }

    public DbSet<Seller> Sellers { get; set; } = default!;
    public DbSet<WebhookEvent> WebhookEvents { get; set; } = default!;
}