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
REM .Net2 => .Net 2.0.0-preview3-006923
REM      Roslyn needs to build using the dotnet SDK 2.0, because it needs the 
REM      latest C#7 compiler. However, this means that the directory size comparison 
REM      is off, because the system-binaries are R2R in the unlinked directory, 
REM      but MSIL in the linked directory. 
REM      TODO: Get the correct version of Crossgen, and manually R2R the system
REM      binaries in the linked directory.

powershell -noprofile -executionPolicy RemoteSigned wget  https://raw.githubusercontent.com/dotnet/cli/master/scripts/obtain/dotnet-install.ps1 -OutFile dotnet-install.ps1
if not exist %__dotnet%  mkdir .Net && powershell -noprofile -executionPolicy RemoteSigned -file dotnet-install.ps1 -Channel master -InstallDir .Net -version 2.0.0-preview2-005905
if not exist %__dotnet2% mkdir .Net2 && powershell -noprofile -executionPolicy RemoteSigned -file dotnet-install.ps1 -Channel master -InstallDir .Net2 -version 2.0.0-preview3-006923
if not exist %__dotnet% set EXITCODE=1&& echo DotNet not installed
if not exist %__dotnet2% set EXITCODE=1&& echo DotNet2 not installed
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
