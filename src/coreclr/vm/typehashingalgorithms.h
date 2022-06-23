// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ---------------------------------------------------------------------------
// Generic functions to compute the hashcode value of types
// ---------------------------------------------------------------------------

#pragma once
#include <stdlib.h>

//
// Returns the hashcode value of the 'src' string
//
inline static int ComputeNameHashCode(LPCUTF8 src)
{
    LIMITED_METHOD_CONTRACT;

    if (src == NULL || *src == '\0')
        return 0;

    int hash1 = 0x6DA3B944;
    int hash2 = 0;

    // DIFFERENT FROM NATIVEAOT: We hash UTF-8 bytes here, while NativeAOT hashes UTF-16 characters.

    for (COUNT_T i = 0; src[i] != '\0'; i += 2)
    {
        hash1 = (hash1 + _rotl(hash1, 5)) ^ src[i];
        if (src[i + 1] != '\0')
            hash2 = (hash2 + _rotl(hash2, 5)) ^ src[i + 1];
        else
            break;
    }

    hash1 += _rotl(hash1, 8);
    hash2 += _rotl(hash2, 8);

    return hash1 ^ hash2;
}

inline static int ComputeNameHashCode(LPCUTF8 pszNamespace, LPCUTF8 pszName)
{
    LIMITED_METHOD_CONTRACT;

    // DIFFERENT FROM NATIVEAOT: NativeAOT hashes the full name as one string ("namespace.name"),
    // as the full name is already available. In CoreCLR we normally only have separate
    // strings for namespace and name, thus we hash them separately.
    return ComputeNameHashCode(pszNamespace) ^ ComputeNameHashCode(pszName);
}

inline static int ComputeArrayTypeHashCode(int elementTypeHashcode, int rank)
{
    LIMITED_METHOD_CONTRACT;

    // DIFFERENT FROM NATIVEAOT: This is much simplified compared to NativeAOT, to avoid converting.rank to string.
    // For single-dimensinal array, the result is identical to NativeAOT.
    int hashCode = 0xd5313556 + rank;
    if (rank == 1)
        _ASSERTE(hashCode == ComputeNameHashCode("System.Array`1"));

    hashCode = (hashCode + _rotl(hashCode, 13)) ^ elementTypeHashcode;
    return (hashCode + _rotl(hashCode, 15));
}

inline static int ComputePointerTypeHashCode(int pointeeTypeHashcode)
{
    LIMITED_METHOD_CONTRACT;

    return (pointeeTypeHashcode + _rotl(pointeeTypeHashcode, 5)) ^ 0x12D0;
}

inline static int ComputeByrefTypeHashCode(int parameterTypeHashcode)
{
    LIMITED_METHOD_CONTRACT;

    return (parameterTypeHashcode + _rotl(parameterTypeHashcode, 7)) ^ 0x4C85;
}

inline static int ComputeNestedTypeHashCode(int enclosingTypeHashcode, int nestedTypeNameHash)
{
    LIMITED_METHOD_CONTRACT;

    return (enclosingTypeHashcode + _rotl(enclosingTypeHashcode, 11)) ^ nestedTypeNameHash;
}

template <typename TA, typename TB>
inline static int ComputeGenericInstanceHashCode(int definitionHashcode, int arity, const TA& genericTypeArguments, int (*getHashCode)(TB))
{
    LIMITED_METHOD_CONTRACT;

    int hashcode = definitionHashcode;
    for (int i = 0; i < arity; i++)
    {
        int argumentHashCode = getHashCode(genericTypeArguments[i]);
        hashcode = (hashcode + _rotl(hashcode, 13)) ^ argumentHashCode;
    }
    return (hashcode + _rotl(hashcode, 15));
}

