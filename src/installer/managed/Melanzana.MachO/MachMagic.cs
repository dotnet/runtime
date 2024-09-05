// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Melanzana.MachO
{
    public enum MachMagic : uint
    {
        MachHeaderLittleEndian = 0xcefaedfe,
        MachHeaderBigEndian = 0xfeedface,
        MachHeader64LittleEndian = 0xcffaedfe,
        MachHeader64BigEndian = 0xfeedfacf,
        FatMagicLittleEndian = 0xbebafeca,
        FatMagicBigEndian = 0xcafebabe,
    }
}
