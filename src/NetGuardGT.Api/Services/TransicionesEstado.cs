using NetGuardGT.Api.Models.Enums;

namespace NetGuardGT.Api.Services;

/// <summary>
/// REGLA 3 — Flujo unidireccional de estados.
/// El avance válido es exactamente: Registrado → Asignado → EnProgreso → Resuelto → Cerrado.
/// No se permite retroceder ni saltar pasos. (Escalado no forma parte de este flujo lineal:
/// se alcanza de forma automática, ver Regla 5).
/// </summary>
public static class TransicionesEstado
{
    /// <summary>Secuencia lineal permitida. El índice define el orden de avance.</summary>
    public static readonly IReadOnlyList<EstadoIncidente> Flujo = new[]
    {
        EstadoIncidente.Registrado,
        EstadoIncidente.Asignado,
        EstadoIncidente.EnProgreso,
        EstadoIncidente.Resuelto,
        EstadoIncidente.Cerrado
    };

    /// <summary>
    /// True solo si <paramref name="nuevo"/> es el paso inmediatamente siguiente a
    /// <paramref name="actual"/> dentro del flujo lineal. Cualquier retroceso, salto,
    /// permanencia o estado fuera del flujo (Escalado) devuelve false.
    /// </summary>
    public static bool EsAvanceValido(EstadoIncidente actual, EstadoIncidente nuevo)
    {
        int indiceActual = IndiceDe(actual);
        int indiceNuevo = IndiceDe(nuevo);
        return indiceActual >= 0 && indiceNuevo >= 0 && indiceNuevo == indiceActual + 1;
    }

    private static int IndiceDe(EstadoIncidente estado)
    {
        for (int i = 0; i < Flujo.Count; i++)
            if (Flujo[i] == estado) return i;
        return -1;
    }
}
