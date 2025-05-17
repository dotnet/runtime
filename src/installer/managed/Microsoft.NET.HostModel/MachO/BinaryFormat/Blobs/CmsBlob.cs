// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// See https://github.com/apple-oss-distributions/Security/blob/3dab46a11f45f2ffdbd70e2127cc5a8ce4a1f222/OSX/libsecurity_utilities/lib/blob.h
/// Code signature data is always big endian / network order.
/// </summary>
internal sealed class CmsWrapperBlob : SimpleBlob
{
    public CmsWrapperBlob(MemoryMappedViewAccessor accessor, long offset)
        : base(accessor, offset)
    {
        if (Magic != BlobMagic.CmsWrapper)
        {
            throw new InvalidDataException($"Invalid magic for CmsWrapperBlob: {Magic}");
        }
    }

    private CmsWrapperBlob(byte[] data)
        : base(BlobMagic.CmsWrapper, data)
    {
    }

    public static CmsWrapperBlob Empty { get; } = new CmsWrapperBlob([]);

    public override void Write(MemoryMappedViewAccessor accessor, long offset)
    {
        if (_data.Length != 0)
        {
            throw new InvalidOperationException("Writing to a non-empty CmsWrapperBlob is not supported.");
        }
        base.Write(accessor, offset);
    }
}
