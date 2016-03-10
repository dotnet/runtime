@if not defined __echo @echo off
setlocal EnableDelayedExpansion EnableExtensions

set __ThisScriptShort=%0
set __ThisScriptFull="%~f0"
set __ThisScriptPath="%~dp0"

:: Note that the msbuild project files (specifically, dir.proj) will use the following variables, if set:
::      __BuildArch         -- default: x64
::      __BuildType         -- default: Debug
::      __BuildOS           -- default: Windows_NT
::      __ProjectDir        -- default: directory of the dir.props file
::      __SourceDir         -- default: %__ProjectDir%\src\
::      __PackagesDir       -- default: %__ProjectDir%\packages\
::      __RootBinDir        -- default: %__ProjectDir%\bin\
::      __BinDir            -- default: %__RootBinDir%\%__BuildOS%.%__BuildArch.%__BuildType%\
::      __IntermediatesDir
::      __PackagesBinDir    -- default: %__BinDir%\.nuget
::      __TestWorkingDir    -- default: %__RootBinDir%\tests\%__BuildOS%.%__BuildArch.%__BuildType%\
::
:: Thus, these variables are not simply internal to this script!
::
:: The UseRoslynCompiler variable is used by src\mscorlib\GenerateCompilerResponseFile.targets.

:: Set the default arguments for build
set __BuildArch=x64
set __BuildType=Debug
set __BuildOS=Windows_NT

:: Default to highest Visual Studio version available
set __VSVersion=vs2015

if defined VS140COMNTOOLS set __VSVersion=vs2015

:: Define a prefix for most output progress messages that come from this script. That makes
:: it easier to see where these are coming from. Note that there is a trailing space here.
set __MsgPrefix=BUILD: 

:: Set the various build properties here so that CMake and MSBuild can pick them up
set "__ProjectDir=%~dp0"
:: remove trailing slash
if %__ProjectDir:~-1%==\ set "__ProjectDir=%__ProjectDir:~0,-1%"
set "__ProjectFilesDir=%__ProjectDir%"
set "__SourceDir=%__ProjectDir%\src"
set "__PackagesDir=%__ProjectDir%\packages"
set "__RootBinDir=%__ProjectDir%\bin"
set "__LogsDir=%__RootBinDir%\Logs"

set __CleanBuild=
set __MscorlibOnly=
set __ConfigureOnly=
set __SkipConfigure=
set __SkipMscorlibBuild=
set __SkipNativeBuild=
set __SkipTestBuild=
set __DoCrossgen=
set __BuildSequential=
set __msbuildCleanBuildArgs=
set __msbuildExtraArgs=
set __SignTypeReal=

set __BuildAll=

set __BuildArchX64=0
set __BuildArchX86=0
set __BuildArchArm=0
set __BuildArchArm64=0

set __BuildTypeDebug=0
set __BuildTypeChecked=0
set __BuildTypeRelease=0

REM __PassThroughArgs is a set of things that will be passed through to nested calls to build.cmd
REM when using "all".
set __PassThroughArgs=

:Arg_Loop
if "%1" == "" goto ArgsDone

if /i "%1" == "/?"    goto Usage
if /i "%1" == "-?"    goto Usage
if /i "%1" == "/h"    goto Usage
if /i "%1" == "-h"    goto Usage
if /i "%1" == "/help" goto Usage
if /i "%1" == "-help" goto Usage

if /i "%1" == "all"                 (set __BuildAll=1&shift&goto Arg_Loop)

if /i "%1" == "x64"                 (set __BuildArchX64=1&shift&goto Arg_Loop)
if /i "%1" == "x86"                 (set __BuildArchX86=1&shift&goto Arg_Loop)
if /i "%1" == "arm"                 (set __BuildArchArm=1&shift&goto Arg_Loop)
if /i "%1" == "arm64"               (set __BuildArchArm64=1&shift&goto Arg_Loop)

