// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_rsa.h"
#include "pal_utilities.h"

EVP_PKEY* CryptoNative_DecodeRsaSpki(const uint8_t* buf, int32_t len)
{
    if (buf == NULL || len <= 0)
    {
        assert(false);
        return NULL;
    }

    return d2i_PUBKEY(NULL, &buf, len);
}

static int CheckRsaPrivateKey(EVP_PKEY* pkey)
{
    if (EVP_PKEY_get0_RSA(pkey) == NULL)
    {
        return 0;
    }

    EVP_PKEY_CTX* ctx = EVP_PKEY_CTX_new(pkey, NULL);

    if (ctx == NULL)
    {
        return 0;
    }

    int ret = EVP_PKEY_check(ctx);
    EVP_PKEY_CTX_free(ctx);
    return ret;
}

EVP_PKEY* CryptoNative_DecodeRsaPkcs8(const uint8_t* buf, int32_t len)
{
    if (buf == NULL || len <= 0)
    {
        assert(false);
        return NULL;
    }

    PKCS8_PRIV_KEY_INFO* p8info = d2i_PKCS8_PRIV_KEY_INFO(NULL, &buf, len);

    if (p8info == NULL)
    {
        return NULL;
    }

    EVP_PKEY* pkey = EVP_PKCS82PKEY(p8info);

    PKCS8_PRIV_KEY_INFO_free(p8info);

    // Check that it's a valid RSA key
    if (pkey != NULL && CheckRsaPrivateKey(pkey) != 1)
    {
        EVP_PKEY_free(pkey);
        return NULL;
    }

    return pkey;
}

static int HasNoPrivateKey(RSA* rsa)
{
    if (rsa == NULL)
        return 1;

    // Shared pointer, don't free.
    const RSA_METHOD* meth = RSA_get_method(rsa);

    // The method has descibed itself as having the private key external to the structure.
    // That doesn't mean it's actually present, but we can't tell.
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wcast-qual"
    if (RSA_meth_get_flags((RSA_METHOD*)meth) & RSA_FLAG_EXT_PKEY)
#pragma clang diagnostic pop
    {
        return 0;
    }

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
    const BIGNUM* d;
    RSA_get0_key(rsa, NULL, NULL, &d);

    if (d != NULL)
    {
        return 0;
    }

    const BIGNUM* p;
    const BIGNUM* q;
    const BIGNUM* dmp1;
    const BIGNUM* dmq1;
    const BIGNUM* iqmp;

    RSA_get0_factors(rsa, &p, &q);
    RSA_get0_crt_params(rsa, &dmp1, &dmq1, &iqmp);

    if (p == NULL || q == NULL || dmp1 == NULL || dmq1 == NULL || iqmp == NULL)
    {
        return 1;
    }

    return 0;
}

int32_t CryptoNative_RsaEncrypt(EVP_PKEY* pkey,
                                const uint8_t* data,
                                int32_t dataLen,
                                RsaPadding padding,
                                const EVP_MD* digest,
                                uint8_t* destination)
{
    const int UsageError = -2;
    const int OpenSslError = -1;

    if (pkey == NULL || data == NULL || destination == NULL)
    {
        return UsageError;
    }

    EVP_PKEY_CTX* ctx = EVP_PKEY_CTX_new(pkey, NULL);

    if (ctx == NULL)
    {
        return OpenSslError;
    }

    int ret = OpenSslError;

    if (EVP_PKEY_encrypt_init(ctx) <= 0)
    {
        goto done;
    }

    if (padding == Pkcs1)
    {
        if (EVP_PKEY_CTX_set_rsa_padding(ctx, RSA_PKCS1_PADDING) <= 0)
        {
            goto done;
        }
    }
    else if (padding == OaepOrPss)
    {
        if (digest == NULL)
        {
            ret = UsageError;
            goto done;
        }

        if (EVP_PKEY_CTX_set_rsa_padding(ctx, RSA_PKCS1_OAEP_PADDING) <= 0)
        {
            goto done;
        }

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wcast-qual"
        if (EVP_PKEY_CTX_set_rsa_oaep_md(ctx, digest) <= 0)
#pragma clang diagnostic pop
        {
            goto done;
        }
    }
    else
    {
        ret = UsageError;
        goto done;
    }

    size_t written;

    if (EVP_PKEY_encrypt(ctx, destination, &written, data, Int32ToSizeT(dataLen)) > 0)
    {
        ret = SizeTToInt32(written);
    }

done:
    EVP_PKEY_CTX_free(ctx);
    return ret;
}

