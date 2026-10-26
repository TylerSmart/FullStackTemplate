#Requires -Version 5.1
param(
    [string] $Ref = 'template/main',
    [switch] $NoFetch
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (git rev-parse --show-toplevel 2>$null)
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($repoRoot)) {
    Write-Error 'Must be run inside a Git repository.'
    exit 1
}
Set-Location $repoRoot

# Guard against running inside the upstream template repository itself
$originUrl = (git remote get-url origin 2>$null)
if ($originUrl -and $originUrl -match 'FullStackTemplate(\.git)?$') {
    Write-Error 'merge-template.ps1 is meant to be run in downstream forks, not in FullStackTemplate itself.'
    exit 1
}

git rev-parse -q --verify MERGE_HEAD 2>$null | Out-Null
if ($LASTEXITCODE -eq 0) {
    Write-Error 'A merge is already in progress. Finish or abort (`git merge --abort`) before running merge-template.ps1.'
    exit 1
}

$dirty = @(git status --porcelain --untracked-files=no)
if ($dirty.Count -gt 0) {
    Write-Error 'Working tree has uncommitted changes to tracked files. Commit or stash before syncing.'
    exit 1
}

if (-not $NoFetch -and $Ref -like 'template/*') {
    git remote get-url template 2>$null | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Remote 'template' is not configured. Run: git remote add template https://github.com/TylerSmart/FullStackTemplate.git"
        exit 1
    }
    Write-Host "Fetching 'template' remote..." -ForegroundColor Cyan
    git fetch template
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to fetch 'template' remote."
        exit 1
    }
}

# Snapshot this fork's own migration files prior to merging so template migrations never pollute fork history
$ourMigrationFiles = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($f in @(git ls-files -- 'Data/Migrations/')) {
    if (-not [string]::IsNullOrWhiteSpace($f)) {
        [void]$ourMigrationFiles.Add($f)
    }
}

Write-Host "Merging '$Ref' (--no-ff --no-commit)..." -ForegroundColor Cyan
$ErrorActionPreference = 'Continue'
$mergeOutput = git merge --no-ff --no-commit $Ref 2>&1
$mergeExit = $LASTEXITCODE
$ErrorActionPreference = 'Stop'

$mergeOutput | ForEach-Object { Write-Host $_ }

git rev-parse -q --verify MERGE_HEAD 2>$null | Out-Null
$hasMergeHead = ($LASTEXITCODE -eq 0)

if (-not $hasMergeHead) {
    if ($mergeExit -eq 0) {
        Write-Host 'Already up to date with template.' -ForegroundColor Green
        exit 0
    }
    Write-Error "git merge failed before creating MERGE_HEAD (exit code $mergeExit). If this fork has unrelated history, see docs/template-sync.md for the one-time baseline merge command."
    exit $mergeExit
}

# Strip any template-added files in Data/Migrations/ and keep this fork's own ModelSnapshot & migrations
$postMergeMigrations = @(git ls-files -u -- 'Data/Migrations/' | ForEach-Object { ($_ -split "`t", 2)[1] } | Select-Object -Unique)
$postMergeMigrations += @(git ls-files -- 'Data/Migrations/')
$postMergeMigrations = @($postMergeMigrations | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)

foreach ($migFile in $postMergeMigrations) {
    if ($ourMigrationFiles.Contains($migFile)) {
        git checkout HEAD -- $migFile | Out-Null
        git add -- $migFile | Out-Null
    }
    else {
        Write-Host "  Stripping upstream template migration file: $migFile" -ForegroundColor DarkYellow
        git rm -f -- $migFile | Out-Null
    }
}

$remainingConflicts = @(git diff --name-only --diff-filter=U)
$schemaTouched = @(git diff --cached --name-only -- Data/Entities Data/Configurations Data/AppDbContext.cs)

Write-Host ''
if ($remainingConflicts.Count -gt 0) {
    Write-Host "Conflicts requiring manual resolution ($($remainingConflicts.Count)):" -ForegroundColor Yellow
    foreach ($c in $remainingConflicts) {
        Write-Host "  - $c" -ForegroundColor Yellow
    }
}
else {
    Write-Host 'All files merged cleanly (0 remaining conflicts).' -ForegroundColor Green
}

if ($schemaTouched.Count -gt 0) {
    Write-Host ''
    Write-Host 'Entity / DbContext changes were brought in from the template:' -ForegroundColor Cyan
    foreach ($s in $schemaTouched) {
        Write-Host "  - $s" -ForegroundColor Cyan
    }
    Write-Host 'After resolving any conflicts, generate this fork''s EF migration before committing:' -ForegroundColor Cyan
    Write-Host '  dotnet ef migrations add <MigrationName> --project Data/App.Data.csproj' -ForegroundColor White
    Write-Host '  dotnet csharpier format Data/Migrations' -ForegroundColor White
}

Write-Host ''
Write-Host 'Next steps:' -ForegroundColor Green
Write-Host '  1. Resolve any conflicts listed above and stage them (`git add <file>`).'
Write-Host '  2. Generate an EF migration if entity models changed (see above).'
Write-Host '  3. Run backend + client builds and tests.'
Write-Host '  4. Complete the merge commit: `git commit`'
