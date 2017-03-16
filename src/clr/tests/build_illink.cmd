if not defined _echo @echo off
setlocal
set rid=win10-x64

:Arg_Loop
if "%1" == ""          goto ArgsDone
if /i "%1" == "-?"     goto Usage
if /i "%1" == "-h"     goto Usage
if /i "%1" == "-help"  goto Usage
if /i "%1" == "clone"  (set doClone=1&shift&goto Arg_Loop)
if /i "%1" == "setenv" (set setEnv=1&shift&goto Arg_Loop)
if /i "%1" == "x64"    (set rid=win10-x64&shift&goto Arg_Loop)
if /i "%1" == "x86"    (set rid=win10-x86&shift&goto Arg_Loop)

goto Usage

:ArgsDone

if defined doCLone (
    git clone --recursive https://github.com/mono/linker
)

pushd linker\corebuild
call restore.cmd -RuntimeIdentifier=%rid%
set DoNotEmbedDescriptors=1
cd ..\linker
..\corebuild\Tools\dotnetcli\dotnet.exe publish -r %rid% -c netcore_Relase
popd

if not defined setEnv goto Done
echo set ILLINK=%cd%\linker\linker\bin\netcore_Relase\netcoreapp2.0\%rid%\publish\illink.exe
endlocal && set ILLINK=%cd%\linker\linker\bin\netcore_Relase\netcoreapp2.0\%rid%\publish\illink.exe

:Done
exit /b 0

:Usage
echo.
echo.Build the ILLINK for CoreCLR testing
echo.
echo.Usage:
echo     build_illink.cmd [clone] [setenv] runtime-ID
echo.Where:
echo.-? -h -help: view this message.
echo.clone: Clone the repository https://github.com/mono/linker
echo.set: set ILLINK to the path to illink.exe
echo.runtime-ID: The os-architecture configuration to build: x64 (default) or x86
goto Done
