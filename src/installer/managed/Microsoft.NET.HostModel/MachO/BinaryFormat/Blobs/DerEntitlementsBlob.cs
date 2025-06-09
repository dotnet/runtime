// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.NET.HostModel.MachO;

internal sealed class DerEntitlementsBlob : SimpleBlob
{
    public DerEntitlementsBlob(byte[] data) : base(BlobMagic.DerEntitlements, data)
    {
        if (Size > MaxSize)
        {
            throw new InvalidDataException($"DerEntitlementsBlob size exceeds maximum allowed size: {Data.Length} > {MaxSize}");
        }
    }

    public DerEntitlementsBlob(SimpleBlob blob) : base(blob)
    {
        if (Size > MaxSize)
        {
            throw new InvalidDataException($"DerEntitlementsBlob size exceeds maximum allowed size: {Size} > {MaxSize}");
        }
        if (Magic != BlobMagic.DerEntitlements)
        {
            throw new InvalidDataException($"Invalid magic for DerEntitlementsBlob: {Magic}");
        }
    }

    public static uint MaxSize => 1024;
}
