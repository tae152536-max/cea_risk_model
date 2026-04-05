-- =============================================
-- Changes the area assignment for a MedRep.
-- Admin use only.
-- =============================================
CREATE PROCEDURE [dbo].[usp_UpdateMedRepArea]
    @MedRepID  INT,
    @NewAreaID INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM [dbo].[MedReps] WHERE [MedRepID] = @MedRepID AND [IsActive] = 1)
    BEGIN
        SELECT 'MedRep not found.' AS [Message]; RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Areas] WHERE [AreaID] = @NewAreaID AND [IsActive] = 1)
    BEGIN
        SELECT 'Area not found.' AS [Message]; RETURN;
    END

    UPDATE [dbo].[MedReps]
    SET    [AreaID]    = @NewAreaID,
           [UpdatedAt] = GETDATE()
    WHERE  [MedRepID] = @MedRepID;

    SELECT 'MedRep area updated successfully.' AS [Message];
END;
