// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_keyderivation.h"

#if !defined(TARGET_IOS) && !defined(TARGET_TVOS)

static int32_t PrfAlgorithmFromHashAlgorithm(PAL_HashAlgorithm hashAlgorithm, CCPseudoRandomAlgorithm* algorithm)
{
    if (algorithm == NULL)
        return 0;
    
    switch (hashAlgorithm)
    {
        case PAL_SHA1:
            *algorithm = kCCPRFHmacAlgSHA1;
            return 1;
        case PAL_SHA256:
            *algorithm = kCCPRFHmacAlgSHA256;
            return 1;
        case PAL_SHA384:
            *algorithm = kCCPRFHmacAlgSHA384;
            return 1;
        case PAL_SHA512:
            *algorithm = kCCPRFHmacAlgSHA512;
            return 1;
        default:
            *algorithm = 0;
            return 0;
    }
}

int32_t AppleCryptoNative_KeyDerivationPBKDF(PAL_HashAlgorithm prfAlgorithm,
                                             const char* password,
                                             int32_t passwordLen,
                                             const uint8_t* salt,
                                             int32_t saltLen,
                                             int32_t iterations,
                                             uint8_t* derivedKey,
                                             uint32_t derivedKeyLen,
                                             int32_t* errorCode)
{
    if (errorCode != NULL)
        *errorCode = noErr;

    if (password == NULL || passwordLen < 0 || salt == NULL || saltLen < 0 ||
        iterations < 0 || derivedKey == NULL || derivedKeyLen < 0 || errorCode == NULL)
    {
        return -1;
    }

    CCPseudoRandomAlgorithm prf;

    if (!PrfAlgorithmFromHashAlgorithm(prfAlgorithm, &prf))
    {
        return -2;
    }

    CCStatus result = CCKeyDerivationPBKDF(kCCPBKDF2, password, passwordLen, salt,
        saltLen, prf,  iterations, derivedKey, derivedKeyLen);
    *errorCode = result;
    return result == kCCSuccess ? 1 : 0;
}
#endif
