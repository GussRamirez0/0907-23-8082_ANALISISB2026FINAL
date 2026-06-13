namespace NetGuardGT.Api.Models.Enums;

/// <summary>
/// Estados por los que pasa un incidente.
/// El flujo normal es unidireccional: Registrado -> Asignado -> EnProgreso -> Resuelto -> Cerrado.
/// <c>Escalado</c> es un estado especial al que se llega automáticamente (Regla 5)
/// y no forma parte del avance lineal.
/// </summary>
public enum EstadoIncidente
{
    Registrado,
    Asignado,
    EnProgreso,
    Resuelto,
    Cerrado,
    Escalado
}
