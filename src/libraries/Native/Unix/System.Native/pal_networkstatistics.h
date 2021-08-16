// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_tcpstate.h"

// Exchange types used to normalize Network protocol statistics information
// from the OS, for use in the NetworkInformation library.

enum {
    InterfaceUp = 0x1,
    InterfaceSupportsMulticast = 0x2,
    InterfaceHasLink = 0x4,
    InterfaceError = 0x8
};

typedef struct
{
    uint64_t ConnectionsAccepted;
    uint64_t ConnectionsInitiated;
    uint64_t CumulativeConnections;
    uint64_t ErrorsReceived;
    uint64_t FailedConnectionAttempts;
    uint64_t SegmentsReceived;
    uint64_t SegmentsResent;
    uint64_t SegmentsSent;
    int32_t CurrentConnections;
    int32_t _padding;
} TcpGlobalStatistics;

typedef struct
{
    uint64_t OutboundPackets;
    uint64_t OutputPacketsNoRoute;
    uint64_t CantFrags;
    uint64_t DatagramsFragmented;
    uint64_t PacketsReassembled;
    uint64_t TotalPacketsReceived;
    uint64_t PacketsDelivered;
    uint64_t PacketsDiscarded;
    uint64_t PacketsForwarded;
    uint64_t BadAddress;
    uint64_t BadHeader;
    uint64_t UnknownProtos;
    int32_t DefaultTtl;
    int32_t Forwarding;
} IPv4GlobalStatistics;

typedef struct
{
    uint64_t DatagramsReceived;
    uint64_t DatagramsSent;
    uint64_t IncomingDiscarded;
    uint64_t IncomingErrors;
    uint64_t UdpListeners;
} UdpGlobalStatistics;

typedef struct
{
    uint64_t AddressMaskRepliesReceived;
    uint64_t AddressMaskRepliesSent;
    uint64_t AddressMaskRequestsReceived;
    uint64_t AddressMaskRequestsSent;
    uint64_t DestinationUnreachableMessagesReceived;
    uint64_t DestinationUnreachableMessagesSent;
    uint64_t EchoRepliesReceived;
    uint64_t EchoRepliesSent;
    uint64_t EchoRequestsReceived;
    uint64_t EchoRequestsSent;
    uint64_t ParameterProblemsReceived;
    uint64_t ParameterProblemsSent;
    uint64_t RedirectsReceived;
    uint64_t RedirectsSent;
    uint64_t SourceQuenchesReceived;
    uint64_t SourceQuenchesSent;
    uint64_t TimeExceededMessagesReceived;
    uint64_t TimeExceededMessagesSent;
    uint64_t TimestampRepliesReceived;
    uint64_t TimestampRepliesSent;
    uint64_t TimestampRequestsReceived;
    uint64_t TimestampRequestsSent;
} Icmpv4GlobalStatistics;

typedef struct
{
    uint64_t DestinationUnreachableMessagesReceived;
    uint64_t DestinationUnreachableMessagesSent;
    uint64_t EchoRepliesReceived;
    uint64_t EchoRepliesSent;
    uint64_t EchoRequestsReceived;
    uint64_t EchoRequestsSent;
    uint64_t MembershipQueriesReceived;
    uint64_t MembershipQueriesSent;
    uint64_t MembershipReductionsReceived;
    uint64_t MembershipReductionsSent;
    uint64_t MembershipReportsReceived;
    uint64_t MembershipReportsSent;
    uint64_t NeighborAdvertisementsReceived;
    uint64_t NeighborAdvertisementsSent;
    uint64_t NeighborSolicitsReceived;
    uint64_t NeighborSolicitsSent;
    uint64_t PacketTooBigMessagesReceived;
    uint64_t PacketTooBigMessagesSent;
    uint64_t ParameterProblemsReceived;
    uint64_t ParameterProblemsSent;
    uint64_t RedirectsReceived;
    uint64_t RedirectsSent;
    uint64_t RouterAdvertisementsReceived;
    uint64_t RouterAdvertisementsSent;
    uint64_t RouterSolicitsReceived;
    uint64_t RouterSolicitsSent;
    uint64_t TimeExceededMessagesReceived;
    uint64_t TimeExceededMessagesSent;
} Icmpv6GlobalStatistics;

typedef struct
{
    uint8_t AddressBytes[16];
    uint32_t NumAddressBytes;
    uint32_t Port;
    uint32_t _padding1;
} IPEndPointInfo;

typedef struct
{
    IPEndPointInfo LocalEndPoint;
    IPEndPointInfo RemoteEndPoint;
    int32_t State;
} NativeTcpConnectionInformation;

typedef struct
{
    uint64_t SendQueueLength;
    uint64_t Mtu;
    uint64_t Speed;
    uint64_t InPackets;
    uint64_t InErrors;
    uint64_t OutPackets;
    uint64_t OutErrors;
    uint64_t InBytes;
    uint64_t OutBytes;
    uint64_t InMulticastPackets;
    uint64_t OutMulticastPackets;
    uint64_t InDrops;
    uint64_t InNoProto;
    uint64_t Flags;
} NativeIPInterfaceStatistics;

PALEXPORT int32_t SystemNative_GetTcpGlobalStatistics(TcpGlobalStatistics* retStats);

PALEXPORT int32_t SystemNative_GetIPv4GlobalStatistics(IPv4GlobalStatistics* retStats);

PALEXPORT int32_t SystemNative_GetUdpGlobalStatistics(UdpGlobalStatistics* retStats);

PALEXPORT int32_t SystemNative_GetIcmpv4GlobalStatistics(Icmpv4GlobalStatistics* retStats);

PALEXPORT int32_t SystemNative_GetIcmpv6GlobalStatistics(Icmpv6GlobalStatistics* retStats);

PALEXPORT int32_t SystemNative_GetEstimatedTcpConnectionCount(void);

PALEXPORT int32_t SystemNative_GetActiveTcpConnectionInfos(NativeTcpConnectionInformation* infos, int32_t* infoCount);

PALEXPORT int32_t SystemNative_GetEstimatedUdpListenerCount(void);

PALEXPORT int32_t SystemNative_GetActiveUdpListeners(IPEndPointInfo* infos, int32_t* infoCount);

PALEXPORT int32_t SystemNative_GetNativeIPInterfaceStatistics(char* interfaceName, NativeIPInterfaceStatistics* retStats);

PALEXPORT int32_t SystemNative_GetNumRoutes(void);
