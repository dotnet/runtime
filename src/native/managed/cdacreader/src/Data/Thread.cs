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

        Id = target.Read<uint>(address + (ulong)type.Fields[nameof(Id)].Offset);
        OSId = target.ReadNUInt(address + (ulong)type.Fields[nameof(OSId)].Offset);
        State = target.Read<uint>(address + (ulong)type.Fields[nameof(State)].Offset);
        PreemptiveGCDisabled = target.Read<uint>(address + (ulong)type.Fields[nameof(PreemptiveGCDisabled)].Offset);

        TargetPointer runtimeThreadLocalsPointer = target.ReadPointer(address + (ulong)type.Fields[nameof(RuntimeThreadLocals)].Offset);
        if (runtimeThreadLocalsPointer != TargetPointer.Null)
            RuntimeThreadLocals = target.ProcessedData.GetOrAdd<RuntimeThreadLocals>(runtimeThreadLocalsPointer);

        Frame = target.ReadPointer(address + (ulong)type.Fields[nameof(Frame)].Offset);

        // TEB does not exist on certain platforms
        TEB = type.Fields.TryGetValue(nameof(TEB), out Target.FieldInfo fieldInfo)
            ? target.ReadPointer(address + (ulong)fieldInfo.Offset)
            : TargetPointer.Null;
        LastThrownObject = target.ProcessedData.GetOrAdd<ObjectHandle>(
            target.ReadPointer(address + (ulong)type.Fields[nameof(LastThrownObject)].Offset));
        LinkNext = target.ReadPointer(address + (ulong)type.Fields[nameof(LinkNext)].Offset);

        // Address of the exception tracker - how it should be read depends on EH funclets feature global value
        ExceptionTracker = address + (ulong)type.Fields[nameof(ExceptionTracker)].Offset;
    }

    public uint Id { get; init; }
    public TargetNUInt OSId { get; init; }
    public uint State { get; init; }
    public uint PreemptiveGCDisabled { get; init; }
    public RuntimeThreadLocals? RuntimeThreadLocals { get; init; }
    public TargetPointer Frame { get; init; }
    public TargetPointer TEB { get; init; }
    public ObjectHandle LastThrownObject { get; init; }
    public TargetPointer LinkNext { get; init; }
    public TargetPointer ExceptionTracker { get; init; }
}