/*

The below hash combining function is based on the xxHash32 logic implemented
in System.HashCode. In particular it is a port of the 2 element hash
combining routines, which are in turn based on xxHash32 logic.

The xxHash32 implementation is based on the code published by Yann Collet:
https://raw.githubusercontent.com/Cyan4973/xxHash/5c174cfa4e45a42f94082dc0d4539b39696afea1/xxhash.c

  xxHash - Fast Hash algorithm
  Copyright (C) 2012-2016, Yann Collet

  BSD 2-Clause License (http://www.opensource.org/licenses/bsd-license.php)

  Redistribution and use in source and binary forms, with or without
  modification, are permitted provided that the following conditions are
  met:

  * Redistributions of source code must retain the above copyright
  notice, this list of conditions and the following disclaimer.
  * Redistributions in binary form must reproduce the above
  copyright notice, this list of conditions and the following disclaimer
  in the documentation and/or other materials provided with the
  distribution.

  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
  "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
  LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
  A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
  OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
  SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
  LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
  DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
  THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
  (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
  OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

  You can contact the author at :
  - xxHash homepage: http://www.xxhash.com
  - xxHash source repository : https://github.com/Cyan4973/xxHash

*/

inline static UINT32 HashMDToken(mdToken token)
{
    // Hash function to generate a value usable for reasonable hashes from a single 32bit value
    // This function was taken from http://burtleburtle.net/bob/hash/integer.html
    UINT32 a = token;
    a -= (a<<6);
    a ^= (a>>17);
    a -= (a<<9);
    a ^= (a<<4);
    a -= (a<<3);
    a ^= (a<<10);
    a ^= (a>>15);
    return a;
}

inline static UINT32 XXHash32_MixEmptyState()
{
    // Unlike System.HashCode, these hash values are required to be stable, so don't
    // mixin a random process specific value
    return 374761393U; // Prime5
}

inline static UINT32 XXHash32_MixState(UINT32 v1, UINT32 v2, UINT32 v3, UINT32 v4)
{
    return (UINT32)_rotl(v1, 1) + (UINT32)_rotl(v2, 7) + (UINT32)_rotl(v3, 12) + (UINT32)_rotl(v4, 18);
}

inline static UINT32 XXHash32_QueueRound(UINT32 hash, UINT32 queuedValue)
{
    return ((UINT32)_rotl((int)(hash + queuedValue * 3266489917U/*Prime3*/), 17)) * 668265263U/*Prime4*/;
}

inline static UINT32 XXHash32_Round(UINT32 hash, UINT32 input)
{
    return ((UINT32)_rotl((int)(hash + input * 2246822519U/*Prime2*/), 13)) * 2654435761U/*Prime1*/;
}

inline static UINT32 XXHash32_MixFinal(UINT32 hash)
{
    hash ^= hash >> 15;
    hash *= 2246822519U/*Prime2*/;
    hash ^= hash >> 13;
    hash *= 3266489917U/*Prime3*/;
    hash ^= hash >> 16;
    return hash;
}

inline static UINT32 MixOneValueIntoHash(UINT32 value1)
{
    // This matches the behavior of System.HashCode.Combine(value1) as of the time of authoring

    // Provide a way of diffusing bits from something with a limited
    // input hash space. For example, many enums only have a few
    // possible hashes, only using the bottom few bits of the code. Some
    // collections are built on the assumption that hashes are spread
    // over a larger space, so diffusing the bits may help the
    // collection work more efficiently.

    DWORD hash = XXHash32_MixEmptyState();
    hash += 4;
    hash = XXHash32_QueueRound(hash, value1);
    hash = XXHash32_MixFinal(hash);
    return hash;
}

inline static UINT32 CombineTwoValuesIntoHash(UINT32 value1, UINT32 value2)
{
    // This matches the behavior of System.HashCode.Combine(value1, value2) as of the time of authoring
    DWORD hash = XXHash32_MixEmptyState();
    hash += 8;
    hash = XXHash32_QueueRound(hash, value1);
    hash = XXHash32_QueueRound(hash, value2);
    hash = XXHash32_MixFinal(hash);
    return hash;
}

inline static UINT32 MixPointerIntoHash(void* ptr)
{
#ifdef HOST_64BIT
    return CombineTwoValuesIntoHash((UINT32)(UINT_PTR)ptr, (UINT32)(((UINT64)ptr) >> 32));
#else
    return MixOneValueIntoHash((UINT32)ptr);
#endif
}


