// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.FieldDesc))]
internal sealed partial class FieldDesc : IData<FieldDesc>
{
    [Field] public uint DWord1 { get; }
    [Field] public uint DWord2 { get; }
    [Field] public TargetPointer MTOfEnclosingClass { get; }
}
