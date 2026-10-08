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
// SatoriHeap.h
//

#ifndef __SATORI_HEAP_H__
#define __SATORI_HEAP_H__

#include "common.h"
#include "../gc.h"
#include "SatoriUtil.h"

#include "SatoriLock.h"
#include "SatoriAllocator.h"
#include "SatoriRecycler.h"
#include "SatoriFinalizationQueue.h"

class SatoriPage;
class SatoriRegion;
class SatoriObject;

class SatoriHeap
{
public:
    SatoriHeap() = delete;
    ~SatoriHeap() = delete;

    static SatoriHeap* Create();

    bool TryAddRegularPage(SatoriPage*& newPage);
    SatoriPage* AddLargePage(size_t size);

    SatoriAllocator* Allocator()
    {
        return &m_allocator;
    }

    SatoriRecycler* Recycler()
    {
        return &m_recycler;
    }

    SatoriFinalizationQueue* FinalizationQueue()
    {
        return &m_finalizationQueue;
    }

    SatoriPage* PageForAddressChecked(size_t address)
    {
        size_t mapIndex = address >> Satori::PAGE_BITS;
        if (mapIndex < CommittedMapLength())
        {
            return m_pageMap[mapIndex];
        }

        return nullptr;
    }

    static bool IsInHeap(size_t address)
    {
        _ASSERTE((address & (sizeof(size_t) - 1)) == 0);

        size_t mapIndex = address >> Satori::PAGE_BITS;
        return s_pageByteMap[mapIndex] != 0;
    }

    SatoriRegion* RegionForAddressChecked(size_t address);

    template <typename F>
    void ForEachPage(F lambda)
    {
        size_t mapIndex = m_usedMapLength - 1;
        while (mapIndex > 0)
        {
            SatoriPage* page = m_pageMap[mapIndex--];
            if (page && page != m_pageMap[mapIndex])
            {
                lambda(page);
            }
        }
    }

    template <typename F>
    void ForEachPageUntil(F lambda)
    {
        size_t mapIndex = m_usedMapLength - 1;
        while (mapIndex > 0)
        {
            SatoriPage* page = m_pageMap[mapIndex--];
            if (page && page != m_pageMap[mapIndex])
            {
                if (lambda(page))
                {
                    return;
                }
            }
        }
    }

    void IncBytesCommitted(size_t bytes)
    {
        Interlocked::ExchangeAdd64(&m_committedBytes, bytes);
        _ASSERTE((ptrdiff_t)m_committedBytes > 0);
    }

    void DecBytesCommitted(size_t bytes)
    {
        Interlocked::ExchangeAdd64(&m_committedBytes, (size_t)(-(ptrdiff_t)bytes));
        _ASSERTE((ptrdiff_t)m_committedBytes > 0);
    }

    size_t GetBytesCommitted()
    {
        return m_committedBytes;
    }

private:
    // We need to cover the whole addressable space, but no need to handle inaccessible VA.
    // Accessing that should not happen for any valid reason, and will fail anyway.
    // - the canonical user VA on x64 uses lower 47 bits (128 TB)
    // - arm64 allows 48 lower bits (256 TB), linux uses that, but other OS use the same range as on x64.
    // - we can eventually support 53 bit (4 PB) and 57 bit (??) extensions, it is too early to worry about that.
    //
    // For consistency and uniform testing, we will default to 48 bit VA.
    static const int availableAddressSpaceBits = 48;
    static const int pageCountBits = availableAddressSpaceBits - Satori::PAGE_BITS;

    SatoriAllocator m_allocator;
    SatoriRecycler m_recycler;
    size_t m_reservedMapSize;
    size_t m_committedMapSize;
    size_t m_usedMapLength;
    size_t m_nextPageIndex;
    SatoriLock m_mapLock;

    size_t m_committedBytes;

    DECLSPEC_ALIGN(64)
    SatoriFinalizationQueue m_finalizationQueue;

    DECLSPEC_ALIGN(64)
    int8_t m_pageByteMap[1 << pageCountBits]{};
    static int8_t* s_pageByteMap;

#if _DEBUG
    // make the Heap obj a bit larger to force some page map commits earlier.
    int8_t dummy[0x7A70]{};
#endif

    SatoriPage* m_pageMap[1];

    bool CommitMoreMap(size_t currentlyCommitted);

    size_t CommittedMapLength()
    {
        return m_committedMapSize / sizeof(SatoriPage*);
    }
};

#endif
