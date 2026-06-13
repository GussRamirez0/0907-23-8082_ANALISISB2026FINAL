using Microsoft.EntityFrameworkCore;
using NetGuardGT.Api.Data;
using NetGuardGT.Api.DTOs;
using NetGuardGT.Api.Models;
using NetGuardGT.Api.Models.Enums;

namespace NetGuardGT.Api.Services;

/// <summary>
/// Implementación de la lógica de negocio de incidentes.
/// Toda regla de negocio vive aquí (no en los controladores).
/// El tiempo se obtiene de <see cref="IRelojSistema"/> para que las reglas
/// dependientes del tiempo sean testeables.
/// </summary>
public class IncidenteService : IIncidenteService
{
    private readonly AppDbContext _db;
    private readonly IRelojSistema _reloj;

    public IncidenteService(AppDbContext db, IRelojSistema reloj)
    {
        _db = db;
        _reloj = reloj;
    }

    // ───────────────────────────── REGLA 1: Crear con SLA ─────────────────────────────
    public async Task<IncidenteResponseDto> CrearAsync(CrearIncidenteDto dto)
    {
        var ahora = _reloj.Ahora;

        var incidente = new Incidente
        {
            SitioRed = dto.SitioRed,
            TipoIncidente = dto.TipoIncidente,
            EspecialidadRequerida = dto.EspecialidadRequerida,
            Severidad = dto.Severidad,
            Descripcion = dto.Descripcion,
            Estado = EstadoIncidente.Registrado,
            FechaReporte = ahora,
            // Regla 1: la fecha límite se calcula con la tabla SLA centralizada.
            FechaLimite = PoliticaSla.CalcularFechaLimite(dto.Severidad, ahora)
        };

        // Regla 7: el alta también se registra en el historial (no hay estado anterior).
        RegistrarHistorial(incidente, anterior: null, nuevo: EstadoIncidente.Registrado,
            responsable: "Sistema", motivo: "Incidente registrado.");

        _db.Incidentes.Add(incidente);
        await _db.SaveChangesAsync();

        return IncidenteResponseDto.DesdeEntidad(incidente, ahora);
    }

    // ───────────────────────────── Listado con filtros ─────────────────────────────
    public async Task<List<IncidenteResponseDto>> ListarAsync(
        EstadoIncidente? estado, Severidad? severidad, int? tecnicoId, string? sitio)
    {
        var consulta = _db.Incidentes
            .Include(i => i.Tecnico)
            .AsNoTracking()
            .AsQueryable();

        if (estado.HasValue) consulta = consulta.Where(i => i.Estado == estado.Value);
        if (severidad.HasValue) consulta = consulta.Where(i => i.Severidad == severidad.Value);
        if (tecnicoId.HasValue) consulta = consulta.Where(i => i.TecnicoId == tecnicoId.Value);
        if (!string.IsNullOrWhiteSpace(sitio))
        {
            var filtro = sitio.Trim().ToLower();
            consulta = consulta.Where(i => i.SitioRed.ToLower().Contains(filtro));
        }

        var incidentes = await consulta
            .OrderBy(i => i.FechaLimite)
            .ToListAsync();

        var ahora = _reloj.Ahora;
        return incidentes.Select(i => IncidenteResponseDto.DesdeEntidad(i, ahora)).ToList();
    }

    public async Task<IncidenteResponseDto> ObtenerPorIdAsync(int id)
    {
        var incidente = await _db.Incidentes
            .Include(i => i.Tecnico)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id)
            ?? throw new NoEncontradoException($"No existe el incidente con Id {id}.");

