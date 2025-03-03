// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// Managed class with information about a Mach-O code signature.
/// </summary>
internal unsafe partial class MachObjectFile
{
    private class CodeSignature
    {
        private const uint SpecialSlotCount = 2;
        private const uint PageSize = 4096;
        private const byte Log2PageSize = 12;
        private const byte DefaultHashSize = 32;
        private const HashType DefaultHashType = HashType.SHA256;
        private static IncrementalHash GetDefaultIncrementalHash() => IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        private readonly long _fileOffset;
        private EmbeddedSignatureHeader _embeddedSignature;
        private CodeDirectoryHeader _codeDirectoryHeader;
        private byte[] _identifier;
        private byte[] _codeDirectoryHashes;
        private RequirementsBlob _requirementsBlob;
        private CmsWrapperBlob _cmsWrapperBlob;
        private bool _unrecognizedFormat;

        private CodeSignature(long fileOffset) { _fileOffset = fileOffset; }

        /// <summary>
        /// Creates a new code signature from the file.
        /// The signature is composed of an Embedded Signature Superblob header, followed by a CodeDirectory blob, a Requirements blob, and a CMS blob.
        /// The codesign tool also adds an empty Requirements blob and an empty CMS blob, which are not strictly required but are added here for compatibility.
        /// </summary>
        internal static CodeSignature CreateSignature(MachObjectFile machObject, MemoryMappedViewAccessor file, string identifier)
        {
            uint signatureStart = machObject.GetSignatureStart();
            EmbeddedSignatureHeader embeddedSignature = new();
            CodeDirectoryHeader codeDirectory = CreateCodeDirectoryHeader(machObject, signatureStart, identifier);
            RequirementsBlob requirementsBlob = RequirementsBlob.Empty;
            CmsWrapperBlob cmsWrapperBlob = CmsWrapperBlob.Empty;

            byte[] identifierBytes = new byte[GetIdentifierLength(identifier)];
            Encoding.UTF8.GetBytes(identifier).CopyTo(identifierBytes, 0);

            byte[] codeDirectoryHashes = new byte[(GetCodeSlotCount(signatureStart) + SpecialSlotCount) * DefaultHashSize];

            // Fill in the CodeDirectory hashes
            {
                var hasher = GetDefaultIncrementalHash();

                // Special slot hashes
                int hashSlotsOffset = 0;
                // -2 is the requirements blob hash
                hasher.AppendData(requirementsBlob.GetBytes());
                byte[] hash = hasher.GetHashAndReset();
                Debug.Assert(hash.Length == DefaultHashSize);
                hash.CopyTo(codeDirectoryHashes, hashSlotsOffset);
                hashSlotsOffset += DefaultHashSize;
                // -1 is the CMS blob hash (which is empty -- nothing to hash)
                hashSlotsOffset += DefaultHashSize;

                // 0 - N are Code hashes
                byte[] pageBuffer = new byte[(int)PageSize];
                long remaining = signatureStart;
                long buffptr = 0;
                while (remaining > 0)
                {
                    int codePageSize = (int)Math.Min(remaining, 4096);
                    int bytesRead = file.ReadArray(buffptr, pageBuffer, 0, codePageSize);
                    if (bytesRead != codePageSize)
                        throw new IOException("Could not read all bytes");
                    buffptr += bytesRead;
                    hasher.AppendData(pageBuffer, 0, codePageSize);
                    hash = hasher.GetHashAndReset();
                    Debug.Assert(hash.Length == DefaultHashSize);
                    hash.CopyTo(codeDirectoryHashes, hashSlotsOffset);
                    remaining -= codePageSize;
                    hashSlotsOffset += DefaultHashSize;
                }
            }

            // Create Embedded Signature Header
            embeddedSignature.Size = GetCodeSignatureSize(signatureStart, identifier);
            embeddedSignature.CodeDirectory = new BlobIndex(
                CodeDirectorySpecialSlot.CodeDirectory,
                (uint)sizeof(EmbeddedSignatureHeader));
            embeddedSignature.Requirements = new BlobIndex(
                CodeDirectorySpecialSlot.Requirements,
                (uint)sizeof(EmbeddedSignatureHeader)
                    + GetCodeDirectorySize(signatureStart, identifier));
            embeddedSignature.CmsWrapper = new BlobIndex(
                CodeDirectorySpecialSlot.CmsWrapper,
                (uint)sizeof(EmbeddedSignatureHeader)
                    + GetCodeDirectorySize(signatureStart, identifier)
                    + (uint)sizeof(RequirementsBlob));

            return new CodeSignature(signatureStart)
            {
                _embeddedSignature = embeddedSignature,
                _codeDirectoryHeader = codeDirectory,
                _identifier = identifierBytes,
                _codeDirectoryHashes = codeDirectoryHashes,
                _requirementsBlob = requirementsBlob,
                _cmsWrapperBlob = cmsWrapperBlob
            };
        }

