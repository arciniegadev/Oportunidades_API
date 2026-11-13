# Oportunidades API

API REST para registrar, consultar y actualizar el estado de oportunidades comerciales.
Construida con **C# / ASP.NET Core Web API (.NET 10)**, **Entity Framework Core** y **SQL Server**.

## Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server 2019 o superior (funciona con Express, Developer o Enterprise; también con LocalDB)
- Opcional: `sqlcmd` o SQL Server Management Studio para ejecutar el script de base de datos

## Estructura del repositorio

```
Oportunidades_API/
├─ OportunidadesAPI.sln
├─ database/
│  └─ 01_crear_base.sql          # Crea la base OportunidadesDb y la tabla dbo.Oportunidades
├─ src/
│  └─ Oportunidades.Api/
│     ├─ Controllers/
│     │  └─ OportunidadesController.cs
│     ├─ Contracts/              # DTOs de entrada y salida
│     ├─ Data/                   # DbContext y configuración de EF Core
│     ├─ Domain/                 # Entidad Oportunidad y enum EstadoOportunidad
│     ├─ Services/               # IOportunidadService y su implementación con EF Core
│     ├─ Validation/             # Reglas de validación reutilizables
│     ├─ Program.cs
│     └─ appsettings.json
└─ README.md
```

## Puesta en marcha desde una base de datos vacía

1. **Crear la base de datos.** El script es idempotente: se puede ejecutar varias veces sin borrar datos.

   ```powershell
   sqlcmd -S localhost -E -C -i database/01_crear_base.sql
   ```

   También puedes abrir `database/01_crear_base.sql` en SQL Server Management Studio y ejecutarlo.
   El script crea la base `OportunidadesDb`, la tabla `dbo.Oportunidades`, sus restricciones
   (checks) y el índice de consulta por estado y fecha.

2. **Configurar la cadena de conexión.** El valor por defecto está en
   `src/Oportunidades.Api/appsettings.json`:

   ```json
   "ConnectionStrings": {
     "OportunidadesDb": "Server=localhost;Database=OportunidadesDb;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False"
   }
   ```

   Ajusta `Server` según tu instancia. Por ejemplo:
   - SQL Server Express: `Server=localhost\\SQLEXPRESS`
   - LocalDB: `Server=(localdb)\\MSSQLLocalDB`
   - Usuario y contraseña: `User Id=sa;Password=TuClave;`

   También puedes sobrescribirla sin tocar el archivo con la variable de entorno
   `ConnectionStrings__OportunidadesDb`.

3. **Restaurar y ejecutar.**

   ```powershell
   dotnet restore
   dotnet run --project src/Oportunidades.Api
   ```

   La API queda disponible en `http://localhost:5020` (perfil `http` de
   `launchSettings.json`). El documento OpenAPI está en
   `http://localhost:5020/openapi/v1.json` y el health check en
   `http://localhost:5020/health`.

No se requieren migraciones ni datos iniciales: con el script ejecutado, la API funciona
sobre una base vacía.

## Arquitectura

El controlador **no conoce la base de datos ni EF Core**: depende únicamente de la interfaz
`IOportunidadService`, registrada con inyección de dependencias en `Program.cs`:

```csharp
builder.Services.AddScoped<IOportunidadService, OportunidadService>();
```

```
OportunidadesController
        │  depende de
        ▼
IOportunidadService  ◄── implementa ──  OportunidadService
                                             │ usa
                                             ▼
                                  OportunidadesDbContext (EF Core)
                                             │
                                             ▼
                                        SQL Server
```

Esto permite sustituir la implementación (por ejemplo, por una en memoria para pruebas) sin
tocar el controlador. El controlador solo se ocupa del contrato HTTP; el servicio concentra
las reglas de negocio y el acceso a datos.

## Endpoints

| Método | Ruta | Descripción |
| --- | --- | --- |
| `GET` | `/api/oportunidades` | Lista oportunidades con filtros y paginación. |
| `GET` | `/api/oportunidades/{id}` | Consulta una oportunidad por id. |
| `POST` | `/api/oportunidades` | Registra una oportunidad. |
| `PATCH` | `/api/oportunidades/{id}/estado` | Actualiza el estado de una oportunidad. |
| `PUT` | `/api/oportunidades/{id}/estado` | Alias de `PATCH` para el mismo cuerpo. |
| `GET` | `/health` | Estado básico del servicio. |

### Listado

