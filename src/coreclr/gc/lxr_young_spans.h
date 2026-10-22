// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// P22 (D2): the spans of memory that mutators filled since the last pause.
//
// The stock allocation helpers bump a pointer inside a buffer that the collector gave to a thread, and never tell the
// collector about the objects. The collector learns what a thread allocated at two points: when the buffer is handed
// out (bulk preparation of the whole range) and when the buffer is retired (the used part is known exactly). A span is
// the used part of one buffer: [start, end), where both addresses are object starts (the address of the object header,
// not of the MethodTable pointer) and the memory between them is a sequence of complete objects with no gaps.
//
// A thread cannot be suspended inside a bump-pointer helper, so every object in a span is complete when a pause
// retires the buffers. The spans are the only record of young objects: no per-object bit exists for them.

#ifndef LXR_YOUNG_SPANS_H
#define LXR_YOUNG_SPANS_H

#include <algorithm>
#include <cstddef>
#include <cstdint>
#include <cstdlib>
#include <new>

struct LxrSpan
{
    uintptr_t start;
    uintptr_t end;
};

// Growable list of spans. One thread appends. A pause merges the lists of all mutators and sorts the result by start
// address; after that, Find is a binary search that any number of threads may run at the same time.
class LxrSpanList final
{
public:
    LxrSpanList() : m_data(nullptr), m_size(0), m_capacity(0) {}
    ~LxrSpanList() { free(m_data); }
    LxrSpanList(const LxrSpanList&) = delete;
    LxrSpanList& operator=(const LxrSpanList&) = delete;

    bool Push(uintptr_t start, uintptr_t end)
    {
        if (m_size == m_capacity)
        {
            size_t capacity = m_capacity == 0 ? 64 : m_capacity * 2;
            LxrSpan* data = static_cast<LxrSpan*>(realloc(m_data, capacity * sizeof(LxrSpan)));
            if (data == nullptr)
            {
                return false;
            }
            m_data = data;
            m_capacity = capacity;
        }
        m_data[m_size++] = {start, end};
        return true;
    }

    // Moves every span of other to the end of this list. Returns false when memory is exhausted.
    bool Take(LxrSpanList& other)
    {
        for (size_t index = 0; index < other.m_size; index++)
        {
            if (!Push(other.m_data[index].start, other.m_data[index].end))
            {
                return false;
            }
        }
        other.m_size = 0;
        return true;
    }

    void Sort()
    {
        std::sort(m_data, m_data + m_size, [](const LxrSpan& left, const LxrSpan& right)
        {
            return left.start < right.start;
        });
    }

    // The span that contains the address (start <= address < end), or nullptr. The list must be sorted.
    const LxrSpan* Find(uintptr_t address) const
    {
        size_t low = 0;
        size_t high = m_size;
        while (low < high)
        {
            size_t middle = low + (high - low) / 2;
            if (m_data[middle].end <= address)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }
        return low < m_size && m_data[low].start <= address ? &m_data[low] : nullptr;
    }

    void Clear()
    {
        m_size = 0;
        if (m_capacity > 4096)
        {
            free(m_data);
            m_data = nullptr;
            m_capacity = 0;
        }
    }

    size_t Size() const { return m_size; }
    const LxrSpan& operator[](size_t index) const { return m_data[index]; }

private:
    LxrSpan* m_data;
    size_t m_size;
    size_t m_capacity;
};

#endif // LXR_YOUNG_SPANS_H