        internal static uint GetCodeSignatureSize(uint signatureStart, string identifier)
        {
            return (uint)(sizeof(EmbeddedSignatureHeader)
                + GetCodeDirectorySize(signatureStart, identifier)
                + sizeof(RequirementsBlob)
                + sizeof(CmsWrapperBlob));
        }

        internal static CodeSignature Read(MemoryMappedViewAccessor file, long fileOffset)
        {
            CodeSignature cs = new CodeSignature(fileOffset);
            file.Read(fileOffset, out cs._embeddedSignature);
            if (cs._embeddedSignature.BlobCount != 3
                || cs._embeddedSignature.CodeDirectory.Slot != CodeDirectorySpecialSlot.CodeDirectory
                || cs._embeddedSignature.Requirements.Slot != CodeDirectorySpecialSlot.Requirements
                || cs._embeddedSignature.CmsWrapper.Slot != CodeDirectorySpecialSlot.CmsWrapper)
            {
                cs._unrecognizedFormat = true;
                return cs;
            }
            var cdOffset = cs._fileOffset + cs._embeddedSignature.CodeDirectory.Offset;
            file.Read(cdOffset, out cs._codeDirectoryHeader);
            if (cs._codeDirectoryHeader.Version != CodeDirectoryVersion.HighestVersion
                || cs._codeDirectoryHeader.HashType != HashType.SHA256
                || cs._codeDirectoryHeader.SpecialSlotCount != SpecialSlotCount)
            {
                cs._unrecognizedFormat = true;
                return cs;
            }

            long identifierOffset = cdOffset + cs._codeDirectoryHeader.IdentifierOffset;
            long codeHashesOffset = cdOffset + cs._codeDirectoryHeader.HashesOffset - (SpecialSlotCount * DefaultHashSize);

            cs._identifier = new byte[codeHashesOffset - identifierOffset];
            file.ReadArray(identifierOffset, cs._identifier, 0, cs._identifier.Length);

            cs._codeDirectoryHashes = new byte[(SpecialSlotCount + cs._codeDirectoryHeader.CodeSlotCount) * DefaultHashSize];
            file.ReadArray(codeHashesOffset, cs._codeDirectoryHashes, 0, cs._codeDirectoryHashes.Length);

            var requirementsOffset = cs._fileOffset + cs._embeddedSignature.Requirements.Offset;
            file.Read(requirementsOffset, out cs._requirementsBlob);
            if (!cs._requirementsBlob.Equals(RequirementsBlob.Empty))
            {
                cs._unrecognizedFormat = true;
                return cs;
            }

            var cmsOffset = fileOffset + cs._embeddedSignature.CmsWrapper.Offset;
            file.Read(cmsOffset, out cs._cmsWrapperBlob);
            if (!cs._cmsWrapperBlob.Equals(CmsWrapperBlob.Empty))
            {
                cs._unrecognizedFormat = true;
                return cs;
            }
            return cs;
        }

        internal void WriteToFile(MemoryMappedViewAccessor file)
        {
            long fileOffset = _fileOffset;

            file.Write(fileOffset, ref _embeddedSignature);
            fileOffset += sizeof(EmbeddedSignatureHeader);

            file.Write(fileOffset, ref _codeDirectoryHeader);
            fileOffset += sizeof(CodeDirectoryHeader);

            file.WriteArray(fileOffset, _identifier, 0, _identifier.Length);
            fileOffset += _identifier.Length;

            file.WriteArray(fileOffset, _codeDirectoryHashes, 0, _codeDirectoryHashes.Length);
            fileOffset += _codeDirectoryHashes.Length;

            file.Write(fileOffset, ref _requirementsBlob);
            fileOffset += sizeof(RequirementsBlob);

            file.Write(fileOffset, ref _cmsWrapperBlob);
            Debug.Assert(fileOffset + sizeof(CmsWrapperBlob) == _fileOffset + _embeddedSignature.Size);
        }

