// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.NET.HostModel.MachO;

#nullable enable

/// <summary>
/// A code signature blob for version 0x20400 only.
/// </summary>
/// <remarks>
/// Format based off of https://github.com/apple-oss-distributions/Security/blob/3dab46a11f45f2ffdbd70e2127cc5a8ce4a1f222/OSX/libsecurity_codesigning/lib/codedirectory.h#L193
/// </remarks>
internal sealed unsafe class CodeDirectoryBlob : Blob
{
    public override uint Size => (uint)(base.Size
        + sizeof(CodeDirectoryHeader)
        + Encoding.UTF8.GetByteCount(_identifier) + 1 // +1 for null terminator
        + SpecialSlotCount * HashSize
        + CodeSlotCount * HashSize);
    private CodeDirectoryHeader _cdHeader;
    private string _identifier;
    private byte[][] _specialSlotHashes;
    private byte[][] _codeHashes;

    public static HashType DefaultHashType => HashType.SHA256;

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
        var _data = new byte[dataSize];
        accessor.ReadArray(offset + base.Size + sizeof(CodeDirectoryHeader), _data, 0, _data.Length);

        int identifierDataOffset = GetDataOffset(IdentifierOffset);
        int nullTerminatorIndex = _data.AsSpan().Slice(identifierDataOffset).IndexOf((byte)0x00);
        _identifier = Encoding.UTF8.GetString(_data, identifierDataOffset, nullTerminatorIndex);
        var hashesDataOffset = GetDataOffset(HashesOffset);
        _specialSlotHashes = new byte[SpecialSlotCount][];
        _codeHashes = new byte[CodeSlotCount][];
        // Ensure the highest special slot is still within _data bounds
        if (hashesDataOffset - SpecialSlotCount * HashSize < 0)
        {
            throw new InvalidDataException("Invalid CodeDirectoryBlob: SpecialSlotCount is too high for the provided data.");
        }
        // Special slot hashes are stored negatively indexed from HashesOffset
        int specialSlotHashesOffset = (int)(hashesDataOffset - SpecialSlotCount * HashSize);
        for (int i = 0; i < SpecialSlotCount; i++)
        {
            _specialSlotHashes[i] = _data.AsSpan(specialSlotHashesOffset + i * HashSize, HashSize).ToArray();
        }
        _specialSlotHashes.Reverse();

