@echo off
setlocal
if not exist "%~dp0build\NVIDIAHelper.exe" (
  echo Application file is missing: build\NVIDIAHelper.exe
  echo Pull the complete repository or run Build.ps1.
  pause
  exit /b 1
)
start "" "%~dp0build\NVIDIAHelper.exe" %*
exit /b 0
