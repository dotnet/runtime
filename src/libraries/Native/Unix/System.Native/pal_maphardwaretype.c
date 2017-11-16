// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_config.h"
#include "pal_maphardwaretype.h"

#include <sys/socket.h>
#include <sys/types.h>

#if defined(AF_PACKET)
#include <linux/if_packet.h>
#include <linux/if_arp.h>
#elif defined(AF_LINK)
#include <net/if_dl.h>
#include <net/if_types.h>
#else
#error System must have AF_PACKET or AF_LINK.
#endif

enum NetworkInterfaceType MapHardwareType(uint16_t nativeType)
{
#if defined(AF_PACKET)
    switch (nativeType)
    {
        case ARPHRD_ETHER:
        case ARPHRD_EETHER:
            return NetworkInterfaceType_Ethernet;
        case ARPHRD_PRONET:
            return NetworkInterfaceType_TokenRing;
        case ARPHRD_ATM:
            return NetworkInterfaceType_Atm;
        case ARPHRD_SLIP:
        case ARPHRD_CSLIP:
        case ARPHRD_SLIP6:
        case ARPHRD_CSLIP6:
            return NetworkInterfaceType_Slip;
        case ARPHRD_PPP:
            return NetworkInterfaceType_Ppp;
        case ARPHRD_TUNNEL:
        case ARPHRD_TUNNEL6:
            return NetworkInterfaceType_Tunnel;
        case ARPHRD_LOOPBACK:
            return NetworkInterfaceType_Loopback;
        case ARPHRD_FDDI:
            return NetworkInterfaceType_Fddi;
        case ARPHRD_IEEE80211:
        case ARPHRD_IEEE80211_PRISM:
        case ARPHRD_IEEE80211_RADIOTAP:
            return NetworkInterfaceType_Wireless80211;
        default:
            return NetworkInterfaceType_Unknown;
    }
#elif defined(AF_LINK)
    switch (nativeType)
    {
        case IFT_ETHER:
            return NetworkInterfaceType_Ethernet;
        case IFT_ISO88025:
            return NetworkInterfaceType_TokenRing;
        case IFT_FDDI:
            return NetworkInterfaceType_Fddi;
        case IFT_ISDNBASIC:
            return NetworkInterfaceType_Isdn;
        case IFT_ISDNPRIMARY:
            return NetworkInterfaceType_PrimaryIsdn;
        case IFT_PPP:
            return NetworkInterfaceType_Ppp;
        case IFT_LOOP:
            return NetworkInterfaceType_Loopback;
        case IFT_XETHER:
            return NetworkInterfaceType_Ethernet3Megabit;
        case IFT_SLIP:
            return NetworkInterfaceType_Slip;
        case IFT_ATM:
            return NetworkInterfaceType_Atm;
        case IFT_MODEM:
            return NetworkInterfaceType_GenericModem;
        case IFT_IEEE1394:
            return NetworkInterfaceType_HighPerformanceSerialBus;
        default:
            return NetworkInterfaceType_Unknown;
    }
#endif
}