        // Code slot hashes are stored positively indexed from HashesOffset
        for (int codeSlotNumber = 0; codeSlotNumber < CodeSlotCount; codeSlotNumber++)
        {
            _codeHashes[codeSlotNumber] = _data.AsSpan(hashesDataOffset + codeSlotNumber * HashSize, HashSize).ToArray();
        }
    }

    private int GetDataOffset(uint blobOffset) => (int)(blobOffset - base.Size - sizeof(CodeDirectoryHeader));

    public CodeDirectoryBlob(
        string identifier,
        ulong signatureStart,
        HashType hashType,
        ExecutableSegmentFlags execSegmentFlags,
        byte[][] specialSlotHashes,
        byte[][] codeHashes) : base(BlobMagic.CodeDirectory)
    {
        // Always assume the executable length is the entire file size
        _cdHeader = new CodeDirectoryHeader(identifier, (uint)codeHashes.Length, (uint)specialSlotHashes.Length, (uint)signatureStart, hashType.GetHashSize(), hashType, signatureStart, 0, signatureStart, execSegmentFlags);
        _identifier = identifier;
        _specialSlotHashes = specialSlotHashes;
        _codeHashes = codeHashes;
    }

    public static CodeDirectoryBlob Create(
        MemoryMappedViewAccessor accessor,
        long signatureStart,
        string identifier,
        RequirementsBlob requirementsBlob,
        EntitlementsBlob? entitlementsBlob = null,
        DerEntitlementsBlob? derEntitlementsBlob = null,
        HashType hashType = HashType.SHA256,
        uint pageSize = MachObjectFile.DefaultPageSize)
    {
        uint codeSlotCount = GetCodeSlotCount((uint)signatureStart, pageSize);
        uint specialCodeSlotCount = (uint)(
            derEntitlementsBlob is not null ?
                CodeDirectorySpecialSlot.DerEntitlements :
            entitlementsBlob is not null ?
                CodeDirectorySpecialSlot.Entitlements :
                CodeDirectorySpecialSlot.Requirements);

        var specialSlotHashes = new byte[specialCodeSlotCount][];
        var codeHashes = new byte[codeSlotCount][];
        var hasher = hashType.CreateHashAlgorithm();
        Debug.Assert(hasher.HashSize / 8 == hashType.GetHashSize());

        var emptyHash = new byte[hashType.GetHashSize()];
        for (int i = 0; i < specialSlotHashes.Length; i++)
        {
            specialSlotHashes[i] = emptyHash;
        }
        // Fill in the CodeDirectory hashes

        // Special slot hashes
        // -7 is the der entitlements blob hash
        if (derEntitlementsBlob != null)
        {
            specialSlotHashes[(int)CodeDirectorySpecialSlot.DerEntitlements - 1] = hasher.ComputeHash(derEntitlementsBlob.GetBytes());
        }

        // -5 is the entitlements blob hash
        if (entitlementsBlob != null)
        {
            specialSlotHashes[(int)CodeDirectorySpecialSlot.Entitlements - 1] = hasher.ComputeHash(entitlementsBlob.GetBytes());
        }

        // -2 is the requirements blob hash
        specialSlotHashes[(int)CodeDirectorySpecialSlot.Requirements - 1] = hasher.ComputeHash(requirementsBlob.GetBytes());
        // -1 is the CMS blob hash (which is empty -- nothing to hash)

        // Reverse special slot hashes
        Array.Reverse(specialSlotHashes);
        // for (int i = 0; i < specialSlotHashes.Length / 2; i++)
        // {
        //     var temp = specialSlotHashes[i];
        //     specialSlotHashes[i] = specialSlotHashes[specialSlotHashes.Length - 1 - i];
        //     specialSlotHashes[specialSlotHashes.Length - 1 - i] = temp;
        // }
        // 0 - N are Code hashes
        long remaining = signatureStart;
        long buffptr = 0;
        int cdIndex = 0;
        byte[] pageBuffer = new byte[pageSize];
        while (remaining > 0)
        {
            int currentPageSize = (int)Math.Min(remaining, pageSize);
            int bytesRead = accessor.ReadArray(buffptr, pageBuffer, 0, currentPageSize);
            if (bytesRead != currentPageSize)
                throw new IOException("Could not read all bytes");
            buffptr += bytesRead;
            codeHashes[cdIndex++] = hasher.ComputeHash(pageBuffer, 0, currentPageSize);
            remaining -= currentPageSize;
        }

        return new CodeDirectoryBlob(
            identifier,
            (ulong)signatureStart,
            hashType,
            ExecutableSegmentFlags.MainBinary,
            specialSlotHashes,
            codeHashes);
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
            _execSegmentFlags = (ExecutableSegmentFlags)((ulong)execSegmentFlags).ConvertToBigEndian();
        }
    }

    public CodeDirectoryVersion Version
    {
        get => (CodeDirectoryVersion)((uint)_cdHeader._version).ConvertFromBigEndian();
    }
    public CodeDirectoryFlags Flags
    {
        get => (CodeDirectoryFlags)((uint)_cdHeader._flags).ConvertFromBigEndian();
    }
    public uint HashesOffset
    {
        get => _cdHeader._hashesOffset.ConvertFromBigEndian();
    }
    public uint IdentifierOffset
    {
        get => _cdHeader._identifierOffset.ConvertFromBigEndian();
    }
    public uint SpecialSlotCount
    {
        get => _cdHeader._specialSlotCount.ConvertFromBigEndian();
    }
    public uint CodeSlotCount
    {
        get => _cdHeader._codeSlotCount.ConvertFromBigEndian();
    }
    public uint ExecutableLength
    {
        get => _cdHeader._executableLength.ConvertFromBigEndian();
    }
    public ulong CodeLimit64
    {
        get => _cdHeader._codeLimit64.ConvertFromBigEndian();
    }
    public ulong ExecSegmentBase
    {
        get => _cdHeader._execSegmentBase.ConvertFromBigEndian();
    }
    public ulong ExecSegmentLimit
    {
        get => _cdHeader._execSegmentLimit.ConvertFromBigEndian();
    }
    public ExecutableSegmentFlags ExecSegmentFlags
    {
        get => (ExecutableSegmentFlags)((ulong)_cdHeader._execSegmentFlags).ConvertFromBigEndian();
    }
    public byte HashSize
    {
        get => _cdHeader.HashSize;
    }

    public override void Write(MemoryMappedViewAccessor accessor, long offset)
    {
        base.Write(accessor, offset);
        accessor.Write<CodeDirectoryHeader>(offset + sizeof(uint) + sizeof(uint), ref _cdHeader);
        var identifierLength = Encoding.UTF8.GetByteCount(_identifier);
        accessor.WriteArray(offset + IdentifierOffset, Encoding.UTF8.GetBytes(_identifier), 0, identifierLength);
        accessor.Write(offset + identifierLength + IdentifierOffset, (byte)0x00); // Null-terminate the identifier
        Debug.Assert(identifierLength + 1 + IdentifierOffset == HashesOffset - SpecialSlotCount * HashSize);
        int specialSlotHashesOffset = (int)(HashesOffset - SpecialSlotCount * HashSize);
        for (int i = 0; i < SpecialSlotCount; i++)
        {
            accessor.WriteArray(offset + specialSlotHashesOffset + i * HashSize, _specialSlotHashes[i], 0, HashSize);
        }

        int codeHashesOffset = (int)HashesOffset;
        for (int i = 0; i < CodeSlotCount; i++)
        {
            accessor.WriteArray(offset + codeHashesOffset + i * HashSize, _codeHashes[i], 0, HashSize);
        }
    }

    public override void Write(Span<byte> buffer)
    {
        throw new NotImplementedException("Blob.Write(Span<byte>) is used by CodeDirectoryBlob for hashing. This menthod should not need to be called on a CodeDirectoryBlob.");
    }

    public override bool Equals(object? obj)
    {
        if (obj is not CodeDirectoryBlob other)
            return false;

        bool cdHeaderIsEqual = _cdHeader._version == other._cdHeader._version &&
            _cdHeader._flags == other._cdHeader._flags &&
            _cdHeader._hashesOffset == other._cdHeader._hashesOffset &&
            _cdHeader._identifierOffset == other._cdHeader._identifierOffset &&
            _cdHeader._specialSlotCount == other._cdHeader._specialSlotCount &&
            _cdHeader._codeSlotCount == other._cdHeader._codeSlotCount &&
            _cdHeader._executableLength == other._cdHeader._executableLength &&
            _cdHeader.HashSize == other._cdHeader.HashSize &&
            _cdHeader.HashType == other._cdHeader.HashType &&
            _cdHeader.Platform == other._cdHeader.Platform &&
            _cdHeader.Log2PageSize == other._cdHeader.Log2PageSize &&
            _cdHeader._reserved == other._cdHeader._reserved &&
            _cdHeader._scatterOffset == other._cdHeader._scatterOffset &&
            _cdHeader._teamIdOffset == other._cdHeader._teamIdOffset &&
            _cdHeader._reserved2 == other._cdHeader._reserved2 &&
            _cdHeader._codeLimit64 == other._cdHeader._codeLimit64 &&
            _cdHeader._execSegmentBase == other._cdHeader._execSegmentBase &&
            _cdHeader._execSegmentFlags == other._cdHeader._execSegmentFlags;
        if (!cdHeaderIsEqual)
        {
            return false;
        }
        for (int i = 0; i < _specialSlotHashes.Length; i++)
        {
            if (!_specialSlotHashes[i].SequenceEqual(other._specialSlotHashes[i]))
            {
                return false;
            }
        }
        // The first 2 code slots may have differences due to the load commands and padding added.
        for (int i = 2; i < _codeHashes.Length; i++)
        {
            if (!_codeHashes[i].SequenceEqual(other._codeHashes[i]))
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }

    public override string ToString()
    {
#pragma warning disable CA1872 // Prefer 'System.Convert.ToHexStringLower(byte[])' over call chains based on 'System.BitConverter.ToString(byte[])'
        return $"""
        Identifier: {_identifier}
        CodeDirectory v={(int)Version:X} size={Size} flags=0x{(int)Flags,0:x}({Flags.ToString().ToLowerInvariant()}) hashes={SpecialSlotCount}+{CodeSlotCount}
        Executable Segment base={ExecSegmentBase}
        Executable Segment limit={ExecSegmentLimit}
        Executable Segment flags=0x{ExecSegmentFlags:x}
        Page size={1 << _cdHeader.Log2PageSize} bytes
            {string.Join($"{Environment.NewLine}    ", _specialSlotHashes.Select((hash, index)
                => $"-{SpecialSlotCount - index}: {BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant()}"))}
            {string.Join($"{Environment.NewLine}    ", _codeHashes.Select((hash, index)
                => $"{index}: {BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant()}"))}
        """;
#pragma warning restore CA1872 // Prefer 'System.Convert.ToHexStringLower(byte[])' over call chains based on 'System.BitConverter.ToString(byte[])'
    }

    internal static uint GetIdentifierLength(string identifier)
    {
        return (uint)(Encoding.UTF8.GetByteCount(identifier) + 1);
    }

    internal static uint GetCodeSlotCount(uint signatureStart, uint pageSize = MachObjectFile.DefaultPageSize)
    {
        return (signatureStart + pageSize - 1) / pageSize;
    }
}
