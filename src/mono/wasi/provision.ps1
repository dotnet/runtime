param(
     [Parameter()]
     [string]$WasiSdkUrl,
     [Parameter()]
     [string]$WasiSdkVersion,
     [Parameter()]
     [string]$WasiSdkPath,
     [Parameter()]
     [string]$WasiLocalPath
)

Set-StrictMode -version 2.0
$ErrorActionPreference='Stop'
$ProgressPreference = 'SilentlyContinue'

New-Item -Path $WasiSdkPath -ItemType "directory"
Invoke-WebRequest -Uri $WasiSdkUrl -OutFile ./wasi-sdk-$WasiSdkVersion.0-x86_64-windows.tar.gz
tar --strip-components=1 -xzf ./wasi-sdk-$WasiSdkVersion.0-x86_64-windows.tar.gz -C $WasiSdkPath
Copy-Item $WasiLocalPath/wasi-sdk-version.txt $WasiSdkPath/wasi-sdk-version.txt
Remove-Item ./wasi-sdk-$WasiSdkVersion.0-x86_64-windows.tar.gz -fo
