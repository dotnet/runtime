TOP=../../../../../..

DOTNET_Q_ARGS=--nologo -v:q -consoleloggerparameters:NoSummary

CONFIG?=Release

all: build

build:
	$(TOP)/.dotnet/dotnet build $(DOTNET_Q_ARGS) /p:TargetArchitecture=wasm /p:TargetOS=Browser /p:Configuration=$(CONFIG) WasmSample.csproj

clean:
	rm -rf bin

run:
	cd bin/$(CONFIG)/publish && ~/.jsvu/v8 --expose_wasm runtime.js -- --run WasmSample.dll

runtimepack:
	EMSDK_PATH=$(abspath $(TOP)/src/mono/wasm/emsdk) $(TOP)/build.sh -c $(CONFIG) -os Browser -arch wasm -subset Mono+Libs
