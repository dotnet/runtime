// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// sha256.cpp
//

//
// contains implementation of sha256 hash algorithm
//
//*****************************************************************************

#ifdef _WIN32
HRESULT Sha256Hash(BYTE* pSrc, DWORD srcSize, BYTE* pDst, DWORD dstSize)
{
    NTSTATUS status;

    BCRYPT_ALG_HANDLE   algHandle = NULL;
    BCRYPT_HASH_HANDLE  hashHandle = NULL;

    BYTE    hash[32]; // 256 bits
    DWORD   hashLength = 0;
    DWORD   resultLength = 0;
    status = BCryptOpenAlgorithmProvider(&algHandle, BCRYPT_SHA256_ALGORITHM, NULL, BCRYPT_HASH_REUSABLE_FLAG);
    if(!NT_SUCCESS(status))
    {
        goto cleanup;
    }
    status = BCryptGetProperty(algHandle, BCRYPT_HASH_LENGTH, (PBYTE)&hashLength, sizeof(hashLength), &resultLength, 0);
    if(!NT_SUCCESS(status))
    {
        goto cleanup;
    }
    if (hashLength != 32)
    {
        status = STATUS_NO_MEMORY;
        goto cleanup;
    }
    status = BCryptCreateHash(algHandle, &hashHandle, NULL, 0, NULL, 0, 0);
    if(!NT_SUCCESS(status))
    {
        goto cleanup;
    }

    status = BCryptHashData(hashHandle, pSrc, srcSize, 0);
    if(!NT_SUCCESS(status))
    {
        goto cleanup;
    }

    status = BCryptFinishHash(hashHandle, hash, hashLength, 0);
    if(!NT_SUCCESS(status))
    {
        goto cleanup;
    }

    memcpy(pDst, hash, min(hashLength, dstSize));
    status = S_OK;

cleanup:
    if (NULL != hashHandle)
    {
         BCryptDestroyHash(hashHandle);
    }
    if(NULL != algHandle)
    {
        BCryptCloseAlgorithmProvider(algHandle, 0);
    }
    return status;
}
#elif defined(__APPLE__)
#include <CommonCrypto/CommonCrypto.h>
#include <CommonCrypto/CommonDigest.h>

HRESULT Sha256Hash(BYTE* pSrc, DWORD srcSize, BYTE* pDst, DWORD dstSize)
{
    CC_SHA256_CTX ctx = {{ 0 }};

    if (!CC_SHA256_Init(&ctx))
    {
        return E_FAIL;
    }

    if (!CC_SHA256_Update(&ctx, pSrc, srcSize))
    {
        return E_FAIL;
    }

    BYTE hash[CC_SHA256_DIGEST_LENGTH];

    if (!CC_SHA256_Final(hash, &ctx))
    {
        return E_FAIL;
    }

    if (dstSize < CC_SHA256_DIGEST_LENGTH)
    {
        memcpy(pDst, hash, dstSize);
    }
    else
    {
        memcpy(pDst, hash, CC_SHA256_DIGEST_LENGTH);
    }

    return S_OK;
}
#elif defined(__linux__)
extern "C" {
    #include "openssl.h"
    #include "pal_evp.h"
}

bool IsOpenSslAvailable()
{
    return CryptoNative_OpenSslAvailable() != 0;
}

HRESULT Sha256Hash(BYTE* pSrc, DWORD srcSize, BYTE* pDst, DWORD dstSize)
{
    if (!IsOpenSslAvailable() || CryptoNative_EnsureOpenSslInitialized())
    {
        return E_FAIL;
    }

    BYTE hash[32];
    DWORD hashLength = 0;

    if (!CryptoNative_EvpDigestOneShot(CryptoNative_EvpSha256(), pSrc, srcSize, hash, &hashLength))
    {
        return E_FAIL;
    }

    fprintf(stderr, "\n");

    for (int i = 0; i < hashLength; i++)
    {
        fprintf(stderr, "%c", hash[i]);
    }

    fprintf(stderr, "\n");

    memcpy(pDst, hash, min(hashLength, dstSize));
    return S_OK;
}
#else
// Unsupported platform
HRESULT Sha256Hash(BYTE* pSrc, DWORD srcSize, BYTE* pDst, DWORD dstSize)
{
    return E_FAIL;
}
#endif
