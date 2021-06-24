// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_evp_pkey_dsa.h"
#include "pal_utilities.h"
#include <assert.h>

EVP_PKEY* CryptoNative_EvpPKeyCreateDsa(DSA* currentKey)
{
    assert(currentKey != NULL);

    EVP_PKEY* pkey = EVP_PKEY_new();

    if (pkey == NULL)
    {
        return NULL;
    }

    if (!EVP_PKEY_set1_DSA(pkey, currentKey))
    {
        EVP_PKEY_free(pkey);
        return NULL;
    }

    return pkey;
}

EVP_PKEY* CryptoNative_DsaGenerateKey(int32_t keySize)
{
    EVP_PKEY_CTX* paramCtx = EVP_PKEY_CTX_new_id(EVP_PKEY_DSA, NULL);

    if (paramCtx == NULL)
    {
        return NULL;
    }

    EVP_PKEY* paramKey = NULL;
    EVP_PKEY* pkey = NULL;
    EVP_PKEY* ret = NULL;
    EVP_PKEY_CTX* keyCtx = NULL;

    // FIPS 186-4 4.2
    // int qbits = keySize > 1024 ? 256 : 160;

    if (EVP_PKEY_paramgen_init(paramCtx) == 1 && EVP_PKEY_CTX_set_dsa_paramgen_bits(paramCtx, keySize) == 1 &&
        // EVP_PKEY_CTX_set_dsa_paramgen_q_bits(paramCtx, qbits) == 1 &&
        // EVP_PKEY_CTX_ctrl(paramCtx, EVP_PKEY_DSA, EVP_PKEY_OP_PARAMGEN, EVP_PKEY_CTRL_DSA_PARAMGEN_Q_BITS, qbits,
        // NULL) == 1 &&
        EVP_PKEY_paramgen(paramCtx, &paramKey) == 1 && (keyCtx = EVP_PKEY_CTX_new(paramKey, NULL)) != NULL &&
        EVP_PKEY_keygen_init(keyCtx) == 1 && EVP_PKEY_keygen(keyCtx, &pkey) == 1)
    {
        ret = pkey;
        pkey = NULL;
    }

    if (paramKey != NULL)
    {
        EVP_PKEY_free(paramKey);
    }

    if (pkey != NULL)
    {
        EVP_PKEY_free(pkey);
    }

    if (paramCtx != NULL)
    {
        EVP_PKEY_CTX_free(paramCtx);
    }

    EVP_PKEY_CTX_free(keyCtx);
    return ret;
}

int32_t CryptoNative_DsaSizeQ(EVP_PKEY* pkey)
{
    assert(pkey != NULL);

    // TODO: OpenSSL 3: Use EVP_PKEY_get_bn_param(key, OSSL_PKEY_PARAM_FFC_Q, &q_out) to get Q.

    int ret = -1;
    DSA* dsa = EVP_PKEY_get0_DSA(pkey);

    if (dsa != NULL)
    {
        const BIGNUM* q = NULL;
        DSA_get0_pqg(dsa, NULL, &q, NULL);

        if (q != NULL)
        {
            ret = BN_num_bytes(q);
        }
    }

    return ret;
}

int32_t CryptoNative_DsaSignHash(
    EVP_PKEY* pkey, const uint8_t* hash, int32_t hashLen, uint8_t* destination, int32_t destinationLen)
{
    assert(pkey != NULL);
    assert(destination != NULL);

    int ret = -1;
    EVP_PKEY_CTX* ctx = EVP_PKEY_CTX_new(pkey, NULL);

    if (ctx == NULL || EVP_PKEY_sign_init(ctx) <= 0)
    {
        goto done;
    }

    // This check may no longer be needed on OpenSSL 3.0
    {
        DSA* dsa = EVP_PKEY_get0_DSA(pkey);

        // DSA_OpenSSL() returns a shared pointer, no need to free/cache.
        if (dsa != NULL)
        {
            if (DSA_get_method(dsa) == DSA_OpenSSL())
            {
                const BIGNUM* privKey;

                DSA_get0_key(dsa, NULL, &privKey);

                if (privKey == NULL)
                {
                    ERR_PUT_error(ERR_LIB_DSA, 0, DSA_R_MISSING_PARAMETERS, __FILE__, __LINE__);
                    goto done;
                }
            }
        }
    }

    size_t written = Int32ToSizeT(destinationLen);

    if (EVP_PKEY_sign(ctx, destination, &written, hash, Int32ToSizeT(hashLen)) > 0)
    {
        ret = SizeTToInt32(written);
    }

done:
    if (ctx != NULL)
    {
        EVP_PKEY_CTX_free(ctx);
    }

    return ret;
}
