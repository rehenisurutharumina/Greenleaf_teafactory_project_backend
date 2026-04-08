using GreenLeafTeaAPI.Data;
using GreenLeafTeaAPI.Middleware;
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
//    Priority: Environment variable → appsettings.json → fail
// ============================================================
var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "JWT secret key is not configured. Set the JWT_SECRET_KEY environment variable or Jwt:Key in appsettings.json.");

if (jwtKey.Length < 32)
{
    throw new InvalidOperationException("JWT secret key must be at least 32 characters long.");
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "GreenLeafTeaAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "GreenLeafTeaFrontend";
var jwtExpiryHours = builder.Configuration.GetValue<int>("Jwt:ExpiryHours", 24);

// Store JWT settings for TokenService to reuse
builder.Services.AddSingleton(new JwtSettings
{
    Key = jwtKey,
    Issuer = jwtIssuer,
    Audience = jwtAudience,
    ExpiryHours = jwtExpiryHours
});

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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromMinutes(1) // Tighten default 5-min skew
    };

    // Return consistent JSON for auth failures instead of empty 401/403
    options.Events = new JwtBearerEvents
    {
        OnChallenge = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            var result = JsonSerializer.Serialize(new { message = "Authentication required. Please log in." });
            return context.Response.WriteAsync(result);
        },
        OnForbidden = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            var result = JsonSerializer.Serialize(new { message = "You do not have permission to access this resource." });
            return context.Response.WriteAsync(result);
        }
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

var webRootPath = builder.Environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
if (!Directory.Exists(webRootPath))
{
    Directory.CreateDirectory(webRootPath);
}
builder.Environment.WebRootPath = webRootPath;

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

// Global exception handler — catches all unhandled exceptions
app.UseGlobalExceptionHandler();

// Only redirect to HTTPS in production (in dev, frontend uses HTTP)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseCors("AllowFrontend");
app.UseAuthentication();   // Must come before Authorization
app.UseAuthorization();
app.MapControllers();
app.Run();
