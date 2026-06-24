// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.TransitionBlock))]
internal partial class TransitionBlock : IData<TransitionBlock>
{
    [Field] public TargetCodePointer ReturnAddress { get; }

    [FieldAddress]
    public TargetPointer CalleeSavedRegisters { get; }

    /// <summary>
    /// Address of the argument registers area within this TransitionBlock.
    /// </summary>
    [FieldAddress]
    public TargetPointer ArgumentRegisters { get; }

    /// <summary>
    /// Address of the first slot covered by the GCRefMap within this TransitionBlock.
    /// </summary>
    [FieldAddress]
    public TargetPointer FirstGCRefMapSlot { get; }
}
