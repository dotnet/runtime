// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// hash.cpp - Class for hashing a text stream using MD5 hashing
//
// Note that on Windows, acquiring the Crypto hash provider is expensive, so
// only do that once and cache it.
//----------------------------------------------------------

#include "standardpch.h"
#include "runtimedetails.h"
#include "errorhandling.h"
#include "md5.h"
#include "hash.h"

Hash::Hash()
#ifndef TARGET_UNIX
    : m_Initialized(false)
    , m_hCryptProv(NULL)
#endif // !TARGET_UNIX
{
}

Hash::~Hash()
{
    Destroy(); // Ignoring return code.
}

// static
bool Hash::Initialize()
{
#ifdef TARGET_UNIX

    // No initialization necessary.
    return true;

#else // !TARGET_UNIX

    if (m_Initialized)
    {
        LogError("Hash class has already been initialized");
        return false;
    }

    // Get handle to the crypto provider
    if (!CryptAcquireContextA(&m_hCryptProv, NULL, NULL, PROV_RSA_FULL, CRYPT_VERIFYCONTEXT))
        goto OnError;

    m_Initialized = true;
    return true;

OnError:
    LogError("Failed to create a hash using the Crypto API (Error 0x%X)", GetLastError());

    if (m_hCryptProv != NULL)
        CryptReleaseContext(m_hCryptProv, 0);

    m_Initialized = false;
    return false;

#endif // !TARGET_UNIX
}

// static
bool Hash::Destroy()
{
#ifdef TARGET_UNIX

    // No destruction necessary.
    return true;

#else // !TARGET_UNIX

    // Should probably check Crypt() function return codes.
    if (m_hCryptProv != NULL)
    {
        CryptReleaseContext(m_hCryptProv, 0);
        m_hCryptProv = NULL;
    }

    m_Initialized = false;
    return true;

#endif // !TARGET_UNIX
}

// Hash::WriteHashValueAsText - Take a binary hash value in the array of bytes pointed to by
// 'pHash' (size in bytes 'cbHash'), and write an ASCII hexadecimal representation of it in the buffer
// 'hashTextBuffer' (size in bytes 'hashTextBufferLen').
//
// Returns true on success, false on failure (only if the arguments are bad).
bool Hash::WriteHashValueAsText(const BYTE* pHash, size_t cbHash, char* hashTextBuffer, size_t hashTextBufferLen)
{
    // This could be:
    //
    // for (DWORD i = 0; i < MD5_HASH_BYTE_SIZE; i++)
    // {
    //    sprintf_s(hash + i * 2, hashLen - i * 2, "%02X", bHash[i]);
    // }
    //
    // But this function is hot, and sprintf_s is too slow. This is a specialized function to speed it up.

    if (hashTextBufferLen < 2 * cbHash + 1) // 2 characters for each byte, plus null terminator
    {
        LogError("WriteHashValueAsText doesn't have enough space to write the output");
        return false;
    }

    static const char hexDigits[] = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
    char* pCur = hashTextBuffer;
    for (size_t i = 0; i < cbHash; i++)
    {
        unsigned digit = pHash[i];
        unsigned lowNibble = digit & 0xF;
        unsigned highNibble = digit >> 4;
        *pCur++ = hexDigits[highNibble];
        *pCur++ = hexDigits[lowNibble];
    }
    *pCur++ = '\0';
    return true;
}

// Hash::HashBuffer - Compute an MD5 hash of the data pointed to by 'pBuffer', of 'bufLen' bytes,
// writing the hexadecimal ASCII text representation of the hash to the buffer pointed to by 'hash',
// of 'hashLen' bytes in size, which must be at least MD5_HASH_BUFFER_SIZE bytes.
//
// Returns the number of bytes written, or -1 on error.
int Hash::HashBuffer(BYTE* pBuffer, size_t bufLen, char* hash, size_t hashLen)
{
#ifdef TARGET_UNIX

    MD5HASHDATA md5_hashdata;
    MD5         md5_hasher;

    if (hashLen < MD5_HASH_BUFFER_SIZE)
        return -1;

    md5_hasher.Hash(pBuffer, (ULONG)bufLen, &md5_hashdata);

    DWORD md5_hashdata_size = sizeof(md5_hashdata.rgb) / sizeof(BYTE);
    Assert(md5_hashdata_size == MD5_HASH_BYTE_SIZE);

    if (!WriteHashValueAsText(md5_hashdata.rgb, md5_hashdata_size, hash, hashLen))
        return -1;

    return MD5_HASH_BUFFER_SIZE; // if we had success we wrote MD5_HASH_BUFFER_SIZE bytes to the buffer

#else // !TARGET_UNIX

    if (!m_Initialized)
    {
        LogError("Hash class not initialized");
        return -1;
    }

    HCRYPTHASH hCryptHash;
    BYTE       bHash[MD5_HASH_BYTE_SIZE];
    DWORD      cbHash = MD5_HASH_BYTE_SIZE;

    if (hashLen < MD5_HASH_BUFFER_SIZE)
        return -1;

    if (!CryptCreateHash(m_hCryptProv, CALG_MD5, 0, 0, &hCryptHash))
        goto OnError;

    if (!CryptHashData(hCryptHash, pBuffer, (DWORD)bufLen, 0))
        goto OnError;

    if (!CryptGetHashParam(hCryptHash, HP_HASHVAL, bHash, &cbHash, 0))
        goto OnError;

    if (cbHash != MD5_HASH_BYTE_SIZE)
        goto OnError;

    if (!WriteHashValueAsText(bHash, cbHash, hash, hashLen))
        return -1;

    // Clean up.
    CryptDestroyHash(hCryptHash);
    hCryptHash = NULL;

    return MD5_HASH_BUFFER_SIZE; // if we had success we wrote MD5_HASH_BUFFER_SIZE bytes to the buffer

OnError:
    LogError("Failed to create a hash using the Crypto API (Error 0x%X)", GetLastError());

    if (hCryptHash != NULL)
    {
        CryptDestroyHash(hCryptHash);
        hCryptHash = NULL;
    }

    return -1;

#endif // !TARGET_UNIX
}
