// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_config.h"
#include "pal_utilities.h"
#include "pal_hmac.h"

#include <assert.h>

HMAC_CTX* CryptoNative_HmacCreate(const uint8_t* key, int32_t keyLen, const EVP_MD* md)
{
    assert(key != NULL || keyLen == 0);
    assert(keyLen >= 0);
    assert(md != NULL);

    ERR_clear_error();

    HMAC_CTX* ctx = HMAC_CTX_new();

    if (ctx == NULL)
    {
        // Allocation failed
        // This is one of the few places that don't report the error to the queue, so
        // we'll do it here.
        ERR_put_error(ERR_LIB_EVP, 0, ERR_R_MALLOC_FAILURE, __FILE__, __LINE__);
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

    ERR_clear_error();

    return HMAC_Init_ex(ctx, NULL, 0, NULL, NULL);
}

int32_t CryptoNative_HmacUpdate(HMAC_CTX* ctx, const uint8_t* data, int32_t len)
{
    assert(ctx != NULL);
    assert(data != NULL || len == 0);
    assert(len >= 0);

    ERR_clear_error();

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

    ERR_clear_error();

    if (len == NULL || *len < 0)
    {
        return 0;
    }

    unsigned int unsignedLen = Int32ToUint32(*len);
    int ret = HMAC_Final(ctx, md, &unsignedLen);
    *len = Uint32ToInt32(unsignedLen);
    return ret;
}

static HMAC_CTX* HmacDup(const HMAC_CTX* ctx)
{
    assert(ctx != NULL);

    ERR_clear_error();

    HMAC_CTX* dup = HMAC_CTX_new();

    if (dup == NULL)
    {
        // This is one of the few places that don't report the error to the queue, so
        // we'll do it here.
        ERR_put_error(ERR_LIB_EVP, 0, ERR_R_MALLOC_FAILURE, __FILE__, __LINE__);
        return NULL;
    }

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wcast-qual"
    if (!HMAC_CTX_copy(dup, (HMAC_CTX*)ctx))
#pragma clang diagnostic pop
    {
        HMAC_CTX_free(dup);
        return NULL;
    }

    return dup;
}

int32_t CryptoNative_HmacCurrent(const HMAC_CTX* ctx, uint8_t* md, int32_t* len)
{
    assert(ctx != NULL);
    assert(len != NULL);
    assert(md != NULL || *len == 0);
    assert(*len >= 0);

    ERR_clear_error();

    if (len == NULL || *len < 0)
    {
        return 0;
    }

    HMAC_CTX* dup = HmacDup(ctx);

    if (dup != NULL)
    {
        int ret = CryptoNative_HmacFinal(dup, md, len);
        HMAC_CTX_free(dup);
        return ret;
    }

    return 0;
}

int32_t CryptoNative_HmacOneShot(const EVP_MD* type,
                                 const uint8_t* key,
                                 int32_t keySize,
                                 const uint8_t* source,
                                 int32_t sourceSize,
                                 uint8_t* md,
                                 int32_t* mdSize)
{
    assert(mdSize != NULL && type != NULL && md != NULL && mdSize != NULL);
    assert(keySize >= 0 && *mdSize >= 0);
    assert(key != NULL || keySize == 0);
    assert(source != NULL || sourceSize == 0);

    ERR_clear_error();

    uint8_t empty = 0;

    if (key == NULL)
    {
        if (keySize != 0)
        {
            return -1;
        }

        key = &empty;
    }

    unsigned int unsignedSource = Int32ToUint32(sourceSize);
    unsigned int unsignedSize = Int32ToUint32(*mdSize);
    unsigned char* result = HMAC(type, key, keySize, source, unsignedSource, md, &unsignedSize);
    *mdSize = Uint32ToInt32(unsignedSize);

    return result == NULL ? 0 : 1;
}
