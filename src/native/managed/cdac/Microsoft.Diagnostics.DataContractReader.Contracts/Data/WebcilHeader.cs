// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.WebcilHeader))]
internal sealed partial class WebcilHeader : IData<WebcilHeader>
{
    // See docs/design/mono/webcil.md for the layout.
    [RawOffset(4)] public ushort VersionMajor { get; }
    [RawOffset(8)] public ushort CoffSections { get; }

    public uint Size => VersionMajor >= 1 ? (uint)32 : (uint)28;
}
