// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Security.Cryptography;
using System.Text;
#nullable enable

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// Managed class with information about a Mach-O code signature.
/// </summary>
internal unsafe partial class MachObjectFile
{
    private class CodeSignature
    {
        private const uint PageSize = MachObjectFile.PageSize;
        private const byte DefaultHashSize = 32;
        private const HashType DefaultHashType = HashType.SHA256;
        private static IncrementalHash GetDefaultIncrementalHash() => IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        internal readonly long FileOffset;
        internal EmbeddedSignatureBlob EmbeddedSignatureBlob;

        private CodeSignature(long fileOffset, EmbeddedSignatureBlob embeddedSignatureBlob)
        {
            FileOffset = fileOffset;
            EmbeddedSignatureBlob = embeddedSignatureBlob;
        }

        /// <summary>
        /// Creates a new code signature from the file.
        /// The signature is composed of an Embedded Signature Superblob header, followed by a CodeDirectory blob, a Requirements blob, and a CMS blob.
        /// The codesign tool also adds an empty Requirements blob and an empty CMS blob, which are not strictly required but are added here for compatibility.
        /// </summary>
        internal static CodeSignature CreateSignature(MachObjectFile machObject, MemoryMappedViewAccessor file, string identifier, CodeSignature? oldSignature)
        {
            var oldSignatureBlob = oldSignature?.EmbeddedSignatureBlob;

            uint signatureStart = machObject.GetSignatureStart();
            RequirementsBlob requirementsBlob = RequirementsBlob.Empty;
            CmsWrapperBlob cmsWrapperBlob = CmsWrapperBlob.Empty;
            EntitlementsBlob? entitlementsBlob = oldSignatureBlob?.EntitlementsBlob;
            DerEntitlementsBlob? derEntitlementsBlob = oldSignatureBlob?.DerEntitlementsBlob;
            uint specialCodeSlotCount = (uint)(
                derEntitlementsBlob is not null ?
                    CodeDirectorySpecialSlot.DerEntitlements :
                entitlementsBlob is not null ?
                    CodeDirectorySpecialSlot.Entitlements :
                    CodeDirectorySpecialSlot.Requirements);


            byte[] identifierBytes = new byte[GetIdentifierLength(identifier)];
            Encoding.UTF8.GetBytes(identifier).CopyTo(identifierBytes, 0);

            byte[] codeDirectoryHashes = new byte[(GetCodeSlotCount(signatureStart) + specialCodeSlotCount) * DefaultHashSize];

            // Fill in the CodeDirectory hashes
            {
                var hasher = GetDefaultIncrementalHash();

                byte[] hash;
                // Special slot hashes
                int hashSlotsOffset = 0;
                // -7 is the der entitlements blob hash
                if (derEntitlementsBlob != null)
                {
                    hasher.AppendData(derEntitlementsBlob.GetBytes());
                    hash = hasher.GetHashAndReset();
                    hash.CopyTo(codeDirectoryHashes, hashSlotsOffset);
                    hashSlotsOffset += DefaultHashSize;

                    // -6 is skipped
                    hashSlotsOffset += DefaultHashSize;
                }

                // -5 is the entitlements blob hash
                if (entitlementsBlob != null)
                {
                    hasher.AppendData(entitlementsBlob.GetBytes());
                    hasher.GetHashAndReset().CopyTo(codeDirectoryHashes, hashSlotsOffset);
                    hashSlotsOffset += DefaultHashSize;
                }

                if (entitlementsBlob != null || derEntitlementsBlob != null)
                {
                    // -4 is skipped
                    hashSlotsOffset += DefaultHashSize;
                    // -3 is skipped
                    hashSlotsOffset += DefaultHashSize;
                }

                // -2 is the requirements blob hash
                hasher.AppendData(requirementsBlob.GetBytes());
                hash = hasher.GetHashAndReset();
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
            CodeDirectoryBlob codeDirectory = new CodeDirectoryBlob(
                identifier: identifier,
                codeSlotCount: GetCodeSlotCount(signatureStart),
                specialCodeSlotCount: specialCodeSlotCount,
                executableLength: signatureStart > uint.MaxValue ? uint.MaxValue : signatureStart,
                hashSize: DefaultHashSize,
                hashType: DefaultHashType,
                signatureStart: signatureStart,
                execSegmentBase: machObject._textSegment64.Command.GetFileOffset(machObject._header),
                execSegmentLimit: machObject._linkEditSegment64.Command.GetFileOffset(machObject._header),
                execSegmentFlags: machObject._header.FileType == MachFileType.Execute ? ExecutableSegmentFlags.MainBinary : 0,
                hashes: codeDirectoryHashes);

            var embeddedSignature = new EmbeddedSignatureBlob(codeDirectory, requirementsBlob, cmsWrapperBlob, entitlementsBlob, derEntitlementsBlob);

            return new CodeSignature(signatureStart, embeddedSignature);
        }

        internal static CodeSignature Read(MemoryMappedViewAccessor file, long fileOffset)
        {
            EmbeddedSignatureBlob signatureBlob = (EmbeddedSignatureBlob)EmbeddedSignatureBlob.Read(file, fileOffset);
            return new CodeSignature(fileOffset, signatureBlob);
        }

        internal void WriteToFile(MemoryMappedViewAccessor file)
        {
            EmbeddedSignatureBlob.Write(file, FileOffset);
        }

        private static uint GetIdentifierLength(string identifier)
        {
            return (uint)(Encoding.UTF8.GetByteCount(identifier) + 1);
        }

        private static uint GetCodeSlotCount(uint signatureStart)
        {
            return (signatureStart + PageSize - 1) / PageSize;
        }

        /// <summary>
        /// Returns the size of a signature used to replace an existing one.
        /// </summary>
        internal static long GetSignatureSize(uint fileSize, string identifier, CodeSignature? existingSignature)
        {
            uint identifierLength = GetIdentifierLength(identifier);
            uint codeSlotCount = GetCodeSlotCount(fileSize);
            uint specialCodeSlotCount = (uint)CodeDirectorySpecialSlot.Requirements;
            uint requirementsBlobSize = RequirementsBlob.Empty.Size;
            uint cmsBlobSize = CmsWrapperBlob.Empty.Size;
            uint entitlementsBlobSize = 0;
            uint derEntitlementsBlobSize = 0;
            uint embeddedSignatureSubBlobCount = 3; // CodeDirectory, Requirements, CMS Wrapper

            if (existingSignature != null)
            {
                // This isn't accurate when the existing signature doesn't have any special slots.
                specialCodeSlotCount = Math.Max((uint)CodeDirectorySpecialSlot.Requirements, existingSignature.EmbeddedSignatureBlob.GetSpecialSlotCount());
                requirementsBlobSize = existingSignature.EmbeddedSignatureBlob.RequirementsBlob?.Size ?? requirementsBlobSize;
                cmsBlobSize = existingSignature.EmbeddedSignatureBlob.CmsWrapperBlob?.Size ?? cmsBlobSize;
                entitlementsBlobSize = existingSignature.EmbeddedSignatureBlob.EntitlementsBlob?.Size ?? entitlementsBlobSize;
                derEntitlementsBlobSize = existingSignature.EmbeddedSignatureBlob.DerEntitlementsBlob?.Size ?? derEntitlementsBlobSize;
                if (existingSignature.EmbeddedSignatureBlob.EntitlementsBlob is not null)
                    embeddedSignatureSubBlobCount += 1;
                if (existingSignature.EmbeddedSignatureBlob.DerEntitlementsBlob is not null)
                    embeddedSignatureSubBlobCount += 1;
            }

            // Calculate the size of the new signature
            long size = 0;
            // EmbeddedSignature
            size += sizeof(BlobMagic); // Signature blob Magic number
            size += sizeof(uint); // Size field
            size += sizeof(uint); // Blob count
            size += sizeof(BlobIndex) * embeddedSignatureSubBlobCount; // EmbeddedSignature sub-blobs
            size += sizeof(BlobMagic); // CD Magic number
            // CodeDirectory
            size += sizeof(uint); // CD Size field
            size += sizeof(CodeDirectoryBlob.CodeDirectoryHeader); // CodeDirectory header
            size += identifierLength; // Identifier
            size += specialCodeSlotCount * DefaultHashSize; // Special code hashes
            size += codeSlotCount * DefaultHashSize; // Code hashes
            // RequirementsBlob
            size += requirementsBlobSize;
            // EntitlementsBlob
            size += entitlementsBlobSize;
            // DER EntitlementsBlob
            size += derEntitlementsBlobSize;
            // CMSWrapperBlob
            size += cmsBlobSize; // CMS blob

            return size;
        }

        /// <summary>
        /// Gets the largest size estimate for a code signature.
        /// </summary>
        public static long GetLargestSizeEstimate(uint fileSize, string identifier)
        {
            uint identifierLength = GetIdentifierLength(identifier);
            uint codeSlotCount = GetCodeSlotCount(fileSize);
            uint specialCodeSlotCount = (uint)CodeDirectorySpecialSlot.DerEntitlements;

            long size = 0;
            size += sizeof(uint) * 2; // SuperBlob header
            size += sizeof(uint); // Blob count
            size += sizeof(BlobMagic); // Magic number
            size += sizeof(CodeDirectoryBlob.CodeDirectoryHeader); // CodeDirectory header
            size += identifierLength; // Identifier
            size += codeSlotCount * DefaultHashSize; // Code hashes
            size += specialCodeSlotCount * DefaultHashSize; // Special code hashes
            size += RequirementsBlob.Empty.Size; // Requirements blob
            size += CmsWrapperBlob.Empty.Size; // CMS blob
            size += EntitlementsBlob.MaxSize;
            size += DerEntitlementsBlob.MaxSize;
            return size + 64;
        }
    }
}
