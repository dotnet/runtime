// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.HostModel.MachO.CodeSign.Blobs
{
    internal enum CodeDirectorySpecialSlot
    {
        InfoPlist = 1,
        Requirements = 2,
        ResourceDirectory = 3,
        TopDirectory = 4,
        Entitlements = 5,
        RepresentationSpecific = 6,
        EntitlementsDer = 7,
        HighestSlot = EntitlementsDer,

        CodeDirectory = 0,
        AlternativeCodeDirectory = 0x1000,
        CmsWrapper = 0x10000,
    }
}
