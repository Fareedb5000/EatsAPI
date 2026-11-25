using Microsoft.EntityFrameworkCore;

namespace EatsAPI.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<CustomerOrder> Orders => Set<CustomerOrder>();
    public DbSet<CustomerOrderItem> OrderItems => Set<CustomerOrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var users = modelBuilder.Entity<AppUser>();
        users.HasKey(user => user.Id);
        users.HasIndex(user => user.NormalizedEmail).IsUnique();
        users.HasIndex(user => user.NormalizedUserName).IsUnique();
        users.Property(user => user.UserName).HasMaxLength(50).IsRequired();
        users.Property(user => user.NormalizedUserName).HasMaxLength(50).IsRequired();
        users.Property(user => user.Email).HasMaxLength(254).IsRequired();
        users.Property(user => user.NormalizedEmail).HasMaxLength(254).IsRequired();
        users.Property(user => user.FirstName).HasMaxLength(100).IsRequired();
        users.Property(user => user.LastName).HasMaxLength(100).IsRequired();
        users.Property(user => user.PasswordHash).HasMaxLength(512).IsRequired();
        users.HasIndex(user => user.VendorRestaurantId)
            .IsUnique()
            .HasFilter("[VendorRestaurantId] IS NOT NULL");
        users.HasOne(user => user.VendorRestaurant)
            .WithOne()
            .HasForeignKey<AppUser>(user => user.VendorRestaurantId)
            .OnDelete(DeleteBehavior.Restrict);

        var restaurants = modelBuilder.Entity<Restaurant>();
        restaurants.HasKey(restaurant => restaurant.Id);
        restaurants.Property(restaurant => restaurant.Name).HasMaxLength(150).IsRequired();
        restaurants.Property(restaurant => restaurant.Cuisine).HasMaxLength(150).IsRequired();
        restaurants.Property(restaurant => restaurant.Rating).HasPrecision(3, 2);
        restaurants.Property(restaurant => restaurant.DeliveryTime).HasMaxLength(50).IsRequired();
        restaurants.Property(restaurant => restaurant.Price).HasMaxLength(20).IsRequired();
        restaurants.Property(restaurant => restaurant.ImageKey).HasMaxLength(20).IsRequired();
        restaurants.HasData(
            new Restaurant
            {
                Id = 1,
                Name = "The Italian Place",
                Cuisine = "Italian • Pizza",
                Rating = 4.6m,
                DeliveryTime = "30-40 min",
                Price = "$$",
                ImageKey = "italian",
                IsActive = true
            },
            new Restaurant
            {
                Id = 2,
                Name = "Sushi World",
                Cuisine = "Sushi • Japanese",
                Rating = 4.8m,
                DeliveryTime = "25-35 min",
                Price = "$$$",
                ImageKey = "sushi",
                IsActive = true
            });

        var menuItems = modelBuilder.Entity<MenuItem>();
        menuItems.HasKey(item => item.Id);
        menuItems.Property(item => item.Name).HasMaxLength(100).IsRequired();
        menuItems.Property(item => item.Description).HasMaxLength(500).IsRequired();
        menuItems.Property(item => item.Price).HasPrecision(10, 2);
        menuItems.Property(item => item.ImageUrl).HasMaxLength(500);
        menuItems.HasIndex(item => new { item.RestaurantId, item.IsAvailable });
        menuItems.HasOne(item => item.Restaurant)
            .WithMany()
            .HasForeignKey(item => item.RestaurantId)
            .OnDelete(DeleteBehavior.Cascade);

        var orders = modelBuilder.Entity<CustomerOrder>();
        orders.HasKey(order => order.Id);
        orders.Property(order => order.CustomerName).HasMaxLength(201).IsRequired();
        orders.Property(order => order.CustomerEmail).HasMaxLength(254).IsRequired();
        orders.Property(order => order.CustomerPhone).HasMaxLength(30).IsRequired();
        orders.Property(order => order.DeliveryAddress).HasMaxLength(300).IsRequired();
        orders.Property(order => order.Subtotal).HasPrecision(10, 2);
        orders.Property(order => order.DeliveryFee).HasPrecision(10, 2);
        orders.Property(order => order.ServiceFee).HasPrecision(10, 2);
        orders.Property(order => order.Total).HasPrecision(10, 2);
        orders.Property(order => order.Status).HasMaxLength(20).IsRequired();
        orders.HasIndex(order => new { order.RestaurantId, order.PlacedAt });
        orders.HasOne(order => order.Restaurant)
            .WithMany()
            .HasForeignKey(order => order.RestaurantId)
            .OnDelete(DeleteBehavior.Restrict);
        orders.HasOne(order => order.Customer)
            .WithMany()
            .HasForeignKey(order => order.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        var orderItems = modelBuilder.Entity<CustomerOrderItem>();
        orderItems.HasKey(item => item.Id);
        orderItems.Property(item => item.Name).HasMaxLength(100).IsRequired();
        orderItems.Property(item => item.UnitPrice).HasPrecision(10, 2);
        orderItems.HasOne(item => item.Order)
            .WithMany(order => order.Items)
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
