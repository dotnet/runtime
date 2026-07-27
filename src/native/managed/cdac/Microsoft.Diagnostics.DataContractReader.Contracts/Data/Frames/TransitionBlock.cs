// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.TransitionBlock))]
internal partial class TransitionBlock : IData<TransitionBlock>
{
    [Field] public partial TargetCodePointer ReturnAddress { get; }

    [FieldAddress]
    public partial TargetPointer CalleeSavedRegisters { get; }

    /// <summary>
    /// Address of the argument registers area within this TransitionBlock.
    /// </summary>
    [FieldAddress]
    public partial TargetPointer ArgumentRegisters { get; }

    /// <summary>
    /// Address of the first slot covered by the GCRefMap within this TransitionBlock.
    /// </summary>
    [FieldAddress]
    public partial TargetPointer FirstGCRefMapSlot { get; }

    /// <summary>
    /// Address just past the end of the TransitionBlock, where caller-pushed
    /// stack arguments begin. On x86 this is where GCRefMap positions
    /// >= NUM_ARGUMENT_REGISTERS map to (see native OffsetFromGCRefMapPos).
    /// Computed as <c>address + sizeof(TransitionBlock)</c>, mirrors native
    /// <c>TransitionBlock::GetOffsetOfArgs()</c>.
    /// </summary>
    [InstanceDataStart]
    public partial TargetPointer OffsetOfArgs { get; }
}
