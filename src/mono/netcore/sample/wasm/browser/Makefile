TOP=../../../../../..

DOTNET_Q_ARGS=--nologo -v:q -consoleloggerparameters:NoSummary

CONFIG?=Release

all: build

build:
	$(TOP)/.dotnet/dotnet build $(DOTNET_Q_ARGS) /p:TargetArchitecture=wasm /p:TargetOS=Browser /p:Configuration=$(CONFIG) WasmSample.csproj

clean:
	rm -rf bin

run:
	cd bin/$(CONFIG)/publish && python server.py