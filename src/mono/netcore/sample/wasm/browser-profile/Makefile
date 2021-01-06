TOP=../../../../../..

include ../wasm.mk

build:
	EMSDK_PATH=$(realpath $(TOP)/src/mono/wasm/emsdk) $(DOTNET) publish $(DOTNET_Q_ARGS) $(WASM_DEFAULT_BUILD_ARGS) $(MSBUILD_ARGS) /p:EnableProfiler=true /p:WasmBuildNative=true WasmSample.csproj

run-aot-profiled:	
ifeq ($(DOWNLOAD_DIR),)
	$(error DOWNLOAD_DIR is not set)
endif
	cp $(DOWNLOAD_DIR)/data.aotprofile bin/$(CONFIG)/AppBundle
	EMSDK_PATH=$(realpath $(TOP)/src/mono/wasm/emsdk) $(DOTNET) publish $(DOTNET_Q_ARGS) /p:RunAOTCompilation=true /p:AOTProfilePath=$(PWD)/bin/$(CONFIG)/AppBundle/data.aotprofile /p:BuildAOTProfiled=true /p:Configuration=$(CONFIG) /p:TargetArchitecture=wasm /p:TargetOS=Browser $(MSBUILD_ARGS) WasmSample.csproj

get-aot-profile: build run

use-aot-profile: run-aot-profiled run
