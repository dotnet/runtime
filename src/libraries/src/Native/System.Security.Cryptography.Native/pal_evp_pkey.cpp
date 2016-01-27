// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_evp_pkey.h"

extern "C" EVP_PKEY* CryptoNative_EvpPkeyCreate()
{
    return EVP_PKEY_new();
}

extern "C" void CryptoNative_EvpPkeyDestroy(EVP_PKEY* pkey)
{
    if (pkey != nullptr)
    {
        EVP_PKEY_free(pkey);
    }
}

extern "C" int32_t CryptoNative_UpRefEvpPkey(EVP_PKEY* pkey)
{
    if (!pkey)
    {
        return 0;
    }

    return CRYPTO_add(&pkey->references, 1, CRYPTO_LOCK_EVP_PKEY);
}
