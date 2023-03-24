TOP=../../../../..

include ../wasm.mk

PROJECT_NAME=Wasm.BrowserProfile.Sample.csproj
BUILD_ARGS=/p:WasmBuildNative=true
BUILD_PROFILED_ARGS=/p:RunAOTCompilation=true /p:AOTProfilePath=$(PROFILE_PATH)

run: run-browser

get-aot-profile: override MSBUILD_ARGS+=$(BUILD_ARGS)
get-aot-profile: build run

use-aot-profile: override MSBUILD_ARGS+=$(BUILD_PROFILED_ARGS)
use-aot-profile: build run
