MONO_CONFIG=Debug

# change to x64 for simulator
MONO_ARCH=arm64
ARTIFACTS_BIN=../../../../../artifacts/bin/
ARTIFACTS_BCL=$(ARTIFACTS_BIN)runtime/netcoreapp5.0-iOS-$(MONO_CONFIG)-$(MONO_ARCH)
ARTIFACTS_MONO=$(ARTIFACTS_BIN)/mono/iOS.$(MONO_ARCH).$(MONO_CONFIG)

DOTNET := $(shell cd ../../ && bash init-tools.sh | tail -1)
SYSROOT := $(shell xcrun --sdk iphoneos --show-sdk-path)

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

# once a new library is added here it should also be 
# added in mono_ios_register_modules() (runtime.m)
all: prepare
	make aot-lib-${MONO_ARCH} LIB=$(ARTIFACTS_MONO)/System.Private.CoreLib.dll
	for lib in $(BCL_LIBS); do make aot-lib-${MONO_ARCH} LIB=$(ARTIFACTS_BCL)/$$lib; done
	make Program.dll.o

# recompile Program.cs AOT
Program.dll.o: bin/Program.dll Makefile
	make aot-lib-${MONO_ARCH} LIB=bin/Program.dll

# we need to copy some BCL libs to ARTIFACTS_MONO
# to be able to aot other bcl libs
prepare:
	for lib in $(BCL_LIBS); do cp $(ARTIFACTS_BCL)/$$lib $(ARTIFACTS_MONO); done

bin/Program.dll: Program.cs
	$(DOTNET) build -c Debug Program.csproj

# we'll use regular jit for simulator
aot-lib-x64:
	cp $(LIB) $(notdir $(LIB))

aot-lib-arm64:
	DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 MONO_PATH=$(ARTIFACTS_MONO) \
	$(ARTIFACTS_MONO)/cross/./mono-aot-cross -O=gsharedvt,float32 --nollvm --debug \
	--aot=mtriple=arm64-ios,static,asmonly,direct-icalls,no-direct-calls,dwarfdebug,full $(LIB) && \
	clang -isysroot $(SYSROOT) -miphoneos-version-min=10.1 -arch arm64 -c $(LIB).s
	cp $(LIB) $(notdir $(LIB))

# generate an xcode project
xcode: all
	cmake -S. -BXcode -GXcode \
	-DCMAKE_SYSTEM_NAME=iOS \
	"-DCMAKE_OSX_ARCHITECTURES=arm64;x86_64" \
	-DCMAKE_OSX_DEPLOYMENT_TARGET=10.1 \
	-DCMAKE_INSTALL_PREFIX=`pwd`/_install \
	-DCMAKE_XCODE_ATTRIBUTE_ONLY_ACTIVE_ARCH=NO \
	-DCMAKE_IOS_INSTALL_COMBINED=YES \
	-DMONO_CONFIG=$(MONO_CONFIG) \
	-DMONO_ARCH=$(MONO_ARCH)

clean:
	rm -rf *.dll
	rm -rf *.dll.o
	rm -rf Xcode