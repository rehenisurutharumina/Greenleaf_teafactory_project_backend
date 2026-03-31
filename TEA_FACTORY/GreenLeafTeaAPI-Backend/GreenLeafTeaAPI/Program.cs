using GreenLeafTeaAPI.Data;
using GreenLeafTeaAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. Read frontend CORS origins from config
// ============================================================
var frontendOrigins = builder.Configuration
    .GetSection("Frontend:AllowedOrigins")
    .Get<string[]>()?
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .ToArray()
    ?? Array.Empty<string>();

// ============================================================
// 2. Add Controllers with camelCase JSON
// ============================================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// ============================================================
// 3. Database — MySQL via Pomelo EF Core
// ============================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("DefaultConnection string is missing in appsettings.json.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// ============================================================
// 4. JWT Authentication
// ============================================================
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is missing in appsettings.json.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "GreenLeafTeaAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "GreenLeafTeaFrontend";

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
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

// ============================================================
// 5. Register custom services
// ============================================================
builder.Services.AddSingleton<TokenService>();

// ============================================================
// 6. CORS
// ============================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (frontendOrigins.Length == 0 || frontendOrigins.Contains("*"))
        {
            policy
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
            return;
        }

        policy
            .WithOrigins(frontendOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// ============================================================
// 7. Swagger (dev only)
// ============================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ============================================================
// 8. Auto-create/migrate database on startup
// ============================================================
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

// ============================================================
// 9. Middleware pipeline
// ============================================================
// Only redirect to HTTPS in production (in dev, frontend uses HTTP)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowFrontend");
app.UseAuthentication();   // Must come before Authorization
app.UseAuthorization();
app.MapControllers();
app.Run();
