// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO.MemoryMappedFiles;
#nullable enable

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// Format based off of https://github.com/apple-oss-distributions/Security/blob/3dab46a11f45f2ffdbd70e2127cc5a8ce4a1f222/OSX/libsecurity_codesigning/lib/cscdefs.h#L23
/// Code Signature data is always big endian / network order.
/// </summary>
internal class EmbeddedSignatureBlob : SuperBlob
{
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
        set
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value), "CodeDirectoryBlob cannot be set to null");
            }
            RemoveBlob(CodeDirectorySpecialSlot.CodeDirectory);
            AddBlob(value, CodeDirectorySpecialSlot.CodeDirectory);
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
        set
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value), "RequirementsBlob cannot be set to null");
            }
            RemoveBlob(CodeDirectorySpecialSlot.Requirements);
            AddBlob(value, CodeDirectorySpecialSlot.Requirements);
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
        set
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value), "CmsWrapperBlob cannot be set to null");
            }
            RemoveBlob(CodeDirectorySpecialSlot.CmsWrapper);
            AddBlob(value, CodeDirectorySpecialSlot.CmsWrapper);
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
        set
        {
            RemoveBlob(CodeDirectorySpecialSlot.Entitlements);
            if (value != null)
            {
                AddBlob(value, CodeDirectorySpecialSlot.Entitlements);
            }
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
        set
        {
            RemoveBlob(CodeDirectorySpecialSlot.DerEntitlements);
            if (value != null)
            {
                AddBlob(value, CodeDirectorySpecialSlot.DerEntitlements);
            }
        }
    }

    public EmbeddedSignatureBlob(MemoryMappedViewAccessor accessor, long offset) : base(accessor, offset)
    {
    }

    public EmbeddedSignatureBlob(CodeDirectoryBlob codeDirectoryBlob, RequirementsBlob requirementsBlob, CmsWrapperBlob cmsWrapperBlob, EntitlementsBlob? entitlementsBlob, DerEntitlementsBlob? derEntitlementsBlob)
        : base(BlobMagic.EmbeddedSignature)
    {
        AddBlob(codeDirectoryBlob, CodeDirectorySpecialSlot.CodeDirectory);
        AddBlob(requirementsBlob, CodeDirectorySpecialSlot.Requirements);
        AddBlob(cmsWrapperBlob, CodeDirectorySpecialSlot.CmsWrapper);
        if (entitlementsBlob != null)
        {
            AddBlob(entitlementsBlob, CodeDirectorySpecialSlot.Entitlements);
        }
        if (derEntitlementsBlob != null)
        {
            AddBlob(derEntitlementsBlob, CodeDirectorySpecialSlot.DerEntitlements);
        }
    }

    public uint GetSpecialSlotCount()
    {
        uint maxSlot = 0;
        foreach (var b in BlobIndices)
        {
            uint slot = 0xFF & (uint)b.Slot;
            if (slot > maxSlot)
            {
                maxSlot = slot;
            }
        }
        return maxSlot;
    }
}
