using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LeagueVisionSimple.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Setup the database connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));

// 2. Setup Microsoft Login system
// NOTE: RequireConfirmedAccount is set to FALSE so you can login immediately without checking a real email.
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddRazorPages();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// 3. FORCE LOGIN: Make sure nobody sees the site unless they are logged in or a guest
app.Use(async (context, next) =>
{
    var path = context.Request.Path.ToString().ToLower();

    // Allow access to login pages, css, and images so the login page can load properly
    if (path.StartsWith("/identity") || path.StartsWith("/css") || 
        path.StartsWith("/js") || path.StartsWith("/lib") || path.StartsWith("/images"))
    {
        await next();
        return;
    }

    // Check if they are logged in OR if they clicked "Guest"
    bool isLoggedIn = context.User.Identity?.IsAuthenticated ?? false;
    bool isGuest = context.Request.Cookies["IsGuest"] == "true";

    // If not logged in and not a guest, send them to the Login page
    if (!isLoggedIn && !isGuest)
    {
        context.Response.Redirect("/Identity/Account/Login");
        return;
    }

    await next();
});

app.MapRazorPages();
app.Run();