// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// P17 S6: the registry of finalizable objects, and the small lock-free list that feeds it.
//
// The registry replaces the per-object finalizer flags and the heap walks of the earlier design. It is a list of
// objects, not a table that is indexed by heap address. It keeps three states:
//   new       Appended by a mutator since the last pause. The reference count of the object does not include the
//             registry yet.
//   counted   The pause has given the object one reference count for the registry (the engine does it for the whole
//             batch). The object cannot die by reference counting while it is here.
//   ready     A tracing pause found the object unreachable. It waits for the finalizer thread. It keeps its count
//             until the finalizer thread takes it.
// Only a tracing pause (final mark or full) decides between "counted" and "ready". A reference-counting pause
// never does.
//
// Threads: Register and Release are lock-free and may run on any mutator. Every other member that changes the
// lists runs on the scan thread inside a pause, or on the finalizer thread in cooperative mode. The two never run at
// the same time because the finalizer thread cannot be in cooperative mode while the runtime is suspended.

#ifndef LXR_FINALIZER_REGISTRY_H
#define LXR_FINALIZER_REGISTRY_H

#include <atomic>
#include <algorithm>
#include <cstddef>
#include <cstdint>
#include <cstdlib>
#include <new>

class Object;

// Append-only list for many producers. A pause (or any single consumer, with no producer running) takes the whole
// content. A producer takes one slot with a single atomic add; a full chunk is replaced by a fresh one.
class LxrAppendList final
{
public:
    static constexpr size_t ChunkCapacity = 1022;

    LxrAppendList() : m_head(nullptr), m_chunks(0) {}
    ~LxrAppendList() { Clear(); }
    LxrAppendList(const LxrAppendList&) = delete;
    LxrAppendList& operator=(const LxrAppendList&) = delete;

    // A value of zero is reserved: it marks a slot that is not written yet.
    bool Append(uintptr_t value)
    {
        for (;;)
        {
            Chunk* chunk = m_head.load(std::memory_order_acquire);
            if (chunk != nullptr)
            {
#if defined(LXR_S6_UNSAFE_APPEND_CONTROL)
                size_t index = chunk->count.load(std::memory_order_relaxed);
                chunk->count.store(index + 1, std::memory_order_relaxed);
#else
                size_t index = chunk->count.fetch_add(1, std::memory_order_acq_rel);
#endif // LXR_S6_UNSAFE_APPEND_CONTROL
                if (index < ChunkCapacity)
                {
                    chunk->entries[index].store(value, std::memory_order_release);
                    return true;
                }
            }
            Chunk* fresh = new (std::nothrow) Chunk();
            if (fresh == nullptr)
            {
                return false;
            }
            fresh->next = chunk;
            if (m_head.compare_exchange_strong(chunk, fresh, std::memory_order_acq_rel))
            {
                m_chunks.fetch_add(1, std::memory_order_relaxed);
            }
            else
            {
                delete fresh;
            }
        }
    }

    // Takes every value. The callback gets each value once. No producer may run at the same time.
    template <typename Visit>
    size_t Drain(Visit&& visit)
    {
        size_t total = 0;
        Chunk* chunk = m_head.exchange(nullptr, std::memory_order_acq_rel);
        m_chunks.store(0, std::memory_order_relaxed);
        while (chunk != nullptr)
        {
            size_t used = chunk->count.load(std::memory_order_acquire);
            if (used > ChunkCapacity)
            {
                used = ChunkCapacity;
            }
            for (size_t index = 0; index < used; index++)
            {
                uintptr_t value = chunk->entries[index].load(std::memory_order_acquire);
                if (value != 0)
                {
#if !defined(LXR_S6_TOUCHED_DROP_CONTROL)
                    visit(value);
#endif // !LXR_S6_TOUCHED_DROP_CONTROL
                    total++;
                }
            }
            Chunk* next = chunk->next;
            delete chunk;
            chunk = next;
        }
        return total;
    }

    size_t Clear() { return Drain([](uintptr_t) {}); }
    bool IsEmpty() const { return m_head.load(std::memory_order_acquire) == nullptr; }
    size_t GetChunkCount() const { return m_chunks.load(std::memory_order_relaxed); }

private:
    struct Chunk
    {
        Chunk() : next(nullptr), count(0)
        {
            for (std::atomic<uintptr_t>& entry : entries)
            {
                entry.store(0, std::memory_order_relaxed);
            }
        }
        Chunk* next;
        std::atomic<size_t> count;
        std::atomic<uintptr_t> entries[ChunkCapacity];
    };

