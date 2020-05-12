TOP=$(realpath $(CURDIR)/../../..)
-include Make.config

#
# These variables are set by wasm.targets
#
EMSDK_PATH?=emsdk
CONFIG?=Release
BINDIR?=$(TOP)/artifacts/bin
OBJDIR?=$(TOP)/artifacts/obj
PINVOKE_TABLE?=$(TOP)/artifacts/obj/mono/Browser.wasm.$(CONFIG)/wasm/pinvoke-table.h
MONO_BIN_DIR?=$(BINDIR)/mono/Browser.wasm.$(CONFIG)
SYS_NATIVE_DIR?=$(BINDIR)/native/net5.0-Browser-$(CONFIG)-wasm/

all: build-native

#
# EMSCRIPTEN SETUP
#
#  If EMSDK_PATH is not set by the caller, download and setup a local emsdk install.
#

EMSCRIPTEN_VERSION=1.39.11
EMSDK_LOCAL_PATH=emsdk
EMCC=source $(CURDIR)/emsdk_env.sh && emcc

.stamp-wasm-install-and-select-$(EMSCRIPTEN_VERSION):
	rm -rf $(EMSDK_LOCAL_PATH)
	git clone https://github.com/emscripten-core/emsdk.git $(EMSDK_LOCAL_PATH)
	cd $(EMSDK_LOCAL_PATH) && git checkout $(EMSCRIPTEN_VERSION)
	cd $(EMSDK_LOCAL_PATH) && ./emsdk install $(EMSCRIPTEN_VERSION)
	cd $(EMSDK_LOCAL_PATH) && ./emsdk activate --embedded $(EMSCRIPTEN_VERSION)
	touch $@

ifeq ($(EMSDK_PATH),emsdk)
provision-wasm: .stamp-wasm-install-and-select-$(EMSCRIPTEN_VERSION)
else
provision-wasm:
endif

# emsdk_env.sh calls emsdk construct_env which is a bit slow so make a copy
emsdk_env.sh: | provision-wasm
	cd $(EMSDK_PATH) && ./emsdk construct_env $(CURDIR)/emsdk_env.sh

MONO_OBJ_DIR=$(OBJDIR)/mono/Browser.wasm.$(CONFIG)
MONO_LIBS = $(MONO_BIN_DIR)/{libmono-ee-interp.a,libmono.a,libmono-ilgen.a,libmono-icall-table.a} ${SYS_NATIVE_DIR}/libSystem.Native.a
MONO_INCLUDE_DIR=$(MONO_BIN_DIR)/include/mono-2.0
BUILDS_BIN_DIR=$(MONO_BIN_DIR)/wasm/runtimes
BUILDS_OBJ_DIR=$(MONO_OBJ_DIR)/wasm/runtimes

EMCC_FLAGS=--profiling-funcs -s WASM=1 -s ALLOW_MEMORY_GROWTH=1 -s BINARYEN=1 -s ALIASING_FUNCTION_POINTERS=0 -s NO_EXIT_RUNTIME=1 -s "EXTRA_EXPORTED_RUNTIME_METHODS=['ccall', 'FS_createPath', 'FS_createDataFile', 'cwrap', 'setValue', 'getValue', 'UTF8ToString', 'addFunction']" -s "EXPORTED_FUNCTIONS=['_putchar']" --source-map-base http://example.com  -s WASM_OBJECT_FILES=0 -s FORCE_FILESYSTEM=1 -s USE_ZLIB=1
EMCC_DEBUG_FLAGS =-g -Os -s ASSERTIONS=1 -DENABLE_NETCORE=1
EMCC_RELEASE_FLAGS=-Oz --llvm-opts 2 --llvm-lto 1 -DENABLE_NETCORE=1

#
# Interpreter builds
#

# $(1) - name
# $(2) - runtime dir
# $(3) - EMCC_FLAGS
# $(4) - libs
define InterpBuildTemplate

$(BUILDS_BIN_DIR)/$(1)/:
	mkdir -p $$@

$(BUILDS_OBJ_DIR)/$(1)/:
	mkdir -p $$@

$(BUILDS_BIN_DIR)/$(1)/dotnet.js: $(BUILDS_OBJ_DIR)/$(1)/driver.o $(BUILDS_OBJ_DIR)/$(1)/corebindings.o runtime/library_mono.js runtime/binding_support.js runtime/dotnet_support.js $(5) | $(BUILDS_BIN_DIR)/$(1)/ emsdk_env.sh
	$(EMCC) $(EMCC_FLAGS) $(3) --js-library runtime/library_mono.js --js-library runtime/binding_support.js --js-library runtime/dotnet_support.js $(BUILDS_OBJ_DIR)/$(1)/driver.o $(BUILDS_OBJ_DIR)/$(1)/corebindings.o $(4) -o $(BUILDS_BIN_DIR)/$(1)/dotnet.js

$(BUILDS_OBJ_DIR)/$(1)/pinvoke-table.h: $(PINVOKE_TABLE) | $(BUILDS_OBJ_DIR)/$(1)/
	if cmp -s $(PINVOKE_TABLE) $$@ ; then : ; else cp $(PINVOKE_TABLE) $$@ ; fi

$(BUILDS_OBJ_DIR)/$(1)/driver.o: runtime/driver.c runtime/corebindings.c $(BUILDS_OBJ_DIR)/$(1)/pinvoke-table.h $(5) | $(BUILDS_OBJ_DIR)/$(1)/ emsdk_env.sh
	$(EMCC) $(EMCC_FLAGS) $(3) -Oz -DCORE_BINDINGS -I$(BUILDS_OBJ_DIR)/$(1) -I$(MONO_INCLUDE_DIR) runtime/driver.c -c -o $$@

$(BUILDS_OBJ_DIR)/$(1)/corebindings.o: runtime/corebindings.c | $(BUILDS_OBJ_DIR)/$(1)/ emsdk_env.sh
	$(EMCC) $(3) -Oz -I$(MONO_INCLUDE_DIR) runtime/corebindings.c -c -o $$@

build-native: $(BUILDS_BIN_DIR)/$(1)/dotnet.js

build-interp-$(1): $(BUILDS_BIN_DIR)/$(1)/dotnet.js

endef

$(eval $(call InterpBuildTemplate,debug,,$(EMCC_DEBUG_FLAGS),$(MONO_LIBS)))
$(eval $(call InterpBuildTemplate,release,,$(EMCC_RELEASE_FLAGS),$(MONO_LIBS)))

build:
	EMSDK_PATH=$(PWD)/wasm/emsdk ../../../.dotnet/dotnet build /p:TargetArchitecture=wasm /p:TargetOS=Browser /p:Configuration=Release

clean-emsdk:
	$(RM) -rf $(EMSDK_LOCAL_PATH)

clean:
	$(RM) -rf $(BUILDS_BIN_DIR) $(BUILDS_OBJ_DIR)
	$(RM) emsdk_env.sh
