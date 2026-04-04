-- Migration: Add Specialty column to Customers table
-- Stores doctor specialty for Hospital-category customers
-- Run once on the live database

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Customers') AND name = 'Specialty'
)
BEGIN
    ALTER TABLE [dbo].[Customers]
    ADD [Specialty] NVARCHAR(100) NULL;
    PRINT 'Specialty column added to Customers.';
END
ELSE
    PRINT 'Specialty column already exists.';
