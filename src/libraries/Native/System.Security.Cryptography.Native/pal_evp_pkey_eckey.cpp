// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_evp_pkey_eckey.h"

extern "C" EC_KEY* EvpPkeyGetEcKey(EVP_PKEY* pkey)
{
    return EVP_PKEY_get1_EC_KEY(pkey);
}

extern "C" int32_t EvpPkeySetEcKey(EVP_PKEY* pkey, EC_KEY* key)
{
    return EVP_PKEY_set1_EC_KEY(pkey, key);
}
