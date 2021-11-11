TOP=../../../../../..

include ../../wasm.mk

ifneq ($(AOT),)
override MSBUILD_ARGS+=/p:RunAOTCompilation=true
endif

PROJECT_NAME=Wasm.Console.Bench.Sample.csproj
CONSOLE_DLL=Wasm.Console.Bench.Sample.dll

run: run-console
