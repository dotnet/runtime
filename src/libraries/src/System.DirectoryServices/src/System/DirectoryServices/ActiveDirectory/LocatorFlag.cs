// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.ActiveDirectory
{
    using System;

    /*
	#define DS_FORCE_REDISCOVERY            0x00000001
	#define DS_DIRECTORY_SERVICE_REQUIRED   0x00000010
	#define DS_DIRECTORY_SERVICE_PREFERRED  0x00000020
	#define DS_GC_SERVER_REQUIRED           0x00000040
	#define DS_PDC_REQUIRED                 0x00000080
	#define DS_BACKGROUND_ONLY              0x00000100
	#define DS_IP_REQUIRED                  0x00000200
	#define DS_KDC_REQUIRED                 0x00000400
	#define DS_TIMESERV_REQUIRED            0x00000800
	#define DS_WRITABLE_REQUIRED            0x00001000
	#define DS_GOOD_TIMESERV_PREFERRED      0x00002000
	#define DS_AVOID_SELF                   0x00004000
	#define DS_ONLY_LDAP_NEEDED             0x00008000
	#define DS_IS_FLAT_NAME                 0x00010000
	#define DS_IS_DNS_NAME                  0x00020000
	#define DS_RETURN_DNS_NAME              0x40000000
	#define DS_RETURN_FLAT_NAME             0x80000000
	*/

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

    [Flags]
    internal enum DcEnumFlag : int
    {
        OnlyDoSiteName = 0x01,
        NotifyAfterSiteRecords = 0x02
    }
}
