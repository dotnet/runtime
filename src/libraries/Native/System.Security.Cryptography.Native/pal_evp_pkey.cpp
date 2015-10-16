// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_evp_pkey.h"

extern "C" EVP_PKEY* EvpPkeyCreate()
{
    return EVP_PKEY_new();
}

extern "C" void EvpPkeyDestroy(EVP_PKEY* pkey)
{
    if (pkey != nullptr)
    {
        EVP_PKEY_free(pkey);
    }
}

extern "C" int32_t UpRefEvpPkey(EVP_PKEY* pkey)
{
    if (!pkey)
    {
        return 0;
    }

    return CRYPTO_add(&pkey->references, 1, CRYPTO_LOCK_EVP_PKEY);
}
