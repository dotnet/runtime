TOP=../../../../..

include ../wasm.mk

ifneq ($(AOT),)
override MSBUILD_ARGS+=/p:RunAOTCompilation=true
endif

ifneq ($(V),)
DOTNET_MONO_LOG_LEVEL=--setenv=MONO_LOG_LEVEL=debug
endif

PROJECT_NAME=Wasm.Console.Node.CJS.Sample.csproj
CONSOLE_DLL=Wasm.Console.Node.CJS.Sample.dll
MAIN_JS=main.cjs

run: run-console-node
