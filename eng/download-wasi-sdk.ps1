param(
     [Parameter()]
     [string]$WasiSdkUrl,
     [Parameter()]
     [string]$WasiSdkVersion,
     [Parameter()]
     [string]$WasiSdkPath
)

Set-StrictMode -version 2.0
$ProgressPreference = 'SilentlyContinue'

$ContinueArgs = @()
for ($i = 0; $i -lt 8; $i++)
{
    curl.exe -L -o wasi-sdk-$WasiSdkVersion-x86_64-windows.tar.gz $WasiSdkUrl @ContinueArgs
    if ($LastExitCode -eq 0)
    {
        break;
    }
    if ($LastExitCode -ne 18) # "end of response with <number> bytes missing"
    {
        exit $LastExitCode
    }
    $ContinueArgs = "--continue-at", "-"
}

$ErrorActionPreference='Stop'
tar --strip-components=1 -xzmf ./wasi-sdk-$WasiSdkVersion-x86_64-windows.tar.gz -C $WasiSdkPath
Remove-Item ./wasi-sdk-$WasiSdkVersion-x86_64-windows.tar.gz -fo
