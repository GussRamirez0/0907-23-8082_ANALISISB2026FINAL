using NetGuardGT.Api.DTOs;

namespace NetGuardGT.Api.Services;

public interface ITecnicoService
{
    /// <summary>Lista todos los técnicos.</summary>
    Task<List<TecnicoDto>> ListarAsync();

    /// <summary>Carga de trabajo de cada técnico: activos, cupos libres y disponibilidad (Regla 2).</summary>
    Task<List<CargaTrabajoDto>> ObtenerCargaAsync();
}
