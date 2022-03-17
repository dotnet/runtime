TOP=../../../../../..

include ../../wasm.mk

ifneq ($(AOT),)
override MSBUILD_ARGS+=/p:RunAOTCompilation=true
endif

PROJECT_NAME=Wasm.Console.Bench.Sample.csproj
CONSOLE_DLL=Wasm.Console.Bench.Sample.dll
MAIN_JS=test-main.js
ARGS=--run $(CONSOLE_DLL)

run: run-console