Parámetros de consulta opcionales:

| Parámetro | Descripción | Valor por defecto |
| --- | --- | --- |
| `estado` | `Nueva`, `EnSeguimiento`, `Ganada` o `Perdida`. | Sin filtro |
| `cliente` | Coincidencia parcial en el nombre del cliente. | Sin filtro |
| `page` | Página, desde 1. Se ajusta a 1 si viene por debajo. | `1` |
| `pageSize` | Tamaño de página entre 1 y 100. | `20` |

Respuesta:

```json
{
  "items": [
    {
      "id": 1,
      "nombre": "Renovación licencias 2027",
      "cliente": "Acme S.A.",
      "montoEstimado": 125000.50,
      "estado": "Nueva",
      "fechaCreacionUtc": "2026-11-13T18:00:00.000Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "total": 1
}
```

### Registrar una oportunidad

`POST /api/oportunidades` — el estado inicial siempre es `Nueva` y la fecha UTC la asigna el
sistema; no se aceptan esos campos desde el cliente.

```bash
curl -i -X POST http://localhost:5020/api/oportunidades \
  -H "Content-Type: application/json" \
  -d '{"nombre":"  Renovacion licencias 2027  ","cliente":"  Acme S.A.  ","montoEstimado":125000.50}'
```

Respuesta `201 Created` con cabecera `Location: /api/oportunidades/1` y el recurso creado.
Los espacios al inicio y al final de `nombre` y `cliente` se eliminan antes de guardar.

### Actualizar el estado

```bash
curl -i -X PATCH http://localhost:5020/api/oportunidades/1/estado \
  -H "Content-Type: application/json" \
  -d '{"estado":"EnSeguimiento"}'
```

El cuerpo debe usar uno de los nombres del enum (`Nueva`, `EnSeguimiento`, `Ganada`,
`Perdida`). No se distingue mayúsculas/minúsculas al recibirlo y la respuesta siempre usa el
nombre canónico. Los valores numéricos se rechazan. Cualquier transición entre estados válidos
está permitida.

## Reglas de negocio y dónde se aplican

| Campo | Regla | Implementación |
| --- | --- | --- |
| `id` | Entero positivo generado por la base de datos. | `IDENTITY(1,1)` en el script + `ValueGeneratedOnAdd()` en EF Core. |
| `nombre` | Obligatorio, máximo 100 caracteres tras quitar espacios de los extremos. | `TextoRequeridoAttribute`, `Trim()` en `OportunidadService` y columna `NVARCHAR(100)` con check de no vacío. |
| `cliente` | Obligatorio, máximo 120 caracteres tras quitar espacios de los extremos. | `TextoRequeridoAttribute`, `Trim()` en `OportunidadService` y columna `NVARCHAR(120)` con check de no vacío. |
| `montoEstimado` | Decimal mayor que cero, máximo 2 decimales y 16 dígitos enteros. | `MontoEstimadoAttribute` + columna `DECIMAL(18,2)` con check `> 0`. |
| `estado` | `Nueva`, `EnSeguimiento`, `Ganada` o `Perdida`. | Enum de dominio, validación del cuerpo y check constraint en la base. |
| `fechaCreacionUtc` | Asignada por el sistema al registrar. | `DateTime.UtcNow` en `OportunidadService`, `DATETIME2(7)` con valor por defecto `SYSUTCDATETIME()` y conversión a `DateTimeKind.Utc` al leer. |

Toda oportunidad inicia en `Nueva`; el DTO de creación no incluye el estado.

## Errores

La API usa `ProblemDetails` (`application/problem+json`). Los errores de validación devuelven
`400` con las claves en camelCase:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "La solicitud contiene errores de validación.",
  "status": 400,
  "errors": {
    "montoEstimado": [
      "El monto estimado debe ser mayor que cero, con un máximo de dos decimales y hasta 16 dígitos enteros."
    ]
  }
}
```

Un id inexistente devuelve `404` con un `ProblemDetails` descriptivo.

## Ejemplos rápidos

El archivo `src/Oportunidades.Api/Oportunidades.Api.http` contiene solicitudes listas para
ejecutar con Visual Studio o la extensión REST Client de VS Code.

## Notas

- No incluye autenticación: es una API de ejemplo para ejecución local.
- La base se crea con el script SQL incluido; no se usan migraciones de EF Core.
- Si cambias el nombre de la base o de la instancia, actualiza la cadena de conexión.
