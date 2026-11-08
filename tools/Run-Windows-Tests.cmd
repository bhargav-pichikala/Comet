@echo off
cd /d "%~dp0"
echo Quit Comet from the system tray before continuing.
echo The test mode accepts synthetic test keystrokes. Do not type or move the mouse during the tests.
pause
start "" "%~dp0Comet.exe" --test-input
timeout /t 2 /nobreak >nul
start /wait "" "%~dp0Comet.WindowsSmoke.exe"
echo Tests closed. Quit test-mode Comet from its tray, then start Comet.exe normally.
pause
