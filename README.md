# Oportunidades API

API REST para registrar, consultar y actualizar el estado de oportunidades comerciales.
Construida con **C# / ASP.NET Core Web API (.NET 10)**, **ADO.NET** y **SQL Server**.
Todo el acceso a datos se realiza mediante **procedimientos almacenados**.

## Requisitos

- [.NET SDK 10.0.403](https://dotnet.microsoft.com/download) (probado con esta versión; el proyecto apunta a `net10.0`).
- SQL Server 2019 o superior (Express, Developer o Enterprise; también funciona con LocalDB).
- Opcional: `sqlcmd` o SQL Server Management Studio para ejecutar los scripts de base de datos.

## Estructura del repositorio

```
Oportunidades_API/
├─ OportunidadesAPI.sln
├─ Directory.Build.props          # UserSecretsId compartido por la API y el generador de tokens
├─ database/
│  ├─ 01_crear_base.sql           # Base OportunidadesDb, tabla, restricciones e índice
│  └─ 02_stored_procedures.sql    # Procedimientos de alta, consulta, listado y cambio de estado
├─ src/
│  └─ Oportunidades.Api/
│     ├─ Controllers/             # Contrato HTTP
│     ├─ DTOs/                    # Cuerpos de entrada, filtros y respuesta
│     ├─ Domain/                  # Entidad y reglas de transición de estado
│     ├─ Repositories/            # Acceso a datos con ADO.NET y procedimientos almacenados
│     ├─ Services/                # Reglas de negocio, concurrencia y logging
│     ├─ Validation/              # Atributos de validación reutilizables
│     ├─ Security/                # Nombres de roles
│     ├─ Infrastructure/          # Manejo global de excepciones
│     ├─ Program.cs
│     └─ appsettings.json
├─ tools/
│  └─ Oportunidades.TokenGenerator/  # Genera tokens JWT locales para Consulta y Editor
└─ README.md
```

## Puesta en marcha desde una base de datos vacía

1. **Crear la base de datos, la tabla y los procedimientos.** Ejecuta los dos scripts en orden.
   Ambos son idempotentes: se pueden ejecutar varias veces sin borrar datos.

   ```powershell
   sqlcmd -S localhost -E -C -i database/01_crear_base.sql
   sqlcmd -S localhost -E -C -i database/02_stored_procedures.sql
   ```

   También puedes abrirlos en SQL Server Management Studio y ejecutarlos en ese mismo orden.

2. **Configurar la cadena de conexión.** El valor por defecto está en
   `src/Oportunidades.Api/appsettings.json`:

   ```json
   "ConnectionStrings": {
     "OportunidadesDb": "Server=localhost;Database=OportunidadesDb;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False"
   }
   ```

   Ajusta `Server` según tu instancia:
   - SQL Server Express: `Server=localhost\\SQLEXPRESS`
   - LocalDB: `Server=(localdb)\\MSSQLLocalDB`
   - Usuario y contraseña: `User Id=sa;Password=TuClave;`

   Para no guardar credenciales en el repositorio, sobrescríbela con la variable de entorno
   `ConnectionStrings__OportunidadesDb` (o con user secrets).

3. **Configurar la clave de firma JWT.** La clave **no está en el repositorio**. La API y el
   generador de tokens comparten los user secrets configurados en `Directory.Build.props`:

   ```powershell
   dotnet user-secrets set "Jwt:SigningKey" "<clave-de-al-menos-32-caracteres>" --project src/Oportunidades.Api
   ```

   También puedes usar la variable de entorno `Jwt__SigningKey`. Si la clave falta o tiene menos
   de 32 caracteres, la API no arranca e indica cómo configurarla.

4. **Restaurar y ejecutar.**

   ```powershell
   dotnet restore
   dotnet run --project src/Oportunidades.Api
   ```

   La API queda disponible en `http://localhost:5020` (perfil `http` de
   `launchSettings.json`). El documento OpenAPI está en
   `http://localhost:5020/openapi/v1.json` y el health check en
   `http://localhost:5020/health` (sin autenticación).

No se requieren migraciones ni datos iniciales: con los scripts ejecutados, la API funciona
sobre una base vacía.

## Configuración

| Clave | Variable de entorno | Descripción |
| --- | --- | --- |
| `ConnectionStrings:OportunidadesDb` | `ConnectionStrings__OportunidadesDb` | Cadena de conexión a SQL Server. |
| `Jwt:Issuer` | `Jwt__Issuer` | Emisor esperado en el token. Por defecto `OportunidadesApi`. |
| `Jwt:Audience` | `Jwt__Audience` | Audiencia esperada. Por defecto `OportunidadesApiClientes`. |
| `Jwt:SigningKey` | `Jwt__SigningKey` | Clave simétrica HMAC-SHA256, mínimo 32 caracteres. **Obligatoria.** No se versiona. |

Las claves sensibles deben configurarse por ambiente con user secrets o variables de entorno,
nunca en `appsettings*.json` dentro del repositorio.

## Autenticación y roles

Los cuatro endpoints de oportunidades están protegidos con **JWT Bearer**. La API valida firma,
emisor, audiencia, expiración y algoritmo (`HS256`).

| Rol | GET (listado y detalle) | POST (registrar) | PATCH (cambiar estado) |
| --- | --- | --- | --- |
| `Consulta` | Sí | No (403) | No (403) |
| `Editor` | Sí | Sí | Sí |

Una solicitud sin token válido devuelve **401 Unauthorized**; un token válido con un rol sin
permiso devuelve **403 Forbidden**. Ambas respuestas usan `ProblemDetails`.

### Generar tokens locales

El generador lee `Jwt:SigningKey` de los user secrets compartidos o de `Jwt__SigningKey`, y usa
el mismo emisor y audiencia que la API:

```powershell
# Token de solo consulta (60 minutos por defecto)
dotnet run --project tools/Oportunidades.TokenGenerator -- Consulta

# Token de editor con duración personalizada
dotnet run --project tools/Oportunidades.TokenGenerator -- Editor --minutes 120
```

Copia el token devuelto y envíalo en la cabecera `Authorization: Bearer <token>`.

## Endpoints

| Método y ruta | Rol permitido | Respuestas |
| --- | --- | --- |
| `POST /api/oportunidades` | `Editor` | 201 Created, 400, 401, 403 |
| `GET /api/oportunidades?estado=EnSeguimiento` | `Consulta`, `Editor` | 200 OK, 400, 401, 403 |
| `GET /api/oportunidades/{id}` | `Consulta`, `Editor` | 200 OK, 400, 401, 403, 404 |
| `PATCH /api/oportunidades/{id}/estado` | `Editor` | 200 OK, 400, 401, 403, 404, 409 |

El filtro `estado` es opcional. Si no hay coincidencias, el listado devuelve `200 OK` con `[]`.
Los estados enviados en el cuerpo o en el filtro deben ser `Nueva`, `EnSeguimiento`, `Ganada` o
`Perdida` (sin distinguir mayúsculas/minúsculas). Los valores numéricos se rechazan con 400.

### Registrar una oportunidad

`POST /api/oportunidades` — el estado inicial siempre es `Nueva` y la fecha UTC la asigna el
sistema; no se aceptan esos campos desde el cliente.

```bash
curl -i -X POST http://localhost:5020/api/oportunidades \
  -H "Authorization: Bearer <token-editor>" \
  -H "Content-Type: application/json" \
  -d '{"nombre":"  Renovacion licencias 2027  ","cliente":"  Acme S.A.  ","montoEstimado":125000.50}'
```

Respuesta `201 Created`:

```json
{
  "id": 1,
  "nombre": "Renovacion licencias 2027",
  "cliente": "Acme S.A.",
  "montoEstimado": 125000.50,
  "estado": "Nueva",
  "fechaCreacionUtc": "2026-11-13T18:00:00.000Z"
}
```

La cabecera `Location` apunta a `/api/oportunidades/1`. Los espacios al inicio y al final de
`nombre` y `cliente` se eliminan antes de guardar.

### Consultar una oportunidad

```bash
curl -i http://localhost:5020/api/oportunidades/1 \
  -H "Authorization: Bearer <token-consulta>"
```

Devuelve `200 OK` con el recurso o `404 Not Found` si no existe.

### Listar oportunidades

```bash
# Todas
curl -i http://localhost:5020/api/oportunidades \
  -H "Authorization: Bearer <token-consulta>"

# Filtradas por estado
curl -i "http://localhost:5020/api/oportunidades?estado=EnSeguimiento" \
  -H "Authorization: Bearer <token-consulta>"
```

Devuelve `200 OK` con un arreglo JSON. Un estado desconocido en el filtro devuelve `400`.

### Actualizar el estado

```bash
curl -i -X PATCH http://localhost:5020/api/oportunidades/1/estado \
  -H "Authorization: Bearer <token-editor>" \
  -H "Content-Type: application/json" \
  -d '{"estado":"EnSeguimiento"}'
```

Devuelve `200 OK` con la oportunidad resultante, `404 Not Found` si no existe o `409 Conflict`
si la transición no está permitida o si otro proceso modificó el estado antes de aplicar el
cambio.

## Modelo y reglas de negocio

| Campo | Regla |
| --- | --- |
| `id` | Entero positivo generado por la base de datos (`IDENTITY`). |
| `nombre` | Obligatorio, máximo 100 caracteres después de quitar espacios al inicio y al final. |
| `cliente` | Obligatorio, máximo 120 caracteres después de quitar espacios al inicio y al final. |
| `montoEstimado` | Decimal mayor que cero, máximo dos decimales y 16 dígitos enteros (`DECIMAL(18,2)`). |
| `estado` | `Nueva`, `EnSeguimiento`, `Ganada` o `Perdida`. |
| `fechaCreacionUtc` | Asignada por el sistema al registrar (`SYSUTCDATETIME()`). |

Toda oportunidad inicia en `Nueva`. Las transiciones permitidas son:

```
Nueva ──────────► EnSeguimiento ──────────► Ganada
                         └────────────────► Perdida
```

`Ganada` y `Perdida` son estados finales. Repetir el estado actual devuelve `200 OK` sin
modificar el recurso. Cualquier otra transición devuelve `409 Conflict`.

La regla se valida en el dominio (`Oportunidad.PuedeCambiarEstado`), en el servicio y también en
el procedimiento `usp_Oportunidades_CambiarEstado`.

### Concurrencia

El cambio de estado se aplica con una actualización condicional: el procedimiento solo actualiza
si el estado persistido sigue siendo el que leyó y validó el servicio. Si otra modificación
concurrente cambió el estado, no se actualiza ninguna fila y la API responde `409 Conflict`.

## Errores

Todas las respuestas de error usan `ProblemDetails` / `ValidationProblemDetails`.
Los errores de validación devuelven `400` con los campos que deben corregirse:

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

- `400`: datos inválidos, id no positivo, monto fuera de rango o estado desconocido.
- `401`: falta un token válido.
- `403`: el rol no tiene permiso.
- `404`: la oportunidad no existe.
- `409`: transición no permitida o modificación concurrente.
- `500`: error inesperado. El detalle se registra en el log y al cliente solo se devuelve un
  mensaje genérico.

## Arquitectura

```
OportunidadesController        (contrato HTTP, códigos de estado)
          │ depende de
          ▼
IOportunidadService  ◄── OportunidadService    (reglas de negocio, transiciones, concurrencia)
          │ depende de
          ▼
IOportunidadRepository ◄── OportunidadRepository   (ADO.NET + procedimientos almacenados)
          │
          ▼
      SQL Server
```

Registro con inyección de dependencias en `Program.cs`:

```csharp
builder.Services.AddSingleton<IDbConnectionFactory>(new SqlConnectionFactory(connectionString));
builder.Services.AddScoped<IOportunidadRepository, OportunidadRepository>();
builder.Services.AddScoped<IOportunidadService, OportunidadService>();
```

El controlador no conoce SQL ni ADO.NET; el repositorio no contiene reglas de negocio.

## Pruebas automatizadas

**Pendiente:** esta entrega todavía no incluye el proyecto de pruebas. Cuando se agregue, las
pruebas unitarias de transiciones y las de integración contra SQL Server se ejecutarán con:

```powershell
dotnet test
```

## Ejemplos rápidos

El archivo `src/Oportunidades.Api/Oportunidades.Api.http` contiene solicitudes listas para
Visual Studio o la extensión REST Client de VS Code, incluyendo las cabeceras `Authorization`.

## Notas

- No se usan migraciones ni Entity Framework Core; la base se crea con los scripts SQL incluidos.
- Los scripts son idempotentes y pueden ejecutarse de nuevo sin borrar datos.
- Si cambias el nombre de la base o de la instancia, actualiza la cadena de conexión.
