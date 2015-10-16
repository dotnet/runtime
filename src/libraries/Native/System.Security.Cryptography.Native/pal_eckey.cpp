// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_eckey.h"

#include <assert.h>
#include <openssl/objects.h>

extern "C" void EcKeyDestroy(EC_KEY* r)
{
    EC_KEY_free(r);
}

extern "C" EC_KEY* EcKeyCreateByCurveName(int32_t nid)
{
    return EC_KEY_new_by_curve_name(nid);
}

extern "C" int32_t EcKeyGenerateKey(EC_KEY* eckey)
{
    return EC_KEY_generate_key(eckey);
}

extern "C" int32_t EcKeyUpRef(EC_KEY* r)
{
    return EC_KEY_up_ref(r);
}

extern "C" int32_t EcKeyGetCurveName(const EC_KEY* key)
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
