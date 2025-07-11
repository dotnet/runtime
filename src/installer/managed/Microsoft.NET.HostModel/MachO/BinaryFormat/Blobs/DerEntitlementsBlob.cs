// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.NET.HostModel.MachO;

internal sealed class DerEntitlementsBlob : IBlob
{
    private SimpleBlob _inner;

    public DerEntitlementsBlob(SimpleBlob blob)
    {
        _inner = blob;
        if (blob.Size > MaxSize)
        {
            throw new InvalidDataException($"DerEntitlementsBlob size exceeds maximum allowed size: {blob.Data.Length} > {MaxSize}");
        }
        if (blob.Magic != BlobMagic.DerEntitlements)
        {
            throw new InvalidDataException($"Invalid magic for DerEntitlementsBlob: {blob.Magic}");
        }
    }

    public static uint MaxSize => 1024;

    /// <inheritdoc />
    public BlobMagic Magic => ((IBlob)_inner).Magic;

    /// <inheritdoc />
    public uint Size => ((IBlob)_inner).Size;

    /// <inheritdoc />
    public int Write(IMachOFileWriter writer, long offset) => ((IBlob)_inner).Write(writer, offset);
}