    std::atomic<Chunk*> m_head;
    std::atomic<size_t> m_chunks;
};

// Growable array of words. Only one thread uses it at a time.
class LxrWordArray final
{
public:
    LxrWordArray() : m_data(nullptr), m_size(0), m_capacity(0) {}
    ~LxrWordArray() { free(m_data); }
    LxrWordArray(const LxrWordArray&) = delete;
    LxrWordArray& operator=(const LxrWordArray&) = delete;

    bool Push(uintptr_t value)
    {
        if (m_size == m_capacity)
        {
            size_t capacity = m_capacity == 0 ? 256 : m_capacity * 2;
            uintptr_t* data = static_cast<uintptr_t*>(realloc(m_data, capacity * sizeof(uintptr_t)));
            if (data == nullptr)
            {
                return false;
            }
            m_data = data;
            m_capacity = capacity;
        }
        m_data[m_size++] = value;
        return true;
    }

    uintptr_t& operator[](size_t index) { return m_data[index]; }
    uintptr_t* Data() { return m_data; }
    const uintptr_t* Data() const { return m_data; }
    uintptr_t operator[](size_t index) const { return m_data[index]; }
    size_t Size() const { return m_size; }
    void Truncate(size_t size) { m_size = size; }
    size_t Capacity() const { return m_capacity; }

    // Gives the memory back when the array is much larger than its content.
    void Trim()
    {
        if (m_capacity > 4096 && m_size < m_capacity / 4)
        {
            size_t capacity = m_size < 256 ? 256 : m_size * 2;
            uintptr_t* data = static_cast<uintptr_t*>(realloc(m_data, capacity * sizeof(uintptr_t)));
            if (data != nullptr)
            {
                m_data = data;
                m_capacity = capacity;
            }
        }
    }

private:
    uintptr_t* m_data;
    size_t m_size;
    size_t m_capacity;
};

class LxrFinalizerRegistry final
{
public:
    // A registry entry is an object address. The lowest bit tells that the finalizer is critical: these objects
    // are handed out after all other objects.
    static constexpr uintptr_t CriticalBit = 1;

    LxrFinalizerRegistry() : m_pending(0), m_batch_start(0), m_ready_traced{0, 0}, m_ready_head{0, 0},
        m_dup_suspect(false), m_decided(0), m_dropped(0), m_registered(0), m_taken(0), m_failures(0),
        m_duplicates(0) {}

    // A mutator registers an object. The object has no count of the registry until the next pause.
    // reregistration: the call comes from GC.ReRegisterForFinalize. The object may have an entry already (finalization
    // was suppressed and an earlier pause has not looked at the entry yet, or it is registered twice), so the pause
    // merges entries of one object. That keeps the registration idempotent, as the flag of the older design was.
    bool Register(Object* object, bool critical, bool reregistration = false)
    {
        if (reregistration)
        {
            m_dup_suspect.store(true, std::memory_order_release);
        }
        if (!m_new.Append(reinterpret_cast<uintptr_t>(object) | (critical ? CriticalBit : 0)))
        {
            return false;
        }
        m_registered.fetch_add(1, std::memory_order_relaxed);
        return true;
    }

    // The registry lets go of one count of the object. The pause that comes next takes the count away.
    bool Release(Object* object)
    {
        return m_released.Append(reinterpret_cast<uintptr_t>(object));
    }

    // Pause, root capture. Takes the entries that were registered since the last pause. An entry whose object has
    // finalization suppressed is dropped: it never got a count. Every other entry gets one count (retain) and joins
    // the registry. Returns false when memory is exhausted.
    template <typename Suppressed, typename Retain>
    bool CaptureNew(Suppressed&& suppressed, Retain&& retain)
    {
        return CaptureNew(suppressed, retain, [](Object*) { return false; });
    }

