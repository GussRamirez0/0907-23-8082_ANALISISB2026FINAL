namespace NetGuardGT.Api.Services;

/// <summary>
/// Abstracción del "tiempo actual". Permite inyectar un reloj falso en las pruebas
/// para verificar de forma determinista reglas que dependen del tiempo
/// (cálculo de SLA y escalamiento por antigüedad), sin tener que esperar horas reales.
/// </summary>
public interface IRelojSistema
{
    DateTime Ahora { get; }
}
