// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_evp_pkey.h"
#include "pal_evp_pkey_ecdsa.h"
#include "pal_utilities.h"
#include <assert.h>

int32_t CryptoNative_EcDsaSignHash(EVP_PKEY* pkey,
                                   void* extraHandle,
                                   const uint8_t* hash,
                                   int32_t hashLen,
                                   uint8_t* destination,
                                   int32_t destinationLen)
{
    assert(pkey != NULL);
    assert(destination != NULL);

    ERR_clear_error();

    EVP_PKEY_CTX* ctx = EvpPKeyCtxCreateFromPKey(pkey, extraHandle);

    int ret = -1;

    if (ctx == NULL || EVP_PKEY_sign_init(ctx) <= 0)
    {
        goto done;
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

int32_t CryptoNative_EcDsaVerifyHash(EVP_PKEY* pkey,
                                     void* extraHandle,
                                     const uint8_t* hash,
                                     int32_t hashLen,
                                     const uint8_t* signature,
                                     int32_t signatureLen)
{
    assert(pkey != NULL);
    assert(signature != NULL);

    ERR_clear_error();

    EVP_PKEY_CTX* ctx = EvpPKeyCtxCreateFromPKey(pkey, extraHandle);

    int ret = -1;

    if (ctx == NULL || EVP_PKEY_verify_init(ctx) <= 0)
    {
        goto done;
    }

    // We normalize all error codes to 1 or 0 because we cannot distinguish between missized hash and other errors
    ret = EVP_PKEY_verify(ctx, signature, Int32ToSizeT(signatureLen), hash, Int32ToSizeT(hashLen)) == 1;

done:
    if (ctx != NULL)
    {
        EVP_PKEY_CTX_free(ctx);
    }

    return ret;
}
