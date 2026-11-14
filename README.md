# Oportunidades API

API REST para registrar, consultar y actualizar el estado de oportunidades comerciales.
Construida con **C# / ASP.NET Core Web API (.NET 10)**, **ADO.NET** y **SQL Server**.
Todo el acceso a datos se realiza mediante **procedimientos almacenados**.

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
├─ tests/
│  ├─ Oportunidades.UnitTests/          # Pruebas de transiciones de estado
│  └─ Oportunidades.IntegrationTests/   # Pruebas HTTP contra SQL Server de pruebas
├─ tools/
│  └─ Oportunidades.TokenGenerator/  # Genera tokens JWT locales para Consulta y Editor
└─ README.md
```
## Requisitos

- [.NET SDK 10.0.403](https://dotnet.microsoft.com/download) (probado con esta versión; el proyecto apunta a `net10.0`).
- SQL Server 2019 o superior (Express, Developer o Enterprise; también funciona con LocalDB).
- Opcional: `sqlcmd` o SQL Server Management Studio para ejecutar los scripts de base de datos.


## Pasos para preparar SQL

 **Crear la base de datos, la tabla y los procedimientos.** 
   Ejecuta los dos scripts en orden.
   Ambos son idempotentes: se pueden ejecutar varias veces sin borrar datos.

   ```powershell
   sqlcmd -S localhost -E -C -i database/01_crear_base.sql
   sqlcmd -S localhost -E -C -i database/02_stored_procedures.sql
   ```

   También puedes abrirlos en SQL Server Management Studio y ejecutarlos en ese mismo orden.

## Configuración

Las claves están en `src/Oportunidades.Api/appsettings.json`:

- `ConnectionStrings:Oportunidades`: cadena de conexión a SQL Server.
- `Jwt:Issuer`: `OportunidadesApi`.
- `Jwt:Audience`: `OportunidadesApiClientes`.



`Jwt:SigningKey` no se llena sola: el programador debe generarla y guardarla una vez. En local
se usa user secrets, no hace falta definir `Jwt__SigningKey`:

```powershell
$bytes = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$key = [Convert]::ToBase64String($bytes)

dotnet user-secrets set "Jwt:SigningKey" "$key" --project src/Oportunidades.Api
```


## Restaurar y ejecutar el API

```powershell
dotnet restore
dotnet run --project src/Oportunidades.Api
```

La API queda disponible en `http://localhost:5020` (perfil `http` de
`launchSettings.json`). El documento OpenAPI está en
`http://localhost:5020/openapi/v1.json` y el health check en
`http://localhost:5020/health` (sin autenticación).

### Generar tokens locales

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



## Pruebas

El repositorio incluye dos proyectos de prueba dentro de la solución:

- `Oportunidades.UnitTests`: pruebas de las transiciones de estado permitidas y rechazadas.
- `Oportunidades.IntegrationTests`: pruebas HTTP de alta, consulta, validación, autenticación y autorización.



### Requisitos

- Las pruebas unitarias no necesitan SQL Server.
- Las pruebas de integración sí necesitan SQL Server.
- La primera ejecución crea automáticamente la base `OportunidadesDb_Test` usando
  `database/01_crear_base.sql` y `database/02_stored_procedures.sql`.
- La base `Oportunidades` no se modifica. Antes de cada prueba se limpia únicamente
  `OportunidadesDb_Test.dbo.Oportunidades`.
- El usuario de SQL Server debe tener permiso para crear bases de datos. Si la base
  `OportunidadesDb_Test` ya existe, se reutiliza.
- Las pruebas generan su propio token JWT; no hace falta ejecutar el generador de tokens.


### Ejecutar todas las pruebas

Desde la raíz del repositorio:

```powershell
dotnet test OportunidadesAPI.sln
```

Resultado esperado: **16 pruebas unitarias y 5 de integración**.

### Ejecutar solo las pruebas unitarias


```powershell
dotnet test tests/Oportunidades.UnitTests/Oportunidades.UnitTests.csproj
```

### Ejecutar solo las pruebas de integración

Requieren SQL Server:

```powershell
dotnet test tests/Oportunidades.IntegrationTests/Oportunidades.IntegrationTests.csproj
```

### Ejecutar una prueba concreta

Por clase:

```powershell
dotnet test tests/Oportunidades.UnitTests/Oportunidades.UnitTests.csproj --filter 
```

Por nombre de prueba:

```powershell
dotnet test tests/Oportunidades.IntegrationTests/Oportunidades.IntegrationTests.csproj --filter 


```

Para ver el detalle de la ejecución:

```powershell
dotnet test OportunidadesAPI.sln --logger "console;verbosity=normal"
```



