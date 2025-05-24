@echo off
echo === KeyManagementWeb Test Raporu ===
echo.

dotnet test KeyManagementWeb.Tests/KeyManagementWeb.Tests.csproj --no-build --logger:"console;verbosity=minimal" | findstr /i "Basarili Basarisiz" > test-output.txt

echo === Test Ozeti ===
type test-output.txt
echo.
