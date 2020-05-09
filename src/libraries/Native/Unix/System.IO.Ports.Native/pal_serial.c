// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_types.h"
#include "pal_utilities.h"
#include <fcntl.h>
#include <errno.h>
#include <pal_serial.h>
#include <termios.h>
#include <unistd.h>
#include <sys/ioctl.h>
#include <poll.h>
#include <stdlib.h>
#include <sys/socket.h>

// ENODATA is not defined in FreeBSD 10.3 but is defined in 11.0
#if defined(__FreeBSD__) & !defined(ENODATA)
#define ENODATA ENOATTR
#endif

/* Open device file in non-blocking mode and without controlling terminal */
intptr_t SystemIoPortsNative_SerialPortOpen(const char * name)
{
    intptr_t fd;
    while ((fd = open(name, O_RDWR | O_NOCTTY | O_CLOEXEC | O_NONBLOCK)) < 0 && errno == EINTR);

    if (fd < 0)
    {
        return fd;
    }

    if (ioctl(fd, TIOCEXCL) != 0)
    {
        // We couldn't get exclusive access to the device file
        int oldErrno = errno;
        close(fd);
        errno = oldErrno;
        return -1;
    }

    return fd;
}

int SystemIoPortsNative_SerialPortClose(intptr_t handle)
{
    int fd = ToFileDescriptor(handle);
    // some devices don't unlock handles from exclusive access
    // preventing reopening after closing the handle

    // ignoring the error - best effort
    ioctl(fd, TIOCNXCL);
    return close(fd);
}

