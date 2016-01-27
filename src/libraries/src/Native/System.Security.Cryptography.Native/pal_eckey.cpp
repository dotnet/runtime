// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_eckey.h"

#include <assert.h>
#include <openssl/objects.h>

extern "C" void CryptoNative_EcKeyDestroy(EC_KEY* r)
{
    EC_KEY_free(r);
}

extern "C" EC_KEY* CryptoNative_EcKeyCreateByCurveName(int32_t nid)
{
    return EC_KEY_new_by_curve_name(nid);
}

extern "C" int32_t CryptoNative_EcKeyGenerateKey(EC_KEY* eckey)
{
    return EC_KEY_generate_key(eckey);
}

extern "C" int32_t CryptoNative_EcKeyUpRef(EC_KEY* r)
{
    return EC_KEY_up_ref(r);
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
