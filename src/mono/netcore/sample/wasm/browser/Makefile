TOP=../../../../../..
DOTNET=$(TOP)/dotnet.sh

ifeq ($(V),)
DOTNET_Q_ARGS=--nologo -v:q -consoleloggerparameters:NoSummary
else
DOTNET_Q_ARGS=--nologo
endif

ifneq ($(AOT),)
DOTNET_RUN_AOT_COMPILATION_ARGS=/p:RunAOTCompilation=true
endif

CONFIG?=Release

all: build

build:
	EMSDK_PATH=$(realpath $(TOP)/src/mono/wasm/emsdk) $(DOTNET) publish $(DOTNET_Q_ARGS) $(DOTNET_RUN_AOT_COMPILATION_ARGS) /p:Configuration=$(CONFIG) /p:TargetArchitecture=wasm /p:TargetOS=Browser $(MSBUILD_ARGS) Wasm.Browser.Sample.csproj

clean:
	rm -rf bin

run:
	if ! $(DOTNET) tool list --global | grep dotnet-serve; then \
		echo "The tool dotnet-serve could not be found. Install with: $(DOTNET) tool install --global dotnet-serve"; \
		exit 1; \
	else  \
		$(DOTNET) serve -d bin/$(CONFIG)/AppBundle -p 8000; \
	fi
