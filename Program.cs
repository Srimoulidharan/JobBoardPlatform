using JobBoardPlatform.Data;
using JobBoardPlatform.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add MVC
builder.Services.AddControllersWithViews();

// Session services
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// SQLite connection string
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

// Configure Entity Framework with SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

var app = builder.Build();

// Apply migrations and seed admin user
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();

        // Use this when using migrations:
        db.Database.Migrate();

        // Seed default admin account
        SeedAdminUser(db, app.Configuration, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration or seed failed. Check the DefaultConnection value.");
    }
}

// Error handling
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Middleware pipeline
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static void SeedAdminUser(ApplicationDbContext db, IConfiguration configuration, ILogger logger)
{
    var adminEmail = (configuration["SeedAdmin:Email"] ?? "admin@jobboard.local")
        .Trim()
        .ToLowerInvariant();

    var adminPassword = configuration["SeedAdmin:Password"] ?? "Admin@123";

    if (db.Users.Any(u => u.Role == "Admin"))
    {
        return;
    }

    db.Users.Add(new User
    {
        FullName = "System Admin",
        Email = adminEmail,
        PasswordHash = ComputeSha256Hash(adminPassword),
        Role = "Admin",
        IsActive = true
    });

    db.SaveChanges();

    logger.LogInformation(
        "Seeded default admin user {Email}. Change SeedAdmin:Password after first login.",
        adminEmail
    );
}

static string ComputeSha256Hash(string rawData)
{
    using var sha256 = SHA256.Create();

    var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));

    var builder = new StringBuilder(bytes.Length * 2);

    foreach (var b in bytes)
    {
        builder.Append(b.ToString("x2"));
    }

    return builder.ToString();
}