// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.Frame))]
internal sealed partial class Frame : IData<Frame>
{
    [Field] public partial TargetPointer Next { get; }
    [CustomInit(nameof(InitIdentifier))] public partial TargetPointer Identifier { get; }

    private partial TargetPointer InitIdentifier(Target target, TargetPointer address)
    {
        return target.ReadPointer(address);
    }
}
