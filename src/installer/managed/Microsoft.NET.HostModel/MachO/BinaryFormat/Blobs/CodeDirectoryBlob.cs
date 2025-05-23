// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// A code signature blob for version 0x20400 only.
/// </summary>
/// <remarks>
/// Format based off of https://github.com/apple-oss-distributions/Security/blob/3dab46a11f45f2ffdbd70e2127cc5a8ce4a1f222/OSX/libsecurity_codesigning/lib/codedirectory.h#L193
/// </remarks>
internal sealed unsafe class CodeDirectoryBlob : Blob
{
    public override uint Size => (uint)(base.Size + sizeof(CodeDirectoryHeader) + _data.Length);
    private CodeDirectoryHeader _cdHeader;
    private byte[] _data;

    public CodeDirectoryBlob(MemoryMappedViewAccessor accessor, long offset)
        : base(accessor, offset)
    {
        if (Magic != BlobMagic.CodeDirectory)
        {
            throw new InvalidDataException($"Invalid magic for CodeDirectoryBlob: {Magic}");
        }
        accessor.Read(offset + sizeof(uint), out uint size);
        size = size.ConvertFromBigEndian();
        accessor.Read(offset + base.Size, out _cdHeader);
        var dataSize = size - (base.Size + sizeof(CodeDirectoryHeader));
        _data = new byte[dataSize];
        accessor.ReadArray(offset + base.Size + sizeof(CodeDirectoryHeader), _data, 0, _data.Length);
    }

