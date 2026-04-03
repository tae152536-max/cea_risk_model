-- =============================================
-- Get all pending customers for admin approval
-- =============================================
CREATE PROCEDURE [dbo].[usp_GetPendingCustomers]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        c.[CustomerID],
        c.[DrName],
        c.[Hospital],
        c.[Address],
        a.[AreaName]    AS [Area],
        a.[AreaID],
        m.[FullName]    AS [MedRep],
        m.[MedRepID],
        p.[ProductName] AS [Product],
        c.[Class],
        c.[Status],
        c.[CreatedAt]
    FROM       [dbo].[Customers] c
    INNER JOIN [dbo].[Areas]    a ON a.[AreaID]   = c.[AreaID]
    INNER JOIN [dbo].[MedReps]  m ON m.[MedRepID] = c.[MedRepID]
    LEFT JOIN  [dbo].[Products] p ON p.[ProductID]= c.[ProductID]
    WHERE c.[Status]   = 'Pending'
      AND c.[IsActive] = 1
    ORDER BY c.[CreatedAt] DESC;
END;
