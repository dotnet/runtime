// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "ebr.h"

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

    // Intrusive linked list through the collector's thread list.
    EbrThreadData*     m_pNext;
};

// Singly-linked list node for pending deletions.
struct EbrPendingEntry
{
    void*            m_pObject;
    EbrDeleteFunc    m_pfnDelete;
    size_t           m_estimatedSize;
    EbrPendingEntry* m_pNext;
};

// Each thread holds its own EbrThreadData via thread_local.
// We support only a single collector per process; if multiple collectors
// are needed this can be extended to a per-collector TLS map, but for the
// HashMap use-case a single global collector suffices.
static thread_local EbrThreadData* t_pThreadData = nullptr;

// Global EBR collector for HashMap's async mode.
EbrCollector g_HashMapEbr;

// Count of objects leaked due to OOM when queuing for deferred deletion.
static LONG s_ebrLeakedDeletionCount = 0;

// ============================================
// EbrCollector implementation
// ============================================

void
EbrCollector::Init(CrstType crstThreadList, CrstType crstPending, size_t memoryBudgetInBytes)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(!m_initialized);

    m_memoryBudgetInBytes = memoryBudgetInBytes;
    m_globalEpoch = 0;
    m_pendingSizeInBytes = 0;
    m_pThreadListHead = nullptr;
    for (uint32_t i = 0; i < EBR_EPOCHS; i++)
        m_pPendingHeads[i] = nullptr;

    m_threadListLock.Init(crstThreadList);
    m_pendingLock.Init(crstPending);

    m_initialized = true;
}

void
EbrCollector::Shutdown()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (!m_initialized)
        return;

    // Drain all pending queues unconditionally.
    {
        CrstHolder lock(&m_pendingLock);
        for (uint32_t i = 0; i < EBR_EPOCHS; i++)
            DrainQueue(i);
    }

    // Free any remaining thread data entries. Threads should have exited
    // their critical regions by now. We walk the list and delete them.
    {
        CrstHolder lock(&m_threadListLock);
        EbrThreadData* pCur = m_pThreadListHead;
        while (pCur != nullptr)
        {
            EbrThreadData* pNext = pCur->m_pNext;
            delete pCur;
            pCur = pNext;
        }
        m_pThreadListHead = nullptr;
    }

    m_threadListLock.Destroy();
    m_pendingLock.Destroy();

    m_initialized = false;
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

    EbrThreadData* pData = t_pThreadData;
    if (pData != nullptr && pData->m_pCollector == this)
        return pData;

    // Allocate new per-thread data.
    pData = new (nothrow) EbrThreadData();
    if (pData == nullptr)
    {
        _ASSERTE(!"EBR: failed to allocate thread data");
        return nullptr;
    }

    pData->m_pCollector = this;
    pData->m_localEpoch = 0;
    pData->m_criticalDepth = 0;
    pData->m_pNext = nullptr;

    // Store in thread_local.
    t_pThreadData = pData;

    // Link into the collector's thread list.
    {
        CrstHolder lock(&m_threadListLock);
        pData->m_pNext = m_pThreadListHead;
        m_pThreadListHead = pData;
    }

    return pData;
}

void
EbrCollector::UnlinkThreadData(EbrThreadData* pData)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (pData == nullptr)
        return;

    CrstHolder lock(&m_threadListLock);
    EbrThreadData** pp = &m_pThreadListHead;
    while (*pp != nullptr)
    {
        if (*pp == pData)
        {
            *pp = pData->m_pNext;
            break;
        }
        pp = &(*pp)->m_pNext;
    }
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
    if (pData == nullptr)
        return;

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

    EbrThreadData* pData = t_pThreadData;
    _ASSERTE(pData != nullptr && pData->m_pCollector == this);
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

    EbrThreadData* pData = t_pThreadData;
    if (pData == nullptr || pData->m_pCollector != this)
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

    EbrThreadData* pData = t_pThreadData;
    if (pData == nullptr || pData->m_pCollector != this)
        return;

    _ASSERTE(!InCriticalRegion());

    UnlinkThreadData(pData);
    t_pThreadData = nullptr;
    delete pData;
}

// ============================================
// Epoch advancement
// ============================================

bool
EbrCollector::CanAdvanceEpoch()
{
    LIMITED_METHOD_CONTRACT;

    // Caller must hold m_threadListLock.
    uint32_t currentEpoch = m_globalEpoch;

    EbrThreadData* pData = m_pThreadListHead;
    while (pData != nullptr)
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

    CrstHolder lock(&m_threadListLock);

    // Acquire fence to synchronize with the MemoryBarrier() calls in
    // EnterCriticalRegion / ExitCriticalRegion.
    MemoryBarrier();

    if (!CanAdvanceEpoch())
        return false;

    uint32_t newEpoch = ((uint32_t)m_globalEpoch + 1) % EBR_EPOCHS;
    m_globalEpoch = newEpoch;
    return true;
}

// ============================================
// Deferred deletion
// ============================================

size_t
EbrCollector::DrainQueue(uint32_t slot)
{
    LIMITED_METHOD_CONTRACT;

    size_t freedSize = 0;

    EbrPendingEntry* pEntry = m_pPendingHeads[slot];
    m_pPendingHeads[slot] = nullptr;

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

void
EbrCollector::TryReclaim()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (TryAdvanceEpoch())
    {
        CrstHolder lock(&m_pendingLock);

        // Objects retired 2 epochs ago are safe to delete. With 3 epochs
        // and clock arithmetic, the safe slot is (current + 1) % 3.
        uint32_t currentEpoch = m_globalEpoch;
        uint32_t safeSlot = (currentEpoch + 1) % EBR_EPOCHS;

        size_t freed = DrainQueue(safeSlot);
        if (freed > 0)
        {
            _ASSERTE((size_t)m_pendingSizeInBytes >= freed);
            m_pendingSizeInBytes = (size_t)m_pendingSizeInBytes - freed;
        }
    }
}

void
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
    EbrThreadData* pData = t_pThreadData;
    _ASSERTE(pData != nullptr && pData->m_pCollector == this && pData->m_criticalDepth > 0);

    // Allocate pending entry.
    EbrPendingEntry* pEntry = new (nothrow) EbrPendingEntry();
    if (pEntry == nullptr)
    {
        // If we can't allocate, we must not delete pObject immediately, because
        // EBR readers in async mode may still be traversing data structures that
        // reference it. As a degraded fallback, intentionally leak pObject rather
        // than risk a use-after-free. This path should be extremely rare.
        InterlockedIncrement(&s_ebrLeakedDeletionCount);
        return;
    }

    pEntry->m_pObject = pObject;
    pEntry->m_pfnDelete = pfnDelete;
    pEntry->m_estimatedSize = estimatedSize;
    pEntry->m_pNext = nullptr;

    // Insert into the current epoch's pending list.
    {
        CrstHolder lock(&m_pendingLock);

        uint32_t slot = m_globalEpoch;
        pEntry->m_pNext = m_pPendingHeads[slot];
        m_pPendingHeads[slot] = pEntry;
        m_pendingSizeInBytes = (size_t)m_pendingSizeInBytes + estimatedSize;
    }

    // Try reclamation if over budget.
    if ((size_t)m_pendingSizeInBytes > m_memoryBudgetInBytes)
        TryReclaim();
}
