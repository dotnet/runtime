// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "openssl.h"
#include "pal_evp_kem.h"
#include "pal_utilities.h"

#include <assert.h>

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wmissing-noreturn"
void CryptoNative_EvpKemFree(EVP_KEM* kem)
{
#ifdef NEED_OPENSSL_3_0
    if (API_EXISTS(EVP_KEM_free))
    {
        // No error queue impact
        EVP_KEM_free(kem);
        return;
    }
#else
    (void)kem;
#endif

    assert(0 && "Inconsistent EVP_KEM API availability.");
}
#pragma clang diagnostic pop


EVP_KEM* CryptoNative_EvpKemFetch(const char* algorithm, int32_t* haveFeature)
{
    assert(haveFeature);

#ifdef NEED_OPENSSL_3_0
    if (API_EXISTS(EVP_KEM_fetch))
    {
        ERR_clear_error();
        EVP_KEM* kem = EVP_KEM_fetch(NULL, algorithm, NULL);

        if (kem)
        {
            *haveFeature = 1;
            return kem;
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

EVP_PKEY* CryptoNative_EvpKemGeneratePkey(const EVP_KEM* kem, uint8_t* seed, int32_t seedLength)
{
    assert(kem);
    assert((seed == NULL) == (seedLength == 0));

#ifdef NEED_OPENSSL_3_0
    if (API_EXISTS(EVP_KEM_fetch))
    {
        assert(
            API_EXISTS(EVP_KEM_get0_name) &&
            API_EXISTS(EVP_PKEY_CTX_new_from_name) &&
            API_EXISTS(OSSL_PARAM_construct_octet_string) &&
            API_EXISTS(EVP_PKEY_CTX_set_params));

        ERR_clear_error();
        const char* name = EVP_KEM_get0_name(kem);

        if (name == NULL)
        {
            return NULL;
        }

        EVP_PKEY_CTX* ctx = NULL;
        EVP_PKEY* key = NULL;

        ctx = EVP_PKEY_CTX_new_from_name(NULL, name, NULL);

        if (ctx == NULL)
        {
            goto done;
        }

        if (EVP_PKEY_keygen_init(ctx) != 1)
        {
            goto done;
        }

        if (seed && seedLength > 0)
        {
            size_t seedLengthT = Int32ToSizeT(seedLength);
            OSSL_PARAM params[] =
            {
                OSSL_PARAM_construct_octet_string(OSSL_PKEY_PARAM_ML_KEM_SEED, (void*)seed, seedLengthT),
                OSSL_PARAM_construct_end(),
            };

            if (EVP_PKEY_CTX_set_params(ctx, params) != 1)
            {
                goto done;
            }
        }

        if (EVP_PKEY_keygen(ctx, &key) != 1)
        {
            if (key != NULL)
            {
                EVP_PKEY_free(key);
                key = NULL;
            }

            goto done;
        }
done:
        if (ctx)
        {
            EVP_PKEY_CTX_free(ctx);
        }

        return key;
    }
#else
    (void)kem;
#endif

    return NULL;
}

int32_t CryptoNative_EvpKemExportPrivateSeed(const EVP_PKEY* pKey, uint8_t* destination, int32_t destinationLength)
{
    assert(pKey);
    assert(destination);

#ifdef NEED_OPENSSL_3_0
    if (API_EXISTS(EVP_PKEY_get_octet_string_param))
    {
        size_t destinationLengthT = Int32ToSizeT(destinationLength);
        size_t outLength = 0;
        int ret = EVP_PKEY_get_octet_string_param(
            pKey,
            OSSL_PKEY_PARAM_ML_KEM_SEED,
            (unsigned char*)destination,
            destinationLengthT,
            &outLength);

        if (outLength != destinationLengthT)
        {
            return -1;
        }

        return ret == 1 ? 1 : 0;
    }
#else
    (void)pKey;
    (void)destination;
    (void)destinationLength;
#endif

    return 0;
}

int32_t CryptoNative_EvpKemEncapsulate(EVP_PKEY* pKey,
                                       uint8_t* ciphertext,
                                       int32_t ciphertextLength,
                                       uint8_t* sharedSecret,
                                       int32_t sharedSecretLength)
{
    assert(pKey);
    assert(ciphertext);
    assert(sharedSecret);

#ifdef NEED_OPENSSL_3_0
    if (API_EXISTS(EVP_PKEY_encapsulate_init))
    {
        assert(API_EXISTS(EVP_PKEY_CTX_new_from_pkey));

        EVP_PKEY_CTX* ctx = NULL;
        ctx = EVP_PKEY_CTX_new_from_pkey(NULL, pKey, NULL);
        int32_t ret = 0;

        if (ctx == NULL)
        {
            goto done;
        }

        if (EVP_PKEY_encapsulate_init(ctx, NULL) != 1)
        {
            goto done;
        }

        size_t ciphertextLengthT = Int32ToSizeT(ciphertextLength);
        size_t sharedSecretLengthT = Int32ToSizeT(sharedSecretLength);

        if (EVP_PKEY_encapsulate(ctx, ciphertext, &ciphertextLengthT, sharedSecret, &sharedSecretLengthT) != 1)
        {
            goto done;
        }

        if (ciphertextLengthT != Int32ToSizeT(ciphertextLength) || sharedSecretLengthT != Int32ToSizeT(sharedSecretLength))
        {
            ret = -1;
        }

        ret = 1;

done:
        if (ctx != NULL)
        {
            EVP_PKEY_CTX_free(ctx);
        }

        return ret;
    }
#endif

    (void)pKey;
    (void)ciphertext;
    (void)ciphertextLength;
    (void)sharedSecret;
    (void)sharedSecretLength;
    return 0;
}
