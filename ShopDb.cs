using Microsoft.EntityFrameworkCore;

public class ShopDb : DbContext {
    public DbSet<ShopItem> ShopItems => Set<ShopItem>();
    public DbSet<User> Users => Set<User>();

        public ShopDb(DbContextOptions<ShopDb> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        /**
        * creating users. Of course in a real application 
        * we would have user endpoints and encrypted passwords...
        **/
    
        modelBuilder.Entity<User>().HasData(
            new User{Id = 1, Email = "user1@mail.com", Passwd = "$2a$11$ZBOWL6fFRfOziik1DXH3W.Hlw9pb83Y4whW6fvDg9gH6L74zSa.nu", Role = "administrator", IsActive = true},
            new User{Id = 2, Email = "user2@mail.com", Passwd = "$2a$11$ZBOWL6fFRfOziik1DXH3W.Hlw9pb83Y4whW6fvDg9gH6L74zSa.nu", Role = "customer", IsActive = true},
            new User{Id = 3, Email = "user3@mail.com", Passwd = "$2a$11$ZBOWL6fFRfOziik1DXH3W.Hlw9pb83Y4whW6fvDg9gH6L74zSa.nu", Role = "customer", IsActive = true}


        );

        // modelBuilder.Entity<ShopItem>().HasData(
        //     new ShopItem{Id=1, Name="Product1", Description="This is the description of product 1", Price=15, ImageUrl="https://via.placeholder.com/150"},
        //     new ShopItem{Id=2, Name="Product2", Description="This is the description of product 2", Price=35, ImageUrl="https://via.placeholder.com/150"},
        //     new ShopItem{Id=3, Name="Product3", Description="This is the description of product 3", Price=20, ImageUrl="https://via.placeholder.com/150"},
        //     new ShopItem{Id=4, Name="Product4", Description="This is the description of product 4", Price=15, ImageUrl="https://via.placeholder.com/150"},
        //     new ShopItem{Id=5, Name="Product5", Description="This is the description of product 5", Price=47, ImageUrl="https://via.placeholder.com/150"},
        //     new ShopItem{Id=6, Name="Product6", Description="This is the description of product 6", Price=90, ImageUrl="https://via.placeholder.com/150"},
        //     new ShopItem{Id=7, Name="Product7", Description="This is the description of product 7", Price=15, ImageUrl="https://via.placeholder.com/150"},
        //     new ShopItem{Id=8, Name="Product8", Description="This is the description of product 8", Price=220, ImageUrl="https://via.placeholder.com/150"}

        // );
    }
}
