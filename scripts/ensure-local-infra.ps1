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

Write-Host 'Ensuring local FullStackTemplate infrastructure (SQL Server)...'

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

docker exec full-stack-template-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P FullStackTemplate_Local_Dev_1 -C -Q "IF DB_ID(N'FullStackTemplate') IS NULL CREATE DATABASE [FullStackTemplate];"
Assert-LastExitCode 'Failed to create the FullStackTemplate database in the local SQL Server container.'

Write-Host 'Local infrastructure is ready.'
