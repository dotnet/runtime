// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.WebcilSectionHeader))]
internal sealed partial class WebcilSectionHeader : IData<WebcilSectionHeader>
{
    [RawOffset(0)]
    public partial uint VirtualSize { get; }

    [RawOffset(4)]
    public partial uint VirtualAddress { get; }

    [RawOffset(8)]
    public partial uint SizeOfRawData { get; }

    [RawOffset(12)]
    public partial uint PointerToRawData { get; }
}
