// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.Frame))]
internal sealed partial class Frame : IData<Frame>
{
    [Field] public TargetPointer Next { get; }
    public TargetPointer Identifier { get; private set; }

    partial void OnInit(Target target, TargetPointer address)
    {
        Identifier = target.ReadPointer(address);
    }
}