    public CodeDirectoryBlob(string identifier, uint codeSlotCount, uint specialCodeSlotCount, uint executableLength, byte hashSize, HashType hashType, ulong signatureStart, ulong execSegmentBase, ulong execSegmentLimit, ExecutableSegmentFlags execSegmentFlags, byte[] hashes) : base(BlobMagic.CodeDirectory)
    {
        _cdHeader = new CodeDirectoryHeader(identifier, codeSlotCount, specialCodeSlotCount, executableLength, hashSize, hashType, signatureStart, execSegmentBase, execSegmentLimit, execSegmentFlags);
        Debug.Assert(hashes.Length == hashSize * (specialCodeSlotCount + codeSlotCount));
        _data = new byte[hashes.Length + Encoding.UTF8.GetByteCount(identifier) + 1];
        int count = Encoding.UTF8.GetBytes(identifier, 0, identifier.Length, _data, 0);
        hashes.CopyTo(_data, count + 1);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CodeDirectoryHeader
    {
        public CodeDirectoryVersion _version;
        public CodeDirectoryFlags _flags;
        public uint _hashesOffset;
        public uint _identifierOffset;
        public uint _specialSlotCount;
        public uint _codeSlotCount;
        public uint _executableLength;
        public byte HashSize;
        public HashType HashType;
        public byte Platform;
        public byte Log2PageSize;
#pragma warning disable CA1805 // Do not initialize unnecessarily
        public readonly uint _reserved = 0;
        public readonly uint _scatterOffset = 0;
        public readonly uint _teamIdOffset = 0;
        public readonly uint _reserved2 = 0;
#pragma warning restore CA1805 // Do not initialize unnecessarily
        public ulong _codeLimit64;
        public ulong _execSegmentBase;
        public ulong _execSegmentLimit;
        public ExecutableSegmentFlags _execSegmentFlags;

        public CodeDirectoryHeader(string identifier, uint codeSlotCount, uint specialCodeSlotCount, uint executableLength, byte hashSize, HashType hashType, ulong signatureStart, ulong execSegmentBase, ulong execSegmentLimit, ExecutableSegmentFlags execSegmentFlags)
        {
            uint identifierLength = (uint)(Encoding.UTF8.GetByteCount(identifier) + 1);
            HashSize = hashSize;
            _version = (CodeDirectoryVersion)((uint)CodeDirectoryVersion.HighestVersion).ConvertToBigEndian();
            _flags = (CodeDirectoryFlags)((uint)CodeDirectoryFlags.Adhoc).ConvertToBigEndian();
            _identifierOffset = ((uint)sizeof(CodeDirectoryHeader) + sizeof(uint) * 2).ConvertToBigEndian();
            _hashesOffset = (_identifierOffset.ConvertFromBigEndian() + identifierLength + HashSize * specialCodeSlotCount).ConvertToBigEndian();
            _codeSlotCount = codeSlotCount.ConvertToBigEndian();
            _specialSlotCount = specialCodeSlotCount.ConvertToBigEndian();
            _executableLength = executableLength.ConvertToBigEndian();
            HashType = hashType;
            Platform = 0;
            Log2PageSize = 12; // 4K page size
            _codeLimit64 = (signatureStart >= uint.MaxValue ? signatureStart : 0).ConvertToBigEndian();
            _execSegmentBase = execSegmentBase.ConvertToBigEndian();
            _execSegmentLimit = execSegmentLimit.ConvertToBigEndian();
            _execSegmentFlags = (ExecutableSegmentFlags)((uint)execSegmentFlags).ConvertToBigEndian();
        }
    }

    public CodeDirectoryVersion Version
    {
        get => (CodeDirectoryVersion)((uint)_cdHeader._version).ConvertFromBigEndian();
        set => _cdHeader._version = (CodeDirectoryVersion)((uint)value).ConvertToBigEndian();
    }
    public CodeDirectoryFlags Flags
    {
        get => (CodeDirectoryFlags)((uint)_cdHeader._flags).ConvertFromBigEndian();
        set => _cdHeader._flags = (CodeDirectoryFlags)((uint)value).ConvertToBigEndian();
    }
    public uint HashesOffset
    {
        get => _cdHeader._hashesOffset.ConvertFromBigEndian();
        set => _cdHeader._hashesOffset = value.ConvertToBigEndian();
    }
    public uint IdentifierOffset
    {
        get => _cdHeader._identifierOffset.ConvertFromBigEndian();
        set => _cdHeader._identifierOffset = value.ConvertToBigEndian();
    }
    public uint SpecialSlotCount
    {
        get => _cdHeader._specialSlotCount.ConvertFromBigEndian();
        set => _cdHeader._specialSlotCount = value.ConvertToBigEndian();
    }
    public uint CodeSlotCount
    {
        get => _cdHeader._codeSlotCount.ConvertFromBigEndian();
        set => _cdHeader._codeSlotCount = value.ConvertToBigEndian();
    }
    public uint ExecutableLength
    {
        get => _cdHeader._executableLength.ConvertFromBigEndian();
        set => _cdHeader._executableLength = value.ConvertToBigEndian();
    }
    public ulong CodeLimit64
    {
        get => _cdHeader._codeLimit64.ConvertFromBigEndian();
        set => _cdHeader._codeLimit64 = value.ConvertToBigEndian();
    }
    public ulong ExecSegmentBase
    {
        get => _cdHeader._execSegmentBase.ConvertFromBigEndian();
        set => _cdHeader._execSegmentBase = value.ConvertToBigEndian();
    }
    public ulong ExecSegmentLimit
    {
        get => _cdHeader._execSegmentLimit.ConvertFromBigEndian();
        set => _cdHeader._execSegmentLimit = value.ConvertToBigEndian();
    }
    public ExecutableSegmentFlags ExecSegmentFlags
    {
        get => (ExecutableSegmentFlags)((ulong)_cdHeader._execSegmentFlags).ConvertFromBigEndian();
        set => _cdHeader._execSegmentFlags = (ExecutableSegmentFlags)((ulong)value).ConvertToBigEndian();
    }

    public override void Write(MemoryMappedViewAccessor accessor, long offset)
    {
        base.Write(accessor, offset);
        accessor.Write<CodeDirectoryHeader>(offset + sizeof(uint) + sizeof(uint), ref _cdHeader);
        accessor.WriteArray(offset + base.Size + sizeof(CodeDirectoryHeader), _data, 0, _data.Length);
    }

    public override void Write(Span<byte> buffer)
    {
        throw new NotImplementedException("Not implemented yet.");
    }
}
