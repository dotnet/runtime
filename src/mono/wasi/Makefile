include Makefile.variable

all: build-all

build-all: $(WASI_SDK_CLANG)
	mkdir -p $(WASI_OBJ_DIR)
	cd $(WASI_OBJ_DIR) && \
	PATH=$(NINJA_DIR):${PATH} cmake -G Ninja \
		-DWASI_SDK_PREFIX=$(WASI_SDK_ROOT) \
		-DCMAKE_SYSROOT=$(WASI_SDK_ROOT)/share/wasi-sysroot \
		-DCMAKE_TOOLCHAIN_FILE=$(WASI_SDK_ROOT)/share/cmake/wasi-sdk.cmake \
		-DCMAKE_C_FLAGS="--sysroot=$(WASI_SDK_ROOT)/share/wasi-sysroot \
		-I$(CURDIR)/include -I$(TOP)/src/mono -I$(TOP)/src/native/public -I$(TOP)/src/mono/mono/eglib -I$(WASI_OBJ_DIR)/mono/eglib -I$(WASI_OBJ_DIR) -I$(TOP)/artifacts/obj/wasm -I$(TOP)/src/mono/wasm/runtime" \
		-DCMAKE_CXX_FLAGS="--sysroot=$(WASI_SDK_ROOT)/share/wasi-sysroot" \
		-DENABLE_MINIMAL=jit,sgen_major_marksweep_conc,sgen_split_nursery,sgen_gc_bridge,sgen_toggleref,sgen_debug_helpers,sgen_binary_protocol,logging,interpreter,threads,qcalls,debugger_agent,sockets,eventpipe \
		-DDISABLE_SHARED_LIBS=1 \
		-Wl,--allow-undefined \
		$(TOP)/src/mono
	cd $(WASI_OBJ_DIR) && PATH=$(NINJA_DIR):${PATH} ninja

	mkdir -p $(WASI_BIN_DIR)
	cp $(WASI_OBJ_DIR)/mono/mini/*.a $(WASI_OBJ_DIR)/libmono-wasi-driver.a $(WASI_BIN_DIR)
	rm -rf $(WASI_BIN_DIR)/libmono-component-hot_reload-static.a
	rm -rf $(WASI_BIN_DIR)/libmono-component-diagnostics_tracing-static.a
	mkdir -p $(WASI_BIN_DIR)/include/mono-wasi
	cp mono-wasi-driver/*.h $(WASI_BIN_DIR)/include/mono-wasi

$(WASI_SDK_CLANG):
	mkdir -p $(WASI_OBJ_DIR)
	cd $(WASI_OBJ_DIR) && \
		wget -q https://github.com/WebAssembly/wasi-sdk/releases/download/wasi-sdk-$(WASI_SDK_VERSION)/wasi-sdk-$(WASI_SDK_VERSION).0-linux.tar.gz && \
		tar xf wasi-sdk-*.tar.gz

.stamp-wasmtime-$(WASMTIME_VERSION):
	@echo "** Provisioning wasmtime $(WASMTIME_VERSION) **"
	rm -Rf $(WASMTIME_DIR) && \
	wget -q https://github.com/bytecodealliance/wasmtime/releases/download/$(WASMTIME_VERSION)/$(WASMTIME_DIR_NAME).tar.xz -O - | tar -C `dirname $(WASMTIME_DIR)` -Jxf - && \
	touch $@

.stamp-ninja-$(NINJA_VERSION):
	@echo "** Provisioning ninja $(NINJA_VERSION) **"
	rm -Rf $(NINJA_DIR); \
	mkdir $(NINJA_DIR) && \
	wget -q https://github.com/ninja-build/ninja/releases/download/v1.11.0/ninja-linux.zip -O $(NINJA_DIR)/ninja.zip && \
	(cd $(NINJA_DIR) && unzip -q ninja.zip && rm ninja.zip) && \
	touch $@

provision-deps: .stamp-wasmtime-$(WASMTIME_VERSION) .stamp-ninja-$(NINJA_VERSION)
	@echo "-------------------------------------------"
	@echo "** Installed wasmtime in $(WASMTIME_DIR)"
	@echo "** Installed ninja in $(NINJA_DIR)"
