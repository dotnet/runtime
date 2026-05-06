// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class WebcilHeader : IData<WebcilHeader>
{
    static WebcilHeader IData<WebcilHeader>.Create(Target target, TargetPointer address) => new WebcilHeader(target, address);
    public WebcilHeader(Target target, TargetPointer address)
    {
        CoffSections = target.Read<ushort>(address + 8); // See docs/design/mono/webcil.md
        VersionMajor = target.Read<ushort>(address + 4); // See docs/design/mono/webcil.md
    }

    public ushort CoffSections { get; init; }
    public ushort VersionMajor { get; init; }
    public uint Size => VersionMajor >= 1 ? (uint)32 : (uint)28; // See docs/design/mono/webcil.md;
}
