// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_types.h"
#include "pal_errno.h"
#include <pal_networking_common.h>

/**
 * These error values are different on every platform so make a
 * platform-agnostic version that we convert to and send to managed
 */
typedef enum
{
    GetAddrInfoErrorFlags_EAI_SUCCESS = 0,  // Success
    GetAddrInfoErrorFlags_EAI_AGAIN = 1,    // Temporary failure in name resolution.
    GetAddrInfoErrorFlags_EAI_BADFLAGS = 2, // Invalid value for `ai_flags' field.
    GetAddrInfoErrorFlags_EAI_FAIL = 3,     // Non-recoverable failure in name resolution.
    GetAddrInfoErrorFlags_EAI_FAMILY = 4,   // 'ai_family' not supported.
    GetAddrInfoErrorFlags_EAI_NONAME = 5,   // NAME or SERVICE is unknown.
    GetAddrInfoErrorFlags_EAI_BADARG = 6,   // One or more input arguments were invalid.
    GetAddrInfoErrorFlags_EAI_NOMORE = 7,   // No more entries are present in the list.
    GetAddrInfoErrorFlags_EAI_MEMORY = 8,   // Out of memory.
} GetAddrInfoErrorFlags;

/**
 * Flags to pass to GetNameInfo. These do not match
 * from platform to platform and must be converted
 */
typedef enum
{
    GetAddrInfoErrorFlags_NI_NAMEREQD = 0x1,
    GetAddrInfoErrorFlags_NI_NUMERICHOST = 0x2,
} GetNameInfoFlags;

/**
 * Error codes from GetHostByName and GetHostByAddr
 */
typedef enum
{
    GetHostErrorCodes_HOST_NOT_FOUND = 1,
    GetHostErrorCodes_TRY_AGAIN = 2,
    GetHostErrorCodes_NO_RECOVERY = 3,
    GetHostErrorCodes_NO_DATA = 4,
    GetHostErrorCodes_NO_ADDRESS = GetHostErrorCodes_NO_DATA,
    GetHostErrorCodes_BAD_ARG = 5,
    GetHostErrorCodes_NO_MEM = 6,
} GetHostErrorCodes;

/**
 * Address families recognized by {Get,Set}AddressFamily.
 *
 * NOTE: these values are taken from System.Net.AddressFamily. If you add
 *       new entries, be sure that the values are chosen accordingly.
 *       Unix specific values have distinct offset to avoid conflict with
 *       Windows values.
 */
typedef enum
{
    AddressFamily_AF_UNKNOWN = -1, // System.Net.AddressFamily.Unknown
    AddressFamily_AF_UNSPEC = 0,   // System.Net.AddressFamily.Unspecified
    AddressFamily_AF_UNIX = 1,     // System.Net.AddressFamily.Unix
    AddressFamily_AF_INET = 2,     // System.Net.AddressFamily.InterNetwork
    AddressFamily_AF_INET6 = 23,   // System.Net.AddressFamily.InterNetworkV6
    AddressFamily_AF_PACKET = 65536,  // System.Net.AddressFamily.Packet
    AddressFamily_AF_CAN = 65537,     // System.Net.AddressFamily.ControllerAreaNetwork
} AddressFamily;

/*
 * Socket types.
 *
 * NOTE: these values are taken from System.Net.SocketType.
 */
typedef enum
{
    SocketType_UNKNOWN = -1,       // System.Net.SocketType.Unknown
    SocketType_SOCK_STREAM = 1,    // System.Net.SocketType.Stream
    SocketType_SOCK_DGRAM = 2,     // System.Net.SocketType.Dgram
    SocketType_SOCK_RAW = 3,       // System.Net.SocketType.Raw
    SocketType_SOCK_RDM = 4,       // System.Net.SocketType.Rdm
    SocketType_SOCK_SEQPACKET = 5, // System.Net.SocketType.SeqPacket
} SocketType;

/*
 * Protocol types.
 *
 * NOTE: these values are taken from System.Net.ProtocolType.
 */
