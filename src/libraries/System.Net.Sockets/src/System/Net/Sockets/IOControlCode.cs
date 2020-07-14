// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Sockets
{
    public enum IOControlCode : long
    {
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        AsyncIO = 0x8004667D,
        NonBlockingIO = 0x8004667E,  // fionbio
        DataToRead = 0x4004667F,  // fionread
        OobDataRead = 0x40047307,
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        AssociateHandle = 0x88000001,  // SIO_ASSOCIATE_HANDLE
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        EnableCircularQueuing = 0x28000002,
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        Flush = 0x28000004,
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        GetBroadcastAddress = 0x48000005,
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        GetExtensionFunctionPointer = 0xC8000006,
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        GetQos = 0xC8000007,
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        GetGroupQos = 0xC8000008,
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        MultipointLoopback = 0x88000009,
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        MulticastScope = 0x8800000A,
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        SetQos = 0x8800000B,
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        SetGroupQos = 0x8800000C,
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        TranslateHandle = 0xC800000D,
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        RoutingInterfaceQuery = 0xC8000014,
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        RoutingInterfaceChange = 0x88000015,
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        AddressListQuery = 0x48000016,
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        AddressListChange = 0x28000017,
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        QueryTargetPnpHandle = 0x48000018,
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        NamespaceChange = 0x88000019,
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        AddressListSort = 0xC8000019,
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        ReceiveAll = 0x98000001,
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        ReceiveAllMulticast = 0x98000002,
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        ReceiveAllIgmpMulticast = 0x98000003,
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        KeepAliveValues = 0x98000004,
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        AbsorbRouterAlert = 0x98000005,
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        UnicastInterface = 0x98000006,
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        LimitBroadcasts = 0x98000007,
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        BindToInterface = 0x98000008,
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        MulticastInterface = 0x98000009,
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        AddMulticastGroupOnInterface = 0x9800000A,
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        DeleteMulticastGroupFromInterface = 0x9800000B
    }
}
