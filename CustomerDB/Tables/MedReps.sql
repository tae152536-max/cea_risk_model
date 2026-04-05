CREATE TABLE [dbo].[MedReps]
(
    [MedRepID]     INT           IDENTITY(1,1) NOT NULL,
    [FullName]     NVARCHAR(150) NOT NULL,
    [Email]        NVARCHAR(200) NOT NULL,
    [Phone]        NVARCHAR(30)  NULL,
    [AreaID]       INT           NOT NULL,          -- current assigned area
    [IsActive]     BIT           NOT NULL CONSTRAINT [DF_MedReps_IsActive] DEFAULT (1),
    [CreatedAt]    DATETIME      NOT NULL CONSTRAINT [DF_MedReps_CreatedAt] DEFAULT (GETDATE()),
    [UpdatedAt]    DATETIME      NOT NULL CONSTRAINT [DF_MedReps_UpdatedAt] DEFAULT (GETDATE()),

    CONSTRAINT [PK_MedReps]         PRIMARY KEY CLUSTERED ([MedRepID] ASC),
    CONSTRAINT [UQ_MedReps_Email]   UNIQUE ([Email]),
    CONSTRAINT [FK_MedReps_Areas]   FOREIGN KEY ([AreaID]) REFERENCES [dbo].[Areas] ([AreaID])
);
