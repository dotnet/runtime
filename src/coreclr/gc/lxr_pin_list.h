// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// P17 S7: the pin list of one pause.
//
// A pause finds its pinned objects while it scans the roots (stack slots and handles that the runtime reports as
// pinned). The list holds the address of each such object. It replaces the per-object pin flag of the earlier
// design, which needed a table with one entry for every 8 bytes of heap.
//
// Life of the list:
//   1. Root capture. Workers call Add. Each worker uses its own shard, so no two workers write the same array.
//   2. Seal. One thread merges the shards into one sorted array without duplicates. This is the only moment
//      that needs all shards to be quiet.
//   3. The pause. Any thread calls Contains. The list does not change, so no lock is needed.
//   4. Clear. The end of the pause empties the list.
//
// The cost of every step follows the number of pinned roots, never the size of the heap.

#ifndef LXR_PIN_LIST_H
#define LXR_PIN_LIST_H

#include "lxr_finalizer_registry.h"

#include <algorithm>
#include <atomic>
#include <cstddef>
#include <cstdint>

class LxrPinList final
{
public:
    static constexpr size_t ShardCount = 16;

    LxrPinList() : m_sealed(false), m_added(0) {}
    LxrPinList(const LxrPinList&) = delete;
    LxrPinList& operator=(const LxrPinList&) = delete;

    // Adds one address. The shard is chosen by the caller (a worker number); callers with different shard numbers
    // may run at the same time. A shard is protected by its own flag, so two callers with the same number are safe
    // too, only slower. Returns false when memory is exhausted.
    bool Add(size_t shard, uintptr_t object)
    {
#if defined(LXR_S7_UNSAFE_ADD_CONTROL)
        // Control: every caller uses shard 0 and nobody takes the lock.
        Shard& target = m_shards[0];
        bool pushed = target.entries.Push(object);
#else
        Shard& target = m_shards[shard % ShardCount];
        while (target.lock.test_and_set(std::memory_order_acquire))
        {
        }
        bool pushed = target.entries.Push(object);
        target.lock.clear(std::memory_order_release);
#endif // LXR_S7_UNSAFE_ADD_CONTROL
        if (pushed)
        {
            m_added.fetch_add(1, std::memory_order_relaxed);
        }
        return pushed;
    }

    // Merges the shards into one sorted array without duplicates. Returns the number of different addresses. No
    // Add may run at the same time. A second call without new entries gives the same count.
    size_t Seal()
    {
        for (Shard& shard : m_shards)
        {
            for (size_t index = 0; index < shard.entries.Size(); index++)
            {
                if (!m_merged.Push(shard.entries[index]))
                {
                    m_failed = true;
                }
            }
            shard.entries.Truncate(0);
        }
        uintptr_t* data = m_merged.Data();
        size_t total = m_merged.Size();
        std::sort(data, data + total);
        size_t unique = 0;
        for (size_t index = 0; index < total; index++)
        {
#if defined(LXR_S7_NO_DEDUPE_CONTROL)
            data[unique++] = data[index];
#else
            if (unique == 0 || data[unique - 1] != data[index])
            {
                data[unique++] = data[index];
            }
#endif // LXR_S7_NO_DEDUPE_CONTROL
        }
        m_merged.Truncate(unique);
        m_sealed.store(true, std::memory_order_release);
        return unique;
    }

    // Whether the address is in the list. Valid after Seal and before Clear. Does not lock.
    bool Contains(uintptr_t object) const
    {
        const uintptr_t* data = m_merged.Data();
        return std::binary_search(data, data + m_merged.Size(), object);
    }

    bool IsSealed() const { return m_sealed.load(std::memory_order_acquire); }
    bool HasFailed() const { return m_failed; }
    // Number of different addresses (after Seal).
    size_t Size() const { return m_merged.Size(); }
    uintptr_t operator[](size_t index) const { return m_merged[index]; }
    // Number of Add calls since the last Clear, duplicates included.
    uint64_t GetAdded() const { return m_added.load(std::memory_order_relaxed); }
    bool IsEmpty() const
    {
        if (m_merged.Size() != 0)
        {
            return false;
        }
        for (const Shard& shard : m_shards)
        {
            if (shard.entries.Size() != 0)
            {
                return false;
            }
        }
        return true;
    }

    // Empties the list. No other call may run at the same time.
    void Clear()
    {
        for (Shard& shard : m_shards)
        {
            shard.entries.Truncate(0);
            shard.entries.Trim();
        }
#if !defined(LXR_S7_NO_CLEAR_CONTROL)
        m_merged.Truncate(0);
        m_merged.Trim();
#endif // !LXR_S7_NO_CLEAR_CONTROL
        m_sealed.store(false, std::memory_order_release);
        m_added.store(0, std::memory_order_relaxed);
        m_failed = false;
    }

private:
    struct alignas(64) Shard
    {
        std::atomic_flag lock = ATOMIC_FLAG_INIT;
        LxrWordArray entries;
    };

    Shard m_shards[ShardCount];
    LxrWordArray m_merged;
    std::atomic<bool> m_sealed;
    std::atomic<uint64_t> m_added;
    bool m_failed = false;
};

#endif // LXR_PIN_LIST_H