if /i "%1" == "debug"               (set __BuildTypeDebug=1&shift&goto Arg_Loop)
if /i "%1" == "checked"             (set __BuildTypeChecked=1&shift&goto Arg_Loop)
if /i "%1" == "release"             (set __BuildTypeRelease=1&shift&goto Arg_Loop)

REM All arguments after this point will be passed through directly to build.cmd on nested invocations
REM using the "all" argument, and must be added to the __PassThroughArgs variable.
set __PassThroughArgs=%__PassThroughArgs% %1

if /i "%1" == "clean"               (set __CleanBuild=1&shift&goto Arg_Loop)

if /i "%1" == "freebsdmscorlib"     (set __MscorlibOnly=1&set __BuildOS=FreeBSD&shift&goto Arg_Loop)
if /i "%1" == "linuxmscorlib"       (set __MscorlibOnly=1&set __BuildOS=Linux&shift&goto Arg_Loop)
if /i "%1" == "netbsdmscorlib"      (set __MscorlibOnly=1&set __BuildOS=NetBSD&shift&goto Arg_Loop)
if /i "%1" == "osxmscorlib"         (set __MscorlibOnly=1&set __BuildOS=OSX&shift&goto Arg_Loop)
if /i "%1" == "windowsmscorlib"     (set __MscorlibOnly=1&set __BuildOS=Windows_NT&shift&goto Arg_Loop)

if /i "%1" == "vs2015"              (set __VSVersion=%1&shift&goto Arg_Loop)
if /i "%1" == "configureonly"       (set __ConfigureOnly=1&set __SkipMscorlibBuild=1&set __SkipTestBuild=1&shift&goto Arg_Loop)
if /i "%1" == "skipconfigure"       (set __SkipConfigure=1&shift&goto Arg_Loop)
if /i "%1" == "skipmscorlib"        (set __SkipMscorlibBuild=1&shift&goto Arg_Loop)
if /i "%1" == "skipnative"          (set __SkipNativeBuild=1&shift&goto Arg_Loop)
if /i "%1" == "skiptests"           (set __SkipTestBuild=1&shift&goto Arg_Loop)
if /i "%1" == "docrossgen"          (set __DoCrossgen=1&shift&goto Arg_Loop)
if /i "%1" == "sequential"          (set __BuildSequential=1&shift&goto Arg_Loop)
if /i "%1" == "disableoss"          (set __SignTypeReal="/p:SignType=real"&shift&goto Arg_Loop)
if /i "%1" == "priority"            (set __TestPriority=%2&set __PassThroughArgs=%__PassThroughArgs% %2&shift&shift&goto Arg_Loop)

@REM For backwards compatibility, continue accepting "skiptestbuild", which was the original name of the option.
if /i "%1" == "skiptestbuild"       (set __SkipTestBuild=1&shift&goto Arg_Loop)

@REM It was initially /toolset_dir. Not sure why, since it doesn't match the other usage.
if /i "%1" == "/toolset_dir"        (set __ToolsetDir=%2&set __PassThroughArgs=%__PassThroughArgs% %2&shift&shift&goto Arg_Loop)
if /i "%1" == "toolset_dir"         (set __ToolsetDir=%2&set __PassThroughArgs=%__PassThroughArgs% %2&shift&shift&goto Arg_Loop)

if /i not "%1" == "msbuildargs" goto SkipMsbuildArgs
:: All the rest of the args will be collected and passed directly to msbuild.
:CollectMsbuildArgs
shift
if "%1"=="" goto ArgsDone
set __msbuildExtraArgs=%__msbuildExtraArgs% %1
set __PassThroughArgs=%__PassThroughArgs% %1
goto CollectMsbuildArgs
:SkipMsbuildArgs

echo Invalid command-line argument: %1
goto Usage

:ArgsDone

if defined __ConfigureOnly if defined __SkipConfigure (
    echo "Error: option 'configureonly' is incompatible with 'skipconfigure'"
    goto Usage
)

