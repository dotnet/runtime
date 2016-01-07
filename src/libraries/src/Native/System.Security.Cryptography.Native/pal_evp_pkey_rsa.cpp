// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_evp_pkey_rsa.h"

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" RSA* EvpPkeyGetRsa(EVP_PKEY* pkey)
{
    return CryptoNative_EvpPkeyGetRsa(pkey);
}

extern "C" RSA* CryptoNative_EvpPkeyGetRsa(EVP_PKEY* pkey)
{
    return EVP_PKEY_get1_RSA(pkey);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t EvpPkeySetRsa(EVP_PKEY* pkey, RSA* rsa)
{
    return CryptoNative_EvpPkeySetRsa(pkey, rsa);
}

extern "C" int32_t CryptoNative_EvpPkeySetRsa(EVP_PKEY* pkey, RSA* rsa)
{
    return EVP_PKEY_set1_RSA(pkey, rsa);
}
