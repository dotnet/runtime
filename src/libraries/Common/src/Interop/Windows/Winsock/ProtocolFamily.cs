// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.Sockets
{
    // Specifies the type of protocol that an instance of the System.Net.Sockets.Socket
    // class can use.
    internal enum ProtocolFamily
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
    }
}
