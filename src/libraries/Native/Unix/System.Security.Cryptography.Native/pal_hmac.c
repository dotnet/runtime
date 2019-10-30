// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_config.h"
#include "pal_utilities.h"
#include "pal_hmac.h"

#include <assert.h>

HMAC_CTX* CryptoNative_HmacCreate(const uint8_t* key, int32_t keyLen, const EVP_MD* md)
{
    assert(key != NULL || keyLen == 0);
    assert(keyLen >= 0);
    assert(md != NULL);

    HMAC_CTX* ctx = HMAC_CTX_new();
    if (ctx == NULL)
    {
        // Allocation failed
        return NULL;
    }

    // NOTE: We can't pass NULL as empty key since HMAC_Init_ex will interpret
    // that as request to reuse the "existing" key.
    uint8_t _;
    if (keyLen == 0)
        key = &_;

    int ret = HMAC_Init_ex(ctx, key, keyLen, md, NULL);

    if (!ret)
    {
        HMAC_CTX_free(ctx);
        return NULL;
    }

    return ctx;
}

void CryptoNative_HmacDestroy(HMAC_CTX* ctx)
{
    if (ctx != NULL)
    {
        HMAC_CTX_free(ctx);
    }
}

int32_t CryptoNative_HmacReset(HMAC_CTX* ctx)
{
    assert(ctx != NULL);

    return HMAC_Init_ex(ctx, NULL, 0, NULL, NULL);
}

int32_t CryptoNative_HmacUpdate(HMAC_CTX* ctx, const uint8_t* data, int32_t len)
{
    assert(ctx != NULL);
    assert(data != NULL || len == 0);
    assert(len >= 0);

    if (len < 0)
    {
        return 0;
    }

    return HMAC_Update(ctx, data, Int32ToSizeT(len));
}

int32_t CryptoNative_HmacFinal(HMAC_CTX* ctx, uint8_t* md, int32_t* len)
{
    assert(ctx != NULL);
    assert(len != NULL);
    assert(md != NULL || *len == 0);
    assert(*len >= 0);

    if (len == NULL || *len < 0)
    {
        return 0;
    }

    unsigned int unsignedLen = Int32ToUint32(*len);
    int ret = HMAC_Final(ctx, md, &unsignedLen);
    *len = Uint32ToInt32(unsignedLen);
    return ret;
}
