param(
     [Parameter()]
     [string]$WasiSdkUrl,
     [Parameter()]
     [string]$WasiSdkVersion
)

Remove-Item ./wasi-sdk/ -r -fo -ErrorAction SilentlyContinue
Remove-Item ./wasi-sdk-$WasiSdkVersion.0-mingw.tar.gz -fo
Invoke-WebRequest -Uri $WasiSdkUrl -OutFile ./wasi-sdk-$WasiSdkVersion.0-mingw.tar.gz
tar -xvzf ./wasi-sdk-$WasiSdkVersion.0-mingw.tar.gz
Remove-Item ./wasi-sdk-$WasiSdkVersion.0-mingw.tar.gz -fo
Move-Item ./wasi-sdk-$WasiSdkVersion.*/ ./wasi-sdk/
Copy-Item ./wasi-sdk-version.txt ./wasi-sdk/wasi-sdk-version.txt
