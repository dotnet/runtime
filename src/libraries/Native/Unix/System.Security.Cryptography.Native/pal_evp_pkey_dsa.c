// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_evp_pkey_dsa.h"

DSA* CryptoNative_EvpPkeyGetDsa(EVP_PKEY* pkey)
{
    return EVP_PKEY_get1_DSA(pkey);
}

int32_t CryptoNative_EvpPkeySetDsa(EVP_PKEY* pkey, DSA* dsa)
{
    return EVP_PKEY_set1_DSA(pkey, dsa);
}
