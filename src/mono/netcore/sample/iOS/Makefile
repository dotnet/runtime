MONO_CONFIG=Debug

# change to x64 for simulator
MONO_ARCH=arm64
ARTIFACTS_BIN=../../../../../artifacts/bin/
ARTIFACTS_BCL=$(ARTIFACTS_BIN)runtime/netcoreapp5.0-iOS-$(MONO_CONFIG)-$(MONO_ARCH)
ARTIFACTS_MONO=$(ARTIFACTS_BIN)/mono/iOS.$(MONO_ARCH).$(MONO_CONFIG)

DOTNET := $(shell cd ../../ && bash init-tools.sh | tail -1)
SYSROOT := $(shell xcrun --sdk iphoneos --show-sdk-path)

# once a new library is added here it should also be 
# added in mono_ios_register_modules() (runtime.m)
all: prepare
	make aot-lib-${MONO_ARCH} LIB=$(ARTIFACTS_MONO)/System.Private.CoreLib.dll
	make aot-lib-${MONO_ARCH} LIB=$(ARTIFACTS_BCL)/System.Runtime.dll
	make aot-lib-${MONO_ARCH} LIB=$(ARTIFACTS_BCL)/System.Runtime.Extensions.dll
	make aot-lib-${MONO_ARCH} LIB=$(ARTIFACTS_BCL)/System.Collections.dll
	make aot-lib-${MONO_ARCH} LIB=$(ARTIFACTS_BCL)/System.Core.dll
	make aot-lib-${MONO_ARCH} LIB=$(ARTIFACTS_BCL)/System.Threading.dll
	make aot-lib-${MONO_ARCH} LIB=$(ARTIFACTS_BCL)/System.Threading.Tasks.dll
	make aot-lib-${MONO_ARCH} LIB=$(ARTIFACTS_BCL)/System.Linq.dll
	make aot-lib-${MONO_ARCH} LIB=$(ARTIFACTS_BCL)/System.Memory.dll
	make aot-lib-${MONO_ARCH} LIB=$(ARTIFACTS_BCL)/System.Runtime.InteropServices.dll
	make aot-lib-${MONO_ARCH} LIB=$(ARTIFACTS_BCL)/System.Text.Encoding.Extensions.dll
	make aot-lib-${MONO_ARCH} LIB=$(ARTIFACTS_BCL)/Microsoft.Win32.Primitives.dll
	make aot-lib-${MONO_ARCH} LIB=$(ARTIFACTS_BCL)/System.Console.dll
	make Program.dll.o

# recompile Program.cs AOT
Program.dll.o: bin/Program.dll Makefile
	make aot-lib-${MONO_ARCH} LIB=bin/Program.dll

# we need to copy some BCL libs to ARTIFACTS_MONO
# to be able to aot other bcl libs
prepare:
	cp $(ARTIFACTS_BCL)/System.Memory.dll $(ARTIFACTS_MONO)
	cp $(ARTIFACTS_BCL)/System.Collections.dll $(ARTIFACTS_MONO)
	cp $(ARTIFACTS_BCL)/System.Threading.dll $(ARTIFACTS_MONO)
	cp $(ARTIFACTS_BCL)/System.Threading.Thread.dll $(ARTIFACTS_MONO)
	cp $(ARTIFACTS_BCL)/System.Runtime.dll $(ARTIFACTS_MONO)
	cp $(ARTIFACTS_BCL)/System.Runtime.InteropServices.dll $(ARTIFACTS_MONO)
	cp $(ARTIFACTS_BCL)/System.Text.Encoding.Extensions.dll $(ARTIFACTS_MONO)
	cp $(ARTIFACTS_BCL)/Microsoft.Win32.Primitives.dll $(ARTIFACTS_MONO)
	cp $(ARTIFACTS_BCL)/System.Console.dll $(ARTIFACTS_MONO)

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