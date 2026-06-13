using NetGuardGT.Api.Models.Enums;

namespace NetGuardGT.Api.Models;

/// <summary>
/// Incidente de red. Es la entidad central del sistema.
/// La <see cref="FechaLimite"/> se calcula al crear según la severidad (Regla 1)
/// y sirve para medir el cumplimiento del SLA (Regla 8).
/// </summary>
public class Incidente
{
    public int Id { get; set; }

    /// <summary>Sitio de red afectado (uno de los 45 sitios de NetGuard GT).</summary>
    public string SitioRed { get; set; } = string.Empty;

    /// <summary>Tipo de incidente (texto libre, p. ej. "Corte de fibra").</summary>
    public string TipoIncidente { get; set; } = string.Empty;

    /// <summary>Especialidad necesaria para atenderlo; condiciona qué técnico se puede asignar (Regla 6).</summary>
    public Especialidad EspecialidadRequerida { get; set; }

    public Severidad Severidad { get; set; }

    public string Descripcion { get; set; } = string.Empty;

    public EstadoIncidente Estado { get; set; } = EstadoIncidente.Registrado;

    /// <summary>Técnico asignado. Es nullable: un incidente puede estar sin asignar o liberado (Regla 4).</summary>
    public int? TecnicoId { get; set; }
    public Tecnico? Tecnico { get; set; }

    /// <summary>Momento en que se reportó el incidente (se fija al crear).</summary>
    public DateTime FechaReporte { get; set; }

    /// <summary>Fecha límite del SLA = FechaReporte + SLA de la severidad (Regla 1).</summary>
    public DateTime FechaLimite { get; set; }

    /// <summary>Se fija al pasar a Resuelto. Se usa para evaluar el cumplimiento del SLA.</summary>
    public DateTime? FechaResolucion { get; set; }

    /// <summary>Se fija al pasar a Cerrado.</summary>
    public DateTime? FechaCierre { get; set; }

    /// <summary>Traza de todos los cambios de estado del incidente (Regla 7).</summary>
    public ICollection<HistorialEstado> Historial { get; set; } = new List<HistorialEstado>();
}
