// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_evp_pkey_rsa.h"

EVP_PKEY* CryptoNative_RsaGenerateKey(int keySize)
{
    EVP_PKEY_CTX* ctx = EVP_PKEY_CTX_new_id(EVP_PKEY_RSA, NULL);

    if (ctx == NULL)
    {
        return NULL;
    }

    EVP_PKEY* pkey = NULL;
    int success = 1;
    success = success && (1 == EVP_PKEY_keygen_init(ctx));
    success = success && (1 == EVP_PKEY_CTX_set_rsa_keygen_bits(ctx, keySize));
    success = success && (1 == EVP_PKEY_keygen(ctx, &pkey));

    if (pkey != NULL && !success)
    {
        EVP_PKEY_free(pkey);
        pkey = NULL;
    }

    EVP_PKEY_CTX_free(ctx);
    return pkey;
}

RSA* CryptoNative_EvpPkeyGetRsa(EVP_PKEY* pkey)
{
    return EVP_PKEY_get1_RSA(pkey);
}

int32_t CryptoNative_EvpPkeySetRsa(EVP_PKEY* pkey, RSA* rsa)
{
    return EVP_PKEY_set1_RSA(pkey, rsa);
}
