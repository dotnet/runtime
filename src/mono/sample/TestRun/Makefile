TOP=../../../../
DOTNET:=$(TOP)dotnet.sh
DOTNET_Q_ARGS=--nologo -v:q -consoleloggerparameters:NoSummary

MONO_CONFIG?=Debug
MONO_ARCH=riscv64
TARGET_OS=linux
AOT?=false
QEMU=qemu-riscv64-static
SAMPLE_PATH=/home/wuxinlong/workspace/samples/csharp/getting-started/console-webapiclient
ANY_PATH= /home/wuxinlong/workspace/runtime/artifacts/tests/coreclr/linux.riscv64.Debug/JIT/jit64/regress/ddb/127931/127931/127931.dll

GitHubKey=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

#NET_TRACE_PATH=<path-to-trace-of-sample>
#PGO_BINARY_PATH=<path-to-dotnet-pgo-executable>
#MIBC_PROFILE_PATH=<path-to-mibc-for-sample>

MONO_ENV_DEBUG_OPTIONS = "-v"
MONO_ENV_RUN_OPTIONS=""

build: TestRun.cs
	@mcs TestRun.cs

run-baseline: build
	COMPlus_DebugWriteToStdErr=1 \
	MONO_ENV_OPTIONS=$(MONO_ENV_RUN_OPTIONS) \
	$(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-x64/dotnet TestRun.exe

run-any-baseline:
	COMPlus_DebugWriteToStdErr=1 \
	MONO_ENV_OPTIONS=$(MONO_ENV_RUN_OPTIONS) \
	$(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-x64/dotnet \
	$(ANY_PATH)

run: build
	@COMPlus_DebugWriteToStdErr=1 \
	MONO_ENV_OPTIONS=$(MONO_ENV_RUN_OPTIONS) \
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet TestRun.exe
	
run-any: 
	COMPlus_DebugWriteToStdErr=1 \
	MONO_ENV_OPTIONS=$(MONO_ENV_RUN_OPTIONS) \
	${CORE_ROOT}/corerun -p System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization=true $(ANY_PATH)

run-webapiclient-baseline:
	COMPlus_DebugWriteToStdErr=1 \
	MONO_ENV_OPTIONS=$(MONO_ENV_RUN_OPTIONS) \
	$(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-x64/dotnet \
	$(SAMPLE_PATH)/bin/Debug/net8.0/webapiclient.dll

run-webapiclient:
	COMPlus_DebugWriteToStdErr=1 \
	MONO_ENV_OPTIONS=$(MONO_ENV_RUN_OPTIONS) \
	$(QEMU) -L $(TOP).tools/rootfs/riscv64/ \
	$(TOP)artifacts/bin/testhost/net8.0-linux-Debug-riscv64/dotnet \
	$(SAMPLE_PATH)/bin/Debug/net8.0/webapiclient.dll

debug: build
	COMPlus_DebugWriteToStdErr=1 \
	MONO_LOG_LEVEL=debug MONO_LOG_MASK=all \
	MONO_ENV_OPTIONS=$(MONO_ENV_DEBUG_OPTIONS) \
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ \
	-g 12345 \
	$(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet TestRun.exe &\
	gdb-multiarch -ex 'target remote localhost:12345'  \
	-ex "set sysroot $(TOP).tools/rootfs/riscv64" -ex "set solib-search-path $(TOP)artifacts/bin/testhost/net8.0-linux-Debug-riscv64/shared/Microsoft.NETCore.App/8.0.0" \
	-ex "b mini.c:2156" \
	$(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet

debug-webapiclient:
	COMPlus_DebugWriteToStdErr=1 \
	MONO_ENV_OPTIONS=$(MONO_ENV_DEBUG_OPTIONS) \
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ \
	-g 12345 \
	$(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(SAMPLE_PATH)/bin/Debug/net8.0/webapiclient.dll &\
	gdb-multiarch -ex 'target remote localhost:12345'  \
	-ex "set sysroot $(TOP).tools/rootfs/riscv64" \
	-ex "set solib-search-path $(TOP)artifacts/bin/testhost/net8.0-linux-Debug-riscv64/shared/Microsoft.NETCore.App/8.0.0" \
	-ex "b mini.c:2156" \
	$(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet

debug-any:
	COMPlus_DebugWriteToStdErr=1 \
	MONO_ENV_OPTIONS=$(MONO_ENV_DEBUG_OPTIONS) \
	$(QEMU) -g 12345 \
	${CORE_ROOT}/corerun -p System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization=true $(ANY_PATH) &\
	gdb-multiarch -ex 'target remote localhost:12345'  \
	-iex "add-auto-load-safe-path /home/wuxinlong/workspace/runtime/src/mono/sample/TestRun/" \
	-ex "set sysroot $(TOP).tools/rootfs/riscv64" \
	-ex "set solib-search-path ${CORE_ROOT}" \
	-ex "b mini.c:2161" \
	-ex "handle SIG35 print nostop ignore" \
	${CORE_ROOT}/corerun

remote_gdb:
	gdb-multiarch -ex 'target remote localhost:32427'  \
	-iex "add-auto-load-safe-path /home/wuxinlong/workspace/runtime/src/mono/sample/TestRun/" \
	-ex "set sysroot $(TOP).tools/rootfs/riscv64" \
	-ex "set solib-search-path $(TOP)artifacts/bin/testhost/net8.0-linux-Debug-riscv64/shared/Microsoft.NETCore.App/8.0.0" \
	-ex "b mini.c:2156" \
	-ex "handle SIG35 print nostop ignore" \
	${CORE_ROOT}/corerun


debug-any-baseline:
	COMPlus_DebugWriteToStdErr=1 \
	MONO_ENV_OPTIONS=$(MONO_ENV_DEBUG_OPTIONS) \
	gdb \
	-ex "set solib-search-path $(TOP)artifacts/bin/testhost/net8.0-linux-Debug-x86/shared/Microsoft.NETCore.App/8.0.0" \
	-ex "b mini.c:2156" \
	--args $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-x64/dotnet \
	$(ANY_PATH) \

clean:
	rm -f *.exe *.exe.mdb

# ILGEN_0xf64b7da3:Method_0xcb0b0b48
# 306

# converting method PrimitiveVT.VT1B PrimitiveVT.CallConv3:f2 (PrimitiveVT.VT1A,PrimitiveVT.VT1B)

# COMPlus_DebugWriteToStdErr="1" MONO_LOG_LEVEL="debug" MONO_LOG_MASK="all" COREHOST_TRACE="1" COREHOST_TRACE_VERBOSITY="4" MONO_ENV_OPTIONS="--compile-all --trace=all" qemu-riscv64 -L ../../../..//.tools/rootfs/riscv64/ -g 12345 ../../../../artifacts/bin/testhost/net8.0-Linux-Debug-riscv64/dotnet TestRun.exe

# ROOTFS_DIR="" ./build.sh mono+libs+host -c Debug
# ./build.sh mono+libs+host -c Debug --cross --arch riscv64 --build

# ROOTFS_DIR="" ./build.sh clr.hosts -c Debug

# ROOTFS_DIR="" ./build.sh mono+libs+host -c Debug  --ninja /p:KeepNativeSymbols=true
# ./build.sh mono+libs+host+clr.host -c Debug --cross --arch riscv64 --ninja /p:KeepNativeSymbols=true --build

# ./build.sh -mono -debug riscv64 -ninja /p:KeepNativeSymbols=true
# ./build.sh clr.hosts --cross --arch riscv64 --ninja /p:KeepNativeSymbols=true
