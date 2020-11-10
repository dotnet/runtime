TOP=../../../../../..
DOTNET=$(TOP)/dotnet.sh

ifeq ($(V),)
DOTNET_Q_ARGS=--nologo -v:q -consoleloggerparameters:NoSummary
else
DOTNET_Q_ARGS=--nologo
DOTNET_MONO_LOG_LEVEL=--setenv=MONO_LOG_LEVEL=debug
endif

ifneq ($(USE_AOT),)
DOTNET_USE_AOT_ARGS=/p:UseAot=true
endif

CONFIG?=Release

all: build

build:
	$(DOTNET) publish $(DOTNET_Q_ARGS) $(DOTNET_USE_AOT_ARGS) /p:TargetArchitecture=wasm /p:TargetOS=Browser /p:Configuration=$(CONFIG) WasmSample.csproj

clean:
	rm -rf bin

run:
	cd bin/$(CONFIG)/AppBundle && ~/.jsvu/v8 --expose_wasm runtime.js -- $(DOTNET_MONO_LOG_LEVEL) --run WasmSample.dll
