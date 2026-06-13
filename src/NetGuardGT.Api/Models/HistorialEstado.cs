using NetGuardGT.Api.Models.Enums;

namespace NetGuardGT.Api.Models;

/// <summary>
/// Registro de auditoría de un cambio en el incidente (Regla 7).
/// Se crea en cada evento relevante: creación, asignación, avance de estado,
/// reasignación/liberación y escalamiento.
/// </summary>
public class HistorialEstado
{
    public int Id { get; set; }

    public int IncidenteId { get; set; }
    public Incidente? Incidente { get; set; }

    /// <summary>
    /// Estado previo al cambio. Es nullable porque en la creación del incidente
    /// no existe un estado anterior.
    /// </summary>
    public EstadoIncidente? EstadoAnterior { get; set; }

    public EstadoIncidente EstadoNuevo { get; set; }

    public DateTime FechaCambio { get; set; }

    /// <summary>Quién provocó el cambio (técnico, supervisor, "Sistema" para automáticos).</summary>
    public string Responsable { get; set; } = string.Empty;

    /// <summary>Motivo o descripción del cambio.</summary>
    public string Motivo { get; set; } = string.Empty;
}
