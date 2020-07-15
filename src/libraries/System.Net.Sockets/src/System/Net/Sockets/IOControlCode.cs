// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Net.Sockets
{
    public enum IOControlCode : long
    {
        [MinimumOSPlatform("windows7.0")]
        AsyncIO = 0x8004667D,
        NonBlockingIO = 0x8004667E,  // fionbio
        DataToRead = 0x4004667F,  // fionread
        OobDataRead = 0x40047307,
        [MinimumOSPlatform("windows7.0")]
        AssociateHandle = 0x88000001,  // SIO_ASSOCIATE_HANDLE
        [MinimumOSPlatform("windows7.0")]
        EnableCircularQueuing = 0x28000002,
        [MinimumOSPlatform("windows7.0")]
        Flush = 0x28000004,
        [MinimumOSPlatform("windows7.0")]
        GetBroadcastAddress = 0x48000005,
        [MinimumOSPlatform("windows7.0")]
        GetExtensionFunctionPointer = 0xC8000006,
        [MinimumOSPlatform("windows7.0")]
        GetQos = 0xC8000007,
        [MinimumOSPlatform("windows7.0")]
        GetGroupQos = 0xC8000008,
        [MinimumOSPlatform("windows7.0")]
        MultipointLoopback = 0x88000009,
        [MinimumOSPlatform("windows7.0")]
        MulticastScope = 0x8800000A,
        [MinimumOSPlatform("windows7.0")]
        SetQos = 0x8800000B,
        [MinimumOSPlatform("windows7.0")]
        SetGroupQos = 0x8800000C,
        [MinimumOSPlatform("windows7.0")]
        TranslateHandle = 0xC800000D,
        [MinimumOSPlatform("windows7.0")]
        RoutingInterfaceQuery = 0xC8000014,
        [MinimumOSPlatform("windows7.0")]
        RoutingInterfaceChange = 0x88000015,
        [MinimumOSPlatform("windows7.0")]
        AddressListQuery = 0x48000016,
        [MinimumOSPlatform("windows7.0")]
        AddressListChange = 0x28000017,
        [MinimumOSPlatform("windows7.0")]
        QueryTargetPnpHandle = 0x48000018,
        [MinimumOSPlatform("windows7.0")]
        NamespaceChange = 0x88000019,
        [MinimumOSPlatform("windows7.0")]
        AddressListSort = 0xC8000019,
        [MinimumOSPlatform("windows7.0")]
        ReceiveAll = 0x98000001,
        [MinimumOSPlatform("windows7.0")]
        ReceiveAllMulticast = 0x98000002,
        [MinimumOSPlatform("windows7.0")]
        ReceiveAllIgmpMulticast = 0x98000003,
        [MinimumOSPlatform("windows7.0")]
        KeepAliveValues = 0x98000004,
        [MinimumOSPlatform("windows7.0")]
        AbsorbRouterAlert = 0x98000005,
        [MinimumOSPlatform("windows7.0")]
        UnicastInterface = 0x98000006,
        [MinimumOSPlatform("windows7.0")]
        LimitBroadcasts = 0x98000007,
        [MinimumOSPlatform("windows7.0")]
        BindToInterface = 0x98000008,
        [MinimumOSPlatform("windows7.0")]
        MulticastInterface = 0x98000009,
        [MinimumOSPlatform("windows7.0")]
        AddMulticastGroupOnInterface = 0x9800000A,
        [MinimumOSPlatform("windows7.0")]
        DeleteMulticastGroupFromInterface = 0x9800000B
    }
}
