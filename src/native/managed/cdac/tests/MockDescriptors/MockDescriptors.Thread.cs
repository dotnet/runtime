// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

internal sealed class MockExceptionInfo : TypedView
{
    private const string PreviousNestedInfoFieldName = "PreviousNestedInfo";
    private const string ThrownObjectHandleFieldName = "ThrownObjectHandle";
    private const string ExceptionFlagsFieldName = "ExceptionFlags";
    private const string StackLowBoundFieldName = "StackLowBound";
    private const string StackHighBoundFieldName = "StackHighBound";
    private const string ExceptionWatsonBucketTrackerBucketsFieldName = "ExceptionWatsonBucketTrackerBuckets";
    private const string PassNumberFieldName = "PassNumber";
    private const string CSFEHClauseFieldName = "CSFEHClause";
    private const string CSFEnclosingClauseFieldName = "CSFEnclosingClause";
    private const string CallerOfActualHandlerFrameFieldName = "CallerOfActualHandlerFrame";

    public static Layout<MockExceptionInfo> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("ExceptionInfo", architecture)
            .AddPointerField(PreviousNestedInfoFieldName)
            .AddPointerField(ThrownObjectHandleFieldName)
            .AddUInt32Field(ExceptionFlagsFieldName)
            .AddPointerField(StackLowBoundFieldName)
            .AddPointerField(StackHighBoundFieldName)
            .AddPointerField(ExceptionWatsonBucketTrackerBucketsFieldName)
            .AddByteField(PassNumberFieldName)
            .AddPointerField(CSFEHClauseFieldName)
            .AddPointerField(CSFEnclosingClauseFieldName)
            .AddPointerField(CallerOfActualHandlerFrameFieldName)
            .Build<MockExceptionInfo>();

    public ulong ThrownObjectHandle
    {
        get => ReadPointerField(ThrownObjectHandleFieldName);
        set => WritePointerField(ThrownObjectHandleFieldName, value);
    }
}

internal sealed class MockGCAllocContext : TypedView
{
    private const string PointerFieldName = "Pointer";
    private const string LimitFieldName = "Limit";
    private const string AllocBytesFieldName = "AllocBytes";
    private const string AllocBytesLohFieldName = "AllocBytesLoh";

    public static Layout<MockGCAllocContext> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("GCAllocContext", architecture)
            .AddPointerField(PointerFieldName)
            .AddPointerField(LimitFieldName)
            .AddInt64Field(AllocBytesFieldName)
            .AddInt64Field(AllocBytesLohFieldName)
            .Build<MockGCAllocContext>();

    public ulong Pointer
    {
        get => ReadPointerField(PointerFieldName);
        set => WritePointerField(PointerFieldName, value);
    }

    public ulong Limit
    {
        get => ReadPointerField(LimitFieldName);
        set => WritePointerField(LimitFieldName, value);
    }

    public long AllocBytes
    {
        get => ReadInt64Field(AllocBytesFieldName);
        set => WriteInt64Field(AllocBytesFieldName, value);
    }

    public long AllocBytesLoh
    {
        get => ReadInt64Field(AllocBytesLohFieldName);
        set => WriteInt64Field(AllocBytesLohFieldName, value);
    }
}

internal sealed class MockEEAllocContext : TypedView
{
    private const string GCAllocationContextFieldName = "GCAllocationContext";

    public static Layout<MockEEAllocContext> CreateLayout(MockTarget.Architecture architecture, Layout<MockGCAllocContext> gcAllocContextLayout)
        => new SequentialLayoutBuilder("EEAllocContext", architecture)
            .AddField(GCAllocationContextFieldName, gcAllocContextLayout.Size)
            .Build<MockEEAllocContext>();

    public ulong GCAllocationContextAddress => GetFieldAddress(GCAllocationContextFieldName);
}

internal sealed class MockRuntimeThreadLocals : TypedView
{
    private const string AllocContextFieldName = "AllocContext";

    private Layout<MockGCAllocContext> GCAllocContextLayout { get; set; } = null!;

    public static Layout<MockRuntimeThreadLocals> CreateLayout(MockTarget.Architecture architecture, Layout<MockEEAllocContext> eeAllocContextLayout)
        => new SequentialLayoutBuilder("RuntimeThreadLocals", architecture)
            .AddField(AllocContextFieldName, eeAllocContextLayout.Size)
            .Build<MockRuntimeThreadLocals>();

    public ulong AllocContextAddress => GetFieldAddress(AllocContextFieldName);

