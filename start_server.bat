@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"

set MAX_CRASHES=5
set CRASH_COUNT=0

:loop
del restart.flag 2>nul
del shutdown.flag 2>nul

echo Starting Railroader dedicated server...
Railroader.exe

if exist shutdown.flag (
    echo Server stopped cleanly.
    exit /b 0
)

if exist restart.flag (
    echo Restart requested by server.
    set CRASH_COUNT=0
    timeout /t 5 >nul
    goto loop
)

set /a CRASH_COUNT+=1
echo Server exited unexpectedly. Crash count: !CRASH_COUNT!/%MAX_CRASHES%

if !CRASH_COUNT! GEQ %MAX_CRASHES% (
    echo Server appears to be stuck in a crash loop. Not restarting.
    exit /b 1
)

timeout /t 15 >nul
goto loop
