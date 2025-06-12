// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// A code signature blob for version 0x20400 only.
/// </summary>
/// <remarks>
/// Format based off of https://github.com/apple-oss-distributions/Security/blob/3dab46a11f45f2ffdbd70e2127cc5a8ce4a1f222/OSX/libsecurity_codesigning/lib/codedirectory.h#L193
/// </remarks>
internal sealed class CodeDirectoryBlob : IBlob
{
    private CodeDirectoryHeader _cdHeader;
    private string _identifier;
    private byte[][] _specialSlotHashes;
    private byte[][] _codeHashes;

    public CodeDirectoryBlob(SimpleBlob blob)
    {
        var data = blob.Data;
        var cdHeader = MemoryMarshal.Read<CodeDirectoryHeader>(data);

        int identifierDataOffset = GetDataOffset(cdHeader._identifierOffset.ConvertFromBigEndian());
        int nullTerminatorIndex = data.AsSpan().Slice(identifierDataOffset).IndexOf((byte)0x00);
        string identifier = Encoding.UTF8.GetString(data, identifierDataOffset, nullTerminatorIndex);

        var specialSlotCount = cdHeader._specialSlotCount.ConvertFromBigEndian();
        var codeSlotCount = cdHeader._codeSlotCount.ConvertFromBigEndian();
        var hashSize = cdHeader.HashSize;
        var hashesDataOffset = GetDataOffset(cdHeader._hashesOffset.ConvertFromBigEndian());

        var specialSlotHashes = new byte[specialSlotCount][];
        var codeHashes = new byte[codeSlotCount][];

        // Special slot hashes are stored negatively indexed from HashesOffset
        int specialSlotHashesOffset = (int)(hashesDataOffset - specialSlotCount * hashSize);
        for (int i = 0; i < specialSlotCount; i++)
        {
            byte[] bytes = data.AsSpan(specialSlotHashesOffset + i * hashSize, hashSize).ToArray();
            specialSlotHashes[i] = bytes;
        }

        // Code slot hashes are stored positively indexed from HashesOffset
        for (int codeSlotNumber = 0; codeSlotNumber < codeSlotCount; codeSlotNumber++)
        {
            codeHashes[codeSlotNumber] = data.AsSpan(hashesDataOffset + codeSlotNumber * hashSize, hashSize).ToArray();
        }

        (_cdHeader, _identifier, _specialSlotHashes, _codeHashes) = (cdHeader, identifier, specialSlotHashes, codeHashes);

        // Convert the offset in the header to the offset into the data array of the SimpleBlob.
        static int GetDataOffset(uint original) => (int)(original - sizeof(uint) - sizeof(uint));
    }

    private CodeDirectoryBlob(
        string identifier,
        ulong signatureStart,
        HashType hashType,
        ExecutableSegmentFlags execSegmentFlags,
        byte[][] specialSlotHashes,
        byte[][] codeHashes)
    {
        // Always assume the executable length is the entire file size / signature start.
        _cdHeader = new CodeDirectoryHeader(
            identifier,
            (uint)codeHashes.Length,
            (uint)specialSlotHashes.Length,
            (uint)signatureStart,
            hashType.GetHashSize(),
            hashType,
            signatureStart,
            0,
            signatureStart,
            execSegmentFlags);
        _identifier = identifier;
        _specialSlotHashes = specialSlotHashes;
        _codeHashes = codeHashes;
    }

    public static HashType DefaultHashType => HashType.SHA256;

    public BlobMagic Magic => BlobMagic.CodeDirectory;

    public uint Size => sizeof(uint) + sizeof(uint) // magic + size
        + CodeDirectoryHeader.Size
        + GetIdentifierLength(_identifier)
        + SpecialSlotCount * HashSize
        + CodeSlotCount * HashSize;