    internal void SetGCAllocContextLayout(Layout<MockGCAllocContext> gcAllocContextLayout)
        => GCAllocContextLayout = gcAllocContextLayout;

    public MockGCAllocContext GCAllocContext
    {
        get
        {
            int allocContextOffset = checked((int)(AllocContextAddress - Address));
            return GCAllocContextLayout.Create(Memory.Slice(allocContextOffset, GCAllocContextLayout.Size), AllocContextAddress);
        }
    }
}

internal sealed class MockThreadStore : TypedView
{
    private const string ThreadCountFieldName = "ThreadCount";
    private const string FirstThreadLinkFieldName = "FirstThreadLink";
    private const string UnstartedCountFieldName = "UnstartedCount";
    private const string BackgroundCountFieldName = "BackgroundCount";
    private const string PendingCountFieldName = "PendingCount";
    private const string DeadCountFieldName = "DeadCount";

    public static Layout<MockThreadStore> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("ThreadStore", architecture)
            .AddUInt32Field(ThreadCountFieldName)
            .AddPointerField(FirstThreadLinkFieldName)
            .AddUInt32Field(UnstartedCountFieldName)
            .AddUInt32Field(BackgroundCountFieldName)
            .AddUInt32Field(PendingCountFieldName)
            .AddUInt32Field(DeadCountFieldName)
            .Build<MockThreadStore>();

    public ulong FirstThreadLink
    {
        get => ReadPointerField(FirstThreadLinkFieldName);
        set => WritePointerField(FirstThreadLinkFieldName, value);
    }

    public int ThreadCount
    {
        set => WriteUInt32Field(ThreadCountFieldName, checked((uint)value));
    }

    public int UnstartedCount
    {
        set => WriteUInt32Field(UnstartedCountFieldName, checked((uint)value));
    }

    public int BackgroundCount
    {
        set => WriteUInt32Field(BackgroundCountFieldName, checked((uint)value));
    }

    public int PendingCount
    {
        set => WriteUInt32Field(PendingCountFieldName, checked((uint)value));
    }

    public int DeadCount
    {
        set => WriteUInt32Field(DeadCountFieldName, checked((uint)value));
    }
}

internal sealed class MockThread : TypedView
{
    private const string IdFieldName = "Id";
    private const string OSIdFieldName = "OSId";
    private const string StateFieldName = "State";
    private const string PreemptiveGCDisabledFieldName = "PreemptiveGCDisabled";
    private const string RuntimeThreadLocalsFieldName = "RuntimeThreadLocals";
    private const string FrameFieldName = "Frame";
    private const string CachedStackBaseFieldName = "CachedStackBase";
    private const string CachedStackLimitFieldName = "CachedStackLimit";
    private const string ExposedObjectFieldName = "ExposedObject";
    private const string LastThrownObjectFieldName = "LastThrownObject";
    private const string LastThrownObjectIsUnhandledFieldName = "LastThrownObjectIsUnhandled";
    private const string CurrentCustomDebuggerNotificationFieldName = "CurrentCustomDebuggerNotification";
    private const string LinkNextFieldName = "LinkNext";
    private const string ExceptionTrackerFieldName = "ExceptionTracker";
    private const string ThreadLocalDataPtrFieldName = "ThreadLocalDataPtr";
    private const string UEWatsonBucketTrackerBucketsFieldName = "UEWatsonBucketTrackerBuckets";
    private const string DebuggerFilterContextFieldName = "DebuggerFilterContext";
    private const string ProfilerFilterContextFieldName = "ProfilerFilterContext";

    public static Layout<MockThread> CreateLayout(MockTarget.Architecture architecture, bool hasProfilingSupport = true)
    {
        SequentialLayoutBuilder layoutBuilder = new SequentialLayoutBuilder("Thread", architecture)
            .AddUInt32Field(IdFieldName)
            .AddPointerField(OSIdFieldName)
            .AddUInt32Field(StateFieldName)
            .AddUInt32Field(PreemptiveGCDisabledFieldName)
            .AddPointerField(RuntimeThreadLocalsFieldName)
            .AddPointerField(FrameFieldName)
            .AddPointerField(CachedStackBaseFieldName)
            .AddPointerField(CachedStackLimitFieldName)
            .AddPointerField(ExposedObjectFieldName)
            .AddPointerField(LastThrownObjectFieldName)
            .AddUInt32Field(LastThrownObjectIsUnhandledFieldName)
            .AddPointerField(CurrentCustomDebuggerNotificationFieldName)
            .AddPointerField(LinkNextFieldName)
            .AddPointerField(ExceptionTrackerFieldName)
            .AddPointerField(ThreadLocalDataPtrFieldName)
            .AddPointerField(UEWatsonBucketTrackerBucketsFieldName)
            .AddPointerField(DebuggerFilterContextFieldName);

        if (hasProfilingSupport)
        {
            layoutBuilder.AddPointerField(ProfilerFilterContextFieldName);
        }

        return layoutBuilder.Build<MockThread>();
    }

