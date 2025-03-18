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
