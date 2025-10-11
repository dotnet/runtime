@echo off
REM Script to run FileStream buffering optimization benchmarks

setlocal

REM Change to the directory containing this script
cd /d "%~dp0"

REM Get the repository root (5 levels up from this script)
set REPO_ROOT=%~dp0..\..\..\..\..
pushd %REPO_ROOT%
set REPO_ROOT=%CD%
popd

REM Set up the dotnet path
set PATH=%REPO_ROOT%\.dotnet;%PATH%

echo Running FileStream Buffering Optimization Benchmark...
echo Repository root: %REPO_ROOT%
dotnet --version
echo.

REM Build in Release mode
echo Building benchmark...
dotnet build -c Release

if errorlevel 1 (
    echo Build failed!
    exit /b 1
)

echo.
echo Running benchmark (this may take several minutes)...
echo.

REM Run the benchmark
dotnet run -c Release --no-build

echo.
echo Benchmark complete!

endlocal