if defined __SkipMscorlibBuild if defined __MscorlibOnly (
    echo Error: option 'skipmscorlib' is incompatible with 'freebsdmscorlib', 'linuxmscorlib', 'netbsdmscorlib', 'osxmscorlib' and 'windowsmscorlib'.
    goto Usage
)

if defined __BuildAll goto BuildAll

set /A __TotalSpecifiedBuildArch=__BuildArchX64 + __BuildArchX86 + __BuildArchArm + __BuildArchArm64
if %__TotalSpecifiedBuildArch% GTR 1 (
    echo Error: more than one build architecture specified, but "all" not specified.
    goto Usage
)

if %__BuildArchX64%==1      set __BuildArch=x64
if %__BuildArchX86%==1      set __BuildArch=x86
if %__BuildArchArm%==1      set __BuildArch=arm
if %__BuildArchArm64%==1    set __BuildArch=arm64

set /A __TotalSpecifiedBuildType=__BuildTypeDebug + __BuildTypeChecked + __BuildTypeRelease
if %__TotalSpecifiedBuildType% GTR 1 (
    echo Error: more than one build type specified, but "all" not specified.
    goto Usage
)

if %__BuildTypeDebug%==1    set __BuildType=Debug
if %__BuildTypeChecked%==1  set __BuildType=Checked
if %__BuildTypeRelease%==1  set __BuildType=Release

echo %__MsgPrefix%Commencing CoreCLR Repo build

:: Set the remaining variables based upon the determined build configuration
set "__BinDir=%__RootBinDir%\Product\%__BuildOS%.%__BuildArch%.%__BuildType%"
set "__IntermediatesDir=%__RootBinDir%\obj\%__BuildOS%.%__BuildArch%.%__BuildType%"
set "__PackagesBinDir=%__BinDir%\.nuget"
set "__TestRootDir=%__RootBinDir%\tests"
set "__TestBinDir=%__TestRootDir%\%__BuildOS%.%__BuildArch%.%__BuildType%"
set "__TestIntermediatesDir=%__RootBinDir%\tests\obj\%__BuildOS%.%__BuildArch%.%__BuildType%"

:: Generate path to be set for CMAKE_INSTALL_PREFIX to contain forward slash
set "__CMakeBinDir=%__BinDir%"
set "__CMakeBinDir=%__CMakeBinDir:\=/%"

:: Configure environment if we are doing a clean build.
if not defined __CleanBuild goto SkipCleanBuild
echo %__MsgPrefix%Doing a clean build

:: MSBuild projects would need a rebuild
set __msbuildCleanBuildArgs=/t:rebuild

:: Cleanup the previous output for the selected configuration
if exist "%__BinDir%"               rd /s /q "%__BinDir%"
if exist "%__IntermediatesDir%"     rd /s /q "%__IntermediatesDir%"
if exist "%__TestBinDir%"           rd /s /q "%__TestBinDir%"
if exist "%__TestIntermediatesDir%" rd /s /q "%__TestIntermediatesDir%"
if exist "%__LogsDir%"              del /f /q "%__LogsDir%\*_%__BuildOS%__%__BuildArch%__%__BuildType%.*"

:SkipCleanBuild

if not exist "%__BinDir%"           md "%__BinDir%"
if not exist "%__IntermediatesDir%" md "%__IntermediatesDir%"
if not exist "%__LogsDir%"          md "%__LogsDir%"

:: CMake isn't a requirement when building mscorlib only
if defined __MscorlibOnly goto CheckVS

echo %__MsgPrefix%Checking prerequisites

:: Validate that PowerShell is accessibile.
for %%X in (powershell.exe) do (set __PSDir=%%~$PATH:X)
if not defined __PSDir goto NoPS

:: Eval the output from probe-win1.ps1
for /f "delims=" %%a in ('powershell -NoProfile -ExecutionPolicy RemoteSigned "& ""%__SourceDir%\pal\tools\probe-win.ps1"""') do %%a

:CheckVS

