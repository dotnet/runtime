// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_digest.h"
#include <Security/Security.h>

#if !defined(TARGET_IOS) && !defined(TARGET_TVOS)
/*
Filled the derivedKey buffer with PBKDF2 derived data.

Implemented by:
1) Validating input
2) Calling CCKeyDerivationPBKDF

password and salt may be NULL if their respective length parameter
is zero. When password is NULL, it will be replaced with a pointer to an empty
location.

Returns -1 on invalid input, or -2 if the prfAlgorithm is an unknown
or unsupported hash algorithm. On valid input, the return value
is 1 if successful, and 0 if unsuccessful.

Returns the result of SecKeychainCreate.

Output:
errorCode: Contains the CCStatus of the operation. This will contain the
error code when the call is unsuccessful with valid input.
*/
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
