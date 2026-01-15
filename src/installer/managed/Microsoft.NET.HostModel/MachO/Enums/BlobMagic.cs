// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// See https://github.com/apple-oss-distributions/Security/blob/3dab46a11f45f2ffdbd70e2127cc5a8ce4a1f222/OSX/libsecurity_codesigning/lib/cscdefs.h#L4
/// </summary>
internal enum BlobMagic : uint
{
    EmbeddedSignature = 0xfade0cc0,
    CodeDirectory = 0xfade0c02,
    Requirements = 0xfade0c01,
    CmsWrapper = 0xfade0b01,
}
