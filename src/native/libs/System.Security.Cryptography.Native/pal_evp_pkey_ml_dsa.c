// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_evp_pkey.h"
#include "pal_evp_pkey_ml_dsa.h"
#include "pal_utilities.h"
#include "openssl.h"
#include <assert.h>

int32_t CryptoNative_MLDsaGetPalId(const EVP_PKEY* pKey, int32_t* mldsaId)
{
#ifdef NEED_OPENSSL_3_0
    assert(pKey && mldsaId);

    if (API_EXISTS(EVP_PKEY_is_a))
    {
        ERR_clear_error();

        if (EVP_PKEY_is_a(pKey, "ML-DSA-44"))
        {
            *mldsaId = PalMLDsaId_MLDsa44;
        }
        else if (EVP_PKEY_is_a(pKey, "ML-DSA-65"))
        {
            *mldsaId = PalMLDsaId_MLDsa65;
        }
        else if (EVP_PKEY_is_a(pKey, "ML-DSA-87"))
        {
            *mldsaId = PalMLDsaId_MLDsa87;
        }
        else
        {
            *mldsaId = PalMLDsaId_Unknown;
        }

        return 1;
    }
#endif

    (void)pKey;
    *mldsaId = PalMLDsaId_Unknown;
    return 0;
}

EVP_PKEY* CryptoNative_MLDsaGenerateKey(const char* keyType, uint8_t* seed, int32_t seedLen)
{
#if defined(NEED_OPENSSL_3_0) && HAVE_OPENSSL_EVP_PKEY_SIGN_MESSAGE_INIT
    if (!API_EXISTS(EVP_PKEY_sign_message_init) ||
        !API_EXISTS(EVP_PKEY_verify_message_init))
    {
        return NULL;
    }

    ERR_clear_error();

    if (seed && seedLen != 32)
    {
        return NULL;
    }

    EVP_PKEY_CTX* pctx = EVP_PKEY_CTX_new_from_name(NULL, keyType, NULL);
    EVP_PKEY* pkey = NULL;

    if (!pctx)
    {
        return NULL;
    }

    if (EVP_PKEY_keygen_init(pctx) <= 0)
    {
        goto done;
    }

    if (seed)
    {
        OSSL_PARAM params[] =
        {
            OSSL_PARAM_construct_octet_string(OSSL_PKEY_PARAM_ML_DSA_SEED, (void*)seed, Int32ToSizeT(seedLen)),
            OSSL_PARAM_construct_end(),
        };

        if (EVP_PKEY_CTX_set_params(pctx, params) <= 0)
        {
            goto done;
        }
    }

    if (EVP_PKEY_keygen(pctx, &pkey) != 1 && pkey != NULL)
    {
        EVP_PKEY_free(pkey);
        pkey = NULL;
    }

done:
    if (pctx != NULL)
    {
        EVP_PKEY_CTX_free(pctx);
    }

    return pkey;
#else
    (void)keyType;
    (void)seed;
    (void)seedLen;
    return NULL;
#endif
}

