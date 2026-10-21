// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef LXR_VIRTUAL_ARRAY_H
#define LXR_VIRTUAL_ARRAY_H

#include <new>
#include <atomic>
#ifndef LXR_SIDE_METADATA_TEST_PLATFORM
#include "gcenv.inl"
#endif // LXR_SIDE_METADATA_TEST_PLATFORM

// Addresses never change. Admission is suspended; failed commits do not publish capacity.
template <typename T>
class LxrVirtualArray final
{
public:
    LxrVirtualArray() = default;
    LxrVirtualArray(const LxrVirtualArray&) = delete;
    LxrVirtualArray& operator=(const LxrVirtualArray&) = delete;
    ~LxrVirtualArray() { Shutdown(); }

    bool Reserve(size_t count)
    {
        size_t page = GCToOSInterface::GetPageSize();
        if (m_data != nullptr || count == 0 || count > (SIZE_MAX - page + 1) / sizeof(T))
        {
            return false;
        }
        m_bytes = (count * sizeof(T) + page - 1) & ~(page - 1);
        m_data = static_cast<T*>(GCToOSInterface::VirtualReserve(
            m_bytes, page, VirtualReserveFlags::None));
        m_count = count;
        return m_data != nullptr;
    }

    bool Commit(size_t first, size_t count, bool initialize = true)
    {
        if (m_data == nullptr || first > m_count || count > m_count - first)
        {
            return false;
        }
        if (count == 0)
        {
            return true;
        }
        size_t page = GCToOSInterface::GetPageSize();
        size_t begin = first * sizeof(T) & ~(page - 1);
        size_t end = ((first + count) * sizeof(T) + page - 1) & ~(page - 1);
        if (!GCToOSInterface::VirtualCommit(reinterpret_cast<uint8_t*>(m_data) + begin, end - begin))
        {
            return false;
        }
        AddCommittedRange(begin, end);
        if (initialize)
        {
            for (size_t index = first; index < first + count; index++)
            {
                new (m_data + index) T{};
            }
        }
        return true;
    }

    // Gives the pages of [first, first + count) back to the operating system and makes the elements of the range
    // zero-valued again. Whole pages that lie in a committed range are decommitted. An element on a page that stays
    // committed (the edge of the range shares it with a neighbour) is reset one by one. A range that was never
    // committed is skipped. Quiescent: no thread may use the elements. Fails without a change when the committed
    // pages would fall into more than two ranges. With tailIsLast the caller guarantees that no element after the
    // range is in use, so the page that holds the last element is released too.
    bool Decommit(size_t first, size_t count, bool tailIsLast = false)
    {
        if (m_data == nullptr || first > m_count || count > m_count - first)
        {
            return false;
        }
        if (count == 0)
        {
            return true;
        }
        size_t page = GCToOSInterface::GetPageSize();
        size_t begin = first * sizeof(T);
        size_t end = (first + count) * sizeof(T);
        size_t bound = tailIsLast ? (end + page - 1) & ~(page - 1) : end;
        size_t kept[4][2];
        size_t keptCount = 0;
        size_t released[2][2];
        size_t resets[4][2];
        size_t resetCount = 0;
        size_t releasedCount = 0;
        for (size_t index = 0; index < 2; index++)
        {
            size_t start = m_starts[index];
            size_t stop = m_ends[index];
            if (stop == 0)
            {
                continue;
            }
            size_t low = start > begin ? start : begin;
            size_t high = stop < bound ? stop : bound;
            size_t used = high < end ? high : end;
            size_t pageLow = (low + page - 1) & ~(page - 1);
            size_t pageHigh = high & ~(page - 1);
            if (high <= low)
            {
                kept[keptCount][0] = start;
                kept[keptCount++][1] = stop;
                continue;
            }
            if (pageHigh <= pageLow)
            {
                resets[resetCount][0] = low;
                resets[resetCount++][1] = used;
                kept[keptCount][0] = start;
                kept[keptCount++][1] = stop;
                continue;
            }
            resets[resetCount][0] = low;
            resets[resetCount++][1] = pageLow;
            resets[resetCount][0] = pageHigh;
            resets[resetCount++][1] = used;
            released[releasedCount][0] = pageLow;
            released[releasedCount++][1] = pageHigh;
            if (start < pageLow)
            {
                kept[keptCount][0] = start;
                kept[keptCount++][1] = pageLow;
            }
            if (pageHigh < stop)
            {
                kept[keptCount][0] = pageHigh;
                kept[keptCount++][1] = stop;
            }
        }
        if (keptCount > 2)
        {
            return false;
        }
        for (size_t index = 0; index < resetCount; index++)
        {
            ResetElements(resets[index][0], resets[index][1]);
        }
        for (size_t index = 0; index < releasedCount; index++)
        {
            if (!GCToOSInterface::VirtualDecommit(
                    reinterpret_cast<uint8_t*>(m_data) + released[index][0], released[index][1] - released[index][0]))
            {
                return false;
            }
        }
        for (size_t index = 0; index < 2; index++)
        {
            m_starts[index] = index < keptCount ? kept[index][0] : 0;
            m_ends[index] = index < keptCount ? kept[index][1] : 0;
        }
        m_committed_bytes.store(m_ends[0] - m_starts[0] + m_ends[1] - m_starts[1], std::memory_order_release);
        return true;
    }

