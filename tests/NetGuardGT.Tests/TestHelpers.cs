using Microsoft.EntityFrameworkCore;
using NetGuardGT.Api.Data;
using NetGuardGT.Api.Models;
using NetGuardGT.Api.Models.Enums;
using NetGuardGT.Api.Services;

namespace NetGuardGT.Tests;

/// <summary>
/// Reloj falso: permite fijar y mover "el ahora" a voluntad para probar de forma
/// determinista las reglas que dependen del tiempo (SLA y escalamiento).
/// </summary>
public class RelojFalso : IRelojSistema
{
    public DateTime Ahora { get; set; }

    public RelojFalso(DateTime inicial) => Ahora = inicial;
}

/// <summary>Utilidades comunes para las pruebas.</summary>
public static class TestHelpers
{
    /// <summary>
    /// Crea un contexto EF Core en memoria con un nombre único por test,
    /// de modo que las pruebas queden totalmente aisladas entre sí.
    /// </summary>
    public static AppDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>Agrega un técnico de la especialidad indicada y devuelve la entidad guardada.</summary>
    public static Tecnico AgregarTecnico(AppDbContext db, Especialidad especialidad, string nombre = "Técnico de Prueba")
    {
        var tecnico = new Tecnico { Nombre = nombre, Especialidad = especialidad };
        db.Tecnicos.Add(tecnico);
        db.SaveChanges();
        return tecnico;
    }
}
