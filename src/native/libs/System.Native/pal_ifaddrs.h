// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#ifndef TARGET_ANDROID
#error The pal_ifaddrs.h shim is intended only for Android
#endif

// Android doesn't include the getifaddrs and freeifaddrs functions in older Bionic libc (pre API 24).
// In recent Android versions (Android 11+) the data returned by the getifaddrs function is not valid.
// This shim is a port of Xamarin Android's implementation of getifaddrs using Netlink.

#include "pal_compiler.h"
#include "pal_config.h"
#include "pal_types.h"

#include <sys/cdefs.h>
#include <netinet/in.h>
#include <sys/socket.h>

struct ifaddrs
{
    struct ifaddrs *ifa_next;
    char *ifa_name;
    unsigned int ifa_flags;
    struct sockaddr *ifa_addr;
    struct sockaddr *ifa_netmask;
    union
    {
        struct sockaddr *ifu_broadaddr;
        struct sockaddr *ifu_dstaddr;
    } ifa_ifu;
    void *ifa_data;
};

// Synonym for `ifa_ifu.ifu_broadaddr` in `struct ifaddrs`.
#define ifa_broadaddr ifa_ifu.ifu_broadaddr
// Synonym for `ifa_ifu.ifu_dstaddr` in `struct ifaddrs`.
#define ifa_dstaddr ifa_ifu.ifu_dstaddr

int getifaddrs (struct ifaddrs **ifap);
void freeifaddrs (struct ifaddrs *ifap);
