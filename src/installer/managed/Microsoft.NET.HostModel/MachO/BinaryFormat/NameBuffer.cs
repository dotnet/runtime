// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// A 16 byte buffer used to store names in Mach-O load commands.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct NameBuffer
{
    private ulong _nameLower;
    private ulong _nameUpper;

    private NameBuffer(ReadOnlySpan<byte> nameBytes)
    {
        byte[] buffer = new byte[16];
        nameBytes.CopyTo(buffer);

        if (BitConverter.IsLittleEndian)
        {
            _nameLower = BitConverter.ToUInt64(buffer, 0);
            _nameUpper = BitConverter.ToUInt64(buffer, 8);
        }
        else
        {
            _nameLower = BitConverter.ToUInt64(buffer, 8);
            _nameUpper = BitConverter.ToUInt64(buffer, 0);
        }
    }

    public static NameBuffer __TEXT = new NameBuffer("__TEXT"u8);
    public static NameBuffer __LINKEDIT = new NameBuffer("__LINKEDIT"u8);
}
