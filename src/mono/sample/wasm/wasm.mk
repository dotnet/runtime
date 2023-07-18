DOTNET=$(TOP)/dotnet.sh

ifeq ($(V),)
DOTNET_Q_ARGS=--nologo -v:q -consoleloggerparameters:NoSummary -bl
else
DOTNET_Q_ARGS=--nologo -bl
endif

CONFIG?=Release

WASM_DEFAULT_BUILD_ARGS?=/p:TargetArchitecture=wasm /p:TargetOS=browser /p:Configuration=$(CONFIG)

# we set specific headers to enable SharedArrayBuffer support in browsers for threading: https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/SharedArrayBuffer#security_requirements
CORS_HEADERS?= -h "Cross-Origin-Opener-Policy:same-origin" -h "Cross-Origin-Embedder-Policy:require-corp"

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
	$(DOTNET) build $(DOTNET_Q_ARGS) $(WASM_DEFAULT_BUILD_ARGS) $(MSBUILD_ARGS) $(PROJECT_NAME)

publish:
	$(DOTNET) publish $(DOTNET_Q_ARGS) $(WASM_DEFAULT_BUILD_ARGS) -p:WasmBuildOnlyAfterPublish=true $(MSBUILD_ARGS) $(PROJECT_NAME)

clean:
	rm -rf bin $(TOP)/artifacts/obj/mono/$(PROJECT_NAME:%.csproj=%)

run-browser:
	if ! $(DOTNET) tool list --global | grep dotnet-serve && ! which dotnet-serve ; then \
		echo "The tool dotnet-serve could not be found. Install with: $(DOTNET) tool install --global dotnet-serve"; \
		exit 1; \
	else  \
		$(DOTNET) serve -d:bin/$(CONFIG)/AppBundle $(CORS_HEADERS) $(OPEN_BROWSER) -p:8000; \
	fi

run-console:
	cd bin/$(CONFIG)/AppBundle && $(V8_PATH) --stack-trace-limit=1000 --single-threaded --expose_wasm --module $(MAIN_JS) -- $(ARGS)

run-console-node:
	cd bin/$(CONFIG)/AppBundle && node --stack-trace-limit=1000 --single-threaded --expose_wasm $(MAIN_JS) $(ARGS)

debug-console-node:
	cd bin/$(CONFIG)/AppBundle && node --inspect=9222 --stack-trace-limit=1000 --single-threaded --expose_wasm $(MAIN_JS) $(ARGS)