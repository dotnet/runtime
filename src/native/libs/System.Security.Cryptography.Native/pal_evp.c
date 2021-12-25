// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_evp.h"

#include <assert.h>

#define SUCCESS 1

EVP_MD_CTX* CryptoNative_EvpMdCtxCreate(const EVP_MD* type)
{
    EVP_MD_CTX* ctx = EVP_MD_CTX_new();
    if (ctx == NULL)
    {
        // Allocation failed
        return NULL;
    }

    int ret = EVP_DigestInit_ex(ctx, type, NULL);
    if (!ret)
    {
        EVP_MD_CTX_free(ctx);
        return NULL;
    }

    return ctx;
}

void CryptoNative_EvpMdCtxDestroy(EVP_MD_CTX* ctx)
{
    if (ctx != NULL)
    {
        EVP_MD_CTX_free(ctx);
    }
}

int32_t CryptoNative_EvpDigestReset(EVP_MD_CTX* ctx, const EVP_MD* type)
{
    return EVP_DigestInit_ex(ctx, type, NULL);
}

int32_t CryptoNative_EvpDigestUpdate(EVP_MD_CTX* ctx, const void* d, int32_t cnt)
{
    return EVP_DigestUpdate(ctx, d, (size_t)cnt);
}

int32_t CryptoNative_EvpDigestFinalEx(EVP_MD_CTX* ctx, uint8_t* md, uint32_t* s)
{
    unsigned int size;
    int32_t ret = EVP_DigestFinal_ex(ctx, md, &size);
    if (ret == SUCCESS)
    {
        *s = size;
    }

    return ret;
}

static EVP_MD_CTX* EvpDup(const EVP_MD_CTX* ctx)
{
    if (ctx == NULL)
    {
        return NULL;
    }

    EVP_MD_CTX* dup = EVP_MD_CTX_new();

    if (dup == NULL)
    {
        return NULL;
    }

    if (!EVP_MD_CTX_copy_ex(dup, ctx))
    {
        EVP_MD_CTX_free(dup);
        return NULL;
    }

    return dup;
}

int32_t CryptoNative_EvpDigestCurrent(const EVP_MD_CTX* ctx, uint8_t* md, uint32_t* s)
{
    EVP_MD_CTX* dup = EvpDup(ctx);

    if (dup != NULL)
    {
        int ret = CryptoNative_EvpDigestFinalEx(dup, md, s);
        EVP_MD_CTX_free(dup);
        return ret;
    }

    return 0;
}

int32_t CryptoNative_EvpDigestOneShot(const EVP_MD* type, const void* source, int32_t sourceSize, uint8_t* md, uint32_t* mdSize)
{
    if (type == NULL || sourceSize < 0 || md == NULL || mdSize == NULL)
        return 0;

    EVP_MD_CTX* ctx = CryptoNative_EvpMdCtxCreate(type);

    if (ctx == NULL)
        return 0;

    int32_t ret = EVP_DigestUpdate(ctx, source, (size_t)sourceSize);

    if (ret != SUCCESS)
    {
        CryptoNative_EvpMdCtxDestroy(ctx);
        return 0;
    }

    ret = CryptoNative_EvpDigestFinalEx(ctx, md, mdSize);

    CryptoNative_EvpMdCtxDestroy(ctx);
    return ret;
}

int32_t CryptoNative_EvpMdSize(const EVP_MD* md)
{
    return EVP_MD_get_size(md);
}

const EVP_MD* CryptoNative_EvpMd5()
{
    return EVP_md5();
}

const EVP_MD* CryptoNative_EvpSha1()
{
    return EVP_sha1();
}

const EVP_MD* CryptoNative_EvpSha256()
{
    return EVP_sha256();
}

const EVP_MD* CryptoNative_EvpSha384()
{
    return EVP_sha384();
}

const EVP_MD* CryptoNative_EvpSha512()
{
    return EVP_sha512();
}

int32_t CryptoNative_GetMaxMdSize()
{
    return EVP_MAX_MD_SIZE;
}

int32_t CryptoNative_Pbkdf2(const char* password,
                            int32_t passwordLength,
                            const unsigned char* salt,
                            int32_t saltLength,
                            int32_t iterations,
                            const EVP_MD* digest,
                            unsigned char* destination,
                            int32_t destinationLength)
{
    if (passwordLength < 0 || saltLength < 0 || iterations <= 0 || digest == NULL ||
        destination == NULL || destinationLength < 0)
    {
        return -1;
    }

    const char* empty = "";

    if (salt == NULL)
    {
        if (saltLength != 0)
        {
            return -1;
        }

        salt = (const unsigned char*)empty;
    }

    if (password == NULL)
    {
        if (passwordLength != 0)
        {
            return -1;
        }

        password = empty;
    }

    return PKCS5_PBKDF2_HMAC(
        password, passwordLength, salt, saltLength, iterations, digest, destinationLength, destination);
}
