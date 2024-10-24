// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.NET.HostModel.MachO;

[StructLayout(LayoutKind.Sequential)]
internal struct CmsWrapperBlob
{
    private BlobMagic _magic;
    private uint _length;

    public static CmsWrapperBlob Empty = new CmsWrapperBlob
    {
        _magic = (BlobMagic)((uint)BlobMagic.CmsWrapper).MakeBigEndian(),
        _length = 8u.MakeBigEndian()
    };
}
