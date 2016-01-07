// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_evp_pkey.h"

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" EVP_PKEY* EvpPkeyCreate()
{
    return CryptoNative_EvpPkeyCreate();
}

extern "C" EVP_PKEY* CryptoNative_EvpPkeyCreate()
{
    return EVP_PKEY_new();
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" void EvpPkeyDestroy(EVP_PKEY* pkey)
{
    return CryptoNative_EvpPkeyDestroy(pkey);
}

extern "C" void CryptoNative_EvpPkeyDestroy(EVP_PKEY* pkey)
{
    if (pkey != nullptr)
    {
        EVP_PKEY_free(pkey);
    }
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t UpRefEvpPkey(EVP_PKEY* pkey)
{
    return CryptoNative_UpRefEvpPkey(pkey);
}

extern "C" int32_t CryptoNative_UpRefEvpPkey(EVP_PKEY* pkey)
{
    if (!pkey)
    {
        return 0;
    }

    return CRYPTO_add(&pkey->references, 1, CRYPTO_LOCK_EVP_PKEY);
}
