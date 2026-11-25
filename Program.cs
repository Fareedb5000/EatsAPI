using System.Text;
using EatsAPI.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using EatsAPI.Services;
using EatsAPI.Services.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "Configure ConnectionStrings:DefaultConnection with a PostgreSQL connection string using user-secrets or an environment variable.")));
builder.Services.AddScoped<IRestaurantService, RestaurantService>();
builder.Services.AddHealthChecks();

var storageUrl = builder.Configuration["Storage:SupabaseUrl"];
var storageServiceRoleKey = builder.Configuration["Storage:SupabaseServiceRoleKey"];
var supabaseStorageConfigured =
    !string.IsNullOrWhiteSpace(storageUrl)
    || !string.IsNullOrWhiteSpace(storageServiceRoleKey);
if (supabaseStorageConfigured)
{
    if (string.IsNullOrWhiteSpace(storageUrl)
        || !Uri.TryCreate(storageUrl, UriKind.Absolute, out var storageUri)
        || storageUri.Scheme != Uri.UriSchemeHttps
        || string.IsNullOrWhiteSpace(storageServiceRoleKey))
    {
        throw new InvalidOperationException(
            "Configure Storage:SupabaseUrl as an HTTPS URL and Storage:SupabaseServiceRoleKey together.");
    }

    builder.Services.AddHttpClient<SupabaseMenuImageStorage>();
    builder.Services.AddScoped<IMenuImageStorage, SupabaseMenuImageStorage>();
}
else if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IMenuImageStorage, LocalMenuImageStorage>();
}
else
{
    throw new InvalidOperationException(
        "Configure Storage:SupabaseUrl and Storage:SupabaseServiceRoleKey for persistent menu image storage.");
}

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "Configure Jwt:Key with a secret of at least 32 bytes using user-secrets or an environment variable.");
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Savorly";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "Savorly";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddProblemDetails();
var configuredCorsOrigins = builder.Configuration["Cors:AllowedOrigins"];
string[] corsOrigins = string.IsNullOrWhiteSpace(configuredCorsOrigins)
    ? builder.Environment.IsDevelopment()
        ? ["http://localhost:8081", "http://localhost:8091", "http://pii_placeholder:8081"]
        : []
    : configuredCorsOrigins.Split(
        ';',
        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(options =>
{
    options.AddPolicy("Savorly", policy =>
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();

var app = builder.Build();

app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/swagger");
}

app.UseCors("Savorly");
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/healthz");

app.Run();
