// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_config.h"
#include "pal_networking.h"
#include "pal_io.h"
#include "pal_utilities.h"

#include <stdlib.h>
#include <pthread.h>
#include <arpa/inet.h>
#include <assert.h>
#if HAVE_EPOLL
#include <sys/epoll.h>
#elif HAVE_KQUEUE
#include <sys/types.h>
#include <sys/event.h>
#include <sys/time.h>
#endif
#include <errno.h>
#include <netdb.h>
#include <netinet/in.h>
#include <netinet/tcp.h>
#include <stdlib.h>
#include <string.h>
#include <sys/ioctl.h>
#include <sys/socket.h>
#include <sys/un.h>
#if defined(__APPLE__) && __APPLE__
#include <sys/socketvar.h>
#endif
#include <unistd.h>
#include <vector>
#include <pwd.h>

#if HAVE_KQUEUE
#if KEVENT_HAS_VOID_UDATA
void* GetKeventUdata(uintptr_t udata)
{
    return reinterpret_cast<void*>(udata);
}
uintptr_t GetSocketEventData(void* udata)
{
    return reinterpret_cast<uintptr_t>(udata);
}
#else
intptr_t GetKeventUdata(uintptr_t udata)
{
    return static_cast<intptr_t>(udata);
}
uintptr_t GetSocketEventData(intptr_t udata)
{
    return static_cast<uintptr_t>(udata);
}
#endif
#if KEVENT_REQUIRES_INT_PARAMS
int GetKeventNchanges(int nchanges)
{
    return nchanges;
}
int16_t GetKeventFilter(int16_t filter)
{
    return filter;
}
uint16_t GetKeventFlags(uint16_t flags)
{
    return flags;
}
#else
size_t GetKeventNchanges(int nchanges)
{
    return static_cast<size_t>(nchanges);
}
int16_t GetKeventFilter(uint32_t filter)
{
    return static_cast<int16_t>(filter);
}
uint16_t GetKeventFlags(uint32_t flags)
{
    return static_cast<uint16_t>(flags);
}
#endif
#endif

#if !HAVE_IN_PKTINFO
// On platforms, such as FreeBSD, where in_pktinfo
// is not available, fallback to custom definition
// with required members.
struct in_pktinfo
{
    in_addr ipi_addr;
};
#define IP_PKTINFO IP_RECVDSTADDR
#endif

#if !defined(IPV6_ADD_MEMBERSHIP) && defined(IPV6_JOIN_GROUP)
#define IPV6_ADD_MEMBERSHIP IPV6_JOIN_GROUP
#endif

#if !defined(IPV6_DROP_MEMBERSHIP) && defined(IPV6_LEAVE_GROUP)
#define IPV6_DROP_MEMBERSHIP IPV6_LEAVE_GROUP
#endif

enum
{
#if defined(__APPLE__) && __APPLE__
    LINGER_OPTION_NAME = SO_LINGER_SEC
#else
    LINGER_OPTION_NAME = SO_LINGER,
#endif
};

enum
{
    HOST_ENTRY_HANDLE_ADDRINFO = 1,
    HOST_ENTRY_HANDLE_HOSTENT = 2,
};

enum
{
    INET6_ADDRSTRLEN_MANAGED = 65 // Managed code has a longer max IPv6 string length
};

static_assert(PAL_HOST_NOT_FOUND == HOST_NOT_FOUND, "");
static_assert(PAL_TRY_AGAIN == TRY_AGAIN, "");
static_assert(PAL_NO_RECOVERY == NO_RECOVERY, "");
static_assert(PAL_NO_DATA == NO_DATA, "");
static_assert(PAL_NO_ADDRESS == NO_ADDRESS, "");
static_assert(sizeof(uint8_t) == sizeof(char), ""); // We make casts from uint8_t to char so make sure it's legal

// NOTE: clang has trouble with offsetof nested inside of static_assert. Instead, store
//       the necessary field offsets in constants.
const int OffsetOfIOVectorBase = offsetof(IOVector, Base);
const int OffsetOfIOVectorCount = offsetof(IOVector, Count);
const int OffsetOfIovecBase = offsetof(iovec, iov_base);
const int OffsetOfIovecLen = offsetof(iovec, iov_len);

// We require that IOVector have the same layout as iovec.
static_assert(sizeof(IOVector) == sizeof(iovec), "");
static_assert(sizeof(decltype(IOVector::Base)) == sizeof(decltype(iovec::iov_base)), "");
static_assert(OffsetOfIOVectorBase == OffsetOfIovecBase, "");
static_assert(sizeof(decltype(IOVector::Count)) == sizeof(decltype(iovec::iov_len)), "");
static_assert(OffsetOfIOVectorCount == OffsetOfIovecLen, "");

template <typename T>
constexpr T Min(T left, T right)
{
    return left < right ? left : right;
}

template <typename T>
constexpr T Max(T left, T right)
{
    return left > right ? left : right;
}

static void ConvertByteArrayToIn6Addr(in6_addr& addr, const uint8_t* buffer, int32_t bufferLength)
{
#if HAVE_IN6_U
    assert(bufferLength == ARRAY_SIZE(addr.__in6_u.__u6_addr8));
    memcpy(addr.__in6_u.__u6_addr8, buffer, UnsignedCast(bufferLength));
#elif HAVE_U6_ADDR
    assert(bufferLength == ARRAY_SIZE(addr.__u6_addr.__u6_addr8));
    memcpy(addr.__u6_addr.__u6_addr8, buffer, UnsignedCast(bufferLength));
#else
    assert(bufferLength == ARRAY_SIZE(addr.s6_addr));
    memcpy(addr.s6_addr, buffer, UnsignedCast(bufferLength));
#endif
}

static void ConvertIn6AddrToByteArray(uint8_t* buffer, int32_t bufferLength, const in6_addr& addr)
{
#if HAVE_IN6_U
    assert(bufferLength == ARRAY_SIZE(addr.__in6_u.__u6_addr8));
    memcpy(buffer, addr.__in6_u.__u6_addr8, UnsignedCast(bufferLength));
#elif HAVE_U6_ADDR
    assert(bufferLength == ARRAY_SIZE(addr.__u6_addr.__u6_addr8));
    memcpy(buffer, addr.__u6_addr.__u6_addr8, UnsignedCast(bufferLength));
#else
    assert(bufferLength == ARRAY_SIZE(addr.s6_addr));
    memcpy(buffer, addr.s6_addr, UnsignedCast(bufferLength));
#endif
}

static void ConvertByteArrayToSockAddrIn6(sockaddr_in6& addr, const uint8_t* buffer, int32_t bufferLength)
{
    ConvertByteArrayToIn6Addr(addr.sin6_addr, buffer, bufferLength);

    // Mark that this is INET6
    addr.sin6_family = AF_INET6;
}

static void ConvertByteArrayToInAddr(in_addr& addr, const uint8_t* buffer, int32_t bufferLength)
{
    assert(bufferLength == NUM_BYTES_IN_IPV4_ADDRESS);
    (void)bufferLength; // Silence compiler warnings about unused variables on release mode

    addr.s_addr = *reinterpret_cast<const uint32_t*>(buffer); // Send back in network byte order.
}

static void ConvertInAddrToByteArray(uint8_t* buffer, int32_t bufferLength, const in_addr& addr)
{
    assert(bufferLength == NUM_BYTES_IN_IPV4_ADDRESS);
    (void)bufferLength; // Silence compiler warnings about unused variables on release mode

    *reinterpret_cast<uint32_t*>(buffer) = addr.s_addr; // Send back in network byte order.
}

static void ConvertByteArrayToSockAddrIn(sockaddr_in& addr, const uint8_t* buffer, int32_t bufferLength)
{
    ConvertByteArrayToInAddr(addr.sin_addr, buffer, bufferLength);

    addr.sin_family = AF_INET;
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
#ifdef EAI_FAIL
        case EAI_FAIL:
            return PAL_EAI_FAIL;
#endif
        case EAI_FAMILY:
            return PAL_EAI_FAMILY;
        case EAI_NONAME:
#ifdef EAI_NODATA
        case EAI_NODATA:
#endif
            return PAL_EAI_NONAME;
    }

    assert(false && "Unknown AddrInfo error flag");
    return -1;
}

extern "C" int32_t
SystemNative_IPv6StringToAddress(const uint8_t* address, const uint8_t* port, uint8_t* buffer, int32_t bufferLength, uint32_t* scope)
{
    assert(buffer != nullptr);
    assert(bufferLength == NUM_BYTES_IN_IPV6_ADDRESS);
    assert(scope != nullptr);
    assert(address != nullptr);

    addrinfo hint;
    memset(&hint, 0, sizeof(addrinfo));
    hint.ai_family = AF_INET6;
    hint.ai_flags = AI_NUMERICHOST | AI_NUMERICSERV;

    addrinfo* info = nullptr;
    int32_t result = getaddrinfo(reinterpret_cast<const char*>(address), reinterpret_cast<const char*>(port), &hint, &info);
    if (result == 0)
    {
        sockaddr_in6* addr = reinterpret_cast<sockaddr_in6*>(info->ai_addr);
        ConvertIn6AddrToByteArray(buffer, bufferLength, addr->sin6_addr);
        *scope = addr->sin6_scope_id;

        freeaddrinfo(info);
    }

    return ConvertGetAddrInfoAndGetNameInfoErrorsToPal(result);
}

extern "C" int32_t SystemNative_IPv4StringToAddress(const uint8_t* address, uint8_t* buffer, int32_t bufferLength, uint16_t* port)
{
    assert(buffer != nullptr);
    assert(bufferLength == NUM_BYTES_IN_IPV4_ADDRESS);
    assert(port != nullptr);
    assert(address != nullptr);

    in_addr inaddr;
    int32_t result = inet_aton(reinterpret_cast<const char*>(address), &inaddr);
    if (result == 0)
    {
        return PAL_EAI_NONAME;
    }

    ConvertInAddrToByteArray(buffer, bufferLength, inaddr);
    *port = 0; // callers expect this to always be zero

    return PAL_EAI_SUCCESS;
}

static void AppendScopeIfNecessary(uint8_t* string, int32_t stringLength, uint32_t scope)
{
    assert(scope != 0);

    // Find the scope ID, if it exists
    int i;
    for (i = 0; i < stringLength && string[i] != '\0'; i++)
    {
        if (string[i] == '%')
        {
            // Found a scope ID. Assume it's correct and return.
            return;
        }
    }

    auto capacity = static_cast<size_t>(stringLength - i);
    int n = snprintf(reinterpret_cast<char*>(&string[i]), capacity, "%%%d", scope);
    assert(static_cast<size_t>(n) < capacity);
    (void)n; // Silence an unused variable warning in release mode
}

