using System.ComponentModel.DataAnnotations;

namespace Oportunidades.Api.Validation;

/// <summary>
/// Valida el monto estimado de una oportunidad: mayor que cero, con un
/// máximo de dos decimales y hasta 16 dígitos enteros.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class MontoEstimadoAttribute : ValidationAttribute
{
    // 16 dígitos enteros: el valor debe ser menor que 10^16.
    private const decimal LimiteExclusivo = 10_000_000_000_000_000m;

    public override bool IsValid(object? value)
    {
        if (value is not decimal monto)
        {
            return false;
        }

        return monto > 0m
            && monto < LimiteExclusivo
            && decimal.Round(monto, 2) == monto;
    }
}
