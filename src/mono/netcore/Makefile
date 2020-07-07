ifeq ($(wildcard ../../../.dotnet),)
  $(info Downloading local dotnet...)
endif

DOTNET := $(shell bash init-tools.sh | tail -1)
# DOTNET_MONO is a copy of DOTNET (local .dotnet) with Mono Runtime bits (see patch-mono-dotnet rule)
DOTNET_MONO = ../../../.dotnet-mono/dotnet

LIBRARIES_TESTS_CONFIG=Release
CORECLR_TESTS_CONFIG=Release
MONO_RUNTIME_CONFIG=Release

# auto detect configurations for mono runtime and coreclr tests
ifeq ($(words $(wildcard ../../../artifacts/bin/mono/*.*.*)), 1)
	MONO_RUNTIME_CONFIG := $(word 3,$(subst ., ,$(notdir $(wildcard ../../../artifacts/bin/mono/*.*.*))))
endif

ifeq ($(words $(wildcard ../../../artifacts/tests/coreclr/*.*.*)), 1)
	CORECLR_TESTS_CONFIG := $(word 3,$(subst ., ,$(notdir $(wildcard ../../../artifacts/tests/coreclr/*.*.*))))
endif

ifeq ($(words $(wildcard ../../../artifacts/bin/testhost/*-*-*-*)), 1)
	LIBRARIES_TESTS_CONFIG := $(word 3,$(subst -, ,$(notdir $(wildcard ../../../artifacts/bin/testhost/*-*-*-*))))
endif

MONO_PROJ=/p:CoreClrTestConfig=$(CORECLR_TESTS_CONFIG) /p:LibrariesTestConfig=$(LIBRARIES_TESTS_CONFIG) /p:Configuration=$(MONO_RUNTIME_CONFIG) ../mono.proj

# run sample using local .dotnet-mono
# build it with .dotnet first in order to be able to use LLVM only for the actual sample
run-sample: patch-mono-dotnet
	$(DOTNET) build -c Release sample/HelloWorld
	MONO_ENV_OPTIONS="--llvm" COMPlus_DebugWriteToStdErr=1 \
	$(DOTNET_MONO) sample/HelloWorld/bin/HelloWorld.dll

# run sample using local .dotnet (coreclr)
run-sample-coreclr:
	$(DOTNET) run -c Debug -p sample/HelloWorld

# build System.Private.CoreLib.dll
bcl corelib:
	../../.././build.sh -c $(MONO_RUNTIME_CONFIG) -subset Mono.CoreLib

# build runtime and copy to artifacts
runtime:
	../../.././build.sh -c $(MONO_RUNTIME_CONFIG) -subset Mono.Runtime

# call it if you want to use $(DOTNET_MONO) in this Makefile
patch-mono-dotnet:
	$(DOTNET) msbuild /t:PatchLocalMonoDotnet $(MONO_PROJ)

# precompile (AOT) managed libs in .dotnet-mono
patch-mono-dotnet-aot:
	$(DOTNET) msbuild /t:PrecompileLocalMonoDotnetBcl $(MONO_PROJ)

# copy mono runtime bits from artifacts to $(DST)
copy-runtime-files-to:
	$(DOTNET) msbuild /t:CopyMonoRuntimeFilesFromArtifactsToDestination /p:Destination=$(DST) $(MONO_PROJ)

# run specific coreclr test, e.g.:
# make run-tests-coreclr CoreClrTest="bash ../../artifacts/tests/coreclr/OSX.x64.Release/JIT/opt/InstructionCombining/DivToMul/DivToMul.sh"
# NOTE: make sure you've built them (cd src/coreclr && ./build-test.sh release)
run-tests-coreclr:
	$(DOTNET) msbuild /t:RunCoreClrTest /p:CoreClrTest="$(CoreClrTest)" $(MONO_PROJ)

# run all coreclr tests
run-tests-coreclr-all:
	$(DOTNET) msbuild /t:RunCoreClrTests $(MONO_PROJ)

# run tests for a bcl lib, e.g.:
#   make run-tests-corefx-System.Runtime 
# for `System.Runtime.Tests` (the rule builds both the lib and the test if needed)
run-tests-corefx-%:
	$(DOTNET) msbuild /t:RunLibrariesTest /p:LibraryToTest=$* $(MONO_PROJ)

# precompile (AOT) all libraries' tests
aot-tests-corefx:
	$(DOTNET) msbuild /t:PrecompileLibrariesTests $(MONO_PROJ)

# precompile (AOT) all libraries inside testhost (it's used for libraries' tests)
aot-testhost-corefx:
	$(DOTNET) msbuild /t:PrecompileTesthostLibraries $(MONO_PROJ)

# run 'dotnet/performance' benchmarks
# e.g. 'make run-benchmarks BenchmarksRepo=/prj/performance'
# you can append BDN parameters at the end, e.g. ` -- --filter Burgers --keepFiles`
run-benchmarks: patch-local-dotnet
	$(DOTNET) msbuild /t:RunBenchmarks /p:BenchmarksRepo=$(BenchmarksRepo)