int32_t CryptoNative_RsaDecrypt(EVP_PKEY* pkey,
                                const uint8_t* data,
                                int32_t dataLen,
                                RsaPadding padding,
                                const EVP_MD* digest,
                                uint8_t* destination)
{
    const int WrongSize = -3;
    const int UsageError = -2;
    const int OpenSslError = -1;

    if (pkey == NULL || data == NULL || destination == NULL)
    {
        return UsageError;
    }

    EVP_PKEY_CTX* ctx = EVP_PKEY_CTX_new(pkey, NULL);

    if (ctx == NULL)
    {
        return OpenSslError;
    }

    int expectedSize = EVP_PKEY_size(pkey);

    if (dataLen != expectedSize)
    {
        return WrongSize;
    }

    int ret = OpenSslError;

    if (EVP_PKEY_decrypt_init(ctx) <= 0)
    {
        goto done;
    }

    if (padding == Pkcs1)
    {
        if (EVP_PKEY_CTX_set_rsa_padding(ctx, RSA_PKCS1_PADDING) <= 0)
        {
            goto done;
        }
    }
    else if (padding == OaepOrPss)
    {
        if (digest == NULL)
        {
            ret = UsageError;
            goto done;
        }

        if (EVP_PKEY_CTX_set_rsa_padding(ctx, RSA_PKCS1_OAEP_PADDING) <= 0)
        {
            goto done;
        }

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wcast-qual"
        if (EVP_PKEY_CTX_set_rsa_oaep_md(ctx, digest) <= 0)
#pragma clang diagnostic pop
        {
            goto done;
        }
    }
    else
    {
        ret = UsageError;
        goto done;
    }

    // This check may no longer be needed on OpenSSL 3.0
    {
        RSA* rsa = EVP_PKEY_get0_RSA(pkey);

        if (rsa == NULL || HasNoPrivateKey(rsa))
        {
            ERR_PUT_error(ERR_LIB_RSA, RSA_F_RSA_NULL_PRIVATE_DECRYPT, RSA_R_VALUE_MISSING, __FILE__, __LINE__);
            ret = -1;
            goto done;
        }
    }

    size_t written;

    if (EVP_PKEY_decrypt(ctx, destination, &written, data, Int32ToSizeT(dataLen)) > 0)
    {
        ret = SizeTToInt32(written);
    }

done:
    EVP_PKEY_CTX_free(ctx);
    return ret;
}

EVP_PKEY* CryptoNative_RsaGenerateKey(int32_t keySize)
{
    EVP_PKEY_CTX* ctx = EVP_PKEY_CTX_new_id(EVP_PKEY_RSA, NULL);

    if (ctx == NULL)
    {
        return NULL;
    }

    EVP_PKEY* pkey = NULL;
    int success = 1;
    success = success && (1 == EVP_PKEY_keygen_init(ctx));
    success = success && (1 == EVP_PKEY_CTX_set_rsa_keygen_bits(ctx, keySize));
    success = success && (1 == EVP_PKEY_keygen(ctx, &pkey));

    if (pkey != NULL && !success)
    {
        EVP_PKEY_free(pkey);
        pkey = NULL;
    }

    EVP_PKEY_CTX_free(ctx);
    return pkey;
}

