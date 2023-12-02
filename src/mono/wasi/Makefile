TOP=$(realpath $(CURDIR)/../../..)
-include Make.config

escape_quote = $(subst ",\",$(1))

ifneq ($(V),)
MSBUILD_ARGS+=/p:MonoVerboseBuild=true
MSBUILD_ARGS+=-bl
endif

DOTNET=$(TOP)/dotnet.sh

WASI_SDK_PATH?=$(TOP)/src/mono/wasi/wasi-sdk
CONFIG?=Release
BINDIR?=$(TOP)/artifacts/bin
OBJDIR?=$(TOP)/artifacts/obj
_MSBUILD_WASM_BUILD_ARGS=/p:TargetOS=wasi /p:TargetArchitecture=wasm /p:Configuration=$(CONFIG)
HELIX_TARGET_QUEUE?=Ubuntu.1804.Amd64.Open

all: build-all

MONO_OBJ_DIR=$(OBJDIR)/mono/wasi.wasm.$(CONFIG)
BUILDS_OBJ_DIR=$(MONO_OBJ_DIR)/wasm

#
# Helper targets
#

.PHONY: runtime
.PHONY: build

build:
	$(TOP)/build.sh mono+libs.pretest -os wasi -c $(CONFIG) --binaryLog /p:ContinueOnError=false /p:StopOnFirstFailure=true $(MSBUILD_ARGS)

build-all:
	$(TOP)/build.sh mono+libs+packs -os wasi -c $(CONFIG) --binaryLog /p:ContinueOnError=false /p:StopOnFirstFailure=true $(MSBUILD_ARGS)

runtime:
	$(TOP)/build.sh mono.runtime+mono.wasiruntime+libs.native+libs.pretest -os wasi -c $(CONFIG) /p:ContinueOnError=false /p:StopOnFirstFailure=true $(MSBUILD_ARGS)

# Rebuild only the mono runtime+cross compiler, don't build dotnet.wasm
mono-runtime:
	$(TOP)/build.sh mono.runtime+libs.native+libs.pretest -os wasi -c $(CONFIG) /p:ContinueOnError=false /p:StopOnFirstFailure=true $(MSBUILD_ARGS)

corlib:
	$(TOP)/build.sh mono.corelib+mono.wasiruntime+libs.pretest -os wasi -c $(CONFIG) /p:ContinueOnError=false /p:StopOnFirstFailure=true $(MSBUILD_ARGS)

test-runner:
	$(DOTNET) build $(TOP)/src/libraries/Common/tests/WasmTestRunner /p:Configuration=$(CONFIG) $(MSBUILD_ARGS)

app-builder:
	$(DOTNET) build $(TOP)/src/tasks/WasmAppBuilder

build-tasks:
	$(DOTNET) build $(TOP)/src/tasks/tasks.proj /p:Configuration=$(CONFIG) $(MSBUILD_ARGS)

build-packages:
	rm -f $(TOP)/artifacts/packages/$(CONFIG)/Shipping/*.nupkg
	WASI_SDK_PATH=$(WASI_SDK_PATH) $(TOP)/build.sh mono.packages+mono.manifests+packs.product -os wasi -c $(CONFIG) --binaryLog /p:ContinueOnError=false /p:StopOnFirstFailure=true $(MSBUILD_ARGS)

clean:
	$(RM) -rf $(BUILDS_OBJ_DIR)

run-tests-%:
	$(DOTNET) build $(TOP)/src/libraries/$*/tests/ /t:Test $(_MSBUILD_WASM_BUILD_ARGS) $(MSBUILD_ARGS)

run-build-tests:
	WASI_SDK_PATH=$(WASI_SDK_PATH) $(DOTNET) build $(TOP)/src/mono/wasi//Wasi.Build.Tests/ /t:Test /bl $(_MSBUILD_WASM_BUILD_ARGS) $(MSBUILD_ARGS)

build-debugger-tests-helix:
	$(DOTNET) build -restore -bl:$(TOP)/artifacts/log/$(CONFIG)/Wasm.Debugger.Tests.binlog \
	/p:ContinuousIntegrationBuild=true /p:ArchiveTests=true \
	$(TOP)/src/mono/wasm/debugger/Wasm.Debugger.Tests/Wasm.Debugger.Tests.csproj \
	$(_MSBUILD_WASM_BUILD_ARGS) $(MSBUILD_ARGS)

submit-debugger-tests-helix: build-debugger-tests-helix
	BUILD_REASON=wasm-test SYSTEM_TEAMPROJECT=public BUILD_REPOSITORY_NAME=dotnet/runtime BUILD_SOURCEBRANCH=main \
		$(TOP)/eng/common/msbuild.sh --ci -restore $(TOP)/src/libraries/sendtohelix.proj \
		/p:TestRunNamePrefixSuffix=WasmDebugger /p:HelixBuild=`date "+%Y%m%d.%H%M"` /p:Creator=`whoami` \
		/bl:$(TOP)/artifacts/log/$(CONFIG)/SendToHelix.binlog -p:HelixTargetQueue=$(HELIX_TARGET_QUEUE) \
		/p:RuntimeFlavor=mono /p:TargetRuntimeIdentifier= /p:MonoForceInterpreter= /p:TestScope=innerloop \
		/p:_Scenarios=wasmdebuggertests \
		$(_MSBUILD_WASM_BUILD_ARGS) \
		$(MSBUILD_ARGS)

submit-wbt-helix:
	PATH="$(JSVU):$(PATH)" \
		$(DOTNET) build $(TOP)/src/mono/wasi/Wasi.Build.Tests/ /v:m /p:ArchiveTests=true /t:ArchiveTests $(_MSBUILD_WASM_BUILD_ARGS) $(MSBUILD_ARGS) && \
	WASI_SDK_PATH=$(WASI_SDK_PATH) BUILD_REASON=wasm-test SYSTEM_TEAMPROJECT=public BUILD_REPOSITORY_NAME=dotnet/runtime BUILD_SOURCEBRANCH=main \
		$(TOP)/eng/common/msbuild.sh --ci -restore $(TOP)/src/libraries/sendtohelix.proj \
		/p:TestRunNamePrefixSuffix=WasiBuildTests /p:HelixBuild=`date "+%Y%m%d.%H%M"` /p:Creator=`whoami` \
		/bl:$(TOP)/artifacts/log/$(CONFIG)/SendToHelix.binlog -v:m -p:HelixTargetQueue=$(HELIX_TARGET_QUEUE) \
		/p:RuntimeFlavor=mono /p:TargetRuntimeIdentifier= /p:MonoForceInterpreter= /p:TestScope=innerloop \
		/p:_Scenarios=buildwasmapps \
		$(_MSBUILD_WASM_BUILD_ARGS) \
		$(MSBUILD_ARGS)

submit-tests-helix:
	echo "\n** This will submit all the available test zip files to helix **\n"
	BUILD_REASON=wasm-test SYSTEM_TEAMPROJECT=public BUILD_REPOSITORY_NAME=dotnet/runtime BUILD_SOURCEBRANCH=main \
		$(TOP)/eng/common/msbuild.sh --ci -restore $(TOP)/src/libraries/sendtohelix.proj \
		/p:TestRunNamePrefixSuffix=WasiTests /p:HelixBuild=`date "+%Y%m%d.%H%M"` /p:Creator=`whoami` \
		/bl:$(TOP)/artifacts/log/$(CONFIG)/SendToHelix.binlog -v:n -p:HelixTargetQueue=$(HELIX_TARGET_QUEUE) \
		/p:RuntimeFlavor=mono /p:TargetRuntimeIdentifier= /p:MonoForceInterpreter= /p:TestScope=innerloop \
		$(_MSBUILD_WASM_BUILD_ARGS) \
		$(MSBUILD_ARGS)

run-debugger-tests:
	rm -f $(TOP)/artifacts/bin/DebuggerTestSuite/x64/Debug/*log; \
	if [ ! -z "$(TEST_FILTER)" ]; then \
	$(DOTNET) test  $(TOP)/src/mono/wasm/debugger/DebuggerTestSuite $(MSBUILD_ARGS) "-l:trx;LogFileName=DebuggerTestsResults.xml" --results-directory $(TOP)/artifacts/log/$(CONFIG) --filter "category!=failing&FullyQualifiedName$(TEST_FILTER)" $(TEST_ARGS); \
	else \
		$(DOTNET) test  $(TOP)/src/mono/wasm/debugger/DebuggerTestSuite $(MSBUILD_ARGS) "-l:trx;LogFileName=DebuggerTestsResults.xml" --results-directory $(TOP)/artifacts/log/$(CONFIG) --filter "category!=failing" $(TEST_ARGS); \
	fi

build-dbg-proxy:
	$(DOTNET) build $(TOP)/src/mono/wasm/debugger/wasiDebugHost $(MSBUILD_ARGS)
build-dbg-testsuite:
	$(DOTNET) build $(TOP)/src/mono/wasm/debugger/DebuggerTestSuite $(MSBUILD_ARGS)
build-app-host:
	$(DOTNET) build $(TOP)/src/mono/wasm/host $(_MSBUILD_WASM_BUILD_ARGS) $(MSBUILD_ARGS)

patch-deterministic:
	cd emsdk/upstream/emscripten/ && patch -p1 < ../../../runtime/deterministic.diff
