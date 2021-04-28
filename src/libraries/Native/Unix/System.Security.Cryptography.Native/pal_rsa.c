// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_rsa.h"
#include "pal_utilities.h"

RSA* CryptoNative_RsaCreate()
{
    return RSA_new();
}

int32_t CryptoNative_RsaUpRef(RSA* rsa)
{
    return RSA_up_ref(rsa);
}

void CryptoNative_RsaDestroy(RSA* rsa)
{
    if (rsa != NULL)
    {
        RSA_free(rsa);
    }
}

RSA* CryptoNative_DecodeRsaPublicKey(const uint8_t* buf, int32_t len)
{
    if (!buf || !len)
    {
        return NULL;
    }

    return d2i_RSAPublicKey(NULL, &buf, len);
}

int32_t CryptoNative_RsaSize(RSA* rsa)
{
    return RSA_size(rsa);
}

int32_t CryptoNative_GetRsaParameters(const RSA* rsa,
                                      const BIGNUM** n,
                                      const BIGNUM** e,
                                      const BIGNUM** d,
                                      const BIGNUM** p,
                                      const BIGNUM** dmp1,
                                      const BIGNUM** q,
                                      const BIGNUM** dmq1,
                                      const BIGNUM** iqmp)
{
    if (!rsa || !n || !e || !d || !p || !dmp1 || !q || !dmq1 || !iqmp)
    {
        assert(false);

        // since these parameters are 'out' parameters in managed code, ensure they are initialized
        if (n)
            *n = NULL;
        if (e)
            *e = NULL;
        if (d)
            *d = NULL;
        if (p)
            *p = NULL;
        if (dmp1)
            *dmp1 = NULL;
        if (q)
            *q = NULL;
        if (dmq1)
            *dmq1 = NULL;
        if (iqmp)
            *iqmp = NULL;

        return 0;
    }

    RSA_get0_key(rsa, n, e, d);
    RSA_get0_factors(rsa, p, q);
    RSA_get0_crt_params(rsa, dmp1, dmq1, iqmp);

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

static int32_t ValidatePrivateRsaParameters(BIGNUM* bnN,
                                            BIGNUM* bnE,
                                            BIGNUM* bnD,
                                            BIGNUM* bnP,
                                            BIGNUM* bnQ,
                                            BIGNUM* bnDmp1,
                                            BIGNUM* bnDmq1,
                                            BIGNUM* bnIqmp)
{
    if (!bnN || !bnE || !bnD || !bnP || !bnQ ||
        !bnDmp1 || !bnDmq1 || !bnIqmp)
    {
        assert(false);
        return 0;
    }

    // This is shared and should not be freed.
    const RSA_METHOD* openssl_method = RSA_PKCS1_OpenSSL();

    RSA* rsa = RSA_new();

    if (!rsa)
    {
        return 0;
    }

    // RSA_check_key only works when it has access to the
    // internal values of the RSA*. If the process
    // has changed the default ENGINE, the function
    // may fail. For purposes of validation, we always
    // do it using the openssl methods.
    if (!RSA_set_method(rsa, openssl_method))
    {
        RSA_free(rsa);
        return 0;
    }

    BIGNUM* bnNdup = BN_dup(bnN);
    BIGNUM* bnEdup = BN_dup(bnE);
    BIGNUM* bnDdup = BN_dup(bnD);

    if (!RSA_set0_key(rsa, bnNdup, bnEdup, bnDdup))
    {
        BN_free(bnNdup);
        BN_free(bnEdup);
        BN_clear_free(bnDdup);
        RSA_free(rsa);
        return 0;
    }

    BIGNUM* bnPdup = BN_dup(bnP);
    BIGNUM* bnQdup = BN_dup(bnQ);

    if (!RSA_set0_factors(rsa, bnPdup, bnQdup))
    {
        BN_clear_free(bnPdup);
        BN_clear_free(bnQdup);
        RSA_free(rsa);
        return 0;
    }

    BIGNUM* bnDmp1dup = BN_dup(bnDmp1);
    BIGNUM* bnDmq1dup = BN_dup(bnDmq1);
    BIGNUM* bnIqmpdup = BN_dup(bnIqmp);

    if (!RSA_set0_crt_params(rsa, bnDmp1dup, bnDmq1dup, bnIqmpdup))
    {
        BN_clear_free(bnDmp1dup);
        BN_clear_free(bnDmq1dup);
        BN_clear_free(bnIqmpdup);
        RSA_free(rsa);
        return 0;
    }

    if (RSA_check_key(rsa) != 1)
    {
        RSA_free(rsa);
        return 0;
    }

    RSA_free(rsa);
    return 1;
}

int32_t CryptoNative_SetRsaParameters(RSA* rsa,
                                      uint8_t* n,
                                      int32_t nLength,
                                      uint8_t* e,
                                      int32_t eLength,
                                      uint8_t* d,
                                      int32_t dLength,
                                      uint8_t* p,
                                      int32_t pLength,
                                      uint8_t* dmp1,
                                      int32_t dmp1Length,
                                      uint8_t* q,
                                      int32_t qLength,
                                      uint8_t* dmq1,
                                      int32_t dmq1Length,
                                      uint8_t* iqmp,
                                      int32_t iqmpLength)
{
    if (!rsa)
    {
        assert(false);
        return 0;
    }

    BIGNUM* bnN = MakeBignum(n, nLength);
    BIGNUM* bnE = MakeBignum(e, eLength);
    BIGNUM* bnD = MakeBignum(d, dLength);

    if (!RSA_set0_key(rsa, bnN, bnE, bnD))
    {
        // BN_free handles NULL input
        BN_free(bnN);
        BN_free(bnE);
        BN_free(bnD);
        return 0;
    }

    if (bnD != NULL)
    {
        BIGNUM* bnP = MakeBignum(p, pLength);
        BIGNUM* bnQ = MakeBignum(q, qLength);
        BIGNUM* bnDmp1 = MakeBignum(dmp1, dmp1Length);
        BIGNUM* bnDmq1 = MakeBignum(dmq1, dmq1Length);
        BIGNUM* bnIqmp = MakeBignum(iqmp, iqmpLength);

        if (!ValidatePrivateRsaParameters(bnN,
                                          bnE,
                                          bnD,
                                          bnP,
                                          bnQ,
                                          bnDmp1,
                                          bnDmq1,
                                          bnIqmp)
            || !RSA_set0_factors(rsa, bnP, bnQ))
        {
            BN_free(bnP);
            BN_free(bnQ);
            BN_free(bnDmp1);
            BN_free(bnDmq1);
            BN_free(bnIqmp);
            return 0;
        }

        if (!RSA_set0_crt_params(rsa, bnDmp1, bnDmq1, bnIqmp))
        {
            BN_free(bnDmp1);
            BN_free(bnDmq1);
            BN_free(bnIqmp);
            return 0;
        }
    }

    return 1;
}
