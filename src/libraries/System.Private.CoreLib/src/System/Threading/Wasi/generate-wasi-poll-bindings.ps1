# Prerequisites:
#   powershell
#   tar
#   [cargo](https://rustup.rs/)
$ProgressPreference = 'SilentlyContinue'
$ErrorActionPreference='Stop'
$scriptpath = $MyInvocation.MyCommand.Path
$dir = Split-Path $scriptpath

Push-Location $dir


cargo install --locked --no-default-features --features csharp --version 0.30.0 wit-bindgen-cli
Invoke-WebRequest -Uri https://github.com/WebAssembly/wasi-http/archive/refs/tags/v0.2.1.tar.gz -OutFile v0.2.1.tar.gz
tar xzf v0.2.1.tar.gz
cp world.wit wasi-http-0.2.1/wit/world.wit
wit-bindgen c-sharp -w wasi-poll -r native-aot --internal --skip-support-files wasi-http-0.2.1/wit
rm -r wasi-http-0.2.1 
rm v0.2.1.tar.gz 

Pop-Location
