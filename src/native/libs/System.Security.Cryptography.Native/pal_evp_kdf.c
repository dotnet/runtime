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
    unsigned char* key,
    int32_t keyLength,
    char* algorithm,
    unsigned char* label,
    int32_t labelLength,
    unsigned char* context,
    int32_t contextLength,
    unsigned char* destination,
    int32_t destinationLength)
{
    assert(kdf);
    assert(key != NULL || keyLength == 0);
    assert(keyLength >= 0);
    assert(algorithm);
    assert(destination || destinationLength == 0);
    assert(destinationLength >= 0);
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

        if (destinationLength == 0)
        {
            // If there is no work to do, bail out early.
            return 1;
        }

        unsigned char zero[] = { 0 };

        if (key == NULL || keyLength == 0)
        {
            // OpenSSL does not permit an empty KBKDF key. Since we know we are in HMAC mode, and HMAC keys are zero-extended,
            // We can create a non-empty key that is functionally equivilent to an empty one.
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
        if (ctx != NULL) EVP_KDF_CTX_free(ctx);
        return ret;
    }
#else
    (void)kdf;
    (void)key;
    (void)keylen;
    (void)algorithm;
    (void)destination;
    (void)destinationLength;
    assert(0 && "Inconsistent EVP_KDF API availability.");
#endif
    return 0;
}
