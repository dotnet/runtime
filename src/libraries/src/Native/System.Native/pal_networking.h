// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "pal_types.h"
#include "pal_errno.h"

/**
 * These error values are different on every platform so make a
 * platform-agnostic version that we convert to and send to managed
 */
enum GetAddrInfoErrorFlags
{
    PAL_EAI_SUCCESS = 0,  // Success
    PAL_EAI_AGAIN = 1,    // Temporary failure in name resolution.
    PAL_EAI_BADFLAGS = 2, // Invalid value for `ai_flags' field.
    PAL_EAI_FAIL = 3,     // Non-recoverable failure in name resolution.
    PAL_EAI_FAMILY = 4,   // 'ai_family' not supported.
    PAL_EAI_NONAME = 5,   // NAME or SERVICE is unknown.
    PAL_EAI_BADARG = 6,   // One or more input arguments were invalid.
    PAL_EAI_NOMORE = 7,   // No more entries are present in the list.
};

/**
 * Flags to pass to GetNameInfo. These do not match
 * from platform to platform and must be converted
 */
enum GetNameInfoFlags
{
    PAL_NI_NAMEREQD = 0x1,
    PAL_NI_NUMERICHOST = 0x2,
};

/**
 * Error codes from GetHostByName and GetHostByAddr
 */
enum GetHostErrorCodes
{
    PAL_HOST_NOT_FOUND = 1,
    PAL_TRY_AGAIN = 2,
    PAL_NO_RECOVERY = 3,
    PAL_NO_DATA = 4,
    PAL_NO_ADDRESS = PAL_NO_DATA,
    PAL_BAD_ARG = 5,
    PAL_NO_MEM = 6,
};

/**
 * Address families recognized by {Get,Set}AddressFamily.
 *
 * NOTE: these values are taken from System.Net.AddressFamily. If you add
 *       new entries, be sure that the values are chosen accordingly.
 */
enum AddressFamily : int32_t
{
    PAL_AF_UNSPEC = 0, // System.Net.AddressFamily.Unspecified
    PAL_AF_UNIX = 1,   // System.Net.AddressFamily.Unix
    PAL_AF_INET = 2,   // System.Net.AddressFamily.InterNetwork
    PAL_AF_INET6 = 23, // System.Net.AddressFamily.InterNetworkV6
};

/*
 * Socket types.
 *
 * NOTE: these values are taken from System.Net.SocketType.
 */
enum SocketType : int32_t
{
    PAL_SOCK_STREAM = 1,    // System.Net.SocketType.Stream
    PAL_SOCK_DGRAM = 2,     // System.Net.SocketType.Dgram
    PAL_SOCK_RAW = 3,       // System.Net.SocketType.Raw
    PAL_SOCK_RDM = 4,       // System.Net.SocketType.Rdm
    PAL_SOCK_SEQPACKET = 5, // System.Net.SocketType.SeqPacket
};

/*
 * Protocol types.
 *
 * NOTE: these values are taken from System.Net.ProtocolType.
 */
enum ProtocolType : int32_t
{
    PAL_PT_UNSPECIFIED = 0, // System.Net.ProtocolType.Unspecified
    PAL_PT_ICMP = 1,        // System.Net.ProtocolType.Icmp
    PAL_PT_TCP = 6,         // System.Net.ProtocolType.Tcp
    PAL_PT_UDP = 17,        // System.Net.ProtocolType.Udp
    PAL_PT_ICMPV6 = 58,     // System.Net.ProtocolType.IcmpV6
};

enum MulticastOption : int32_t
{
    PAL_MULTICAST_ADD = 0, // IP{,V6}_ADD_MEMBERSHIP
    PAL_MULTICAST_DROP = 1 // IP{,V6}_DROP_MEMBERSHIP
};

/*
 * Socket shutdown modes.
 *
 * NOTE: these values are taken from System.Net.SocketShutdown.
 */
enum SocketShutdown : int32_t
{
    PAL_SHUT_READ = 0,  // SHUT_RD
    PAL_SHUT_WRITE = 1, // SHUT_WR
    PAL_SHUT_BOTH = 2,  // SHUT_RDWR
};

/*
 * Socket option levels.
 *
 * NOTE: these values are taken from System.Net.SocketOptionLevel.
 */
enum SocketOptionLevel : int32_t
{
    PAL_SOL_SOCKET = 0xffff,
    PAL_SOL_IP = 0,
    PAL_SOL_IPV6 = 41,
    PAL_SOL_TCP = 6,
    PAL_SOL_UDP = 17,
};

