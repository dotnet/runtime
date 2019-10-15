// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

internal static partial class Interop
{
    // /usr/include/linux/if.h
    [Flags]
    internal enum LinuxNetDeviceFlags
    {
        IFF_UP = 1 << 0,            /* sysfs */
        IFF_BROADCAST = 1 << 1,     /* volatile */
        IFF_DEBUG = 1 << 2,         /* sysfs */
        IFF_LOOPBACK = 1 << 3,      /* volatile */
        IFF_POINTOPOINT = 1 << 4,   /* volatile */
        IFF_NOTRAILERS = 1 << 5,    /* sysfs */
        IFF_RUNNING = 1 << 6,       /* volatile */
        IFF_NOARP = 1 << 7,         /* sysfs */
        IFF_PROMISC = 1 << 8,       /* sysfs */
        IFF_ALLMULTI = 1 << 9,      /* sysfs */
        IFF_MASTER = 1 << 10,       /* volatile */
        IFF_SLAVE = 1 << 11,        /* volatile */
        IFF_MULTICAST = 1 << 12,    /* sysfs */
        IFF_PORTSEL = 1 << 13,      /* sysfs */
        IFF_AUTOMEDIA = 1 << 14,    /* sysfs */
        IFF_DYNAMIC = 1 << 15,      /* sysfs */
        IFF_LOWER_UP = 1 << 16,     /* volatile */
        IFF_DORMANT = 1 << 17,      /* volatile */
        IFF_ECHO = 1 << 18,         /* volatile */
    }
}
