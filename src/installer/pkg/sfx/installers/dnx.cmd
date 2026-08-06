:: Licensed to the .NET Foundation under one or more agreements.
:: The .NET Foundation licenses this file to you under the MIT license.

@echo off
setlocal enableextensions

set "DOTNET=%~dp0dotnet.exe"

set "DNX_PATH=%~1"
if "%DNX_PATH:\=%"=="%DNX_PATH%" if "%DNX_PATH:/=%"=="%DNX_PATH%" goto run_tool
if not exist "%~f1" goto run_tool
if exist "%~f1\*" goto run_tool
if /I "%~x1"==".cs" goto run_file

:check_shebang
for /f "delims=" %%i in ('findstr /n "^" "%~f1" 2^>nul ^| findstr /b /l /c:"1:#!"') do goto run_file

:run_tool
set "SDK_VERSION="
for /f "tokens=1" %%i in ('"%DOTNET%" --list-sdks') do (
    set "SDK_VERSION=%%i"
)

if not defined SDK_VERSION (
    echo Error: dnx requires a .NET SDK to be installed, but none was found. 1>&2
    exit /b 1
)

set "SDK_PATH=%~dp0sdk\%SDK_VERSION%\dotnet.dll"

"%DOTNET%" exec "%SDK_PATH%" dnx %*

endlocal & exit /b %ERRORLEVEL%

:run_file
set "DNX_WORKING_DIRECTORY=%CD%"
pushd "%~dp1" || exit /b 1

"%DOTNET%" run --file-mode --working-directory "%DNX_WORKING_DIRECTORY%" -- %*
set "EXIT_CODE=%ERRORLEVEL%"

popd
endlocal & exit /b %EXIT_CODE%
