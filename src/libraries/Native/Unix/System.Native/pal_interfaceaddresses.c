// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
#if HAVE_SYS_IOCTL_H
#include <sys/ioctl.h>
#endif
#if HAVE_ETHTOOL_H
#include <linux/ethtool.h>
#include <linux/sockios.h>
#include <arpa/inet.h>
#endif
#if HAVE_NET_IFMEDIA_H
#include <net/if_media.h>
#endif

#if defined(AF_PACKET)
#include <sys/ioctl.h>
#if HAVE_NETPACKET_PACKET_H
#include <netpacket/packet.h>
#else
#include <linux/if_packet.h>
#endif
#elif defined(AF_LINK)
#include <net/if_dl.h>
#include <net/if_types.h>
#else
#error System must have AF_PACKET or AF_LINK.
#endif

#if HAVE_RT_MSGHDR
#if HAVE_IOS_NET_ROUTE_H
#include "ios/net/route.h"
#else
#include <net/route.h>
#endif
#endif

#if HAVE_GETIFADDRS
// Convert mask to prefix length e.g. 255.255.255.0 -> 24
// mask parameter is pointer to buffer where address starts and length is
// buffer length e.g. 4 for IPv4 and 16 for IPv6.
// Code bellow counts consecutive number of 1 bits.
static inline uint8_t mask2prefix(uint8_t* mask, int length)
{
    uint8_t len = 0;
    uint8_t* end = mask + length;

    if (mask == NULL)
    {
        // If we did not get valid mask, assume host address.
        return (uint8_t)length * 8;
    }

    // Get whole bytes
    while ((mask < end) && (*mask == 0xff))
    {
        len += 8;
        mask++;
    }

    // Get last incomplete byte
    if (mask < end)
    {
        while (*mask)
        {
            len++;
            *mask <<= 1;
        }
    }

    if (len == 0 && length == 4)
    {
        len = 32;
    }

    return len;
}
#endif

int32_t SystemNative_EnumerateInterfaceAddresses(void* context,
                                               IPv4AddressFound onIpv4Found,
                                               IPv6AddressFound onIpv6Found,
                                               LinkLayerAddressFound onLinkLayerFound)
{
#if HAVE_GETIFADDRS
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
                IpAddressInfo iai;
                memset(&iai, 0, sizeof(IpAddressInfo));
                iai.InterfaceIndex = interfaceIndex;
                iai.NumAddressBytes = NUM_BYTES_IN_IPV4_ADDRESS;

                struct sockaddr_in* sain = (struct sockaddr_in*)current->ifa_addr;
                memcpy_s(iai.AddressBytes, sizeof_member(IpAddressInfo, AddressBytes), &sain->sin_addr.s_addr, sizeof(sain->sin_addr.s_addr));

                struct sockaddr_in* mask_sain = (struct sockaddr_in*)current->ifa_netmask;
                // ifa_netmask can be NULL according to documentation, probably P2P interfaces.
                iai.PrefixLength = mask_sain != NULL ? mask2prefix((uint8_t*)&mask_sain->sin_addr.s_addr, NUM_BYTES_IN_IPV4_ADDRESS) : NUM_BYTES_IN_IPV4_ADDRESS * 8;

                onIpv4Found(context, actualName, &iai);
            }
        }
        else if (family == AF_INET6)
        {
            if (onIpv6Found != NULL)
            {
                IpAddressInfo iai;
                memset(&iai, 0, sizeof(IpAddressInfo));
                iai.InterfaceIndex = interfaceIndex;
                iai.NumAddressBytes = NUM_BYTES_IN_IPV6_ADDRESS;

                struct sockaddr_in6* sain6 = (struct sockaddr_in6*)current->ifa_addr;
                memcpy_s(iai.AddressBytes, sizeof_member(IpAddressInfo, AddressBytes), sain6->sin6_addr.s6_addr, sizeof(sain6->sin6_addr.s6_addr));
                uint32_t scopeId = sain6->sin6_scope_id;

                struct sockaddr_in6* mask_sain6 = (struct sockaddr_in6*)current->ifa_netmask;
                iai.PrefixLength = mask_sain6 != NULL ? mask2prefix((uint8_t*)&mask_sain6->sin6_addr.s6_addr, NUM_BYTES_IN_IPV6_ADDRESS) : NUM_BYTES_IN_IPV6_ADDRESS * 8;
                onIpv6Found(context, actualName, &iai, &scopeId);
            }
        }
