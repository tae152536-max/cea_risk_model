-- Migration: Add Category column to Customers table
-- Values: 'Hospital' (default) | 'OTC'
-- Run once on the live database

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Customers') AND name = 'Category'
)
BEGIN
    ALTER TABLE [dbo].[Customers]
    ADD [Category] NVARCHAR(20) NOT NULL DEFAULT 'Hospital';
    PRINT 'Category column added to Customers.';
END
ELSE
    PRINT 'Category column already exists.';
