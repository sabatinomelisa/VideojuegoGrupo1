@echo off
setlocal
cd /d "%~dp0"
where dotnet >nul 2>nul
if errorlevel 1 (
  echo Instala el SDK de .NET 8 para compilar.
  pause
  exit /b 1
)
dotnet run --project Kepler7.Checks -c Release
if errorlevel 1 goto fallo
dotnet publish Kepler7.Game -c Release -r win-x64 --self-contained true -o Publicado
if errorlevel 1 goto fallo
echo Publicacion lista. Abri Publicado\Kepler7.exe para jugar.
pause
exit /b 0
:fallo
echo No se pudo completar la publicacion. Revisa el error anterior.
pause
exit /b 1
