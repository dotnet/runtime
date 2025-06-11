// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Immutable;
using System.IO;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// Format based off of https://github.com/apple-oss-distributions/Security/blob/3dab46a11f45f2ffdbd70e2127cc5a8ce4a1f222/OSX/libsecurity_codesigning/lib/cscdefs.h#L23
/// Code Signature data is always big endian / network order.
/// The EmbeddedSignatureBlob is a SuperBlob that usually contains the CodeDirectoryBlob, RequirementsBlob, and CmsWrapperBlob.
/// The RequirementsBlob and CmsWrapperBlob may be null if the blob is not present in the read file (usually linker signed MachO files),
/// but will be present in newly created signatures.
/// Optionally, it may also contain the EntitlementsBlob and DerEntitlementsBlob.
/// </summary>
internal sealed class EmbeddedSignatureBlob : ISuperBlob
{
    private SuperBlob _inner;

    public EmbeddedSignatureBlob(SuperBlob superBlob)
    {
        _inner = superBlob;
        if (superBlob.Magic != BlobMagic.EmbeddedSignature)
        {
            throw new InvalidDataException($"Invalid magic for EmbeddedSignatureBlob: {superBlob.Magic}");
        }
    }

    /// <summary>
    /// Creates a new EmbeddedSignatureBlob with the specified blobs.
    /// </summary>
    public EmbeddedSignatureBlob(
        CodeDirectoryBlob codeDirectoryBlob,
        RequirementsBlob requirementsBlob,
        CmsWrapperBlob cmsWrapperBlob,
        EntitlementsBlob? entitlementsBlob,
        DerEntitlementsBlob? derEntitlementsBlob)
    {
        int blobCount = 3 + (entitlementsBlob is not null ? 1 : 0) + (derEntitlementsBlob is not null ? 1 : 0);
        var blobs = ImmutableArray.CreateBuilder<IBlob>(blobCount);
        var blobIndices = ImmutableArray.CreateBuilder<BlobIndex>(blobCount);
        uint expectedOffset = (uint)(sizeof(uint) * 3 + (BlobIndex.Size * blobCount));
        blobs.Add(codeDirectoryBlob);
        blobIndices.Add(new BlobIndex(CodeDirectorySpecialSlot.CodeDirectory, expectedOffset));
        expectedOffset += codeDirectoryBlob.Size;
        blobs.Add(requirementsBlob);
        blobIndices.Add(new BlobIndex(CodeDirectorySpecialSlot.Requirements, expectedOffset));
        expectedOffset += requirementsBlob.Size;
        blobs.Add(cmsWrapperBlob);
        blobIndices.Add(new BlobIndex(CodeDirectorySpecialSlot.CmsWrapper, expectedOffset));
        expectedOffset += cmsWrapperBlob.Size;
        if (entitlementsBlob is not null)
        {
            blobs.Add(entitlementsBlob);
            blobIndices.Add(new BlobIndex(CodeDirectorySpecialSlot.Entitlements, expectedOffset));
            expectedOffset += entitlementsBlob.Size;
        }
        if (derEntitlementsBlob is not null)
        {
            blobs.Add(derEntitlementsBlob);
            blobIndices.Add(new BlobIndex(CodeDirectorySpecialSlot.DerEntitlements, expectedOffset));
        }
        _inner = new SuperBlob(BlobMagic.EmbeddedSignature, blobIndices.MoveToImmutable(), blobs.MoveToImmutable());
    }

    public BlobMagic Magic => _inner.Magic;
    public uint Size => _inner.Size;
    public uint BlobCount => ((ISuperBlob)_inner).BlobCount;
    public ImmutableArray<BlobIndex> BlobIndices => _inner.BlobIndices;
    public ImmutableArray<IBlob> Blobs => _inner.Blobs;

    public CodeDirectoryBlob CodeDirectoryBlob
    {
        get
        {
            foreach (var b in Blobs)
            {
                if (b.Magic == BlobMagic.CodeDirectory)
                    return (CodeDirectoryBlob)b;
            }
            throw new InvalidOperationException("CodeDirectoryBlob not found.");
        }
    }

    /// <summary>
    /// The RequirementsBlob. This may be null if the blob is not present in the read file, but will be present in newly created signatures
    /// </summary>
    public RequirementsBlob? RequirementsBlob
    {
        get
        {
            foreach (var b in Blobs)
            {
                if (b.Magic == BlobMagic.Requirements)
                    return (RequirementsBlob)b;
            }
            return null;
        }
    }

    /// <summary>
    /// The CmsWrapperBlob. This may be null if the blob is not present in the read file, but will be present in newly created signatures
    /// </summary>
    public CmsWrapperBlob? CmsWrapperBlob
    {
        get
        {
            foreach (var b in Blobs)
            {
                if (b.Magic == BlobMagic.CmsWrapper)
                    return (CmsWrapperBlob)b;
            }
            return null;
        }
    }

    public EntitlementsBlob? EntitlementsBlob
    {
        get
        {
            foreach (var b in Blobs)
            {
                if (b.Magic == BlobMagic.Entitlements)
                    return (EntitlementsBlob)b;
            }
            return null;
        }
    }

    public DerEntitlementsBlob? DerEntitlementsBlob
    {
        get
        {
            foreach (var b in Blobs)
            {
                if (b.Magic == BlobMagic.DerEntitlements)
                    return (DerEntitlementsBlob)b;
            }
            return null;
        }
    }


