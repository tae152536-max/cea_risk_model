@echo off
title CustomerDB Servers
echo Starting CustomerDB servers...
echo.

:: Start .NET API on port 5000
start "CustomerDB API (port 5000)" cmd /k "cd /d C:\Users\Admin\Desktop\CustomerDB\CustomerAPI && dotnet run"

:: Wait a moment then start Python proxy on port 8600
timeout /t 3 /nobreak >nul
start "Python Proxy (port 8600)" cmd /k "cd /d C:\Users\Admin\Desktop\CustomerDB && python server.py"

echo.
echo Both servers starting. Wait ~10 seconds for .NET to compile.
echo Then open medrep.html or admin.html in your browser.
pause