    public static CodeDirectoryBlob Create(
        IMachOFileReader accessor,
        long signatureStart,
        string identifier,
        RequirementsBlob requirementsBlob,
        HashType hashType = HashType.SHA256,
        uint pageSize = MachObjectFile.DefaultPageSize)
    {
        uint codeSlotCount = GetCodeSlotCount((uint)signatureStart, pageSize);
        uint specialCodeSlotCount = (uint)CodeDirectorySpecialSlot.Requirements;

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
        // -2 is the requirements blob hash
        using (var reqStream = new MemoryStreamWriter((int)requirementsBlob.Size))
        {
            requirementsBlob.Write(reqStream, 0);
            specialSlotHashes[(int)CodeDirectorySpecialSlot.Requirements - 1] = hasher.ComputeHash(reqStream.GetBuffer());
        }
        // -1 is the CMS blob hash (which is empty -- nothing to hash)

        // Reverse special slot hashes
        Array.Reverse(specialSlotHashes);

        // 0 - N are Code hashes
        long remaining = signatureStart;
        long buffptr = 0;
        int cdIndex = 0;
        byte[] pageBuffer = new byte[pageSize];
        while (remaining > 0)
        {
            int currentPageSize = (int)Math.Min(remaining, pageSize);
            int bytesRead = accessor.Read(buffptr, pageBuffer, 0, currentPageSize);
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

        public static readonly uint Size = GetSize();
        private static unsafe uint GetSize() => (uint)sizeof(CodeDirectoryHeader);

        public CodeDirectoryHeader(string identifier, uint codeSlotCount, uint specialCodeSlotCount, uint executableLength, byte hashSize, HashType hashType, ulong signatureStart, ulong execSegmentBase, ulong execSegmentLimit, ExecutableSegmentFlags execSegmentFlags)
        {
            HashSize = hashSize;
            _version = (CodeDirectoryVersion)((uint)CodeDirectoryVersion.HighestVersion).ConvertToBigEndian();
            _flags = (CodeDirectoryFlags)((uint)CodeDirectoryFlags.Adhoc).ConvertToBigEndian();
            _identifierOffset = (CodeDirectoryHeader.Size + sizeof(uint) * 2).ConvertToBigEndian();
            _hashesOffset = (_identifierOffset.ConvertFromBigEndian() + GetIdentifierLength(identifier) + HashSize * specialCodeSlotCount).ConvertToBigEndian();
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

    public uint HashesOffset => _cdHeader._hashesOffset.ConvertFromBigEndian();
    public uint SpecialSlotCount => _cdHeader._specialSlotCount.ConvertFromBigEndian();
    public uint CodeSlotCount => _cdHeader._codeSlotCount.ConvertFromBigEndian();
    public byte HashSize => _cdHeader.HashSize;

    public override bool Equals(object? obj)
    {
        if (obj is not CodeDirectoryBlob other)
            return false;

        if (_identifier != other._identifier)
            return false;

        CodeDirectoryHeader thisHeader = _cdHeader;
        CodeDirectoryHeader otherHeader = other._cdHeader;
        // Ignore the exec segment limit for equality checks, as it may differ
        thisHeader._execSegmentLimit = 0;
        otherHeader._execSegmentLimit = 0;
        if (!thisHeader.Equals(otherHeader))
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

    internal static uint GetIdentifierLength(string identifier)
    {
        return (uint)(Encoding.UTF8.GetByteCount(identifier) + 1);
    }

    internal static uint GetCodeSlotCount(uint signatureStart, uint pageSize = MachObjectFile.DefaultPageSize)
    {
        return (signatureStart + pageSize - 1) / pageSize;
    }

    public int Write(IMachOFileWriter accessor, long offset)
    {
        accessor.WriteUInt32BigEndian(offset, (uint)Magic);
        accessor.WriteUInt32BigEndian(offset + sizeof(uint), Size);
        accessor.Write(offset + sizeof(uint) * 2, ref _cdHeader);
        var identifierBytes = Encoding.UTF8.GetBytes(_identifier);
        Debug.Assert(sizeof(uint) * 2 + CodeDirectoryHeader.Size == _cdHeader._identifierOffset.ConvertFromBigEndian());
        accessor.WriteExactly(offset + sizeof(uint) * 2 + CodeDirectoryHeader.Size, identifierBytes);
        accessor.WriteByte(offset + sizeof(uint) * 2 + CodeDirectoryHeader.Size + identifierBytes.Length, 0x00); // null terminator
        int specialSlotHashesOffset = (int)(offset + sizeof(uint) * 2 + CodeDirectoryHeader.Size + identifierBytes.Length + 1);
        for (int i = 0; i < SpecialSlotCount; i++)
        {
            accessor.WriteExactly(specialSlotHashesOffset + i * HashSize, _specialSlotHashes[i]);
        }
        for (int i = 0; i < CodeSlotCount; i++)
        {
            accessor.WriteExactly(offset + HashesOffset + i * HashSize, _codeHashes[i]);
            if (_codeHashes[i].All(h => h == 0))
            {
                throw new InvalidDataException("Code hashes are all zero");
            }
        }
        return (int)Size;
    }
}
