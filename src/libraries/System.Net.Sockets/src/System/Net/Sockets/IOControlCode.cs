// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Net.Sockets
{
    public enum IOControlCode : long
    {
        [SupportedOSPlatform("windows")]
        AsyncIO = 0x8004667D,
        NonBlockingIO = 0x8004667E,  // fionbio
        DataToRead = 0x4004667F,  // fionread
        OobDataRead = 0x40047307,
        [SupportedOSPlatform("windows")]
        AssociateHandle = 0x88000001,  // SIO_ASSOCIATE_HANDLE
        [SupportedOSPlatform("windows")]
        EnableCircularQueuing = 0x28000002,
        [SupportedOSPlatform("windows")]
        Flush = 0x28000004,
        [SupportedOSPlatform("windows")]
        GetBroadcastAddress = 0x48000005,
        [SupportedOSPlatform("windows")]
        GetExtensionFunctionPointer = 0xC8000006,
        [SupportedOSPlatform("windows")]
        GetQos = 0xC8000007,
        [SupportedOSPlatform("windows")]
        GetGroupQos = 0xC8000008,
        [SupportedOSPlatform("windows")]
        MultipointLoopback = 0x88000009,
        [SupportedOSPlatform("windows")]
        MulticastScope = 0x8800000A,
        [SupportedOSPlatform("windows")]
        SetQos = 0x8800000B,
        [SupportedOSPlatform("windows")]
        SetGroupQos = 0x8800000C,
        [SupportedOSPlatform("windows")]
        TranslateHandle = 0xC800000D,
        [SupportedOSPlatform("windows")]
        RoutingInterfaceQuery = 0xC8000014,
        [SupportedOSPlatform("windows")]
        RoutingInterfaceChange = 0x88000015,
        [SupportedOSPlatform("windows")]
        AddressListQuery = 0x48000016,
        [SupportedOSPlatform("windows")]
        AddressListChange = 0x28000017,
        [SupportedOSPlatform("windows")]
        QueryTargetPnpHandle = 0x48000018,
        [SupportedOSPlatform("windows")]
        NamespaceChange = 0x88000019,
        [SupportedOSPlatform("windows")]
        AddressListSort = 0xC8000019,
        [SupportedOSPlatform("windows")]
        ReceiveAll = 0x98000001,
        [SupportedOSPlatform("windows")]
        ReceiveAllMulticast = 0x98000002,
        [SupportedOSPlatform("windows")]
        ReceiveAllIgmpMulticast = 0x98000003,
        [SupportedOSPlatform("windows")]
        KeepAliveValues = 0x98000004,
        [SupportedOSPlatform("windows")]
        AbsorbRouterAlert = 0x98000005,
        [SupportedOSPlatform("windows")]
        UnicastInterface = 0x98000006,
        [SupportedOSPlatform("windows")]
        LimitBroadcasts = 0x98000007,
        [SupportedOSPlatform("windows")]
        BindToInterface = 0x98000008,
        [SupportedOSPlatform("windows")]
        MulticastInterface = 0x98000009,
        [SupportedOSPlatform("windows")]
        AddMulticastGroupOnInterface = 0x9800000A,
        [SupportedOSPlatform("windows")]
        DeleteMulticastGroupFromInterface = 0x9800000B
    }
}
