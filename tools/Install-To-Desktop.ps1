# Run explicitly from the extracted Comet 0.2 binary folder. No SDK or administrator access needed.
$ErrorActionPreference = 'Stop'
if (Get-Process Comet -ErrorAction SilentlyContinue) { throw 'Quit Comet from its tray menu before copying a new build.' }
if (-not (Test-Path (Join-Path $PSScriptRoot 'Comet.exe'))) { throw 'Run this script from the extracted Comet binary folder.' }
$desktop = [Environment]::GetFolderPath('DesktopDirectory')
if (-not $desktop -or -not (Test-Path $desktop)) { throw 'Your Windows Desktop could not be found. Choose a writable local folder and extract the ZIP there.' }
$destination = Join-Path $desktop 'Comet-Windows'
if ([IO.Path]::GetFullPath($PSScriptRoot).TrimEnd('\') -eq [IO.Path]::GetFullPath($destination).TrimEnd('\')) { throw 'Extract into a different folder before running this backup-and-copy helper.' }
if (Test-Path $destination) {
    $backup = $destination + '-backup-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff')
    Move-Item -LiteralPath $destination -Destination $backup
    Write-Host "Previous Desktop build preserved at $backup"
}
New-Item -ItemType Directory -Path $destination | Out-Null
Get-ChildItem -LiteralPath $PSScriptRoot -Force | Copy-Item -Destination $destination -Recurse -Force
$contents = @(Get-ChildItem -LiteralPath $destination -Force | Where-Object Name -ne 'Comet-win-x64.zip')
Compress-Archive -LiteralPath $contents.FullName -DestinationPath (Join-Path $destination 'Comet-win-x64.zip')
Write-Host "Ready: $destination\Comet.exe"
Write-Host 'Existing startup settings still point to the old path. Save Settings after launching this copy if startup is enabled.'
