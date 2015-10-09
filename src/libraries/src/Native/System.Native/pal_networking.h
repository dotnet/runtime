// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
};

/**
 * Address families recognized by {Get,Set}AddressFamily.
 *
 * NOTE: these values are taken from System.Net.AddressFamily. If you add
 *       new entries, be sure that the values are chosen accordingly.
 */
enum AddressFamily : int32_t
{
    PAL_AF_UNSPEC = 0,  // System.Net.AddressFamily.Unspecified
    PAL_AF_UNIX = 1,    // System.Net.AddressFamily.Unix
    PAL_AF_INET = 2,    // System.Net.AddressFamily.InterNetwork
    PAL_AF_INET6 = 23,  // System.Net.AddressFamily.InterNetworkV6
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
    uint8_t* CanonicalName;  // Canonical Name of the Host
    void* AddressListHandle; // Handle for host socket addresses
    int32_t IPAddressCount; // Number of IP end points in the list
    int32_t Padding;         // Pad out to 8-byte alignment
};

/**
 * Converts string-representations of IP Addresses to
 */
extern "C" int32_t IPv6StringToAddress(const uint8_t* address, const uint8_t* port, uint8_t* buffer, int32_t bufferLength, uint32_t* scope);

extern "C" int32_t IPv4StringToAddress(const uint8_t* address, uint8_t* buffer, int32_t bufferLength, uint16_t* port);

extern "C" int32_t IPAddressToString(const uint8_t* address,
                                     int32_t addressLength,
                                     bool isIPv6,
                                     uint8_t* string,
                                     int32_t stringLength,
                                     uint32_t scope = 0);

extern "C" int32_t GetHostEntryForName(const uint8_t* address, HostEntry* entry);

extern "C" int32_t GetNextIPAddress(void** addressListHandle, IPAddress* endPoint);

extern "C" void FreeHostEntry(HostEntry* entry);

extern "C" int32_t GetNameInfo(const uint8_t* address,
                               int32_t addressLength,
                               bool isIPv6,
                               uint8_t* host,
                               int32_t hostLength,
                               uint8_t* service,
                               int32_t serviceLength,
                               int32_t flags);

extern "C" int32_t GetHostName(uint8_t* name, int32_t nameLength);

extern "C" Error GetIPSocketAddressSizes(int32_t* ipv4SocketAddressSize, int32_t* ipv6SocketAddressSize);

extern "C" Error GetAddressFamily(const uint8_t* socketAddress, int32_t socketAddressLen, int32_t* addressFamily);

extern "C" Error SetAddressFamily(uint8_t* socketAddress, int32_t socketAddressLen, int32_t addressFamily);

extern "C" Error GetPort(const uint8_t* socketAddress, int32_t socketAddressLen, uint16_t* port);

extern "C" Error SetPort(uint8_t* socketAddress, int32_t socketAddressLen, uint16_t port);

extern "C" Error GetIPv4Address(const uint8_t* socketAddress, int32_t socketAddressLen, uint32_t* address);

extern "C" Error SetIPv4Address(uint8_t* socketAddress, int32_t socketAddressLen, uint32_t address);

extern "C" Error GetIPv6Address(const uint8_t* socketAddress, int32_t socketAddressLen, uint8_t* address, int32_t addressLen, uint32_t* scopeId);

extern "C" Error SetIPv6Address(uint8_t* socketAddress, int32_t socketAddressLen, uint8_t* address, int32_t addressLen, uint32_t scopeId);