    // critical(object): whether the finalizer of the object is critical. Allocation registers an object before the
    // runtime has written its MethodTable, so the bit is read here, when every thread is stopped.
    template <typename Suppressed, typename Retain, typename Critical>
    bool CaptureNew(Suppressed&& suppressed, Retain&& retain, Critical&& critical)
    {
        m_batch_start = m_entries.Size();
        bool success = true;
        m_batch.Truncate(0);
        m_new.Drain([&](uintptr_t entry)
        {
            if (!m_batch.Push(entry))
            {
                success = false;
            }
        });
        uintptr_t* data = m_batch.Data();
        size_t total = m_batch.Size();
#if defined(LXR_S6_NO_DEDUPE_CONTROL)
        if (false)
#else
        if (m_dup_suspect.load(std::memory_order_acquire))
#endif // LXR_S6_NO_DEDUPE_CONTROL
        {
            // New entries have no count yet, so a duplicate is dropped without a release.
            std::sort(data, data + total, [](uintptr_t left, uintptr_t right)
            {
                return (left & ~CriticalBit) < (right & ~CriticalBit);
            });
            size_t unique = 0;
            for (size_t index = 0; index < total; index++)
            {
                if (unique != 0 && (data[unique - 1] & ~CriticalBit) == (data[index] & ~CriticalBit))
                {
                    data[unique - 1] |= data[index] & CriticalBit;
                    m_duplicates.fetch_add(1, std::memory_order_relaxed);
                    continue;
                }
                data[unique++] = data[index];
            }
            total = unique;
        }
        for (size_t index = 0; index < total; index++)
        {
            uintptr_t entry = data[index];
            Object* object = Strip(entry);
#if defined(LXR_S6_NO_SUPPRESS_CONTROL)
            if (false)
#else
            if (suppressed(object))
#endif // LXR_S6_NO_SUPPRESS_CONTROL
            {
                m_dropped.fetch_add(1, std::memory_order_relaxed);
                continue;
            }
            if (critical(object))
            {
                entry |= CriticalBit;
            }
            if (!m_entries.Push(entry))
            {
                success = false;
                continue;
            }
            retain(object);
        }
        m_batch.Truncate(0);
        m_batch.Trim();
        return success;
    }

    // Pause, root capture. Hands the counts that the finalizer thread and earlier decisions gave up to the engine.
    template <typename OnRelease>
    size_t DrainReleased(OnRelease&& release)
    {
        size_t total = m_held.Size();
        for (size_t index = 0; index < total; index++)
        {
            release(reinterpret_cast<Object*>(m_held[index]));
        }
        m_held.Truncate(0);
        m_held.Trim();
        return total + m_released.Drain([&](uintptr_t value) { release(reinterpret_cast<Object*>(value)); });
    }

