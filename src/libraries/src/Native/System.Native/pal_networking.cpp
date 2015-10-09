// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_config.h"
#include "pal_networking.h"
#include "pal_utilities.h"

#include <arpa/inet.h>
#include <assert.h>
#include <functional>
#include <netdb.h>
#include <netinet/in.h>
#include <sys/socket.h>
#include <unistd.h>
#include <vector>

const int INET6_ADDRSTRLEN_MANAGED = 65; // The C# code has a longer max string length

static_assert(PAL_HOST_NOT_FOUND == HOST_NOT_FOUND, "");
static_assert(PAL_TRY_AGAIN == TRY_AGAIN, "");
static_assert(PAL_NO_RECOVERY == NO_RECOVERY, "");
static_assert(PAL_NO_DATA == NO_DATA, "");
static_assert(PAL_NO_ADDRESS == NO_ADDRESS, "");
static_assert(sizeof(uint8_t) == sizeof(char), ""); // We make casts from uint8_t to char for OS functions, make sure it's legal

static int IpStringToAddressHelper(const uint8_t* address, const uint8_t* port, bool isIPv6, addrinfo*& info)
{
    assert(address != nullptr);

    addrinfo hint = {
        .ai_family = isIPv6 ? AF_INET6 : AF_INET,
        .ai_flags = AI_NUMERICHOST | AI_NUMERICSERV
    };

    info = nullptr;
    return getaddrinfo(reinterpret_cast<const char*>(address), reinterpret_cast<const char*>(port), &hint, &info);
}

static void ConvertByteArrayToV6SockAddrIn(sockaddr_in6& addr, const uint8_t* buffer, int32_t bufferLength)
{
#if HAVE_IN6_U
    assert(bufferLength == ARRAY_SIZE(addr.sin6_addr.__in6_u.__u6_addr8));
    memcpy(addr.sin6_addr.__in6_u.__u6_addr8, buffer, UnsignedCast(bufferLength));
#else
    assert(bufferLength == ARRAY_SIZE(addr.sin6_addr.__u6_addr.__u6_addr8));
    memcpy(addr.sin6_addr.__u6_addr.__u6_addr8, buffer, UnsignedCast(bufferLength));
#endif

    // Mark that this is INET6
    addr.sin6_family = AF_INET6;
}

static void ConvertV6SockAddrInToByteArray(uint8_t* buffer, int32_t bufferLength, const sockaddr_in6& addr)
{
#if HAVE_IN6_U
    assert(bufferLength == ARRAY_SIZE(addr.sin6_addr.__in6_u.__u6_addr8));
    memcpy(buffer, addr.sin6_addr.__in6_u.__u6_addr8, UnsignedCast(bufferLength));
#else
    assert(bufferLength == ARRAY_SIZE(addr.sin6_addr.__u6_addr.__u6_addr8));
    memcpy(buffer, addr.sin6_addr.__u6_addr.__u6_addr8, UnsignedCast(bufferLength));
#endif
}

static void ConvertByteArrayToSockAddrIn(sockaddr_in& addr, const uint8_t* buffer, int32_t bufferLength)
{
    assert(bufferLength == NUM_BYTES_IN_IPV4_ADDRESS);
    (void)bufferLength; // Silence compiler warnings about unused variables on release mode

    addr.sin_addr.s_addr = *reinterpret_cast<const uint32_t*>(buffer); // The address comes as network byte order
    addr.sin_family = AF_INET;
}

static void ConvertSockAddrInToByteArray(uint8_t* buffer, int32_t bufferLength, const sockaddr_in& addr)
{
    assert(bufferLength == NUM_BYTES_IN_IPV4_ADDRESS);
    (void)bufferLength;  // Silence compiler warnings about unused variables on release mode

    uint32_t* output = reinterpret_cast<uint32_t*>(buffer);
    *output = addr.sin_addr.s_addr; // Send back in network byte order
}

