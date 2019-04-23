include ../mcs/build/config.make

#
# NETCORETESTS_VERSION and NETCOREAPP_VERSION must be updated in sync, we are using coreclr repo for that but that's fine for now
#

# Extracted MicrosoftPrivateCoreFxNETCoreAppVersion from https://github.com/dotnet/coreclr/blob/master/eng/Versions.props#L12
NETCORETESTS_VERSION := 4.6.0-preview5.19215.16

# Extracted MicrosoftNETCoreAppVersion from https://github.com/dotnet/coreclr/blob/master/eng/Versions.props#L14
NETCOREAPP_VERSION := 3.0.0-preview5-27609-17

ifeq ($(HOST_PLATFORM),win32)
NETCORESDK_EXT = zip
UNZIPCMD = python -c "import zipfile,sys; zipfile.ZipFile(sys.argv[1], 'r').extractall()"
XUNIT_FLAGS =
endif

ifeq ($(HOST_PLATFORM),macos)
NETCORESDK_EXT = tar.gz
UNZIPCMD = tar -xvf
XUNIT_FLAGS = -notrait category=nonosxtests
endif

ifeq ($(HOST_PLATFORM),linux)
NETCORESDK_EXT = tar.gz
UNZIPCMD = tar -xvf
XUNIT_FLAGS = -notrait category=nonlinuxtests
endif

NETCORESDK_FILE := dotnet-runtime-$(NETCOREAPP_VERSION)-$(RID).$(NETCORESDK_EXT)
URL := https://dotnetcli.blob.core.windows.net/dotnet/Runtime/$(NETCOREAPP_VERSION)/$(NETCORESDK_FILE)
FEED_BASE_URL := https://dotnetfeed.blob.core.windows.net/dotnet-core
TEST_ASSETS_URL = $(FEED_BASE_URL)/corefx-tests/$(NETCORETESTS_VERSION)/OSX.x64/netcoreapp/corefx-test-assets.xml

$(NETCORESDK_FILE):
	curl $(URL) --output $(NETCORESDK_FILE)
	$(UNZIPCMD) $(NETCORESDK_FILE)

update-corefx: corefx/.stamp-dl-corefx-$(NETCORETESTS_VERSION)

corefx/.stamp-dl-corefx-$(NETCORETESTS_VERSION): corefx-restore.csproj
	dotnet build --runtime $(RID) --packages corefx/packages -p:MicrosoftPrivateCoreFxNETCoreAppVersion=$(NETCORETESTS_VERSION) -p:OutputPath=corefx/restore/
	touch $@

update-corefx-tests: corefx/.stamp-dl-corefx-tests-$(NETCORETESTS_VERSION)

corefx/.stamp-dl-corefx-tests-$(NETCORETESTS_VERSION):
	curl $(TEST_ASSETS_URL) --output corefx-test-assets.xml
	python dl-test-assets.py corefx-test-assets.xml $(FEED_BASE_URL) corefx/tests
	touch $@

build-sample:
	dotnet build sample/HelloWorld

run-sample:
	COMPlus_DebugWriteToStdErr=1 ./dotnet --fx-version "$(NETCOREAPP_VERSION)" sample/HelloWorld/bin/Debug/netcoreapp3.0/HelloWorld.dll

# COREHOST_TRACE=1 
SHAREDRUNTIME := shared/Microsoft.NETCore.App/$(NETCOREAPP_VERSION)

bcl:
	$(MAKE) -C ../mcs/build/ common/Consts.cs
	$(MAKE) -C ../mcs/class/System.Private.CoreLib
	cp ../mcs/class/System.Private.CoreLib/bin/$(COREARCH)/System.Private.CoreLib.dll $(SHAREDRUNTIME)

runtime:
	$(MAKE) -C ../mono
	cp ../mono/mini/.libs/libmonosgen-2.0$(PLATFORM_AOT_SUFFIX) $(SHAREDRUNTIME)/$(PLATFORM_AOT_PREFIX)coreclr$(PLATFORM_AOT_SUFFIX)

link-mono:
	cp ../mono/mini/.libs/libmonosgen-2.0$(PLATFORM_AOT_SUFFIX) $(SHAREDRUNTIME)/$(PLATFORM_AOT_PREFIX)coreclr$(PLATFORM_AOT_SUFFIX)
	cp ../mcs/class/System.Private.CoreLib/bin/$(COREARCH)/System.Private.CoreLib.{dll,pdb} $(SHAREDRUNTIME)

prepare: $(NETCORESDK_FILE) update-corefx link-mono

nupkg:
	nuget pack runtime.nuspec -properties VERSION=$(VERSION)\;RID=$(RID)\;PLATFORM_AOT_SUFFIX=$(PLATFORM_AOT_SUFFIX)\;COREARCH=$(COREARCH)

clean:
	rm -rf sdk shared host dotnet tests obj corefx LICENSE.txt ThirdPartyNotices.txt $(NETCORESDK_FILE)

