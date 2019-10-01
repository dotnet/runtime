ifneq ($(wildcard config.make),)

include config.make

NETCORETESTS_VERSION := $(shell cat ../eng/Versions.props | sed -n 's/.*MicrosoftPrivateCoreFxNETCoreAppVersion>\(.*\)<\/MicrosoftPrivateCoreFxNETCoreAppVersion.*/\1/p' )
NETCOREAPP_VERSION := $(shell cat ../eng/Versions.props | sed -n 's/.*MicrosoftNETCoreAppVersion>\(.*\)<\/MicrosoftNETCoreAppVersion.*/\1/p' )
ROSLYN_VERSION:=  $(shell cat ../eng/Versions.props | sed -n 's/.*MicrosoftNetCompilersVersion>\(.*\)<\/MicrosoftNetCompilersVersion.*/\1/p' )

# Should be automated but not sure how for now
# the name is misleading osx version is used for any unix like systems
# the url is manually extracted from coreclr-outerloop (https://dev.azure.com/dnceng/public/_build?definitionId=98&_a=summary) Build artifacts published page
CORECLR_TESTS:=https://dev.azure.com/dnceng/9ee6d478-d288-47f7-aacc-f6e6d082ae6d/_apis/build/builds/368250/artifacts?artifactName=Tests_OSX_x64_checked_outerloop&api-version=5.2-preview.5&%24format=zip

# runtime version used by $(DOTNET) - local .net core sdk to bootstrap stuff
# it doesn't match NETCOREAPP_VERSION
BOOTSTRAP_RUNTIME = $(shell ls ../.dotnet/shared/Microsoft.NETCore.App | tail -1)

ifeq ($(HOST_PLATFORM),win32)
PLATFORM_AOT_SUFFIX := .dll
PLATFORM_AOT_PREFIX :=
NETCORESDK_EXT = zip
UNZIPCMD = python -c "import zipfile,sys; zipfile.ZipFile(sys.argv[1], 'r').extractall()"
XUNIT_FLAGS = -notrait category=nonwindowstests @../../../../CoreFX.issues_windows.rsp
TESTS_PLATFORM = Windows_NT.x64
ON_RUNTIME_EXTRACT = chmod -R 755 {host,shared,./dotnet}
DOTNET := $(shell powershell -ExecutionPolicy Bypass -Command "./init-tools.ps1")/dotnet.exe
DOTNET := "$(subst \,/,$(DOTNET))"
endif

ifeq ($(HOST_PLATFORM),macos)
PLATFORM_AOT_SUFFIX := .dylib
PLATFORM_AOT_PREFIX := lib
NETCORESDK_EXT = tar.gz
UNZIPCMD = tar -xvf
XUNIT_FLAGS = -notrait category=nonosxtests
TESTS_PLATFORM = OSX.x64
DOTNET := $(shell ./init-tools.sh | tail -1)
endif

ifeq ($(HOST_PLATFORM),linux)
PLATFORM_AOT_SUFFIX := .so
PLATFORM_AOT_PREFIX := lib
NETCORESDK_EXT = tar.gz
UNZIPCMD = tar -xvf
XUNIT_FLAGS = -notrait category=nonlinuxtests @../../../../CoreFX.issues_linux.rsp
ifeq ($(COREARCH), arm64)
XUNIT_FLAGS += @../../../../CoreFX.issues_linux_arm64.rsp
endif
TESTS_PLATFORM = Linux.x64
DOTNET := $(shell ./init-tools.sh | tail -1)
endif

NETCORESDK_FILE := dotnet-runtime-$(NETCOREAPP_VERSION)-$(RID).$(NETCORESDK_EXT)
NETCORE_URL := https://dotnetcli.blob.core.windows.net/dotnet/Runtime/$(NETCOREAPP_VERSION)/$(NETCORESDK_FILE)
FEED_BASE_URL := https://dotnetfeed.blob.core.windows.net/dotnet-core
TEST_ASSETS_PATH := corefx-tests/$(NETCORETESTS_VERSION)/$(TESTS_PLATFORM)/netcoreapp/corefx-test-assets.xml

# used to calculate exact version number for generating nupkg
BLAME = $(shell git blame ../configure.ac | grep AC_INIT | cut -f1 -d' ')
VERSTUB = .$(shell git rev-list --count $(BLAME)..HEAD)-preview

all: bcl

$(NETCORESDK_FILE):
	curl $(NETCORE_URL) --output $(NETCORESDK_FILE)
	rm -rf shared/Microsoft.NETCore.App
	$(UNZIPCMD) $(NETCORESDK_FILE)
	$(ON_RUNTIME_EXTRACT)

update-corefx: corefx/.stamp-dl-corefx-$(NETCORETESTS_VERSION)

