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

static int HasNoPrivateKey(RSA* rsa)
{
    if (rsa == nullptr)
        return 1;

    // Shared pointer, don't free.
    const RSA_METHOD* meth = RSA_get_method(rsa);

    // The method has descibed itself as having the private key external to the structure.
    // That doesn't mean it's actually present, but we can't tell.
    if (meth->flags & RSA_FLAG_EXT_PKEY)
       return 0;

    // In the event that there's a middle-ground where we report failure when success is expected,
    // one could do something like check if the RSA_METHOD intercepts all private key operations:
    //
    // * meth->rsa_priv_enc
    // * meth->rsa_priv_dec
    // * meth->rsa_sign (in 1.0.x this is only respected if the RSA_FLAG_SIGN_VER flag is asserted)
    //
    // But, for now, leave it at the EXT_PKEY flag test.

    // The module is documented as accepting either d or the full set of CRT parameters (p, q, dp, dq, qInv)
    // So if we see d, we're good. Otherwise, if any of the rest are missing, we're public-only.
    if (rsa->d != nullptr)
        return 0;

    if (rsa->p == nullptr || rsa->q == nullptr || rsa->dmp1 == nullptr || rsa->dmq1 == nullptr || rsa->iqmp == nullptr)
        return 1;

    return 0;
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
    if (HasNoPrivateKey(rsa))
    {
        ERR_PUT_error(ERR_LIB_RSA, RSA_F_RSA_PRIVATE_DECRYPT, RSA_R_VALUE_MISSING, __FILE__, __LINE__);
        return -1;
    }

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
    if (siglen == nullptr)
    {
        assert(false);
        return 0;
    }

    *siglen = 0;

    if (HasNoPrivateKey(rsa))
    {
        ERR_PUT_error(ERR_LIB_RSA, RSA_F_RSA_SIGN, RSA_R_VALUE_MISSING, __FILE__, __LINE__);
        return 0;
    }

    // Shared pointer to the metadata about the message digest algorithm
    const EVP_MD* digest = EVP_get_digestbynid(type);

    // If the digest itself isn't known then RSA_R_UNKNOWN_ALGORITHM_TYPE will get reported, but
    // we have to check that the digest size matches what we expect.
    if (digest != nullptr && mlen != EVP_MD_size(digest))
    {
        ERR_PUT_error(ERR_LIB_RSA, RSA_F_RSA_SIGN, RSA_R_INVALID_MESSAGE_LENGTH, __FILE__, __LINE__);
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
