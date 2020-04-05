MONO_CONFIG=Debug
MONO_ARCH=arm64

ARTIFACTS_BIN=../../../../../artifacts/bin/
ARTIFACTS_BCL=$(ARTIFACTS_BIN)runtime/netcoreapp5.0-iOS-$(MONO_CONFIG)-$(MONO_ARCH)
ARTIFACTS_MONO=$(ARTIFACTS_BIN)/mono/iOS.$(MONO_ARCH).$(MONO_CONFIG)

DOTNET := $(shell cd ../../ && bash init-tools.sh | tail -1)

all: mono-libraries build

program:
	$(DOTNET) build Program.csproj

# copy everything to bin/ and run BuildIosApp task
build: clean program
	cp $(ARTIFACTS_MONO)/System.Private.CoreLib.{dll,pdb} bin
	cp $(ARTIFACTS_MONO)/libmono.a bin
	cp $(ARTIFACTS_BCL)/* bin
	$(DOTNET) msbuild /t:BuildIosApp /p:Configuration=$(MONO_CONFIG) \
	/p:TargetArchitecture=$(MONO_ARCH) \
	/p:TargetOS=iOS \
	/p:AppDir=$(CURDIR)/bin \
	/p:NativeMainSource=$(CURDIR)/main.m \
	/p:GenerateOnlyXcodeProject=true \
	/p:DevTeamProvisioning= \
	/p:EntryPointLib=Program.dll \
	../../../mono.proj

# rebuild mono and libraries for iOS and arm64 or x64
mono-libraries:
	../../../../.././build.sh -os iOS -arch $(MONO_ARCH) --subsetCategory mono-libraries

clean:
	rm -rf bin
