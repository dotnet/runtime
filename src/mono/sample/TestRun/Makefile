TOP=../../../../
DOTNET:=$(TOP)dotnet.sh
DOTNET_Q_ARGS=--nologo -v:q -consoleloggerparameters:NoSummary

MONO_CONFIG?=Debug
MONO_ARCH=riscv64
TARGET_OS=linux
AOT?=false
QEMU=qemu-riscv64-static
SAMPLE_PATH=/home/wuxinlong/workspace/samples/csharp/getting-started/console-webapiclient
ANY_PATH= /home/wuxinlong/workspace/samples/orleans/HelloWorld/bin/Debug/net8.0/HelloWorld.dll

GitHubKey=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

#NET_TRACE_PATH=<path-to-trace-of-sample>
#PGO_BINARY_PATH=<path-to-dotnet-pgo-executable>
#MIBC_PROFILE_PATH=<path-to-mibc-for-sample>

MONO_ENV_DEBUG_OPTIONS = "-v"
MONO_ENV_RUN_OPTIONS = "--trace=all"

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
	-iex "add-auto-load-safe-path /home/wuxinlong/workspace/runtime/src/mono/sample/TestRun/" \
	-ex "set sysroot $(TOP).tools/rootfs/riscv64" \
	-ex "set solib-search-path $(TOP)artifacts/bin/testhost/net8.0-linux-Debug-riscv64/shared/Microsoft.NETCore.App/8.0.0" \
	-ex "b mini.c:2156" \
	-ex "handle SIG35 print nostop ignore" \
	$(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet

remote_gdb:
	gdb-multiarch -ex 'target remote localhost:12345'  \
	-iex "add-auto-load-safe-path /home/wuxinlong/workspace/runtime/src/mono/sample/TestRun/" \
	-ex "set sysroot $(TOP).tools/rootfs/riscv64" \
	-ex "set solib-search-path $(TOP)artifacts/bin/testhost/net8.0-linux-Debug-riscv64/shared/Microsoft.NETCore.App/8.0.0" \
	-ex "b mono_break" \
	-ex "handle SIG35 print nostop ignore" \
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
# 0x4015437b1a

# System.Runtime.CompilerServices.AsyncTaskMethodBuilder:SetException
# 1248 +0xd4
# System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<System.Threading.Tasks.VoidTaskResult>:SetException
# +0x118

# System.Runtime.CompilerServices.AsyncTaskMethodBuilder:Create
# 311

# TeleprompterConsole.Program/<ShowTeleprompter>d__2:MoveNext ()
# 322 +0xec

# TeleprompterConsole.Program:ShowTeleprompter
# 319 +0xcc


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

# ./build.sh -mono -debug riscv64 -ninja /p:KeepNativeSymbols=true
# ./build.sh clr.hosts --cross --arch riscv64 --ninja /p:KeepNativeSymbols=true

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

string_format:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)csharp/api/string.format/bin/Debug/net8.0/string.format.examples.dll

classes-quickstart:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)csharp/classes-quickstart/bin/Debug/net8.0/classes.dll

console-linq:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)csharp/getting-started/console-linq/bin/Debug/net8.0/console-linq.dll 

console-teleprompter:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)csharp/getting-started/console-teleprompter/bin/Debug/net8.0/console-teleprompter.dll 

console-webapiclient:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)csharp/getting-started/console-webapiclient/bin/Debug/net8.0/webapiclient.dll 

MetadataLoadContext:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)core/assembly/MetadataLoadContext/bin/Debug/net8.0/MetadataLoadContextSample.dll 

HelloMsBuild:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)core/console-apps/HelloMsBuild/bin/Debug/net8.0/Hello.dll 

DllMapDemo:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)core/extensions/DllMapDemo/bin/Debug/net8.0/Demo.dll 

AppWithPlugin:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)core/extensions/AppWithPlugin/AppWithPlugin/bin/Debug/net8.0/AppWithPlugin.dll

AssemblyLoading:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)core/extensions/AssemblyLoading/AssemblyLoading/bin/Debug/net8.0/AssemblyLoading.dll 

cyrillic-to-latin:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)core/encoding/cyrillic-to-latin/cs/bin/Debug/net8.0/CyrillicToLatin.dll 

linker:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)core/linker/TrimmedWithAdditionalRoots/bin/Debug/net8.0/TrimmedWithAdditionalRoots.dll

linq/aggregate:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)core/linq/csharp/aggregate/bin/Debug/net8.0/aggregate.dll

linq/concatenation:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)core/linq/csharp/concatenation/bin/Debug/net8.0/concatenation.dll

linq/conversion:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)core/linq/csharp/conversion/bin/Debug/net8.0/conversion.dll
	
linq/customsequence:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)core/linq/csharp/customsequence/bin/Debug/net8.0/customsequence.dll

linq/element:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)core/linq/csharp/element/bin/Debug/net8.0/element.dll

linq/equality:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)core/linq/csharp/equality/bin/Debug/net8.0/equality.dll

linq/generation:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)core/linq/csharp/generation/bin/Debug/net8.0/generation.dll

linq/grouping:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)core/linq/csharp/grouping/bin/Debug/net8.0/grouping.dll

linq/join:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)core/linq/csharp/join/bin/Debug/net8.0/join.dll

linq/ordering:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)core/linq/csharp/ordering/bin/Debug/net8.0/ordering.dll

linq/partitioning:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)core/linq/csharp/partitioning/bin/Debug/net8.0/partitioning.dll

linq/quantifier:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)core/linq/csharp/quantifier/bin/Debug/net8.0/quantifier.dll

