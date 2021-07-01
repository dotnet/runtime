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
_MSBUILD_WASM_BUILD_ARGS=/p:TargetOS=Browser /p:TargetArchitecture=wasm /p:Configuration=$(CONFIG)
XHARNESS_BROWSER?=chrome
EMCC_DEFAULT_RSP=$(NATIVE_BIN_DIR)/src/emcc-default.rsp

all: build-native icu-files source-files header-files

#
# EMSCRIPTEN SETUP
#
#  If EMSDK_PATH is not set by the caller, download and setup a local emsdk install.
#

EMSCRIPTEN_VERSION := $(shell cat $(TOP)/src/mono/wasm/emscripten-version.txt)
EMSDK_LOCAL_PATH=emsdk
EMCC=source $(EMSDK_PATH)/emsdk_env.sh 2>/dev/null && emcc

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

# FIXME: When https://github.com/dotnet/runtime/issues/54565 is fixed, and the WasmApp targets are updated to use mono runtime components, remove this
MONO_COMPONENT_LIBS= \
	$(MONO_BIN_DIR)/libmono-component-hot_reload-static.a \
	$(MONO_BIN_DIR)/libmono-component-debugger-static.a \
	$(MONO_BIN_DIR)/libmono-component-diagnostics_tracing-stub-static.a

MONO_OBJ_DIR=$(OBJDIR)/mono/Browser.wasm.$(CONFIG)
MONO_INCLUDE_DIR=$(MONO_BIN_DIR)/include/mono-2.0
BUILDS_OBJ_DIR=$(MONO_OBJ_DIR)/wasm
# libmonosgen-2.0 is in MONO_LIBS twice because the components and the runtime are depend on each other
MONO_LIBS = \
	$(MONO_BIN_DIR)/libmono-ee-interp.a \
	$(MONO_BIN_DIR)/libmonosgen-2.0.a \
	$(MONO_COMPONENT_LIBS) \
	$(MONO_BIN_DIR)/libmonosgen-2.0.a \
	$(MONO_BIN_DIR)/libmono-ilgen.a \
	$(MONO_BIN_DIR)/libmono-icall-table.a \
	$(MONO_BIN_DIR)/libmono-profiler-aot.a \
	${NATIVE_BIN_DIR}/libSystem.Native.a \
	${NATIVE_BIN_DIR}/libSystem.IO.Compression.Native.a \
	$(ICU_LIBDIR)/libicuuc.a \
	$(ICU_LIBDIR)/libicui18n.a

EMCC_DEBUG_FLAGS =-g -Os -s ASSERTIONS=1 -DDEBUG=1
EMCC_RELEASE_FLAGS=-Oz

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

$(NATIVE_BIN_DIR)/dotnet.js: $(BUILDS_OBJ_DIR)/driver.o $(BUILDS_OBJ_DIR)/pinvoke.o $(BUILDS_OBJ_DIR)/corebindings.o runtime/library_mono.js runtime/binding_support.js runtime/dotnet_support.js $(SYSTEM_NATIVE_LIBDIR)/pal_random.js $(2) $(EMCC_DEFAULT_RSP) | $(NATIVE_BIN_DIR)
	$(EMCC) @$(EMCC_DEFAULT_RSP) $(1) --js-library runtime/library_mono.js --js-library runtime/binding_support.js --js-library runtime/dotnet_support.js --js-library $(SYSTEM_NATIVE_LIBDIR)/pal_random.js $(BUILDS_OBJ_DIR)/driver.o $(BUILDS_OBJ_DIR)/pinvoke.o $(BUILDS_OBJ_DIR)/corebindings.o $(2) -o $(NATIVE_BIN_DIR)/dotnet.js $(3)

$(BUILDS_OBJ_DIR)/pinvoke-table.h: $(PINVOKE_TABLE) | $(BUILDS_OBJ_DIR)
	if cmp -s $(PINVOKE_TABLE) $$@ ; then : ; else cp $(PINVOKE_TABLE) $$@ ; fi

$(BUILDS_OBJ_DIR)/driver.o: runtime/driver.c $(EMCC_DEFAULT_RSP) | $(BUILDS_OBJ_DIR)
	$(EMCC) @$(EMCC_DEFAULT_RSP) $(1) -Oz -DCORE_BINDINGS -I$(BUILDS_OBJ_DIR) -I$(MONO_INCLUDE_DIR) runtime/driver.c -c -o $$@

$(BUILDS_OBJ_DIR)/pinvoke.o: runtime/pinvoke.c runtime/pinvoke.h $(BUILDS_OBJ_DIR)/pinvoke-table.h $(EMCC_DEFAULT_RSP) | $(BUILDS_OBJ_DIR)
	$(EMCC) @$(EMCC_DEFAULT_RSP) $(1) -Oz -DGEN_PINVOKE=1 -I$(BUILDS_OBJ_DIR) runtime/pinvoke.c -c -o $$@

$(BUILDS_OBJ_DIR)/corebindings.o: runtime/corebindings.c $(EMCC_DEFAULT_RSP) | $(BUILDS_OBJ_DIR)
	$(EMCC) @$(EMCC_DEFAULT_RSP) $(1) -Oz -I$(MONO_INCLUDE_DIR) runtime/corebindings.c -c -o $$@

