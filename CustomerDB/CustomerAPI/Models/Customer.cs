namespace CustomerAPI.Models;

public class ProductClassItem
{
    public string Name  { get; set; } = string.Empty;
    public string Class { get; set; } = "C";
}

// Payload sent from Google Forms via Apps Script
public class GoogleFormPayload
{
    public string DrName      { get; set; } = string.Empty;
    public string Hospital    { get; set; } = string.Empty;
    public string Address     { get; set; } = string.Empty;
    public string Area        { get; set; } = string.Empty;
    public string MedRepEmail { get; set; } = string.Empty;
    public string Product     { get; set; } = string.Empty;   // first/legacy
    public string Class       { get; set; } = "C";             // first/legacy
    public List<ProductClassItem> Products { get; set; } = new(); // all products
}

public class CustomerResult
{
    public int    CustomerID         { get; set; }
    public int    ExistingCustomerID { get; set; }
    public string Message            { get; set; } = string.Empty;
}

public class MedRepRequest
{
    public string? FullName { get; set; }
    public string? Email    { get; set; }
    public string? Phone    { get; set; }
    public int     AreaID   { get; set; }
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

public class AreaRequest
{
    public string  AreaName { get; set; } = string.Empty;
    public string  AreaCode { get; set; } = string.Empty;
    public string? Region   { get; set; }
}

public class CustomerEditRequest
{
    public int     MedRepID  { get; set; }
    public string? DrName    { get; set; }
    public string? Hospital  { get; set; }
    public string? Address   { get; set; }
    public string? Product   { get; set; }
    public string? Class     { get; set; }
    public List<ProductClassItem> Products { get; set; } = new();
}

public class DuplicateResult
{
    public int    CustomerID { get; set; }
    public string DrName     { get; set; } = string.Empty;
    public string Hospital   { get; set; } = string.Empty;
    public bool   IsDuplicate { get; set; }
    public string Reason     { get; set; } = string.Empty;
}
