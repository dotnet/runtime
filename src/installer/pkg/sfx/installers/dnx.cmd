:: Licensed to the .NET Foundation under one or more agreements.
:: The .NET Foundation licenses this file to you under the MIT license.

@echo off
setlocal enableextensions

set "DOTNET=%~dp0dotnet.exe"

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