int32_t CryptoNative_MLDsaSignPure(EVP_PKEY *pkey,
                                   void* extraHandle,
                                   uint8_t* msg, int32_t msgLen,
                                   uint8_t* context, int32_t contextLen,
                                   uint8_t* destination, int32_t destinationLen)
{
    assert(pkey);
    assert(msgLen >= 0);
    assert(contextLen >= 0);
    assert(destination);
    assert(destinationLen >= 2420 /* ML-DSA-44 signature size */);

#if defined(NEED_OPENSSL_3_0) && HAVE_OPENSSL_EVP_PKEY_SIGN_MESSAGE_INIT
    if (!API_EXISTS(EVP_PKEY_sign_message_init) ||
        !API_EXISTS(EVP_PKEY_verify_message_init))
    {
        return -1;
    }

    ERR_clear_error();

    EVP_PKEY_CTX* ctx = NULL;

    int ret = -1;

    ctx = EvpPKeyCtxCreateFromPKey(pkey, extraHandle);
    if (!ctx)
    {
        goto done;
    }

    OSSL_PARAM contextParams[] =
    {
        OSSL_PARAM_construct_end(),
        OSSL_PARAM_construct_end(),
    };

    if (context)
    {
        contextParams[0] = OSSL_PARAM_construct_octet_string(OSSL_SIGNATURE_PARAM_CONTEXT_STRING, (void*)context, Int32ToSizeT(contextLen));
    }

    if (EVP_PKEY_sign_message_init(ctx, NULL, contextParams) <= 0)
    {
        goto done;
    }

    size_t dstLen = Int32ToSizeT(destinationLen);
    if (EVP_PKEY_sign(ctx, destination, &dstLen, msg, Int32ToSizeT(msgLen)) == 1)
    {
        if (dstLen != Int32ToSizeT(destinationLen))
        {
            assert(false); // length mismatch
            goto done;
        }

        ret = 1;
    }
    else
    {
        ret = 0;
    }

done:
    if (ctx != NULL) EVP_PKEY_CTX_free(ctx);
    return ret;
#else
    (void)pkey;
    (void)extraHandle;
    (void)msg;
    (void)msgLen;
    (void)context;
    (void)contextLen;
    (void)destination;
    (void)destinationLen;
    return -1;
#endif
}

int32_t CryptoNative_MLDsaVerifyPure(EVP_PKEY *pkey,
                                     void* extraHandle,
                                     uint8_t* msg, int32_t msgLen,
                                     uint8_t* context, int32_t contextLen,
                                     uint8_t* sig, int32_t sigLen)
{
    assert(pkey);
    assert(msgLen >= 0);
    assert(sig);
    assert(sigLen >= 2420 /* ML-DSA-44 signature size */);
    assert(contextLen >= 0);

#if defined(NEED_OPENSSL_3_0) && HAVE_OPENSSL_EVP_PKEY_SIGN_MESSAGE_INIT
    if (!API_EXISTS(EVP_PKEY_sign_message_init) ||
        !API_EXISTS(EVP_PKEY_verify_message_init))
    {
        return -1;
    }

    ERR_clear_error();

    EVP_PKEY_CTX* ctx = NULL;

    int ret = -1;

    ctx = EvpPKeyCtxCreateFromPKey(pkey, extraHandle);
    if (!ctx)
    {
        goto done;
    }

    OSSL_PARAM contextParams[] =
    {
        OSSL_PARAM_construct_end(),
        OSSL_PARAM_construct_end(),
    };

    if (context)
    {
        contextParams[0] = OSSL_PARAM_construct_octet_string(OSSL_SIGNATURE_PARAM_CONTEXT_STRING, (void*)context, Int32ToSizeT(contextLen));
    }

    if (EVP_PKEY_verify_message_init(ctx, NULL, contextParams) <= 0)
    {
        goto done;
    }

    ret = EVP_PKEY_verify(ctx, sig, Int32ToSizeT(sigLen), msg, Int32ToSizeT(msgLen)) == 1;

done:
    if (ctx != NULL) EVP_PKEY_CTX_free(ctx);
    return ret;
#else
    (void)pkey;
    (void)extraHandle;
    (void)msg;
    (void)msgLen;
    (void)context;
    (void)contextLen;
    (void)sig;
    (void)sigLen;
    return -1;
#endif
}

int32_t CryptoNative_MLDsaExportSecretKey(const EVP_PKEY* pKey, uint8_t* destination, int32_t destinationLength)
{
    return EvpPKeyGetKeyOctetStringParam(pKey, OSSL_PKEY_PARAM_PRIV_KEY, destination, destinationLength);
}

int32_t CryptoNative_MLDsaExportSeed(const EVP_PKEY* pKey, uint8_t* destination, int32_t destinationLength)
{
    return EvpPKeyGetKeyOctetStringParam(pKey, OSSL_PKEY_PARAM_ML_DSA_SEED, destination, destinationLength);
}

int32_t CryptoNative_MLDsaExportPublicKey(const EVP_PKEY* pKey, uint8_t* destination, int32_t destinationLength)
{
    return EvpPKeyGetKeyOctetStringParam(pKey, OSSL_PKEY_PARAM_PUB_KEY, destination, destinationLength);
}
