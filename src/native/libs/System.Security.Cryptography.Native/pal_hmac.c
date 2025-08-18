// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_config.h"
#include "pal_utilities.h"
#include "pal_hmac.h"

#include <assert.h>

static EVP_MAC* g_evpMacHmac = NULL;
static pthread_once_t g_evpMacHmacInit = PTHREAD_ONCE_INIT;

static void EnsureMacHmac(void)
{
#ifdef NEED_OPENSSL_3_0
    if (API_EXISTS(EVP_MAC_fetch))
    {
        g_evpMacHmac = EVP_MAC_fetch(NULL, "HMAC", NULL);
        return;
    }
#endif

    g_evpMacHmac = NULL;
}

#define HAVE_EVP_MAC g_evpMacHmac == NULL
#define ENSURE_DN_MAC_CONSISTENCY(ctx) \
    do \
    { \
        assert((ctx->legacy == NULL) != (ctx->mac == NULL)); \
    } \
    while (0)

DN_MAC_CTX* CryptoNative_HmacCreate(uint8_t* key, int32_t keyLen, const EVP_MD* md)
{
    assert(key != NULL || keyLen == 0);
    assert(keyLen >= 0);
    assert(md != NULL);

    pthread_once(&g_evpMacHmacInit, EnsureMacHmac);
    ERR_clear_error();

    // NOTE: We can't pass NULL as empty key since HMAC_Init_ex will interpret
    // that as request to reuse the "existing" key.
    uint8_t _;
    if (keyLen == 0)
    {
        key = &_;
    }

#ifdef NEED_OPENSSL_3_0
    if (HAVE_EVP_MAC)
    {
        assert(API_EXISTS(EVP_MAC_CTX_new));
        assert(API_EXISTS(EVP_MAC_init));
        assert(API_EXISTS(EVP_MD_get0_name));
        assert(API_EXISTS(OSSL_PARAM_construct_octet_string));
        assert(API_EXISTS(OSSL_PARAM_construct_utf8_string));
        assert(API_EXISTS(OSSL_PARAM_construct_end));

        EVP_MAC_CTX* evpMac = EVP_MAC_CTX_new(g_evpMacHmac);

        if (evpMac == NULL)
        {
            return NULL;
        }

        const char* algorithm = EVP_MD_get0_name(md);
        char* algorithmDup = strdup(algorithm);

        if (algorithmDup == NULL)
        {
            EVP_MAC_CTX_free(evpMac);
            return NULL;
        }

        size_t keyLenT = Int32ToSizeT(keyLen);

        OSSL_PARAM params[] =
        {
            OSSL_PARAM_construct_octet_string(OSSL_MAC_PARAM_KEY, (void*) key, keyLenT),
            OSSL_PARAM_construct_utf8_string(OSSL_MAC_PARAM_DIGEST, algorithmDup, 0),
            OSSL_PARAM_construct_end(),
        };

        if (!EVP_MAC_init(evpMac, NULL, 0, params))
        {
            EVP_MAC_CTX_free(evpMac);
            free(algorithmDup);
            return NULL;
        }

        free(algorithmDup);

        DN_MAC_CTX* dnCtx = malloc(sizeof(DN_MAC_CTX));

        if (dnCtx == NULL)
        {
            return NULL;
        }

        memset(dnCtx, 0, sizeof(DN_MAC_CTX));
        dnCtx->mac = evpMac;
        return dnCtx;
    }
#endif

    HMAC_CTX* ctx = HMAC_CTX_new();

    if (ctx == NULL)
    {
        // Allocation failed
        // This is one of the few places that don't report the error to the queue, so
        // we'll do it here.
        ERR_put_error(ERR_LIB_EVP, 0, ERR_R_MALLOC_FAILURE, __FILE__, __LINE__);
        return NULL;
    }

    int ret = HMAC_Init_ex(ctx, key, keyLen, md, NULL);

    if (!ret)
    {
        HMAC_CTX_free(ctx);
        return NULL;
    }

    DN_MAC_CTX* dnCtx = malloc(sizeof(DN_MAC_CTX));

    if (dnCtx == NULL)
    {
        return NULL;
    }

    memset(dnCtx, 0, sizeof(DN_MAC_CTX));
    dnCtx->legacy = ctx;
    return dnCtx;
}

void CryptoNative_HmacDestroy(DN_MAC_CTX* ctx)
{
    if (ctx != NULL)
    {
        ENSURE_DN_MAC_CONSISTENCY(ctx);

#ifdef NEED_OPENSSL_3_0
        if (HAVE_EVP_MAC && ctx->mac)
        {
            EVP_MAC_CTX_free(ctx->mac);
            ctx->mac = NULL;
        }
#endif
        if (ctx->legacy)
        {
            HMAC_CTX_free(ctx->legacy);
            ctx->legacy = NULL;
        }

        OPENSSL_free(ctx);
    }
}

