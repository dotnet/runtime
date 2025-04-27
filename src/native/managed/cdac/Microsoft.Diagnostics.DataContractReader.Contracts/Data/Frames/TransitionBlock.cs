// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal class TransitionBlock : IData<TransitionBlock>
{
    static TransitionBlock IData<TransitionBlock>.Create(Target target, TargetPointer address)
        => new TransitionBlock(target, address);

    public TransitionBlock(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.TransitionBlock);
        ReturnAddress = target.ReadPointer(address + (ulong)type.Fields[nameof(ReturnAddress)].Offset);
        CalleeSavedRegisters = address + (ulong)type.Fields[nameof(CalleeSavedRegisters)].Offset;
    }

    public TargetPointer ReturnAddress { get; }
    public TargetPointer CalleeSavedRegisters { get; }
}
