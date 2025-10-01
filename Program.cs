using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite("Data Source=bookstore.db"));

// Add Repositories
builder.Services.AddScoped<IBookRepository, BookRepository>();

// JWT Auth
var jwtKey = "super_secret_key_12345_67890_abcdefg";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token in the text input below.\r\n\r\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9\""
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});


builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();



app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.UseCors();

// Register
app.MapPost("/register", async (AppDbContext db, UserDto userDto) =>
{
    var user = new User
    {
        Username = userDto.Username,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Username + "123"), // Simple hash for demo
        Budget = userDto.Budget,
        IsSeller = userDto.IsSeller
    };
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Ok();
});

// Login
app.MapPost("/login", async (AppDbContext db, UserDto userDto) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == userDto.Username);
    if (user == null || !BCrypt.Net.BCrypt.Verify(userDto.Username + "123", user.PasswordHash))
        return Results.Unauthorized();

    var token = JwtHelper.GenerateToken(user, jwtKey);
    return Results.Ok(new { token });
});

// Get all books
app.MapGet("/books", async (IBookRepository repo) =>
{
    var books = await repo.GetAllAsync();
    return Results.Ok(books.Select(b => new BookDto
    {
        Id = b.Id,
        Title = b.Title,
        Author = b.Author,
        Price = b.Price,
        Category = b.Category?.Name ?? "",
        Seller = b.Seller?.Username ?? ""
    }));
});
// Seller: Add book
app.MapPost("/books", async (IBookRepository repo, AppDbContext db, BookDto dto, HttpContext ctx) =>
{
    var userId = int.Parse(ctx.User.Claims.First(c => c.Type == "UserId").Value);
    var user = await db.Users.FindAsync(userId);
    if (user == null || !user.IsSeller) return Results.Forbid();

    var category = await db.Categories.FirstOrDefaultAsync(c => c.Name == dto.Category);
    if (category == null)
    {
        category = new Category { Name = dto.Category };
        db.Categories.Add(category);
        await db.SaveChangesAsync(); // Save to get the Id
    }

    var book = new Book
    {
        Title = dto.Title,
        Author = dto.Author,
        Price = dto.Price,
        CategoryId = category.Id,
        SellerId = user.Id
    };
    await repo.AddAsync(book);
    return Results.Ok();
}).RequireAuthorization();


// Seller: Edit book
app.MapPut("/books/{id}", async (IBookRepository repo, int id, BookDto dto, HttpContext ctx) =>
{
    var userId = int.Parse(ctx.User.Claims.First(c => c.Type == "UserId").Value);
    var book = await repo.GetByIdAsync(id);
    if (book == null || book.SellerId != userId) return Results.Forbid();

    book.Title = dto.Title;
    book.Author = dto.Author;
    book.Price = dto.Price;
    await repo.UpdateAsync(book);
    return Results.Ok();
}).RequireAuthorization();

// Seller: Remove book
app.MapDelete("/books/{id}", async (IBookRepository repo, int id, HttpContext ctx) =>
{
    var userId = int.Parse(ctx.User.Claims.First(c => c.Type == "UserId").Value);
    var book = await repo.GetByIdAsync(id);
    if (book == null || book.SellerId != userId) return Results.Forbid();

    await repo.DeleteAsync(id);
    return Results.Ok();
}).RequireAuthorization();

// Client: Buy book
app.MapPost("/buy/{bookId}", async (AppDbContext db, int bookId, HttpContext ctx) =>
{
    var userId = int.Parse(ctx.User.Claims.First(c => c.Type == "UserId").Value);
    var user = await db.Users.FindAsync(userId);
    var book = await db.Books.FindAsync(bookId);
    if (user == null || book == null) return Results.NotFound();
    if (user.Budget < book.Price) return Results.BadRequest("Not enough money");

    user.Budget -= book.Price;
    var order = new Order { Book = book, User = user, OrderDate = DateTime.UtcNow };
    db.Orders.Add(order);
    await db.SaveChangesAsync();
    return Results.Ok();
}).RequireAuthorization();

// Seller: See posted books
app.MapGet("/mybooks", async (AppDbContext db, HttpContext ctx) =>
{
    var userId = int.Parse(ctx.User.Claims.First(c => c.Type == "UserId").Value);
    var books = await db.Books.Where(b => b.SellerId == userId).ToListAsync();
    return Results.Ok(books);
}).RequireAuthorization();

// Client: See orders
app.MapGet("/myorders", async (AppDbContext db, HttpContext ctx) =>
{
    var userId = int.Parse(ctx.User.Claims.First(c => c.Type == "UserId").Value);
    var orders = await db.Orders.Include(o => o.Book).Where(o => o.UserId == userId).ToListAsync();
    return Results.Ok(orders.Select(o => new OrderDto
    {
        Id = o.Id,
        BookTitle = o.Book?.Title ?? "",
        Buyer = o.User?.Username ?? "",
        OrderDate = o.OrderDate
    }));
}).RequireAuthorization();
app.MapGet("/", () => "Book Store API is running!");

// Open Swagger UI in browser
var url = "https://localhost:7160/swagger";
try
{
    if (OperatingSystem.IsWindows())
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
    else if (OperatingSystem.IsMacOS())
    {
        System.Diagnostics.Process.Start("open", url);
    }
    else if (OperatingSystem.IsLinux())
    {
        System.Diagnostics.Process.Start("xdg-open", url);
    }
}
catch
{
    // Ignore errors
}
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();
