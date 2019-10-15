// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_dsa.h"
#include "pal_utilities.h"

int32_t CryptoNative_DsaUpRef(DSA* dsa)
{
    return DSA_up_ref(dsa);
}

void CryptoNative_DsaDestroy(DSA* dsa)
{
    if (dsa != NULL)
    {
        DSA_free(dsa);
    }
}

int32_t CryptoNative_DsaGenerateKey(DSA** dsa, int32_t bits)
{
    if (!dsa)
    {
        assert(false);
        return 0;
    }

    *dsa = DSA_new();
    if (!(*dsa))
    {
        return 0;
    }

    if (!DSA_generate_parameters_ex(*dsa, bits, NULL, 0, NULL, NULL, NULL) ||
        !DSA_generate_key(*dsa))
    {
        DSA_free(*dsa);
        *dsa = NULL;
        return 0;
    }

    return 1;
}

int32_t CryptoNative_DsaSizeSignature(DSA* dsa)
{
    return DSA_size(dsa);
}

int32_t CryptoNative_DsaSizeP(DSA* dsa)
{
    if (dsa)
    {
        const BIGNUM* p;
        DSA_get0_pqg(dsa, &p, NULL, NULL);

        if (p)
        {
            return BN_num_bytes(p);
        }
    }

    return -1;
}

int32_t CryptoNative_DsaSizeQ(DSA* dsa)
{
    if (dsa)
    {
        const BIGNUM* q;
        DSA_get0_pqg(dsa, NULL, &q, NULL);

        if (q)
        {
            return BN_num_bytes(q);
        }
    }

    return -1;
}

int32_t CryptoNative_DsaSign(
    DSA* dsa,
    const uint8_t* hash,
    int32_t hashLength,
    uint8_t* refsignature,
    int32_t* outSignatureLength)
{
    if (outSignatureLength == NULL || dsa == NULL)
    {
        assert(false);
        return 0;
    }

    // DSA_OpenSSL() returns a shared pointer, no need to free/cache.
    if (DSA_get_method(dsa) == DSA_OpenSSL())
    {
        const BIGNUM* privKey;

        DSA_get0_key(dsa, NULL, &privKey);

        if (!privKey)
        {
            *outSignatureLength = 0;
            ERR_PUT_error(ERR_LIB_DSA, DSA_F_DSA_DO_SIGN, DSA_R_MISSING_PARAMETERS, __FILE__, __LINE__);
            return 0;
        }
    }

    unsigned int unsignedSigLen = 0;
    int32_t success = DSA_sign(0, hash, hashLength, refsignature, &unsignedSigLen, dsa);
    if (!success) // Only 0 and 1 returned
    {
        *outSignatureLength = 0;
        return 0;
    }

    assert(unsignedSigLen <= INT32_MAX);
    *outSignatureLength = (int32_t)unsignedSigLen;
    return 1;
}

int32_t CryptoNative_DsaVerify(
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
            // Clear the queue, as we don't check the error information.
            // Managed caller expects the error queue to be cleared in case of error.
            ERR_clear_error();
        }
        return 0;
    }

    return 1;
}

int32_t CryptoNative_GetDsaParameters(
    const DSA* dsa,
    const BIGNUM** p, int32_t* pLength,
    const BIGNUM** q, int32_t* qLength,
    const BIGNUM** g, int32_t* gLength,
    const BIGNUM** y, int32_t* yLength,
    const BIGNUM** x, int32_t* xLength)
{
    if (!dsa || !p || !q || !g || !y || !x)
    {
        assert(false);

        // since these parameters are 'out' parameters in managed code, ensure they are initialized
        if (p) *p = NULL; if (pLength) *pLength = 0;
        if (q) *q = NULL; if (qLength) *qLength = 0;
        if (g) *g = NULL; if (gLength) *gLength = 0;
        if (y) *y = NULL; if (yLength) *yLength = 0;
        if (x) *x = NULL; if (xLength) *xLength = 0;
        return 0;
    }

    DSA_get0_pqg(dsa, p, q, g);
    *pLength = BN_num_bytes(*p);
    *qLength = BN_num_bytes(*q);
    *gLength = BN_num_bytes(*g);

    DSA_get0_key(dsa, y, x);
    *yLength = BN_num_bytes(*y);
    // x (the private key) is optional
    *xLength = (*x == NULL) ? 0 : BN_num_bytes(*x);

    return 1;
}

static BIGNUM* MakeBignum(uint8_t* buffer, int32_t bufferLength)
{
    if (buffer && bufferLength)
    {
        return BN_bin2bn(buffer, bufferLength, NULL);
    }

    return NULL;
}

int32_t CryptoNative_DsaKeyCreateByExplicitParameters(
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

    BIGNUM* bnP = MakeBignum(p, pLength);
    BIGNUM* bnQ = MakeBignum(q, qLength);
    BIGNUM* bnG = MakeBignum(g, gLength);

    if (!DSA_set0_pqg(dsa, bnP, bnQ, bnG))
    {
        // BN_free handles NULL input
        BN_free(bnP);
        BN_free(bnQ);
        BN_free(bnG);
        return 0;
    }

    // Control was transferred, do not free.
    bnP = NULL;
    bnQ = NULL;
    bnG = NULL;

    BIGNUM* bnY = MakeBignum(y, yLength);
    BIGNUM* bnX = MakeBignum(x, xLength);

    if (!DSA_set0_key(dsa, bnY, bnX))
    {
        BN_free(bnY);
        BN_free(bnX);
        return 0;
    }

    return 1;
}
