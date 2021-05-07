// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_maphardwaretype.h"
#include "pal_types.h"

typedef enum
{
    OperationalStatus_Up = 1,
    OperationalStatus_Down = 2,
    OperationalStatus_Unknown = 4,
    OperationalStatus_LowerLayerDown = 7,
} OperationalStatus;

typedef struct
{
    uint32_t InterfaceIndex; // The index of the interface to which this address belongs.
    uint8_t AddressBytes[8]; // A pointer to the bytes containing the address.
    uint8_t NumAddressBytes; // The number of bytes actually stored in the address.
    uint8_t __padding;
    uint16_t HardwareType;
} LinkLayerAddressInfo;

typedef struct
{
    uint32_t InterfaceIndex;
    uint8_t AddressBytes[16];
    uint8_t NumAddressBytes;
    uint8_t PrefixLength;
    uint8_t __padding[2];
} IpAddressInfo;

typedef struct
{
    char Name[16];              // OS Interface name.
    int64_t Speed;              // Link speed for physical interfaces.
    uint32_t InterfaceIndex;    // Interface index.
    int32_t Mtu;                // Interface MTU.
    uint16_t HardwareType;      // Interface mapped from L2 to NetworkInterfaceType.
    uint8_t OperationalState;   // Operational status.
    uint8_t NumAddressBytes;    // The number of bytes actually stored in the address.
    uint8_t AddressBytes[8];    // Link address.
    uint8_t SupportsMulticast;  // Interface supports multicast.
    uint8_t __padding[3];
} NetworkInterfaceInfo;

typedef void (*IPv4AddressFound)(void* context, const char* interfaceName, IpAddressInfo* addressInfo);
typedef void (*IPv6AddressFound)(void* context, const char* interfaceName, IpAddressInfo* info, uint32_t* scopeId);
typedef void (*LinkLayerAddressFound)(void* context, const char* interfaceName, LinkLayerAddressInfo* llAddress);
typedef void (*GatewayAddressFound)(void* context, IpAddressInfo* addressInfo);

PALEXPORT  int32_t SystemNative_EnumerateInterfaceAddresses(
    void* context, IPv4AddressFound onIpv4Found, IPv6AddressFound onIpv6Found, LinkLayerAddressFound onLinkLayerFound);
PALEXPORT int32_t SystemNative_GetNetworkInterfaces(int32_t * interfaceCount, NetworkInterfaceInfo** interfaces, int32_t * addressCount, IpAddressInfo **addressList);

PALEXPORT int32_t SystemNative_EnumerateGatewayAddressesForInterface(void* context, uint32_t interfaceIndex, GatewayAddressFound onGatewayFound);
