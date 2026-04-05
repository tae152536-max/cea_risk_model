CREATE TABLE [dbo].[Areas]
(
    [AreaID]    INT           IDENTITY(1,1) NOT NULL,
    [AreaName]  NVARCHAR(100) NOT NULL,
    [AreaCode]  NVARCHAR(20)  NULL,
    [Region]    NVARCHAR(100) NULL,
    [IsActive]  BIT           NOT NULL CONSTRAINT [DF_Areas_IsActive] DEFAULT (1),
    [CreatedAt] DATETIME      NOT NULL CONSTRAINT [DF_Areas_CreatedAt] DEFAULT (GETDATE()),

    CONSTRAINT [PK_Areas] PRIMARY KEY CLUSTERED ([AreaID] ASC),
    CONSTRAINT [UQ_Areas_Name] UNIQUE ([AreaName])
);
