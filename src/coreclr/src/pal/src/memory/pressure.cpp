// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal/palinternal.h"
#include "pal_assert.h"
#include "pal.h"

#include <poll.h>
#include <errno.h>

static HANDLE pollingThread;
static HANDLE notificationHandles[2];

static int CreatePSIMemTrigger(const char *type, int stallAmountMicro, int timeWindowMicro)
{
    int fd = open("/proc/pressure/memory", O_RDWR | O_NONBLOCK | O_CLOEXEC);

    if (fd < 0)
    {
        return fd;
    }

    char buffer[32];
    int r = snprintf(buffer, sizeof(buffer), "%s %d %d", type, stallAmountMicro, timeWindowMicro);
    if (r < 0 || r >= (int)sizeof(buffer))
    {
        close(fd);
        return -1;
    }

    while (TRUE)
    {
        if (write(fd, buffer, (size_t)r) < 0)
        {
            if (errno == EINTR)
            {
                continue;
            }

            close(fd);
            return -1;
        }
    }

    return fd;
}

static DWORD PALAPI PollingThreadProc(LPVOID lpParam)
{
    int fdLowMemTrigger, fdHighMemTrigger;

    fdLowMemTrigger = CreatePSIMemTrigger("some", 750000, 1000000);
    if (fdLowMemTrigger < 0)
    {
        return -1;
    }

    fdHighMemTrigger = CreatePSIMemTrigger("some", 5000, 1000000);
    if (fdHighMemTrigger < 0)
    {
        goto closeLowMemTriggerFd;
    }

    while (TRUE)
    {
        struct pollfd fds[] =
        {
            {fdLowMemTrigger, POLLPRI, 0},
            {fdHighMemTrigger, POLLPRI, 0},
        };
        int nFds = poll(fds, 2, -1);

        if (nFds < 0)
        {
            if (errno == EINTR)
            {
                continue;
            }

            break;
        }

        for (int i = 0; i < nFds; i++)
        {
            if (fds[i].revents & POLLERR)
            {
                continue;
            }

            if (fds[i].revents & POLLPRI)
            {
                if (fds[i].fd == fdLowMemTrigger)
                {
                    SetEvent(notificationHandles[LowMemoryResourceNotification]);
                }
                else if (fds[i].fd == fdHighMemTrigger)
                {
                    SetEvent(notificationHandles[HighMemoryResourceNotification]);
                }
            }
        }
    }

    close(fdHighMemTrigger);

closeLowMemTriggerFd:
    close(fdLowMemTrigger);
    return -1;
}

static BOOL CreateEventForType(MEMORY_RESOURCE_NOTIFICATION_TYPE type)
{
    static const LPCWSTR eventNamesByType[] =
    {
        W("LowMemoryResourceNotification"),
        W("HighMemoryResourceNotification"),
    };

    // Instead of being a HANDLE created by CreateEvent(), this would
    // ideally be something managed by the PAL in order to properly wrap the
    // trigger file descriptor, and the polling thread.  This way, if
    // CloseHandle() is called on this handle, its associated trigger file
    // descriptor would also be closed.
    //
    // SetHandleInformation() could be optionally be called in this HANDLE
    // to set HANDLE_FLAG_PROTECT_FROM_CLOSE, but it's not implemented by
    // the PAL at the moment.  The only place where
    // CreateMemoryResourceNotification() is used in the runtime is in the
    // VM, and it doesn't call CloseHandle() for this notification type.

    notificationHandles[type] = CreateEventW(/* lpEventAttributes */ nullptr,
                                             /* bManualReset */ FALSE,
                                             /* bInitialState */ FALSE,
                                             eventNamesByType[type]);

    return notificationHandles[type] != nullptr;
}

HANDLE CreateMemoryResourceNotification(MEMORY_RESOURCE_NOTIFICATION_TYPE notificationType)
{
    if (pollingThread == nullptr)
    {
        if (!CreateEventForType(LowMemoryResourceNotification))
        {
            return nullptr;
        }
        if (!CreateEventForType(HighMemoryResourceNotification))
        {
            goto closeLowMemoryNotificationHandle;
        }

        pollingThread = CreateThread(/* lpThreadAttributes */ nullptr,
                                     /* dwStackSize */ 0,
                                     PollingThreadProc,
                                     /* lpParameter */ nullptr,
                                     /* dwCreationFlags */ 0,
                                     /* lpThreadId */ nullptr);
        if (!pollingThread)
        {
            goto closeHighMemoryNotificationHandle;
        }
    }    

    _ASSERTE(notificationHandles[notificationType] != nullptr);
    return notificationHandles[notificationType];

closeHighMemoryNotificationHandle:
    CloseHandle(notificationHandles[HighMemoryResourceNotification]);
    notificationHandles[HighMemoryResourceNotification] = nullptr;

closeLowMemoryNotificationHandle:
    CloseHandle(notificationHandles[LowMemoryResourceNotification]);
    notificationHandles[LowMemoryResourceNotification] = nullptr;

    return nullptr;
}
