@echo off
setlocal

REM Do a .NET Core SuperPMI collection across all tests in the coreclr repo.

REM Set the repo root.
set _root=d:\src\coreclr

REM Set the build flavor.
set _flavor=windows.x64.Debug

REM Everything else in this script is parameterized using the above two variables.

if not exist %_root% echo Error: %_root% not found&goto :eof

REM Where to put the resulting MCH file?
set _mch=%_root%\artifacts\tests\alltests_win.mch

set _testbuild=%_root%\artifacts\tests\%_flavor%
if not exist %_testbuild% echo Error: %_testbuild% not found&goto :eof
if not exist %_testbuild%\JIT\superpmi\superpmicollect\superpmicollect.exe echo Error: superpmicollect.exe not found&goto :eof

set _collect_script=%_root%\tests\src\JIT\superpmi\collect_runtest.cmd
if not exist %_collect_script% echo Error: %_collect_script% not found&goto :eof

if not exist %_root%\tests\runtest.cmd echo Error: %_root%\tests\runtest.cmd not found&goto :eof

if not defined CORE_ROOT echo ERROR: set CORE_ROOT before running this script&goto :eof
if not exist %CORE_ROOT% echo Error: CORE_ROOT (%CORE_ROOT%) not found&goto :eof
if not exist %CORE_ROOT%\coreclr.dll echo Error: coreclr.dll (%CORE_ROOT%\coreclr.dll) not found&goto :eof
if not exist %CORE_ROOT%\corerun.exe echo Error: corerun.exe (%CORE_ROOT%\corerun.exe) not found&goto :eof

REM Do the collection!
pushd %_testbuild%
%core_root%\corerun.exe %_testbuild%\JIT\superpmi\superpmicollect\superpmicollect.exe -mch %_mch% -run %_collect_script% %_root%\tests\runtest.cmd
popd
