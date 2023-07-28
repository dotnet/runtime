// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#ifndef TARGET_ANDROID
#error The pal_ifaddrs.h shim is intended only for Android
#endif

#if __ANDROID_API__ >= 24
#error The pal_ifaddrs.h shim is only necessary for Android API 21-23 and it should be removed now that the minimum supported API level is 24 or higher
#endif

// Android doesn't include the getifaddrs and freeifaddrs functions in older Bionic libc (pre API 24).
// This shim is a port of Xamarin Android's implementation of getifaddrs using Netlink.
// https://github.com/xamarin/xamarin-android/blob/681887ebdbd192ce7ce1cd02221d4939599ba762/src/monodroid/jni/xamarin_getifaddrs.h

#include <ifaddrs.h>

int _netlink_getifaddrs (struct ifaddrs **ifap);
void _netlink_freeifaddrs (struct ifaddrs *ifap);
