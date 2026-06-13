using NetGuardGT.Api.Models.Enums;

namespace NetGuardGT.Api.DTOs;

/// <summary>
/// Reporte de cumplimiento de SLA (Regla 8).
/// Un incidente CUMPLE el SLA si se resolvió a tiempo (FechaResolucion &lt;= FechaLimite).
/// Los incidentes abiertos cuya FechaLimite ya pasó se cuentan como vencidos (incumplidos).
/// </summary>
public class ReporteSlaDto
{
    public int TotalIncidentes { get; set; }
    public int TotalResueltos { get; set; }

    /// <summary>Resueltos a tiempo (FechaResolucion &lt;= FechaLimite).</summary>
    public int Cumplidos { get; set; }

    /// <summary>Resueltos fuera de plazo (FechaResolucion &gt; FechaLimite).</summary>
    public int IncumplidosResueltos { get; set; }

    /// <summary>Incidentes aún abiertos cuya FechaLimite ya pasó.</summary>
    public int VencidosAbiertos { get; set; }

    /// <summary>Total de incumplimientos = resueltos tarde + abiertos vencidos.</summary>
    public int TotalIncumplidos { get; set; }

    /// <summary>
    /// % de cumplimiento sobre los incidentes con resultado SLA definitivo
    /// (cumplidos + incumplidos). Los abiertos aún en plazo no cuentan.
    /// </summary>
    public double PorcentajeCumplimiento { get; set; }

    /// <summary>Detalle incidente por incidente con su clasificación de SLA.</summary>
    public List<IncidenteSlaDto> Detalle { get; set; } = new();
}

/// <summary>Clasificación de SLA de un incidente concreto dentro del reporte.</summary>
public class IncidenteSlaDto
{
    public int Id { get; set; }
    public string SitioRed { get; set; } = string.Empty;
    public Severidad Severidad { get; set; }
    public EstadoIncidente Estado { get; set; }
    public DateTime FechaLimite { get; set; }
    public DateTime? FechaResolucion { get; set; }

    /// <summary>Cumplido | IncumplidoResuelto | VencidoAbierto | EnPlazo.</summary>
    public string Clasificacion { get; set; } = string.Empty;
}
