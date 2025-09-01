param(
     [Parameter()]
     [string]$WasiSdkUrl,
     [Parameter()]
     [string]$WasiSdkVersion,
     [Parameter()]
     [string]$WasiSdkPath
)

Set-StrictMode -version 2.0
$ErrorActionPreference='Stop'
$ProgressPreference = 'SilentlyContinue'

Invoke-WebRequest -Uri $WasiSdkUrl -OutFile ./wasi-sdk-$WasiSdkVersion-x86_64-windows.tar.gz
tar --strip-components=1 -xzmf ./wasi-sdk-$WasiSdkVersion-x86_64-windows.tar.gz -C $WasiSdkPath
Remove-Item ./wasi-sdk-$WasiSdkVersion-x86_64-windows.tar.gz -fo
