@if not defined __echo @echo off
setlocal EnableDelayedExpansion EnableExtensions

set __ThisScriptFull="%~f0"
set __VSToolsRoot=%VS140COMNTOOLS%
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

:: Set the default arguments for build
set __BuildArch=x64
set __BuildType=Debug
set __BuildOS=Windows_NT

:: Default to highest Visual Studio version available
set __VSVersion=vs2015

:: Define a prefix for most output progress messages that come from this script. That makes
:: it easier to see where these are coming from. Note that there is a trailing space here.
set "__MsgPrefix=BUILD: "

:: Set the various build properties here so that CMake and MSBuild can pick them up
set "__ProjectDir=%~dp0"
:: remove trailing slash
if %__ProjectDir:~-1%==\ set "__ProjectDir=%__ProjectDir:~0,-1%"
set "__ProjectFilesDir=%__ProjectDir%"
set "__SourceDir=%__ProjectDir%\src"
set "__PackagesDir=%__ProjectDir%\packages"
set "__RootBinDir=%__ProjectDir%\bin"
set "__LogsDir=%__RootBinDir%\Logs"

set __BuildAll=

set __BuildArchX64=0
set __BuildArchX86=0
set __BuildArchArm=0
set __BuildArchArm64=0

set __BuildTypeDebug=0
set __BuildTypeChecked=0
set __BuildTypeRelease=0
set __BuildJit32="-DBUILD_JIT32=0"

REM __PassThroughArgs is a set of things that will be passed through to nested calls to build.cmd
REM when using "all".
set __PassThroughArgs=

REM __UnprocessedBuildArgs are args that we pass to msbuild (e.g. /p:__BuildArch=x64)
set "__args= %*"
set processedArgs=
set __UnprocessedBuildArgs=
set __RunArgs=

set __BuildCoreLib=1
set __BuildNative=1
set __BuildTests=1
set __BuildPackages=1
set __BuildNativeCoreLib=1

:Arg_Loop
if "%1" == "" goto ArgsDone

if /i "%1" == "-?"    goto Usage
if /i "%1" == "-h"    goto Usage
if /i "%1" == "-help" goto Usage