    public uint GetSpecialSlotHashCount()
    {
        uint maxSlot = 0;
        foreach (var b in BlobIndices)
        {
            // Blobs that have special slots hashes have their slot value in the lower 8 bits.
            // CMSWrapperBlob has a special slot value of 0x1000 and does not have a hash.
            uint slot = 0xFF & (uint)b.Slot;
            if (slot > maxSlot)
            {
                maxSlot = slot;
            }
        }
        return maxSlot;
    }

    public int Write(IMachOFileWriter writer, long offset)
    {
        return _inner.Write(writer, offset);
    }

    /// <summary>
    /// Gets the largest size estimate for a code signature.
    /// </summary>
    public static unsafe long GetLargestSizeEstimate(uint fileSize, string identifier, byte? hashSize = null)
    {
        byte usedHashSize = hashSize ?? CodeDirectoryBlob.DefaultHashType.GetHashSize();

        long size = 0;
        // SuperBlob header
        size += sizeof(BlobMagic);
        size += sizeof(uint); // Blob size
        size += sizeof(uint); // Blob count
        size += sizeof(BlobIndex) * 5; // 5 sub-blobs: CodeDirectory, Requirements, CmsWrapper, Entitlements, DerEntitlements

        // CodeDirectoryBlob
        size += sizeof(BlobMagic);
        size += sizeof(uint); // Blob size
        size += sizeof(CodeDirectoryBlob.CodeDirectoryHeader); // CodeDirectory header
        size += CodeDirectoryBlob.GetIdentifierLength(identifier); // Identifier
        size += (long)CodeDirectoryBlob.GetCodeSlotCount(fileSize) * usedHashSize; // Code hashes
        size += (long)(uint)CodeDirectorySpecialSlot.DerEntitlements * usedHashSize; // Special code hashes

        size += RequirementsBlob.Empty.Size; // Requirements is always written as an empty blob
        size += CmsWrapperBlob.Empty.Size; // CMS blob is always written as an empty blob
        size += EntitlementsBlob.MaxSize;
        size += DerEntitlementsBlob.MaxSize;
        return size;
    }

    /// <summary>
    /// Returns the size of a signature used to replace an existing one.
    /// If the existing signature is null, it will assume sizing using the default signature, which includes the Requirements and CMS blobs.
    /// If the existing signature is not null, it will preserve the Entitlements and DER Entitlements blobs if they exist.
    /// </summary>
    internal static unsafe long GetSignatureSize(uint fileSize, string identifier, EmbeddedSignatureBlob? existingSignature, byte? hashSize = null)
    {
        byte usedHashSize = hashSize ?? CodeDirectoryBlob.DefaultHashType.GetHashSize();
        uint specialCodeSlotCount = (uint)CodeDirectorySpecialSlot.Requirements;
        uint embeddedSignatureSubBlobCount = 3; // CodeDirectory, Requirements, CMS Wrapper are always present
        uint entitlementsBlobSize = 0;
        uint derEntitlementsBlobSize = 0;

        if (existingSignature != null)
        {
            // We preserve Entitlements and DER Entitlements blobs if they exist in the old signature.
            // We need to update the relevant sizes and counts to reflect this.
            specialCodeSlotCount = Math.Max((uint)CodeDirectorySpecialSlot.Requirements, existingSignature.GetSpecialSlotHashCount());
            entitlementsBlobSize = existingSignature.EntitlementsBlob?.Size ?? 0;
            derEntitlementsBlobSize = existingSignature.DerEntitlementsBlob?.Size ?? 0;
            // Requirements and CMSWrapper blobs are always overwritten as emtpy, but present.
            if (existingSignature.EntitlementsBlob is not null)
                embeddedSignatureSubBlobCount += 1;
            if (existingSignature.DerEntitlementsBlob is not null)
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
        size += CodeDirectoryBlob.GetIdentifierLength(identifier); // Identifier
        size += specialCodeSlotCount * usedHashSize; // Special code hashes
        size += CodeDirectoryBlob.GetCodeSlotCount(fileSize) * usedHashSize; // Code hashes
                                                 // RequirementsBlob
        size += RequirementsBlob.Empty.Size;
        // EntitlementsBlob
        size += entitlementsBlobSize;
        // DER EntitlementsBlob
        size += derEntitlementsBlobSize;
        // CMSWrapperBlob
        size += CmsWrapperBlob.Empty.Size; // CMS blob

        return size;
    }

    public static void AssertEquivalent(EmbeddedSignatureBlob? a, EmbeddedSignatureBlob? b)
    {
        if (a == null && b == null)
            return;

        if (a == null || b == null)
            throw new ArgumentNullException("Both EmbeddedSignatureBlobs must be non-null for comparison.");

        if (a.GetSpecialSlotHashCount() != b.GetSpecialSlotHashCount())
            throw new ArgumentException("Special slot hash counts are not equivalent.");

        if (!a.CodeDirectoryBlob.Equals(b.CodeDirectoryBlob))
            throw new ArgumentException("CodeDirectory blobs are not equivalent");

        if (a.RequirementsBlob?.Size != b.RequirementsBlob?.Size)
            throw new ArgumentException("Requirements blobs are not equivalent");

        if (a.EntitlementsBlob?.Size != b.EntitlementsBlob?.Size)
            throw new ArgumentException("Entitlements blobs are not equivalent");

        if (a.DerEntitlementsBlob?.Size != b.DerEntitlementsBlob?.Size)
            throw new ArgumentException("DER Entitlements blobs are not equivalent");

        if (a.CmsWrapperBlob?.Size != b.CmsWrapperBlob?.Size)
            throw new ArgumentException("CMS Wrapper blobs are not equivalent");
    }
}
