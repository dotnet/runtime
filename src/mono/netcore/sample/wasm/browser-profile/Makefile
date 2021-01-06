TOP=../../../../../..

include ../wasm.mk

build:
	EMSDK_PATH=$(realpath $(TOP)/src/mono/wasm/emsdk) $(DOTNET) publish $(DOTNET_Q_ARGS) $(WASM_DEFAULT_BUILD_ARGS) $(MSBUILD_ARGS) /p:EnableProfiler=true /p:WasmBuildNative=true WasmSample.csproj

run-aot-profiled:
	cp $(HOME)/Downloads/data.aotprofile ./bin/$(CONFIG)/AppBundle
	EMSDK_PATH=$(realpath $(TOP)/src/mono/wasm/emsdk) $(DOTNET) publish $(DOTNET_Q_ARGS) $(WASM_DEFAULT_BUILD_ARGS) $(MSBUILD_ARGS) /p:EnableProfiler=false /p:AOTProfilePath=./bin/$(CONFIG)/AppBundle/data.aotprofile /p:BuildAOTProfiled=true WasmSample.csproj

get-aot-profile: build run

use-aot-profile: run-aot-profiled run