#if defined(AF_PACKET)
        else if (family == AF_PACKET)
        {
            if (onLinkLayerFound != NULL)
            {
                struct sockaddr_ll* sall = (struct sockaddr_ll*)current->ifa_addr;

                if (sall->sll_halen > sizeof(sall->sll_addr))
                {
                    // sockaddr_ll->sll_addr has a maximum capacity of 8 bytes (unsigned char sll_addr[8])
                    // so if we get a address length greater than that, we truncate it to 8 bytes.
                    // This is following the kernel docs where they always treat physical addresses with a maximum of 8 bytes.
                    // However in WSL we hit an issue where sll_halen was 16 bytes so the memcpy_s below would fail because it was greater.
                    sall->sll_halen = sizeof(sall->sll_addr);
                }

                LinkLayerAddressInfo lla;
                memset(&lla, 0, sizeof(LinkLayerAddressInfo));
                lla.InterfaceIndex = interfaceIndex;
                lla.NumAddressBytes = sall->sll_halen;
                lla.HardwareType = MapHardwareType(sall->sll_hatype);

                memcpy_s(&lla.AddressBytes, sizeof_member(LinkLayerAddressInfo, AddressBytes), &sall->sll_addr, sall->sll_halen);
                onLinkLayerFound(context, current->ifa_name, &lla);
            }
        }
#elif defined(AF_LINK)
        else if (family == AF_LINK)
        {
            if (onLinkLayerFound != NULL)
            {
                struct sockaddr_dl* sadl = (struct sockaddr_dl*)current->ifa_addr;

                LinkLayerAddressInfo lla;
                memset(&lla, 0, sizeof(LinkLayerAddressInfo));
                lla.InterfaceIndex = interfaceIndex;
                lla.NumAddressBytes = sadl->sdl_alen;
                lla.HardwareType = MapHardwareType(sadl->sdl_type);

#if HAVE_NET_IFMEDIA_H
                if (lla.HardwareType == NetworkInterfaceType_Ethernet)
                {
                    // WI-FI and Ethernet have same address type so we can try to distinguish more
                    int fd = socket(AF_INET, SOCK_DGRAM, 0);
                    if (fd >= 0)
                    {
                        struct ifmediareq ifmr;
                        memset(&ifmr, 0, sizeof(ifmr));
                        strncpy(ifmr.ifm_name, actualName, sizeof(ifmr.ifm_name));

                        if ((ioctl(fd, SIOCGIFMEDIA, (caddr_t)&ifmr) == 0) && (IFM_TYPE(ifmr.ifm_current) == IFM_IEEE80211))
                        {
                            lla.HardwareType = NetworkInterfaceType_Wireless80211;
                        }

                        close(fd);
                    }
                }
#endif
                memcpy_s(&lla.AddressBytes, sizeof_member(LinkLayerAddressInfo, AddressBytes), (uint8_t*)LLADDR(sadl), sadl->sdl_alen);
                onLinkLayerFound(context, current->ifa_name, &lla);
            }
        }
#endif
    }

    freeifaddrs(headAddr);
    return 0;
#else
    // Not supported on e.g. Android. Also, prevent a compiler error because parameters are unused
    (void)context;
    (void)onIpv4Found;
    (void)onIpv6Found;
    (void)onLinkLayerFound;
    errno = ENOTSUP;
    return -1;
#endif
}

