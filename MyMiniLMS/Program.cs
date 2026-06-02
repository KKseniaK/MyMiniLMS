using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using MyMiniLMS.Data;
using MyMiniLMS.Models;
using MyMiniLMS.Services;
using Npgsql;

namespace MyMiniLMS;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // MVC
        builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
        builder.Services
            .AddControllersWithViews()
            .AddViewLocalization();
        builder.Services.AddRazorPages();

        // PostgreSQL
        var databaseConnectionString = GetDatabaseConnectionString(builder.Configuration);

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                databaseConnectionString,
                npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()));

        // Identity
        builder.Services
            .AddDefaultIdentity<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        // Services
        builder.Services.AddScoped<CourseService>();
        builder.Services.AddScoped<AssignmentService>();
        builder.Services.AddScoped<StudentCourseService>();
        builder.Services.AddScoped<StudentService>();
        builder.Services.AddScoped<ReportService>();
        builder.Services.AddScoped<CalendarService>();

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        await DbInitializer.SeedAsync(app.Services);

        // Middleware pipeline
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        var supportedCultures = new[]
        {
            new CultureInfo("ru"),
            new CultureInfo("en")
        };

        app.UseRequestLocalization(new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture("ru"),
            SupportedCultures = supportedCultures,
            SupportedUICultures = supportedCultures
        });

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.MapRazorPages();

        app.Run();
    }

    private static string GetDatabaseConnectionString(IConfiguration configuration)
    {
        var databaseUrl = configuration["DATABASE_URL"];

        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            return ConvertDatabaseUrl(databaseUrl);
        }

        return configuration.GetConnectionString("DefaultConnection") ??
            throw new InvalidOperationException("Database connection string is not configured.");
    }

    private static string ConvertDatabaseUrl(string databaseUrl)
    {
        if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "postgres" && uri.Scheme != "postgresql"))
        {
            return databaseUrl;
        }

        var userInfo = uri.UserInfo.Split(':', 2);

        var connectionString = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : string.Empty,
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
            SslMode = SslMode.Require
        };

        if (!uri.IsDefaultPort)
        {
            connectionString.Port = uri.Port;
        }

        return connectionString.ConnectionString;
    }
}
