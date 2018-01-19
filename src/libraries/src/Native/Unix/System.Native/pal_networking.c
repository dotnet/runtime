// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_config.h"
#include "pal_networking.h"
#include "pal_io.h"
#include "pal_safecrt.h"
#include "pal_utilities.h"

#include <stdlib.h>
#include <limits.h>
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
#if !HAVE_GETDOMAINNAME && HAVE_UNAME
#include <sys/utsname.h>
#include <stdio.h>
#endif
#include <unistd.h>
#include <pwd.h>
#if HAVE_SENDFILE_4
#include <sys/sendfile.h>
#elif HAVE_SENDFILE_6
#include <sys/uio.h>
#endif

#if HAVE_KQUEUE
#if KEVENT_HAS_VOID_UDATA
void* GetKeventUdata(uintptr_t udata)
{
    return (void*)udata;
}
uintptr_t GetSocketEventData(void* udata)
{
    return (uintptr_t)udata;
}
#else
intptr_t GetKeventUdata(uintptr_t udata)
{
    return (intptr_t)udata;
}
uintptr_t GetSocketEventData(intptr_t udata)
{
    return (uintptr_t)udata;
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
    return (size_t)nchanges;
}
int16_t GetKeventFilter(uint32_t filter)
{
    return (int16_t)filter;
}
uint16_t GetKeventFlags(uint32_t flags)
{
    return (uint16_t)flags;
}
#endif
#endif

