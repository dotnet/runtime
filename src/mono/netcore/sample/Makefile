all: run

URL:=https://dotnetcli.blob.core.windows.net/dotnet/Sdk/3.0.100-preview4-010272/dotnet-sdk-3.0.100-preview4-010272-osx-x64.tar.gz

download-macos:
	curl $(URL) --output dotnet-sdk-3.tar.gz
	tar -xvf dotnet-sdk-3.tar.gz

build-sample:
	dotnet build HelloWorld

# COREHOST_TRACE=1 
VERSION:=3.0.0-preview-27408-5
SHAREDRUNTIME:=shared/Microsoft.NETCore.App/$(VERSION)

link-mono:
	cp ../../mono/mini/.libs/libmonosgen-2.0.dylib $(SHAREDRUNTIME)/libcoreclr.dylib
	cp ../../mcs/class/System.Private.CoreLib/bin/x64/System.Private.CoreLib.dll $(SHAREDRUNTIME)

prepare: download-macos build-sample link-mono 

run:
	@./dotnet --fx-version "$(VERSION)" HelloWorld/bin/Debug/netcoreapp3.0/HelloWorld.dll

clean:
	rm -rf sdk shared host dotnet LICENCE.txt ThirdPartyNotices.txt
