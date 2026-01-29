using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }

    public DbSet<WebhookEvent> WebhookEvents { get; set; } = default!;
    public DbSet<HubSpotResponse> HubSpotResponses { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure relationship between WebhookEvent and HubSpotResponse
        modelBuilder.Entity<HubSpotResponse>()
            .HasOne(h => h.WebhookEvent)
            .WithMany()
            .HasForeignKey(h => h.WebhookEventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}