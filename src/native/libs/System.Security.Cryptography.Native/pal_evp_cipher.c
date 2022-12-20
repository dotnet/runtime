// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_evp_cipher.h"

#include <assert.h>

#define SUCCESS 1
#define KEEP_CURRENT_DIRECTION -1

EVP_CIPHER_CTX*
CryptoNative_EvpCipherCreate2(const EVP_CIPHER* type, uint8_t* key, int32_t keyLength, unsigned char* iv, int32_t enc)
{
    ERR_clear_error();

    EVP_CIPHER_CTX* ctx = EVP_CIPHER_CTX_new();

    if (ctx == NULL)
    {
        // Allocation failed
        // This is one of the few places that don't report the error to the queue, so
        // we'll do it here.
        ERR_put_error(ERR_LIB_EVP, 0, ERR_R_MALLOC_FAILURE, __FILE__, __LINE__);
        return NULL;
    }

    if (!EVP_CIPHER_CTX_reset(ctx))
    {
        EVP_CIPHER_CTX_free(ctx);
        return NULL;
    }

    // Perform partial initialization so we can set the key lengths
    int ret = EVP_CipherInit_ex(ctx, type, NULL, NULL, NULL, 0);
    if (!ret)
    {
        EVP_CIPHER_CTX_free(ctx);
        return NULL;
    }

    if (keyLength > 0)
    {
        // Necessary when the default key size is different than current
        ret = EVP_CIPHER_CTX_set_key_length(ctx, keyLength / 8);
        if (!ret)
        {
            EVP_CIPHER_CTX_free(ctx);
            return NULL;
        }
    }

    int nid = EVP_CIPHER_get_nid(type);

    switch (nid)
    {
        case NID_rc2_ecb:
        case NID_rc2_cbc:
            // Necessary for RC2
            ret = EVP_CIPHER_CTX_ctrl(ctx, EVP_CTRL_SET_RC2_KEY_BITS, keyLength, NULL);
            if (ret <= 0)
            {
                EVP_CIPHER_CTX_free(ctx);
                return NULL;
            }
            break;
        default:
            break;
    }

    // Perform final initialization specifying the remaining arguments
    ret = EVP_CipherInit_ex(ctx, NULL, NULL, key, iv, enc);
    if (!ret)
    {
        EVP_CIPHER_CTX_free(ctx);
        return NULL;
    }

    return ctx;
}

EVP_CIPHER_CTX*
CryptoNative_EvpCipherCreatePartial(const EVP_CIPHER* type)
{
    ERR_clear_error();

    EVP_CIPHER_CTX* ctx = EVP_CIPHER_CTX_new();

    if (ctx == NULL)
    {
        // Allocation failed
        // This is one of the few places that don't report the error to the queue, so
        // we'll do it here.
        ERR_put_error(ERR_LIB_EVP, 0, ERR_R_MALLOC_FAILURE, __FILE__, __LINE__);
        return NULL;
    }

    if (!EVP_CIPHER_CTX_reset(ctx))
    {
        EVP_CIPHER_CTX_free(ctx);
        return NULL;
    }

    // Perform partial initialization so we can set the key lengths
    int ret = EVP_CipherInit_ex(ctx, type, NULL, NULL, NULL, 0);
    if (!ret)
    {
        EVP_CIPHER_CTX_free(ctx);
        return NULL;
    }

    return ctx;
}

int32_t CryptoNative_EvpCipherSetKeyAndIV(EVP_CIPHER_CTX* ctx, uint8_t* key, unsigned char* iv, int32_t enc)
{
    ERR_clear_error();

    // Perform final initialization specifying the remaining arguments
    return EVP_CipherInit_ex(ctx, NULL, NULL, key, iv, enc);
}

int32_t CryptoNative_EvpCipherSetGcmNonceLength(EVP_CIPHER_CTX* ctx, int32_t ivLength)
{
    ERR_clear_error();
    return EVP_CIPHER_CTX_ctrl(ctx, EVP_CTRL_GCM_SET_IVLEN, ivLength, NULL);
}

int32_t CryptoNative_EvpCipherSetCcmNonceLength(EVP_CIPHER_CTX* ctx, int32_t ivLength)
{
    ERR_clear_error();
    return EVP_CIPHER_CTX_ctrl(ctx, EVP_CTRL_CCM_SET_IVLEN, ivLength, NULL);
}

