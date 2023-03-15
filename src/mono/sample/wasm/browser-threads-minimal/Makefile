TOP=../../../../..

include ../wasm.mk

override MSBUILD_ARGS+=/p:WasmEnableThreads=true

ifneq ($(AOT),)
override MSBUILD_ARGS+=/p:RunAOTCompilation=true
endif

PROJECT_NAME=Wasm.Browser.Threads.Minimal.Sample.csproj

run: run-browser