#
# Running tests:
#
# We have two modes to run then with xtest-Foo.Test to be the prefered one because it runs all tests and simple runner
# with rules like tes-Foo which runs limited set of test
#

# e.g. `make xtest-System.Collections.Tests`
# '-parallel none -verbose' for debug, `-parallel all` to run all tests in parallel
xtest-%: prepare update-corefx-tests
	echo -n "***************** $* *********************"
	cp corefx/restore/corefx-restore.deps.json corefx/tests/extracted/$*/xunit.console.deps.json
	cp corefx/restore/corefx-restore.deps.json corefx/tests/extracted/$*/Microsoft.DotNet.RemoteExecutorHost.deps.json
	cp corefx/restore/corefx-restore.runtimeconfig.dev.json corefx/tests/extracted/$*/xunit.console.runtimeconfig.dev.json
	cp corefx/restore/corefx-restore.runtimeconfig.dev.json corefx/tests/extracted/$*/Microsoft.DotNet.RemoteExecutorHost.runtimeconfig.dev.json
	cp corefx/restore/corefx-restore.dll corefx/tests/extracted/$*/corefx-restore.dll
	sed -i -e 's/9.9.9/$(NETCOREAPP_VERSION)/g' corefx/tests/extracted/$*/*.runtimeconfig.json
	cd corefx/tests/extracted/$* && \
	COMPlus_DebugWriteToStdErr=1 $(CURDIR)/./dotnet --fx-version "$(NETCOREAPP_VERSION)" xunit.console.dll $*.dll \
		-notrait category=outerloop -notrait category=nonosxtests -notrait category=failing \
		-notrait category=nonnetcoreapptests \
		-html ../../TestResult-$*.html -nunit ../../TestResult-$*-netcore-xunit.xml \
		$(shell if [ -a $(CURDIR)/excludes-$*.rsp ]; then grep -v '^#\|^$$' $(CURDIR)/excludes-$*.rsp; fi;) \
		$(FIXTURE) || true

# these tests won't be included in `xtestall`
EXCLUDED_COREFX_TESTS = \
	System.Utf8String.Experimental.Tests \
	System.ComponentModel.Composition.Registration.Tests

# Tests suites which do pass yet
EXCLUDED_COREFX_TESTS_NOT_PASSING = \
	$(EXCLUDED_COREFX_TESTS) \
	System.ComponentModel.Composition.Tests \
	System.Composition.TypedParts.Tests \
	System.Console.Tests \
	System.Data.Common.Tests \
	System.Data.SqlClient.Tests \
	System.Diagnostics.StackTrace.Tests \
	System.Drawing.Common.Tests \
	System.Numerics.Vectors.Tests \
	System.Reflection.Context.Tests \
	System.Reflection.CoreCLR.Tests \
	System.Reflection.Emit.ILGeneration.Tests \
	System.Reflection.Emit.Lightweight.Tests \
	System.Reflection.Metadata.Tests \
	System.Reflection.MetadataLoadContext.Tests \
	System.Reflection.TypeExtensions.CoreCLR.Tests \
	System.Reflection.TypeExtensions.Tests \
	System.Runtime.Extensions.Tests \
	System.Runtime.InteropServices.Tests \
	System.Runtime.Loader.DefaultContext.Tests \
	System.Runtime.Loader.RefEmitLoadContext.Tests \
	System.Runtime.Loader.Tests \
	System.Runtime.Serialization.Formatters.Tests \
	System.Threading.Overlapped.Tests \
	System.Xml.Xsl.XslTransformApi.Tests

xtestall: update-corefx-tests $(foreach workingtest, $(foreach test, $(wildcard corefx/tests/extracted/*), \
		$(filter-out $(EXCLUDED_COREFX_TESTS), $(notdir $(test)))), $(addprefix xtest-, $(workingtest)))

xtestpass: update-corefx-tests $(foreach workingtest, $(foreach test, $(wildcard corefx/tests/extracted/*), \
		$(filter-out $(EXCLUDED_COREFX_TESTS_NOT_PASSING), $(notdir $(test)))), $(addprefix xtest-, $(workingtest)))

build-local-corefx-test-%: check-env
	cd $(COREFX_ROOT)/src/$*/tests && dotnet build -o "$(CURDIR)/corefx/tests/extracted/tmp"
	cp $(CURDIR)/corefx/tests/extracted/tmp/$*.Tests.{dll,pdb} $(CURDIR)/corefx/tests/extracted/$*.Tests/
	rm -rf $(CURDIR)/corefx/tests/extracted/tmp

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

COREFX_BINDIR=$(COREFX_ROOT)/artifacts/bin

check-env:
	@if [ "x$(COREFX_ROOT)" == "x" ]; then echo "Set COREFX_ROOT to the root of the fully built corefx repo."; exit 1; fi

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