linq/queryexecution:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)core/linq/csharp/queryexecution/bin/Debug/net8.0/queryexecution.dll

linq/restriction:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)core/linq/csharp/restriction/bin/Debug/net8.0/restriction.dll

linq/setoperators:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)core/linq/csharp/setoperators/bin/Debug/net8.0/setoperators.dll

logging/console-formatter-custom:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)core/logging/console-formatter-custom/bin/Debug/net8.0/console-formatter-custom.dll

logging/console-formatter-custom-with-config:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)core/logging/console-formatter-custom-with-config/bin/Debug/net8.0/console-formatter-custom-with-config.dll	

logging/console-formatter-simple:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)core/logging/console-formatter-simple/bin/Debug/net8.0/console-formatter-simple.dll

logging/console-formatter-systemd:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)core/logging/console-formatter-systemd/bin/Debug/net8.0/console-formatter-systemd.dll

logging/logger-message-generator:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)core/logging/logger-message-generator/bin/Debug/net8.0/logger-message-generator.dll

logging/worker-service-options:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)core/logging/worker-service-options/bin/Debug/net8.0/worker-service-options.dll

whats-new:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)core/whats-new/whats-new-in-21/cs/bin/Debug/net8.0/whats-new.dll

workers/background-service:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)core/workers/background-service/bin/Debug/net8.0/App.WorkerService.dll

workers/cloud-service:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)core/workers/cloud-service/bin/Debug/net8.0/App.CloudService.dll

workers/queue-service:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)core/workers/queue-service/bin/Debug/net8.0/App.QueueService.dll
	
workers/scoped-service:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)core/workers/scoped-service/bin/Debug/net8.0/App.ScopedService.dll

workers/timer-service:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)/core/workers/timer-service/bin/Debug/net8.0/App.TimerHostedService.dll

parallel/ComputePi:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)/csharp/parallel/ComputePi/bin/Debug/net8.0/ComputePi.dll

parallel/EditDistance:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)/csharp/parallel/EditDistance/bin/Debug/net8.0/EditDistance.dll

parallel/PLINQ:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)/csharp/parallel/PLINQ/bin/Debug/net8.0/PLINQ.dll

safe-efficient-code/ref-readonly-struct:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)/csharp/safe-efficient-code/ref-readonly-struct/bin/Debug/net8.0/ref-readonly-struct.dll

unit-testing/XUnit.TestProject:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)/csharp/unit-testing/XUnit.TestProject/bin/Debug/net8.0/XUnit.Project.dll

unit-testing/NUnit.TestProject:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)/csharp/unit-testing/NUnit.TestProject/bin/Debug/net8.0/NUnit.Project.dll

unit-testing/MSTest.Project:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)/csharp/unit-testing/MSTest.Project/bin/Debug/net8.0/MSTest.Project.dll

unit-testing-code-coverage:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)/csharp/unit-testing-code-coverage/XUnit.Coverlet.Collector/bin/Debug/net8.0/XUnit.Coverlet.Collector.dll

roslyn-sdk/Tutorials:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)/csharp/roslyn-sdk/Tutorials/MakeConstTestProject/MakeConstTestProject/bin/Debug/net8.0/MakeConstTestProject.dll

roslyn-sdk/HelloSyntaxTree:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)/csharp/roslyn-sdk/SyntaxQuickStart/HelloSyntaxTree/bin/Debug/net8.0/HelloSyntaxTree.dll 

roslyn-sdk/SyntaxWalker:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)/csharp/roslyn-sdk/SyntaxQuickStart/SyntaxWalker/bin/Debug/net8.0/SyntaxWalker.dll

roslyn-sdk/SemanticQuickStart:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)/csharp/roslyn-sdk/SemanticQuickStart/bin/Debug/net8.0/SemanticQuickStart.dll

roslyn-sdk/ConstructionCS:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)/csharp/roslyn-sdk/SyntaxTransformationQuickStart/ConstructionCS/bin/Debug/net8.0/ConstructionCS.dll

roslyn-sdk/TransformationCS:
	$(QEMU) -L $(TOP)/.tools/rootfs/riscv64/ $(TOP)artifacts/bin/testhost/net8.0-$(TARGET_OS)-Debug-$(MONO_ARCH)/dotnet \
	$(TEST_PATH)/csharp/roslyn-sdk/SyntaxTransformationQuickStart/TransformationCS/bin/Debug/net8.0/TransformationCS.dll
	

test: customer-relationship exploration toll-calculator mixins-with-interfaces delegates-and-events branches-quickstart \
events expression-trees indexers list-quickstart NullableIntroduction numbers-quickstart PatternMatching versioning-new \
versioning-override fibonacci-msbuild classes-quickstart console-linq console-webapiclient MetadataLoadContext HelloMsBuild \
DllMapDemo AppWithPlugin AssemblyLoading cyrillic-to-latin linker linq/aggregate linq/concatenation linq/conversion linq/customsequence \
linq/element linq/equality linq/generation linq/grouping linq/join linq/ordering linq/partitioning linq/quantifier linq/queryexecution \
linq/restriction linq/setoperators logging/console-formatter-custom logging/console-formatter-simple logging/console-formatter-systemd \
logging/logger-message-generator whats-new safe-efficient-code/ref-readonly-struct unit-testing/XUnit.TestProject unit-testing/NUnit.TestProject \
unit-testing/MSTest.Project unit-testing-code-coverage roslyn-sdk/Tutorials roslyn-sdk/HelloSyntaxTree roslyn-sdk/SyntaxWalker