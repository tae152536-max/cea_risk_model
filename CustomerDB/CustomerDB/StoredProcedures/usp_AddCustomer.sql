-- =============================================
-- Adds a new customer submitted by a MedRep.
-- Runs duplicate detection first.
-- Returns the new CustomerID, or -1 if duplicate.
-- =============================================
CREATE PROCEDURE [dbo].[usp_AddCustomer]
    @DrName      NVARCHAR(200),
    @Hospital    NVARCHAR(200),
    @Address     NVARCHAR(500),
    @AreaID      INT,
    @MedRepID    INT,
    @ProductID   INT           = NULL,
    @Class       NVARCHAR(10)  = 'C',
    @ForceInsert BIT           = 0    -- set 1 to bypass duplicate block (admin only)
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Duplicate check
    DECLARE @IsDuplicate    BIT = 0;
    DECLARE @ExistingID     INT = 0;
    DECLARE @DupReason      NVARCHAR(200);

    SELECT
        @ExistingID  = [CustomerID],
        @IsDuplicate = [IsDuplicate],
        @DupReason   = [Reason]
    FROM (
        EXEC [dbo].[usp_DetectDuplicate] @DrName, @Hospital
    ) AS dup;

    -- Workaround: inline the duplicate logic directly
    SET @IsDuplicate = 0;
    SET @ExistingID  = 0;

    SELECT TOP 1
        @ExistingID  = [CustomerID],
        @IsDuplicate = 1
    FROM [dbo].[Customers]
    WHERE ([DrName] = @DrName AND [Hospital] = @Hospital)
       OR (LEFT([DrName], 10) = LEFT(@DrName, 10) AND [Hospital] = @Hospital)
    AND [IsActive] = 1;

    -- 2. If duplicate and not forced
    IF @IsDuplicate = 1 AND @ForceInsert = 0
    BEGIN
        -- Log the duplicate attempt
        INSERT INTO [dbo].[DuplicateLog]
            ([NewDrName], [NewHospital], [ExistingCustomerID], [SubmittedByMedRep])
        VALUES
            (@DrName,     @Hospital,     @ExistingID,          @MedRepID);

        -- Return -1 to signal duplicate
        SELECT
            -1           AS [CustomerID],
            @ExistingID  AS [ExistingCustomerID],
            'Duplicate detected — record not inserted.' AS [Message];
        RETURN;
    END

    -- 3. Validate AreaID belongs to this MedRep (unless admin forces)
    IF NOT EXISTS (
        SELECT 1 FROM [dbo].[MedReps]
        WHERE [MedRepID] = @MedRepID AND [AreaID] = @AreaID AND [IsActive] = 1
    )
    BEGIN
        SELECT
            -2 AS [CustomerID],
            0  AS [ExistingCustomerID],
            'Area does not match MedRep assignment.' AS [Message];
        RETURN;
    END

    -- 4. Insert
    INSERT INTO [dbo].[Customers]
        ([DrName], [Hospital], [Address], [AreaID], [MedRepID], [ProductID], [Class], [CreatedBy])
    VALUES
        (@DrName,  @Hospital,  @Address,  @AreaID,  @MedRepID,  @ProductID,  @Class,  @MedRepID);

    DECLARE @NewID INT = SCOPE_IDENTITY();

    SELECT
        @NewID AS [CustomerID],
        0      AS [ExistingCustomerID],
        'Customer added successfully.' AS [Message];
END;
