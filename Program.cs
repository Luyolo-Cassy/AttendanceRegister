using AttendanceRegister.Data;
using AttendanceRegister.Models;
using AttendanceRegister.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------------- Services ----------------

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddScoped<IAttendanceCodeGenerator, AttendanceCodeGenerator>();

// ---------------- AI ----------------

builder.Services.Configure<GeminiOptions>(
    builder.Configuration.GetSection("Gemini"));

builder.Services.AddHttpClient<
    IAiAssistantService,
    GeminiAssistantService>();


// ---------------- Razor Pages ----------------

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Student", "StudentOnly");
    options.Conventions.AuthorizeFolder("/Lecturer", "LecturerOnly");
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("StudentOnly",
        policy => policy.RequireRole("Student"));

    options.AddPolicy("LecturerOnly",
        policy => policy.RequireRole("Lecturer"));
});

var app = builder.Build();

// ---------------- Seed Data ----------------

using (var scope = app.Services.CreateScope())
{
    await SeedData.InitializeAsync(scope.ServiceProvider);
}

// ---------------- Pipeline ----------------

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

app.MapRazorPages();

// ---------------- Chat API ----------------

app.MapPost("/api/chat", async (
    ChatRequest request,
    System.Security.Claims.ClaimsPrincipal principal,
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext db,
    IAiAssistantService ai) =>
{
    var user = await userManager.GetUserAsync(principal);

    if (user is null)
        return Results.Unauthorized();

    var isLecturer =
        await userManager.IsInRoleAsync(user, "Lecturer");

    var systemPrompt =
        await ChatContextBuilder.BuildSystemPromptAsync(
            db,
            user,
            isLecturer);

    var history = request.History.Select(h =>
        new ChatTurn(h.Role, h.Content));

    var reply =
        await ai.ChatAsync(systemPrompt, history);

    return Results.Ok(new { reply });
})
.RequireAuthorization();

app.Run();