static int32_t ConvertGetAddrInfoAndGetNameInfoErrorsToPal(int32_t error)
{
    switch (error)
    {
        case 0:
            return 0;
        case EAI_AGAIN:
            return PAL_EAI_AGAIN;
        case EAI_BADFLAGS:
            return PAL_EAI_BADFLAGS;
        case EAI_FAIL:
            return PAL_EAI_FAIL;
        case EAI_FAMILY:
            return PAL_EAI_FAMILY;
        case EAI_NONAME:
        case EAI_NODATA:
            return PAL_EAI_NONAME;
    }

    assert(false && "Unknown AddrInfo error flag");
    return -1;
}

extern "C" int32_t
IPv6StringToAddress(const uint8_t* address, const uint8_t* port, uint8_t* buffer, int32_t bufferLength, uint32_t* scope)
{
    assert(buffer != nullptr);
    assert(bufferLength == NUM_BYTES_IN_IPV6_ADDRESS);
    assert(scope != nullptr);

    // Call our helper to do the getaddrinfo call for us; once we have the info, copy what we need
    addrinfo* info;
    int32_t result = IpStringToAddressHelper(address, port, true, info);
    if (result == 0)
    {
        sockaddr_in6* addr = reinterpret_cast<sockaddr_in6*>(info->ai_addr);
        ConvertV6SockAddrInToByteArray(buffer, bufferLength, *addr);
        *scope = addr->sin6_scope_id;

        freeaddrinfo(info);
    }

    return ConvertGetAddrInfoAndGetNameInfoErrorsToPal(result);
}

extern "C" int32_t IPv4StringToAddress(const uint8_t* address, uint8_t* buffer, int32_t bufferLength, uint16_t* port)
{
    assert(buffer != nullptr);
    assert(bufferLength == NUM_BYTES_IN_IPV4_ADDRESS);
    assert(port != nullptr);

    // Call our helper to do the getaddrinfo call for us; once we have the info, copy what we need
    addrinfo* info;
    int32_t result = IpStringToAddressHelper(address, nullptr, false, info);
    if (result == 0)
    {
        sockaddr_in* addr = reinterpret_cast<sockaddr_in*>(info->ai_addr);
        ConvertSockAddrInToByteArray(buffer, bufferLength, *addr);
        *port = addr->sin_port;

        freeaddrinfo(info);
    }

    return ConvertGetAddrInfoAndGetNameInfoErrorsToPal(result);
}

extern "C" int32_t IPAddressToString(const uint8_t* address,
                                     int32_t addressLength,
                                     bool isIPv6,
                                     uint8_t* string,
                                     int32_t stringLength,
                                     uint32_t scope /* = 0*/)
{
    assert(address != nullptr);
    assert((addressLength == NUM_BYTES_IN_IPV6_ADDRESS) || (addressLength == NUM_BYTES_IN_IPV4_ADDRESS));
    assert(string != nullptr);

    // These constants differ per platform so the managed side uses the bigger value; therefore, check that
    // the length is between the two lengths
    assert((stringLength >= INET_ADDRSTRLEN) && (stringLength <= INET6_ADDRSTRLEN_MANAGED));
    (void)addressLength;  // Silence compiler warnings about unused variables on release mode
    (void)INET6_ADDRSTRLEN_MANAGED; // Silence compiler warnings about unused variables on release mode

    int32_t result;
    socklen_t len = UnsignedCast(stringLength);

    if (isIPv6)
    {
        sockaddr_in6 addr = {
            .sin6_scope_id = scope
        };

        ConvertByteArrayToV6SockAddrIn(addr, address, addressLength);
        result = getnameinfo(
            reinterpret_cast<const sockaddr*>(&addr), 
            sizeof(sockaddr_in6), 
            reinterpret_cast<char*>(string), 
            len, 
            nullptr, 
            0, 
            NI_NUMERICHOST);
    }
    else
    {
        sockaddr_in addr = {};
        ConvertByteArrayToSockAddrIn(addr, address, addressLength);
        result = getnameinfo(
            reinterpret_cast<const sockaddr*>(&addr), 
            sizeof(sockaddr_in), 
            reinterpret_cast<char*>(string), 
            len, 
            nullptr, 
            0, 
            NI_NUMERICHOST);
    }

    return ConvertGetAddrInfoAndGetNameInfoErrorsToPal(result);
}

