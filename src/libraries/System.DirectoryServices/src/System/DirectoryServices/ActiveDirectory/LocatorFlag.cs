// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.ActiveDirectory
{
    [Flags]
    public enum LocatorOptions : long
    {
        ForceRediscovery = 0x00000001,
        KdcRequired = 0x00000400,
        TimeServerRequired = 0x00000800,
        WriteableRequired = 0x00001000,
        AvoidSelf = 0x00004000
    }

    [Flags]
    internal enum PrivateLocatorFlags : long
    {
        DirectoryServicesRequired = 0x00000010,
        DirectoryServicesPreferred = 0x00000020,
        GCRequired = 0x00000040,
        PdcRequired = 0x00000080,
        BackgroundOnly = 0x00000100,
        IPRequired = 0x00000200,
        DSWriteableRequired = 0x00001000,
        GoodTimeServerPreferred = 0x00002000,
        OnlyLDAPNeeded = 0X0008000,
        IsFlatName = 0x00010000,
        IsDNSName = 0x00020000,
        ReturnDNSName = 0x40000000,
        ReturnFlatName = 0x80000000
    }
}
