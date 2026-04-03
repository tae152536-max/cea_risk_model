CREATE TABLE [dbo].[Products]
(
    [ProductID]   INT           IDENTITY(1,1) NOT NULL,
    [ProductName] NVARCHAR(100) NOT NULL,
    [Class]       NVARCHAR(50)  NOT NULL,   -- e.g. Anticoagulant, NSAID, Contraceptive
    [IsActive]    BIT           NOT NULL CONSTRAINT [DF_Products_IsActive] DEFAULT (1),

    CONSTRAINT [PK_Products] PRIMARY KEY CLUSTERED ([ProductID] ASC),
    CONSTRAINT [UQ_Products_Name] UNIQUE ([ProductName])
);
