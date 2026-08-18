// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.Thread))]
internal sealed partial class Thread : IData<Thread>
{
    [Field] public partial uint Id { get; }
    [Field] public partial TargetNUInt OSId { get; }
    [Field] public partial uint State { get; }
    [Field(Writable = true)] public partial uint DebuggerControlledThreadState { get; private set; }
    [Field] public partial uint PreemptiveGCDisabled { get; }
    [Field] public partial TargetPointer Frame { get; }
    [Field] public partial TargetPointer GCFrame { get; }
    [Field] public partial TargetPointer CachedStackBase { get; }
    [Field] public partial TargetPointer CachedStackLimit { get; }
    [Field] public partial ObjectHandle ExposedObject { get; }
    [Field] public partial ObjectHandle LastThrownObject { get; }
    [Field] public partial uint LastThrownObjectIsUnhandled { get; }
    [Field] public partial TargetPointer LinkNext { get; }

    [FieldAddress]
    public partial TargetPointer ExceptionTracker { get; }

    // Descriptor-optional: not present on non-Windows platforms.
    [Field] public partial TargetPointer? UEWatsonBucketTrackerBuckets { get; }
    [Field] public partial TargetPointer ThreadLocalDataPtr { get; }
    [Field] public partial TargetPointer DebuggerFilterContext { get; }
    [Field] public partial uint InteropDebuggingHijacked { get; }
    [Field] public partial ObjectHandle CurrentCustomDebuggerNotification { get; }
    [CustomInit(nameof(InitRuntimeThreadLocals))] public partial RuntimeThreadLocals? RuntimeThreadLocals { get; }

    // Descriptor-optional: not present on all platforms.
    [CustomInit(nameof(InitThreadHandle))] public partial TargetPointer ThreadHandle { get; }

    [DataDescriptorDependency(nameof(RuntimeThreadLocals), "pointer")]
    private partial RuntimeThreadLocals? InitRuntimeThreadLocals(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.Thread);
        TargetPointer rtlPointer = target.ReadPointerField(address, type, nameof(RuntimeThreadLocals));
        return rtlPointer != TargetPointer.Null
            ? target.ProcessedData.GetOrAdd<RuntimeThreadLocals>(rtlPointer)
            : null;
    }
    [DataDescriptorDependency(nameof(ThreadHandle), "pointer")]
    private partial TargetPointer InitThreadHandle(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.Thread);
        return target.ReadPointerFieldOrNull(address, type, nameof(ThreadHandle));
    }
}
