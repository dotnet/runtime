// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// See https://github.com/apple-oss-distributions/Security/blob/3dab46a11f45f2ffdbd70e2127cc5a8ce4a1f222/OSX/libsecurity_codesigning/lib/codedirectory.h#L86
/// </summary>
internal enum CodeDirectorySpecialSlot
{
    CodeDirectory = 0,
    Requirements = 2,
    CmsWrapper = 0x10000,
}
