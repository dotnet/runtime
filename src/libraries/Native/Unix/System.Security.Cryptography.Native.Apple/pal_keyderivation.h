// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_digest.h"
#include <Security/Security.h>

#if !defined(TARGET_IOS) && !defined(TARGET_TVOS)
PALEXPORT int32_t AppleCryptoNative_Pbkdf2(PAL_HashAlgorithm prfAlgorithm,
                                           const char* password,
                                           int32_t passwordLen,
                                           const uint8_t* salt,
                                           int32_t saltLen,
                                           int32_t iterations,
                                           uint8_t* derivedKey,
                                           uint32_t derivedKeyLen,
                                           int32_t* errorCode);
#endif
