-- =============================================
-- Reassigns a Customer to a different Area.
-- Only an admin / manager can call this.
-- =============================================
CREATE PROCEDURE [dbo].[usp_UpdateArea]
    @CustomerID INT,
    @NewAreaID  INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Customers] WHERE [CustomerID] = @CustomerID AND [IsActive] = 1)
    BEGIN
        SELECT 'Customer not found.' AS [Message]; RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Areas] WHERE [AreaID] = @NewAreaID AND [IsActive] = 1)
    BEGIN
        SELECT 'Area not found.' AS [Message]; RETURN;
    END

    UPDATE [dbo].[Customers]
    SET    [AreaID]    = @NewAreaID,
           [UpdatedAt] = GETDATE()
    WHERE  [CustomerID] = @CustomerID;

    SELECT 'Area updated successfully.' AS [Message];
END;
