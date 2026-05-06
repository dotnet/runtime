// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include <pal_utilities.h>
#include <sys/socket.h>

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
} SockerShutdown;

inline static int32_t Common_Shutdown(intptr_t socket, int32_t socketShutdown)
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
