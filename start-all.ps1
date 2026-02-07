# start-all.ps1 — Lance le backend (GameServerApi) et le serveur statique pour le front
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$backDir = Join-Path $scriptDir "back"
$backendProjDir = Join-Path $backDir "GameServerApi"
$frontDir = Join-Path $scriptDir "front"
$backendPort = 5000
$frontPort = 8000

# Apply EF migrations before starting backend to avoid DB errors
if (Get-Command dotnet -ErrorAction SilentlyContinue -and (Test-Path $backendProjDir)) {
    Write-Host "Applying EF migrations (dotnet ef database update) in $backendProjDir ..." -ForegroundColor Cyan
    Push-Location $backendProjDir
    try {
        dotnet ef database update
    }
    catch {
        Write-Host "Failed to apply EF migrations: $_" -ForegroundColor Red
        Pop-Location
        exit 1
    }
    Pop-Location
}

# Backend: prefer dotnet (GameServerApi), fallback to mock-server if présent
if (Get-Command dotnet -ErrorAction SilentlyContinue) {
    Write-Host "Starting backend (GameServerApi) on http://localhost:$backendPort ..." -ForegroundColor Green
    Start-Process powershell -ArgumentList "-NoExit","-Command","cd '$backendProjDir'; `$env:ASPNETCORE_URLS='http://localhost:$backendPort'; dotnet run"
} elseif (Test-Path (Join-Path $scriptDir "mock-server")) {
    $mockDir = Join-Path $scriptDir "mock-server"
    # Install node deps if missing
    if (Test-Path (Join-Path $mockDir "package.json")) {
        if (-not (Test-Path (Join-Path $mockDir "node_modules"))) {
            Write-Host "Installing Node dependencies for mock backend..." -ForegroundColor Cyan
            Push-Location $mockDir
            npm install
            Pop-Location
        }
    }
    Write-Host "Starting mock backend on http://localhost:5000 ..." -ForegroundColor Green
    Start-Process powershell -ArgumentList "-NoExit","-Command","cd '$mockDir'; node server.js"
} else {
    Write-Host "Aucun backend détecté (dotnet ni mock-server). Lancez le backend manuellement." -ForegroundColor Yellow
}

# Front: serve `front` folder (python preferred, fallback to npx serve)
Write-Host "Starting static server for front on http://localhost:$frontPort ..." -ForegroundColor Green
if (Test-Path $frontDir) {
    if (Get-Command py -ErrorAction SilentlyContinue) {
        Start-Process powershell -ArgumentList "-NoExit","-Command","cd '$frontDir'; py -3 -m http.server $frontPort"
    } elseif (Get-Command python -ErrorAction SilentlyContinue) {
        Start-Process powershell -ArgumentList "-NoExit","-Command","cd '$frontDir'; python -m http.server $frontPort"
    } elseif (Get-Command npx -ErrorAction SilentlyContinue) {
        Start-Process powershell -ArgumentList "-NoExit","-Command","cd '$frontDir'; npx serve -l $frontPort"
    } else {
        Write-Host "Aucun outil détecté (Python ou npx). Lancez un serveur statique manuellement dans le dossier front." -ForegroundColor Yellow
    }
} else {
    Write-Host "Dossier front introuvable: $frontDir" -ForegroundColor Yellow
}

Write-Host "Done - backend: http://localhost:$backendPort  front: http://localhost:$frontPort" -ForegroundColor Cyan
