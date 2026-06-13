using System.ComponentModel.DataAnnotations;
using NetGuardGT.Api.Models.Enums;

namespace NetGuardGT.Api.DTOs;

/// <summary>
/// Datos para crear un incidente. La FechaReporte y la FechaLimite NO se reciben:
/// se calculan en el servidor (Regla 1). El estado inicial siempre es Registrado.
/// </summary>
public class CrearIncidenteDto
{
    [Required(ErrorMessage = "El sitio de red es obligatorio.")]
    [MaxLength(120)]
    public string SitioRed { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tipo de incidente es obligatorio.")]
    [MaxLength(120)]
    public string TipoIncidente { get; set; } = string.Empty;

    [Required(ErrorMessage = "La especialidad requerida es obligatoria.")]
    public Especialidad EspecialidadRequerida { get; set; }

    [Required(ErrorMessage = "La severidad es obligatoria.")]
    public Severidad Severidad { get; set; }

    [MaxLength(1000)]
    public string Descripcion { get; set; } = string.Empty;
}
