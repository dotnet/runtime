// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ebr.h - Epoch-Based Reclamation for safe memory reclamation
//
// Implements the EBR algorithm from K. Fraser, "Practical Lock-Freedom"
// (UCAM-CL-TR-579). Provides safe, low-overhead deferred deletion for
// concurrent data structures without requiring GC suspension or COOP
// mode transitions.
//
// Usage:
//   // Startup:
//   g_EbrCollector.Init();
//
//   // Reader/Writer thread:
//   {
//       EbrCriticalRegionHolder ebr(&g_EbrCollector, fAsyncMode);
//       // ... access shared data safely ...
//       // Objects queued for deletion will not be freed while any thread
//       // is in a critical region that observed a prior epoch.
//   }
//
//   // Writer thread (inside critical region), after replacing shared pointer:
//   g_EbrCollector.QueueForDeletion(pOldData, deleteFn, sizeEstimate);
//
//   // Shutdown:
//   The EBR collector doesn't support a shutdown feature. CoreCLR doesn't support
//   clean shutdown.

#ifndef __EBR_H__
#define __EBR_H__

// Number of epoch slots: current, current-1, current-2
static constexpr uint32_t EBR_EPOCHS = 3;

// Callback to delete a retired object.
typedef void (*EbrDeleteFunc)(void* pObject);

// Forward declarations
struct EbrThreadData;
struct EbrPendingEntry;

// EBR Collector - manages epoch-based deferred reclamation.
//
// A single collector instance is typically shared across all threads that
// access a particular set of shared data structures.
class EbrCollector final
{
public:
    EbrCollector() = default;
    ~EbrCollector() = default;

    EbrCollector(const EbrCollector&) = delete;
    EbrCollector& operator=(const EbrCollector&) = delete;
    EbrCollector(EbrCollector&&) = delete;
    EbrCollector& operator=(EbrCollector&&) = delete;

    // Initialize the collector.
    void Init();

    // Enter a critical region. While in a critical region, objects queued for
    // deletion will not be freed. Re-entrant: nested calls are counted.
    void EnterCriticalRegion();

    // Exit a critical region. Must pair with EnterCriticalRegion.
    void ExitCriticalRegion();

    // Queue an object for deferred deletion. Must be called from within a
    // critical region. The object will be deleted via pfnDelete once all
    // threads have passed through a quiescent state.
    //   pObject:       the object to retire (must not be nullptr)
    //   pfnDelete:     function to call to delete the object
    //   estimatedSize: approximate size in bytes (for tracking)
    // Returns true if the object was successfully queued for deletion, false if
    // the queue allocation failed (in which case the object was not queued and will not be deleted).
    // Note: if queuing fails, the caller is responsible for ensuring the object is eventually deleted,
    // either by retrying the queue or by deleting it directly if safe to do so.
    bool QueueForDeletion(void* pObject, EbrDeleteFunc pfnDelete, size_t estimatedSize);

    // Returns true if the calling thread is currently in a critical region.
    bool InCriticalRegion();

    // Detach the calling thread from this collector. Marks per-thread EBR state
    // for deferred cleanup. Should be called during thread shutdown.
    void ThreadDetach();

    // Returns true if there are pending deletions that may be reclaimable.
    bool CleanUpRequested();

    // Attempt to advance the epoch and reclaim safe pending deletions.
    void CleanUpPending();

private:
    // Thread list management
    EbrThreadData* GetOrCreateThreadData();

    // Epoch management
    bool CanAdvanceEpoch();
    bool TryAdvanceEpoch();

    // Reclamation
    EbrPendingEntry* DetachQueue(uint32_t slot);

    // State
    bool             m_initialized = false;

    // Global epoch counter [0, EBR_EPOCHS-1]
    Volatile<uint32_t> m_globalEpoch;

    // Registered thread list (m_threadListLock used for pruning and epoch scanning)
    CrstStatic       m_threadListLock;
    EbrThreadData*   m_pThreadListHead = nullptr;

    // Pending deletion queues, one per epoch slot (protected by m_pendingLock)
    CrstStatic       m_pendingLock;
    EbrPendingEntry* m_pPendingHeads[EBR_EPOCHS] = {};
    Volatile<size_t> m_pendingSizeInBytes;
};

// Global EBR collector for safe deferred deletion of memory that may be
// concurrently accessed by reader threads (e.g. HashMap and EEHashTable
// bucket arrays during resize).
extern EbrCollector g_EbrCollector;

#ifndef DACCESS_COMPILE
// RAII holder for EBR critical regions, analogous to GCX_COOP pattern.
// When fEnable is false, the holder is a no-op.
class EbrCriticalRegionHolder final
{
public:
    EbrCriticalRegionHolder(EbrCollector* pCollector, bool fEnable)
        : m_pCollector(fEnable ? pCollector : nullptr)
    {
        WRAPPER_NO_CONTRACT;
        if (m_pCollector != nullptr)
            m_pCollector->EnterCriticalRegion();
    }

    ~EbrCriticalRegionHolder()
    {
        WRAPPER_NO_CONTRACT;
        if (m_pCollector != nullptr)
            m_pCollector->ExitCriticalRegion();
    }

    EbrCriticalRegionHolder(const EbrCriticalRegionHolder&) = delete;
    EbrCriticalRegionHolder& operator=(const EbrCriticalRegionHolder&) = delete;
    EbrCriticalRegionHolder(EbrCriticalRegionHolder&&) = delete;
    EbrCriticalRegionHolder& operator=(EbrCriticalRegionHolder&&) = delete;

private:
    EbrCollector* m_pCollector;
};
#endif // !DACCESS_COMPILE

#endif // __EBR_H__
