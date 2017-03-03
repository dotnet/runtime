@rem Licensed to the .NET Foundation under one or more agreements.
@rem The .NET Foundation licenses this file to you under the MIT license.
@rem See the LICENSE file in the project root for more information.

@setlocal
@echo off
Setlocal EnableDelayedExpansion

rem Set defaults for the file extension, architecture and configuration
set CORECLR_REPO=%CD%
set TEST_FILE_EXT=exe
set TEST_ARCH=x64
set TEST_ARCHITECTURE=x64
set TEST_CONFIG=Release

goto :ARGLOOP


:SETUP

IF /I [%TEST_ARCHITECTURE%] == [x86jit32] (
    set TEST_ARCH=x86
) ELSE (
    set TEST_ARCH=%TEST_ARCHITECTURE%
)

set CORECLR_OVERLAY=%CORECLR_REPO%\bin\tests\Windows_NT.%TEST_ARCH%.%TEST_CONFIG%\Tests\Core_Root
set RUNLOG=%CORECLR_REPO%\bin\Logs\perfrun.log

if NOT EXIST %CORECLR_OVERLAY% (
  echo Can't find test overlay directory '%CORECLR_OVERLAY%'
  echo Please build and run Release CoreCLR tests
  exit /B 1
)

@echo --- setting up sandbox

if exist sandbox rd /s /q sandbox
if exist sandbox echo ERROR: Failed to remove the sandbox folder& exit /b 1
if not exist sandbox mkdir sandbox
if not exist sandbox echo ERROR: Failed to create the sandbox folder& exit /b 1
pushd sandbox

@rem stage stuff we need

@rem xunit and perf
"%CORECLR_REPO%\Tools\dotnetcli\dotnet.exe" restore "%CORECLR_REPO%\tests\src\Common\PerfHarness\project.json"
"%CORECLR_REPO%\Tools\dotnetcli\dotnet.exe" publish "%CORECLR_REPO%\tests\src\Common\PerfHarness\project.json" -c Release -o %CORECLR_REPO%\sandbox
xcopy /sy %CORECLR_REPO%\packages\Microsoft.Diagnostics.Tracing.TraceEvent\1.0.0-alpha-experimental\lib\native\* . >> %RUNLOG%
xcopy /sy %CORECLR_REPO%\bin\tests\Windows_NT.%TEST_ARCH%.%TEST_CONFIG%\Tests\Core_Root\* . >> %RUNLOG%

@rem find and stage the tests
for /R %CORECLR_PERF% %%T in (*.%TEST_FILE_EXT%) do (
  call :DOIT %%T
)

@rem optionally upload results to benchview
if not [%BENCHVIEW_PATH%] == [] (
  py "%BENCHVIEW_PATH%\submission.py" measurement.json ^
                                    --build ..\build.json ^
                                    --machine-data ..\machinedata.json ^
                                    --metadata ..\submission-metadata.json ^
                                    --group "CoreCLR" ^
                                    --type "%RUN_TYPE%" ^
                                    --config-name "%TEST_CONFIG%" ^
                                    --config Configuration "%TEST_CONFIG%" ^
                                    --config OS "Windows_NT" ^
                                    --arch "%TEST_ARCHITECTURE%" ^
                                    --machinepool "PerfSnake"
  py "%BENCHVIEW_PATH%\upload.py" submission.json --container coreclr
)

goto :EOF

:DOIT
set BENCHNAME=%~n1
set BENCHDIR=%~p1
set PERFOUT=perf-%BENCHNAME%
set XMLOUT=%PERFOUT%-summary.xml

echo --- Running %BENCHNAME%

@rem copy benchmark and any input files

xcopy /s %1 . >> %RUNLOG%
xcopy /s %BENCHDIR%*.txt . >> %RUNLOG%

set CORE_ROOT=%CORECLR_REPO%\sandbox

@rem setup additional environment variables
if DEFINED TEST_ENV (
    if EXIST !TEST_ENV! (
        call %TEST_ENV%
    )
)

corerun.exe PerfHarness.dll %WORKSPACE%\sandbox\%BENCHNAME%.%TEST_FILE_EXT% --perf:runid Perf > %BENCHNAME%.out

@rem optionally generate results for benchview
if not [%BENCHVIEW_PATH%] == [] (
  py "%BENCHVIEW_PATH%\measurement.py" xunit "Perf-%BENCHNAME%.xml" --better desc --drop-first-value --append
  REM Save off the results to the root directory for recovery later in Jenkins
  xcopy Perf-%BENCHNAME%*.xml %CORECLR_REPO%\
  xcopy Perf-%BENCHNAME%*.etl %CORECLR_REPO%\
) else (
  type %XMLOUT% | findstr "test name"
  type %XMLOUT% | findstr Duration
  type %XMLOUT% | findstr InstRetired
)

goto :EOF

:ARGLOOP
IF /I [%1] == [-testBinLoc] (
set CORECLR_PERF=%CORECLR_REPO%\%2
shift
shift
goto :ARGLOOP
)
IF /I [%1] == [-runtype] (
set RUN_TYPE=%2
shift
shift
goto :ARGLOOP
)
IF /I [%1] == [-library] (
set TEST_FILE_EXT=dll
shift
goto :ARGLOOP
)
IF /I [%1] == [-uploadtobenchview] (
set BENCHVIEW_PATH=%2
shift
shift
goto :ARGLOOP
)
IF /I [%1] == [-arch] (
set TEST_ARCHITECTURE=%2
shift
shift
goto :ARGLOOP
)
IF /I [%1] == [-testEnv] (
set TEST_ENV=%2
shift
shift
goto :ARGLOOP
)
IF /I [%1] == [-configuration] (
set TEST_CONFIG=%2
shift
shift
goto :ARGLOOP
)
if /I [%1] == [-?] (
goto :USAGE
)
if /I [%1] == [-help] (
goto :USAGE
)

if [%CORECLR_PERF%] == [] (
goto :USAGE
)

goto :SETUP

:USAGE
echo run-xunit-perf.cmd -testBinLoc ^<path_to_tests^> [-library] [-arch] ^<x86^|x64^> [-configuration] ^<Release^|Debug^> [-uploadToBenchview] ^<path_to_benchview_tools^> [-runtype] ^<rolling^|private^>

echo For the path to the tests you can pass a parent directory and the script will grovel for
echo all tests in subdirectories and run them.
echo The library flag denotes whether the tests are build as libraries (.dll) or an executable (.exe)
echo Architecture defaults to x64 and configuration defaults to release.
echo -uploadtoBenchview is used to specify a path to the Benchview tooling and when this flag is
echo set we will upload the results of the tests to the coreclr container in benchviewupload.
echo Runtype sets the runtype that we upload to Benchview, rolling for regular runs, and private for
echo PRs.

goto :EOF
