// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.HostModel.MachO.CodeSign.Blobs
{
    internal enum BlobMagic : uint
    {
        Requirement = 0xfade0c00,
        Requirements = 0xfade0c01,
        CodeDirectory = 0xfade0c02,
        EmbeddedSignature = 0xfade0cc0,
        DetachedSignature = 0xfade0cc1,
        CmsWrapper = 0xfade0b01,
        Entitlements = 0xfade7171,
        EntitlementsDer = 0xfade7172,
    }
}
