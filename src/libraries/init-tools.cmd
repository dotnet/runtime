@if not defined _echo @echo off
setlocal

set INIT_TOOLS_LOG=%~dp0init-tools.log
if [%PACKAGES_DIR%]==[] set PACKAGES_DIR=%~dp0packages\
if [%TOOLRUNTIME_DIR%]==[] set TOOLRUNTIME_DIR=%~dp0Tools
set DOTNET_PATH=%TOOLRUNTIME_DIR%\dotnetcli\
if [%DOTNET_CMD%]==[] set DOTNET_CMD=%DOTNET_PATH%dotnet.exe
if [%BUILDTOOLS_SOURCE%]==[] set BUILDTOOLS_SOURCE=https://dotnet.myget.org/F/dotnet-buildtools/api/v3/index.json
set /P BUILDTOOLS_VERSION=< "%~dp0BuildToolsVersion.txt"
set BUILD_TOOLS_PATH=%PACKAGES_DIR%Microsoft.DotNet.BuildTools\%BUILDTOOLS_VERSION%\lib\
set INIT_TOOLS_RESTORE_PROJECT=%~dp0init-tools.msbuild
set BUILD_TOOLS_SEMAPHORE_DIR=%TOOLRUNTIME_DIR%\%BUILDTOOLS_VERSION%
set BUILD_TOOLS_SEMAPHORE=%BUILD_TOOLS_SEMAPHORE_DIR%\init-tools.completed

:: if force option is specified then clean the tool runtime and build tools package directory to force it to get recreated
if [%1]==[force] (
  if exist "%TOOLRUNTIME_DIR%" rmdir /S /Q "%TOOLRUNTIME_DIR%"
  if exist "%PACKAGES_DIR%Microsoft.DotNet.BuildTools" rmdir /S /Q "%PACKAGES_DIR%Microsoft.DotNet.BuildTools"
)

:: If semaphore exists do nothing
if exist "%BUILD_TOOLS_SEMAPHORE%" (
  echo Tools are already initialized.
  goto :EOF
)

if exist "%TOOLRUNTIME_DIR%" rmdir /S /Q "%TOOLRUNTIME_DIR%"

if exist "%DotNetBuildToolsDir%" (
  echo Using tools from '%DotNetBuildToolsDir%'.
  mklink /j "%TOOLRUNTIME_DIR%" "%DotNetBuildToolsDir%"

  if not exist "%DOTNET_CMD%" (
    echo ERROR: Ensure that '%DotNetBuildToolsDir%' contains the .NET Core SDK at '%DOTNET_PATH%'
    exit /b 1
  )

  echo Done initializing tools.
  if NOT exist "%BUILD_TOOLS_SEMAPHORE_DIR%" mkdir "%BUILD_TOOLS_SEMAPHORE_DIR%"
  echo Using tools from '%DotNetBuildToolsDir%'. > "%BUILD_TOOLS_SEMAPHORE%"
  exit /b 0
)

echo Running %0 > "%INIT_TOOLS_LOG%"

set /p DOTNET_VERSION=< "%~dp0DotnetCLIVersion.txt"
if exist "%DOTNET_CMD%" goto :afterdotnetrestore

