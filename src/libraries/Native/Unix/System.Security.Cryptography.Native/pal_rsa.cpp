// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_rsa.h"
#include "pal_utilities.h"

extern "C" RSA* CryptoNative_RsaCreate()
{
    return RSA_new();
}

extern "C" int32_t CryptoNative_RsaUpRef(RSA* rsa)
{
    return RSA_up_ref(rsa);
}

extern "C" void CryptoNative_RsaDestroy(RSA* rsa)
{
    if (rsa != nullptr)
    {
        RSA_free(rsa);
    }
}

extern "C" RSA* CryptoNative_DecodeRsaPublicKey(const uint8_t* buf, int32_t len)
{
    if (!buf || !len)
    {
        return nullptr;
    }

    return d2i_RSAPublicKey(nullptr, &buf, len);
}

static int GetOpenSslPadding(RsaPadding padding)
{
    assert(padding == Pkcs1 || padding == OaepSHA1);

    return padding == Pkcs1 ? RSA_PKCS1_PADDING : RSA_PKCS1_OAEP_PADDING;
}

extern "C" int32_t
CryptoNative_RsaPublicEncrypt(int32_t flen, const uint8_t* from, uint8_t* to, RSA* rsa, RsaPadding padding)
{
    int openSslPadding = GetOpenSslPadding(padding);
    return RSA_public_encrypt(flen, from, to, rsa, openSslPadding);
}

extern "C" int32_t
CryptoNative_RsaPrivateDecrypt(int32_t flen, const uint8_t* from, uint8_t* to, RSA* rsa, RsaPadding padding)
{
    int openSslPadding = GetOpenSslPadding(padding);
    return RSA_private_decrypt(flen, from, to, rsa, openSslPadding);
}

extern "C" int32_t CryptoNative_RsaSize(RSA* rsa)
{
    return RSA_size(rsa);
}

extern "C" int32_t CryptoNative_RsaGenerateKeyEx(RSA* rsa, int32_t bits, BIGNUM* e)
{
    return RSA_generate_key_ex(rsa, bits, e, nullptr);
}

extern "C" int32_t
CryptoNative_RsaSign(int32_t type, const uint8_t* m, int32_t mlen, uint8_t* sigret, int32_t* siglen, RSA* rsa)
{
    if (!siglen)
    {
        assert(false);
        return 0;
    }

    unsigned int unsignedSigLen = 0;
    int32_t ret = RSA_sign(type, m, UnsignedCast(mlen), sigret, &unsignedSigLen, rsa);
    assert(unsignedSigLen <= INT32_MAX);
    *siglen = static_cast<int32_t>(unsignedSigLen);
    return ret;
}

extern "C" int32_t
CryptoNative_RsaVerify(int32_t type, const uint8_t* m, int32_t mlen, uint8_t* sigbuf, int32_t siglen, RSA* rsa)
{
    return RSA_verify(type, m, UnsignedCast(mlen), sigbuf, UnsignedCast(siglen), rsa);
}

extern "C" int32_t CryptoNative_GetRsaParameters(const RSA* rsa,
                                                 BIGNUM** n,
                                                 BIGNUM** e,
                                                 BIGNUM** d,
                                                 BIGNUM** p,
                                                 BIGNUM** dmp1,
                                                 BIGNUM** q,
                                                 BIGNUM** dmq1,
                                                 BIGNUM** iqmp)
{
    if (!rsa || !n || !e || !d || !p || !dmp1 || !q || !dmq1 || !iqmp)
    {
        assert(false);

        // since these parameters are 'out' parameters in managed code, ensure they are initialized
        if (n)
            *n = nullptr;
        if (e)
            *e = nullptr;
        if (d)
            *d = nullptr;
        if (p)
            *p = nullptr;
        if (dmp1)
            *dmp1 = nullptr;
        if (q)
            *q = nullptr;
        if (dmq1)
            *dmq1 = nullptr;
        if (iqmp)
            *iqmp = nullptr;

        return 0;
    }

    *n = rsa->n;
    *e = rsa->e;
    *d = rsa->d;
    *p = rsa->p;
    *dmp1 = rsa->dmp1;
    *q = rsa->q;
    *dmq1 = rsa->dmq1;
    *iqmp = rsa->iqmp;

    return 1;
}

static void SetRsaParameter(BIGNUM** rsaFieldAddress, uint8_t* buffer, int32_t bufferLength)
{
    assert(rsaFieldAddress != nullptr);
    if (rsaFieldAddress)
    {
        if (!buffer || !bufferLength)
        {
            *rsaFieldAddress = nullptr;
        }
        else
        {
            BIGNUM* bigNum = BN_bin2bn(buffer, bufferLength, nullptr);
            *rsaFieldAddress = bigNum;
        }
    }
}

extern "C" void CryptoNative_SetRsaParameters(RSA* rsa,
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
        return;
    }

    SetRsaParameter(&rsa->n, n, nLength);
    SetRsaParameter(&rsa->e, e, eLength);
    SetRsaParameter(&rsa->d, d, dLength);
    SetRsaParameter(&rsa->p, p, pLength);
    SetRsaParameter(&rsa->dmp1, dmp1, dmp1Length);
    SetRsaParameter(&rsa->q, q, qLength);
    SetRsaParameter(&rsa->dmq1, dmq1, dmq1Length);
    SetRsaParameter(&rsa->iqmp, iqmp, iqmpLength);
}
