using Microsoft.EntityFrameworkCore;
using NetGuardGT.Api.Models;

namespace NetGuardGT.Api.Data;

/// <summary>
/// Contexto de EF Core para NetGuard GT. Mapea las tres entidades a SQLite.
/// Los enums se guardan como texto para que la base de datos sea legible al inspeccionarla.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Tecnico> Tecnicos => Set<Tecnico>();
    public DbSet<Incidente> Incidentes => Set<Incidente>();
    public DbSet<HistorialEstado> Historiales => Set<HistorialEstado>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tecnico>(e =>
        {
            e.Property(t => t.Nombre).IsRequired().HasMaxLength(120);
            // El enum se persiste como string ("FibraOptica") en vez de un número.
            e.Property(t => t.Especialidad).HasConversion<string>().HasMaxLength(40);
        });

        modelBuilder.Entity<Incidente>(e =>
        {
            e.Property(i => i.SitioRed).IsRequired().HasMaxLength(120);
            e.Property(i => i.TipoIncidente).IsRequired().HasMaxLength(120);
            e.Property(i => i.Descripcion).HasMaxLength(1000);
            e.Property(i => i.EspecialidadRequerida).HasConversion<string>().HasMaxLength(40);
            e.Property(i => i.Severidad).HasConversion<string>().HasMaxLength(20);
            e.Property(i => i.Estado).HasConversion<string>().HasMaxLength(20);

            // Un incidente pertenece a 0..1 técnico; al borrar el técnico, el incidente queda sin asignar.
            e.HasOne(i => i.Tecnico)
                .WithMany(t => t.Incidentes)
                .HasForeignKey(i => i.TecnicoId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<HistorialEstado>(e =>
        {
            e.Property(h => h.Responsable).IsRequired().HasMaxLength(120);
            e.Property(h => h.Motivo).HasMaxLength(500);
            e.Property(h => h.EstadoAnterior).HasConversion<string>().HasMaxLength(20);
            e.Property(h => h.EstadoNuevo).HasConversion<string>().HasMaxLength(20);

            // Al borrar un incidente se borra su historial (cascade).
            e.HasOne(h => h.Incidente)
                .WithMany(i => i.Historial)
                .HasForeignKey(h => h.IncidenteId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
