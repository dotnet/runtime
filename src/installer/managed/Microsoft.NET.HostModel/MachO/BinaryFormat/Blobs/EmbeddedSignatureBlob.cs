// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// Format based off of https://github.com/apple-oss-distributions/Security/blob/3dab46a11f45f2ffdbd70e2127cc5a8ce4a1f222/OSX/libsecurity_codesigning/lib/cscdefs.h#L23
/// Code Signature data is always big endian / network order.
/// The EmbeddedSignatureBlob is a SuperBlob that usually contains the CodeDirectoryBlob, RequirementsBlob, and CmsWrapperBlob.
/// The RequirementsBlob and CmsWrapperBlob may be null if the blob is not present in the read file (usually linker signed MachO files),
/// but will be present in newly created signatures.
/// </summary>
internal sealed class EmbeddedSignatureBlob : IBlob
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
        EntitlementsBlob? entitlementsBlob = null,
        DerEntitlementsBlob? derEntitlementsBlob = null)
    {
        int blobCount = 3 + (entitlementsBlob is not null ? 1 : 0) + (derEntitlementsBlob is not null ? 1 : 0);
        var blobs = ImmutableArray.CreateBuilder<IBlob>(blobCount);
        var blobIndices = ImmutableArray.CreateBuilder<BlobIndex>(blobCount);
        uint nextBlobOffset = (uint)(sizeof(uint) * 3 + (BlobIndex.Size * blobCount));

        blobs.Add(codeDirectoryBlob);
        blobIndices.Add(new BlobIndex(CodeDirectorySpecialSlot.CodeDirectory, nextBlobOffset));
        nextBlobOffset += codeDirectoryBlob.Size;

        blobs.Add(requirementsBlob);
        blobIndices.Add(new BlobIndex(CodeDirectorySpecialSlot.Requirements, nextBlobOffset));
        nextBlobOffset += requirementsBlob.Size;

        blobs.Add(cmsWrapperBlob);
        blobIndices.Add(new BlobIndex(CodeDirectorySpecialSlot.CmsWrapper, nextBlobOffset));
        nextBlobOffset += cmsWrapperBlob.Size;

        if (entitlementsBlob is not null)
        {
            blobs.Add(entitlementsBlob);
            blobIndices.Add(new BlobIndex(CodeDirectorySpecialSlot.Entitlements, nextBlobOffset));
            nextBlobOffset += entitlementsBlob.Size;
        }
        if (derEntitlementsBlob is not null)
        {
            blobs.Add(derEntitlementsBlob);
            blobIndices.Add(new BlobIndex(CodeDirectorySpecialSlot.DerEntitlements, nextBlobOffset));
        }
        _inner = new SuperBlob(BlobMagic.EmbeddedSignature, blobIndices.MoveToImmutable(), blobs.MoveToImmutable());
    }

    public BlobMagic Magic => _inner.Magic;
    public uint Size => _inner.Size;
    public uint SubBlobCount => _inner.SubBlobCount;

    /// <summary>
    /// The CodeDirectoryBlob. This is always present in the signature.
    /// </summary>
    public CodeDirectoryBlob CodeDirectoryBlob => (CodeDirectoryBlob)GetBlob(BlobMagic.CodeDirectory, throwIfNotFound: true)!;

    /// <summary>
    /// The RequirementsBlob. This may be null if the blob is not present in the read file, but will be present in newly created signatures
    /// </summary>
    public RequirementsBlob? RequirementsBlob => GetBlob(BlobMagic.Requirements) as RequirementsBlob;

    /// <summary>
    /// The CmsWrapperBlob. This may be null if the blob is not present in the read file, but will be present in newly created signatures
    /// </summary>
    public CmsWrapperBlob? CmsWrapperBlob => GetBlob(BlobMagic.CmsWrapper) as CmsWrapperBlob;

    /// <summary>
    /// The EntitlementsBlob. This is only included in created signatures if present in the original signature.
    /// </summary>
    public EntitlementsBlob? EntitlementsBlob => GetBlob(BlobMagic.Entitlements) as EntitlementsBlob;

    /// <summary>
    /// The DerEntitlementsBlob. This is only included in created signatures if present in the original signature.
    /// </summary>
    public DerEntitlementsBlob? DerEntitlementsBlob => GetBlob(BlobMagic.DerEntitlements) as DerEntitlementsBlob;

    public uint GetSpecialSlotHashCount()
    {
        uint maxSlot = 0;
        foreach (var b in _inner.BlobIndices)
        {
            // Blobs that have special slots hashes have their slot value in the lower 8 bits.
            // CMSWrapperBlob has a special slot value of 0x1000 and does not have a hash.
            uint slot = 0xFF & (uint)b.Slot;
            if (slot > maxSlot)
            {
                maxSlot = slot;
            }
        }
        Debug.Assert((CodeDirectorySpecialSlot)maxSlot is 0 or CodeDirectorySpecialSlot.Requirements or CodeDirectorySpecialSlot.Entitlements or CodeDirectorySpecialSlot.DerEntitlements);
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
        size += (long)(uint)CodeDirectorySpecialSlot.DerEntitlements * usedHashSize; // Special code hashes. The highest special slot is DerEntitlements.

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
    internal static unsafe long GetSignatureSize(uint fileSize, string identifier, EmbeddedSignatureBlob? existingSignature = null, byte? hashSize = null)
    {
        byte usedHashSize = hashSize ?? CodeDirectoryBlob.DefaultHashType.GetHashSize();
        // CodeDirectory, Requirements, CMS Wrapper are always present
        uint specialCodeSlotCount = (uint)CodeDirectorySpecialSlot.Requirements;
        uint embeddedSignatureSubBlobCount = 3;
        uint entitlementsBlobSize = 0;
        uint derEntitlementsBlobSize = 0;

        if (existingSignature != null)
        {
            // We preserve Entitlements and DER Entitlements blobs if they exist in the old signature.
            // We need to update the relevant sizes and counts to reflect this.
            specialCodeSlotCount = Math.Max((uint)CodeDirectorySpecialSlot.Requirements, existingSignature.GetSpecialSlotHashCount());
            if (existingSignature.EntitlementsBlob is not null)
            {
                entitlementsBlobSize = existingSignature.EntitlementsBlob.Size;
                embeddedSignatureSubBlobCount += 1;
            }
            if (existingSignature.DerEntitlementsBlob is not null)
            {
                derEntitlementsBlobSize = existingSignature.DerEntitlementsBlob.Size;
                embeddedSignatureSubBlobCount += 1;
            }
        }

        // Calculate the size of the new signature
        long size = 0;
        // EmbeddedSignature
        size += sizeof(BlobMagic); // Signature blob Magic number
        size += sizeof(uint); // Size field
        size += sizeof(uint); // Blob count
        size += sizeof(BlobIndex) * embeddedSignatureSubBlobCount; // EmbeddedSignature sub-blobs
        // CodeDirectory
        size += sizeof(BlobMagic); // CodeDirectory Magic number
        size += sizeof(uint); // CodeDirectory Size field
        size += sizeof(CodeDirectoryBlob.CodeDirectoryHeader); // CodeDirectory header
        size += CodeDirectoryBlob.GetIdentifierLength(identifier); // Identifier
        size += specialCodeSlotCount * usedHashSize; // Special code hashes
        size += CodeDirectoryBlob.GetCodeSlotCount(fileSize) * usedHashSize; // Code hashes
        // RequirementsBlob is always empty
        size += RequirementsBlob.Empty.Size;
        // EntitlementsBlob
        size += entitlementsBlobSize;
        // DER EntitlementsBlob
        size += derEntitlementsBlobSize;
        // CMSWrapperBlob is always empty
        size += CmsWrapperBlob.Empty.Size;

        return size;
    }

    private IBlob? GetBlob(BlobMagic magic, bool throwIfNotFound = false)
    {
        foreach (var b in _inner.Blobs)
        {
            if (b.Magic == magic)
            {
                return b;
            }
        }
        if (throwIfNotFound)
        {
            throw new InvalidOperationException($"{magic} blob not found.");
        }
        return null;
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

        if (a.CmsWrapperBlob?.Size != b.CmsWrapperBlob?.Size)
            throw new ArgumentException("CMS Wrapper blobs are not equivalent");

        if (a.EntitlementsBlob?.Size != b.EntitlementsBlob?.Size)
            throw new ArgumentException("Entitlements blobs are not equivalent");

        if (a.DerEntitlementsBlob?.Size != b.DerEntitlementsBlob?.Size)
            throw new ArgumentException("DER Entitlements blobs are not equivalent");
    }
}
