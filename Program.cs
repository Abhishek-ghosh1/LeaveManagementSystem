using Leave_Management_System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);
//Think of builder as a temporary object where you configure your application before the application actually starts.


// ✅ Add MVC with runtime compilation
builder.Services.AddControllersWithViews() //This tells ASP.NET Core:"This application uses MVC Controllers + Views."
    .AddRazorRuntimeCompilation();//Razor views to be recompiled while the application is running when you modify them.

// ✅ Register DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ConnectionString")));

// Add Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

var app = builder.Build();//takes all that configuration and builds the actual web application.

// ✅ Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // serve CSS, JS, images, etc.

app.UseRouting();

// Authentication middleware
app.UseAuthentication();

app.UseAuthorization();

// ✅ Default route to login page
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
