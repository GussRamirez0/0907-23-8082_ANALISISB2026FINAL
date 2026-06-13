using Microsoft.EntityFrameworkCore;
using NetGuardGT.Api.Data;
using NetGuardGT.Api.DTOs;
using NetGuardGT.Api.Models.Enums;

namespace NetGuardGT.Api.Services;

/// <summary>Lógica de consulta de técnicos y su carga de trabajo.</summary>
public class TecnicoService : ITecnicoService
{
    private readonly AppDbContext _db;

    public TecnicoService(AppDbContext db) => _db = db;

    public async Task<List<TecnicoDto>> ListarAsync()
    {
        var tecnicos = await _db.Tecnicos
            .OrderBy(t => t.Id)
            .AsNoTracking()
            .ToListAsync();

        return tecnicos.Select(TecnicoDto.DesdeEntidad).ToList();
    }

    public async Task<List<CargaTrabajoDto>> ObtenerCargaAsync()
    {
        var tecnicos = await _db.Tecnicos.OrderBy(t => t.Id).AsNoTracking().ToListAsync();

        // Conteo de incidentes activos (estado != Cerrado) agrupado por técnico.
        var activosPorTecnico = await _db.Incidentes
            .Where(i => i.TecnicoId != null && i.Estado != EstadoIncidente.Cerrado)
            .GroupBy(i => i.TecnicoId!.Value)
            .Select(g => new { TecnicoId = g.Key, Cantidad = g.Count() })
            .ToListAsync();

        var mapa = activosPorTecnico.ToDictionary(x => x.TecnicoId, x => x.Cantidad);

        return tecnicos.Select(t =>
        {
            int activos = mapa.TryGetValue(t.Id, out var cantidad) ? cantidad : 0;
            return new CargaTrabajoDto
            {
                TecnicoId = t.Id,
                Nombre = t.Nombre,
                Especialidad = t.Especialidad,
                IncidentesActivos = activos,
                CuposDisponibles = Math.Max(0, ReglasNegocio.MaxIncidentesActivos - activos),
                Disponible = activos < ReglasNegocio.MaxIncidentesActivos
            };
        }).ToList();
    }
}
