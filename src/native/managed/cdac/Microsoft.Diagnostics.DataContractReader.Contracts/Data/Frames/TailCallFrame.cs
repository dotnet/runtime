// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal class TailCallFrame : IData<TailCallFrame>
{
    static TailCallFrame IData<TailCallFrame>.Create(Target target, TargetPointer address)
        => new TailCallFrame(target, address);

    public TailCallFrame(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.TailCallFrame);
        Address = address;
        CalleeSavedRegisters = address + (ulong)type.Fields[nameof(CalleeSavedRegisters)].Offset;
        ReturnAddress = target.ReadPointer(address + (ulong)type.Fields[nameof(ReturnAddress)].Offset);
    }

    public TargetPointer Address { get; }
    public TargetPointer CalleeSavedRegisters { get; }
    public TargetPointer ReturnAddress { get; }
}