int32_t CryptoNative_RsaSignHash(EVP_PKEY* pkey,
                                 RsaPadding padding,
                                 const EVP_MD* digest,
                                 const uint8_t* hash,
                                 int32_t hashLen,
                                 uint8_t* dest,
                                 int32_t* sigLen)
{
    if (sigLen == NULL)
    {
        assert(false);
        return -1;
    }

    *sigLen = 0;

    if (pkey == NULL || digest == NULL || hash == NULL || hashLen < 0 || dest == NULL)
    {
        return -1;
    }

    EVP_PKEY_CTX* ctx = EVP_PKEY_CTX_new(pkey, NULL);

    if (ctx == NULL)
    {
        return 0;
    }

    int ret = 0;

    if (EVP_PKEY_sign_init(ctx) <= 0)
    {
        goto done;
    }

    if (padding == Pkcs1)
    {
        if (EVP_PKEY_CTX_set_rsa_padding(ctx, RSA_PKCS1_PADDING) <= 0)
        {
            goto done;
        }
    }
    else if (padding == OaepOrPss)
    {
        if (EVP_PKEY_CTX_set_rsa_padding(ctx, RSA_PKCS1_PSS_PADDING) <= 0)
        {
            goto done;
        }

        if (EVP_PKEY_CTX_set_rsa_pss_saltlen(ctx, RSA_PSS_SALTLEN_DIGEST) <= 0)
        {
            goto done;
        }
    }
    else
    {
        // Usage error: unknown padding.
        ret = -1;
        goto done;
    }

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wcast-qual"
    if (EVP_PKEY_CTX_set_signature_md(ctx, digest) <= 0)
#pragma clang diagnostic pop
    {
        goto done;
    }

    // This check may no longer be needed on OpenSSL 3.0
    {
        RSA* rsa = EVP_PKEY_get0_RSA(pkey);

        if (rsa == NULL || HasNoPrivateKey(rsa))
        {
            ERR_PUT_error(ERR_LIB_RSA, RSA_F_RSA_NULL_PRIVATE_DECRYPT, RSA_R_VALUE_MISSING, __FILE__, __LINE__);
            ret = 0;
            goto done;
        }
    }

    size_t written;

    if (EVP_PKEY_sign(ctx, dest, &written, hash, Int32ToSizeT(hashLen)) > 0)
    {
        ret = 1;
        *sigLen = SizeTToInt32(written);
    }

done:
    EVP_PKEY_CTX_free(ctx);
    return ret;
}

int32_t CryptoNative_RsaVerifyHash(EVP_PKEY* pkey,
                                   RsaPadding padding,
                                   const EVP_MD* digest,
                                   const uint8_t* hash,
                                   int32_t hashLen,
                                   uint8_t* signature,
                                   int32_t sigLen)
{
    const int UsageError = INT_MIN;

    if (pkey == NULL || digest == NULL || hash == NULL || hashLen < 0 || signature == NULL || sigLen < 0)
    {
        return UsageError;
    }

    EVP_PKEY_CTX* ctx = EVP_PKEY_CTX_new(pkey, NULL);

    if (ctx == NULL)
    {
        return -1;
    }

    int ret = 0;

    if (EVP_PKEY_verify_init(ctx) <= 0)
    {
        goto done;
    }

    if (padding == Pkcs1)
    {
        if (EVP_PKEY_CTX_set_rsa_padding(ctx, RSA_PKCS1_PADDING) <= 0)
        {
            goto done;
        }
    }
    else if (padding == OaepOrPss)
    {
        if (EVP_PKEY_CTX_set_rsa_padding(ctx, RSA_PKCS1_PSS_PADDING) <= 0)
        {
            goto done;
        }

        if (EVP_PKEY_CTX_set_rsa_pss_saltlen(ctx, RSA_PSS_SALTLEN_DIGEST) <= 0)
        {
            goto done;
        }
    }
    else
    {
        // Usage error: unknown padding.
        ret = UsageError;
        goto done;
    }

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wcast-qual"
    if (EVP_PKEY_CTX_set_signature_md(ctx, digest) <= 0)
#pragma clang diagnostic pop
    {
        goto done;
    }

    // EVP_PKEY_verify is not consistent on whether a mis-sized hash is an error or just a mismatch.
    // Normalize to mismatch.
    if (hashLen != EVP_MD_size(digest))
    {
        ret = 0;
        goto done;
    }

    ret = EVP_PKEY_verify(ctx, signature, Int32ToSizeT(sigLen), hash, Int32ToSizeT(hashLen));

done:
    EVP_PKEY_CTX_free(ctx);
    return ret;
}

BIO* CryptoNative_ExportRSAPublicKey(EVP_PKEY* pkey)
{
    BIO* bio = BIO_new(BIO_s_mem());

    if (bio == NULL)
    {
        return NULL;
    }

    // get0 means not upreffed, don't free.
    RSA* rsa = EVP_PKEY_get0_RSA(pkey);

    if (rsa == NULL || i2d_RSAPublicKey_bio(bio, rsa) <= 0)
    {
        BIO_free(bio);
        return NULL;
    }

    return bio;
}

BIO* CryptoNative_ExportRSAPrivateKey(EVP_PKEY* pkey)
{
    BIO* bio = BIO_new(BIO_s_mem());

    if (bio == NULL)
    {
        return NULL;
    }

    // get0 means not upreffed, don't free.
    RSA* rsa = EVP_PKEY_get0_RSA(pkey);

    if (rsa == NULL || HasNoPrivateKey(rsa) || i2d_RSAPrivateKey_bio(bio, rsa) <= 0)
    {
        BIO_free(bio);
        return NULL;
    }

    return bio;
}
