CREATE TABLE [dbo].[DuplicateLog]
(
    [LogID]              INT           IDENTITY(1,1) NOT NULL,
    [NewDrName]          NVARCHAR(200) NOT NULL,
    [NewHospital]        NVARCHAR(200) NOT NULL,
    [ExistingCustomerID] INT           NOT NULL,
    [SubmittedByMedRep]  INT           NOT NULL,
    [DetectedAt]         DATETIME      NOT NULL CONSTRAINT [DF_DuplicateLog_DetectedAt] DEFAULT (GETDATE()),
    [Resolved]           BIT           NOT NULL CONSTRAINT [DF_DuplicateLog_Resolved] DEFAULT (0),
    [Resolution]         NVARCHAR(200) NULL,

    CONSTRAINT [PK_DuplicateLog] PRIMARY KEY CLUSTERED ([LogID] ASC),
    CONSTRAINT [FK_DupLog_Existing] FOREIGN KEY ([ExistingCustomerID]) REFERENCES [dbo].[Customers] ([CustomerID]),
    CONSTRAINT [FK_DupLog_MedRep]   FOREIGN KEY ([SubmittedByMedRep])  REFERENCES [dbo].[MedReps]   ([MedRepID])
);