corefx/.stamp-dl-corefx-$(NETCORETESTS_VERSION): corefx-restore.csproj
	$(DOTNET) build corefx-restore.csproj --runtime $(RID) --packages corefx/packages -p:MicrosoftPrivateCoreFxNETCoreAppVersion=$(NETCORETESTS_VERSION) -p:OutputPath=corefx/restore/
	touch $@

update-roslyn: roslyn/packages/microsoft.net.compilers.toolset/$(ROSLYN_VERSION)/microsoft.net.compilers.toolset.nuspec

roslyn/packages/microsoft.net.compilers.toolset/$(ROSLYN_VERSION)/microsoft.net.compilers.toolset.nuspec:
	$(DOTNET) restore roslyn-restore.csproj -p:RoslynVersion=$(ROSLYN_VERSION) --packages roslyn/packages -p:OutputPath=roslyn/restore/

run-sample: prepare
	$(DOTNET) build sample/HelloWorld
	MONO_ENV_OPTIONS="--debug" COMPlus_DebugWriteToStdErr=1 ./dotnet --fx-version "$(NETCOREAPP_VERSION)" sample/HelloWorld/bin/netcoreapp3.0/HelloWorld.dll

run-sample-dbg: prepare
	$(DOTNET) build sample/HelloWorld
	MONO_ENV_OPTIONS="--debug --debugger-agent=transport=dt_socket,address=127.0.0.1:1235,server=y,suspend=y" COMPlus_DebugWriteToStdErr=1 ./dotnet --fx-version "$(NETCOREAPP_VERSION)" sample/HelloWorld/bin/netcoreapp3.0/HelloWorld.dll

run-sample-coreclr:
	cd sample/HelloWorld && $(DOTNET) run

# build any app in "self contained" mode and patch coreclr.dll and corelib with mono bits
# e.g. `make patch-app APP_PATH=/my/aspnetapp`
patch-app: prepare
	@if test -z "$(APP_PATH)"; then echo "APP_PATH is not set"; exit 1; fi
	cp NuGet.config $(APP_PATH)
	cd $(APP_PATH) && $(DOTNET) publish -c Release -r $(RID) -f netcoreapp3.0
	cp ../mono/mini/.libs/libmonosgen-2.0$(PLATFORM_AOT_SUFFIX) $(APP_PATH)/bin/Release/netcoreapp3.0/$(RID)/publish/$(PLATFORM_AOT_PREFIX)coreclr$(PLATFORM_AOT_SUFFIX)	
	cp System.Private.CoreLib/bin/$(COREARCH)/System.Private.CoreLib.dll $(APP_PATH)/bin/Release/netcoreapp3.0/$(RID)/publish
	cp System.Private.CoreLib/bin/$(COREARCH)/System.Private.CoreLib.pdb $(APP_PATH)/bin/Release/netcoreapp3.0/$(RID)/publish

# makes $(DOTNET) to use mono runtime (to run real-world apps using '$(DOTNET) run')
patch-local-dotnet: prepare
	cp ../mono/mini/.libs/libmonosgen-2.0$(PLATFORM_AOT_SUFFIX) ../.dotnet/shared/Microsoft.NETCore.App/$(BOOTSTRAP_RUNTIME)/$(PLATFORM_AOT_PREFIX)coreclr$(PLATFORM_AOT_SUFFIX)
	cp System.Private.CoreLib/bin/$(COREARCH)/System.Private.CoreLib.dll ../.dotnet/shared/Microsoft.NETCore.App/$(BOOTSTRAP_RUNTIME)
	cp System.Private.CoreLib/bin/$(COREARCH)/System.Private.CoreLib.pdb ../.dotnet/shared/Microsoft.NETCore.App/$(BOOTSTRAP_RUNTIME)

