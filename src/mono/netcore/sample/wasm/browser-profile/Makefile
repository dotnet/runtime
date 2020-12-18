# TODO: clean this up a bit
TOP=../../../../../..
DOTNET=$(TOP)/dotnet.sh

ifeq ($(V),)
DOTNET_Q_ARGS=--nologo -v:q -consoleloggerparameters:NoSummary
else
DOTNET_Q_ARGS=--nologo
DOTNET_MONO_LOG_LEVEL=--setenv=MONO_LOG_LEVEL=debug
endif

ifneq ($(AOT),)
DOTNET_RUN_AOT_COMPILATION_ARGS=/p:RunAOTCompilation=true /p:EnableProfiler=false
ifneq ($(PROFILE),)
DOTNET_PROFILE_ARGS=/p:AOTProfilePath=bin/$(CONFIG)/AppBundle/data.aotprofile /p:BuildAOTProfiled=true
endif
else
ifneq ($(PROFILE),)
DOTNET_PROFILE_ARGS=/p:EnableProfiler=true /p:WasmBuildNative=true
endif
endif

CONFIG?=Release

all: build

build:
	EMSDK_PATH=$(realpath $(TOP)/src/mono/wasm/emsdk) $(DOTNET) publish $(DOTNET_Q_ARGS) $(DOTNET_RUN_AOT_COMPILATION_ARGS) $(DOTNET_PROFILE_ARGS) /p:Configuration=$(CONFIG) /p:TargetArchitecture=wasm /p:TargetOS=Browser $(MSBUILD_ARGS) WasmSample.csproj

build-aot-profiled:
	cp $(HOME)/Downloads/data.aotprofile ./bin/$(CONFIG)/AppBundle
	EMSDK_PATH=$(realpath $(TOP)/src/mono/wasm/emsdk) $(DOTNET) publish $(DOTNET_Q_ARGS) /p:RunAOTCompilation=true /p:EnableProfiler=false /p:AOTProfilePath=$(PWD)/bin/$(CONFIG)/AppBundle/data.aotprofile /p:BuildAOTProfiled=true /p:Configuration=$(CONFIG) /p:TargetArchitecture=wasm /p:TargetOS=Browser $(MSBUILD_ARGS) WasmSample.csproj

clean:
	rm -rf bin

run:
	if ! $(DOTNET) tool list --global | grep dotnet-serve; then \
		echo "The tool dotnet-serve could not be found. Install with: $(DOTNET) tool install --global dotnet-serve"; \
		exit 1; \
	else  \
		$(DOTNET) serve -d bin/$(CONFIG)/AppBundle -p 8000; \
	fi
