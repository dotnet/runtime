// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Stub implementations of network statistics functions for WASM (browser) target.

#include "pal_config.h"
#include "pal_types.h"
#include "pal_utilities.h"
#include "pal_networkstatistics.h"

#include <errno.h>
#include <string.h>

int32_t SystemNative_GetTcpGlobalStatistics(TcpGlobalStatistics* retStats)
{
    (void)retStats;
    errno = ENOTSUP;
    return -1; // Not supported
}

int32_t SystemNative_GetIPv4GlobalStatistics(IPv4GlobalStatistics* retStats)
{
    (void)retStats;
    errno = ENOTSUP;
    return -1; // Not supported
}

int32_t SystemNative_GetUdpGlobalStatistics(UdpGlobalStatistics* retStats)
{
    (void)retStats;
    errno = ENOTSUP;
    return -1; // Not supported
}

int32_t SystemNative_GetIcmpv4GlobalStatistics(Icmpv4GlobalStatistics* retStats)
{
    (void)retStats;
    errno = ENOTSUP;
    return -1; // Not supported
}

int32_t SystemNative_GetIcmpv6GlobalStatistics(Icmpv6GlobalStatistics* retStats)
{
    (void)retStats;
    errno = ENOTSUP;
    return -1; // Not supported
}

int32_t SystemNative_GetEstimatedTcpConnectionCount(void)
{
    errno = ENOTSUP;
    return -1;
}

int32_t SystemNative_GetActiveTcpConnectionInfos(NativeTcpConnectionInformation* infos, int32_t* infoCount)
{
    (void)infos;
    (void)infoCount;
    errno = ENOTSUP;
    return -1;
}

int32_t SystemNative_GetEstimatedUdpListenerCount(void)
{
    errno = ENOTSUP;
    return -1;
}

int32_t SystemNative_GetActiveUdpListeners(IPEndPointInfo* infos, int32_t* infoCount)
{
    (void)infos;
    (void)infoCount;
    errno = ENOTSUP;
    return -1;
}

int32_t SystemNative_GetNativeIPInterfaceStatistics(char* interfaceName, NativeIPInterfaceStatistics* retStats)
{
    (void)interfaceName;
    (void)retStats;
    errno = ENOTSUP;
    return -1; // Not supported
}

int32_t SystemNative_GetNumRoutes(void)
{
    errno = ENOTSUP;
    return -1;
}