extern "C" int32_t SystemNative_IPAddressToString(
    const uint8_t* address, int32_t addressLength, bool isIPv6, uint8_t* string, int32_t stringLength, uint32_t scope)
{
    assert(address != nullptr);
    assert((addressLength == NUM_BYTES_IN_IPV6_ADDRESS) || (addressLength == NUM_BYTES_IN_IPV4_ADDRESS));
    assert(string != nullptr);

    // These constants differ per platform so the managed side uses the bigger value; therefore, check that
    // the length is between the two lengths
    assert((stringLength >= INET_ADDRSTRLEN) && (stringLength <= INET6_ADDRSTRLEN_MANAGED));

    socklen_t len = UnsignedCast(stringLength);

    sockaddr_in inAddr;
    sockaddr_in6 in6Addr;
    const sockaddr* addr;
    socklen_t addrLen;

    if (!isIPv6)
    {
        ConvertByteArrayToSockAddrIn(inAddr, address, addressLength);
        addr = reinterpret_cast<const sockaddr*>(&inAddr);
        addrLen = sizeof(inAddr);
    }
    else
    {
        in6Addr.sin6_scope_id = scope;
        ConvertByteArrayToSockAddrIn6(in6Addr, address, addressLength);
        addr = reinterpret_cast<const sockaddr*>(&in6Addr);
        addrLen = sizeof(in6Addr);
    }

    int result = getnameinfo(addr, addrLen, reinterpret_cast<char*>(string), len, nullptr, 0, NI_NUMERICHOST);
    if (result != 0)
    {
        return ConvertGetAddrInfoAndGetNameInfoErrorsToPal(result);
    }

    // Some platforms do not append unknown scope IDs, but the managed code wants this behavior.
    if (isIPv6 && scope != 0)
    {
        AppendScopeIfNecessary(string, stringLength, scope);
    }

    return 0;
}

