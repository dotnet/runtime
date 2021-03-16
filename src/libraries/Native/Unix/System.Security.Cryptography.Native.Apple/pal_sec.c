// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_sec.h"

CFStringRef AppleCryptoNative_SecCopyErrorMessageString(int32_t osStatus)
{
#if (defined(TARGET_IOS) && __IPHONE_OS_VERSION_MIN_REQUIRED < __IPHONE_11_3) || (defined(TARGET_TVOS) && __IPHONE_OS_VERSION_MIN_REQUIRED < __TVOS_11_3)
    return CFStringCreateWithCString(NULL, "", kCFStringEncodingUTF8);
#else
    return SecCopyErrorMessageString(osStatus, NULL);
#endif
}
