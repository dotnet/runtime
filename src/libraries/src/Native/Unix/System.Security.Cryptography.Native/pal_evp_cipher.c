// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_evp_cipher.h"

#include <assert.h>

#define SUCCESS 1
#define KEEP_CURRENT_DIRECTION -1

EVP_CIPHER_CTX*
CryptoNative_EvpCipherCreate(const EVP_CIPHER* type, uint8_t* key, unsigned char* iv, int32_t enc)
{
    return CryptoNative_EvpCipherCreate2(type, key, 0, 0, iv, enc);
}

EVP_CIPHER_CTX*
CryptoNative_EvpCipherCreate2(const EVP_CIPHER* type, uint8_t* key, int32_t keyLength, int32_t effectiveKeyLength, unsigned char* iv, int32_t enc)
{
    EVP_CIPHER_CTX* ctx = EVP_CIPHER_CTX_new();
    if (ctx == NULL)
    {
        // Allocation failed
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

    if (effectiveKeyLength > 0)
    {
        // Necessary for RC2
        ret = EVP_CIPHER_CTX_ctrl(ctx, EVP_CTRL_SET_RC2_KEY_BITS, effectiveKeyLength, NULL);
        if (ret <= 0)
        {
            EVP_CIPHER_CTX_free(ctx);
            return NULL;
        }
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
    EVP_CIPHER_CTX* ctx = EVP_CIPHER_CTX_new();
    if (ctx == NULL)
    {
        // Allocation failed
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
    // Perform final initialization specifying the remaining arguments
    return EVP_CipherInit_ex(ctx, NULL, NULL, key, iv, enc);
}

int32_t CryptoNative_EvpCipherSetGcmNonceLength(EVP_CIPHER_CTX* ctx, int32_t ivLength)
{
    return EVP_CIPHER_CTX_ctrl(ctx, EVP_CTRL_GCM_SET_IVLEN, ivLength, NULL);
}

int32_t CryptoNative_EvpCipherSetCcmNonceLength(EVP_CIPHER_CTX* ctx, int32_t ivLength)
{
    return EVP_CIPHER_CTX_ctrl(ctx, EVP_CTRL_CCM_SET_IVLEN, ivLength, NULL);
}

void CryptoNative_EvpCipherDestroy(EVP_CIPHER_CTX* ctx)
{
    if (ctx != NULL)
    {
        EVP_CIPHER_CTX_free(ctx);
    }
}

int32_t CryptoNative_EvpCipherReset(EVP_CIPHER_CTX* ctx)
{
    // EVP_CipherInit_ex with all nulls preserves the algorithm, resets the IV,
    // and maintains the key.
    //
    // The only thing that you can't do is change the encryption direction,
    // that requires passing the key and IV in again.
    //
    // But since we have a different object returned for CreateEncryptor
    // and CreateDecryptor we don't need to worry about that.

    return EVP_CipherInit_ex(ctx, NULL, NULL, NULL, NULL, KEEP_CURRENT_DIRECTION);
}

int32_t CryptoNative_EvpCipherCtxSetPadding(EVP_CIPHER_CTX* x, int32_t padding)
{
    return EVP_CIPHER_CTX_set_padding(x, padding);
}

int32_t
CryptoNative_EvpCipherUpdate(EVP_CIPHER_CTX* ctx, uint8_t* out, int32_t* outl, unsigned char* in, int32_t inl)
{
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
    return EVP_CIPHER_CTX_ctrl(ctx, EVP_CTRL_GCM_GET_TAG, tagLength, tag);
}

int32_t CryptoNative_EvpCipherSetGcmTag(EVP_CIPHER_CTX* ctx, uint8_t* tag, int32_t tagLength)
{
    return EVP_CIPHER_CTX_ctrl(ctx, EVP_CTRL_GCM_SET_TAG, tagLength, tag);
}

int32_t CryptoNative_EvpCipherGetCcmTag(EVP_CIPHER_CTX* ctx, uint8_t* tag, int32_t tagLength)
{
    return EVP_CIPHER_CTX_ctrl(ctx, EVP_CTRL_CCM_GET_TAG, tagLength, tag);
}

int32_t CryptoNative_EvpCipherSetCcmTag(EVP_CIPHER_CTX* ctx, uint8_t* tag, int32_t tagLength)
{
    return EVP_CIPHER_CTX_ctrl(ctx, EVP_CTRL_CCM_SET_TAG, tagLength, tag);
}

const EVP_CIPHER* CryptoNative_EvpAes128Ecb()
{
    return EVP_aes_128_ecb();
}

const EVP_CIPHER* CryptoNative_EvpAes128Cbc()
{
    return EVP_aes_128_cbc();
}

const EVP_CIPHER* CryptoNative_EvpAes128Gcm()
{
    return EVP_aes_128_gcm();
}

const EVP_CIPHER* CryptoNative_EvpAes128Ccm()
{
    return EVP_aes_128_ccm();
}

const EVP_CIPHER* CryptoNative_EvpAes192Ecb()
{
    return EVP_aes_192_ecb();
}

const EVP_CIPHER* CryptoNative_EvpAes192Cbc()
{
    return EVP_aes_192_cbc();
}

const EVP_CIPHER* CryptoNative_EvpAes192Gcm()
{
    return EVP_aes_192_gcm();
}

const EVP_CIPHER* CryptoNative_EvpAes192Ccm()
{
    return EVP_aes_192_ccm();
}

const EVP_CIPHER* CryptoNative_EvpAes256Ecb()
{
    return EVP_aes_256_ecb();
}

const EVP_CIPHER* CryptoNative_EvpAes256Cbc()
{
    return EVP_aes_256_cbc();
}

const EVP_CIPHER* CryptoNative_EvpAes256Gcm()
{
    return EVP_aes_256_gcm();
}

const EVP_CIPHER* CryptoNative_EvpAes256Ccm()
{
    return EVP_aes_256_ccm();
}

const EVP_CIPHER* CryptoNative_EvpDesEcb()
{
    return EVP_des_ecb();
}

const EVP_CIPHER* CryptoNative_EvpDesCbc()
{
    return EVP_des_cbc();
}

const EVP_CIPHER* CryptoNative_EvpDes3Ecb()
{
    return EVP_des_ede3();
}

const EVP_CIPHER* CryptoNative_EvpDes3Cbc()
{
    return EVP_des_ede3_cbc();
}

const EVP_CIPHER* CryptoNative_EvpRC2Ecb()
{
    return EVP_rc2_ecb();
}

const EVP_CIPHER* CryptoNative_EvpRC2Cbc()
{
    return EVP_rc2_cbc();
}
