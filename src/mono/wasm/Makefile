TOP=$(realpath $(CURDIR)/../../..)
-include Make.config

escape_quote = $(subst ",\",$(1))

DOTNET=$(TOP)/dotnet.sh
JSVU=$(HOME)/.jsvu
CHROMEDRIVER?=$(HOME)/.chromedriver
GECKODRIVER?=$(HOME)/.geckodriver

#
# These variables are set by wasm.proj
#
EMSDK_PATH?=$(TOP)/src/mono/wasm/emsdk
CONFIG?=Release
BINDIR?=$(TOP)/artifacts/bin
OBJDIR?=$(TOP)/artifacts/obj
PINVOKE_TABLE?=$(TOP)/artifacts/obj/wasm/pinvoke-table.h
MONO_BIN_DIR?=$(BINDIR)/mono/Browser.wasm.$(CONFIG)
NATIVE_BIN_DIR?=$(BINDIR)/native/net6.0-Browser-$(CONFIG)-wasm
ICU_LIBDIR?=
SYSTEM_NATIVE_LIBDIR?=$(TOP)/src/libraries/Native/Unix/System.Native
ENABLE_ES6?=false
_MSBUILD_WASM_BUILD_ARGS=/p:TargetOS=Browser /p:TargetArchitecture=wasm /p:Configuration=$(CONFIG)
ENABLE_METADATA_UPDATE?=false
XHARNESS_BROWSER?=chrome

all: build-native icu-files source-files header-files

#
# EMSCRIPTEN SETUP
#
#  If EMSDK_PATH is not set by the caller, download and setup a local emsdk install.
#

EMSCRIPTEN_VERSION=2.0.12
EMSDK_LOCAL_PATH=emsdk
EMCC=source $(EMSDK_PATH)/emsdk_env.sh && emcc

.stamp-wasm-install-and-select-$(EMSCRIPTEN_VERSION):
	rm -rf $(EMSDK_LOCAL_PATH)
	git clone https://github.com/emscripten-core/emsdk.git $(EMSDK_LOCAL_PATH)
	cd $(EMSDK_LOCAL_PATH) && git checkout $(EMSCRIPTEN_VERSION)
	cd $(EMSDK_LOCAL_PATH) && ./emsdk install $(EMSCRIPTEN_VERSION)
	cd $(EMSDK_LOCAL_PATH) && ./emsdk activate $(EMSCRIPTEN_VERSION)
	touch $@

provision-wasm: .stamp-wasm-install-and-select-$(EMSCRIPTEN_VERSION)
	@echo "----------------------------------------------------------"
	@echo "Installed emsdk into EMSDK_PATH=$(TOP)/src/mono/wasm/emsdk"

MONO_OBJ_DIR=$(OBJDIR)/mono/Browser.wasm.$(CONFIG)
MONO_INCLUDE_DIR=$(MONO_BIN_DIR)/include/mono-2.0
BUILDS_OBJ_DIR=$(MONO_OBJ_DIR)/wasm
MONO_LIBS = \
	$(MONO_BIN_DIR)/libmono-ee-interp.a \
	$(MONO_BIN_DIR)/libmonosgen-2.0.a \
	$(MONO_BIN_DIR)/libmono-ilgen.a \
	$(MONO_BIN_DIR)/libmono-icall-table.a \
	$(MONO_BIN_DIR)/libmono-profiler-aot.a \
	${NATIVE_BIN_DIR}/libSystem.Native.a \
	${NATIVE_BIN_DIR}/libSystem.IO.Compression.Native.a \
	$(ICU_LIBDIR)/libicuuc.a \
	$(ICU_LIBDIR)/libicui18n.a

EMCC_FLAGS=--profiling-funcs -s ALLOW_MEMORY_GROWTH=1 -s NO_EXIT_RUNTIME=1 -s USE_ZLIB=1  -s FORCE_FILESYSTEM=1 -s "EXTRA_EXPORTED_RUNTIME_METHODS=['ccall', 'FS_createPath', 'FS_createDataFile', 'cwrap', 'setValue', 'getValue', 'UTF8ToString', 'UTF8ArrayToString', 'addFunction']" -s "EXPORTED_FUNCTIONS=['_putchar']" --source-map-base http://example.com  -emit-llvm
ifneq ($(ENABLE_ES6),false)
    EMCC_FLAGS+=-s MODULARIZE=1 -s EXPORT_ES6=1
endif
ifeq ($(ENABLE_METADATA_UPDATE),true)
    EMCC_FLAGS+=-DENABLE_METADATA_UPDATE=1
endif

EMCC_DEBUG_FLAGS =-g -Os -s ASSERTIONS=1 -DDEBUG=1
EMCC_RELEASE_FLAGS=-Oz --llvm-opts 2

ifeq ($(NOSTRIP),)
STRIP_CMD=&& $(EMSDK_PATH)/upstream/bin/wasm-opt --strip-dwarf $(NATIVE_BIN_DIR)/dotnet.wasm -o $(NATIVE_BIN_DIR)/dotnet.wasm
else
STRIP_CMD=
endif

#
# Wasm builds
#

# $(1) - EMCC_FLAGS
# $(2) - libs
# $(3) - strip cmd
define WasmBuildTemplate

$(NATIVE_BIN_DIR):
	mkdir -p $$@

$(NATIVE_BIN_DIR)/src:
	mkdir -p $$@

$(NATIVE_BIN_DIR)/include/wasm:
	mkdir -p $$@

$(BUILDS_OBJ_DIR):
	mkdir -p $$@

$(NATIVE_BIN_DIR)/dotnet.js: $(BUILDS_OBJ_DIR)/driver.o $(BUILDS_OBJ_DIR)/pinvoke.o $(BUILDS_OBJ_DIR)/corebindings.o runtime/library_mono.js runtime/binding_support.js runtime/dotnet_support.js $(SYSTEM_NATIVE_LIBDIR)/pal_random.js $(2) | $(NATIVE_BIN_DIR)
	$(EMCC) $(EMCC_FLAGS) $(1) --js-library runtime/library_mono.js --js-library runtime/binding_support.js --js-library runtime/dotnet_support.js --js-library $(SYSTEM_NATIVE_LIBDIR)/pal_random.js $(BUILDS_OBJ_DIR)/driver.o $(BUILDS_OBJ_DIR)/pinvoke.o $(BUILDS_OBJ_DIR)/corebindings.o $(2) -o $(NATIVE_BIN_DIR)/dotnet.js $(3)

$(BUILDS_OBJ_DIR)/pinvoke-table.h: $(PINVOKE_TABLE) | $(BUILDS_OBJ_DIR)
	if cmp -s $(PINVOKE_TABLE) $$@ ; then : ; else cp $(PINVOKE_TABLE) $$@ ; fi

$(BUILDS_OBJ_DIR)/driver.o: runtime/driver.c | $(BUILDS_OBJ_DIR)
	$(EMCC) $(EMCC_FLAGS) $(1) -Oz -DCORE_BINDINGS -I$(BUILDS_OBJ_DIR) -I$(MONO_INCLUDE_DIR) runtime/driver.c -c -o $$@

$(BUILDS_OBJ_DIR)/pinvoke.o: runtime/pinvoke.c runtime/pinvoke.h $(BUILDS_OBJ_DIR)/pinvoke-table.h | $(BUILDS_OBJ_DIR)
	$(EMCC) $(EMCC_FLAGS) $(1) -Oz -DGEN_PINVOKE=1 -I$(BUILDS_OBJ_DIR) runtime/pinvoke.c -c -o $$@

$(BUILDS_OBJ_DIR)/corebindings.o: runtime/corebindings.c | $(BUILDS_OBJ_DIR)
	$(EMCC) $(EMCC_FLAGS) $(1) -Oz -I$(MONO_INCLUDE_DIR) runtime/corebindings.c -c -o $$@

$(NATIVE_BIN_DIR)/src/emcc-flags.txt: | $(NATIVE_BIN_DIR)/src Makefile
	echo "$(call escape_quote,$(EMCC_FLAGS)) $(1) -Oz" > $$@

$(NATIVE_BIN_DIR)/src/emcc-version.txt: $(EMSDK_PATH)/upstream/.emsdk_version | $(NATIVE_BIN_DIR)/src
	$(EMCC) --version | head -1 > $$@

build-native: $(NATIVE_BIN_DIR)/dotnet.js $(NATIVE_BIN_DIR)/src/emcc-flags.txt $(NATIVE_BIN_DIR)/src/emcc-version.txt

endef

ifeq ($(CONFIG),Debug)
$(eval $(call WasmBuildTemplate,$(EMCC_DEBUG_FLAGS),$(MONO_LIBS),))
else
$(eval $(call WasmBuildTemplate,$(EMCC_RELEASE_FLAGS),$(MONO_LIBS),$(STRIP_CMD)))
endif

clean-emsdk:
	$(RM) -rf $(EMSDK_LOCAL_PATH)

clean:
	$(RM) -rf $(BUILDS_OBJ_DIR)

icu-files: $(wildcard $(ICU_LIBDIR)/*.dat) $(ICU_LIBDIR)/libicuuc.a $(ICU_LIBDIR)/libicui18n.a | $(NATIVE_BIN_DIR)
	cp $^ $(NATIVE_BIN_DIR)

source-files: runtime/driver.c runtime/pinvoke.c runtime/corebindings.c runtime/binding_support.js runtime/dotnet_support.js runtime/library_mono.js $(SYSTEM_NATIVE_LIBDIR)/pal_random.js | $(NATIVE_BIN_DIR)/src
	cp $^ $(NATIVE_BIN_DIR)/src

header-files: runtime/pinvoke.h $(BUILDS_OBJ_DIR)/pinvoke-table.h | $(NATIVE_BIN_DIR)/include/wasm
	cp $^ $(NATIVE_BIN_DIR)/include/wasm

# Helper targets
.PHONY: runtime
.PHONY: build

build:
	EMSDK_PATH=$(EMSDK_PATH) $(TOP)/build.sh mono+libs.pretest -os Browser -c $(CONFIG) /p:ContinueOnError=false /p:StopOnFirstFailure=true $(MSBUILD_ARGS)

build-all:
	EMSDK_PATH=$(EMSDK_PATH) $(TOP)/build.sh mono+libs -os Browser -c $(CONFIG) /p:ContinueOnError=false /p:StopOnFirstFailure=true $(MSBUILD_ARGS)

runtime:
	EMSDK_PATH=$(EMSDK_PATH) $(TOP)/build.sh mono.runtime+mono.wasmruntime+libs.native+libs.pretest -os Browser -c $(CONFIG) /p:ContinueOnError=false /p:StopOnFirstFailure=true $(MSBUILD_ARGS)

corlib:
	EMSDK_PATH=$(EMSDK_PATH) $(TOP)/build.sh mono.corelib+mono.wasmruntime+libs.pretest -os Browser -c $(CONFIG) /p:ContinueOnError=false /p:StopOnFirstFailure=true $(MSBUILD_ARGS)

test-runner:
	$(DOTNET) build $(TOP)/src/libraries/Common/tests/WasmTestRunner /p:Configuration=$(CONFIG) $(MSBUILD_ARGS)

app-builder:
	$(DOTNET) build $(TOP)/src/tasks/WasmAppBuilder

build-tasks:
	$(DOTNET) build $(TOP)/src/tasks/WasmBuildTasks $(MSBUILD_ARGS)

run-tests-v8-%:
	PATH="$(JSVU):$(PATH)" $(DOTNET) build $(TOP)/src/libraries/$*/tests/ /t:Test $(_MSBUILD_WASM_BUILD_ARGS) /p:JSEngine=V8 $(MSBUILD_ARGS)
run-tests-sm-%:
	PATH="$(JSVU):$(PATH)" $(DOTNET) build $(TOP)/src/libraries/$*/tests/ /t:Test $(_MSBUILD_WASM_BUILD_ARGS) /p:JSEngine=SpiderMonkey $(MSBUILD_ARGS)
run-tests-jsc-%:
	PATH="$(JSVU):$(PATH)" $(DOTNET) build $(TOP)/src/libraries/$*/tests/ /t:Test $(_MSBUILD_WASM_BUILD_ARGS) /p:JSEngine=JavaScriptCore $(MSBUILD_ARGS)

run-tests-%:
	PATH="$(JSVU):$(PATH)" $(DOTNET) build $(TOP)/src/libraries/$*/tests/ /t:Test $(_MSBUILD_WASM_BUILD_ARGS) $(MSBUILD_ARGS)

run-browser-tests-%:
	PATH="$(GECKODRIVER):$(CHROMEDRIVER):$(PATH)" XHARNESS_COMMAND="test-browser --browser=$(XHARNESS_BROWSER)" $(DOTNET) build $(TOP)/src/libraries/$*/tests/ /t:Test $(_MSBUILD_WASM_BUILD_ARGS) $(MSBUILD_ARGS)

run-debugger-tests:
	if [ ! -z "$(TEST_FILTER)" ]; then \
		export LC_ALL=en_US.UTF-8; \
		$(DOTNET) test  $(TOP)/src/mono/wasm/debugger/DebuggerTestSuite $(MSBUILD_ARGS) --filter FullyQualifiedName~$(TEST_FILTER); \
		unset LC_ALL; \
	else \
		export LC_ALL=en_US.UTF-8; \
		$(DOTNET) test  $(TOP)/src/mono/wasm/debugger/DebuggerTestSuite $(MSBUILD_ARGS) $(TEST_ARGS); \
		unset LC_ALL; \
	fi

build-dbg-proxy:
	$(DOTNET) build $(TOP)/src/mono/wasm/debugger/BrowserDebugHost $(MSBUILD_ARGS)
build-dbg-testsuite:
	$(DOTNET) build $(TOP)/src/mono/wasm/debugger/DebuggerTestSuite $(MSBUILD_ARGS)
