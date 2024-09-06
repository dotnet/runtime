TOP=../../../../..

include ../wasm.mk

PROJECT_NAME=Wasm.BrowserLogProfile.Sample.csproj
BUILD_ARGS=/p:WasmBuildNative=true

run: run-browser

get-aot-profile: override MSBUILD_ARGS+=$(BUILD_ARGS)
get-aot-profile: build run