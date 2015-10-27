// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once

#include "pal_maphardwaretype.h"
#include "pal_types.h"

struct LinkLayerAddressInfo
{
    uint32_t InterfaceIndex; // The index of the interface to which this address belongs.
    uint8_t AddressBytes[8]; // A pointer to the bytes containing the address.
    uint8_t NumAddressBytes; // The number of bytes actually stored in the address.
    uint8_t __padding;
    NetworkInterfaceType HardwareType;
};

struct IpAddressInfo
{
    uint32_t InterfaceIndex;
    uint8_t AddressBytes[16];
    uint8_t NumAddressBytes;
    uint8_t __padding[3];
};

typedef void (*IPv4AddressFound)(const char* interfaceName, IpAddressInfo* addressInfo, IpAddressInfo* netMaskInfo);
typedef void (*IPv6AddressFound)(const char* interfaceName, IpAddressInfo* info, uint32_t* scopeId);
typedef void (*LinkLayerAddressFound)(const char* interfaceName, LinkLayerAddressInfo* llAddress);
typedef void (*GatewayAddressFound)(IpAddressInfo* addressInfo);

int32_t EnumerateGatewayAddressesForInterface(uint32_t interfaceIndex,
                                              GatewayAddressFound onGatewayFound,
                                              uint16_t addressFamily);
