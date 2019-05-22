// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if SYSTEM_NET_SOCKETS_DLL
namespace System.Net.Sockets
#else
using System.Net.Sockets;
namespace System.Net.Internals
#endif
{
    public enum ProtocolFamily
    {
        Unknown = AddressFamily.Unknown,
        Unspecified = AddressFamily.Unspecified,
        Unix = AddressFamily.Unix,
        InterNetwork = AddressFamily.InterNetwork,
        ImpLink = AddressFamily.ImpLink,
        Pup = AddressFamily.Pup,
        Chaos = AddressFamily.Chaos,
        NS = AddressFamily.NS,
        Ipx = AddressFamily.Ipx,
        Iso = AddressFamily.Iso,
        Osi = AddressFamily.Osi,
        Ecma = AddressFamily.Ecma,
        DataKit = AddressFamily.DataKit,
        Ccitt = AddressFamily.Ccitt,
        Sna = AddressFamily.Sna,
        DecNet = AddressFamily.DecNet,
        DataLink = AddressFamily.DataLink,
        Lat = AddressFamily.Lat,
        HyperChannel = AddressFamily.HyperChannel,
        AppleTalk = AddressFamily.AppleTalk,
        NetBios = AddressFamily.NetBios,
        VoiceView = AddressFamily.VoiceView,
        FireFox = AddressFamily.FireFox,
        Banyan = AddressFamily.Banyan,
        Atm = AddressFamily.Atm,
        InterNetworkV6 = AddressFamily.InterNetworkV6,
        Cluster = AddressFamily.Cluster,
        Ieee12844 = AddressFamily.Ieee12844,
        Irda = AddressFamily.Irda,
        NetworkDesigners = AddressFamily.NetworkDesigners,
        Max = 29, //AddressFamily.Max
        Netlink = AddressFamily.Netlink,
        Packet = AddressFamily.Packet,
        ControllerAreaNetwork = AddressFamily.ControllerAreaNetwork,
    }
}
