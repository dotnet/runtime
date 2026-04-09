// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// hash.cpp - Class for hashing a text stream using MurMurHash3 hashing
//----------------------------------------------------------

#include "standardpch.h"
#include "runtimedetails.h"
#include "errorhandling.h"
#include "hash.h"

// MurmurHash3 was written by Austin Appleby, and is placed in the public
// domain. The author hereby disclaims copyright to this source code.
//
// Implementation was copied from https://github.com/aappleby/smhasher/blob/master/src/MurmurHash3.cpp
// with changes around strict-aliasing/unaligned reads

inline uint64_t ROTL64(uint64_t x, int8_t r)
{
    return (x << r) | (x >> (64 - r));
}

inline uint64_t getblock64(const uint8_t* ptr)
{
    uint64_t val = 0;
    memcpy(&val, ptr, sizeof(uint64_t));
    return val;
}

inline void setblock64(uint8_t* ptr, uint64_t val)
{
    memcpy(ptr, &val, sizeof(uint64_t));
}

// Finalization mix - force all bits of a hash block to avalanche
inline uint64_t fmix64(uint64_t k)
{
    k ^= k >> 33;
    k *= 0xff51afd7ed558ccdLLU;
    k ^= k >> 33;
    k *= 0xc4ceb9fe1a85ec53LLU;
    k ^= k >> 33;
    return k;
}

static void MurmurHash3_128(const void* key, const size_t len, const uint32_t seed, void* out)
{
    const uint8_t* data = static_cast<const uint8_t*>(key);
    const size_t nblocks = len / MM3_HASH_BYTE_SIZE;
    uint64_t h1 = seed;
    uint64_t h2 = seed;
    const uint64_t c1 = 0x87c37b91114253d5LLU;
    const uint64_t c2 = 0x4cf5ad432745937fLLU;

    // body
    for (size_t i = 0; i < nblocks; i++)
    {
        uint64_t k1 = getblock64(data + (i * 2 + 0) * sizeof(uint64_t));
        uint64_t k2 = getblock64(data + (i * 2 + 1) * sizeof(uint64_t));

        k1 *= c1; k1 = ROTL64(k1, 31); k1 *= c2; h1 ^= k1;
        h1 = ROTL64(h1, 27); h1 += h2; h1 = h1 * 5 + 0x52dce729;
        k2 *= c2; k2 = ROTL64(k2, 33); k2 *= c1; h2 ^= k2;
        h2 = ROTL64(h2, 31); h2 += h1; h2 = h2 * 5 + 0x38495ab5;
    }

    // tail
    const uint8_t* tail = data + nblocks * MM3_HASH_BYTE_SIZE;
    uint64_t k1 = 0;
    uint64_t k2 = 0;

    switch (len & 15)
    {
        case 15: k2 ^= static_cast<uint64_t>(tail[14]) << 48; FALLTHROUGH;
        case 14: k2 ^= static_cast<uint64_t>(tail[13]) << 40; FALLTHROUGH;
        case 13: k2 ^= static_cast<uint64_t>(tail[12]) << 32; FALLTHROUGH;
        case 12: k2 ^= static_cast<uint64_t>(tail[11]) << 24; FALLTHROUGH;
        case 11: k2 ^= static_cast<uint64_t>(tail[10]) << 16; FALLTHROUGH;
        case 10: k2 ^= static_cast<uint64_t>(tail[9]) << 8;   FALLTHROUGH;
        case 9:  k2 ^= static_cast<uint64_t>(tail[8]) << 0;
            k2 *= c2; k2 = ROTL64(k2, 33); k2 *= c1; h2 ^= k2;
            FALLTHROUGH;

        case 8: k1 ^= static_cast<uint64_t>(tail[7]) << 56; FALLTHROUGH;
        case 7: k1 ^= static_cast<uint64_t>(tail[6]) << 48; FALLTHROUGH;
        case 6: k1 ^= static_cast<uint64_t>(tail[5]) << 40; FALLTHROUGH;
        case 5: k1 ^= static_cast<uint64_t>(tail[4]) << 32; FALLTHROUGH;
        case 4: k1 ^= static_cast<uint64_t>(tail[3]) << 24; FALLTHROUGH;
        case 3: k1 ^= static_cast<uint64_t>(tail[2]) << 16; FALLTHROUGH;
        case 2: k1 ^= static_cast<uint64_t>(tail[1]) << 8;  FALLTHROUGH;
        case 1: k1 ^= static_cast<uint64_t>(tail[0]) << 0;
            k1 *= c1; k1 = ROTL64(k1, 31); k1 *= c2; h1 ^= k1;
            break;
    }

    // finalization
    h1 ^= len;
    h2 ^= len;
    h1 += h2;
    h2 += h1;
    h1 = fmix64(h1);
    h2 = fmix64(h2);
    h1 += h2;
    h2 += h1;

    setblock64(static_cast<uint8_t*>(out), h1);
    setblock64(static_cast<uint8_t*>(out) + sizeof(uint64_t), h2);
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
    // for (DWORD i = 0; i < MM3_HASH_BYTE_SIZE; i++)
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

// Hash::HashBuffer - Compute a MurMurHash3 hash of the data pointed to by 'pBuffer', of 'bufLen' bytes,
// writing the hexadecimal ASCII text representation of the hash to the buffer pointed to by 'hash',
// of 'hashLen' bytes in size, which must be at least MM3_HASH_BUFFER_SIZE bytes.
//
// Returns the number of bytes written, or -1 on error.
int Hash::HashBuffer(BYTE* pBuffer, size_t bufLen, char* hash, size_t hashLen)
{
    uint8_t murMurHash[MM3_HASH_BYTE_SIZE];
    MurmurHash3_128(pBuffer, bufLen, 0, murMurHash);

    if (!WriteHashValueAsText(murMurHash, MM3_HASH_BYTE_SIZE, hash, hashLen))
        return -1;

    return MM3_HASH_BUFFER_SIZE; // if we had success we wrote MM3_HASH_BUFFER_SIZE bytes to the buffer
}
