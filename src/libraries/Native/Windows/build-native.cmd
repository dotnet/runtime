@echo off
setlocal

:SetupArgs
:: Initialize the args that will be passed to cmake
set __sourceDir=%~dp0
set __binDir=%~dp0..\..\..\bin
set __CMakeBinDir=""
set __IntermediatesDir=""
set __BuildArch=AnyCPU
set __VCBuildArch=x86_amd64
set CMAKE_BUILD_TYPE=Debug

:Arg_Loop
:: Since the native build requires some configuration information before msbuild is called, we have to do some manual args parsing
:: For consistency with building the managed components, args are taken in the msbuild style i.e. /p:
if [%1] == [] goto :ToolsVersion
if /i [%1] == [/p:ConfigurationGroup]    (
    if /i [%2] == [Release] (set CMAKE_BUILD_TYPE=Release&&shift&&shift&goto Arg_Loop)
    if /i [%2] == [Debug]   (set CMAKE_BUILD_TYPE=Debug&&shift&&shift&goto Arg_Loop)
    echo Error: Invalid configuration args "%1 and %2"
    exit /b 1
)
if /i [%1] == [/p:Platform]    (
    if /i [%2] == [AnyCPU]  (set __BuildArch=AnyCPU&&set __VCBuildArch=x86_amd64&&shift&&shift&goto Arg_Loop)
    if /i [%2] == [x86]     (set __BuildArch=x86&&set __VCBuildArch=x86&&shift&&shift&goto Arg_Loop)
    if /i [%2] == [arm]     (set __BuildArch=arm&&set __VCBuildArch=x86_arm&&shift&&shift&goto Arg_Loop)
    if /i [%2] == [x64]     (set __BuildArch=x64&&set __VCBuildArch=x86_amd64&&shift&&shift&goto Arg_Loop)
    echo Error: Invalid platform args "%1 and %2"
    exit /b 1
)
if /i [%1] == [-intermediateDir]    (
    set __IntermediatesDir=%2
)
if /i [%1] == [-binDir]    (
    set __CMakeBinDir=%2
)
shift
goto :Arg_Loop

:ToolsVersion
:: Determine the tools version to pass to cmake/msbuild
if not defined VisualStudioVersion (
    if defined VS140COMNTOOLS (
        goto :VS2015
    ) 
    if defined VS120COMNTOOLS (
        goto :VS2013
    )
    goto :MissingVersion
) 
if "%VisualStudioVersion%"=="14.0" (
    goto :VS2015
) 
if "%VisualStudioVersion%"=="12.0" (
    goto :VS2013
)   

:MissingVersion
:: Can't find VS 2013+
echo Error: build.cmd requires Visual Studio 2013 or 2015.  
echo        Please see https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/developer-guide.md for build instructions.
exit /b 1

:VS2013
:: Setup vars for VS2013
set __VSVersion=vs2013
set __PlatformToolset="v120"
:: Set the environment for the native build
call "%VS120COMNTOOLS%\..\..\VC\vcvarsall.bat" %__VCBuildArch%
goto :SetupDirs

:VS2015
:: Setup vars for VS2015
set __VSVersion=vs2015
set __PlatformToolset="v140"
:: Set the environment for the native build
call "%VS140COMNTOOLS%\..\..\VC\vcvarsall.bat" %__VCBuildArch%
goto :SetupDirs


:SetupDirs
:: Setup to cmake the native components
echo Commencing build of native components
echo.

if %__CMakeBinDir% == "" (
    set "__CMakeBinDir=%__binDir%\Windows_NT.%__BuildArch%.%CMAKE_BUILD_TYPE%\Native"
)
if %__IntermediatesDir% == "" (
    set "__IntermediatesDir=%__binDir%\obj\Windows_NT.%__BuildArch%.%CMAKE_BUILD_TYPE%\Native"
)
    set "__CMakeBinDir=%__CMakeBinDir:\=/%"
    set "__IntermediatesDir=%__IntermediatesDir:\=/%"

:: We wanted to allow AnyCPU for folder creation purposes, but it doesn't make sense for a native build. Default to something else for the actual building.
if "%__BuildArch%" == "AnyCPU" (
    set __BuildArch=x64
)
echo %__CMakeBinDir%

:: Check that the intermediate directory exists so we can place our cmake build tree there
if exist "%__IntermediatesDir%" rd /s /q "%__IntermediatesDir%"
if not exist "%__IntermediatesDir%" md "%__IntermediatesDir%"

if exist "%VSINSTALLDIR%DIA SDK" goto GenVSSolution
echo Error: DIA SDK is missing at "%VSINSTALLDIR%DIA SDK". ^
This is due to a bug in the Visual Studio installer. It does not install DIA SDK at "%VSINSTALLDIR%" but rather ^
at VS install location of previous version. Workaround is to copy DIA SDK folder from VS install location ^
of previous version to "%VSINSTALLDIR%" and then resume build.
:: DIA SDK not included in Express editions
echo Visual Studio 2013 Express does not include the DIA SDK. ^
You need Visual Studio 2013+ (Community is free).
echo See: https://github.com/dotnet/coreclr/blob/master/Documentation/project-docs/developer-guide.md#prerequisites
exit /b 1

:GenVSSolution
:: Regenerate the VS solution
pushd "%__IntermediatesDir%"
call "%__sourceDir%\gen-buildsys-win.bat" %__sourceDir% %__VSVersion% %__BuildArch%
popd

:CheckForProj
:: Check that the project created by Cmake exists
if exist "%__IntermediatesDir%\install.vcxproj" goto BuildNativeProj
goto :Failure

:BuildNativeProj
:: Build the project created by Cmake
msbuild "%__IntermediatesDir%\install.vcxproj" /t:rebuild /nologo /p:Configuration=%CMAKE_BUILD_TYPE% /p:Platform=%__BuildArch% /maxcpucount /nodeReuse:false /p:PlatformToolset="%__PlatformToolset%" /fileloggerparameters:Verbosity=normal
IF ERRORLEVEL 1 (
    goto :Failure
)

:Success
:: Successful build
echo Done building Native components
EXIT /B 0

:Failure
:: Build failed
echo Failed to generate native component build project!
exit /b 1