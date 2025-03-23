// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// Format based off of https://github.com/apple-oss-distributions/Security/blob/3dab46a11f45f2ffdbd70e2127cc5a8ce4a1f222/OSX/libsecurity_codesigning/lib/cscdefs.h#L23
/// Code Signature data is always big endian / network order.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct EmbeddedSignatureHeader
{
    private readonly BlobMagic _magic = (BlobMagic)((uint)BlobMagic.EmbeddedSignature).ConvertToBigEndian();
    private uint _size;
    private readonly uint _blobCount = 3u.ConvertToBigEndian();
    public BlobIndex CodeDirectory;
    public BlobIndex Requirements;
    public BlobIndex CmsWrapper;

    public EmbeddedSignatureHeader() { }

    public uint BlobCount => _blobCount.ConvertFromBigEndian();
    public uint Size
    {
        get => _size.ConvertFromBigEndian();
        set => _size = value.ConvertToBigEndian();
    }
}
