# CustomerDB Migration Runner
# Run as Administrator — uses built-in .NET SqlClient, no extra modules needed

$db     = "CustomerDB"
$folder = "C:\Users\Admin\Desktop\CustomerDB\CustomerDB\Scripts"

# Auto-detect SQL Server instance
$servers = @(".\SQLEXPRESS", ".\MSSQLSERVER", "localhost", ".", "(local)")
$connStr = $null
foreach ($s in $servers) {
    try {
        $test = New-Object System.Data.SqlClient.SqlConnection "Server=$s;Database=$db;Integrated Security=True;Connect Timeout=3;"
        $test.Open()
        $test.Close()
        $connStr = "Server=$s;Database=$db;Integrated Security=True;"
        Write-Host "Connected to: $s" -ForegroundColor Green
        break
    } catch {}
}

if (!$connStr) {
    Write-Host "Cannot connect to SQL Server. Check that SQL Server is running." -ForegroundColor Red
    pause; exit
}

function Run-Script($file) {
    $sql = Get-Content "$folder\$file" -Raw
    $batches = $sql -split '\r?\nGO\r?\n|\r?\nGO$'
    $cn = New-Object System.Data.SqlClient.SqlConnection $connStr
    $cn.Open()
    foreach ($batch in $batches) {
        $b = $batch.Trim()
        if ($b.Length -eq 0) { continue }
        try {
            $cmd = New-Object System.Data.SqlClient.SqlCommand($b, $cn)
            $cmd.ExecuteNonQuery() | Out-Null
        } catch {
            Write-Host "  Warning: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
    $cn.Close()
    Write-Host "✓ $file done" -ForegroundColor Green
}

Write-Host "Running migrations..." -ForegroundColor Cyan
Run-Script "Migration_AddClassD.sql"
Run-Script "Migration_AddCategory.sql"
Run-Script "Migration_AddSpecialty.sql"
Run-Script "Migration_CustomerProducts.sql"

Write-Host ""
Write-Host "All migrations done! Now restart StartServers.bat as Admin." -ForegroundColor Yellow
pause
