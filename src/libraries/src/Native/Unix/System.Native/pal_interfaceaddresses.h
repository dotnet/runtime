// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "pal_compiler.h"

BEGIN_EXTERN_C

#include "pal_maphardwaretype.h"
#include "pal_types.h"

struct LinkLayerAddressInfo
{
    uint32_t InterfaceIndex; // The index of the interface to which this address belongs.
    uint8_t AddressBytes[8]; // A pointer to the bytes containing the address.
    uint8_t NumAddressBytes; // The number of bytes actually stored in the address.
    uint8_t __padding;
    uint16_t HardwareType;
};

struct IpAddressInfo
{
    uint32_t InterfaceIndex;
    uint8_t AddressBytes[16];
    uint8_t NumAddressBytes;
    uint8_t __padding[3];
};

typedef void (*IPv4AddressFound)(const char* interfaceName, struct IpAddressInfo* addressInfo, struct IpAddressInfo* netMaskInfo);
typedef void (*IPv6AddressFound)(const char* interfaceName, struct IpAddressInfo* info, uint32_t* scopeId);
typedef void (*LinkLayerAddressFound)(const char* interfaceName, struct LinkLayerAddressInfo* llAddress);
typedef void (*GatewayAddressFound)(struct IpAddressInfo* addressInfo);

DLLEXPORT  int32_t SystemNative_EnumerateInterfaceAddresses(
    IPv4AddressFound onIpv4Found, IPv6AddressFound onIpv6Found, LinkLayerAddressFound onLinkLayerFound);

#if HAVE_RT_MSGHDR
DLLEXPORT int32_t SystemNative_EnumerateGatewayAddressesForInterface(uint32_t interfaceIndex, GatewayAddressFound onGatewayFound);
#endif

END_EXTERN_C
