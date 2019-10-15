// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_evp_pkey_eckey.h"

EC_KEY* CryptoNative_EvpPkeyGetEcKey(EVP_PKEY* pkey)
{
    return EVP_PKEY_get1_EC_KEY(pkey);
}

int32_t CryptoNative_EvpPkeySetEcKey(EVP_PKEY* pkey, EC_KEY* key)
{
    return EVP_PKEY_set1_EC_KEY(pkey, key);
}
