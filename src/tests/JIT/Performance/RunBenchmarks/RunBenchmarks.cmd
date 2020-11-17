@setlocal
@echo off 
@rem ***************************************************************************
@rem    RunBenchmarks.cmd
@rem    
@rem    This is a sample script for how to run benchmarks on Windows.
@rem    
@rem    It requires the user to set CORECLR_ROOT to the root directory
@rem    of the enlistment(repo).  It also requires that CoreCLR has been built, 
@rem    and that all CoreCLR tests have been built.
@rem    
@rem    The preformance harness "RunBenchmarks.exe" is built as a test case
@rem    as are all the performance tests it runs.
@rem    
@rem    For the ByteMark tests, it must copy the command scripts to the 
@rem    binary directory for the tests.
@rem
@rem    By default, the performance harness is run on top of CoreCLR.  There
@rem    is a commented out section that can be used to run on top of DesktopCLR.
@rem    
@rem    A standard benchmark run is done with one warmup run, and five iterations
@rem    of the benchmark.
@rem
@rem ***************************************************************************

set ARCH=%1
set BUILD=%2

if /I "%ARCH%" == "" set ARCH=x64
if /I "%BUILD%" == "" set BUILD=Release

rem *** set this appropriately for enlistment you are running benchmarks in
if /I NOT "%CORECLR_ROOT%" == "" goto over
@echo "You must set CORECLR_ROOT to be the root of your coreclr repo (e.g. \git\repos\coreclr)"
@goto done
:over

set BENCHMARK_ROOT_DIR=%CORECLR_ROOT%\artifacts\tests\windows.%ARCH%.%BUILD%\JIT\Performance\CodeQuality
set BENCHMARK_SRC_DIR=%CORECLR_ROOT%\tests\src\JIT\Performance\RunBenchmarks
set BENCHMARK_HOST=CoreRun.exe %CORECLR_ROOT%\artifacts\tests\windows.%ARCH%.%BUILD%\JIT\Performance\RunBenchmarks\RunBenchmarks\RunBenchmarks.exe
set BENCHMARK_RUNNER=-runner CoreRun.exe

rem *** used for desktop hosted run
rem set BENCHMARK_HOST=%BENCHMARK_SRC_DIR%\artifacts\%BUILD%\desktop\RunBenchmarks.exe

rem *** need to copy command files for Bytemark
xcopy /y /e /s %CORECLR_ROOT%\tests\src\JIT\Performance\CodeQuality\Bytemark\commands %BENCHMARK_ROOT_DIR%\Bytemark\Bytemark\commands

rem *** if you have problems with pop-ups, enable DHandler.exe
rem start DHandler.exe

set BENCHMARK_CONTROLS=-run -v -w -n 5
set BENCHMARK_SET=-f %BENCHMARK_SRC_DIR%\coreclr_benchmarks.xml -notags broken
set BENCHMARK_OUTPUT=-csvfile %BENCHMARK_SRC_DIR%\coreclr_benchmarks.csv
set BENCHMARK_SWITCHES=%BENCHMARK_CONTROLS% -r %BENCHMARK_ROOT_DIR%

%BENCHMARK_HOST% %BENCHMARK_RUNNER% %BENCHMARK_SET% %BENCHMARK_OUTPUT% %BENCHMARK_SWITCHES%

rem *** if you have problems with pop-ups, enable DHandler.exe
rem taskkill /im DHandler.exe

:done
@endlocal
