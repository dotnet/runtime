// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_types.h"
#include <fcntl.h>
#include <errno.h>
#include <pal_serial.h>

/* Open device file in non-blocking mode and without controlling terminal */
intptr_t SystemIoPortsNative_SerialPortOpen(const char * name)
{
    intptr_t result;
    while ((result = open(name, O_RDWR | O_NOCTTY | O_CLOEXEC | O_NONBLOCK)) < 0 && errno == EINTR);
    return result;
}
