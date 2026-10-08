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
// SatoriFinalizationQueue.h
//

#ifndef __SATORI_FINALIZATION_QUEUE_H__
#define __SATORI_FINALIZATION_QUEUE_H__

#include "common.h"
#include "../gc.h"

class SatoriObject;
class SatoriHeap;
class SatoriRegion;

class SatoriFinalizationQueue
{
public:
    void Initialize(SatoriHeap* heap);
    bool TryUpdateScanTicket(int currentScanTicket);
    bool TryScheduleForFinalization(SatoriObject* finalizable);

    bool TryReserveSpace(size_t count, size_t* index);
    void ScheduleForFinalizationAt(size_t index, SatoriObject* finalizable);

    SatoriObject* TryGetNextItem();
    bool HasItems();
    size_t Count();

    int OverflowedGen();
    void SetOverflow(int generation);
    void TryResizeIfOverflowing();
    void ResetOverflow(int generation);

    template <typename F>
    void ForEachObjectRef(F lambda)
    {
        for (size_t i = m_dequeue; i != m_enqueue; i++)
        {
            lambda(&m_data[i & m_sizeMask].value);
        }
    }

private:
    struct Entry
    {
        SatoriObject* value;
        size_t        version;
    };

    size_t m_enqueue;
    DECLSPEC_ALIGN(64)
    size_t m_dequeue;
    DECLSPEC_ALIGN(64)
    size_t m_sizeMask;
    int m_overflowedGen;
    int m_scanTicket;
    Entry* m_data;
    SatoriHeap* m_heap;
    Entry* m_newData;
};

#endif
