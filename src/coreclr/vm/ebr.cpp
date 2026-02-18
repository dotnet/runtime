// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "ebr.h"
#include "finalizerthread.h"

// ============================================
// Per-thread EBR state
// ============================================

// Bit flag indicating the thread is in a critical region.
// Combined with the epoch value in m_localEpoch for a single atomic field.
static constexpr uint32_t ACTIVE_FLAG = 0x80000000U;

struct EbrThreadData
{
    EbrCollector*      m_pCollector;

    // Local epoch with ACTIVE_FLAG. When the thread is quiescent (outside a
    // critical region) ACTIVE_FLAG is cleared and the epoch bits are zero.
    Volatile<uint32_t> m_localEpoch;

    // Nesting depth for re-entrant critical regions.
    uint32_t           m_criticalDepth;

    // Logically deleted from the thread list.
    Volatile<BOOL>     m_detached;

    // Intrusive linked list through the collector's thread list.
    EbrThreadData*     m_pNext;
};

// Singly-linked list node for pending deletions.
struct EbrPendingEntry final
{
    EbrPendingEntry(void* pObject, EbrDeleteFunc pfnDelete, size_t estimatedSize)
        : m_pObject{ pObject }
        , m_pfnDelete{ pfnDelete }
        , m_estimatedSize{ estimatedSize }
        , m_pNext{ nullptr }
    {}

    void*            m_pObject;
    EbrDeleteFunc    m_pfnDelete;
    size_t           m_estimatedSize;
    EbrPendingEntry* m_pNext;
};

// Each thread holds its own EbrThreadData via thread_local.
// We support only a single collector per process; if multiple collectors
// are needed this can be extended to a per-collector TLS map, but for the
// HashMap use-case a single global collector suffices.
static thread_local EbrThreadData t_pThreadData = {};

// Global EBR collector for HashMap's async mode.
// If you want to add another usage for Ebr in the future, please consider
// the tradeoffs between creating multiple collectors or treating this as
// a single shared global collector.
EbrCollector g_HashMapEbr;

// ============================================
// EbrCollector implementation
// ============================================

void
EbrCollector::Init(CrstType crstThreadList, CrstType crstPending)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(!m_initialized);

    m_globalEpoch = 0;
    m_pendingSizeInBytes = 0;
    m_pThreadListHead = nullptr;
    for (uint32_t i = 0; i < EBR_EPOCHS; i++)
        m_pPendingHeads[i] = nullptr;

    m_threadListLock.Init(crstThreadList);
    m_pendingLock.Init(crstPending);

    m_initialized = true;
}

// Delete all entries in a detached pending list. Must be called outside m_pendingLock.
// Returns the total estimated size freed.
static size_t DeletePendingEntries(EbrPendingEntry* pEntry)
{
    LIMITED_METHOD_CONTRACT;

    size_t freedSize = 0;
    while (pEntry != nullptr)
    {
        EbrPendingEntry* pNext = pEntry->m_pNext;
        pEntry->m_pfnDelete(pEntry->m_pObject);
        freedSize += pEntry->m_estimatedSize;
        delete pEntry;
        pEntry = pNext;
    }
    return freedSize;
}

// ============================================
// Thread registration
// ============================================

EbrThreadData*
EbrCollector::GetOrCreateThreadData()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    EbrThreadData* pData = &t_pThreadData;
    if (pData->m_pCollector == this)
        return pData;

    pData->m_pCollector = this;
    pData->m_localEpoch = 0;
    pData->m_criticalDepth = 0;
    pData->m_detached = FALSE;
    pData->m_pNext = nullptr;

    // Link into the collector's thread list.
    // See TryAdvanceEpoch for the removal semantics of detached nodes.
    EbrThreadData* pHead;
    do
    {
        pHead = VolatileLoad(&m_pThreadListHead);
        pData->m_pNext = pHead;
    } while (InterlockedCompareExchangeT(&m_pThreadListHead, pData, pHead) != pHead);

    return pData;
}

void
EbrCollector::UnlinkThreadData(EbrThreadData* pData)
{
    LIMITED_METHOD_CONTRACT;

    if (pData == nullptr)
        return;

    // Logically detach: the epoch scanner skips detached nodes.
    pData->m_detached = TRUE;
}

// ============================================
// Critical region enter/exit
// ============================================

void
EbrCollector::EnterCriticalRegion()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(m_initialized);

    EbrThreadData* pData = GetOrCreateThreadData();
    _ASSERTE(pData != nullptr);

    pData->m_criticalDepth++;

    if (pData->m_criticalDepth == 1)
    {
        // Outermost entry: observe the global epoch and set ACTIVE_FLAG.
        uint32_t epoch = m_globalEpoch;
        pData->m_localEpoch = (epoch | ACTIVE_FLAG);

        // Full fence to ensure the epoch observation is visible before any
        // reads in the critical region. This pairs with the acquire fence
        // in TryAdvanceEpoch's scan.
        MemoryBarrier();
    }
}

void
EbrCollector::ExitCriticalRegion()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(m_initialized);

    EbrThreadData* pData = &t_pThreadData;
    _ASSERTE(pData->m_pCollector == this);
    _ASSERTE(pData->m_criticalDepth > 0);

    pData->m_criticalDepth--;

    if (pData->m_criticalDepth == 0)
    {
        // Outermost exit: ensure all stores in the critical path are visible
        // before clearing the active flag.
        MemoryBarrier();
        pData->m_localEpoch = 0;
    }
}

