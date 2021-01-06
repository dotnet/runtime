TOP=../../../../../..
DOTNET=$(TOP)/dotnet.sh

ifeq ($(V),)
DOTNET_Q_ARGS=--nologo -v:q -consoleloggerparameters:NoSummary
else
DOTNET_Q_ARGS=--nologo
DOTNET_MONO_LOG_LEVEL=--setenv=MONO_LOG_LEVEL=debug
endif

CONFIG?=Release

build-aot-profile:
	EMSDK_PATH=$(realpath $(TOP)/src/mono/wasm/emsdk) $(DOTNET) publish $(DOTNET_Q_ARGS) /p:EnableProfiler=true /p:WasmBuildNative=true /p:Configuration=$(CONFIG) /p:TargetArchitecture=wasm /p:TargetOS=Browser $(MSBUILD_ARGS) WasmSample.csproj

run-aot-profiled:	
ifeq ($(DOWNLOAD_DIR),)
	$(error DOWNLOAD_DIR is not set)
endif
	cp $(DOWNLOAD_DIR)/data.aotprofile bin/$(CONFIG)/AppBundle
	EMSDK_PATH=$(realpath $(TOP)/src/mono/wasm/emsdk) $(DOTNET) publish $(DOTNET_Q_ARGS) /p:RunAOTCompilation=true /p:AOTProfilePath=$(PWD)/bin/$(CONFIG)/AppBundle/data.aotprofile /p:BuildAOTProfiled=true /p:Configuration=$(CONFIG) /p:TargetArchitecture=wasm /p:TargetOS=Browser $(MSBUILD_ARGS) WasmSample.csproj

clean:
	rm -rf bin

run:
	if ! $(DOTNET) tool list --global | grep dotnet-serve; then \
		echo "The tool dotnet-serve could not be found. Install with: $(DOTNET) tool install --global dotnet-serve"; \
		exit 1; \
	else  \
		$(DOTNET) serve -d bin/$(CONFIG)/AppBundle -p 8000; \
	fi

get-aot-profile: build-aot-profile run

use-aot-profile: run-aot-profiled run