typedef enum
{
    ProtocolType_PT_UNKNOWN = -1,    // System.Net.ProtocolType.Unknown
    ProtocolType_PT_UNSPECIFIED = 0, // System.Net.ProtocolType.Unspecified
    ProtocolType_PT_ICMP = 1,        // System.Net.ProtocolType.Icmp
    ProtocolType_PT_TCP = 6,         // System.Net.ProtocolType.Tcp
    ProtocolType_PT_UDP = 17,        // System.Net.ProtocolType.Udp
    ProtocolType_PT_ICMPV6 = 58,     // System.Net.ProtocolType.IcmpV6
    ProtocolType_PT_RAW = 255,       // System.Net.ProtocolType.Raw
    ProtocolType_PT_IGMP = 2,        // System.Net.ProtocolType.Igmp
    ProtocolType_PT_NONE = 59,       // System.Net.ProtocolType.IPv6NoNextHeader
    ProtocolType_PT_DSTOPTS = 60,    // System.Net.ProtocolType.IPv6DestinationOptions
    ProtocolType_PT_ROUTING = 43,    // System.Net.ProtocolType.IPv6RoutingHeader
    ProtocolType_PT_FRAGMENT = 44,   // System.Net.ProtocolType.IPv6FragmentHeader
} ProtocolType;

typedef enum
{
    MulticastOption_MULTICAST_ADD = 0,  // IP{,V6}_ADD_MEMBERSHIP
    MulticastOption_MULTICAST_DROP = 1, // IP{,V6}_DROP_MEMBERSHIP
    MulticastOption_MULTICAST_IF = 2    // IP_MULTICAST_IF
} MulticastOption;

/*
 * Socket option levels.
 *
 * NOTE: these values are taken from System.Net.SocketOptionLevel.
 */
typedef enum
{
    SocketOptionLevel_SOL_SOCKET = 0xffff,
    SocketOptionLevel_SOL_IP = 0,
    SocketOptionLevel_SOL_IPV6 = 41,
    SocketOptionLevel_SOL_TCP = 6,
    SocketOptionLevel_SOL_UDP = 17,
} SocketOptionLevel;

/*
 * Socket option names.
 *
 * NOTE: these values are taken from System.Net.SocketOptionName. Only values that are known to be usable on all target
 *       platforms are represented here. Unsupported values are present as commented-out entries.
 */
typedef enum
{
    // Names for level SocketOptionLevel_SOL_SOCKET
    SocketOptionName_SO_DEBUG = 0x0001,
    SocketOptionName_SO_ACCEPTCONN = 0x0002,
    SocketOptionName_SO_REUSEADDR = 0x0004,
    SocketOptionName_SO_KEEPALIVE = 0x0008,
    SocketOptionName_SO_DONTROUTE = 0x0010,
    SocketOptionName_SO_BROADCAST = 0x0020,
    // SocketOptionName_SO_USELOOPBACK = 0x0040,
    SocketOptionName_SO_LINGER = 0x0080,
    SocketOptionName_SO_OOBINLINE = 0x0100,
    // SocketOptionName_SO_DONTLINGER = ~SocketOptionName_SO_LINGER,
    SocketOptionName_SO_EXCLUSIVEADDRUSE = ~SocketOptionName_SO_REUSEADDR,
    SocketOptionName_SO_SNDBUF = 0x1001,
    SocketOptionName_SO_RCVBUF = 0x1002,
    SocketOptionName_SO_SNDLOWAT = 0x1003,
    SocketOptionName_SO_RCVLOWAT = 0x1004,
    SocketOptionName_SO_SNDTIMEO = 0x1005,
    SocketOptionName_SO_RCVTIMEO = 0x1006,
    SocketOptionName_SO_ERROR = 0x1007,
    SocketOptionName_SO_TYPE = 0x1008,
    // SocketOptionName_SO_MAXCONN = 0x7fffffff,

    // Names for level SocketOptionLevel_SOL_IP
    SocketOptionName_SO_IP_OPTIONS = 1,
    SocketOptionName_SO_IP_HDRINCL = 2,
    SocketOptionName_SO_IP_TOS = 3,
    SocketOptionName_SO_IP_TTL = 4,
    SocketOptionName_SO_IP_MULTICAST_IF = 9,
    SocketOptionName_SO_IP_MULTICAST_TTL = 10,
    SocketOptionName_SO_IP_MULTICAST_LOOP = 11,
    SocketOptionName_SO_IP_ADD_MEMBERSHIP = 12,
    SocketOptionName_SO_IP_DROP_MEMBERSHIP = 13,
    SocketOptionName_SO_IP_DONTFRAGMENT = 14,
    SocketOptionName_SO_IP_ADD_SOURCE_MEMBERSHIP = 15,
    SocketOptionName_SO_IP_DROP_SOURCE_MEMBERSHIP = 16,
    SocketOptionName_SO_IP_BLOCK_SOURCE = 17,
    SocketOptionName_SO_IP_UNBLOCK_SOURCE = 18,
    SocketOptionName_SO_IP_PKTINFO = 19,

    // Names for SocketOptionLevel_SOL_IPV6
    SocketOptionName_SO_IPV6_HOPLIMIT = 21,
    // SocketOptionName_SO_IPV6_PROTECTION_LEVEL = 23,
    SocketOptionName_SO_IPV6_V6ONLY = 27,

    // Names for SocketOptionLevel_SOL_TCP
    SocketOptionName_SO_TCP_NODELAY = 1,
    // SocketOptionName_SO_TCP_BSDURGENT = 2,
    SocketOptionName_SO_TCP_KEEPALIVE_RETRYCOUNT = 16,
    SocketOptionName_SO_TCP_KEEPALIVE_TIME = 3,
    SocketOptionName_SO_TCP_KEEPALIVE_INTERVAL = 17,

    // Names for SocketOptionLevel_SOL_UDP
    // SocketOptionName_SO_UDP_NOCHECKSUM = 1,
    // SocketOptionName_SO_UDP_CHECKSUM_COVERAGE = 20,
    // SocketOptionName_SO_UDP_UPDATEACCEPTCONTEXT = 0x700b,
    // SocketOptionName_SO_UDP_UPDATECONNECTCONTEXT = 0x7010,
} SocketOptionName;

