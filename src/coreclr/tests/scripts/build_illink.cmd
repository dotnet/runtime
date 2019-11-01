@if not defined _echo @echo off
setlocal
set rid=win10-x64

:Arg_Loop
if "%1" == ""          goto ArgsDone
if /i "%1" == "-?"     goto Usage
if /i "%1" == "-h"     goto Usage
if /i "%1" == "-help"  goto Usage
if /i "%1" == "clone"  (set doClone=1&shift&goto Arg_Loop)
if /i "%1" == "x64"    (set rid=win10-x64&shift&goto Arg_Loop)
if /i "%1" == "x86"    (set rid=win10-x86&shift&goto Arg_Loop)

goto Usage

:ArgsDone

if defined doCLone (
    git clone --recursive https://github.com/mono/linker
)

pushd linker\corebuild
call restore.cmd -r %rid%
cd ..\linker
..\corebuild\dotnet.cmd publish -r %rid% -c netcore_Release
popd

echo Built %cd%\linker\linker\bin\netcore_Release\netcoreapp2.0\%rid%\publish\illink.exe

:Done
exit /b 0

:Usage
echo.
echo.Build ILLINKer for CoreCLR testing
echo.
echo.Usage:
echo     build_illink.cmd [clone] [setenv] [arch]
echo.Where:
echo.-? -h -help: view this message.
echo.clone      : Clone the repository https://github.com/mono/linker
echo.arch       : The architecture to build: x64 (default) or x86
echo.
goto Done
