# Optional per-user installation. Run from the extracted binary distribution.
$ErrorActionPreference = 'Stop'
$source = $PSScriptRoot
$destination = Join-Path $env:LOCALAPPDATA 'Programs\Comet'
if (-not (Test-Path (Join-Path $source 'Comet.exe'))) { throw 'Run install.ps1 from the extracted Comet binary folder.' }
if (Get-Process Comet -ErrorAction SilentlyContinue) { throw 'Quit Comet from its tray menu before installing.' }
if ([IO.Path]::GetFullPath($source).TrimEnd('\') -eq [IO.Path]::GetFullPath($destination).TrimEnd('\')) { throw 'Comet is already in the installation folder.' }
New-Item -ItemType Directory -Path $destination -Force | Out-Null
Copy-Item -Path (Join-Path $source '*') -Destination $destination -Recurse -Force
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut((Join-Path ([Environment]::GetFolderPath('StartMenu')) 'Programs\Comet.lnk'))
$shortcut.TargetPath = Join-Path $destination 'Comet.exe'
$shortcut.WorkingDirectory = $destination
$shortcut.Save()
Write-Host "Installed Comet in $destination. Launch it from the Start menu. Startup is opt-in in Settings."
