using NetGuardGT.Api.Models;
using NetGuardGT.Api.Models.Enums;

namespace NetGuardGT.Api.DTOs;

/// <summary>Representación pública de un técnico.</summary>
public class TecnicoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public Especialidad Especialidad { get; set; }

    public static TecnicoDto DesdeEntidad(Tecnico t) => new()
    {
        Id = t.Id,
        Nombre = t.Nombre,
        Especialidad = t.Especialidad
    };
}
