using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Oportunidades.Api.Domain;

namespace Oportunidades.Api.OpenApi;

/// <summary>
/// Completa el contrato de las operaciones: nombre del filtro en minúscula
/// y encabezado Location en la respuesta 201 del alta.
/// </summary>
public sealed class OportunidadOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (operation.Parameters is not null)
        {
            foreach (var parameter in operation.Parameters.OfType<OpenApiParameter>())
            {
                if (parameter.In == ParameterLocation.Query &&
                    string.Equals(parameter.Name, "Estado", StringComparison.Ordinal))
                {
                    parameter.Name = "estado";
                    parameter.Required = false;
                    parameter.Description =
                        "Filtro opcional por estado: Nueva, EnSeguimiento, Ganada o Perdida.";
                    parameter.Schema = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Enum = Enum.GetNames<EstadoOportunidad>()
                            .Select(estado => (JsonNode)JsonValue.Create(estado)!)
                            .ToList(),
                        Description = parameter.Description
                    };
                }
            }
        }

        if (operation.Responses is not null &&
            operation.Responses.TryGetValue("201", out var created) &&
            created is OpenApiResponse response)
        {
            var headers = response.Headers ??= new Dictionary<string, IOpenApiHeader>();
            headers["Location"] = new OpenApiHeader
            {
                Description = "URL del recurso creado.",
                Required = true,
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.String,
                    Format = "uri"
                }
            };
        }

        return Task.CompletedTask;
    }
}
