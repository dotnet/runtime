// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ILCodeVersioningState))]
internal sealed partial class ILCodeVersioningState : IData<ILCodeVersioningState>
{
    [Field] public partial TargetPointer FirstVersionNode { get; }
    [Field] public partial uint ActiveVersionKind { get; }
    [Field] public partial TargetPointer ActiveVersionNode { get; }
    [Field] public partial TargetPointer ActiveVersionModule { get; }
    [Field] public partial uint ActiveVersionMethodDef { get; }
}
