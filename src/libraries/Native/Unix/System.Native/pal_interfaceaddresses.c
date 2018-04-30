// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_config.h"
#include "pal_interfaceaddresses.h"
#include "pal_maphardwaretype.h"
#include "pal_utilities.h"
#include "pal_safecrt.h"
#include "pal_networking.h"

#include <stdlib.h>
#include <sys/types.h>
#include <assert.h>
#include <ifaddrs.h>
#include <net/if.h>
#include <netinet/in.h>
#include <string.h>
#include <sys/socket.h>
#if HAVE_SYS_SYSCTL_H
#include <sys/sysctl.h>
#endif

#if defined(AF_PACKET)
#include <linux/if_packet.h>
#elif defined(AF_LINK)
#include <net/if_dl.h>
#include <net/if_types.h>
#else
#error System must have AF_PACKET or AF_LINK.
#endif

#if HAVE_RT_MSGHDR
#include <net/route.h>
#endif

int32_t SystemNative_EnumerateInterfaceAddresses(IPv4AddressFound onIpv4Found,
                                               IPv6AddressFound onIpv6Found,
                                               LinkLayerAddressFound onLinkLayerFound)
{
    struct ifaddrs* headAddr;
    if (getifaddrs(&headAddr) == -1)
    {
        return -1;
    }

    for (struct ifaddrs* current = headAddr; current != NULL; current = current->ifa_next)
    {
        if (current->ifa_addr == NULL)
        {
            continue;
        }
        uint32_t interfaceIndex = if_nametoindex(current->ifa_name);
        // ifa_name may be an aliased interface name.
        // Use if_indextoname to map back to the true device name.
        char actualName[IF_NAMESIZE];
        char* result = if_indextoname(interfaceIndex, actualName);
        if (result == NULL)
        {
            freeifaddrs(headAddr);
            return -1;
        }
        
        assert(result == actualName);
        int family = current->ifa_addr->sa_family;
        if (family == AF_INET)
        {
            if (onIpv4Found != NULL)
            {
                // IP Address
                struct IpAddressInfo iai;
                memset(&iai, 0, sizeof(struct IpAddressInfo));
                iai.InterfaceIndex = interfaceIndex;
                iai.NumAddressBytes = NUM_BYTES_IN_IPV4_ADDRESS;

                struct sockaddr_in* sain = (struct sockaddr_in*)current->ifa_addr;
                memcpy_s(iai.AddressBytes, sizeof_member(struct IpAddressInfo, AddressBytes), &sain->sin_addr.s_addr, sizeof(sain->sin_addr.s_addr));

                // Net Mask
                struct IpAddressInfo maskInfo;
                memset(&maskInfo, 0, sizeof(struct IpAddressInfo));
                maskInfo.InterfaceIndex = interfaceIndex;
                maskInfo.NumAddressBytes = NUM_BYTES_IN_IPV4_ADDRESS;

                struct sockaddr_in* mask_sain = (struct sockaddr_in*)current->ifa_netmask;
                memcpy_s(maskInfo.AddressBytes, sizeof_member(struct IpAddressInfo, AddressBytes), &mask_sain->sin_addr.s_addr, sizeof(mask_sain->sin_addr.s_addr));

                onIpv4Found(actualName, &iai, &maskInfo);
            }
        }
        else if (family == AF_INET6)
        {
            if (onIpv6Found != NULL)
            {
                struct IpAddressInfo iai;
                memset(&iai, 0, sizeof(struct IpAddressInfo));
                iai.InterfaceIndex = interfaceIndex;
                iai.NumAddressBytes = NUM_BYTES_IN_IPV6_ADDRESS;

                struct sockaddr_in6* sain6 = (struct sockaddr_in6*)current->ifa_addr;
                memcpy_s(iai.AddressBytes, sizeof_member(struct IpAddressInfo, AddressBytes), sain6->sin6_addr.s6_addr, sizeof(sain6->sin6_addr.s6_addr));
                uint32_t scopeId = sain6->sin6_scope_id;
                onIpv6Found(actualName, &iai, &scopeId);
            }
        }

#if defined(AF_PACKET)
        else if (family == AF_PACKET)
        {
            if (onLinkLayerFound != NULL)
            {
                struct sockaddr_ll* sall = (struct sockaddr_ll*)current->ifa_addr;

                struct LinkLayerAddressInfo lla;
                memset(&lla, 0, sizeof(struct LinkLayerAddressInfo));
                lla.InterfaceIndex = interfaceIndex;
                lla.NumAddressBytes = sall->sll_halen;
                lla.HardwareType = MapHardwareType(sall->sll_hatype);

                memcpy_s(&lla.AddressBytes, sizeof_member(struct LinkLayerAddressInfo, AddressBytes), &sall->sll_addr, sall->sll_halen);
                onLinkLayerFound(current->ifa_name, &lla);
            }
        }
#elif defined(AF_LINK)
        else if (family == AF_LINK)
        {
            if (onLinkLayerFound != NULL)
            {
                struct sockaddr_dl* sadl = (struct sockaddr_dl*)current->ifa_addr;

                struct LinkLayerAddressInfo lla;
                memset(&lla, 0, sizeof(struct LinkLayerAddressInfo));
                lla.InterfaceIndex = interfaceIndex;
                lla.NumAddressBytes = sadl->sdl_alen;
                lla.HardwareType = MapHardwareType(sadl->sdl_type);

                memcpy_s(&lla.AddressBytes, sizeof_member(struct LinkLayerAddressInfo, AddressBytes), (uint8_t*)LLADDR(sadl), sadl->sdl_alen);
                onLinkLayerFound(current->ifa_name, &lla);
            }
        }
#endif
    }

    freeifaddrs(headAddr);
    return 0;
}

