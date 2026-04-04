-- Migration: Add CustomerProducts table for multi-product support
-- Run this once on the live database

-- Create CustomerProducts junction table
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CustomerProducts')
BEGIN
    CREATE TABLE [dbo].[CustomerProducts] (
        [ID]         INT IDENTITY(1,1) PRIMARY KEY,
        [CustomerID] INT NOT NULL,
        [ProductID]  INT NOT NULL,
        [Class]      NVARCHAR(10) NOT NULL DEFAULT 'C' CHECK ([Class] IN ('A','B','C','D')),
        [CreatedAt]  DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_CP_Customer FOREIGN KEY ([CustomerID]) REFERENCES [dbo].[Customers]([CustomerID]),
        CONSTRAINT FK_CP_Product  FOREIGN KEY ([ProductID])  REFERENCES [dbo].[Products]([ProductID]),
        CONSTRAINT UQ_CP_CustomerProduct UNIQUE ([CustomerID], [ProductID])
    );
    PRINT 'CustomerProducts table created.';
END
ELSE
    PRINT 'CustomerProducts table already exists.';

-- Migrate existing single-product data into CustomerProducts
INSERT INTO [dbo].[CustomerProducts] ([CustomerID], [ProductID], [Class])
SELECT c.[CustomerID], c.[ProductID], ISNULL(c.[Class], 'C')
FROM [dbo].[Customers] c
WHERE c.[ProductID] IS NOT NULL
  AND NOT EXISTS (
    SELECT 1 FROM [dbo].[CustomerProducts] cp
    WHERE cp.[CustomerID] = c.[CustomerID] AND cp.[ProductID] = c.[ProductID]
  );

PRINT CAST(@@ROWCOUNT AS VARCHAR) + ' existing customer products migrated.';
