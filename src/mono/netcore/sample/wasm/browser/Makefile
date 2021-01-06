TOP=../../../../../..

include ../wasm.mk

ifneq ($(AOT),)
DOTNET_PUBLISH_ARGS+=/p:RunAOTCompilation=true
endif

DOTNET_PUBLISH_ARGS+=Wasm.Browser.Sample.csproj
