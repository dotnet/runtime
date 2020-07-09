// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_evp_pkey_rsa.h"

RSA* CryptoNative_EvpPkeyGetRsa(EVP_PKEY* pkey)
{
    return EVP_PKEY_get1_RSA(pkey);
}

int32_t CryptoNative_EvpPkeySetRsa(EVP_PKEY* pkey, RSA* rsa)
{
    return EVP_PKEY_set1_RSA(pkey, rsa);
}