/*
 * Socket flags.
 *
 * NOTE: these values are taken from System.Net.SocketFlags. Only values that are known to be usable on all target
 *       platforms are represented here. Unsupported values are present as commented-out entries.
 */

typedef enum
{
    SocketFlags_MSG_OOB = 0x0001,       // SocketFlags.OutOfBand
    SocketFlags_MSG_PEEK = 0x0002,      // SocketFlags.Peek
    SocketFlags_MSG_DONTROUTE = 0x0004, // SocketFlags.DontRoute
    SocketFlags_MSG_TRUNC = 0x0100,     // SocketFlags.Truncated
    SocketFlags_MSG_CTRUNC = 0x0200,    // SocketFlags.ControlDataTruncated
} SocketFlags;

/*
 * Socket async events.
 */
typedef enum
{
    SocketEvents_SA_NONE = 0x00,
    SocketEvents_SA_READ = 0x01,
    SocketEvents_SA_WRITE = 0x02,
    SocketEvents_SA_READCLOSE = 0x04,
    SocketEvents_SA_CLOSE = 0x08,
    SocketEvents_SA_ERROR = 0x10,
    // Force the enum to use int32_t instead of uint32_t
    SocketEvents__IGNORE_SIGNED = -1,
} SocketEvents;

/**
 * IP address sizes.
 */
enum
{
    NUM_BYTES_IN_IPV4_ADDRESS = 4,
    NUM_BYTES_IN_IPV6_ADDRESS = 16,
    MAX_IP_ADDRESS_BYTES = 16,
};

typedef struct
{
    uint8_t Address[MAX_IP_ADDRESS_BYTES]; // Buffer to fit IPv4 or IPv6 address
    uint32_t IsIPv6;                       // Non-zero if this is an IPv6 endpoint; zero for IPv4.
    uint32_t ScopeId;                      // Scope ID (IPv6 only)
} IPAddress;

typedef struct
{
    uint8_t* CanonicalName;    // Canonical name of the host
    uint8_t** Aliases;         // List of aliases for the host
    IPAddress* IPAddressList;  // Handle for host socket addresses
    int32_t IPAddressCount;    // Number of IP end points in the socket address list
} HostEntry;

typedef struct
{
    IPAddress Address;      // Destination IP address
    int32_t InterfaceIndex; // Interface index
    int32_t Padding;        // Pad out to 8-byte alignment
} IPPacketInformation;

typedef struct
{
    uint32_t MulticastAddress; // Multicast address
    uint32_t LocalAddress;     // Local address
    int32_t InterfaceIndex;    // Interface index
    int32_t Padding;           // Pad out to 8-byte alignment
} IPv4MulticastOption;

typedef struct
{
    IPAddress Address;      // Multicast address
    int32_t InterfaceIndex; // Interface index
    int32_t Padding;        // Pad out to 8-byte alignment
} IPv6MulticastOption;

typedef struct
{
    int32_t OnOff;   // Non-zero to enable linger
    int32_t Seconds; // Number of seconds to linger for
} LingerOption;

