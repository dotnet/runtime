// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <assert.h>
#include "pal_evp_pkey.h"

EVP_PKEY* CryptoNative_EvpPkeyCreate()
{
    return EVP_PKEY_new();
}

void CryptoNative_EvpPkeyDestroy(EVP_PKEY* pkey)
{
    if (pkey != NULL)
    {
        EVP_PKEY_free(pkey);
    }
}

int32_t CryptoNative_EvpPKeySize(EVP_PKEY* pkey)
{
    assert(pkey != NULL);
    return EVP_PKEY_size(pkey);
}

int32_t CryptoNative_UpRefEvpPkey(EVP_PKEY* pkey)
{
    if (!pkey)
    {
        return 0;
    }

    return EVP_PKEY_up_ref(pkey);
}
