$testResults = dotnet test KeyManagementWeb.Tests/KeyManagementWeb.Tests.csproj --no-build --verbosity=normal
$output = $testResults -join "`n"

# Create report header
$totalTests = 0
$passedTests = 0
$failedTests = 0

if ($output -match "Toplam test sayisi: (\d+)") {
    $totalTests = $matches[1]
}
if ($output -match "Gecti: (\d+)") {
    $passedTests = $matches[1]
}
if ($output -match "Basarisiz: (\d+)") {
    $failedTests = $matches[1]
}

# Extract test results
$testResults = @()

$lines = $output -split "`n"
$currentTest = ""
$currentStatus = ""
$errorMessage = ""

foreach ($line in $lines) {
    # Determine test name and status
    if ($line -match "^\s*((Basarili)|(Basarisiz)) (.+) \[\d+ ms\]") {
        # Save previous test
        if ($currentTest -ne "") {
            $testObj = @{
                "Test" = $currentTest
                "Status" = $currentStatus
                "Error" = $errorMessage
            }
            $testResults += $testObj
        }
        
        $currentStatus = $matches[1]
        $currentTest = $matches[4]
        $errorMessage = ""
    }
    # Determine error message
    elseif ($line -match "^\s*Hata Iletisi:(.+)") {
        $errorMessage = $matches[1].Trim()
    }
}

# Add the last test too
if ($currentTest -ne "") {
    $testObj = @{
        "Test" = $currentTest
        "Status" = $currentStatus
        "Error" = $errorMessage
    }
    $testResults += $testObj
}

# Create report
Write-Output "============ TEST RAPORU ============"
Write-Output "Toplam Test: $totalTests"
Write-Output "Basarili: $passedTests"
Write-Output "Basarisiz: $failedTests"
Write-Output "======================================"
Write-Output ""
Write-Output "BASARILI TESTLER:"
Write-Output "----------------"
foreach ($test in $testResults) {
    if ($test.Status -eq "Basarili") {
        Write-Output "+ $($test.Test)"
    }
}

Write-Output ""
Write-Output "BASARISIZ TESTLER:"
Write-Output "----------------"
foreach ($test in $testResults) {
    if ($test.Status -eq "Basarisiz") {
        Write-Output "- $($test.Test)"
        if ($test.Error -ne "") {
            Write-Output "   Hata: $($test.Error)"
        }
        Write-Output ""
    }
}

Write-Output "======================================="