/*
 * Socket option names.
 *
 * NOTE: these values are taken from System.Net.SocketOptionName. Only values that are known to be usable on all target
 *       platforms are represented here. Unsupported values are present as commented-out entries.
 */
enum SocketOptionName : int32_t
{
    // Names for level PAL_SOL_SOCKET
    PAL_SO_DEBUG = 0x0001,
    PAL_SO_ACCEPTCONN = 0x0002,
    PAL_SO_REUSEADDR = 0x0004,
    PAL_SO_KEEPALIVE = 0x0008,
    PAL_SO_DONTROUTE = 0x0010,
    PAL_SO_BROADCAST = 0x0020,
    // PAL_SO_USELOOPBACK = 0x0040,
    PAL_SO_LINGER = 0x0080,
    PAL_SO_OOBINLINE = 0x0100,
    // PAL_SO_DONTLINGER = ~PAL_SO_LINGER,
    // PAL_SO_EXCLUSIVEADDRUSE = ~PAL_SO_REUSEADDR,
    PAL_SO_SNDBUF = 0x1001,
    PAL_SO_RCVBUF = 0x1002,
    PAL_SO_SNDLOWAT = 0x1003,
    PAL_SO_RCVLOWAT = 0x1004,
    PAL_SO_SNDTIMEO = 0x1005,
    PAL_SO_RCVTIMEO = 0x1006,
    PAL_SO_ERROR = 0x1007,
    PAL_SO_TYPE = 0x1008,
    // PAL_SO_MAXCONN = 0x7fffffff,

    // Names for level PAL_SOL_IP
    PAL_SO_IP_OPTIONS = 1,
    PAL_SO_IP_HDRINCL = 2,
    PAL_SO_IP_TOS = 3,
    PAL_SO_IP_TTL = 4,
    PAL_SO_IP_MULTICAST_IF = 9,
    PAL_SO_IP_MULTICAST_TTL = 10,
    PAL_SO_IP_MULTICAST_LOOP = 11,
    PAL_SO_IP_ADD_MEMBERSHIP = 12,
    PAL_SO_IP_DROP_MEMBERSHIP = 13,
    // PAL_SO_IP_DONTFRAGMENT = 14,
    PAL_SO_IP_ADD_SOURCE_MEMBERSHIP = 15,
    PAL_SO_IP_DROP_SOURCE_MEMBERSHIP = 16,
    PAL_SO_IP_BLOCK_SOURCE = 17,
    PAL_SO_IP_UNBLOCK_SOURCE = 18,
    PAL_SO_IP_PKTINFO = 19,

    // Names for PAL_SOL_IPV6
    PAL_SO_IPV6_HOPLIMIT = 21,
    // PAL_SO_IPV6_PROTECTION_LEVEL = 23,
    PAL_SO_IPV6_V6ONLY = 27,

    // Names for PAL_SOL_TCP
    PAL_SO_TCP_NODELAY = 1,
    // PAL_SO_TCP_BSDURGENT = 2,

    // Names for PAL_SOL_UDP
    // PAL_SO_UDP_NOCHECKSUM = 1,
    // PAL_SO_UDP_CHECKSUM_COVERAGE = 20,
    // PAL_SO_UDP_UPDATEACCEPTCONTEXT = 0x700b,
    // PAL_SO_UDP_UPDATECONNECTCONTEXT = 0x7010,
};

/*
 * Socket flags.
 *
 * NOTE: these values are taken from System.Net.SocketFlags. Only values that are known to be usable on all target
 *       platforms are represented here. Unsupported values are present as commented-out entries.
 */

enum SocketFlags : int32_t
{
    PAL_MSG_OOB = 0x0001,       // SocketFlags.OutOfBand
    PAL_MSG_PEEK = 0x0002,      // SocketFlags.Peek
    PAL_MSG_DONTROUTE = 0x0004, // SocketFlags.DontRoute
    PAL_MSG_TRUNC = 0x0100,     // SocketFlags.Truncated
    PAL_MSG_CTRUNC = 0x0200,    // SocketFlags.ControlDataTruncated
};

/*
 * Socket async events.
 */
enum SocketEvents : int32_t
{
    PAL_SA_NONE = 0x00,
    PAL_SA_READ = 0x01,
    PAL_SA_WRITE = 0x02,
    PAL_SA_READCLOSE = 0x04,
    PAL_SA_CLOSE = 0x08,
    PAL_SA_ERROR = 0x10,
};

/**
 * IP address sizes.
 */
enum
{
    NUM_BYTES_IN_IPV4_ADDRESS = 4,
    NUM_BYTES_IN_IPV6_ADDRESS = 16,
    MAX_IP_ADDRESS_BYTES = 16,
};

