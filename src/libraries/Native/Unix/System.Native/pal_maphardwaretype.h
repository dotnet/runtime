// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "pal_compiler.h"

BEGIN_EXTERN_C

#include "pal_types.h"

// Managed interface types
enum NetworkInterfaceType
{
    NetworkInterfaceType_Unknown = 1,
    NetworkInterfaceType_Ethernet = 6,
    NetworkInterfaceType_TokenRing = 9,
    NetworkInterfaceType_Fddi = 15,
    NetworkInterfaceType_BasicIsdn = 20,
    NetworkInterfaceType_PrimaryIsdn = 21,
    NetworkInterfaceType_Ppp = 23,
    NetworkInterfaceType_Loopback = 24,
    NetworkInterfaceType_Ethernet3Megabit = 26,
    NetworkInterfaceType_Slip = 28, // GenericSlip
    NetworkInterfaceType_Atm = 37,
    NetworkInterfaceType_GenericModem = 48,     // GenericModem
    NetworkInterfaceType_FastEthernetT = 62,    // FastEthernet(100BaseT)
    NetworkInterfaceType_Isdn = 63,             // ISDNandX.25
    NetworkInterfaceType_FastEthernetFx = 69,   // FastEthernet(100BaseFX)
    NetworkInterfaceType_Wireless80211 = 71,    // IEEE80211
    NetworkInterfaceType_AsymmetricDsl = 94,    // AsymmetricDigitalSubscrbrLoop
    NetworkInterfaceType_RateAdaptDsl = 95,     // Rate-AdaptDigitalSubscrbrLoop
    NetworkInterfaceType_SymmetricDsl = 96,     // SymmetricDigitalSubscriberLoop
    NetworkInterfaceType_VeryHighSpeedDsl = 97, // VeryH-SpeedDigitalSubscrbLoop
    NetworkInterfaceType_IPOverAtm = 114,
    NetworkInterfaceType_GigabitEthernet = 117,
    NetworkInterfaceType_Tunnel = 131,
    NetworkInterfaceType_MultiRateSymmetricDsl = 143,    // Multi-rate Symmetric DSL
    NetworkInterfaceType_HighPerformanceSerialBus = 144, // IEEE1394
    NetworkInterfaceType_Wman = 237,                     // IF_TYPE_IEEE80216_WMAN WIMAX
    NetworkInterfaceType_Wwanpp = 243,                   // IF_TYPE_WWANPP Mobile Broadband devices based on GSM technology
    NetworkInterfaceType_Wwanpp2 = 244,                  // IF_TYPE_WWANPP2 Mobile Broadband devices based on CDMA technology
};

enum NetworkInterfaceType MapHardwareType(uint16_t nativeType);

END_EXTERN_C
