// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.Thread))]
internal sealed partial class Thread : IData<Thread>
{
    [Field] public uint Id { get; }
    [Field] public TargetNUInt OSId { get; }
    [Field] public uint State { get; }
    [Field(Writable = true)] public uint DebuggerControlledThreadState { get; private set; }
    [Field] public uint PreemptiveGCDisabled { get; }
    [Field] public TargetPointer Frame { get; }
    [Field] public TargetPointer GCFrame { get; }
    [Field] public TargetPointer CachedStackBase { get; }
    [Field] public TargetPointer CachedStackLimit { get; }
    [Field] public ObjectHandle ExposedObject { get; }
    [Field] public ObjectHandle LastThrownObject { get; }
    [Field] public uint LastThrownObjectIsUnhandled { get; }
    [Field] public TargetPointer LinkNext { get; }

    [FieldAddress]
    public TargetPointer ExceptionTracker { get; }

    // Descriptor-optional: not present on non-Windows platforms.
    [Field] public TargetPointer? UEWatsonBucketTrackerBuckets { get; }
    [Field] public TargetPointer ThreadLocalDataPtr { get; }
    [Field] public TargetPointer DebuggerFilterContext { get; }
    [Field] public uint InteropDebuggingHijacked { get; }
    [Field] public ObjectHandle CurrentCustomDebuggerNotification { get; }

    public RuntimeThreadLocals? RuntimeThreadLocals { get; private set; }
    // Descriptor-optional: not present on all platforms.
    public TargetPointer ThreadHandle { get; private set; }

    partial void OnInit(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.Thread);

        TargetPointer rtlPointer = target.ReadPointerField(address, type, nameof(RuntimeThreadLocals));
        if (rtlPointer != TargetPointer.Null)
            RuntimeThreadLocals = target.ProcessedData.GetOrAdd<RuntimeThreadLocals>(rtlPointer);

        ThreadHandle = target.ReadPointerFieldOrNull(address, type, nameof(ThreadHandle));
    }
}
