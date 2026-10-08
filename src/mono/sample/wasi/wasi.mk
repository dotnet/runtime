DOTNET=$(TOP)/dotnet.sh

ifeq ($(V),)
DOTNET_Q_ARGS=--nologo -v:q -consoleloggerparameters:NoSummary -bl
else
DOTNET_Q_ARGS=--nologo -bl
endif

CONFIG?=Release

WASM_DEFAULT_BUILD_ARGS?=/p:TargetArchitecture=wasm /p:TargetOS=wasi /p:Configuration=$(CONFIG)
# Resolve wasmtime from the shared wasm tool cache (see eng/wasm/WasmToolCache.props).
WASMTIME_PROV_DIR=$(shell . $(TOP)/eng/wasm/wasm-tool-cache.sh && wasm_tool_cache_dir wasmtime $(TOP)/src/mono/wasi/wasmtime-version.txt $(TOP))
WASMTIME_PROV_PATH=${WASMTIME_PROV_DIR}/wasmtime

all: publish

build:
	$(DOTNET) build $(DOTNET_Q_ARGS) $(WASM_DEFAULT_BUILD_ARGS) $(MSBUILD_ARGS) $(PROJECT_NAME)

publish:
	$(DOTNET) publish $(DOTNET_Q_ARGS) $(WASM_DEFAULT_BUILD_ARGS) -p:WasmBuildOnlyAfterPublish=true $(MSBUILD_ARGS) $(PROJECT_NAME)

clean:
	rm -rf bin $(TOP)/artifacts/obj/mono/$(PROJECT_NAME:%.csproj=%)

run-console:
	@test -n "${WASMTIME_PROV_DIR}" || { echo "Error: wasmtime is not provisioned. Build a wasi target (e.g. './build.sh mono+libs -os wasi') to provision it, or set DOTNET_WASM_TOOL_CACHE_DIR to a cache that already has it."; exit 1; }
	cd bin/wasi-wasm/AppBundle && PATH="${WASMTIME_PROV_DIR}:${PATH}" ./run-wasmtime.sh $(ARGS)
