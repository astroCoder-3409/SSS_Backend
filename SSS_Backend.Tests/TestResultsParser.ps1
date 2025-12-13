# PowerShell script to parse test results and organize by file
param(
    [string]$TestOutputFile = "test-results.txt"
)

# Run tests and capture output
Write-Host "Running tests..." -ForegroundColor Cyan
$testOutput = dotnet test ..\SSS_Backend.Tests\SSS_Backend.Tests.csproj --logger "console;verbosity=normal" 2>&1 | Out-String

# Save raw output
$testOutput | Out-File -FilePath $TestOutputFile -Encoding UTF8

# Parse test results
$results = @{
    "DatabaseQueryTests.cs" = @{
        Passed = @()
        Failed = @()
    }
    "LLMServiceTests.cs" = @{
        Passed = @()
        Failed = @()
    }
}

# Parse the output
$lines = $testOutput -split "`n"
$currentTest = $null
$inFailure = $false
$failureDetails = @()

foreach ($line in $lines) {
    # Check for test results
    if ($line -match "Passed\s+([^\s]+)") {
        $testName = $matches[1]
        $currentTest = $testName
        
        # Determine which file this test belongs to
        $file = "Unknown"
        if ($testName -match "DatabaseQueryTests") {
            $file = "DatabaseQueryTests.cs"
        } elseif ($testName -match "LLMServiceTests") {
            $file = "LLMServiceTests.cs"
        }
        
        if ($results.ContainsKey($file)) {
            $results[$file].Passed += $testName
        }
        $inFailure = $false
    }
    elseif ($line -match "Failed\s+([^\s]+)") {
        $testName = $matches[1]
        $currentTest = $testName
        
        # Determine which file this test belongs to
        $file = "Unknown"
        if ($testName -match "DatabaseQueryTests") {
            $file = "DatabaseQueryTests.cs"
        } elseif ($testName -match "LLMServiceTests") {
            $file = "LLMServiceTests.cs"
        }
        
        if ($results.ContainsKey($file)) {
            $results[$file].Failed += $testName
        }
        $inFailure = $true
    }
}

# Generate report
$report = @"
# Test Results Report

Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

"@

foreach ($file in $results.Keys | Sort-Object) {
    $fileResults = $results[$file]
    $totalTests = $fileResults.Passed.Count + $fileResults.Failed.Count
    $passedCount = $fileResults.Passed.Count
    $failedCount = $fileResults.Failed.Count
    
    $report += @"

## $file

**Total Tests:** $totalTests | **Passed:** $passedCount | **Failed:** $failedCount

### Failed Tests ($failedCount)

"@
    
    if ($fileResults.Failed.Count -eq 0) {
        $report += "✅ No failed tests`n`n"
    } else {
        foreach ($test in $fileResults.Failed) {
            $report += "- ❌ $test`n"
        }
        $report += "`n"
    }
    
    $report += @"
### Passed Tests ($passedCount)

"@
    
    if ($fileResults.Passed.Count -eq 0) {
        $report += "⚠️ No passed tests`n`n"
    } else {
        foreach ($test in $fileResults.Passed) {
            $report += "- ✅ $test`n"
        }
        $report += "`n"
    }
}

# Calculate totals
$totalPassed = ($results.Values | ForEach-Object { $_.Passed.Count } | Measure-Object -Sum).Sum
$totalFailed = ($results.Values | ForEach-Object { $_.Failed.Count } | Measure-Object -Sum).Sum
$totalTests = $totalPassed + $totalFailed

$report += @"

---

## Summary

**Total Tests:** $totalTests
**Passed:** $totalPassed ✅
**Failed:** $totalFailed ❌
**Success Rate:** $([math]::Round(($totalPassed / $totalTests) * 100, 2))%
"@

# Output report
Write-Host "`n" -NoNewline
Write-Host $report

# Save report
$report | Out-File -FilePath "TEST_RESULTS_REPORT.md" -Encoding UTF8
Write-Host "`nReport saved to TEST_RESULTS_REPORT.md" -ForegroundColor Green
