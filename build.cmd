@echo off
setlocal EnableDelayedExpansion

REM Copyright (c) .NET Foundation and contributors. All rights reserved.
REM Licensed under the MIT license. See LICENSE file in the project root for full license information.

:Arg_Loop
:: Since the native build requires some configuration information before msbuild is called, we have to do some manual args parsing
if [%1] == [] goto :InvokeBuild
if /i [%1] == [-TargetArch]  ( 
    
    if /i [%2] == [arm64] (
        set __BuildArch=x86_amd64
    )

    if /i [%2] == [arm] (
        set __BuildArch=x86_arm
    )

    if not defined VS140COMNTOOLS (
        echo Error: Visual Studio 2015 required  
        echo        Please see https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/developer-guide.md for build instructions.
        exit /b 1
    )

    call "%VS140COMNTOOLS%\..\..\VC\vcvarsall.bat" !__BuildArch!
    goto :InvokeBuild
)

shift
goto :Arg_Loop

:InvokeBuild
powershell -NoProfile -NoLogo -Command "%~dp0build_projects\dotnet-host-build\build.ps1 %*; exit $LastExitCode;"
if %errorlevel% neq 0 exit /b %errorlevel%