set __VSProductVersion=
if /i "%__VSVersion%" == "vs2015" set __VSProductVersion=140

:: Check presence of VS
if not defined VS%__VSProductVersion%COMNTOOLS goto NoVS

set __VSToolsRoot=!VS%__VSProductVersion%COMNTOOLS!
if %__VSToolsRoot:~-1%==\ set "__VSToolsRoot=%__VSToolsRoot:~0,-1%"

:: Does VS really exist?
if not exist "%__VSToolsRoot%\..\IDE\devenv.exe"      goto NoVS
if not exist "%__VSToolsRoot%\..\..\VC\vcvarsall.bat" goto NoVS
if not exist "%__VSToolsRoot%\VsDevCmd.bat"           goto NoVS

:MSBuild14
set _msbuildexe="%ProgramFiles(x86)%\MSBuild\14.0\Bin\MSBuild.exe"
set UseRoslynCompiler=true
:CheckMSBuild14
if not exist %_msbuildexe% set _msbuildexe="%ProgramFiles%\MSBuild\14.0\Bin\MSBuild.exe"
if not exist %_msbuildexe% echo %__MsgPrefix%Error: Could not find MSBuild.exe.  Please see https://github.com/dotnet/coreclr/blob/master/Documentation/project-docs/developer-guide.md for build instructions. && exit /b 1

:: Note: We've disabled node reuse because it causes file locking issues.
::       The issue is that we extend the build with our own targets which
::       means that that rebuilding cannot successfully delete the task
::       assembly. 
set __msbuildCommonArgs=/nologo /nodeReuse:false %__msbuildCleanBuildArgs% %__msbuildExtraArgs%

if not defined __BuildSequential (
    set __msbuildCommonArgs=%__msbuildCommonArgs% /maxcpucount
)

REM =========================================================================================
REM ===
REM === Restore Build Tools
REM ===
REM =========================================================================================
call %__ThisScriptPath%init-tools.cmd  

REM =========================================================================================
REM ===
REM === Start the build steps
REM ===
REM =========================================================================================

if defined __MscorlibOnly goto PerformMScorlibBuild

if defined __SkipNativeBuild (
    echo %__MsgPrefix%Skipping native components build
    goto SkipNativeBuild
)

echo %__MsgPrefix%Commencing build of native components for %__BuildOS%.%__BuildArch%.%__BuildType%

REM Use setlocal to restrict environment changes form vcvarsall.bat and more to just this native components build section.
setlocal EnableDelayedExpansion EnableExtensions

if /i "%__BuildArch%" == "arm64" ( 
rem arm64 builds currently use private toolset which has not been released yet
REM TODO, remove once the toolset is open.
call :PrivateToolSet

goto GenVSSolution
)

:: Set the environment for the native build
set __VCBuildArch=x86_amd64
if /i "%__BuildArch%" == "x86" (set __VCBuildArch=x86)
echo %__MsgPrefix%Using environment: "%__VSToolsRoot%\..\..\VC\vcvarsall.bat" %__VCBuildArch%
call                                 "%__VSToolsRoot%\..\..\VC\vcvarsall.bat" %__VCBuildArch%
@if defined __echo @echo on

if not defined VSINSTALLDIR (
    echo %__MsgPrefix%Error: VSINSTALLDIR variable not defined.
    exit /b 1
)
if not exist "%VSINSTALLDIR%DIA SDK" goto NoDIA

:GenVSSolution

if defined __SkipConfigure goto SkipConfigure

echo %__MsgPrefix%Regenerating the Visual Studio solution

pushd "%__IntermediatesDir%"
call "%__SourceDir%\pal\tools\gen-buildsys-win.bat" "%__ProjectDir%" %__VSVersion% %__BuildArch%
@if defined __echo @echo on
popd

:SkipConfigure

if not exist "%__IntermediatesDir%\install.vcxproj" (
    echo %__MsgPrefix%Error: failed to generate native component build project!
    exit /b 1
)

