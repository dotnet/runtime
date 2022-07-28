// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <assert.h>
#include "pal_evp_pkey.h"

EVP_PKEY* CryptoNative_EvpPkeyCreate()
{
    ERR_clear_error();
    return EVP_PKEY_new();
}

EVP_PKEY* CryptoNative_EvpPKeyDuplicate(EVP_PKEY* currentKey, int32_t algId)
{
    assert(currentKey != NULL);

    ERR_clear_error();

    int currentAlgId = EVP_PKEY_get_base_id(currentKey);

    if (algId != NID_undef && algId != currentAlgId)
    {
        ERR_put_error(ERR_LIB_EVP, 0, EVP_R_DIFFERENT_KEY_TYPES, __FILE__, __LINE__);
        return NULL;
    }

    EVP_PKEY* newKey = EVP_PKEY_new();

    if (newKey == NULL)
    {
        return NULL;
    }

    bool success = true;

    if (currentAlgId == EVP_PKEY_RSA)
    {
        const RSA* rsa = EVP_PKEY_get0_RSA(currentKey);

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wcast-qual"
        if (rsa == NULL || !EVP_PKEY_set1_RSA(newKey, (RSA*)rsa))
#pragma clang diagnostic pop
        {
            success = false;
        }
    }
    else
    {
        ERR_put_error(ERR_LIB_EVP, 0, EVP_R_UNSUPPORTED_ALGORITHM, __FILE__, __LINE__);
        success = false;
    }

    if (!success)
    {
        EVP_PKEY_free(newKey);
        newKey = NULL;
    }

    return newKey;
}

void CryptoNative_EvpPkeyDestroy(EVP_PKEY* pkey)
{
    if (pkey != NULL)
    {
        EVP_PKEY_free(pkey);
    }
}

int32_t CryptoNative_EvpPKeySize(EVP_PKEY* pkey)
{
    // This function is not expected to populate the error queue with
    // any errors, but it's technically possible that an external
    // ENGINE or OSSL_PROVIDER populate the queue in their implementation,
    // but the calling code does not check for one.
    assert(pkey != NULL);
    return EVP_PKEY_get_size(pkey);
}

int32_t CryptoNative_UpRefEvpPkey(EVP_PKEY* pkey)
{
    if (!pkey)
    {
        return 0;
    }

    // No error queue impact.
    return EVP_PKEY_up_ref(pkey);
}

static bool CheckKey(EVP_PKEY* key, int32_t algId, int32_t (*check_func)(EVP_PKEY_CTX*))
{
    if (algId != NID_undef && EVP_PKEY_get_base_id(key) != algId)
    {
        ERR_put_error(ERR_LIB_EVP, 0, EVP_R_UNSUPPORTED_ALGORITHM, __FILE__, __LINE__);
        return false;
    }

    EVP_PKEY_CTX* ctx = EVP_PKEY_CTX_new(key, NULL);

    if (ctx == NULL)
    {
        // The malloc error should have already been set.
        return false;
    }

    int check = check_func(ctx);
    EVP_PKEY_CTX_free(ctx);

    // 1: Success
    // -2: The key object had no check routine available.
    if (check == 1 || check == -2)
    {
        // We need to clear for -2, doesn't hurt for 1.
        ERR_clear_error();
        return true;
    }

    return false;
}

EVP_PKEY* CryptoNative_DecodeSubjectPublicKeyInfo(const uint8_t* buf, int32_t len, int32_t algId)
{
    assert(buf != NULL);
    assert(len > 0);

    ERR_clear_error();

    EVP_PKEY* key = d2i_PUBKEY(NULL, &buf, len);

    if (key != NULL && !CheckKey(key, algId, EVP_PKEY_public_check))
    {
        EVP_PKEY_free(key);
        key = NULL;
    }

    return key;
}

