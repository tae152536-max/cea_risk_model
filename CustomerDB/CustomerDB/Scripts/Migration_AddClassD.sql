-- Migration: Allow Class 'D' in Customers table
-- The existing CHECK constraint only allows A/B/C — drop and recreate to add D
-- Run once on the live database

IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_Customers_Class'
      AND parent_object_id = OBJECT_ID('dbo.Customers')
)
BEGIN
    ALTER TABLE [dbo].[Customers] DROP CONSTRAINT [CK_Customers_Class];
    PRINT 'Old CK_Customers_Class constraint dropped.';
END

ALTER TABLE [dbo].[Customers]
ADD CONSTRAINT [CK_Customers_Class] CHECK ([Class] IN ('A','B','C','D'));

PRINT 'CK_Customers_Class updated — A, B, C, D now allowed.';
