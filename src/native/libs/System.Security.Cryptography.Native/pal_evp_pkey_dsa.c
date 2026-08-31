// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_evp_pkey_dsa.h"

DSA* CryptoNative_EvpPkeyGetDsa(EVP_PKEY* pkey)
{
    ERR_clear_error();
    return EVP_PKEY_get1_DSA(pkey);
}

int32_t CryptoNative_EvpPkeySetDsa(EVP_PKEY* pkey, DSA* dsa)
{
    ERR_clear_error();
    return EVP_PKEY_set1_DSA(pkey, dsa);
}
