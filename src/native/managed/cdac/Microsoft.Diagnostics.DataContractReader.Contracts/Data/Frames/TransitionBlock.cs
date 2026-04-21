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

        if (type.Fields.ContainsKey(nameof(ArgumentRegisters)))
        {
            ArgumentRegisters = address + (ulong)type.Fields[nameof(ArgumentRegisters)].Offset;
        }

        // These are offsets relative to the TransitionBlock pointer, stored as field "offsets"
        // in the data descriptor. They represent computed layout positions, not actual memory reads.
        FirstGCRefMapSlot = (uint)type.Fields[nameof(FirstGCRefMapSlot)].Offset;
        ArgumentRegistersOffset = (uint)type.Fields[nameof(ArgumentRegistersOffset)].Offset;
        OffsetOfFloatArgumentRegisters = type.Fields[nameof(OffsetOfFloatArgumentRegisters)].Offset;
    }

    public TargetPointer ReturnAddress { get; }
    public TargetPointer CalleeSavedRegisters { get; }

    /// <summary>
    /// Only available on ARM targets.
    /// </summary>
    public TargetPointer? ArgumentRegisters { get; }

    /// <summary>
    /// Offset to the first slot covered by the GCRefMap, relative to the TransitionBlock pointer.
    /// </summary>
    public uint FirstGCRefMapSlot { get; }

    /// <summary>
    /// Offset to the argument registers area, relative to the TransitionBlock pointer.
    /// </summary>
    public uint ArgumentRegistersOffset { get; }

    /// <summary>
    /// Offset to the float argument registers area, relative to the TransitionBlock pointer.
    /// Negative on most platforms (float regs are stored before the TransitionBlock).
    /// Zero on platforms without float argument registers (x86, Windows x64).
    /// </summary>
    public int OffsetOfFloatArgumentRegisters { get; }
}