int32_t ConvertErrorPlatformToPal(int32_t platformErrno)
{
    switch (platformErrno)
    {
        case 0:
            return Error_SUCCESS;
        case E2BIG:
            return Error_E2BIG;
        case EACCES:
            return Error_EACCES;
        case EADDRINUSE:
            return Error_EADDRINUSE;
        case EADDRNOTAVAIL:
            return Error_EADDRNOTAVAIL;
        case EAFNOSUPPORT:
            return Error_EAFNOSUPPORT;
        case EAGAIN:
            return Error_EAGAIN;
        case EALREADY:
            return Error_EALREADY;
        case EBADF:
            return Error_EBADF;
        case EBADMSG:
            return Error_EBADMSG;
        case EBUSY:
            return Error_EBUSY;
        case ECANCELED:
            return Error_ECANCELED;
        case ECHILD:
            return Error_ECHILD;
        case ECONNABORTED:
            return Error_ECONNABORTED;
        case ECONNREFUSED:
            return Error_ECONNREFUSED;
        case ECONNRESET:
            return Error_ECONNRESET;
        case EDEADLK:
            return Error_EDEADLK;
        case EDESTADDRREQ:
            return Error_EDESTADDRREQ;
        case EDOM:
            return Error_EDOM;
        case EDQUOT:
            return Error_EDQUOT;
        case EEXIST:
            return Error_EEXIST;
        case EFAULT:
            return Error_EFAULT;
        case EFBIG:
            return Error_EFBIG;
        case EHOSTUNREACH:
            return Error_EHOSTUNREACH;
        case EIDRM:
            return Error_EIDRM;
        case EILSEQ:
            return Error_EILSEQ;
        case EINPROGRESS:
            return Error_EINPROGRESS;
        case EINTR:
            return Error_EINTR;
        case EINVAL:
            return Error_EINVAL;
        case EIO:
            return Error_EIO;
        case EISCONN:
            return Error_EISCONN;
        case EISDIR:
            return Error_EISDIR;
        case ELOOP:
            return Error_ELOOP;
        case EMFILE:
            return Error_EMFILE;
        case EMLINK:
            return Error_EMLINK;
        case EMSGSIZE:
            return Error_EMSGSIZE;
        case EMULTIHOP:
            return Error_EMULTIHOP;
        case ENAMETOOLONG:
            return Error_ENAMETOOLONG;
        case ENETDOWN:
            return Error_ENETDOWN;
        case ENETRESET:
            return Error_ENETRESET;
        case ENETUNREACH:
            return Error_ENETUNREACH;
        case ENFILE:
            return Error_ENFILE;
        case ENOBUFS:
            return Error_ENOBUFS;
        case ENODEV:
            return Error_ENODEV;
        case ENOENT:
            return Error_ENOENT;
        case ENOEXEC:
            return Error_ENOEXEC;
        case ENOLCK:
            return Error_ENOLCK;
        case ENOLINK:
            return Error_ENOLINK;
        case ENOMEM:
            return Error_ENOMEM;
        case ENOMSG:
            return Error_ENOMSG;
        case ENOPROTOOPT:
            return Error_ENOPROTOOPT;
        case ENOSPC:
            return Error_ENOSPC;
        case ENOSYS:
            return Error_ENOSYS;
        case ENOTCONN:
            return Error_ENOTCONN;
        case ENOTDIR:
            return Error_ENOTDIR;
#if ENOTEMPTY != EEXIST // AIX defines this
        case ENOTEMPTY:
            return Error_ENOTEMPTY;
#endif
#ifdef ENOTRECOVERABLE // not available in NetBSD
        case ENOTRECOVERABLE:
            return Error_ENOTRECOVERABLE;
#endif
        case ENOTSOCK:
            return Error_ENOTSOCK;
        case ENOTSUP:
            return Error_ENOTSUP;
        case ENOTTY:
            return Error_ENOTTY;
        case ENXIO:
            return Error_ENXIO;
        case EOVERFLOW:
            return Error_EOVERFLOW;
#ifdef EOWNERDEAD // not available in NetBSD
        case EOWNERDEAD:
            return Error_EOWNERDEAD;
#endif
        case EPERM:
            return Error_EPERM;
        case EPIPE:
            return Error_EPIPE;
        case EPROTO:
            return Error_EPROTO;
        case EPROTONOSUPPORT:
            return Error_EPROTONOSUPPORT;
        case EPROTOTYPE:
            return Error_EPROTOTYPE;
        case ERANGE:
            return Error_ERANGE;
        case EROFS:
            return Error_EROFS;
        case ESPIPE:
            return Error_ESPIPE;
        case ESRCH:
            return Error_ESRCH;
        case ESTALE:
            return Error_ESTALE;
        case ETIMEDOUT:
            return Error_ETIMEDOUT;
        case ETXTBSY:
            return Error_ETXTBSY;
        case EXDEV:
            return Error_EXDEV;
#ifdef ESOCKTNOSUPPORT
        case ESOCKTNOSUPPORT:
            return Error_ESOCKTNOSUPPORT;
#endif
        case EPFNOSUPPORT:
            return Error_EPFNOSUPPORT;
        case ESHUTDOWN:
            return Error_ESHUTDOWN;
        case EHOSTDOWN:
            return Error_EHOSTDOWN;
        case ENODATA:
            return Error_ENODATA;

// #if because these will trigger duplicate case label warnings when
// they have the same value, which is permitted by POSIX and common.
#if EOPNOTSUPP != ENOTSUP
        case EOPNOTSUPP:
            return Error_EOPNOTSUPP;
#endif
#if EWOULDBLOCK != EAGAIN
        case EWOULDBLOCK:
            return Error_EWOULDBLOCK;
#endif
    }

    return Error_ENONSTANDARD;
}

int32_t SystemIoPortsNative_Read(intptr_t fd, void* buffer, int32_t bufferSize)
{
    assert(buffer != NULL || bufferSize == 0);
    assert(bufferSize >= 0);

    if (bufferSize < 0)
    {
        errno = EINVAL;
        return -1;
    }

    ssize_t count;
    while ((count = read(ToFileDescriptor(fd), buffer, (uint32_t)bufferSize)) < 0 && errno == EINTR);

    assert(count >= -1 && count <= bufferSize);
    return (int32_t)count;
}

int32_t SystemIoPortsNative_Write(intptr_t fd, const void* buffer, int32_t bufferSize)
{
    assert(buffer != NULL || bufferSize == 0);
    assert(bufferSize >= 0);

    if (bufferSize < 0)
    {
        errno = ERANGE;
        return -1;
    }

    ssize_t count;
    while ((count = write(ToFileDescriptor(fd), buffer, (uint32_t)bufferSize)) < 0 && errno == EINTR);

    assert(count >= -1 && count <= bufferSize);
    return (int32_t)count;
}

