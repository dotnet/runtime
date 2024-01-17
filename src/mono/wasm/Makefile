TOP=$(realpath $(CURDIR)/../../..)
-include Make.config

escape_quote = $(subst ",\",$(1))

ifneq ($(V),)
MSBUILD_ARGS+=/p:MonoVerboseBuild=true
MSBUILD_ARGS+=-bl
endif

MSBUILD_ARGS+=/p:KeepNativeSymbols=true

DOTNET=$(TOP)/dotnet.sh

EMSDK_PATH?=$(TOP)/src/mono/browser/emsdk
CONFIG?=Release
BINDIR?=$(TOP)/artifacts/bin
OBJDIR?=$(TOP)/artifacts/obj
_MSBUILD_WASM_BUILD_ARGS=/p:TargetOS=browser /p:TargetArchitecture=wasm /p:Configuration=$(CONFIG)
XHARNESS_BROWSER?=chrome
HELIX_TARGET_QUEUE?=Ubuntu.1804.Amd64.Open

LOG_PATH=$(TOP)/artifacts/log/$(CONFIG)

all: build-all

MONO_OBJ_DIR=$(OBJDIR)/mono/browser.wasm.$(CONFIG)
BUILDS_OBJ_DIR=$(MONO_OBJ_DIR)/wasm

#
# Helper targets
#

.PHONY: runtime
.PHONY: build

test-runner:
	$(DOTNET) build $(TOP)/src/libraries/Common/tests/WasmTestRunner /p:Configuration=$(CONFIG) $(MSBUILD_ARGS)

build-tasks:
	$(DOTNET) build $(TOP)/src/tasks/tasks.proj /p:Configuration=$(CONFIG) $(MSBUILD_ARGS)

clean:
	$(RM) -rf $(BUILDS_OBJ_DIR)

build-app-host:
	$(DOTNET) build $(TOP)/src/mono/wasm/host $(_MSBUILD_WASM_BUILD_ARGS) $(MSBUILD_ARGS)
