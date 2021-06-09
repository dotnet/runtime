TOP=../../../../..

include ../wasm.mk

ifneq ($(AOT),)
override MSBUILD_ARGS+=/p:RunAOTCompilation=true
endif

PROJECT_NAME=Wasm.Browser.Sample.Bench.csproj

run: run-browser
