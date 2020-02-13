DOTNET := $(shell bash init-tools.sh | tail -1)
# DOTNET_MONO is a copy of DOTNET (local .dotnet) with Mono Runtime bits (see patch-mono-dotnet rule)
DOTNET_MONO = ../../../.dotnet-mono/dotnet

CORECLR_TESTS_CONFIG=Release
MONO_RUNTIME_CONFIG=Release

# auto detect configurations for mono runtime and coreclr tests
ifeq ($(words $(wildcard ../../../artifacts/bin/mono/*.*.*)), 1)
	MONO_RUNTIME_CONFIG := $(word 3,$(subst ., ,$(notdir $(wildcard ../../../artifacts/bin/mono/*.*.*))))
endif

ifeq ($(words $(wildcard ../../../artifacts/tests/coreclr/*.*.*)), 1)
	CORECLR_TESTS_CONFIG := $(word 3,$(subst ., ,$(notdir $(wildcard ../../../artifacts/tests/coreclr/*.*.*))))
endif

MONO_PROJ=/p:CoreClrTestConfig=$(CORECLR_TESTS_CONFIG) /p:Configuration=$(MONO_RUNTIME_CONFIG) ../mono.proj

# run sample using local .dotnet-mono
# build it with .dotnet first in order to be able to use LLVM only for the actual sample
run-sample: patch-mono-dotnet
	$(DOTNET) build -c Release sample/HelloWorld
	MONO_ENV_OPTIONS="" COMPlus_DebugWriteToStdErr=1 \
	$(DOTNET_MONO) sample/HelloWorld/bin/HelloWorld.dll

run-sample-coreclr:
	$(DOTNET) run -c Debug -p sample/HelloWorld

bcl corelib:
	$(DOTNET) msbuild /t:BuildCoreLib $(MONO_PROJ)

runtime:
	$(DOTNET) msbuild /t:Build $(MONO_PROJ)

# call it if you want to use $(DOTNET_MONO) in this Makefile
patch-mono-dotnet:
	$(DOTNET) msbuild /t:PatchLocalMonoDotnet $(MONO_PROJ)

# run specific coreclr test, e.g.:
# make run-tests-coreclr CoreClrTest="bash ../../artifacts/tests/coreclr/OSX.x64.Release/JIT/opt/InstructionCombining/DivToMul/DivToMul.sh"
run-tests-coreclr:
	$(DOTNET) msbuild /t:RunCoreClrTest /p:CoreClrTest="$(CoreClrTest)" $(MONO_PROJ)

# run all coreclr tests
run-tests-coreclr-all:
	$(DOTNET) msbuild /t:RunCoreClrTests $(MONO_PROJ)

# run 'dotnet/performance' benchmarks
# e.g. 'make run-benchmarks BenchmarksRepo=/prj/performance'
# you can append BDN parameters at the end, e.g. ` -- --filter Burgers --keepFiles`
run-benchmarks: patch-local-dotnet
	$(DOTNET) msbuild /t:RunBenchmarks /p:BenchmarksRepo=$(BenchmarksRepo)
