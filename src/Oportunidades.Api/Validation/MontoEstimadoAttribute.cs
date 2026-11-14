using System.ComponentModel.DataAnnotations;

namespace Oportunidades.Api.Validation;


[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class MontoEstimadoAttribute : ValidationAttribute
{
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
