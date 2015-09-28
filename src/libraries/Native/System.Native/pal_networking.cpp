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

const int NUM_BYTES_IN_IPV4_ADDRESS = 4;
const int NUM_BYTES_IN_IPV6_ADDRESS = 16;
const int INET6_ADDRSTRLEN_MANAGED = 65; // The C# code has a longer max string length

static_assert(PAL_HOST_NOT_FOUND == HOST_NOT_FOUND, "");
static_assert(PAL_TRY_AGAIN == TRY_AGAIN, "");
static_assert(PAL_NO_RECOVERY == NO_RECOVERY, "");
static_assert(PAL_NO_DATA == NO_DATA, "");
static_assert(PAL_NO_ADDRESS == NO_ADDRESS, "");
static_assert(sizeof(uint8_t) == sizeof(char), ""); // We make casts from uint8_t to char for OS functions, make sure it's legal

static void IpStringToAddressHelper(const uint8_t* address,
                                    const uint8_t* port,
                                    bool isIpV6,
                                    int32_t* err,
                                    const std::function<void(const addrinfo& info)>& lambda)
{
    assert(address != nullptr);
    assert(err != nullptr);

    addrinfo hint = {
        .ai_family = isIpV6 ? AF_INET6 : AF_INET,
        .ai_flags = AI_NUMERICHOST | AI_NUMERICSERV
    };

    addrinfo* info = nullptr;
    int result = getaddrinfo(reinterpret_cast<const char*>(address), reinterpret_cast<const char*>(port), &hint, &info);
    if (result == 0)
    {
        *err = 0;
        lambda(*info);
        freeaddrinfo(info);
    }
    else
    {
        *err = result;
    }
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
            return PAL_EAI_NONAME;
    }

    assert(false && "Unknown AddrInfo error flag");
    return -1;
}

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wpadded"

extern "C" int32_t
Ipv6StringToAddress(const uint8_t* address, const uint8_t* port, uint8_t* buffer, int32_t bufferLength, uint32_t* scope)
{
    assert(buffer != nullptr);
    assert(bufferLength == NUM_BYTES_IN_IPV6_ADDRESS);
    assert(scope != nullptr);

    // Call our helper to do the getaddrinfo and freeaddrinfo calls for us; once we have the info, copy what we need
    int32_t result;
    IpStringToAddressHelper(address,
                            port,
                            true,
                            &result,
                            [buffer, scope, bufferLength](const addrinfo& info)
                            {
                                sockaddr_in6* addr = reinterpret_cast<sockaddr_in6*>(info.ai_addr);
                                ConvertV6SockAddrInToByteArray(buffer, bufferLength, *addr);
                                *scope = addr->sin6_scope_id;
                            });

    return ConvertGetAddrInfoAndGetNameInfoErrorsToPal(result);
}

extern "C" int32_t Ipv4StringToAddress(const uint8_t* address, uint8_t* buffer, int32_t bufferLength, uint16_t* port)
{
    assert(buffer != nullptr);
    assert(bufferLength == NUM_BYTES_IN_IPV4_ADDRESS);
    assert(port != nullptr);

    // Call our helper to do the getaddrinfo and freeaddrinfo calls for us; once we have the info, copy what we need
    int32_t result;
    IpStringToAddressHelper(address,
                            nullptr,
                            false,
                            &result,
                            [buffer, port, bufferLength](const addrinfo& info)
                            {
                                sockaddr_in* addr = reinterpret_cast<sockaddr_in*>(info.ai_addr);
                                ConvertSockAddrInToByteArray(buffer, bufferLength, *addr);
                                *port = addr->sin_port;
                            });

    return ConvertGetAddrInfoAndGetNameInfoErrorsToPal(result);
}

#pragma clang diagnostic pop

extern "C" int32_t IpAddressToString(const uint8_t* address,
                                     int32_t addressLength,
                                     bool isIpv6,
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

    if (isIpv6)
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

extern "C" int32_t GetHostEntriesForName(const uint8_t* address, HostEntry** entry)
{
    assert(address != nullptr);
    assert(entry != nullptr);

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

    // Allocate our data and ensure everything is 0
    *entry = new HostEntry;
    HostEntry* he = *entry;
    memset(he, 0, sizeof(HostEntry));

    // Cache the first so we can walk the linked list again
    addrinfo* first = info;

    // Walk the linked-list of entries and get the IP Addresses
    for (addrinfo* ai = info; ai != nullptr; ai = ai->ai_next)
    {
        // If we haven't found a canonical name yet and this addrinfo has one, copy it
        if ((he->CanonicalName == nullptr) && (ai->ai_canonname != nullptr))
        {
            size_t len = strlen(ai->ai_canonname) + 1; // Name + null
            he->CanonicalName = new uint8_t[len];
            SafeStringCopy(reinterpret_cast<char*>(he->CanonicalName), len, ai->ai_canonname);
        }

        // Skip non-IPv4 and non-IPv6 addresses
        if ((ai->ai_family != AF_INET) && (ai->ai_family != AF_INET6))
        {
            continue;
        }

        // Only increment the count of addresses here; don't try to parse on the first
        // walk since we would (most likely) need to realloc the array, which is more
        // expensive than just incrementing the limited number of IPs twice.
        he->Count++;
    }

    // Now that we have the final count of all IP Addresses for this host name, allocate them
    he->Addresses = new PalIpAddress[he->Count];

    // Walk the addrinfo's that we want and construct the IP Addresses for the managed side
    int i = 0;
    for (addrinfo* ai = first; ai != nullptr; ai = ai->ai_next)
    {
        // Skip non-IPv4 and non-IPv6 addresses
        if ((ai->ai_family != AF_INET) && (ai->ai_family != AF_INET6))
        {
            continue;
        }

        bool isv6 = ai->ai_family == AF_INET6;
        PalIpAddress* ip = &he->Addresses[i];
        ip->IsIpv6 = isv6;
        ip->Count = isv6 ? NUM_BYTES_IN_IPV6_ADDRESS : NUM_BYTES_IN_IPV4_ADDRESS;
        ip->Address = new uint8_t[ip->Count];

        // If this is an IPv6 address then get the addrinfo as a v6 address and
        // copy the 128-bit value to our buffer. If this is an IPv4 address then
        // get the addrinfo as a v4 address and copy the lone 32-bit value to our buffer.
        if (isv6)
        {
            sockaddr_in6* addrin = reinterpret_cast<sockaddr_in6*>(ai->ai_addr);
            ConvertV6SockAddrInToByteArray(ip->Address, ip->Count, *addrin);
        }
        else
        {
            sockaddr_in* addrin = reinterpret_cast<sockaddr_in*>(ai->ai_addr);
            ConvertSockAddrInToByteArray(ip->Address, ip->Count, *addrin);
        }

        i++;
    }

    freeaddrinfo(info);
    return 0;
}

extern "C" void FreeHostEntriesForName(HostEntry* entry)
{
    assert(entry != nullptr);

    for (int i = 0; i < entry->Count; i++)
    {
        delete[] entry->Addresses[i].Address;
    }

    delete[] entry->Addresses;
    delete[] entry->CanonicalName;
    delete entry;
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
                               bool isIpv6,
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

    if (isIpv6)
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
