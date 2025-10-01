using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using LexiCore.Infrastructure.Persistence;                    // AppDbContext
using LexiCore.Application.Features.Seguridad;                // IAuthService, AuthService, IJwtTokenService, JwtTokenService, IQrService, QrService, IQrCardGenerator, QrCardGenerator
using LexiCore.Application.Features.Seguridad.Notifications;  // INotificationService, SmtpEmailNotificationService
using LexiCore.Shared.Options;                                // JwtOptions, EmailOptions

var builder = WebApplication.CreateBuilder(args);

// 1) Connection string
var mysqlConn = builder.Configuration.GetConnectionString("MySQLConnection");
if (string.IsNullOrWhiteSpace(mysqlConn))
    throw new InvalidOperationException("ConnectionStrings:MySQLConnection no está configurada en appsettings.json.");

// 2) DbContext (Pomelo MySQL)
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseMySql(mysqlConn, ServerVersion.AutoDetect(mysqlConn))
    
    );

// 3) Options tipadas
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));

// 4) JWT Bearer
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "lexicore";
var jwtKey    = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("Jwt:Key no está configurado en appsettings.json.");

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.RequireHttpsMetadata = false; // DEV; en prod => true + HTTPS
        opt.SaveToken = true;
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false, // pon true si agregas Jwt:Audience
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// 5) CORS (dev)
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowAll", p => p
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// 6) DI de servicios de Seguridad
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IQrService, QrService>();
builder.Services.AddSingleton<IQrCardGenerator, QrCardGenerator>(); // stateless
builder.Services.AddScoped<INotificationService, SmtpEmailNotificationService>();

// 7) Controllers
builder.Services.AddControllers();

var app = builder.Build();

// 8) Middleware
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
