CREATE TABLE [dbo].[Customers]
(
    -- Identity
    [CustomerID]    INT           IDENTITY(1,1) NOT NULL,

    -- Doctor info
    [DrName]        NVARCHAR(200) NOT NULL,
    [Hospital]      NVARCHAR(200) NOT NULL,
    [Address]       NVARCHAR(500) NULL,

    -- Classification
    [Class]         NVARCHAR(10)  NOT NULL CONSTRAINT [DF_Customers_Class] DEFAULT ('C'),
                    -- A = High value, B = Medium, C = New/Low

    -- Relationships
    [AreaID]        INT           NOT NULL,
    [MedRepID]      INT           NOT NULL,
    [ProductID]     INT           NULL,

    -- Visit tracking
    [TotalVisits]   INT           NOT NULL CONSTRAINT [DF_Customers_TotalVisits] DEFAULT (0),
    [LastVisitDate] DATE          NULL,

    -- Audit
    [IsActive]      BIT           NOT NULL CONSTRAINT [DF_Customers_IsActive] DEFAULT (1),
    [IsDuplicate]   BIT           NOT NULL CONSTRAINT [DF_Customers_IsDuplicate] DEFAULT (0),
    [CreatedAt]     DATETIME      NOT NULL CONSTRAINT [DF_Customers_CreatedAt] DEFAULT (GETDATE()),
    [UpdatedAt]     DATETIME      NOT NULL CONSTRAINT [DF_Customers_UpdatedAt] DEFAULT (GETDATE()),
    [CreatedBy]     INT           NULL,   -- MedRepID who added this record

    CONSTRAINT [PK_Customers]          PRIMARY KEY CLUSTERED ([CustomerID] ASC),
    CONSTRAINT [FK_Customers_Areas]    FOREIGN KEY ([AreaID])    REFERENCES [dbo].[Areas]    ([AreaID]),
    CONSTRAINT [FK_Customers_MedReps]  FOREIGN KEY ([MedRepID])  REFERENCES [dbo].[MedReps]  ([MedRepID]),
    CONSTRAINT [FK_Customers_Products] FOREIGN KEY ([ProductID]) REFERENCES [dbo].[Products] ([ProductID]),
    CONSTRAINT [CK_Customers_Class]    CHECK ([Class] IN ('A','B','C'))
);

CREATE NONCLUSTERED INDEX [IX_Customers_MedRep]  ON [dbo].[Customers] ([MedRepID]);
CREATE NONCLUSTERED INDEX [IX_Customers_Area]    ON [dbo].[Customers] ([AreaID]);
CREATE NONCLUSTERED INDEX [IX_Customers_DrName]  ON [dbo].[Customers] ([DrName]);
