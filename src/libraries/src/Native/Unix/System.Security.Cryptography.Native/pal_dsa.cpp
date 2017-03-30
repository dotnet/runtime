// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_dsa.h"
#include "pal_utilities.h"

extern "C" int32_t CryptoNative_DsaUpRef(DSA* dsa)
{
    return DSA_up_ref(dsa);
}

extern "C" void CryptoNative_DsaDestroy(DSA* dsa)
{
    if (dsa != nullptr)
    {
        DSA_free(dsa);
    }
}

extern "C" int32_t CryptoNative_DsaGenerateKey(DSA** dsa, int32_t bits)
{
    *dsa = DSA_new();
    if (!dsa)
    {
        assert(false);
        return 0;
    }

    if (!DSA_generate_parameters_ex(*dsa, bits, nullptr, 0, nullptr, nullptr, nullptr) ||
        !DSA_generate_key(*dsa))
    {
        DSA_free(*dsa);
        *dsa = nullptr;
        return 0;
    }

    return 1;
}

extern "C" int32_t CryptoNative_DsaSizeSignature(DSA* dsa)
{
    return DSA_size(dsa);
}

extern "C" int32_t CryptoNative_DsaSizeP(DSA* dsa)
{
    return BN_num_bytes(dsa->p);
}

extern "C" int32_t CryptoNative_DsaSizeQ(DSA* dsa)
{
    return BN_num_bytes(dsa->q);
}

extern "C" int32_t CryptoNative_DsaSign(
    DSA* dsa,
    const uint8_t* hash,
    int32_t hashLength,
    uint8_t* refsignature,
    int32_t* outSignatureLength)
{
    if (outSignatureLength == nullptr || dsa == nullptr)
    {
        assert(false);
        return 0;
    }

    // DSA_OpenSSL() returns a shared pointer, no need to free/cache.
    if (dsa->meth == DSA_OpenSSL() && dsa->priv_key == nullptr)
    {
        *outSignatureLength = 0;
        ERR_PUT_error(ERR_LIB_DSA, DSA_F_DSA_DO_SIGN, DSA_R_MISSING_PARAMETERS, __FILE__, __LINE__);
        return 0;
    }

    unsigned int unsignedSigLen = 0;
    int32_t success = DSA_sign(0, hash, hashLength, refsignature, &unsignedSigLen, dsa);
    if (!success) // Only 0 and 1 returned
    {
        *outSignatureLength = 0;
        return 0;
    }

    assert(unsignedSigLen <= INT32_MAX);
    *outSignatureLength = static_cast<int32_t>(unsignedSigLen);
    return 1;
}

extern "C" int32_t CryptoNative_DsaVerify(
    DSA* dsa,
    const uint8_t* hash,
    int32_t hashLength,
    uint8_t* signature,
    int32_t signatureLength)
{
    int32_t success = DSA_verify(0, hash, hashLength, signature, signatureLength, dsa);
    if (success != 1)
    {
        if (success == -1)
        {
            // Clear the queue, as we don't check the error information
            ERR_clear_error();
        }
        return 0;
    }

    return 1;
}

extern "C" int32_t CryptoNative_GetDsaParameters(
    const DSA* dsa,
    BIGNUM** p, int32_t* pLength,
    BIGNUM** q, int32_t* qLength,
    BIGNUM** g, int32_t* gLength,
    BIGNUM** y, int32_t* yLength,
    BIGNUM** x, int32_t* xLength)
{
    if (!dsa || !p || !q || !g || !y || !x)
    {
        assert(false);

        // since these parameters are 'out' parameters in managed code, ensure they are initialized
        if (p) *p = nullptr; if (pLength) *pLength = 0;
        if (q) *q = nullptr; if (qLength) *qLength = 0;
        if (g) *g = nullptr; if (gLength) *gLength = 0;
        if (y) *y = nullptr; if (yLength) *yLength = 0;
        if (x) *x = nullptr; if (xLength) *xLength = 0;
        return 0;
    }
    
    *p = dsa->p; *pLength = BN_num_bytes(*p);
    *q = dsa->q; *qLength = BN_num_bytes(*q);
    *g = dsa->g; *gLength = BN_num_bytes(*g);
    *y = dsa->pub_key; *yLength = BN_num_bytes(*y);

    // dsa->priv_key is optional
    *x = dsa->priv_key;
    *xLength = (*x == nullptr) ? 0 : BN_num_bytes(*x);

    return 1;
}

static void SetDsaParameter(BIGNUM** dsaFieldAddress, uint8_t* buffer, int32_t bufferLength)
{
    assert(dsaFieldAddress != nullptr);
    if (dsaFieldAddress)
    {
        if (!buffer || !bufferLength)
        {
            *dsaFieldAddress = nullptr;
        }
        else
        {
            BIGNUM* bigNum = BN_bin2bn(buffer, bufferLength, nullptr);
            *dsaFieldAddress = bigNum;
        }
    }
}

extern "C" int32_t CryptoNative_DsaKeyCreateByExplicitParameters(
    DSA** outDsa,
    uint8_t* p,
    int32_t pLength,
    uint8_t* q,
    int32_t qLength,
    uint8_t* g,
    int32_t gLength,
    uint8_t* y,
    int32_t yLength,
    uint8_t* x,
    int32_t xLength)
{
    if (!outDsa)
    {
        assert(false);
        return 0;
    }

    *outDsa = DSA_new();
    if (!*outDsa)
    {
        return 0;
    }

    DSA* dsa = *outDsa;

    SetDsaParameter(&dsa->p, p, pLength);
    SetDsaParameter(&dsa->q, q, qLength);
    SetDsaParameter(&dsa->g, g, gLength);
    SetDsaParameter(&dsa->pub_key, y, yLength);
    SetDsaParameter(&dsa->priv_key, x, xLength);

    return 1;
}
