// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.NET.HostModel.MachO;

[StructLayout(LayoutKind.Sequential)]
internal struct RequirementsBlob
{
    private BlobMagic _magic;
    private uint _requirementsBlobSize;
    private uint _entitlementsBlobLength;

    public static RequirementsBlob Empty = new RequirementsBlob
    {
        _magic = (BlobMagic)((uint)BlobMagic.Requirements).MakeBigEndian(),
        _requirementsBlobSize = 12u.MakeBigEndian(),
        _entitlementsBlobLength = 0
    };

    public byte[] GetBytes()
    {
        byte[] buffer = new byte[12];
        BitConverter.GetBytes((uint)_magic).CopyTo(buffer, 0);
        BitConverter.GetBytes(_requirementsBlobSize).CopyTo(buffer, 4);
        BitConverter.GetBytes(_entitlementsBlobLength).CopyTo(buffer, 8);
        return buffer;
    }
}
