-- =============================================
-- Returns customer list. MedRep only sees their own area.
-- Pass @MedRepID = 0 to get all (admin).
-- =============================================
CREATE PROCEDURE [dbo].[usp_GetCustomers]
    @MedRepID INT = 0,
    @AreaID   INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        c.[CustomerID],
        c.[DrName],
        c.[Hospital],
        c.[Address],
        a.[AreaName]      AS [Area],
        m.[FullName]      AS [MedRep],
        p.[ProductName]   AS [Product],
        p.[Class]         AS [ProductClass],
        c.[Class]         AS [CustomerClass],
        c.[TotalVisits],
        c.[LastVisitDate],
        c.[CreatedAt]
    FROM       [dbo].[Customers] c
    INNER JOIN [dbo].[Areas]     a ON a.[AreaID]    = c.[AreaID]
    INNER JOIN [dbo].[MedReps]   m ON m.[MedRepID]  = c.[MedRepID]
    LEFT JOIN  [dbo].[Products]  p ON p.[ProductID] = c.[ProductID]
    WHERE c.[IsActive]    = 1
      AND c.[IsDuplicate] = 0
      AND (@MedRepID = 0 OR c.[MedRepID] = @MedRepID)
      AND (@AreaID   = 0 OR c.[AreaID]   = @AreaID)
    ORDER BY a.[AreaName], c.[DrName];
END;
