using CustomerAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace CustomerAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly string _conn;

    public CustomersController(IConfiguration cfg)
    {
        _conn = cfg.GetConnectionString("CustomerDB")
            ?? throw new InvalidOperationException("Connection string 'CustomerDB' not found.");
    }

    // ------------------------------------------------------------------
    // POST api/customers/from-google-form
    // Called by Google Apps Script on form submission
    // ------------------------------------------------------------------
    [HttpPost("from-google-form")]
    public async Task<IActionResult> FromGoogleForm([FromBody] GoogleFormPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.DrName) ||
            string.IsNullOrWhiteSpace(payload.Hospital) ||
            string.IsNullOrWhiteSpace(payload.MedRepEmail))
            return BadRequest("DrName, Hospital, and MedRepEmail are required.");

        await using var cn = new SqlConnection(_conn);
        await cn.OpenAsync();

        // Resolve AreaID
        int areaId = await LookupAreaAsync(cn, payload.Area);
        if (areaId == 0)
            return BadRequest($"Area '{payload.Area}' not found in database.");

        // Resolve MedRepID
        int medRepId = await LookupMedRepAsync(cn, payload.MedRepEmail);
        if (medRepId == 0)
            return BadRequest($"MedRep with email '{payload.MedRepEmail}' not found.");

        // Resolve ProductID (optional)
        int? productId = await LookupProductAsync(cn, payload.Product);

        // Call usp_AddCustomer
        await using var cmd = new SqlCommand("usp_AddCustomer", cn)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@DrName",    payload.DrName);
        cmd.Parameters.AddWithValue("@Hospital",  payload.Hospital);
        cmd.Parameters.AddWithValue("@Address",   payload.Address ?? "");
        cmd.Parameters.AddWithValue("@AreaID",    areaId);
        cmd.Parameters.AddWithValue("@MedRepID",  medRepId);
        cmd.Parameters.AddWithValue("@ProductID", (object?)productId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Class",     payload.Class ?? "C");
        cmd.Parameters.AddWithValue("@ForceInsert", 0);

        var result = new CustomerResult();
        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            result.CustomerID         = reader.GetInt32(0);
            result.ExistingCustomerID = reader.GetInt32(1);
            result.Message            = reader.GetString(2);
        }

        if (result.CustomerID == -1)
            return Conflict(result);   // 409 — duplicate
        if (result.CustomerID == -2)
            return BadRequest(result); // area mismatch

        return Ok(result);
    }

    // ------------------------------------------------------------------
    // GET api/customers?medRepId=5&areaId=0
    // ------------------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> GetCustomers(int medRepId = 0, int areaId = 0)
    {
        await using var cn = new SqlConnection(_conn);
        await cn.OpenAsync();

        await using var cmd = new SqlCommand("usp_GetCustomers", cn)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@MedRepID", medRepId);
        cmd.Parameters.AddWithValue("@AreaID",   areaId);

        var list = new List<object>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new
            {
                CustomerID    = reader["CustomerID"],
                DrName        = reader["DrName"],
                Hospital      = reader["Hospital"],
                Address       = reader["Address"],
                Area          = reader["Area"],
                MedRep        = reader["MedRep"],
                Product       = reader["Product"],
                ProductClass  = reader["ProductClass"],
                CustomerClass = reader["CustomerClass"],
                TotalVisits   = reader["TotalVisits"],
                LastVisitDate = reader["LastVisitDate"],
                CreatedAt     = reader["CreatedAt"]
            });
        }
        return Ok(list);
    }

    // ------------------------------------------------------------------
    // PATCH api/customers/{id}/area
    // ------------------------------------------------------------------
    [HttpPatch("{id}/area")]
    public async Task<IActionResult> UpdateArea(int id, [FromQuery] int newAreaId)
    {
        await using var cn = new SqlConnection(_conn);
        await cn.OpenAsync();

        await using var cmd = new SqlCommand("usp_UpdateArea", cn)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@CustomerID", id);
        cmd.Parameters.AddWithValue("@NewAreaID",  newAreaId);

        var msg = (string?)await cmd.ExecuteScalarAsync();
        return Ok(new { Message = msg });
    }

    // ------------------------------------------------------------------
    // PATCH api/customers/medrep/{medRepId}/area
    // ------------------------------------------------------------------
    [HttpPatch("medrep/{medRepId}/area")]
    public async Task<IActionResult> UpdateMedRepArea(int medRepId, [FromQuery] int newAreaId)
    {
        await using var cn = new SqlConnection(_conn);
        await cn.OpenAsync();

        await using var cmd = new SqlCommand("usp_UpdateMedRepArea", cn)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@MedRepID",  medRepId);
        cmd.Parameters.AddWithValue("@NewAreaID", newAreaId);

        var msg = (string?)await cmd.ExecuteScalarAsync();
        return Ok(new { Message = msg });
    }

    // ------------------------------------------------------------------
    // POST api/customers/login  — authenticate MedRep or Admin
    // ------------------------------------------------------------------
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
            return Unauthorized(new { Message = "Username and password required." });

        await using var cn = new SqlConnection(_conn);
        await cn.OpenAsync();

        // Admin login: username=admin, password=ADMIN2025
        if (req.Username.ToLower() == "admin" && req.Password == "ADMIN2025")
            return Ok(new { Role = "admin", Name = "Administrator", AreaName = "All Areas", AreaCode = "" });

        // MedRep login: username=sale, password=their area code
        if (req.Username.ToLower() == "sale")
        {
            var sql = @"SELECT m.MedRepID, m.FullName, m.Email, m.Phone,
                               a.AreaID, a.AreaName, a.AreaCode
                        FROM dbo.MedReps m
                        INNER JOIN dbo.Areas a ON a.AreaID = m.AreaID
                        WHERE a.AreaCode = @pwd AND m.IsActive = 1";
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@pwd", req.Password);
            await using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
                return Ok(new {
                    Role      = "medrep",
                    MedRepID  = r["MedRepID"],
                    Name      = r["FullName"],
                    Email     = r["Email"],
                    Phone     = r["Phone"],
                    AreaID    = r["AreaID"],
                    AreaName  = r["AreaName"],
                    AreaCode  = r["AreaCode"]
                });
            return Unauthorized(new { Message = "Invalid area code." });
        }

        return Unauthorized(new { Message = "Invalid username." });
    }

    // ------------------------------------------------------------------
    // GET api/customers/medrep/{medRepId}/list  — MedRep's own customers
    // ------------------------------------------------------------------
    [HttpGet("medrep/{medRepId}/list")]
    public async Task<IActionResult> GetMyCustomers(int medRepId)
    {
        await using var cn = new SqlConnection(_conn);
        await cn.OpenAsync();
        var sql = @"
            SELECT c.[CustomerID], c.[DrName], c.[Hospital], c.[Address],
                   a.[AreaName] AS Area, p.[ProductName] AS Product,
                   c.[Class], c.[Status], c.[TotalVisits], c.[LastVisitDate], c.[CreatedAt]
            FROM [dbo].[Customers] c
            INNER JOIN [dbo].[Areas]    a ON a.[AreaID]   = c.[AreaID]
            LEFT  JOIN [dbo].[Products] p ON p.[ProductID]= c.[ProductID]
            WHERE c.[MedRepID] = @MedRepID AND c.[IsActive] = 1
            ORDER BY c.[DrName]";
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@MedRepID", medRepId);
        var list = new List<object>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            list.Add(new {
                CustomerID   = r["CustomerID"],
                DrName       = r["DrName"],
                Hospital     = r["Hospital"],
                Address      = r["Address"],
                Area         = r["Area"],
                Product      = r["Product"],
                Class        = r["Class"],
                Status       = r["Status"],
                TotalVisits  = r["TotalVisits"],
                LastVisitDate= r["LastVisitDate"],
                CreatedAt    = r["CreatedAt"]
            });
        return Ok(list);
    }

    // ------------------------------------------------------------------
    // POST api/customers/{id}/visit  — record a visit
    // ------------------------------------------------------------------
    [HttpPost("{id}/visit")]
    public async Task<IActionResult> RecordVisit(int id, [FromBody] VisitRequest req)
    {
        await using var cn = new SqlConnection(_conn);
        await cn.OpenAsync();

        // Insert into Visits table
        var insertSql = @"
            INSERT INTO [dbo].[Visits] ([CustomerID],[MedRepID],[VisitDate],[ProductID],[Notes],[Outcome])
            VALUES (@CID, @MID, @Date, @PID, @Notes, @Outcome);
            UPDATE [dbo].[Customers]
            SET [TotalVisits]   = [TotalVisits] + 1,
                [LastVisitDate] = @Date,
                [UpdatedAt]     = GETDATE()
            WHERE [CustomerID] = @CID;";
        await using var cmd = new SqlCommand(insertSql, cn);
        cmd.Parameters.AddWithValue("@CID",    id);
        cmd.Parameters.AddWithValue("@MID",    req.MedRepID);
        cmd.Parameters.AddWithValue("@Date",   req.VisitDate?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("@PID",    (object?)req.ProductID ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Notes",  (object?)req.Notes     ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Outcome",(object?)req.Outcome   ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();

        // Return updated visit count
        await using var cmd2 = new SqlCommand(
            "SELECT TotalVisits, LastVisitDate FROM dbo.Customers WHERE CustomerID=@id", cn);
        cmd2.Parameters.AddWithValue("@id", id);
        await using var r = await cmd2.ExecuteReaderAsync();
        if (await r.ReadAsync())
            return Ok(new { Message = "Visit recorded.", TotalVisits = r["TotalVisits"], LastVisitDate = r["LastVisitDate"] });
        return Ok(new { Message = "Visit recorded." });
    }

    // ------------------------------------------------------------------
    // GET api/customers/{id}/visits  — visit history for a customer
    // ------------------------------------------------------------------
    [HttpGet("{id}/visits")]
    public async Task<IActionResult> GetVisits(int id)
    {
        await using var cn = new SqlConnection(_conn);
        await cn.OpenAsync();
        var sql = @"
            SELECT v.[VisitID], v.[VisitDate], m.[FullName] AS MedRep,
                   p.[ProductName] AS Product, v.[Notes], v.[Outcome], v.[CreatedAt]
            FROM [dbo].[Visits] v
            INNER JOIN [dbo].[MedReps]  m ON m.[MedRepID]  = v.[MedRepID]
            LEFT  JOIN [dbo].[Products] p ON p.[ProductID] = v.[ProductID]
            WHERE v.[CustomerID] = @id
            ORDER BY v.[VisitDate] DESC";
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@id", id);
        var list = new List<object>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            list.Add(new {
                VisitID   = r["VisitID"],
                VisitDate = r["VisitDate"],
                MedRep    = r["MedRep"],
                Product   = r["Product"],
                Notes     = r["Notes"],
                Outcome   = r["Outcome"]
            });
        return Ok(list);
    }

    // ------------------------------------------------------------------
    // GET api/customers/pending  — admin approval queue
    // ------------------------------------------------------------------
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        await using var cn = new SqlConnection(_conn);
        await cn.OpenAsync();
        await using var cmd = new SqlCommand("usp_GetPendingCustomers", cn)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };
        var list = new List<object>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new
            {
                CustomerID = reader["CustomerID"],
                DrName     = reader["DrName"],
                Hospital   = reader["Hospital"],
                Address    = reader["Address"],
                Area       = reader["Area"],
                AreaID     = reader["AreaID"],
                MedRep     = reader["MedRep"],
                MedRepID   = reader["MedRepID"],
                Product    = reader["Product"],
                Class      = reader["Class"],
                Status     = reader["Status"],
                CreatedAt  = reader["CreatedAt"]
            });
        }
        return Ok(list);
    }

    // ------------------------------------------------------------------
    // PATCH api/customers/{id}/approve  — approve or reject
    // ------------------------------------------------------------------
    [HttpPatch("{id}/approve")]
    public async Task<IActionResult> Approve(int id,
        [FromQuery] string action,
        [FromQuery] string adminName = "Admin",
        [FromQuery] string? rejectReason = null)
    {
        if (action != "Approve" && action != "Reject")
            return BadRequest("action must be 'Approve' or 'Reject'");

        await using var cn = new SqlConnection(_conn);
        await cn.OpenAsync();
        await using var cmd = new SqlCommand("usp_ApproveCustomer", cn)
        {
            CommandType = System.Data.CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@CustomerID",   id);
        cmd.Parameters.AddWithValue("@Action",       action);
        cmd.Parameters.AddWithValue("@AdminName",    adminName);
        cmd.Parameters.AddWithValue("@RejectReason", (object?)rejectReason ?? DBNull.Value);
        var msg = (string?)await cmd.ExecuteScalarAsync();
        return Ok(new { Message = msg });
    }

    // ------------------------------------------------------------------
    // GET api/customers/all  — all customers with status (admin view)
    // ------------------------------------------------------------------
    [HttpGet("all")]
    public async Task<IActionResult> GetAll([FromQuery] string? status = null)
    {
        await using var cn = new SqlConnection(_conn);
        await cn.OpenAsync();
        var sql = @"
            SELECT c.[CustomerID], c.[DrName], c.[Hospital], c.[Address],
                   a.[AreaName] AS Area, a.[AreaID],
                   m.[FullName] AS MedRep, m.[MedRepID],
                   p.[ProductName] AS Product, c.[Class],
                   c.[Status], c.[ApprovedBy], c.[ApprovedAt],
                   c.[RejectReason], c.[TotalVisits], c.[LastVisitDate], c.[CreatedAt]
            FROM [dbo].[Customers] c
            INNER JOIN [dbo].[Areas]   a ON a.[AreaID]   = c.[AreaID]
            INNER JOIN [dbo].[MedReps] m ON m.[MedRepID] = c.[MedRepID]
            LEFT  JOIN [dbo].[Products] p ON p.[ProductID]= c.[ProductID]
            WHERE c.[IsActive] = 1
              AND (@Status IS NULL OR c.[Status] = @Status)
            ORDER BY c.[CreatedAt] DESC";
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.AddWithValue("@Status", (object?)status ?? DBNull.Value);
        var list = new List<object>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new
            {
                CustomerID   = reader["CustomerID"],
                DrName       = reader["DrName"],
                Hospital     = reader["Hospital"],
                Address      = reader["Address"],
                Area         = reader["Area"],
                AreaID       = reader["AreaID"],
                MedRep       = reader["MedRep"],
                MedRepID     = reader["MedRepID"],
                Product      = reader["Product"],
                Class        = reader["Class"],
                Status       = reader["Status"],
                ApprovedBy   = reader["ApprovedBy"],
                ApprovedAt   = reader["ApprovedAt"],
                RejectReason = reader["RejectReason"],
                TotalVisits  = reader["TotalVisits"],
                LastVisitDate= reader["LastVisitDate"],
                CreatedAt    = reader["CreatedAt"]
            });
        }
        return Ok(list);
    }

    // ------------------------------------------------------------------
    // GET api/customers/areas  — list all areas
    // ------------------------------------------------------------------
    [HttpGet("areas")]
    public async Task<IActionResult> GetAreas()
    {
        await using var cn = new SqlConnection(_conn);
        await cn.OpenAsync();
        await using var cmd = new SqlCommand(
            "SELECT AreaID, AreaName, AreaCode, Region FROM dbo.Areas WHERE IsActive=1 ORDER BY AreaName", cn);
        var list = new List<object>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            list.Add(new { AreaID = r["AreaID"], AreaName = r["AreaName"], AreaCode = r["AreaCode"], Region = r["Region"] });
        return Ok(list);
    }

    // ------------------------------------------------------------------
    // POST api/customers/areas  — add a new area
    // ------------------------------------------------------------------
    [HttpPost("areas")]
    public async Task<IActionResult> AddArea([FromBody] AreaRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.AreaName) || string.IsNullOrWhiteSpace(req.AreaCode))
            return BadRequest("AreaName and AreaCode are required.");

        await using var cn = new SqlConnection(_conn);
        await cn.OpenAsync();
        await using var cmd = new SqlCommand(
            @"IF EXISTS (SELECT 1 FROM dbo.Areas WHERE AreaCode=@c OR AreaName=@n)
                  SELECT -1 AS AreaID;
              ELSE BEGIN
                  INSERT INTO dbo.Areas (AreaName, AreaCode, Region)
                  VALUES (@n, @c, @r);
                  SELECT SCOPE_IDENTITY() AS AreaID;
              END", cn);
        cmd.Parameters.AddWithValue("@n", req.AreaName);
        cmd.Parameters.AddWithValue("@c", req.AreaCode.ToUpper());
        cmd.Parameters.AddWithValue("@r", (object?)req.Region ?? DBNull.Value);
        var newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        if (newId == -1)
            return Conflict(new { Message = "Area name or code already exists." });
        return Ok(new { AreaID = newId, Message = "Area created successfully." });
    }

    // ------------------------------------------------------------------
    // PATCH api/customers/areas/{id}  — update area name/code/region
    // ------------------------------------------------------------------
    [HttpPatch("areas/{id}")]
    public async Task<IActionResult> UpdateArea(int id, [FromBody] AreaRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.AreaName) || string.IsNullOrWhiteSpace(req.AreaCode))
            return BadRequest("AreaName and AreaCode are required.");

        await using var cn = new SqlConnection(_conn);
        await cn.OpenAsync();
        await using var cmd = new SqlCommand(
            @"UPDATE dbo.Areas
              SET AreaName = @n, AreaCode = @c, Region = @r
              WHERE AreaID = @id AND IsActive = 1;
              SELECT @@ROWCOUNT;", cn);
        cmd.Parameters.AddWithValue("@n",  req.AreaName);
        cmd.Parameters.AddWithValue("@c",  req.AreaCode.ToUpper());
        cmd.Parameters.AddWithValue("@r",  (object?)req.Region ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@id", id);
        var rows = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        return rows > 0
            ? Ok(new { Message = "Area updated successfully." })
            : NotFound(new { Message = "Area not found." });
    }

    // ------------------------------------------------------------------
    // GET api/customers/medreps  — list all medreps
    // ------------------------------------------------------------------
    [HttpGet("medreps")]
    public async Task<IActionResult> GetMedReps()
    {
        await using var cn = new SqlConnection(_conn);
        await cn.OpenAsync();
        await using var cmd = new SqlCommand(
            @"SELECT m.MedRepID, m.FullName, m.Email, m.Phone, a.AreaID, a.AreaName, a.AreaCode
              FROM dbo.MedReps m INNER JOIN dbo.Areas a ON a.AreaID=m.AreaID
              WHERE m.IsActive=1 ORDER BY m.FullName", cn);
        var list = new List<object>();
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            list.Add(new { MedRepID = r["MedRepID"], FullName = r["FullName"],
                           Email = r["Email"], Phone = r["Phone"],
                           AreaID = r["AreaID"], AreaName = r["AreaName"], AreaCode = r["AreaCode"] });
        return Ok(list);
    }

    // ------------------------------------------------------------------
    // POST api/customers/medreps  — add new MedRep
    // ------------------------------------------------------------------
    [HttpPost("medreps")]
    public async Task<IActionResult> AddMedRep([FromBody] MedRepRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.FullName) || req.AreaID == 0)
            return BadRequest("FullName and AreaID are required.");

        await using var cn = new SqlConnection(_conn);
        await cn.OpenAsync();
        await using var cmd = new SqlCommand(
            @"INSERT INTO dbo.MedReps (FullName, Email, Phone, AreaID)
              VALUES (@n, @e, @p, @a);
              SELECT SCOPE_IDENTITY();", cn);
        cmd.Parameters.AddWithValue("@n", req.FullName);
        cmd.Parameters.AddWithValue("@e", (object?)req.Email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@p", (object?)req.Phone ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@a", req.AreaID);
        var newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        return Ok(new { MedRepID = newId, Message = "MedRep added successfully." });
    }

    // ------------------------------------------------------------------
    // PATCH api/customers/medrep/{id}  — update MedRep name/phone/area
    // ------------------------------------------------------------------
    [HttpPatch("medrep/{id}")]
    public async Task<IActionResult> UpdateMedRep(int id, [FromBody] MedRepRequest req)
    {
        await using var cn = new SqlConnection(_conn);
        await cn.OpenAsync();
        await using var cmd = new SqlCommand(
            @"UPDATE dbo.MedReps
              SET FullName   = COALESCE(NULLIF(@n,''), FullName),
                  Email      = COALESCE(NULLIF(@e,''), Email),
                  Phone      = COALESCE(NULLIF(@p,''), Phone),
                  AreaID     = CASE WHEN @a > 0 THEN @a ELSE AreaID END,
                  UpdatedAt  = GETDATE()
              WHERE MedRepID = @id AND IsActive = 1", cn);
        cmd.Parameters.AddWithValue("@n",  req.FullName ?? "");
        cmd.Parameters.AddWithValue("@e",  req.Email    ?? "");
        cmd.Parameters.AddWithValue("@p",  req.Phone    ?? "");
        cmd.Parameters.AddWithValue("@a",  req.AreaID);
        cmd.Parameters.AddWithValue("@id", id);
        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0
            ? Ok(new { Message = "MedRep updated successfully." })
            : NotFound(new { Message = "MedRep not found." });
    }

    // ------------------------------------------------------------------
    // DELETE api/customers/medrep/{id}  — deactivate MedRep
    // ------------------------------------------------------------------
    [HttpDelete("medrep/{id}")]
    public async Task<IActionResult> DeactivateMedRep(int id)
    {
        await using var cn = new SqlConnection(_conn);
        await cn.OpenAsync();
        await using var cmd = new SqlCommand(
            "UPDATE dbo.MedReps SET IsActive=0, UpdatedAt=GETDATE() WHERE MedRepID=@id", cn);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
        return Ok(new { Message = "MedRep deactivated." });
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------
    private static async Task<int> LookupAreaAsync(SqlConnection cn, string areaName)
    {
        await using var cmd = new SqlCommand(
            "SELECT [AreaID] FROM [dbo].[Areas] WHERE [AreaName] = @n AND [IsActive] = 1", cn);
        cmd.Parameters.AddWithValue("@n", areaName ?? "");
        return (int?)await cmd.ExecuteScalarAsync() ?? 0;
    }

    private static async Task<int> LookupMedRepAsync(SqlConnection cn, string email)
    {
        await using var cmd = new SqlCommand(
            "SELECT [MedRepID] FROM [dbo].[MedReps] WHERE [Email] = @e AND [IsActive] = 1", cn);
        cmd.Parameters.AddWithValue("@e", email ?? "");
        return (int?)await cmd.ExecuteScalarAsync() ?? 0;
    }

    private static async Task<int?> LookupProductAsync(SqlConnection cn, string productName)
    {
        if (string.IsNullOrWhiteSpace(productName)) return null;
        await using var cmd = new SqlCommand(
            "SELECT [ProductID] FROM [dbo].[Products] WHERE [ProductName] = @p AND [IsActive] = 1", cn);
        cmd.Parameters.AddWithValue("@p", productName);
        return (int?)await cmd.ExecuteScalarAsync();
    }
}