extern "C" int32_t GetHostEntryForName(const uint8_t* address, HostEntry* entry)
{
    if (address == nullptr || entry == nullptr)
    {
        return PAL_EAI_BADARG;
    }

    // Get all address families and the canonical name
    addrinfo hint = {
        .ai_family = AF_UNSPEC,
        .ai_flags = AI_CANONNAME
    };

    addrinfo* info = nullptr;
    int result = getaddrinfo(reinterpret_cast<const char*>(address), nullptr, &hint, &info);
    if (result != 0)
    {
        return ConvertGetAddrInfoAndGetNameInfoErrorsToPal(result);
    }

    *entry = {
        .CanonicalName = nullptr,
        .AddressListHandle = reinterpret_cast<void*>(info),
        .IPAddressCount = 0
    };

    // Find the canonical name for this host (if any) and count the number of IP end points.
    for (addrinfo* ai = info; ai != nullptr; ai = ai->ai_next)
    {
        // If we haven't found a canonical name yet and this addrinfo has one, copy it
        if ((entry->CanonicalName == nullptr) && (ai->ai_canonname != nullptr))
        {
            entry->CanonicalName = reinterpret_cast<uint8_t*>(ai->ai_canonname);
        }

        if (ai->ai_family == AF_INET || ai->ai_family == AF_INET6)
        {
            entry->IPAddressCount++;
        }
    }

    return PAL_EAI_SUCCESS;
}

extern "C" int32_t GetNextIPAddress(void** addressListHandle, IPAddress* endPoint)
{
    if (addressListHandle == nullptr || endPoint == nullptr)
    {
        return PAL_EAI_BADARG;
    }

    auto* ai = reinterpret_cast<addrinfo*>(*addressListHandle);
    for (; ai != nullptr; ai = ai->ai_next)
    {
        switch (ai->ai_family)
        {
            case AF_INET: {
                auto* inetSockAddr = reinterpret_cast<sockaddr_in*>(ai->ai_addr);

                ConvertSockAddrInToByteArray(endPoint->Address, NUM_BYTES_IN_IPV4_ADDRESS, *inetSockAddr);
                endPoint->IsIPv6 = 0;
                break;
            }

            case AF_INET6: {
                auto* inet6SockAddr = reinterpret_cast<sockaddr_in6*>(ai->ai_addr);

                ConvertV6SockAddrInToByteArray(endPoint->Address, NUM_BYTES_IN_IPV6_ADDRESS, *inet6SockAddr);
                endPoint->IsIPv6 = 1;
                endPoint->ScopeId = inet6SockAddr->sin6_scope_id;
                break;
            }

            default:
                // Skip non-IPv4 and non-IPv6 addresses
                continue;
        }

        *addressListHandle = reinterpret_cast<void*>(ai->ai_next);
        return PAL_EAI_SUCCESS;
    }

    return PAL_EAI_NOMORE;
}

extern "C" void FreeHostEntry(HostEntry* entry)
{
    if (entry != nullptr)
    {
        auto* ai = reinterpret_cast<addrinfo*>(entry->AddressListHandle);
        freeaddrinfo(ai);
    }
}

inline int32_t ConvertGetNameInfoFlagsToPal(int32_t flags)
{
    int32_t outFlags = 0;
    if ((flags & NI_NAMEREQD) == NI_NAMEREQD)
    {
        outFlags |= PAL_NI_NAMEREQD;
    }
    if ((flags & NI_NUMERICHOST) == NI_NUMERICHOST)
    {
        outFlags |= PAL_NI_NUMERICHOST;
    }

    return outFlags;
}

