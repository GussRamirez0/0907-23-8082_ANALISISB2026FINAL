using NetGuardGT.Api.Models;
using NetGuardGT.Api.Models.Enums;
using NetGuardGT.Api.Services;

namespace NetGuardGT.Api.Data;

/// <summary>
/// Siembra datos de prueba al iniciar la aplicación para poder probarla de inmediato.
/// Es idempotente: si la base ya tiene técnicos, no vuelve a sembrar.
/// Se usan fechas relativas a "ahora" (UTC) para que el escalamiento y el reporte
/// de SLA muestren casos realistas sin tener que esperar.
/// </summary>
public static class DbSeeder
{
    public static void Inicializar(AppDbContext db)
    {
        if (db.Tecnicos.Any())
            return; // ya sembrado

        // ── 12 técnicos: 4 por cada especialidad ──
        var tecnicos = new List<Tecnico>
        {
            new() { Nombre = "Carlos Méndez",    Especialidad = Especialidad.FibraOptica },
            new() { Nombre = "Ana López",        Especialidad = Especialidad.FibraOptica },
            new() { Nombre = "Luis García",      Especialidad = Especialidad.FibraOptica },
            new() { Nombre = "María Pérez",      Especialidad = Especialidad.FibraOptica },

            new() { Nombre = "Jorge Ramírez",    Especialidad = Especialidad.Microondas },
            new() { Nombre = "Sofía Castillo",   Especialidad = Especialidad.Microondas },
            new() { Nombre = "Pedro Hernández",  Especialidad = Especialidad.Microondas },
            new() { Nombre = "Lucía Morales",    Especialidad = Especialidad.Microondas },

            new() { Nombre = "Diego Torres",     Especialidad = Especialidad.SistemasElectricos },
            new() { Nombre = "Elena Vargas",     Especialidad = Especialidad.SistemasElectricos },
            new() { Nombre = "Roberto Díaz",     Especialidad = Especialidad.SistemasElectricos },
            new() { Nombre = "Carmen Flores",    Especialidad = Especialidad.SistemasElectricos },
        };
        db.Tecnicos.AddRange(tecnicos);
        db.SaveChanges();

        Tecnico T(string nombre) => tecnicos.First(t => t.Nombre == nombre);

        var ahora = DateTime.UtcNow;
        var incidentes = new List<Incidente>();

        // Inc 1 — Crítico en Registrado hace 3h, SIN técnico.
        //         Candidato a escalamiento (Regla 5) y además vencido para el reporte SLA.
        var f1 = ahora.AddHours(-3);
        var inc1 = new Incidente
        {
            SitioRed = "Sitio-Guatemala-Central",
            TipoIncidente = "Corte total de fibra",
            EspecialidadRequerida = Especialidad.FibraOptica,
            Severidad = Severidad.Critico,
            Descripcion = "Pérdida total de enlace de fibra en el anillo central.",
            Estado = EstadoIncidente.Registrado,
            FechaReporte = f1,
            FechaLimite = PoliticaSla.CalcularFechaLimite(Severidad.Critico, f1)
        };
        inc1.Historial.Add(Hist(null, EstadoIncidente.Registrado, f1, "Sistema", "Incidente registrado."));
        incidentes.Add(inc1);

        // Inc 2 — Alta, RESUELTO a tiempo (cumple SLA).
        var f2 = ahora.AddHours(-6);
        var inc2 = new Incidente
        {
            SitioRed = "Sitio-Xela-Norte",
            TipoIncidente = "Degradación de señal microondas",
            EspecialidadRequerida = Especialidad.Microondas,
            Severidad = Severidad.Alta,
            Descripcion = "Enlace microondas con alta tasa de errores.",
            Estado = EstadoIncidente.Resuelto,
            Tecnico = T("Jorge Ramírez"),
            FechaReporte = f2,
            FechaLimite = PoliticaSla.CalcularFechaLimite(Severidad.Alta, f2), // f2 + 4h = ahora - 2h
            FechaResolucion = ahora.AddHours(-5)                               // resuelto dentro de plazo
        };
        inc2.Historial.Add(Hist(null, EstadoIncidente.Registrado, f2, "Sistema", "Incidente registrado."));
        inc2.Historial.Add(Hist(EstadoIncidente.Registrado, EstadoIncidente.Asignado, f2.AddMinutes(10), "Supervisor", "Asignado al técnico Jorge Ramírez."));
        inc2.Historial.Add(Hist(EstadoIncidente.Asignado, EstadoIncidente.EnProgreso, f2.AddMinutes(25), "Jorge Ramírez", "Técnico en sitio."));
        inc2.Historial.Add(Hist(EstadoIncidente.EnProgreso, EstadoIncidente.Resuelto, ahora.AddHours(-5), "Jorge Ramírez", "Enlace restablecido."));
        incidentes.Add(inc2);

        // Inc 3 — Media, RESUELTO pero fuera de plazo (incumple SLA).
        var f3 = ahora.AddHours(-12);
        var inc3 = new Incidente
        {
            SitioRed = "Sitio-Peten-Sur",
            TipoIncidente = "Falla en UPS",
            EspecialidadRequerida = Especialidad.SistemasElectricos,
            Severidad = Severidad.Media,
            Descripcion = "Banco de baterías agotado en sitio remoto.",
            Estado = EstadoIncidente.Resuelto,
            Tecnico = T("Diego Torres"),
            FechaReporte = f3,
            FechaLimite = PoliticaSla.CalcularFechaLimite(Severidad.Media, f3), // f3 + 8h = ahora - 4h
            FechaResolucion = ahora.AddHours(-1)                                // resuelto tarde
        };
        inc3.Historial.Add(Hist(null, EstadoIncidente.Registrado, f3, "Sistema", "Incidente registrado."));
        inc3.Historial.Add(Hist(EstadoIncidente.Registrado, EstadoIncidente.Asignado, f3.AddMinutes(15), "Supervisor", "Asignado al técnico Diego Torres."));
        inc3.Historial.Add(Hist(EstadoIncidente.Asignado, EstadoIncidente.EnProgreso, f3.AddHours(2), "Diego Torres", "Traslado al sitio remoto."));
        inc3.Historial.Add(Hist(EstadoIncidente.EnProgreso, EstadoIncidente.Resuelto, ahora.AddHours(-1), "Diego Torres", "Baterías reemplazadas."));
        incidentes.Add(inc3);

        // Inc 4 — Urgente, ASIGNADO hace 30 min, dentro de plazo (no se escala: ya no está en Registrado).
        var f4 = ahora.AddMinutes(-30);
        var inc4 = new Incidente
        {
            SitioRed = "Sitio-Guatemala-Sur",
            TipoIncidente = "Intermitencia en enlace de fibra",
            EspecialidadRequerida = Especialidad.FibraOptica,
            Severidad = Severidad.Urgente,
            Descripcion = "Cliente corporativo con cortes intermitentes.",
            Estado = EstadoIncidente.Asignado,
            Tecnico = T("Carlos Méndez"),
            FechaReporte = f4,
            FechaLimite = PoliticaSla.CalcularFechaLimite(Severidad.Urgente, f4)
        };
        inc4.Historial.Add(Hist(null, EstadoIncidente.Registrado, f4, "Sistema", "Incidente registrado."));
        inc4.Historial.Add(Hist(EstadoIncidente.Registrado, EstadoIncidente.Asignado, f4.AddMinutes(5), "Supervisor", "Asignado al técnico Carlos Méndez."));
        incidentes.Add(inc4);

        // Inc 5 — Baja, EN PROGRESO desde hace 30h: abierto y vencido (incumple SLA).
        var f5 = ahora.AddHours(-30);
        var inc5 = new Incidente
        {
            SitioRed = "Sitio-Izabal-Costa",
            TipoIncidente = "Mantenimiento de antena",
            EspecialidadRequerida = Especialidad.Microondas,
            Severidad = Severidad.Baja,
            Descripcion = "Ajuste de alineación de antena microondas.",
            Estado = EstadoIncidente.EnProgreso,
            Tecnico = T("Sofía Castillo"),
            FechaReporte = f5,
            FechaLimite = PoliticaSla.CalcularFechaLimite(Severidad.Baja, f5) // f5 + 24h = ahora - 6h
        };
        inc5.Historial.Add(Hist(null, EstadoIncidente.Registrado, f5, "Sistema", "Incidente registrado."));
        inc5.Historial.Add(Hist(EstadoIncidente.Registrado, EstadoIncidente.Asignado, f5.AddMinutes(20), "Supervisor", "Asignado a la técnica Sofía Castillo."));
        inc5.Historial.Add(Hist(EstadoIncidente.Asignado, EstadoIncidente.EnProgreso, f5.AddHours(1), "Sofía Castillo", "Trabajo de mantenimiento iniciado."));
        incidentes.Add(inc5);

        db.Incidentes.AddRange(incidentes);
        db.SaveChanges();
    }

    private static HistorialEstado Hist(EstadoIncidente? anterior, EstadoIncidente nuevo,
        DateTime fecha, string responsable, string motivo) => new()
    {
        EstadoAnterior = anterior,
        EstadoNuevo = nuevo,
        FechaCambio = fecha,
        Responsable = responsable,
        Motivo = motivo
    };
}
