// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    // DIFFERENT FROM CORERT: We hash UTF-8 bytes here, while CoreRT hashes UTF-16 characters.

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

    // DIFFERENT FROM CORERT: CoreRT hashes the full name as one string ("namespace.name"),
    // as the full name is already available. In CoreCLR we normally only have separate
    // strings for namespace and name, thus we hash them separately.
    return ComputeNameHashCode(pszNamespace) ^ ComputeNameHashCode(pszName);
}

inline static int ComputeArrayTypeHashCode(int elementTypeHashcode, int rank)
{
    LIMITED_METHOD_CONTRACT;

    // DIFFERENT FROM CORERT: This is much simplified compared to CoreRT, to avoid converting.rank to string.
    // For single-dimensinal array, the result is identical to CoreRT.
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
    // Hash function to generate a value useable for reasonable hashes from a single 32bit value
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

inline static UINT32 XXHash32_QueueRound(UINT32 hash, UINT32 queuedValue)
{
    return ((UINT32)_rotl((int)(hash + queuedValue * 3266489917U/*Prime3*/), 17)) * 668265263U/*Prime4*/;
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
