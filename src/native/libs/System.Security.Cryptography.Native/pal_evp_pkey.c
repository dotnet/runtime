// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <assert.h>
#include "pal_evp_pkey.h"

EVP_PKEY* CryptoNative_EvpPkeyCreate()
{
    return EVP_PKEY_new();
}

EVP_PKEY* CryptoNative_EvpPKeyDuplicate(EVP_PKEY* currentKey, int32_t algId)
{
    assert(currentKey != NULL);

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
    assert(pkey != NULL);
    return EVP_PKEY_get_size(pkey);
}

int32_t CryptoNative_UpRefEvpPkey(EVP_PKEY* pkey)
{
    if (!pkey)
    {
        return 0;
    }

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

int32_t CryptoNative_GetPkcs8PrivateKeySize(EVP_PKEY* pkey)
{
    assert(pkey != NULL);

    PKCS8_PRIV_KEY_INFO* p8 = EVP_PKEY2PKCS8(pkey);

    if (p8 == NULL)
    {
        return -1;
    }

    int ret = i2d_PKCS8_PRIV_KEY_INFO(p8, NULL);
    PKCS8_PRIV_KEY_INFO_free(p8);
    return ret;
}

int32_t CryptoNative_EncodePkcs8PrivateKey(EVP_PKEY* pkey, uint8_t* buf)
{
    assert(pkey != NULL);
    assert(buf != NULL);

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

    return i2d_PUBKEY(pkey, NULL);
}

int32_t CryptoNative_EncodeSubjectPublicKeyInfo(EVP_PKEY* pkey, uint8_t* buf)
{
    assert(pkey != NULL);
    assert(buf != NULL);

    return i2d_PUBKEY(pkey, &buf);
}
