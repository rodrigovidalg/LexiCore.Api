using ProyectoAnalisisLexico.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Cargar cadena de conexión desde appsettings.json
var connectionString = builder.Configuration.GetConnectionString("MySQLConnection");

// Agregar EF Core con MySQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();
app.Run();
