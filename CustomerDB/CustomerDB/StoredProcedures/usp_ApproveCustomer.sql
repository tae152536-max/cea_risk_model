-- =============================================
-- Approve or Reject a pending customer
-- =============================================
CREATE PROCEDURE [dbo].[usp_ApproveCustomer]
    @CustomerID   INT,
    @Action       NVARCHAR(10),   -- 'Approve' or 'Reject'
    @AdminName    NVARCHAR(150),
    @RejectReason NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Customers] WHERE [CustomerID] = @CustomerID)
    BEGIN
        SELECT 'Customer not found.' AS [Message]; RETURN;
    END

    IF @Action = 'Approve'
    BEGIN
        UPDATE [dbo].[Customers]
        SET [Status]     = 'Approved',
            [ApprovedBy] = @AdminName,
            [ApprovedAt] = GETDATE(),
            [UpdatedAt]  = GETDATE()
        WHERE [CustomerID] = @CustomerID;
        SELECT 'Customer approved successfully.' AS [Message];
    END
    ELSE IF @Action = 'Reject'
    BEGIN
        UPDATE [dbo].[Customers]
        SET [Status]       = 'Rejected',
            [ApprovedBy]   = @AdminName,
            [ApprovedAt]   = GETDATE(),
            [RejectReason] = @RejectReason,
            [UpdatedAt]    = GETDATE()
        WHERE [CustomerID] = @CustomerID;
        SELECT 'Customer rejected.' AS [Message];
    END
    ELSE
        SELECT 'Invalid action. Use Approve or Reject.' AS [Message];
END;