if /i "%1" == "all"                 (set __BuildAll=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "x64"                 (set __BuildArchX64=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "x86"                 (set __BuildArchX86=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "arm"                 (set __BuildArchArm=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "arm64"               (set __BuildArchArm64=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)

if /i "%1" == "debug"               (set __BuildTypeDebug=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "checked"             (set __BuildTypeChecked=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "release"             (set __BuildTypeRelease=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)

REM All arguments after this point will be passed through directly to build.cmd on nested invocations
REM using the "all" argument, and must be added to the __PassThroughArgs variable.
set __PassThroughArgs=%__PassThroughArgs% %1

if /i "%1" == "freebsdmscorlib"     (set __BuildNativeCoreLib=0&set __BuildNative=0&set __BuildTests=0&set __BuildPackages=0&set __BuildOS=FreeBSD&set __SkipNugetPackage=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "linuxmscorlib"       (set __BuildNativeCoreLib=0&set __BuildNative=0&set __BuildTests=0&set __BuildPackages=0&set __BuildOS=Linux&set __SkipNugetPackage=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "netbsdmscorlib"      (set __BuildNativeCoreLib=0&set __BuildNative=0&set __BuildTests=0&set __BuildPackages=0&set __BuildOS=NetBSD&set __SkipNugetPackage=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "osxmscorlib"         (set __BuildNativeCoreLib=0&set __BuildNative=0&set __BuildTests=0&set __BuildPackages=0&set __BuildOS=OSX&set __SkipNugetPackage=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "windowsmscorlib"     (set __BuildNativeCoreLib=0&set __BuildNative=0&set __BuildTests=0&set __BuildPackages=0&set __BuildOS=Windows_NT&set __SkipNugetPackage=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "nativemscorlib"      (set __BuildNativeCoreLib=1&set __BuildCoreLib=0&set __BuildNative=0&set __BuildTests=0&set __BuildPackages=0&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "configureonly"       (set __ConfigureOnly=1&set __BuildNative=1&set __BuildNativeCoreLib=0&set __BuildCoreLib=0&set __BuildTests=0&set __BuildPackages=0&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "skipconfigure"       (set __SkipConfigure=1&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "skipmscorlib"        (set __BuildCoreLib=0&set __BuildNativeCoreLib=0&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "skipnative"          (set __BuildNative=0&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "skiptests"           (set __BuildTests=0&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "skipbuildpackages"   (set __BuildPackages=0&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "usenmakemakefiles"   (set __NMakeMakefiles=1&set __ConfigureOnly=1&set __BuildNative=1&set __BuildNativeCoreLib=0&set __BuildCoreLib=0&set __BuildTests=0&set __BuildPackages=0&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "buildjit32"          (set __BuildJit32="-DBUILD_JIT32=1"&set processedArgs=!processedArgs! %1&shift&goto Arg_Loop)
if /i "%1" == "toolset_dir"         (set __ToolsetDir=%2&set __PassThroughArgs=%__PassThroughArgs% %2&set processedArgs=!processedArgs! %1 %2&shift&shift&goto Arg_Loop)

if [!processedArgs!]==[] (
  call set __UnprocessedBuildArgs=!__args!
) else (
  call set __UnprocessedBuildArgs=%%__args:*!processedArgs!=%%
)

:ArgsDone

if defined __BuildAll goto BuildAll

set /A __TotalSpecifiedBuildArch=__BuildArchX64 + __BuildArchX86 + __BuildArchArm + __BuildArchArm64
if %__TotalSpecifiedBuildArch% GTR 1 (
    echo Error: more than one build architecture specified, but "all" not specified.
    goto Usage
)

if %__BuildArchX64%==1      set __BuildArch=x64
if %__BuildArchX86%==1      set __BuildArch=x86
if %__BuildArchArm%==1 (
    set __BuildArch=arm
    set __CrossArch=x86
)
if %__BuildArchArm64%==1 (
    set __BuildArch=arm64
    set __CrossArch=x64
)

set /A __TotalSpecifiedBuildType=__BuildTypeDebug + __BuildTypeChecked + __BuildTypeRelease
if %__TotalSpecifiedBuildType% GTR 1 (
    echo Error: more than one build type specified, but "all" not specified.
    goto Usage
)

if %__BuildTypeDebug%==1    set __BuildType=Debug
if %__BuildTypeChecked%==1  set __BuildType=Checked
if %__BuildTypeRelease%==1  set __BuildType=Release

set __RunArgs=-BuildOS=%__BuildOS% -BuildType=%__BuildType% -BuildArch=%__BuildArch%

:: Set the remaining variables based upon the determined build configuration
set "__BinDir=%__RootBinDir%\Product\%__BuildOS%.%__BuildArch%.%__BuildType%"
set "__IntermediatesDir=%__RootBinDir%\obj\%__BuildOS%.%__BuildArch%.%__BuildType%"
if "%__NMakeMakefiles%"=="1" (set "__IntermediatesDir=%__RootBinDir%\nmakeobj\%__BuildOS%.%__BuildArch%.%__BuildType%")
set "__PackagesBinDir=%__BinDir%\.nuget"
set "__TestRootDir=%__RootBinDir%\tests"
set "__TestBinDir=%__TestRootDir%\%__BuildOS%.%__BuildArch%.%__BuildType%"
set "__TestIntermediatesDir=%__RootBinDir%\tests\obj\%__BuildOS%.%__BuildArch%.%__BuildType%"
set "__CrossComponentBinDir=%__BinDir%"
set "__CrossCompIntermediatesDir=%__IntermediatesDir%\crossgen"

if NOT "%__CrossArch%" == "" set __CrossComponentBinDir=%__CrossComponentBinDir%\%__CrossArch%
set "__CrossGenCoreLibLog=%__LogsDir%\CrossgenCoreLib_%__BuildOS%__%__BuildArch%__%__BuildType%.log"
set "__CrossgenExe=%__CrossComponentBinDir%\crossgen.exe"

:: Generate path to be set for CMAKE_INSTALL_PREFIX to contain forward slash
set "__CMakeBinDir=%__BinDir%"
set "__CMakeBinDir=%__CMakeBinDir:\=/%"

if not exist "%__BinDir%"           md "%__BinDir%"
if not exist "%__IntermediatesDir%" md "%__IntermediatesDir%"
if not exist "%__LogsDir%"          md "%__LogsDir%"

echo %__MsgPrefix%Commencing CoreCLR Repo build

:: Set the remaining variables based upon the determined build configuration

echo %__MsgPrefix%Checking prerequisites
:: Eval the output from probe-win1.ps1
for /f "delims=" %%a in ('powershell -NoProfile -ExecutionPolicy RemoteSigned "& ""%__SourceDir%\pal\tools\probe-win.ps1"""') do %%a

REM =========================================================================================
REM ===
REM === Start the build steps
REM ===
REM =========================================================================================

echo %__MsgPrefix%Using environment: "%__VSToolsRoot%\VsDevCmd.bat"
call                                 "%__VSToolsRoot%\VsDevCmd.bat"

@call %__ProjectDir%\run.cmd build -Project=%__ProjectDir%\build.proj -generateHeaderWindows -NativeVersionHeaderFile="%__RootBinDir%\obj\_version.h" %__RunArgs% %__UnprocessedBuildArgs% 

REM =========================================================================================
REM ===
REM === Build the CLR VM
REM ===
REM =========================================================================================

if %__BuildNative% EQU 1 ( 
    echo %__MsgPrefix%Commencing build of native components for %__BuildOS%.%__BuildArch%.%__BuildType%

	set nativePlatfromArgs=-platform=%__BuildArch%
    if /i "%__BuildArch%" == "arm64" ( set nativePlatfromArgs=-useEnv )

    set __MsbuildLog=/flp:Verbosity=normal;LogFile="%__LogsDir%\CoreCLR_%__BuildOS%__%__BuildArch%__%__BuildType%.log"
    set __MsbuildWrn=/flp1:WarningsOnly;LogFile="%__LogsDir%\CoreCLR_%__BuildOS%__%__BuildArch%__%__BuildType%.wrn"
    set __MsbuildErr=/flp2:ErrorsOnly;LogFile="%__LogsDir%\CoreCLR_%__BuildOS%__%__BuildArch%__%__BuildType%.err"

    if /i "%__BuildArch%" == "arm64" ( 
        rem arm64 builds currently use private toolset which has not been released yet
        REM TODO, remove once the toolset is open.
        call :PrivateToolSet
        goto GenVSSolution
    )

    :: Set the environment for the native build
    set __VCBuildArch=x86_amd64
    if /i "%__BuildArch%" == "x86" ( set __VCBuildArch=x86 )
    if /i "%__BuildArch%" == "arm" (set __VCBuildArch=x86_arm)
    echo %__MsgPrefix%Using environment: "%__VSToolsRoot%\..\..\VC\vcvarsall.bat" !__VCBuildArch!
    call                                 "%__VSToolsRoot%\..\..\VC\vcvarsall.bat" !__VCBuildArch!
	@if defined __echo @echo on

    if not defined VSINSTALLDIR (
        echo %__MsgPrefix%Error: VSINSTALLDIR variable not defined.
        exit /b 1
    )
    if not exist "!VSINSTALLDIR!DIA SDK" goto NoDIA
:GenVSSolution
    if defined __SkipConfigure goto SkipConfigure

    echo %__MsgPrefix%Regenerating the Visual Studio solution

    pushd "%__IntermediatesDir%"
    call "%__SourceDir%\pal\tools\gen-buildsys-win.bat" "%__ProjectDir%" %__VSVersion% %__BuildArch% %__BuildJit32%
	@if defined __echo @echo on
    popd
:SkipConfigure
    if defined __ConfigureOnly goto SkipNativeBuild

    if not exist "%__IntermediatesDir%\install.vcxproj" (
        echo %__MsgPrefix%Error: failed to generate native component build project!
        exit /b 1
    )

    @call %__ProjectDir%\run.cmd build -Project=%__IntermediatesDir%\install.vcxproj -MsBuildLog=!__MsbuildLog! -MsBuildWrn=!__MsbuildWrn! -MsBuildErr=!__MsbuildErr! -configuration=%__BuildType% %nativePlatfromArgs% %__RunArgs% %__UnprocessedBuildArgs%

    if not !errorlevel! == 0 (
        echo %__MsgPrefix%Error: native component build failed. Refer to the build log files for details:
        echo     "%__LogsDir%\CoreCLR_%__BuildOS%__%__BuildArch%__%__BuildType%.log"
        echo     "%__LogsDir%\CoreCLR_%__BuildOS%__%__BuildArch%__%__BuildType%.wrn"
        echo     "%__LogsDir%\CoreCLR_%__BuildOS%__%__BuildArch%__%__BuildType%.err"
        exit /b 1
    )   
)
:SkipNativeBuild

REM =========================================================================================
REM ===
REM === Build Cross-Architecture Native Components (if applicable)
REM ===
REM =========================================================================================

if /i "%__BuildArch%"=="arm64" (
    set __DoCrossArchBuild=1
    )

if /i "%__BuildArch%"=="arm" (
    set __DoCrossArchBuild=1
    )

if /i "%__DoCrossArchBuild%"=="1" (

    echo %__MsgPrefix%Commencing build of cross architecture native components for %__BuildOS%.%__BuildArch%.%__BuildType%

    :: Set the environment for the native build
    set __VCBuildArch=x86_amd64
    if /i "%__CrossArch%" == "x86" ( set __VCBuildArch=x86 )
    @call "%__VSToolsRoot%\..\..\VC\vcvarsall.bat" !__VCBuildArch!
    @if defined __echo @echo on

    if not exist "%__CrossCompIntermediatesDir%" md "%__CrossCompIntermediatesDir%"
    if defined __SkipConfigure goto SkipConfigureCrossBuild

    pushd "%__CrossCompIntermediatesDir%"
    set __CMakeBinDir=%__CrossComponentBinDir%
    set "__CMakeBinDir=!__CMakeBinDir:\=/!"
    set __ExtraCmakeArgs="-DCLR_CROSS_COMPONENTS_BUILD=1" "-DCLR_CMAKE_TARGET_ARCH=%__BuildArch%"
    call "%__SourceDir%\pal\tools\gen-buildsys-win.bat" "%__ProjectDir%" %__VSVersion% %__CrossArch% !__ExtraCmakeArgs!
    @if defined __echo @echo on
    popd
:SkipConfigureCrossBuild
    if not exist "%__CrossCompIntermediatesDir%\install.vcxproj" (
        echo %__MsgPrefix%Error: failed to generate cross-arch components build project!
        exit /b 1
    )

    if defined __ConfigureOnly goto SkipCrossCompBuild

    echo %__MsgPrefix%Invoking msbuild

    set __MsbuildLog=/flp:Verbosity=normal;LogFile="%__LogsDir%\Cross_%__BuildOS%__%__BuildArch%__%__BuildType%.log"
    set __MsbuildWrn=/flp1:WarningsOnly;LogFile="%__LogsDir%\Cross_%__BuildOS%__%__BuildArch%__%__BuildType%.wrn"
    set __MsbuildErr=/flp2:ErrorsOnly;LogFile="%__LogsDir%\Cross_%__BuildOS%__%__BuildArch%__%__BuildType%.err"
    @call %__ProjectDir%\run.cmd build -Project=%__CrossCompIntermediatesDir%\install.vcxproj -configuration=%__BuildType% -platform=%__CrossArch% -MsBuildLog=!__MsbuildLog! -MsBuildWrn=!__MsbuildWrn! -MsBuildErr=!__MsbuildErr! %__RunArgs% %__UnprocessedBuildArgs%
    if not !errorlevel! == 0 (
        echo %__MsgPrefix%Error: cross-arch components build failed. Refer to the build log files for details:
        echo     "%__LogsDir%\Cross_%__BuildOS%__%__BuildArch%__%__BuildType%.log"
        echo     "%__LogsDir%\Cross_%__BuildOS%__%__BuildArch%__%__BuildType%.wrn"
        echo     "%__LogsDir%\Cross_%__BuildOS%__%__BuildArch%__%__BuildType%.err"
        exit /b 1
    )    
)

:SkipCrossCompBuild

REM =========================================================================================
REM ===
REM === CoreLib and NuGet package build section.
REM ===
REM =========================================================================================

if %__BuildCoreLib% EQU 1 (  
	
	echo %__MsgPrefix%Commencing build of System.Private.CoreLib for %__BuildOS%.%__BuildArch%.%__BuildType%
    rem Explicitly set Platform causes conflicts in CoreLib project files. Clear it to allow building from VS x64 Native Tools Command Prompt
    set Platform=

    set __MsbuildLog=/flp:Verbosity=normal;LogFile="%__LogsDir%\System.Private.CoreLib_%__BuildOS%__%__BuildArch%__%__BuildType%.log"
    set __MsbuildWrn=/flp1:WarningsOnly;LogFile="%__LogsDir%\System.Private.CoreLib_%__BuildOS%__%__BuildArch%__%__BuildType%.wrn"
    set __MsbuildErr=/flp2:ErrorsOnly;LogFile="%__LogsDir%\System.Private.CoreLib_%__BuildOS%__%__BuildArch%__%__BuildType%.err"

    if /i "%__BuildArch%" == "arm64" (
		set __nugetBuildArgs=-buildNugetPackage=false
    ) else if "%__SkipNugetPackage%" == "1" (
		set __nugetBuildArgs=-buildNugetPackage=false
    ) else (
		set __nugetBuildArgs=-buildNugetPackage=true
	)

    @call %__ProjectDir%\run.cmd build -Project=%__ProjectDir%\build.proj -MsBuildLog=!__MsbuildLog! -MsBuildWrn=!__MsbuildWrn! -MsBuildErr=!__MsbuildErr! !__nugetBuildArgs! %__RunArgs% %__UnprocessedBuildArgs% 
    if not !errorlevel! == 0 (
        echo %__MsgPrefix%Error: System.Private.CoreLib build failed. Refer to the build log files for details:
        echo     "%__LogsDir%\System.Private.CoreLib_%__BuildOS%__%__BuildArch%__%__BuildType%.log"
        echo     "%__LogsDir%\System.Private.CoreLib_%__BuildOS%__%__BuildArch%__%__BuildType%.wrn"
        echo     "%__LogsDir%\System.Private.CoreLib_%__BuildOS%__%__BuildArch%__%__BuildType%.err"
        exit /b 1
    )
)

if %__BuildNativeCoreLib% EQU 1 (
    echo %__MsgPrefix%Generating native image of System.Private.CoreLib for %__BuildOS%.%__BuildArch%.%__BuildType%
	
    echo "%__CrossgenExe%" /Platform_Assemblies_Paths "%__BinDir%" /out "%__BinDir%\System.Private.CoreLib.ni.dll" "%__BinDir%\System.Private.CoreLib.dll"
    "%__CrossgenExe%" /Platform_Assemblies_Paths "%__BinDir%" /out "%__BinDir%\System.Private.CoreLib.ni.dll" "%__BinDir%\System.Private.CoreLib.dll" > "%__CrossGenCoreLibLog%" 2>&1
    if NOT !errorlevel! == 0 (
        echo %__MsgPrefix%Error: CrossGen System.Private.CoreLib build failed. Refer to the build log file for details:
        echo     %__CrossGenCoreLibLog%
        exit /b 1
    )

    echo %__MsgPrefix%Generating native image of MScorlib facade for %__BuildOS%.%__BuildArch%.%__BuildType%

    set "__CrossGenCoreLibLog=%__LogsDir%\CrossgenMSCoreLib_%__BuildOS%__%__BuildArch%__%__BuildType%.log"
    set "__CrossgenExe=%__CrossComponentBinDir%\crossgen.exe"
    "!__CrossgenExe!" /Platform_Assemblies_Paths "%__BinDir%" /out "%__BinDir%\mscorlib.ni.dll" "%__BinDir%\mscorlib.dll" > "!__CrossGenCoreLibLog!" 2>&1
    if NOT !errorlevel! == 0 (
        echo %__MsgPrefix%Error: CrossGen mscorlib facade build failed. Refer to the build log file for details:
        echo     !__CrossGenCoreLibLog!
        exit /b 1
    )
)

if %__BuildPackages% EQU 1 (
    set __MsbuildLog=/flp:Verbosity=normal;LogFile="%__LogsDir%\Nuget_%__BuildOS%__%__BuildArch%__%__BuildType%.log"
	set __MsbuildWrn=/flp1:WarningsOnly;LogFile="%__LogsDir%\Nuget_%__BuildOS%__%__BuildArch%__%__BuildType%.wrn"
	set __MsbuildErr=/flp2:ErrorsOnly;LogFile="%__LogsDir%\Nuget_%__BuildOS%__%__BuildArch%__%__BuildType%.err"

    REM The conditions as to what to build are captured in the builds file.
    @call %__ProjectDir%\run.cmd build -Project=%__SourceDir%\.nuget\packages.builds -platform=%__BuildArch% -MsBuildLog=!__MsbuildLog! -MsBuildWrn=!__MsbuildWrn! -MsBuildErr=!__MsbuildErr! %__RunArgs% %__UnprocessedBuildArgs%

    if not !errorlevel! == 0 (
        echo %__MsgPrefix%Error: Nuget package generation failed build failed. Refer to the build log files for details:
        echo     "%__LogsDir%\Nuget_%__BuildOS%__%__BuildArch%__%__BuildType%.log"
        echo     "%__LogsDir%\Nuget_%__BuildOS%__%__BuildArch%__%__BuildType%.wrn"
        echo     "%__LogsDir%\Nuget_%__BuildOS%__%__BuildArch%__%__BuildType%.err"
        exit /b 1
    )
)

REM =========================================================================================
REM ===
REM === Test build section
REM ===
REM =========================================================================================

if %__BuildTests% EQU 1 (
    echo %__MsgPrefix%Commencing build of tests for %__BuildOS%.%__BuildArch%.%__BuildType%

    REM Construct the arguments to pass to the test build script.

    rem arm64 builds currently use private toolset which has not been released yet
    REM TODO, remove once the toolset is open.
    if /i "%__BuildArch%" == "arm64" call :PrivateToolSet 

    echo "%__ProjectDir%\build-test.cmd %__BuildArch% %__BuildType% %__UnprocessedBuildArgs%"
    @call %__ProjectDir%\build-test.cmd %__BuildArch% %__BuildType% %__UnprocessedBuildArgs%

    if not !errorlevel! == 0 (
        REM buildtest.cmd has already emitted an error message and mentioned the build log file to examine.
        exit /b 1
    )
)

REM =========================================================================================
REM ===
REM === All builds complete!
REM ===
REM =========================================================================================

echo %__MsgPrefix%Repo successfully built.
echo %__MsgPrefix%Product binaries are available at !__BinDir!
if %__BuildTests% EQU 1 (
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
if not !errorlevel! == 0 (
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
echo     build.cmd [option1] [option2]
echo or:
echo     build.cmd all [option1] [option2] -- ...
echo.
echo All arguments are optional. The options are:
echo.
echo.-? -h -help: view this message.
echo all: Builds all configurations and platforms.
echo Build architecture: one of x64, x86, arm, arm64 ^(default: x64^).
echo Build type: one of Debug, Checked, Release ^(default: Debug^).
echo -- ... : all arguments following this tag will be passed directly to msbuild.
echo mscorlib version: one of freebsdmscorlib, linuxmscorlib, netbsdmscorlib, osxmscorlib,
echo     or windowsmscorlib. If one of these is passed, only System.Private.CoreLib is built,
echo     for the specified platform ^(FreeBSD, Linux, NetBSD, OS X or Windows,
echo     respectively^).
echo     add nativemscorlib to go further and build the native image for designated mscorlib.
echo toolset_dir ^<dir^> : set the toolset directory -- Arm64 use only. Required for Arm64 builds.
echo configureonly: skip all builds; only run CMake ^(default: CMake and builds are run^)
echo skipconfigure: skip CMake ^(default: CMake is run^)
echo skipmscorlib: skip building System.Private.CoreLib ^(default: System.Private.CoreLib is built^).
echo skipnative: skip building native components ^(default: native components are built^).
echo skiptests: skip building tests ^(default: tests are built^).
echo skipbuildpackages: skip building nuget packages ^(default: packages are built^).
echo -skiprestore: skip restoring packages ^(default: packages are restored during build^).
echo -disableoss: Disable Open Source Signing for System.Private.CoreLib.
echo -priority=^<N^> : specify a set of test that will be built and run, with priority N.
echo -sequential: force a non-parallel build ^(default is to build in parallel
echo     using all processors^).
echo -officialbuildid=^<ID^>: specify the official build ID to be used by this build.
echo -Rebuild: passes /t:rebuild to the build projects.
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

set PATH=%__ToolsetDir%\VC_sdk\bin;%PATH%
set LIB=%__ToolsetDir%\VC_sdk\lib\arm64;%__ToolsetDir%\sdpublic\sdk\lib\arm64
set INCLUDE=^
%__ToolsetDir%\VC_sdk\inc;^
%__ToolsetDir%\sdpublic\sdk\inc;^
%__ToolsetDir%\sdpublic\shared\inc;^
%__ToolsetDir%\sdpublic\shared\inc\minwin;^
%__ToolsetDir%\sdpublic\sdk\inc\ucrt;^
%__ToolsetDir%\sdpublic\sdk\inc\minwin;^
%__ToolsetDir%\sdpublic\sdk\inc\mincore;^
%__ToolsetDir%\sdpublic\sdk\inc\abi;^
%__ToolsetDir%\sdpublic\sdk\inc\clientcore;^
%__ToolsetDir%\diasdk\include
exit /b 0
