// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_evp_pkey_rsa.h"

extern "C" RSA* CryptoNative_EvpPkeyGetRsa(EVP_PKEY* pkey)
{
    return EVP_PKEY_get1_RSA(pkey);
}

extern "C" int32_t CryptoNative_EvpPkeySetRsa(EVP_PKEY* pkey, RSA* rsa)
{
    return EVP_PKEY_set1_RSA(pkey, rsa);
}
