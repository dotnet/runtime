// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.DynamicILBlobTable))]
internal sealed partial class DynamicILBlobEntry : IData<DynamicILBlobEntry>
{
    [Field] public uint EntryMethodToken { get; }
    [Field] public TargetPointer EntryIL { get; }

    public DynamicILBlobEntry(uint entryMethodToken, TargetPointer entryIL)
    {
        EntryMethodToken = entryMethodToken;
        EntryIL = entryIL;
    }
}
