// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.NET.HostModel.MachO;

[StructLayout(LayoutKind.Sequential)]
internal struct BlobIndex
{
    private readonly CodeDirectorySpecialSlot _slot;
    private readonly uint _offset;

    public CodeDirectorySpecialSlot Slot => (CodeDirectorySpecialSlot)((uint)_slot).ConvertFromBigEndian();
    public uint Offset => _offset.ConvertFromBigEndian();

    public BlobIndex(CodeDirectorySpecialSlot slot, uint offset)
    {
        _slot = (CodeDirectorySpecialSlot)((uint)slot).MakeBigEndian();
        _offset = offset.MakeBigEndian();
    }
}
