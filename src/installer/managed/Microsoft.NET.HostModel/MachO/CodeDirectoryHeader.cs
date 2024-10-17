// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// Version 0x20400
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CodeDirectoryHeader
{
    private BlobMagic _magic;
    private uint _size;
    private CodeDirectoryVersion _version;
    private CodeDirectoryFlags _flags;
    private uint _hashesOffset;
    private uint _identifierOffset;
    private uint _specialSlotCount;
    private uint _codeSlotCount;
    private uint _executableLength;
    public byte HashSize;
    public HashType HashType;
    public byte Platform;
    public byte Log2PageSize;
    private uint _reserved;
    private uint _scatterOffset;
    private uint _teamIdOffset;
    private uint _reserved2;
    private ulong _codeLimit64;
    private ulong _execSegmentBase;
    private ulong _execSegmentLimit;
    private ExecutableSegmentFlags _execSegmentFlags;

    public BlobMagic Magic
    {
        get => (BlobMagic)((uint)_magic).ConvertFromBigEndian();
        set => _magic = (BlobMagic)((uint)value).MakeBigEndian();
    }
    public uint Size
    {
        get => _size.ConvertFromBigEndian();
        set => _size = value.MakeBigEndian();
    }
    public CodeDirectoryVersion Version
    {
        get => (CodeDirectoryVersion)((uint)_version).ConvertFromBigEndian();
        set => _version = (CodeDirectoryVersion)((uint)value).MakeBigEndian();
    }
    public CodeDirectoryFlags Flags
    {
        get => (CodeDirectoryFlags)((uint)_flags).ConvertFromBigEndian();
        set => _flags = (CodeDirectoryFlags)((uint)value).MakeBigEndian();
    }
    public uint HashesOffset
    {
        get => _hashesOffset.ConvertFromBigEndian();
        set => _hashesOffset = value.MakeBigEndian();
    }
    public uint IdentifierOffset
    {
        get => _identifierOffset.ConvertFromBigEndian();
        set => _identifierOffset = value.MakeBigEndian();
    }
    public uint SpecialSlotCount
    {
        get => _specialSlotCount.ConvertFromBigEndian();
        set => _specialSlotCount = value.MakeBigEndian();
    }
    public uint CodeSlotCount
    {
        get => _codeSlotCount.ConvertFromBigEndian();
        set => _codeSlotCount = value.MakeBigEndian();
    }
    public uint ExecutableLength
    {
        get => _executableLength.ConvertFromBigEndian();
        set => _executableLength = value.MakeBigEndian();
    }
    public uint Reserved
    {
        get => _reserved.ConvertFromBigEndian();
        set => _reserved = value.MakeBigEndian();
    }
    public uint ScatterOffset
    {
        get => _scatterOffset.ConvertFromBigEndian();
        set => _scatterOffset = value.MakeBigEndian();
    }
    public uint TeamIdOffset
    {
        get => _teamIdOffset.ConvertFromBigEndian();
        set => _teamIdOffset = value.MakeBigEndian();
    }
    public uint Reserved2
    {
        get => _reserved2.ConvertFromBigEndian();
        set => _reserved2 = value.MakeBigEndian();
    }
    public ulong CodeLimit64
    {
        get => _codeLimit64.ConvertFromBigEndian();
        set => _codeLimit64 = value.MakeBigEndian();
    }
    public ulong ExecSegmentBase
    {
        get => _execSegmentBase.ConvertFromBigEndian();
        set => _execSegmentBase = value.MakeBigEndian();
    }
    public ulong ExecSegmentLimit
    {
        get => _execSegmentLimit.ConvertFromBigEndian();
        set => _execSegmentLimit = value.MakeBigEndian();
    }
    public ExecutableSegmentFlags ExecSegmentFlags
    {
        get => (ExecutableSegmentFlags)((ulong)_execSegmentFlags).ConvertFromBigEndian();
        set => _execSegmentFlags = (ExecutableSegmentFlags)((ulong)value).MakeBigEndian();
    }
}
