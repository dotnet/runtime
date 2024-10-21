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
    if (dstSize < CC_SHA256_DIGEST_LENGTH)
    {
        return E_FAIL;
    }

    // Apple's documentation states these functions return 1 on success.
    CC_SHA256_CTX ctx = {{ 0 }};
    int ret = CC_SHA256_Init(&ctx);
    assert(ret == 1);

    ret = CC_SHA256_Update(&ctx, pSrc, srcSize);
    assert(ret == 1);

    ret = CC_SHA256_Final(pDst, &ctx);
    assert(ret == 1);

    return S_OK;
}
#else
// Unsupported platform
HRESULT Sha256Hash(BYTE* pSrc, DWORD srcSize, BYTE* pDst, DWORD dstSize)
{
    memset(pDst, 0, dstSize);
    return S_OK;
}
#endif
