
ARTIFACTS_BIN=../../../../../artifacts/bin/
ARTIFACTS_BCL=$(ARTIFACTS_BIN)runtime/netcoreapp5.0-iOS-Debug-arm64
ARTIFACTS_MONO=$(ARTIFACTS_BIN)/mono/iOS.arm64.Debug

DOTNET := $(shell cd ../../ && bash init-tools.sh | tail -1)
SYSROOT := $(shell xcrun --sdk iphoneos --show-sdk-path)

# once a new library is added here (e.g. System.Console.dll) it should also be 
# added in mono_ios_register_modules() (runtime.m) and both lib.dll and lib.dll.o 
# should be added to xcodeproj 
all: prepare bin/Program.dll
	make aot-lib LIB=$(ARTIFACTS_MONO)/System.Private.CoreLib.dll
	make aot-lib LIB=$(ARTIFACTS_BCL)/System.Runtime.dll
	make aot-lib LIB=$(ARTIFACTS_BCL)/System.Runtime.Extensions.dll
	make aot-lib LIB=$(ARTIFACTS_BCL)/System.Collections.dll
	make aot-lib LIB=$(ARTIFACTS_BCL)/System.Core.dll
	make aot-lib LIB=$(ARTIFACTS_BCL)/System.Threading.dll
	make aot-lib LIB=$(ARTIFACTS_BCL)/System.Threading.Tasks.dll
	make aot-lib LIB=$(ARTIFACTS_BCL)/System.Linq.dll
	make aot-lib LIB=$(ARTIFACTS_BCL)/System.Memory.dll
	make aot-lib LIB=$(ARTIFACTS_BCL)/System.Runtime.InteropServices.dll
	make aot-lib LIB=$(ARTIFACTS_BCL)/System.Text.Encoding.Extensions.dll
	make aot-lib LIB=$(ARTIFACTS_BCL)/Microsoft.Win32.Primitives.dll
	make aot-lib LIB=$(ARTIFACTS_BCL)/System.Console.dll
	make aot-program

# recompile Program.cs
aot-program:
	make aot-lib LIB=bin/Program.dll

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

bin/Program.dll: Program.cs
	$(DOTNET) build -c Debug Program.csproj

aot-lib:
	DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 MONO_PATH=$(ARTIFACTS_MONO) \
	$(ARTIFACTS_MONO)/cross/./mono-aot-cross -O=gsharedvt,float32 --debug \
	--aot=mtriple=arm64-ios,static,asmonly,direct-icalls,no-direct-calls,dwarfdebug,full $(LIB) && \
	clang -isysroot $(SYSROOT) -miphoneos-version-min=10.1 -arch arm64 -c $(LIB).s
	cp $(LIB) $(notdir $(LIB))
