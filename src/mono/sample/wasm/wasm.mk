DOTNET=$(TOP)/dotnet.sh

ifeq ($(V),)
DOTNET_Q_ARGS=--nologo -v:q -consoleloggerparameters:NoSummary
else
DOTNET_Q_ARGS=--nologo
endif

CONFIG?=Release

WASM_DEFAULT_BUILD_ARGS?=/p:TargetArchitecture=wasm /p:TargetOS=Browser /p:Configuration=$(CONFIG)

# if we're in a container, don't try to open the browser
ifneq ("$(wildcard /.dockerenv)", "")
  OPEN_BROWSER=
  V8_PATH=v8
else
  OPEN_BROWSER=-o
  V8_PATH=~/.jsvu/v8
endif

all: publish

build:
	EMSDK_PATH=$(realpath $(TOP)/src/mono/wasm/emsdk) $(DOTNET) build $(DOTNET_Q_ARGS) $(WASM_DEFAULT_BUILD_ARGS) $(MSBUILD_ARGS) $(PROJECT_NAME)

publish:
	EMSDK_PATH=$(realpath $(TOP)/src/mono/wasm/emsdk) $(DOTNET) publish $(DOTNET_Q_ARGS) $(WASM_DEFAULT_BUILD_ARGS) $(MSBUILD_ARGS) $(PROJECT_NAME)

clean:
	rm -rf bin $(TOP)/artifacts/obj/mono/$(PROJECT_NAME:%.csproj=%)

run-browser:
	if ! $(DOTNET) tool list --global | grep dotnet-serve && ! which dotnet-serve ; then \
		echo "The tool dotnet-serve could not be found. Install with: $(DOTNET) tool install --global dotnet-serve"; \
		exit 1; \
	else  \
		$(DOTNET) serve -d:bin/$(CONFIG)/AppBundle $(OPEN_BROWSER) -p:8000; \
	fi

run-console:
	cd bin/$(CONFIG)/AppBundle && $(V8_PATH) --stack-trace-limit=1000 --single-threaded --expose_wasm $(MAIN_JS) -- $(DOTNET_MONO_LOG_LEVEL) --run $(CONSOLE_DLL) $(ARGS)

run-console-node:
	cd bin/$(CONFIG)/AppBundle && node --stack-trace-limit=1000 --single-threaded --expose_wasm $(MAIN_JS) -- $(DOTNET_MONO_LOG_LEVEL) --run $(CONSOLE_DLL) $(ARGS)