    // Tracing pause, after marking. Looks at every counted entry:
    //   suppressed or eagerly finalized  -> dropped; when the object is unmarked, onDrop gets it so that the caller
    //                                       can keep it marked until the count is taken away
    //   unmarked                         -> moved to the ready list; the caller traces it afterwards (ForEachNewReady)
    //   marked                           -> stays
    // Every unmarked entry is found before any of them is traced, so that f-reachability cannot hide a candidate.
    template <typename Resolve, typename Suppressed, typename Marked, typename Eager, typename OnDrop>
    bool Decide(Resolve&& resolve, Suppressed&& suppressed, Marked&& marked, Eager&& eager, OnDrop&& onDrop)
    {
        size_t kept = 0;
        bool success = true;
#if defined(LXR_S6_NO_DEDUPE_CONTROL)
        if (m_dup_suspect.exchange(false, std::memory_order_acq_rel) && false)
#else
        if (m_dup_suspect.exchange(false, std::memory_order_acq_rel))
#endif // LXR_S6_NO_DEDUPE_CONTROL
        {
            // A re-registration may have given one object more than one counted entry. The order of the entries does
            // not matter, so they are sorted and merged; each extra entry lets go of its count.
            uintptr_t* entries = m_entries.Data();
            size_t total = m_entries.Size();
            std::sort(entries, entries + total, [](uintptr_t left, uintptr_t right)
            {
                return (left & ~CriticalBit) < (right & ~CriticalBit);
            });
            size_t unique = 0;
            for (size_t index = 0; index < total; index++)
            {
                if (unique != 0 && (entries[unique - 1] & ~CriticalBit) == (entries[index] & ~CriticalBit))
                {
                    entries[unique - 1] |= entries[index] & CriticalBit;
                    if (!Hold(Strip(entries[index])))
                    {
                        success = false;
                    }
                    m_duplicates.fetch_add(1, std::memory_order_relaxed);
                    continue;
                }
                entries[unique++] = entries[index];
            }
            m_entries.Truncate(unique);
        }
        for (size_t index = 0; index < m_entries.Size(); index++)
        {
            uintptr_t entry = m_entries[index];
            uintptr_t critical = entry & CriticalBit;
            Object* object = resolve(Strip(entry));
#if defined(LXR_S6_MARKED_READY_CONTROL)
            bool live = false;
#else
            bool live = marked(object);
#endif // LXR_S6_MARKED_READY_CONTROL
            if (suppressed(object) || (!live && eager(object)))
            {
                m_dropped.fetch_add(1, std::memory_order_relaxed);
                if (!Hold(object))
                {
                    success = false;
                }
                if (!live)
                {
                    onDrop(object);
                }
                continue;
            }
            if (live)
            {
                m_entries[kept++] = reinterpret_cast<uintptr_t>(object) | critical;
                continue;
            }
            m_candidates.Push(reinterpret_cast<uintptr_t>(object) | critical);
        }
        m_entries.Truncate(kept);
        m_entries.Trim();
        // An object can have more than one entry (finalization suppressed, then registered again before a pause looked
        // at it, or registered twice). Both entries reach the same decision, so the duplicates are found among the
        // candidates. One entry moves on; each other entry lets go of its count.
        uintptr_t* data = m_candidates.Data();
        size_t total = m_candidates.Size();
        std::sort(data, data + total, [](uintptr_t left, uintptr_t right)
        {
            return (left & ~CriticalBit) < (right & ~CriticalBit);
        });
        for (size_t index = 0; index < total;)
        {
            uintptr_t entry = data[index];
            size_t next = index + 1;
#if defined(LXR_S6_NO_DEDUPE_CONTROL)
            while (false)
#else
            while (next < total && (data[next] & ~CriticalBit) == (entry & ~CriticalBit))
#endif // LXR_S6_NO_DEDUPE_CONTROL
            {
                entry |= data[next] & CriticalBit;
                if (!Hold(Strip(entry)))
                {
                    success = false;
                }
                m_duplicates.fetch_add(1, std::memory_order_relaxed);
                next++;
            }
            index = next;
            size_t list = (entry & CriticalBit) != 0 ? 1 : 0;
            if (!m_ready[list].Push(entry))
            {
                success = false;
                m_entries.Push(entry);
                continue;
            }
            m_pending.fetch_add(1, std::memory_order_acq_rel);
            m_decided.fetch_add(1, std::memory_order_relaxed);
        }
        m_candidates.Truncate(0);
        m_candidates.Trim();
        return success;
    }

    // The objects that Decide moved to the ready lists and that nobody has traced yet.
    template <typename Visit>
    void ForEachNewReady(Visit&& visit)
    {
        for (size_t list = 0; list < 2; list++)
        {
            for (size_t index = m_ready_traced[list]; index < m_ready[list].Size(); index++)
            {
                visit(Strip(m_ready[list][index]));
            }
            m_ready_traced[list] = m_ready[list].Size();
        }
    }

    // Every entry that has its count and is not ready. For diagnostics and tests.
    template <typename Visit>
    void ForEachCounted(Visit&& visit) const
    {
        for (size_t index = 0; index < m_entries.Size(); index++)
        {
            visit(Strip(m_entries[index]), (m_entries[index] & CriticalBit) != 0);
        }
    }

    // Every object that waits for the finalizer thread. Roots for a tracing pause, and the diagnostic walks.
    template <typename Visit>
    void ForEachReady(Visit&& visit) const
    {
        for (size_t list = 0; list < 2; list++)
        {
            for (size_t index = m_ready_head[list]; index < m_ready[list].Size(); index++)
            {
                visit(Strip(m_ready[list][index]), list != 0);
            }
        }
    }

