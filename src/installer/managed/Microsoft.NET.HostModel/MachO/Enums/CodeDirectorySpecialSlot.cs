// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.HostModel.MachO;

internal enum CodeDirectorySpecialSlot
{
    Requirements = 2,

    CodeDirectory = 0,
    CmsWrapper = 0x10000,
}
