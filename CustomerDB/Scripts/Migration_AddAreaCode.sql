-- =============================================
-- Migration: Add AreaCode column to Areas table
-- Run once on existing database
-- =============================================

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Areas') AND name = 'AreaCode'
)
BEGIN
    ALTER TABLE [dbo].[Areas]
    ADD [AreaCode] NVARCHAR(20) NULL;

    PRINT 'AreaCode column added.';
END
ELSE
    PRINT 'AreaCode column already exists.';
GO

-- Set default area codes for seeded areas (update if already exist)
UPDATE [dbo].[Areas] SET [AreaCode] = 'CNR'  WHERE [AreaName] = 'Cairo North'  AND ([AreaCode] IS NULL OR [AreaCode] = '');
UPDATE [dbo].[Areas] SET [AreaCode] = 'CSO'  WHERE [AreaName] = 'Cairo South'  AND ([AreaCode] IS NULL OR [AreaCode] = '');
UPDATE [dbo].[Areas] SET [AreaCode] = 'ALX'  WHERE [AreaName] = 'Alexandria'   AND ([AreaCode] IS NULL OR [AreaCode] = '');
UPDATE [dbo].[Areas] SET [AreaCode] = 'GIZ'  WHERE [AreaName] = 'Giza'         AND ([AreaCode] IS NULL OR [AreaCode] = '');
UPDATE [dbo].[Areas] SET [AreaCode] = 'DEL'  WHERE [AreaName] = 'Delta'        AND ([AreaCode] IS NULL OR [AreaCode] = '');
UPDATE [dbo].[Areas] SET [AreaCode] = 'UPE'  WHERE [AreaName] = 'Upper Egypt'  AND ([AreaCode] IS NULL OR [AreaCode] = '');
UPDATE [dbo].[Areas] SET [AreaCode] = 'CNL'  WHERE [AreaName] = 'Canal Zone'   AND ([AreaCode] IS NULL OR [AreaCode] = '');

PRINT 'Area codes set.';
GO

-- Show current areas and their codes
SELECT AreaID, AreaName, AreaCode FROM [dbo].[Areas] WHERE IsActive = 1;
