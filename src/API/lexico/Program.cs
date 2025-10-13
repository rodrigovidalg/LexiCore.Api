using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;

using Lexico.Application.Services;
using Lexico.Application.Services.Email;
using Lexico.Application.Contracts.Email;

using Lexico.Infrastructure.Data;  // Asegúrate de que aquí esté ReporteRepository
using Lexico.Infrastructure.Email;

using Lexico.Application.Contracts;  // Asegúrate de que aquí esté IReporteRepository

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------------------------------
// Puerto dinámico (Railway/Render/etc) — local por defecto 8080
// -----------------------------------------------------------------------------
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://*:{port}");

// -----------------------------------------------------------------------------
// Swagger
// -----------------------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Lexico.API",
        Version = "v1",
        Description = "API de Léxico"
    });

    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

    // XML comments si está generado en el .csproj (GenerateDocumentationFile=true)
    var xmlName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlName);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
});

// Permitir IFormFile sin exigir [Consumes] por acción
builder.Services.Configure<ApiBehaviorOptions>(opt =>
{
    opt.SuppressConsumesConstraintForFormFileParameters = true;
});

// -----------------------------------------------------------------------------
// CORS
// -----------------------------------------------------------------------------
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("Default", p =>
    {
        if (allowedOrigins.Length == 0 || allowedOrigins.Contains("*"))
            p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        else
            p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    });
});

// -----------------------------------------------------------------------------
// Límites de subida (multipart/form-data)
// -----------------------------------------------------------------------------
var maxMultipart = builder.Configuration.GetValue<long?>("Uploads:MaxMultipartBodyLength") ?? 10_000_000; // 10 MB
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = maxMultipart;
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = maxMultipart;
});

// -----------------------------------------------------------------------------
// Infraestructura: conexiones y repos
// -----------------------------------------------------------------------------
builder.Services.AddSingleton<DapperConnectionFactory>();

// Repositorios (interfaces -> implementaciones)
// Cambié la ruta del repositorio para asegurarnos de que no hay ambigüedad en los namespaces
builder.Services.AddScoped<Lexico.Application.Contracts.IReporteRepository, Lexico.Infrastructure.Data.ReporteRepository>();

// Repositorios adicionales
builder.Services.AddScoped<IIdiomaRepository, IdiomaRepository>();
builder.Services.AddScoped<IDocumentoRepository, DocumentoRepository>();
builder.Services.AddScoped<IAnalisisRepository, AnalisisRepository>();
builder.Services.AddScoped<ILogProcesamientoRepository, LogProcesamientoRepository>();
builder.Services.AddScoped<IConfiguracionAnalisisRepository, ConfiguracionAnalisisRepository>();

// Servicios de aplicación (análisis, upload, reportes)
builder.Services.AddScoped<IAnalysisService, AnalysisService>();
builder.Services.AddScoped<IUploadDocumentoService, UploadDocumentoService>();
builder.Services.AddScoped<IReportService, ReportService>();

// -----------------------------------------------------------------------------
// Email: IEmailService (app) delega en IEmailSender (infra) — SMTP en dev / SendGrid en prod
// -----------------------------------------------------------------------------
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddScoped<IEmailSender>(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var env = sp.GetRequiredService<IHostEnvironment>();

    // Configuración para el correo SMTP en desarrollo
    var host = cfg["Email:Host"] ?? "localhost";
    var port = int.TryParse(cfg["Email:Port"], out var p) ? p : 25;
    var user = cfg["Email:User"];
    var pass = cfg["Email:Password"];
    var from = cfg["Email:From"] ?? "Lexico API <no-reply@localhost>";
    var useStartTls = bool.TryParse(cfg["Email:UseStartTls"], out var tls) && tls;

    return new SmtpEmailService(host, port, user, pass, from, useStartTls);
});

// -----------------------------------------------------------------------------
// Controllers + JSON
// -----------------------------------------------------------------------------
builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = null;
        o.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

var app = builder.Build();

// -----------------------------------------------------------------------------
// Proxy headers (X-Forwarded-For / X-Forwarded-Proto) — útil en PaaS
// -----------------------------------------------------------------------------
var fwdOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
fwdOptions.KnownNetworks.Clear();
fwdOptions.KnownProxies.Clear();
app.UseForwardedHeaders(fwdOptions);

// -----------------------------------------------------------------------------
// Swagger UI
// -----------------------------------------------------------------------------
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Lexico.API v1");
});

// -----------------------------------------------------------------------------
// CORS
// -----------------------------------------------------------------------------
app.UseCors("Default");

// app.UseHttpsRedirection(); // deshabilitado si usas proxy/terminación TLS externa

app.MapControllers();

// Health simple
app.MapGet("/health", () => Results.Ok(new { ok = true, ts = DateTime.UtcNow }));

app.Run();