int32_t SystemNative_GetNetworkInterfaces(int32_t * interfaceCount, NetworkInterfaceInfo **interfaceList, int32_t * addressCount, IpAddressInfo **addressList )
{
#if HAVE_GETIFADDRS
    struct ifaddrs* head;   // Pointer to block allocated by getifaddrs().
    struct ifaddrs* ifaddrsEntry;
    IpAddressInfo *ai;
    int count = 0;       // Count of entries returned by getifaddrs().
    int ip4count = 0;    // Total number of IPv4 addresses.
    int ip6count = 0;    // Total number of IPv6 addresses.
    int ifcount = 0;     // Total number of unique network interface.
    int index;
    int socketfd = -1;

    NetworkInterfaceInfo *nii;

    if (getifaddrs(&head) == -1)
    {
        assert(errno != 0);
        return -1;
    }

    ifaddrsEntry = head;
    while (ifaddrsEntry != NULL)
    {
        count ++;
        if (ifaddrsEntry->ifa_addr != NULL && ifaddrsEntry->ifa_addr->sa_family == AF_INET)
        {
            ip4count++;
        }
        else if (ifaddrsEntry->ifa_addr != NULL && ifaddrsEntry->ifa_addr->sa_family == AF_INET6)
        {
            ip6count++;
        }

        ifaddrsEntry = ifaddrsEntry->ifa_next;
    }

    // Allocate estimated space. It can be little bit more than we need.
    // To save allocation need for separate free() we will allocate one memory chunk
    // where we first write out NetworkInterfaceInfo entries immediately followed by
    // IpAddressInfo list.
    void * memoryBlock = calloc((size_t)count, sizeof(NetworkInterfaceInfo));
    if (memoryBlock == NULL)
    {
        errno = ENOMEM;
        return -1;
    }

    // Reset head pointers again.
    ifaddrsEntry = head;
    *interfaceList = nii = (NetworkInterfaceInfo*)memoryBlock;
    // address of first IpAddressInfo after all NetworkInterfaceInfo entries.
    *addressList = ai = (IpAddressInfo*)(nii + (count - ip4count - ip6count));

    while (ifaddrsEntry != NULL)
    {
        //current = NULL;
        nii = NULL;
        uint ifindex = if_nametoindex(ifaddrsEntry->ifa_name);
        for (index = 0; index < (int)ifcount; index ++)
        {
            if (((NetworkInterfaceInfo*)memoryBlock)[index].InterfaceIndex == ifindex)
            {
                nii = &((NetworkInterfaceInfo*)memoryBlock)[index];
                break;
            }
        }

        if (nii == NULL)
        {
            // We git new interface.
            nii = &((NetworkInterfaceInfo*)memoryBlock)[ifcount++];

            memcpy(nii->Name, ifaddrsEntry->ifa_name, sizeof(nii->Name));
            nii->InterfaceIndex = if_nametoindex(ifaddrsEntry->ifa_name);
            nii->Speed = -1;
            nii->HardwareType = NetworkInterfaceType_Unknown;

            // Get operational state and multicast support.
            if ((ifaddrsEntry->ifa_flags & (IFF_MULTICAST|IFF_ALLMULTI)) != 0)
            {
                nii->SupportsMulticast = 1;
            }

            // OperationalState returns whether the interface can transmit data packets.
            // The administrator must have enabled the interface (IFF_UP), and the cable must be plugged in (IFF_RUNNING).
            nii->OperationalState = ((ifaddrsEntry->ifa_flags & (IFF_UP|IFF_RUNNING)) == (IFF_UP|IFF_RUNNING)) ? OperationalStatus_Up : OperationalStatus_Down;
        }

        if (ifaddrsEntry->ifa_addr == NULL)
        {
            // Interface with no addresses. Not even link layer. (like PPP or tunnel)
            ifaddrsEntry = ifaddrsEntry->ifa_next;
            continue;
        }
        else if (ifaddrsEntry->ifa_addr->sa_family == AF_INET)
        {
            ai->InterfaceIndex = ifindex;
            ai->NumAddressBytes = NUM_BYTES_IN_IPV4_ADDRESS;
            memcpy(ai->AddressBytes, &((struct sockaddr_in*)ifaddrsEntry->ifa_addr)->sin_addr, NUM_BYTES_IN_IPV4_ADDRESS);
            ai->PrefixLength = mask2prefix((uint8_t*)&((struct sockaddr_in*)ifaddrsEntry->ifa_netmask)->sin_addr, NUM_BYTES_IN_IPV4_ADDRESS);
            ai++;
        }
        else if (ifaddrsEntry->ifa_addr->sa_family == AF_INET6)
        {
            ai->InterfaceIndex = ifindex;
            ai->NumAddressBytes = NUM_BYTES_IN_IPV6_ADDRESS;
            memcpy(ai->AddressBytes, &((struct sockaddr_in6*)ifaddrsEntry->ifa_addr)->sin6_addr, NUM_BYTES_IN_IPV6_ADDRESS);
            ai->PrefixLength = mask2prefix((uint8_t*)&(((struct sockaddr_in6*)ifaddrsEntry->ifa_netmask)->sin6_addr), NUM_BYTES_IN_IPV6_ADDRESS);
            ai++;
        }
#if defined(AF_LINK)
        else if (ifaddrsEntry->ifa_addr->sa_family == AF_LINK)
        {
            struct sockaddr_dl* sadl = (struct sockaddr_dl*)ifaddrsEntry->ifa_addr;

            nii->HardwareType = MapHardwareType(sadl->sdl_type);
            nii->NumAddressBytes =  sadl->sdl_alen;
            memcpy_s(&nii->AddressBytes, sizeof_member(NetworkInterfaceInfo, AddressBytes), (uint8_t*)LLADDR(sadl), sadl->sdl_alen);
        }
#endif
#if defined(AF_PACKET)
        else if (ifaddrsEntry->ifa_addr->sa_family == AF_PACKET)
        {
            struct sockaddr_ll* sall = (struct sockaddr_ll*)ifaddrsEntry->ifa_addr;

            if (sall->sll_halen > sizeof(sall->sll_addr))
            {
                // sockaddr_ll->sll_addr has a maximum capacity of 8 bytes (unsigned char sll_addr[8])
                // so if we get a address length greater than that, we truncate it to 8 bytes.
                // This is following the kernel docs where they always treat physical addresses with a maximum of 8 bytes.
                // However in WSL we hit an issue where sll_halen was 16 bytes so the memcpy_s below would fail because it was greater.
                sall->sll_halen = sizeof(sall->sll_addr);
            }

            nii->HardwareType = MapHardwareType(sall->sll_hatype);
            nii->NumAddressBytes = sall->sll_halen;
            memcpy_s(&nii->AddressBytes, sizeof_member(NetworkInterfaceInfo, AddressBytes), &sall->sll_addr, sall->sll_halen);

            struct ifreq ifr;
            strncpy(ifr.ifr_name, nii->Name, sizeof(ifr.ifr_name)-1);

            if (socketfd == -1)
            {
                socketfd = socket(AF_INET, SOCK_DGRAM, IPPROTO_IP);
            }

            if (socketfd > -1)
            {
                if (ioctl(socketfd, SIOCGIFMTU, &ifr) == 0)
                {
                    nii->Mtu = ifr.ifr_mtu;
                }

#if HAVE_ETHTOOL_H
                // Do not even try to get speed on certain interface types.
                if (nii->HardwareType != NetworkInterfaceType_Unknown && nii->HardwareType != NetworkInterfaceType_Tunnel &&
                    nii->HardwareType != NetworkInterfaceType_Loopback)
                {
                    struct ethtool_cmd ecmd;

                    ecmd.cmd = ETHTOOL_GLINK;
                    ifr.ifr_data = (char *) &ecmd;
                    if (ioctl(socketfd, SIOCETHTOOL, &ifr) == 0)
                    {
                        if (!ecmd.supported)
                        {
                            // Mark Interface as down if we succeeded getting link status and it is down.
                            nii->OperationalState = OperationalStatus_Down;
                        }

                        // Try to get link speed if link is up.
                        // Use older ETHTOOL_GSET instead of ETHTOOL_GLINKSETTINGS to support RH6
                        ecmd.cmd = ETHTOOL_GSET;
                        if (ioctl(socketfd, SIOCETHTOOL, &ifr) == 0)
                        {
#ifdef TARGET_ANDROID
                            nii->Speed = (int64_t)ecmd.speed;
#else
                            nii->Speed = (int64_t)ethtool_cmd_speed(&ecmd);
#endif
                            if (nii->Speed > 0)
                            {
                                // If we did not get -1
                                nii->Speed *= 1000000; // convert from mbits
                            }
                        }
                    }
                }
#endif
            }
        }
#endif
        ifaddrsEntry = ifaddrsEntry->ifa_next;
    }

    *interfaceCount = ifcount;
    *addressCount = ip4count + ip6count;

    // Cleanup.
    freeifaddrs(head);
    if (socketfd != -1)
    {
        close(socketfd);
    }

    return 0;
#else
    // Not supported on e.g. Android. Also, prevent a compiler error because parameters are unused
    (void)interfaceCount;
    (void)interfaceList;
    (void)addressCount;
    (void)addressList;
    errno = ENOTSUP;
    return -1;
#endif
}

