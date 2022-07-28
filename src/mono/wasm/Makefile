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

EMSDK_PATH?=$(TOP)/src/mono/wasm/emsdk
CONFIG?=Release
BINDIR?=$(TOP)/artifacts/bin
OBJDIR?=$(TOP)/artifacts/obj
_MSBUILD_WASM_BUILD_ARGS=/p:TargetOS=Browser /p:TargetArchitecture=wasm /p:Configuration=$(CONFIG)
XHARNESS_BROWSER?=chrome
HELIX_TARGET_QUEUE?=Ubuntu.1804.Amd64.Open

all: build-all

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
	cd $(EMSDK_LOCAL_PATH) && ./emsdk install $(EMSCRIPTEN_VERSION)
	cd $(EMSDK_LOCAL_PATH) && ./emsdk activate $(EMSCRIPTEN_VERSION)
	python3 ./sanitize.py $(EMSDK_PATH)
	touch $@

provision-wasm: .stamp-wasm-install-and-select-$(EMSCRIPTEN_VERSION)
	@echo "----------------------------------------------------------"
	@echo "Installed emsdk into EMSDK_PATH=$(TOP)/src/mono/wasm/emsdk"

MONO_OBJ_DIR=$(OBJDIR)/mono/Browser.wasm.$(CONFIG)
BUILDS_OBJ_DIR=$(MONO_OBJ_DIR)/wasm

clean-emsdk:
	$(RM) -rf $(EMSDK_LOCAL_PATH)

#
# Helper targets
#

.PHONY: runtime
.PHONY: build

build:
	EMSDK_PATH=$(EMSDK_PATH) $(TOP)/build.sh mono+libs.pretest -os Browser -c $(CONFIG) --binaryLog /p:ContinueOnError=false /p:StopOnFirstFailure=true $(MSBUILD_ARGS)

build-all:
	EMSDK_PATH=$(EMSDK_PATH) $(TOP)/build.sh mono+libs -os Browser -c $(CONFIG) --binaryLog /p:ContinueOnError=false /p:StopOnFirstFailure=true $(MSBUILD_ARGS)

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

clean:
	$(RM) -rf $(BUILDS_OBJ_DIR)

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
	rm -f $(TOP)/artifacts/bin/DebuggerTestSuite/x64/Debug/*log; \
	if [ ! -z "$(TEST_FILTER)" ]; then \
	$(DOTNET) test  $(TOP)/src/mono/wasm/debugger/DebuggerTestSuite $(MSBUILD_ARGS) "-l:trx;LogFileName=DebuggerTestsResults.xml" --results-directory $(TOP)/artifacts/log/$(CONFIG) --filter "Category!=failing&FullyQualifiedName~$(TEST_FILTER)" $(TEST_ARGS); \
	else \
		$(DOTNET) test  $(TOP)/src/mono/wasm/debugger/DebuggerTestSuite $(MSBUILD_ARGS) "-l:trx;LogFileName=DebuggerTestsResults.xml" --results-directory $(TOP)/artifacts/log/$(CONFIG) --filter "Category!=failing" $(TEST_ARGS); \
	fi

build-dbg-proxy:
	$(DOTNET) build $(TOP)/src/mono/wasm/debugger/BrowserDebugHost $(MSBUILD_ARGS)
build-dbg-testsuite:
	$(DOTNET) build $(TOP)/src/mono/wasm/debugger/DebuggerTestSuite $(MSBUILD_ARGS)
