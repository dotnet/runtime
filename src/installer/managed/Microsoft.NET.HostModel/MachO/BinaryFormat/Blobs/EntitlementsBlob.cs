// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// See https://github.com/apple-oss-distributions/Security/blob/3dab46a11f45f2ffdbd70e2127cc5a8ce4a1f222/OSX/libsecurity_utilities/lib/blob.h
/// Code signature data is always big endian / network order.
/// </summary>
internal sealed class EntitlementsBlob : SimpleBlob
{
    public EntitlementsBlob(byte[] data)
        : base(BlobMagic.Entitlements, data)
    {
        if (Size > MaxSize)
        {
            throw new ArgumentException($"EntitlementsBlob data exceeds maximum size of {MaxSize} bytes.", nameof(data));
        }
    }

    public EntitlementsBlob(SimpleBlob blob)
        : base(blob)
    {
        if (Magic != BlobMagic.Entitlements)
        {
            throw new InvalidDataException($"Invalid magic for EntitlementsBlob: {Magic}");
        }
        if (Size > MaxSize)
        {
            throw new InvalidDataException($"EntitlementsBlob data exceeds maximum size of {MaxSize} bytes.");
        }
    }

    public static uint MaxSize => 2048;
}
