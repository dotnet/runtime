TOP=../../../../..

all: build

build:
	$(TOP)/.dotnet/dotnet build /p:TargetArchitecture=wasm /p:TargetOS=Browser /p:Configuration=Release

clean:
	rm -rf bin

run:
	cd bin/Release/publish && ~/.jsvu/v8 --expose_wasm runtime.js -- --run WasmSample.dll
