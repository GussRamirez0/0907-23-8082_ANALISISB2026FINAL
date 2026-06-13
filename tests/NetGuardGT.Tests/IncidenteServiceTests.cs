using NetGuardGT.Api.Data;
using NetGuardGT.Api.DTOs;
using NetGuardGT.Api.Models.Enums;
using NetGuardGT.Api.Services;

namespace NetGuardGT.Tests;

/// <summary>
/// Pruebas de la lógica de negocio de incidentes: SLA (Regla 1), máximo de activos
/// (Regla 2), flujo de estados (Regla 3), especialidad (Regla 6) e historial (Regla 7).
/// </summary>
public class IncidenteServiceTests
{
    // Instante fijo para que las pruebas sean deterministas.
    private static readonly DateTime Ahora = new(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);

    // ───────────────────────── REGLA 1: cálculo del SLA ─────────────────────────

    [Theory]
    [InlineData(Severidad.Critico, 1)]
    [InlineData(Severidad.Urgente, 2)]
    [InlineData(Severidad.Alta, 4)]
    [InlineData(Severidad.Media, 8)]
    [InlineData(Severidad.Baja, 24)]
    public async Task CrearIncidente_CalculaFechaLimiteSegunSeveridad(Severidad severidad, int horasSla)
    {
        using var db = TestHelpers.CrearContexto();
        var service = new IncidenteService(db, new RelojFalso(Ahora));

        var creado = await service.CrearAsync(NuevoIncidente(severidad: severidad));

        Assert.Equal(Ahora, creado.FechaReporte);
        Assert.Equal(Ahora.AddHours(horasSla), creado.FechaLimite);
        Assert.Equal(EstadoIncidente.Registrado, creado.Estado);
    }

    // ───────────────────────── REGLA 2: máximo 3 activos ─────────────────────────

