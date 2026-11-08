$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    dotnet run --project tests/Comet.Tests -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Core tests failed' }
    dotnet publish src/Comet.Windows -c Release -r win-x64 --self-contained true -o artifacts/Comet-win-x64
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
    dotnet build tests/Comet.WindowsSmoke -c Release -r win-x64
    if ($LASTEXITCODE -ne 0) { throw 'Smoke harness build failed' }
    Copy-Item docs artifacts/Comet-win-x64/docs -Recurse -Force
    Copy-Item README.md,LICENSE artifacts/Comet-win-x64 -Force
    Copy-Item tools/install.ps1,tools/uninstall.ps1 artifacts/Comet-win-x64 -Force
    Compress-Archive -Path artifacts/Comet-win-x64/* -DestinationPath artifacts/Comet-win-x64.zip -Force
    Write-Host 'Created artifacts/Comet-win-x64.zip'
} finally { Pop-Location }
