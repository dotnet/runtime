// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable enable

using System;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// Format based off of https://github.com/apple-oss-distributions/Security/blob/3dab46a11f45f2ffdbd70e2127cc5a8ce4a1f222/OSX/libsecurity_codesigning/lib/cscdefs.h#L23
/// Code Signature data is always big endian / network order.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal class EmbeddedSignatureBlob : SuperBlob
{
    public CodeDirectoryBlob CodeDirectoryBlob
    {
        get => (CodeDirectoryBlob)Blobs.Single(b => b.Magic == BlobMagic.CodeDirectory);
        set
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value), "CodeDirectoryBlob cannot be null");
            }
            RemoveBlob(CodeDirectorySpecialSlot.CodeDirectory);
            AddBlob(value, CodeDirectorySpecialSlot.CodeDirectory);
        }
    }

    public RequirementsBlob RequirementsBlob
    {
        get => (RequirementsBlob)Blobs.Single(b => b.Magic == BlobMagic.Requirements);
        set
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value), "RequirementsBlob cannot be null");
            }
            RemoveBlob(CodeDirectorySpecialSlot.Requirements);
            AddBlob(value, CodeDirectorySpecialSlot.Requirements);
        }
    }
    public CmsWrapperBlob CmsWrapperBlob {
        get => (CmsWrapperBlob) Blobs.Single(b => b.Magic == BlobMagic.CmsWrapper);
        set
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value), "CmsWrapperBlob cannot be null");
            }
            RemoveBlob(CodeDirectorySpecialSlot.CmsWrapper);
            AddBlob(value, CodeDirectorySpecialSlot.CmsWrapper);
        }
    }
    public EntitlementsBlob? EntitlementsBlob
    {
        get => (EntitlementsBlob?)Blobs.SingleOrDefault(b => b.Magic == BlobMagic.Entitlements);
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
        get => (DerEntitlementsBlob?)Blobs.SingleOrDefault(b => b.Magic == BlobMagic.DerEntitlements);
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
        return BlobIndices.Max(b => 0xFF & (uint)b.Slot);
    }
}