bool
EbrCollector::InCriticalRegion()
{
    LIMITED_METHOD_CONTRACT;

    EbrThreadData* pData = &t_pThreadData;
    if (pData->m_pCollector != this)
        return false;
    return pData->m_criticalDepth > 0;
}

void
EbrCollector::ThreadDetach()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    EbrThreadData* pData = &t_pThreadData;
    if (pData->m_pCollector != this)
        return;

    _ASSERTE(!InCriticalRegion());

    UnlinkThreadData(pData);
    t_pThreadData = {}; // Clear thread_local to prevent reuse after detach.
}

// ============================================
// Epoch advancement
// ============================================

bool
EbrCollector::CanAdvanceEpoch()
{
    LIMITED_METHOD_CONTRACT;

    uint32_t currentEpoch = m_globalEpoch;

    EbrThreadData* pData = VolatileLoad(&m_pThreadListHead);
    while (pData != nullptr)
    {
        if (!pData->m_detached)
        {
            uint32_t localEpoch = pData->m_localEpoch;
            bool active = (localEpoch & ACTIVE_FLAG) != 0;
            if (active)
            {
                // If an active thread has not yet observed the current epoch,
                // we cannot advance.
                if (localEpoch != (currentEpoch | ACTIVE_FLAG))
                    return false;
            }
        }

        pData = pData->m_pNext;
    }

    return true;
}

bool
EbrCollector::TryAdvanceEpoch()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;
    _ASSERTE(FinalizerThread::IsCurrentThreadFinalizer());

    // Serialize scan, epoch advance, and pruning under the lock. This prevents
    // two concurrent callers from double-advancing the epoch and ensures the
    // CanAdvanceEpoch result is still valid when we act on it.
    CrstHolder lock(&m_threadListLock);

    // Acquire fence to synchronize with the MemoryBarrier() calls in
    // EnterCriticalRegion / ExitCriticalRegion.
    MemoryBarrier();

    if (!CanAdvanceEpoch())
        return false;

    uint32_t newEpoch = ((uint32_t)m_globalEpoch + 1) % EBR_EPOCHS;
    m_globalEpoch = newEpoch;

    // Physically prune detached nodes. New nodes are only ever CAS-pushed at
    // the head, so unlinking interior nodes here is safe without interfering
    // with concurrent inserts.
    EbrThreadData** pp = &m_pThreadListHead;
    while (*pp != nullptr)
    {
        if ((*pp)->m_detached)
            *pp = (*pp)->m_pNext;
        else
            pp = &(*pp)->m_pNext;
    }

    return true;
}

// ============================================
// Deferred deletion
// ============================================

// Detach the pending list for a given slot.
// Returns the head of the detached list.
EbrPendingEntry*
EbrCollector::DetachQueue(uint32_t slot)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(m_pendingLock.OwnedByCurrentThread());

    EbrPendingEntry* pHead = m_pPendingHeads[slot];
    m_pPendingHeads[slot] = nullptr;
    return pHead;
}

void
EbrCollector::CleanUpPending()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;
    _ASSERTE(FinalizerThread::IsCurrentThreadFinalizer());

    if (TryAdvanceEpoch())
    {
        EbrPendingEntry* pDetached;
        {
            CrstHolder lock(&m_pendingLock);

            // Objects retired 2 epochs ago are safe to delete. With 3 epochs
            // and clock arithmetic, the safe slot is (current + 1) % 3.
            uint32_t currentEpoch = m_globalEpoch;
            uint32_t safeSlot = (currentEpoch + 1) % EBR_EPOCHS;

            pDetached = DetachQueue(safeSlot);
        }

        size_t freed = DeletePendingEntries(pDetached);
        if (freed > 0)
        {
            _ASSERTE((size_t)m_pendingSizeInBytes >= freed);
            m_pendingSizeInBytes = (size_t)m_pendingSizeInBytes - freed;
        }
    }
}

bool
EbrCollector::CleanUpRequested()
{
    LIMITED_METHOD_CONTRACT;
    return m_initialized && (size_t)m_pendingSizeInBytes > 0;
}

bool
EbrCollector::QueueForDeletion(void* pObject, EbrDeleteFunc pfnDelete, size_t estimatedSize)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(m_initialized);
    _ASSERTE(pObject != nullptr);
    _ASSERTE(pfnDelete != nullptr);

    // Must be in a critical region.
    EbrThreadData* pData = &t_pThreadData;
    _ASSERTE(pData->m_pCollector == this && pData->m_criticalDepth > 0);

    // Allocate pending entry.
    EbrPendingEntry* pEntry = new (nothrow) EbrPendingEntry(pObject, pfnDelete, estimatedSize);
    if (pEntry == nullptr)
    {
        // If we can't allocate, we must not delete pObject immediately, because
        // EBR readers in async mode may still be traversing data structures that
        // reference it.
        return false;
    }

    // Insert into the current epoch's pending list.
    {
        CrstHolder lock(&m_pendingLock);

        uint32_t slot = m_globalEpoch;
        pEntry->m_pNext = m_pPendingHeads[slot];
        m_pPendingHeads[slot] = pEntry;
        m_pendingSizeInBytes = (size_t)m_pendingSizeInBytes + estimatedSize;
    }
    return true;
}
