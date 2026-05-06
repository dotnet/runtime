// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_evp_pkey.h"
#include "pal_evp_pkey_x25519.h"
#include "pal_utilities.h"
#include "openssl.h"
#include <assert.h>

static int32_t ExportRawKeyMaterial(
    const EVP_PKEY* key,
    uint8_t* destination,
    int32_t destinationLength,
    int (*exporter)(const EVP_PKEY*, unsigned char*, size_t*))
{
    assert(key != NULL && destination != NULL && exporter != NULL);

    ERR_clear_error();

    size_t len = Int32ToSizeT(destinationLength);
    int result = exporter(key, destination, &len);

    if (result != 1)
    {
        return 0;
    }

    if (len != Int32ToSizeT(destinationLength))
    {
        assert("Exported raw key was not the correct length." && 0);
        return -1;
    }

    return 1;
}

int32_t CryptoNative_X25519Available(void)
{
    ERR_clear_error();
    EVP_PKEY_CTX* ctx = EVP_PKEY_CTX_new_id(EVP_PKEY_X25519, NULL);

    if (ctx)
    {
        EVP_PKEY_CTX_free(ctx);
        return 1;
    }

    // X25519 might not be available for two reasons.
    // 1. It was built with `no-ecx` which is available starting in OpenSSL 3.2.
    // 2. The default Provider is the FIPS provider, and X25519 is not available in the FIPS provider.
    // In both cases, ERR_R_UNSUPPORTED is put in the error queue.
    // If we errored for a different reason, we _still_ want to return "yes" for is supported. This will allow for
    // actual use of X25519 to throw the appropriate error.
    unsigned long error = ERR_peek_error();
    int32_t result = ERR_GET_REASON(error) == ERR_R_UNSUPPORTED ? 0 : 1;
    ERR_clear_error();
    return result;
}

int32_t CryptoNative_X25519ExportPrivateKey(const EVP_PKEY* key, uint8_t* destination, int32_t destinationLength)
{
    return ExportRawKeyMaterial(key, destination, destinationLength, EVP_PKEY_get_raw_private_key);
}

int32_t CryptoNative_X25519ExportPublicKey(const EVP_PKEY* key, uint8_t* destination, int32_t destinationLength)
{
    return ExportRawKeyMaterial(key, destination, destinationLength, EVP_PKEY_get_raw_public_key);
}

EVP_PKEY* CryptoNative_X25519GenerateKey(void)
{
    ERR_clear_error();

    EVP_PKEY_CTX* ctx = EVP_PKEY_CTX_new_id(EVP_PKEY_X25519, NULL);

    if (ctx == NULL)
    {
        return NULL;
    }

    EVP_PKEY* pkey = NULL;
    EVP_PKEY* ret = NULL;

    if (EVP_PKEY_keygen_init(ctx) == 1 && EVP_PKEY_keygen(ctx, &pkey) == 1)
    {
        ret = pkey;
        pkey = NULL;
    }

    if (pkey != NULL)
    {
        EVP_PKEY_free(pkey);
    }

    EVP_PKEY_CTX_free(ctx);
    return ret;
}

EVP_PKEY* CryptoNative_X25519ImportPrivateKey(const uint8_t* source, int32_t sourceLength)
{
    assert(source && sourceLength > 0);
    ERR_clear_error();

    return EVP_PKEY_new_raw_private_key(
        EVP_PKEY_X25519,
        NULL,
        source,
        Int32ToSizeT(sourceLength));
}

EVP_PKEY* CryptoNative_X25519ImportPublicKey(const uint8_t* source, int32_t sourceLength)
{
    assert(source && sourceLength > 0);
    ERR_clear_error();

    return EVP_PKEY_new_raw_public_key(
        EVP_PKEY_X25519,
        NULL,
        source,
        Int32ToSizeT(sourceLength));
}

int32_t CryptoNative_X25519IsValidHandle(const EVP_PKEY* key, int32_t* hasPrivateKey)
{
    assert(key != NULL && hasPrivateKey != NULL);
    ERR_clear_error();

    *hasPrivateKey = 0;

    if (EVP_PKEY_get_base_id(key) != EVP_PKEY_X25519)
    {
        return 0;
    }

    size_t privateKeyLength = 0;

    if (EVP_PKEY_get_raw_private_key(key, NULL, &privateKeyLength) == 1)
    {
        *hasPrivateKey = 1;
    }
    else
    {
        ERR_clear_error();
    }

    return 1;
}