    [Fact]
    public async Task Asignar_CuartoIncidenteActivo_LanzaReglaNegocio()
    {
        using var db = TestHelpers.CrearContexto();
        var tecnico = TestHelpers.AgregarTecnico(db, Especialidad.FibraOptica);
        var service = new IncidenteService(db, new RelojFalso(Ahora));

        // Creamos 4 incidentes que requieren la misma especialidad del técnico.
        var ids = new List<int>();
        for (int i = 0; i < 4; i++)
        {
            var creado = await service.CrearAsync(NuevoIncidente());
            ids.Add(creado.Id);
        }

        // Los primeros 3 se asignan sin problema.
        for (int i = 0; i < 3; i++)
            await service.AsignarAsync(ids[i], new AsignarTecnicoDto { TecnicoId = tecnico.Id });

        // El 4º debe ser rechazado por la regla de negocio (máximo 3 activos).
        await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            service.AsignarAsync(ids[3], new AsignarTecnicoDto { TecnicoId = tecnico.Id }));
    }

    [Fact]
    public async Task Asignar_LiberarUnoYAsignarOtro_RespetaElLimite()
    {
        // Verifica que al liberar un activo se vuelve a tener cupo (la regla cuenta activos reales).
        using var db = TestHelpers.CrearContexto();
        var tecnico = TestHelpers.AgregarTecnico(db, Especialidad.FibraOptica);
        var service = new IncidenteService(db, new RelojFalso(Ahora));

        var ids = new List<int>();
        for (int i = 0; i < 3; i++)
        {
            var creado = await service.CrearAsync(NuevoIncidente());
            await service.AsignarAsync(creado.Id, new AsignarTecnicoDto { TecnicoId = tecnico.Id });
            ids.Add(creado.Id);
        }

        // Liberamos uno -> el técnico baja a 2 activos.
        await service.ReasignarAsync(ids[0], new ReasignarDto { TecnicoId = null });

        // Ahora un nuevo incidente sí se puede asignar.
        var nuevo = await service.CrearAsync(NuevoIncidente());
        var resultado = await service.AsignarAsync(nuevo.Id, new AsignarTecnicoDto { TecnicoId = tecnico.Id });

        Assert.Equal(tecnico.Id, resultado.TecnicoId);
    }

    // ───────────────────────── REGLA 6: especialidad coincidente ─────────────────────────

    [Fact]
    public async Task Asignar_EspecialidadDistinta_LanzaReglaNegocio()
    {
        using var db = TestHelpers.CrearContexto();
        var tecnicoMicroondas = TestHelpers.AgregarTecnico(db, Especialidad.Microondas);
        var service = new IncidenteService(db, new RelojFalso(Ahora));

        // Incidente que requiere FibraOptica, pero el técnico es de Microondas.
        var incidente = await service.CrearAsync(NuevoIncidente(especialidad: Especialidad.FibraOptica));

        await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            service.AsignarAsync(incidente.Id, new AsignarTecnicoDto { TecnicoId = tecnicoMicroondas.Id }));
    }

    [Fact]
    public async Task Asignar_EspecialidadCoincide_AsignaYAvanzaAAsignado()
    {
        using var db = TestHelpers.CrearContexto();
        var tecnico = TestHelpers.AgregarTecnico(db, Especialidad.FibraOptica);
        var service = new IncidenteService(db, new RelojFalso(Ahora));

        var incidente = await service.CrearAsync(NuevoIncidente(especialidad: Especialidad.FibraOptica));
        var resultado = await service.AsignarAsync(incidente.Id, new AsignarTecnicoDto { TecnicoId = tecnico.Id });

        Assert.Equal(tecnico.Id, resultado.TecnicoId);
        Assert.Equal(EstadoIncidente.Asignado, resultado.Estado);
    }

    // ───────────────────────── REGLA 3: flujo unidireccional ─────────────────────────

    [Fact]
    public async Task CambiarEstado_FlujoCompletoHaciaAdelante_Ok()
    {
        var (service, _, incidenteId) = await PrepararIncidenteAsignadoAsync();

        var enProgreso = await service.CambiarEstadoAsync(incidenteId,
            new CambiarEstadoDto { NuevoEstado = EstadoIncidente.EnProgreso });
        Assert.Equal(EstadoIncidente.EnProgreso, enProgreso.Estado);

        var resuelto = await service.CambiarEstadoAsync(incidenteId,
            new CambiarEstadoDto { NuevoEstado = EstadoIncidente.Resuelto });
        Assert.Equal(EstadoIncidente.Resuelto, resuelto.Estado);
        Assert.NotNull(resuelto.FechaResolucion); // se sella la fecha de resolución

        var cerrado = await service.CambiarEstadoAsync(incidenteId,
            new CambiarEstadoDto { NuevoEstado = EstadoIncidente.Cerrado });
        Assert.Equal(EstadoIncidente.Cerrado, cerrado.Estado);
        Assert.NotNull(cerrado.FechaCierre); // se sella la fecha de cierre
    }

    [Fact]
    public async Task CambiarEstado_Retroceso_LanzaReglaNegocio()
    {
        var (service, _, incidenteId) = await PrepararIncidenteAsignadoAsync();

        // Avanzamos a EnProgreso...
        await service.CambiarEstadoAsync(incidenteId, new CambiarEstadoDto { NuevoEstado = EstadoIncidente.EnProgreso });

        // ...e intentamos retroceder a Asignado: debe rechazarse.
        await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            service.CambiarEstadoAsync(incidenteId, new CambiarEstadoDto { NuevoEstado = EstadoIncidente.Asignado }));
    }

    [Fact]
    public async Task CambiarEstado_SaltarPasos_LanzaReglaNegocio()
    {
        var (service, _, incidenteId) = await PrepararIncidenteAsignadoAsync();

        // Desde Asignado se intenta saltar directo a Resuelto (saltándose EnProgreso): debe rechazarse.
        await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            service.CambiarEstadoAsync(incidenteId, new CambiarEstadoDto { NuevoEstado = EstadoIncidente.Resuelto }));
    }

    // ───────────────────────── REGLA 7: historial ─────────────────────────

    [Fact]
    public async Task Historial_SeRegistraEnCadaCambio()
    {
        using var db = TestHelpers.CrearContexto();
        var tecnico = TestHelpers.AgregarTecnico(db, Especialidad.FibraOptica);
        var service = new IncidenteService(db, new RelojFalso(Ahora));

        var inc = await service.CrearAsync(NuevoIncidente());                                   // 1) Creación
        await service.AsignarAsync(inc.Id, new AsignarTecnicoDto { TecnicoId = tecnico.Id });   // 2) Asignación
        await service.CambiarEstadoAsync(inc.Id, new CambiarEstadoDto { NuevoEstado = EstadoIncidente.EnProgreso }); // 3)
        await service.CambiarEstadoAsync(inc.Id, new CambiarEstadoDto { NuevoEstado = EstadoIncidente.Resuelto });   // 4)

        var historial = await service.ObtenerHistorialAsync(inc.Id);

        Assert.Equal(4, historial.Count);

        // La primera entrada es la creación: no tiene estado anterior.
        Assert.Null(historial[0].EstadoAnterior);
        Assert.Equal(EstadoIncidente.Registrado, historial[0].EstadoNuevo);

        // La segunda es la asignación: Registrado -> Asignado.
        Assert.Equal(EstadoIncidente.Registrado, historial[1].EstadoAnterior);
        Assert.Equal(EstadoIncidente.Asignado, historial[1].EstadoNuevo);

        // Las dos últimas reflejan los avances de estado.
        Assert.Equal(EstadoIncidente.Asignado, historial[2].EstadoAnterior);
        Assert.Equal(EstadoIncidente.EnProgreso, historial[2].EstadoNuevo);
        Assert.Equal(EstadoIncidente.EnProgreso, historial[3].EstadoAnterior);
        Assert.Equal(EstadoIncidente.Resuelto, historial[3].EstadoNuevo);
    }

    // ═══════════════════════ Helpers privados de las pruebas ═══════════════════════

    private static CrearIncidenteDto NuevoIncidente(
        Especialidad especialidad = Especialidad.FibraOptica,
        Severidad severidad = Severidad.Alta) => new()
    {
        SitioRed = "Sitio-1",
        TipoIncidente = "Falla de prueba",
        EspecialidadRequerida = especialidad,
        Severidad = severidad,
        Descripcion = "Incidente de prueba"
    };

    /// <summary>Crea un incidente ya asignado (estado Asignado) listo para probar avances de estado.</summary>
    private static async Task<(IncidenteService service, AppDbContext db, int incidenteId)> PrepararIncidenteAsignadoAsync()
    {
        var db = TestHelpers.CrearContexto();
        var tecnico = TestHelpers.AgregarTecnico(db, Especialidad.FibraOptica);
        var service = new IncidenteService(db, new RelojFalso(Ahora));

        var inc = await service.CrearAsync(NuevoIncidente());
        await service.AsignarAsync(inc.Id, new AsignarTecnicoDto { TecnicoId = tecnico.Id });

        return (service, db, inc.Id);
    }
}
