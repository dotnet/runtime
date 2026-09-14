// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_evp_pkey.h"
#include "pal_evp_pkey_ecdh.h"
#include "pal_evp_pkey_x25519.h"
#include "pal_utilities.h"
#include "openssl.h"
#include <assert.h>

#define X25519_KEY_SIZE_IN_BYTES 32

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

int32_t CryptoNative_X25519DeriveSecretAgreementWithBytes(EVP_PKEY* pkey,
                                                          void* extraHandle,
                                                          const uint8_t* peerKey,
                                                          int32_t peerKeyLength,
                                                          uint8_t* secret,
                                                          uint32_t secretLength)
{
    if (pkey == NULL || peerKey == NULL || peerKeyLength <= 0 || secret == NULL || secretLength == 0)
    {
        return 0;
    }

    EVP_PKEY* peerPKey = CryptoNative_X25519ImportPublicKey(peerKey, peerKeyLength);

    if (peerPKey == NULL)
    {
        return 0;
    }

    int32_t ret = CryptoNative_EvpPKeyDeriveSecretAgreement(pkey, extraHandle, peerPKey, secret, secretLength);
    EVP_PKEY_free(peerPKey);
    return ret;
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

    uint8_t privateKey[X25519_KEY_SIZE_IN_BYTES];
    size_t privateKeyLength = sizeof(privateKey);
    int32_t ret = 0;

    // In OpenSSL 1.1.1, a NULL buffer for a private key will succeed even if the EVP_PKEY does not have a private key
    // because it thinks you are asking it "how big a buffer do I need to contain the private key". Only after you
    // give it a buffer big enough will it error saying the key does not have a private key.
    // In OpenSSL 3.0 it will error saying it does not have a private key, even when all you are doing is asking how
    // big the private key is. So always give it a buffer big enough.
    if (EVP_PKEY_get_raw_private_key(key, privateKey, &privateKeyLength) == 1)
    {
        if (privateKeyLength == sizeof(privateKey))
        {
            ret = 1;
            *hasPrivateKey = 1;
            goto done;
        }
        else
        {
            // This means the X25519 key is not the correct size somehow. We can't work with an X25519 key that does not
            // use correct key sizes, so report it as an invalid handle.
            goto done;
        }
    }
    else
    {
        // We still want to return success in this case, this is the "the key is only public" case.
        ret = 1;
        ERR_clear_error();
        goto done;
    }

done:
    OPENSSL_cleanse(privateKey, sizeof(privateKey));
    return ret;
}
