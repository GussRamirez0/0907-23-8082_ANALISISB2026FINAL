using NetGuardGT.Api.DTOs;
using NetGuardGT.Api.Models.Enums;

namespace NetGuardGT.Api.Services;

/// <summary>
/// Lógica de negocio de los incidentes (Reglas 1 a 7).
/// Los controladores dependen de esta interfaz, nunca de la implementación concreta.
/// </summary>
public interface IIncidenteService
{
    /// <summary>Crea un incidente y calcula su FechaLimite según la severidad (Regla 1).</summary>
    Task<IncidenteResponseDto> CrearAsync(CrearIncidenteDto dto);

    /// <summary>Lista incidentes con filtros opcionales.</summary>
    Task<List<IncidenteResponseDto>> ListarAsync(EstadoIncidente? estado, Severidad? severidad, int? tecnicoId, string? sitio);

    /// <summary>Devuelve el detalle de un incidente (lanza NoEncontradoException si no existe).</summary>
    Task<IncidenteResponseDto> ObtenerPorIdAsync(int id);

    /// <summary>Asigna un técnico a un incidente sin asignar (Reglas 2 y 6, registra historial).</summary>
    Task<IncidenteResponseDto> AsignarAsync(int id, AsignarTecnicoDto dto);

    /// <summary>Avanza el estado respetando el flujo unidireccional (Regla 3, registra historial).</summary>
    Task<IncidenteResponseDto> CambiarEstadoAsync(int id, CambiarEstadoDto dto);

    /// <summary>Reasigna a otro técnico o libera el incidente (Reglas 2, 6 y 7).</summary>
    Task<IncidenteResponseDto> ReasignarAsync(int id, ReasignarDto dto);

    /// <summary>Evalúa todos los incidentes y escala los Crítico/Urgente vencidos (Regla 5).</summary>
    Task<List<IncidenteResponseDto>> EscalarVencidosAsync();

    /// <summary>Historial de cambios de un incidente (Regla 7).</summary>
    Task<List<HistorialEstadoDto>> ObtenerHistorialAsync(int id);
}
