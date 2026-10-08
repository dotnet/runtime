// Copyright (c) 2025 Vladimir Sadov
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//
// SatoriUtil.h
//

#ifndef __SATORI_UTIL_H__
#define __SATORI_UTIL_H__

#include "common.h"
#include "../gc.h"

namespace Satori
{
    class StackOnly {
    private:
        void* operator new(size_t size) throw() = delete;
        void* operator new[](size_t size) throw() = delete;
    };

    // page granularity is 1 Gb, but they can be bigger
    static const int PAGE_BITS = 30;
    static const size_t PAGE_SIZE_GRANULARITY = (size_t)1 << PAGE_BITS;

    // regions are aligned at 2 Mb
    // objects can be larger than that and straddle multiple region granules.
    // all real objects start in the first granule though to allow for fixed size of the metadata.
    const static int REGION_BITS = 21;
    const static size_t REGION_SIZE_GRANULARITY = 1 << REGION_BITS;

    const static int INDEX_GRANULARITY_BITS = 11;
    const static int INDEX_GRANULARITY = 1 << INDEX_GRANULARITY_BITS;
    const static int INDEX_LENGTH = REGION_SIZE_GRANULARITY / INDEX_GRANULARITY;

    static const int ALLOCATOR_BUCKET_COUNT = PAGE_BITS - REGION_BITS;

    // object starts are aligned to this
    static const size_t OBJECT_ALIGNMENT = sizeof(size_t);

    // minimal free size that can be made parseable.
    // we use a trivial array object to fill holes, thus this is the size of a shortest array object.
    static const size_t MIN_FREE_SIZE = 3 * sizeof(size_t);

    // If a single mark takes very roughly ~50ns (5-20 for CAS + some extra), then 1k objects marks in 50us
    // we set the chunk to roughly 1/2k to expect it mark in under 20us or so
    const static size_t MARK_CHUNK_COUNT = 512;

    // this includes header, so the number of objects is slightly less (by -2)
    const static size_t MARK_CHUNK_SIZE = MARK_CHUNK_COUNT * sizeof(size_t);

    // objects that are bigger are chunked into ranges when marking.
    // the threshold is slightly less than MARK_CHUNK_SIZE, so that object in the range
    // could fit into same chunk
    const static size_t MARK_RANGE_THRESHOLD = MARK_CHUNK_SIZE - 2 * sizeof(size_t);

    // if we have more than this much and work list is empty we can share half
    const static int SHARE_WORK_THRESHOLD = 8;

    // if we allocated this much, check if help is needed
    const static int PACE_BUDGET = REGION_SIZE_GRANULARITY / 16;

    // address bits set to track finalizable that needs to be scheduled to F-queue
    const static size_t FINALIZATION_PENDING = 1;
    const static size_t FINALIZATION_PENDING_CRITICAL = 2;
    const static size_t FINALIZATION_PENDING_ANY = FINALIZATION_PENDING | FINALIZATION_PENDING_CRITICAL;

    // one card covers 512 bytes (64 possible pointers)
    static const int BYTES_PER_CARD_BYTE = 512;
    static const int BYTES_PER_CARD_GROUP = REGION_SIZE_GRANULARITY / 2;
    static const int CARD_BYTES_IN_CARD_GROUP = Satori::BYTES_PER_CARD_GROUP / BYTES_PER_CARD_BYTE;

    namespace CardState
    {
        static const int8_t EPHEMERAL = -128; // 0b10000000 only used in cards (not groups or higher)
        static const int8_t BLANK = 0;
        static const int8_t REMEMBERED = 1;
        static const int8_t PROCESSING = 2;
        static const int8_t DIRTY = 4;
    }

    // TUNING: this is just a threshold for cases when region has too much escaped content.
    //         The actual value may not matter a lot. Still may be worth revisiting.
    // When 1/4 escapes, we stop tracking escapes.
    static const int MAX_ESCAPE_SIZE = REGION_SIZE_GRANULARITY / 4;

    // objects smaller than this go into regular region.
    static const int LARGE_OBJECT_THRESHOLD = 32 * 1024;

    // freelist bucket contais items that fit at least 1 << (i + min_bits)
    // min freelist capacity is 4K+
    static const int MIN_FREELIST_CAPACITY_BITS = 11;
    static const size_t MIN_FREELIST_CAPACITY = 1 << MIN_FREELIST_CAPACITY_BITS;
    static const int FREELIST_COUNT = Satori::REGION_BITS - MIN_FREELIST_CAPACITY_BITS;

    // large bucket size is 32K+ - so that any small obj would fit
    static const int LARGE_BUCKETS = 6;

    // we will limit number of demoted objects to not use too many chunks (up to 4)
    // it will softly limit the occupancy as well.
    const static size_t MAX_DEMOTED_OBJECTS_IN_REGION = (MARK_CHUNK_COUNT - 2) * 4;
}

class SatoriUtil
{
public:
    static size_t RoundDownPwr2(size_t value)
    {
        _ASSERTE(value > 0);
        DWORD highestBit;
#ifdef HOST_64BIT
        BitScanReverse64(&highestBit, value);
#else
        BitScanReverse(&highestBit, value);
#endif
        return (size_t)1 << highestBit;
    }

    static int32_t BucketForAlloc(size_t allocSize)
    {
        if (allocSize <= Satori::MIN_FREELIST_CAPACITY)
        {
            return 0;
        }
        else
        {
            // skip buckets that could not possibly fit allocSize
            DWORD bucket;
            BitScanReverse64(&bucket, allocSize);
            bucket = bucket - Satori::MIN_FREELIST_CAPACITY_BITS;

            // this bucket may be able to fit allocSize or may be not,
            // but we will just use the next bucket, which guarantees the allocSize will fit.
            bucket++;
            return (int32_t)bucket;
        }
    }