#if !HAVE_IN_PKTINFO
// On platforms, such as FreeBSD, where in_pktinfo
// is not available, fallback to custom definition
// with required members.
struct in_pktinfo
{
    struct in_addr ipi_addr;
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

c_static_assert(GetHostErrorCodes_HOST_NOT_FOUND == HOST_NOT_FOUND);
c_static_assert(GetHostErrorCodes_TRY_AGAIN == TRY_AGAIN);
c_static_assert(GetHostErrorCodes_NO_RECOVERY == NO_RECOVERY);
c_static_assert(GetHostErrorCodes_NO_DATA == NO_DATA);
c_static_assert(GetHostErrorCodes_NO_ADDRESS == NO_ADDRESS);
c_static_assert(sizeof(uint8_t) == sizeof(char)); // We make casts from uint8_t to char so make sure it's legal

// We require that IOVector have the same layout as iovec.
c_static_assert(sizeof(struct IOVector) == sizeof(struct iovec));
c_static_assert(sizeof_member(struct IOVector, Base) == sizeof_member(struct iovec, iov_base));
c_static_assert(offsetof(struct IOVector, Base) == offsetof(struct iovec, iov_base));
c_static_assert(sizeof_member(struct IOVector, Count) == sizeof_member(struct iovec, iov_len));
c_static_assert(offsetof(struct IOVector, Count) == offsetof(struct iovec, iov_len));

#define Min(left,right) (((left) < (right)) ? (left) : (right))

static void ConvertByteArrayToIn6Addr(struct in6_addr* addr, const uint8_t* buffer, int32_t bufferLength)
{
    assert(bufferLength == NUM_BYTES_IN_IPV6_ADDRESS);
    memcpy_s(addr->s6_addr, NUM_BYTES_IN_IPV6_ADDRESS, buffer, (uint32_t)bufferLength);
}

static void ConvertIn6AddrToByteArray(uint8_t* buffer, int32_t bufferLength, const struct in6_addr* addr)
{
    assert(bufferLength == NUM_BYTES_IN_IPV6_ADDRESS);
    memcpy_s(buffer, (uint32_t)bufferLength, addr->s6_addr, NUM_BYTES_IN_IPV6_ADDRESS);
}

static void ConvertByteArrayToSockAddrIn6(struct sockaddr_in6* addr, const uint8_t* buffer, int32_t bufferLength)
{
    ConvertByteArrayToIn6Addr(&addr->sin6_addr, buffer, bufferLength);

    // Mark that this is INET6
    addr->sin6_family = AF_INET6;
}

static void ConvertByteArrayToInAddr(struct in_addr* addr, const uint8_t* buffer, int32_t bufferLength)
{
    assert(bufferLength == NUM_BYTES_IN_IPV4_ADDRESS);
    memcpy_s(&addr->s_addr, NUM_BYTES_IN_IPV4_ADDRESS, buffer, (uint32_t)bufferLength); // Send back in network byte order.
}

static void ConvertInAddrToByteArray(uint8_t* buffer, int32_t bufferLength, const struct in_addr* addr)
{
    assert(bufferLength == NUM_BYTES_IN_IPV4_ADDRESS);
    memcpy_s(buffer, (uint32_t)bufferLength, &addr->s_addr, NUM_BYTES_IN_IPV4_ADDRESS); // Send back in network byte order.
}

static void ConvertByteArrayToSockAddrIn(struct sockaddr_in* addr, const uint8_t* buffer, int32_t bufferLength)
{
    ConvertByteArrayToInAddr(&addr->sin_addr, buffer, bufferLength);

    addr->sin_family = AF_INET;
}

static int32_t ConvertGetAddrInfoAndGetNameInfoErrorsToPal(int32_t error)
{
    switch (error)
    {
        case 0:
            return 0;
        case EAI_AGAIN:
            return GetAddrInfoErrorFlags_EAI_AGAIN;
        case EAI_BADFLAGS:
            return GetAddrInfoErrorFlags_EAI_BADFLAGS;
#ifdef EAI_FAIL
        case EAI_FAIL:
            return GetAddrInfoErrorFlags_EAI_FAIL;
#endif
        case EAI_FAMILY:
            return GetAddrInfoErrorFlags_EAI_FAMILY;
        case EAI_NONAME:
#ifdef EAI_NODATA
        case EAI_NODATA:
#endif
            return GetAddrInfoErrorFlags_EAI_NONAME;
    }

    assert_err(0, "Unknown AddrInfo error flag", error);
    return -1;
}

int32_t SystemNative_GetHostEntryForName(const uint8_t* address, struct HostEntry* entry)
{
    if (address == NULL || entry == NULL)
    {
        return GetAddrInfoErrorFlags_EAI_BADARG;
    }

    // Get all address families and the canonical name
    struct addrinfo hint;
    memset(&hint, 0, sizeof(struct addrinfo));
    hint.ai_family = AF_UNSPEC;
    hint.ai_flags = AI_CANONNAME;

    struct addrinfo* info = NULL;
    int result = getaddrinfo((const char*)address, NULL, &hint, &info);
    if (result != 0)
    {
        return ConvertGetAddrInfoAndGetNameInfoErrorsToPal(result);
    }

    entry->CanonicalName = NULL;
    entry->Aliases = NULL;
    entry->AddressListHandle = (void*)info;
    entry->IPAddressCount = 0;
    entry->HandleType = HOST_ENTRY_HANDLE_ADDRINFO;

    // Find the canonical name for this host (if any) and count the number of IP end points.
    for (struct addrinfo* ai = info; ai != NULL; ai = ai->ai_next)
    {
        // If we haven't found a canonical name yet and this addrinfo has one, copy it
        if ((entry->CanonicalName == NULL) && (ai->ai_canonname != NULL))
        {
            entry->CanonicalName = (uint8_t*)ai->ai_canonname;
        }

        if (ai->ai_family == AF_INET || ai->ai_family == AF_INET6)
        {
            entry->IPAddressCount++;
        }
    }

    return GetAddrInfoErrorFlags_EAI_SUCCESS;
}

static int ConvertGetHostErrorPlatformToPal(int error)
{
    switch (error)
    {
        case HOST_NOT_FOUND:
            return GetHostErrorCodes_HOST_NOT_FOUND;

        case TRY_AGAIN:
            return GetHostErrorCodes_TRY_AGAIN;

        case NO_RECOVERY:
            return GetHostErrorCodes_NO_RECOVERY;

        case NO_DATA:
            return GetHostErrorCodes_NO_DATA;

        default:
            assert_err(0, "Unknown gethostbyname/gethostbyaddr error code", error);
            return GetHostErrorCodes_HOST_NOT_FOUND;
    }
}

static void ConvertHostEntPlatformToPal(struct HostEntry* hostEntry, struct hostent* entry)
{
    hostEntry->CanonicalName = (uint8_t*)entry->h_name;
    hostEntry->Aliases = (uint8_t**)entry->h_aliases;
    hostEntry->AddressListHandle = (void*)entry;
    hostEntry->IPAddressCount = 0;
    hostEntry->HandleType = HOST_ENTRY_HANDLE_HOSTENT;

    for (int i = 0; entry->h_addr_list[i] != NULL; i++)
    {
        hostEntry->IPAddressCount++;
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
                           char* buffer, size_t buflen, struct hostent** entry, int* error)
{
    assert(hostname != NULL);
    assert(result != NULL);
    assert(buffer != NULL);
    assert(entry != NULL);
    assert(error != NULL);

    if (hostname == NULL || entry == NULL || error == NULL || buffer == NULL || result == NULL)
    {
        if (error != NULL)
        {
            *error = GetHostErrorCodes_BAD_ARG;
        }

        return GetHostErrorCodes_BAD_ARG;
    }

    pthread_mutex_lock(&lock_hostbyx_mutex);

    *entry = gethostbyname(hostname);
    if ((!(*entry)) || ((*entry)->h_addrtype != AF_INET) || ((*entry)->h_length != 4))
    {
        *error = h_errno;
        *entry = NULL;
    }
    else
    {
        h_errno = copy_hostent(*entry, result, buffer, buflen);
        *entry = (h_errno == 0) ? result : NULL;
    }

    pthread_mutex_unlock(&lock_hostbyx_mutex);

    return h_errno;
}

static int gethostbyaddr_r(const uint8_t* addr, const socklen_t len, int type, struct hostent* result,
                           char* buffer, size_t buflen, struct hostent** entry, int* error)
{
    assert(addr != NULL);
    assert(result != NULL);
    assert(buffer != NULL);
    assert(entry != NULL);
    assert(error != NULL);

    if (addr == NULL || entry == NULL || buffer == NULL || result == NULL)
    {
        if (error != NULL)
        {
            *error = GetHostErrorCodes_BAD_ARG;
        }

        return GetHostErrorCodes_BAD_ARG;
    }

    pthread_mutex_lock(&lock_hostbyx_mutex);

    *entry = gethostbyaddr((const char*)addr, (unsigned int)len, type);
    if ((!(*entry)) || ((*entry)->h_addrtype != AF_INET) || ((*entry)->h_length != 4))
    {
        *error = h_errno;
        *entry = NULL;
    }
    else
    {
        h_errno = copy_hostent(*entry, result, buffer, buflen);
        *entry = (h_errno == 0) ? result : NULL;
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
static int GetHostByNameHelper(const uint8_t* hostname, struct hostent** entry)
{
    assert(hostname != NULL);
    assert(entry != NULL);

    size_t scratchLen = 512;

    for (;;)
    {
        size_t bufferSize;
        uint8_t* buffer;
        if (!add_s(sizeof(struct hostent), scratchLen, &bufferSize) ||
            (buffer = (uint8_t*)malloc(bufferSize)) == NULL)
        {
            return GetHostErrorCodes_NO_MEM;
        }

        struct hostent* result = (struct hostent*)buffer;
        char* scratch = (char*)&buffer[sizeof(struct hostent)];

        int getHostErrno = 0;
        int err = gethostbyname_r((const char*)hostname, result, scratch, scratchLen, entry, &getHostErrno);
        if (!err && *entry != NULL)
        {
            assert(*entry == result);
            return 0;
        }
        else if (err == ERANGE)
        {
            free(buffer);
            size_t tmpScratchLen;
            if (!multiply_s(scratchLen, (size_t)2, &tmpScratchLen))
            {
                *entry = NULL;
                return GetHostErrorCodes_NO_MEM;
            }
            scratchLen = tmpScratchLen;
        }
        else
        {
            free(buffer);
            *entry = NULL;
            return getHostErrno ? getHostErrno : HOST_NOT_FOUND;
        }
    }
}
#endif /* HAVE_GETHOSTBYNAME_R */
#endif /* !HAVE_THREAD_SAFE_GETHOSTBYNAME_AND_GETHOSTBYADDR */

int32_t SystemNative_GetHostByName(const uint8_t* hostname, struct HostEntry* entry)
{
    if (hostname == NULL || entry == NULL)
    {
        return GetHostErrorCodes_BAD_ARG;
    }

    struct hostent* hostEntry = NULL;
    int error = 0;

#if HAVE_THREAD_SAFE_GETHOSTBYNAME_AND_GETHOSTBYADDR
    hostEntry = gethostbyname((const char*)hostname);
    error = h_errno;
#elif HAVE_GETHOSTBYNAME_R
    error = GetHostByNameHelper(hostname, &hostEntry);
#else
#error Platform does not provide thread-safe gethostbyname
#endif

    if (hostEntry == NULL)
    {
        return ConvertGetHostErrorPlatformToPal(error);
    }

    ConvertHostEntPlatformToPal(entry, hostEntry);
    return Error_SUCCESS;
}

#if !HAVE_THREAD_SAFE_GETHOSTBYNAME_AND_GETHOSTBYADDR && HAVE_GETHOSTBYADDR_R
static int GetHostByAddrHelper(const uint8_t* addr, const socklen_t addrLen, int type, struct hostent** entry)
{
    assert(addr != NULL);
    assert(addrLen >= 0);
    assert(entry != NULL);

    size_t scratchLen = 512;

    for (;;)
    {
        size_t bufferSize;
        uint8_t* buffer;
        if (!add_s(sizeof(struct hostent), scratchLen, &bufferSize) ||
            (buffer = (uint8_t*)malloc(bufferSize)) == NULL)
        {
            return GetHostErrorCodes_NO_MEM;
        }

        struct hostent* result = (struct hostent*)buffer;
        char* scratch = (char*)&buffer[sizeof(struct hostent)];

        int getHostErrno = 0;
        int err = gethostbyaddr_r(addr, addrLen, type, result, scratch, scratchLen, entry, &getHostErrno);
        if (!err && *entry != NULL)
        {
            assert(*entry == result);
            return 0;
        }
        else if (err == ERANGE)
        {
            free(buffer);
            size_t tmpScratchLen;
            if (!multiply_s(scratchLen, (size_t)2, &tmpScratchLen))
            {
                *entry = NULL;
                return GetHostErrorCodes_NO_MEM;
            }
            scratchLen = tmpScratchLen;
        }
        else
        {
            free(buffer);
            *entry = NULL;
            return getHostErrno ? getHostErrno : HOST_NOT_FOUND;
        }
    }
}
#endif /* !HAVE_THREAD_SAFE_GETHOSTBYNAME_AND_GETHOSTBYADDR && HAVE_GETHOSTBYADDR_R */

int32_t SystemNative_GetHostByAddress(const struct IPAddress* address, struct HostEntry* entry)
{
    if (address == NULL || entry == NULL)
    {
        return GetHostErrorCodes_BAD_ARG;
    }

    uint8_t* addr = NULL;
    socklen_t addrLen = 0;
    int type = AF_UNSPEC;

    struct in_addr inAddr;
    memset(&inAddr, 0, sizeof(struct in_addr));
    struct in6_addr in6Addr;
    memset(&in6Addr, 0, sizeof(struct in6_addr));

    if (!address->IsIPv6)
    {
        ConvertByteArrayToInAddr(&inAddr, address->Address, NUM_BYTES_IN_IPV4_ADDRESS);
        addr = (uint8_t*)&inAddr;
        addrLen = sizeof(inAddr);
        type = AF_INET;
    }
    else
    {
        ConvertByteArrayToIn6Addr(&in6Addr, address->Address, NUM_BYTES_IN_IPV6_ADDRESS);
        addr = (uint8_t*)&in6Addr;
        addrLen = sizeof(in6Addr);
        type = AF_INET6;
    }

    struct hostent* hostEntry = NULL;
    int error = 0;

#if HAVE_THREAD_SAFE_GETHOSTBYNAME_AND_GETHOSTBYADDR
    hostEntry = gethostbyaddr(addr, addrLen, type);
    error = h_errno;
#elif HAVE_GETHOSTBYADDR_R
    error = GetHostByAddrHelper(addr, addrLen, type, &hostEntry);
#else
#error Platform does not provide thread-safe gethostbyname
#endif

    if (hostEntry == NULL)
    {
        return ConvertGetHostErrorPlatformToPal(error);
    }

    ConvertHostEntPlatformToPal(entry, hostEntry);
    return Error_SUCCESS;
}

static int32_t GetNextIPAddressFromAddrInfo(struct addrinfo** info, struct IPAddress* endPoint)
{
    assert(info != NULL);
    assert(endPoint != NULL);

    for (struct addrinfo* ai = *info; ai != NULL; ai = ai->ai_next)
    {
        switch (ai->ai_family)
        {
            case AF_INET:
            {
                struct sockaddr_in* inetSockAddr = (struct sockaddr_in*)ai->ai_addr;

                ConvertInAddrToByteArray(endPoint->Address, NUM_BYTES_IN_IPV4_ADDRESS, &inetSockAddr->sin_addr);
                endPoint->IsIPv6 = 0;
                break;
            }

            case AF_INET6:
            {
                struct sockaddr_in6* inet6SockAddr = (struct sockaddr_in6*)ai->ai_addr;

                ConvertIn6AddrToByteArray(endPoint->Address, NUM_BYTES_IN_IPV6_ADDRESS, &inet6SockAddr->sin6_addr);
                endPoint->IsIPv6 = 1;
                endPoint->ScopeId = inet6SockAddr->sin6_scope_id;
                break;
            }

            default:
                // Skip non-IPv4 and non-IPv6 addresses
                continue;
        }

        *info = ai->ai_next;
        return GetAddrInfoErrorFlags_EAI_SUCCESS;
    }

    return GetAddrInfoErrorFlags_EAI_NOMORE;
}

static int32_t GetNextIPAddressFromHostEnt(struct hostent** hostEntry, struct IPAddress* address)
{
    assert(hostEntry != NULL);
    assert(address != NULL);

    struct hostent* entry = *hostEntry;
    if (*entry->h_addr_list == NULL)
    {
        return GetAddrInfoErrorFlags_EAI_NOMORE;
    }

    switch (entry->h_addrtype)
    {
        case AF_INET:
        {
            struct in_addr* inAddr = (struct in_addr*)entry->h_addr_list[0];

            ConvertInAddrToByteArray(address->Address, NUM_BYTES_IN_IPV4_ADDRESS, inAddr);
            address->IsIPv6 = 0;
            break;
        }

        case AF_INET6:
        {
            struct in6_addr* in6Addr = (struct in6_addr*)entry->h_addr_list[0];

            ConvertIn6AddrToByteArray(address->Address, NUM_BYTES_IN_IPV6_ADDRESS, in6Addr);
            address->IsIPv6 = 1;
            address->ScopeId = 0;
            break;
        }

        default:
            return GetAddrInfoErrorFlags_EAI_NOMORE;
    }

    entry->h_addr_list = &entry->h_addr_list[1];
    return GetAddrInfoErrorFlags_EAI_SUCCESS;
}

int32_t SystemNative_GetNextIPAddress(const struct HostEntry* hostEntry, void** addressListHandle, struct IPAddress* endPoint)
{
    if (hostEntry == NULL || addressListHandle == NULL || endPoint == NULL)
    {
        return GetAddrInfoErrorFlags_EAI_BADARG;
    }

    switch (hostEntry->HandleType)
    {
        case HOST_ENTRY_HANDLE_ADDRINFO:
            return GetNextIPAddressFromAddrInfo((struct addrinfo**)addressListHandle, endPoint);

        case HOST_ENTRY_HANDLE_HOSTENT:
            return GetNextIPAddressFromHostEnt((struct hostent**)addressListHandle, endPoint);

        default:
            return GetAddrInfoErrorFlags_EAI_BADARG;
    }
}

void SystemNative_FreeHostEntry(struct HostEntry* entry)
{
    if (entry != NULL)
    {
        switch (entry->HandleType)
        {
            case HOST_ENTRY_HANDLE_ADDRINFO:
            {
                struct addrinfo* ai = (struct addrinfo*)entry->AddressListHandle;
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

// There were several versions of glibc that had the flags parameter of getnameinfo unsigned
#if HAVE_GETNAMEINFO_SIGNED_FLAGS
typedef int32_t NativeFlagsType;
#else
typedef uint32_t NativeFlagsType;
#endif

static inline NativeFlagsType ConvertGetNameInfoFlagsToNative(int32_t flags)
{
    NativeFlagsType outFlags = 0;
    if ((flags & GetAddrInfoErrorFlags_NI_NAMEREQD) == GetAddrInfoErrorFlags_NI_NAMEREQD)
    {
        outFlags |= NI_NAMEREQD;
    }
    if ((flags & GetAddrInfoErrorFlags_NI_NUMERICHOST) == GetAddrInfoErrorFlags_NI_NUMERICHOST)
    {
        outFlags |= NI_NUMERICHOST;
    }

    return outFlags;
}

int32_t SystemNative_GetNameInfo(const uint8_t* address,
                               int32_t addressLength,
                               int8_t isIPv6,
                               uint8_t* host,
                               int32_t hostLength,
                               uint8_t* service,
                               int32_t serviceLength,
                               int32_t flags)
{
    assert(address != NULL);
    assert(addressLength > 0);
    assert((host != NULL) || (service != NULL));
    assert((hostLength > 0) || (serviceLength > 0));

    NativeFlagsType nativeFlags = ConvertGetNameInfoFlagsToNative(flags);
    int32_t result;

    if (isIPv6)
    {
        struct sockaddr_in6 addr;
        memset(&addr, 0, sizeof(struct sockaddr_in6));
        ConvertByteArrayToSockAddrIn6(&addr, address, addressLength);
        result = getnameinfo((const struct sockaddr*)&addr,
                             sizeof(struct sockaddr_in6),
                             (char*)host,
                             (uint32_t)hostLength,
                             (char*)service,
                             (uint32_t)serviceLength,
                             nativeFlags);
    }
    else
    {
        struct sockaddr_in addr;
        memset(&addr, 0, sizeof(struct sockaddr_in));
        ConvertByteArrayToSockAddrIn(&addr, address, addressLength);
        result = getnameinfo((const struct sockaddr*)&addr,
                             sizeof(struct sockaddr_in),
                             (char*)host,
                             (uint32_t)hostLength,
                             (char*)service,
                             (uint32_t)serviceLength,
                             nativeFlags);
    }

    return ConvertGetAddrInfoAndGetNameInfoErrorsToPal(result);
}

int32_t SystemNative_GetDomainName(uint8_t* name, int32_t nameLength)
{
    assert(name != NULL);
    assert(nameLength > 0);

#if HAVE_GETDOMAINNAME
#if HAVE_GETDOMAINNAME_SIZET
    size_t namelen = (uint32_t)nameLength;
#else
    int namelen = nameLength;
#endif

    return getdomainname((char*)name, namelen);
#elif HAVE_UNAME
    // On Android, there's no getdomainname but we can use uname to fetch the domain name
    // of the current device
    size_t namelen = (uint32_t)nameLength;
    utsname  uts;

    // If uname returns an error, bail out.
    if (uname(&uts) == -1)
    {
        return -1;
    }

    // If we don't have enough space to copy the name, bail out.
    if (strlen(uts.domainname) >= namelen)
    {
        errno = EINVAL;
        return -1;
    }

    // Copy the domain name
    SafeStringCopy((char*)name, nameLength, uts.domainname);
    return 0;
#else
    // GetDomainName is not supported on this platform.
    errno = ENOTSUP;
    return -1;
#endif
}

int32_t SystemNative_GetHostName(uint8_t* name, int32_t nameLength)
{
    assert(name != NULL);
    assert(nameLength > 0);

    size_t unsignedSize = (uint32_t)nameLength;
    return gethostname((char*)name, unsignedSize);
}

static bool IsInBounds(const uint8_t* baseAddr, size_t len, const uint8_t* valueAddr, size_t valueSize)
{
    return valueAddr >= baseAddr && (valueAddr + valueSize) <= (baseAddr + len);
}

int32_t SystemNative_GetIPSocketAddressSizes(int32_t* ipv4SocketAddressSize, int32_t* ipv6SocketAddressSize)
{
    if (ipv4SocketAddressSize == NULL || ipv6SocketAddressSize == NULL)
    {
        return Error_EFAULT;
    }

    *ipv4SocketAddressSize = sizeof(struct sockaddr_in);
    *ipv6SocketAddressSize = sizeof(struct sockaddr_in6);
    return Error_SUCCESS;
}

static bool TryConvertAddressFamilyPlatformToPal(sa_family_t platformAddressFamily, int32_t* palAddressFamily)
{
    assert(palAddressFamily != NULL);

    switch (platformAddressFamily)
    {
        case AF_UNSPEC:
            *palAddressFamily = AddressFamily_AF_UNSPEC;
            return true;

        case AF_UNIX:
            *palAddressFamily = AddressFamily_AF_UNIX;
            return true;

        case AF_INET:
            *palAddressFamily = AddressFamily_AF_INET;
            return true;

        case AF_INET6:
            *palAddressFamily = AddressFamily_AF_INET6;
            return true;

        default:
            *palAddressFamily = platformAddressFamily;
            return false;
    }
}

static bool TryConvertAddressFamilyPalToPlatform(int32_t palAddressFamily, sa_family_t* platformAddressFamily)
{
    assert(platformAddressFamily != NULL);

    switch (palAddressFamily)
    {
        case AddressFamily_AF_UNSPEC:
            *platformAddressFamily = AF_UNSPEC;
            return true;

        case AddressFamily_AF_UNIX:
            *platformAddressFamily = AF_UNIX;
            return true;

        case AddressFamily_AF_INET:
            *platformAddressFamily = AF_INET;
            return true;

        case AddressFamily_AF_INET6:
            *platformAddressFamily = AF_INET6;
            return true;

        default:
            *platformAddressFamily = (sa_family_t)palAddressFamily;
            return false;
    }
}

int32_t SystemNative_GetAddressFamily(const uint8_t* socketAddress, int32_t socketAddressLen, int32_t* addressFamily)
{
    if (socketAddress == NULL || addressFamily == NULL || socketAddressLen < 0)
    {
        return Error_EFAULT;
    }

    const struct sockaddr* sockAddr = (const struct sockaddr*)socketAddress;
    if (!IsInBounds((const uint8_t*)sockAddr, (size_t)socketAddressLen, (const uint8_t*)&sockAddr->sa_family, sizeof_member(struct sockaddr, sa_family)))
    {
        return Error_EFAULT;
    }

    if (!TryConvertAddressFamilyPlatformToPal(sockAddr->sa_family, addressFamily))
    {
        return Error_EAFNOSUPPORT;
    }

    return Error_SUCCESS;
}

int32_t SystemNative_SetAddressFamily(uint8_t* socketAddress, int32_t socketAddressLen, int32_t addressFamily)
{
    struct sockaddr* sockAddr = (struct sockaddr*)socketAddress;
    if (sockAddr == NULL || socketAddressLen < 0 ||
        !IsInBounds((const uint8_t*)sockAddr, (size_t)socketAddressLen, (const uint8_t*)&sockAddr->sa_family, sizeof_member(struct sockaddr, sa_family)))
    {
        return Error_EFAULT;
    }

    if (!TryConvertAddressFamilyPalToPlatform(addressFamily, &sockAddr->sa_family))
    {
        return Error_EAFNOSUPPORT;
    }

    return Error_SUCCESS;
}

int32_t SystemNative_GetPort(const uint8_t* socketAddress, int32_t socketAddressLen, uint16_t* port)
{
    if (socketAddress == NULL)
    {
        return Error_EFAULT;
    }

    const struct sockaddr* sockAddr = (const struct sockaddr*)socketAddress;
    if (!IsInBounds((const uint8_t*)sockAddr, (size_t)socketAddressLen, (const uint8_t*)&sockAddr->sa_family, sizeof_member(struct sockaddr, sa_family)))
    {
        return Error_EFAULT;
    }

    switch (sockAddr->sa_family)
    {
        case AF_INET:
        {
            if (socketAddressLen < 0 || (size_t)socketAddressLen < sizeof(struct sockaddr_in))
            {
                return Error_EFAULT;
            }

            *port = ntohs(((const struct sockaddr_in*)socketAddress)->sin_port);
            return Error_SUCCESS;
        }

        case AF_INET6:
        {
            if (socketAddressLen < 0 || (size_t)socketAddressLen < sizeof(struct sockaddr_in6))
            {
                return Error_EFAULT;
            }

            *port = ntohs(((const struct sockaddr_in6*)socketAddress)->sin6_port);
            return Error_SUCCESS;
        }

        default:
            return Error_EAFNOSUPPORT;
    }
}

int32_t SystemNative_SetPort(uint8_t* socketAddress, int32_t socketAddressLen, uint16_t port)
{
    if (socketAddress == NULL)
    {
        return Error_EFAULT;
    }

    const struct sockaddr* sockAddr = (const struct sockaddr*)socketAddress;
    if (!IsInBounds((const uint8_t*)sockAddr, (size_t)socketAddressLen, (const uint8_t*)&sockAddr->sa_family, sizeof_member(struct sockaddr, sa_family)))
    {
        return Error_EFAULT;
    }

    switch (sockAddr->sa_family)
    {
        case AF_INET:
        {
            if (socketAddressLen < 0 || (size_t)socketAddressLen < sizeof(struct sockaddr_in))
            {
                return Error_EFAULT;
            }

            ((struct sockaddr_in*)socketAddress)->sin_port = htons(port);
            return Error_SUCCESS;
        }

        case AF_INET6:
        {
            if (socketAddressLen < 0 || (size_t)socketAddressLen < sizeof(struct sockaddr_in6))
            {
                return Error_EFAULT;
            }

            ((struct sockaddr_in6*)socketAddress)->sin6_port = htons(port);
            return Error_SUCCESS;
        }

        default:
            return Error_EAFNOSUPPORT;
    }
}

int32_t SystemNative_GetIPv4Address(const uint8_t* socketAddress, int32_t socketAddressLen, uint32_t* address)
{
    if (socketAddress == NULL || address == NULL || socketAddressLen < 0 ||
        (size_t)socketAddressLen < sizeof(struct sockaddr_in))
    {
        return Error_EFAULT;
    }

    const struct sockaddr* sockAddr = (const struct sockaddr*)socketAddress;
    if (!IsInBounds((const uint8_t*)sockAddr, (size_t)socketAddressLen, (const uint8_t*)&sockAddr->sa_family, sizeof_member(struct sockaddr, sa_family)))
    {
        return Error_EFAULT;
    }

    if (sockAddr->sa_family != AF_INET)
    {
        return Error_EINVAL;
    }

    *address = ((const struct sockaddr_in*)socketAddress)->sin_addr.s_addr;
    return Error_SUCCESS;
}

int32_t SystemNative_SetIPv4Address(uint8_t* socketAddress, int32_t socketAddressLen, uint32_t address)
{
    if (socketAddress == NULL || socketAddressLen < 0 || (size_t)socketAddressLen < sizeof(struct sockaddr_in))
    {
        return Error_EFAULT;
    }

    struct sockaddr* sockAddr = (struct sockaddr*)socketAddress;
    if (!IsInBounds((const uint8_t*)sockAddr, (size_t)socketAddressLen, (const uint8_t*)&sockAddr->sa_family, sizeof_member(struct sockaddr, sa_family)))
    {
        return Error_EFAULT;
    }

    if (sockAddr->sa_family != AF_INET)
    {
        return Error_EINVAL;
    }

    struct sockaddr_in* inetSockAddr = (struct sockaddr_in*)sockAddr;

    inetSockAddr->sin_family = AF_INET;
    inetSockAddr->sin_addr.s_addr = address;
    return Error_SUCCESS;
}

int32_t SystemNative_GetIPv6Address(
    const uint8_t* socketAddress, int32_t socketAddressLen, uint8_t* address, int32_t addressLen, uint32_t* scopeId)
{
    if (socketAddress == NULL || address == NULL || scopeId == NULL || socketAddressLen < 0 ||
        (size_t)socketAddressLen < sizeof(struct sockaddr_in6) || addressLen < NUM_BYTES_IN_IPV6_ADDRESS)
    {
        return Error_EFAULT;
    }

    const struct sockaddr* sockAddr = (const struct sockaddr*)socketAddress;
    if (!IsInBounds((const uint8_t*)sockAddr, (size_t)socketAddressLen, (const uint8_t*)&sockAddr->sa_family, sizeof_member(struct sockaddr, sa_family)))
    {
        return Error_EFAULT;
    }

    if (sockAddr->sa_family != AF_INET6)
    {
        return Error_EINVAL;
    }

    const struct sockaddr_in6* inet6SockAddr = (const struct sockaddr_in6*)sockAddr;
    ConvertIn6AddrToByteArray(address, addressLen, &inet6SockAddr->sin6_addr);
    *scopeId = inet6SockAddr->sin6_scope_id;

    return Error_SUCCESS;
}

int32_t
SystemNative_SetIPv6Address(uint8_t* socketAddress, int32_t socketAddressLen, uint8_t* address, int32_t addressLen, uint32_t scopeId)
{
    if (socketAddress == NULL || address == NULL || socketAddressLen < 0 ||
        (size_t)socketAddressLen < sizeof(struct sockaddr_in6) || addressLen < NUM_BYTES_IN_IPV6_ADDRESS)
    {
        return Error_EFAULT;
    }

    struct sockaddr* sockAddr = (struct sockaddr*)socketAddress;
    if (!IsInBounds((const uint8_t*)sockAddr, (size_t)socketAddressLen, (const uint8_t*)&sockAddr->sa_family, sizeof_member(struct sockaddr, sa_family)))
    {
        return Error_EFAULT;
    }

    if (sockAddr->sa_family != AF_INET6)
    {
        return Error_EINVAL;
    }

    struct sockaddr_in6* inet6SockAddr = (struct sockaddr_in6*)sockAddr;
    ConvertByteArrayToSockAddrIn6(inet6SockAddr, address, addressLen);
    inet6SockAddr->sin6_family = AF_INET6;
    inet6SockAddr->sin6_flowinfo = 0;
    inet6SockAddr->sin6_scope_id = scopeId;

    return Error_SUCCESS;
}

static int8_t IsStreamSocket(int socket)
{
    int type;
    socklen_t length = sizeof(int);
    return getsockopt(socket, SOL_SOCKET, SO_TYPE, &type, &length) == 0
           && type == SOCK_STREAM;
}

static void ConvertMessageHeaderToMsghdr(struct msghdr* header, const struct MessageHeader* messageHeader, int socket)
{
    // sendmsg/recvmsg can return EMSGSIZE when msg_iovlen is greather than IOV_MAX.
    // We avoid this for stream sockets by truncating msg_iovlen to IOV_MAX. This is ok since sendmsg is
    // not required to send all data and recvmsg can be called again to receive more.
    int iovlen = (int)messageHeader->IOVectorCount;
    if (iovlen > IOV_MAX && IsStreamSocket(socket))
    {
        iovlen = (int)IOV_MAX;
    }
    header->msg_name = messageHeader->SocketAddress;
    header->msg_namelen = (unsigned int)messageHeader->SocketAddressLen;
    header->msg_iov = (struct iovec*)messageHeader->IOVectors;
    header->msg_iovlen = (__typeof__(header->msg_iovlen))iovlen;
    header->msg_control = messageHeader->ControlBuffer;
    header->msg_controllen = (uint32_t)messageHeader->ControlBufferLen;
}

int32_t SystemNative_GetControlMessageBufferSize(int32_t isIPv4, int32_t isIPv6)
{
    // Note: it is possible that the address family of the socket is neither
    //       AF_INET nor AF_INET6. In this case both inputs will be 0 and
    //       the controll message buffer size should be zero.
    return (isIPv4 != 0 ? CMSG_SPACE(sizeof(struct in_pktinfo)) : 0) + (isIPv6 != 0 ? CMSG_SPACE(sizeof(struct in6_pktinfo)) : 0);
}

static int32_t GetIPv4PacketInformation(struct cmsghdr* controlMessage, struct IPPacketInformation* packetInfo)
{
    assert(controlMessage != NULL);
    assert(packetInfo != NULL);

    if (controlMessage->cmsg_len < sizeof(struct in_pktinfo))
    {
        assert(false && "expected a control message large enough to hold an in_pktinfo value");
        return 0;
    }

    struct in_pktinfo* pktinfo = (struct in_pktinfo*)CMSG_DATA(controlMessage);
    ConvertInAddrToByteArray(&packetInfo->Address.Address[0], NUM_BYTES_IN_IPV4_ADDRESS, &pktinfo->ipi_addr);
#if HAVE_IN_PKTINFO
    packetInfo->InterfaceIndex = (int32_t)pktinfo->ipi_ifindex;
#else
    // TODO (#7855): Figure out how to get interface index with in_addr.
    // One option is http://www.unix.com/man-page/freebsd/3/if_nametoindex
    // which requires interface name to be known.
    // Meanwhile:
    packetInfo->InterfaceIndex = 0;
#endif

    return 1;
}

static int32_t GetIPv6PacketInformation(struct cmsghdr* controlMessage, struct IPPacketInformation* packetInfo)
{
    assert(controlMessage != NULL);
    assert(packetInfo != NULL);

    if (controlMessage->cmsg_len < sizeof(struct in6_pktinfo))
    {
        assert(false && "expected a control message large enough to hold an in6_pktinfo value");
        return 0;
    }

    struct in6_pktinfo* pktinfo = (struct in6_pktinfo*)CMSG_DATA(controlMessage);
    ConvertIn6AddrToByteArray(&packetInfo->Address.Address[0], NUM_BYTES_IN_IPV6_ADDRESS, &pktinfo->ipi6_addr);
    packetInfo->Address.IsIPv6 = 1;
    packetInfo->InterfaceIndex = (int32_t)pktinfo->ipi6_ifindex;

    return 1;
}

struct cmsghdr* GET_CMSG_NXTHDR(struct msghdr* mhdr, struct cmsghdr* cmsg)
{
#ifndef __GLIBC__
// Tracking issue: #6312
// In musl-libc, CMSG_NXTHDR typecasts char* to struct cmsghdr* which causes
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

int32_t
SystemNative_TryGetIPPacketInformation(struct MessageHeader* messageHeader, int32_t isIPv4, struct IPPacketInformation* packetInfo)
{
    if (messageHeader == NULL || packetInfo == NULL)
    {
        return 0;
    }

    struct msghdr header;
    ConvertMessageHeaderToMsghdr(&header, messageHeader, -1);

    struct cmsghdr* controlMessage = CMSG_FIRSTHDR(&header);
    if (isIPv4 != 0)
    {
        for (; controlMessage != NULL && controlMessage->cmsg_len > 0;
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
        for (; controlMessage != NULL && controlMessage->cmsg_len > 0;
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

static int8_t GetMulticastOptionName(int32_t multicastOption, int8_t isIPv6, int* optionName)
{
    switch (multicastOption)
    {
        case MulticastOption_MULTICAST_ADD:
            *optionName = isIPv6 ? IPV6_ADD_MEMBERSHIP : IP_ADD_MEMBERSHIP;
            return true;

        case MulticastOption_MULTICAST_DROP:
            *optionName = isIPv6 ? IPV6_DROP_MEMBERSHIP : IP_DROP_MEMBERSHIP;
            return true;

        case MulticastOption_MULTICAST_IF:
            *optionName = isIPv6 ? IPV6_MULTICAST_IF : IP_MULTICAST_IF;
            return true;

        default:
            return false;
    }
}

int32_t SystemNative_GetIPv4MulticastOption(intptr_t socket, int32_t multicastOption, struct IPv4MulticastOption* option)
{
    if (option == NULL)
    {
        return Error_EFAULT;
    }

    int fd = ToFileDescriptor(socket);

    int optionName;
    if (!GetMulticastOptionName(multicastOption, 0, &optionName))
    {
        return Error_EINVAL;
    }

#if HAVE_IP_MREQN
    struct ip_mreqn opt;
#else
    struct ip_mreq opt;
#endif
    socklen_t len = sizeof(opt);
    int err = getsockopt(fd, IPPROTO_IP, optionName, &opt, &len);
    if (err != 0)
    {
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

    memset(option, 0, sizeof(struct IPv4MulticastOption));
    option->MulticastAddress = opt.imr_multiaddr.s_addr;
#if HAVE_IP_MREQN
    option->LocalAddress = opt.imr_address.s_addr;
    option->InterfaceIndex = opt.imr_ifindex;
#else
    option->LocalAddress = opt.imr_interface.s_addr;
    option->InterfaceIndex = 0;
#endif
    return Error_SUCCESS;
}

int32_t SystemNative_SetIPv4MulticastOption(intptr_t socket, int32_t multicastOption, struct IPv4MulticastOption* option)
{
    if (option == NULL)
    {
        return Error_EFAULT;
    }

    int fd = ToFileDescriptor(socket);

    int optionName;
    if (!GetMulticastOptionName(multicastOption, 0, &optionName))
    {
        return Error_EINVAL;
    }

#if HAVE_IP_MREQN
    struct ip_mreqn opt;
    memset(&opt, 0, sizeof(struct ip_mreqn));
    opt.imr_multiaddr.s_addr = option->MulticastAddress;
    opt.imr_address.s_addr = option->LocalAddress;
    opt.imr_ifindex = option->InterfaceIndex;
#else
    struct ip_mreq opt;
    memset(&opt, 0, sizeof(struct ip_mreq));
    opt.imr_multiaddr.s_addr = option->MulticastAddress;
    opt.imr_interface.s_addr = option->LocalAddress;
    if (option->InterfaceIndex != 0)
    {
        return Error_ENOPROTOOPT;
    }
#endif
    int err = setsockopt(fd, IPPROTO_IP, optionName, &opt, sizeof(opt));
    return err == 0 ? Error_SUCCESS : SystemNative_ConvertErrorPlatformToPal(errno);
}

int32_t SystemNative_GetIPv6MulticastOption(intptr_t socket, int32_t multicastOption, struct IPv6MulticastOption* option)
{
    if (option == NULL)
    {
        return Error_EFAULT;
    }

    int fd = ToFileDescriptor(socket);

    int optionName;
    if (!GetMulticastOptionName(multicastOption, 1, &optionName))
    {
        return Error_EINVAL;
    }

    struct ipv6_mreq opt;
    socklen_t len = sizeof(opt);
    int err = getsockopt(fd, IPPROTO_IPV6, optionName, &opt, &len);
    if (err != 0)
    {
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

    ConvertIn6AddrToByteArray(&option->Address.Address[0], NUM_BYTES_IN_IPV6_ADDRESS, &opt.ipv6mr_multiaddr);
    option->InterfaceIndex = (int32_t)opt.ipv6mr_interface;
    return Error_SUCCESS;
}

int32_t SystemNative_SetIPv6MulticastOption(intptr_t socket, int32_t multicastOption, struct IPv6MulticastOption* option)
{
    if (option == NULL)
    {
        return Error_EFAULT;
    }

    int fd = ToFileDescriptor(socket);

    int optionName;
    if (!GetMulticastOptionName(multicastOption, 1, &optionName))
    {
        return Error_EINVAL;
    }

    struct ipv6_mreq opt;
    memset(&opt, 0, sizeof(struct ipv6_mreq));

    opt.ipv6mr_interface =
#if IPV6MR_INTERFACE_UNSIGNED
        (unsigned int)option->InterfaceIndex;
#else
        option->InterfaceIndex;
#endif

    ConvertByteArrayToIn6Addr(&opt.ipv6mr_multiaddr, &option->Address.Address[0], NUM_BYTES_IN_IPV6_ADDRESS);

    int err = setsockopt(fd, IPPROTO_IPV6, optionName, &opt, sizeof(opt));
    return err == 0 ? Error_SUCCESS : SystemNative_ConvertErrorPlatformToPal(errno);
}

#if defined(__APPLE__) && __APPLE__
static int32_t GetMaxLingerTime()
{
    static volatile int32_t MaxLingerTime = -1;
    c_static_assert(sizeof_member(struct xsocket, so_linger) == 2);

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
        maxLingerTime = (int32_t)(32767 / ticksPerSecond);
        MaxLingerTime = maxLingerTime;
    }

    return maxLingerTime;
}
#else
static int32_t GetMaxLingerTime()
{
    // On other platforms, the maximum linger time is locked to the smaller of
    // 65535 (the maximum time for winsock) and the maximum signed value that
    // will fit in linger::l_linger.

    return Min(65535U, (1U << (sizeof_member(struct linger, l_linger) * 8 - 1)) - 1);
}
#endif

int32_t SystemNative_GetLingerOption(intptr_t socket, struct LingerOption* option)
{
    if (option == NULL)
    {
        return Error_EFAULT;
    }

    int fd = ToFileDescriptor(socket);

    struct linger opt;
    socklen_t len = sizeof(opt);
    int err = getsockopt(fd, SOL_SOCKET, LINGER_OPTION_NAME, &opt, &len);
    if (err != 0)
    {
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

    memset(option, 0, sizeof(struct LingerOption));
    option->OnOff = opt.l_onoff;
    option->Seconds = opt.l_linger;
    return Error_SUCCESS;
}

int32_t SystemNative_SetLingerOption(intptr_t socket, struct LingerOption* option)
{
    if (option == NULL)
    {
        return Error_EFAULT;
    }

    if (option->OnOff != 0 && (option->Seconds < 0 || option->Seconds > GetMaxLingerTime()))
    {
        return Error_EINVAL;
    }

    int fd = ToFileDescriptor(socket);

    struct linger opt;
    memset(&opt, 0, sizeof(struct linger));
    opt.l_onoff = option->OnOff;
    opt.l_linger = option->Seconds;
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

    return err == 0 ? Error_SUCCESS : SystemNative_ConvertErrorPlatformToPal(errno);
}

int32_t SetTimeoutOption(int32_t socket, int32_t millisecondsTimeout, int optionName)
{
    if (millisecondsTimeout < 0)
    {
        return Error_EINVAL;
    }

    struct timeval timeout;
    timeout.tv_sec = millisecondsTimeout / 1000;
    timeout.tv_usec = (millisecondsTimeout % 1000) * 1000;

    int err = setsockopt(socket, SOL_SOCKET, optionName, &timeout, sizeof(timeout));
    return err == 0 ? Error_SUCCESS : SystemNative_ConvertErrorPlatformToPal(errno);
}

int32_t SystemNative_SetReceiveTimeout(intptr_t socket, int32_t millisecondsTimeout)
{
    return SetTimeoutOption(ToFileDescriptor(socket), millisecondsTimeout, SO_RCVTIMEO);
}

int32_t SystemNative_SetSendTimeout(intptr_t socket, int32_t millisecondsTimeout)
{
    return SetTimeoutOption(ToFileDescriptor(socket), millisecondsTimeout, SO_SNDTIMEO);
}

static int8_t ConvertSocketFlagsPalToPlatform(int32_t palFlags, int* platformFlags)
{
    const int32_t SupportedFlagsMask = SocketFlags_MSG_OOB | SocketFlags_MSG_PEEK | SocketFlags_MSG_DONTROUTE | SocketFlags_MSG_TRUNC | SocketFlags_MSG_CTRUNC;

    if ((palFlags & ~SupportedFlagsMask) != 0)
    {
        return false;
    }

    *platformFlags = ((palFlags & SocketFlags_MSG_OOB) == 0 ? 0 : MSG_OOB) | ((palFlags & SocketFlags_MSG_PEEK) == 0 ? 0 : MSG_PEEK) |
                    ((palFlags & SocketFlags_MSG_DONTROUTE) == 0 ? 0 : MSG_DONTROUTE) |
                    ((palFlags & SocketFlags_MSG_TRUNC) == 0 ? 0 : MSG_TRUNC) |
                    ((palFlags & SocketFlags_MSG_CTRUNC) == 0 ? 0 : MSG_CTRUNC);

    return true;
}

static int32_t ConvertSocketFlagsPlatformToPal(int platformFlags)
{
    const int SupportedFlagsMask = MSG_OOB | MSG_PEEK | MSG_DONTROUTE | MSG_TRUNC | MSG_CTRUNC;

    platformFlags &= SupportedFlagsMask;

    return ((platformFlags & MSG_OOB) == 0 ? 0 : SocketFlags_MSG_OOB) | ((platformFlags & MSG_PEEK) == 0 ? 0 : SocketFlags_MSG_PEEK) |
           ((platformFlags & MSG_DONTROUTE) == 0 ? 0 : SocketFlags_MSG_DONTROUTE) |
           ((platformFlags & MSG_TRUNC) == 0 ? 0 : SocketFlags_MSG_TRUNC) |
           ((platformFlags & MSG_CTRUNC) == 0 ? 0 : SocketFlags_MSG_CTRUNC);
}

int32_t SystemNative_ReceiveMessage(intptr_t socket, struct MessageHeader* messageHeader, int32_t flags, int64_t* received)
{
    if (messageHeader == NULL || received == NULL || messageHeader->SocketAddressLen < 0 ||
        messageHeader->ControlBufferLen < 0 || messageHeader->IOVectorCount < 0)
    {
        return Error_EFAULT;
    }

    int fd = ToFileDescriptor(socket);

    int socketFlags;
    if (!ConvertSocketFlagsPalToPlatform(flags, &socketFlags))
    {
        return Error_ENOTSUP;
    }

    struct msghdr header;
    ConvertMessageHeaderToMsghdr(&header, messageHeader, fd);

    ssize_t res;
    while ((res = recvmsg(fd, &header, socketFlags)) < 0 && errno == EINTR);

    assert(header.msg_name == messageHeader->SocketAddress); // should still be the same location as set in ConvertMessageHeaderToMsghdr
    assert(header.msg_control == messageHeader->ControlBuffer);

    assert((int32_t)header.msg_namelen <= messageHeader->SocketAddressLen);
    messageHeader->SocketAddressLen = Min((int32_t)header.msg_namelen, messageHeader->SocketAddressLen);
    
    assert(header.msg_controllen <= (size_t)messageHeader->ControlBufferLen);
    messageHeader->ControlBufferLen = Min((int32_t)header.msg_controllen, messageHeader->ControlBufferLen);

    messageHeader->Flags = ConvertSocketFlagsPlatformToPal(header.msg_flags);

    if (res != -1)
    {
        *received = res;
        return Error_SUCCESS;
    }

    *received = 0;
    return SystemNative_ConvertErrorPlatformToPal(errno);
}

int32_t SystemNative_SendMessage(intptr_t socket, struct MessageHeader* messageHeader, int32_t flags, int64_t* sent)
{
    if (messageHeader == NULL || sent == NULL || messageHeader->SocketAddressLen < 0 ||
        messageHeader->ControlBufferLen < 0 || messageHeader->IOVectorCount < 0)
    {
        return Error_EFAULT;
    }

    int fd = ToFileDescriptor(socket);

    int socketFlags;
    if (!ConvertSocketFlagsPalToPlatform(flags, &socketFlags))
    {
        return Error_ENOTSUP;
    }

    struct msghdr header;
    ConvertMessageHeaderToMsghdr(&header, messageHeader, fd);

    ssize_t res;
    while ((res = sendmsg(fd, &header, flags)) < 0 && errno == EINTR);
    if (res != -1)
    {
        *sent = res;
        return Error_SUCCESS;
    }

    *sent = 0;
    return SystemNative_ConvertErrorPlatformToPal(errno);
}

int32_t SystemNative_Accept(intptr_t socket, uint8_t* socketAddress, int32_t* socketAddressLen, intptr_t* acceptedSocket)
{
    if (socketAddress == NULL || socketAddressLen == NULL || acceptedSocket == NULL || *socketAddressLen < 0)
    {
        return Error_EFAULT;
    }

    int fd = ToFileDescriptor(socket);

    socklen_t addrLen = (socklen_t)*socketAddressLen;
    int accepted;
#if defined(HAVE_ACCEPT4) && defined(SOCK_CLOEXEC)
    while ((accepted = accept4(fd, (struct sockaddr*)socketAddress, &addrLen, SOCK_CLOEXEC)) < 0 && errno == EINTR);
#else
    while ((accepted = accept(fd, (struct sockaddr*)socketAddress, &addrLen)) < 0 && errno == EINTR);
#endif
    if (accepted == -1)
    {
        *acceptedSocket = -1;
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

    assert(addrLen <= (socklen_t)*socketAddressLen);
    *socketAddressLen = (int32_t)addrLen;
    *acceptedSocket = accepted;
    return Error_SUCCESS;
}

int32_t SystemNative_Bind(intptr_t socket, int32_t protocolType, uint8_t* socketAddress, int32_t socketAddressLen)
{
    if (socketAddress == NULL || socketAddressLen < 0)
    {
        return Error_EFAULT;
    }   

    int fd = ToFileDescriptor(socket);

    // On Windows, Bind during TCP_WAIT is allowed.
    // On Unix, we set SO_REUSEADDR to get the same behavior.
    if (protocolType == ProtocolType_PT_TCP)
    {
        int optionValue = 1;
        setsockopt(fd, SOL_SOCKET, SO_REUSEADDR, &optionValue, sizeof(int));
    }

    int err = bind(
        fd,
        (struct sockaddr*)socketAddress,
#if BIND_ADDRLEN_UNSIGNED
        (socklen_t)socketAddressLen);
#else
        socketAddressLen);
#endif

    return err == 0 ? Error_SUCCESS : SystemNative_ConvertErrorPlatformToPal(errno);
}

int32_t SystemNative_Connect(intptr_t socket, uint8_t* socketAddress, int32_t socketAddressLen)
{
    if (socketAddress == NULL || socketAddressLen < 0)
    {
        return Error_EFAULT;
    }

    int fd = ToFileDescriptor(socket);

    int err;
    while ((err = connect(fd, (struct sockaddr*)socketAddress, (socklen_t)socketAddressLen)) < 0 && errno == EINTR);
    return err == 0 ? Error_SUCCESS : SystemNative_ConvertErrorPlatformToPal(errno);
}

int32_t SystemNative_GetPeerName(intptr_t socket, uint8_t* socketAddress, int32_t* socketAddressLen)
{
    if (socketAddress == NULL || socketAddressLen == NULL || *socketAddressLen < 0)
    {
        return Error_EFAULT;
    }

    int fd = ToFileDescriptor(socket);

    socklen_t addrLen = (socklen_t)*socketAddressLen;
    int err = getpeername(fd, (struct sockaddr*)socketAddress, &addrLen);
    if (err != 0)
    {
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

    assert(addrLen <= (socklen_t)*socketAddressLen);
    *socketAddressLen = (int32_t)addrLen;
    return Error_SUCCESS;
}

int32_t SystemNative_GetSockName(intptr_t socket, uint8_t* socketAddress, int32_t* socketAddressLen)
{
    if (socketAddress == NULL || socketAddressLen == NULL || *socketAddressLen < 0)
    {
        return Error_EFAULT;
    }

    int fd = ToFileDescriptor(socket);

    socklen_t addrLen = (socklen_t)*socketAddressLen;
    int err = getsockname(fd, (struct sockaddr*)socketAddress, &addrLen);
    if (err != 0)
    {
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

    assert(addrLen <= (socklen_t)*socketAddressLen);
    *socketAddressLen = (int32_t)addrLen;
    return Error_SUCCESS;
}

int32_t SystemNative_Listen(intptr_t socket, int32_t backlog)
{
    int fd = ToFileDescriptor(socket);
    int err = listen(fd, backlog);
    return err == 0 ? Error_SUCCESS : SystemNative_ConvertErrorPlatformToPal(errno);
}

int32_t SystemNative_Shutdown(intptr_t socket, int32_t socketShutdown)
{
    int fd = ToFileDescriptor(socket);

    int how;
    switch (socketShutdown)
    {
        case SocketShutdown_SHUT_READ:
            how = SHUT_RD;
            break;

        case SocketShutdown_SHUT_WRITE:
            how = SHUT_WR;
            break;

        case SocketShutdown_SHUT_BOTH:
            how = SHUT_RDWR;
            break;

        default:
            return Error_EINVAL;
    }

    int err = shutdown(fd, how);
    return err == 0 ? Error_SUCCESS : SystemNative_ConvertErrorPlatformToPal(errno);
}

int32_t SystemNative_GetSocketErrorOption(intptr_t socket, int32_t* error)
{
    if (error == NULL)
    {
        return Error_EFAULT;
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
    return Error_SUCCESS;
}

static bool TryGetPlatformSocketOption(int32_t socketOptionName, int32_t socketOptionLevel, int* optLevel, int* optName)
{
    switch (socketOptionName)
    {
        case SocketOptionLevel_SOL_SOCKET:
            *optLevel = SOL_SOCKET;

            switch (socketOptionLevel)
            {
                case SocketOptionName_SO_DEBUG:
                    *optName = SO_DEBUG;
                    return true;

                case SocketOptionName_SO_ACCEPTCONN:
                    *optName = SO_ACCEPTCONN;
                    return true;

                case SocketOptionName_SO_REUSEADDR:
                    *optName = SO_REUSEADDR;
                    return true;

                case SocketOptionName_SO_KEEPALIVE:
                    *optName = SO_KEEPALIVE;
                    return true;

                case SocketOptionName_SO_DONTROUTE:
                    *optName = SO_DONTROUTE;
                    return true;

                case SocketOptionName_SO_BROADCAST:
                    *optName = SO_BROADCAST;
                    return true;

                // case SocketOptionName_SO_USELOOPBACK:

                case SocketOptionName_SO_LINGER:
                    *optName = SO_LINGER;
                    return true;

                case SocketOptionName_SO_OOBINLINE:
                    *optName = SO_OOBINLINE;
                    return true;

                // case SocketOptionName_SO_DONTLINGER:

                // case SocketOptionName_SO_EXCLUSIVEADDRUSE:

                case SocketOptionName_SO_SNDBUF:
                    *optName = SO_SNDBUF;
                    return true;

                case SocketOptionName_SO_RCVBUF:
                    *optName = SO_RCVBUF;
                    return true;

                case SocketOptionName_SO_SNDLOWAT:
                    *optName = SO_SNDLOWAT;
                    return true;

                case SocketOptionName_SO_RCVLOWAT:
                    *optName = SO_RCVLOWAT;
                    return true;

                case SocketOptionName_SO_SNDTIMEO:
                    *optName = SO_SNDTIMEO;
                    return true;

                case SocketOptionName_SO_RCVTIMEO:
                    *optName = SO_RCVTIMEO;
                    return true;

                case SocketOptionName_SO_ERROR:
                    *optName = SO_ERROR;
                    return true;

                case SocketOptionName_SO_TYPE:
                    *optName = SO_TYPE;
                    return true;

                // case SocketOptionName_SO_MAXCONN:

                default:
                    return false;
            }

        case SocketOptionLevel_SOL_IP:
            *optLevel = IPPROTO_IP;

            switch (socketOptionLevel)
            {
                case SocketOptionName_SO_IP_OPTIONS:
                    *optName = IP_OPTIONS;
                    return true;

                case SocketOptionName_SO_IP_HDRINCL:
                    *optName = IP_HDRINCL;
                    return true;

                case SocketOptionName_SO_IP_TOS:
                    *optName = IP_TOS;
                    return true;

                case SocketOptionName_SO_IP_TTL:
                    *optName = IP_TTL;
                    return true;

                case SocketOptionName_SO_IP_MULTICAST_IF:
                    *optName = IP_MULTICAST_IF;
                    return true;

                case SocketOptionName_SO_IP_MULTICAST_TTL:
                    *optName = IP_MULTICAST_TTL;
                    return true;

                case SocketOptionName_SO_IP_MULTICAST_LOOP:
                    *optName = IP_MULTICAST_LOOP;
                    return true;

                case SocketOptionName_SO_IP_ADD_MEMBERSHIP:
                    *optName = IP_ADD_MEMBERSHIP;
                    return true;

                case SocketOptionName_SO_IP_DROP_MEMBERSHIP:
                    *optName = IP_DROP_MEMBERSHIP;
                    return true;

#ifdef IP_MTU_DISCOVER
                case SocketOptionName_SO_IP_DONTFRAGMENT:
                    *optName = IP_MTU_DISCOVER; // option values will also need to be translated
                    return true;
#endif

#ifdef IP_ADD_SOURCE_MEMBERSHIP
                case SocketOptionName_SO_IP_ADD_SOURCE_MEMBERSHIP:
                    *optName = IP_ADD_SOURCE_MEMBERSHIP;
                    return true;
#endif

#ifdef IP_DROP_SOURCE_MEMBERSHIP
                case SocketOptionName_SO_IP_DROP_SOURCE_MEMBERSHIP:
                    *optName = IP_DROP_SOURCE_MEMBERSHIP;
                    return true;
#endif

#ifdef IP_BLOCK_SOURCE
                case SocketOptionName_SO_IP_BLOCK_SOURCE:
                    *optName = IP_BLOCK_SOURCE;
                    return true;
#endif

#ifdef IP_UNBLOCK_SOURCE
                case SocketOptionName_SO_IP_UNBLOCK_SOURCE:
                    *optName = IP_UNBLOCK_SOURCE;
                    return true;
#endif

                case SocketOptionName_SO_IP_PKTINFO:
                    *optName = IP_PKTINFO;
                    return true;

                default:
                    return false;
            }

        case SocketOptionLevel_SOL_IPV6:
            *optLevel = IPPROTO_IPV6;

            switch (socketOptionLevel)
            {
                case SocketOptionName_SO_IPV6_HOPLIMIT:
                    *optName = IPV6_HOPLIMIT;
                    return true;

                // case SocketOptionName_SO_IPV6_PROTECTION_LEVEL:

                case SocketOptionName_SO_IPV6_V6ONLY:
                    *optName = IPV6_V6ONLY;
                    return true;

                case SocketOptionName_SO_IP_PKTINFO:
                    *optName = IPV6_RECVPKTINFO;
                    return true;

                case SocketOptionName_SO_IP_MULTICAST_IF:
                    *optName = IPV6_MULTICAST_IF;
                    return true;

                default:
                    return false;
            }

        case SocketOptionLevel_SOL_TCP:
            *optLevel = IPPROTO_TCP;

            switch (socketOptionLevel)
            {
                case SocketOptionName_SO_TCP_NODELAY:
                    *optName = TCP_NODELAY;
                    return true;

                // case SocketOptionName_SO_TCP_BSDURGENT:

                default:
                    return false;
            }

        case SocketOptionLevel_SOL_UDP:
            *optLevel = IPPROTO_UDP;

            switch (socketOptionLevel)
            {
                // case SocketOptionName_SO_UDP_NOCHECKSUM:

                // case SocketOptionName_SO_UDP_CHECKSUM_COVERAGE:

                // case SocketOptionName_SO_UDP_UPDATEACCEPTCONTEXT:

                // case SocketOptionName_SO_UDP_UPDATECONNECTCONTEXT:

                default:
                    return false;
            }

        default:
            return false;
    }
}

int32_t SystemNative_GetSockOpt(
    intptr_t socket, int32_t socketOptionLevel, int32_t socketOptionName, uint8_t* optionValue, int32_t* optionLen)
{
    if (optionLen == NULL || *optionLen < 0)
    {
        return Error_EFAULT;
    }

    int fd = ToFileDescriptor(socket);

    //
    // Handle some special cases for compatibility with Windows
    //
    if (socketOptionLevel == SocketOptionLevel_SOL_SOCKET)
    {
        if (socketOptionName == SocketOptionName_SO_EXCLUSIVEADDRUSE || socketOptionName == SocketOptionName_SO_REUSEADDR)
        {
            if (*optionLen != sizeof(int32_t))
            {
                return Error_EINVAL;
            }

            socklen_t optLen = (socklen_t)*optionLen;
            // On Unix, SO_REUSEPORT controls the ability to bind multiple sockets to the same address.
            int err = getsockopt(fd, SOL_SOCKET, SO_REUSEPORT, optionValue, &optLen);

            if (err != 0)
            {
                return SystemNative_ConvertErrorPlatformToPal(errno);
            }

            int value = *(int32_t*)optionValue;

            // macOS returns non-zero values other than 1.
            value = value == 0 ? 0 : 1;

            // SocketOptionName_SO_EXCLUSIVEADDRUSE is inverse of SocketOptionName_SO_REUSEADDR (see comment in SystemNative_SetSockOpt).
            if (socketOptionName == SocketOptionName_SO_EXCLUSIVEADDRUSE)
            {
                value = value == 0 ? 1 : 0;
            }
            *(int32_t*)optionValue = value;

            return Error_SUCCESS;
        }
    }

    int optLevel, optName;
    if (!TryGetPlatformSocketOption(socketOptionLevel, socketOptionName, &optLevel, &optName))
    {
        return Error_ENOTSUP;
    }

    socklen_t optLen = (socklen_t)*optionLen;
    int err = getsockopt(fd, optLevel, optName, optionValue, &optLen);
    if (err != 0)
    {
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

#ifdef IP_MTU_DISCOVER
    // Handle some special cases for compatibility with Windows
    if (socketOptionLevel == SocketOptionLevel_SOL_IP)
    {
        if (socketOptionName == SocketOptionName_SO_IP_DONTFRAGMENT)
        {
            *optionValue = *optionValue == IP_PMTUDISC_DO ? 1 : 0;
        }
    }
#endif

    assert(optLen <= (socklen_t)*optionLen);
    *optionLen = (int32_t)optLen;
    return Error_SUCCESS;
}

int32_t
SystemNative_SetSockOpt(intptr_t socket, int32_t socketOptionLevel, int32_t socketOptionName, uint8_t* optionValue, int32_t optionLen)
{
    if (optionLen < 0 || optionValue == NULL)
    {
        return Error_EFAULT;
    }

    int fd = ToFileDescriptor(socket);

    //
    // Handle some special cases for compatibility with Windows
    //
    if (socketOptionLevel == SocketOptionLevel_SOL_SOCKET)
    {
        // Windows supports 3 address sharing modes:
        // - not sharing      (SO_EXCLUSIVEADDRUSE=1, SO_REUSEADDR=0)
        // - explicit sharing (SO_EXCLUSIVEADDRUSE=0, SO_REUSEADDR=1)
        // - implicit sharing (SO_EXCLUSIVEADDRUSE=0, SO_REUSEADDR=0)
        // On Unix we have two address sharing modes:
        // - not sharing      (SO_REUSEPORT=0)
        // - explicit sharing (SO_REUSEPORT=1)
        // We make both SocketOptionName_SO_REUSEADDR and SocketOptionName_SO_EXCLUSIVEADDRUSE control SO_REUSEPORT.
        if (socketOptionName == SocketOptionName_SO_EXCLUSIVEADDRUSE || socketOptionName == SocketOptionName_SO_REUSEADDR)
        {
            if (optionLen != sizeof(int32_t))
            {
                return Error_EINVAL;
            }

            int value = *(int32_t*)optionValue;

            // SocketOptionName_SO_EXCLUSIVEADDRUSE is inverse of SocketOptionName_SO_REUSEADDR.
            if (socketOptionName == SocketOptionName_SO_EXCLUSIVEADDRUSE)
            {
                if ((value != 0) && (value != 1))
                {
                    return Error_EINVAL;
                }
                else
                {
                    value = value == 0 ? 1 : 0;
                }
            }

            int err = setsockopt(fd, SOL_SOCKET, SO_REUSEPORT, &value, (socklen_t)optionLen);
            return err == 0 ? Error_SUCCESS : SystemNative_ConvertErrorPlatformToPal(errno);
        }
    }
#ifdef IP_MTU_DISCOVER
    else if (socketOptionLevel == SocketOptionLevel_SOL_IP)
    {
        if (socketOptionName == SocketOptionName_SO_IP_DONTFRAGMENT)
        {
            *optionValue = *optionValue != 0 ? IP_PMTUDISC_DO : IP_PMTUDISC_DONT;
        }
    }
#endif

    int optLevel, optName;
    if (!TryGetPlatformSocketOption(socketOptionLevel, socketOptionName, &optLevel, &optName))
    {
        return Error_ENOTSUP;
    }

    int err = setsockopt(fd, optLevel, optName, optionValue, (socklen_t)optionLen);
    return err == 0 ? Error_SUCCESS : SystemNative_ConvertErrorPlatformToPal(errno);
}

static bool TryConvertSocketTypePalToPlatform(int32_t palSocketType, int* platformSocketType)
{
    assert(platformSocketType != NULL);

    switch (palSocketType)
    {
        case SocketType_SOCK_STREAM:
            *platformSocketType = SOCK_STREAM;
            return true;

        case SocketType_SOCK_DGRAM:
            *platformSocketType = SOCK_DGRAM;
            return true;

        case SocketType_SOCK_RAW:
            *platformSocketType = SOCK_RAW;
            return true;

        case SocketType_SOCK_RDM:
            *platformSocketType = SOCK_RDM;
            return true;

        case SocketType_SOCK_SEQPACKET:
            *platformSocketType = SOCK_SEQPACKET;
            return true;

        default:
            *platformSocketType = (int)palSocketType;
            return false;
    }
}

static bool TryConvertProtocolTypePalToPlatform(int32_t palProtocolType, int* platformProtocolType)
{
    assert(platformProtocolType != NULL);

    switch (palProtocolType)
    {
        case ProtocolType_PT_UNSPECIFIED:
            *platformProtocolType = 0;
            return true;

        case ProtocolType_PT_ICMP:
            *platformProtocolType = IPPROTO_ICMP;
            return true;

        case ProtocolType_PT_TCP:
            *platformProtocolType = IPPROTO_TCP;
            return true;

        case ProtocolType_PT_UDP:
            *platformProtocolType = IPPROTO_UDP;
            return true;

        case ProtocolType_PT_ICMPV6:
            *platformProtocolType = IPPROTO_ICMPV6;
            return true;

        default:
            *platformProtocolType = (int)palProtocolType;
            return false;
    }
}

int32_t SystemNative_Socket(int32_t addressFamily, int32_t socketType, int32_t protocolType, intptr_t* createdSocket)
{
    if (createdSocket == NULL)
    {
        return Error_EFAULT;
    }

    sa_family_t platformAddressFamily;
    int platformSocketType, platformProtocolType;

    if (!TryConvertAddressFamilyPalToPlatform(addressFamily, &platformAddressFamily))
    {
        *createdSocket = -1;
        return Error_EAFNOSUPPORT;
    }

    if (!TryConvertSocketTypePalToPlatform(socketType, &platformSocketType))
    {
        *createdSocket = -1;
        return Error_EPROTOTYPE;
    }

    if (!TryConvertProtocolTypePalToPlatform(protocolType, &platformProtocolType))
    {
        *createdSocket = -1;
        return Error_EPROTONOSUPPORT;
    }

#ifdef SOCK_CLOEXEC
    platformSocketType |= SOCK_CLOEXEC;
#endif
    *createdSocket = socket(platformAddressFamily, platformSocketType, platformProtocolType);
    return *createdSocket != -1 ? Error_SUCCESS : SystemNative_ConvertErrorPlatformToPal(errno);
}

int32_t SystemNative_GetAtOutOfBandMark(intptr_t socket, int32_t* atMark)
{
    if (atMark == NULL)
    {
        return Error_EFAULT;
    }

    int fd = ToFileDescriptor(socket);

    int result;
    int err;
    while ((err = ioctl(fd, SIOCATMARK, &result)) < 0 && errno == EINTR);
    if (err == -1)
    {
        *atMark = 0;
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

    *atMark = (int32_t)result;
    return Error_SUCCESS;
}

int32_t SystemNative_GetBytesAvailable(intptr_t socket, int32_t* available)
{
    if (available == NULL)
    {
        return Error_EFAULT;
    }

    int fd = ToFileDescriptor(socket);

    int result;
    int err;
    while ((err = ioctl(fd, FIONREAD, &result)) < 0 && errno == EINTR);
    if (err == -1)
    {
        *available = 0;
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

    *available = (int32_t)result;
    return Error_SUCCESS;
}

#if HAVE_EPOLL

static const size_t SocketEventBufferElementSize = sizeof(struct epoll_event) > sizeof(struct SocketEvent) ? sizeof(struct epoll_event) : sizeof(struct SocketEvent);

static enum SocketEvents GetSocketEvents(uint32_t events)
{
    int asyncEvents = (((events & EPOLLIN) != 0) ? SocketEvents_SA_READ : 0) | (((events & EPOLLOUT) != 0) ? SocketEvents_SA_WRITE : 0) |
                      (((events & EPOLLRDHUP) != 0) ? SocketEvents_SA_READCLOSE : 0) |
                      (((events & EPOLLHUP) != 0) ? SocketEvents_SA_CLOSE : 0) | (((events & EPOLLERR) != 0) ? SocketEvents_SA_ERROR : 0);

    return (enum SocketEvents)asyncEvents;
}

static uint32_t GetEPollEvents(enum SocketEvents events)
{
    return (((events & SocketEvents_SA_READ) != 0) ? EPOLLIN : 0) | (((events & SocketEvents_SA_WRITE) != 0) ? EPOLLOUT : 0) |
           (((events & SocketEvents_SA_READCLOSE) != 0) ? EPOLLRDHUP : 0) | (((events & SocketEvents_SA_CLOSE) != 0) ? EPOLLHUP : 0) |
           (((events & SocketEvents_SA_ERROR) != 0) ? EPOLLERR : 0);
}

static int32_t CreateSocketEventPortInner(int32_t* port)
{
    assert(port != NULL);

    int epollFd = epoll_create1(EPOLL_CLOEXEC);
    if (epollFd == -1)
    {
        *port = -1;
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

    *port = epollFd;
    return Error_SUCCESS;
}

static int32_t CloseSocketEventPortInner(int32_t port)
{
    int err = close(port);
    return err == 0 || (err < 0 && errno == EINTR) ? Error_SUCCESS : SystemNative_ConvertErrorPlatformToPal(errno);
}

static int32_t TryChangeSocketEventRegistrationInner(
    int32_t port, int32_t socket, enum SocketEvents currentEvents, enum SocketEvents newEvents, uintptr_t data)
{
    assert(currentEvents != newEvents);

    int op = EPOLL_CTL_MOD;
    if (currentEvents == SocketEvents_SA_NONE)
    {
        op = EPOLL_CTL_ADD;
    }
    else if (newEvents == SocketEvents_SA_NONE)
    {
        op = EPOLL_CTL_DEL;
    }

    struct epoll_event evt;
    memset(&evt, 0, sizeof(struct epoll_event));
    evt.events = GetEPollEvents(newEvents) | (unsigned int)EPOLLET;
    evt.data.ptr = (void*)data;
    int err = epoll_ctl(port, op, socket, &evt);
    return err == 0 ? Error_SUCCESS : SystemNative_ConvertErrorPlatformToPal(errno);
}

static void ConvertEventEPollToSocketAsync(struct SocketEvent* sae, struct epoll_event* epoll)
{
    assert(sae != NULL);
    assert(epoll != NULL);

    // epoll does not play well with disconnected connection-oriented sockets, frequently
    // reporting spurious EPOLLHUP events. Fortunately, EPOLLHUP may be handled as an
    // EPOLLIN | EPOLLOUT event: the usual processing for these events will recognize and
    // handle the HUP condition.
    uint32_t events = epoll->events;
    if ((events & EPOLLHUP) != 0)
    {
        events = (events & ((uint32_t)~EPOLLHUP)) | EPOLLIN | EPOLLOUT;
    }

    memset(sae, 0, sizeof(struct SocketEvent));
    sae->Data = (uintptr_t)epoll->data.ptr;
    sae->Events = GetSocketEvents(events);
}

static int32_t WaitForSocketEventsInner(int32_t port, struct SocketEvent* buffer, int32_t* count)
{
    assert(buffer != NULL);
    assert(count != NULL);
    assert(*count >= 0);

    struct epoll_event* events = (struct epoll_event*)buffer;
    int numEvents;
    while ((numEvents = epoll_wait(port, events, *count, -1)) < 0 && errno == EINTR);
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
    assert(numEvents <= *count);

    if (sizeof(struct epoll_event) < sizeof(struct SocketEvent))
    {
        // Copy backwards to avoid overwriting earlier data.
        for (int i = numEvents - 1; i >= 0; i--)
        {
            // This copy is made deliberately to avoid overwriting data.
            struct epoll_event evt = events[i];
            ConvertEventEPollToSocketAsync(&buffer[i], &evt);
        }
    }
    else
    {
        // Copy forwards for better cache behavior
        for (int i = 0; i < numEvents; i++)
        {
            // This copy is made deliberately to avoid overwriting data.
            struct epoll_event evt = events[i];
            ConvertEventEPollToSocketAsync(&buffer[i], &evt);
        }
    }

    *count = numEvents;
    return Error_SUCCESS;
}

#elif HAVE_KQUEUE

c_static_assert(sizeof(struct SocketEvent) <= sizeof(struct kevent));
static const size_t SocketEventBufferElementSize = sizeof(struct kevent);

static enum SocketEvents GetSocketEvents(int16_t filter, uint16_t flags)
{
    int32_t events;
    switch (filter)
    {
        case EVFILT_READ:
            events = SocketEvents_SA_READ;
            if ((flags & EV_EOF) != 0)
            {
                events |= SocketEvents_SA_READCLOSE;
            }
            break;

        case EVFILT_WRITE:
            events = SocketEvents_SA_WRITE;

            // kqueue does not play well with disconnected connection-oriented sockets, frequently
            // reporting spurious EOF events. Fortunately, EOF may be handled as an EVFILT_READ |
            // EVFILT_WRITE event: the usual processing for these events will recognize and
            // handle the EOF condition.
            if ((flags & EV_EOF) != 0)
            {
                events |= SocketEvents_SA_READ;
            }
            break;

        default:
            assert_msg(0, "unexpected kqueue filter type", (int)filter);
            return SocketEvents_SA_NONE;
    }

    if ((flags & EV_ERROR) != 0)
    {
        events |= SocketEvents_SA_ERROR;
    }

    return (enum SocketEvents)events;
}

static int32_t CreateSocketEventPortInner(int32_t* port)
{
    assert(port != NULL);

    int kqueueFd = kqueue();
    if (kqueueFd == -1)
    {
        *port = -1;
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

    *port = kqueueFd;
    return Error_SUCCESS;
}

static int32_t CloseSocketEventPortInner(int32_t port)
{
    int err = close(port);
    return err == 0 || (err < 0 && errno == EINTR) ? Error_SUCCESS : SystemNative_ConvertErrorPlatformToPal(errno);
}

static int32_t TryChangeSocketEventRegistrationInner(
    int32_t port, int32_t socket, enum SocketEvents currentEvents, enum SocketEvents newEvents, uintptr_t data)
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
    int8_t readChanged = (changes & SocketEvents_SA_READ) != 0;
    int8_t writeChanged = (changes & SocketEvents_SA_WRITE) != 0;

    struct kevent events[2];

    int i = 0;
    if (readChanged)
    {
        EV_SET(&events[i++],
               (uint64_t)socket,
               EVFILT_READ,
               (newEvents & SocketEvents_SA_READ) == 0 ? RemoveFlags : AddFlags,
               0,
               0,
               GetKeventUdata(data));
    }

    if (writeChanged)
    {
        EV_SET(&events[i++],
               (uint64_t)socket,
               EVFILT_WRITE,
               (newEvents & SocketEvents_SA_WRITE) == 0 ? RemoveFlags : AddFlags,
               0,
               0,
               GetKeventUdata(data));
    }

    int err;
    while ((err = kevent(port, events, GetKeventNchanges(i), NULL, 0, NULL)) < 0 && errno == EINTR);
    return err == 0 ? Error_SUCCESS : SystemNative_ConvertErrorPlatformToPal(errno);
}

static int32_t WaitForSocketEventsInner(int32_t port, struct SocketEvent* buffer, int32_t* count)
{
    assert(buffer != NULL);
    assert(count != NULL);
    assert(*count >= 0);

    struct kevent* events = (struct kevent*)buffer;
    int numEvents;
    while ((numEvents = kevent(port, NULL, 0, events, GetKeventNchanges(*count), NULL)) < 0 && errno == EINTR);
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
    assert(numEvents <= *count);

    for (int i = 0; i < numEvents; i++)
    {
        // This copy is made deliberately to avoid overwriting data.
        struct kevent evt = events[i];
        memset(&buffer[i], 0, sizeof(struct SocketEvent));
        buffer[i].Data = GetSocketEventData(evt.udata);
        buffer[i].Events = GetSocketEvents(GetKeventFilter(evt.filter), GetKeventFlags(evt.flags));
    }

    *count = numEvents;
    return Error_SUCCESS;
}

#else
#error Asynchronous sockets require epoll or kqueue support.
#endif

int32_t SystemNative_CreateSocketEventPort(intptr_t* port)
{
    if (port == NULL)
    {
        return Error_EFAULT;
    }

    int fd;
    int32_t error = CreateSocketEventPortInner(&fd);
    *port = fd;
    return error;
}

int32_t SystemNative_CloseSocketEventPort(intptr_t port)
{
    return CloseSocketEventPortInner(ToFileDescriptor(port));
}

int32_t SystemNative_CreateSocketEventBuffer(int32_t count, struct SocketEvent** buffer)
{
    if (buffer == NULL || count < 0)
    {
        return Error_EFAULT;
    }

    size_t bufferSize;
    if (!multiply_s(SocketEventBufferElementSize, (size_t)count, &bufferSize) ||
        (*buffer = (struct SocketEvent*)malloc(SocketEventBufferElementSize * (size_t)count)) == NULL)
    {
        return Error_ENOMEM;
    }

    return Error_SUCCESS;
}

int32_t SystemNative_FreeSocketEventBuffer(struct SocketEvent* buffer)
{
    free(buffer);
    return Error_SUCCESS;
}

int32_t
SystemNative_TryChangeSocketEventRegistration(intptr_t port, intptr_t socket, int32_t currentEvents, int32_t newEvents, uintptr_t data)
{
    int portFd = ToFileDescriptor(port);
    int socketFd = ToFileDescriptor(socket);

    const int32_t SupportedEvents = SocketEvents_SA_READ | SocketEvents_SA_WRITE | SocketEvents_SA_READCLOSE | SocketEvents_SA_CLOSE | SocketEvents_SA_ERROR;

    if ((currentEvents & ~SupportedEvents) != 0 || (newEvents & ~SupportedEvents) != 0)
    {
        return Error_EINVAL;
    }

    if (currentEvents == newEvents)
    {
        return Error_SUCCESS;
    }

    return TryChangeSocketEventRegistrationInner(
        portFd, socketFd, (enum SocketEvents)currentEvents, (enum SocketEvents)newEvents, data);
}

int32_t SystemNative_WaitForSocketEvents(intptr_t port, struct SocketEvent* buffer, int32_t* count)
{
    if (buffer == NULL || count == NULL || *count < 0)
    {
        return Error_EFAULT;
    }

    int fd = ToFileDescriptor(port);

    return WaitForSocketEventsInner(fd, buffer, count);
}

int32_t SystemNative_PlatformSupportsDualModeIPv4PacketInfo(void)
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
    while (1)
    {
        char *buffer = (char*)malloc(bufferLength);
        if (buffer == NULL)
            return NULL;

        struct passwd pw;
        struct passwd* result;
        if (getpwuid_r(uid, &pw, buffer, bufferLength, &result) == 0)
        {
            if (result == NULL)
            {
                errno = ENOENT;
                free(buffer);
                return NULL;
            }
            else
            {
                char* name = strdup(pw.pw_name);
                free(buffer);
                return name;
            }
        }

        free(buffer);
        size_t tmpBufferLength;
        if (errno != ERANGE || !multiply_s(bufferLength, (size_t)2, &tmpBufferLength))
        {
            return NULL;
        }
        bufferLength = tmpBufferLength;
    }
}

char* SystemNative_GetPeerUserName(intptr_t socket)
{
    uid_t euid;
    return SystemNative_GetPeerID(socket, &euid) == 0 ?
        GetNameFromUid(euid) :
        NULL;
}

void SystemNative_GetDomainSocketSizes(int32_t* pathOffset, int32_t* pathSize, int32_t* addressSize)
{
    assert(pathOffset != NULL);
    assert(pathSize != NULL);
    assert(addressSize != NULL);

    struct sockaddr_un domainSocket;

    *pathOffset = offsetof(struct sockaddr_un, sun_path);
    *pathSize = sizeof(domainSocket.sun_path);
    *addressSize = sizeof(domainSocket);
}

int32_t SystemNative_SendFile(intptr_t out_fd, intptr_t in_fd, int64_t offset, int64_t count, int64_t* sent)
{
    assert(sent != NULL);

    int outfd = ToFileDescriptor(out_fd);
    int infd = ToFileDescriptor(in_fd);

#if HAVE_SENDFILE_4
    off_t offtOffset = (off_t)offset;

    ssize_t res;
    while ((res = sendfile(outfd, infd, &offtOffset, (size_t)count)) < 0 && errno == EINTR);
    if (res != -1)
    {
        *sent = res;
        return Error_SUCCESS;
    }

    *sent = 0;
    return SystemNative_ConvertErrorPlatformToPal(errno);

#elif HAVE_SENDFILE_6
    *sent = 0;
    while (1) // in case we need to retry for an EINTR
    {
        off_t len = count;
        ssize_t res = sendfile(infd, outfd, (off_t)offset, &len, NULL, 0);
        assert(len >= 0);

        // If the call succeeded, store the number of bytes sent, and return.  We add
        // rather than copy len because a previous call to sendfile could have sent bytes
        // but been interrupted by EINTR, in which case we need to add to that.
        if (res != -1)
        {
            *sent += len;
            return Error_SUCCESS;
        }

        // We got an error. If sendfile "fails" with EINTR or EAGAIN, it may have sent
        // some data that needs to be counted.
        if (errno == EAGAIN || errno == EINTR)
        {
            *sent += len;
            offset += len;
            count -= len;

            // If we actually transferred everything in spite of the error, return success.
            assert(count >= 0);
            if (count == 0) return Error_SUCCESS;

            // For EINTR, loop around and go again.
            if (errno == EINTR) continue;
        }

        // For everything other than EINTR, bail.
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

#else    
    // If we ever need to run on a platform that doesn't have sendfile,
    // we can implement this with a simple read/send loop.  For now,
    // we just mark it as not supported.
    (void)outfd;
    (void)infd;
    (void)offset;
    (void)count;
    *sent = 0;
    errno = ENOTSUP;
    return SystemNative_ConvertErrorPlatformToPal(errno);
#endif
}