# Precompile BCL libs in $(DOTNET) using LLVM (requires `--enable-llvm` build)
patch-local-dotnet-aot-llvm: patch-local-dotnet
	for assembly in ../.dotnet/shared/Microsoft.NETCore.App/$(BOOTSTRAP_RUNTIME)/*.dll; do \
		echo "[AOT] $$assembly"; \
		PATH="../llvm/usr/bin/:$(PATH)" \
		MONO_ENV_OPTIONS="--aot=llvm,llvmllc=\"-mcpu=native\"" \
		$(DOTNET) $$assembly ; \
	done; \

# run 'dotnet/performance' benchmarks
# e.g. 'make run-benchmarks DOTNET_PERF_REPO=/prj/performance'
# you can append BDN parameters at the end, e.g. ` -- --filter Burgers --keepFiles`
# NOTE: we have to delete a couple of System.Drawing files to make it work
run-benchmarks: patch-local-dotnet
	@if test -z "$(DOTNET_PERF_REPO)"; then echo "DOTNET_PERF_REPO is not set"; exit 1; fi
	cd $(DOTNET_PERF_REPO)/src/benchmarks/micro/ && \
	> corefx/System.Drawing/Perf_Color.cs && \
	> corefx/System.Drawing/Perf_Graphics_DrawBeziers.cs && \
	> corefx/System.Drawing/Perf_Graphics_Transforms.cs && \
	> corefx/System.Drawing/Perf_Image_Load.cs && \
	$(DOTNET) run -c Release -f netcoreapp5.0 --cli $(CURDIR)/../.dotnet/dotnet

# run HelloWorld using --aot=llvm (requires `--enable-llvm` build)
run-sample-local-dotnet-llvm: patch-local-dotnet
	$(DOTNET) build -c Release sample/HelloWorld
	PATH="../llvm/usr/bin/:$(PATH)" \
	MONO_ENV_OPTIONS="--aot=llvm,mcpu=native" \
	$(DOTNET) sample/HelloWorld/bin/netcoreapp3.0/HelloWorld.dll
	$(DOTNET) sample/HelloWorld/bin/netcoreapp3.0/HelloWorld.dll

# COREHOST_TRACE=1 
SHAREDRUNTIME := shared/Microsoft.NETCore.App/$(NETCOREAPP_VERSION)

System.Private.CoreLib/src/System/Environment.Mono.cs: System.Private.CoreLib/src/System/Environment.Mono.in config.make
	test -n '$(MONO_CORLIB_VERSION)'
	sed -e 's,@''MONO_CORLIB_VERSION@,$(MONO_CORLIB_VERSION),' $< > $@

CORLIB_BUILD_FLAGS?=-c Release

bcl: update-roslyn System.Private.CoreLib/src/System/Environment.Mono.cs
	$(DOTNET) build $(CORETARGETS) $(CORLIB_BUILD_FLAGS) -p:BuildArch=$(COREARCH) \
	-p:OutputPath=bin/$(COREARCH) \
	-p:RoslynPropsFile="../roslyn/packages/microsoft.net.compilers.toolset/$(ROSLYN_VERSION)/build/Microsoft.Net.Compilers.Toolset.props" \
	System.Private.CoreLib/System.Private.CoreLib.csproj

debug-bcl:
	$(MAKE) bcl CORLIB_BUILD_FLAGS="-c Debug"

runtime:
	$(MAKE) -C ../mono

link-mono: $(SHAREDRUNTIME)/.stamp-link-mono

$(SHAREDRUNTIME)/.stamp-link-mono: ../mono/mini/.libs/libmonosgen-2.0$(PLATFORM_AOT_SUFFIX) System.Private.CoreLib/bin/$(COREARCH)/System.Private.CoreLib.dll System.Private.CoreLib/bin/$(COREARCH)/System.Private.CoreLib.pdb
	cp ../mono/mini/.libs/libmonosgen-2.0$(PLATFORM_AOT_SUFFIX) $(SHAREDRUNTIME)/$(PLATFORM_AOT_PREFIX)coreclr$(PLATFORM_AOT_SUFFIX)
	cp System.Private.CoreLib/bin/$(COREARCH)/System.Private.CoreLib.dll $(SHAREDRUNTIME)
	cp System.Private.CoreLib/bin/$(COREARCH)/System.Private.CoreLib.pdb $(SHAREDRUNTIME)
	touch $@

prepare: $(NETCORESDK_FILE) update-corefx update-roslyn link-mono

pack-%:
	$(DOTNET) pack roslyn-restore.csproj -p:IsPackable=true -p:NuspecFile=$*.nuspec -p:NuspecProperties=\"RID=$(RID)\;VERSION=$(VERSION)$(VERSTUB)\;PLATFORM_AOT_SUFFIX=$(PLATFORM_AOT_SUFFIX)\;COREARCH=$(COREARCH)\;PLATFORM_AOT_PREFIX=$(PLATFORM_AOT_PREFIX)\" --output ../artifacts/ --no-build

nupkg: pack-metapackage pack-runtime
nupkg-llvm: pack-metapackage-llvm pack-runtime-llvm

clean:
	rm -rf .configured ../.dotnet sdk shared host dotnet tests obj corefx roslyn LICENSE.txt ThirdPartyNotices.txt $(NETCORESDK_FILE)


update-tests-corefx: corefx/.stamp-dl-corefx-tests-$(NETCORETESTS_VERSION)

corefx/.stamp-dl-corefx-tests-$(NETCORETESTS_VERSION):
	rm -rf corefx/tests
	$(DOTNET) msbuild corefx-tests-restore.proj -m -t:DownloadAllTests -p:TEST_ASSETS_PATH="$(TEST_ASSETS_PATH)" -p:FEED_BASE_URL="$(FEED_BASE_URL)"
	rm -rf corefx/tests/extracted/System.Utf8String.Experimental.Tests
	touch $@

#
# Running CoreFX tests
#
# You can run either individual test assembly or run all CoreFX tests. The tests are automatically downloaded
# from the shared location and built locally.
#
# Running all test: `make run-tests-corefx`
#
# Running individual test: MONO_ENV_OPTIONS="--debug" make run-tests-corefx-System.Collections.Tests
#

run-tests-corefx: prepare update-tests-corefx
	@rm -f .failures; \
	counter=0; \
	tests_count=$(words $(dir $(wildcard corefx/tests/extracted/*/))); \
	for testdir in corefx/tests/extracted/*; do \
		counter=$$((counter+1)); \
		testname=$$(basename $$testdir); \
		if [ -n "$$USE_TIMEOUT" ]; then timeoutcmd="../scripts/ci/run-step.sh --label=$$testname --timeout=10m --fatal"; fi; \
		$$timeoutcmd $(MAKE) run-tests-corefx-$$testname TEST_COUNTER="($$counter / $$tests_count) " || echo $$testname >> .failures; \
	done; \
	$(MAKE) xunit-summary; \
	if [ -e ".failures" ]; then \
		echo ""; \
		echo "Failures in test suites:"; \
		cat .failures; \
		echo ""; \
		exit 1; \
	fi

run-tests-corefx-%: prepare update-tests-corefx
	@echo ""
	@echo "***************** $* $${TEST_COUNTER}*********************"
	@cp corefx/restore/corefx-restore.deps.json corefx/tests/extracted/$*/$*.deps.json
	@cp corefx/restore/corefx-restore.runtimeconfig.dev.json corefx/tests/extracted/$*/$*.runtimeconfig.dev.json
	@cp corefx/restore/corefx-restore.dll corefx/tests/extracted/$*/corefx-restore.dll
	@sed -i -e 's/5.0.0\"/$(NETCOREAPP_VERSION)\"/g' corefx/tests/extracted/$*/*.runtimeconfig.json
	cd corefx/tests/extracted/$* && \
	MONO_ENV_OPTIONS="--debug" COMPlus_DebugWriteToStdErr=1 $(CURDIR)/./dotnet exec \
		--runtimeconfig $*.runtimeconfig.json --additional-deps $*.deps.json \
		--fx-version "$(NETCOREAPP_VERSION)" xunit.console.dll $*.dll \
		-html ../../TestResult-$*.html -xml ../../TestResult-$*-netcore-xunit.xml \
		$(XUNIT_FLAGS) @../../../../CoreFX.issues.rsp \
		$(FIXTURE)

# build a test assembly in COREFX_ROOT (path to a fully built corefx repo) and copy it to corefx/tests/extracted
# e.g. `make build-test-corefx-System.Runtime COREFX_ROOT=/prj/corefx-master`
build-test-corefx-%:
	cd $(COREFX_ROOT)/src/$*/tests && $(DOTNET) msbuild /p:OutputPath=tmp
	cp $(COREFX_ROOT)/src/$*/tests/tmp/$*.Tests.{dll,pdb} $(CURDIR)/corefx/tests/extracted/$*.Tests/
	rm -rf $(COREFX_ROOT)/src/$*/tests/tmp

