# Removes only this per-user installation; personal settings/GIFs are preserved.
$ErrorActionPreference = 'Stop'
if (Get-Process Comet -ErrorAction SilentlyContinue) { throw 'Quit Comet from its tray menu before uninstalling.' }
$destination = Join-Path $env:LOCALAPPDATA 'Programs\Comet'
$shortcut = Join-Path ([Environment]::GetFolderPath('StartMenu')) 'Programs\Comet.lnk'
Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name Comet -ErrorAction SilentlyContinue
if (Test-Path $shortcut) { Remove-Item $shortcut }
if (Test-Path $destination) { Remove-Item $destination -Recurse -Force }
Write-Host 'Comet uninstalled. Personal settings and GIFs remain in %LOCALAPPDATA%\Comet.'
