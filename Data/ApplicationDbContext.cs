using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MiniShop.Models;

namespace MiniShop.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<ProductReview> ProductReviews { get; set; } = null!;
    public DbSet<ProductLike> ProductLikes { get; set; } = null!;
    public DbSet<ProductBookmark> ProductBookmarks { get; set; } = null!;
    public DbSet<ProductWaitlist> ProductWaitlist { get; set; } = null!;
    public DbSet<UserCartItem> UserCartItems { get; set; } = null!;
    public DbSet<UserNotification> UserNotifications { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderItem> OrderItems { get; set; } = null!;
    public DbSet<ProductWaitlist> ProductWaitlists { get; set; } = null!;
    public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
    public DbSet<ReturnRequest> ReturnRequests { get; set; } = null!;

    // История изменения статусов заказов
    public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; } = null!;
    public DbSet<SupportRequest> SupportRequests { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ReturnRequest>()
            .HasOne(r => r.Order)
            .WithMany()
            .HasForeignKey(r => r.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ReturnRequest>()
            .HasOne(r => r.OrderItem)
            .WithMany()
            .HasForeignKey(r => r.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<UserNotification>()
            .HasOne(n => n.ReturnRequest)
            .WithMany()
            .HasForeignKey(n => n.ReturnRequestId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<OrderStatusHistory>()
            .HasOne(h => h.Order)
            .WithMany()
            .HasForeignKey(h => h.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<ChatMessage>()
            .HasOne(m => m.SupportRequest)
            .WithMany(r => r.Messages)
            .HasForeignKey(m => m.SupportRequestId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
