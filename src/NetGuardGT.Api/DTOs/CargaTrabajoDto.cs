using NetGuardGT.Api.Models.Enums;

namespace NetGuardGT.Api.DTOs;

/// <summary>
/// Carga de trabajo de un técnico: cuántos incidentes activos tiene y si puede
/// recibir más (recordando el máximo de 3 activos de la Regla 2).
/// </summary>
public class CargaTrabajoDto
{
    public int TecnicoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public Especialidad Especialidad { get; set; }

    /// <summary>Incidentes con estado distinto de Cerrado.</summary>
    public int IncidentesActivos { get; set; }

    /// <summary>Cupos libres hasta llegar al máximo de 3 (Regla 2).</summary>
    public int CuposDisponibles { get; set; }

    /// <summary>True si todavía puede recibir incidentes (menos de 3 activos).</summary>
    public bool Disponible { get; set; }
}
