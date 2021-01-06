TOP=../../../../../..

include ../wasm.mk

ifneq ($(AOT),)
DOTNET_PUBLISH_ARGS+=/p:RunAOTCompilation=true
endif

ifneq ($(V),)
DOTNET_MONO_LOG_LEVEL=--setenv=MONO_LOG_LEVEL=debug
endif

DOTNET_PUBLISH_ARGS+=Wasm.Console.Sample.csproj

run:
	cd bin/$(CONFIG)/AppBundle && ~/.jsvu/v8 --expose_wasm runtime.js -- $(DOTNET_MONO_LOG_LEVEL) --run Wasm.Console.Sample.dll
