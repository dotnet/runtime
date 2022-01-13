TOP=$(realpath $(CURDIR)/../../..)
CONFIG?=Release
WASI_OBJ_DIR=$(TOP)/artifacts/obj/mono/Wasi.$(CONFIG)
WASI_BIN_DIR=$(TOP)/artifacts/bin/mono/Wasi.$(CONFIG)
WASI_SDK_ROOT=/home/steve/wasi-sdk-12.0

all: build-all

build-all:
	mkdir -p $(WASI_OBJ_DIR)
	cd $(WASI_OBJ_DIR) && \
	cmake -G Ninja \
		-DWASI_SDK_PREFIX=$(WASI_SDK_ROOT) \
		-DCMAKE_SYSROOT=$(WASI_SDK_ROOT)/share/wasi-sysroot/ \
		-DCMAKE_TOOLCHAIN_FILE=$(WASI_SDK_ROOT)/share/cmake/wasi-sdk.cmake \
		-DCMAKE_C_FLAGS="--sysroot=$(WASI_SDK_ROOT)/share/wasi-sysroot" \
		-DCMAKE_CXX_FLAGS="--sysroot=$(WASI_SDK_ROOT)/share/wasi-sysroot" \
		-DENABLE_MINIMAL=jit,sgen_major_marksweep_conc,sgen_split_nursery,sgen_gc_bridge,sgen_toggleref,sgen_debug_helpers,sgen_binary_protocol,logging,shared_perfcounters,interpreter,threads,qcalls,debugger_agent,sockets,eventpipe \
		-DDISABLE_SHARED_LIBS=1 \
		-Wl,--allow-undefined \
		$(TOP)/src/mono
	cd $(WASI_OBJ_DIR) && ninja

	mkdir -p $(WASI_BIN_DIR)
	cp $(WASI_OBJ_DIR)/mono/mini/*.a $(WASI_BIN_DIR)