extern "C" int32_t GetNameInfo(const uint8_t* address,
                               int32_t addressLength,
                               bool isIPv6,
                               uint8_t* host,
                               int32_t hostLength,
                               uint8_t* service,
                               int32_t serviceLength,
                               int32_t flags)
{
    assert(address != nullptr);
    assert(addressLength > 0);
    assert((host != nullptr) || (service != nullptr));
    assert((hostLength > 0) || (serviceLength > 0));

    int32_t nativeFlags = ConvertGetNameInfoFlagsToPal(flags);
    int32_t result;

    if (isIPv6)
    {
        sockaddr_in6 addr = {};
        ConvertByteArrayToV6SockAddrIn(addr, address, addressLength);
        result = getnameinfo(reinterpret_cast<const sockaddr*>(&addr),
                             sizeof(sockaddr_in6),
                             reinterpret_cast<char*>(host),
                             UnsignedCast(hostLength),
                             reinterpret_cast<char*>(service),
                             UnsignedCast(serviceLength),
                             nativeFlags);
    }
    else
    {
        sockaddr_in addr = {};
        ConvertByteArrayToSockAddrIn(addr, address, addressLength);
        result = getnameinfo(reinterpret_cast<const sockaddr*>(&addr),
                             sizeof(sockaddr_in),
                             reinterpret_cast<char*>(host),
                             UnsignedCast(hostLength),
                             reinterpret_cast<char*>(service),
                             UnsignedCast(serviceLength),
                             nativeFlags);
    }

    return ConvertGetAddrInfoAndGetNameInfoErrorsToPal(result);
}

extern "C" int32_t GetHostName(uint8_t* name, int32_t nameLength)
{
    assert(name != nullptr);
    assert(nameLength > 0);

    size_t unsignedSize = UnsignedCast(nameLength);
    return gethostname(reinterpret_cast<char*>(name), unsignedSize);
}

template <typename TType, typename TField>
static bool IsInBounds(const TType* base, size_t len, const TField* value)
{
    auto* baseAddr = reinterpret_cast<const uint8_t*>(base);
    auto* valueAddr = reinterpret_cast<const uint8_t*>(value);
    return valueAddr >= baseAddr && (valueAddr + sizeof(TField)) <= (baseAddr + len);
}

extern "C" Error GetIPSocketAddressSizes(int32_t* ipv4SocketAddressSize, int32_t* ipv6SocketAddressSize)
{
    if (ipv4SocketAddressSize == nullptr || ipv6SocketAddressSize == nullptr)
    {
        return PAL_EFAULT;
    }

    *ipv4SocketAddressSize = sizeof(sockaddr_in);
    *ipv6SocketAddressSize = sizeof(sockaddr_in6);
    return PAL_SUCCESS;
}

extern "C" Error GetAddressFamily(const uint8_t* socketAddress, int32_t socketAddressLen, int32_t* addressFamily)
{
    if (socketAddress == nullptr || addressFamily == nullptr || socketAddressLen < 0)
    {
        return PAL_EFAULT;
    }

    auto* sockAddr = reinterpret_cast<const sockaddr*>(socketAddress);
    if (!IsInBounds(sockAddr, static_cast<size_t>(socketAddressLen), &sockAddr->sa_family))
    {
        return PAL_EFAULT;
    }

    switch (sockAddr->sa_family)
    {
        case AF_UNSPEC:
            *addressFamily = PAL_AF_UNSPEC;
            return PAL_SUCCESS;

        case AF_UNIX:
            *addressFamily = PAL_AF_UNIX;
            return PAL_SUCCESS;

        case AF_INET:
            *addressFamily = PAL_AF_INET;
            return PAL_SUCCESS;

        case AF_INET6:
            *addressFamily = PAL_AF_INET6;
            return PAL_SUCCESS;

        default:
            return PAL_EAFNOSUPPORT;
    }
}