        return IncidenteResponseDto.DesdeEntidad(incidente, _reloj.Ahora);
    }

    // ──────────────────────── REGLAS 2 y 6: Asignar técnico ────────────────────────
    public async Task<IncidenteResponseDto> AsignarAsync(int id, AsignarTecnicoDto dto)
    {
        var incidente = await CargarIncidenteConTecnicoAsync(id);

        if (incidente.Estado == EstadoIncidente.Cerrado)
            throw new ReglaNegocioException("No se puede asignar un incidente que ya está Cerrado.");

        if (incidente.TecnicoId != null)
            throw new ReglaNegocioException(
                "El incidente ya tiene un técnico asignado. Use el endpoint de reasignación para cambiarlo.");

        var tecnico = await _db.Tecnicos.FindAsync(dto.TecnicoId)
            ?? throw new NoEncontradoException($"No existe el técnico con Id {dto.TecnicoId}.");

        ValidarEspecialidad(tecnico, incidente);          // Regla 6
        await ValidarCupoAsync(tecnico.Id, incidente.Id); // Regla 2

        var estadoAnterior = incidente.Estado;
        incidente.TecnicoId = tecnico.Id;
        incidente.Tecnico = tecnico;

        // Al asignar, el incidente entra en el flujo: Registrado/Escalado → Asignado.
        if (incidente.Estado is EstadoIncidente.Registrado or EstadoIncidente.Escalado)
            incidente.Estado = EstadoIncidente.Asignado;

        RegistrarHistorial(incidente, estadoAnterior, incidente.Estado, dto.Responsable,
            $"Asignado al técnico {tecnico.Nombre}.");

        await _db.SaveChangesAsync();
        return IncidenteResponseDto.DesdeEntidad(incidente, _reloj.Ahora);
    }

    // ──────────────────────── REGLA 3: Avanzar estado ────────────────────────
    public async Task<IncidenteResponseDto> CambiarEstadoAsync(int id, CambiarEstadoDto dto)
    {
        var incidente = await CargarIncidenteConTecnicoAsync(id);

        var estadoActual = incidente.Estado;
        var nuevoEstado = dto.NuevoEstado;

        // Regla 3: solo se permite avanzar exactamente un paso en el flujo lineal.
        if (!TransicionesEstado.EsAvanceValido(estadoActual, nuevoEstado))
            throw new ReglaNegocioException(
                $"Transición de estado inválida: no se puede pasar de '{estadoActual}' a '{nuevoEstado}'. " +
                "El flujo permitido es Registrado → Asignado → EnProgreso → Resuelto → Cerrado (sin retroceder ni saltar pasos).");

        // No se puede marcar como Asignado sin un técnico responsable.
        if (nuevoEstado == EstadoIncidente.Asignado && incidente.TecnicoId == null)
            throw new ReglaNegocioException(
                "No se puede pasar a 'Asignado' sin un técnico. Use primero el endpoint de asignación.");

        var ahora = _reloj.Ahora;
        incidente.Estado = nuevoEstado;

        // Sellos de tiempo según el estado alcanzado.
        if (nuevoEstado == EstadoIncidente.Resuelto) incidente.FechaResolucion = ahora;
        if (nuevoEstado == EstadoIncidente.Cerrado) incidente.FechaCierre = ahora;

        RegistrarHistorial(incidente, estadoActual, nuevoEstado, dto.Responsable,
            string.IsNullOrWhiteSpace(dto.Motivo) ? $"Avance a {nuevoEstado}." : dto.Motivo);

        await _db.SaveChangesAsync();
        return IncidenteResponseDto.DesdeEntidad(incidente, ahora);
    }

    // ──────────────────── REGLAS 2, 6 y 7: Reasignar / Liberar ────────────────────
    public async Task<IncidenteResponseDto> ReasignarAsync(int id, ReasignarDto dto)
    {
        var incidente = await CargarIncidenteConTecnicoAsync(id);

        // Regla 4: se puede reasignar/liberar en cualquier momento salvo si está Cerrado.
        if (incidente.Estado == EstadoIncidente.Cerrado)
            throw new ReglaNegocioException("No se puede reasignar un incidente que ya está Cerrado.");

        var estadoAnterior = incidente.Estado;
        string motivoHistorial;

        if (dto.TecnicoId == null)
        {
            // ── Liberar: el incidente queda sin técnico ──
            if (incidente.TecnicoId == null)
                throw new ReglaNegocioException("El incidente ya está sin técnico asignado.");

            var nombrePrevio = incidente.Tecnico?.Nombre ?? "el técnico anterior";
            incidente.TecnicoId = null;
            incidente.Tecnico = null;
            motivoHistorial = $"Liberado (se retiró a {nombrePrevio}).";
        }
        else
        {
            // ── Reasignar a otro técnico ──
            var tecnico = await _db.Tecnicos.FindAsync(dto.TecnicoId.Value)
                ?? throw new NoEncontradoException($"No existe el técnico con Id {dto.TecnicoId}.");

            ValidarEspecialidad(tecnico, incidente);          // Regla 6 (revalidación)
            await ValidarCupoAsync(tecnico.Id, incidente.Id); // Regla 2 (revalidación)

            var nombrePrevio = incidente.Tecnico?.Nombre;
            incidente.TecnicoId = tecnico.Id;
            incidente.Tecnico = tecnico;

            // Si todavía no había entrado al flujo, la reasignación también lo activa.
            if (incidente.Estado is EstadoIncidente.Registrado or EstadoIncidente.Escalado)
                incidente.Estado = EstadoIncidente.Asignado;

            motivoHistorial = nombrePrevio == null
                ? $"Asignado al técnico {tecnico.Nombre}."
                : $"Reasignado de {nombrePrevio} a {tecnico.Nombre}.";
        }

        // Si el usuario aportó un motivo, lo anexamos.
        if (!string.IsNullOrWhiteSpace(dto.Motivo))
            motivoHistorial += $" Motivo: {dto.Motivo}";

        // Regla 7: registramos el evento aunque el estado no haya cambiado.
        RegistrarHistorial(incidente, estadoAnterior, incidente.Estado, dto.Responsable, motivoHistorial);

        await _db.SaveChangesAsync();
        return IncidenteResponseDto.DesdeEntidad(incidente, _reloj.Ahora);
    }

    // ──────────────────────── REGLA 5: Escalamiento automático ────────────────────────
    public async Task<List<IncidenteResponseDto>> EscalarVencidosAsync()
    {
        var ahora = _reloj.Ahora;

        // Candidatos: Crítico o Urgente que siguen en Registrado.
        var candidatos = await _db.Incidentes
            .Where(i => i.Estado == EstadoIncidente.Registrado &&
                        (i.Severidad == Severidad.Critico || i.Severidad == Severidad.Urgente))
            .ToListAsync();

        var escalados = new List<Incidente>();
        foreach (var incidente in candidatos)
        {
            // Se escala solo si han pasado más de 2 horas desde el reporte.
            if (ahora - incidente.FechaReporte > ReglasNegocio.UmbralEscalamiento)
            {
                var estadoAnterior = incidente.Estado;
                incidente.Estado = EstadoIncidente.Escalado;
                RegistrarHistorial(incidente, estadoAnterior, EstadoIncidente.Escalado, "Sistema",
                    $"Escalado automáticamente: incidente {incidente.Severidad} sin atender por más de 2 horas.");
                escalados.Add(incidente);
            }
        }

        if (escalados.Count > 0)
            await _db.SaveChangesAsync();

        return escalados.Select(i => IncidenteResponseDto.DesdeEntidad(i, ahora)).ToList();
    }

    // ──────────────────────── REGLA 7: Historial ────────────────────────
    public async Task<List<HistorialEstadoDto>> ObtenerHistorialAsync(int id)
    {
        var existe = await _db.Incidentes.AnyAsync(i => i.Id == id);
        if (!existe)
            throw new NoEncontradoException($"No existe el incidente con Id {id}.");

        var historial = await _db.Historiales
            .Where(h => h.IncidenteId == id)
            .OrderBy(h => h.FechaCambio).ThenBy(h => h.Id)
            .AsNoTracking()
            .ToListAsync();

        return historial.Select(HistorialEstadoDto.DesdeEntidad).ToList();
    }

    // ═════════════════════════ Helpers privados ═════════════════════════

    /// <summary>Carga un incidente (con su técnico) o lanza 404 si no existe.</summary>
    private async Task<Incidente> CargarIncidenteConTecnicoAsync(int id)
    {
        return await _db.Incidentes
            .Include(i => i.Tecnico)
            .FirstOrDefaultAsync(i => i.Id == id)
            ?? throw new NoEncontradoException($"No existe el incidente con Id {id}.");
    }

    /// <summary>REGLA 6: la especialidad del técnico debe coincidir con la requerida.</summary>
    private static void ValidarEspecialidad(Tecnico tecnico, Incidente incidente)
    {
        if (tecnico.Especialidad != incidente.EspecialidadRequerida)
            throw new ReglaNegocioException(
                $"El técnico {tecnico.Nombre} es de especialidad {tecnico.Especialidad}, " +
                $"pero el incidente requiere {incidente.EspecialidadRequerida}.");
    }

    /// <summary>REGLA 2: un técnico no puede superar el máximo de incidentes activos.</summary>
    private async Task ValidarCupoAsync(int tecnicoId, int incidenteIdActual)
    {
        // Contamos los activos del técnico, excluyendo el incidente actual
        // (para que reasignar al mismo técnico no se cuente doble).
        var activos = await _db.Incidentes.CountAsync(i =>
            i.TecnicoId == tecnicoId &&
            i.Estado != EstadoIncidente.Cerrado &&
            i.Id != incidenteIdActual);

        if (activos >= ReglasNegocio.MaxIncidentesActivos)
            throw new ReglaNegocioException(
                $"El técnico ya tiene {activos} incidentes activos " +
                $"(máximo permitido: {ReglasNegocio.MaxIncidentesActivos}). No se le puede asignar otro.");
    }

    /// <summary>REGLA 7: agrega una entrada al historial del incidente.</summary>
    private void RegistrarHistorial(Incidente incidente, EstadoIncidente? anterior,
        EstadoIncidente nuevo, string responsable, string motivo)
    {
        incidente.Historial.Add(new HistorialEstado
        {
            Incidente = incidente,
            EstadoAnterior = anterior,
            EstadoNuevo = nuevo,
            FechaCambio = _reloj.Ahora,
            Responsable = string.IsNullOrWhiteSpace(responsable) ? "Sistema" : responsable,
            Motivo = motivo
        });
    }
}