#if HAVE_RT_MSGHDR && defined(CTL_NET)
int32_t SystemNative_EnumerateGatewayAddressesForInterface(void* context, uint32_t interfaceIndex, GatewayAddressFound onGatewayFound)
{
    static struct in6_addr anyaddr = IN6ADDR_ANY_INIT;
    int routeDumpName[] = {CTL_NET, AF_ROUTE, 0, 0, NET_RT_DUMP, 0};

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
        if (errno != ENOMEM)
        {
            return -1;
        }

        // If buffer is not big enough double size to avoid calling sysctl again
        // as byteCount only gets estimated size when passed buffer is NULL.
        // This only happens if routing table grows between first and second call.
        size_t tmpEstimatedSize;
        if (!multiply_s(byteCount, (size_t)2, &tmpEstimatedSize))
        {
            errno = ENOMEM;
            return -1;
        }

        byteCount = tmpEstimatedSize;
        free(buffer);
        buffer = malloc(byteCount);
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

        if (isGateway && gatewayPresent && ((int)interfaceIndex == -1 || interfaceIndex == hdr->rtm_index))
        {
            IpAddressInfo iai;
            struct sockaddr_storage* sock = (struct sockaddr_storage*)(hdr + 1);
            memset(&iai, 0, sizeof(IpAddressInfo));
            iai.InterfaceIndex = hdr->rtm_index;

            if (sock->ss_family == AF_INET)
            {
                iai.NumAddressBytes = NUM_BYTES_IN_IPV4_ADDRESS;
                struct sockaddr_in* sain = (struct sockaddr_in*)sock;
                if (sain->sin_addr.s_addr != 0)
                {
                    // filter out normal routes.
                    continue;
                }

                sain = sain + 1; // Skip over the first sockaddr, the destination address. The second is the gateway.
                memcpy_s(iai.AddressBytes, sizeof_member(IpAddressInfo, AddressBytes), &sain->sin_addr.s_addr, sizeof(sain->sin_addr.s_addr));
            }
            else if (sock->ss_family == AF_INET6)
            {
                struct sockaddr_in6* sain6 = (struct sockaddr_in6*)sock;
                iai.NumAddressBytes = NUM_BYTES_IN_IPV6_ADDRESS;
                if (memcmp(&anyaddr, &sain6->sin6_addr, sizeof(sain6->sin6_addr)) != 0)
                {
                    // filter out normal routes.
                    continue;
                }

                sain6 = sain6 + 1; // Skip over the first sockaddr, the destination address. The second is the gateway.
                if ((sain6->sin6_addr.__u6_addr.__u6_addr16[0] & htons(0xfe80)) == htons(0xfe80))
                {
                    // clear embedded if index.
                    sain6->sin6_addr.__u6_addr.__u6_addr16[1] = 0;
                }

                memcpy_s(iai.AddressBytes, sizeof_member(IpAddressInfo, AddressBytes), &sain6->sin6_addr, sizeof(sain6->sin6_addr));
            }
            else
            {
                // Ignore other address families.
                continue;
            }
            onGatewayFound(context, &iai);
        }
    }

    free(buffer);
    return 0;
}
#else
int32_t SystemNative_EnumerateGatewayAddressesForInterface(void* context, uint32_t interfaceIndex, GatewayAddressFound onGatewayFound)
{
    (void)context;
    (void)interfaceIndex;
    (void)onGatewayFound;
    errno = ENOTSUP;
    return -1;
}
#endif // HAVE_RT_MSGHDR
