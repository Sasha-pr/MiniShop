
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MiniShop.Models;

namespace MiniShop.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserCartItem> UserCartItems => Set<UserCartItem>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<ProductLike> ProductLikes => Set<ProductLike>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductBookmark> ProductBookmarks => Set<ProductBookmark>();
    public DbSet<ProductWaitlist> ProductWaitlists => Set<ProductWaitlist>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ProductBookmark>()
            .HasIndex(b => new { b.UserId, b.ProductId })
            .IsUnique();
    }
}
