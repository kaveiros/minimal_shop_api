using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http.HttpResults;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Runtime.InteropServices;
using static BCrypt.Net.BCrypt;




var builder = WebApplication.CreateBuilder(args);

var configuration = new ConfigurationBuilder()
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .Build();

// Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = configuration["Jwt:Issuer"],
        ValidAudience = configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured.")))
    };
});


// Add authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireClaim("userType", "administrator"));
});

builder.Services.AddCors();


builder.Services.AddDbContext<ShopDb>(opt => opt.UseInMemoryDatabase("shopDb"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
var app = builder.Build();

app.UseAuthentication();
app.UseRouting();
app.UseAuthorization();


// Seed data on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ShopDb>();
    context.Database.EnsureCreated();
}

app.UseHttpsRedirection();



app.UseCors(builder => builder
.AllowAnyOrigin()
.AllowAnyMethod()
.AllowAnyHeader()
);

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
            .Where(u => u.Email == loginRequest.Email)
            .FirstOrDefaultAsync<User>();


        if (foundUser != null)
        {
            var passwordMatch = Verify(loginRequest.Passwd, foundUser.Passwd);
            if (passwordMatch)
            {
                // Create JWT token
                var jwtSettings = context.RequestServices.GetRequiredService<IConfiguration>().GetSection("Jwt");
                var keyEncoded = Encoding.UTF8.GetBytes(jwtSettings["Key"]);
                var tokenHandler = new JwtSecurityTokenHandler();
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                    new Claim(ClaimTypes.Email, foundUser.Email),
                    new Claim(ClaimTypes.Role, foundUser.Role),
                    new Claim("userId", foundUser.Id.ToString()),
                    new Claim("isActive", foundUser.IsActive.ToString())
                }),
                    Expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpireMinutes"])),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyEncoded), SecurityAlgorithms.HmacSha256Signature),
                    Issuer = jwtSettings["Issuer"],
                    Audience = jwtSettings["Audience"]
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var jwtToken = tokenHandler.WriteToken(token);

                return Results.Ok(new { Token = jwtToken });
            }
            else
            {
                return Results.NotFound(new { error = "Invalid email or password" });

            }

        }
    }

    return Results.NotFound(new { error = "Invalid email or password" });
}).Produces<String>(StatusCodes.Status200OK);


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
        string hashedPassword = HashPassword(pass);
        User user1 = new()
        {
            Email = email,
            Passwd = hashedPassword,
            Role = role,
            IsActive = true
        };
        await db.Users.AddAsync(user1);
        await db.SaveChangesAsync();
        return Results.Created($"/get-user/{user1.Id}", user1);
    }

}).RequireAuthorization("AdminOnly");

app.MapGet("/user/{id}", async (int id, ShopDb db) =>
        await db.Users.FindAsync(id)
        is User user
            ? Results.Ok(user)
            : Results.NotFound());


app.MapGet("/list-users", async (HttpContext context, ShopDb db) =>
{

    var usr = context.User;
    var users = await db.Users.ToListAsync();
    return Results.Ok(users);
}).RequireAuthorization("AdminOnly");

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
}).RequireAuthorization("AdminOnly");

app.MapDelete("/users/{id}", async (int id, ShopDb db) =>
{
    if (await db.Users.FindAsync(id) is User user)
    {
        db.Users.Remove(user);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    return Results.NotFound();
}).RequireAuthorization("AdminOnly");

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

}).RequireAuthorization("AdminOnly");

app.MapGet("/item/{id}", async (int id, ShopDb db) =>
        await db.ShopItems.FindAsync(id)
        is ShopItem item
            ? Results.Ok(item)
            : Results.NotFound());


app.MapGet("/list-items", async (ShopDb db) =>
{
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
}).RequireAuthorization("AdminOnly");

app.MapDelete("/items/{id}", async (int id, ShopDb db) =>
{
    if (await db.ShopItems.FindAsync(id) is ShopItem item)
    {
        db.ShopItems.Remove(item);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    return Results.NotFound();
}).RequireAuthorization("AdminOnly");


app.MapGet("/sync-items", async (ShopDb db) =>
{
    List<ShopItemJSON> products = new List<ShopItemJSON>();
    string fakeProductsAPI = "https://fakestoreapi.com";
    HttpClient client = new HttpClient();
    client.BaseAddress = new Uri(fakeProductsAPI);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    HttpResponseMessage response = await client.GetAsync("/products").ConfigureAwait(false);
    if (response.IsSuccessStatusCode)
    {
        var content = await response.Content.ReadAsStringAsync();
        products = JsonConvert.DeserializeObject<List<ShopItemJSON>>(content);
        foreach (var product in products)
        {
            Random random = new Random();
            int price = random.Next(1, 200);
            Rate rating = new()
            {
                Id = product.id,
                Count = product.rateJSON.count,
                Rating = product.rateJSON.rate

            };
            ShopItem shopItem = new()
            {
                Id = product.id,
                Name = product.title,
                Description = product.description,
                Price = price,
                Rate = rating,
                ImageUrl = product.image


            };
            db.ShopItems.Add(shopItem);

        }
        await db.SaveChangesAsync();
        return Results.Ok(products);
    }
    else
    {
        return Results.BadRequest();
    }
});

//TO DO add methods for storing the orders
app.MapPost("/create-order", () => { });

app.MapGet("/order/{id}", () => { });

app.MapGet("/list-orders", () => { });

app.MapDelete("/remove-order/{id}", () => { });

app.Run();
