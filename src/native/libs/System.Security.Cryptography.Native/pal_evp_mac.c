// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "openssl.h"
#include "pal_evp_mac.h"
#include "pal_utilities.h"

#include <assert.h>

EVP_MAC* CryptoNative_EvpMacFetch(const char* algorithm, int32_t* haveFeature)
{
    assert(haveFeature);

#ifdef NEED_OPENSSL_3_0
    if (API_EXISTS(EVP_MAC_fetch))
    {
        ERR_clear_error();
        EVP_MAC* mac = EVP_MAC_fetch(NULL, algorithm, NULL);

        if (mac)
        {
            *haveFeature = 1;
            return mac;
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

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wmissing-noreturn"
void CryptoNative_EvpMacFree(EVP_MAC *mac)
{
#ifdef NEED_OPENSSL_3_0
    if (API_EXISTS(EVP_MAC_free))
    {
        // No error queue impact
        EVP_MAC_free(mac);
        return;
    }
#else
    (void)mac;
#endif

    assert(0 && "Inconsistent EVP_MAC API availability.");
}
#pragma clang diagnostic pop

EVP_MAC_CTX* CryptoNative_EvpMacCtxNew(EVP_MAC* mac)
{
    assert(mac);

#ifdef NEED_OPENSSL_3_0
    if (API_EXISTS(EVP_MAC_CTX_new))
    {
        ERR_clear_error();
        return EVP_MAC_CTX_new(mac);
    }
#else
    (void)mac;
#endif

    assert(0 && "Inconsistent EVP_MAC API availability.");
    return NULL;
}

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wmissing-noreturn"
void CryptoNative_EvpMacCtxFree(EVP_MAC_CTX* ctx)
{
#ifdef NEED_OPENSSL_3_0
    if (API_EXISTS(EVP_MAC_CTX_free))
    {
        EVP_MAC_CTX_free(ctx);
        return;
    }
#else
    (void)ctx;
#endif

    assert(0 && "Inconsistent EVP_MAC API availability.");
}
#pragma clang diagnostic pop

int32_t CryptoNative_EvpMacInit(EVP_MAC_CTX* ctx,
                                uint8_t* key,
                                int32_t keyLength,
                                uint8_t* customizationString,
                                int32_t customizationStringLength,
                                int32_t xof)
{
    if (ctx == NULL ||
        (key == NULL && keyLength > 0) || keyLength < 0 ||
        (customizationString == NULL && customizationStringLength > 0) || customizationStringLength < 0)
    {
        return -1;
    }

#ifdef NEED_OPENSSL_3_0
    if (API_EXISTS(EVP_MAC_init))
    {
        assert(API_EXISTS(OSSL_PARAM_construct_octet_string));
        assert(API_EXISTS(OSSL_PARAM_construct_end));

        ERR_clear_error();

        size_t keyLengthT = Int32ToSizeT(keyLength);

        OSSL_PARAM params[4] = {{0}};
        int i = 0;
        params[i++] = OSSL_PARAM_construct_octet_string(OSSL_MAC_PARAM_KEY, (void*) key, keyLengthT);
        params[i++] = OSSL_PARAM_construct_int32(OSSL_MAC_PARAM_XOF, &xof);

        if (customizationString && customizationStringLength > 0)
        {
            size_t customizationStringLengthT = Int32ToSizeT(customizationStringLength);
            params[i++] = OSSL_PARAM_construct_octet_string(OSSL_MAC_PARAM_CUSTOM, (void*) customizationString, customizationStringLengthT);
        }

        params[i] = OSSL_PARAM_construct_end();

        if (!EVP_MAC_init(ctx, NULL, 0, params))
        {
            return 0;
        }

        return 1;
    }
#else
    (void)ctx;
    (void)key;
    (void)keyLength;
    (void)customizationString;
    (void)customizationStringLength;
    (void)xof;
#endif

    assert(0 && "Inconsistent EVP_MAC API availability.");
    return -2;
}

int32_t CryptoNative_EvpMacReset(EVP_MAC_CTX* ctx)
{
    if (ctx == NULL)
    {
        return -1;
    }

#ifdef NEED_OPENSSL_3_0
    if (API_EXISTS(EVP_MAC_init))
    {
        ERR_clear_error();

        if (!EVP_MAC_init(ctx, NULL, 0, NULL))
        {
            return 0;
        }

        return 1;
    }
#endif

    assert(0 && "Inconsistent EVP_MAC API availability.");
    return -2;
}

int32_t CryptoNative_EvpMacUpdate(EVP_MAC_CTX* ctx, uint8_t* data, int32_t dataLength)
{
    if (ctx == NULL || (data == NULL && dataLength > 0) || dataLength < 0)
    {
        return -1;
    }

#ifdef NEED_OPENSSL_3_0
    if (API_EXISTS(EVP_MAC_update))
    {
        ERR_clear_error();

        if (dataLength > 0)
        {
            size_t dataLengthT = Int32ToSizeT(dataLength);

            if (!EVP_MAC_update(ctx, data, dataLengthT))
            {
                return 0;
            }
        }

        return 1;
    }
#endif

    assert(0 && "Inconsistent EVP_MAC API availability.");
    return -2;
}

int32_t CryptoNative_EvpMacFinal(EVP_MAC_CTX* ctx, uint8_t* mac, int32_t macLength)
{
    if (ctx == NULL || (mac == NULL && macLength > 0) || macLength < 0)
    {
        return -1;
    }

#ifdef NEED_OPENSSL_3_0
    if (API_EXISTS(EVP_MAC_final))
    {
        assert(API_EXISTS(OSSL_PARAM_construct_end));
        assert(API_EXISTS(OSSL_PARAM_construct_int32));
        assert(API_EXISTS(EVP_MAC_CTX_set_params));

        ERR_clear_error();

        size_t macLengthT = Int32ToSizeT(macLength);
        OSSL_PARAM params[] =
        {
            OSSL_PARAM_construct_int32(OSSL_MAC_PARAM_SIZE, &macLength),
            OSSL_PARAM_construct_end(),
        };

        if (!EVP_MAC_CTX_set_params(ctx, params))
        {
            return 0;
        }

        size_t written = 0;

        if (!EVP_MAC_final(ctx, mac, &written, macLengthT))
        {
            return 0;
        }

        if (written != macLengthT)
        {
            return -3;
        }

        return 1;
    }
#endif

    assert(0 && "Inconsistent EVP_MAC API availability.");
    return -2;
}

int32_t CryptoNative_EvpMacCurrent(EVP_MAC_CTX* ctx, uint8_t* mac, int32_t macLength)
{
    // CryptoNative_EvpMacFinal will perform parameter validation. These are invariants
    // so it's okay to validate them after allocations since this is not expected to
    // ever occur.

#ifdef NEED_OPENSSL_3_0
    if (API_EXISTS(EVP_MAC_CTX_dup))
    {
        assert(API_EXISTS(EVP_MAC_CTX_free));

        EVP_MAC_CTX* dup = EVP_MAC_CTX_dup(ctx);

        if (dup == NULL)
        {
            return 0;
        }

        int ret = CryptoNative_EvpMacFinal(dup, mac, macLength);
        EVP_MAC_CTX_free(dup);
        return ret;
    }
#else
    (void)ctx;
    (void)mac;
    (void)macLength;
#endif

    assert(0 && "Inconsistent EVP_MAC API availability.");
    return -2;
}

int32_t CryptoNative_EvpMacOneShot(EVP_MAC* mac,
                                   uint8_t* key,
                                   int32_t keyLength,
                                   uint8_t* customizationString,
                                   int32_t customizationStringLength,
                                   const uint8_t* data,
                                   int32_t dataLength,
                                   uint8_t* destination,
                                   int32_t destinationLength,
                                   int32_t xof)
{
    if (mac == NULL ||
        keyLength < 0 || customizationStringLength < 0 || destinationLength < 0 || dataLength < 0 ||
        (key == NULL && keyLength > 0) ||
        (customizationString == NULL && customizationStringLength > 0) ||
        (destination == NULL && destinationLength > 0) ||
        (data == NULL && dataLength > 0))
    {
        return -1;
    }

#ifdef NEED_OPENSSL_3_0
    if (API_EXISTS(EVP_MAC_CTX_new))
    {
        assert(API_EXISTS(OSSL_PARAM_construct_octet_string));
        assert(API_EXISTS(OSSL_PARAM_construct_int32));
        assert(API_EXISTS(OSSL_PARAM_construct_end));
        assert(API_EXISTS(EVP_MAC_init));
        assert(API_EXISTS(EVP_MAC_update));
        assert(API_EXISTS(EVP_MAC_final));
        assert(API_EXISTS(EVP_MAC_CTX_free));

        // Don't bother computing an empty mac.
        if (destinationLength == 0)
        {
            return 1;
        }

        ERR_clear_error();

        EVP_MAC_CTX* ctx = EVP_MAC_CTX_new(mac);

        if (!ctx)
        {
            return 0;
        }

        size_t keyLengthT = Int32ToSizeT(keyLength);
        size_t dataLengthT = Int32ToSizeT(dataLength);
        size_t macLengthT = Int32ToSizeT(destinationLength);

        OSSL_PARAM params[5] = {{0}};
        int i = 0;

        params[i++] = OSSL_PARAM_construct_octet_string(OSSL_MAC_PARAM_KEY, (void*)key, keyLengthT);
        params[i++] = OSSL_PARAM_construct_int32(OSSL_MAC_PARAM_SIZE, &destinationLength);
        params[i++] = OSSL_PARAM_construct_int32(OSSL_MAC_PARAM_XOF, &xof);

        if (customizationString && customizationStringLength > 0)
        {
            size_t customizationStringLengthT = Int32ToSizeT(customizationStringLength);
            params[i++] = OSSL_PARAM_construct_octet_string(OSSL_MAC_PARAM_CUSTOM, (void*) customizationString, customizationStringLengthT);
        }

        params[i] = OSSL_PARAM_construct_end();

        if (!EVP_MAC_init(ctx, NULL, 0, params))
        {
            EVP_MAC_CTX_free(ctx);
            return 0;
        }

        if (!EVP_MAC_update(ctx, data, dataLengthT))
        {
            EVP_MAC_CTX_free(ctx);
            return 0;
        }

        size_t written = 0;

        if (!EVP_MAC_final(ctx, destination, &written, macLengthT))
        {
            EVP_MAC_CTX_free(ctx);
            return 0;
        }

        if (written != macLengthT)
        {
            return -3;
        }

        return 1;
    }
#else
    (void)mac;
    (void)key;
    (void)keyLength;
    (void)customizationString;
    (void)customizationStringLength;
    (void)data;
    (void)dataLength;
    (void)destination;
    (void)destinationLength;
    (void)xof;
#endif

    return -2;
}
