using Microsoft.EntityFrameworkCore;



var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ShopDb>(opt => opt.UseInMemoryDatabase("shopDb"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
var app = builder.Build();

// Seed data on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ShopDb>();
    context.Database.EnsureCreated();
}


app.MapGet("/", () => "Welcome to shop api!");

/** 
* User endpoints
**/

app.MapPost("/login", async (HttpContext context, ShopDb db) =>
{

    var loginRequest = await context.Request.ReadFromJsonAsync<LoginRequest>();
    if (loginRequest != null)
    {
        var email = loginRequest.Email;
        string pass = loginRequest.Passwd;
        var foundUser = await db.Users.Where(u => u.Email == email).FirstOrDefaultAsync<User>();
        var obj = "{email:" + email + "password: " + pass + "}";
        //TO DO RETURN JSON WEB TOKEN
        return Results.Ok(foundUser);
    }
    else
    {
        return Results.NotFound("User not found");
    }
});

app.MapPost("/create-user", async (HttpContext context, ShopDb db) =>
{

    var user = await context.Request.ReadFromJsonAsync<User>();
    if (user == null)
    {
        return Results.BadRequest();
    }
    else
    {
        string email = user.Email;
        string pass = user.Passwd;
        string role = user.Role;
        User user1 = new()
        {
            Email = email,
            Passwd = pass,
            Role = role
        };
        await db.Users.AddAsync(user1);
        await db.SaveChangesAsync();

        //var obj = "{email:" + email + ", password: " + pass + ", role: " + role + "}";
        return Results.Created($"/get-user/{user1.Id}", user1);
    }

});

app.MapGet("/user/{id}", async (int id, ShopDb db)=> 
        await db.Users.FindAsync(id)
        is User user
            ? Results.Ok(user)
            : Results.NotFound());


app.MapGet("/list-users", async (ShopDb db)=> {
    var users = await db.Users.ToListAsync();
    return Results.Ok(users);
});

app.MapPut("/update-user", async (HttpContext context, ShopDb db) =>
{
    var user = await context.Request.ReadFromJsonAsync<User>();
    if (user == null)
    {
        return Results.BadRequest();
    }
    var userToBeUpdated = await db.Users.FindAsync(user.Id);
    if (userToBeUpdated == null)
    {
        return Results.NotFound();
    }
    userToBeUpdated.Email = user.Email;
    userToBeUpdated.Passwd = user.Passwd;
    userToBeUpdated.Role = user.Role;
    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapDelete("/users/{id}", async (int id, ShopDb db) =>
{
    if (await db.Users.FindAsync(id) is User user)
    {
        db.Users.Remove(user);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    return Results.NotFound();
});

/**
* Items endpoints
**/

app.MapPost("/create-item", async (HttpContext context, ShopDb db) =>
{

    var item = await context.Request.ReadFromJsonAsync<ShopItem>();
    if (item == null)
    {
        return Results.BadRequest();
    }
    else
    {
        string name = item.Name;
        string description = item.Description;
        int price = item.Price;
        ShopItem item1 = new()
        {
            Name = name,
            Description = description,
            Price = price
        };
        await db.ShopItems.AddAsync(item1);
        await db.SaveChangesAsync();
        return Results.Created($"/item/{item1.Id}", item1);
    }

});

app.MapGet("/item/{id}", async (int id, ShopDb db)=> 
        await db.ShopItems.FindAsync(id)
        is ShopItem item
            ? Results.Ok(item)
            : Results.NotFound());


app.MapGet("/list-items", async (ShopDb db)=> {
    var items = await db.ShopItems.ToListAsync();
    return Results.Ok(items);
});

app.MapPut("/update-item", async (HttpContext context, ShopDb db) =>
{
    var item = await context.Request.ReadFromJsonAsync<ShopItem>();
    if (item == null)
    {
        return Results.BadRequest();
    }
    var updatedItem = await db.ShopItems.FindAsync(item.Id);
    if (updatedItem == null)
    {
        return Results.NotFound();
    }
    updatedItem.Description = item.Description;
    updatedItem.Name = item.Name;
    updatedItem.Price = item.Price;
    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapDelete("/items/{id}", async (int id, ShopDb db) =>
{
    if (await db.ShopItems.FindAsync(id) is ShopItem item)
    {
        db.ShopItems.Remove(item);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    return Results.NotFound();
});

app.Run();