    public uint Id
    {
        get => ReadUInt32Field(IdFieldName);
        set => WriteUInt32Field(IdFieldName, value);
    }

    public ulong OSId
    {
        get => ReadPointerField(OSIdFieldName);
        set => WritePointerField(OSIdFieldName, value);
    }

    public ulong RuntimeThreadLocals
    {
        get => ReadPointerField(RuntimeThreadLocalsFieldName);
        set => WritePointerField(RuntimeThreadLocalsFieldName, value);
    }

    public ulong CachedStackBase
    {
        get => ReadPointerField(CachedStackBaseFieldName);
        set => WritePointerField(CachedStackBaseFieldName, value);
    }

    public ulong CachedStackLimit
    {
        get => ReadPointerField(CachedStackLimitFieldName);
        set => WritePointerField(CachedStackLimitFieldName, value);
    }

    public ulong LinkNext
    {
        get => ReadPointerField(LinkNextFieldName);
        set => WritePointerField(LinkNextFieldName, value);
    }

    public ulong ExceptionTracker
    {
        get => ReadPointerField(ExceptionTrackerFieldName);
        set => WritePointerField(ExceptionTrackerFieldName, value);
    }

    public ulong ExposedObject
    {
        get => ReadPointerField(ExposedObjectFieldName);
        set => WritePointerField(ExposedObjectFieldName, value);
    }

    public uint LastThrownObjectIsUnhandled
    {
        get => ReadUInt32Field(LastThrownObjectIsUnhandledFieldName);
        set => WriteUInt32Field(LastThrownObjectIsUnhandledFieldName, value);
    }

    public ulong CurrentCustomDebuggerNotification
    {
        get => ReadPointerField(CurrentCustomDebuggerNotificationFieldName);
        set => WritePointerField(CurrentCustomDebuggerNotificationFieldName, value);
    }

    public ulong FrameAddress => GetFieldAddress(FrameFieldName);
}

internal sealed class MockThreadBuilder
{
    private const ulong DefaultAllocationRangeStart = 0x0003_0000;
    private const ulong DefaultAllocationRangeEnd = 0x0004_0000;

    internal MockMemorySpace.Builder Builder { get; }
    internal Layout<MockExceptionInfo> ExceptionInfoLayout { get; }
    internal Layout<MockThread> ThreadLayout { get; }
    internal Layout<MockThreadStore> ThreadStoreLayout { get; }
    internal Layout<MockGCAllocContext> GCAllocContextLayout { get; }
    internal Layout<MockEEAllocContext> EEAllocContextLayout { get; }
    internal Layout<MockRuntimeThreadLocals> RuntimeThreadLocalsLayout { get; }

    public ulong ThreadStoreGlobalAddress { get; }
    public ulong FinalizerThreadGlobalAddress { get; }
    public ulong GCThreadGlobalAddress { get; }
    internal MockThreadStore ThreadStore => _threadStore;
    internal ulong ThreadStoreAddress { get; }
    internal ulong FinalizerThreadAddress { get; }
    internal ulong GCThreadAddress { get; }

    private readonly MockMemorySpace.BumpAllocator _allocator;
    private readonly MockThreadStore _threadStore;

    private MockThread? _previousThread;

    public MockThreadBuilder(MockMemorySpace.Builder builder, bool hasProfilingSupport = true)
        : this(builder, (DefaultAllocationRangeStart, DefaultAllocationRangeEnd), hasProfilingSupport)
    {
    }

