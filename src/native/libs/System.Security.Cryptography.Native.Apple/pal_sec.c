// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_sec.h"

CFStringRef AppleCryptoNative_SecCopyErrorMessageString(OSStatus osStatus)
{
    return SecCopyErrorMessageString(osStatus, NULL);
}
