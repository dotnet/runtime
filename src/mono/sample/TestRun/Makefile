TOP=../../../../
DOTNET:=$(TOP)dotnet.sh
DOTNET_Q_ARGS=--nologo -v:q -consoleloggerparameters:NoSummary

MONO_CONFIG?=Debug
MONO_ARCH=riscv64
TARGET_OS=linux
AOT?=false

#NET_TRACE_PATH=<path-to-trace-of-sample>
#PGO_BINARY_PATH=<path-to-dotnet-pgo-executable>
#MIBC_PROFILE_PATH=<path-to-mibc-for-sample>

MONO_ENV_DEBUG_OPTIONS = "-v --trace=all"
MONO_ENV_RUN_OPTIONS = ""

build: TestRun.cs
	@csc TestRun.cs

run-baseline: build
	COMPlus_DebugWriteToStdErr=1 \
	MONO_ENV_OPTIONS=$(MONO_ENV_RUN_OPTIONS) \
	$(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-x64/dotnet TestRun.exe

run-webapiclient:
	@COMPlus_DebugWriteToStdErr=1 \
	MONO_ENV_OPTIONS=$(MONO_ENV_RUN_OPTIONS) \
	qemu-riscv64 -L $(TOP)/.tools/rootfs/riscv64/ \
	$(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	/home/wuxinlong/workspace/samples/csharp/getting-started/console-webapiclient/bin/Debug/net8.0/webapiclient.dll

run: build
	@COMPlus_DebugWriteToStdErr=1 \
	MONO_ENV_OPTIONS=$(MONO_ENV_RUN_OPTIONS) \
	qemu-riscv64 -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet TestRun.exe

debug: build
	COMPlus_DebugWriteToStdErr=1 \
	MONO_LOG_LEVEL=debug MONO_LOG_MASK=all \
	MONO_ENV_OPTIONS=$(MONO_ENV_DEBUG_OPTIONS) \
	qemu-riscv64 -L $(TOP)/.tools/rootfs/riscv64/ \
	-g 12345 \
	$(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet TestRun.exe &\
	riscv64-unknown-linux-gnu-gdb -ex 'target remote localhost:12345'  \
	-ex "set sysroot $(TOP).tools/rootfs/riscv64" -ex "set solib-search-path $(TOP)artifacts/bin/testhost/net8.0-linux-Debug-riscv64/shared/Microsoft.NETCore.App/8.0.0" \
	-ex "b handle_exception_first_pass" \
	$(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet

clean:
	rm -f *.exe *.exe.mdb


# COMPlus_DebugWriteToStdErr="1" MONO_LOG_LEVEL="debug" MONO_LOG_MASK="all" COREHOST_TRACE="1" COREHOST_TRACE_VERBOSITY="4" MONO_ENV_OPTIONS="--compile-all --trace=all" qemu-riscv64 -L ../../../..//.tools/rootfs/riscv64/ -g 12345 ../../../../artifacts/bin/testhost/net8.0-Linux-Debug-riscv64/dotnet TestRun.exe

# ROOTFS_DIR="" ./build.sh mono+libs+host -c Debug
# ./build.sh mono+libs+host -c Debug --cross --arch riscv64 --build

# ROOTFS_DIR="" ./build.sh clr.hosts -c Debug
