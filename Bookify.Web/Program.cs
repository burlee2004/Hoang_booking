using Bookify.Data.Data;
using Bookify.Data.Models;
using Bookify.Services.ModelsRepos;
using Bookify.Web.Areas.Identity.Data;
using Bookify.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// ✅ Connection String
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ Identity Setup
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
    options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

// ✅ Repositories
builder.Services.AddScoped<RoomRepo>();
builder.Services.AddScoped<RoomTypeRepo>();
builder.Services.AddScoped<ReservationRepo>();
builder.Services.AddScoped<ReservationItemRepo>();
builder.Services.AddScoped<PaymentRepo>();
builder.Services.AddScoped<RoomRepo>();

// VNPay
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<VNPayService>();


builder.Services.AddControllersWithViews();

// Add Session 
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60); // مدة تخزين الـ cart في الجلسة
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// read stripe keys
var stripeSection = builder.Configuration.GetSection("Stripe");
StripeConfiguration.ApiKey = stripeSection.GetValue<string>("SecretKey");

var app = builder.Build();

// ✅ Configure Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
// DÒNG QUAN TRỌNG NHẤT: Tự động bẻ lái HTTP sang HTTPS
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();


app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

// ✅ Routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.MapRazorPages();


using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Tự động apply migrations khi khởi động
    context.Database.Migrate();

    // Đảm bảo bảng ContactMessages tồn tại (tạo nếu chưa có)
    context.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ContactMessages' AND xtype='U')
        BEGIN
            CREATE TABLE [ContactMessages] (
                [Id]         INT            NOT NULL IDENTITY(1,1),
                [FullName]   NVARCHAR(100)  NOT NULL,
                [Email]      NVARCHAR(150)  NOT NULL,
                [Message]    NVARCHAR(1000) NOT NULL,
                [UserId]     NVARCHAR(MAX)  NULL,
                [SentAt]     DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
                [IsRead]     BIT            NOT NULL DEFAULT 0,
                [AdminReply] NVARCHAR(MAX)  NULL,
                [RepliedAt]  DATETIME2      NULL,
                [IsUserRead] BIT            NOT NULL DEFAULT 0,
                CONSTRAINT [PK_ContactMessages] PRIMARY KEY ([Id])
            )
        END
        ELSE
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('ContactMessages') AND name='AdminReply')
                ALTER TABLE [ContactMessages] ADD [AdminReply] NVARCHAR(MAX) NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('ContactMessages') AND name='RepliedAt')
                ALTER TABLE [ContactMessages] ADD [RepliedAt] DATETIME2 NULL;
            IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id=OBJECT_ID('ContactMessages') AND name='IsUserRead')
                ALTER TABLE [ContactMessages] ADD [IsUserRead] BIT NOT NULL DEFAULT 0;
        END
    ");

    string[] roles = { "Admin", "Customer" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    var adminEmail = "admin@123.com";
    var adminPass = "Admin@123";
    var admin = await userManager.FindByEmailAsync(adminEmail);
    if (admin == null)
    {
        admin = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(admin, adminPass);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "Admin");
            var userId = await userManager.GetUserIdAsync(admin);
            //user profile creation
            var profile = new UserProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId
            };
            context.UserProfiles.Add(profile);
            await context.SaveChangesAsync();
        }
    }
}

app.Run();
