// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.InterpMethodContextFrame))]
internal sealed partial class InterpMethodContextFrame : IData<InterpMethodContextFrame>
{
    [Field] public TargetPointer StartIp { get; }
    [Field] public TargetPointer ParentPtr { get; }
    [Field] public TargetPointer Ip { get; }
    [Field] public TargetPointer NextPtr { get; }
    [Field] public TargetPointer Stack { get; }
}
