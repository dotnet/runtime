// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.PEImageLayout))]
internal sealed partial class PEImageLayout : IData<PEImageLayout>
{
    [Field] public TargetPointer Base { get; }
    [Field] public uint Size { get; }
    [Field] public uint Flags { get; }
    [Field] public uint Format { get; }
}
