// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// See https://github.com/apple-oss-distributions/Security/blob/3dab46a11f45f2ffdbd70e2127cc5a8ce4a1f222/OSX/libsecurity_utilities/lib/blob.h
/// The CMS wrapper blob is a simple blob. It should be empty, but present for all created / written signatures.
/// </summary>
internal sealed class CmsWrapperBlob : SimpleBlob
{
    public static CmsWrapperBlob Empty { get; } = new CmsWrapperBlob([]);

    public CmsWrapperBlob(byte[] data) : base(BlobMagic.CmsWrapper, data)
    {
    }

    public CmsWrapperBlob(SimpleBlob blob) : base(blob)
    {
        if (blob.Magic != BlobMagic.CmsWrapper)
        {
            throw new ArgumentException($"Cannot create CmsWrapperBlob of blob with magic value '{blob.Magic}'. Magic value must be {BlobMagic.CmsWrapper}");
        }
    }
}