#if HAVE_RT_MSGHDR
int32_t SystemNative_EnumerateGatewayAddressesForInterface(uint32_t interfaceIndex, GatewayAddressFound onGatewayFound)
{
    int routeDumpName[] = {CTL_NET, AF_ROUTE, 0, AF_INET, NET_RT_DUMP, 0};

    size_t byteCount;

    if (sysctl(routeDumpName, 6, NULL, &byteCount, NULL, 0) != 0)
    {
        return -1;
    }

    uint8_t* buffer = malloc(byteCount);
    if (buffer == NULL)
    {
        errno = ENOMEM;
        return -1;
    }

    while (sysctl(routeDumpName, 6, buffer, &byteCount, NULL, 0) != 0)
    {
        buffer = realloc(buffer, byteCount);
        if (buffer == NULL)
        {
            errno = ENOMEM;
            return -1;
        }
    }

    struct rt_msghdr* hdr;
    for (size_t i = 0; i < byteCount; i += hdr->rtm_msglen)
    {
        hdr = (struct rt_msghdr*)&buffer[i];
        int flags = hdr->rtm_flags;
        int isGateway = flags & RTF_GATEWAY;
        int gatewayPresent = hdr->rtm_addrs & RTA_GATEWAY;

        if (isGateway && gatewayPresent)
        {
            struct IpAddressInfo iai;
            memset(&iai, 0, sizeof(struct IpAddressInfo));
            iai.InterfaceIndex = interfaceIndex;
            iai.NumAddressBytes = NUM_BYTES_IN_IPV4_ADDRESS;
            struct sockaddr_in* sain = (struct sockaddr_in*)(hdr + 1);
            sain = sain + 1; // Skip over the first sockaddr, the destination address. The second is the gateway.
            memcpy_s(iai.AddressBytes, sizeof_member(struct IpAddressInfo, AddressBytes), &sain->sin_addr.s_addr, sizeof(sain->sin_addr.s_addr));
            onGatewayFound(&iai);
        }
    }

    free(buffer);
    return 0;
}
#endif // HAVE_RT_MSGHDR