struct IPAddress
{
    uint8_t Address[MAX_IP_ADDRESS_BYTES]; // Buffer to fit IPv4 or IPv6 address
    uint32_t IsIPv6;                       // Non-zero if this is an IPv6 endpoint; zero for IPv4.
    uint32_t ScopeId;                      // Scope ID (IPv6 only)
};

struct HostEntry
{
    uint8_t* CanonicalName;  // Canonical name of the host
    uint8_t** Aliases;       // List of aliases for the host
    void* AddressListHandle; // Handle for host socket addresses
    int32_t IPAddressCount;  // Number of IP end points in the list
    int32_t HandleType;      // Indicates the type of the handle. Opaque to managed code.
};

struct IPPacketInformation
{
    IPAddress Address;      // Destination IP address
    int32_t InterfaceIndex; // Interface index
    int32_t Padding;        // Pad out to 8-byte alignment
};

struct IPv4MulticastOption
{
    uint32_t MulticastAddress; // Multicast address
    uint32_t LocalAddress;     // Local address
    int32_t InterfaceIndex;    // Interface index
    int32_t Padding;           // Pad out to 8-byte alignment
};

struct IPv6MulticastOption
{
    IPAddress Address;      // Multicast address
    int32_t InterfaceIndex; // Interface index
    int32_t Padding;        // Pad out to 8-byte alignment
};

struct LingerOption
{
    int32_t OnOff;   // Non-zero to enable linger
    int32_t Seconds; // Number of seconds to linger for
};

// NOTE: the layout of this type is intended to exactly  match the layout of a `struct iovec`. There are
//       assertions in pal_networking.cpp that validate this.
struct IOVector
{
    uint8_t* Base;
    uintptr_t Count;
};

struct MessageHeader
{
    uint8_t* SocketAddress;
    IOVector* IOVectors;
    uint8_t* ControlBuffer;
    int32_t SocketAddressLen;
    int32_t IOVectorCount;
    int32_t ControlBufferLen;
    int32_t Flags;
};
struct SocketEvent
{
    uintptr_t Data;      // User data for this event
    SocketEvents Events; // Event flags
    uint32_t Padding;    // Pad out to 8-byte alignment
};

/**
 * Converts string-representations of IP Addresses to
 */
extern "C" int32_t SystemNative_IPv6StringToAddress(
    const uint8_t* address, const uint8_t* port, uint8_t* buffer, int32_t bufferLength, uint32_t* scope);

extern "C" int32_t SystemNative_IPv4StringToAddress(const uint8_t* address, uint8_t* buffer, int32_t bufferLength, uint16_t* port);

extern "C" int32_t SystemNative_IPAddressToString(const uint8_t* address,
                                     int32_t addressLength,
                                     bool isIPv6,
                                     uint8_t* string,
                                     int32_t stringLength,
                                     uint32_t scope = 0);

extern "C" int32_t SystemNative_GetHostEntryForName(const uint8_t* address, HostEntry* entry);

extern "C" int32_t SystemNative_GetHostByName(const uint8_t* hostname, HostEntry* entry);

extern "C" int32_t SystemNative_GetHostByAddress(const IPAddress* address, HostEntry* entry);

extern "C" int32_t SystemNative_GetNextIPAddress(const HostEntry* entry, void** addressListHandle, IPAddress* endPoint);

extern "C" void SystemNative_FreeHostEntry(HostEntry* entry);

extern "C" int32_t SystemNative_GetNameInfo(const uint8_t* address,
                               int32_t addressLength,
                               bool isIPv6,
                               uint8_t* host,
                               int32_t hostLength,
                               uint8_t* service,
                               int32_t serviceLength,
                               int32_t flags);

extern "C" int32_t SystemNative_GetDomainName(uint8_t* name, int32_t nameLength);

extern "C" int32_t SystemNative_GetHostName(uint8_t* name, int32_t nameLength);

extern "C" Error SystemNative_GetIPSocketAddressSizes(int32_t* ipv4SocketAddressSize, int32_t* ipv6SocketAddressSize);

extern "C" Error SystemNative_GetAddressFamily(const uint8_t* socketAddress, int32_t socketAddressLen, int32_t* addressFamily);

extern "C" Error SystemNative_SetAddressFamily(uint8_t* socketAddress, int32_t socketAddressLen, int32_t addressFamily);

extern "C" Error SystemNative_GetPort(const uint8_t* socketAddress, int32_t socketAddressLen, uint16_t* port);

extern "C" Error SystemNative_SetPort(uint8_t* socketAddress, int32_t socketAddressLen, uint16_t port);

extern "C" Error SystemNative_GetIPv4Address(const uint8_t* socketAddress, int32_t socketAddressLen, uint32_t* address);

