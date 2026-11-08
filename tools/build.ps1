$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    dotnet run --project tests/Comet.Tests -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Core tests failed' }
    dotnet publish src/Comet.Windows -c Release -r win-x64 --self-contained true -o artifacts/Comet-win-x64
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }
    dotnet publish tests/Comet.WindowsSmoke -c Release -r win-x64 --self-contained true -o artifacts/WindowsSmoke
    if ($LASTEXITCODE -ne 0) { throw 'Smoke harness publish failed' }
    foreach ($name in 'Comet.WindowsSmoke.exe','Comet.WindowsSmoke.dll','Comet.WindowsSmoke.deps.json','Comet.WindowsSmoke.runtimeconfig.json') {
        Copy-Item (Join-Path artifacts/WindowsSmoke $name) artifacts/Comet-win-x64 -Force
    }
    Copy-Item docs artifacts/Comet-win-x64/docs -Recurse -Force
    Copy-Item README.md,LICENSE artifacts/Comet-win-x64 -Force
    Copy-Item README.md artifacts/Comet-win-x64/README.txt -Force
    Copy-Item docs/TEST-RESULTS.md artifacts/Comet-win-x64/TEST_RESULTS.md -Force
    Copy-Item docs/KNOWN-LIMITATIONS.md artifacts/Comet-win-x64/KNOWN_LIMITATIONS.md -Force
    Copy-Item tools/install.ps1,tools/uninstall.ps1,tools/Install-To-Desktop.ps1,tools/Run-Windows-Tests.cmd artifacts/Comet-win-x64 -Force
    Copy-Item src/Comet.Windows/comet.ico artifacts/Comet-win-x64/Comet.ico -Force
    Compress-Archive -Path artifacts/Comet-win-x64/* -DestinationPath artifacts/Comet-0.2-win-x64.zip -Force
    Write-Host 'Created artifacts/Comet-0.2-win-x64.zip'
} finally { Pop-Location }