    // Replaces each address by the new address of a moved object. With all = false only the entries of the
    // current batch are looked at: they are the only ones that a nursery copy can have moved.
    template <typename Resolve>
    void Relocate(bool all, Resolve&& resolve)
    {
        for (size_t index = all ? 0 : m_batch_start; index < m_entries.Size(); index++)
        {
            uintptr_t entry = m_entries[index];
            m_entries[index] = reinterpret_cast<uintptr_t>(resolve(Strip(entry))) | (entry & CriticalBit);
        }
        if (all)
        {
            // The counts that this pause gave up are addresses too: a mature copy in this pause can have moved them.
#if !defined(LXR_S6_NO_RELOCATE_HELD_CONTROL)
            for (size_t index = 0; index < m_held.Size(); index++)
            {
                m_held[index] = reinterpret_cast<uintptr_t>(resolve(reinterpret_cast<Object*>(m_held[index])));
            }
#endif // !LXR_S6_NO_RELOCATE_HELD_CONTROL
            for (size_t list = 0; list < 2; list++)
            {
                for (size_t index = m_ready_head[list]; index < m_ready[list].Size(); index++)
                {
                    uintptr_t entry = m_ready[list][index];
                    m_ready[list][index] = reinterpret_cast<uintptr_t>(resolve(Strip(entry))) | (entry & CriticalBit);
                }
            }
        }
    }

    // Finalizer thread. Returns the next object whose finalizer must run, or null. Objects that are not critical
    // come first. An object whose finalization was suppressed after the decision is skipped. The registry lets
    // go of its count in both cases.
    template <typename Suppressed>
    Object* TakeNext(Suppressed&& suppressed)
    {
        for (size_t list = 0; list < 2; list++)
        {
            while (m_ready_head[list] < m_ready[list].Size())
            {
                Object* object = Strip(m_ready[list][m_ready_head[list]++]);
                m_pending.fetch_sub(1, std::memory_order_acq_rel);
                if (m_ready_head[list] == m_ready[list].Size())
                {
                    m_ready[list].Truncate(0);
                    m_ready_head[list] = 0;
                    m_ready_traced[list] = 0;
                }
                else if (m_ready_traced[list] < m_ready_head[list])
                {
                    m_ready_traced[list] = m_ready_head[list];
                }
#if !defined(LXR_S6_NO_RELEASE_CONTROL)
                if (!Release(object))
                {
                    m_failures.fetch_add(1, std::memory_order_relaxed);
                }
#endif // !LXR_S6_NO_RELEASE_CONTROL
                m_taken.fetch_add(1, std::memory_order_relaxed);
                if (!suppressed(object))
                {
                    return object;
                }
            }
        }
        return nullptr;
    }

    size_t GetPendingCount() const { return m_pending.load(std::memory_order_acquire); }
    size_t GetCountedEntries() const { return m_entries.Size(); }
    size_t GetCountedCapacity() const { return m_entries.Capacity(); }
    size_t GetBatchStart() const { return m_batch_start; }
    uint64_t GetRegistered() const { return m_registered.load(std::memory_order_relaxed); }
    uint64_t GetDecided() const { return m_decided.load(std::memory_order_relaxed); }
    uint64_t GetDropped() const { return m_dropped.load(std::memory_order_relaxed); }
    uint64_t GetTaken() const { return m_taken.load(std::memory_order_relaxed); }
    // Counts that the registry could not give back because memory was exhausted. A caller treats it as fatal.
    uint64_t GetFailures() const { return m_failures.load(std::memory_order_relaxed); }
    uint64_t GetDuplicates() const { return m_duplicates.load(std::memory_order_relaxed); }
    bool HasNewEntries() const { return !m_new.IsEmpty(); }
    bool HasReleases() const { return !m_released.IsEmpty(); }

private:
    static Object* Strip(uintptr_t entry) { return reinterpret_cast<Object*>(entry & ~CriticalBit); }

    // The count of an entry that a tracing pause drops or merges. It waits here, where relocation can reach it, and
    // the next root capture hands it to the engine. (The release list of the finalizer thread needs no relocation:
    // no pause runs between the take and the next root capture.)
    bool Hold(Object* object) { return m_held.Push(reinterpret_cast<uintptr_t>(object)); }

    LxrAppendList m_new;
    LxrAppendList m_released;
    LxrWordArray m_entries;
    LxrWordArray m_ready[2];
    LxrWordArray m_candidates;
    LxrWordArray m_batch;
    LxrWordArray m_held;
    std::atomic<size_t> m_pending;
    size_t m_batch_start;
    size_t m_ready_traced[2];
    size_t m_ready_head[2];
    std::atomic<bool> m_dup_suspect;
    std::atomic<uint64_t> m_decided;
    std::atomic<uint64_t> m_dropped;
    std::atomic<uint64_t> m_registered;
    std::atomic<uint64_t> m_taken;
    std::atomic<uint64_t> m_failures;
    std::atomic<uint64_t> m_duplicates;
};

#endif // LXR_FINALIZER_REGISTRY_H
