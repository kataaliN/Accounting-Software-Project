using System.IO;
using System.Linq;
using FirstClassFinance.Services;
using FirstClassFinance.Interfaces;
using FirstClassFinance.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "FrontEnd")
});

// Add API Controllers
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

// Enable CORS for frontend (JS app)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// SQLite Database
builder.Services.AddDbContext<AppDBContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["Key"];
if (string.IsNullOrEmpty(secretKey))
{
    throw new Exception("JWT key is missing, please check the userSecrets.");
}
    
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

// Dependency Injection
builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<EventLogService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<ILedgerService, LedgerService>();
builder.Services.AddScoped<IValidationService, ValidationService>();
builder.Services.AddScoped<JournalService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

var app = builder.Build();

// Seed initial data for development
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDBContext>();
    context.Database.EnsureCreated(); // Ensure the DB and schema are created if they don't exist

    var adminUser = context.Users.FirstOrDefault(u => u.EmployeeUsername == "admin");
    if (adminUser == null)
    {
        context.Users.Add(new FirstClassFinance.Models.UserModel
        {
            EmployeeUsername = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Role = "Administrator",
            IsActive = true,
            EmailAddress = "kataalinashiru@gmail.com",
            ResetToken = "" // Added to satisfy the required property
        });
        context.SaveChanges();
    }
    else
    {
        // Update existing admin if needed
        bool changed = false;
        if (adminUser.EmailAddress != "kataalinashiru@gmail.com")
        {
            adminUser.EmailAddress = "kataalinashiru@gmail.com";
            changed = true;
        }
        
        // Robust check for password hashing
        bool needsRehash = false;
        try
        {
            // If it's plain text "Admin123!", Verify will throw or return false
            if (!BCrypt.Net.BCrypt.Verify("Admin123!", adminUser.PasswordHash))
            {
                needsRehash = true;
            }
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // This confirms the stored string is NOT a valid BCrypt hash (likely plain text)
            needsRehash = true;
        }

        if (needsRehash)
        {
            adminUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!");
            changed = true;
        }
        if (!adminUser.IsActive)
        {
            adminUser.IsActive = true;
            changed = true;
        }
        if (adminUser.Role != "Administrator")
        {
            adminUser.Role = "Administrator";
            changed = true;
        }

        if (changed)
        {
            context.SaveChanges();
        }
    }
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();