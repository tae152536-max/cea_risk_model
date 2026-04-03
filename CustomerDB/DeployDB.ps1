# ============================================================
# DeployDB.ps1  —  Creates CustomerDB on LocalDB and runs all SQL files
# Run with: Right-click → "Run with PowerShell"
# ============================================================

$server = ".\SQLEXPRESS"
$db     = "CustomerDB"
$root   = "$PSScriptRoot\CustomerDB"

function Exec-Sql($connectionString, $sql) {
    $cn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $cn.Open()
    $cmd = $cn.CreateCommand()
    $cmd.CommandText = $sql
    $cmd.CommandTimeout = 60
    $cmd.ExecuteNonQuery() | Out-Null
    $cn.Close()
}

function Exec-File($connectionString, $filePath) {
    $sql = Get-Content $filePath -Raw
    # Split on GO statements
    $batches = $sql -split '\r?\nGO\r?\n|\r?\nGO$'
    $cn = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $cn.Open()
    foreach ($batch in $batches) {
        $b = $batch.Trim()
        if ($b -eq "") { continue }
        $cmd = $cn.CreateCommand()
        $cmd.CommandText = $b
        $cmd.CommandTimeout = 60
        try { $cmd.ExecuteNonQuery() | Out-Null }
        catch { Write-Warning "  Warning in $([System.IO.Path]::GetFileName($filePath)): $_" }
    }
    $cn.Close()
    Write-Host "  OK: $([System.IO.Path]::GetFileName($filePath))" -ForegroundColor Green
}

$masterConn = "Server=$server;Database=master;Integrated Security=True;"
$dbConn     = "Server=$server;Database=$db;Integrated Security=True;"

Write-Host "`n=== Customer Database Deployment ===" -ForegroundColor Cyan

# 1. Create database
Write-Host "`n[1] Creating database '$db'..."
Exec-Sql $masterConn "IF DB_ID('$db') IS NULL CREATE DATABASE [$db]"
Write-Host "  OK: Database ready" -ForegroundColor Green

# 2. Tables (order matters — FK dependencies)
Write-Host "`n[2] Creating tables..."
$tables = @("Areas","Products","MedReps","Customers","Visits","DuplicateLog")
foreach ($t in $tables) {
    $file = "$root\Tables\$t.sql"
    if (Test-Path $file) { Exec-File $dbConn $file }
    else { Write-Warning "  Not found: $file" }
}

# 3. Stored procedures
Write-Host "`n[3] Creating stored procedures..."
$procs = @(
    "usp_DetectDuplicate",
    "usp_AddCustomer",
    "usp_UpdateArea",
    "usp_UpdateMedRepArea",
    "usp_GetCustomers"
)
foreach ($p in $procs) {
    $file = "$root\StoredProcedures\$p.sql"
    if (Test-Path $file) { Exec-File $dbConn $file }
    else { Write-Warning "  Not found: $file" }
}

# 4. Seed data
Write-Host "`n[4] Seeding reference data..."
Exec-File $dbConn "$root\Scripts\SeedData.sql"

Write-Host "`n=== Deployment Complete! ===" -ForegroundColor Cyan
Write-Host "Connection string:" -ForegroundColor Yellow
Write-Host "  Server=$server;Database=$db;Integrated Security=True;" -ForegroundColor White
Write-Host "`nPress any key to close..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