void CryptoNative_EvpCipherDestroy(EVP_CIPHER_CTX* ctx)
{
    if (ctx != NULL)
    {
        EVP_CIPHER_CTX_free(ctx);
    }
}

int32_t CryptoNative_EvpCipherReset(EVP_CIPHER_CTX* ctx, uint8_t* pIv, int32_t cIv)
{
    assert(cIv >= 0 && (pIv != NULL || cIv == 0));
    (void)cIv;
    ERR_clear_error();

    return EVP_CipherInit_ex(ctx, NULL, NULL, NULL, pIv, KEEP_CURRENT_DIRECTION);
}

int32_t CryptoNative_EvpCipherCtxSetPadding(EVP_CIPHER_CTX* x, int32_t padding)
{
    // No error queue impact.
    return EVP_CIPHER_CTX_set_padding(x, padding);
}

int32_t
CryptoNative_EvpCipherUpdate(EVP_CIPHER_CTX* ctx, uint8_t* out, int32_t* outl, unsigned char* in, int32_t inl)
{
    ERR_clear_error();

    int outLength;
    int32_t ret = EVP_CipherUpdate(ctx, out, &outLength, in, inl);
    if (ret == SUCCESS)
    {
        *outl = outLength;
    }

    return ret;
}

int32_t CryptoNative_EvpCipherFinalEx(EVP_CIPHER_CTX* ctx, uint8_t* outm, int32_t* outl)
{
    ERR_clear_error();

    int outLength;
    int32_t ret = EVP_CipherFinal_ex(ctx, outm, &outLength);
    if (ret == SUCCESS)
    {
        *outl = outLength;
    }

    return ret;
}

int32_t CryptoNative_EvpCipherGetGcmTag(EVP_CIPHER_CTX* ctx, uint8_t* tag, int32_t tagLength)
{
    ERR_clear_error();
    return EVP_CIPHER_CTX_ctrl(ctx, EVP_CTRL_GCM_GET_TAG, tagLength, tag);
}

int32_t CryptoNative_EvpCipherSetGcmTag(EVP_CIPHER_CTX* ctx, uint8_t* tag, int32_t tagLength)
{
    ERR_clear_error();
    return EVP_CIPHER_CTX_ctrl(ctx, EVP_CTRL_GCM_SET_TAG, tagLength, tag);
}

int32_t CryptoNative_EvpCipherGetCcmTag(EVP_CIPHER_CTX* ctx, uint8_t* tag, int32_t tagLength)
{
    ERR_clear_error();
    return EVP_CIPHER_CTX_ctrl(ctx, EVP_CTRL_CCM_GET_TAG, tagLength, tag);
}

int32_t CryptoNative_EvpCipherSetCcmTag(EVP_CIPHER_CTX* ctx, uint8_t* tag, int32_t tagLength)
{
    ERR_clear_error();
    return EVP_CIPHER_CTX_ctrl(ctx, EVP_CTRL_CCM_SET_TAG, tagLength, tag);
}

int32_t CryptoNative_EvpCipherGetAeadTag(EVP_CIPHER_CTX* ctx, uint8_t* tag, int32_t tagLength)
{
    ERR_clear_error();
#if HAVE_OPENSSL_CHACHA20POLY1305
    return EVP_CIPHER_CTX_ctrl(ctx, EVP_CTRL_AEAD_GET_TAG, tagLength, tag);
#else
    (void)ctx;
    (void)tag;
    (void)tagLength;
    return 0;
#endif
}

int32_t CryptoNative_EvpCipherSetAeadTag(EVP_CIPHER_CTX* ctx, uint8_t* tag, int32_t tagLength)
{
    ERR_clear_error();
#if HAVE_OPENSSL_CHACHA20POLY1305
    return EVP_CIPHER_CTX_ctrl(ctx, EVP_CTRL_AEAD_SET_TAG, tagLength, tag);
#else
    (void)ctx;
    (void)tag;
    (void)tagLength;
    return 0;
#endif
}

const EVP_CIPHER* CryptoNative_EvpAes128Ecb(void)
{
    // No error queue impact.
    return EVP_aes_128_ecb();
}

const EVP_CIPHER* CryptoNative_EvpAes128Cbc(void)
{
    // No error queue impact.
    return EVP_aes_128_cbc();
}

