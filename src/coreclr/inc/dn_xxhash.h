// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ---------------------------------------------------------------------------
// xxHash32 primitives and helpers
//
// Based on the xxHash32 logic implemented in System.HashCode, which is in
// turn based on the code published by Yann Collet:
// https://raw.githubusercontent.com/Cyan4973/xxHash/5c174cfa4e45a42f94082dc0d4539b39696afea1/xxhash.c
//
//   xxHash - Fast Hash algorithm
//   Copyright (C) 2012-2016, Yann Collet
//
//   BSD 2-Clause License (http://www.opensource.org/licenses/bsd-license.php)
//
//   Redistribution and use in source and binary forms, with or without
//   modification, are permitted provided that the following conditions are
//   met:
//
//   * Redistributions of source code must retain the above copyright
//   notice, this list of conditions and the following disclaimer.
//   * Redistributions in binary form must reproduce the above
//   copyright notice, this list of conditions and the following disclaimer
//   in the documentation and/or other materials provided with the
//   distribution.
//
//   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
//   "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
//   LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
//   A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
//   OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
//   SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
//   LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
//   DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
//   THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
//   (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
//   OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
//   You can contact the author at :
//   - xxHash homepage: http://www.xxhash.com
//   - xxHash source repository : https://github.com/Cyan4973/xxHash
// ---------------------------------------------------------------------------

#pragma once
#include <stdlib.h>
#include <type_traits>
#include <minipal/random.h>
#include "clrtypes.h"

struct xxHashDefaultTraits
{
    static uint32_t GenerateGlobalSeed()
    {
        static uint32_t seed = []()
        {
            uint32_t s;
            minipal_get_non_cryptographically_secure_random_bytes((uint8_t*)&s, sizeof(s));
            return s;
        }();
        return seed;
    }
};

// This is a port of the System.HashCode logic for computing a hashcode using the xxHash algorithm.
// The traits type T must provide a static GenerateGlobalSeed() method that returns the seed value.
template <typename T>
class xxHash
{
    static_assert(std::is_same<decltype(T::GenerateGlobalSeed()), uint32_t>::value,
        "T must provide a static uint32_t GenerateGlobalSeed() method");

    static constexpr uint32_t Prime1 = 2654435761U;
    static constexpr uint32_t Prime2 = 2246822519U;
    static constexpr uint32_t Prime3 = 3266489917U;
    static constexpr uint32_t Prime4 = 668265263U;
    static constexpr uint32_t Prime5 = 374761393U;

public: // static
    static uint32_t MixEmptyState()
    {
        return T::GenerateGlobalSeed() + Prime5;
    }

    static uint32_t MixState(uint32_t v1, uint32_t v2, uint32_t v3, uint32_t v4)
    {
        return (uint32_t)_rotl(v1, 1) + (uint32_t)_rotl(v2, 7) + (uint32_t)_rotl(v3, 12) + (uint32_t)_rotl(v4, 18);
    }

    static uint32_t QueueRound(uint32_t hash, uint32_t queuedValue)
    {
        return ((uint32_t)_rotl((int)(hash + queuedValue * Prime3), 17)) * Prime4;
    }

    static uint32_t Round(uint32_t hash, uint32_t input)
    {
        return ((uint32_t)_rotl((int)(hash + input * Prime2), 13)) * Prime1;
    }

    static uint32_t MixFinal(uint32_t hash)
    {
        hash ^= hash >> 15;
        hash *= Prime2;
        hash ^= hash >> 13;
        hash *= Prime3;
        hash ^= hash >> 16;
        return hash;
    }

private:
    static constexpr uint32_t MixEmptyState(uint32_t seed)
    {
        return seed + Prime5;
    }

    const uint32_t seed;
    uint32_t _v1;
    uint32_t _v2;
    uint32_t _v3;
    uint32_t _v4;
    uint32_t _queue1 = 0;
    uint32_t _queue2 = 0;
    uint32_t _queue3 = 0;
    uint32_t _length = 0;

public:
    xxHash()
        : seed(T::GenerateGlobalSeed())
        , _v1{seed + Prime1 + Prime2}
        , _v2{seed + Prime2}
        , _v3{seed}
        , _v4{seed - Prime1}
    {
    }

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
            _v1 = Round(_v1, _queue1);
            _v2 = Round(_v2, _queue2);
            _v3 = Round(_v3, _queue3);
            _v4 = Round(_v4, val);
        }
    }

    void AddPointer(void* ptr)
    {
#ifdef HOST_64BIT
        Add((uint32_t)(UINT_PTR)ptr);
        Add((uint32_t)(((UINT_PTR)ptr) >> 32));
#else
        Add((uint32_t)(UINT_PTR)ptr);
#endif
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

        uint32_t hash = length < 4 ? MixEmptyState(seed) : MixState(_v1, _v2, _v3, _v4);

        // _length is incremented once per Add(Int32) and is therefore 4
        // times too small (xxHash length is in bytes, not ints).

        hash += length * 4;

        // Mix what remains in the queue

        // Switch can't be inlined right now, so use as few branches as
        // possible by manually excluding impossible scenarios (position > 1
        // is always false if position is not > 0).
        if (position > 0)
        {
            hash = QueueRound(hash, _queue1);
            if (position > 1)
            {
                hash = QueueRound(hash, _queue2);
                if (position > 2)
                    hash = QueueRound(hash, _queue3);
            }
        }

        hash = MixFinal(hash);
        return hash;
    }
};

template <typename T = xxHashDefaultTraits>
inline static uint32_t MixOneValueIntoHash(uint32_t value1)
{
    // This matches the behavior of System.HashCode.Combine(value1) as of the time of authoring

    // Provide a way of diffusing bits from something with a limited
    // input hash space. For example, many enums only have a few
    // possible hashes, only using the bottom few bits of the code. Some
    // collections are built on the assumption that hashes are spread
    // over a larger space, so diffusing the bits may help the
    // collection work more efficiently.

    uint32_t hash = xxHash<T>::MixEmptyState();
    hash += 4;
    hash = xxHash<T>::QueueRound(hash, value1);
    hash = xxHash<T>::MixFinal(hash);
    return hash;
}

template <typename T = xxHashDefaultTraits>
inline static uint32_t CombineTwoValuesIntoHash(uint32_t value1, uint32_t value2)
{
    // This matches the behavior of System.HashCode.Combine(value1, value2) as of the time of authoring
    uint32_t hash = xxHash<T>::MixEmptyState();
    hash += 8;
    hash = xxHash<T>::QueueRound(hash, value1);
    hash = xxHash<T>::QueueRound(hash, value2);
    hash = xxHash<T>::MixFinal(hash);
    return hash;
}

template <typename T = xxHashDefaultTraits>
inline static uint32_t MixPointerIntoHash(void* ptr)
{
    // This matches the behavior of System.HashCode.Combine(ptr) as of the time of authoring
#ifdef HOST_64BIT
    return CombineTwoValuesIntoHash<T>((uint32_t)(UINT_PTR)ptr, (uint32_t)(((UINT64)(UINT_PTR)ptr) >> 32));
#else
    return MixOneValueIntoHash<T>((uint32_t)ptr);
#endif
}
