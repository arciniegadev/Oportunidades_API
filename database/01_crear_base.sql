/*
    Oportunidades API - Script de creación de la base de datos.
    Ejecutar en una instancia de SQL Server con permisos para crear bases de datos.
    El script es idempotente: se puede ejecutar más de una vez sin borrar datos.
*/

IF DB_ID(N'Oportunidades') IS NULL
BEGIN
    CREATE DATABASE Oportunidades;
END
GO

USE Oportunidades;
GO

IF OBJECT_ID(N'dbo.Oportunidades', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Oportunidades
    (
        Id               INT            IDENTITY(1, 1) NOT NULL,
        Nombre           NVARCHAR(100)  NOT NULL,
        Cliente          NVARCHAR(120)  NOT NULL,
        MontoEstimado    DECIMAL(18, 2) NOT NULL,
        Estado           NVARCHAR(20)   NOT NULL
            CONSTRAINT DF_Oportunidades_Estado DEFAULT (N'Nueva'),
        FechaCreacionUtc DATETIME2(7)   NOT NULL
            CONSTRAINT DF_Oportunidades_FechaCreacionUtc DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT PK_Oportunidades PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Oportunidades_Nombre_NoVacio
            CHECK (LEN(LTRIM(RTRIM(Nombre))) > 0),
        CONSTRAINT CK_Oportunidades_Cliente_NoVacio
            CHECK (LEN(LTRIM(RTRIM(Cliente))) > 0),
        CONSTRAINT CK_Oportunidades_MontoEstimado_Positivo
            CHECK (MontoEstimado > 0),
        CONSTRAINT CK_Oportunidades_Estado
            CHECK (Estado IN (N'Nueva', N'EnSeguimiento', N'Ganada', N'Perdida'))
    );

    CREATE INDEX IX_Oportunidades_Estado_FechaCreacionUtc
        ON dbo.Oportunidades (Estado, FechaCreacionUtc DESC);
END
GO

PRINT N'Base de datos OportunidadesDb lista.';
GO
