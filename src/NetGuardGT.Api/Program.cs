using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using NetGuardGT.Api.Data;
using NetGuardGT.Api.Middleware;
using NetGuardGT.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Puerto para Render.com ──
// Render inyecta la variable de entorno PORT. Si existe, escuchamos en ese puerto
// en todas las interfaces (0.0.0.0). En local, launchSettings define los puertos.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// ── Controladores + serialización de enums como texto ──
// Así la API recibe/devuelve "Critico" en lugar de 0, mucho más legible.
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// ── Base de datos SQLite (EF Core) ──
var connectionString = builder.Configuration.GetConnectionString("Default")
                       ?? "Data Source=netguard.db";
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

// ── Inyección de dependencias de la capa de negocio ──
// El reloj es Singleton (no tiene estado); los servicios son Scoped (uno por petición),
// como el DbContext.
builder.Services.AddSingleton<IRelojSistema, RelojSistema>();
builder.Services.AddScoped<IIncidenteService, IncidenteService>();
builder.Services.AddScoped<ITecnicoService, TecnicoService>();
builder.Services.AddScoped<IReporteService, ReporteService>();

// ── Swagger / OpenAPI ──
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NetGuard GT — API de Gestión de Incidentes de Red",
        Version = "v1",
        Description = "Prototipo de API REST para registrar, asignar, dar seguimiento y reportar " +
                      "incidentes de red de NetGuard GT (Análisis de Sistemas I)."
    });

    // Incluye los comentarios /// en la documentación de Swagger.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// ── Creación y siembra automática de la base de datos ──
// EnsureCreated crea el archivo SQLite y el esquema si no existen, de modo que la app
// arranca con un solo "dotnet run", sin migraciones ni pasos manuales.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    DbSeeder.Inicializar(db);
}

// ── Pipeline HTTP ──
// El manejador de errores va primero para capturar las excepciones de toda la cadena.
app.UseMiddleware<ManejadorErroresMiddleware>();

// Swagger habilitado SIEMPRE (también en producción) para poder probar el prototipo en Render.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "NetGuard GT API v1");
    c.DocumentTitle = "NetGuard GT API";
});

app.MapControllers();

// La raíz redirige a Swagger para mayor comodidad al abrir la app.
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();
