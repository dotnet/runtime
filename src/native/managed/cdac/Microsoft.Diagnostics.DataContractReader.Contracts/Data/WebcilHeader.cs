// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Generated;

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.WebcilHeader))]
internal sealed partial class WebcilHeader : IData<WebcilHeader>
{
    // See docs/design/mono/webcil.md for the layout.
    [FieldOffset(4)] public ushort VersionMajor { get; }
    [FieldOffset(8)] public ushort CoffSections { get; }

    public uint Size => VersionMajor >= 1 ? (uint)32 : (uint)28;
}
