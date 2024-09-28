using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;



var builder = WebApplication.CreateBuilder(args);

// Add JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtSettings = builder.Configuration.GetSection("Jwt");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSettings["Key"]??String.Empty))
    };
});

// Add authorization
builder.Services.AddAuthorizationBuilder()
                        // Add authorization
                        .AddPolicy("AdminPolicy", policy => policy.RequireRole("admin"));


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
        var foundUser = await db.Users
            .Where(u => u.Email == loginRequest.Email && u.Passwd == loginRequest.Passwd)
            .FirstOrDefaultAsync<User>();

        if (foundUser != null)
        {
            // Create JWT token
            var jwtSettings = context.RequestServices.GetRequiredService<IConfiguration>().GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]?? String.Empty);
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Email, foundUser.Email),
                    new Claim(ClaimTypes.Role, foundUser.Role),
                    new Claim(ClaimTypes.PrimarySid, foundUser.Id.ToString())
                }),
                Expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpireMinutes"]?? "5")),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"]
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = tokenHandler.WriteToken(token);

            return Results.Ok(new { Token = jwtToken });
        }
    }

    return Results.NotFound("Invalid email or password");
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
