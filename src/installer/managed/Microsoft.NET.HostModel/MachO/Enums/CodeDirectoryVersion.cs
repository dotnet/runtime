// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.HostModel.MachO;

/// <summary>
/// See https://github.com/apple-oss-distributions/Security/blob/3dab46a11f45f2ffdbd70e2127cc5a8ce4a1f222/OSX/libsecurity_codesigning/lib/codedirectory.h#L222-L227
/// </summary>
internal enum CodeDirectoryVersion : int
{
    Baseline = 0x20001,
    SupportsScatter = 0x20100,
    SupportsTeamId = 0x20200,
    SupportsCodeLimit64 = 0x20300,
    SupportsExecSegment = 0x20400,
    SupportsPreEncrypt = 0x20500,
    HighestVersion = SupportsExecSegment, // TODO: We don't support pre-encryption yet
}
