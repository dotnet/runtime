
DOTNET=../../../.dotnet/dotnet
TESTHOST_PATH=../../../artifacts/bin/testhost/

TARGET_FRAMEWORK=
CONFIGURATION=
OS=
PLATFORM=

# let's check ../../../artifacts/bin/testhost/ for available configurations
# it's expected to see there only a single config, e.g. "netcoreapp5.0-OSX-Release-x64"
# otherwise you have to set these variables for desired config
ifeq ($(words $(wildcard $(TESTHOST_PATH)/*)), 1)
	TESTHOST_CFG := $(subst -, ,$(notdir $(wildcard $(TESTHOST_PATH)/*)))
	CONFIGURATION := $(word 3,$(TESTHOST_CFG))
	OS := $(word 2,$(TESTHOST_CFG))
	TARGET_FRAMEWORK := $(word 1,$(TESTHOST_CFG))
	PLATFORM := $(word 4,$(TESTHOST_CFG))
endif

check-config:
	@if test -z "$(CONFIGURATION)"; then echo "CONFIGURATION is not set"; exit 1; fi

run-sample: check-config
	$(DOTNET) msbuild /t:RunSample /p:Configuration=$(CONFIGURATION) ../mono.proj

runtime:
	$(DOTNET) msbuild /t:Build ../mono.proj
