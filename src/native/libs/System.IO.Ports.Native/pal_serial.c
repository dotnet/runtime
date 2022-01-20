// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_types.h"
#include "pal_utilities.h"
#include <fcntl.h>
#include <errno.h>
#include <pal_serial.h>
#include <termios.h>
#include <unistd.h>
#include <sys/ioctl.h>
#include <stdlib.h>
#include <sys/socket.h>
#include <pal_networking_common.h>

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

int32_t SystemIoPortsNative_Read(intptr_t fd, void* buffer, int32_t bufferSize)
{
    return Common_Read(fd, buffer, bufferSize);
}

int32_t SystemIoPortsNative_Write(intptr_t fd, const void* buffer, int32_t bufferSize)
{
    return Common_Write(fd, buffer, bufferSize);
}

int32_t SystemIoPortsNative_Poll(PollEvent* pollEvents, uint32_t eventCount, int32_t milliseconds, uint32_t* triggered)
{
    return Common_Poll(pollEvents, eventCount, milliseconds, triggered);
}

int32_t SystemIoPortsNative_Shutdown(intptr_t socket, int32_t socketShutdown)
{
    return Common_Shutdown(socket, socketShutdown);
}

int32_t SystemIoPortsNative_ConvertErrorPlatformToPal(int32_t platformErrno)
{
    return ConvertErrorPlatformToPal(platformErrno);
}

int32_t SystemIoPortsNative_ConvertErrorPalToPlatform(int32_t error)
{
    return ConvertErrorPalToPlatform(error);
}

const char* SystemIoPortsNative_StrErrorR(int32_t platformErrno, char* buffer, int32_t bufferSize)
{
    return StrErrorR(platformErrno, buffer, bufferSize);
}
