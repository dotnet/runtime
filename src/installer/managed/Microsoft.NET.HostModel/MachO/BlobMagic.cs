// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.HostModel.MachO;

internal enum BlobMagic : uint
{
    Requirements = 0xfade0c01,
    CodeDirectory = 0xfade0c02,
    EmbeddedSignature = 0xfade0cc0,
    CmsWrapper = 0xfade0b01,
}
