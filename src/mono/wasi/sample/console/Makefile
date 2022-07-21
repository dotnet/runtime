include ../../Makefile.variable
include ../SampleMakefile.variable

BIN_DIR=WasiConsoleApp/bin/Debug/net7.0

all: console.wasm $(BIN_DIR)/WasiConsoleApp.dll

run: all
	wasmtime --dir=. console.wasm

run-wasmer: all
	wasmer --dir=. console.wasm

console.wasm: main.c
	$(WASI_SDK_CLANG) main.c -o console.wasm $(COMPILE_FLAGS) $(LINK_FLAGS)

$(BIN_DIR)/WasiConsoleApp.dll: WasiConsoleApp/*.csproj WasiConsoleApp/*.cs
	cd WasiConsoleApp && $(DOTNET_ROOT)/dotnet build -c Debug
	touch $(BIN_DIR)/*.dll
	cp -R $(BROWSER_WASM_RUNTIME_PATH) $(BIN_DIR)/runtime

debug: all
	wasmtime --tcplisten localhost:64000 --dir=. console.wasm --env DEBUGGER_FD=4