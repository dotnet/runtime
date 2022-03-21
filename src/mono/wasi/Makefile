include Makefile.variable

all: build-all

build-all: $(WASI_SDK_CLANG)
	mkdir -p $(WASI_OBJ_DIR)
	cd $(WASI_OBJ_DIR) && \
	cmake -G Ninja \
		-DWASI_SDK_PREFIX=$(WASI_SDK_ROOT) \
		-DCMAKE_SYSROOT=$(WASI_SDK_ROOT)/share/wasi-sysroot \
		-DCMAKE_TOOLCHAIN_FILE=$(WASI_SDK_ROOT)/share/cmake/wasi-sdk.cmake \
		-DCMAKE_C_FLAGS="--sysroot=$(WASI_SDK_ROOT)/share/wasi-sysroot -I$(CURDIR)/include -I$(TOP)/src/mono -I$(TOP)/src/native/public" \
		-DCMAKE_CXX_FLAGS="--sysroot=$(WASI_SDK_ROOT)/share/wasi-sysroot" \
		-DENABLE_MINIMAL=jit,sgen_major_marksweep_conc,sgen_split_nursery,sgen_gc_bridge,sgen_toggleref,sgen_debug_helpers,sgen_binary_protocol,logging,interpreter,threads,qcalls,debugger_agent,sockets,eventpipe \
		-DDISABLE_SHARED_LIBS=1 \
		-Wl,--allow-undefined \
		$(TOP)/src/mono
	cd $(WASI_OBJ_DIR) && ninja

	mkdir -p $(WASI_BIN_DIR)
	cp $(WASI_OBJ_DIR)/mono/mini/*.a $(WASI_OBJ_DIR)/libmono-wasi-driver.a $(WASI_BIN_DIR)
	mkdir -p $(WASI_BIN_DIR)/include/mono-wasi
	cp mono-wasi-driver/*.h $(WASI_BIN_DIR)/include/mono-wasi

$(WASI_SDK_CLANG):
	mkdir -p $(WASI_OBJ_DIR)
	cd $(WASI_OBJ_DIR) && \
		wget https://github.com/WebAssembly/wasi-sdk/releases/download/wasi-sdk-$(WASI_SDK_VERSION)/wasi-sdk-$(WASI_SDK_VERSION).0-linux.tar.gz && \
		tar xf wasi-sdk-*.tar.gz
