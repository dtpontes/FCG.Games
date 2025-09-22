try {
    Write-Host "Testing health endpoint..."
    $healthResponse = Invoke-RestMethod -Uri "http://localhost:5000/health" -Method GET -TimeoutSec 10
    Write-Host "Health check response: $healthResponse"
    
    Write-Host "Testing games endpoint..."
    $gamesResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/Game/all" -Method GET -TimeoutSec 10
    Write-Host "Games endpoint response: $gamesResponse"
} catch {
    Write-Host "Error: $($_.Exception.Message)"
}