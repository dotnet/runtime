
NETCORESDK_VERSION=3.0.100-preview4-010759
NETCORESDK_FILE=dotnet-sdk-$(NETCORESDK_VERSION)-osx-x64.tar.gz
URL:=https://dotnetcli.blob.core.windows.net/dotnet/Sdk/$(NETCORESDK_VERSION)/$(NETCORESDK_FILE)

$(NETCORESDK_FILE):
	curl $(URL) --output $(NETCORESDK_FILE)
	tar -xvf $(NETCORESDK_FILE)

build-sample:
	dotnet build HelloWorld

# COREHOST_TRACE=1 
VERSION:=3.0.0-preview4-27514-06
SHAREDRUNTIME:=shared/Microsoft.NETCore.App/$(VERSION)

bcl:
	$(MAKE) -C ../mcs/class/System.Private.CoreLib
	cp ../mcs/class/System.Private.CoreLib/bin/x64/System.Private.CoreLib.dll $(SHAREDRUNTIME)

runtime:
	$(MAKE) -C ../mono
	cp ../mono/mini/.libs/libmonosgen-2.0.dylib $(SHAREDRUNTIME)/libcoreclr.dylib

link-mono:
	cp ../mono/mini/.libs/libmonosgen-2.0.dylib $(SHAREDRUNTIME)/libcoreclr.dylib
	cp ../mcs/class/System.Private.CoreLib/bin/x64/System.Private.CoreLib.dll $(SHAREDRUNTIME)

prepare: $(NETCORESDK_FILE) link-mono

all: build-System.Runtime.Tests

COREFX_BINDIR=$(COREFX_ROOT)/artifacts/bin

check-env:
	@if [ "x$(COREFX_ROOT)" == "x" ]; then echo "Set COREFX_ROOT to the root of the fully built corefx repo."; exit 1; fi

#
# Running tests:
#  - Tests are run using dotnet bin/Debug/netcoreapp3.0/<...>.dll
# - One of the test helper assemblies contains pinvokes for System.Globalization.Native, so
#   the dylib has to be in the current working directory when running, since the runtime will
#   try to load the dylib from the same directory as the assembly which refers to it.
#   It has to be symlinked otherwise we load 2 copies of it which fails.
# - RemoteExecutorConsoleApp.exe needs to be in the current directory, since
#   its executed as: 'dotnet RemoteExecutorConsoleApp.exe <test assembly> ...'
# - MONO_PATH needs to be set to the bin dir so the remote executor can find the
#   test assembly.
# - The RemoteExecutorConsoleApp.runtimeconfig.json needs to be changed to use the proper
#   framework version.
#

build-System.Runtime.Tests: check-env
	cd gen-xunit-runner && dotnet run ../tests $(COREFX_ROOT) $(COREFX_BINDIR)/System.Runtime.Tests/netcoreapp-Unix-Debug/System.Runtime.Tests.dll -notrait category=nonosxtests -notrait category=failing -notrait category=Outerloop -notrait category=nonnetcoreapptests @../excludes-System.Runtime.Tests.rsp $(XUNIT_ARGS)
	cp $(COREFX_BINDIR)/RemoteExecutorConsoleApp/netstandard-Debug/RemoteExecutorConsoleApp.* tests/System.Runtime.Tests/
	cp $(COREFX_BINDIR)/System.Runtime.Tests/netcoreapp-Unix-Debug/TestLoadAssembly.dll tests/System.Runtime.Tests/
	ln -sf $(CURDIR)/$(SHAREDRUNTIME)/System.Globalization.Native.dylib tests/System.Runtime.Tests/
	sed -e 's/9.9.9/$(VERSION)/g' < tests/System.Runtime.Tests/RemoteExecutorConsoleApp.runtimeconfig.json > 2 && mv 2 tests/System.Runtime.Tests/RemoteExecutorConsoleApp.runtimeconfig.json
	cd tests/System.Runtime.Tests && dotnet build

run-System.Runtime.Tests: check-env
	cd tests/System.Runtime.Tests && MONO_PATH=bin/Debug/netcoreapp3.0 MONO_ENV_OPTIONS="--debug --explicit-null-checks" COMPlus_DebugWriteToStdErr=1 ../../dotnet bin/Debug/netcoreapp3.0/System.Runtime.Tests-runner.dll

clean:
	rm -rf sdk shared host dotnet LICENSE.txt ThirdPartyNotices.txt $(NETCORESDK_FILE)

