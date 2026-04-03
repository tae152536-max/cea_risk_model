namespace CustomerAPI.Models;

// Payload sent from Google Forms via Apps Script
public class GoogleFormPayload
{
    public string DrName    { get; set; } = string.Empty;
    public string Hospital  { get; set; } = string.Empty;
    public string Address   { get; set; } = string.Empty;
    public string Area      { get; set; } = string.Empty;   // Area name (looked up to AreaID)
    public string MedRepEmail { get; set; } = string.Empty; // used to identify MedRep
    public string Product   { get; set; } = string.Empty;
    public string Class     { get; set; } = "C";
}

public class CustomerResult
{
    public int    CustomerID         { get; set; }
    public int    ExistingCustomerID { get; set; }
    public string Message            { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class VisitRequest
{
    public int       MedRepID   { get; set; }
    public DateTime? VisitDate  { get; set; }
    public int?      ProductID  { get; set; }
    public string?   Notes      { get; set; }
    public string?   Outcome    { get; set; }
}

public class DuplicateResult
{
    public int    CustomerID { get; set; }
    public string DrName     { get; set; } = string.Empty;
    public string Hospital   { get; set; } = string.Empty;
    public bool   IsDuplicate { get; set; }
    public string Reason     { get; set; } = string.Empty;
}
