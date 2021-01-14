TOP=../../../../../..

include ../wasm.mk

build:
	EMSDK_PATH=$(realpath $(TOP)/src/mono/wasm/emsdk) $(DOTNET) publish $(DOTNET_Q_ARGS) $(WASM_DEFAULT_BUILD_ARGS) $(MSBUILD_ARGS) /p:EnableProfiler=true /p:WasmBuildNative=true Wasm.BrowserProfile.Sample.csproj

run-aot-profiled:
ifeq ($(PROFILE_PATH),)
	$(error PROFILE_PATH is not set)
endif
	EMSDK_PATH=$(realpath $(TOP)/src/mono/wasm/emsdk) $(DOTNET) publish $(DOTNET_Q_ARGS) /p:RunAOTCompilation=true /p:AOTProfilePath=$(PROFILE_PATH) /p:BuildAOTProfiled=true /p:Configuration=$(CONFIG) /p:TargetArchitecture=wasm /p:TargetOS=Browser $(MSBUILD_ARGS) Wasm.BrowserProfile.Sample.csproj

get-aot-profile: build run

use-aot-profile: run-aot-profiled run
