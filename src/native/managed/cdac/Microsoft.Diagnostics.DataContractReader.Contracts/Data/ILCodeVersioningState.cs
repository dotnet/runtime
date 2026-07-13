// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ILCodeVersioningState))]
internal sealed partial class ILCodeVersioningState : IData<ILCodeVersioningState>
{
    [Field] public partial TargetPointer FirstVersionNode { get; set; }
    [Field] public partial uint ActiveVersionKind { get; set; }
    [Field] public partial TargetPointer ActiveVersionNode { get; set; }
    [Field] public partial TargetPointer ActiveVersionModule { get; set; }
    [Field] public partial uint ActiveVersionMethodDef { get; set; }
}
