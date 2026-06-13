using NetGuardGT.Api.DTOs;
using NetGuardGT.Api.Models.Enums;
using NetGuardGT.Api.Services;

namespace NetGuardGT.Tests;

/// <summary>
/// REGLA 5 — Escalamiento automático.
/// Un incidente Crítico o Urgente que sigue en Registrado por más de 2 horas debe escalarse.
/// Se usa un reloj falso: se crea el incidente "en el pasado" y luego se adelanta el reloj.
/// </summary>
public class EscalamientoTests
{
    private static readonly DateTime Ahora = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(Severidad.Critico)]
    [InlineData(Severidad.Urgente)]
    public async Task Escalar_CriticoOUrgenteConMasDe2h_SeEscala(Severidad severidad)
    {
        using var db = TestHelpers.CrearContexto();
        var reloj = new RelojFalso(Ahora);
        var service = new IncidenteService(db, reloj);

        // Se reporta hace 3 horas (supera el umbral de 2h) y sigue en Registrado.
        reloj.Ahora = Ahora.AddHours(-3);
        var inc = await service.CrearAsync(NuevoIncidente(severidad));
        reloj.Ahora = Ahora; // ahora han pasado 3 horas

        var escalados = await service.EscalarVencidosAsync();

        Assert.Single(escalados);
        Assert.Equal(inc.Id, escalados[0].Id);
        Assert.Equal(EstadoIncidente.Escalado, escalados[0].Estado);
    }

    [Fact]
    public async Task Escalar_CriticoConMenosDe2h_NoSeEscala()
    {
        using var db = TestHelpers.CrearContexto();
        var reloj = new RelojFalso(Ahora);
        var service = new IncidenteService(db, reloj);

        // Reportado hace solo 90 minutos: aún no debe escalarse.
        reloj.Ahora = Ahora.AddMinutes(-90);
        var inc = await service.CrearAsync(NuevoIncidente(Severidad.Critico));
        reloj.Ahora = Ahora;

        var escalados = await service.EscalarVencidosAsync();

        Assert.Empty(escalados);
        var detalle = await service.ObtenerPorIdAsync(inc.Id);
        Assert.Equal(EstadoIncidente.Registrado, detalle.Estado); // sigue en Registrado
    }

    [Fact]
    public async Task Escalar_SeveridadNoCriticaNiUrgente_NoSeEscala()
    {
        using var db = TestHelpers.CrearContexto();
        var reloj = new RelojFalso(Ahora);
        var service = new IncidenteService(db, reloj);

        // Un incidente de severidad Alta, aunque lleve mucho tiempo, NO se escala automáticamente.
        reloj.Ahora = Ahora.AddHours(-10);
        await service.CrearAsync(NuevoIncidente(Severidad.Alta));
        reloj.Ahora = Ahora;

        var escalados = await service.EscalarVencidosAsync();

        Assert.Empty(escalados);
    }

    [Fact]
    public async Task Escalar_CriticoYaAsignado_NoSeEscala()
    {
        using var db = TestHelpers.CrearContexto();
        var tecnico = TestHelpers.AgregarTecnico(db, Especialidad.FibraOptica);
        var reloj = new RelojFalso(Ahora);
        var service = new IncidenteService(db, reloj);

        // Crítico de hace 3h pero ya asignado (no está en Registrado): no se escala.
        reloj.Ahora = Ahora.AddHours(-3);
        var inc = await service.CrearAsync(NuevoIncidente(Severidad.Critico));
        await service.AsignarAsync(inc.Id, new AsignarTecnicoDto { TecnicoId = tecnico.Id });
        reloj.Ahora = Ahora;

        var escalados = await service.EscalarVencidosAsync();

        Assert.Empty(escalados);
    }

    private static CrearIncidenteDto NuevoIncidente(Severidad severidad) => new()
    {
        SitioRed = "Sitio-Crit",
        TipoIncidente = "Falla crítica",
        EspecialidadRequerida = Especialidad.FibraOptica,
        Severidad = severidad,
        Descripcion = "Incidente de prueba para escalamiento"
    };
}
