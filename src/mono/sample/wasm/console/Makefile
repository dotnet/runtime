TOP=../../../../..
CULTURE=en-us

include ../wasm.mk

ifneq ($(AOT),)
override MSBUILD_ARGS+=/p:RunAOTCompilation=true
endif

ifneq ($(V),)
DOTNET_MONO_LOG_LEVEL=--setenv=MONO_LOG_LEVEL=debug
endif

ifneq ($(SHARD),)
override MSBUILD_ARGS+=/p:EnableSharding=true /p:ICUDefaultCulture=$(CULTURE)
endif

PROJECT_NAME=Wasm.Console.Sample.csproj

run: run-console