        private static CodeDirectoryHeader CreateCodeDirectoryHeader(MachObjectFile machObject, uint signatureStart, string identifier)
        {
            CodeDirectoryVersion version = CodeDirectoryVersion.HighestVersion;
            uint identifierLength = GetIdentifierLength(identifier);
            uint codeDirectorySize = GetCodeDirectorySize((uint)signatureStart, identifier);

            CodeDirectoryHeader codeDirectoryBlob = new();
            uint hashesOffset;
            hashesOffset = (uint)sizeof(CodeDirectoryHeader) + identifierLength + DefaultHashSize * SpecialSlotCount;
            codeDirectoryBlob.Size = codeDirectorySize;
            codeDirectoryBlob.Version = version;
            codeDirectoryBlob.Flags = CodeDirectoryFlags.Adhoc;
            codeDirectoryBlob.HashesOffset = hashesOffset;
            codeDirectoryBlob.IdentifierOffset = (uint)sizeof(CodeDirectoryHeader);
            codeDirectoryBlob.SpecialSlotCount = SpecialSlotCount;
            codeDirectoryBlob.CodeSlotCount = GetCodeSlotCount(signatureStart);
            codeDirectoryBlob.ExecutableLength = signatureStart > uint.MaxValue ? uint.MaxValue : signatureStart;
            codeDirectoryBlob.HashSize = DefaultHashSize;
            codeDirectoryBlob.HashType = DefaultHashType;
            codeDirectoryBlob.Platform = 0;
            codeDirectoryBlob.Log2PageSize = Log2PageSize;

            codeDirectoryBlob.CodeLimit64 = signatureStart >= uint.MaxValue ? signatureStart : 0;
            codeDirectoryBlob.ExecSegmentBase = machObject._textSegment64.Command.GetFileOffset(machObject._header);
            codeDirectoryBlob.ExecSegmentLimit = machObject._textSegment64.Command.GetFileSize(machObject._header);
            if (machObject._header.FileType == MachFileType.Execute)
                codeDirectoryBlob.ExecSegmentFlags |= ExecutableSegmentFlags.MainBinary;

            return codeDirectoryBlob;
        }

        private static uint GetIdentifierLength(string identifier)
        {
            return (uint)(Encoding.UTF8.GetByteCount(identifier) + 1);
        }

        private static uint GetCodeDirectorySize(uint signatureStart, string identifier)
        {
            return (uint)(sizeof(CodeDirectoryHeader)
                + GetIdentifierLength(identifier)
                + SpecialSlotCount * DefaultHashSize
                + GetCodeSlotCount(signatureStart) * DefaultHashSize);
        }

        private static uint GetCodeSlotCount(uint signatureStart)
        {
            return (signatureStart + PageSize - 1) / PageSize;
        }

        public static bool AreEquivalent(CodeSignature a, CodeSignature b)
        {
            if (a is null ^ b is null)
                return false;
            if (a is null && b is null)
                return true;
            if (a._unrecognizedFormat || b._unrecognizedFormat)
                return false;
            if (!a._embeddedSignature.Equals(b._embeddedSignature))
                return false;
            if (!a._codeDirectoryHeader.Equals(b._codeDirectoryHeader))
                return false;
            if (!a._identifier.SequenceEqual(b._identifier))
                return false;

            var aSpecialSlotHashes = a._codeDirectoryHashes.AsSpan(0, (int)SpecialSlotCount * DefaultHashSize);
            var bSpecialSlotHashes = b._codeDirectoryHashes.AsSpan(0, (int)SpecialSlotCount * DefaultHashSize);
            if (!aSpecialSlotHashes.SequenceEqual(bSpecialSlotHashes))
                return false;
            var aCodeHashes = a._codeDirectoryHashes.AsSpan(((int)SpecialSlotCount + 1) * DefaultHashSize);
            var bCodeHashes = b._codeDirectoryHashes.AsSpan(((int)SpecialSlotCount + 1) * DefaultHashSize);
            if (!aCodeHashes.SequenceEqual(bCodeHashes))
                return false;

            return true;
        }
    }
}
