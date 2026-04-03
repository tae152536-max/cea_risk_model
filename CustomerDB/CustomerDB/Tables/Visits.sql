CREATE TABLE [dbo].[Visits]
(
    [VisitID]      INT           IDENTITY(1,1) NOT NULL,
    [CustomerID]   INT           NOT NULL,
    [MedRepID]     INT           NOT NULL,
    [VisitDate]    DATE          NOT NULL CONSTRAINT [DF_Visits_VisitDate] DEFAULT (CAST(GETDATE() AS DATE)),
    [ProductID]    INT           NULL,
    [Notes]        NVARCHAR(MAX) NULL,
    [Outcome]      NVARCHAR(200) NULL,
    [CreatedAt]    DATETIME      NOT NULL CONSTRAINT [DF_Visits_CreatedAt] DEFAULT (GETDATE()),

    CONSTRAINT [PK_Visits]           PRIMARY KEY CLUSTERED ([VisitID] ASC),
    CONSTRAINT [FK_Visits_Customers] FOREIGN KEY ([CustomerID]) REFERENCES [dbo].[Customers] ([CustomerID]),
    CONSTRAINT [FK_Visits_MedReps]   FOREIGN KEY ([MedRepID])   REFERENCES [dbo].[MedReps]   ([MedRepID]),
    CONSTRAINT [FK_Visits_Products]  FOREIGN KEY ([ProductID])  REFERENCES [dbo].[Products]  ([ProductID])
);

CREATE NONCLUSTERED INDEX [IX_Visits_Customer] ON [dbo].[Visits] ([CustomerID]);
CREATE NONCLUSTERED INDEX [IX_Visits_MedRep]   ON [dbo].[Visits] ([MedRepID]);
CREATE NONCLUSTERED INDEX [IX_Visits_Date]     ON [dbo].[Visits] ([VisitDate]);
