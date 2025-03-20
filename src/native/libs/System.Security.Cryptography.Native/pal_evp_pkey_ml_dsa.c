// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_evp_pkey.h"
#include "pal_evp_pkey_ml_dsa.h"
#include "pal_utilities.h"
#include "openssl.h"
#include <assert.h>

// ML-DSA-44 signature length is 2420 bytes.
#define SHORTEST_POSSIBLE_ML_DSA_SIG_LEN 2420

int32_t CryptoNative_IsSignatureAlgorithmAvailable(const char* algorithm)
{
#ifdef FEATURE_DISTRO_AGNOSTIC_SSL
    if (!API_EXISTS(EVP_PKEY_sign_message_init) ||
        !API_EXISTS(EVP_PKEY_verify_message_init))
    {
        return 0;
    }
#endif

    int32_t ret = 0;
#ifdef HAVE_OPENSSL_EVP_PKEY_SIGN_MESSAGE_INIT
    EVP_SIGNATURE* sigAlg = NULL;

    sigAlg = EVP_SIGNATURE_fetch(NULL, algorithm, NULL);
    if (sigAlg)
    {
        ret = 1;
        EVP_SIGNATURE_free(sigAlg);
    } 
#endif

    (void)algorithm;
    return ret;
}

EVP_PKEY* CryptoNative_MLDsaGenerateKey(const char* keyType, uint8_t* seed, int32_t seedLen)
{
#ifdef FEATURE_DISTRO_AGNOSTIC_SSL
    if (!API_EXISTS(EVP_PKEY_sign_message_init) ||
        !API_EXISTS(EVP_PKEY_verify_message_init))
    {
        return NULL;
    }
#endif

#ifdef HAVE_OPENSSL_EVP_PKEY_SIGN_MESSAGE_INIT
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
        OSSL_PARAM params[2];
        params[0] = OSSL_PARAM_construct_octet_string(OSSL_PKEY_PARAM_ML_DSA_SEED, (void*)seed, Int32ToSizeT(seedLen));
        params[1] = OSSL_PARAM_construct_end();

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

static bool IsValidMLDsaAlgorithm(const char* alg)
{
    return alg != NULL && (strcmp(alg, "ML-DSA-44") == 0 ||
                           strcmp(alg, "ML-DSA-65") == 0 ||
                           strcmp(alg, "ML-DSA-87") == 0);
}

int32_t CryptoNative_MLDsaSignPure(EVP_PKEY *pkey,
                                   void* extraHandle,
                                   uint8_t* msg, int32_t msgLen,
                                   uint8_t* context, int32_t contextLen,
                                   uint8_t* destination, int32_t destinationLen)
{
#ifdef FEATURE_DISTRO_AGNOSTIC_SSL
    if (!API_EXISTS(EVP_PKEY_sign_message_init) ||
        !API_EXISTS(EVP_PKEY_verify_message_init))
    {
        return -1;
    }
#endif

#ifdef HAVE_OPENSSL_EVP_PKEY_SIGN_MESSAGE_INIT
    ERR_clear_error();

    if (!pkey || !msg || !destination || msgLen < 0 || contextLen < 0 || destinationLen < SHORTEST_POSSIBLE_ML_DSA_SIG_LEN)
    {
        return -1;
    }

    const char* alg = EVP_PKEY_get0_type_name(pkey);
    EVP_SIGNATURE* sigAlg = NULL;
    EVP_PKEY_CTX* ctx = NULL;
    EvpPKeyExtraHandle* extra = (EvpPKeyExtraHandle*)extraHandle;
    OSSL_LIB_CTX* libCtx = extra ? extra->libCtx : NULL;

    if (!IsValidMLDsaAlgorithm(alg))
    {
        return -1;
    }

    int ret = -1;

    sigAlg = EVP_SIGNATURE_fetch(libCtx, alg, NULL);
    if (!sigAlg)
    {
        goto done;
    }

    ctx = EvpPKeyCtxCreateFromPKey(pkey, extraHandle);
    if (!ctx)
    {
        goto done;
    }

    OSSL_PARAM contextParams[2];
    if (context)
    {
        contextParams[0] = OSSL_PARAM_construct_octet_string(OSSL_SIGNATURE_PARAM_CONTEXT_STRING, (void*)context, Int32ToSizeT(contextLen));
        contextParams[1] = OSSL_PARAM_construct_end();
    }

    OSSL_PARAM* params = context ? contextParams : NULL;

    if (EVP_PKEY_sign_message_init(ctx, sigAlg, params) <= 0)
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
    if (sigAlg != NULL) EVP_SIGNATURE_free(sigAlg);
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
#ifdef FEATURE_DISTRO_AGNOSTIC_SSL
    if (!API_EXISTS(EVP_PKEY_sign_message_init) ||
        !API_EXISTS(EVP_PKEY_verify_message_init))
    {
        return -1;
    }
#endif

#ifdef HAVE_OPENSSL_EVP_PKEY_SIGN_MESSAGE_INIT
    ERR_clear_error();

    if (!pkey || !msg || !sig || msgLen < 0 || contextLen < 0 || sigLen < SHORTEST_POSSIBLE_ML_DSA_SIG_LEN)
    {
        return -1;
    }

    const char *alg = EVP_PKEY_get0_type_name(pkey);
    EVP_SIGNATURE* sigAlg = NULL;
    EVP_PKEY_CTX* ctx = NULL;
    EvpPKeyExtraHandle* extra = (EvpPKeyExtraHandle*)extraHandle;
    OSSL_LIB_CTX* libCtx = extra ? extra->libCtx : NULL;

    if (!IsValidMLDsaAlgorithm(alg))
    {
        return -1;
    }

    int ret = -1;

    sigAlg = EVP_SIGNATURE_fetch(libCtx, alg, NULL);
    if (!sigAlg)
    {
        goto done;
    }

    ctx = EvpPKeyCtxCreateFromPKey(pkey, extraHandle);
    if (!ctx)
    {
        goto done;
    }

    OSSL_PARAM contextParams[2];
    if (context)
    {
        contextParams[0] = OSSL_PARAM_construct_octet_string(OSSL_SIGNATURE_PARAM_CONTEXT_STRING, (void*)context, Int32ToSizeT(contextLen));
        contextParams[1] = OSSL_PARAM_construct_end();
    }

    OSSL_PARAM* params = context ? contextParams : NULL;

    if (EVP_PKEY_verify_message_init(ctx, sigAlg, params) <= 0)
    {
        goto done;
    }

    ret = EVP_PKEY_verify(ctx, sig, Int32ToSizeT(sigLen), msg, Int32ToSizeT(msgLen)) == 1;

done:
    if (sigAlg != NULL) EVP_SIGNATURE_free(sigAlg);
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
