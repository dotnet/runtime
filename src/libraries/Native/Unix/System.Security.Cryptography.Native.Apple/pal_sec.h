// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_types.h"
#include "pal_compiler.h"

#include <Security/Security.h>

/*
Get an error message for an OSStatus error from the security library.

Returns NULL if no message is available for the code.
*/
PALEXPORT CFStringRef AppleCryptoNative_SecCopyErrorMessageString(OSStatus osStatus);
