# Test script for LLM endpoint with DUMMY DATA
# Make sure your backend AND LLM server are running before executing this script

$baseUrl = "http://localhost:5290"  # Default port from launchSettings.json

Write-Host "================================" -ForegroundColor Cyan
Write-Host "Testing LLM Endpoint (Dummy Data)" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "NOTE: Using dummy data - no database required!" -ForegroundColor Green
Write-Host ""

# Test 1: Default query (current month)
Write-Host "Test 1: Testing with default query (current month)..." -ForegroundColor Yellow
$body1 = @{
    query = "How can I cut down on expenses and save more?"
} | ConvertTo-Json

try {
    $response1 = Invoke-RestMethod -Uri "$baseUrl/api/test/llm-dummy" -Method Post -Body $body1 -ContentType "application/json"
    Write-Host "✓ Success!" -ForegroundColor Green
    
    if ($response1.test_mode) {
        Write-Host "Test Mode: $($response1.test_mode)" -ForegroundColor Gray
    }
    if ($response1.query) {
        Write-Host "Query: $($response1.query)" -ForegroundColor Gray
    }
    if ($response1.month -and $response1.year) {
        $monthStr = [string]$response1.month
        $yearStr = [string]$response1.year
        Write-Host "Period: $monthStr $yearStr" -ForegroundColor Gray
    }
    if ($null -ne $response1.total_spending) {
        Write-Host "Total Spending: `$$($response1.total_spending)" -ForegroundColor Gray
    }
    if ($null -ne $response1.total_categories) {
        Write-Host "Categories Found: $($response1.total_categories)" -ForegroundColor Gray
    }
    
    Write-Host ""
    if ($response1.spending_summary) {
        Write-Host "Dummy Spending Categories:" -ForegroundColor Cyan
        foreach ($category in $response1.spending_summary) {
            $catName = [string]$category.category
            $catAmount = [string]$category.amount
            Write-Host "  - ${catName}: `$${catAmount}" -ForegroundColor White
        }
        Write-Host ""
    }
    
    if ($response1.llm_response) {
        Write-Host "LLM Response:" -ForegroundColor Cyan
        Write-Host "$($response1.llm_response)" -ForegroundColor White
    }
    Write-Host ""
}
catch {
    Write-Host "✗ Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "Status Code: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
    if ($_.ErrorDetails.Message) {
        Write-Host "Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Test 2: Specific month and year
Write-Host "Test 2: Testing with specific month (January 2024)..." -ForegroundColor Yellow
$body2 = @{
    query = "What were my biggest expenses?"
    month = 1
    year = 2024
} | ConvertTo-Json

try {
    $response2 = Invoke-RestMethod -Uri "$baseUrl/api/test/llm-dummy" -Method Post -Body $body2 -ContentType "application/json"
    Write-Host "✓ Success!" -ForegroundColor Green
    
    if ($response2.month -and $response2.year) {
        $monthStr = [string]$response2.month
        $yearStr = [string]$response2.year
        Write-Host "Period: $monthStr $yearStr" -ForegroundColor Gray
    }
    if ($null -ne $response2.total_spending) {
        Write-Host "Total Spending: `$$($response2.total_spending)" -ForegroundColor Gray
    }
    
    Write-Host ""
    if ($response2.llm_response) {
        Write-Host "LLM Response:" -ForegroundColor Cyan
        Write-Host "$($response2.llm_response)" -ForegroundColor White
    }
}
catch {
    Write-Host "✗ Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "Status Code: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
    if ($_.ErrorDetails.Message) {
        Write-Host "Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
Write-Host "Testing Complete!" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Cyan

