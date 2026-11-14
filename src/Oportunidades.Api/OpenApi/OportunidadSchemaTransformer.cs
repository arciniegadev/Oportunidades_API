using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Oportunidades.Api.Domain;
using Oportunidades.Api.DTOs;

namespace Oportunidades.Api.OpenApi;

/// <summary>
/// Ajusta el contrato OpenAPI para que refleje los tipos y restricciones
/// reales de los DTOs y del enum de estados.
/// </summary>
public sealed class OportunidadSchemaTransformer : IOpenApiSchemaTransformer
{
    private static readonly string[] Estados = Enum.GetNames<EstadoOportunidad>();

    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (context.JsonTypeInfo.Type == typeof(EstadoOportunidad))
        {
            ConfigurarEstado(schema);
            return Task.CompletedTask;
        }

        var propiedad = context.JsonPropertyInfo;

        if (propiedad is null)
        {
            ConfigurarObjeto(schema, context.JsonTypeInfo.Type);
            return Task.CompletedTask;
        }

        if (propiedad.DeclaringType == typeof(CrearOportunidadRequest))
        {
            switch (propiedad.Name)
            {
                case "nombre":
                    ConfigurarTexto(
                        schema,
                        100,
                        "Nombre de la oportunidad (se recorta antes de validar).");
                    break;

                case "cliente":
                    ConfigurarTexto(
                        schema,
                        120,
                        "Cliente de la oportunidad (se recorta antes de validar).");
                    break;

                case "montoEstimado":
                    ConfigurarMonto(schema);
                    break;
            }
        }
        else if (propiedad.DeclaringType == typeof(ActualizarEstadoRequest) &&
                 propiedad.Name == "estado")
        {
            ConfigurarEstado(schema);
        }
        else if (propiedad.DeclaringType == typeof(ListarOportunidadesQuery) &&
                 propiedad.Name == "estado")
        {
            ConfigurarEstado(schema);
        }
        else if (propiedad.DeclaringType == typeof(OportunidadResponse))
        {
            switch (propiedad.Name)
            {
                case "id":
                    schema.Type = JsonSchemaType.Integer;
                    schema.Format = "int32";
                    schema.Minimum = "1";
                    schema.Pattern = null;
                    schema.Description = "Identificador generado por la base de datos.";
                    break;

                case "nombre":
                    ConfigurarTexto(schema, 100, "Nombre de la oportunidad.");
                    break;

                case "cliente":
                    ConfigurarTexto(schema, 120, "Cliente de la oportunidad.");
                    break;

                case "montoEstimado":
                    ConfigurarMonto(schema);
                    break;

                case "fechaCreacionUtc":
                    schema.Type = JsonSchemaType.String;
                    schema.Format = "date-time";
                    schema.ReadOnly = true;
                    schema.Description = "Fecha y hora UTC asignadas por el sistema.";
                    break;
            }
        }

        return Task.CompletedTask;
    }

    private static void ConfigurarObjeto(OpenApiSchema schema, Type tipo)
    {
        if (tipo == typeof(CrearOportunidadRequest))
        {
            schema.Required = new HashSet<string> { "nombre", "cliente", "montoEstimado" };
        }
        else if (tipo == typeof(ActualizarEstadoRequest))
        {
            schema.Required = new HashSet<string> { "estado" };
        }
        else if (tipo == typeof(OportunidadResponse))
        {
            schema.Required = new HashSet<string>
            {
                "id",
                "nombre",
                "cliente",
                "montoEstimado",
                "estado",
                "fechaCreacionUtc"
            };
        }
    }

    private static void ConfigurarTexto(OpenApiSchema schema, int maxLength, string descripcion)
    {
        schema.Type = JsonSchemaType.String;
        schema.Format = null;
        schema.Pattern = null;
        schema.MinLength = 1;
        schema.MaxLength = maxLength;
        schema.Description = descripcion;
    }

    private static void ConfigurarMonto(OpenApiSchema schema)
    {
        schema.Type = JsonSchemaType.Number;
        schema.Format = "decimal";
        schema.Pattern = null;
        schema.ExclusiveMinimum = "0";
        schema.Maximum = "9999999999999999.99";
        schema.MultipleOf = 0.01m;
        schema.Description =
            "Monto mayor que cero, con hasta dos decimales y 16 dígitos enteros.";
    }

    private static void ConfigurarEstado(OpenApiSchema schema)
    {
        schema.Type = JsonSchemaType.String;
        schema.Format = null;
        schema.Pattern = null;
        schema.Enum = Estados
            .Select(estado => (JsonNode)JsonValue.Create(estado)!)
            .ToList();
        schema.Description = "Estado de la oportunidad.";
    }
}