extern "C" Error SystemNative_SetIPv4Address(uint8_t* socketAddress, int32_t socketAddressLen, uint32_t address);

extern "C" Error SystemNative_GetIPv6Address(
    const uint8_t* socketAddress, int32_t socketAddressLen, uint8_t* address, int32_t addressLen, uint32_t* scopeId);

extern "C" int32_t SystemNative_GetControlMessageBufferSize(int32_t isIPv4, int32_t isIPv6);

extern "C" int32_t
SystemNative_TryGetIPPacketInformation(MessageHeader* messageHeader, int32_t isIPv4, IPPacketInformation* packetInfo);

extern "C" Error SystemNative_GetIPv4MulticastOption(int32_t socket, int32_t multicastOption, IPv4MulticastOption* option);

extern "C" Error SystemNative_SetIPv4MulticastOption(int32_t socket, int32_t multicastOption, IPv4MulticastOption* option);

extern "C" Error SystemNative_GetIPv6MulticastOption(int32_t socket, int32_t multicastOption, IPv6MulticastOption* option);

extern "C" Error SystemNative_SetIPv6MulticastOption(int32_t socket, int32_t multicastOption, IPv6MulticastOption* option);

extern "C" Error SystemNative_GetLingerOption(int32_t socket, LingerOption* option);

extern "C" Error SystemNative_SetLingerOption(int32_t socket, LingerOption* option);

extern "C" Error SystemNative_SetReceiveTimeout(int32_t socket, int32_t millisecondsTimeout);

extern "C" Error SystemNative_SetSendTimeout(int32_t socket, int32_t millisecondsTimeout);

extern "C" Error SystemNative_ReceiveMessage(int32_t socket, MessageHeader* messageHeader, int32_t flags, int64_t* received);

extern "C" Error SystemNative_SendMessage(int32_t socket, MessageHeader* messageHeader, int32_t flags, int64_t* sent);

extern "C" Error SystemNative_Accept(int32_t socket, uint8_t* socketAddress, int32_t* socketAddressLen, int32_t* acceptedSocket);

extern "C" Error SystemNative_Bind(int32_t socket, uint8_t* socketAddress, int32_t socketAddressLen);

extern "C" Error SystemNative_Connect(int32_t socket, uint8_t* socketAddress, int32_t socketAddressLen);

extern "C" Error SystemNative_GetPeerName(int32_t socket, uint8_t* socketAddress, int32_t* socketAddressLen);

extern "C" Error SystemNative_GetSockName(int32_t socket, uint8_t* socketAddress, int32_t* socketAddressLen);

extern "C" Error SystemNative_Listen(int32_t socket, int32_t backlog);

extern "C" Error SystemNative_Shutdown(int32_t socket, int32_t socketShutdown);

extern "C" Error SystemNative_GetSocketErrorOption(int32_t socket, Error* error);

extern "C" Error SystemNative_GetSockOpt(
    int32_t socket, int32_t socketOptionLevel, int32_t socketOptionName, uint8_t* optionValue, int32_t* optionLen);

extern "C" Error SystemNative_SetSockOpt(
    int32_t socket, int32_t socketOptionLevel, int32_t socketOptionName, uint8_t* optionValue, int32_t optionLen);

extern "C" Error SystemNative_Socket(int32_t addressFamily, int32_t socketType, int32_t protocolType, int32_t* createdSocket);

extern "C" int32_t SystemNative_FdSetSize();

extern "C" Error SystemNative_Select(
    int32_t fdCount, uint32_t* readFdSet, uint32_t* writeFdSet, uint32_t* errorFdSet, int32_t microseconds, int32_t* selected);

extern "C" Error SystemNative_GetBytesAvailable(int32_t socket, int32_t* available);

extern "C" Error SystemNative_CreateSocketEventPort(int32_t* port);

extern "C" Error SystemNative_CloseSocketEventPort(int32_t port);

extern "C" Error SystemNative_CreateSocketEventBuffer(int32_t count, SocketEvent** buffer);

extern "C" Error SystemNative_FreeSocketEventBuffer(SocketEvent* buffer);

extern "C" Error SystemNative_TryChangeSocketEventRegistration(
    int32_t port, int32_t socket, int32_t currentEvents, int32_t newEvents, uintptr_t data);

extern "C" Error SystemNative_WaitForSocketEvents(int32_t port, SocketEvent* buffer, int32_t* count);

extern "C" int32_t SystemNative_PlatformSupportsMultipleConnectAttempts();

extern "C" int32_t SystemNative_PlatformSupportsDualModeIPv4PacketInfo();
