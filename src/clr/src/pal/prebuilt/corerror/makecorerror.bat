@if "%_echo%"=="" echo off
REM Licensed to the .NET Foundation under one or more agreements.
REM The .NET Foundation licenses this file to you under the MIT license.
REM See the LICENSE file in the project root for more information.
setlocal

if "%_NTROOT%" == "" goto LUsage

set MANAGED_TOOLS_PATH=%MANAGED_TOOLS_ROOT%\%MANAGED_TOOLS_VERSION%
set CORERROR_PATH=%_NTROOT%\ndp\clr\src\inc

%MANAGED_TOOLS_PATH%\genheaders.exe %CORERROR_PATH%\corerror.xml ..\inc\corerror.h mscorurt.rc

goto LExit

:LUsage

echo.
echo makecorerror.bat
echo.
echo    Builds corerror.h for PALRT
echo.
echo    Should be run inside razzle environment, depends on %%_NTROOT%% environment variable.
echo.

:LExit