extern "C" int32_t SystemNative_GetHostEntryForName(const uint8_t* address, HostEntry* entry)
{
    if (address == nullptr || entry == nullptr)
    {
        return PAL_EAI_BADARG;
    }

    // Get all address families and the canonical name
    addrinfo hint;
    memset(&hint, 0, sizeof(addrinfo));
    hint.ai_family = AF_UNSPEC;
    hint.ai_flags = AI_CANONNAME;

    addrinfo* info = nullptr;
    int result = getaddrinfo(reinterpret_cast<const char*>(address), nullptr, &hint, &info);
    if (result != 0)
    {
        return ConvertGetAddrInfoAndGetNameInfoErrorsToPal(result);
    }

    *entry = {.CanonicalName = nullptr,
              .Aliases = nullptr,
              .AddressListHandle = reinterpret_cast<void*>(info),
              .IPAddressCount = 0,
              .HandleType = HOST_ENTRY_HANDLE_ADDRINFO};

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

static int ConvertGetHostErrorPlatformToPal(int error)
{
    switch (error)
    {
        case HOST_NOT_FOUND:
            return PAL_HOST_NOT_FOUND;

        case TRY_AGAIN:
            return PAL_TRY_AGAIN;

        case NO_RECOVERY:
            return PAL_NO_RECOVERY;

        case NO_DATA:
            return PAL_NO_DATA;

        default:
            assert(false && "Unknown gethostbyname/gethostbyaddr error code");
            return PAL_HOST_NOT_FOUND;
    }
}

static void ConvertHostEntPlatformToPal(HostEntry& hostEntry, hostent& entry)
{
    hostEntry = {.CanonicalName = reinterpret_cast<uint8_t*>(entry.h_name),
                 .Aliases = reinterpret_cast<uint8_t**>(entry.h_aliases),
                 .AddressListHandle = reinterpret_cast<void*>(&entry),
                 .IPAddressCount = 0,
                 .HandleType = HOST_ENTRY_HANDLE_HOSTENT};

    for (int i = 0; entry.h_addr_list[i] != nullptr; i++)
    {
        hostEntry.IPAddressCount++;
    }
}

#if !HAVE_THREAD_SAFE_GETHOSTBYNAME_AND_GETHOSTBYADDR
#if !HAVE_GETHOSTBYNAME_R
static int copy_hostent(struct hostent* from, struct hostent* to,
                        char* buffer, size_t buflen)
{
    // FIXME: the implementation done for this function in https://github.com/dotnet/corefx/commit/6a99b74
    //        requires testing when managed assemblies are built and tested on NetBSD. Until that time,
    //        return an error code.
    (void)from;   // unused arg
    (void)to;     // unused arg
    (void)buffer; // unused arg
    (void)buflen; // unused arg
    return ENOSYS;
}

/*
Note: we're assuming that all access to these functions are going through these shims on the platforms, which do not provide
      thread-safe functions to get host name or address. If that is not the case (which is very likely) race condition is
      possible, for instance; if other libs (such as libcurl) call gethostby[name/addr] simultaneously.
*/
static pthread_mutex_t lock_hostbyx_mutex = PTHREAD_MUTEX_INITIALIZER;

static int gethostbyname_r(char const* hostname, struct hostent* result,
                           char* buffer, size_t buflen, hostent** entry, int* error)
{
    assert(hostname != nullptr);
    assert(result != nullptr);
    assert(buffer != nullptr);
    assert(entry != nullptr);
    assert(error != nullptr);

    if (hostname == nullptr || entry == nullptr || error == nullptr || buffer == nullptr || result == nullptr)
    {
        if (error != nullptr)
        {
            *error = PAL_BAD_ARG;
        }

        return PAL_BAD_ARG;
    }

    pthread_mutex_lock(&lock_hostbyx_mutex);

    *entry = gethostbyname(hostname);
    if ((!(*entry)) || ((*entry)->h_addrtype != AF_INET) || ((*entry)->h_length != 4))
    {
        *error = h_errno;
        *entry = nullptr;
    }
    else
    {
        h_errno = copy_hostent(*entry, result, buffer, buflen);
        *entry = (h_errno == 0) ? result : nullptr;
    }

    pthread_mutex_unlock(&lock_hostbyx_mutex);

    return h_errno;
}

static int gethostbyaddr_r(const uint8_t* addr, const socklen_t len, int type, struct hostent* result,
                           char* buffer, size_t buflen, hostent** entry, int* error)
{
    assert(addr != nullptr);
    assert(result != nullptr);
    assert(buffer != nullptr);
    assert(entry != nullptr);
    assert(error != nullptr);

    if (addr == nullptr || entry == nullptr || buffer == nullptr || result == nullptr)
    {
        if (error != nullptr)
        {
            *error = PAL_BAD_ARG;
        }

        return PAL_BAD_ARG;
    }

    pthread_mutex_lock(&lock_hostbyx_mutex);

    *entry = gethostbyaddr(reinterpret_cast<const char*>(addr), static_cast<unsigned int>(len), type);
    if ((!(*entry)) || ((*entry)->h_addrtype != AF_INET) || ((*entry)->h_length != 4))
    {
        *error = h_errno;
        *entry = nullptr;
    }
    else
    {
        h_errno = copy_hostent(*entry, result, buffer, buflen);
        *entry = (h_errno == 0) ? result : nullptr;
    }

    pthread_mutex_unlock(&lock_hostbyx_mutex);

    return h_errno;
}
#undef HAVE_GETHOSTBYNAME_R
#undef HAVE_GETHOSTBYADDR_R
#define HAVE_GETHOSTBYNAME_R 1
#define HAVE_GETHOSTBYADDR_R 1
#endif /* !HAVE_GETHOSTBYNAME_R */

#if HAVE_GETHOSTBYNAME_R
static int GetHostByNameHelper(const uint8_t* hostname, hostent** entry)
{
    assert(hostname != nullptr);
    assert(entry != nullptr);

    size_t scratchLen = 512;

    for (;;)
    {
        uint8_t* buffer = reinterpret_cast<uint8_t*>(malloc(sizeof(hostent) + scratchLen));
        if (buffer == nullptr)
        {
            return PAL_NO_MEM;
        }

        hostent* result = reinterpret_cast<hostent*>(buffer);
        char* scratch = reinterpret_cast<char*>(&buffer[sizeof(hostent)]);

        int getHostErrno;
        int err =
            gethostbyname_r(reinterpret_cast<const char*>(hostname), result, scratch, scratchLen, entry, &getHostErrno);
        switch (err)
        {
            case 0:
                *entry = result;
                return 0;

            case ERANGE:
                free(buffer);
                scratchLen *= 2;
                break;

            default:
                free(buffer);
                *entry = nullptr;
                return getHostErrno;
        }
    }
}
#endif /* HAVE_GETHOSTBYNAME_R */
#endif /* !HAVE_THREAD_SAFE_GETHOSTBYNAME_AND_GETHOSTBYADDR */

extern "C" int32_t SystemNative_GetHostByName(const uint8_t* hostname, HostEntry* entry)
{
    if (hostname == nullptr || entry == nullptr)
    {
        return PAL_BAD_ARG;
    }

    hostent* hostEntry = nullptr;
    int error = 0;

#if HAVE_THREAD_SAFE_GETHOSTBYNAME_AND_GETHOSTBYADDR
    hostEntry = gethostbyname(reinterpret_cast<const char*>(hostname));
    error = h_errno;
#elif HAVE_GETHOSTBYNAME_R
    error = GetHostByNameHelper(hostname, &hostEntry);
#else
#error Platform does not provide thread-safe gethostbyname
#endif

    if (hostEntry == nullptr)
    {
        return ConvertGetHostErrorPlatformToPal(error);
    }

    ConvertHostEntPlatformToPal(*entry, *hostEntry);
    return PAL_SUCCESS;
}

#if !HAVE_THREAD_SAFE_GETHOSTBYNAME_AND_GETHOSTBYADDR && HAVE_GETHOSTBYADDR_R
static int GetHostByAddrHelper(const uint8_t* addr, const socklen_t addrLen, int type, hostent** entry)
{
    assert(addr != nullptr);
    assert(addrLen >= 0);
    assert(entry != nullptr);

    size_t scratchLen = 512;

    for (;;)
    {
        uint8_t* buffer = reinterpret_cast<uint8_t*>(malloc(sizeof(hostent) + scratchLen));
        if (buffer == nullptr)
        {
            return PAL_NO_MEM;
        }

        hostent* result = reinterpret_cast<hostent*>(buffer);
        char* scratch = reinterpret_cast<char*>(&buffer[sizeof(hostent)]);

        int getHostErrno;
        int err = gethostbyaddr_r(addr, addrLen, type, result, scratch, scratchLen, entry, &getHostErrno);
        switch (err)
        {
            case 0:
                *entry = result;
                return 0;

            case ERANGE:
                free(buffer);
                scratchLen *= 2;
                break;

            default:
                free(buffer);
                *entry = nullptr;
                return getHostErrno;
        }
    }
}
#endif /* !HAVE_THREAD_SAFE_GETHOSTBYNAME_AND_GETHOSTBYADDR && HAVE_GETHOSTBYADDR_R */

extern "C" int32_t SystemNative_GetHostByAddress(const IPAddress* address, HostEntry* entry)
{
    if (address == nullptr || entry == nullptr)
    {
        return PAL_BAD_ARG;
    }

    uint8_t* addr = nullptr;
    socklen_t addrLen = 0;
    int type = AF_UNSPEC;

    in_addr inAddr = {};
    in6_addr in6Addr = {};

    if (!address->IsIPv6)
    {
        ConvertByteArrayToInAddr(inAddr, address->Address, NUM_BYTES_IN_IPV4_ADDRESS);
        addr = reinterpret_cast<uint8_t*>(&inAddr);
        addrLen = sizeof(inAddr);
        type = AF_INET;
    }
    else
    {
        ConvertByteArrayToIn6Addr(in6Addr, address->Address, NUM_BYTES_IN_IPV6_ADDRESS);
        addr = reinterpret_cast<uint8_t*>(&in6Addr);
        addrLen = sizeof(in6Addr);
        type = AF_INET6;
    }

    hostent* hostEntry = nullptr;
    int error = 0;

#if HAVE_THREAD_SAFE_GETHOSTBYNAME_AND_GETHOSTBYADDR
    hostEntry = gethostbyaddr(addr, addrLen, type);
    error = h_errno;
#elif HAVE_GETHOSTBYADDR_R
    error = GetHostByAddrHelper(addr, addrLen, type, &hostEntry);
#else
#error Platform does not provide thread-safe gethostbyname
#endif

    if (hostEntry == nullptr)
    {
        return ConvertGetHostErrorPlatformToPal(error);
    }

    ConvertHostEntPlatformToPal(*entry, *hostEntry);
    return PAL_SUCCESS;
}

static int32_t GetNextIPAddressFromAddrInfo(addrinfo** info, IPAddress* endPoint)
{
    assert(info != nullptr);
    assert(endPoint != nullptr);

    for (addrinfo* ai = *info; ai != nullptr; ai = ai->ai_next)
    {
        switch (ai->ai_family)
        {
            case AF_INET:
            {
                auto* inetSockAddr = reinterpret_cast<sockaddr_in*>(ai->ai_addr);

                ConvertInAddrToByteArray(endPoint->Address, NUM_BYTES_IN_IPV4_ADDRESS, inetSockAddr->sin_addr);
                endPoint->IsIPv6 = 0;
                break;
            }

            case AF_INET6:
            {
                auto* inet6SockAddr = reinterpret_cast<sockaddr_in6*>(ai->ai_addr);

                ConvertIn6AddrToByteArray(endPoint->Address, NUM_BYTES_IN_IPV6_ADDRESS, inet6SockAddr->sin6_addr);
                endPoint->IsIPv6 = 1;
                endPoint->ScopeId = inet6SockAddr->sin6_scope_id;
                break;
            }

            default:
                // Skip non-IPv4 and non-IPv6 addresses
                continue;
        }

        *info = ai->ai_next;
        return PAL_EAI_SUCCESS;
    }

    return PAL_EAI_NOMORE;
}

static int32_t GetNextIPAddressFromHostEnt(hostent** hostEntry, IPAddress* address)
{
    assert(hostEntry != nullptr);
    assert(address != nullptr);

    hostent* entry = *hostEntry;
    if (*entry->h_addr_list == nullptr)
    {
        return PAL_EAI_NOMORE;
    }

    switch (entry->h_addrtype)
    {
        case AF_INET:
        {
            auto* inAddr = reinterpret_cast<in_addr*>(entry->h_addr_list[0]);

            ConvertInAddrToByteArray(address->Address, NUM_BYTES_IN_IPV4_ADDRESS, *inAddr);
            address->IsIPv6 = 0;
            break;
        }

        case AF_INET6:
        {
            auto* in6Addr = reinterpret_cast<in6_addr*>(entry->h_addr_list[0]);

            ConvertIn6AddrToByteArray(address->Address, NUM_BYTES_IN_IPV6_ADDRESS, *in6Addr);
            address->IsIPv6 = 1;
            address->ScopeId = 0;
            break;
        }

        default:
            return PAL_EAI_NOMORE;
    }

    entry->h_addr_list = &entry->h_addr_list[1];
    return PAL_EAI_SUCCESS;
}

extern "C" int32_t SystemNative_GetNextIPAddress(const HostEntry* hostEntry, void** addressListHandle, IPAddress* endPoint)
{
    if (hostEntry == nullptr || addressListHandle == nullptr || endPoint == nullptr)
    {
        return PAL_EAI_BADARG;
    }

    switch (hostEntry->HandleType)
    {
        case HOST_ENTRY_HANDLE_ADDRINFO:
            return GetNextIPAddressFromAddrInfo(reinterpret_cast<addrinfo**>(addressListHandle), endPoint);

        case HOST_ENTRY_HANDLE_HOSTENT:
            return GetNextIPAddressFromHostEnt(reinterpret_cast<hostent**>(addressListHandle), endPoint);

        default:
            return PAL_EAI_BADARG;
    }
}

extern "C" void SystemNative_FreeHostEntry(HostEntry* entry)
{
    if (entry != nullptr)
    {
        switch (entry->HandleType)
        {
            case HOST_ENTRY_HANDLE_ADDRINFO:
            {
                auto* ai = reinterpret_cast<addrinfo*>(entry->AddressListHandle);
                freeaddrinfo(ai);
                break;
            }

            case HOST_ENTRY_HANDLE_HOSTENT:
            {
#if !HAVE_THREAD_SAFE_GETHOSTBYNAME_AND_GETHOSTBYADDR
                free(entry->AddressListHandle);
#endif
                break;
            }

            default:
                break;
        }
    }
}

inline int32_t ConvertGetNameInfoFlagsToNative(int32_t flags)
{
    int32_t outFlags = 0;
    if ((flags & PAL_NI_NAMEREQD) == PAL_NI_NAMEREQD)
    {
        outFlags |= NI_NAMEREQD;
    }
    if ((flags & PAL_NI_NUMERICHOST) == PAL_NI_NUMERICHOST)
    {
        outFlags |= NI_NUMERICHOST;
    }

    return outFlags;
}

extern "C" int32_t SystemNative_GetNameInfo(const uint8_t* address,
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

    int32_t nativeFlags = ConvertGetNameInfoFlagsToNative(flags);
    int32_t result;

    if (isIPv6)
    {
        sockaddr_in6 addr = {};
        ConvertByteArrayToSockAddrIn6(addr, address, addressLength);
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

extern "C" int32_t SystemNative_GetDomainName(uint8_t* name, int32_t nameLength)
{
    assert(name != nullptr);
    assert(nameLength > 0);

#if HAVE_GETDOMAINNAME_SIZET
    size_t namelen = UnsignedCast(nameLength);
#else
    int namelen = nameLength;
#endif

    return getdomainname(reinterpret_cast<char*>(name), namelen);
}

extern "C" int32_t SystemNative_GetHostName(uint8_t* name, int32_t nameLength)
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

extern "C" Error SystemNative_GetIPSocketAddressSizes(int32_t* ipv4SocketAddressSize, int32_t* ipv6SocketAddressSize)
{
    if (ipv4SocketAddressSize == nullptr || ipv6SocketAddressSize == nullptr)
    {
        return PAL_EFAULT;
    }

    *ipv4SocketAddressSize = sizeof(sockaddr_in);
    *ipv6SocketAddressSize = sizeof(sockaddr_in6);
    return PAL_SUCCESS;
}

static bool TryConvertAddressFamilyPlatformToPal(sa_family_t platformAddressFamily, int32_t* palAddressFamily)
{
    assert(palAddressFamily != nullptr);

    switch (platformAddressFamily)
    {
        case AF_UNSPEC:
            *palAddressFamily = PAL_AF_UNSPEC;
            return true;

        case AF_UNIX:
            *palAddressFamily = PAL_AF_UNIX;
            return true;

        case AF_INET:
            *palAddressFamily = PAL_AF_INET;
            return true;

        case AF_INET6:
            *palAddressFamily = PAL_AF_INET6;
            return true;

        default:
            *palAddressFamily = platformAddressFamily;
            return false;
    }
}

static bool TryConvertAddressFamilyPalToPlatform(int32_t palAddressFamily, sa_family_t* platformAddressFamily)
{
    assert(platformAddressFamily != nullptr);

    switch (palAddressFamily)
    {
        case PAL_AF_UNSPEC:
            *platformAddressFamily = AF_UNSPEC;
            return true;

        case PAL_AF_UNIX:
            *platformAddressFamily = AF_UNIX;
            return true;

        case PAL_AF_INET:
            *platformAddressFamily = AF_INET;
            return true;

        case PAL_AF_INET6:
            *platformAddressFamily = AF_INET6;
            return true;

        default:
            *platformAddressFamily = static_cast<sa_family_t>(palAddressFamily);
            return false;
    }
}

extern "C" Error SystemNative_GetAddressFamily(const uint8_t* socketAddress, int32_t socketAddressLen, int32_t* addressFamily)
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

    if (!TryConvertAddressFamilyPlatformToPal(sockAddr->sa_family, addressFamily))
    {
        return PAL_EAFNOSUPPORT;
    }

    return PAL_SUCCESS;
}

extern "C" Error SystemNative_SetAddressFamily(uint8_t* socketAddress, int32_t socketAddressLen, int32_t addressFamily)
{
    auto* sockAddr = reinterpret_cast<sockaddr*>(socketAddress);
    if (sockAddr == nullptr || socketAddressLen < 0 ||
        !IsInBounds(sockAddr, static_cast<size_t>(socketAddressLen), &sockAddr->sa_family))
    {
        return PAL_EFAULT;
    }

    if (!TryConvertAddressFamilyPalToPlatform(addressFamily, &sockAddr->sa_family))
    {
        return PAL_EAFNOSUPPORT;
    }

    return PAL_SUCCESS;
}

extern "C" Error SystemNative_GetPort(const uint8_t* socketAddress, int32_t socketAddressLen, uint16_t* port)
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
        case AF_INET:
        {
            if (socketAddressLen < 0 || static_cast<size_t>(socketAddressLen) < sizeof(sockaddr_in))
            {
                return PAL_EFAULT;
            }

            *port = ntohs(reinterpret_cast<const sockaddr_in*>(socketAddress)->sin_port);
            return PAL_SUCCESS;
        }

        case AF_INET6:
        {
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

extern "C" Error SystemNative_SetPort(uint8_t* socketAddress, int32_t socketAddressLen, uint16_t port)
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
        case AF_INET:
        {
            if (socketAddressLen < 0 || static_cast<size_t>(socketAddressLen) < sizeof(sockaddr_in))
            {
                return PAL_EFAULT;
            }

            reinterpret_cast<sockaddr_in*>(socketAddress)->sin_port = htons(port);
            return PAL_SUCCESS;
        }

        case AF_INET6:
        {
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

extern "C" Error SystemNative_GetIPv4Address(const uint8_t* socketAddress, int32_t socketAddressLen, uint32_t* address)
{
    if (socketAddress == nullptr || address == nullptr || socketAddressLen < 0 ||
        static_cast<size_t>(socketAddressLen) < sizeof(sockaddr_in))
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

extern "C" Error SystemNative_SetIPv4Address(uint8_t* socketAddress, int32_t socketAddressLen, uint32_t address)
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

extern "C" Error SystemNative_GetIPv6Address(
    const uint8_t* socketAddress, int32_t socketAddressLen, uint8_t* address, int32_t addressLen, uint32_t* scopeId)
{
    if (socketAddress == nullptr || address == nullptr || scopeId == nullptr || socketAddressLen < 0 ||
        static_cast<size_t>(socketAddressLen) < sizeof(sockaddr_in6) || addressLen < NUM_BYTES_IN_IPV6_ADDRESS)
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
    ConvertIn6AddrToByteArray(address, addressLen, inet6SockAddr->sin6_addr);
    *scopeId = inet6SockAddr->sin6_scope_id;

    return PAL_SUCCESS;
}

extern "C" Error
SystemNative_SetIPv6Address(uint8_t* socketAddress, int32_t socketAddressLen, uint8_t* address, int32_t addressLen, uint32_t scopeId)
{
    if (socketAddress == nullptr || address == nullptr || socketAddressLen < 0 ||
        static_cast<size_t>(socketAddressLen) < sizeof(sockaddr_in6) || addressLen < NUM_BYTES_IN_IPV6_ADDRESS)
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
    ConvertByteArrayToSockAddrIn6(*inet6SockAddr, address, addressLen);
    inet6SockAddr->sin6_family = AF_INET6;
    inet6SockAddr->sin6_flowinfo = 0;
    inet6SockAddr->sin6_scope_id = scopeId;

    return PAL_SUCCESS;
}

static void ConvertMessageHeaderToMsghdr(msghdr* header, const MessageHeader& messageHeader)
{
    *header = {
        .msg_name = messageHeader.SocketAddress,
        .msg_namelen = static_cast<unsigned int>(messageHeader.SocketAddressLen),
        .msg_iov = reinterpret_cast<iovec*>(messageHeader.IOVectors),
        .msg_iovlen = static_cast<decltype(header->msg_iovlen)>(messageHeader.IOVectorCount),
        .msg_control = messageHeader.ControlBuffer,
        .msg_controllen = static_cast<decltype(header->msg_controllen)>(messageHeader.ControlBufferLen),
    };
}

extern "C" int32_t SystemNative_GetControlMessageBufferSize(int32_t isIPv4, int32_t isIPv6)
{
    // Note: it is possible that the address family of the socket is neither
    //       AF_INET nor AF_INET6. In this case both inputs will be false and
    //       the controll message buffer size should be zero.
    return (isIPv4 != 0 ? CMSG_SPACE(sizeof(in_pktinfo)) : 0) + (isIPv6 != 0 ? CMSG_SPACE(sizeof(in6_pktinfo)) : 0);
}

static int32_t GetIPv4PacketInformation(cmsghdr* controlMessage, IPPacketInformation* packetInfo)
{
    assert(controlMessage != nullptr);
    assert(packetInfo != nullptr);

    if (controlMessage->cmsg_len < sizeof(in_pktinfo))
    {
        assert(false && "expected a control message large enough to hold an in_pktinfo value");
        return 0;
    }

    auto* pktinfo = reinterpret_cast<in_pktinfo*>(CMSG_DATA(controlMessage));
    ConvertInAddrToByteArray(&packetInfo->Address.Address[0], NUM_BYTES_IN_IPV4_ADDRESS, pktinfo->ipi_addr);
#if HAVE_IN_PKTINFO
    packetInfo->InterfaceIndex = static_cast<int32_t>(pktinfo->ipi_ifindex);
#else
    // TODO (#7855): Figure out how to get interface index with in_addr.
    // One option is http://www.unix.com/man-page/freebsd/3/if_nametoindex
    // which requires interface name to be known.
    // Meanwhile:
    packetInfo->InterfaceIndex = 0;
#endif

    return 1;
}

static int32_t GetIPv6PacketInformation(cmsghdr* controlMessage, IPPacketInformation* packetInfo)
{
    assert(controlMessage != nullptr);
    assert(packetInfo != nullptr);

    if (controlMessage->cmsg_len < sizeof(in6_pktinfo))
    {
        assert(false && "expected a control message large enough to hold an in6_pktinfo value");
        return 0;
    }

    auto* pktinfo = reinterpret_cast<in6_pktinfo*>(CMSG_DATA(controlMessage));
    ConvertIn6AddrToByteArray(&packetInfo->Address.Address[0], NUM_BYTES_IN_IPV6_ADDRESS, pktinfo->ipi6_addr);
    packetInfo->Address.IsIPv6 = 1;
    packetInfo->InterfaceIndex = static_cast<int32_t>(pktinfo->ipi6_ifindex);

    return 1;
}

cmsghdr* GET_CMSG_NXTHDR(msghdr* mhdr, cmsghdr* cmsg)
{
#ifndef __GLIBC__
// Tracking issue: #6312
// In musl-libc, CMSG_NXTHDR typecasts char* to cmsghdr* which causes
// clang to throw cast-align warning. This is to suppress the warning
// inline.
// There is also a problem in the CMSG_NXTHDR macro in musl-libc.
// It compares signed and unsigned value and clang warns about that.
// So we suppress the warning inline too.
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wcast-align"
#pragma clang diagnostic ignored "-Wsign-compare"
#endif
    return CMSG_NXTHDR(mhdr, cmsg);
#ifndef __GLIBC__
#pragma clang diagnostic pop
#endif
}

extern "C" int32_t
SystemNative_TryGetIPPacketInformation(MessageHeader* messageHeader, int32_t isIPv4, IPPacketInformation* packetInfo)
{
    if (messageHeader == nullptr || packetInfo == nullptr)
    {
        return 0;
    }

    msghdr header;
    ConvertMessageHeaderToMsghdr(&header, *messageHeader);

    cmsghdr* controlMessage = CMSG_FIRSTHDR(&header);
    if (isIPv4 != 0)
    {
        for (; controlMessage != nullptr && controlMessage->cmsg_len > 0;
             controlMessage = GET_CMSG_NXTHDR(&header, controlMessage))
        {
            if (controlMessage->cmsg_level == IPPROTO_IP && controlMessage->cmsg_type == IP_PKTINFO)
            {
                return GetIPv4PacketInformation(controlMessage, packetInfo);
            }
        }
    }
    else
    {
        for (; controlMessage != nullptr && controlMessage->cmsg_len > 0;
             controlMessage = GET_CMSG_NXTHDR(&header, controlMessage))
        {
            if (controlMessage->cmsg_level == IPPROTO_IPV6 && controlMessage->cmsg_type == IPV6_PKTINFO)
            {
                return GetIPv6PacketInformation(controlMessage, packetInfo);
            }
        }
    }

    return 0;
}

static bool GetMulticastOptionName(int32_t multicastOption, bool isIPv6, int& optionName)
{
    switch (multicastOption)
    {
        case PAL_MULTICAST_ADD:
            optionName = isIPv6 ? IPV6_ADD_MEMBERSHIP : IP_ADD_MEMBERSHIP;
            return true;

        case PAL_MULTICAST_DROP:
            optionName = isIPv6 ? IPV6_DROP_MEMBERSHIP : IP_DROP_MEMBERSHIP;
            return true;

        case PAL_MULTICAST_IF:
            optionName = IP_MULTICAST_IF;
            return true;

        default:
            return false;
    }
}

extern "C" Error SystemNative_GetIPv4MulticastOption(intptr_t socket, int32_t multicastOption, IPv4MulticastOption* option)
{
    if (option == nullptr)
    {
        return PAL_EFAULT;
    }

    int fd = ToFileDescriptor(socket);

    int optionName;
    if (!GetMulticastOptionName(multicastOption, false, optionName))
    {
        return PAL_EINVAL;
    }

#if HAVE_IP_MREQN
    ip_mreqn opt;
#else
    ip_mreq opt;
#endif
    socklen_t len = sizeof(opt);
    int err = getsockopt(fd, IPPROTO_IP, optionName, &opt, &len);
    if (err != 0)
    {
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

    *option = {.MulticastAddress = opt.imr_multiaddr.s_addr,
#if HAVE_IP_MREQN
               .LocalAddress = opt.imr_address.s_addr,
               .InterfaceIndex = opt.imr_ifindex};
#else
               .LocalAddress = opt.imr_interface.s_addr,
               .InterfaceIndex = 0};
#endif
    return PAL_SUCCESS;
}

extern "C" Error SystemNative_SetIPv4MulticastOption(intptr_t socket, int32_t multicastOption, IPv4MulticastOption* option)
{
    if (option == nullptr)
    {
        return PAL_EFAULT;
    }

    int fd = ToFileDescriptor(socket);

    int optionName;
    if (!GetMulticastOptionName(multicastOption, false, optionName))
    {
        return PAL_EINVAL;
    }

#if HAVE_IP_MREQN
    ip_mreqn opt = {.imr_multiaddr = {.s_addr = option->MulticastAddress},
                    .imr_address = {.s_addr = option->LocalAddress},
                    .imr_ifindex = option->InterfaceIndex};
#else
    ip_mreq opt = {.imr_multiaddr = {.s_addr = option->MulticastAddress},
                   .imr_interface = {.s_addr = option->LocalAddress}};
    if (option->InterfaceIndex != 0)
    {
        return PAL_ENOPROTOOPT;
    }
#endif
    int err = setsockopt(fd, IPPROTO_IP, optionName, &opt, sizeof(opt));
    return err == 0 ? PAL_SUCCESS : SystemNative_ConvertErrorPlatformToPal(errno);
}

extern "C" Error SystemNative_GetIPv6MulticastOption(intptr_t socket, int32_t multicastOption, IPv6MulticastOption* option)
{
    if (option == nullptr)
    {
        return PAL_EFAULT;
    }

    int fd = ToFileDescriptor(socket);

    int optionName;
    if (!GetMulticastOptionName(multicastOption, false, optionName))
    {
        return PAL_EINVAL;
    }

    ipv6_mreq opt;
    socklen_t len = sizeof(opt);
    int err = getsockopt(fd, IPPROTO_IP, optionName, &opt, &len);
    if (err != 0)
    {
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

    ConvertIn6AddrToByteArray(&option->Address.Address[0], NUM_BYTES_IN_IPV6_ADDRESS, opt.ipv6mr_multiaddr);
    option->InterfaceIndex = static_cast<int32_t>(opt.ipv6mr_interface);
    return PAL_SUCCESS;
}

extern "C" Error SystemNative_SetIPv6MulticastOption(intptr_t socket, int32_t multicastOption, IPv6MulticastOption* option)
{
    if (option == nullptr)
    {
        return PAL_EFAULT;
    }

    int fd = ToFileDescriptor(socket);

    int optionName;
    if (!GetMulticastOptionName(multicastOption, false, optionName))
    {
        return PAL_EINVAL;
    }

    ipv6_mreq opt;
    memset(&opt, 0, sizeof(ipv6_mreq));
    opt.ipv6mr_interface = static_cast<unsigned int>(option->InterfaceIndex);
    
    ConvertByteArrayToIn6Addr(opt.ipv6mr_multiaddr, &option->Address.Address[0], NUM_BYTES_IN_IPV6_ADDRESS);

    int err = setsockopt(fd, IPPROTO_IP, optionName, &opt, sizeof(opt));
    return err == 0 ? PAL_SUCCESS : SystemNative_ConvertErrorPlatformToPal(errno);
}

#if defined(__APPLE__) && __APPLE__
static int32_t GetMaxLingerTime()
{
    static volatile int32_t MaxLingerTime = -1;
    static_assert(sizeof(xsocket::so_linger) == 2, "");

    // OS X does not define the linger time in seconds by default, but in ticks.
    // Furthermore, when SO_LINGER_SEC is used, the value is simply scaled by
    // the number of ticks per second and then the result is used to set the
    // underlying linger time. Unfortunately, the underlying linger time is
    // stored as a `short` and out-of-range values are simply truncated to fit
    // within 16 bits and then reinterpreted as 2's complement signed integers.
    // This results in some *very* strange behavior and a rather low limit for
    // the linger time. Instead of admitting this behavior, we determine the
    // maximum linger time in seconds and return an error if the input is out
    // of range.
    int32_t maxLingerTime = MaxLingerTime;
    if (maxLingerTime == -1)
    {
        long ticksPerSecond = sysconf(_SC_CLK_TCK);
        maxLingerTime = static_cast<int32_t>(32767 / ticksPerSecond);
        MaxLingerTime = maxLingerTime;
    }

    return maxLingerTime;
}
#else
constexpr int32_t GetMaxLingerTime()
{
    // On other platforms, the maximum linger time is locked to the smaller of
    // 65535 (the maximum time for winsock) and the maximum signed value that
    // will fit in linger::l_linger.

    return Min(65535U, (1U << (sizeof(linger::l_linger) * 8 - 1)) - 1);
}
#endif

extern "C" Error SystemNative_GetLingerOption(intptr_t socket, LingerOption* option)
{
    if (option == nullptr)
    {
        return PAL_EFAULT;
    }

    int fd = ToFileDescriptor(socket);

    linger opt;
    socklen_t len = sizeof(opt);
    int err = getsockopt(fd, SOL_SOCKET, LINGER_OPTION_NAME, &opt, &len);
    if (err != 0)
    {
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

    *option = {.OnOff = opt.l_onoff, .Seconds = opt.l_linger};
    return PAL_SUCCESS;
}

extern "C" Error SystemNative_SetLingerOption(intptr_t socket, LingerOption* option)
{
    if (option == nullptr)
    {
        return PAL_EFAULT;
    }

    if (option->OnOff != 0 && (option->Seconds < 0 || option->Seconds > GetMaxLingerTime()))
    {
        return PAL_EINVAL;
    }

    int fd = ToFileDescriptor(socket);

    linger opt = {.l_onoff = option->OnOff, .l_linger = option->Seconds};
    int err = setsockopt(fd, SOL_SOCKET, LINGER_OPTION_NAME, &opt, sizeof(opt));

#if defined(__APPLE__) && __APPLE__
    if (err != 0 && errno == EINVAL)
    {
        // On OSX, SO_LINGER can return EINVAL if the other end of the socket is already closed.
        // In that case, there is nothing for this end of the socket to do, so there's no reason to "linger."
        // Windows and Linux do not return errors in this case, so we'll simulate success on OSX as well.
        err = 0;
    }
#endif

    return err == 0 ? PAL_SUCCESS : SystemNative_ConvertErrorPlatformToPal(errno);
}

Error SetTimeoutOption(int32_t socket, int32_t millisecondsTimeout, int optionName)
{
    if (millisecondsTimeout < 0)
    {
        return PAL_EINVAL;
    }

    timeval timeout =
    {
        timeout.tv_sec = millisecondsTimeout / 1000,
        timeout.tv_usec = (millisecondsTimeout % 1000) * 1000
    };

    int err = setsockopt(socket, SOL_SOCKET, optionName, &timeout, sizeof(timeout));
    return err == 0 ? PAL_SUCCESS : SystemNative_ConvertErrorPlatformToPal(errno);
}

extern "C" Error SystemNative_SetReceiveTimeout(intptr_t socket, int32_t millisecondsTimeout)
{
    return SetTimeoutOption(ToFileDescriptor(socket), millisecondsTimeout, SO_RCVTIMEO);
}

extern "C" Error SystemNative_SetSendTimeout(intptr_t socket, int32_t millisecondsTimeout)
{
    return SetTimeoutOption(ToFileDescriptor(socket), millisecondsTimeout, SO_SNDTIMEO);
}

static bool ConvertSocketFlagsPalToPlatform(int32_t palFlags, int& platformFlags)
{
    const int32_t SupportedFlagsMask = PAL_MSG_OOB | PAL_MSG_PEEK | PAL_MSG_DONTROUTE | PAL_MSG_TRUNC | PAL_MSG_CTRUNC;

    if ((palFlags & ~SupportedFlagsMask) != 0)
    {
        return false;
    }

    platformFlags = ((palFlags & PAL_MSG_OOB) == 0 ? 0 : MSG_OOB) | ((palFlags & PAL_MSG_PEEK) == 0 ? 0 : MSG_PEEK) |
                    ((palFlags & PAL_MSG_DONTROUTE) == 0 ? 0 : MSG_DONTROUTE) |
                    ((palFlags & PAL_MSG_TRUNC) == 0 ? 0 : MSG_TRUNC) |
                    ((palFlags & PAL_MSG_CTRUNC) == 0 ? 0 : MSG_CTRUNC);

    return true;
}

static int32_t ConvertSocketFlagsPlatformToPal(int platformFlags)
{
    const int SupportedFlagsMask = MSG_OOB | MSG_PEEK | MSG_DONTROUTE | MSG_TRUNC | MSG_CTRUNC;

    platformFlags &= SupportedFlagsMask;

    return ((platformFlags & MSG_OOB) == 0 ? 0 : PAL_MSG_OOB) | ((platformFlags & MSG_PEEK) == 0 ? 0 : PAL_MSG_PEEK) |
           ((platformFlags & MSG_DONTROUTE) == 0 ? 0 : PAL_MSG_DONTROUTE) |
           ((platformFlags & MSG_TRUNC) == 0 ? 0 : PAL_MSG_TRUNC) |
           ((platformFlags & MSG_CTRUNC) == 0 ? 0 : PAL_MSG_CTRUNC);
}

extern "C" Error SystemNative_ReceiveMessage(intptr_t socket, MessageHeader* messageHeader, int32_t flags, int64_t* received)
{
    if (messageHeader == nullptr || received == nullptr || messageHeader->SocketAddressLen < 0 ||
        messageHeader->ControlBufferLen < 0 || messageHeader->IOVectorCount < 0)
    {
        return PAL_EFAULT;
    }

    int fd = ToFileDescriptor(socket);

    int socketFlags;
    if (!ConvertSocketFlagsPalToPlatform(flags, socketFlags))
    {
        return PAL_ENOTSUP;
    }

    msghdr header;
    ConvertMessageHeaderToMsghdr(&header, *messageHeader);

    ssize_t res;
    while (CheckInterrupted(res = recvmsg(fd, &header, socketFlags)));

    assert(static_cast<int32_t>(header.msg_namelen) <= messageHeader->SocketAddressLen);
    messageHeader->SocketAddressLen = Min(static_cast<int32_t>(header.msg_namelen), messageHeader->SocketAddressLen);
    memcpy(messageHeader->SocketAddress, header.msg_name, static_cast<size_t>(messageHeader->SocketAddressLen));

    assert(header.msg_controllen <= static_cast<size_t>(messageHeader->ControlBufferLen));
    messageHeader->ControlBufferLen = Min(static_cast<int32_t>(header.msg_controllen), messageHeader->ControlBufferLen);
    memcpy(messageHeader->ControlBuffer, header.msg_control, static_cast<size_t>(messageHeader->ControlBufferLen));

    messageHeader->Flags = ConvertSocketFlagsPlatformToPal(header.msg_flags);

    if (res != -1)
    {
        *received = res;
        return PAL_SUCCESS;
    }

    *received = 0;
    return SystemNative_ConvertErrorPlatformToPal(errno);
}

extern "C" Error SystemNative_SendMessage(intptr_t socket, MessageHeader* messageHeader, int32_t flags, int64_t* sent)
{
    if (messageHeader == nullptr || sent == nullptr || messageHeader->SocketAddressLen < 0 ||
        messageHeader->ControlBufferLen < 0 || messageHeader->IOVectorCount < 0)
    {
        return PAL_EFAULT;
    }

    int fd = ToFileDescriptor(socket);

    int socketFlags;
    if (!ConvertSocketFlagsPalToPlatform(flags, socketFlags))
    {
        return PAL_ENOTSUP;
    }

    msghdr header;
    ConvertMessageHeaderToMsghdr(&header, *messageHeader);

    ssize_t res;
    while (CheckInterrupted(res = sendmsg(fd, &header, flags)));
    if (res != -1)
    {
        *sent = res;
        return PAL_SUCCESS;
    }

    *sent = 0;
    return SystemNative_ConvertErrorPlatformToPal(errno);
}

extern "C" Error SystemNative_Accept(intptr_t socket, uint8_t* socketAddress, int32_t* socketAddressLen, intptr_t* acceptedSocket)
{
    if (socketAddress == nullptr || socketAddressLen == nullptr || acceptedSocket == nullptr || *socketAddressLen < 0)
    {
        return PAL_EFAULT;
    }

    int fd = ToFileDescriptor(socket);

    socklen_t addrLen = static_cast<socklen_t>(*socketAddressLen);
    int accepted;
    while (CheckInterrupted(accepted = accept(fd, reinterpret_cast<sockaddr*>(socketAddress), &addrLen)));
    if (accepted == -1)
    {
        *acceptedSocket = -1;
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

    assert(addrLen <= static_cast<socklen_t>(*socketAddressLen));
    *socketAddressLen = static_cast<int32_t>(addrLen);
    *acceptedSocket = accepted;
    return PAL_SUCCESS;
}

extern "C" Error SystemNative_Bind(intptr_t socket, uint8_t* socketAddress, int32_t socketAddressLen)
{
    if (socketAddress == nullptr || socketAddressLen < 0)
    {
        return PAL_EFAULT;
    }

    int fd = ToFileDescriptor(socket);

    int err = bind(fd, reinterpret_cast<sockaddr*>(socketAddress), static_cast<socklen_t>(socketAddressLen));
    return err == 0 ? PAL_SUCCESS : SystemNative_ConvertErrorPlatformToPal(errno);
}

extern "C" Error SystemNative_Connect(intptr_t socket, uint8_t* socketAddress, int32_t socketAddressLen)
{
    if (socketAddress == nullptr || socketAddressLen < 0)
    {
        return PAL_EFAULT;
    }

    int fd = ToFileDescriptor(socket);

    int err;
    while (CheckInterrupted(err = connect(fd, reinterpret_cast<sockaddr*>(socketAddress), static_cast<socklen_t>(socketAddressLen))));
    return err == 0 ? PAL_SUCCESS : SystemNative_ConvertErrorPlatformToPal(errno);
}

extern "C" Error SystemNative_GetPeerName(intptr_t socket, uint8_t* socketAddress, int32_t* socketAddressLen)
{
    if (socketAddress == nullptr || socketAddressLen == nullptr || *socketAddressLen < 0)
    {
        return PAL_EFAULT;
    }

    int fd = ToFileDescriptor(socket);

    socklen_t addrLen = static_cast<socklen_t>(*socketAddressLen);
    int err = getpeername(fd, reinterpret_cast<sockaddr*>(socketAddress), &addrLen);
    if (err != 0)
    {
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

    assert(addrLen <= static_cast<socklen_t>(*socketAddressLen));
    *socketAddressLen = static_cast<int32_t>(addrLen);
    return PAL_SUCCESS;
}

extern "C" Error SystemNative_GetSockName(intptr_t socket, uint8_t* socketAddress, int32_t* socketAddressLen)
{
    if (socketAddress == nullptr || socketAddressLen == nullptr || *socketAddressLen < 0)
    {
        return PAL_EFAULT;
    }

    int fd = ToFileDescriptor(socket);

    socklen_t addrLen = static_cast<socklen_t>(*socketAddressLen);
    int err = getsockname(fd, reinterpret_cast<sockaddr*>(socketAddress), &addrLen);
    if (err != 0)
    {
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

    assert(addrLen <= static_cast<socklen_t>(*socketAddressLen));
    *socketAddressLen = static_cast<int32_t>(addrLen);
    return PAL_SUCCESS;
}

extern "C" Error SystemNative_Listen(intptr_t socket, int32_t backlog)
{
    int fd = ToFileDescriptor(socket);
    int err = listen(fd, backlog);
    return err == 0 ? PAL_SUCCESS : SystemNative_ConvertErrorPlatformToPal(errno);
}

extern "C" Error SystemNative_Shutdown(intptr_t socket, int32_t socketShutdown)
{
    int fd = ToFileDescriptor(socket);

    int how;
    switch (socketShutdown)
    {
        case PAL_SHUT_READ:
            how = SHUT_RD;
            break;

        case PAL_SHUT_WRITE:
            how = SHUT_WR;
            break;

        case PAL_SHUT_BOTH:
            how = SHUT_RDWR;
            break;

        default:
            return PAL_EINVAL;
    }

    int err = shutdown(fd, how);
    return err == 0 ? PAL_SUCCESS : SystemNative_ConvertErrorPlatformToPal(errno);
}

extern "C" Error SystemNative_GetSocketErrorOption(intptr_t socket, Error* error)
{
    if (error == nullptr)
    {
        return PAL_EFAULT;
    }

    int fd = ToFileDescriptor(socket);

    int socketErrno;
    socklen_t optLen = sizeof(socketErrno);
    int err = getsockopt(fd, SOL_SOCKET, SO_ERROR, &socketErrno, &optLen);
    if (err != 0)
    {
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

    assert(optLen == sizeof(socketErrno));
    *error = SystemNative_ConvertErrorPlatformToPal(socketErrno);
    return PAL_SUCCESS;
}

static bool TryGetPlatformSocketOption(int32_t socketOptionName, int32_t socketOptionLevel, int& optLevel, int& optName)
{
    switch (socketOptionName)
    {
        case PAL_SOL_SOCKET:
            optLevel = SOL_SOCKET;

            switch (socketOptionLevel)
            {
                case PAL_SO_DEBUG:
                    optName = SO_DEBUG;
                    return true;

                case PAL_SO_ACCEPTCONN:
                    optName = SO_ACCEPTCONN;
                    return true;

                case PAL_SO_REUSEADDR:
                    optName = SO_REUSEADDR;
                    return true;

                case PAL_SO_KEEPALIVE:
                    optName = SO_KEEPALIVE;
                    return true;

                case PAL_SO_DONTROUTE:
                    optName = SO_DONTROUTE;
                    return true;

                case PAL_SO_BROADCAST:
                    optName = SO_BROADCAST;
                    return true;

                // case PAL_SO_USELOOPBACK:

                case PAL_SO_LINGER:
                    optName = SO_LINGER;
                    return true;

                case PAL_SO_OOBINLINE:
                    optName = SO_OOBINLINE;
                    return true;

                // case PAL_SO_DONTLINGER:

                // case PAL_SO_EXCLUSIVEADDRUSE:

                case PAL_SO_SNDBUF:
                    optName = SO_SNDBUF;
                    return true;

                case PAL_SO_RCVBUF:
                    optName = SO_RCVBUF;
                    return true;

                case PAL_SO_SNDLOWAT:
                    optName = SO_SNDLOWAT;
                    return true;

                case PAL_SO_RCVLOWAT:
                    optName = SO_RCVLOWAT;
                    return true;

                case PAL_SO_SNDTIMEO:
                    optName = SO_SNDTIMEO;
                    return true;

                case PAL_SO_RCVTIMEO:
                    optName = SO_RCVTIMEO;
                    return true;

                case PAL_SO_ERROR:
                    optName = SO_ERROR;
                    return true;

                case PAL_SO_TYPE:
                    optName = SO_TYPE;
                    return true;

                // case PAL_SO_MAXCONN:

                default:
                    return false;
            }

        case PAL_SOL_IP:
            optLevel = IPPROTO_IP;

            switch (socketOptionLevel)
            {
                case PAL_SO_IP_OPTIONS:
                    optName = IP_OPTIONS;
                    return true;

                case PAL_SO_IP_HDRINCL:
                    optName = IP_HDRINCL;
                    return true;

                case PAL_SO_IP_TOS:
                    optName = IP_TOS;
                    return true;

                case PAL_SO_IP_TTL:
                    optName = IP_TTL;
                    return true;

                case PAL_SO_IP_MULTICAST_IF:
                    optName = IP_MULTICAST_IF;
                    return true;

                case PAL_SO_IP_MULTICAST_TTL:
                    optName = IP_MULTICAST_TTL;
                    return true;

                case PAL_SO_IP_MULTICAST_LOOP:
                    optName = IP_MULTICAST_LOOP;
                    return true;

                case PAL_SO_IP_ADD_MEMBERSHIP:
                    optName = IP_ADD_MEMBERSHIP;
                    return true;

                case PAL_SO_IP_DROP_MEMBERSHIP:
                    optName = IP_DROP_MEMBERSHIP;
                    return true;

                // case PAL_SO_IP_DONTFRAGMENT:

#ifdef IP_ADD_SOURCE_MEMBERSHIP
                case PAL_SO_IP_ADD_SOURCE_MEMBERSHIP:
                    optName = IP_ADD_SOURCE_MEMBERSHIP;
                    return true;
#endif

#ifdef IP_DROP_SOURCE_MEMBERSHIP
                case PAL_SO_IP_DROP_SOURCE_MEMBERSHIP:
                    optName = IP_DROP_SOURCE_MEMBERSHIP;
                    return true;
#endif

#ifdef IP_BLOCK_SOURCE
                case PAL_SO_IP_BLOCK_SOURCE:
                    optName = IP_BLOCK_SOURCE;
                    return true;
#endif

#ifdef IP_UNBLOCK_SOURCE
                case PAL_SO_IP_UNBLOCK_SOURCE:
                    optName = IP_UNBLOCK_SOURCE;
                    return true;
#endif

                case PAL_SO_IP_PKTINFO:
                    optName = IP_PKTINFO;
                    return true;

                default:
                    return false;
            }

        case PAL_SOL_IPV6:
            optLevel = IPPROTO_IPV6;

            switch (socketOptionLevel)
            {
                case PAL_SO_IPV6_HOPLIMIT:
                    optName = IPV6_HOPLIMIT;
                    return true;

                // case PAL_SO_IPV6_PROTECTION_LEVEL:

                case PAL_SO_IPV6_V6ONLY:
                    optName = IPV6_V6ONLY;
                    return true;

                case PAL_SO_IP_PKTINFO:
                    optName = IPV6_RECVPKTINFO;
                    return true;

                default:
                    return false;
            }

        case PAL_SOL_TCP:
            optLevel = IPPROTO_TCP;

            switch (socketOptionLevel)
            {
                case PAL_SO_TCP_NODELAY:
                    optName = TCP_NODELAY;
                    return true;

                // case PAL_SO_TCP_BSDURGENT:

                default:
                    return false;
            }

        case PAL_SOL_UDP:
            optLevel = IPPROTO_UDP;

            switch (socketOptionLevel)
            {
                // case PAL_SO_UDP_NOCHECKSUM:

                // case PAL_SO_UDP_CHECKSUM_COVERAGE:

                // case PAL_SO_UDP_UPDATEACCEPTCONTEXT:

                // case PAL_SO_UDP_UPDATECONNECTCONTEXT:

                default:
                    return false;
            }

        default:
            return false;
    }
}

extern "C" Error SystemNative_GetSockOpt(
    intptr_t socket, int32_t socketOptionLevel, int32_t socketOptionName, uint8_t* optionValue, int32_t* optionLen)
{
    if (optionLen == nullptr || *optionLen < 0)
    {
        return PAL_EFAULT;
    }

    int fd = ToFileDescriptor(socket);

    //
    // Handle some special cases for compatibility with Windows
    //
    if (socketOptionLevel == PAL_SOL_SOCKET)
    {
        if (socketOptionName == PAL_SO_EXCLUSIVEADDRUSE)
        {
            //
            // SO_EXCLUSIVEADDRUSE makes Windows behave like Unix platforms do WRT the SO_REUSEADDR option.
            // So, for non-Windows platforms, we act as if SO_EXCLUSIVEADDRUSE is always enabled.
            //
            if (*optionLen != sizeof(int32_t))
            {
                return PAL_EINVAL;
            }

            *reinterpret_cast<int32_t*>(optionValue) = 1;
            return PAL_SUCCESS;
        }
        else if (socketOptionName == PAL_SO_REUSEADDR)
        {
            //
            // On Windows, SO_REUSEADDR allows the address *and* port to be reused.  It's equivalent to 
            // SO_REUSEADDR + SO_REUSEPORT other systems.  Se we only return "true" if both of those options are true.
            //
            auto optLen = static_cast<socklen_t>(*optionLen);

            int err = getsockopt(fd, SOL_SOCKET, SO_REUSEADDR, optionValue, &optLen);

            if (err == 0 && *reinterpret_cast<uint32_t*>(optionValue) != 0)
            {
                err = getsockopt(fd, SOL_SOCKET, SO_REUSEPORT, optionValue, &optLen);
            }

            if (err != 0)
            {
                return SystemNative_ConvertErrorPlatformToPal(errno);
            }

            assert(optLen <= static_cast<socklen_t>(*optionLen));
            *optionLen = static_cast<int32_t>(optLen);
            return PAL_SUCCESS;
        }
    }

    int optLevel, optName;
    if (!TryGetPlatformSocketOption(socketOptionLevel, socketOptionName, optLevel, optName))
    {
        return PAL_ENOTSUP;
    }

    auto optLen = static_cast<socklen_t>(*optionLen);
    int err = getsockopt(fd, optLevel, optName, optionValue, &optLen);
    if (err != 0)
    {
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

    assert(optLen <= static_cast<socklen_t>(*optionLen));
    *optionLen = static_cast<int32_t>(optLen);
    return PAL_SUCCESS;
}

extern "C" Error
SystemNative_SetSockOpt(intptr_t socket, int32_t socketOptionLevel, int32_t socketOptionName, uint8_t* optionValue, int32_t optionLen)
{
    if (optionLen < 0)
    {
        return PAL_EFAULT;
    }

    int fd = ToFileDescriptor(socket);

    //
    // Handle some special cases for compatibility with Windows
    //
    if (socketOptionLevel == PAL_SOL_SOCKET)
    {
        if (socketOptionName == PAL_SO_EXCLUSIVEADDRUSE)
        {
            //
            // SO_EXCLUSIVEADDRUSE makes Windows behave like Unix platforms do WRT the SO_REUSEADDR option.
            // So, on Unix platforms, we consider SO_EXCLUSIVEADDRUSE to always be set.  We allow manually setting this
            // to "true", but not "false."
            //
            if (optionLen != sizeof(int32_t))
            {
                return PAL_EINVAL;
            }

            if (*reinterpret_cast<int32_t*>(optionValue) == 0)
            {
                return PAL_ENOTSUP;
            }
            else
            {
                return PAL_SUCCESS;
            }
        }
        else if (socketOptionName == PAL_SO_REUSEADDR)
        {
            //
            // On Windows, SO_REUSEADDR allows the address *and* port to be reused.  It's equivalent to 
            // SO_REUSEADDR + SO_REUSEPORT other systems. 
            //
            int err = setsockopt(fd, SOL_SOCKET, SO_REUSEADDR, optionValue, static_cast<socklen_t>(optionLen));
            if (err == 0)
            {
                err = setsockopt(fd, SOL_SOCKET, SO_REUSEPORT, optionValue, static_cast<socklen_t>(optionLen));
            }
            return err == 0 ? PAL_SUCCESS : SystemNative_ConvertErrorPlatformToPal(errno);
        }
    }

    int optLevel, optName;
    if (!TryGetPlatformSocketOption(socketOptionLevel, socketOptionName, optLevel, optName))
    {
        return PAL_ENOTSUP;
    }

    int err = setsockopt(fd, optLevel, optName, optionValue, static_cast<socklen_t>(optionLen));
    return err == 0 ? PAL_SUCCESS : SystemNative_ConvertErrorPlatformToPal(errno);
}

static bool TryConvertSocketTypePalToPlatform(int32_t palSocketType, int* platformSocketType)
{
    assert(platformSocketType != nullptr);

    switch (palSocketType)
    {
        case PAL_SOCK_STREAM:
            *platformSocketType = SOCK_STREAM;
            return true;

        case PAL_SOCK_DGRAM:
            *platformSocketType = SOCK_DGRAM;
            return true;

        case PAL_SOCK_RAW:
            *platformSocketType = SOCK_RAW;
            return true;

        case PAL_SOCK_RDM:
            *platformSocketType = SOCK_RDM;
            return true;

        case PAL_SOCK_SEQPACKET:
            *platformSocketType = SOCK_SEQPACKET;
            return true;

        default:
            *platformSocketType = static_cast<int>(palSocketType);
            return false;
    }
}

static bool TryConvertProtocolTypePalToPlatform(int32_t palProtocolType, int* platformProtocolType)
{
    assert(platformProtocolType != nullptr);

    switch (palProtocolType)
    {
        case PAL_PT_UNSPECIFIED:
            *platformProtocolType = 0;
            return true;

        case PAL_PT_ICMP:
            *platformProtocolType = IPPROTO_ICMP;
            return true;

        case PAL_PT_TCP:
            *platformProtocolType = IPPROTO_TCP;
            return true;

        case PAL_PT_UDP:
            *platformProtocolType = IPPROTO_UDP;
            return true;

        case PAL_PT_ICMPV6:
            *platformProtocolType = IPPROTO_ICMPV6;
            return true;

        default:
            *platformProtocolType = static_cast<int>(palProtocolType);
            return false;
    }
}

extern "C" Error SystemNative_Socket(int32_t addressFamily, int32_t socketType, int32_t protocolType, intptr_t* createdSocket)
{
    if (createdSocket == nullptr)
    {
        return PAL_EFAULT;
    }

    sa_family_t platformAddressFamily;
    int platformSocketType, platformProtocolType;

    if (!TryConvertAddressFamilyPalToPlatform(addressFamily, &platformAddressFamily))
    {
        *createdSocket = -1;
        return PAL_EAFNOSUPPORT;
    }

    if (!TryConvertSocketTypePalToPlatform(socketType, &platformSocketType))
    {
        *createdSocket = -1;
        return PAL_EPROTOTYPE;
    }

    if (!TryConvertProtocolTypePalToPlatform(protocolType, &platformProtocolType))
    {
        *createdSocket = -1;
        return PAL_EPROTONOSUPPORT;
    }

    *createdSocket = socket(platformAddressFamily, platformSocketType, platformProtocolType);
    return *createdSocket != -1 ? PAL_SUCCESS : SystemNative_ConvertErrorPlatformToPal(errno);
}

extern "C" Error SystemNative_GetBytesAvailable(intptr_t socket, int32_t* available)
{
    if (available == nullptr)
    {
        return PAL_EFAULT;
    }

    int fd = ToFileDescriptor(socket);

    int avail;
    int err;
    while (CheckInterrupted(err = ioctl(fd, FIONREAD, &avail)));
    if (err == -1)
    {
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

    *available = static_cast<int32_t>(avail);
    return PAL_SUCCESS;
}

#if HAVE_EPOLL

const size_t SocketEventBufferElementSize = Max(sizeof(epoll_event), sizeof(SocketEvent));

static SocketEvents GetSocketEvents(uint32_t events)
{
    int asyncEvents = (((events & EPOLLIN) != 0) ? PAL_SA_READ : 0) | (((events & EPOLLOUT) != 0) ? PAL_SA_WRITE : 0) |
                      (((events & EPOLLRDHUP) != 0) ? PAL_SA_READCLOSE : 0) |
                      (((events & EPOLLHUP) != 0) ? PAL_SA_CLOSE : 0) | (((events & EPOLLERR) != 0) ? PAL_SA_ERROR : 0);

    return static_cast<SocketEvents>(asyncEvents);
}

static uint32_t GetEPollEvents(SocketEvents events)
{
    return (((events & PAL_SA_READ) != 0) ? EPOLLIN : 0) | (((events & PAL_SA_WRITE) != 0) ? EPOLLOUT : 0) |
           (((events & PAL_SA_READCLOSE) != 0) ? EPOLLRDHUP : 0) | (((events & PAL_SA_CLOSE) != 0) ? EPOLLHUP : 0) |
           (((events & PAL_SA_ERROR) != 0) ? EPOLLERR : 0);
}

static Error CreateSocketEventPortInner(int32_t* port)
{
    assert(port != nullptr);

    int epollFd = epoll_create1(EPOLL_CLOEXEC);
    if (epollFd == -1)
    {
        *port = -1;
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

    *port = epollFd;
    return PAL_SUCCESS;
}

static Error CloseSocketEventPortInner(int32_t port)
{
    int err = close(port);
    return err == 0 || CheckInterrupted(err) ? PAL_SUCCESS : SystemNative_ConvertErrorPlatformToPal(errno);
}

static Error TryChangeSocketEventRegistrationInner(
    int32_t port, int32_t socket, SocketEvents currentEvents, SocketEvents newEvents, uintptr_t data)
{
    assert(currentEvents != newEvents);

    int op = EPOLL_CTL_MOD;
    if (currentEvents == PAL_SA_NONE)
    {
        op = EPOLL_CTL_ADD;
    }
    else if (newEvents == PAL_SA_NONE)
    {
        op = EPOLL_CTL_DEL;
    }

    epoll_event evt = {.events = GetEPollEvents(newEvents) | EPOLLET, .data = {.ptr = reinterpret_cast<void*>(data)}};
    int err = epoll_ctl(port, op, socket, &evt);
    return err == 0 ? PAL_SUCCESS : SystemNative_ConvertErrorPlatformToPal(errno);
}

static void ConvertEventEPollToSocketAsync(SocketEvent* sae, epoll_event* epoll)
{
    assert(sae != nullptr);
    assert(epoll != nullptr);

    // epoll does not play well with disconnected connection-oriented sockets, frequently
    // reporting spurious EPOLLHUP events. Fortunately, EPOLLHUP may be handled as an
    // EPOLLIN | EPOLLOUT event: the usual processing for these events will recognize and
    // handle the HUP condition.
    uint32_t events = epoll->events;
    if ((events & EPOLLHUP) != 0)
    {
        events = (events & static_cast<uint32_t>(~EPOLLHUP)) | EPOLLIN | EPOLLOUT;
    }

    *sae = {.Data = reinterpret_cast<uintptr_t>(epoll->data.ptr), .Events = GetSocketEvents(events)};
}

static Error WaitForSocketEventsInner(int32_t port, SocketEvent* buffer, int32_t* count)
{
    assert(buffer != nullptr);
    assert(count != nullptr);
    assert(*count >= 0);

    auto* events = reinterpret_cast<epoll_event*>(buffer);
    int numEvents;
    while (CheckInterrupted(numEvents = epoll_wait(port, events, *count, -1)));
    if (numEvents == -1)
    {
        *count = 0;
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

    // We should never see 0 events. Given an infinite timeout, epoll_wait will never return
    // 0 events even if there are no file descriptors registered with the epoll fd. In
    // that case, the wait will block until a file descriptor is added and an event occurs
    // on the added file descriptor.
    assert(numEvents != 0);
    assert(numEvents < *count);

    if (sizeof(epoll_event) < sizeof(SocketEvent))
    {
        // Copy backwards to avoid overwriting earlier data.
        for (int i = numEvents - 1; i >= 0; i--)
        {
            // This copy is made deliberately to avoid overwriting data.
            epoll_event evt = events[i];
            ConvertEventEPollToSocketAsync(&buffer[i], &evt);
        }
    }
    else
    {
        // Copy forwards for better cache behavior
        for (int i = 0; i < numEvents; i++)
        {
            // This copy is made deliberately to avoid overwriting data.
            epoll_event evt = events[i];
            ConvertEventEPollToSocketAsync(&buffer[i], &evt);
        }
    }

    *count = numEvents;
    return PAL_SUCCESS;
}

#elif HAVE_KQUEUE

static_assert(sizeof(SocketEvent) <= sizeof(struct kevent), "");
const size_t SocketEventBufferElementSize = sizeof(struct kevent);

static SocketEvents GetSocketEvents(int16_t filter, uint16_t flags)
{
    int32_t events;
    switch (filter)
    {
        case EVFILT_READ:
            events = PAL_SA_READ;
            if ((flags & EV_EOF) != 0)
            {
                events |= PAL_SA_READCLOSE;
            }
            break;

        case EVFILT_WRITE:
            events = PAL_SA_WRITE;

            // kqueue does not play well with disconnected connection-oriented sockets, frequently
            // reporting spurious EOF events. Fortunately, EOF may be handled as an EVFILT_READ |
            // EVFILT_WRITE event: the usual processing for these events will recognize and
            // handle the EOF condition.
            if ((flags & EV_EOF) != 0)
            {
                events |= PAL_SA_READ;
            }
            break;

        default:
            assert(false && "unexpected kqueue filter type");
            return PAL_SA_NONE;
    }

    if ((flags & EV_ERROR) != 0)
    {
        events |= PAL_SA_ERROR;
    }

    return static_cast<SocketEvents>(events);
};

static Error CreateSocketEventPortInner(int32_t* port)
{
    assert(port != nullptr);

    int kqueueFd = kqueue();
    if (kqueueFd == -1)
    {
        *port = -1;
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

    *port = kqueueFd;
    return PAL_SUCCESS;
}

static Error CloseSocketEventPortInner(int32_t port)
{
    int err = close(port);
    return err == 0 || CheckInterrupted(err) ? PAL_SUCCESS : SystemNative_ConvertErrorPlatformToPal(errno);
}

static Error TryChangeSocketEventRegistrationInner(
    int32_t port, int32_t socket, SocketEvents currentEvents, SocketEvents newEvents, uintptr_t data)
{
#ifdef EV_RECEIPT
    const uint16_t AddFlags = EV_ADD | EV_CLEAR | EV_RECEIPT;
    const uint16_t RemoveFlags = EV_DELETE | EV_RECEIPT;
#else
    const uint16_t AddFlags = EV_ADD | EV_CLEAR;
    const uint16_t RemoveFlags = EV_DELETE;
#endif

    assert(currentEvents != newEvents);

    int32_t changes = currentEvents ^ newEvents;
    bool readChanged = (changes & PAL_SA_READ) != 0;
    bool writeChanged = (changes & PAL_SA_WRITE) != 0;

    struct kevent events[2];

    int i = 0;
    if (readChanged)
    {
        EV_SET(&events[i++],
               static_cast<uint64_t>(socket),
               EVFILT_READ,
               (newEvents & PAL_SA_READ) == 0 ? RemoveFlags : AddFlags,
               0,
               0,
               GetKeventUdata(data));
    }

    if (writeChanged)
    {
        EV_SET(&events[i++],
               static_cast<uint64_t>(socket),
               EVFILT_WRITE,
               (newEvents & PAL_SA_WRITE) == 0 ? RemoveFlags : AddFlags,
               0,
               0,
               GetKeventUdata(data));
    }

    int err;
    while (CheckInterrupted(err = kevent(port, events, GetKeventNchanges(i), nullptr, 0, nullptr)));
    return err == 0 ? PAL_SUCCESS : SystemNative_ConvertErrorPlatformToPal(errno);
}

static Error WaitForSocketEventsInner(int32_t port, SocketEvent* buffer, int32_t* count)
{
    assert(buffer != nullptr);
    assert(count != nullptr);
    assert(*count >= 0);

    auto* events = reinterpret_cast<struct kevent*>(buffer);
    int numEvents;
    while (CheckInterrupted(numEvents = kevent(port, nullptr, 0, events, GetKeventNchanges(*count), nullptr)));
    if (numEvents == -1)
    {
        *count = -1;
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

    // We should never see 0 events. Given an infinite timeout, kevent will never return
    // 0 events even if there are no file descriptors registered with the kqueue fd. In
    // that case, the wait will block until a file descriptor is added and an event occurs
    // on the added file descriptor.
    assert(numEvents != 0);
    assert(numEvents < *count);

    for (int i = 0; i < numEvents; i++)
    {
        // This copy is made deliberately to avoid overwriting data.
        struct kevent evt = events[i];
        buffer[i] = {.Data = GetSocketEventData(evt.udata), .Events = GetSocketEvents(GetKeventFilter(evt.filter), GetKeventFlags(evt.flags))};
    }

    *count = numEvents;
    return PAL_SUCCESS;
}

#else
#error Asynchronous sockets require epoll or kqueue support.
#endif

extern "C" Error SystemNative_CreateSocketEventPort(intptr_t* port)
{
    if (port == nullptr)
    {
        return PAL_EFAULT;
    }

    int fd;
    Error error = CreateSocketEventPortInner(&fd);
    *port = fd;
    return error;
}

extern "C" Error SystemNative_CloseSocketEventPort(intptr_t port)
{
    return CloseSocketEventPortInner(ToFileDescriptor(port));
}

extern "C" Error SystemNative_CreateSocketEventBuffer(int32_t count, SocketEvent** buffer)
{
    if (buffer == nullptr || count < 0)
    {
        return PAL_EFAULT;
    }

    void* b = malloc(SocketEventBufferElementSize * static_cast<size_t>(count));
    if (b == nullptr)
    {
        *buffer = nullptr;
        return PAL_ENOMEM;
    }

    *buffer = reinterpret_cast<SocketEvent*>(b);
    return PAL_SUCCESS;
}

extern "C" Error SystemNative_FreeSocketEventBuffer(SocketEvent* buffer)
{
    free(buffer);
    return PAL_SUCCESS;
}

extern "C" Error
SystemNative_TryChangeSocketEventRegistration(intptr_t port, intptr_t socket, int32_t currentEvents, int32_t newEvents, uintptr_t data)
{
    int portFd = ToFileDescriptor(port);
    int socketFd = ToFileDescriptor(socket);

    const int32_t SupportedEvents = PAL_SA_READ | PAL_SA_WRITE | PAL_SA_READCLOSE | PAL_SA_CLOSE | PAL_SA_ERROR;

    if ((currentEvents & ~SupportedEvents) != 0 || (newEvents & ~SupportedEvents) != 0)
    {
        return PAL_EINVAL;
    }

    if (currentEvents == newEvents)
    {
        return PAL_SUCCESS;
    }

    return TryChangeSocketEventRegistrationInner(
        portFd, socketFd, static_cast<SocketEvents>(currentEvents), static_cast<SocketEvents>(newEvents), data);
}

extern "C" Error SystemNative_WaitForSocketEvents(intptr_t port, SocketEvent* buffer, int32_t* count)
{
    if (buffer == nullptr || count == nullptr || *count < 0)
    {
        return PAL_EFAULT;
    }

    int fd = ToFileDescriptor(port);

    return WaitForSocketEventsInner(fd, buffer, count);
}

extern "C" int32_t SystemNative_PlatformSupportsDualModeIPv4PacketInfo()
{
#if HAVE_SUPPORT_FOR_DUAL_MODE_IPV4_PACKET_INFO
    return 1;
#else
    return 0;
#endif
}

static char* GetNameFromUid(uid_t uid)
{
    size_t bufferLength = 512;
    while (true)
    {
        char *buffer = reinterpret_cast<char*>(malloc(bufferLength));
        if (buffer == nullptr)
            return nullptr;

        struct passwd pw;
        struct passwd* result;
        if (getpwuid_r(uid, &pw, buffer, bufferLength, &result) == 0)
        {
            if (result == nullptr)
            {
                errno = ENOENT;
                free(buffer);
                return nullptr;
            }
            else
            {
                char* name = strdup(pw.pw_name);
                free(buffer);
                return name;
            }
        }

        free(buffer);
        if (errno == ERANGE)
        {
            bufferLength *= 2;
        }
        else
        {
            return nullptr;
        }
    }
}

extern "C" char* SystemNative_GetPeerUserName(intptr_t socket)
{
    uid_t euid;
    return SystemNative_GetPeerID(socket, &euid) == 0 ?
        GetNameFromUid(euid) :
        nullptr;
}

extern "C" void SystemNative_GetDomainSocketSizes(int32_t* pathOffset, int32_t* pathSize, int32_t* addressSize)
{
    assert(pathOffset != nullptr);
    assert(pathSize != nullptr);
    assert(addressSize != nullptr);

    struct sockaddr_un domainSocket;

    *pathOffset = offsetof(struct sockaddr_un, sun_path);
    *pathSize = sizeof(domainSocket.sun_path);
    *addressSize = sizeof(domainSocket);
}
