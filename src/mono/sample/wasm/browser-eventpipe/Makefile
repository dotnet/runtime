TOP=../../../../..

include ../wasm.mk

override MSBUILD_ARGS+=/p:WasmEnablePerfTracing=true

ifneq ($(AOT),)
override MSBUILD_ARGS+=/p:RunAOTCompilation=true
endif

PROJECT_NAME=Wasm.Browser.EventPipe.Sample.csproj

run: run-browser
