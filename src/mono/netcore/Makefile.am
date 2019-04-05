#
# NETCORETESTS_VERSION and NETCOREAPP_VERSION must be updated in sync, we are using coreclr repo for that but that's fine for now
#

# Extracted MicrosoftPrivateCoreFxNETCoreAppVersion from https://github.com/dotnet/coreclr/blob/master/eng/Versions.props#L11
NETCORETESTS_VERSION := 4.6.0-preview4.19202.4

# Extracted MicrosoftNETCoreAppVersion from https://github.com/dotnet/coreclr/blob/master/eng/Versions.props#L13
NETCOREAPP_VERSION := 3.0.0-preview4-27529-11

NETCORESDK_FILE := dotnet-runtime-$(NETCOREAPP_VERSION)-osx-x64.tar.gz
URL := https://dotnetcli.blob.core.windows.net/dotnet/Runtime/$(NETCOREAPP_VERSION)/$(NETCORESDK_FILE)

$(NETCORESDK_FILE):
	curl $(URL) --output $(NETCORESDK_FILE)
	tar -xvf $(NETCORESDK_FILE)

build-sample:
	dotnet build sample/HelloWorld

run-sample:
	COMPlus_DebugWriteToStdErr=1 ./dotnet --fx-version "$(NETCOREAPP_VERSION)" sample/HelloWorld/bin/Debug/netcoreapp3.0/HelloWorld.dll

# COREHOST_TRACE=1 
SHAREDRUNTIME := shared/Microsoft.NETCore.App/$(NETCOREAPP_VERSION)

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

nupkg:
	nuget pack runtime.nuspec -properties VERSION=@VERSION@\;RID=@RID@\;PLATFORM_AOT_SUFFIX=@PLATFORM_AOT_SUFFIX@

COREFX_BINDIR=$(COREFX_ROOT)/artifacts/bin

check-env:
	@if [ "x$(COREFX_ROOT)" == "x" ]; then echo "Set COREFX_ROOT to the root of the fully built corefx repo."; exit 1; fi

clean:
	rm -rf sdk shared host dotnet tests LICENSE.txt ThirdPartyNotices.txt $(NETCORESDK_FILE)

#
# Running tests:
#
# We have two modes to run then with xtest-Foo.Test to be the prefered one because it runs all tests and simple runner
# with rules like tes-Foo which runs limited set of test
#

# e.g. `make xtest-System.Collections.Tests COREFX_ROOT=/prj/corefx`
# '-parallel none -verbose' for debug, `-parallel all` to run all tests in parallel
# we need COREFX_ROOT only for xunit.console.deps.json and CoreFx.Private.TestUtilities.dll
xtest-%: prepare check-env dl-test-assets
	echo -n "***************** $* *********************"
	cp $(COREFX_BINDIR)/runtime/netcoreapp-OSX-Debug-x64/Newtonsoft.Json.dll assets/extracted/$*/
	cp $(COREFX_BINDIR)/runtime/netcoreapp-OSX-Debug-x64/xunit.console.deps.json assets/extracted/$*/
	cp $(COREFX_BINDIR)/runtime/netcoreapp-OSX-Debug-x64/CoreFx.Private.TestUtilities.dll assets/extracted/$*/
	sed -i -e 's/9.9.9/$(NETCOREAPP_VERSION)/g' assets/extracted/$*/*.runtimeconfig.json
	cd assets/extracted/$* && \
	COMPlus_DebugWriteToStdErr=1 $(CURDIR)/./dotnet --fx-version "$(NETCOREAPP_VERSION)" xunit.console.dll $*.dll \
		-notrait category=outerloop -notrait category=nonosxtests -notrait category=failing \
		-notrait category=nonnetcoreapptests \
		-html ../../../TestResult-$*.html -nunit ../../TestResult-$*-netcore-xunit.xml \
		$(shell if [ -a $(CURDIR)/excludes-$*.rsp ]; then grep -v '^#\|^$$' $(CURDIR)/excludes-$*.rsp; fi;) \
		$(FIXTURE) || true

# these tests won't be included in `xtestall` (some of them crash runtime, some hang)
EXCLUDED_COREFX_TESTS = \
	System.Diagnostics.Tracing.Tests \
	System.Net.Http.Functional.Tests \
	System.Net.HttpListener.Tests \
	System.Net.NameResolution.Functional.Tests \
	System.Net.Sockets.Tests

# Tests suites which pass
PASSING_COREFX_TESTS = \
	System.Collections.Immutable.Tests \
	System.Reflection.Extensions.Tests \
	System.Runtime.CompilerServices.Unsafe.Tests \
	System.Runtime.CompilerServices.VisualC.Tests \
	System.Runtime.InteropServices.RuntimeInformation.Tests

xtestall: dl-test-assets $(foreach workingtest, $(foreach test, $(wildcard assets/extracted/*), \
		$(filter-out $(EXCLUDED_COREFX_TESTS), $(notdir $(test)))), $(addprefix xtest-, $(workingtest)))

xtestpass: dl-test-assets $(foreach workingtest, $(foreach test, $(wildcard assets/extracted/*), \
		$(filter $(PASSING_COREFX_TESTS), $(notdir $(test)))), $(addprefix xtest-, $(workingtest)))

FEED_BASE_URL := https://dotnetfeed.blob.core.windows.net/dotnet-core
TEST_ASSETS_URL = $(FEED_BASE_URL)/corefx-tests/$(NETCORETESTS_VERSION)/OSX.x64/netcoreapp/corefx-test-assets.xml
corefx-test-assets.xml:
	curl $(TEST_ASSETS_URL) --output corefx-test-assets.xml

dl-test-assets: corefx-test-assets.xml .stamp-dl-test-assets

.stamp-dl-test-assets:
	python dl-test-assets.py corefx-test-assets.xml $(FEED_BASE_URL) assets
	touch $@

#
# console test runner (obsolete)
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
	sed -e 's/9.9.9/$(VERSION)/g' < tests/$*/RemoteExecutorConsoleApp.runtimeconfig.json > 2 && mv 2 tests/$*/RemoteExecutorConsoleApp.runtimeconfig.json
	cd tests/$* && dotnet build

run-%: check-env
	cd tests/$* && MONO_PATH=bin/Debug/netcoreapp3.0 MONO_ENV_OPTIONS="--debug --explicit-null-checks" COMPlus_DebugWriteToStdErr=1 ../../dotnet bin/Debug/netcoreapp3.0/$*-runner.dll

build-base: $(addprefix build-, $(TEST_SUITES))

check: $(addprefix run-, $(TEST_SUITES))
