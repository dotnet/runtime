TOP=$(realpath $(CURDIR)/../../..)
-include Make.config

escape_quote = $(subst ",\",$(1))

ifneq ($(V),)
MSBUILD_ARGS+=/p:MonoVerboseBuild=true
endif

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
NATIVE_BIN_DIR?=$(BINDIR)/native/net7.0-Browser-$(CONFIG)-wasm
ICU_LIBDIR?=
SYSTEM_NATIVE_LIBDIR?=$(TOP)/src/native/libs/System.Native
_MSBUILD_WASM_BUILD_ARGS=/p:TargetOS=Browser /p:TargetArchitecture=wasm /p:Configuration=$(CONFIG)
XHARNESS_BROWSER?=chrome
EMCC_DEFAULT_RSP=$(NATIVE_BIN_DIR)/src/emcc-default.rsp
HELIX_TARGET_QUEUE?=Ubuntu.1804.Amd64.Open

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

ifeq ($(NOSTRIP),)
STRIP_CMD=&& $(EMSDK_PATH)/upstream/bin/wasm-opt --strip-dwarf $(NATIVE_BIN_DIR)/dotnet.wasm -o $(NATIVE_BIN_DIR)/dotnet.wasm
else
STRIP_CMD=
endif

#
# Wasm builds
#

$(NATIVE_BIN_DIR):
	mkdir -p $$@

$(NATIVE_BIN_DIR)/src:
	mkdir -p $$@

$(NATIVE_BIN_DIR)/include/wasm:
	mkdir -p $$@

$(BUILDS_OBJ_DIR):
	mkdir -p $$@

$(NATIVE_BIN_DIR)/dotnet.js: runtime/driver.c runtime/pinvoke.c runtime/pinvoke.h runtime/corebindings.c $(NATIVE_BIN_DIR)/src/cjs/runtime.cjs.iffe.js runtime/cjs/dotnet.cjs.lib.js $(SYSTEM_NATIVE_LIBDIR)/pal_random.lib.js $(MONO_LIBS) $(EMCC_DEFAULT_RSP) | $(NATIVE_BIN_DIR)
	$(DOTNET) build $(CURDIR)/wasm.proj $(_MSBUILD_WASM_BUILD_ARGS) /t:BuildWasmRuntimes $(MSBUILD_ARGS)

$(EMCC_DEFAULT_RSP): $(CURDIR)/wasm.proj | $(NATIVE_BIN_DIR)/src Makefile
	$(DOTNET) build $(CURDIR)/wasm.proj /p:Configuration=$(CONFIG) /t:GenerateEmccPropsAndRspFiles

$(NATIVE_BIN_DIR)/src/emcc-props.json: $(EMSDK_PATH)/upstream/.emsdk_version | $(NATIVE_BIN_DIR)/src
	$(DOTNET) build $(CURDIR)/wasm.proj /p:Configuration=$(CONFIG) /t:GenerateEmccPropsAndRspFiles

build-native: $(NATIVE_BIN_DIR)/dotnet.js $(NATIVE_BIN_DIR)/src/emcc-default.rsp $(NATIVE_BIN_DIR)/src/emcc-props.json

clean-emsdk:
	$(RM) -rf $(EMSDK_LOCAL_PATH)

clean:
	$(RM) -rf $(BUILDS_OBJ_DIR)

icu-files: $(wildcard $(ICU_LIBDIR)/*.dat) $(ICU_LIBDIR)/libicuuc.a $(ICU_LIBDIR)/libicui18n.a | $(NATIVE_BIN_DIR)
	cp $^ $(NATIVE_BIN_DIR)

source-files: runtime/driver.c runtime/pinvoke.c runtime/corebindings.c runtime/cjs/dotnet.cjs.lib.js $(SYSTEM_NATIVE_LIBDIR)/pal_random.lib.js | $(NATIVE_BIN_DIR)/src
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

build-debugger-tests-helix:
	$(DOTNET) build -restore -bl:$(TOP)/artifacts/log/$(CONFIG)/Wasm.Debugger.Tests.binlog \
	/p:ContinuousIntegrationBuild=true /p:ArchiveTests=true \
	$(TOP)/src/tests/BuildWasmApps/Wasm.Debugger.Tests/Wasm.Debugger.Tests.csproj \
	$(_MSBUILD_WASM_BUILD_ARGS) $(MSBUILD_ARGS)

submit-debugger-tests-helix: build-debugger-tests-helix
	EMSDK_PATH=$(EMSDK_PATH) BUILD_REASON=wasm-test SYSTEM_TEAMPROJECT=public BUILD_REPOSITORY_NAME=dotnet/runtime BUILD_SOURCEBRANCH=main \
		$(TOP)/eng/common/msbuild.sh --ci -restore $(TOP)/src/libraries/sendtohelix.proj \
		/p:TestRunNamePrefixSuffix=WasmDebugger /p:HelixBuild=`date "+%Y%m%d.%H%M"` /p:Creator=`whoami` \
		/bl:$(TOP)/artifacts/log/$(CONFIG)/SendToHelix.binlog -p:HelixTargetQueue=$(HELIX_TARGET_QUEUE) \
		/p:RuntimeFlavor=mono /p:TargetRuntimeIdentifier= /p:MonoForceInterpreter= /p:TestScope=innerloop \
		/p:_Scenarios=wasmdebuggertests \
		$(_MSBUILD_WASM_BUILD_ARGS) \
		$(MSBUILD_ARGS)

submit-tests-helix:
	echo "\n** This will submit all the available test zip files to helix **\n"
	EMSDK_PATH=$(EMSDK_PATH) BUILD_REASON=wasm-test SYSTEM_TEAMPROJECT=public BUILD_REPOSITORY_NAME=dotnet/runtime BUILD_SOURCEBRANCH=main \
		$(TOP)/eng/common/msbuild.sh --ci -restore $(TOP)/src/libraries/sendtohelix.proj \
		/p:TestRunNamePrefixSuffix=WasmTests /p:HelixBuild=`date "+%Y%m%d.%H%M"` /p:Creator=`whoami` \
		/bl:$(TOP)/artifacts/log/$(CONFIG)/SendToHelix.binlog -v:n -p:HelixTargetQueue=$(HELIX_TARGET_QUEUE) \
		/p:RuntimeFlavor=mono /p:TargetRuntimeIdentifier= /p:MonoForceInterpreter= /p:TestScope=innerloop \
		$(_MSBUILD_WASM_BUILD_ARGS) \
		$(MSBUILD_ARGS)

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
