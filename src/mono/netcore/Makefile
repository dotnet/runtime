
NETCORESDK_VERSION=3.0.100-preview4-010759
NETCORESDK_FILE=dotnet-sdk-$(NETCORESDK_VERSION)-osx-x64.tar.gz
URL:=https://dotnetcli.blob.core.windows.net/dotnet/Sdk/$(NETCORESDK_VERSION)/$(NETCORESDK_FILE)

$(NETCORESDK_FILE):
	curl $(URL) --output $(NETCORESDK_FILE)
	tar -xvf $(NETCORESDK_FILE)

build-sample:
	dotnet build sample/HelloWorld

run-sample:
	COMPlus_DebugWriteToStdErr=1 ./dotnet --fx-version "$(VERSION)" sample/HelloWorld/bin/Debug/netcoreapp3.0/HelloWorld.dll

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
	cp ../mcs/class/System.Private.CoreLib/bin/x64/System.Private.CoreLib.{dll,pdb} $(SHAREDRUNTIME)

prepare: $(NETCORESDK_FILE) link-mono

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

TEST_SUITES = \
	System.Runtime.Tests \
	System.Runtime.CompilerServices.Unsafe.Tests \
	System.Collections.Tests

# The binary directory under $(COREFIX_BINDIR)
System.Runtime.CompilerServices.Unsafe.Tests_PROFILE = netstandard-Debug
System.Runtime.Tests_PROFILE = netcoreapp-Unix-Debug
System.Runtime.Tests_XUNIT_ARGS = @../excludes-System.Runtime.Tests.rsp
System.Collections.Tests_PROFILE = netcoreapp-Debug
System.Reflection.Emit.Tests_PROFILE = netcoreapp-Debug
System.Reflection.Emit.Tests_XUNIT_ARGS = @../excludes-System.Reflection.Emit.Tests.rsp
System.Reflection.Emit.ILGeneration.Tests_PROFILE = netcoreapp-Debug
System.Reflection.Emit.Lightweight.Tests_PROFILE = netcoreapp-Debug
System.Threading.Tests_PROFILE = netcoreapp-Debug
System.Threading.Thread.Tests_PROFILE = netcoreapp-Debug

build-%: check-env
	cd gen-xunit-runner && dotnet run ../tests $(COREFX_ROOT) $(COREFX_BINDIR)/$*/$($*_PROFILE)/$*.dll -notrait category=nonosxtests -notrait category=failing -notrait category=Outerloop -notrait category=nonnetcoreapptests $($*_XUNIT_ARGS) $(XUNIT_ARGS)
	cp $(COREFX_BINDIR)/RemoteExecutorConsoleApp/netstandard-Debug/RemoteExecutorConsoleApp.* tests/$*
	cp $(COREFX_BINDIR)/System.Runtime.Tests/netcoreapp-Unix-Debug/TestLoadAssembly.dll tests/$*
	ln -sf $(CURDIR)/$(SHAREDRUNTIME)/System.Globalization.Native.dylib tests/$*
	sed -e 's/9.9.9/$(VERSION)/g' < tests/$*/RemoteExecutorConsoleApp.runtimeconfig.json > 2 && mv 2 tests/$*/RemoteExecutorConsoleApp.runtimeconfig.json
	cd tests/$* && dotnet build

run-%: check-env
	cd tests/$* && MONO_PATH=bin/Debug/netcoreapp3.0 MONO_ENV_OPTIONS="--debug --explicit-null-checks" COMPlus_DebugWriteToStdErr=1 ../../dotnet bin/Debug/netcoreapp3.0/$*-runner.dll

all: $(addprefix build-, $(TEST_SUITES))

check: $(addprefix run-, $(TEST_SUITES))

clean:
	rm -rf sdk shared host dotnet tests LICENSE.txt ThirdPartyNotices.txt $(NETCORESDK_FILE)

xtest-%: prepare check-env
	ln -sf $(CURDIR)/$(SHAREDRUNTIME)/System.Globalization.Native.dylib \
		$(COREFX_BINDIR)/testhost/netcoreapp-OSX-Debug-x64/shared/Microsoft.NETCore.App/9.9.9/
	COMPlus_DebugWriteToStdErr=1 ./dotnet --fx-version "$(VERSION)" \
		$(COREFX_BINDIR)/testhost/netcoreapp-OSX-Debug-x64/shared/Microsoft.NETCore.App/9.9.9/xunit.console.dll \
		$(COREFX_BINDIR)/$*/$($*_PROFILE)/$*.dll -notrait category=outerloop -notrait category=nonosxtests -notrait category=failing \
		-notrait category=nonnetcoreapptests -noappdomain -noshadow -parallel all -nunit TestResult-$*-netcore-xunit.xml \
		$(shell grep -v '^#\|^$$' excludes-$*.rsp) $(FIXTURE)

xtest-all: $(addprefix xtest-, $(TEST_SUITES))

FEED_BASE_URL = https://dotnetfeed.blob.core.windows.net/dotnet-core
TEST_ASSETS_URL = https://dotnetfeed.blob.core.windows.net/dotnet-core/corefx-tests/4.6.0-preview4.19156.10/OSX.x64/netcoreapp/corefx-test-assets.xml

corefx-test-assets.xml:
	wget $(TEST_ASSETS_URL)

dl-test-assets: corefx-test-assets.xml .stamp-dl-test-assets

.stamp-dl-test-assets:
	python dl-test-assets.py corefx-test-assets.xml $(FEED_BASE_URL) assets
	touch $@
