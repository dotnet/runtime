DOTNET=../../../.dotnet/dotnet
TESTHOST_PATH=../../../artifacts/bin/testhost/

# let's check ../../../artifacts/bin/testhost/ for available configurations
# it's expected to see there only a single config, e.g. "netcoreapp5.0-OSX-Release-x64"
# otherwise you have to set these variables for desired config
ifeq ($(words $(wildcard $(TESTHOST_PATH)/*)), 1)
	TESTHOST_CFG := $(subst -, ,$(notdir $(wildcard $(TESTHOST_PATH)/*)))
	CONFIGURATION := $(word 3,$(TESTHOST_CFG))
	MONO_PROPS := /p:Configuration=$(CONFIGURATION)
endif

check-config:
	@if test -z "$(CONFIGURATION)"; then echo "CONFIGURATION is not set"; exit 1; fi

run-sample: check-config
	$(DOTNET) msbuild /t:RunSample $(MONO_PROPS) ../mono.proj

runtime:
	$(DOTNET) msbuild /t:Build $(MONO_PROPS) ../mono.proj

# temp: makes $(DOTNET) to use mono runtime (to run real-world apps using '$(DOTNET) run')
patch-local-dotnet: check-config
	$(DOTNET) msbuild /t:PatchLocalDotnet $(MONO_PROPS) ../mono.proj

run-tests-coreclr: check-config
	$(DOTNET) msbuild /t:RunCoreClrTest /p:CoreClrTest=$(CoreClrTest) $(MONO_PROPS) ../mono.proj

run-tests-coreclr-all: check-config
	$(DOTNET) msbuild /t:RunCoreClrTests $(MONO_PROPS) ../mono.proj

# run 'dotnet/performance' benchmarks
# e.g. 'make run-benchmarks BenchmarksRepo=/prj/performance'
# you can append BDN parameters at the end, e.g. ` -- --filter Burgers --keepFiles`
run-benchmarks: patch-local-dotnet
	$(DOTNET) msbuild /t:RunBenchmarks /p:BenchmarksRepo=$(BenchmarksRepo)