int32_t SystemIoPortsNative_Poll(PollEvent* pollEvents, uint32_t eventCount, int32_t milliseconds, uint32_t* triggered)
{
    if (pollEvents == NULL || triggered == NULL)
    {
        return Error_EFAULT;
    }

    if (milliseconds < -1)
    {
        return Error_EINVAL;
    }

    struct pollfd stackBuffer[(uint32_t)(2048/sizeof(struct pollfd))];
    int useStackBuffer = eventCount <= ARRAY_SIZE(stackBuffer);
    struct pollfd* pollfds = NULL;
    if (useStackBuffer)
    {
        pollfds = &stackBuffer[0];
    }
    else
    {
        pollfds = calloc(eventCount, sizeof(*pollfds));
        if (pollfds == NULL)
        {
            return Error_ENOMEM;
        }
    }

    for (uint32_t i = 0; i < eventCount; i++)
    {
        const PollEvent* event = &pollEvents[i];
        pollfds[i].fd = event->FileDescriptor;
        // we need to do this for platforms like AIX where PAL_POLL* doesn't
        // match up to their reality; this is PollEvent -> system polling
        switch (event->Events)
        {
            case PAL_POLLIN:
                pollfds[i].events = POLLIN;
                break;
            case PAL_POLLPRI:
                pollfds[i].events = POLLPRI;
                break;
            case PAL_POLLOUT:
                pollfds[i].events = POLLOUT;
                break;
            case PAL_POLLERR:
                pollfds[i].events = POLLERR;
                break;
            case PAL_POLLHUP:
                pollfds[i].events = POLLHUP;
                break;
            case PAL_POLLNVAL:
                pollfds[i].events = POLLNVAL;
                break;
            default:
                pollfds[i].events = event->Events;
                break;
        }
        pollfds[i].revents = 0;
    }

    int rv;
    while ((rv = poll(pollfds, (nfds_t)eventCount, milliseconds)) < 0 && errno == EINTR);

    if (rv < 0)
    {
        if (!useStackBuffer)
        {
            free(pollfds);
        }

        *triggered = 0;
        return ConvertErrorPlatformToPal(errno);
    }

    for (uint32_t i = 0; i < eventCount; i++)
    {
        const struct pollfd* pfd = &pollfds[i];
        assert(pfd->fd == pollEvents[i].FileDescriptor);
        assert(pfd->events == pollEvents[i].Events);

        // same as the other switch, just system -> PollEvent
        switch (pfd->revents)
        {
            case POLLIN:
                pollEvents[i].TriggeredEvents = PAL_POLLIN;
                break;
            case POLLPRI:
                pollEvents[i].TriggeredEvents = PAL_POLLPRI;
                break;
            case POLLOUT:
                pollEvents[i].TriggeredEvents = PAL_POLLOUT;
                break;
            case POLLERR:
                pollEvents[i].TriggeredEvents = PAL_POLLERR;
                break;
            case POLLHUP:
                pollEvents[i].TriggeredEvents = PAL_POLLHUP;
                break;
            case POLLNVAL:
                pollEvents[i].TriggeredEvents = PAL_POLLNVAL;
                break;
            default:
                pollEvents[i].TriggeredEvents = (int16_t)pfd->revents;
                break;
        }
    }

    *triggered = (uint32_t)rv;

    if (!useStackBuffer)
    {
        free(pollfds);
    }

    return Error_SUCCESS;
}

/*
 * Socket shutdown modes.
 *
 * NOTE: these values are taken from System.Net.SocketShutdown.
 */
typedef enum
{
    SocketShutdown_SHUT_READ = 0,  // SHUT_RD
    SocketShutdown_SHUT_WRITE = 1, // SHUT_WR
    SocketShutdown_SHUT_BOTH = 2,  // SHUT_RDWR
} SocketShutdown;

int32_t SystemIoPortsNative_Shutdown(intptr_t socket, int32_t socketShutdown)
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
    return err == 0 ? Error_SUCCESS : ConvertErrorPlatformToPal(errno);
}