REM =========================================================================================
REM ===
REM === Build the CLR VM
REM ===
REM =========================================================================================

if defined __ConfigureOnly goto SkipNativeBuild

echo %__MsgPrefix%Invoking msbuild

set "__BuildLog=%__LogsDir%\CoreCLR_%__BuildOS%__%__BuildArch%__%__BuildType%.log"
set "__BuildWrn=%__LogsDir%\CoreCLR_%__BuildOS%__%__BuildArch%__%__BuildType%.wrn"
set "__BuildErr=%__LogsDir%\CoreCLR_%__BuildOS%__%__BuildArch%__%__BuildType%.err"
set __msbuildLogArgs=^
/fileloggerparameters:Verbosity=normal;LogFile="%__BuildLog%" ^
/fileloggerparameters1:WarningsOnly;LogFile="%__BuildWrn%" ^
/fileloggerparameters2:ErrorsOnly;LogFile="%__BuildErr%" ^
/consoleloggerparameters:Summary ^
/verbosity:minimal

set __msbuildArgs="%__IntermediatesDir%\install.vcxproj" %__msbuildCommonArgs% %__msbuildLogArgs% /p:Configuration=%__BuildType%

if /i "%__BuildArch%" == "arm64" (  
    REM TODO, remove once we have msbuild support for this platform.
    set __msbuildArgs=%__msbuildArgs% /p:UseEnv=true
) else (
    set __msbuildArgs=%__msbuildArgs% /p:Platform=%__BuildArch%
)

%_msbuildexe% %__msbuildArgs%
if errorlevel 1 (
    echo %__MsgPrefix%Error: native component build failed. Refer to the build log files for details:
    echo     %__BuildLog%
    echo     %__BuildWrn%
    echo     %__BuildErr%
    exit /b 1
)

REM endlocal to rid us of environment changes from vcvarsall.bat
endlocal

:SkipNativeBuild

REM =========================================================================================
REM ===
REM === Mscorlib build section.
REM ===
REM =========================================================================================

:PerformMScorlibBuild
if defined __SkipMscorlibBuild (
    echo %__MsgPrefix%Skipping Mscorlib build
    goto SkipMscorlibBuild
)

echo %__MsgPrefix%Commencing build of mscorlib for %__BuildOS%.%__BuildArch%.%__BuildType%

REM setlocal to prepare for vsdevcmd.bat
setlocal EnableDelayedExpansion EnableExtensions

rem Explicitly set Platform causes conflicts in mscorlib project files. Clear it to allow building from VS x64 Native Tools Command Prompt
set Platform=

:: Set the environment for the managed build
echo %__MsgPrefix%Using environment: "%__VSToolsRoot%\VsDevCmd.bat"
call                                 "%__VSToolsRoot%\VsDevCmd.bat"

set "__BuildLog=%__LogsDir%\MScorlib_%__BuildOS%__%__BuildArch%__%__BuildType%.log"
set "__BuildWrn=%__LogsDir%\MScorlib_%__BuildOS%__%__BuildArch%__%__BuildType%.wrn"
set "__BuildErr=%__LogsDir%\MScorlib_%__BuildOS%__%__BuildArch%__%__BuildType%.err"
set __msbuildLogArgs=^
/fileloggerparameters:Verbosity=normal;LogFile="%__BuildLog%" ^
/fileloggerparameters1:WarningsOnly;LogFile="%__BuildWrn%" ^
/fileloggerparameters2:ErrorsOnly;LogFile="%__BuildErr%" ^
/consoleloggerparameters:Summary ^
/verbosity:minimal

set __msbuildArgs="%__ProjectFilesDir%\build.proj" %__msbuildCommonArgs% %__msbuildLogArgs% %__SignTypeReal%

set __BuildNugetPackage=true
if defined __MscorlibOnly       set __BuildNugetPackage=false
if /i "%__BuildArch%" =="arm64" set __BuildNugetPackage=false
if %__BuildNugetPackage%==false set __msbuildArgs=%__msbuildArgs% /p:BuildNugetPackage=false

