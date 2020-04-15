MONO_CONFIG=Debug
MONO_ARCH=arm64

ARTIFACTS_BIN=../../../../../artifacts/bin/
ARTIFACTS_BCL=$(ARTIFACTS_BIN)runtime/netcoreapp5.0-iOS-$(MONO_CONFIG)-$(MONO_ARCH)
ARTIFACTS_MONO=$(ARTIFACTS_BIN)/mono/iOS.$(MONO_ARCH).$(MONO_CONFIG)

DOTNET := $(shell cd ../../ && bash init-tools.sh | tail -1)

BCL_LIBS = \
	System.Runtime.dll \
	System.Runtime.Extensions.dll \
	System.Collections.dll \
	System.Core.dll \
	System.Threading.dll \
	System.Threading.Tasks.dll \
	System.Linq.dll \
	System.Memory.dll \
	System.Runtime.InteropServices.dll \
	System.Text.Encoding.Extensions.dll \
	Microsoft.Win32.Primitives.dll \
	System.Console.dll

all: mono build

program:
	$(DOTNET) build -c Debug Program.csproj

# copy everything to bin/ and run BuildIosApp task
build: clean program
	cp $(ARTIFACTS_MONO)/System.Private.CoreLib.{dll,pdb} bin
	cp $(ARTIFACTS_MONO)/libmono.a bin
	for lib in $(BCL_LIBS); do cp $(ARTIFACTS_BCL)/$$lib bin; done
	cp $(ARTIFACTS_BCL)/*.a bin
	$(DOTNET) msbuild /t:BuildAppleApp /p:Configuration=$(MONO_CONFIG) \
	/p:TargetArchitecture=$(MONO_ARCH) \
	/p:TargetOS=iOS \
	/p:OutputDirectory=bin \
	/p:ProjectName=HelloiOS \
	/p:Optimized=false \
	/p:AppDir=$(CURDIR)/bin \
	/p:NativeMainSource=$(CURDIR)/main.m \
	/p:BuildAppBundle=true \
	/p:GenerateXcodeProject=true \
	/p:DevTeamProvisioning= \
	/p:MainLibraryFileName=Program.dll \
	../../../mono.proj

deploy-sim:
	make build MONO_ARCH=x64
	xcrun simctl shutdown "iPhone 11" || true
	xcrun simctl boot "iPhone 11"
	open -a Simulator
	xcrun simctl install "iPhone 11" $(realpath ../../../../../artifacts/bin/mono/iOS.x64.$(MONO_CONFIG)/HelloiOS/HelloiOS/Debug-iphonesimulator/HelloiOS.app)
	xcrun simctl launch booted net.dot.HelloiOS

# rebuild mono for iOS-arm64 or x64
mono:
	../../../../.././build.sh -c $(MONO_CONFIG) -os iOS -arch $(MONO_ARCH) -subset Mono

clean:
	rm -rf bin
