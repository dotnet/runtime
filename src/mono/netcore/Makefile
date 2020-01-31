DOTNET_DIR=../../../.dotnet
DOTNET=$(DOTNET_DIR)/dotnet
TESTHOST_PATH=../../../artifacts/bin/testhost/
BOOTSTRAP_RUNTIME = $(shell ls $(DOTNET_DIR)/shared/Microsoft.NETCore.App | tail -1)

TARGET_FRAMEWORK=
CONFIGURATION=
OS=
PLATFORM=

# let's check ../../../artifacts/bin/testhost/ for available configurations
# it's expected to see there only a single config, e.g. "netcoreapp5.0-OSX-Release-x64"
# otherwise you have to set these variables for desired config
ifeq ($(words $(wildcard $(TESTHOST_PATH)/*)), 1)
	TESTHOST_CFG := $(subst -, ,$(notdir $(wildcard $(TESTHOST_PATH)/*)))
	TARGET_FRAMEWORK := $(word 1,$(TESTHOST_CFG))
	OS := $(word 2,$(TESTHOST_CFG))
	CONFIGURATION := $(word 3,$(TESTHOST_CFG))
	PLATFORM := $(word 4,$(TESTHOST_CFG))
endif

ifeq ($(OS), OSX)
	NATIVE_EXT := .dylib
else
	NATIVE_EXT := .so
endif

check-config:
	@if test -z "$(CONFIGURATION)"; then echo "CONFIGURATION is not set"; exit 1; fi

run-sample: check-config
	$(DOTNET) msbuild /t:RunSample /p:Configuration=$(CONFIGURATION) ../mono.proj

runtime:
	$(DOTNET) msbuild /t:Build ../mono.proj

# temp: makes $(DOTNET) to use mono runtime (to run real-world apps using '$(DOTNET) run')
patch-local-dotnet: check-config
	cp ../../../artifacts/obj/mono/$(OS).$(PLATFORM).$(CONFIGURATION)/mono/mini/.libs/libmonosgen-2.0$(NATIVE_EXT) $(DOTNET_DIR)/shared/Microsoft.NETCore.App/$(BOOTSTRAP_RUNTIME)/libcoreclr$(NATIVE_EXT)
	cp ../../../artifacts/bin/mono/$(OS).$(PLATFORM).$(CONFIGURATION)/System.Private.CoreLib.* $(DOTNET_DIR)/shared/Microsoft.NETCore.App/$(BOOTSTRAP_RUNTIME)

# Precompile BCL libs in $(DOTNET) using LLVM (requires `--enable-llvm` build)
patch-local-dotnet-aot-llvm: patch-local-dotnet
	for assembly in $(DOTNET_DIR)/shared/Microsoft.NETCore.App/$(BOOTSTRAP_RUNTIME)/*.dll; do \
		echo "[AOT] $$assembly"; \
		PATH="../llvm/usr/bin/:$(PATH)" \
		MONO_ENV_OPTIONS="--aot=llvm,mcpu=native" \
		$(DOTNET) $$assembly ; \
	done; \

# run 'dotnet/performance' benchmarks
# e.g. 'make run-benchmarks DOTNET_PERF_REPO=/prj/performance'
# you can append BDN parameters at the end, e.g. ` -- --filter Burgers --keepFiles`
run-benchmarks: patch-local-dotnet
	@if test -z "$(DOTNET_PERF_REPO)"; then echo "DOTNET_PERF_REPO is not set"; exit 1; fi
	cd $(DOTNET_PERF_REPO)/src/benchmarks/micro/ && \
	$(DOTNET) run -c Release -f netcoreapp5.0 --cli $(CURDIR)/../.dotnet/dotnet