-- =============================================
-- Checks if a doctor already exists.
-- Matches on DrName similarity + same Hospital.
-- Returns the existing CustomerID if duplicate found, else 0.
-- =============================================
CREATE PROCEDURE [dbo].[usp_DetectDuplicate]
    @DrName   NVARCHAR(200),
    @Hospital NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    -- Exact match (case-insensitive by collation)
    IF EXISTS (
        SELECT 1
        FROM   [dbo].[Customers]
        WHERE  [DrName]   = @DrName
          AND  [Hospital] = @Hospital
          AND  [IsActive] = 1
    )
    BEGIN
        SELECT TOP 1
            [CustomerID],
            [DrName],
            [Hospital],
            [AreaID],
            [MedRepID],
            1 AS [IsDuplicate],
            'Exact match on Dr Name + Hospital' AS [Reason]
        FROM [dbo].[Customers]
        WHERE [DrName]   = @DrName
          AND [Hospital] = @Hospital
          AND [IsActive] = 1;
        RETURN;
    END

    -- Fuzzy: same hospital, name starts the same (first 10 chars)
    IF EXISTS (
        SELECT 1
        FROM   [dbo].[Customers]
        WHERE  LEFT([DrName], 10)   = LEFT(@DrName, 10)
          AND  [Hospital]           = @Hospital
          AND  [IsActive]           = 1
    )
    BEGIN
        SELECT TOP 1
            [CustomerID],
            [DrName],
            [Hospital],
            [AreaID],
            [MedRepID],
            1 AS [IsDuplicate],
            'Possible duplicate — similar name at same hospital' AS [Reason]
        FROM [dbo].[Customers]
        WHERE LEFT([DrName], 10) = LEFT(@DrName, 10)
          AND [Hospital]         = @Hospital
          AND [IsActive]         = 1;
        RETURN;
    END

    -- No duplicate found
    SELECT
        0    AS [CustomerID],
        NULL AS [DrName],
        NULL AS [Hospital],
        NULL AS [AreaID],
        NULL AS [MedRepID],
        0    AS [IsDuplicate],
        'No duplicate found' AS [Reason];
END;
