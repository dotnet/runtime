// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// See https://github.com/apple-oss-distributions/Security/blob/3dab46a11f45f2ffdbd70e2127cc5a8ce4a1f222/OSX/libsecurity_utilities/lib/blob.h
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct CmsWrapperBlob
{
    private BlobMagic _magic;
    private uint _length;

    public static CmsWrapperBlob Empty = new CmsWrapperBlob
    {
        _magic = (BlobMagic)((uint)BlobMagic.CmsWrapper).ConvertToBigEndian(),
        _length = 8u.ConvertToBigEndian()
    };
}
