TOP=../../../../../..
DOTNET=$(TOP)/dotnet.sh

ifeq ($(V),)
DOTNET_Q_ARGS=--nologo -v:q -consoleloggerparameters:NoSummary
else
DOTNET_Q_ARGS=--nologo
endif

CONFIG?=Release

all: build

build:
	$(DOTNET) build $(DOTNET_Q_ARGS) /p:TargetArchitecture=wasm /p:TargetOS=Browser /p:Configuration=$(CONFIG) WasmSample.csproj

clean:
	rm -rf bin

run:
	cd bin/$(CONFIG)/publish && python3 server.py
