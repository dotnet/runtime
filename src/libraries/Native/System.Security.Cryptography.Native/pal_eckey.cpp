// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_eckey.h"

#include <assert.h>
#include <openssl/objects.h>

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" void EcKeyDestroy(EC_KEY* r)
{
    return CryptoNative_EcKeyDestroy(r);
}

extern "C" void CryptoNative_EcKeyDestroy(EC_KEY* r)
{
    EC_KEY_free(r);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" EC_KEY* EcKeyCreateByCurveName(int32_t nid)
{
    return CryptoNative_EcKeyCreateByCurveName(nid);
}

extern "C" EC_KEY* CryptoNative_EcKeyCreateByCurveName(int32_t nid)
{
    return EC_KEY_new_by_curve_name(nid);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t EcKeyGenerateKey(EC_KEY* eckey)
{
    return CryptoNative_EcKeyGenerateKey(eckey);
}

extern "C" int32_t CryptoNative_EcKeyGenerateKey(EC_KEY* eckey)
{
    return EC_KEY_generate_key(eckey);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t EcKeyUpRef(EC_KEY* r)
{
    return CryptoNative_EcKeyUpRef(r);
}

extern "C" int32_t CryptoNative_EcKeyUpRef(EC_KEY* r)
{
    return EC_KEY_up_ref(r);
}

// TODO: temporarily keeping the un-prefixed signature of this method  
// to keep tests running in CI. This will be removed once the managed assemblies  
// are synced up with the native assemblies.
extern "C" int32_t EcKeyGetCurveName(const EC_KEY* key)
{
    return CryptoNative_EcKeyGetCurveName(key);
}

extern "C" int32_t CryptoNative_EcKeyGetCurveName(const EC_KEY* key)
{
    if (key == nullptr)
    {
        return NID_undef;
    }

    const EC_GROUP* group = EC_KEY_get0_group(key);
    if (group == nullptr)
    {
        return NID_undef;
    }

    return EC_GROUP_get_curve_name(group);
}
