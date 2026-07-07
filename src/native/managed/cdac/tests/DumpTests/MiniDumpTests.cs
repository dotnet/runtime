// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Xunit;
using static Microsoft.Diagnostics.DataContractReader.TestInfrastructure.TestHelpers;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Dump-based integration tests that validate the cDAC reader against a <c>Mini</c> (normal)
/// minidump (<c>MiniDumpNormal</c>, <c>DOTNET_DbgMiniDumpType=1</c>).
///
/// A mini dump only contains the memory the runtime reports while walking each thread's
/// stack — it does NOT include the GC heap. Per the runtime's dump-generation design
/// (<c>EnumMemoryRegionsWorkerSkinny</c>) and the documented SOS behavior for triage/normal
/// dumps, the supported scenarios are limited to:
///   * stack traces for all threads  — <c>clrstack</c>  (see <see cref="MiniDumpStackWalkTests"/>)
///   * managed thread enumeration     — <c>clrthreads</c> (see <see cref="MiniDumpThreadTests"/>)
///   * current exception viewing       — <c>!pe</c>
///   * partial module info             — <c>lm</c> / <c>!eeheap -loader</c>
/// Heap-dependent scenarios (<c>!dumpheap</c>, <c>!dumpobj</c>, <c>!gcroot</c>, locals) are
/// intentionally NOT covered because the backing memory is absent from a mini dump.
/// </summary>
public class MiniDumpStackWalkTests : DumpTestBase
{
    protected override string DebuggeeName => "StackWalk";
    protected override string DumpType => "mini";

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void CanWalkCrashingThread(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IStackWalk stackWalk = Target.Contracts.StackWalk;

        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        List<IStackDataFrameHandle> frames = DumpTestStackWalker.LegacyVisibleFrames(stackWalk, crashingThread).ToList();

        Assert.True(frames.Count > 0, "Expected at least one stack frame on the crashing thread");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void HasMultipleFrames(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IStackWalk stackWalk = Target.Contracts.StackWalk;

        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        List<IStackDataFrameHandle> frames = DumpTestStackWalker.LegacyVisibleFrames(stackWalk, crashingThread).ToList();

        // The debuggee has Main → MethodA → MethodB → MethodC → FailFast,
        // plus runtime helper and native transition frames.
        Assert.True(frames.Count >= 5,
            $"Expected multiple stack frames from the crashing thread, got {frames.Count}");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void ContainsExpectedFrames(TestConfiguration config)
    {
        InitializeDumpTest(config);

        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        DumpTestStackWalker.Walk(Target, crashingThread)
            .ExpectFrame("MethodC")
            .ExpectAdjacentFrame("MethodB")
            .ExpectAdjacentFrame("MethodA")
            .ExpectAdjacentFrame("Main")
            .Verify();
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void ManagedFramesHaveValidMethodDescs(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IStackWalk stackWalk = Target.Contracts.StackWalk;
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;

        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        foreach (IStackDataFrameHandle frame in DumpTestStackWalker.LegacyVisibleFrames(stackWalk, crashingThread))
        {
            TargetPointer methodDescPtr = stackWalk.GetMethodDescPtr(frame);
            if (methodDescPtr == TargetPointer.Null)
                continue;

            MethodDescHandle mdHandle = rts.GetMethodDescHandle(methodDescPtr);
            uint token = rts.GetMethodToken(mdHandle);
            Assert.Equal(0x06000000u, token & 0xFF000000u);
        }
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void FramesHaveRawContext(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IStackWalk stackWalk = Target.Contracts.StackWalk;

        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);

        IStackDataFrameHandle? firstFrame = DumpTestStackWalker.LegacyVisibleFrames(stackWalk, crashingThread).FirstOrDefault();
        Assert.NotNull(firstFrame);

        byte[] context = stackWalk.GetRawContext(firstFrame);
        Assert.NotNull(context);
        Assert.True(context.Length > 0, "Expected non-empty raw context for stack frame");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void GetContext_ReturnsNonEmptyContext(TestConfiguration config)
    {
        InitializeDumpTest(config);

        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);
        uint allFlags = Contracts.StackWalkHelpers.IPlatformAgnosticContext.GetContextForPlatform(Target).AllContextFlags;
        byte[] context = Target.Contracts.StackWalk.GetContext(crashingThread, ThreadContextSource.None, allFlags);

        Assert.NotNull(context);
        Assert.True(context.Length > 0, "Expected non-empty context");

        var ctx = Contracts.StackWalkHelpers.IPlatformAgnosticContext.GetContextForPlatform(Target);
        ctx.FillFromBuffer(context);
        Assert.NotEqual(TargetCodePointer.Null, ctx.InstructionPointer);
    }
}

/// <summary>
/// Mini-tier coverage for the <c>clrthreads</c> scenario. A normal minidump iterates the
/// thread store while enumerating each thread's stack (<c>EnumMemDumpAllThreadsStack</c>),
/// so the thread store and every <see cref="ThreadData"/> record it touches are captured.
/// This validates that the Thread contract can enumerate the thread list from a mini dump
/// even though the GC heap is absent.
/// </summary>
public class MiniDumpThreadTests : DumpTestBase
{
    protected override string DebuggeeName => "StackWalk";
    protected override string DumpType => "mini";

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void ThreadStoreData_IsReadable(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IThread threadContract = Target.Contracts.Thread;
        Assert.NotNull(threadContract);

        ThreadStoreData storeData = threadContract.GetThreadStoreData();

        Assert.True(storeData.ThreadCount > 0, $"Expected at least one thread, got {storeData.ThreadCount}");
        Assert.NotEqual(TargetPointer.Null, storeData.FirstThread);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void EnumerateThreads_CanWalkThreadList(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IThread threadContract = Target.Contracts.Thread;

        ThreadStoreData storeData = threadContract.GetThreadStoreData();

        int count = 0;
        HashSet<uint> seenIds = new();
        TargetPointer currentThread = storeData.FirstThread;
        while (currentThread != TargetPointer.Null)
        {
            ThreadData threadData = threadContract.GetThreadData(currentThread);
            count++;
            Assert.True(seenIds.Add(threadData.Id), $"Duplicate thread ID: {threadData.Id}");
            currentThread = threadData.NextThread;
        }

        Assert.Equal(storeData.ThreadCount, count);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public void ThreadStoreData_HasFinalizerThread(TestConfiguration config)
    {
        InitializeDumpTest(config);
        IThread threadContract = Target.Contracts.Thread;

        ThreadStoreData storeData = threadContract.GetThreadStoreData();

        Assert.NotEqual(TargetPointer.Null, storeData.FinalizerThread);
    }
}

/// <summary>
/// Exercises the <see cref="ISOSDacInterface"/> APIs that back the SOS commands supported on a
/// normal minidump, driving them through the same <see cref="SOSDacImpl"/> the shipping cDAC
/// exposes to SOS. Each test maps to a command and validates a cdac-lite normal dump captures
/// enough memory for it:
/// <list type="bullet">
///   <item><c>clrthreads</c>   → GetThreadStoreData / GetThreadData</item>
///   <item><c>clrstack</c> / <c>dumpmd</c> → GetMethodDescData / GetMethodDescName</item>
///   <item><c>ip2md</c> / <c>u</c>  → GetMethodDescPtrFromIP / GetCodeHeaderData (the latter decodes
///         the method's GC info for MethodSize, which the scan emits alongside the code)</item>
///   <item><c>dumpmt</c>       → GetMethodTableData / GetMethodTableName</item>
///   <item><c>dumpmodule</c>   → GetModuleData</item>
///   <item><c>dumpdomain</c>   → GetAppDomainStoreData / GetAppDomainList / GetAppDomainData
///         (the domain object, global LoaderAllocator heaps, and the assembly/module list)</item>
/// </list>
/// Out of scope for a stack-scoped mini dump (backing memory absent): all heap-dependent commands
/// (<c>dumpheap</c>, <c>dumpobj</c>, <c>gcroot</c>, object locals).
/// </summary>
public class MiniDumpSosDacTests : DumpTestBase
{
    protected override string DebuggeeName => "StackWalk";
    protected override string DumpType => "mini";

    // Returns an app frame (JIT'd, frameless) resolved to a MethodDesc on the crashing thread.
    private ResolvedFrame FindAppFrame(string methodName)
    {
        ThreadData crashingThread = DumpTestHelpers.FindFailFastThread(Target);
        return DumpTestStackWalker.Walk(Target, crashingThread).Frames
            .First(f => f.Name == methodName && f.MethodDescPtr != TargetPointer.Null);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public unsafe void ClrThreads_GetThreadStoreAndThreadData(TestConfiguration config)
    {
        InitializeDumpTest(config);
        ISOSDacInterface sosDac = new SOSDacImpl(Target, legacyObj: null);
        IThread threadContract = Target.Contracts.Thread;

        DacpThreadStoreData tsData;
        AssertHResult(System.HResults.S_OK, sosDac.GetThreadStoreData(&tsData));
        Assert.True(tsData.threadCount > 0, $"Expected thread count > 0, got {tsData.threadCount}");
        Assert.NotEqual(0ul, tsData.firstThread.Value);

        // Every thread record the thread store references must be readable (clrthreads walks them).
        int walked = 0;
        TargetPointer thread = threadContract.GetThreadStoreData().FirstThread;
        while (thread != TargetPointer.Null)
        {
            DacpThreadData tdata;
            AssertHResult(System.HResults.S_OK, sosDac.GetThreadData(thread.ToClrDataAddress(Target), &tdata));
            walked++;
            thread = threadContract.GetThreadData(thread).NextThread;
        }
        Assert.Equal(tsData.threadCount, walked);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public unsafe void ClrStack_GetMethodDescDataAndName(TestConfiguration config)
    {
        InitializeDumpTest(config);
        ISOSDacInterface sosDac = new SOSDacImpl(Target, legacyObj: null);

        ResolvedFrame frame = FindAppFrame("MethodC");
        ClrDataAddress md = frame.MethodDescPtr.ToClrDataAddress(Target);

        DacpMethodDescData mdData;
        uint rejitNeeded;
        AssertHResult(System.HResults.S_OK, sosDac.GetMethodDescData(md, default, &mdData, 0, null, &rejitNeeded));
        Assert.Equal(md.Value, mdData.MethodDescPtr.Value);
        Assert.NotEqual(0ul, mdData.MethodTablePtr.Value);

        char* name = stackalloc char[512];
        uint nameNeeded;
        AssertHResult(System.HResults.S_OK, sosDac.GetMethodDescName(md, 512, name, &nameNeeded));
        string methodName = new string(name);
        Assert.Contains("MethodC", methodName);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public unsafe void Ip2Md_GetMethodDescPtrFromIPAndCodeHeaderData(TestConfiguration config)
    {
        InitializeDumpTest(config);
        ISOSDacInterface sosDac = new SOSDacImpl(Target, legacyObj: null);
        IStackWalk stackWalk = Target.Contracts.StackWalk;

        ResolvedFrame frame = FindAppFrame("MethodC");
        TargetCodePointer ip = stackWalk.GetInstructionPointer(frame.FrameHandle);
        Assert.NotEqual(TargetCodePointer.Null, ip);
        ClrDataAddress ipAddr = ip.ToClrDataAddress(Target);

        ClrDataAddress mdFromIp;
        AssertHResult(System.HResults.S_OK, sosDac.GetMethodDescPtrFromIP(ipAddr, &mdFromIp));
        Assert.Equal(frame.MethodDescPtr.ToClrDataAddress(Target).Value, mdFromIp.Value);

        DacpCodeHeaderData chData;
        AssertHResult(System.HResults.S_OK, sosDac.GetCodeHeaderData(ipAddr, &chData));
        Assert.Equal(frame.MethodDescPtr.ToClrDataAddress(Target).Value, chData.MethodDescPtr.Value);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public unsafe void DumpMt_GetMethodTableDataAndName(TestConfiguration config)
    {
        InitializeDumpTest(config);
        ISOSDacInterface sosDac = new SOSDacImpl(Target, legacyObj: null);
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;

        ResolvedFrame frame = FindAppFrame("MethodC");
        TargetPointer mt = rts.GetMethodTable(rts.GetMethodDescHandle(frame.MethodDescPtr));
        ClrDataAddress mtAddr = mt.ToClrDataAddress(Target);

        DacpMethodTableData mtData;
        AssertHResult(System.HResults.S_OK, sosDac.GetMethodTableData(mtAddr, &mtData));
        Assert.NotEqual(0ul, mtData.module.Value);

        char* mtName = stackalloc char[512];
        uint nameNeeded;
        AssertHResult(System.HResults.S_OK, sosDac.GetMethodTableName(mtAddr, 512, mtName, &nameNeeded));
        Assert.False(string.IsNullOrEmpty(new string(mtName)), "Expected a non-empty MethodTable name");
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public unsafe void DumpModule_GetModuleData(TestConfiguration config)
    {
        InitializeDumpTest(config);
        ISOSDacInterface sosDac = new SOSDacImpl(Target, legacyObj: null);
        IRuntimeTypeSystem rts = Target.Contracts.RuntimeTypeSystem;

        ResolvedFrame frame = FindAppFrame("MethodC");
        TargetPointer mt = rts.GetMethodTable(rts.GetMethodDescHandle(frame.MethodDescPtr));

        DacpMethodTableData mtData;
        AssertHResult(System.HResults.S_OK, sosDac.GetMethodTableData(mt.ToClrDataAddress(Target), &mtData));

        DacpModuleData modData;
        AssertHResult(System.HResults.S_OK, sosDac.GetModuleData(mtData.module, &modData));
        Assert.Equal(mtData.module.Value, modData.Address.Value);
    }

    [ConditionalTheory]
    [MemberData(nameof(TestConfigurations))]
    [SkipOnVersion("net10.0", "InlinedCallFrame.Datum was added after net10.0")]
    public unsafe void DumpDomain_GetAppDomainStoreListAndData(TestConfiguration config)
    {
        InitializeDumpTest(config);
        ISOSDacInterface sosDac = new SOSDacImpl(Target, legacyObj: null);

        // The cDAC intentionally reports sharedDomain/systemDomain as 0 (deprecated concepts); the
        // real dumpdomain flow enumerates the app domain via GetAppDomainList + GetAppDomainData.
        DacpAppDomainStoreData adStore;
        AssertHResult(System.HResults.S_OK, sosDac.GetAppDomainStoreData(&adStore));
        Assert.True(adStore.DomainCount >= 1, $"Expected at least one app domain, got {adStore.DomainCount}");

        ClrDataAddress[] domains = new ClrDataAddress[adStore.DomainCount];
        uint needed;
        AssertHResult(System.HResults.S_OK, sosDac.GetAppDomainList((uint)adStore.DomainCount, domains, &needed));
        Assert.True(needed >= 1, $"Expected GetAppDomainList to return at least one domain, got {needed}");

        ClrDataAddress domain = domains[0];
        Assert.NotEqual(0ul, domain.Value);

        // Full GetAppDomainData: reads the domain object, the global LoaderAllocator's loader heaps,
        // and enumerates the domain's assembly/module list (AssemblyCount). cdac-lite emits the
        // LoaderAllocator + each ArrayList block's assembly-pointer array so this succeeds.
        DacpAppDomainData adData;
        AssertHResult(System.HResults.S_OK, sosDac.GetAppDomainData(domain, &adData));
        Assert.Equal(domain.Value, adData.AppDomainPtr.Value);
        Assert.True(adData.AssemblyCount >= 1, $"Expected at least one assembly, got {adData.AssemblyCount}");
    }
}