    T* Get() const { return m_data; }
    size_t GetReservedBytes() const { return m_bytes; }
    bool DecommitAll()
    {
        for (size_t index = 0; index < 2; index++)
        {
            if (m_ends[index] == 0)
            {
                continue;
            }
            if (!GCToOSInterface::VirtualDecommit(reinterpret_cast<uint8_t*>(m_data) + m_starts[index],
                m_ends[index] - m_starts[index]))
            {
                return false;
            }
            m_starts[index] = m_ends[index] = 0;
            m_committed_bytes.store(
                m_ends[0] - m_starts[0] + m_ends[1] - m_starts[1], std::memory_order_release);
        }
        return true;
    }
    size_t GetCommittedBytes() const
    {
        return m_committed_bytes.load(std::memory_order_acquire);
    }
    size_t ProjectedCommittedBytes(size_t first, size_t count) const
    {
        size_t page = GCToOSInterface::GetPageSize();
        size_t begin = first * sizeof(T) & ~(page - 1);
        size_t end = ((first + count) * sizeof(T) + page - 1) & ~(page - 1);
        size_t added = end - begin;
        for (size_t index = 0; index < 2; index++)
        {
            size_t overlapBegin = begin > m_starts[index] ? begin : m_starts[index];
            size_t overlapEnd = end < m_ends[index] ? end : m_ends[index];
            if (overlapEnd > overlapBegin)
            {
                added -= overlapEnd - overlapBegin;
            }
        }
        return GetCommittedBytes() + added;
    }
    void Shutdown()
    {
        if (m_data != nullptr)
        {
            GCToOSInterface::VirtualRelease(m_data, m_bytes);
            m_data = nullptr;
        }
        m_bytes = 0;
        m_count = 0;
        m_starts[0] = m_starts[1] = m_ends[0] = m_ends[1] = 0;
        m_committed_bytes.store(0, std::memory_order_release);
    }

private:
    // Elements are page-aligned in the byte range, so the loop never straddles a page.
    void ResetElements(size_t beginByte, size_t endByte)
    {
        for (size_t index = beginByte / sizeof(T); index < endByte / sizeof(T); index++)
        {
            new (m_data + index) T{};
        }
    }

    void AddCommittedRange(size_t begin, size_t end)
    {
        for (size_t index = 0; index < 2; index++)
        {
            if (m_ends[index] != 0 && begin <= m_ends[index] && end >= m_starts[index])
            {
                m_starts[index] = begin < m_starts[index] ? begin : m_starts[index];
                m_ends[index] = end > m_ends[index] ? end : m_ends[index];
                size_t other = index ^ 1;
                if (m_ends[other] != 0 && m_starts[index] <= m_ends[other] &&
                    m_ends[index] >= m_starts[other])
                {
                    m_starts[index] = m_starts[index] < m_starts[other]
                        ? m_starts[index] : m_starts[other];
                    m_ends[index] = m_ends[index] > m_ends[other] ? m_ends[index] : m_ends[other];
                    m_starts[other] = m_ends[other] = 0;
                }
                m_committed_bytes.store(
                    m_ends[0] - m_starts[0] + m_ends[1] - m_starts[1], std::memory_order_release);
                return;
            }
        }
        for (size_t index = 0; index < 2; index++)
        {
            if (m_ends[index] == 0)
            {
                m_starts[index] = begin;
                m_ends[index] = end;
                m_committed_bytes.store(
                    m_ends[0] - m_starts[0] + m_ends[1] - m_starts[1], std::memory_order_release);
                return;
            }
        }
        _ASSERTE(!"LXR native planes admit at most two contiguous ranges.");
    }

    T* m_data = nullptr;
    size_t m_bytes = 0;
    size_t m_count = 0;
    size_t m_starts[2]{};
    size_t m_ends[2]{};
    std::atomic<size_t> m_committed_bytes{0};
};

#endif // LXR_VIRTUAL_ARRAY_H