// NOTE: the layout of this type is intended to exactly  match the layout of a `struct iovec`. There are
//       assertions in pal_networking.cpp that validate this.
typedef struct
{
    uint8_t* Base;
    uintptr_t Count;
} IOVector;

typedef struct
{
    uint8_t* SocketAddress;
    IOVector* IOVectors;
    uint8_t* ControlBuffer;
    int32_t SocketAddressLen;
    int32_t IOVectorCount;
    int32_t ControlBufferLen;
    int32_t Flags;
} MessageHeader;

typedef struct
{
    uintptr_t Data;      // User data for this event
    int32_t Events;      // Event flags
    uint32_t Padding;    // Pad out to 8-byte alignment
} SocketEvent;

PALEXPORT int32_t SystemNative_GetHostEntryForName(const uint8_t* address, int32_t addressFamily, HostEntry* entry);

PALEXPORT void SystemNative_FreeHostEntry(HostEntry* entry);


PALEXPORT int32_t SystemNative_GetNameInfo(const uint8_t* address,
                               int32_t addressLength,
                               int8_t isIPv6,
                               uint8_t* host,
                               int32_t hostLength,
                               uint8_t* service,
                               int32_t serviceLength,
                               int32_t flags);

PALEXPORT int32_t SystemNative_GetDomainName(uint8_t* name, int32_t nameLength);

PALEXPORT int32_t SystemNative_GetHostName(uint8_t* name, int32_t nameLength);

PALEXPORT int32_t SystemNative_GetIPSocketAddressSizes(int32_t* ipv4SocketAddressSize, int32_t* ipv6SocketAddressSize);

PALEXPORT int32_t SystemNative_GetAddressFamily(const uint8_t* socketAddress, int32_t socketAddressLen, int32_t* addressFamily);

PALEXPORT int32_t SystemNative_SetAddressFamily(uint8_t* socketAddress, int32_t socketAddressLen, int32_t addressFamily);

PALEXPORT int32_t SystemNative_GetPort(const uint8_t* socketAddress, int32_t socketAddressLen, uint16_t* port);

PALEXPORT int32_t SystemNative_SetPort(uint8_t* socketAddress, int32_t socketAddressLen, uint16_t port);

PALEXPORT int32_t SystemNative_GetIPv4Address(const uint8_t* socketAddress, int32_t socketAddressLen, uint32_t* address);

PALEXPORT int32_t SystemNative_SetIPv4Address(uint8_t* socketAddress, int32_t socketAddressLen, uint32_t address);

PALEXPORT int32_t SystemNative_GetIPv6Address(
    const uint8_t* socketAddress, int32_t socketAddressLen, uint8_t* address, int32_t addressLen, uint32_t* scopeId);

PALEXPORT int32_t SystemNative_SetIPv6Address(
    uint8_t* socketAddress, int32_t socketAddressLen, uint8_t* address, int32_t addressLen, uint32_t scopeId);

PALEXPORT int32_t SystemNative_GetControlMessageBufferSize(int32_t isIPv4, int32_t isIPv6);

PALEXPORT int32_t SystemNative_TryGetIPPacketInformation(MessageHeader* messageHeader, int32_t isIPv4, IPPacketInformation* packetInfo);

PALEXPORT int32_t SystemNative_GetIPv4MulticastOption(intptr_t socket, int32_t multicastOption, IPv4MulticastOption* option);

PALEXPORT int32_t SystemNative_SetIPv4MulticastOption(intptr_t socket, int32_t multicastOption, IPv4MulticastOption* option);

PALEXPORT int32_t SystemNative_GetIPv6MulticastOption(intptr_t socket, int32_t multicastOption, IPv6MulticastOption* option);

PALEXPORT int32_t SystemNative_SetIPv6MulticastOption(intptr_t socket, int32_t multicastOption, IPv6MulticastOption* option);

PALEXPORT int32_t SystemNative_GetLingerOption(intptr_t socket, LingerOption* option);

PALEXPORT int32_t SystemNative_SetLingerOption(intptr_t socket, LingerOption* option);

PALEXPORT int32_t SystemNative_SetReceiveTimeout(intptr_t socket, int32_t millisecondsTimeout);

PALEXPORT int32_t SystemNative_SetSendTimeout(intptr_t socket, int32_t millisecondsTimeout);

PALEXPORT int32_t SystemNative_Receive(intptr_t socket, void* buffer, int32_t bufferLen, int32_t flags, int32_t* received);