    public MockThreadBuilder(MockMemorySpace.Builder builder, (ulong Start, ulong End) allocationRange, bool hasProfilingSupport = true)
    {
        Builder = builder;
        _allocator = Builder.CreateAllocator(allocationRange.Start, allocationRange.End);

        TargetTestHelpers helpers = builder.TargetTestHelpers;
        ExceptionInfoLayout = MockExceptionInfo.CreateLayout(helpers.Arch);
        ThreadLayout = MockThread.CreateLayout(helpers.Arch, hasProfilingSupport);
        ThreadStoreLayout = MockThreadStore.CreateLayout(helpers.Arch);
        GCAllocContextLayout = MockGCAllocContext.CreateLayout(helpers.Arch);
        EEAllocContextLayout = MockEEAllocContext.CreateLayout(helpers.Arch, GCAllocContextLayout);
        RuntimeThreadLocalsLayout = MockRuntimeThreadLocals.CreateLayout(helpers.Arch, EEAllocContextLayout);

        MockMemorySpace.HeapFragment threadStoreGlobal = _allocator.Allocate((ulong)helpers.PointerSize, "[global pointer] ThreadStore");
        MockMemorySpace.HeapFragment threadStore = _allocator.Allocate((ulong)ThreadStoreLayout.Size, "ThreadStore");
        helpers.WritePointer(threadStoreGlobal.Data, threadStore.Address);
        ThreadStoreGlobalAddress = threadStoreGlobal.Address;
        ThreadStoreAddress = threadStore.Address;
        _threadStore = ThreadStoreLayout.Create(threadStore);

        FinalizerThreadGlobalAddress = AddPointerGlobal("Finalizer thread", out ulong finalizerThreadAddress, (ulong)ThreadLayout.Size, "Finalizer thread");
        FinalizerThreadAddress = finalizerThreadAddress;

        GCThreadGlobalAddress = AddPointerGlobal("GC thread", out ulong gcThreadAddress, (ulong)ThreadLayout.Size, "GC thread");
        GCThreadAddress = gcThreadAddress;
    }

    internal MockThread AddThread(uint id, ulong osId)
        => AddThread(id, osId, allocBytes: 0, allocBytesLoh: 0);

    internal MockThread AddThread(uint id, ulong osId, long allocBytes, long allocBytesLoh)
    {
        MockExceptionInfo exceptionInfo = ExceptionInfoLayout.Create(_allocator.Allocate((ulong)ExceptionInfoLayout.Size, "ExceptionInfo"));
        MockRuntimeThreadLocals runtimeThreadLocals = RuntimeThreadLocalsLayout.Create(_allocator.Allocate((ulong)RuntimeThreadLocalsLayout.Size, "RuntimeThreadLocals"));
        runtimeThreadLocals.SetGCAllocContextLayout(GCAllocContextLayout);
        MockGCAllocContext gcAllocContext = runtimeThreadLocals.GCAllocContext;
        gcAllocContext.AllocBytes = allocBytes;
        gcAllocContext.AllocBytesLoh = allocBytesLoh;

        MockThread thread = ThreadLayout.Create(_allocator.Allocate((ulong)ThreadLayout.Size, "Thread"));
        thread.Id = id;
        thread.OSId = osId;
        thread.ExceptionTracker = exceptionInfo.Address;
        thread.RuntimeThreadLocals = runtimeThreadLocals.Address;

        if (_previousThread is not null)
        {
            _previousThread.LinkNext = thread.Address;
        }
        else
        {
            _threadStore.FirstThreadLink = thread.Address;
        }

        _previousThread = thread;
        return thread;
    }

    internal MockRuntimeThreadLocals GetRuntimeThreadLocals(MockThread thread)
    {
        MockRuntimeThreadLocals runtimeThreadLocals = RuntimeThreadLocalsLayout.Create(BorrowMemory(thread.RuntimeThreadLocals, RuntimeThreadLocalsLayout.Size), thread.RuntimeThreadLocals);
        runtimeThreadLocals.SetGCAllocContextLayout(GCAllocContextLayout);
        return runtimeThreadLocals;
    }

    internal MockExceptionInfo GetExceptionInfo(MockThread thread)
        => ExceptionInfoLayout.Create(BorrowMemory(thread.ExceptionTracker, ExceptionInfoLayout.Size), thread.ExceptionTracker);

    private ulong AddPointerGlobal(string name, out ulong value, ulong pointeeSize, string pointeeName)
    {
        TargetTestHelpers helpers = Builder.TargetTestHelpers;
        MockMemorySpace.HeapFragment global = _allocator.Allocate((ulong)helpers.PointerSize, $"[global pointer] {name}");
        MockMemorySpace.HeapFragment pointee = _allocator.Allocate(pointeeSize, pointeeName);
        helpers.WritePointer(global.Data, pointee.Address);
        value = pointee.Address;
        return global.Address;
    }

    private Memory<byte> BorrowMemory(ulong address, int length)
        => Builder.BorrowAddressRangeMemory(address, length);
}
