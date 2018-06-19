// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_eckey.h"

#include <assert.h>

void CryptoNative_EcKeyDestroy(EC_KEY* r)
{
    EC_KEY_free(r);
}

// For backwards compatibility
EC_KEY* CryptoNative_EcKeyCreateByCurveName(int32_t nid)
{
    return EC_KEY_new_by_curve_name(nid);
}

EC_KEY* CryptoNative_EcKeyCreateByOid(const char* oid)
{
    // oid can be friendly name or value
    int nid = OBJ_txt2nid(oid);
    return CryptoNative_EcKeyCreateByCurveName(nid);
}

int32_t CryptoNative_EcKeyGenerateKey(EC_KEY* eckey)
{
    if (!EC_KEY_generate_key(eckey))
        return 0;

    return EC_KEY_check_key(eckey);
}

int32_t CryptoNative_EcKeyUpRef(EC_KEY* r)
{
    return EC_KEY_up_ref(r);
}

int32_t CryptoNative_EcKeyGetSize(const EC_KEY* key, int32_t* keySize)
{
    if (!keySize)
        return 0;
    
    *keySize = 0;

    if (!key)
        return 0;

    const EC_GROUP* group = EC_KEY_get0_group(key);
    if (!group)
        return 0;

    *keySize = EC_GROUP_get_degree(group);

    return 1;
}

// For backwards compatibility
int32_t CryptoNative_EcKeyGetCurveName(const EC_KEY* key)
{
    if (key == NULL)
    {
        return NID_undef;
    }

    const EC_GROUP* group = EC_KEY_get0_group(key);
    if (group == NULL)
    {
        return NID_undef;
    }

    return EC_GROUP_get_curve_name(group);
}

int32_t CryptoNative_EcKeyGetCurveName2(const EC_KEY* key, int32_t* nidName)
{
    if (!nidName)
        return 0;

    *nidName = NID_undef;

    if (!key)
        return 0;

    const EC_GROUP* group = EC_KEY_get0_group(key);
    if (!group)
        return 0;

    *nidName = EC_GROUP_get_curve_name(group);
    return 1;
}
