// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// Format based off of https://github.com/apple-oss-distributions/Security/blob/3dab46a11f45f2ffdbd70e2127cc5a8ce4a1f222/OSX/libsecurity_codesigning/lib/cscdefs.h#L18
/// Code signature data is always big endian / network order.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct BlobIndex
{
    private readonly CodeDirectorySpecialSlot _slot;
    private readonly uint _offset;

    internal const int Size = sizeof(CodeDirectorySpecialSlot) + sizeof(uint);

    public CodeDirectorySpecialSlot Slot => (CodeDirectorySpecialSlot)((uint)_slot).ConvertFromBigEndian();

    public uint Offset => _offset.ConvertFromBigEndian();

    public BlobIndex(CodeDirectorySpecialSlot slot, uint offset)
    {
        _slot = (CodeDirectorySpecialSlot)((uint)slot).ConvertToBigEndian();
        _offset = offset.ConvertToBigEndian();
    }
}
