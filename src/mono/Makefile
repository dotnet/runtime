DOTNET := ../../dotnet.sh

LIBRARIES_TESTS_CONFIG=Release
CORECLR_TESTS_CONFIG=Release
MONO_RUNTIME_CONFIG=Release

# auto detect configurations for mono runtime and coreclr tests
ifeq ($(words $(wildcard ../../artifacts/bin/mono/*.*.*)), 1)
	MONO_RUNTIME_CONFIG := $(word 3,$(subst ., ,$(notdir $(wildcard ../../artifacts/bin/mono/*.*.*))))
endif

ifeq ($(words $(wildcard ../../artifacts/tests/coreclr/*.*.*)), 1)
	CORECLR_TESTS_CONFIG := $(word 3,$(subst ., ,$(notdir $(wildcard ../../artifacts/tests/coreclr/*.*.*))))
endif

ifeq ($(words $(wildcard ../../artifacts/bin/testhost/*-*-*-*)), 1)
	LIBRARIES_TESTS_CONFIG := $(word 3,$(subst -, ,$(notdir $(wildcard ../../artifacts/bin/testhost/*-*-*-*))))
endif

MONO_PROJ=/p:CoreClrTestConfig=$(CORECLR_TESTS_CONFIG) /p:LibrariesTestConfig=$(LIBRARIES_TESTS_CONFIG) /p:Configuration=$(MONO_RUNTIME_CONFIG) mono.proj

# run sample using local .dotnet-mono
# build it with .dotnet first in order to be able to use LLVM only for the actual sample
run-sample:
	make run -C sample/HelloWorld

# run sample using local .dotnet (coreclr)
run-sample-coreclr:
	$(DOTNET) run -c Release -p sample/HelloWorld

# build System.Private.CoreLib.dll
bcl corelib:
	../.././build.sh -c $(MONO_RUNTIME_CONFIG) -subset Mono.CoreLib

# build runtime and copy to artifacts
runtime:
	../.././build.sh -c $(MONO_RUNTIME_CONFIG) -subset Mono.Runtime+Libs.Pretest

# copy mono runtime bits from artifacts to $(DST)
copy-runtime-files-to:
	$(DOTNET) msbuild /t:CopyMonoRuntimeFilesFromArtifactsToDestination /p:Destination=$(DST) $(MONO_PROJ)

# run specific coreclr test, e.g.:
# make run-tests-coreclr CoreClrTest="bash ../../artifacts/tests/coreclr/OSX.x64.Release/JIT/opt/InstructionCombining/DivToMul/DivToMul.sh"
# NOTE: make sure you've built them (cd src/tests && ./build.sh release)
run-tests-coreclr:
	$(DOTNET) msbuild /t:RunCoreClrTest /p:CoreClrTest="$(CoreClrTest)" $(MONO_PROJ)

# run all coreclr tests
run-tests-coreclr-all:
	$(DOTNET) msbuild /t:RunCoreClrTests $(MONO_PROJ)

# run tests for a bcl lib, e.g.:
#   make run-tests-corefx-System.Runtime 
# for `System.Runtime.Tests` (the rule builds both the lib and the test if needed)
run-tests-corefx-%:
	$(DOTNET) build ../../src/libraries/$*/tests -c $(LIBRARIES_TESTS_CONFIG) /p:RuntimeFlavor=mono /t:Test

