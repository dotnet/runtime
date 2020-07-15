// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_sec.h"

#if !defined(TARGET_IOS) && !defined(TARGET_TVOS)
CFStringRef AppleCryptoNative_SecCopyErrorMessageString(int32_t osStatus)
{
    return SecCopyErrorMessageString(osStatus, NULL);
}
#endif
