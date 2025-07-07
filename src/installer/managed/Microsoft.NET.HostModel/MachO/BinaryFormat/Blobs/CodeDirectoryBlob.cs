// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
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

        int identifierDataOffset = GetDataOffset(cdHeader.IdentifierOffset);
        int nullTerminatorIndex = data.AsSpan().Slice(identifierDataOffset).IndexOf((byte)0x00);
        string identifier = Encoding.UTF8.GetString(data, identifierDataOffset, nullTerminatorIndex);

        var specialSlotCount = cdHeader.SpecialSlotCount;
        var codeSlotCount = cdHeader.CodeSlotCount;
        var hashSize = cdHeader.HashSize;
        var hashesDataOffset = GetDataOffset(cdHeader.HashesOffset);

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

    public string Identifier => _identifier;
    public CodeDirectoryFlags Flags => _cdHeader.Flags;
    public CodeDirectoryVersion Version => _cdHeader.Version;
    public IReadOnlyList<IReadOnlyList<byte>> SpecialSlotHashes => _specialSlotHashes;

    // Properties for test assertions only
    internal IReadOnlyList<IReadOnlyList<byte>> CodeHashes => _codeHashes;
    internal ulong ExecutableSegmentBase => _cdHeader.ExecSegmentBase;
    internal ulong ExecutableSegmentLimit => _cdHeader.ExecSegmentLimit;
    internal ExecutableSegmentFlags ExecutableSegmentFlags => _cdHeader.ExecSegmentFlags;

    private uint SpecialSlotCount => _cdHeader.SpecialSlotCount;
    private uint CodeSlotCount => _cdHeader.CodeSlotCount;
    private byte HashSize => _cdHeader.HashSize;
    private uint HashesOffset => _cdHeader.HashesOffset;

    public static CodeDirectoryBlob Create(
        IMachOFileReader accessor,
        long signatureStart,
        string identifier,
        RequirementsBlob requirementsBlob,
        EntitlementsBlob? entitlementsBlob = null,
        DerEntitlementsBlob? derEntitlementsBlob = null,
        HashType hashType = HashType.SHA256,
        uint pageSize = MachObjectFile.DefaultPageSize)
    {
        uint codeSlotCount = GetCodeSlotCount((uint)signatureStart, pageSize);
        uint specialCodeSlotCount = (uint)(derEntitlementsBlob != null
            ? CodeDirectorySpecialSlot.DerEntitlements
            : entitlementsBlob != null
                ? CodeDirectorySpecialSlot.Entitlements
                : CodeDirectorySpecialSlot.Requirements);

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
            using var derStream = new MemoryStreamWriter((int)derEntitlementsBlob.Size);
            derEntitlementsBlob.Write(derStream, 0);
            specialSlotHashes[(int)CodeDirectorySpecialSlot.DerEntitlements - 1] = hasher.ComputeHash(derStream.GetBuffer());
        }

        // -5 is the entitlements blob hash
        if (entitlementsBlob != null)
        {
            using var entStream = new MemoryStreamWriter((int)entitlementsBlob.Size);
            entitlementsBlob.Write(entStream, 0);
            specialSlotHashes[(int)CodeDirectorySpecialSlot.Entitlements - 1] = hasher.ComputeHash(entStream.GetBuffer());
        }

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
#pragma warning disable CA1805 // Do not initialize unnecessarily
        private readonly uint _reserved = 0;
        private readonly uint _scatterOffset = 0;
        private readonly uint _teamIdOffset = 0;
        private readonly uint _reserved2 = 0;
#pragma warning restore CA1805 // Do not initialize unnecessarily
        private ulong _codeLimit64;
        private ulong _execSegmentBase;
        private ulong _execSegmentLimit;
        private ExecutableSegmentFlags _execSegmentFlags;

        public static readonly uint Size = GetSize();

        public CodeDirectoryVersion Version => (CodeDirectoryVersion)((uint)_version).ConvertFromBigEndian();
        public CodeDirectoryFlags Flags => (CodeDirectoryFlags)((uint)_flags).ConvertFromBigEndian();
        public uint HashesOffset => _hashesOffset.ConvertFromBigEndian();
        public uint IdentifierOffset => _identifierOffset.ConvertFromBigEndian();
        public uint SpecialSlotCount => _specialSlotCount.ConvertFromBigEndian();
        public uint CodeSlotCount => _codeSlotCount.ConvertFromBigEndian();
        public ulong ExecSegmentBase => _execSegmentBase.ConvertFromBigEndian();
        public ulong ExecSegmentLimit
        {
            get => _execSegmentLimit.ConvertFromBigEndian();
            private set => _execSegmentLimit = value < uint.MaxValue ? 0 : value.ConvertToBigEndian();
        }
        public ExecutableSegmentFlags ExecSegmentFlags => (ExecutableSegmentFlags)((ulong)_execSegmentFlags).ConvertFromBigEndian();

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

        public static bool AreEqual(CodeDirectoryHeader first, CodeDirectoryHeader second)
        {
            // Ignore the exec segment limit for equality checks, as it may differ between codesign and the managed implementation.
            first.ExecSegmentLimit = 0;
            second.ExecSegmentLimit = 0;
            return first.Equals(second);
        }
    }

    public override bool Equals(object? obj)
    {
        if (obj is not CodeDirectoryBlob other)
            return false;

        if (_identifier != other._identifier)
            return false;

        CodeDirectoryHeader thisHeader = _cdHeader;
        CodeDirectoryHeader otherHeader = other._cdHeader;
        if (!CodeDirectoryHeader.AreEqual(thisHeader, otherHeader))
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
        Debug.Assert(sizeof(uint) * 2 + CodeDirectoryHeader.Size == _cdHeader.IdentifierOffset);
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
