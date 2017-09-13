setlocal ENABLEDELAYEDEXPANSION 
@echo on

set EXITCODE=0
pushd %LinkBenchRoot%

call :DotNet

if defined __test_HelloWorld call :HelloWorld
if defined __test_WebAPI call :WebAPI
if defined __test_MusicStore call :MusicStore
if defined __test_MusicStore_R2R call :MusicStore_R2R 
if defined __test_CoreFx call :CoreFx
if defined __test_Roslyn call :Roslyn

popd
exit /b %EXITCODE%

:DotNet
REM We clone different versions of .Net CLI in order to cope with 
REM different runtimes that the benchmarks target, and certain limitations 
REM in the ILLink/CLI integration and packaging.
REM
REM .Net => .Net 2.0.0-preview2-005905
REM      This version is used to build most benchmarks.
REM      We use this specific version instead of the latest available from 
REM      the master branch, because the latest CLI generates R2R images for 
REM      system binaries, while ILLink cannot yet. We need pure MSIL images
REM      in the unlinked version in order to be able to do a fair dir-size comparison.
REM .Net1 => This is .Net CLI v 1.1.0
REM      Since Roslyn targets netcoreapp v1, it cannot use the IlLink.Tasks package.
REM      We use the ILLink package to get the linker and run it manually.
REM      Since IlLink.exe from this package only runs on .Net v1

powershell -noprofile -executionPolicy RemoteSigned wget  https://raw.githubusercontent.com/dotnet/cli/master/scripts/obtain/dotnet-install.ps1 -OutFile dotnet-install.ps1
if not exist %__dotnet%  mkdir .Net  && powershell -noprofile -executionPolicy RemoteSigned -file dotnet-install.ps1 -Channel master -InstallDir .Net -version 2.0.0-preview2-005905
if not exist %__dotnet1% mkdir .Net1 && powershell -noprofile -executionPolicy RemoteSigned -file dotnet-install.ps1 -InstallDir .Net1
if not exist %__dotnet% set EXITCODE=1&& echo DotNet not installed
if not exist %__dotnet1% set EXITCODE=1&& echo DotNet.1 not installed
exit /b 

:HelloWorld
mkdir HelloWorld
cd HelloWorld
call %__dotnet% new console
if errorlevel 1 set EXITCODE=1&&echo Setup HelloWorld failed
cd ..
exit /b 

:WebAPI
mkdir WebAPI
cd WebAPI
call %__dotnet% new webapi
if errorlevel 1 set EXITCODE=1&&echo Setup WebAPI failed
cd ..
exit /b

:MusicStore
git clone https://github.com/aspnet/JitBench -b dev
if errorlevel 1 set EXITCODE=1&&echo Setup MusicStore failed
exit /b

:MusicStore_R2R
REM MusicStore_R2R requires a previous MusicStore run.
REM No Additional Setup
exit /b

:CoreFx
git clone http://github.com/dotnet/corefx
if errorlevel 1 set EXITCODE=1&&echo Setup CoreFX failed
exit /b

:Roslyn
git clone https://github.com/dotnet/roslyn.git
if errorlevel 1 set EXITCODE=1&&echo Setup Roslyn failed
exit /b
