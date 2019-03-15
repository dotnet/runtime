all: build-sample

NETCORESDK_VERSION=3.0.100-preview4-010759
URL:=https://dotnetcli.blob.core.windows.net/dotnet/Sdk/$(NETCORESDK_VERSION)/dotnet-sdk-$(NETCORESDK_VERSION)-osx-x64.tar.gz

download-macos:
	curl $(URL) --output dotnet-sdk-3.tar.gz
	tar -xvf dotnet-sdk-3.tar.gz

build-sample:
	dotnet build HelloWorld

# COREHOST_TRACE=1 
VERSION:=3.0.0-preview4-27514-06
SHAREDRUNTIME:=shared/Microsoft.NETCore.App/$(VERSION)

link-mono:
	cp ../../mono/mini/.libs/libmonosgen-2.0.dylib $(SHAREDRUNTIME)/libcoreclr.dylib
	cp ../../mcs/class/System.Private.CoreLib/bin/x64/System.Private.CoreLib.dll $(SHAREDRUNTIME)

prepare: download-macos build-sample link-mono 

run:
	COMPlus_DebugWriteToStdErr=1 ./dotnet --fx-version "$(VERSION)" HelloWorld/bin/Debug/netcoreapp3.0/HelloWorld.dll

clean:
	rm -rf sdk shared host dotnet LICENCE.txt ThirdPartyNotices.txt
