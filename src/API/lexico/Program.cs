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

using Lexico.Infrastructure.Data;
using Lexico.Infrastructure.Email;

using Lexico.Application.Contracts;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------------------------------
// C O R S  ✅ CORREGIDO: Incluye tanto localhost como Railway
// -----------------------------------------------------------------------------
var allowedOrigins = new[]
{
    "http://localhost:3000",
    "http://localhost:5173",
    "http://localhost:5174",
    // ✅ AGREGADO: Permite solicitudes desde la propia API de Railway
    "https://lexicoreapi-production.up.railway.app"
    // Si tienes un frontend desplegado, agrégalo aquí también:
    // "https://tu-frontend-en-prod.com"
};

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontPolicy", policy =>
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()  // ✅ AGREGADO: Permite credenciales (tokens, cookies)
            .WithExposedHeaders("Content-Disposition")
    );
});


// -----------------------------------------------------------------------------
// Puerto dinámico (Railway) — local por defecto 8080
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

    var xmlName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlName);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
});

// Permitir IFormFile sin exigir [Consumes]
builder.Services.Configure<ApiBehaviorOptions>(opt =>
{
    opt.SuppressConsumesConstraintForFormFileParameters = true;
});

// -----------------------------------------------------------------------------
// Límites de subida (multipart/form-data)
// -----------------------------------------------------------------------------
var maxMultipart = builder.Configuration.GetValue<long?>("Uploads:MaxMultipartBodyLength") ?? 10_000_000; // 10 MB
builder.Services.Configure<FormOptions>(o => { o.MultipartBodyLengthLimit = maxMultipart; });
builder.WebHost.ConfigureKestrel(options => { options.Limits.MaxRequestBodySize = maxMultipart; });

// -----------------------------------------------------------------------------
// Infraestructura: conexiones y repos
// -----------------------------------------------------------------------------
builder.Services.AddSingleton<DapperConnectionFactory>();

builder.Services.AddScoped<Lexico.Application.Contracts.IReporteRepository, Lexico.Infrastructure.Data.ReporteRepository>();
builder.Services.AddScoped<IIdiomaRepository, IdiomaRepository>();
builder.Services.AddScoped<IDocumentoRepository, DocumentoRepository>();
builder.Services.AddScoped<IAnalisisRepository, AnalisisRepository>();
builder.Services.AddScoped<ILogProcesamientoRepository, LogProcesamientoRepository>();
builder.Services.AddScoped<IConfiguracionAnalisisRepository, ConfiguracionAnalisisRepository>();

// Servicios de aplicación
builder.Services.AddScoped<IAnalysisService, AnalysisService>();
builder.Services.AddScoped<IUploadDocumentoService, UploadDocumentoService>();
builder.Services.AddScoped<IReportService, ReportService>();

// Email
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IEmailSender>(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var host = cfg["Email:Host"] ?? "localhost";
    var port = int.TryParse(cfg["Email:Port"], out var p) ? p : 25;
    var user = cfg["Email:User"];
    var pass = cfg["Email:Password"];
    var from = cfg["Email:From"] ?? "Lexico API <no-reply@localhost>";
    var useStartTls = bool.TryParse(cfg["Email:UseStartTls"], out var tls) && tls;
    return new SmtpEmailService(host, port, user, pass, from, useStartTls);
});

// Controllers + JSON
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
// Proxy headers (PaaS)
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
app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Lexico.API v1"); });

// -----------------------------------------------------------------------------
// P I P E L I N E  (orden correcto)
// -----------------------------------------------------------------------------
app.UseRouting();

// ✅ CORS debe ir ANTES de Authentication/Authorization
app.UseCors("FrontPolicy");

// app.UseHttpsRedirection();     // deshabilitado si TLS termina en el proxy (Railway)

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health simple
app.MapGet("/health", () => Results.Ok(new { ok = true, ts = DateTime.UtcNow }));

app.Run();