%_msbuildexe% %__msbuildArgs%
if errorlevel 1 (
    echo %__MsgPrefix%Error: MScorlib build failed. Refer to the build log files for details:
    echo     %__BuildLog%
    echo     %__BuildWrn%
    echo     %__BuildErr%
    exit /b 1
)

if defined __MscorlibOnly (
    echo %__MsgPrefix%Mscorlib successfully built.
    exit /b 0
)

REM Consider doing crossgen build of mscorlib unless we are skipping it intentionally
if /i "%__BuildArch%" == "arm64" (
    if not defined __DoCrossgen (
        echo %__MsgPrefix%Skipping Crossgen
        goto SkipCrossGenBuild
    )
)

echo %__MsgPrefix%Generating native image of mscorlib for %__BuildOS%.%__BuildArch%.%__BuildType%

set "__CrossGenMScorlibLog=%__LogsDir%\CrossgenMScorlib_%__BuildOS%__%__BuildArch%__%__BuildType%.log"
"%__BinDir%\crossgen.exe" "%__BinDir%\mscorlib.dll" > "%__CrossGenMScorlibLog%" 2>&1
if errorlevel 1 (
    echo %__MsgPrefix%Error: CrossGen mscorlib build failed. Refer to the build log file for details:
    echo     %__CrossGenMScorlibLog%
    exit /b 1
)

:GenerateNuget
if /i "%__BuildArch%" =="arm64" goto :SkipNuget

set "__BuildLog=%__LogsDir%\Nuget_%__BuildOS%__%__BuildArch%__%__BuildType%.log"
set "__BuildWrn=%__LogsDir%\Nuget_%__BuildOS%__%__BuildArch%__%__BuildType%.wrn"
set "__BuildErr=%__LogsDir%\Nuget_%__BuildOS%__%__BuildArch%__%__BuildType%.err"
set __msbuildLogArgs=^
/fileloggerparameters:Verbosity=normal;LogFile="%__BuildLog%" ^
/fileloggerparameters1:WarningsOnly;LogFile="%__BuildWrn%" ^
/fileloggerparameters2:ErrorsOnly;LogFile="%__BuildErr%" ^
/consoleloggerparameters:Summary ^
/verbosity:minimal

set __msbuildArgs="%__ProjectFilesDir%\src\.nuget\Microsoft.NETCore.Runtime.CoreClr\Microsoft.NETCore.Runtime.CoreCLR.builds" /p:Platform=%__BuildArch%
%_msbuildexe% %__msbuildArgs% %__msbuildLogArgs%
if errorlevel 1 (
    echo %__MsgPrefix%Error: Nuget package generation failed build failed. Refer to the build log files for details:
    echo     %__BuildLog%
    echo     %__BuildWrn%
    echo     %__BuildErr%
    exit /b 1
)

:SkipNuget

:SkipCrossGenBuild

REM endlocal to rid us of environment changes from vsdevenv.bat
endlocal

:SkipMscorlibBuild

REM =========================================================================================
REM ===
REM === Test build section
REM ===
REM =========================================================================================

if defined __SkipTestBuild (
    echo %__MsgPrefix%Skipping test build
    goto SkipTestBuild
)

echo %__MsgPrefix%Commencing build of tests for %__BuildOS%.%__BuildArch%.%__BuildType%

REM Construct the arguments to pass to the test build script.

set __BuildtestArgs=%__BuildArch% %__BuildType% %__VSVersion%

if defined __CleanBuild (
    set "__BuildtestArgs=%__BuildtestArgs% clean"
)

if defined __BuildSequential (
    set "__BuildtestArgs=%__BuildtestArgs% sequential"
)

if defined __TestPriority (
    set "__BuildtestArgs=%__BuildtestArgs% Priority %__TestPriority%"
)

