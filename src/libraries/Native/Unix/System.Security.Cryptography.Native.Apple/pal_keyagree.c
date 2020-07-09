// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_keyagree.h"

int32_t
AppleCryptoNative_EcdhKeyAgree(SecKeyRef privateKey, SecKeyRef publicKey, CFDataRef* pAgreeOut, CFErrorRef* pErrorOut)
{
    if (pAgreeOut != NULL)
        *pAgreeOut = NULL;
    if (pErrorOut != NULL)
        *pErrorOut = NULL;

    if (privateKey == NULL || publicKey == NULL)
        return kErrorBadInput;

    CFDictionaryRef dict = NULL;

    *pAgreeOut =
        SecKeyCopyKeyExchangeResult(privateKey, kSecKeyAlgorithmECDHKeyExchangeStandard, publicKey, dict, pErrorOut);

    if (*pErrorOut != NULL)
        return kErrorSeeError;

    return *pAgreeOut != NULL;
}
