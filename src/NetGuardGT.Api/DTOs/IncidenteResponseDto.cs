using NetGuardGT.Api.Models;
using NetGuardGT.Api.Models.Enums;

namespace NetGuardGT.Api.DTOs;

/// <summary>
/// Representación de un incidente que sí se expone hacia el exterior.
/// Incluye el nombre del técnico y un indicador calculado de "Vencido".
/// </summary>
public class IncidenteResponseDto
{
    public int Id { get; set; }
    public string SitioRed { get; set; } = string.Empty;
    public string TipoIncidente { get; set; } = string.Empty;
    public Especialidad EspecialidadRequerida { get; set; }
    public Severidad Severidad { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public EstadoIncidente Estado { get; set; }
    public int? TecnicoId { get; set; }
    public string? TecnicoNombre { get; set; }
    public DateTime FechaReporte { get; set; }
    public DateTime FechaLimite { get; set; }
    public DateTime? FechaResolucion { get; set; }
    public DateTime? FechaCierre { get; set; }

    /// <summary>
    /// True si el incidente sigue abierto (no Resuelto ni Cerrado) y su FechaLimite ya pasó.
    /// </summary>
    public bool Vencido { get; set; }

    /// <summary>Convierte la entidad en DTO. <paramref name="ahora"/> se usa para calcular "Vencido".</summary>
    public static IncidenteResponseDto DesdeEntidad(Incidente i, DateTime ahora)
    {
        bool abierto = i.Estado != EstadoIncidente.Resuelto && i.Estado != EstadoIncidente.Cerrado;
        return new IncidenteResponseDto
        {
            Id = i.Id,
            SitioRed = i.SitioRed,
            TipoIncidente = i.TipoIncidente,
            EspecialidadRequerida = i.EspecialidadRequerida,
            Severidad = i.Severidad,
            Descripcion = i.Descripcion,
            Estado = i.Estado,
            TecnicoId = i.TecnicoId,
            TecnicoNombre = i.Tecnico?.Nombre,
            FechaReporte = i.FechaReporte,
            FechaLimite = i.FechaLimite,
            FechaResolucion = i.FechaResolucion,
            FechaCierre = i.FechaCierre,
            Vencido = abierto && ahora > i.FechaLimite
        };
    }
}
