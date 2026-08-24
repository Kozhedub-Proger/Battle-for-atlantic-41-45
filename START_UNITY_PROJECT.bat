@echo off
setlocal EnableExtensions
chcp 65001 >nul
cd /d "%~dp0"

echo ================================================
echo   Battle for Atlantic 41-45 - Unity launcher
echo ================================================
echo.

if exist ".git" (
    where git >nul 2>nul
    if %errorlevel%==0 (
        echo [UPDATE] Pulling latest project changes...
        git pull --ff-only
        if errorlevel 1 (
            echo [WARNING] git pull failed. Project will still be launched.
        ) else (
            echo [OK] Project updated.
        )
        echo.
    ) else (
        echo [WARNING] Git is not available in PATH. Skipping update.
        echo.
    )
) else (
    echo [INFO] This folder is not a Git clone ^(no .git folder^).
    echo [INFO] Automatic pull is unavailable for ZIP copies.
    echo [INFO] Download a fresh ZIP or clone the repository once to enable auto-update.
    echo.
)

if not exist "Assets" mkdir "Assets"
if not exist "Assets\Scenes" mkdir "Assets\Scenes"
if not exist "Assets\Ships" mkdir "Assets\Ships"
if not exist "Assets\Water" mkdir "Assets\Water"
if not exist "Assets\Scripts" mkdir "Assets\Scripts"
if not exist "Assets\Materials" mkdir "Assets\Materials"
if not exist "Assets\Prefabs" mkdir "Assets\Prefabs"

set "UNITY_EXE=C:\Program Files\Unity\Hub\Editor\6000.3.22f1\Editor\Unity.exe"

if exist "%UNITY_EXE%" goto launch

for /d %%D in ("C:\Program Files\Unity\Hub\Editor\6000.3.*") do (
    if exist "%%~fD\Editor\Unity.exe" (
        set "UNITY_EXE=%%~fD\Editor\Unity.exe"
        goto launch
    )
)

echo [ERROR] Unity 6.3 LTS not found.
echo Install Unity 6000.3.22f1 in Unity Hub and run this file again.
echo.
pause
exit /b 1

:launch
echo Unity: %UNITY_EXE%
echo Project: %CD%
echo.
start "Battle for Atlantic - Unity" "%UNITY_EXE%" -projectPath "%CD%"
exit /b 0
