// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "openssl.h"
#include "pal_evp.h"
#include "pal_utilities.h"

#include <assert.h>
#include <pthread.h>

#define SUCCESS 1

static const EVP_MD* g_evpFetchMd5 = NULL;
static pthread_once_t g_evpFetch = PTHREAD_ONCE_INIT;

static void EnsureFetchEvpMdAlgorithms(void)
{
    // This is called from a pthread_once - this method should not be called directly.

#ifdef NEED_OPENSSL_3_0
    if (API_EXISTS(EVP_MD_fetch))
    {
        ERR_clear_error();

        // Try to fetch an MD5 implementation that will work regardless if
        // FIPS is enforced or not.
        g_evpFetchMd5 = EVP_MD_fetch(NULL, "MD5", "-fips");
    }
#endif

    // No error queue impact.
    // If EVP_MD_fetch is unavailable, use the implicit loader. If it failed, use the implicit loader as a last resort.
    if (g_evpFetchMd5 == NULL)
    {
        g_evpFetchMd5 = EVP_md5();
    }
}

EVP_MD_CTX* CryptoNative_EvpMdCtxCreate(const EVP_MD* type)
{
    ERR_clear_error();

    EVP_MD_CTX* ctx = EVP_MD_CTX_new();

    if (ctx == NULL)
    {
        // Allocation failed.
        // This is one of the few places that don't report the error to the queue, so
        // we'll do it here.
        ERR_put_error(ERR_LIB_EVP, 0, ERR_R_MALLOC_FAILURE, __FILE__, __LINE__);
        return NULL;
    }

    // For OpenSSL 1.x, set the non-FIPS allow flag for MD5. OpenSSL 3 does this differently with EVP_MD_fetch
    // and no longer has this flag.
    if (CryptoNative_OpenSslVersionNumber() < OPENSSL_VERSION_3_0_RTM && type == EVP_md5())
    {
        EVP_MD_CTX_set_flags(ctx, EVP_MD_CTX_FLAG_NON_FIPS_ALLOW);
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
    ERR_clear_error();
    return EVP_DigestInit_ex(ctx, type, NULL);
}

int32_t CryptoNative_EvpDigestUpdate(EVP_MD_CTX* ctx, const void* d, int32_t cnt)
{
    // No error queue impact
    return EVP_DigestUpdate(ctx, d, (size_t)cnt);
}

int32_t CryptoNative_EvpDigestFinalEx(EVP_MD_CTX* ctx, uint8_t* md, uint32_t* s)
{
    ERR_clear_error();

    unsigned int size;
    int32_t ret = EVP_DigestFinal_ex(ctx, md, &size);
    if (ret == SUCCESS)
    {
        *s = size;
    }

    return ret;
}

int32_t CryptoNative_EvpDigestFinalXOF(EVP_MD_CTX* ctx, uint8_t* md, uint32_t len)
{
    #if HAVE_OPENSSL_SHA3
        if (API_EXISTS(EVP_DigestFinalXOF))
        {
            ERR_clear_error();

            // https://github.com/openssl/openssl/issues/9431
            // EVP_DigestFinalXOF has a bug in some arch-optimized code paths where it cannot tolerate a zero length
            // digest.
            // If the caller asked for no bytes, use a temporary buffer to ask for 1 byte, then throw away the result.
            // We don't want to skip calling FinalXOF entirely because we want to make sure the EVP_MD_CTX is in a
            // finalized state regardless of the length of the digest.
            // We can remove this work around when OpenSSL 3.0 is the minimum OpenSSL requirement.
            if (len == 0)
            {
                uint8_t single[1] = { 0 };
                int result = EVP_DigestFinalXOF(ctx, single, 1);
                OPENSSL_cleanse(single, sizeof(single));
                return result;
            }
            else if (!md)
            {
                // Length is not zero but we don't have a buffer to write to.
                return -1;
            }
            else
            {
                return EVP_DigestFinalXOF(ctx, md, len);
            }
        }
    #else
        // Use each parameter to avoid unused parameter warnings.
        (void)(ctx);
        (void)(md);
        (void)(len);
    #endif

    return 0;
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
        // Allocation failed.
        // This is one of the few places that don't report the error to the queue, so
        // we'll do it here.
        ERR_put_error(ERR_LIB_EVP, 0, ERR_R_MALLOC_FAILURE, __FILE__, __LINE__);
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
    ERR_clear_error();

    EVP_MD_CTX* dup = EvpDup(ctx);

    if (dup != NULL)
    {
        int ret = CryptoNative_EvpDigestFinalEx(dup, md, s);
        EVP_MD_CTX_free(dup);
        return ret;
    }

    return 0;
}

int32_t CryptoNative_EvpDigestCurrentXOF(const EVP_MD_CTX* ctx, uint8_t* md, uint32_t len)
{
    ERR_clear_error();

    EVP_MD_CTX* dup = EvpDup(ctx);

    if (dup != NULL)
    {
        int ret = CryptoNative_EvpDigestFinalXOF(dup, md, len);
        EVP_MD_CTX_free(dup);
        return ret;
    }

    return 0;
}

int32_t CryptoNative_EvpDigestOneShot(const EVP_MD* type, const void* source, int32_t sourceSize, uint8_t* md, uint32_t* mdSize)
{
    ERR_clear_error();

    if (type == NULL || sourceSize < 0 || md == NULL || mdSize == NULL)
    {
        return 0;
    }

    EVP_MD_CTX* ctx = CryptoNative_EvpMdCtxCreate(type);

    if (ctx == NULL)
    {
        return 0;
    }

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

int32_t CryptoNative_EvpDigestXOFOneShot(const EVP_MD* type, const void* source, int32_t sourceSize, uint8_t* md, uint32_t len)
{
    ERR_clear_error();

    if (type == NULL || sourceSize < 0 || (md == NULL && len > 0))
    {
        return 0;
    }

    EVP_MD_CTX* ctx = CryptoNative_EvpMdCtxCreate(type);

    if (ctx == NULL)
    {
        return 0;
    }

    int32_t ret = EVP_DigestUpdate(ctx, source, Int32ToSizeT(sourceSize));

    if (ret != SUCCESS)
    {
        CryptoNative_EvpMdCtxDestroy(ctx);
        return 0;
    }

    ret = CryptoNative_EvpDigestFinalXOF(ctx, md, len);

    CryptoNative_EvpMdCtxDestroy(ctx);
    return ret;
}

int32_t CryptoNative_EvpMdSize(const EVP_MD* md)
{
    // No error queue impact.
    return EVP_MD_get_size(md);
}

const EVP_MD* CryptoNative_EvpMd5(void)
{
    pthread_once(&g_evpFetch, EnsureFetchEvpMdAlgorithms);
    return g_evpFetchMd5;
}

const EVP_MD* CryptoNative_EvpSha1(void)
{
    // No error queue impact.
    return EVP_sha1();
}

const EVP_MD* CryptoNative_EvpSha256(void)
{
    // No error queue impact.
    return EVP_sha256();
}

const EVP_MD* CryptoNative_EvpSha384(void)
{
    // No error queue impact.
    return EVP_sha384();
}

const EVP_MD* CryptoNative_EvpSha512(void)
{
    // No error queue impact.
    return EVP_sha512();
}

const EVP_MD* CryptoNative_EvpSha3_256(void)
{
    // No error queue impact.
#if HAVE_OPENSSL_SHA3
    if (API_EXISTS(EVP_sha3_256))
    {
        return EVP_sha3_256();
    }
#endif

    return NULL;
}

const EVP_MD* CryptoNative_EvpSha3_384(void)
{
    // No error queue impact.
#if HAVE_OPENSSL_SHA3
    if (API_EXISTS(EVP_sha3_384))
    {
        return EVP_sha3_384();
    }
#endif

    return NULL;
}

const EVP_MD* CryptoNative_EvpSha3_512(void)
{
    // No error queue impact.
#if HAVE_OPENSSL_SHA3
    if (API_EXISTS(EVP_sha3_512))
    {
        return EVP_sha3_512();
    }
#endif

    return NULL;
}

const EVP_MD* CryptoNative_EvpShake128(void)
{
    // No error queue impact.
#if HAVE_OPENSSL_SHA3
    if (API_EXISTS(EVP_shake128))
    {
        return EVP_shake128();
    }
#endif

    return NULL;
}

const EVP_MD* CryptoNative_EvpShake256(void)
{
    // No error queue impact.
#if HAVE_OPENSSL_SHA3
    if (API_EXISTS(EVP_shake256))
    {
        return EVP_shake256();
    }
#endif

    return NULL;
}

int32_t CryptoNative_GetMaxMdSize(void)
{
    // No error queue impact.
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

    ERR_clear_error();

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