rem arm64 builds currently use private toolset which has not been released yet
REM TODO, remove once the toolset is open.
if /i "%__BuildArch%" == "arm64" call :PrivateToolSet 

call %__ProjectDir%\tests\buildtest.cmd %__BuildtestArgs%

if errorlevel 1 (
    REM buildtest.cmd has already emitted an error message and mentioned the build log file to examine.
    exit /b 1
)

:SkipTestBuild

REM =========================================================================================
REM ===
REM === All builds complete!
REM ===
REM =========================================================================================

echo %__MsgPrefix%Repo successfully built.
echo %__MsgPrefix%Product binaries are available at !__BinDir!
if not defined __SkipTestBuild (
    echo %__MsgPrefix%Test binaries are available at !__TestBinDir!
)
exit /b 0

REM =========================================================================================
REM ===
REM === Handle the "all" case.
REM ===
REM =========================================================================================

:BuildAll

set __BuildArchList=

set /A __TotalSpecifiedBuildArch=__BuildArchX64 + __BuildArchX86 + __BuildArchArm + __BuildArchArm64
if %__TotalSpecifiedBuildArch% EQU 0 (
    REM Nothing specified means we want to build all architectures.
    set __BuildArchList=x64 x86 arm arm64
)

REM Otherwise, add all the specified architectures to the list.

if %__BuildArchX64%==1      set __BuildArchList=%__BuildArchList% x64
if %__BuildArchX86%==1      set __BuildArchList=%__BuildArchList% x86
if %__BuildArchArm%==1      set __BuildArchList=%__BuildArchList% arm
if %__BuildArchArm64%==1    set __BuildArchList=%__BuildArchList% arm64

set __BuildTypeList=

set /A __TotalSpecifiedBuildType=__BuildTypeDebug + __BuildTypeChecked + __BuildTypeRelease
if %__TotalSpecifiedBuildType% EQU 0 (
    REM Nothing specified means we want to build all build types.
    set __BuildTypeList=Debug Checked Release
)

if %__BuildTypeDebug%==1    set __BuildTypeList=%__BuildTypeList% Debug
if %__BuildTypeChecked%==1  set __BuildTypeList=%__BuildTypeList% Checked
if %__BuildTypeRelease%==1  set __BuildTypeList=%__BuildTypeList% Release

REM Create a temporary file to collect build results. We always build all flavors specified, and
REM report a summary of the results at the end.

set __AllBuildSuccess=true
set __BuildResultFile=%TEMP%\build-all-summary-%RANDOM%.txt
if exist %__BuildResultFile% del /f /q %__BuildResultFile%

for %%i in (%__BuildArchList%) do (
    for %%j in (%__BuildTypeList%) do (
        call :BuildOne %%i %%j
    )
)

if %__AllBuildSuccess%==true (
    echo %__MsgPrefix%All builds succeeded!
    exit /b 0
) else (
    echo %__MsgPrefix%Builds failed:
    type %__BuildResultFile%
    del /f /q %__BuildResultFile%
    exit /b 1
)

REM This code is unreachable, but leaving it nonetheless, just in case things change.
exit /b 99

:BuildOne
set __BuildArch=%1
set __BuildType=%2
set __NextCmd=call %__ThisScriptFull% %__BuildArch% %__BuildType% %__PassThroughArgs%
echo %__MsgPrefix%Invoking: %__NextCmd%
%__NextCmd%
if errorlevel 1 (
    echo %__MsgPrefix%    %__BuildArch% %__BuildType% %__PassThroughArgs% >> %__BuildResultFile%
    set __AllBuildSuccess=false
)
exit /b 0

REM =========================================================================================
REM ===
REM === Helper routines
REM ===
REM =========================================================================================

