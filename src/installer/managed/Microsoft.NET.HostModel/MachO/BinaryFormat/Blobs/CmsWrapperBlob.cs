// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// See https://github.com/apple-oss-distributions/Security/blob/3dab46a11f45f2ffdbd70e2127cc5a8ce4a1f222/OSX/libsecurity_utilities/lib/blob.h
/// The CMS wrapper blob is a simple blob.
/// Blobs read from a file may have data, but it should be empty (but still present) for all created / written signatures.
/// </summary>
internal sealed class CmsWrapperBlob : IBlob
{
    private SimpleBlob _inner;

    public CmsWrapperBlob(SimpleBlob blob)
    {
        _inner = blob;
        if (blob.Magic != BlobMagic.CmsWrapper)
        {
            throw new ArgumentException($"Cannot create CmsWrapperBlob of blob with magic value '{blob.Magic}'. Magic value must be {BlobMagic.CmsWrapper}");
        }
    }

    public static CmsWrapperBlob Empty { get; } = new CmsWrapperBlob(new SimpleBlob(BlobMagic.CmsWrapper, []));

    public BlobMagic Magic => _inner.Magic;

    public uint Size => _inner.Size;

    public int Write(IMachOFileWriter writer, long offset) => _inner.Write(writer, offset);
}
