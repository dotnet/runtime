// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_evp_pkey_ecdh.h"
#include "pal_evp_pkey.h"
#include "pal_utilities.h"
#include <assert.h>

static EVP_PKEY_CTX* EvpPKeyCtxCreate(EVP_PKEY* pkey, void* extraHandle, EVP_PKEY* peerkey)
{
    if (pkey == NULL || peerkey == NULL)
    {
        return NULL;
    }

    /* Create the context for the shared secret derivation */
    EVP_PKEY_CTX* ctx = EvpPKeyCtxCreateFromPKey(pkey, extraHandle);

    if (ctx == NULL)
    {
        return NULL;
    }

    size_t tmpLength = 0;

    /* Initialize, provide the peer public key */
    if (1 != EVP_PKEY_derive_init(ctx) || 1 != EVP_PKEY_derive_set_peer(ctx, peerkey))
    {
        EVP_PKEY_CTX_free(ctx);
        return NULL;
    }

    return ctx;
}

int32_t CryptoNative_EvpPKeyDeriveSecretAgreement(EVP_PKEY* pkey, void* extraHandle, EVP_PKEY* peerKey, uint8_t* secret, uint32_t secretLength)
{
    if (pkey == NULL || peerKey == NULL || secretLength == 0 || secret == NULL)
    {
        return 0;
    }

    ERR_clear_error();

    size_t tmpSize = (size_t)secretLength;
    EVP_PKEY_CTX* ctx = EvpPKeyCtxCreate(pkey, extraHandle, peerKey);

    if (ctx == NULL)
    {
        return 0;
    }

    int ret = EVP_PKEY_derive(ctx, secret, &tmpSize);

    EVP_PKEY_CTX_free(ctx);

    if (ret != 1)
    {
        return 0;
    }

    assert(tmpSize > 0 && tmpSize <= secretLength);
    return SizeTToInt32(tmpSize);
}
