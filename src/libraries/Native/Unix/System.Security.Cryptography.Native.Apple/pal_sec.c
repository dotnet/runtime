// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_sec.h"

#ifndef TARGET_IOS
CFStringRef AppleCryptoNative_SecCopyErrorMessageString(int32_t osStatus)
{
    return SecCopyErrorMessageString(osStatus, NULL);
}
#endif
