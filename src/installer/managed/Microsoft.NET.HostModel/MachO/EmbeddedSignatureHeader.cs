// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// Format based off of https://github.com/apple-oss-distributions/Security/blob/3dab46a11f45f2ffdbd70e2127cc5a8ce4a1f222/OSX/libsecurity_codesigning/lib/cscdefs.h#L23
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct EmbeddedSignatureHeader
{
    private BlobMagic _magic;
    private uint _size;
    private uint _blobCount; // 3
    public BlobIndex CodeDirectory;
    public BlobIndex Requirements;
    public BlobIndex CmsWrapper;

    public BlobMagic Magic
    {
        get => (BlobMagic)((uint)_magic).ConvertFromBigEndian();
        set => _magic = (BlobMagic)((uint)value).MakeBigEndian();
    }
    public uint Size
    {
        get => _size.ConvertFromBigEndian();
        set => _size = value.MakeBigEndian();
    }
    public uint BlobCount
    {
        get => _blobCount.ConvertFromBigEndian();
        set => _blobCount = value.MakeBigEndian();
    }
}