const EVP_CIPHER* CryptoNative_EvpAes128Gcm(void)
{
    // No error queue impact.
    return EVP_aes_128_gcm();
}

const EVP_CIPHER* CryptoNative_EvpAes128Cfb128(void)
{
    // No error queue impact.
    return EVP_aes_128_cfb128();
}

const EVP_CIPHER* CryptoNative_EvpAes128Cfb8(void)
{
    // No error queue impact.
    return EVP_aes_128_cfb8();
}

const EVP_CIPHER* CryptoNative_EvpAes128Ccm(void)
{
    // No error queue impact.
    return EVP_aes_128_ccm();
}

const EVP_CIPHER* CryptoNative_EvpAes192Ecb(void)
{
    // No error queue impact.
    return EVP_aes_192_ecb();
}

const EVP_CIPHER* CryptoNative_EvpAes192Cfb128(void)
{
    // No error queue impact.
    return EVP_aes_192_cfb128();
}

const EVP_CIPHER* CryptoNative_EvpAes192Cfb8(void)
{
    // No error queue impact.
    return EVP_aes_192_cfb8();
}

const EVP_CIPHER* CryptoNative_EvpAes192Cbc(void)
{
    // No error queue impact.
    return EVP_aes_192_cbc();
}

const EVP_CIPHER* CryptoNative_EvpAes192Gcm(void)
{
    // No error queue impact.
    return EVP_aes_192_gcm();
}

const EVP_CIPHER* CryptoNative_EvpAes192Ccm(void)
{
    // No error queue impact.
    return EVP_aes_192_ccm();
}

const EVP_CIPHER* CryptoNative_EvpAes256Ecb(void)
{
    // No error queue impact.
    return EVP_aes_256_ecb();
}

const EVP_CIPHER* CryptoNative_EvpAes256Cfb128(void)
{
    // No error queue impact.
    return EVP_aes_256_cfb128();
}

const EVP_CIPHER* CryptoNative_EvpAes256Cfb8(void)
{
    // No error queue impact.
    return EVP_aes_256_cfb8();
}

const EVP_CIPHER* CryptoNative_EvpAes256Cbc(void)
{
    // No error queue impact.
    return EVP_aes_256_cbc();
}

const EVP_CIPHER* CryptoNative_EvpAes256Gcm(void)
{
    // No error queue impact.
    return EVP_aes_256_gcm();
}

const EVP_CIPHER* CryptoNative_EvpAes256Ccm(void)
{
    // No error queue impact.
    return EVP_aes_256_ccm();
}

const EVP_CIPHER* CryptoNative_EvpDesEcb(void)
{
    // No error queue impact.
    return EVP_des_ecb();
}

const EVP_CIPHER* CryptoNative_EvpDesCfb8(void)
{
    // No error queue impact.
    return EVP_des_cfb8();
}

const EVP_CIPHER* CryptoNative_EvpDesCbc(void)
{
    // No error queue impact.
    return EVP_des_cbc();
}

const EVP_CIPHER* CryptoNative_EvpDes3Ecb(void)
{
    // No error queue impact.
    return EVP_des_ede3();
}

const EVP_CIPHER* CryptoNative_EvpDes3Cfb8(void)
{
    // No error queue impact.
    return EVP_des_ede3_cfb8();
}

const EVP_CIPHER* CryptoNative_EvpDes3Cfb64(void)
{
    // No error queue impact.
    return EVP_des_ede3_cfb64();
}

const EVP_CIPHER* CryptoNative_EvpDes3Cbc(void)
{
    // No error queue impact.
    return EVP_des_ede3_cbc();
}

const EVP_CIPHER* CryptoNative_EvpRC2Ecb(void)
{
    // No error queue impact.
    return EVP_rc2_ecb();
}

const EVP_CIPHER* CryptoNative_EvpRC2Cbc(void)
{
    // No error queue impact.
    return EVP_rc2_cbc();
}

const EVP_CIPHER* CryptoNative_EvpChaCha20Poly1305(void)
{
    // No error queue impact.
#if HAVE_OPENSSL_CHACHA20POLY1305
    if (API_EXISTS(EVP_chacha20_poly1305))
    {
        return EVP_chacha20_poly1305();
    }
#endif

    return NULL;
}
