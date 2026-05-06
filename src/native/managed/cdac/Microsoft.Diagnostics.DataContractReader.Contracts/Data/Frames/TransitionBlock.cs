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
        ReturnAddress = target.ReadPointerField(address, type, nameof(ReturnAddress));
        CalleeSavedRegisters = address + (ulong)type.Fields[nameof(CalleeSavedRegisters)].Offset;

        // These are computed positions within the TransitionBlock.
        ArgumentRegisters = address + (ulong)type.Fields[nameof(ArgumentRegisters)].Offset;
        FirstGCRefMapSlot = address + (ulong)type.Fields[nameof(FirstGCRefMapSlot)].Offset;
    }

    public TargetPointer ReturnAddress { get; }
    public TargetPointer CalleeSavedRegisters { get; }

    /// <summary>
    /// Address of the argument registers area within this TransitionBlock.
    /// </summary>
    public TargetPointer ArgumentRegisters { get; }

    /// <summary>
    /// Address of the first slot covered by the GCRefMap within this TransitionBlock.
    /// </summary>
    public TargetPointer FirstGCRefMapSlot { get; }
}
