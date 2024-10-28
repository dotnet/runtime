// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// For code signature version 0x20400 only. Code signature headers/blobs are all big endian / network order.
/// </summary>
/// <remarks>
/// Format based off of https://github.com/apple-oss-distributions/Security/blob/3dab46a11f45f2ffdbd70e2127cc5a8ce4a1f222/OSX/libsecurity_codesigning/lib/codedirectory.h#L193
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
internal struct CodeDirectoryHeader
{
    private readonly BlobMagic _magic = (BlobMagic)((uint)BlobMagic.CodeDirectory).ConvertToBigEndian();
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
    private readonly uint _reserved = 0;
    private readonly uint _scatterOffset = 0;
    private readonly uint _teamIdOffset = 0;
    private readonly uint _reserved2 = 0;
    private ulong _codeLimit64;
    private ulong _execSegmentBase;
    private ulong _execSegmentLimit;
    private ExecutableSegmentFlags _execSegmentFlags;

    public CodeDirectoryHeader()
    {
    }

    public uint Size
    {
        get => _size.ConvertFromBigEndian();
        set => _size = value.ConvertToBigEndian();
    }
    public CodeDirectoryVersion Version
    {
        get => (CodeDirectoryVersion)((uint)_version).ConvertFromBigEndian();
        set => _version = (CodeDirectoryVersion)((uint)value).ConvertToBigEndian();
    }
    public CodeDirectoryFlags Flags
    {
        get => (CodeDirectoryFlags)((uint)_flags).ConvertFromBigEndian();
        set => _flags = (CodeDirectoryFlags)((uint)value).ConvertToBigEndian();
    }
    public uint HashesOffset
    {
        get => _hashesOffset.ConvertFromBigEndian();
        set => _hashesOffset = value.ConvertToBigEndian();
    }
    public uint IdentifierOffset
    {
        get => _identifierOffset.ConvertFromBigEndian();
        set => _identifierOffset = value.ConvertToBigEndian();
    }
    public uint SpecialSlotCount
    {
        get => _specialSlotCount.ConvertFromBigEndian();
        set => _specialSlotCount = value.ConvertToBigEndian();
    }
    public uint CodeSlotCount
    {
        get => _codeSlotCount.ConvertFromBigEndian();
        set => _codeSlotCount = value.ConvertToBigEndian();
    }
    public uint ExecutableLength
    {
        get => _executableLength.ConvertFromBigEndian();
        set => _executableLength = value.ConvertToBigEndian();
    }
    public ulong CodeLimit64
    {
        get => _codeLimit64.ConvertFromBigEndian();
        set => _codeLimit64 = value.ConvertToBigEndian();
    }
    public ulong ExecSegmentBase
    {
        get => _execSegmentBase.ConvertFromBigEndian();
        set => _execSegmentBase = value.ConvertToBigEndian();
    }
    public ulong ExecSegmentLimit
    {
        get => _execSegmentLimit.ConvertFromBigEndian();
        set => _execSegmentLimit = value.ConvertToBigEndian();
    }
    public ExecutableSegmentFlags ExecSegmentFlags
    {
        get => (ExecutableSegmentFlags)((ulong)_execSegmentFlags).ConvertFromBigEndian();
        set => _execSegmentFlags = (ExecutableSegmentFlags)((ulong)value).ConvertToBigEndian();
    }
}
