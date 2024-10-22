// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// See https://github.com/apple-oss-distributions/Security/blob/3dab46a11f45f2ffdbd70e2127cc5a8ce4a1f222/OSX/libsecurity_codesigning/lib/requirement.h#L211
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct RequirementsBlob
{
    private BlobMagic _magic;
    private uint _size;
    private uint _subBlobCount;

    public static RequirementsBlob Empty = new RequirementsBlob
    {
        _magic = (BlobMagic)((uint)BlobMagic.Requirements).MakeBigEndian(),
        _size = 12u.MakeBigEndian(),
        _subBlobCount = 0
    };

    public byte[] GetBytes()
    {
        byte[] buffer = new byte[12];
        BitConverter.GetBytes((uint)_magic).CopyTo(buffer, 0);
        BitConverter.GetBytes(_size).CopyTo(buffer, 4);
        BitConverter.GetBytes(_subBlobCount).CopyTo(buffer, 8);
        return buffer;
    }
}
