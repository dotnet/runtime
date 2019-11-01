// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.NetworkInformation
{
    /// <summary>
    /// Specifies types of network interfaces.
    /// </summary>
    public enum NetworkInterfaceType
    {
        Unknown = 1,
        Ethernet = 6,
        TokenRing = 9,
        Fddi = 15,
        BasicIsdn = 20,
        PrimaryIsdn = 21,
        Ppp = 23,
        Loopback = 24,
        Ethernet3Megabit = 26,
        Slip = 28, // GenericSlip
        Atm = 37,
        GenericModem = 48, // GenericModem
        FastEthernetT = 62, // FastEthernet(100BaseT)
        Isdn = 63, // ISDNandX.25
        FastEthernetFx = 69, // FastEthernet(100BaseFX)
        Wireless80211 = 71, // IEEE80211
        AsymmetricDsl = 94, // AsymmetricDigitalSubscrbrLoop
        RateAdaptDsl = 95, // Rate-AdaptDigitalSubscrbrLoop
        SymmetricDsl = 96, // SymmetricDigitalSubscriberLoop
        VeryHighSpeedDsl = 97, // VeryH-SpeedDigitalSubscrbLoop
        IPOverAtm = 114,
        GigabitEthernet = 117,
        Tunnel = 131,
        MultiRateSymmetricDsl = 143, // Multi-rate Symmetric DSL
        HighPerformanceSerialBus = 144, // IEEE1394
        Wman = 237, // IF_TYPE_IEEE80216_WMAN WIMAX
        Wwanpp = 243, // IF_TYPE_WWANPP Mobile Broadband devices based on GSM technology
        Wwanpp2 = 244, // IF_TYPE_WWANPP2 Mobile Broadband devices based on CDMA technology
    }
}
