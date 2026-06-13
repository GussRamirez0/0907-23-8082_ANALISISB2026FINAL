using NetGuardGT.Api.Models;
using NetGuardGT.Api.Models.Enums;

namespace NetGuardGT.Api.DTOs;

/// <summary>Entrada del historial de cambios de un incidente (Regla 7).</summary>
public class HistorialEstadoDto
{
    public int Id { get; set; }
    public EstadoIncidente? EstadoAnterior { get; set; }
    public EstadoIncidente EstadoNuevo { get; set; }
    public DateTime FechaCambio { get; set; }
    public string Responsable { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;

    public static HistorialEstadoDto DesdeEntidad(HistorialEstado h) => new()
    {
        Id = h.Id,
        EstadoAnterior = h.EstadoAnterior,
        EstadoNuevo = h.EstadoNuevo,
        FechaCambio = h.FechaCambio,
        Responsable = h.Responsable,
        Motivo = h.Motivo
    };
}
