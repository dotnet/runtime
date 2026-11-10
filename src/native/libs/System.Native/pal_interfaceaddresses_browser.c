// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Stub implementations of interface address functions for browser target.

#include "pal_config.h"
#include "pal_interfaceaddresses.h"
#include "pal_utilities.h"

#include <errno.h>
#include <string.h>
#include <stdlib.h>

int32_t SystemNative_EnumerateInterfaceAddresses(
    void* context, IPv4AddressFound onIpv4Found, IPv6AddressFound onIpv6Found, LinkLayerAddressFound onLinkLayerFound)
{
    (void)context;
    (void)onIpv4Found;
    (void)onIpv6Found;
    (void)onLinkLayerFound;
    // Return success but don't enumerate any interfaces
    return 0;
}

int32_t SystemNative_GetNetworkInterfaces(int32_t* interfaceCount, NetworkInterfaceInfo** interfaces, int32_t* addressCount, IpAddressInfo** addressList)
{
    if (interfaceCount == NULL || interfaces == NULL || addressCount == NULL || addressList == NULL)
    {
        errno = EFAULT;
        return -1;
    }

    // Return empty lists
    *interfaceCount = 0;
    *interfaces = NULL;
    *addressCount = 0;
    *addressList = NULL;
    return 0;
}

int32_t SystemNative_EnumerateGatewayAddressesForInterface(void* context, uint32_t interfaceIndex, GatewayAddressFound onGatewayFound)
{
    (void)context;
    (void)interfaceIndex;
    (void)onGatewayFound;
    // Return success but don't enumerate any gateways
    return 0;
}
