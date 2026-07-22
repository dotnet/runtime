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
