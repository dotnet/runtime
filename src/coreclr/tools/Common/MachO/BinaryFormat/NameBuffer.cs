// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

#if HOST_MODEL
namespace Microsoft.NET.HostModel.MachO;
#else
namespace ILCompiler.Reflection.ReadyToRun.MachO;
#endif

/// <summary>
/// A 16 byte buffer used to store names in Mach-O load commands.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct NameBuffer
{
    private ulong _nameLower;
    private ulong _nameUpper;

    private const int BufferLength = 16;

    private NameBuffer(ReadOnlySpan<byte> nameBytes)
    {
        byte[] buffer = new byte[BufferLength];
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

    public unsafe string GetString()
    {
        fixed (ulong* ptr = &_nameLower)
        {
            byte* bytePtr = (byte*)ptr;
            int length = 0;
            while (length < BufferLength && bytePtr[length] != 0)
            {
                length++;
            }

            return System.Text.Encoding.UTF8.GetString(bytePtr, length);
        }
    }
}
