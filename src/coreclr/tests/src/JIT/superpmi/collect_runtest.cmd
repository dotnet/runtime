@echo off
setlocal

goto :start
:usage
echo Set up for individual test execution, with SuperPMI collection.
echo.
echo Usage: collect_runtest.cmd ^<path to runtest.cmd^>
echo.
echo We can't set the SuperPMI collection variables before runtest.cmd executes,
echo because it runs other managed apps, including desktop apps, that are also
echo affected by these variables. For SuperPMI collection, we only want the test
echo itself to be collected. So, create a temporary script that just sets the
echo SuperPMI collection variables, and pass that as the "TestEnv" argument to
echo runtest.cmd, which uses it to set the environment for running an individual
echo test.
echo.
echo Example usage of this script:
echo    %CORE_ROOT%\corerun superpmicollect.exe -mch alltests.mch -run collect_runtest.cmd c:\repos\coreclr\tests\runtest.cmd
goto :eof

:start
if "%1"=="" echo ERROR: missing argument.&goto usage

set runtestscript=%1

set testenvfile=%TEMP%\superpmitestenv_%RANDOM%.cmd

echo set SuperPMIShimLogPath=%SuperPMIShimLogPath%> %testenvfile%
echo set SuperPMIShimPath=%SuperPMIShimPath%>> %testenvfile%
echo set COMPlus_AltJit=%COMPlus_AltJit%>> %testenvfile%
echo set COMPlus_AltJitName=%COMPlus_AltJitName%>> %testenvfile%

set SuperPMIShimLogPath=
set SuperPMIShimPath=
set COMPlus_AltJit=
set COMPlus_AltJitName=

call %runtestscript% testEnv %testenvfile%

del /f /q %testenvfile%