EVP_PKEY* CryptoNative_DecodePkcs8PrivateKey(const uint8_t* buf, int32_t len, int32_t algId)
{
    assert(buf != NULL);
    assert(len > 0);

    ERR_clear_error();

    PKCS8_PRIV_KEY_INFO* p8info = d2i_PKCS8_PRIV_KEY_INFO(NULL, &buf, len);

    if (p8info == NULL)
    {
        return NULL;
    }

    EVP_PKEY* key = EVP_PKCS82PKEY(p8info);
    PKCS8_PRIV_KEY_INFO_free(p8info);

    if (key != NULL && !CheckKey(key, algId, EVP_PKEY_check))
    {
        EVP_PKEY_free(key);
        key = NULL;
    }

    return key;
}

int32_t CryptoNative_GetPkcs8PrivateKeySize(EVP_PKEY* pkey, int32_t* p8size)
{
    assert(pkey != NULL);
    assert(p8size != NULL);

    *p8size = 0;
    ERR_clear_error();

    PKCS8_PRIV_KEY_INFO* p8 = EVP_PKEY2PKCS8(pkey);

    if (p8 == NULL)
    {
        // OpenSSL 1.1 and 3 have a behavioral change with EVP_PKEY2PKCS8
        // with regard to handling EVP_PKEYs that do not contain a private key.
        //
        // In OpenSSL 1.1, it would always succeed, but the private parameters
        // would be missing (thus making an invalid PKCS8 structure).
        // Over in the managed side, we detect these invalid PKCS8 blobs and
        // convert that to a "no private key" error.
        //
        // In OpenSSL 3, this now correctly errors, with the error
        // ASN1_R_ILLEGAL_ZERO_CONTENT. We want to preserve allocation failures
        // as OutOfMemoryException. So we peek at the error. If it's a malloc
        // failure, -1 is returned to indcate "throw what is on the error queue".
        // If the error is not a malloc failure, return -2 to mean "no private key".
        // If OpenSSL ever changes the error to something more to explicitly mean
        // "no private key" then we should test for that explicitly. Until then,
        // we treat all errors, except a malloc error, to mean "no private key".

        const char* file = NULL;
        int line = 0;
        unsigned long error = ERR_peek_error_line(&file, &line);

        // If it's not a malloc failure, assume it's because the private key is
        // missing.
        if (ERR_GET_REASON(error) != ERR_R_MALLOC_FAILURE)
        {
            ERR_clear_error();
            return -2;
        }

        // It is a malloc failure. Clear the error queue and set the error
        // as a malloc error so it's the only error in the queue.
        ERR_clear_error();
        ERR_put_error(ERR_GET_LIB(error), 0, ERR_R_MALLOC_FAILURE, file, line);

        // Since ERR_peek_error() matches what exception is thrown, leave the OOM on top.
        return -1;
    }

    *p8size = i2d_PKCS8_PRIV_KEY_INFO(p8, NULL);
    PKCS8_PRIV_KEY_INFO_free(p8);

    return *p8size < 0 ? -1 : 1;
}

int32_t CryptoNative_EncodePkcs8PrivateKey(EVP_PKEY* pkey, uint8_t* buf)
{
    assert(pkey != NULL);
    assert(buf != NULL);

    ERR_clear_error();

    PKCS8_PRIV_KEY_INFO* p8 = EVP_PKEY2PKCS8(pkey);

    if (p8 == NULL)
    {
        return -1;
    }

    int ret = i2d_PKCS8_PRIV_KEY_INFO(p8, &buf);
    PKCS8_PRIV_KEY_INFO_free(p8);
    return ret;
}

int32_t CryptoNative_GetSubjectPublicKeyInfoSize(EVP_PKEY* pkey)
{
    assert(pkey != NULL);

    ERR_clear_error();
    return i2d_PUBKEY(pkey, NULL);
}

int32_t CryptoNative_EncodeSubjectPublicKeyInfo(EVP_PKEY* pkey, uint8_t* buf)
{
    assert(pkey != NULL);
    assert(buf != NULL);

    ERR_clear_error();
    return i2d_PUBKEY(pkey, &buf);
}
