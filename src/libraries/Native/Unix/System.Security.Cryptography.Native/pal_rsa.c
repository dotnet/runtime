// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

static int GetOpenSslPadding(RsaPadding padding)
{
    assert(padding == Pkcs1 || padding == OaepSHA1 || padding == NoPadding);

    switch (padding)
    {
        case Pkcs1:
            return RSA_PKCS1_PADDING;
        case OaepSHA1:
            return RSA_PKCS1_OAEP_PADDING;
        case NoPadding:
            return RSA_NO_PADDING;
    }
}

static int HasNoPrivateKey(RSA* rsa)
{
    if (rsa == NULL)
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
    if (rsa->d != NULL)
        return 0;

    if (rsa->p == NULL || rsa->q == NULL || rsa->dmp1 == NULL || rsa->dmq1 == NULL || rsa->iqmp == NULL)
        return 1;

    return 0;
}

int32_t
CryptoNative_RsaPublicEncrypt(int32_t flen, const uint8_t* from, uint8_t* to, RSA* rsa, RsaPadding padding)
{
    int openSslPadding = GetOpenSslPadding(padding);
    return RSA_public_encrypt(flen, from, to, rsa, openSslPadding);
}

int32_t
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

int32_t CryptoNative_RsaSignPrimitive(int32_t flen, const uint8_t* from, uint8_t* to, RSA* rsa)
{
    if (HasNoPrivateKey(rsa))
    {
        ERR_PUT_error(ERR_LIB_RSA, RSA_F_RSA_PRIVATE_ENCRYPT, RSA_R_VALUE_MISSING, __FILE__, __LINE__);
        return -1;
    }

    return RSA_private_encrypt(flen, from, to, rsa, RSA_NO_PADDING);
}

int32_t CryptoNative_RsaVerificationPrimitive(int32_t flen, const uint8_t* from, uint8_t* to, RSA* rsa)
{
    return RSA_public_decrypt(flen, from, to, rsa, RSA_NO_PADDING);
}

int32_t CryptoNative_RsaSize(RSA* rsa)
{
    return RSA_size(rsa);
}

int32_t CryptoNative_RsaGenerateKeyEx(RSA* rsa, int32_t bits, BIGNUM* e)
{
    return RSA_generate_key_ex(rsa, bits, e, NULL);
}

int32_t
CryptoNative_RsaSign(int32_t type, const uint8_t* m, int32_t mlen, uint8_t* sigret, int32_t* siglen, RSA* rsa)
{
    if (siglen == NULL)
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
    if (digest != NULL && mlen != EVP_MD_size(digest))
    {
        ERR_PUT_error(ERR_LIB_RSA, RSA_F_RSA_SIGN, RSA_R_INVALID_MESSAGE_LENGTH, __FILE__, __LINE__);
        return 0;
    }

    unsigned int unsignedSigLen = 0;
    int32_t ret = RSA_sign(type, m, Int32ToUint32(mlen), sigret, &unsignedSigLen, rsa);
    assert(unsignedSigLen <= INT32_MAX);
    *siglen = (int32_t)unsignedSigLen;
    return ret;
}

int32_t
CryptoNative_RsaVerify(int32_t type, const uint8_t* m, int32_t mlen, uint8_t* sigbuf, int32_t siglen, RSA* rsa)
{
    return RSA_verify(type, m, Int32ToUint32(mlen), sigbuf, Int32ToUint32(siglen), rsa);
}

int32_t CryptoNative_GetRsaParameters(const RSA* rsa,
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

static int32_t SetRsaParameter(BIGNUM** rsaFieldAddress, uint8_t* buffer, int32_t bufferLength)
{
    assert(rsaFieldAddress != NULL);
    if (rsaFieldAddress)
    {
        if (!buffer || !bufferLength)
        {
            *rsaFieldAddress = NULL;
            return 1;
        }
        else
        {
            BIGNUM* bigNum = BN_bin2bn(buffer, bufferLength, NULL);
            *rsaFieldAddress = bigNum;

            return bigNum != NULL;
        }
    }

    return 0;
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

    return 
        SetRsaParameter(&rsa->n, n, nLength) &&
        SetRsaParameter(&rsa->e, e, eLength) &&
        SetRsaParameter(&rsa->d, d, dLength) &&
        SetRsaParameter(&rsa->p, p, pLength) &&
        SetRsaParameter(&rsa->dmp1, dmp1, dmp1Length) &&
        SetRsaParameter(&rsa->q, q, qLength) &&
        SetRsaParameter(&rsa->dmq1, dmq1, dmq1Length) &&
        SetRsaParameter(&rsa->iqmp, iqmp, iqmpLength);
}
