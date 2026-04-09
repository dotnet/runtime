// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Sockets
{
    /// <summary>Specifies the addressing scheme that an instance of the Socket class can use.</summary>
    public enum AddressFamily
    {
        Unknown = -1,           // Unknown
        Unspecified = 0,        // Unspecified
        Unix = 1,               // Local to host (pipes, portals)
        InterNetwork = 2,       // Internetwork: UDP, TCP, etc.
        ImpLink = 3,            // ARPAnet imp addresses
        Pup = 4,                // pup protocols: e.g. BSP
        Chaos = 5,              // MIT CHAOS protocols
        NS = 6,                 // XEROX NS protocols
        Ipx = NS,               // IPX and SPX
        Iso = 7,                // ISO protocols
        Osi = Iso,              // OSI is ISO
        Ecma = 8,               // European Computer Manufacturers
        DataKit = 9,            // DataKit protocols
        Ccitt = 10,             // CCITT protocols, X.25 etc
        Sna = 11,               // IBM SNA
        DecNet = 12,            // DECnet
        DataLink = 13,          // Direct data link interface
        Lat = 14,               // LAT
        HyperChannel = 15,      // NSC Hyperchannel
        AppleTalk = 16,         // AppleTalk
        NetBios = 17,           // NetBios-style addresses
        VoiceView = 18,         // VoiceView
        FireFox = 19,           // FireFox
        Banyan = 21,            // Banyan
        Atm = 22,               // Native ATM Services
        InterNetworkV6 = 23,    // Internetwork Version 6
        Cluster = 24,           // Microsoft Wolfpack
        Ieee12844 = 25,         // IEEE 1284.4 WG AF
        Irda = 26,              // IrDA
        NetworkDesigners = 28,  // Network Designers OSI & gateway enabled protocols
        Max = 29,               // Max
        // Unix specific values are past Uint16.MaxValue to avoid conflicts with Windows values.
        // On Windows we pass values straight to OS and if we add new protocol supported by Windows,
        // we should use actual OS value.
        Packet = 65536,         // Linux Packet
        ControllerAreaNetwork = 65537, // Controller Area Network automotive bus protocol
    }
}
