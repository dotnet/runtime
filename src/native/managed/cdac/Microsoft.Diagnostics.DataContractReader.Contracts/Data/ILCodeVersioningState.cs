// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ILCodeVersioningState))]
internal sealed partial class ILCodeVersioningState : IData<ILCodeVersioningState>
{
    [Field] public TargetPointer FirstVersionNode { get; set; }
    [Field] public uint ActiveVersionKind { get; set; }
    [Field] public TargetPointer ActiveVersionNode { get; set; }
    [Field] public TargetPointer ActiveVersionModule { get; set; }
    [Field] public uint ActiveVersionMethodDef { get; set; }
}
