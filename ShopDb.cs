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
            new User{Id = 1, Email = "user1@mail.com", Passwd = "password1", Role = "admininstrator"},
            new User{Id = 2, Email = "user2@mail.com", Passwd = "password2", Role = "customer"}

        );
    }
}
