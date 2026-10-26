#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

function Assert-LastExitCode {
    param(
        [Parameter(Mandatory)]
        [string] $FailureMessage
    )

    if ($LASTEXITCODE -ne 0) {
        Write-Error $FailureMessage
        exit $LASTEXITCODE
    }
}

$devSettingsPath = Join-Path $repoRoot 'API/appsettings.Development.json'
if (-not (Test-Path -LiteralPath $devSettingsPath)) {
    Write-Error "Missing $devSettingsPath"
    exit 1
}

$devSettings = Get-Content -LiteralPath $devSettingsPath -Raw | ConvertFrom-Json
$connStr = $null
if ($devSettings.PSObject.Properties['ConnectionStrings'] -and $devSettings.ConnectionStrings.PSObject.Properties['DefaultConnection']) {
    $connStr = $devSettings.ConnectionStrings.DefaultConnection
}
elseif ($devSettings.PSObject.Properties['ConnectionString']) {
    $firstProp = $devSettings.ConnectionString.PSObject.Properties | Select-Object -First 1
    if ($firstProp) { $connStr = $firstProp.Value }
}

if ([string]::IsNullOrWhiteSpace($connStr)) {
    Write-Error "No development connection string configured in $devSettingsPath"
    exit 1
}

$dbMatch = [regex]::Match($connStr, '(?i)(?:Database|Initial Catalog)\s*=\s*([^;]+)')
$pwMatch = [regex]::Match($connStr, '(?i)(?:Password|Pwd)\s*=\s*([^;]+)')
if (-not $dbMatch.Success -or -not $pwMatch.Success) {
    Write-Error "Could not parse Database and Password from development connection string."
    exit 1
}

$databaseName = $dbMatch.Groups[1].Value.Trim()
$saPassword = $pwMatch.Groups[1].Value.Trim()

Write-Host "Ensuring local infrastructure (SQL Server database '$databaseName')..."

docker info --format '{{.ServerVersion}}' 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Error @'
Docker is not available. Start Docker Desktop for Windows with Linux containers enabled, then retry.
'@
    exit 1
}

docker compose version | Out-Null
Assert-LastExitCode 'Docker Compose is not available. Update Docker Desktop and retry.'

docker compose up -d --wait sqlserver
Assert-LastExitCode 'Failed to start local Docker services (SQL Server). See the output above.'

docker compose exec -T sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P $saPassword -C -Q "IF DB_ID(N'$databaseName') IS NULL CREATE DATABASE [$databaseName];"
Assert-LastExitCode "Failed to create the $databaseName database in the local SQL Server container."

Write-Host 'Local infrastructure is ready.'
