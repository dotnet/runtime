@echo off
setlocal

IF [%PACKAGES_DIR%]==[] set PACKAGES_DIR=%~dp0packages\
IF [%TOOLRUNTIME_DIR%]==[] set TOOLRUNTIME_DIR=%~dp0Tools
set DOTNET_PATH=%TOOLRUNTIME_DIR%\dotnetcli\
IF [%DOTNET_CMD%]==[] set DOTNET_CMD=%DOTNET_PATH%bin\dotnet.exe
IF [%BUILDTOOLS_SOURCE%]==[] set BUILDTOOLS_SOURCE=https://www.myget.org/F/dotnet-buildtools/
set /P BUILDTOOLS_VERSION=< %~dp0BuildToolsVersion.txt
set BUILD_TOOLS_PATH=%PACKAGES_DIR%Microsoft.DotNet.BuildTools\%BUILDTOOLS_VERSION%\lib\
set PROJECT_JSON_PATH=%TOOLRUNTIME_DIR%\%BUILDTOOLS_VERSION%
set PROJECT_JSON_FILE=%PROJECT_JSON_PATH%\project.json
set PROJECT_JSON_CONTENTS={ "dependencies": { "Microsoft.DotNet.BuildTools": "%BUILDTOOLS_VERSION%" }, "frameworks": { "dnxcore50": { } } }
set BUILD_TOOLS_SEMAPHORE=%PROJECT_JSON_PATH%\init-tools.completed

IF EXIST "%BUILD_TOOLS_SEMAPHORE%" goto :EOF
IF EXIST %TOOLRUNTIME_DIR% RMDIR /S /Q %TOOLRUNTIME_DIR%

if exist "%DOTNET_CMD%" goto :afterdotnetrestore

echo **Installing dotnet cli at %DOTNET_PATH%
mkdir "%DOTNET_PATH%"
powershell -NoProfile -ExecutionPolicy unrestricted -Command "(New-Object Net.WebClient).DownloadFile('https://dotnetcli.blob.core.windows.net/dotnet/dev/Binaries/Latest/dotnet-win-x64.latest.zip', '%DOTNET_PATH%' + '\dotnet-win-x64.latest.zip')"
powershell -NoProfile -ExecutionPolicy unrestricted -Command "(New-Object -com shell.application).namespace('%DOTNET_PATH%').CopyHere((new-object -com shell.application).namespace('%DOTNET_PATH%' + 'dotnet-win-x64.latest.zip').Items(),16)"

:afterdotnetrestore

echo **Creating semaphore file: %PROJECT_JSON_FILE%
mkdir "%PROJECT_JSON_PATH%"
echo %PROJECT_JSON_CONTENTS% > %PROJECT_JSON_FILE%
echo **Finished creating semaphore file.

IF EXIST "%BUILD_TOOLS_PATH%" goto :afterbuildtoolsrestore
echo **Installing build tools at %BUILD_TOOLS_PATH%
echo Running: %DOTNET_CMD% restore %PROJECT_JSON_FILE% --packages %PACKAGES_DIR% --source %BUILDTOOLS_SOURCE%
call %DOTNET_CMD% restore %PROJECT_JSON_FILE% --packages %PACKAGES_DIR% --source %BUILDTOOLS_SOURCE%
IF NOT EXIST "%BUILD_TOOLS_PATH%init-tools.cmd" goto :errordownloadingbuildtools
echo **Finished Installing build tools

:afterbuildtoolsrestore
echo **Calling %BUILD_TOOLS_PATH%init-tools.cmd
echo Running: %BUILD_TOOLS_PATH%init-tools.cmd %~dp0 %DOTNET_CMD% %TOOLRUNTIME_DIR%
call "%BUILD_TOOLS_PATH%init-tools.cmd" "%~dp0" "%DOTNET_CMD%" "%TOOLRUNTIME_DIR%"
echo **Finished calling %BUILD_TOOLS_PATH%init-tools.cmd

echo Init-Tools.cmd completed> "%BUILD_TOOLS_SEMAPHORE%"
goto :EOF

:errordownloadingbuildtools
echo ERROR: Could not restore build tools correctly
goto :EOF