int32_t CryptoNative_HmacReset(DN_MAC_CTX* ctx)
{
    assert(ctx != NULL);
    ENSURE_DN_MAC_CONSISTENCY(ctx);

    ERR_clear_error();

#ifdef NEED_OPENSSL_3_0
    if (HAVE_EVP_MAC)
    {
        assert(ctx->mac);
        return EVP_MAC_init(ctx->mac, NULL, 0, NULL);
    }
#endif

    if (ctx->legacy)
    {
        return HMAC_Init_ex(ctx->legacy, NULL, 0, NULL, NULL);
    }

    return -1;
}

int32_t CryptoNative_HmacUpdate(DN_MAC_CTX* ctx, const uint8_t* data, int32_t len)
{
    assert(ctx != NULL);
    assert(data != NULL || len == 0);
    assert(len >= 0);

    ENSURE_DN_MAC_CONSISTENCY(ctx);
    ERR_clear_error();

    if (len < 0)
    {
        return 0;
    }

#ifdef NEED_OPENSSL_3_0
    if (HAVE_EVP_MAC)
    {
        assert(ctx->mac);
        return EVP_MAC_update(ctx->mac, data, Int32ToSizeT(len));
    }
#endif

    if (ctx->legacy)
    {
        return HMAC_Update(ctx->legacy, data, Int32ToSizeT(len));
    }

    return -1;
}

int32_t CryptoNative_HmacFinal(DN_MAC_CTX* ctx, uint8_t* md, int32_t* len)
{
    assert(ctx != NULL);
    assert(len != NULL);
    assert(md != NULL || *len == 0);
    assert(*len >= 0);

    ENSURE_DN_MAC_CONSISTENCY(ctx);
    ERR_clear_error();

    if (len == NULL || *len < 0)
    {
        return 0;
    }

    int ret = -1;

#ifdef NEED_OPENSSL_3_0
    if (HAVE_EVP_MAC)
    {
        assert(ctx->mac);
        size_t outl = 0;
        size_t lenT = Int32ToSizeT(*len);
        ret = EVP_MAC_final(ctx->mac, md, &outl, lenT);
        assert(outl == lenT);
        *len = SizeTToInt32(outl);
        return ret;
    }
#endif

    if (ctx->legacy)
    {
        unsigned int unsignedLen = Int32ToUint32(*len);
        ret = HMAC_Final(ctx->legacy, md, &unsignedLen);
        *len = Uint32ToInt32(unsignedLen);
        return ret;
    }

    return ret;
}

DN_MAC_CTX* CryptoNative_HmacCopy(const DN_MAC_CTX* ctx)
{
    assert(ctx != NULL);
    ENSURE_DN_MAC_CONSISTENCY(ctx);

    ERR_clear_error();

#ifdef NEED_OPENSSL_3_0
    if (HAVE_EVP_MAC)
    {
        assert(ctx->mac);
        EVP_MAC_CTX* macDup = EVP_MAC_CTX_dup(ctx->mac);

        if (macDup == NULL)
        {
            return NULL;
        }

        DN_MAC_CTX* dnCtx = malloc(sizeof(DN_MAC_CTX));

        if (dnCtx == NULL)
        {
            return NULL;
        }

        memset(dnCtx, 0, sizeof(DN_MAC_CTX));
        dnCtx->mac = macDup;
        return dnCtx;
    }
#endif

    if (ctx->legacy == NULL)
    {
        return NULL;
    }

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
    if (!HMAC_CTX_copy(dup, (HMAC_CTX*)(ctx->legacy)))
#pragma clang diagnostic pop
    {
        HMAC_CTX_free(dup);
        return NULL;
    }

    DN_MAC_CTX* dnCtx = malloc(sizeof(DN_MAC_CTX));

    if (dnCtx == NULL)
    {
        return NULL;
    }

    memset(dnCtx, 0, sizeof(DN_MAC_CTX));
    dnCtx->legacy = dup;
    return dnCtx;
}

int32_t CryptoNative_HmacCurrent(const DN_MAC_CTX* ctx, uint8_t* md, int32_t* len)
{
    assert(ctx != NULL);
    assert(len != NULL);
    assert(md != NULL || *len == 0);
    assert(*len >= 0);

    ENSURE_DN_MAC_CONSISTENCY(ctx);
    ERR_clear_error();

    if (len == NULL || *len < 0)
    {
        return 0;
    }

    DN_MAC_CTX* dup = CryptoNative_HmacCopy(ctx);

    if (dup != NULL)
    {
        int ret = CryptoNative_HmacFinal(dup, md, len);
        CryptoNative_HmacDestroy(dup);
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
