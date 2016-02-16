@setlocal
@echo off 

set ARCH=%1
set BUILD=%2

if /I "%ARCH%" == "" set ARCH=x64
if /I "%BUILD%" == "" set BUILD=Release

rem *** set this appropriately for enlistment you are running benchmarks in
if /I NOT "%CORECLR_ROOT%" == "" goto over
@echo "You must set CORECLR_ROOT to be the root of your coreclr repo (e.g. \git\repos\coreclr)"
@goto done
:over

set BENCHMARK_ROOT_DIR=%CORECLR_ROOT%\bin\tests\Windows_NT.%ARCH%.%BUILD%\JIT\Performance\CodeQuality
set BENCHMARK_SRC_DIR=%CORECLR_ROOT%\tests\src\JIT\Performance\RunBenchmarks
set BENCHMARK_HOST=CoreRun.exe %CORECLR_ROOT%\bin\tests\Windows_NT.%ARCH%.%BUILD%\JIT\Performance\RunBenchmarks\RunBenchmarks\RunBenchmarks.exe
set BENCHMARK_RUNNER=-runner CoreRun.exe

rem *** used for desktop hosted run
rem set BENCHMARK_HOST=%BENCHMARK_SRC_DIR%\bin\%BUILD%\desktop\RunBenchmarks.exe

rem *** need to copy command files for Bytemark
xcopy /y /e /s %CORECLR_ROOT%\tests\src\JIT\Performance\CodeQuality\Bytemark\commands %BENCHMARK_ROOT_DIR%\Bytemark\Bytemark\commands

start DHandler.exe

set BENCHMARK_CONTROLS=-run -v -w -n 5
set BENCHMARK_SET=-f %BENCHMARK_SRC_DIR%\coreclr_benchmarks.xml -notags broken
set BENCHMARK_SWITCHES=%BENCHMARK_CONTROLS% -r %BENCHMARK_ROOT_DIR%

%BENCHMARK_HOST% %BENCHMARK_RUNNER% %BENCHMARK_SET% %BENCHMARK_SWITCHES%

taskkill /im DHandler.exe

:done
@endlocal
