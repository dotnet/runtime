// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_keyagree.h"

extern "C" int32_t
AppleCryptoNative_EcdhKeyAgree(SecKeyRef privateKey, SecKeyRef publicKey, CFDataRef* pAgreeOut, CFErrorRef* pErrorOut)
{
    if (pAgreeOut != nullptr)
        *pAgreeOut = nullptr;
    if (pErrorOut != nullptr)
        *pErrorOut = nullptr;

    if (privateKey == nullptr || publicKey == nullptr)
        return kErrorBadInput;

    CFDictionaryRef dict = nullptr;

    *pAgreeOut =
        SecKeyCopyKeyExchangeResult(privateKey, kSecKeyAlgorithmECDHKeyExchangeStandard, publicKey, dict, pErrorOut);

    if (*pErrorOut != nullptr)
        return kErrorSeeError;

    return *pAgreeOut != nullptr;
}
