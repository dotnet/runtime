include ../../Makefile.variable
include ../SampleMakefile.variable

BIN_DIR=WasiConsoleApp/bin/Debug/net7.0

all: console.wasm $(BIN_DIR)/WasiConsoleApp.dll

run: all
	@(which wasmtime || PATH=$(WASMTIME_DIR):${PATH} which wasmtime); \
	test "$$?" -ne 0 \
		&& echo "wasmtime not found. Either install that yourself, or use 'make provision-deps' to install one." \
		|| PATH=$(WASMTIME_DIR):${PATH} wasmtime --dir=. console.wasm

run-wasmer: all
	wasmer --dir=. console.wasm

console.wasm: main.c
	$(WASI_SDK_CLANG) main.c -o console.wasm $(COMPILE_FLAGS) $(LINK_FLAGS)

$(BIN_DIR)/WasiConsoleApp.dll: WasiConsoleApp/*.csproj WasiConsoleApp/*.cs
	find $(BROWSER_WASM_RUNTIME_PATH) -type f
	cd WasiConsoleApp && $(DOTNET_ROOT)/dotnet build -c Debug
	touch $(BIN_DIR)/*.dll
	cp -R $(BROWSER_WASM_RUNTIME_PATH) $(BIN_DIR)/runtime

debug: all
#wasmtime has a bug when passing --tcplisten and --dir, to make it work we should use this PR https://github.com/bytecodealliance/wasmtime/pull/3995 and then the fd of the socket will be 4.
	PATH=$(WASMTIME_DIR):${PATH} wasmtime --tcplisten localhost:64000 --dir=. console.wasm --env DEBUGGER_FD=4
