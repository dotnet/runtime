// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// See https://github.com/apple-oss-distributions/cctools/blob/7a5450708479bbff61527d5e0c32a3f7b7e4c1d0/tests/include/MachO/MachHeader.pm#L12 for definitions.
/// </summary>
internal enum MachMagic : uint
{
    MachHeaderOppositeEndian = 0xcefaedfe,
    MachHeaderCurrentEndian = 0xfeedface,
    MachHeader64OppositeEndian = 0xcffaedfe,
    MachHeader64CurrentEndian = 0xfeedfacf,
    FatMagicOppositeEndian = 0xbebafeca,
    FatMagicCurrentEndian = 0xcafebabe,
}
