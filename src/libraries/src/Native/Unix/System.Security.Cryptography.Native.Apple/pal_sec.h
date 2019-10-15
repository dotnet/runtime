
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "pal_types.h"
#include "pal_compiler.h"

#include <Security/Security.h>

/*
Get an error message for an OSStatus error from the security library.

Returns NULL if no message is available for the code.
*/
DLLEXPORT CFStringRef AppleCryptoNative_SecCopyErrorMessageString(OSStatus osStatus);