echo Installing dotnet cli...
if NOT exist "%DOTNET_PATH%" mkdir "%DOTNET_PATH%"
set DOTNET_ZIP_NAME=dotnet-sdk-%DOTNET_VERSION%-win-x64.zip
set DOTNET_REMOTE_PATH=https://dotnetcli.azureedge.net/dotnet/Sdk/%DOTNET_VERSION%/%DOTNET_ZIP_NAME%
set DOTNET_LOCAL_PATH=%DOTNET_PATH%%DOTNET_ZIP_NAME%
echo Installing '%DOTNET_REMOTE_PATH%' to '%DOTNET_LOCAL_PATH%' >> "%INIT_TOOLS_LOG%"
powershell -NoProfile -ExecutionPolicy unrestricted -Command "$retryCount = 0; $success = $false; $proxyCredentialsRequired = $false; do { try { $wc = New-Object Net.WebClient; if ($proxyCredentialsRequired) { [Net.WebRequest]::DefaultWebProxy.Credentials = [Net.CredentialCache]::DefaultNetworkCredentials; } $wc.DownloadFile('%DOTNET_REMOTE_PATH%', '%DOTNET_LOCAL_PATH%'); $success = $true; } catch { if ($retryCount -ge 6) { throw; } else { $we = $_.Exception.InnerException -as [Net.WebException]; $proxyCredentialsRequired = ($we -ne $null -and ([Net.HttpWebResponse]$we.Response).StatusCode -eq [Net.HttpStatusCode]::ProxyAuthenticationRequired); Start-Sleep -Seconds (5 * $retryCount); $retryCount++; } } } while ($success -eq $false); Add-Type -Assembly 'System.IO.Compression.FileSystem' -ErrorVariable AddTypeErrors; if ($AddTypeErrors.Count -eq 0) { [System.IO.Compression.ZipFile]::ExtractToDirectory('%DOTNET_LOCAL_PATH%', '%DOTNET_PATH%') } else { (New-Object -com shell.application).namespace('%DOTNET_PATH%').CopyHere((new-object -com shell.application).namespace('%DOTNET_LOCAL_PATH%').Items(),16) }" >> "%INIT_TOOLS_LOG%"
if NOT exist "%DOTNET_LOCAL_PATH%" (
  echo ERROR: Could not install dotnet cli correctly. 1>&2
  goto :error
)

:afterdotnetrestore

if exist "%BUILD_TOOLS_PATH%" goto :afterbuildtoolsrestore
echo Restoring BuildTools version %BUILDTOOLS_VERSION%...
echo Running: "%DOTNET_CMD%" restore "%INIT_TOOLS_RESTORE_PROJECT%" --no-cache --packages %PACKAGES_DIR% --source "%BUILDTOOLS_SOURCE%" /p:BuildToolsPackageVersion=%BUILDTOOLS_VERSION% /p:ToolsDir=%TOOLRUNTIME_DIR% >> "%INIT_TOOLS_LOG%"
call "%DOTNET_CMD%" restore "%INIT_TOOLS_RESTORE_PROJECT%" --no-cache --packages %PACKAGES_DIR% --source "%BUILDTOOLS_SOURCE%" /p:BuildToolsPackageVersion=%BUILDTOOLS_VERSION% /p:ToolsDir=%TOOLRUNTIME_DIR% >> "%INIT_TOOLS_LOG%"
if NOT exist "%BUILD_TOOLS_PATH%init-tools.cmd" (
  echo ERROR: Could not restore build tools correctly. 1>&2
  goto :error
)

:afterbuildtoolsrestore

:: Ask init-tools to also restore ILAsm
set /p ILASMCOMPILER_VERSION=< "%~dp0tools-local\ILAsmVersion.txt"

echo Initializing BuildTools...
echo Running: "%BUILD_TOOLS_PATH%init-tools.cmd" "%~dp0" "%DOTNET_CMD%" "%TOOLRUNTIME_DIR%" >> "%INIT_TOOLS_LOG%"
call "%BUILD_TOOLS_PATH%init-tools.cmd" "%~dp0" "%DOTNET_CMD%" "%TOOLRUNTIME_DIR%" >> "%INIT_TOOLS_LOG%"
set INIT_TOOLS_ERRORLEVEL=%ERRORLEVEL%
if not [%INIT_TOOLS_ERRORLEVEL%]==[0] (
  echo ERROR: An error occured when trying to initialize the tools. 1>&2
  goto :error
)

:: Create semaphore file
echo Done initializing tools.
if NOT exist "%BUILD_TOOLS_SEMAPHORE_DIR%" mkdir "%BUILD_TOOLS_SEMAPHORE_DIR%"
echo Init-Tools.cmd completed for BuildTools Version: %BUILDTOOLS_VERSION% > "%BUILD_TOOLS_SEMAPHORE%"
exit /b 0

:error
echo Please check the detailed log that follows. 1>&2
type "%INIT_TOOLS_LOG%" 1>&2
exit /b 1