:Usage
echo.
echo Build the CoreCLR repo.
echo.
echo Usage:
echo     %__ThisScriptShort% [option1] [option2] ...
echo or:
echo     %__ThisScriptShort% all [option1] [option2] ...
echo.
echo All arguments are optional. The options are:
echo.
echo./? -? /h -h /help -help: view this message.
echo Build architecture: one of x64, x86, arm, arm64 ^(default: x64^).
echo Build type: one of Debug, Checked, Release ^(default: Debug^).
echo Visual Studio version: ^(default: VS2015^).
echo clean: force a clean build ^(default is to perform an incremental build^).
echo docrossgen: do a crossgen build of mscorlib.
echo msbuildargs ... : all arguments following this tag will be passed directly to msbuild.
echo mscorlib version: one of freebsdmscorlib, linuxmscorlib, netbsdmscorlib, osxmscorlib,
echo     or windowsmscorlib. If one of these is passed, only mscorlib is built,
echo     for the specified platform ^(FreeBSD, Linux, NetBSD, OS X or Windows,
echo     respectively^).
echo priority ^<N^> : specify a set of test that will be built and run, with priority N.
echo sequential: force a non-parallel build ^(default is to build in parallel
echo     using all processors^).
echo configureonly: skip all builds; only run CMake ^(default: CMake and builds are run^)
echo skipconfigure: skip CMake ^(default: CMake is run^)
echo skipmscorlib: skip building mscorlib ^(default: mscorlib is built^).
echo skipnative: skip building native components ^(default: native components are built^).
echo skiptests: skip building tests ^(default: tests are built^).
echo disableoss: Disable Open Source Signing for mscorlib.
echo toolset_dir ^<dir^> : set the toolset directory -- Arm64 use only. Required for Arm64 builds.
echo.
echo If "all" is specified, then all build architectures and types are built. If, in addition,
echo one or more build architectures or types is specified, then only those build architectures
echo and types are built.
echo.
echo For example:
echo     build all
echo        -- builds all architectures, and all build types per architecture
echo     build all x86
echo        -- builds all build types for x86
echo     build all x64 x86 Checked Release
echo        -- builds x64 and x86 architectures, Checked and Release build types for each
exit /b 1

:NoPS
echo PowerShell is a prerequisite to build this repository, but it is not accessible.
echo Ensure that it is defined in the PATH environment variable.
echo Typically it should be %%SYSTEMROOT%%\System32\WindowsPowerShell\v1.0\.
exit /b 1

:NoVS
echo Visual Studio 2015+ ^(Community is free^) is a prerequisite to build this repository.
echo See: https://github.com/dotnet/coreclr/blob/master/Documentation/project-docs/developer-guide.md#prerequisites
exit /b 1

:NoDIA
echo Error: DIA SDK is missing at "%VSINSTALLDIR%DIA SDK". ^
This is due to a bug in the Visual Studio installer. It does not install DIA SDK at "%VSINSTALLDIR%" but rather ^
at the install location of previous Visual Studio version. The workaround is to copy the DIA SDK folder from the Visual Studio install location ^
of the previous version to "%VSINSTALLDIR%" and then build.
:: DIA SDK not included in Express editions
echo Visual Studio Express does not include the DIA SDK. ^
You need Visual Studio 2015+ (Community is free).
echo See: https://github.com/dotnet/coreclr/blob/master/Documentation/project-docs/developer-guide.md#prerequisites
exit /b 1

:PrivateToolSet

echo %__MsgPrefix% Setting Up the usage of __ToolsetDir:%__ToolsetDir%

if /i "%__ToolsetDir%" == "" (
    echo %__MsgPrefix%Error: A toolset directory is required for the Arm64 Windows build. Use the toolset_dir argument.
    exit /b 1
)

set PATH=%PATH%;%__ToolsetDir%\cpp\bin
set LIB=%__ToolsetDir%\OS\lib;%__ToolsetDir%\cpp\lib
set INCLUDE=^
%__ToolsetDir%\cpp\inc;^
%__ToolsetDir%\OS\inc\Windows;^
%__ToolsetDir%\OS\inc\Windows\crt;^
%__ToolsetDir%\cpp\inc\vc;^
%__ToolsetDir%\OS\inc\win8
exit /b 0
