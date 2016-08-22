@rem Licensed to the .NET Foundation under one or more agreements.
@rem The .NET Foundation licenses this file to you under the MIT license.
@rem See the LICENSE file in the project root for more information.

@setlocal
@echo off

set HERE=%CD%
set CORECLR_REPO=%CD%
set CORECLR_OVERLAY=%CORECLR_REPO%\bin\tests\Windows_NT.x64.Release\Tests\Core_Root
set CORECLR_PERF=%CORECLR_REPO%\bin\tests\Windows_NT.x64.Release\Jit\Performance\CodeQuality
set RUNLOG=%HERE%\bin\Logs\perfrun.log

if NOT EXIST %CORECLR_OVERLAY% (
  echo Can't find test overlay directory '%CORECLR_OVERLAY%'
  echo Please build and run Release CoreCLR tests
  exit /B 1
)

:SETUP

@echo --- setting up sandbox

rd /s /q sandbox
mkdir sandbox
pushd sandbox

@rem stage stuff we need

@rem xunit and perf
xcopy /sy %CORECLR_REPO%\packages\Microsoft.DotNet.xunit.performance.runner.Windows\1.0.0-alpha-build0035\tools\* . > %RUNLOG%
xcopy /sy %CORECLR_REPO%\packages\Microsoft.DotNet.xunit.performance.analysis\1.0.0-alpha-build0035\tools\* . > %RUNLOG%
xcopy /sy %CORECLR_REPO%\packages\xunit.console.netcore\1.0.2-prerelease-00101\runtimes\any\native\* . > %RUNLOG%
xcopy /sy %CORECLR_REPO%\bin\tests\Windows_NT.x64.Release\Tests\Core_Root\* . > %RUNLOG%

@rem find and stage the tests

for /R %CORECLR_PERF% %%T in (*.exe) do (
  call :DOIT %%T
)

goto :EOF

:DOIT

set BENCHNAME=%~n1
set PERFOUT=perf-%BENCHNAME%
set XMLOUT=%PERFOUT%-summary.xml

echo --- Running %BENCHNAME%

xcopy /s %1 . > %RUNLOG%

set CORE_ROOT=%HERE%\sandbox

xunit.performance.run.exe %BENCHNAME%.exe -runner xunit.console.netcore.exe -runnerhost corerun.exe -verbose -runid %PERFOUT% > %BENCHNAME%.out

xunit.performance.analysis.exe %PERFOUT%.xml -xml %XMLOUT% > %BENCHNAME%-analysis.out

type %XMLOUT% | findstr "test name"
type %XMLOUT% | findstr Duration
type %XMLOUT% | findstr InstRetired
