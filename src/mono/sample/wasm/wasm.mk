DOTNET=$(TOP)/dotnet.sh

ifeq ($(V),)
DOTNET_Q_ARGS=--nologo -v:q -consoleloggerparameters:NoSummary
else
DOTNET_Q_ARGS=--nologo
endif

CONFIG?=Release

WASM_DEFAULT_BUILD_ARGS?=/p:TargetArchitecture=wasm /p:TargetOS=Browser /p:Configuration=$(CONFIG)

all: build

build:
	EMSDK_PATH=$(realpath $(TOP)/src/mono/wasm/emsdk) $(DOTNET) publish $(DOTNET_Q_ARGS) $(WASM_DEFAULT_BUILD_ARGS) $(MSBUILD_ARGS) $(PROJECT_NAME)

clean:
	rm -rf bin $(TOP)/artifacts/obj/mono/$(PROJECT_NAME:%.csproj=%)

run-browser:
	if ! $(DOTNET) tool list --global | grep dotnet-serve; then \
		echo "The tool dotnet-serve could not be found. Install with: $(DOTNET) tool install --global dotnet-serve"; \
		exit 1; \
	else  \
		$(DOTNET) serve -d:bin/$(CONFIG)/AppBundle -p:8000; \
	fi

run-console:
	cd bin/$(CONFIG)/AppBundle && ~/.jsvu/v8 --stack-trace-limit=1000 --single-threaded --expose_wasm runtime.js -- $(DOTNET_MONO_LOG_LEVEL) --run Wasm.Console.Sample.dll
