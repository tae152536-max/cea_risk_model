-- =============================================
-- Migration: Add approval workflow to Customers
-- Run once on existing database
-- =============================================

-- 1. Add Status column to Customers
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Customers') AND name = 'Status'
)
BEGIN
    ALTER TABLE [dbo].[Customers]
    ADD [Status]      NVARCHAR(20) NOT NULL CONSTRAINT [DF_Customers_Status] DEFAULT ('Pending'),
        [ApprovedBy]  NVARCHAR(150) NULL,
        [ApprovedAt]  DATETIME      NULL,
        [RejectReason] NVARCHAR(300) NULL;

    PRINT 'Approval columns added.';
END
ELSE
    PRINT 'Approval columns already exist.';

-- 2. Set existing customers as Approved
UPDATE [dbo].[Customers]
SET [Status] = 'Approved'
WHERE [Status] = 'Pending';
