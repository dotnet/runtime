// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "openssl.h"
#include "pal_evp_kdf.h"
#include "pal_utilities.h"

#include <assert.h>

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wmissing-noreturn"
void CryptoNative_EvpKdfFree(EVP_KDF* kdf)
{
#ifdef NEED_OPENSSL_3_0
    if (API_EXISTS(EVP_KDF_free))
    {
        // No error queue impact
        EVP_KDF_free(kdf);
        return;
    }
#else
    (void)kdf;
#endif

    assert(0 && "Inconsistent EVP_KDF API availability.");
}
#pragma clang diagnostic pop


EVP_KDF* CryptoNative_EvpKdfFetch(const char* algorithm, int32_t* haveFeature)
{
    assert(haveFeature);

#ifdef NEED_OPENSSL_3_0
    if (API_EXISTS(EVP_KDF_fetch))
    {
        ERR_clear_error();
        EVP_KDF* kdf = EVP_KDF_fetch(NULL, algorithm, NULL);

        if (kdf)
        {
            *haveFeature = 1;
            return kdf;
        }
        else
        {
            unsigned long error = ERR_peek_error();

            // If the fetch failed because the algorithm is unsupported, then set
            // haveFeature to 0. Otherwise, assume the algorithm exists and the
            // fetch failed for another reason, and set haveFeature to 1.
            *haveFeature = ERR_GET_REASON(error) == ERR_R_UNSUPPORTED ? 0 : 1;
            return NULL;
        }
    }
#else
    (void)algorithm;
    (void)haveFeature;
#endif

    *haveFeature = 0;
    return NULL;
}

int32_t CryptoNative_KbkdfHmacOneShot(
    EVP_KDF* kdf,
    uint8_t* key,
    int32_t keyLength,
    char* algorithm,
    uint8_t* label,
    int32_t labelLength,
    uint8_t* context,
    int32_t contextLength,
    uint8_t* destination,
    int32_t destinationLength)
{
    assert(kdf);
    assert(key != NULL || keyLength == 0);
    assert(keyLength >= 0);
    assert(algorithm);
    assert(destination);
    assert(destinationLength > 0);
    assert(label != NULL || labelLength == 0);
    assert(context != NULL || contextLength == 0);

    ERR_clear_error();

#ifdef NEED_OPENSSL_3_0
    if (API_EXISTS(EVP_KDF_CTX_new))
    {
        assert(API_EXISTS(EVP_KDF_CTX_free));
        assert(API_EXISTS(EVP_KDF_derive));
        assert(API_EXISTS(OSSL_PARAM_construct_utf8_string));
        assert(API_EXISTS(OSSL_PARAM_construct_octet_string));
        assert(API_EXISTS(OSSL_PARAM_construct_end));

        unsigned char zero[] = { 0 };

        if (key == NULL || keyLength == 0)
        {
            // OpenSSL does not permit an empty KBKDF key. Since we know we are in HMAC mode, and HMAC keys are zero-extended,
            // We can create a non-empty key that is functionally equivalent to an empty one.
            key = zero;
            keyLength = 1;
        }

        EVP_KDF_CTX* ctx = EVP_KDF_CTX_new(kdf);
        int32_t ret = 0;

        if (ctx == NULL)
        {
            goto cleanup;
        }

        size_t keyLengthT = Int32ToSizeT(keyLength);
        size_t destinationLengthT = Int32ToSizeT(destinationLength);
        size_t labelLengthT = Int32ToSizeT(labelLength);
        size_t contextLengthT = Int32ToSizeT(contextLength);

        OSSL_PARAM params[] =
        {
            OSSL_PARAM_construct_utf8_string(OSSL_KDF_PARAM_DIGEST, algorithm, 0),
            OSSL_PARAM_construct_utf8_string(OSSL_KDF_PARAM_MAC, "HMAC", 0),
            OSSL_PARAM_construct_octet_string(OSSL_KDF_PARAM_KEY, (void*)key, keyLengthT),
            OSSL_PARAM_construct_octet_string(OSSL_KDF_PARAM_SALT, (void*)label, labelLengthT),
            OSSL_PARAM_construct_octet_string(OSSL_KDF_PARAM_INFO, (void*)context, contextLengthT),
            OSSL_PARAM_construct_end(),
        };

        if (EVP_KDF_derive(ctx, destination, destinationLengthT, params) <= 0)
        {
            goto cleanup;
        }

        ret = 1;

cleanup:
        if (ctx != NULL)
        {
            EVP_KDF_CTX_free(ctx);
        }

        return ret;
    }
#else
    (void)kdf;
    (void)key;
    (void)keyLength;
    (void)algorithm;
    (void)label;
    (void)labelLength;
    (void)context;
    (void)contextLength;
    (void)destination;
    (void)destinationLength;
    assert(0 && "Inconsistent EVP_KDF API availability.");
#endif
    return 0;
}

