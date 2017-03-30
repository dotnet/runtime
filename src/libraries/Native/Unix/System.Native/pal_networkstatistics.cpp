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

#include <errno.h>
#include <memory>
#include <net/route.h>

#include <sys/types.h>
#if HAVE_SYS_SYSCTL_H
#include <sys/sysctl.h>
#endif
#include <sys/socketvar.h>
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

template <class RetType>
int32_t ReadSysctlVar(const char* name, RetType* value)
{
    size_t oldlenp = sizeof(RetType);
    void* newp = nullptr;
    size_t newlen = 0;
    return sysctlbyname(name, value, &oldlenp, newp, newlen);
}

extern "C" int32_t SystemNative_GetTcpGlobalStatistics(TcpGlobalStatistics* retStats)
{
    assert(retStats != nullptr);

    tcpstat systemStats;
    if (ReadSysctlVar("net.inet.tcp.stats", &systemStats))
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

    if (ReadSysctlVar("net.inet.tcp.pcbcount", &retStats->CurrentConnections))
    {
        retStats->CurrentConnections = 0;
        return -1;
    }

    return 0;
}

extern "C" int32_t SystemNative_GetIPv4GlobalStatistics(IPv4GlobalStatistics* retStats)
{
    assert(retStats != nullptr);

    ipstat systemStats;
    if (ReadSysctlVar("net.inet.ip.stats", &systemStats))
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

    if (ReadSysctlVar("net.inet.ip.ttl", &retStats->DefaultTtl))
    {
        retStats->DefaultTtl = 0;
        retStats->Forwarding = 0;
        return -1;
    }
    if (ReadSysctlVar("net.inet.ip.forwarding", &retStats->Forwarding))
    {
        retStats->Forwarding = 0;
        return -1;
    }

    return 0;
}

extern "C" int32_t SystemNative_GetUdpGlobalStatistics(UdpGlobalStatistics* retStats)
{
    assert(retStats != nullptr);

    udpstat systemStats;
    if (ReadSysctlVar("net.inet.udp.stats", &systemStats))
    {
        memset(retStats, 0, sizeof(UdpGlobalStatistics)); // out parameter must be initialized.
        return -1;
    }

    retStats->DatagramsReceived = systemStats.udps_ipackets;
    retStats->DatagramsSent = systemStats.udps_opackets;
    retStats->IncomingDiscarded = systemStats.udps_noport;
    retStats->IncomingErrors = systemStats.udps_hdrops + systemStats.udps_badsum + systemStats.udps_badlen;

    // This may contain both UDP4 and UDP6 listeners.
    if (ReadSysctlVar("net.inet.udp.pcbcount", &retStats->UdpListeners))
    {
        retStats->UdpListeners = 0;
        return -1;
    }

    return 0;
}

