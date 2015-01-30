@if "%_echo%"=="" echo off
REM ==++==
REM 
REM Copyright (c) Microsoft Corporation.  All rights reserved.
REM 
REM ==--==
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

