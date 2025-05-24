$testResults = dotnet test KeyManagementWeb.Tests/KeyManagementWeb.Tests.csproj --no-build --verbosity=normal
$output = $testResults -join "`n"

# Rapor başlığı oluşturma
$totalTests = 0
$passedTests = 0
$failedTests = 0

if ($output -match "Toplam test sayısı: (\d+)") {
    $totalTests = $matches[1]
}
if ($output -match "Geçti: (\d+)") {
    $passedTests = $matches[1]
}
if ($output -match "Başarısız: (\d+)") {
    $failedTests = $matches[1]
}

# Test sonuçlarını çıkarma
$testResults = @()

$lines = $output -split "`n"
$currentTest = ""
$currentStatus = ""
$errorMessage = ""

foreach ($line in $lines) {
    # Test ismi ve durumu belirleme
    if ($line -match "^\s*((Başarılı)|(Başarısız)) (.+) \[\d+ ms\]") {
        # Önceki testi kaydet
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
    # Hata mesajı belirleme
    elseif ($line -match "^\s*Hata İletisi:(.+)") {
        $errorMessage = $matches[1].Trim()
    }
}

# Son testi de ekle
if ($currentTest -ne "") {
    $testObj = @{
        "Test" = $currentTest
        "Status" = $currentStatus
        "Error" = $errorMessage
    }
    $testResults += $testObj
}

# Raporu oluştur
Write-Output "============ TEST RAPORU ============"
Write-Output "Toplam Test: $totalTests"
Write-Output "Başarılı: $passedTests"
Write-Output "Başarısız: $failedTests"
Write-Output "======================================"
Write-Output ""
Write-Output "BAŞARILI TESTLER:"
Write-Output "----------------"
foreach ($test in $testResults) {
    if ($test.Status -eq "Başarılı") {
        Write-Output "✓ $($test.Test)"
    }
}

Write-Output ""
Write-Output "BAŞARISIZ TESTLER:"
Write-Output "----------------"
foreach ($test in $testResults) {
    if ($test.Status -eq "Başarısız") {
        Write-Output "✗ $($test.Test)"
        if ($test.Error -ne "") {
            Write-Output "   Hata: $($test.Error)"
        }
        Write-Output ""
    }
}

Write-Output "======================================"
