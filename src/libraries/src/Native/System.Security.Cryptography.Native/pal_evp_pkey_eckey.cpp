// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_evp_pkey_eckey.h"

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" EC_KEY* EvpPkeyGetEcKey(EVP_PKEY* pkey)
{
    return CryptoNative_EvpPkeyGetEcKey(pkey);
}

extern "C" EC_KEY* CryptoNative_EvpPkeyGetEcKey(EVP_PKEY* pkey)
{
    return EVP_PKEY_get1_EC_KEY(pkey);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t EvpPkeySetEcKey(EVP_PKEY* pkey, EC_KEY* key)
{
    return CryptoNative_EvpPkeySetEcKey(pkey, key);
}

extern "C" int32_t CryptoNative_EvpPkeySetEcKey(EVP_PKEY* pkey, EC_KEY* key)
{
    return EVP_PKEY_set1_EC_KEY(pkey, key);
}
