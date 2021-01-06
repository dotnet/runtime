TOP=../../../../../..
DOTNET=$(TOP)/dotnet.sh

ifeq ($(V),)
DOTNET_Q_ARGS=--nologo -v:q -consoleloggerparameters:NoSummary
else
DOTNET_Q_ARGS=--nologo
DOTNET_MONO_LOG_LEVEL=--setenv=MONO_LOG_LEVEL=debug
endif

ifneq ($(AOT),)
DOTNET_RUN_AOT_COMPILATION_ARGS=/p:RunAOTCompilation=true
endif

CONFIG?=Release

all: build

build:
	EMSDK_PATH=$(realpath $(TOP)/src/mono/wasm/emsdk) $(DOTNET) publish $(DOTNET_Q_ARGS) $(DOTNET_RUN_AOT_COMPILATION_ARGS) $(DOTNET_EXTRA_ARGS) /p:TargetArchitecture=wasm /p:TargetOS=Browser /p:Configuration=$(CONFIG) $(MSBUILD_ARGS) Wasm.Console.Sample.csproj

clean:
	rm -rf bin

run:
	cd bin/$(CONFIG)/AppBundle && ~/.jsvu/v8 --expose_wasm runtime.js -- $(DOTNET_MONO_LOG_LEVEL) --run Wasm.Console.Sample.dll
