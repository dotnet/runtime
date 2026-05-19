// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Generated;

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.WebcilSectionHeader))]
internal sealed partial class WebcilSectionHeader : IData<WebcilSectionHeader>
{
    [FieldOffset(0)]
    public uint VirtualSize { get; }

    [FieldOffset(4)]
    public uint VirtualAddress { get; }

    [FieldOffset(8)]
    public uint SizeOfRawData { get; }

    [FieldOffset(12)]
    public uint PointerToRawData { get; }
}