static int32_t HkdfCore(
    EVP_KDF* kdf,
    int operation,
    uint8_t* key,
    int32_t keyLength,
    char* algorithm,
    uint8_t* salt,
    int32_t saltLength,
    uint8_t* info,
    int32_t infoLength,
    uint8_t* destination,
    int32_t destinationLength)
{
    assert(kdf);
    assert(key != NULL || keyLength == 0);
    assert(keyLength >= 0);
    assert(algorithm);
    assert(destination);
    assert(destinationLength > 0);
    assert(salt != NULL || saltLength == 0);
    assert(info != NULL || infoLength == 0);

    ERR_clear_error();

#ifdef NEED_OPENSSL_3_0
    if (API_EXISTS(EVP_KDF_CTX_new))
    {
        assert(API_EXISTS(EVP_KDF_CTX_free));
        assert(API_EXISTS(EVP_KDF_derive));
        assert(API_EXISTS(OSSL_PARAM_construct_utf8_string));
        assert(API_EXISTS(OSSL_PARAM_construct_octet_string));
        assert(API_EXISTS(OSSL_PARAM_construct_end));

        EVP_KDF_CTX* ctx = EVP_KDF_CTX_new(kdf);
        int32_t ret = 0;

        if (ctx == NULL)
        {
            goto cleanup;
        }

        size_t keyLengthT = Int32ToSizeT(keyLength);
        size_t destinationLengthT = Int32ToSizeT(destinationLength);
        size_t saltLengthT = Int32ToSizeT(saltLength);
        size_t infoLengthT = Int32ToSizeT(infoLength);

        OSSL_PARAM params[] =
        {
            OSSL_PARAM_construct_octet_string(OSSL_KDF_PARAM_KEY, (void*)key, keyLengthT),
            OSSL_PARAM_construct_utf8_string(OSSL_KDF_PARAM_DIGEST, algorithm, 0),
            OSSL_PARAM_construct_octet_string(OSSL_KDF_PARAM_SALT, (void*)salt, saltLengthT),
            OSSL_PARAM_construct_octet_string(OSSL_KDF_PARAM_INFO, (void*)info, infoLengthT),
            OSSL_PARAM_construct_int(OSSL_KDF_PARAM_MODE, &operation),
            OSSL_PARAM_construct_end(),
        };

        if (EVP_KDF_derive(ctx, destination, destinationLengthT, params) <= 0)
        {
            goto cleanup;
        }

        ret = 1;

cleanup:
        if (ctx != NULL)
        {
            EVP_KDF_CTX_free(ctx);
        }

        return ret;
    }
#else
    (void)kdf;
    (void)operation;
    (void)key;
    (void)keyLength;
    (void)algorithm;
    (void)salt;
    (void)saltLength;
    (void)info;
    (void)infoLength;
    (void)destination;
    (void)destinationLength;
    assert(0 && "Inconsistent EVP_KDF API availability.");
#endif
    return 0;
}

int32_t CryptoNative_HkdfDeriveKey(
    EVP_KDF* kdf,
    uint8_t* ikm,
    int32_t ikmLength,
    char* algorithm,
    uint8_t* salt,
    int32_t saltLength,
    uint8_t* info,
    int32_t infoLength,
    uint8_t* destination,
    int32_t destinationLength)
{
    return HkdfCore(
        kdf,
        EVP_KDF_HKDF_MODE_EXTRACT_AND_EXPAND,
        ikm,
        ikmLength,
        algorithm,
        salt,
        saltLength,
        info,
        infoLength,
        destination,
        destinationLength);
}

int32_t CryptoNative_HkdfExpand(
    EVP_KDF* kdf,
    uint8_t* prk,
    int32_t prkLength,
    char* algorithm,
    uint8_t* info,
    int32_t infoLength,
    uint8_t* destination,
    int32_t destinationLength)
{
    return HkdfCore(
        kdf,
        EVP_KDF_HKDF_MODE_EXPAND_ONLY,
        prk,
        prkLength,
        algorithm,
        NULL /* salt */,
        0 /* saltLength */,
        info,
        infoLength,
        destination,
        destinationLength);
}

int32_t CryptoNative_HkdfExtract(
    EVP_KDF* kdf,
    uint8_t* ikm,
    int32_t ikmLength,
    char* algorithm,
    uint8_t* salt,
    int32_t saltLength,
    uint8_t* destination,
    int32_t destinationLength)
{
    return HkdfCore(
        kdf,
        EVP_KDF_HKDF_MODE_EXTRACT_ONLY,
        ikm,
        ikmLength,
        algorithm,
        salt,
        saltLength,
        NULL /* info */,
        0 /* infoLength */,
        destination,
        destinationLength);
}