    // must match what is used in barriers.
    static size_t GetCurrentThreadTag()
    {
#if TARGET_WINDOWS
        // we use linear address of TEB on NT
#if defined(TARGET_ARM64)
        return (size_t)__getReg(18);
#else
        return (size_t)__readgsqword(0x30);
#endif

#elif defined(TARGET_OSX)

        size_t tag;
#if defined(TARGET_ARM64)
        __asm__ ("mrs %0, tpidrro_el0" : "=r" (tag));
        tag &= (size_t)~7;
#else
        __asm__ ("movq %%gs:0, %0" : "=r" (tag));
#endif
        return tag;

#elif defined (TARGET_LINUX)

        size_t tag;
#if defined(TARGET_ARM64)
        __asm__ ("mrs %0, tpidr_el0" : "=r" (tag));
#else
        __asm__ ("movq %%fs:0, %0" : "=r" (tag));
#endif
        return tag;
#else
        UNSUPPORTED_PLATFORM
#endif
    }

    static void Prefetch(void* addr)
    {
#if TARGET_WINDOWS
#if defined(TARGET_ARM64)
        __prefetch(addr);
#else
        _mm_prefetch((char *) addr, _MM_HINT_T0);
#endif
#else
        __builtin_prefetch(addr);
#endif
    }

    // TUNING: Needs tuning?
    // When doing regular allocation we clean this much memory
    // if we do cleaning, and if available.
    // Set to 1/2 of a typical 32K L1 cache for now
    static const size_t MinZeroInitSize()
    {
        return 16 * 1024;
    }

    // DOTNET_gcConservative
    static bool IsConservativeMode()
    {
        return (GCConfig::GetConservativeGC());
    }

    // DOTNET_gcConcurrent
    static bool IsConcurrentEnabled()
    {
        return (GCConfig::GetConcurrentGC());
    }

    // DOTNET_gcForceCompact
    static bool IsForceCompact()
    {
        return (GCConfig::GetForceCompact());
    }

    // DOTNET_gcRelocatingGen1
    static bool IsRelocatingInGen1()
    {
        return (GCConfig::GetRelocatingInGen1());
    }

    // DOTNET_gcRelocatingGen2
    static bool IsRelocatingInGen2()
    {
        return (GCConfig::GetRelocatingInGen2());
    }

    // DOTNET_gcGen0
    static bool IsGen0Enabled()
    {
        return (GCConfig::GetGen0GC());
    }

    // DOTNET_gcGen1
    static bool IsGen1Enabled()
    {
        return (GCConfig::GetGen1GC());
    }

    // DOTNET_gcTHP
    static bool UseTHP()
    {
        return (GCConfig::GetUseTHP());
    }

    // DOTNET_gcTrim
    static bool IsTrimmingEnabled()
    {
        return (GCConfig::GetTrimmigGC());
    }

    // DOTNET_gcPace
    static bool IsPacingEnabled()
    {
        return (GCConfig::GetPacingGC());
    }

    // DOTNET_GCLatencyMode
    static bool IsLowLatencyMode()
    {
        return (GCConfig::GetLatencyMode()) >= 2;
    }

    static int HandlePartitionsCount()
    {
        int partitionCount = (int)GCConfig::GetHeapCount();
        if (partitionCount < 1)
        {
            partitionCount = GCToOSInterface::GetTotalProcessorCount();
        }

        return partitionCount;
    }

    // DOTNET_gcParallel
    static int MaxWorkersCount()
    {
        return (int)GCConfig::GetParallelGC();
    }

    // DOTNET_gcRate
    static int GcRate()
    {
        int gcRate = (int)GCConfig::GetGCRate();
        if (gcRate == -1)
        {
#if _DEBUG
            // minimum rate-limiting in debug
            return 0;
#else
            return 3;
#endif
        }

        return gcRate;
    }
    
    // DOTNET_gcSpin
    static int GcSpin()
    {
        int gcSpin = (int)GCConfig::GetGCSpin();
        if (gcSpin == -1)
        {
            return 10;
        }

        return gcSpin;
    }

    // DOTNET_gcGen2Target
    static int Gen2Target()
    {
        int target = (int)GCConfig::GetGen2Target();
        if (target < 100)
        {
            // target must be > 100%
            // if we see less, just default to triggering GC when heap doubles
            target = 200;
        }

        return target;
    }

    // DOTNET_gcGen1Target
    static int Gen1Target()
    {
        int target = (int)GCConfig::GetGen1Target();
        if (target < 100)
        {
            // target must be > 100%
            // if we see less, just default to triggering GC when ephemeral heap quadruples in size
            target = Gen2Target() * 2;
        }

        return target;
    }

    static size_t CommitGranularity()
    {
        // we can support sizes that are > OS page and binary fractions of REGION_SIZE_GRANULARITY.
        // we can also support PAGE_SIZE_GRANULARITY
        size_t result = 1024 * 32;

#if defined(TARGET_LINUX)

#if defined(TARGET_ARM64)
        result = max(result, GCToOSInterface::GetPageSize());
#endif

        if (UseTHP())
        {
            result = Satori::REGION_SIZE_GRANULARITY;
        }
#endif

        // result = Satori::REGION_SIZE_GRANULARITY;

        // result = Satori::PAGE_SIZE_GRANULARITY;

        return result;
    }
};

#endif
