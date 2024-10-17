// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.NET.HostModel.MachO;

[StructLayout(LayoutKind.Sequential)]
internal struct NameBuffer
{
    private ulong _nameLower;
    private ulong _nameUpper;

    public static NameBuffer __TEXT = BitConverter.IsLittleEndian ?
        new NameBuffer { _nameLower = 0x0000545845545F5F, _nameUpper = 0x0000000000000000 }
        : new NameBuffer { _nameLower = 0x5F5F544558540000, _nameUpper = 0x0000000000000000 };

    public static NameBuffer __LINKEDIT = BitConverter.IsLittleEndian ?
        new NameBuffer { _nameLower = 0x44454B4E494C5F5F, _nameUpper = 0x0000000000005449 }
        : new NameBuffer { _nameLower = 0x5F5F4C494E4B4544, _nameUpper = 0x4954000000000000 };
}
