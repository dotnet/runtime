// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// See https://github.com/apple-oss-distributions/Security/blob/3dab46a11f45f2ffdbd70e2127cc5a8ce4a1f222/OSX/libsecurity_utilities/lib/blob.h
/// Code signature data is always big endian / network order.
/// </summary>
internal sealed class EntitlementsBlob : IBlob
{
    private SimpleBlob _inner;

    public EntitlementsBlob(SimpleBlob blob)
    {
        _inner = blob;
        if (blob.Magic != BlobMagic.Entitlements)
        {
            throw new InvalidDataException($"Invalid magic for EntitlementsBlob: {blob.Magic}");
        }
        if (blob.Size > MaxSize)
        {
            throw new InvalidDataException($"EntitlementsBlob data exceeds maximum size of {MaxSize} bytes.");
        }
    }

    public static uint MaxSize => 2048;

    /// <inheritdoc />
    public BlobMagic Magic => ((IBlob)_inner).Magic;

    /// <inheritdoc />
    public uint Size => ((IBlob)_inner).Size;

    /// <inheritdoc />
    public int Write(IMachOFileWriter writer, long offset) => ((IBlob)_inner).Write(writer, offset);
}
