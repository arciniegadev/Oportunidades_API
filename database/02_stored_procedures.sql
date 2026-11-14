/*
    Oportunidades API - Procedimientos almacenados.
    Ejecutar después de 01_crear_base.sql (creación de la base de datos y la tabla).
    El script es idempotente: usa CREATE OR ALTER y se puede ejecutar más de una vez.
*/

USE Oportunidades;
GO

-- ============================================================
-- Alta
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.usp_Oportunidades_Crear
    @Nombre        NVARCHAR(100),
    @Cliente       NVARCHAR(120),
    @MontoEstimado DECIMAL(18, 2)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Oportunidades (Nombre, Cliente, MontoEstimado, Estado, FechaCreacionUtc)
    OUTPUT inserted.Id,
           inserted.Nombre,
           inserted.Cliente,
           inserted.MontoEstimado,
           inserted.Estado,
           inserted.FechaCreacionUtc
    VALUES (LTRIM(RTRIM(@Nombre)), LTRIM(RTRIM(@Cliente)), @MontoEstimado, N'Nueva', SYSUTCDATETIME());
END
GO

-- ============================================================
-- Consulta por ID
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.usp_Oportunidades_ObtenerPorId
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Nombre, Cliente, MontoEstimado, Estado, FechaCreacionUtc
    FROM dbo.Oportunidades
    WHERE Id = @Id;
END
GO

-- ============================================================
-- Listado (filtro opcional por estado)
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.usp_Oportunidades_Listar
    @Estado NVARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, Nombre, Cliente, MontoEstimado, Estado, FechaCreacionUtc
    FROM dbo.Oportunidades
    WHERE @Estado IS NULL OR Estado = @Estado
    ORDER BY FechaCreacionUtc DESC, Id DESC;
END
GO

-- ============================================================
-- Cambio de estado con control de concurrencia.
--
-- Solo actualiza si:
--   1. El estado persistido sigue siendo @EstadoEsperado.
--   2. La transición es válida:
--        Nueva         -> EnSeguimiento
--        EnSeguimiento -> Ganada
--        EnSeguimiento -> Perdida
--      (Ganada y Perdida son finales y no admiten cambios).
--
-- Si no coincide el estado esperado (modificación concurrente) o la
-- transición no es válida, no devuelve filas y la aplicación responde
-- 409 Conflict.
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.usp_Oportunidades_CambiarEstado
    @Id             INT,
    @EstadoEsperado NVARCHAR(20),
    @NuevoEstado    NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Oportunidades
    SET Estado = @NuevoEstado
    OUTPUT inserted.Id,
           inserted.Nombre,
           inserted.Cliente,
           inserted.MontoEstimado,
           inserted.Estado,
           inserted.FechaCreacionUtc
    WHERE Id = @Id
      AND Estado = @EstadoEsperado
      AND (
            (@EstadoEsperado = N'Nueva'         AND @NuevoEstado = N'EnSeguimiento')
         OR (@EstadoEsperado = N'EnSeguimiento' AND @NuevoEstado IN (N'Ganada', N'Perdida'))
      );
END
GO

PRINT N'Procedimientos almacenados de Oportunidades listos.';
GO
