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

New-Item -Path $WasiSdkPath -ItemType "directory"
Invoke-WebRequest -Uri $WasiSdkUrl -OutFile ./wasi-sdk-$WasiSdkVersion.0-mingw.tar.gz
tar --strip-components=1 -xzf ./wasi-sdk-$WasiSdkVersion.0-mingw.tar.gz -C $WasiSdkPath
Copy-Item $WasiLocalPath/wasi-sdk-version.txt $WasiSdkPath/wasi-sdk-version.txt
Remove-Item ./wasi-sdk-$WasiSdkVersion.0-mingw.tar.gz -fo

# TODO https://github.com/dotnet/runtime/issues/104773
# Temporary WASI-SDK 22 workaround #2: The version of `wasm-component-ld` that
# ships with WASI-SDK 22 contains a
# [bug](https://github.com/bytecodealliance/wasm-component-ld/issues/22) which
# has been fixed in a v0.5.3 of that utility, so we upgrade it here.
Invoke-WebRequest -Uri https://github.com/bytecodealliance/wasm-component-ld/releases/download/v0.5.5/wasm-component-ld-v0.5.5-x86_64-windows.zip -OutFile wasm-component-ld-v0.5.5-x86_64-windows.zip
Expand-Archive -LiteralPath wasm-component-ld-v0.5.5-x86_64-windows.zip -DestinationPath .
Copy-Item wasm-component-ld-v0.5.5-x86_64-windows/wasm-component-ld.exe $WasiSdkPath/bin