extern "C" Error SetAddressFamily(uint8_t* socketAddress, int32_t socketAddressLen, int32_t addressFamily)
{
    auto* sockAddr = reinterpret_cast<sockaddr*>(socketAddress);
    if (sockAddr == nullptr || socketAddressLen < 0 || !IsInBounds(sockAddr, static_cast<size_t>(socketAddressLen), &sockAddr->sa_family))
    {
        return PAL_EFAULT;
    }

    switch (addressFamily)
    {
        case PAL_AF_UNSPEC:
            sockAddr->sa_family = AF_UNSPEC;
            return PAL_SUCCESS;

        case PAL_AF_UNIX:
            sockAddr->sa_family = AF_UNIX;
            return PAL_SUCCESS;

        case PAL_AF_INET:
            sockAddr->sa_family = AF_INET;
            return PAL_SUCCESS;

        case PAL_AF_INET6:
            sockAddr->sa_family = AF_INET6;
            return PAL_SUCCESS;

        default:
            return PAL_EAFNOSUPPORT;
    }
}

extern "C" Error GetPort(const uint8_t* socketAddress, int32_t socketAddressLen, uint16_t* port)
{
    if (socketAddress == nullptr)
    {
        return PAL_EFAULT;
    }

    auto* sockAddr = reinterpret_cast<const sockaddr*>(socketAddress);
    if (!IsInBounds(sockAddr, static_cast<size_t>(socketAddressLen), &sockAddr->sa_family))
    {
        return PAL_EFAULT;
    }

    switch (sockAddr->sa_family)
    {
        case AF_INET: {
            if (socketAddressLen < 0 || static_cast<size_t>(socketAddressLen) < sizeof(sockaddr_in))
            {
                return PAL_EFAULT;
            }

            *port = ntohs(reinterpret_cast<const sockaddr_in*>(socketAddress)->sin_port);
            return PAL_SUCCESS;
        }

        case AF_INET6: {
            if (socketAddressLen < 0 || static_cast<size_t>(socketAddressLen) < sizeof(sockaddr_in6))
            {
                return PAL_EFAULT;
            }

            *port = ntohs(reinterpret_cast<const sockaddr_in6*>(socketAddress)->sin6_port);
            return PAL_SUCCESS;
        }

        default:
            return PAL_EAFNOSUPPORT;
    }
}

extern "C" Error SetPort(uint8_t* socketAddress, int32_t socketAddressLen, uint16_t port)
{
    if (socketAddress == nullptr)
    {
        return PAL_EFAULT;
    }

    auto* sockAddr = reinterpret_cast<const sockaddr*>(socketAddress);
    if (!IsInBounds(sockAddr, static_cast<size_t>(socketAddressLen), &sockAddr->sa_family))
    {
        return PAL_EFAULT;
    }

    switch (sockAddr->sa_family)
    {
        case AF_INET: {
            if (socketAddressLen < 0 || static_cast<size_t>(socketAddressLen) < sizeof(sockaddr_in))
            {
                return PAL_EFAULT;
            }

            reinterpret_cast<sockaddr_in*>(socketAddress)->sin_port = htons(port);
            return PAL_SUCCESS;
        }

        case AF_INET6: {
            if (socketAddressLen < 0 || static_cast<size_t>(socketAddressLen) < sizeof(sockaddr_in6))
            {
                return PAL_EFAULT;
            }

            reinterpret_cast<sockaddr_in6*>(socketAddress)->sin6_port = htons(port);
            return PAL_SUCCESS;
        }

        default:
            return PAL_EAFNOSUPPORT;
    }
}

