// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "openssl.h"
#include "pal_evp_kem.h"
#include "pal_utilities.h"

#include <assert.h>

static int32_t GetKeyOctetStringParam(const EVP_PKEY* pKey,
                                      const char* name,
                                      uint8_t* destination,
                                      int32_t destinationLength)
{
    assert(pKey);
    assert(destination);
    assert(name);

#ifdef NEED_OPENSSL_3_0
    if (API_EXISTS(EVP_PKEY_get_octet_string_param))
    {
        ERR_clear_error();

        size_t destinationLengthT = Int32ToSizeT(destinationLength);
        size_t outLength = 0;

        int ret = EVP_PKEY_get_octet_string_param(
            pKey,
            name,
            NULL,
            0,
            &outLength);

        if (ret != 1)
        {
            return -1;
        }

        ret = EVP_PKEY_get_octet_string_param(
            pKey,
            name,
            (unsigned char*)destination,
            destinationLengthT,
            &outLength);

        if (ret != 1)
        {
            return 0;
        }

        if (outLength != destinationLengthT)
        {
            return -2;
        }

        return 1;
    }
#else
    (void)pKey;
    (void)destination;
    (void)destinationLength;
#endif

    return 0;
}


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

EVP_PKEY* CryptoNative_EvpKemImportKey(const EVP_KEM* kem, uint8_t* key, int32_t keyLength, int32_t privateKey)
{
    assert(kem);
    assert(key);
    assert(keyLength > 0);

#ifdef NEED_OPENSSL_3_0
    if (API_EXISTS(EVP_PKEY_CTX_new_from_name))
    {
        ERR_clear_error();
        const char* name = EVP_KEM_get0_name(kem);

        if (name == NULL)
        {
            return NULL;
        }

        EVP_PKEY_CTX* ctx = NULL;
        EVP_PKEY* pkey = NULL;
        ctx = EVP_PKEY_CTX_new_from_name(NULL, name, NULL);

        if (ctx == NULL)
        {
            goto done;
        }

        if (EVP_PKEY_fromdata_init(ctx) != 1)
        {
            goto done;
        }

        const char* paramName = privateKey == 0 ? OSSL_PKEY_PARAM_PUB_KEY : OSSL_PKEY_PARAM_PRIV_KEY;
        int selection = privateKey == 0 ? EVP_PKEY_PUBLIC_KEY : EVP_PKEY_KEYPAIR;
        size_t keyLengthT = Int32ToSizeT(keyLength);

        OSSL_PARAM params[] =
        {
            OSSL_PARAM_construct_octet_string(paramName, (void*)key, keyLengthT),
            OSSL_PARAM_construct_end(),
        };

        if (EVP_PKEY_fromdata(ctx, &pkey, selection, params) != 1)
        {
            if (pkey != NULL)
            {
                EVP_PKEY_free(pkey);
                pkey = NULL;
            }

            goto done;
        }

done:
        if (ctx)
        {
            EVP_PKEY_CTX_free(ctx);
        }

        return pkey;
    }
#endif

    (void)kem;
    (void)key;
    (void)keyLength;
    (void)privateKey;
    return NULL;
}

EVP_PKEY* CryptoNative_EvpKemGeneratePkey(const EVP_KEM* kem, uint8_t* seed, int32_t seedLength)
{
    assert(kem);
    assert((seed == NULL) == (seedLength == 0));

#ifdef NEED_OPENSSL_3_0
    if (API_EXISTS(EVP_PKEY_CTX_new_from_name))
    {
        assert(
            API_EXISTS(EVP_KEM_get0_name) &&
            API_EXISTS(OSSL_PARAM_construct_octet_string) &&
            API_EXISTS(OSSL_PARAM_construct_end) &&
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
        assert(API_EXISTS(EVP_PKEY_CTX_new_from_pkey) && API_EXISTS(EVP_PKEY_encapsulate));
        ERR_clear_error();

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

int32_t CryptoNative_EvpKemDecapsulate(EVP_PKEY* pKey,
                                       const uint8_t* ciphertext,
                                       int32_t ciphertextLength,
                                       uint8_t* sharedSecret,
                                       int32_t sharedSecretLength)
{
    assert(pKey);
    assert(ciphertext);
    assert(sharedSecret);

#ifdef NEED_OPENSSL_3_0
    if (API_EXISTS(EVP_PKEY_decapsulate_init))
    {
        assert(API_EXISTS(EVP_PKEY_CTX_new_from_pkey) && API_EXISTS(EVP_PKEY_decapsulate));
        ERR_clear_error();

        EVP_PKEY_CTX* ctx = NULL;
        ctx = EVP_PKEY_CTX_new_from_pkey(NULL, pKey, NULL);
        int32_t ret = 0;

        if (ctx == NULL)
        {
            goto done;
        }

        if (EVP_PKEY_decapsulate_init(ctx, NULL) != 1)
        {
            goto done;
        }

        size_t ciphertextLengthT = Int32ToSizeT(ciphertextLength);
        size_t sharedSecretLengthT = Int32ToSizeT(sharedSecretLength);

        if (EVP_PKEY_decapsulate(ctx, sharedSecret, &sharedSecretLengthT, ciphertext, ciphertextLengthT) != 1)
        {
            goto done;
        }

        if (sharedSecretLengthT != Int32ToSizeT(sharedSecretLength))
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

int32_t CryptoNative_EvpKemExportPrivateSeed(const EVP_PKEY* pKey, uint8_t* destination, int32_t destinationLength)
{
    return GetKeyOctetStringParam(pKey, OSSL_PKEY_PARAM_ML_KEM_SEED, destination, destinationLength);
}

int32_t CryptoNative_EvpKemExportDecapsulationKey(const EVP_PKEY* pKey, uint8_t* destination, int32_t destinationLength)
{
    return GetKeyOctetStringParam(pKey, OSSL_PKEY_PARAM_PRIV_KEY, destination, destinationLength);
}


int32_t CryptoNative_EvpKemExportEncapsulationKey(const EVP_PKEY* pKey, uint8_t* destination, int32_t destinationLength)
{
    return GetKeyOctetStringParam(pKey, OSSL_PKEY_PARAM_PUB_KEY, destination, destinationLength);
}
