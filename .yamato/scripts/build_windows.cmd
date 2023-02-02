@echo off

rem Usage: build_windows.cmd <Architecture> <Configuration>
rem
rem   Architecture is one of x86 or x64 - x64 is the default if not specified
rem   Configuration is one of Debug or Release - Release is the default is not specified
rem
rem To specify Configuration, Architecture must be specified as well.

if "%~1"=="" goto :default_architecture
set architecture=%1
goto :set_configuration
:default_architecture
set architecture=x64

:set_configuration
if "%~2"=="" goto :default_configuration
set configuration=%2
goto :run_build
:default_configuration
set configuration=Release

:run_build

echo *****************************
echo Unity: Starting CoreCLR build
echo   Platform:      Windows
echo   Architecture:  %architecture%
echo   Configuration: %configuration%
echo *****************************

echo.
echo ******************************
echo Unity: Building embedding host
echo ******************************
echo.
call dotnet build unity\managed.sln -c %configuration% || goto :error

echo.
echo ***********************
echo Unity: Building Null GC
echo ***********************
echo.
if "%architecture%"=="x86" (set cmake_architecture=Win32) else (set cmake_architecture=x64)
cd unity\unitygc || goto :error
cmake . -A %cmake_architecture% || goto :error
cmake --build . --config %configuration% || goto :error
cd ../../ || goto :error

echo.
echo *******************************
echo Unity: Building CoreCLR runtime
echo *******************************
echo.
call build.cmd -subset clr+libs -a %architecture% -c %configuration% -ci || goto :error

echo.
echo ******************************
echo Unity: Copying built artifacts
echo ******************************
echo.
copy unity\unitygc\%configuration%\unitygc.dll artifacts\bin\microsoft.netcore.app.runtime.win-%architecture%\%configuration%\runtimes\win-%architecture%\native || goto :error
copy unity\unity-embed-host\bin\%configuration%\net7.0\unity-embed-host.dll artifacts\bin\microsoft.netcore.app.runtime.win-%architecture%\%configuration%\runtimes\win-%architecture%\lib\net7.0 || goto :error
copy unity\unity-embed-host\bin\%configuration%\net7.0\unity-embed-host.pdb artifacts\bin\microsoft.netcore.app.runtime.win-%architecture%\%configuration%\runtimes\win-%architecture%\lib\net7.0 || goto :error

rem Every thing succeeded - jump to the end of the file and return a 0 exit code
echo.
echo *********************************
echo Unity: Built CoreCLR successfully
echo *********************************
goto :EOF

rem If we get here, one of the commands above failed
:error
echo.
echo ******************************
echo Unity: Failed to build CoreCLR
echo ******************************
exit /b %errorlevel%
