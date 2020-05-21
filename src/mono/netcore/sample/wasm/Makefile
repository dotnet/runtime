TOP=../../../../..

DOTNET_Q_ARGS=--nologo -v:q -consoleloggerparameters:NoSummary

all: build

build:
	$(TOP)/.dotnet/dotnet build $(DOTNET_Q_ARGS) /p:TargetArchitecture=wasm /p:TargetOS=Browser /p:Configuration=Release

clean:
	rm -rf bin

run:
	cd bin/Release/publish && ~/.jsvu/v8 --expose_wasm runtime.js -- --enable-gc --run WasmSample.dll

runtimepack:
	EMSDK_PATH=$(TOP)/src/mono/wasm/emsdk $(TOP)/build.sh -c $(MONO_CONFIG) -os Browser -arch wasm -subset Mono+Libs
