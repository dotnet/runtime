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

NetworkInterfaceType MapHardwareType(uint16_t nativeType)
{
#if defined(AF_PACKET)
    switch (nativeType)
    {
        case ARPHRD_ETHER:
        case ARPHRD_EETHER:
            return Ethernet;
        case ARPHRD_PRONET:
            return TokenRing;
        case ARPHRD_ATM:
            return Atm;
        case ARPHRD_SLIP:
        case ARPHRD_CSLIP:
        case ARPHRD_SLIP6:
        case ARPHRD_CSLIP6:
            return Slip;
        case ARPHRD_PPP:
            return Ppp;
        case ARPHRD_TUNNEL:
        case ARPHRD_TUNNEL6:
            return Tunnel;
        case ARPHRD_LOOPBACK:
            return Loopback;
        case ARPHRD_FDDI:
            return Fddi;
        case ARPHRD_IEEE80211:
        case ARPHRD_IEEE80211_PRISM:
        case ARPHRD_IEEE80211_RADIOTAP:
            return Wireless80211;
        default:
            return Unknown;
    }
#elif defined(AF_LINK)
    switch (nativeType)
    {
        case IFT_ETHER:
            return Ethernet;
        case IFT_ISO88025:
            return TokenRing;
        case IFT_FDDI:
            return Fddi;
        case IFT_ISDNBASIC:
            return Isdn;
        case IFT_ISDNPRIMARY:
            return PrimaryIsdn;
        case IFT_PPP:
            return Ppp;
        case IFT_LOOP:
            return Loopback;
        case IFT_XETHER:
            return Ethernet3Megabit;
        case IFT_SLIP:
            return Slip;
        case IFT_ATM:
            return Atm;
        case IFT_MODEM:
            return GenericModem;
        case IFT_IEEE1394:
            return HighPerformanceSerialBus;
        default:
            return Unknown;
    }
#endif
}
