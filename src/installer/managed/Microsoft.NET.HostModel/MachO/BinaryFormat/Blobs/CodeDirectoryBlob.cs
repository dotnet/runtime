// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// For code signature version 0x20400 only. Code signature headers/blobs are all big endian / network order.
/// </summary>
/// <remarks>
/// Format based off of https://github.com/apple-oss-distributions/Security/blob/3dab46a11f45f2ffdbd70e2127cc5a8ce4a1f222/OSX/libsecurity_codesigning/lib/codedirectory.h#L193
/// </remarks>
internal unsafe class CodeDirectoryBlob : Blob
{
    public override uint Size => (uint)(base.Size + sizeof(CodeDirectoryHeader) + _data.Length);
    private CodeDirectoryHeader _cddata;
    private byte[] _data;

    public CodeDirectoryBlob(MemoryMappedViewAccessor accessor, long offset)
        : base(accessor, offset)
    {
        if (Magic != BlobMagic.CodeDirectory)
        {
            throw new InvalidDataException($"Invalid magic for CodeDirectoryBlob: {Magic}");
        }
        accessor.Read(offset + sizeof(uint), out uint size);
        accessor.Read<CodeDirectoryHeader>(offset + sizeof(uint) + sizeof(uint), out _cddata);
        _data = new byte[size - (sizeof(uint) * 2) - sizeof(CodeDirectoryHeader)];
        accessor.ReadArray(offset + sizeof(uint) * 2 + sizeof(CodeDirectoryHeader), _data, 0, _data.Length);
        if (Size != size)
        {
            throw new InvalidOperationException($"Invalid size for CodeDirectoryBlob: {size}");
        }
    }

    public CodeDirectoryBlob(string identifier, uint codeSlotCount, uint specialCodeSlotCount, uint executableLength, byte hashSize, HashType hashType, ulong signatureStart, ulong execSegmentBase, ulong execSegmentLimit, ExecutableSegmentFlags execSegmentFlags, byte[] hashes) : base(BlobMagic.CodeDirectory)
    {
        _cddata = new CodeDirectoryHeader(identifier, codeSlotCount, specialCodeSlotCount, executableLength, hashSize, hashType, signatureStart, execSegmentBase, execSegmentLimit, execSegmentFlags);
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
            _version = (CodeDirectoryVersion)((uint)CodeDirectoryVersion.HighestVersion).ConvertToBigEndian();
            _flags = (CodeDirectoryFlags)((uint)CodeDirectoryFlags.Adhoc).ConvertToBigEndian();
            _hashesOffset = ((uint)sizeof(CodeDirectoryHeader) + identifierLength + HashSize * specialCodeSlotCount).ConvertToBigEndian();
            _codeSlotCount = codeSlotCount.ConvertToBigEndian();
            _specialSlotCount = specialCodeSlotCount.ConvertToBigEndian();
            _executableLength = executableLength.ConvertToBigEndian();
            HashSize = hashSize;
            HashType = hashType;
            Platform = 0;
            Log2PageSize = 12; // 4K page size
            _codeLimit64 = signatureStart >= uint.MaxValue ? signatureStart : 0;
            _execSegmentBase = execSegmentBase.ConvertToBigEndian();
            _execSegmentLimit = execSegmentLimit.ConvertToBigEndian();
            _execSegmentFlags = (ExecutableSegmentFlags)((uint)execSegmentFlags).ConvertToBigEndian();
        }
    }

    public CodeDirectoryVersion Version
    {
        get => (CodeDirectoryVersion)((uint)_cddata._version).ConvertFromBigEndian();
        set => _cddata._version = (CodeDirectoryVersion)((uint)value).ConvertToBigEndian();
    }
    public CodeDirectoryFlags Flags
    {
        get => (CodeDirectoryFlags)((uint)_cddata._flags).ConvertFromBigEndian();
        set => _cddata._flags = (CodeDirectoryFlags)((uint)value).ConvertToBigEndian();
    }
    public uint HashesOffset
    {
        get => _cddata._hashesOffset.ConvertFromBigEndian();
        set => _cddata._hashesOffset = value.ConvertToBigEndian();
    }
    public uint IdentifierOffset
    {
        get => _cddata._identifierOffset.ConvertFromBigEndian();
        set => _cddata._identifierOffset = value.ConvertToBigEndian();
    }
    public uint SpecialSlotCount
    {
        get => _cddata._specialSlotCount.ConvertFromBigEndian();
        set => _cddata._specialSlotCount = value.ConvertToBigEndian();
    }
    public uint CodeSlotCount
    {
        get => _cddata._codeSlotCount.ConvertFromBigEndian();
        set => _cddata._codeSlotCount = value.ConvertToBigEndian();
    }
    public uint ExecutableLength
    {
        get => _cddata._executableLength.ConvertFromBigEndian();
        set => _cddata._executableLength = value.ConvertToBigEndian();
    }
    public ulong CodeLimit64
    {
        get => _cddata._codeLimit64.ConvertFromBigEndian();
        set => _cddata._codeLimit64 = value.ConvertToBigEndian();
    }
    public ulong ExecSegmentBase
    {
        get => _cddata._execSegmentBase.ConvertFromBigEndian();
        set => _cddata._execSegmentBase = value.ConvertToBigEndian();
    }
    public ulong ExecSegmentLimit
    {
        get => _cddata._execSegmentLimit.ConvertFromBigEndian();
        set => _cddata._execSegmentLimit = value.ConvertToBigEndian();
    }
    public ExecutableSegmentFlags ExecSegmentFlags
    {
        get => (ExecutableSegmentFlags)((ulong)_cddata._execSegmentFlags).ConvertFromBigEndian();
        set => _cddata._execSegmentFlags = (ExecutableSegmentFlags)((ulong)value).ConvertToBigEndian();
    }

    public string Identifier
    {
        get
        {
            fixed (byte* dataPtr = _data)
            {
                return UTF8Encoding.UTF8.GetString(dataPtr, (int)IdentifierOffset - (sizeof(uint) * 2 + sizeof(CodeDirectoryHeader)));
            }
        }
    }

    public override void Write(MemoryMappedViewAccessor accessor, long offset)
    {
        base.Write(accessor, offset);
        accessor.Write<CodeDirectoryHeader>(offset + sizeof(uint) + sizeof(uint), ref _cddata);
    }
    public override void Write(Stream stream)
    {
        throw new NotImplementedException("Not done yet");
    }
}
