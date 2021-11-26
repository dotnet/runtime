// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_sec.h"

CFStringRef AppleCryptoNative_SecCopyErrorMessageString(OSStatus osStatus)
{
    if (__builtin_available(iOS 11.3, tvOS 11.3, *))
    {
        return SecCopyErrorMessageString(osStatus, NULL);
    }

    return CFStringCreateWithFormat(NULL, NULL, CFSTR("OSStatus %d"), (int)osStatus);
}
