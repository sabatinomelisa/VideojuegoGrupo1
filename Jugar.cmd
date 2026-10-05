@echo off
setlocal
set "DOTNET_ROOT=%~dp0Juego\runtime"
set "DOTNET_ROOT_X64=%~dp0Juego\runtime"
set "DOTNET_MULTILEVEL_LOOKUP=0"
if not exist "%~dp0Juego\Kepler7.exe" (
 echo Extrae todo el ZIP antes de jugar.
 pause
 exit /b 1
)
start "" "%~dp0Juego\Kepler7.exe"
