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
// SatoriWorkList.h
//

#ifndef __SATORI_WORK_LIST_H__
#define __SATORI_WORK_LIST_H__

#include "common.h"
#include "../gc.h"
#include "SatoriWorkChunk.h"

#if _DEBUG
#define COUNT_CHUNKS
#endif

#if defined(TARGET_WINDOWS)
FORCEINLINE uint8_t Cas128(int64_t volatile *pDst, int64_t iValueHigh, int64_t iValueLow, int64_t *pComparand)
{
    return _InterlockedCompareExchange128(pDst, iValueHigh, iValueLow, pComparand);
}
#else
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Watomic-alignment"
FORCEINLINE uint8_t Cas128(int64_t volatile *pDst, int64_t iValueHigh, int64_t iValueLow, int64_t *pComparand)
{
    __int128_t iValue = ((__int128_t)iValueHigh << 64) + (uint64_t)iValueLow;
    return __sync_bool_compare_and_swap_16((__int128_t*)pDst, *(__int128_t*)pComparand, iValue);
}
#pragma clang diagnostic pop
#endif // HOST_AMD64

class DECLSPEC_ALIGN(128) SatoriWorkList
{
public:
    SatoriWorkList() :
        SatoriWorkList(nullptr, 0)
    {}

    SatoriWorkList(SatoriWorkChunk* head, size_t aba)
    {
        m_head = head;
        m_aba = aba;
#ifdef COUNT_CHUNKS
        m_count = 0;
#endif
    }

    static SatoriWorkList* AllocAligned()
    {
        const size_t align =
#if defined(TARGET_AMD64)
            64;
#else
            128;
#endif

#ifdef _MSC_VER
        void* buffer = _aligned_malloc(sizeof(SatoriWorkList), align);
#else
        void* buffer = malloc(sizeof(SatoriWorkList) + align);
        buffer = (void*)ALIGN_UP((size_t)buffer, align);
#endif
        return new(buffer)SatoriWorkList();
    }

    bool IsEmpty()
    {
        return m_head == nullptr;
    }

    FORCEINLINE
    void Push(SatoriWorkChunk* item)
    {
        _ASSERTE(item->m_next == nullptr);

        SatoriWorkChunk* head = this->m_head;
        size_t aba = this->m_aba;

        item->m_next = head;

        SatoriWorkList orig(head, aba);
        if (Cas128((int64_t*)this, aba + 1, (int64_t)item, (int64_t*)&orig))
        {
#ifdef COUNT_CHUNKS
            Interlocked::Increment(&m_count);
#endif
            return;
        }

        PushSlow(item);
    }

    FORCEINLINE
    SatoriWorkChunk* TryPop()
    {
        SatoriWorkChunk* head  = this->m_head;
        size_t aba = this->m_aba;

        if (head == nullptr)
        {
            return nullptr;
        }

        SatoriWorkList orig(head, aba);
        if (Cas128((int64_t*)this, aba + 1, (int64_t)head->m_next, (int64_t*)&orig))
        {
#ifdef COUNT_CHUNKS
            Interlocked::Decrement(&m_count);
#endif
            head->m_next = nullptr;
            return head;
        }

        return TryPopSlow();
    }

#ifdef COUNT_CHUNKS
    size_t Count()
    {
        return m_count;
    }
#endif

private:
    struct
    {
        SatoriWorkChunk* volatile m_head;
        volatile size_t m_aba;
    };
#ifdef COUNT_CHUNKS
    size_t m_count;
#endif

    NOINLINE
    void PushSlow(SatoriWorkChunk* item);

    NOINLINE
    SatoriWorkChunk* TryPopSlow();
};

#endif
