using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;              // ⬅️ para licencia
using Seguridad.Api.Infrastructure;
using Seguridad.Api.Services;
using Seguridad.Api.Services.Notifications;

var builder = WebApplication.CreateBuilder(args);

// ---------- EF Core + MySQL ----------
builder.Services.AddDbContext<AppDbContext>(opts =>
{
    var cs = builder.Configuration.GetConnectionString("Default");
    opts.UseMySql(cs, ServerVersion.AutoDetect(cs));
});

// ---------- JWT ----------
var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services.Configure<JwtOptions>(jwtSection);
var keyStr = jwtSection["Key"];
if (string.IsNullOrWhiteSpace(keyStr))
    throw new InvalidOperationException("Jwt:Key no configurado.");
var key = Encoding.UTF8.GetBytes(keyStr);

builder.Services.AddAuthentication(o =>
{
    o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(o =>
{
    o.RequireHttpsMetadata = false;
    o.SaveToken = true;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// ---------- CORS (dev) ----------
builder.Services.AddCors(o =>
{
    o.AddPolicy("dev", p => p
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// ---------- Servicios de la app ----------
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// QR + Carnet PDF
builder.Services.AddScoped<IQrService, QrService>();
builder.Services.AddScoped<IQrCardGenerator, QrCardGenerator>();

// Notificaciones (SMTP o tu Gmail API). Estás configurando EmailOptions:
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
// Si usas SMTP:
builder.Services.AddScoped<INotificationService, SmtpEmailNotificationService>();
// Si usas Gmail API, registra tu propia implementación:
// builder.Services.AddScoped<INotificationService, GmailApiNotificationService>();

builder.Services.AddControllers();

// ---------- Swagger con Bearer ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Seguridad.Api", Version = "v1" });
    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "Ingrese el token JWT (Bearer {token})",
        Reference = new OpenApiReference { Id = JwtBearerDefaults.AuthenticationScheme, Type = ReferenceType.SecurityScheme }
    };
    c.AddSecurityDefinition(jwtSecurityScheme.Reference.Id, jwtSecurityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { jwtSecurityScheme, Array.Empty<string>() } });
});

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// CORS antes de Auth
app.UseCors("dev");

app.UseAuthentication();

// Middleware de revocación: verifica que el token esté activo en DB
app.Use(async (ctx, next) =>
{
    var auth = ctx.Request.Headers.Authorization.ToString();
    if (string.IsNullOrWhiteSpace(auth) ||
        !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        await next();
        return;
    }

    var token = auth[7..].Trim();
    var jwt = ctx.RequestServices.GetRequiredService<IJwtTokenService>();
    var db  = ctx.RequestServices.GetRequiredService<AppDbContext>();
    var hash = jwt.ComputeSha256(token);

    var activa = await db.Sesiones.AnyAsync(s => s.SessionTokenHash == hash && s.Activa);
    if (!activa)
    {
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await ctx.Response.WriteAsync("Sesión no activa o token revocado.");
        return;
    }

    await next();
});

app.UseAuthorization();

app.MapControllers();

app.Run();

public class JwtOptions
{
    public string Issuer { get; set; } = default!;
    public string Audience { get; set; } = default!;
    public string Key { get; set; } = default!;
    public int AccessTokenMinutes { get; set; } = 60;
}
public class EmailOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 465;
    public string User { get; set; } = string.Empty;
    public string Pass { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
}
