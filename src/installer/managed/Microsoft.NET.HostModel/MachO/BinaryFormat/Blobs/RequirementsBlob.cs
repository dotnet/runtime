// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// See https://github.com/apple-oss-distributions/Security/blob/3dab46a11f45f2ffdbd70e2127cc5a8ce4a1f222/OSX/libsecurity_codesigning/lib/requirement.h#L211
/// Requirements is a SuperBlob.
/// It should be empty but present for all created or written signatures.
/// </summary>
internal sealed class RequirementsBlob : IBlob
{
    private SuperBlob _inner;

    public RequirementsBlob(SuperBlob blob)
    {
        _inner = blob;
        if (blob.Magic != BlobMagic.Requirements)
        {
            throw new ArgumentException($"Expected a SuperBlob with Magic number '{BlobMagic.Requirements}', got '{blob.Magic}'.");
        }
    }

    public static RequirementsBlob Empty { get; } = new RequirementsBlob(new SuperBlob(BlobMagic.Requirements, [], []));

    /// <inheritdoc />
    public BlobMagic Magic => _inner.Magic;

    /// <inheritdoc />
    public uint Size => _inner.Size;

    /// <inheritdoc />
    public int Write(IMachOFileWriter writer, long offset) => _inner.Write(writer, offset);
}
