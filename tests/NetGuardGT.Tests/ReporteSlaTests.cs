using NetGuardGT.Api.Models;
using NetGuardGT.Api.Models.Enums;
using NetGuardGT.Api.Services;

namespace NetGuardGT.Tests;

/// <summary>
/// REGLA 8 — Reporte de cumplimiento de SLA.
/// Verifica que cada incidente se clasifique correctamente y que los totales y el
/// porcentaje de cumplimiento se calculen bien.
/// </summary>
public class ReporteSlaTests
{
    private static readonly DateTime Ahora = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ReporteSla_ClasificaCumplidosEIncumplidos()
    {
        using var db = TestHelpers.CrearContexto();

        db.Incidentes.AddRange(
            // A — CUMPLIDO: resuelto antes de su fecha límite.
            new Incidente
            {
                SitioRed = "A", TipoIncidente = "t", EspecialidadRequerida = Especialidad.FibraOptica,
                Severidad = Severidad.Alta, Estado = EstadoIncidente.Resuelto,
                FechaReporte = Ahora.AddHours(-10), FechaLimite = Ahora.AddHours(-6),
                FechaResolucion = Ahora.AddHours(-8)
            },
            // B — INCUMPLIDO (resuelto tarde): resuelto después de la fecha límite.
            new Incidente
            {
                SitioRed = "B", TipoIncidente = "t", EspecialidadRequerida = Especialidad.Microondas,
                Severidad = Severidad.Media, Estado = EstadoIncidente.Resuelto,
                FechaReporte = Ahora.AddHours(-10), FechaLimite = Ahora.AddHours(-6),
                FechaResolucion = Ahora.AddHours(-2)
            },
            // C — VENCIDO ABIERTO: sigue abierto y su fecha límite ya pasó.
            new Incidente
            {
                SitioRed = "C", TipoIncidente = "t", EspecialidadRequerida = Especialidad.SistemasElectricos,
                Severidad = Severidad.Critico, Estado = EstadoIncidente.EnProgreso,
                FechaReporte = Ahora.AddHours(-3), FechaLimite = Ahora.AddHours(-1),
                FechaResolucion = null
            },
            // D — EN PLAZO: abierto pero su fecha límite es futura (no cuenta aún).
            new Incidente
            {
                SitioRed = "D", TipoIncidente = "t", EspecialidadRequerida = Especialidad.FibraOptica,
                Severidad = Severidad.Baja, Estado = EstadoIncidente.Asignado,
                FechaReporte = Ahora.AddHours(-1), FechaLimite = Ahora.AddHours(3),
                FechaResolucion = null
            }
        );
        db.SaveChanges();

        var service = new ReporteService(db, new RelojFalso(Ahora));
        var reporte = await service.GenerarReporteSlaAsync();

        Assert.Equal(4, reporte.TotalIncidentes);
        Assert.Equal(2, reporte.TotalResueltos);
        Assert.Equal(1, reporte.Cumplidos);
        Assert.Equal(1, reporte.IncumplidosResueltos);
        Assert.Equal(1, reporte.VencidosAbiertos);
        Assert.Equal(2, reporte.TotalIncumplidos);

        // % cumplimiento = cumplidos / (cumplidos + incumplidos) = 1 / 3 = 33.33 %
        Assert.Equal(33.33, reporte.PorcentajeCumplimiento);
    }

    [Fact]
    public async Task ReporteSla_TodosCumplidos_Da100PorCiento()
    {
        using var db = TestHelpers.CrearContexto();

        db.Incidentes.Add(new Incidente
        {
            SitioRed = "X", TipoIncidente = "t", EspecialidadRequerida = Especialidad.FibraOptica,
            Severidad = Severidad.Alta, Estado = EstadoIncidente.Cerrado,
            FechaReporte = Ahora.AddHours(-5), FechaLimite = Ahora.AddHours(-1),
            FechaResolucion = Ahora.AddHours(-3), FechaCierre = Ahora.AddHours(-2)
        });
        db.SaveChanges();

        var service = new ReporteService(db, new RelojFalso(Ahora));
        var reporte = await service.GenerarReporteSlaAsync();

        Assert.Equal(1, reporte.Cumplidos);
        Assert.Equal(0, reporte.TotalIncumplidos);
        Assert.Equal(100.0, reporte.PorcentajeCumplimiento);
    }
}
