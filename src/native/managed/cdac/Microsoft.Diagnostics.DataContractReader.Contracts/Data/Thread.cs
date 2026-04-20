// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class Thread : IData<Thread>
{
    static Thread IData<Thread>.Create(Target target, TargetPointer address)
        => new Thread(target, address);

    public Thread(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.Thread);

        Id = target.ReadField<uint>(address, type, nameof(Id));
        OSId = target.ReadNUIntField(address, type, nameof(OSId));
        State = target.ReadField<uint>(address, type, nameof(State));
        PreemptiveGCDisabled = target.ReadField<uint>(address, type, nameof(PreemptiveGCDisabled));

        RuntimeThreadLocals = target.ReadDataFieldPointer<RuntimeThreadLocals>(address, type, nameof(RuntimeThreadLocals));

        Frame = target.ReadPointerField(address, type, nameof(Frame));
        CachedStackBase = target.ReadPointerField(address, type, nameof(CachedStackBase));
        CachedStackLimit = target.ReadPointerField(address, type, nameof(CachedStackLimit));

        ExposedObject = target.ReadPointerField(address, type, nameof(ExposedObject));
        LastThrownObject = target.ProcessedData.GetOrAdd<ObjectHandle>(
            target.ReadPointerField(address, type, nameof(LastThrownObject)));
        LastThrownObjectIsUnhandled = target.ReadField<uint>(address, type, nameof(LastThrownObjectIsUnhandled));
        LinkNext = target.ReadPointerField(address, type, nameof(LinkNext));

        // Address of the exception tracker
        ExceptionTracker = address + (ulong)type.Fields[nameof(ExceptionTracker)].Offset;
        // UEWatsonBucketTrackerBuckets does not exist on non-Windows platforms
        UEWatsonBucketTrackerBuckets = target.ReadPointerFieldOrNull(address, type, nameof(UEWatsonBucketTrackerBuckets));
        ThreadLocalDataPtr = target.ReadPointerField(address, type, nameof(ThreadLocalDataPtr));
        DebuggerFilterContext = target.ReadPointerField(address, type, nameof(DebuggerFilterContext));
        ProfilerFilterContext = target.ReadPointerFieldOrNull(address, type, nameof(ProfilerFilterContext));
        CurrentCustomDebuggerNotification = target.ReadPointerField(address, type, nameof(CurrentCustomDebuggerNotification));
    }

    public uint Id { get; init; }
    public TargetNUInt OSId { get; init; }
    public uint State { get; init; }
    public uint PreemptiveGCDisabled { get; init; }
    public RuntimeThreadLocals? RuntimeThreadLocals { get; init; }
    public TargetPointer Frame { get; init; }
    public TargetPointer CachedStackBase { get; init; }
    public TargetPointer CachedStackLimit { get; init; }
    public TargetPointer ExposedObject { get; init; }
    public ObjectHandle LastThrownObject { get; init; }
    public uint LastThrownObjectIsUnhandled { get; init; }
    public TargetPointer LinkNext { get; init; }
    public TargetPointer ExceptionTracker { get; init; }
    public TargetPointer UEWatsonBucketTrackerBuckets { get; init; }
    public TargetPointer ThreadLocalDataPtr { get; init; }
    public TargetPointer DebuggerFilterContext { get; init; }
    public TargetPointer ProfilerFilterContext { get; init; }
    public TargetPointer CurrentCustomDebuggerNotification { get; init; }
}
