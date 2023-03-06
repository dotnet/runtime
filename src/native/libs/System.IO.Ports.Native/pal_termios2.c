// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_types.h"
#include "pal_utilities.h"
#include <asm/termbits.h>
#include <asm/ioctls.h>
#include <sys/ioctl.h>

int SystemIoPortsNative_Termios2SetSpeed(int fd, int speed)
{
    struct termios2 tio2;
    if (ioctl(fd, TCGETS2, &tio2) < 0)
    {
        return -1;
    }

    tio2.c_cflag &= ~(CBAUD | CBAUDEX | ((CBAUD | CBAUDEX) << IBSHIFT));
    tio2.c_cflag |= BOTHER | (BOTHER << IBSHIFT);
    tio2.c_ospeed = speed;
    tio2.c_ispeed = speed;

    if (ioctl(fd, TCSETS2, &tio2) < 0)
    {
        return -1;
    }

    return 0;
}


