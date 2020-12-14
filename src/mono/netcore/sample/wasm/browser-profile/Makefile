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
ifneq ($(PROFILE),)
DOTNET_PROFILE_ARGS=/p:ProfileAOT=true
endif
endif

CONFIG?=Release

all: build

build:
	$(DOTNET) publish $(DOTNET_Q_ARGS) $(DOTNET_RUN_AOT_COMPILATION_ARGS) $(DOTNET_PROFILE_ARGS) /p:TargetArchitecture=wasm /p:TargetOS=Browser /p:Configuration=$(CONFIG) WasmSample.csproj

clean:
	rm -rf bin

run:
	if ! $(DOTNET) tool list --global | grep dotnet-serve; then \
		echo "The tool dotnet-serve could not be found. Install with: $(DOTNET) tool install --global dotnet-serve"; \
		exit 1; \
	else  \
		$(DOTNET) serve -d bin/$(CONFIG)/publish -p 8000; \
	fi