extern "C" Error GetIPv4Address(const uint8_t* socketAddress, int32_t socketAddressLen, uint32_t* address)
{
    if (socketAddress == nullptr || address == nullptr || socketAddressLen < 0 || static_cast<size_t>(socketAddressLen) < sizeof(sockaddr_in))
    {
        return PAL_EFAULT;
    }

    auto* sockAddr = reinterpret_cast<const sockaddr*>(socketAddress);
    if (!IsInBounds(sockAddr, static_cast<size_t>(socketAddressLen), &sockAddr->sa_family))
    {
        return PAL_EFAULT;
    }

    if (sockAddr->sa_family != AF_INET)
    {
        return PAL_EINVAL;
    }

    *address = reinterpret_cast<const sockaddr_in*>(socketAddress)->sin_addr.s_addr;
    return PAL_SUCCESS;
}

extern "C" Error SetIPv4Address(uint8_t* socketAddress, int32_t socketAddressLen, uint32_t address)
{
    if (socketAddress == nullptr || socketAddressLen < 0 || static_cast<size_t>(socketAddressLen) < sizeof(sockaddr_in))
    {
        return PAL_EFAULT;
    }

    auto* sockAddr = reinterpret_cast<sockaddr*>(socketAddress);
    if (!IsInBounds(sockAddr, static_cast<size_t>(socketAddressLen), &sockAddr->sa_family))
    {
        return PAL_EFAULT;
    }

    if (sockAddr->sa_family != AF_INET)
    {
        return PAL_EINVAL;
    }

    auto* inetSockAddr = reinterpret_cast<sockaddr_in*>(sockAddr);

    inetSockAddr->sin_family = AF_INET;
    inetSockAddr->sin_addr.s_addr = address;
    return PAL_SUCCESS;
}

extern "C" Error GetIPv6Address(const uint8_t* socketAddress, int32_t socketAddressLen, uint8_t* address, int32_t addressLen, uint32_t* scopeId)
{
    if (socketAddress == nullptr || address == nullptr || scopeId == nullptr || socketAddressLen < 0 || static_cast<size_t>(socketAddressLen) < sizeof(sockaddr_in6) || addressLen < NUM_BYTES_IN_IPV6_ADDRESS)
    {
        return PAL_EFAULT;
    }

    auto* sockAddr = reinterpret_cast<const sockaddr*>(socketAddress);
    if (!IsInBounds(sockAddr, static_cast<size_t>(socketAddressLen), &sockAddr->sa_family))
    {
        return PAL_EFAULT;
    }

    if (sockAddr->sa_family != AF_INET6)
    {
        return PAL_EINVAL;
    }

    auto* inet6SockAddr = reinterpret_cast<const sockaddr_in6*>(sockAddr);
    ConvertV6SockAddrInToByteArray(address, addressLen, *inet6SockAddr);
    *scopeId = inet6SockAddr->sin6_scope_id;

    return PAL_SUCCESS;
}

extern "C" Error SetIPv6Address(uint8_t* socketAddress, int32_t socketAddressLen, uint8_t* address, int32_t addressLen, uint32_t scopeId)
{
    if (socketAddress == nullptr || address == nullptr || socketAddressLen < 0 || static_cast<size_t>(socketAddressLen) < sizeof(sockaddr_in6) || addressLen < NUM_BYTES_IN_IPV6_ADDRESS)
    {
        return PAL_EFAULT;
    }

    auto* sockAddr = reinterpret_cast<sockaddr*>(socketAddress);
    if (!IsInBounds(sockAddr, static_cast<size_t>(socketAddressLen), &sockAddr->sa_family))
    {
        return PAL_EFAULT;
    }

    if (sockAddr->sa_family != AF_INET6)
    {
        return PAL_EINVAL;
    }

    auto* inet6SockAddr = reinterpret_cast<sockaddr_in6*>(sockAddr);
    ConvertByteArrayToV6SockAddrIn(*inet6SockAddr, address, addressLen);
    inet6SockAddr->sin6_family = AF_INET6;
    inet6SockAddr->sin6_flowinfo = 0;
    inet6SockAddr->sin6_scope_id = scopeId;

    return PAL_SUCCESS;

}