extern "C" int32_t SystemNative_GetIcmpv4GlobalStatistics(Icmpv4GlobalStatistics* retStats)
{
    assert(retStats != nullptr);

    icmpstat systemStats;
    if (ReadSysctlVar("net.inet.icmp.stats", &systemStats))
    {
        memset(retStats, 0, sizeof(Icmpv4GlobalStatistics));
        return -1;
    }

    u_int32_t* inHist = systemStats.icps_inhist;
    u_int32_t* outHist = systemStats.icps_outhist;

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

extern "C" int32_t SystemNative_GetIcmpv6GlobalStatistics(Icmpv6GlobalStatistics* retStats)
{
    assert(retStats != nullptr);

    icmp6stat systemStats;
    if (ReadSysctlVar("net.inet6.icmp6.stats", &systemStats))
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

extern "C" int32_t SystemNative_GetEstimatedTcpConnectionCount()
{
    int32_t count;
    ReadSysctlVar("net.inet.tcp.pcbcount", &count);
    return count;
}

size_t GetEstimatedTcpPcbSize()
{
    void* oldp = nullptr;
    void* newp = nullptr;
    size_t oldlenp, newlen = 0;

    sysctlbyname("net.inet.tcp.pcblist", oldp, &oldlenp, newp, newlen);
    return oldlenp;
}

extern "C" int32_t SystemNative_GetActiveTcpConnectionInfos(NativeTcpConnectionInformation* infos, int32_t* infoCount)
{
    assert(infos != nullptr);
    assert(infoCount != nullptr);

    size_t estimatedSize = GetEstimatedTcpPcbSize();
    uint8_t* buffer = new (std::nothrow) uint8_t[estimatedSize];
    if (buffer == nullptr)
    {
        errno = ENOMEM;
        return -1;
    }

    void* newp = nullptr;
    size_t newlen = 0;

    while (sysctlbyname("net.inet.tcp.pcblist", buffer, &estimatedSize, newp, newlen) != 0)
    {
        delete[] buffer;
        size_t tmpEstimatedSize;
        if (!multiply_s(estimatedSize, static_cast<size_t>(2), &tmpEstimatedSize) ||
            (buffer = new (std::nothrow) uint8_t[estimatedSize]) == nullptr)
        {
            errno = ENOMEM;
            return -1;
        }
        estimatedSize = tmpEstimatedSize;
    }

    int32_t count = static_cast<int32_t>(estimatedSize / sizeof(xtcpcb));
    if (count > *infoCount)
    {
        // Not enough space in caller-supplied buffer.
        delete[] buffer;
        *infoCount = count;
        return -1;
    }
    *infoCount = count;

    //  sizeof(xtcpcb) == 524
    tcpcb tcp_pcb;
    inpcb in_pcb;
    xinpgen* xHeadPtr;
    int32_t connectionIndex = -1;
    xHeadPtr = reinterpret_cast<xinpgen*>(buffer);
    for (xHeadPtr = reinterpret_cast<xinpgen*>(reinterpret_cast<uint8_t*>(xHeadPtr) + xHeadPtr->xig_len);
         xHeadPtr->xig_len >= sizeof(xtcpcb);
         xHeadPtr = reinterpret_cast<xinpgen*>(reinterpret_cast<uint8_t*>(xHeadPtr) + xHeadPtr->xig_len))
    {
        connectionIndex++;
        xtcpcb* head_xtcpb = reinterpret_cast<xtcpcb*>(xHeadPtr);

        tcp_pcb = head_xtcpb->xt_tp;
        in_pcb = head_xtcpb->xt_inp;

        NativeTcpConnectionInformation* ntci = &infos[connectionIndex];
        ntci->State = SystemNative_MapTcpState(tcp_pcb.t_state);

        uint8_t vflag = in_pcb.inp_vflag; // INP_IPV4 or INP_IPV6
        bool isIpv4 = (vflag & INP_IPV4) == INP_IPV4;
        if (isIpv4)
        {
            memcpy_s(&ntci->LocalEndPoint.AddressBytes, sizeof(IPEndPointInfo::AddressBytes), &in_pcb.inp_laddr.s_addr, 4);
            memcpy_s(&ntci->RemoteEndPoint.AddressBytes, sizeof(IPEndPointInfo::AddressBytes), &in_pcb.inp_faddr.s_addr, 4);
            ntci->LocalEndPoint.NumAddressBytes = 4;
            ntci->RemoteEndPoint.NumAddressBytes = 4;
        }
        else
        {
            memcpy_s(&ntci->LocalEndPoint.AddressBytes, sizeof(IPEndPointInfo::AddressBytes), &in_pcb.in6p_laddr.s6_addr, 16);
            memcpy_s(&ntci->RemoteEndPoint.AddressBytes, sizeof(IPEndPointInfo::AddressBytes), &in_pcb.in6p_faddr.s6_addr, 16);
            ntci->LocalEndPoint.NumAddressBytes = 16;
            ntci->RemoteEndPoint.NumAddressBytes = 16;
        }

        ntci->LocalEndPoint.Port = in_pcb.inp_lport;
        ntci->RemoteEndPoint.Port = in_pcb.inp_fport;
    }

    delete[] buffer;
    return 0;
}

extern "C" int32_t SystemNative_GetEstimatedUdpListenerCount()
{
    int32_t count;
    ReadSysctlVar("net.inet.udp.pcbcount", &count);
    return count;
}

size_t GetEstimatedUdpPcbSize()
{
    void* oldp = nullptr;
    void* newp = nullptr;
    size_t oldlenp, newlen = 0;

    sysctlbyname("net.inet.udp.pcblist", oldp, &oldlenp, newp, newlen);
    return oldlenp;
}

extern "C" int32_t SystemNative_GetActiveUdpListeners(IPEndPointInfo* infos, int32_t* infoCount)
{
    assert(infos != nullptr);
    assert(infoCount != nullptr);

    size_t estimatedSize = GetEstimatedUdpPcbSize();
    uint8_t* buffer = new (std::nothrow) uint8_t[estimatedSize];
    if (buffer == nullptr)
    {
        errno = ENOMEM;
        return -1;
    }

    void* newp = nullptr;
    size_t newlen = 0;

    while (sysctlbyname("net.inet.udp.pcblist", buffer, &estimatedSize, newp, newlen) != 0)
    {
        delete[] buffer;
        size_t tmpEstimatedSize;
        if (!multiply_s(estimatedSize, static_cast<size_t>(2), &tmpEstimatedSize) ||
            (buffer = new (std::nothrow) uint8_t[estimatedSize]) == nullptr)
        {
            errno = ENOMEM;
            return -1;
        }
        estimatedSize = tmpEstimatedSize;
    }
    int32_t count = static_cast<int32_t>(estimatedSize / sizeof(xtcpcb));
    if (count > *infoCount)
    {
        // Not enough space in caller-supplied buffer.
        delete[] buffer;
        *infoCount = count;
        return -1;
    }
    *infoCount = count;

    inpcb in_pcb;
    xinpgen* xHeadPtr;
    int32_t connectionIndex = -1;
    xHeadPtr = reinterpret_cast<xinpgen*>(buffer);
    for (xHeadPtr = reinterpret_cast<xinpgen*>(reinterpret_cast<uint8_t*>(xHeadPtr) + xHeadPtr->xig_len);
         xHeadPtr->xig_len >= sizeof(xinpcb);
         xHeadPtr = reinterpret_cast<xinpgen*>(reinterpret_cast<uint8_t*>(xHeadPtr) + xHeadPtr->xig_len))
    {
        connectionIndex++;
        xinpcb* head_xinpcb = reinterpret_cast<xinpcb*>(xHeadPtr);
        in_pcb = head_xinpcb->xi_inp;
        IPEndPointInfo* iepi = &infos[connectionIndex];

        uint8_t vflag = in_pcb.inp_vflag; // INP_IPV4 or INP_IPV6
        bool isIpv4 = (vflag & INP_IPV4) == INP_IPV4;
        if (isIpv4)
        {
            memcpy_s(iepi->AddressBytes, sizeof(IPEndPointInfo::AddressBytes), &in_pcb.inp_laddr.s_addr, 4);
            iepi->NumAddressBytes = 4;
        }
        else
        {
            memcpy_s(iepi->AddressBytes, sizeof(IPEndPointInfo::AddressBytes), &in_pcb.in6p_laddr.s6_addr, 16);
            iepi->NumAddressBytes = 16;
        }

        iepi->Port = in_pcb.inp_lport;
    }

    delete[] buffer;
    return 0;
}

extern "C" int32_t SystemNative_GetNativeIPInterfaceStatistics(char* interfaceName, NativeIPInterfaceStatistics* retStats)
{
    assert(interfaceName != nullptr && retStats != nullptr);
    unsigned int interfaceIndex = if_nametoindex(interfaceName);
    if (interfaceIndex == 0)
    {
        // An invalid interface name was given (doesn't exist).
        return -1;
    }

    int statisticsMib[] = {CTL_NET, PF_ROUTE, 0, 0, NET_RT_IFLIST2, 0};

    size_t len;
    // Get estimated data length
    if (sysctl(statisticsMib, 6, nullptr, &len, nullptr, 0) == -1)
    {
        memset(retStats, 0, sizeof(NativeIPInterfaceStatistics));
        return -1;
    }

    uint8_t* buffer = new (std::nothrow) uint8_t[len];
    if (buffer == nullptr)
    {
        errno = ENOMEM;
        return -1;
    }

    if (sysctl(statisticsMib, 6, buffer, &len, nullptr, 0) == -1)
    {
        // Not enough space.
        delete[] buffer;
        memset(retStats, 0, sizeof(NativeIPInterfaceStatistics));
        return -1;
    }

    for (uint8_t* headPtr = buffer; headPtr <= buffer + len;
         headPtr += reinterpret_cast<if_msghdr*>(headPtr)->ifm_msglen)
    {
        if_msghdr* ifHdr = reinterpret_cast<if_msghdr*>(headPtr);
        if (ifHdr->ifm_index == interfaceIndex && ifHdr->ifm_type == RTM_IFINFO2)
        {
            if_msghdr2* ifHdr2 = reinterpret_cast<if_msghdr2*>(ifHdr);
            retStats->SendQueueLength = static_cast<uint64_t>(ifHdr2->ifm_snd_maxlen);

            if_data64 systemStats = ifHdr2->ifm_data;
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
            delete[] buffer;
            return 0;
        }
    }

    // No statistics were found with the given interface index; shouldn't happen.
    delete[] buffer;
    memset(retStats, 0, sizeof(NativeIPInterfaceStatistics));
    return -1;
}

extern "C" int32_t SystemNative_GetNumRoutes()
{
    int routeDumpMib[] = {CTL_NET, PF_ROUTE, 0, 0, NET_RT_DUMP, 0};

    size_t len;
    if (sysctl(routeDumpMib, 6, nullptr, &len, nullptr, 0) == -1)
    {
        return -1;
    }

    uint8_t* buffer = new (std::nothrow) uint8_t[len];
    if (buffer == nullptr)
    {
        errno = ENOMEM;
        return -1;
    }

    if (sysctl(routeDumpMib, 6, buffer, &len, nullptr, 0) == -1)
    {
        delete[] buffer;
        return -1;
    }

    uint8_t* headPtr = buffer;
    rt_msghdr2* rtmsg;
    int32_t count = 0;

    for (size_t i = 0; i < len; i += rtmsg->rtm_msglen)
    {
        rtmsg = reinterpret_cast<rt_msghdr2*>(&buffer[i]);
        if (rtmsg->rtm_flags & RTF_UP)
        {
            count++;
        }

        headPtr += rtmsg->rtm_msglen;
    }

    delete[] buffer;
    return count;
}

#endif // HAVE_TCP_VAR_H