$(EMCC_DEFAULT_RSP): $(CURDIR)/wasm.proj | $(NATIVE_BIN_DIR)/src Makefile
	$(DOTNET) build $(CURDIR)/wasm.proj /p:Configuration=$(CONFIG) /t:GenerateEmccPropsAndRspFiles

$(NATIVE_BIN_DIR)/src/Emcc.props: $(EMSDK_PATH)/upstream/.emsdk_version | $(NATIVE_BIN_DIR)/src
	$(DOTNET) build $(CURDIR)/wasm.proj /p:Configuration=$(CONFIG) /t:GenerateEmccPropsAndRspFiles

build-native: $(NATIVE_BIN_DIR)/dotnet.js $(NATIVE_BIN_DIR)/src/emcc-default.rsp $(NATIVE_BIN_DIR)/src/Emcc.props

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

header-files: runtime/pinvoke.h | $(NATIVE_BIN_DIR)/include/wasm
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

# Rebuild only the mono runtime+cross compiler, don't build dotnet.wasm
mono-runtime:
	EMSDK_PATH=$(EMSDK_PATH) $(TOP)/build.sh mono.runtime+libs.native+libs.pretest -os Browser -c $(CONFIG) /p:ContinueOnError=false /p:StopOnFirstFailure=true $(MSBUILD_ARGS)

corlib:
	EMSDK_PATH=$(EMSDK_PATH) $(TOP)/build.sh mono.corelib+mono.wasmruntime+libs.pretest -os Browser -c $(CONFIG) /p:ContinueOnError=false /p:StopOnFirstFailure=true $(MSBUILD_ARGS)

test-runner:
	$(DOTNET) build $(TOP)/src/libraries/Common/tests/WasmTestRunner /p:Configuration=$(CONFIG) $(MSBUILD_ARGS)

app-builder:
	$(DOTNET) build $(TOP)/src/tasks/WasmAppBuilder

build-tasks:
	$(DOTNET) build $(TOP)/src/tasks/WasmBuildTasks $(MSBUILD_ARGS)

run-tests-v8-%:
	EMSDK_PATH=$(EMSDK_PATH) PATH="$(JSVU):$(PATH)" $(DOTNET) build $(TOP)/src/libraries/$*/tests/ /t:Test $(_MSBUILD_WASM_BUILD_ARGS) /p:JSEngine=V8 $(MSBUILD_ARGS)
run-tests-sm-%:
	EMSDK_PATH=$(EMSDK_PATH) PATH="$(JSVU):$(PATH)" $(DOTNET) build $(TOP)/src/libraries/$*/tests/ /t:Test $(_MSBUILD_WASM_BUILD_ARGS) /p:JSEngine=SpiderMonkey $(MSBUILD_ARGS)
run-tests-jsc-%:
	EMSDK_PATH=$(EMSDK_PATH) PATH="$(JSVU):$(PATH)" $(DOTNET) build $(TOP)/src/libraries/$*/tests/ /t:Test $(_MSBUILD_WASM_BUILD_ARGS) /p:JSEngine=JavaScriptCore $(MSBUILD_ARGS)

run-tests-%:
	EMSDK_PATH=$(EMSDK_PATH) PATH="$(JSVU):$(PATH)" $(DOTNET) build $(TOP)/src/libraries/$*/tests/ /t:Test $(_MSBUILD_WASM_BUILD_ARGS) $(MSBUILD_ARGS)

run-build-tests:
	PATH="$(JSVU):$(PATH)" $(DOTNET) build $(TOP)/src/tests/BuildWasmApps/Wasm.Build.Tests/ /t:Test $(_MSBUILD_WASM_BUILD_ARGS) $(MSBUILD_ARGS)

run-browser-tests-%:
	PATH="$(GECKODRIVER):$(CHROMEDRIVER):$(PATH)" XHARNESS_COMMAND="test-browser --browser=$(XHARNESS_BROWSER)" $(DOTNET) build $(TOP)/src/libraries/$*/tests/ /t:Test $(_MSBUILD_WASM_BUILD_ARGS) $(MSBUILD_ARGS)

run-debugger-tests:
	if [ ! -z "$(TEST_FILTER)" ]; then \
		$(DOTNET) test  $(TOP)/src/mono/wasm/debugger/DebuggerTestSuite $(MSBUILD_ARGS) --filter FullyQualifiedName~$(TEST_FILTER) $(TEST_ARGS); \
	else \
		$(DOTNET) test  $(TOP)/src/mono/wasm/debugger/DebuggerTestSuite $(MSBUILD_ARGS) $(TEST_ARGS); \
	fi

build-dbg-proxy:
	$(DOTNET) build $(TOP)/src/mono/wasm/debugger/BrowserDebugHost $(MSBUILD_ARGS)
build-dbg-testsuite:
	$(DOTNET) build $(TOP)/src/mono/wasm/debugger/DebuggerTestSuite $(MSBUILD_ARGS)