xunit-summary:
	./xunit-summary.py "corefx/tests"


update-tests-coreclr: coreclr/.stamp-tests

coreclr/.stamp-tests:
	rm -rf coreclr/tests
	curl -L "$(CORECLR_TESTS)" --output coreclr/tests.zip
	tar -xvf coreclr/tests.zip -C coreclr/
	mkdir coreclr/tests
	tar -xvf coreclr/Tests_OSX_x64_checked_outerloop/Tests_OSX_x64_checked_outerloop.tar.gz -C coreclr/tests
	touch $@

.PHONY: corerun
corerun:
	cp corerun/corerun $(SHAREDRUNTIME)

run-tests-coreclr: prepare update-tests-coreclr corerun
	rm -rf coreclr/output.log
	@counter=0; \
	failures=0; \
	test_files=$$(find coreclr/tests -type f -name "*.sh"); \
	for testdir in $$test_files; do \
		counter=$$((counter+1)); \
		echo "Running $$testdir"; \
		if sh $$testdir -coreroot=$(realpath $(SHAREDRUNTIME)) >>coreclr/output.log 2>&1 ; then \
			echo "FAILED"; \
			failures=$$((failures+1)); \
		fi; \
	done; \
	echo "Tests Count: $$counter"; \
	echo "Failed: $$failures"
endif

distdir:
distclean:
