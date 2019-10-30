// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_config.h"

// These functions are only used for platforms which support
// using sysctl to gather protocol statistics information.
// Currently, this is all keyed off of whether the include tcp_var.h
// exists, but we may want to make this more granular for differnet platforms.

#if HAVE_TCP_VAR_H

#include "pal_utilities.h"
#include "pal_networkstatistics.h"
#include "pal_errno.h"
#include "pal_tcpstate.h"
#include "pal_safecrt.h"

#include <stdlib.h>
#include <errno.h>
#include <sys/socket.h>
#include <net/route.h>
#include <net/if.h>

#include <sys/types.h>
#if HAVE_SYS_SYSCTL_H
#include <sys/sysctl.h>
#endif
#if HAVE_NET_IFMEDIA_H
#include <net/if_media.h>
#include <sys/ioctl.h>
#endif
#include <sys/socketvar.h>
#include <netinet/in.h>
#include <netinet/in_pcb.h>
#include <netinet/ip.h>
#include <netinet/ip_icmp.h>
#include <netinet/ip_var.h>
#include <netinet/tcp.h>
#include <netinet/tcp_fsm.h>
#include <netinet/tcp_var.h>
#include <netinet/udp.h>
#include <netinet/udp_var.h>
#include <netinet/icmp6.h>
#include <netinet/icmp_var.h>

int32_t SystemNative_GetTcpGlobalStatistics(TcpGlobalStatistics* retStats)
{
    size_t oldlenp;

    assert(retStats != NULL);

    struct tcpstat systemStats;
    oldlenp = sizeof(systemStats);
    if (sysctlbyname("net.inet.tcp.stats", &systemStats, &oldlenp, NULL, 0))
    {
        memset(retStats, 0, sizeof(TcpGlobalStatistics)); // out parameter must be initialized.
        return -1;
    }

    retStats->ConnectionsAccepted = systemStats.tcps_accepts;
    retStats->ConnectionsInitiated = systemStats.tcps_connattempt;
    retStats->CumulativeConnections = systemStats.tcps_connects;
    retStats->ErrorsReceived = systemStats.tcps_rcvbadsum + systemStats.tcps_rcvbadoff;
    retStats->FailedConnectionAttempts = systemStats.tcps_connattempt - systemStats.tcps_accepts;
    retStats->SegmentsReceived = systemStats.tcps_rcvtotal;
    retStats->SegmentsResent = systemStats.tcps_sndrexmitpack;
    retStats->SegmentsSent = systemStats.tcps_sndtotal;

    oldlenp = sizeof(retStats->CurrentConnections);
    if (sysctlbyname("net.inet.tcp.pcbcount", &retStats->CurrentConnections, &oldlenp, NULL, 0))
    {
        retStats->CurrentConnections = 0;
        return -1;
    }

    return 0;
}

int32_t SystemNative_GetIPv4GlobalStatistics(IPv4GlobalStatistics* retStats)
{
    size_t oldlenp;

    assert(retStats != NULL);

    struct ipstat systemStats;
    oldlenp = sizeof(systemStats);
    if (sysctlbyname("net.inet.ip.stats", &systemStats, &oldlenp, NULL, 0))
    {
        memset(retStats, 0, sizeof(IPv4GlobalStatistics)); // out parameter must be initialized.
        return -1;
    }

    retStats->OutboundPackets = systemStats.ips_localout;
    retStats->OutputPacketsNoRoute = systemStats.ips_noroute;
    retStats->CantFrags = systemStats.ips_cantfrag;
    retStats->DatagramsFragmented = systemStats.ips_fragmented;
    retStats->PacketsReassembled = systemStats.ips_reassembled;
    retStats->TotalPacketsReceived = systemStats.ips_total;
    retStats->PacketsDelivered = systemStats.ips_delivered;
    retStats->PacketsDiscarded = systemStats.ips_total - systemStats.ips_delivered;
    retStats->PacketsForwarded = systemStats.ips_forward;
    retStats->BadAddress = systemStats.ips_badaddr;
    retStats->BadHeader = systemStats.ips_badhlen; // Also include badaddr?
    retStats->UnknownProtos = systemStats.ips_noproto;

    oldlenp = sizeof(retStats->DefaultTtl);
    if (sysctlbyname("net.inet.ip.ttl", &retStats->DefaultTtl, &oldlenp, NULL, 0))
    {
        retStats->DefaultTtl = 0;
        retStats->Forwarding = 0;
        return -1;
    }
    oldlenp = sizeof(retStats->Forwarding);
    if (sysctlbyname("net.inet.ip.forwarding", &retStats->Forwarding, &oldlenp, NULL, 0))
    {
        retStats->Forwarding = 0;
        return -1;
    }

    return 0;
}

int32_t SystemNative_GetUdpGlobalStatistics(UdpGlobalStatistics* retStats)
{
    size_t oldlenp;

    assert(retStats != NULL);

    struct udpstat systemStats;
    oldlenp = sizeof(systemStats);
    if (sysctlbyname("net.inet.udp.stats", &systemStats, &oldlenp, NULL, 0))
    {
        memset(retStats, 0, sizeof(UdpGlobalStatistics)); // out parameter must be initialized.
        return -1;
    }

    retStats->DatagramsReceived = systemStats.udps_ipackets;
    retStats->DatagramsSent = systemStats.udps_opackets;
    retStats->IncomingDiscarded = systemStats.udps_noport;
    retStats->IncomingErrors = systemStats.udps_hdrops + systemStats.udps_badsum + systemStats.udps_badlen;

#if defined(__FreeBSD__)
    // FreeBSD does not have net.inet.udp.pcbcount
    retStats->UdpListeners = 0;
#else
    // This may contain both UDP4 and UDP6 listeners.
    oldlenp = sizeof(retStats->UdpListeners);
    if (sysctlbyname("net.inet.udp.pcbcount", &retStats->UdpListeners, &oldlenp, NULL, 0))
    {
        retStats->UdpListeners = 0;
        return -1;
    }
#endif

    return 0;
}

int32_t SystemNative_GetIcmpv4GlobalStatistics(Icmpv4GlobalStatistics* retStats)
{
    size_t oldlenp;

    assert(retStats != NULL);

    struct icmpstat systemStats;
    oldlenp = sizeof(systemStats);
    if (sysctlbyname("net.inet.icmp.stats", &systemStats, &oldlenp, NULL, 0))
    {
        memset(retStats, 0, sizeof(Icmpv4GlobalStatistics));
        return -1;
    }

    __typeof(systemStats.icps_inhist[0])* inHist = systemStats.icps_inhist;
    __typeof(systemStats.icps_outhist[0])* outHist = systemStats.icps_outhist;

    retStats->AddressMaskRepliesReceived = inHist[ICMP_MASKREPLY];
    retStats->AddressMaskRepliesSent = outHist[ICMP_MASKREPLY];
    retStats->AddressMaskRequestsReceived = inHist[ICMP_MASKREQ];
    retStats->AddressMaskRequestsSent = outHist[ICMP_MASKREQ];
    retStats->DestinationUnreachableMessagesReceived = inHist[ICMP_UNREACH];
    retStats->DestinationUnreachableMessagesSent = outHist[ICMP_UNREACH];
    retStats->EchoRepliesReceived = inHist[ICMP_ECHOREPLY];
    retStats->EchoRepliesSent = outHist[ICMP_ECHOREPLY];
    retStats->EchoRequestsReceived = inHist[ICMP_ECHO];
    retStats->EchoRequestsSent = outHist[ICMP_ECHO];
    retStats->ParameterProblemsReceived = inHist[ICMP_PARAMPROB];
    retStats->ParameterProblemsSent = outHist[ICMP_PARAMPROB];
    retStats->RedirectsReceived = inHist[ICMP_REDIRECT];
    retStats->RedirectsSent = outHist[ICMP_REDIRECT];
    retStats->SourceQuenchesReceived = inHist[ICMP_SOURCEQUENCH];
    retStats->SourceQuenchesSent = outHist[ICMP_SOURCEQUENCH];
    retStats->TimeExceededMessagesReceived = inHist[ICMP_TIMXCEED];
    retStats->TimeExceededMessagesSent = outHist[ICMP_TIMXCEED];
    retStats->TimestampRepliesReceived = inHist[ICMP_TSTAMPREPLY];
    retStats->TimestampRepliesSent = outHist[ICMP_TSTAMPREPLY];
    retStats->TimestampRequestsReceived = inHist[ICMP_TSTAMP];
    retStats->TimestampRequestsSent = outHist[ICMP_TSTAMP];

    return 0;
}

int32_t SystemNative_GetIcmpv6GlobalStatistics(Icmpv6GlobalStatistics* retStats)
{
    size_t oldlenp;

    assert(retStats != NULL);

    struct icmp6stat systemStats;
    oldlenp = sizeof(systemStats);
    if (sysctlbyname("net.inet6.icmp6.stats", &systemStats, &oldlenp, NULL, 0))
    {
        memset(retStats, 0, sizeof(Icmpv6GlobalStatistics));
        return -1;
    }

    uint64_t* inHist = systemStats.icp6s_inhist;
    uint64_t* outHist = systemStats.icp6s_outhist;

    retStats->DestinationUnreachableMessagesReceived = inHist[ICMP6_DST_UNREACH];
    retStats->DestinationUnreachableMessagesSent = outHist[ICMP6_DST_UNREACH];
    retStats->EchoRepliesReceived = inHist[ICMP6_ECHO_REPLY];
    retStats->EchoRepliesSent = outHist[ICMP6_ECHO_REPLY];
    retStats->EchoRequestsReceived = inHist[ICMP6_ECHO_REQUEST];
    retStats->EchoRequestsSent = outHist[ICMP6_ECHO_REQUEST];
    retStats->MembershipQueriesReceived = inHist[ICMP6_MEMBERSHIP_QUERY];
    retStats->MembershipQueriesSent = outHist[ICMP6_MEMBERSHIP_QUERY];
    retStats->MembershipReductionsReceived = inHist[ICMP6_MEMBERSHIP_REDUCTION];
    retStats->MembershipReductionsSent = outHist[ICMP6_MEMBERSHIP_REDUCTION];
    retStats->MembershipReportsReceived = inHist[ICMP6_MEMBERSHIP_REPORT];
    retStats->MembershipReportsSent = outHist[ICMP6_MEMBERSHIP_REPORT];
    retStats->NeighborAdvertisementsReceived = inHist[ND_NEIGHBOR_ADVERT];
    retStats->NeighborAdvertisementsSent = outHist[ND_NEIGHBOR_ADVERT];
    retStats->NeighborSolicitsReceived = inHist[ND_NEIGHBOR_SOLICIT];
    retStats->NeighborSolicitsSent = outHist[ND_NEIGHBOR_SOLICIT];
    retStats->PacketTooBigMessagesReceived = inHist[ICMP6_PACKET_TOO_BIG];
    retStats->PacketTooBigMessagesSent = outHist[ICMP6_PACKET_TOO_BIG];
    retStats->ParameterProblemsReceived = inHist[ICMP6_PARAM_PROB];
    retStats->ParameterProblemsSent = outHist[ICMP6_PARAM_PROB];
    retStats->RedirectsReceived = inHist[ND_REDIRECT];
    retStats->RedirectsSent = outHist[ND_REDIRECT];
    retStats->RouterAdvertisementsReceived = inHist[ND_ROUTER_ADVERT];
    retStats->RouterAdvertisementsSent = outHist[ND_ROUTER_ADVERT];
    retStats->RouterSolicitsReceived = inHist[ND_ROUTER_SOLICIT];
    retStats->RouterSolicitsSent = outHist[ND_ROUTER_SOLICIT];
    retStats->TimeExceededMessagesReceived = inHist[ICMP6_TIME_EXCEEDED];
    retStats->TimeExceededMessagesSent = outHist[ICMP6_TIME_EXCEEDED];

    return 0;
}

int32_t SystemNative_GetEstimatedTcpConnectionCount()
{
    int32_t count;
    size_t oldlenp = sizeof(count);
    sysctlbyname("net.inet.tcp.pcbcount", &count, &oldlenp, NULL, 0);
    return count;
}

static size_t GetEstimatedTcpPcbSize()
{
    void* oldp = NULL;
    void* newp = NULL;
    size_t oldlenp, newlen = 0;

    sysctlbyname("net.inet.tcp.pcblist", oldp, &oldlenp, newp, newlen);
    return oldlenp;
}

int32_t SystemNative_GetActiveTcpConnectionInfos(NativeTcpConnectionInformation* infos, int32_t* infoCount)
{
    assert(infos != NULL);
    assert(infoCount != NULL);

    size_t estimatedSize = GetEstimatedTcpPcbSize();
    uint8_t* buffer = (uint8_t*)malloc(estimatedSize * sizeof(uint8_t));
    if (buffer == NULL)
    {
        errno = ENOMEM;
        return -1;
    }

    void* newp = NULL;
    size_t newlen = 0;

    while (sysctlbyname("net.inet.tcp.pcblist", buffer, &estimatedSize, newp, newlen) != 0)
    {
        free(buffer);
        size_t tmpEstimatedSize;
        if (!multiply_s(estimatedSize, (size_t)2, &tmpEstimatedSize) ||
            (buffer = (uint8_t*)malloc(estimatedSize * sizeof(uint8_t))) == NULL)
        {
            errno = ENOMEM;
            return -1;
        }
        estimatedSize = tmpEstimatedSize;
    }

    int32_t count = (int32_t)(estimatedSize / sizeof(struct xtcpcb));
    if (count > *infoCount)
    {
        // Not enough space in caller-supplied buffer.
        free(buffer);
        *infoCount = count;
        return -1;
    }
    *infoCount = count;

    //  sizeof(struct xtcpcb) == 524
    struct tcpcb tcp_pcb;
    struct inpcb in_pcb;
    struct xinpgen* xHeadPtr;
    int32_t connectionIndex = -1;
    xHeadPtr = (struct xinpgen*)buffer;
    for (xHeadPtr = (struct xinpgen*)((uint8_t*)xHeadPtr + xHeadPtr->xig_len);
         xHeadPtr->xig_len >= sizeof(struct xtcpcb);
         xHeadPtr = (struct xinpgen*)((uint8_t*)xHeadPtr + xHeadPtr->xig_len))
    {
        connectionIndex++;
        struct xtcpcb* head_xtcpb = (struct xtcpcb*)xHeadPtr;

        tcp_pcb = head_xtcpb->xt_tp;
        in_pcb = head_xtcpb->xt_inp;

        NativeTcpConnectionInformation* ntci = &infos[connectionIndex];
        ntci->State = SystemNative_MapTcpState(tcp_pcb.t_state);

        uint8_t vflag = in_pcb.inp_vflag; // INP_IPV4 or INP_IPV6
        if ((vflag & INP_IPV4) == INP_IPV4)
        {
            memcpy_s(&ntci->LocalEndPoint.AddressBytes, sizeof_member(IPEndPointInfo, AddressBytes), &in_pcb.inp_laddr.s_addr, 4);
            memcpy_s(&ntci->RemoteEndPoint.AddressBytes, sizeof_member(IPEndPointInfo, AddressBytes), &in_pcb.inp_faddr.s_addr, 4);
            ntci->LocalEndPoint.NumAddressBytes = 4;
            ntci->RemoteEndPoint.NumAddressBytes = 4;
        }
        else
        {
            memcpy_s(&ntci->LocalEndPoint.AddressBytes, sizeof_member(IPEndPointInfo, AddressBytes), &in_pcb.in6p_laddr.s6_addr, 16);
            memcpy_s(&ntci->RemoteEndPoint.AddressBytes, sizeof_member(IPEndPointInfo, AddressBytes), &in_pcb.in6p_faddr.s6_addr, 16);
            ntci->LocalEndPoint.NumAddressBytes = 16;
            ntci->RemoteEndPoint.NumAddressBytes = 16;
        }

        ntci->LocalEndPoint.Port = ntohs(in_pcb.inp_lport);
        ntci->RemoteEndPoint.Port = ntohs(in_pcb.inp_fport);
    }

    free(buffer);
    return 0;
}

int32_t SystemNative_GetEstimatedUdpListenerCount()
{
    int32_t count;
    size_t oldlenp = sizeof(count);
    sysctlbyname("net.inet.udp.pcbcount", &count, &oldlenp, NULL, 0);
    return count;
}

static size_t GetEstimatedUdpPcbSize()
{
    void* oldp = NULL;
    void* newp = NULL;
    size_t oldlenp, newlen = 0;

    sysctlbyname("net.inet.udp.pcblist", oldp, &oldlenp, newp, newlen);
    return oldlenp;
}

int32_t SystemNative_GetActiveUdpListeners(IPEndPointInfo* infos, int32_t* infoCount)
{
    assert(infos != NULL);
    assert(infoCount != NULL);

    size_t estimatedSize = GetEstimatedUdpPcbSize();
    uint8_t* buffer = (uint8_t*)malloc(estimatedSize * sizeof(uint8_t));
    if (buffer == NULL)
    {
        errno = ENOMEM;
        return -1;
    }

    void* newp = NULL;
    size_t newlen = 0;

    while (sysctlbyname("net.inet.udp.pcblist", buffer, &estimatedSize, newp, newlen) != 0)
    {
        free(buffer);
        size_t tmpEstimatedSize;
        if (!multiply_s(estimatedSize, (size_t)2, &tmpEstimatedSize) ||
            (buffer = (uint8_t*)malloc(estimatedSize * sizeof(uint8_t))) == NULL)
        {
            errno = ENOMEM;
            return -1;
        }
        estimatedSize = tmpEstimatedSize;
    }
    int32_t count = (int32_t)(estimatedSize / sizeof(struct xtcpcb));
    if (count > *infoCount)
    {
        // Not enough space in caller-supplied buffer.
        free(buffer);
        *infoCount = count;
        return -1;
    }
    *infoCount = count;

    struct inpcb in_pcb;
    struct xinpgen* xHeadPtr;
    int32_t connectionIndex = -1;
    xHeadPtr = (struct xinpgen*)buffer;
    for (xHeadPtr = (struct xinpgen*)((uint8_t*)xHeadPtr + xHeadPtr->xig_len);
         xHeadPtr->xig_len >= sizeof(struct xinpcb);
         xHeadPtr = (struct xinpgen*)((uint8_t*)xHeadPtr + xHeadPtr->xig_len))
    {
        connectionIndex++;
        struct xinpcb* head_xinpcb = (struct xinpcb*)xHeadPtr;
        in_pcb = head_xinpcb->xi_inp;
        IPEndPointInfo* iepi = &infos[connectionIndex];

        uint8_t vflag = in_pcb.inp_vflag; // INP_IPV4 or INP_IPV6
        if ((vflag & INP_IPV4) == INP_IPV4)
        {
            memcpy_s(iepi->AddressBytes, sizeof_member(IPEndPointInfo, AddressBytes), &in_pcb.inp_laddr.s_addr, 4);
            iepi->NumAddressBytes = 4;
        }
        else
        {
            memcpy_s(iepi->AddressBytes, sizeof_member(IPEndPointInfo, AddressBytes), &in_pcb.in6p_laddr.s6_addr, 16);
            iepi->NumAddressBytes = 16;
        }

        iepi->Port = ntohs(in_pcb.inp_lport);
    }

    free(buffer);
    return 0;
}

int32_t SystemNative_GetNativeIPInterfaceStatistics(char* interfaceName, NativeIPInterfaceStatistics* retStats)
{
    assert(interfaceName != NULL && retStats != NULL);
    unsigned int interfaceIndex = if_nametoindex(interfaceName);
    if (interfaceIndex == 0)
    {
        // An invalid interface name was given (doesn't exist).
        return -1;
    }
#if HAVE_IF_MSGHDR2
    int statisticsMib[] = {CTL_NET, PF_ROUTE, 0, 0, NET_RT_IFLIST2, (int)interfaceIndex};
#else
    int statisticsMib[] = {CTL_NET, PF_ROUTE, 0, 0, NET_RT_IFLIST, (int)interfaceIndex};
#endif

    size_t len;
    // Get estimated data length
    if (sysctl(statisticsMib, 6, NULL, &len, NULL, 0) == -1)
    {
        memset(retStats, 0, sizeof(NativeIPInterfaceStatistics));
        return -1;
    }

    uint8_t* buffer = (uint8_t*)malloc(len * sizeof(uint8_t));
    if (buffer == NULL)
    {
        errno = ENOMEM;
        return -1;
    }

    if (sysctl(statisticsMib, 6, buffer, &len, NULL, 0) == -1)
    {
        // Not enough space.
        free(buffer);
        memset(retStats, 0, sizeof(NativeIPInterfaceStatistics));
        return -1;
    }

    for (uint8_t* headPtr = buffer; headPtr <= buffer + len;
         headPtr += ((struct if_msghdr*)headPtr)->ifm_msglen)
    {
        struct if_msghdr* ifHdr = (struct if_msghdr*)headPtr;
#if HAVE_IF_MSGHDR2
        if (ifHdr->ifm_index == interfaceIndex && ifHdr->ifm_type == RTM_IFINFO2)
        {
            struct if_msghdr2* ifHdr2 = (struct if_msghdr2*)ifHdr;
            retStats->SendQueueLength = (uint64_t)ifHdr2->ifm_snd_maxlen;

            struct if_data64 systemStats = ifHdr2->ifm_data;
#else
        if (ifHdr->ifm_index == interfaceIndex && ifHdr->ifm_type == RTM_IFINFO)
        {
            struct if_msghdr* ifHdr2 = (struct if_msghdr*)ifHdr;
            retStats->SendQueueLength = 0;

            struct if_data systemStats = ifHdr2->ifm_data;
#endif
            retStats->Mtu = systemStats.ifi_mtu;
            retStats->Speed = systemStats.ifi_baudrate; // bits per second.
            retStats->InPackets = systemStats.ifi_ipackets;
            retStats->InErrors = systemStats.ifi_ierrors;
            retStats->OutPackets = systemStats.ifi_opackets;
            retStats->OutErrors = systemStats.ifi_oerrors;
            retStats->InBytes = systemStats.ifi_ibytes;
            retStats->OutBytes = systemStats.ifi_obytes;
            retStats->InMulticastPackets = systemStats.ifi_imcasts;
            retStats->OutMulticastPackets = systemStats.ifi_omcasts;
            retStats->InDrops = systemStats.ifi_iqdrops;
            retStats->InNoProto = systemStats.ifi_noproto;

            retStats->Flags = 0;
            if (ifHdr->ifm_flags & IFF_UP)
            {
                retStats->Flags |= InterfaceUp;
#if HAVE_NET_IFMEDIA_H
                int fd =  socket(AF_INET, SOCK_DGRAM, 0);
                if (fd < 0) {
                    retStats->Flags |= InterfaceError;
                }
                else
                {
                    struct ifmediareq ifmr;
                    memset(&ifmr, 0, sizeof(ifmr));
                    strncpy(ifmr.ifm_name, interfaceName, sizeof(ifmr.ifm_name));

                    if (ioctl(fd, SIOCGIFMEDIA, (caddr_t)&ifmr) < 0)
                    {
                        if (errno == EOPNOTSUPP || errno == EINVAL)
                        {
                            // Virtual interfaces like loopback do not have media.
                            // Assume they are up when administrative state is up.
                            retStats->Flags |= InterfaceHasLink;
                        }
                        else
                        {
                            retStats->Flags |= InterfaceError;
                        }
                    }
                    else if ((ifmr.ifm_status & IFM_AVALID) == 0)
                    {
                        retStats->Flags |= InterfaceError;
                    }
                    else
                    {
                        if (ifmr.ifm_status & IFM_ACTIVE)
                        {
                            retStats->Flags |= InterfaceHasLink;
                        }
                    }
                    close(fd);
                }
#else
                retStats->Flags |= InterfaceError;
#endif
            }

            if (ifHdr->ifm_flags & (IFF_MULTICAST | IFF_ALLMULTI))
            {
                retStats->Flags |= InterfaceSupportsMulticast;
            }

            free(buffer);
            return 0;
        }
    }

    // No statistics were found with the given interface index; shouldn't happen.
    free(buffer);
    memset(retStats, 0, sizeof(NativeIPInterfaceStatistics));
    return -1;
}
int32_t SystemNative_GetNumRoutes()
{
    int32_t count = 0;
#if HAVE_RT_MSGHDR2
    int routeDumpMib[] = {CTL_NET, PF_ROUTE, 0, 0, NET_RT_DUMP, 0};

    size_t len;
    if (sysctl(routeDumpMib, 6, NULL, &len, NULL, 0) == -1)
    {
        return -1;
    }

    uint8_t* buffer = (uint8_t*)malloc(len * sizeof(uint8_t));
    if (buffer == NULL)
    {
        errno = ENOMEM;
        return -1;
    }

    if (sysctl(routeDumpMib, 6, buffer, &len, NULL, 0) == -1)
    {
        free(buffer);
        return -1;
    }

    uint8_t* headPtr = buffer;
    struct rt_msghdr2* rtmsg;

    for (size_t i = 0; i < len; i += rtmsg->rtm_msglen)
    {
        rtmsg = (struct rt_msghdr2*)&buffer[i];
        if (rtmsg->rtm_flags & RTF_UP)
        {
            count++;
        }

        headPtr += rtmsg->rtm_msglen;
    }

    free(buffer);
#endif // HAVE_RT_MSGHDR2
    return count;
}
#endif // HAVE_TCP_VAR_H
