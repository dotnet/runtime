// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Sockets
{
    public enum IOControlCode : long
    {
        AsyncIO = 0x8004667D,
        NonBlockingIO = 0x8004667E,  // fionbio
        DataToRead = 0x4004667F,  // fionread
        OobDataRead = 0x40047307,
        AssociateHandle = 0x88000001,  // SIO_ASSOCIATE_HANDLE
        EnableCircularQueuing = 0x28000002,
        Flush = 0x28000004,
        GetBroadcastAddress = 0x48000005,
        GetExtensionFunctionPointer = 0xC8000006,
        GetQos = 0xC8000007,
        GetGroupQos = 0xC8000008,
        MultipointLoopback = 0x88000009,
        MulticastScope = 0x8800000A,
        SetQos = 0x8800000B,
        SetGroupQos = 0x8800000C,
        TranslateHandle = 0xC800000D,
        RoutingInterfaceQuery = 0xC8000014,
        RoutingInterfaceChange = 0x88000015,
        AddressListQuery = 0x48000016,
        AddressListChange = 0x28000017,
        QueryTargetPnpHandle = 0x48000018,
        NamespaceChange = 0x88000019,
        AddressListSort = 0xC8000019,
        ReceiveAll = 0x98000001,
        ReceiveAllMulticast = 0x98000002,
        ReceiveAllIgmpMulticast = 0x98000003,
        KeepAliveValues = 0x98000004,
        AbsorbRouterAlert = 0x98000005,
        UnicastInterface = 0x98000006,
        LimitBroadcasts = 0x98000007,
        BindToInterface = 0x98000008,
        MulticastInterface = 0x98000009,
        AddMulticastGroupOnInterface = 0x9800000A,
        DeleteMulticastGroupFromInterface = 0x9800000B
    }
}
