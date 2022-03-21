// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_types.h"
#include "pal_compiler.h"

#include <Security/Security.h>

// Unless another interpretation is "obvious", pal_seckey functions return 1 on success.
// functions which represent a boolean return 0 on "successful false"
// otherwise functions will return one of the following return values:
static const int32_t kErrorBadInput = -1;
static const int32_t kErrorSeeError = -2;
static const int32_t kErrorUnknownAlgorithm = -3;
static const int32_t kErrorUnknownState = -4;
static const int32_t kPlatformNotSupported = -5;

enum
{
    PAL_KeyAlgorithm_Unknown = 0,
    PAL_KeyAlgorithm_EC = 1,
    PAL_KeyAlgorithm_RSA = 2,
};
typedef uint32_t PAL_KeyAlgorithm;

/*
For RSA and DSA this function returns the number of bytes in "the key", which corresponds to
the length of n/Modulus for RSA and for P in DSA.

For ECC the value should not be used.

0 is returned for invalid inputs.
*/
PALEXPORT uint64_t AppleCryptoNative_SecKeyGetSimpleKeySizeInBytes(SecKeyRef publicKey);

/*
Create an iOS-style key from raw data.

Follows pal_seckey return conventions.
*/
PALEXPORT int32_t AppleCryptoNative_SecKeyCreateWithData(uint8_t* pKey,
                                                         int32_t cbKey,
                                                         PAL_KeyAlgorithm keyAlgorithm,
                                                         int32_t isPublic,
                                                         SecKeyRef* pKeyOut,
                                                         CFErrorRef* pErrorOut);

/*
Return an external key data representation.

For RSA keys this function returns the PKCS#1 public or private key.
For EC keys this function returns the ANSI X.963 representation.

Follows pal_seckey return conventions.
*/
PALEXPORT int32_t AppleCryptoNative_SecKeyCopyExternalRepresentation(SecKeyRef pKey,
                                                                     CFDataRef* ppDataOut,
                                                                     CFErrorRef* pErrorOut);

/*
Return a corresponding public key from a private key.
*/
PALEXPORT SecKeyRef AppleCryptoNative_SecKeyCopyPublicKey(SecKeyRef privateKey);
