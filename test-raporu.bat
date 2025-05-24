@echo off
echo ==========================================
echo     KeyManagementWeb Test Raporu
echo ==========================================
echo.

dotnet test KeyManagementWeb.Tests/KeyManagementWeb.Tests.csproj --no-build --nologo --logger:"console;verbosity=minimal"

echo.
echo ==========================================