inline static UINT32 CombineThreeValuesIntoHash(UINT32 value1, UINT32 value2, UINT32 value3)
{
    // This matches the behavior of System.HashCode.Combine(value1, value2, value3) as of the time of authoring
    DWORD hash = XXHash32_MixEmptyState();
    hash += 12;
    hash = XXHash32_QueueRound(hash, value1);
    hash = XXHash32_QueueRound(hash, value2);
    hash = XXHash32_QueueRound(hash, value3);
    hash = XXHash32_MixFinal(hash);
    return hash;
}

// This is a port of the System.HashCode logic for computing a hashcode using the xxHash algorithm
// However, as this is intended to provide a stable hash, the seed value is always 0.
class xxHash
{
    const uint32_t seed =   0;
    const uint32_t Prime1 = 2654435761U;
    const uint32_t Prime2 = 2246822519U;
    const uint32_t Prime3 = 3266489917U;
    const uint32_t Prime4 = 668265263U;
    const uint32_t Prime5 = 374761393U;

    uint32_t _v1 = seed + Prime1 + Prime2;
    uint32_t _v2 = seed + Prime2;
    uint32_t _v3 = seed;
    uint32_t _v4 = seed - Prime1;
    uint32_t _queue1 = 0;
    uint32_t _queue2 = 0;
    uint32_t _queue3 = 0;
    uint32_t _length = 0;

public:
    void Add(uint32_t val)
    {
        // The original xxHash works as follows:
        // 0. Initialize immediately. We can't do this in a struct (no
        //    default ctor).
        // 1. Accumulate blocks of length 16 (4 uints) into 4 accumulators.
        // 2. Accumulate remaining blocks of length 4 (1 uint) into the
        //    hash.
        // 3. Accumulate remaining blocks of length 1 into the hash.

        // There is no need for #3 as this type only accepts ints. _queue1,
        // _queue2 and _queue3 are basically a buffer so that when
        // ToHashCode is called we can execute #2 correctly.

        // Storing the value of _length locally shaves of quite a few bytes
        // in the resulting machine code.
        uint32_t previousLength = _length++;
        uint32_t position = previousLength % 4;

        // Switch can't be inlined.

        if (position == 0)
            _queue1 = val;
        else if (position == 1)
            _queue2 = val;
        else if (position == 2)
            _queue3 = val;
        else // position == 3
        {
            _v1 = XXHash32_Round(_v1, _queue1);
            _v2 = XXHash32_Round(_v2, _queue2);
            _v3 = XXHash32_Round(_v3, _queue3);
            _v4 = XXHash32_Round(_v4, val);
        }
    }

    uint32_t ToHashCode()
    {
        // Storing the value of _length locally shaves of quite a few bytes
        // in the resulting machine code.
        uint32_t length = _length;

        // position refers to the *next* queue position in this method, so
        // position == 1 means that _queue1 is populated; _queue2 would have
        // been populated on the next call to Add.
        uint32_t position = length % 4;

        // If the length is less than 4, _v1 to _v4 don't contain anything
        // yet. xxHash32 treats this differently.

        uint32_t hash = length < 4 ? XXHash32_MixEmptyState() : XXHash32_MixState(_v1, _v2, _v3, _v4);

        // _length is incremented once per Add(Int32) and is therefore 4
        // times too small (xxHash length is in bytes, not ints).

        hash += length * 4;

        // Mix what remains in the queue

        // Switch can't be inlined right now, so use as few branches as
        // possible by manually excluding impossible scenarios (position > 1
        // is always false if position is not > 0).
        if (position > 0)
        {
            hash = XXHash32_QueueRound(hash, _queue1);
            if (position > 1)
            {
                hash = XXHash32_QueueRound(hash, _queue2);
                if (position > 2)
                    hash = XXHash32_QueueRound(hash, _queue3);
            }
        }

        hash = XXHash32_MixFinal(hash);
        return (int)hash;
    }
};
