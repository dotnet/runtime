@if not defined _echo @echo off
setlocal EnableDelayedExpansion

if /I [%1] == [-?] goto Usage
if /I [%1] == [-help] goto Usage

@if [%1]==[] set __args=-p

 @call %~dp0run.cmd sync %__args% %*
@exit /b %ERRORLEVEL%

:Usage
echo.
echo Repository syncing script.
echo.
echo Options:
echo     -?     - Prints Usage
echo     -help  - Prints Usage
echo     -p     - Restores all nuget packages for repository
echo     -ab    - Downloads the latests product packages from Azure.
echo              The following properties are required:
echo                 -AzureAccount="Account name"
echo                 -AzureToken="Access token"
echo              To download a specific group of product packages, specify:
echo                 -BuildMajor
echo                 -BuildMinor
echo              To download from a specific container, specify:
echo                 -Container="container name"
echo              To download blobs starting with a specific prefix, specify:
echo                 -BlobNamePrefix="Blob name prefix"
echo              To specify which RID you are downloading binaries for (optional):
echo                 -RuntimeId="RID" (Needs to match what's in the container)
echo.
echo.
echo.
echo If no option is specified then sync.cmd -p is implied.