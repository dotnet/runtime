// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_evp_pkey_ecdh.h"

EVP_PKEY_CTX* CryptoNative_EvpPKeyCtxCreate(EVP_PKEY* pkey, EVP_PKEY* peerkey, uint32_t* secretLength)
{
    if (secretLength != NULL)
        *secretLength = 0;

    if (pkey == NULL || peerkey == NULL || secretLength == NULL)
    {
        return NULL;
    }

    /* Create the context for the shared secret derivation */
    EVP_PKEY_CTX* ctx = EVP_PKEY_CTX_new(pkey, NULL);

    if (ctx == NULL)
    {
        return NULL;
    }

    size_t tmpLength = 0;

    /* Initialize, provide the peer public key, and determine the buffer size */
    if (1 != EVP_PKEY_derive_init(ctx) || 1 != EVP_PKEY_derive_set_peer(ctx, peerkey) ||
        1 != EVP_PKEY_derive(ctx, NULL, &tmpLength))
    {
        EVP_PKEY_CTX_free(ctx);
        return NULL;
    }

    *secretLength = (uint32_t)tmpLength;
    return ctx;
}

int32_t CryptoNative_EvpPKeyDeriveSecretAgreement(uint8_t* secret, uint32_t secretLength, EVP_PKEY_CTX* ctx)
{
    size_t tmpSize = (size_t)secretLength;
    int ret = 0;

    if (secret != NULL && ctx != NULL)
    {
        ret = EVP_PKEY_derive(ctx, secret, &tmpSize);

        if (ret == 1 && tmpSize != (size_t)secretLength)
        {
            OPENSSL_cleanse(secret, secretLength);
            ret = 0;
        }
    }

    return ret;
}

void CryptoNative_EvpPKeyCtxDestroy(EVP_PKEY_CTX* ctx)
{
    if (ctx != NULL)
    {
        EVP_PKEY_CTX_free(ctx);
    }
}
