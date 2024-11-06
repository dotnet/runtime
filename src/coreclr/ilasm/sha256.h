// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// sha256.h
//

//
// contains implementation of sha256 hash algorithm
//
//*****************************************************************************
#ifndef __sha256__h__
#define __sha256__h__

#ifdef _WIN32
inline HRESULT Sha256Hash(BYTE* pSrc, DWORD srcSize, BYTE* pDst, DWORD dstSize)
{
    BCRYPT_ALG_HANDLE  algHandle = NULL;
    BCRYPT_HASH_HANDLE hashHandle = NULL;

    BYTE  hash[32]; // 256 bits
    DWORD hashLength = 0;
    DWORD resultLength = 0;

    NTSTATUS status = BCryptOpenAlgorithmProvider(&algHandle, BCRYPT_SHA256_ALGORITHM, NULL, 0);

    if (!NT_SUCCESS(status))
    {
        goto cleanup;
    }

    status = BCryptGetProperty(algHandle, BCRYPT_HASH_LENGTH, (PBYTE)&hashLength, sizeof(hashLength), &resultLength, 0);

    if (!NT_SUCCESS(status))
    {
        goto cleanup;
    }

    assert(hashLength == 32);
    status = BCryptCreateHash(algHandle, &hashHandle, NULL, 0, NULL, 0, 0);

    if (!NT_SUCCESS(status))
    {
        goto cleanup;
    }

    status = BCryptHashData(hashHandle, pSrc, srcSize, 0);

    if (!NT_SUCCESS(status))
    {
        goto cleanup;
    }

    status = BCryptFinishHash(hashHandle, hash, hashLength, 0);

    if (!NT_SUCCESS(status))
    {
        goto cleanup;
    }

    if (dstSize < hashLength)
    {
    	memcpy(pDst, hash, dstSize);
    }
    else
    {
    	memcpy(pDst, hash, hashLength);
    }

    status = S_OK;

cleanup:
    if (hashHandle != NULL)
    {
         BCryptDestroyHash(hashHandle);
    }

    if (algHandle != NULL)
    {
        BCryptCloseAlgorithmProvider(algHandle, 0);
    }

    return status;
}
#elif defined(__APPLE__)
#include <CommonCrypto/CommonCrypto.h>
#include <CommonCrypto/CommonDigest.h>

inline HRESULT Sha256Hash(BYTE* pSrc, DWORD srcSize, BYTE* pDst, DWORD dstSize)
{
    BYTE hash[32];
    CC_SHA256(pSrc, (CC_LONG)srcSize, hash);

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
#else
extern "C" {
    #include "openssl.h"
    #include "pal_evp.h"
}

inline bool IsOpenSslAvailable()
{
    return CryptoNative_OpenSslAvailable() != 0;
}

inline HRESULT Sha256Hash(BYTE* pSrc, DWORD srcSize, BYTE* pDst, DWORD dstSize)
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

    if (dstSize < hashLength)
    {
    	memcpy(pDst, hash, dstSize);
    }
    else
    {
    	memcpy(pDst, hash, hashLength);
    }

    return S_OK;
}
#endif

#endif // __sha256__h__
