DOTNET=../../../.dotnet/dotnet

CORECLR_TESTS_CONFIG=Release
MONO_RUNTIME_CONFIG=Release

# auto detect configurations for mono runtime and coreclr tests
ifeq ($(words $(wildcard ../../../artifacts/bin/mono/*.*.*)), 1)
	MONO_RUNTIME_CONFIG := $(word 3,$(subst ., ,$(notdir $(wildcard ../../../artifacts/bin/mono/*.*.*))))	
endif

ifeq ($(words $(wildcard ../../../artifacts/tests/coreclr/*.*.*)), 1)
	CORECLR_TESTS_CONFIG := $(word 3,$(subst ., ,$(notdir $(wildcard ../../../artifacts/tests/coreclr/*.*.*))))	
endif

MONO_PROJ=/p:Configuration=$(MONO_RUNTIME_CONFIG) ../mono.proj

# run sample using local .dotnet (will be patched with Mono Runtime)
run-sample:
	$(DOTNET) msbuild /t:RunSample $(MONO_PROJ)

# run sample using dotnet from PATH
run-sample-coreclr:
	dotnet run -c Release -p sample/HelloWorld -f netcoreapp3.1

bcl corelib:
	$(DOTNET) msbuild /t:BuildCoreLib $(MONO_PROJ)

runtime:
	$(DOTNET) msbuild /t:Build $(MONO_PROJ)

# temp: makes $(DOTNET) to use mono runtime (to run real-world apps using '$(DOTNET) run')
patch-local-dotnet:
	$(DOTNET) msbuild /t:PatchLocalDotnet $(MONO_PROJ)

restore-local-dotnet:
	$(DOTNET) msbuild /t:RestoreLocalDotnet $(MONO_PROJ)

# run specific coreclr test, e.g.:
# make run-tests-coreclr CoreClrTest="bash ../../artifacts/tests/coreclr/OSX.x64.Checked/JIT/opt/InstructionCombining/DivToMul/DivToMul.sh"
run-tests-coreclr:
	$(DOTNET) msbuild /t:RunCoreClrTest /p:CoreClrTestConfig=$(CORECLR_TESTS_CONFIG) /p:CoreClrTest="$(CoreClrTest)" $(MONO_PROJ)

# run all coreclr tests
run-tests-coreclr-all:
	$(DOTNET) msbuild /t:RunCoreClrTests /p:CoreClrTestConfig=$(CORECLR_TESTS_CONFIG) $(MONO_PROJ)

# show summary for coreclr tests
tests-coreclr-summary:
	$(DOTNET) msbuild /t:CoreClrTestsSummary $(MONO_PROJ)

# run 'dotnet/performance' benchmarks
# e.g. 'make run-benchmarks BenchmarksRepo=/prj/performance'
# you can append BDN parameters at the end, e.g. ` -- --filter Burgers --keepFiles`
run-benchmarks: patch-local-dotnet
	$(DOTNET) msbuild /t:RunBenchmarks /p:BenchmarksRepo=$(BenchmarksRepo)