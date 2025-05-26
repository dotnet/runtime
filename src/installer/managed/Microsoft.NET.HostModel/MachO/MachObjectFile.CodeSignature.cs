// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
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
        private static byte[] EmptyHash => new byte[DefaultHashSize];
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
        /// The signature is composed of an Embedded Signature Superblob header, followed by a CodeDirectory blob, a Requirements blob, and a CMS blob. Optionally, it may also contain an Entitlements blob and a DER Entitlements blob if the <paramref name="oldSignature"/> contains them.
        /// The codesign tool also adds an empty Requirements blob and an empty CMS blob, which are not strictly required but are added here for compatibility.
        /// If there are entitlements blobs in the old signature, they are preserved in the new signature.
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


            byte[][] specialSlotHashes = new byte[(int)specialCodeSlotCount][];
            for (int i = 0; i < specialSlotHashes.Length; i++)
            {
                specialSlotHashes[i] = EmptyHash;
            }
            byte[][] codeDirectoryHashes = new byte[(int)GetCodeSlotCount(signatureStart)][];

            // Fill in the CodeDirectory hashes
            var hasher = GetDefaultIncrementalHash();

            // Special slot hashes
            // -7 is the der entitlements blob hash
            if (derEntitlementsBlob != null)
            {
                hasher.AppendData(derEntitlementsBlob.GetBytes());
                specialSlotHashes[(int)CodeDirectorySpecialSlot.DerEntitlements - 1] = hasher.GetHashAndReset();
            }

            // -5 is the entitlements blob hash
            if (entitlementsBlob != null)
            {
                hasher.AppendData(entitlementsBlob.GetBytes());
                specialSlotHashes[(int)CodeDirectorySpecialSlot.Entitlements - 1] = hasher.GetHashAndReset();
            }

            // -2 is the requirements blob hash
            hasher.AppendData(requirementsBlob.GetBytes());
            byte[] hash = hasher.GetHashAndReset();
            Debug.Assert(hash.Length == DefaultHashSize);
            specialSlotHashes[(int)CodeDirectorySpecialSlot.Requirements - 1] = hash;
            // -1 is the CMS blob hash (which is empty -- nothing to hash)

            specialSlotHashes.Reverse();
            // 0 - N are Code hashes
            byte[] pageBuffer = new byte[(int)PageSize];
            long remaining = signatureStart;
            long buffptr = 0;
            int cdIndex = 0;
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
                codeDirectoryHashes[cdIndex++] = hash;
                remaining -= codePageSize;
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
                specialSlotHashes: specialSlotHashes,
                codeHashes: codeDirectoryHashes);

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
        /// If the existing signature is null, it will assume sizing using the default signature, which includes the Requirements and CMS blobs.
        /// If the existing signature is not null, it will preserve the Entitlements and DER Entitlements blobs if they exist.
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
                // We preserve Entitlements and DER Entitlements blobs if they exist in the old signature.
                // We need to update the relevant sizes and counts to reflect this.
                specialCodeSlotCount = Math.Max((uint)CodeDirectorySpecialSlot.Requirements, existingSignature.EmbeddedSignatureBlob.GetSpecialSlotCount());
                // Requirements and CMSWrapper blobs are always overwritten as emtpy, but present.
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

        public static bool AreEquivalent(CodeSignature a, CodeSignature b)
        {
            if (a.EmbeddedSignatureBlob == null && b.EmbeddedSignatureBlob == null)
                return true;

            if (a.EmbeddedSignatureBlob == null || b.EmbeddedSignatureBlob == null)
                return false;

            if (a.FileOffset != b.FileOffset)
                return false;

            if (a.EmbeddedSignatureBlob.GetSpecialSlotCount() != b.EmbeddedSignatureBlob.GetSpecialSlotCount())
                return false;

            if (!a.EmbeddedSignatureBlob.CodeDirectoryBlob.Equals(b.EmbeddedSignatureBlob.CodeDirectoryBlob))
                throw new ArgumentException("CodeDirectory blobs are not equivalent");

            if (a.EmbeddedSignatureBlob.RequirementsBlob == null ^ b.EmbeddedSignatureBlob.RequirementsBlob == null)
                return false;

            if (a.EmbeddedSignatureBlob.EntitlementsBlob == null ^ b.EmbeddedSignatureBlob.EntitlementsBlob == null)
                return false;

            if (a.EmbeddedSignatureBlob.DerEntitlementsBlob == null ^ b.EmbeddedSignatureBlob.DerEntitlementsBlob == null)
                return false;

            if (a.EmbeddedSignatureBlob.CmsWrapperBlob == null ^ b.EmbeddedSignatureBlob.CmsWrapperBlob == null)
                return false;

            // TODO: Compare the contents of the blobs

            return true;
        }
    }
}