PALEXPORT int32_t SystemNative_ReceiveMessage(intptr_t socket, MessageHeader* messageHeader, int32_t flags, int64_t* received);

PALEXPORT int32_t SystemNative_Send(intptr_t socket, void* buffer, int32_t bufferLen, int32_t flags, int32_t* sent);

PALEXPORT int32_t SystemNative_SendMessage(intptr_t socket, MessageHeader* messageHeader, int32_t flags, int64_t* sent);

PALEXPORT int32_t SystemNative_Accept(intptr_t socket, uint8_t* socketAddress, int32_t* socketAddressLen, intptr_t* acceptedSocket);

PALEXPORT int32_t SystemNative_Bind(intptr_t socket, int32_t protocolType, uint8_t* socketAddress, int32_t socketAddressLen);

PALEXPORT int32_t SystemNative_Connect(intptr_t socket, uint8_t* socketAddress, int32_t socketAddressLen);

PALEXPORT int32_t SystemNative_GetPeerName(intptr_t socket, uint8_t* socketAddress, int32_t* socketAddressLen);

PALEXPORT int32_t SystemNative_GetSockName(intptr_t socket, uint8_t* socketAddress, int32_t* socketAddressLen);

PALEXPORT int32_t SystemNative_Listen(intptr_t socket, int32_t backlog);

PALEXPORT int32_t SystemNative_Shutdown(intptr_t socket, int32_t socketShutdown);

PALEXPORT int32_t SystemNative_GetSocketErrorOption(intptr_t socket, int32_t* error);

PALEXPORT int32_t SystemNative_GetSockOpt(
    intptr_t socket, int32_t socketOptionLevel, int32_t socketOptionName, uint8_t* optionValue, int32_t* optionLen);

PALEXPORT int32_t SystemNative_GetRawSockOpt(
    intptr_t socket, int32_t socketOptionLevel, int32_t socketOptionName, uint8_t* optionValue, int32_t* optionLen);

PALEXPORT int32_t SystemNative_SetSockOpt(
    intptr_t socket, int32_t socketOptionLevel, int32_t socketOptionName, uint8_t* optionValue, int32_t optionLen);

PALEXPORT int32_t SystemNative_SetRawSockOpt(
    intptr_t socket, int32_t socketOptionLevel, int32_t socketOptionName, uint8_t* optionValue, int32_t optionLen);

PALEXPORT int32_t SystemNative_Socket(int32_t addressFamily, int32_t socketType, int32_t protocolType, intptr_t* createdSocket);

PALEXPORT int32_t SystemNative_GetSocketType(intptr_t socket, int32_t* addressFamily, int32_t* socketType, int32_t* protocolType, int32_t* isListening);

PALEXPORT int32_t SystemNative_GetAtOutOfBandMark(intptr_t socket, int32_t* available);

PALEXPORT int32_t SystemNative_GetBytesAvailable(intptr_t socket, int32_t* available);

PALEXPORT int32_t SystemNative_CreateSocketEventPort(intptr_t* port);

PALEXPORT int32_t SystemNative_CloseSocketEventPort(intptr_t port);

PALEXPORT int32_t SystemNative_CreateSocketEventBuffer(int32_t count, SocketEvent** buffer);

PALEXPORT int32_t SystemNative_FreeSocketEventBuffer(SocketEvent* buffer);

PALEXPORT int32_t SystemNative_TryChangeSocketEventRegistration(
    intptr_t port, intptr_t socket, int32_t currentEvents, int32_t newEvents, uintptr_t data);

PALEXPORT int32_t SystemNative_WaitForSocketEvents(intptr_t port, SocketEvent* buffer, int32_t* count);

PALEXPORT int32_t SystemNative_PlatformSupportsDualModeIPv4PacketInfo(void);

PALEXPORT char* SystemNative_GetPeerUserName(intptr_t socket);

PALEXPORT void SystemNative_GetDomainSocketSizes(int32_t* pathOffset, int32_t* pathSize, int32_t* addressSize);

PALEXPORT int32_t SystemNative_GetMaximumAddressSize(void);

PALEXPORT int32_t SystemNative_SendFile(intptr_t out_fd, intptr_t in_fd, int64_t offset, int64_t count, int64_t* sent);

PALEXPORT int32_t SystemNative_Disconnect(intptr_t socket);

PALEXPORT uint32_t SystemNative_InterfaceNameToIndex(char* interfaceName);
