// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// See https://github.com/apple-oss-distributions/Security/blob/3dab46a11f45f2ffdbd70e2127cc5a8ce4a1f222/OSX/libsecurity_codesigning/lib/requirement.h#L211
/// Code signature data is always big endian / network order.
/// Requirements is a SuperBlob.
/// </summary>
internal class RequirementsBlob : SuperBlob
{
    public RequirementsBlob(MemoryMappedViewAccessor accessor, long offset)
        : base(accessor, offset)
    {
        if (Magic != BlobMagic.Requirements)
        {
            throw new InvalidDataException($"Invalid magic for RequirementsBlob: {Magic}");
        }
    }

    private RequirementsBlob()
        : base(BlobMagic.Requirements)
    {
    }

    public static RequirementsBlob Empty { get; } = new RequirementsBlob();
}
