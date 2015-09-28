// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once

#include "pal_types.h"

/**
 * These error values are different on every platform so make a
 * platform-agnostic version that we convert to and send to managed
 */
enum GetAddrInfoErrorFlags
{
    PAL_EAI_AGAIN = 1,    // Temporary failure in name resolution.
    PAL_EAI_BADFLAGS = 2, // Invalid value for `ai_flags' field.
    PAL_EAI_FAIL = 3,     // Non-recoverable failure in name resolution.
    PAL_EAI_FAMILY = 4,   // 'ai_family' not supported.
    PAL_EAI_NONAME = 5,   // NAME or SERVICE is unknown.
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

struct PalIpAddress
{
    uint8_t* Address;    // Buffer to fit IPv4 or IPv6 address
    int32_t Count;       // Number of bytes in the address buffer
    bool IsIpv6;         // If this is an IPv6 Address or IPv4
    uint8_t reserved[3]; // Padding on 64 bit systems to align struct and not compile with a warning.
};

struct HostEntry
{
    uint8_t* CanonicalName;  // Canonical Name of the Host
    PalIpAddress* Addresses; // List of IP Addresses associated with this host
    int32_t Count;           // Number of IP Addresses associated with this host
    int32_t reserved;        // Padding on 64 bit systems to align struct and not compile with a warning.
};

/**
 * Converts string-representations of IP Addresses to
 */
extern "C" int32_t Ipv6StringToAddress(
    const uint8_t* address, const uint8_t* port, uint8_t* buffer, int32_t bufferLength, uint32_t* scope);
extern "C" int32_t Ipv4StringToAddress(const uint8_t* address, uint8_t* buffer, int32_t bufferLength, uint16_t* port);

extern "C" int32_t IpAddressToString(const uint8_t* address,
                                     int32_t addressLength,
                                     bool isIpv6,
                                     uint8_t* string,
                                     int32_t stringLength,
                                     uint32_t scope = 0);

extern "C" int32_t GetHostEntriesForName(const uint8_t* address, HostEntry** entry);

extern "C" void FreeHostEntriesForName(HostEntry* entry);

extern "C" int32_t GetNameInfo(const uint8_t* address,
                               int32_t addressLength,
                               bool isIpv6,
                               uint8_t* host,
                               int32_t hostLength,
                               uint8_t* service,
                               int32_t serviceLength,
                               int32_t flags);

extern "C" int32_t GetHostName(uint8_t* name, int32_t nameLength);
