TOP=../../../../
DOTNET:=$(TOP)dotnet.sh
DOTNET_Q_ARGS=--nologo -v:q -consoleloggerparameters:NoSummary

MONO_CONFIG?=Debug
MONO_ARCH=riscv64
TARGET_OS=linux
AOT?=false
QEMU=qemu-riscv64-static
SAMPLE_PATH=/home/wuxinlong/workspace/samples/csharp/getting-started/console-webapiclient
ANY_PATH= /home/wuxinlong/workspace/samples/csharp/roslyn-sdk/SemanticQuickStart/bin/Debug/net8.0/SemanticQuickStart.dll

GitHubKey=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

#NET_TRACE_PATH=<path-to-trace-of-sample>
#PGO_BINARY_PATH=<path-to-dotnet-pgo-executable>
#MIBC_PROFILE_PATH=<path-to-mibc-for-sample>

MONO_ENV_DEBUG_OPTIONS = "-v --trace=all"
MONO_ENV_RUN_OPTIONS = "-v --trace=all"

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
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(ANY_PATH)

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
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ \
	-g 12345 \
	$(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(ANY_PATH) &\
	gdb-multiarch -ex 'target remote localhost:12345'  \
	-ex "set sysroot $(TOP).tools/rootfs/riscv64" \
	-ex "set solib-search-path $(TOP)artifacts/bin/testhost/net8.0-linux-Debug-riscv64/shared/Microsoft.NETCore.App/8.0.0" \
	-ex "b mini.c:2156" \
	$(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet

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

# Microsoft.CodeAnalysis.Syntax.InternalSyntax.SyntaxListBuilder:ToListNode
# 829 
# c 87
# 0x4002c1abf8

# Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax.SyntaxToken:Create
# 831
# +0x848
# c 5

# Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax.SyntaxToken/SyntaxTokenWithTrivia:.ctor
# 0xe8
# Microsoft.CodeAnalysis.GreenNode:AdjustFlagsAndWidth
# 0xd4

# COMPlus_DebugWriteToStdErr="1" MONO_LOG_LEVEL="debug" MONO_LOG_MASK="all" COREHOST_TRACE="1" COREHOST_TRACE_VERBOSITY="4" MONO_ENV_OPTIONS="--compile-all --trace=all" qemu-riscv64 -L ../../../..//.tools/rootfs/riscv64/ -g 12345 ../../../../artifacts/bin/testhost/net8.0-Linux-Debug-riscv64/dotnet TestRun.exe

# ROOTFS_DIR="" ./build.sh mono+libs+host -c Debug
# ./build.sh mono+libs+host -c Debug --cross --arch riscv64 --build

# ROOTFS_DIR="" ./build.sh clr.hosts -c Debug

# ROOTFS_DIR="" ./build.sh mono+libs+host -c Debug  --ninja /p:KeepNativeSymbols=true
# ./build.sh mono+libs+host -c Debug --cross --arch riscv64 --ninja /p:KeepNativeSymbols=true --build

TEST_PATH=/home/wuxinlong/workspace/samples/

customer-relationship:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)csharp/tutorials/default-interface-members-versions/finished/customer-relationship/bin/Debug/net8.0/customer-relationship.dll

exploration:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)csharp/tutorials/exploration/csharp6-finished/bin/Debug/net8.0/csharp6-finished.dll

toll-calculator:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)csharp/tutorials/patterns/finished/toll-calculator/bin/Debug/net8.0/toll-calculator.dll

mixins-with-interfaces:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)csharp/tutorials/mixins-with-interfaces/bin/Debug/net8.0/mixins-with-interfaces.dll

datetime-parse:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)csharp/api/datetime.parse/bin/Debug/net8.0/datetime.parse.dll

delegates-and-events:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)csharp/delegates-and-events/bin/Debug/net8.0/delegates-and-events.dll

branches-quickstart:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)csharp/branches-quickstart/bin/Debug/net8.0/BranchesAndLoops.dll

events:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)csharp/events/bin/Debug/net8.0/events.dll

expression-trees:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)csharp/expression-trees/bin/Debug/net8.0/expression-trees.dll

indexers:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)csharp/indexers/bin/Debug/net8.0/indexers.dll

iterators:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)csharp/iterators/bin/Debug/net8.0/iterators.dll

list-quickstart:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)csharp/list-quickstart/bin/Debug/net8.0/list-quickstart.dll

NullableIntroduction:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)csharp/NullableIntroduction/NullableIntroduction/bin/Debug/net8.0/NullableIntroduction.dll

numbers-quickstart:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)csharp/numbers-quickstart/bin/Debug/net8.0/NumbersInCSharp.dll

PatternMatching:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)csharp/PatternMatching/bin/Debug/net8.0/PatternMatching.dll

ref-tutorial:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)csharp/ref-tutorial/IntruderAlert-finished/bin/Debug/net8.0/IntruderAlert.dll

versioning-new:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)csharp/versioning/new/bin/Debug/net8.0/new.dll

versioning-override:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)csharp/versioning/override/bin/Debug/net8.0/override.dll

fibonacci-msbuild:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)core/console-apps/fibonacci-msbuild/bin/Debug/net8.0/Hello.dll

FibonacciBetterMsBuild:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)core/console-apps/FibonacciBetterMsBuild/bin/Debug/net8.0/Fibonacci.dll

serialization:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)csharp/serialization/bin/Debug/net8.0/serialization.dll


test: customer-relationship exploration toll-calculator mixins-with-interfaces delegates-and-events branches-quickstart \
events expression-trees indexers list-quickstart NullableIntroduction numbers-quickstart PatternMatching versioning-new versioning-override fibonacci-msbuild