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
