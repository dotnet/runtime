// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <stdlib.h>
#include <assert.h>
#include <poll.h>
#include <pal_error_common.h>
#include <pal_utilities.h>

/**
 * Our intermediate pollfd struct to normalize the data types
 */
typedef struct
{
    int32_t FileDescriptor;  // The file descriptor to poll
    int16_t Events;          // The events to poll for
    int16_t TriggeredEvents; // The events that triggered the poll
} PollEvent;

/**
 * Constants passed to and from poll describing what to poll for and what
 * kind of data was received from poll.
 */
typedef enum
{
    PAL_POLLIN = 0x0001,   /* non-urgent readable data available */
    PAL_POLLPRI = 0x0002,  /* urgent readable data available */
    PAL_POLLOUT = 0x0004,  /* data can be written without blocked */
    PAL_POLLERR = 0x0008,  /* an error occurred */
    PAL_POLLHUP = 0x0010,  /* the file descriptor hung up */
    PAL_POLLNVAL = 0x0020, /* the requested events were invalid */
} PollEvents;

inline static int32_t Common_Read(intptr_t fd, void* buffer, int32_t bufferSize)
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

inline static int32_t Common_Write(intptr_t fd, const void* buffer, int32_t bufferSize)
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

inline static int16_t Common_ConvertPollEventsPalToPlatform(int16_t palEvents)
{
    // we need to do this for platforms like AIX where PAL_POLL* doesn't
    // match up to their reality; this is PollEvent -> system polling
    int16_t platformEvents = 0;

    if ((palEvents & PAL_POLLIN) != 0)
    {
        platformEvents |= POLLIN;
    }
#ifdef POLLPRI // not available in WASI
    if ((palEvents & PAL_POLLPRI) != 0)
    {
        platformEvents |= POLLPRI;
    }
#endif
    if ((palEvents & PAL_POLLOUT) != 0)
    {
        platformEvents |= POLLOUT;
    }
    if ((palEvents & PAL_POLLERR) != 0)
    {
        platformEvents |= POLLERR;
    }
    if ((palEvents & PAL_POLLHUP) != 0)
    {
        platformEvents |= POLLHUP;
    }
    if ((palEvents & PAL_POLLNVAL) != 0)
    {
        platformEvents |= POLLNVAL;
    }

    return platformEvents;
}

inline static int16_t Common_ConvertPollEventsPlatformToPal(int16_t platformEvents)
{
    // same as the other function, just system -> PollEvent
    int16_t palEvents = 0;

    if ((platformEvents & POLLIN) != 0)
    {
        palEvents |= PAL_POLLIN;
    }
#ifdef POLLPRI // not available in WASI
    if ((platformEvents & POLLPRI) != 0)
    {
        palEvents |= PAL_POLLPRI;
    }
#endif
    if ((platformEvents & POLLOUT) != 0)
    {
        palEvents |= PAL_POLLOUT;
    }
    if ((platformEvents & POLLERR) != 0)
    {
        palEvents |= PAL_POLLERR;
    }
    if ((platformEvents & POLLHUP) != 0)
    {
        palEvents |= PAL_POLLHUP;
    }
    if ((platformEvents & POLLNVAL) != 0)
    {
        palEvents |= PAL_POLLNVAL;
    }

    return palEvents;
}

inline static int32_t Common_Poll(PollEvent* pollEvents, uint32_t eventCount, int32_t milliseconds, uint32_t* triggered)
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
        pollfds = (struct pollfd*)calloc(eventCount, sizeof(*pollfds));
        if (pollfds == NULL)
        {
            return Error_ENOMEM;
        }
    }

    for (uint32_t i = 0; i < eventCount; i++)
    {
        const PollEvent* event = &pollEvents[i];
        pollfds[i].fd = event->FileDescriptor;
        pollfds[i].events = Common_ConvertPollEventsPalToPlatform(event->Events);
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
        assert(pfd->events == Common_ConvertPollEventsPalToPlatform(pollEvents[i].Events));
        pollEvents[i].TriggeredEvents = Common_ConvertPollEventsPlatformToPal(pfd->revents);
    }

    *triggered = (uint32_t)rv;

    if (!useStackBuffer)
    {
        free(pollfds);
    }

    return Error_SUCCESS;
}
