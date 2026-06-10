// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.DynamicMetadata))]
internal sealed partial class DynamicMetadata : IData<DynamicMetadata>
{
    [Field] public uint Size { get; }

    [FieldAddress]
    public TargetPointer Data { get; }
}
