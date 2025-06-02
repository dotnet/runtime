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
    private static class CodeSignature
    {

        /// <summary>
        /// Creates a new code signature from the file.
        /// The signature is composed of an Embedded Signature Superblob header, followed by a CodeDirectory blob, a Requirements blob, and a CMS blob. Optionally, it may also contain an Entitlements blob and a DER Entitlements blob if the <paramref name="oldSignature"/> contains them.
        /// The codesign tool also adds an empty Requirements blob and an empty CMS blob, which are not strictly required but are added here for compatibility.
        /// If there are entitlements blobs in the old signature, they are preserved in the new signature.
        /// </summary>
        internal static EmbeddedSignatureBlob CreateSignature(MachObjectFile machObject, MemoryMappedViewAccessor file, string identifier, EmbeddedSignatureBlob? oldSignature)
        {
            var oldSignatureBlob = oldSignature;

            uint signatureStart = machObject.GetSignatureStart();
            RequirementsBlob requirementsBlob = RequirementsBlob.Empty;
            CmsWrapperBlob cmsWrapperBlob = CmsWrapperBlob.Empty;
            EntitlementsBlob? entitlementsBlob = oldSignatureBlob?.EntitlementsBlob;
            DerEntitlementsBlob? derEntitlementsBlob = oldSignatureBlob?.DerEntitlementsBlob;

            var codeDirectory = CodeDirectoryBlob.Create(
                file,
                signatureStart,
                identifier,
                requirementsBlob,
                entitlementsBlob,
                derEntitlementsBlob);

            return new EmbeddedSignatureBlob(
                codeDirectoryBlob: codeDirectory,
                requirementsBlob: requirementsBlob,
                cmsWrapperBlob: cmsWrapperBlob,
                entitlementsBlob: entitlementsBlob,
                derEntitlementsBlob: derEntitlementsBlob);


            // byte[][] specialSlotHashes = new byte[(int)specialCodeSlotCount][];
            // for (int i = 0; i < specialSlotHashes.Length; i++)
            // {
            //     specialSlotHashes[i] = new byte[CodeDirectoryBlob.DefaultHashType.GetHashSize()];
            // }
            // byte[][] codeDirectoryHashes = new byte[(int)CodeDirectoryBlob.GetCodeSlotCount(signatureStart)][];

            // // Fill in the CodeDirectory hashes
            // var hashType = CodeDirectoryBlob.DefaultHashType;
            // var hashSize = hashType.GetHashSize();
            // var hasher = hashType.CreateIncrementalHash();

            // // Special slot hashes
            // // -7 is the der entitlements blob hash
            // if (derEntitlementsBlob != null)
            // {
            //     hasher.AppendData(derEntitlementsBlob.GetBytes());
            //     specialSlotHashes[(int)CodeDirectorySpecialSlot.DerEntitlements - 1] = hasher.GetHashAndReset();
            // }

            // // -5 is the entitlements blob hash
            // if (entitlementsBlob != null)
            // {
            //     hasher.AppendData(entitlementsBlob.GetBytes());
            //     specialSlotHashes[(int)CodeDirectorySpecialSlot.Entitlements - 1] = hasher.GetHashAndReset();
            // }

            // // -2 is the requirements blob hash
            // hasher.AppendData(requirementsBlob.GetBytes());
            // byte[] hash = hasher.GetHashAndReset();
            // Debug.Assert(hash.Length == hashSize);
            // specialSlotHashes[(int)CodeDirectorySpecialSlot.Requirements - 1] = hash;
            // // -1 is the CMS blob hash (which is empty -- nothing to hash)

            // specialSlotHashes.Reverse();
            // // 0 - N are Code hashes
            // byte[] pageBuffer = new byte[(int)DefaultPageSize];
            // long remaining = signatureStart;
            // long buffptr = 0;
            // int cdIndex = 0;
            // while (remaining > 0)
            // {
            //     int codePageSize = (int)Math.Min(remaining, 4096);
            //     int bytesRead = file.ReadArray(buffptr, pageBuffer, 0, codePageSize);
            //     if (bytesRead != codePageSize)
            //         throw new IOException("Could not read all bytes");
            //     buffptr += bytesRead;
            //     hasher.AppendData(pageBuffer, 0, codePageSize);
            //     hash = hasher.GetHashAndReset();
            //     Debug.Assert(hash.Length == hashSize);
            //     codeDirectoryHashes[cdIndex++] = hash;
            //     remaining -= codePageSize;
            // }
            // throw new NotImplementedException("CodeDirectoryBlob.Create() is not implemented yet. This is a placeholder for the code signature creation logic.");

            // CodeDirectoryBlob codeDirectory = new CodeDirectoryBlob(
            //     identifier: identifier,
            //     codeSlotCount: CodeDirectoryBlob.GetCodeSlotCount(signatureStart),
            //     specialCodeSlotCount: specialCodeSlotCount,
            //     executableLength: signatureStart > uint.MaxValue ? uint.MaxValue : signatureStart,
            //     hashSize: hashSize,
            //     hashType: hashType,
            //     signatureStart: signatureStart,
            //     execSegmentBase: machObject._textSegment64.Command.GetFileOffset(machObject._header),
            //     execSegmentLimit: machObject._linkEditSegment64.Command.GetFileOffset(machObject._header),
            //     execSegmentFlags: machObject._header.FileType == MachFileType.Execute ? ExecutableSegmentFlags.MainBinary : 0,
            //     specialSlotHashes: specialSlotHashes,
            //     codeHashes: codeDirectoryHashes);

            // var embeddedSignature = new EmbeddedSignatureBlob(codeDirectory, requirementsBlob, cmsWrapperBlob, entitlementsBlob, derEntitlementsBlob);

            // return embeddedSignature;
        }
    }
}
