@echo off
echo ============================================
echo  CustomerDB Server Launcher (Run as Admin)
echo ============================================

:: Kill existing processes
echo Stopping old processes...
taskkill /F /IM CustomerAPI.exe /T >nul 2>&1
taskkill /F /IM dotnet.exe /T >nul 2>&1
taskkill /F /IM python.exe /T >nul 2>&1
timeout /t 2 /nobreak >nul

:: Open firewall for port 8600 only (proxy handles everything)
echo Opening firewall port 8600...
netsh advfirewall firewall delete rule name="CustomerDB-HTTP-8600" >nul 2>&1
netsh advfirewall firewall add rule name="CustomerDB-HTTP-8600" dir=in action=allow protocol=TCP localport=8600 profile=any >nul
echo Firewall OK.

:: Start API on localhost only (not exposed to network - proxy handles it)
echo Starting API on localhost:5000...
start "CustomerDB API" /min cmd /c "cd /d C:\Users\Admin\Desktop\CustomerDB\CustomerAPI && dotnet run"
timeout /t 8 /nobreak >nul

:: Start proxy server on port 8600 (serves static files + proxies /api/ to port 5000)
echo Starting proxy server on port 8600...
start "CustomerDB Proxy" /min cmd /c "cd /d C:\Users\Admin\Desktop\CustomerDB && python server.py"
timeout /t 2 /nobreak >nul

echo.
echo ============================================
echo  All servers running!
echo.
echo  Any device on WiFi can access:
echo    Admin:   http://192.168.1.35:8600/admin.html
echo    MedRep:  http://192.168.1.35:8600/medrep.html
echo.
echo  Login: username=sale  password=area code (e.g. CN01)
echo  Admin: username=admin password=ADMIN2025
echo ============================================
echo.
pause
