// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.TLSIndex))]
internal sealed partial class TLSIndex : IData<TLSIndex>
{
    [Field] public uint TLSIndexRawIndex { get; }

    public int IndexOffset => (int)(TLSIndexRawIndex & 0xFFFFFF);
    public int IndexType => (int)(TLSIndexRawIndex >> 24);
    public bool IsAllocated => TLSIndexRawIndex != 0xFFFFFFFF;
}
