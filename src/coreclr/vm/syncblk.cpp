// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// SYNCBLK.CPP
//

//
// Definition of a SyncBlock and the SyncBlockCache which manages it
//


#include "common.h"

#include "vars.hpp"
#include "util.hpp"
#include "class.h"
#include "object.h"
#include "threads.h"
#include "excep.h"
#include "threads.h"
#include "syncblk.h"
#include "interoputil.h"
#include "encee.h"
#include "eventtrace.h"
#include "dllimportcallback.h"
#include "comcallablewrapper.h"
#include "eeconfig.h"
#include "corhost.h"
#include "comdelegate.h"
#include "finalizerthread.h"
#include "minipal/time.h"

#ifdef FEATURE_COMINTEROP
#include "runtimecallablewrapper.h"
#endif // FEATURE_COMINTEROP

// Allocate 4K worth. Typically enough
#define MAXSYNCBLOCK (0x1000-sizeof(void*))/sizeof(SyncBlock)
#define SYNC_TABLE_INITIAL_SIZE 250

//#define DUMP_SB

class  SyncBlockArray
{
  public:
    SyncBlockArray *m_Next;
    BYTE            m_Blocks[MAXSYNCBLOCK * sizeof (SyncBlock)];
};

SyncBlockCache g_SyncBlockCacheInstance;

SPTR_IMPL (SyncBlockCache, SyncBlockCache, s_pSyncBlockCache);

#ifndef DACCESS_COMPILE

InteropSyncBlockInfo::~InteropSyncBlockInfo()
{
    CONTRACTL
    {
        NOTHROW;
        DESTRUCTOR_CHECK;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    FreeUMEntryThunk();
}

void InteropSyncBlockInfo::FreeUMEntryThunk()
{
    CONTRACTL
    {
        NOTHROW;
        DESTRUCTOR_CHECK;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END

    UMEntryThunkData *pUMEntryThunk = m_pUMEntryThunk;
    if (pUMEntryThunk != NULL)
    {
        UMEntryThunkData::FreeUMEntryThunk(pUMEntryThunk);
        m_pUMEntryThunk = NULL;
    }
}

#ifdef FEATURE_COMINTEROP
// Returns either NULL or an RCW on which AcquireLock has been called.
RCW* InteropSyncBlockInfo::GetRCWAndIncrementUseCount()
{
    LIMITED_METHOD_CONTRACT;

    DWORD dwSwitchCount = 0;
    while (true)
    {
        RCW *pRCW = VolatileLoad(&m_pRCW);
        if ((size_t)pRCW <= 0x1)
        {
            // the RCW never existed or has been released
            return NULL;
        }

        if (((size_t)pRCW & 0x1) == 0x0)
        {
            // it looks like we have a chance, try to acquire the lock
            RCW *pLockedRCW = (RCW *)((size_t)pRCW | 0x1);
            if (InterlockedCompareExchangeT(&m_pRCW, pLockedRCW, pRCW) == pRCW)
            {
                // we have the lock on the m_pRCW field, now we can safely "use" the RCW
                pRCW->IncrementUseCount();

                // release the m_pRCW lock
                VolatileStore(&m_pRCW, pRCW);

                // and return the RCW
                return pRCW;
            }
        }

        // somebody else holds the lock, retry
        __SwitchToThread(0, ++dwSwitchCount);
    }
}

// Sets the m_pRCW field in a thread-safe manner, pRCW can be NULL.
void InteropSyncBlockInfo::SetRawRCW(RCW* pRCW)
{
    LIMITED_METHOD_CONTRACT;

    if (pRCW != NULL)
    {
        // we never set two different RCWs on a single object
        _ASSERTE(m_pRCW == NULL);
        m_pRCW = pRCW;
    }
    else
    {
        DWORD dwSwitchCount = 0;
        while (true)
        {
            RCW *pOldRCW = VolatileLoad(&m_pRCW);

            if ((size_t)pOldRCW <= 0x1)
            {
                // the RCW never existed or has been released
                VolatileStore(&m_pRCW, (RCW *)0x1);
                return;
            }

            if (((size_t)pOldRCW & 0x1) == 0x0)
            {
                // it looks like we have a chance, set the RCW to 0x1
                if (InterlockedCompareExchangeT(&m_pRCW, (RCW *)0x1, pOldRCW) == pOldRCW)
                {
                    // we made it
                    return;
                }
            }

            // somebody else holds the lock, retry
            __SwitchToThread(0, ++dwSwitchCount);
        }
    }
}
#endif // FEATURE_COMINTEROP

#endif // !DACCESS_COMPILE

PTR_SyncTableEntry SyncTableEntry::GetSyncTableEntry()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    return (PTR_SyncTableEntry)g_pSyncTable;
}

#ifndef DACCESS_COMPILE

SyncTableEntry*& SyncTableEntry::GetSyncTableEntryByRef()
{
    LIMITED_METHOD_CONTRACT;
    return g_pSyncTable;
}

/* static */
SyncBlockCache*& SyncBlockCache::GetSyncBlockCache()
{
    LIMITED_METHOD_CONTRACT;

    return s_pSyncBlockCache;
}

#endif //DACCESS_COMPILE

#ifndef DACCESS_COMPILE

// ***************************************************************************
//
//              Ephemeral Bitmap Helper
//
// ***************************************************************************

#define card_size 32

#define card_word_width 32

size_t CardIndex (size_t card)
{
    LIMITED_METHOD_CONTRACT;
    return card_size * card;
}

size_t CardOf (size_t idx)
{
    LIMITED_METHOD_CONTRACT;
    return idx / card_size;
}

size_t CardWord (size_t card)
{
    LIMITED_METHOD_CONTRACT;
    return card / card_word_width;
}
inline
unsigned CardBit (size_t card)
{
    LIMITED_METHOD_CONTRACT;
    return (unsigned)(card % card_word_width);
}

inline
void SyncBlockCache::SetCard (size_t card)
{
    WRAPPER_NO_CONTRACT;
    m_EphemeralBitmap [CardWord (card)] =
        (m_EphemeralBitmap [CardWord (card)] | (1 << CardBit (card)));
}

inline
void SyncBlockCache::ClearCard (size_t card)
{
    WRAPPER_NO_CONTRACT;
    m_EphemeralBitmap [CardWord (card)] =
        (m_EphemeralBitmap [CardWord (card)] & ~(1 << CardBit (card)));
}

inline
BOOL  SyncBlockCache::CardSetP (size_t card)
{
    WRAPPER_NO_CONTRACT;
    return ( m_EphemeralBitmap [ CardWord (card) ] & (1 << CardBit (card)));
}

inline
void SyncBlockCache::CardTableSetBit (size_t idx)
{
    WRAPPER_NO_CONTRACT;
    SetCard (CardOf (idx));
}


size_t BitMapSize (size_t cacheSize)
{
    LIMITED_METHOD_CONTRACT;

    return (cacheSize + card_size * card_word_width - 1)/ (card_size * card_word_width);
}

// ***************************************************************************
//
//              SyncBlockCache class implementation
//
// ***************************************************************************

void SyncBlockCache::Init()
{
    CONTRACTL
    {
        CONSTRUCTOR_CHECK;
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    m_pCleanupBlockList = NULL;
    m_FreeBlockList     = NULL;

    // NOTE: CRST_UNSAFE_ANYMODE prevents a GC mode switch when entering this crst.
    // If you remove this flag, we will switch to preemptive mode when entering
    // g_criticalSection, which means all functions that enter it will become
    // GC_TRIGGERS.  (This includes all uses of LockHolder around SyncBlockCache::GetSyncBlockCache().
    // So be sure to update the contracts if you remove this flag.
    m_CacheLock.Init(CrstSyncBlockCache, (CrstFlags) (CRST_UNSAFE_ANYMODE | CRST_DEBUGGER_THREAD));

    m_FreeCount = 0;
    m_ActiveCount = 0;
    m_SyncBlocks = 0;
    m_FreeSyncBlock = 0;
    m_FreeSyncTableIndex = 1;
    m_FreeSyncTableList = 0;
    m_SyncTableSize = SYNC_TABLE_INITIAL_SIZE;
    m_OldSyncTables = 0;
    m_bSyncBlockCleanupInProgress = FALSE;
    m_EphemeralBitmap = 0;
}

void SyncBlockCache::Destroy()
{
    CONTRACTL
    {
        DESTRUCTOR_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Clear the list the fast way.
    m_FreeBlockList = NULL;
    //<TODO>@todo we can clear this fast too I guess</TODO>
    m_pCleanupBlockList = NULL;

    m_CacheLock.Destroy();

    // destruct all arrays
    while (m_SyncBlocks)
    {
        SyncBlockArray *next = m_SyncBlocks->m_Next;
        delete m_SyncBlocks;
        m_SyncBlocks = next;
    }

    // Also, now is a good time to clean up all the old tables which we discarded
    // when we overflowed them.
    SyncTableEntry* arr;
    while ((arr = m_OldSyncTables) != 0)
    {
        m_OldSyncTables = (SyncTableEntry*)arr[0].m_Object.Load();
        delete arr;
    }
}


// When the GC determines that an object is dead the low bit of the
// m_Object field of SyncTableEntry is set, however it is not
// cleaned up because we cant do the COM interop cleanup at GC time.
// It is put on a cleanup list and at a later time (typically during
// finalization, this list is cleaned up.
//
void SyncBlockCache::CleanupSyncBlocks()
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    _ASSERTE(FinalizerThread::IsCurrentThreadFinalizer());

    // Set the flag indicating sync block cleanup is in progress.
    // IMPORTANT: This must be set before the sync block cleanup bit is reset on the thread.
    m_bSyncBlockCleanupInProgress = TRUE;

    struct Param
    {
        SyncBlockCache *pThis;
        SyncBlock* psb;
#ifdef FEATURE_COMINTEROP
        RCW* pRCW;
#endif
    } param;
    param.pThis = this;
    param.psb = NULL;
#ifdef FEATURE_COMINTEROP
    param.pRCW = NULL;
#endif

    EE_TRY_FOR_FINALLY(Param *, pParam, &param)
    {
        // reset the flag
        FinalizerThread::GetFinalizerThread()->ResetSyncBlockCleanup();

        // walk the cleanup list and cleanup 'em up
        while ((pParam->psb = pParam->pThis->GetNextCleanupSyncBlock()) != NULL)
        {
#ifdef FEATURE_COMINTEROP
            InteropSyncBlockInfo* pInteropInfo = pParam->psb->GetInteropInfoNoCreate();
            if (pInteropInfo)
            {
                pParam->pRCW = pInteropInfo->GetRawRCW();
                if (pParam->pRCW)
                {
                    // We should have initialized the cleanup list with the
                    // first RCW cache we created
                    _ASSERTE(g_pRCWCleanupList != NULL);

                    g_pRCWCleanupList->AddWrapper(pParam->pRCW);

                    pParam->pRCW = NULL;
                    pInteropInfo->SetRawRCW(NULL);
                }
            }
#endif // FEATURE_COMINTEROP

            // Delete the sync block.
            pParam->pThis->DeleteSyncBlock(pParam->psb);
            pParam->psb = NULL;

            // pulse GC mode to allow GC to perform its work
            if (FinalizerThread::GetFinalizerThread()->CatchAtSafePoint())
            {
                FinalizerThread::GetFinalizerThread()->PulseGCMode();
            }
        }

#ifdef FEATURE_COMINTEROP
        // Now clean up the rcw's sorted by context
        if (g_pRCWCleanupList != NULL)
            g_pRCWCleanupList->CleanupAllWrappers();
#endif // FEATURE_COMINTEROP
    }
    EE_FINALLY
    {
        // We are finished cleaning up the sync blocks.
        m_bSyncBlockCleanupInProgress = FALSE;

#ifdef FEATURE_COMINTEROP
        if (param.pRCW)
            param.pRCW->Cleanup();
#endif

        if (param.psb)
            DeleteSyncBlock(param.psb);
    } EE_END_FINALLY;
}

// create the sync block cache
/* static */
void SyncBlockCache::Start()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    DWORD* bm = new DWORD [BitMapSize(SYNC_TABLE_INITIAL_SIZE+1)];

    memset (bm, 0, BitMapSize (SYNC_TABLE_INITIAL_SIZE+1)*sizeof(DWORD));

    SyncTableEntry::GetSyncTableEntryByRef() = new SyncTableEntry[SYNC_TABLE_INITIAL_SIZE+1];
#ifdef _DEBUG
    for (int i=0; i<SYNC_TABLE_INITIAL_SIZE+1; i++) {
        SyncTableEntry::GetSyncTableEntry()[i].m_SyncBlock = NULL;
    }
#endif

    SyncTableEntry::GetSyncTableEntry()[0].m_SyncBlock = 0;
    SyncBlockCache::GetSyncBlockCache() = &g_SyncBlockCacheInstance;
    g_SyncBlockCacheInstance.Init();

    SyncBlockCache::GetSyncBlockCache()->m_EphemeralBitmap = bm;
}


// destroy the sync block cache
/* static */
void SyncBlockCache::Stop()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // cache must be destroyed first, since it can traverse the table to find all the
    // sync blocks which are live and thus must have their critical sections destroyed.
    if (SyncBlockCache::GetSyncBlockCache())
    {
        SyncBlockCache::GetSyncBlockCache()->Destroy();
        SyncBlockCache::GetSyncBlockCache() = 0;
    }

    if (SyncTableEntry::GetSyncTableEntry())
    {
        delete SyncTableEntry::GetSyncTableEntry();
        SyncTableEntry::GetSyncTableEntryByRef() = 0;
    }
}


void    SyncBlockCache::InsertCleanupSyncBlock(SyncBlock* psb)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

#if defined(FEATURE_COMINTEROP)
    if (psb->m_pInteropInfo)
    {
        // called during GC
        // so do only minorcleanup
        MinorCleanupSyncBlockComData(psb->m_pInteropInfo);
    }
#endif // FEATURE_COMINTEROP || FEATURE_COMWRAPPERS

    // This method will be called only by the GC thread
    //<TODO>@todo add an assert for the above statement</TODO>
    // we don't need to lock here
    //EnterCacheLock();

    psb->m_Link.m_pNext = m_pCleanupBlockList;
    m_pCleanupBlockList = &psb->m_Link;

    // we don't need a lock here
    //LeaveCacheLock();
}

SyncBlock* SyncBlockCache::GetNextCleanupSyncBlock()
{
    LIMITED_METHOD_CONTRACT;

    // we don't need a lock here,
    // as this is called only on the finalizer thread currently

    SyncBlock       *psb = NULL;
    if (m_pCleanupBlockList)
    {
        // get the actual sync block pointer
        psb = (SyncBlock *) (((BYTE *) m_pCleanupBlockList) - offsetof(SyncBlock, m_Link));
        m_pCleanupBlockList = m_pCleanupBlockList->m_pNext;
    }
    return psb;
}


// returns and removes the next free syncblock from the list
// the cache lock must be entered to call this
SyncBlock *SyncBlockCache::GetNextFreeSyncBlock()
{
    CONTRACTL
    {
        INJECT_FAULT(COMPlusThrowOM());
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

#ifdef _DEBUG  // Instrumentation for OOM fault injection testing
    delete new char;
#endif

    SyncBlock       *psb;
    SLink           *plst = m_FreeBlockList;

    m_ActiveCount++;

    if (plst)
    {
        m_FreeBlockList = m_FreeBlockList->m_pNext;

        // shouldn't be 0
        m_FreeCount--;

        // get the actual sync block pointer
        psb = (SyncBlock *) (((BYTE *) plst) - offsetof(SyncBlock, m_Link));

        return psb;
    }
    else
    {
        if ((m_SyncBlocks == NULL) || (m_FreeSyncBlock >= MAXSYNCBLOCK))
        {
#ifdef DUMP_SB
//            LogSpewAlways("Allocating new syncblock array\n");
//            DumpSyncBlockCache();
#endif
            SyncBlockArray* newsyncblocks = new(SyncBlockArray);
            if (!newsyncblocks)
                COMPlusThrowOM ();

            newsyncblocks->m_Next = m_SyncBlocks;
            m_SyncBlocks = newsyncblocks;
            m_FreeSyncBlock = 0;
        }
        return &(((SyncBlock*)m_SyncBlocks->m_Blocks)[m_FreeSyncBlock++]);
    }

}

void SyncBlockCache::Grow()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    STRESS_LOG0(LF_SYNC, LL_INFO10000, "SyncBlockCache::NewSyncBlockSlot growing SyncBlockCache \n");

    NewArrayHolder<SyncTableEntry> newSyncTable (NULL);
    NewArrayHolder<DWORD>          newBitMap    (NULL);
    DWORD *                        oldBitMap;

    // Compute the size of the new synctable. Normally, we double it - unless
    // doing so would create slots with indices too high to fit within the
    // mask. If so, we create a synctable up to the mask limit. If we're
    // already at the mask limit, then caller is out of luck.
    DWORD newSyncTableSize;
    if (m_SyncTableSize <= (MASK_SYNCBLOCKINDEX >> 1))
    {
        newSyncTableSize = m_SyncTableSize * 2;
    }
    else
    {
        newSyncTableSize = MASK_SYNCBLOCKINDEX;
    }

    if (!(newSyncTableSize > m_SyncTableSize)) // Make sure we actually found room to grow!
    {
        EX_THROW(EEMessageException, (kOutOfMemoryException, IDS_EE_OUT_OF_SYNCBLOCKS));
    }

    newSyncTable = new SyncTableEntry[newSyncTableSize];
    newBitMap = new DWORD[BitMapSize (newSyncTableSize)];


    {
        //! From here on, we assume that we will succeed and start doing global side-effects.
        //! Any operation that could fail must occur before this point.
        CANNOTTHROWCOMPLUSEXCEPTION();
        FAULT_FORBID();

        newSyncTable.SuppressRelease();
        newBitMap.SuppressRelease();


        // We chain old table because we can't delete
        // them before all the threads are stopped
        // (next GC)
        SyncTableEntry::GetSyncTableEntry() [0].m_Object = (Object *)m_OldSyncTables;
        m_OldSyncTables = SyncTableEntry::GetSyncTableEntry();

        memset (newSyncTable, 0, newSyncTableSize*sizeof (SyncTableEntry));
        memset (newBitMap, 0, BitMapSize (newSyncTableSize)*sizeof (DWORD));
        CopyMemory (newSyncTable, SyncTableEntry::GetSyncTableEntry(),
                    m_SyncTableSize*sizeof (SyncTableEntry));

        CopyMemory (newBitMap, m_EphemeralBitmap,
                    BitMapSize (m_SyncTableSize)*sizeof (DWORD));

        oldBitMap = m_EphemeralBitmap;
        m_EphemeralBitmap = newBitMap;
        delete[] oldBitMap;

        _ASSERTE((m_SyncTableSize & MASK_SYNCBLOCKINDEX) == m_SyncTableSize);
        // note: we do not care if another thread does not see the new size
        // however we really do not want it to see the new size without seeing the new array
        //@TODO do we still leak here if two threads come here at the same time ?
        InterlockedExchangeT(&SyncTableEntry::GetSyncTableEntryByRef(), newSyncTable.GetValue());

        m_FreeSyncTableIndex++;

        m_SyncTableSize = newSyncTableSize;

#ifdef _DEBUG
        static int dumpSBOnResize = -1;

        if (dumpSBOnResize == -1)
            dumpSBOnResize = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_SBDumpOnResize);

        if (dumpSBOnResize)
        {
            LogSpewAlways("SyncBlockCache resized\n");
            DumpSyncBlockCache();
        }
#endif
    }
}

DWORD SyncBlockCache::NewSyncBlockSlot(Object *obj)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;
    _ASSERTE(m_CacheLock.OwnedByCurrentThread()); // GetSyncBlock takes the lock, make sure no one else does.

    DWORD indexNewEntry;
    if (m_FreeSyncTableList)
    {
        indexNewEntry = (DWORD)(m_FreeSyncTableList >> 1);
        _ASSERTE ((size_t)SyncTableEntry::GetSyncTableEntry()[indexNewEntry].m_Object.Load() & 1);
        m_FreeSyncTableList = (size_t)SyncTableEntry::GetSyncTableEntry()[indexNewEntry].m_Object.Load() & ~1;
    }
    else if ((indexNewEntry = (DWORD)(m_FreeSyncTableIndex)) >= m_SyncTableSize)
    {
        // This is kept out of line to keep stuff like the C++ EH prolog (needed for holders) off
        // of the common path.
        Grow();
    }
    else
    {
#ifdef _DEBUG
        static int dumpSBOnNewIndex = -1;

        if (dumpSBOnNewIndex == -1)
            dumpSBOnNewIndex = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_SBDumpOnNewIndex);

        if (dumpSBOnNewIndex)
        {
            LogSpewAlways("SyncBlockCache index incremented\n");
            DumpSyncBlockCache();
        }
#endif
        m_FreeSyncTableIndex ++;
    }


    CardTableSetBit (indexNewEntry);

    // In debug builds the m_SyncBlock at indexNewEntry should already be null, since we should
    // start out with a null table and always null it out on delete.
    _ASSERTE(SyncTableEntry::GetSyncTableEntry() [indexNewEntry].m_SyncBlock == NULL);
    SyncTableEntry::GetSyncTableEntry() [indexNewEntry].m_SyncBlock = NULL;
    SyncTableEntry::GetSyncTableEntry() [indexNewEntry].m_Object = obj;

    _ASSERTE(indexNewEntry != 0);

    return indexNewEntry;
}


// free a used sync block, only called from CleanupSyncBlocks.
void SyncBlockCache::DeleteSyncBlock(SyncBlock *psb)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    // clean up comdata
    if (psb->m_pInteropInfo)
    {
#if defined(FEATURE_COMINTEROP)
        CleanupSyncBlockComData(psb->m_pInteropInfo);
#endif // FEATURE_COMINTEROP

        delete psb->m_pInteropInfo;
    }

#ifdef FEATURE_METADATA_UPDATER
    // clean up EnC info
    if (psb->m_pEnCInfo)
        psb->m_pEnCInfo->Cleanup();
#endif // FEATURE_METADATA_UPDATER

    // Cleanup lock info
    psb->m_thinLock = 0;
    if (psb->m_Lock)
    {
        DestroyHandle(psb->m_Lock);
        psb->m_Lock = NULL;
    }

    // Destruct the SyncBlock, but don't reclaim its memory.  (Overridden
    // operator delete).
    delete psb;

    //synchronizer with the consumers,
    // <TODO>@todo we don't really need a lock here, we can come up
    // with some simple algo to avoid taking a lock </TODO>
    {
        SyncBlockCache::LockHolder lh(this);

        DeleteSyncBlockMemory(psb);
    }
}


// returns the sync block memory to the free pool but does not destruct sync block (must own cache lock already)
void    SyncBlockCache::DeleteSyncBlockMemory(SyncBlock *psb)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
    }
    CONTRACTL_END

    m_ActiveCount--;
    m_FreeCount++;

    psb->m_Link.m_pNext = m_FreeBlockList;
    m_FreeBlockList = &psb->m_Link;

}

// free a used sync block
void SyncBlockCache::GCDeleteSyncBlock(SyncBlock *psb)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Destruct the SyncBlock, but don't reclaim its memory.  (Overridden
    // operator delete).
    delete psb;

    m_ActiveCount--;
    m_FreeCount++;

    psb->m_Link.m_pNext = m_FreeBlockList;
    m_FreeBlockList = &psb->m_Link;
}

void SyncBlockCache::GCWeakPtrScan(HANDLESCANPROC scanProc, uintptr_t lp1, uintptr_t lp2)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;


    // First delete the obsolete arrays since we have exclusive access
    BOOL fSetSyncBlockCleanup = FALSE;

    SyncTableEntry* arr;
    while ((arr = m_OldSyncTables) != NULL)
    {
        m_OldSyncTables = (SyncTableEntry*)arr[0].m_Object.Load();
        delete[] arr;
    }

#ifdef DUMP_SB
    LogSpewAlways("GCWeakPtrScan starting\n");
#endif

#ifdef VERIFY_HEAP
   if (g_pConfig->GetHeapVerifyLevel()& EEConfig::HEAPVERIFY_SYNCBLK)
       STRESS_LOG0 (LF_GC | LF_SYNC, LL_INFO100, "GCWeakPtrScan starting\n");
#endif

   if (GCHeapUtilities::GetGCHeap()->GetCondemnedGeneration() < GCHeapUtilities::GetGCHeap()->GetMaxGeneration())
   {
#ifdef VERIFY_HEAP
        //for VSW 294550: we saw stale obeject reference in SyncBlkCache, so we want to make sure the card
        //table logic above works correctly so that every ephemeral entry is promoted.
        //For verification, we make a copy of the sync table in relocation phase and promote it use the
        //slow approach and compare the result with the original one
        DWORD freeSyncTableIndexCopy = m_FreeSyncTableIndex;
        SyncTableEntry * syncTableShadow = NULL;
        if ((g_pConfig->GetHeapVerifyLevel()& EEConfig::HEAPVERIFY_SYNCBLK) && !((ScanContext*)lp1)->promotion)
        {
            syncTableShadow = new(nothrow) SyncTableEntry [m_FreeSyncTableIndex];
            if (syncTableShadow)
            {
                memcpy ((void*)syncTableShadow, SyncTableEntry::GetSyncTableEntry(), m_FreeSyncTableIndex * sizeof (SyncTableEntry));
            }
        }
#endif //VERIFY_HEAP

        //scan the bitmap
        size_t dw = 0;
        while (1)
        {
            while (dw < BitMapSize (m_SyncTableSize) && (m_EphemeralBitmap[dw]==0))
            {
                dw++;
            }
            if (dw < BitMapSize (m_SyncTableSize))
            {
                //found one
                for (int i = 0; i < card_word_width; i++)
                {
                    size_t card = i+dw*card_word_width;
                    if (CardSetP (card))
                    {
                        BOOL clear_card = TRUE;
                        for (int idx = 0; idx < card_size; idx++)
                        {
                            size_t nb = CardIndex (card) + idx;
                            if (( nb < m_FreeSyncTableIndex) && (nb > 0))
                            {
                                Object* o = SyncTableEntry::GetSyncTableEntry()[nb].m_Object;
                                if (o && !((size_t)o & 1))
                                {
                                    if (GCHeapUtilities::GetGCHeap()->IsEphemeral (o))
                                    {
                                        clear_card = FALSE;

                                        GCWeakPtrScanElement ((int)nb, scanProc,
                                                              lp1, lp2, fSetSyncBlockCleanup);
                                    }
                                }
                            }
                        }
                        if (clear_card)
                            ClearCard (card);
                    }
                }
                dw++;
            }
            else
                break;
        }

#ifdef VERIFY_HEAP
        //for VSW 294550: we saw stale obeject reference in SyncBlkCache, so we want to make sure the card
        //table logic above works correctly so that every ephemeral entry is promoted. To verify, we make a
        //copy of the sync table and promote it use the slow approach and compare the result with the real one
        if (g_pConfig->GetHeapVerifyLevel()& EEConfig::HEAPVERIFY_SYNCBLK)
        {
            if (syncTableShadow)
            {
                for (DWORD nb = 1; nb < m_FreeSyncTableIndex; nb++)
                {
                    Object **keyv = (Object **) &syncTableShadow[nb].m_Object;

                    if (((size_t) *keyv & 1) == 0)
                    {
                        (*scanProc) (keyv, NULL, lp1, lp2);
                        SyncBlock   *pSB = syncTableShadow[nb].m_SyncBlock;
                        if (*keyv != 0 && (!pSB || !pSB->IsIDisposable()))
                        {
                            if (syncTableShadow[nb].m_Object != SyncTableEntry::GetSyncTableEntry()[nb].m_Object)
                                DebugBreak ();
                        }
                    }
                }
                delete []syncTableShadow;
                syncTableShadow = NULL;
            }
            if (freeSyncTableIndexCopy != m_FreeSyncTableIndex)
                DebugBreak ();
        }
#endif //VERIFY_HEAP

    }
    else
    {
        for (DWORD nb = 1; nb < m_FreeSyncTableIndex; nb++)
        {
            GCWeakPtrScanElement (nb, scanProc, lp1, lp2, fSetSyncBlockCleanup);
        }


    }

    if (fSetSyncBlockCleanup)
    {
        // mark the finalizer thread saying requires cleanup
        FinalizerThread::GetFinalizerThread()->SetSyncBlockCleanup();
        FinalizerThread::EnableFinalization();
    }

#if defined(VERIFY_HEAP)
    if (g_pConfig->GetHeapVerifyLevel() & EEConfig::HEAPVERIFY_GC)
    {
        if (((ScanContext*)lp1)->promotion)
        {

            for (int nb = 1; nb < (int)m_FreeSyncTableIndex; nb++)
            {
                Object* o = SyncTableEntry::GetSyncTableEntry()[nb].m_Object;
                if (o && ((size_t)o & 1) == 0)
                {
                    o->Validate();
                }
            }
        }
    }
#endif // VERIFY_HEAP
}

/* Scan the weak pointers in the SyncBlockEntry and report them to the GC.  If the
   reference is dead, then return TRUE */

BOOL SyncBlockCache::GCWeakPtrScanElement (int nb, HANDLESCANPROC scanProc, LPARAM lp1, LPARAM lp2,
                                           BOOL& cleanup)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    Object **keyv = (Object **) &SyncTableEntry::GetSyncTableEntry()[nb].m_Object;

#ifdef DUMP_SB
    struct Param
    {
        Object **keyv;
        char *name;
    } param;
    param.keyv = keyv;

    PAL_TRY(Param *, pParam, &param) {
        if (! *pParam->keyv)
            pParam->name = "null";
        else if ((size_t) *pParam->keyv & 1)
            pParam->name = "free";
        else {
            pParam->name = (*pParam->keyv)->GetClass()->GetDebugClassName();
            if (strlen(pParam->name) == 0)
                pParam->name = "<INVALID>";
        }
    } PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER) {
        param.name = "<INVALID>";
    }
    PAL_ENDTRY
    LogSpewAlways("[%4.4d]: %8.8x, %s\n", nb, *keyv, param.name);
#endif

    if (((size_t) *keyv & 1) == 0)
    {
#ifdef VERIFY_HEAP
        if (g_pConfig->GetHeapVerifyLevel () & EEConfig::HEAPVERIFY_SYNCBLK)
        {
            STRESS_LOG3 (LF_GC | LF_SYNC, LL_INFO100000, "scanning syncblk[%d, %p, %p]\n", nb, (size_t)SyncTableEntry::GetSyncTableEntry()[nb].m_SyncBlock, (size_t)*keyv);
        }
#endif

        (*scanProc) (keyv, NULL, lp1, lp2);
        SyncBlock   *pSB = SyncTableEntry::GetSyncTableEntry()[nb].m_SyncBlock;
        if ((*keyv == 0 ) || (pSB && pSB->IsIDisposable()))
        {
#ifdef VERIFY_HEAP
            if (g_pConfig->GetHeapVerifyLevel () & EEConfig::HEAPVERIFY_SYNCBLK)
            {
                STRESS_LOG3 (LF_GC | LF_SYNC, LL_INFO100000, "freeing syncblk[%d, %p, %p]\n", nb, (size_t)pSB, (size_t)*keyv);
            }
#endif

            if (*keyv)
            {
                _ASSERTE (pSB);
                GCDeleteSyncBlock(pSB);
                //clean the object syncblock header
                ((Object*)(*keyv))->GetHeader()->GCResetIndex();
            }
            else if (pSB)
            {

                cleanup = TRUE;
                // insert block into cleanup list
                InsertCleanupSyncBlock (SyncTableEntry::GetSyncTableEntry()[nb].m_SyncBlock);
#ifdef DUMP_SB
                LogSpewAlways("       Cleaning up block at %4.4d\n", nb);
#endif
            }

            // delete the entry
#ifdef DUMP_SB
            LogSpewAlways("       Deleting block at %4.4d\n", nb);
#endif
            SyncTableEntry::GetSyncTableEntry()[nb].m_Object = (Object *)(m_FreeSyncTableList | 1);
            m_FreeSyncTableList = nb << 1;
            SyncTableEntry::GetSyncTableEntry()[nb].m_SyncBlock = NULL;
            return TRUE;
        }
        else
        {
#ifdef DUMP_SB
            LogSpewAlways("       Keeping block at %4.4d with oref %8.8x\n", nb, *keyv);
#endif
        }
    }
    return FALSE;
}

void SyncBlockCache::GCDone(BOOL demoting, int max_gen)
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (demoting &&
        (GCHeapUtilities::GetGCHeap()->GetCondemnedGeneration() ==
         GCHeapUtilities::GetGCHeap()->GetMaxGeneration()))
    {
        //scan the bitmap
        size_t dw = 0;
        while (1)
        {
            while (dw < BitMapSize (m_SyncTableSize) &&
                   (m_EphemeralBitmap[dw]==(DWORD)~0))
            {
                dw++;
            }
            if (dw < BitMapSize (m_SyncTableSize))
            {
                //found one
                for (int i = 0; i < card_word_width; i++)
                {
                    size_t card = i+dw*card_word_width;
                    if (!CardSetP (card))
                    {
                        for (int idx = 0; idx < card_size; idx++)
                        {
                            size_t nb = CardIndex (card) + idx;
                            if (( nb < m_FreeSyncTableIndex) && (nb > 0))
                            {
                                Object* o = SyncTableEntry::GetSyncTableEntry()[nb].m_Object;
                                if (o && !((size_t)o & 1))
                                {
                                    if (GCHeapUtilities::GetGCHeap()->WhichGeneration (o) < (unsigned int)max_gen)
                                    {
                                        SetCard (card);
                                        break;

                                    }
                                }
                            }
                        }
                    }
                }
                dw++;
            }
            else
                break;
        }
    }
}


#if defined (VERIFY_HEAP)

#ifndef _DEBUG
#ifdef _ASSERTE
#undef _ASSERTE
#endif
#define _ASSERTE(c) if (!(c)) DebugBreak()
#endif

void SyncBlockCache::VerifySyncTableEntry()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    for (DWORD nb = 1; nb < m_FreeSyncTableIndex; nb++)
    {
        Object* o = SyncTableEntry::GetSyncTableEntry()[nb].m_Object;
        // if the slot was just allocated, the object may still be null
        if (o && (((size_t)o & 1) == 0))
        {
            //there is no need to verify next object's header because this is called
            //from verify_heap, which will verify every object anyway
            o->Validate(TRUE, FALSE);

            //
            // This loop is just a heuristic to try to catch errors, but it is not 100%.
            // To prevent false positives, we weaken our assert below to exclude the case
            // where the index is still NULL, but we've reached the end of our loop.
            //
            static const DWORD max_iterations = 100;
            DWORD loop = 0;

            for (; loop < max_iterations; loop++)
            {
                // The syncblock index may be updating by another thread.
                if (o->GetHeader()->GetHeaderSyncBlockIndex() != 0)
                {
                    break;
                }
                __SwitchToThread(0, CALLER_LIMITS_SPINNING);
            }

            DWORD idx = o->GetHeader()->GetHeaderSyncBlockIndex();
            _ASSERTE(idx == nb || ((0 == idx) && (loop == max_iterations)));
            _ASSERTE(!GCHeapUtilities::GetGCHeap()->IsEphemeral(o) || CardSetP(CardOf(nb)));
        }
    }
}

#ifndef _DEBUG
#undef _ASSERTE
#define _ASSERTE(expr) ((void)0)
#endif   // _DEBUG

#endif // VERIFY_HEAP

#ifdef _DEBUG

void DumpSyncBlockCache()
{
    STATIC_CONTRACT_NOTHROW;

    SyncBlockCache *pCache = SyncBlockCache::GetSyncBlockCache();

    LogSpewAlways("Dumping SyncBlockCache size %u\n", pCache->m_FreeSyncTableIndex);

    static int dumpSBStyle = -1;
    if (dumpSBStyle == -1)
        dumpSBStyle = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_SBDumpStyle);
    if (dumpSBStyle == 0)
        return;

    DWORD objectCount = 0;
    DWORD slotCount = 0;

    for (DWORD nb = 1; nb < pCache->m_FreeSyncTableIndex; nb++)
    {
        char buffer[1024];
        LPCUTF8 descrip = "null";
        SyncTableEntry *pEntry = &SyncTableEntry::GetSyncTableEntry()[nb];
        Object *oref = (Object *) pEntry->m_Object;
        if (((size_t) oref & 1) != 0)
        {
            descrip = "free";
            oref = 0;
        }
        else
        {
            ++slotCount;
            if (oref)
            {
                ++objectCount;

                struct Param
                {
                    LPCUTF8 descrip;
                    Object *oref;
                } param;
                param.descrip = descrip;
                param.oref = oref;

                PAL_TRY(Param *, pParam, &param)
                {
                    pParam->descrip = pParam->oref->GetMethodTable()->GetDebugClassName();
                    if (strlen(pParam->descrip) == 0)
                        pParam->descrip = "<INVALID>";
                }
                PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER) {
                    param.descrip = "<INVALID>";
                }
                PAL_ENDTRY

                descrip = param.descrip;
            }
            sprintf_s(buffer, ARRAY_SIZE(buffer), "%s", descrip);
            descrip = buffer;
        }
        if (dumpSBStyle < 2)
            LogSpewAlways("[%4.4d]: %zx %s\n", nb, oref, descrip);
        else if (dumpSBStyle == 2)
            LogSpewAlways("[%4.4d]: %s\n", nb, descrip);
    }
    LogSpewAlways("Done dumping SyncBlockCache used slots: %d, objects: %d\n", slotCount, objectCount);
}
#endif

// ***************************************************************************
//
//              ObjHeader class implementation
//
// ***************************************************************************

#ifdef MP_LOCKS
DEBUG_NOINLINE void ObjHeader::EnterSpinLock()
{
    // NOTE: This function cannot have a dynamic contract.  If it does, the contract's
    // destructor will reset the CLR debug state to what it was before entering the
    // function, which will undo the BeginNoTriggerGC() call below.
    STATIC_CONTRACT_GC_NOTRIGGER;

#ifdef _DEBUG
    int i = 0;
#endif

    DWORD dwSwitchCount = 0;

    while (TRUE)
    {
#ifdef _DEBUG
#ifdef HOST_64BIT
        // Give 64bit more time because there isn't a remoting fast path now, and we've hit this assert
        // needlessly in CLRSTRESS.
        if (i++ > 30000)
#else
        if (i++ > 10000)
#endif // HOST_64BIT
            _ASSERTE(!"ObjHeader::EnterLock timed out");
#endif
        // get the value so that it doesn't get changed under us.
        LONG curValue = m_SyncBlockValue.LoadWithoutBarrier();

        // check if lock taken
        if (! (curValue & BIT_SBLK_SPIN_LOCK))
        {
            // try to take the lock
            LONG newValue = curValue | BIT_SBLK_SPIN_LOCK;
            LONG result = InterlockedCompareExchange((LONG*)&m_SyncBlockValue, newValue, curValue);
            if (result == curValue)
                break;
        }
        if  (g_SystemInfo.dwNumberOfProcessors > 1)
        {
            for (int spinCount = 0; spinCount < BIT_SBLK_SPIN_COUNT; spinCount++)
            {
                if  (! (m_SyncBlockValue & BIT_SBLK_SPIN_LOCK))
                    break;
                YieldProcessorNormalized(); // indicate to the processor that we are spinning
            }
            if  (m_SyncBlockValue & BIT_SBLK_SPIN_LOCK)
                __SwitchToThread(0, ++dwSwitchCount);
        }
        else
            __SwitchToThread(0, ++dwSwitchCount);
    }

    INCONTRACT(Thread* pThread = GetThreadNULLOk());
    INCONTRACT(if (pThread != NULL) pThread->BeginNoTriggerGC(__FILE__, __LINE__));
}
#else
DEBUG_NOINLINE void ObjHeader::EnterSpinLock()
{
    STATIC_CONTRACT_GC_NOTRIGGER;

#ifdef _DEBUG
    int i = 0;
#endif

    DWORD dwSwitchCount = 0;

    while (TRUE)
    {
#ifdef _DEBUG
        if (i++ > 10000)
            _ASSERTE(!"ObjHeader::EnterLock timed out");
#endif
        // get the value so that it doesn't get changed under us.
        LONG curValue = m_SyncBlockValue.LoadWithoutBarrier();

        // check if lock taken
        if (! (curValue & BIT_SBLK_SPIN_LOCK))
        {
            // try to take the lock
            LONG newValue = curValue | BIT_SBLK_SPIN_LOCK;
            LONG result = InterlockedCompareExchange((LONG*)&m_SyncBlockValue, newValue, curValue);
            if (result == curValue)
                break;
        }
        __SwitchToThread(0, ++dwSwitchCount);
    }

    INCONTRACT(Thread* pThread = GetThreadNULLOk());
    INCONTRACT(if (pThread != NULL) pThread->BeginNoTriggerGC(__FILE__, __LINE__));
}
#endif //MP_LOCKS

DEBUG_NOINLINE void ObjHeader::ReleaseSpinLock()
{
    LIMITED_METHOD_CONTRACT;

    INCONTRACT(Thread* pThread = GetThreadNULLOk());
    INCONTRACT(if (pThread != NULL) pThread->EndNoTriggerGC());

    InterlockedAnd((LONG*)&m_SyncBlockValue, ~BIT_SBLK_SPIN_LOCK);
}

#endif //!DACCESS_COMPILE

#ifndef DACCESS_COMPILE

DWORD ObjHeader::GetSyncBlockIndex()
{
    CONTRACTL
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
    }
    CONTRACTL_END;

    DWORD   indx;

    if ((indx = GetHeaderSyncBlockIndex()) == 0)
    {
        BOOL fMustCreateSyncBlock = FALSE;
        {
            //Need to get it from the cache
            SyncBlockCache::LockHolder lh(SyncBlockCache::GetSyncBlockCache());

            //Try one more time
            if (GetHeaderSyncBlockIndex() == 0)
            {
                ENTER_SPIN_LOCK(this);
                // Now the header will be stable - check whether hashcode, appdomain index or lock information is stored in it.
                DWORD bits = GetBits();
                if (((bits & (BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX | BIT_SBLK_IS_HASHCODE)) == (BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX | BIT_SBLK_IS_HASHCODE)) ||
                    ((bits & BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX) == 0))
                {
                    // Need a sync block to store this info
                    fMustCreateSyncBlock = TRUE;
                }
                else
                {
                    SetIndex(BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX | SyncBlockCache::GetSyncBlockCache()->NewSyncBlockSlot(GetBaseObject()));
                }
                LEAVE_SPIN_LOCK(this);
            }
            // SyncBlockCache::LockHolder goes out of scope here
        }

        if (fMustCreateSyncBlock)
            GetSyncBlock();

        if ((indx = GetHeaderSyncBlockIndex()) == 0)
            COMPlusThrowOM();
    }

    return indx;
}

#if defined (VERIFY_HEAP)

BOOL ObjHeader::Validate (BOOL bVerifySyncBlkIndex)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    DWORD bits = GetBits ();
    Object * obj = GetBaseObject ();
    BOOL bVerifyMore = g_pConfig->GetHeapVerifyLevel() & EEConfig::HEAPVERIFY_SYNCBLK;
    //the highest 2 bits have reloaded meaning
    //         BIT_UNUSED                          0x80000000
    //         BIT_SBLK_FINALIZER_RUN              0x40000000
    if (bits & BIT_SBLK_FINALIZER_RUN)
    {
        ASSERT_AND_CHECK (obj->GetGCSafeMethodTable ()->HasFinalizer ());
    }

    //BIT_SBLK_GC_RESERVE (0x20000000) is only set during GC. But for frozen object, we don't clean the bit
    if (bits & BIT_SBLK_GC_RESERVE)
    {
        if (!GCHeapUtilities::IsGCInProgress () && !GCHeapUtilities::GetGCHeap()->IsConcurrentGCInProgress ())
        {
#ifdef FEATURE_BASICFREEZE
            ASSERT_AND_CHECK (GCHeapUtilities::GetGCHeap()->IsInFrozenSegment(obj));
#else //FEATURE_BASICFREEZE
            _ASSERTE(!"Reserve bit not cleared");
            return FALSE;
#endif //FEATURE_BASICFREEZE
        }
    }

    //Don't know how to verify BIT_SBLK_SPIN_LOCK (0x10000000)

    //BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX (0x08000000)
    if (bits & BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX)
    {
        //if BIT_SBLK_IS_HASHCODE (0x04000000) is not set,
        //rest of the DWORD is SyncBlk Index
        if (!(bits & BIT_SBLK_IS_HASHCODE))
        {
            if (bVerifySyncBlkIndex  && GCHeapUtilities::GetGCHeap()->RuntimeStructuresValid ())
            {
                DWORD sbIndex = bits & MASK_SYNCBLOCKINDEX;
                ASSERT_AND_CHECK(SyncTableEntry::GetSyncTableEntry()[sbIndex].m_Object == obj);
            }
        }
        else
        {
            //  rest of the DWORD is a hash code and we don't have much to validate it
        }
    }
    else
    {
        //if BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX is clear, rest of DWORD is thin lock thread ID,
        //thin lock recursion level and appdomain index
        DWORD lockThreadId = bits & SBLK_MASK_LOCK_THREADID;
        DWORD recursionLevel = (bits & SBLK_MASK_LOCK_RECLEVEL) >> SBLK_RECLEVEL_SHIFT;
        //if thread ID is 0, recursionLeve got to be zero
        //but thread ID doesn't have to be valid because the lock could be orphanend
        ASSERT_AND_CHECK (lockThreadId != 0 || recursionLevel == 0 );
    }

    return TRUE;
}

#endif //VERIFY_HEAP

// This holder takes care of the SyncBlock memory cleanup if an OOM occurs inside a call to NewSyncBlockSlot.
//
// Warning: Assumes you already own the cache lock.
//          Assumes nothing allocated inside the SyncBlock (only releases the memory, does not destruct.)
//
// This holder really just meets GetSyncBlock()'s special requirements. It's not a general purpose holder.


// Do not inline this call. (fyuan)
// SyncBlockMemoryHolder is normally a check for empty pointer and return. Inlining VoidDeleteSyncBlockMemory adds expensive exception handling.
void VoidDeleteSyncBlockMemory(SyncBlock* psb)
{
    LIMITED_METHOD_CONTRACT;
    SyncBlockCache::GetSyncBlockCache()->DeleteSyncBlockMemory(psb);
}

typedef Wrapper<SyncBlock*, DoNothing<SyncBlock*>, VoidDeleteSyncBlockMemory, 0> SyncBlockMemoryHolder;


// get the sync block for an existing object
SyncBlock *ObjHeader::GetSyncBlock()
{
    CONTRACT(SyncBlock *)
    {
        INSTANCE_CHECK;
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
        INJECT_FAULT(COMPlusThrowOM(););
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    PTR_SyncBlock syncBlock = GetBaseObject()->PassiveGetSyncBlock();
    DWORD      indx = 0;
    BOOL indexHeld = FALSE;

    if (syncBlock)
    {
#ifdef _DEBUG
        // Has our backpointer been correctly updated through every GC?
        PTR_SyncTableEntry pEntries(SyncTableEntry::GetSyncTableEntry());
        _ASSERTE(pEntries[GetHeaderSyncBlockIndex()].m_Object == GetBaseObject());
#endif // _DEBUG
        RETURN syncBlock;
    }

    //Need to get it from the cache
    {
        SyncBlockCache::LockHolder lh(SyncBlockCache::GetSyncBlockCache());

        //Try one more time
        syncBlock = GetBaseObject()->PassiveGetSyncBlock();
        if (syncBlock)
            RETURN syncBlock;

        SyncBlockMemoryHolder syncBlockMemoryHolder(SyncBlockCache::GetSyncBlockCache()->GetNextFreeSyncBlock());
        syncBlock = syncBlockMemoryHolder;

        if ((indx = GetHeaderSyncBlockIndex()) == 0)
        {
            indx = SyncBlockCache::GetSyncBlockCache()->NewSyncBlockSlot(GetBaseObject());
        }
        else
        {
            //We already have an index, we need to hold the syncblock
            indexHeld = TRUE;
        }

        {
            //! NewSyncBlockSlot has side-effects that we don't have backout for - thus, that must be the last
            //! failable operation called.
            CANNOTTHROWCOMPLUSEXCEPTION();
            FAULT_FORBID();


            syncBlockMemoryHolder.SuppressRelease();

            new (syncBlock) SyncBlock(indx);

            {
                // after this point, nobody can update the index in the header
                ENTER_SPIN_LOCK(this);

                {
                    // If the thin lock in the header is in use, transfer the information to the syncblock
                    DWORD bits = GetBits();
                    if ((bits & BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX) == 0)
                    {
                        DWORD lockThreadId = bits & SBLK_MASK_LOCK_THREADID;
                        DWORD recursionLevel = (bits & SBLK_MASK_LOCK_RECLEVEL) >> SBLK_RECLEVEL_SHIFT;
                        if (lockThreadId != 0 || recursionLevel != 0)
                        {
                            // recursionLevel can't be non-zero if thread id is 0
                            _ASSERTE(lockThreadId != 0);
                            syncBlock->InitializeThinLock(recursionLevel, lockThreadId);
                        }
                    }
                    else if ((bits & BIT_SBLK_IS_HASHCODE) != 0)
                    {
                        DWORD hashCode = bits & MASK_HASHCODE;

                        syncBlock->SetHashCode(hashCode);
                    }
                }

                SyncTableEntry::GetSyncTableEntry() [indx].m_SyncBlock = syncBlock;

                // in order to avoid a race where some thread tries to get the AD index and we've already zapped it,
                // make sure the syncblock etc is all setup with the AD index prior to replacing the index
                // in the header
                if (GetHeaderSyncBlockIndex() == 0)
                {
                    // We have transferred the AppDomain into the syncblock above.
                    SetIndex(BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX | indx);
                }

                //If we had already an index, hold the syncblock
                //for the lifetime of the object.
                if (indexHeld)
                    syncBlock->SetPrecious();

                LEAVE_SPIN_LOCK(this);
            }
        }
        // SyncBlockCache::LockHolder goes out of scope here
    }

    RETURN syncBlock;
}

// ***************************************************************************
//
//              SyncBlock class implementation
//
// ***************************************************************************

bool SyncBlock::SetInteropInfo(InteropSyncBlockInfo* pInteropInfo)
{
    WRAPPER_NO_CONTRACT;
    SetPrecious();

    // We could be agile, but not have noticed yet.  We can't assert here
    //  that we live in any given domain, nor is this an appropriate place
    //  to re-parent the syncblock.
/*    _ASSERTE (m_dwAppDomainIndex.m_dwIndex == 0 ||
              m_dwAppDomainIndex == SystemDomain::System()->DefaultDomain()->GetIndex() ||
              m_dwAppDomainIndex == GetAppDomain()->GetIndex());
    m_dwAppDomainIndex = GetAppDomain()->GetIndex();
*/
    return (InterlockedCompareExchangeT(&m_pInteropInfo,
                                                pInteropInfo,
                                                NULL) == NULL);
}

#ifdef FEATURE_METADATA_UPDATER
// Store information about fields added to this object by EnC
// This must be called from a thread in the AppDomain of this object instance
void SyncBlock::SetEnCInfo(EnCSyncBlockInfo *pEnCInfo)
{
    WRAPPER_NO_CONTRACT;

    // We can't recreate the field contents, so this SyncBlock can never go away
    SetPrecious();

    // Store the field info (should only ever happen once)
    _ASSERTE( m_pEnCInfo == NULL );
    m_pEnCInfo = pEnCInfo;
}
#endif // FEATURE_METADATA_UPDATER

void SyncBlock::InitializeThinLock(DWORD recursionLevel, DWORD threadId)
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(m_Lock == (OBJECTHANDLE)NULL);
    _ASSERTE(m_thinLock == 0u);
    m_thinLock = (threadId & SBLK_MASK_LOCK_THREADID) | (recursionLevel << SBLK_RECLEVEL_SHIFT);
}

OBJECTHANDLE SyncBlock::GetOrCreateLock(OBJECTREF lockObj)
{
    CONTRACTL
    {
        GC_TRIGGERS;
        THROWS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    if (m_Lock != (OBJECTHANDLE)NULL)
    {
        return m_Lock;
    }

    SetPrecious();

    // We need to create a new lock
    DWORD thinLock = m_thinLock;
    OBJECTHANDLEHolder lockHandle = GetAppDomain()->CreateHandle(lockObj);

    if (thinLock != 0)
    {
        GCPROTECT_BEGIN(lockObj);

        // We have thin-lock info that needs to be transferred to the lock object.
        DWORD lockThreadId = thinLock & SBLK_MASK_LOCK_THREADID;
        DWORD recursionLevel = (thinLock & SBLK_MASK_LOCK_RECLEVEL) >> SBLK_RECLEVEL_SHIFT;
        _ASSERTE(lockThreadId != 0);
        PREPARE_NONVIRTUAL_CALLSITE(METHOD__LOCK__INITIALIZE_TO_LOCKED_WITH_NO_WAITERS);
        DECLARE_ARGHOLDER_ARRAY(args, 3);
        args[ARGNUM_0] = OBJECTREF_TO_ARGHOLDER(lockObj);
        args[ARGNUM_1] = DWORD_TO_ARGHOLDER(lockThreadId);
        args[ARGNUM_2] = DWORD_TO_ARGHOLDER(recursionLevel);
        CALL_MANAGED_METHOD_NORET(args);

        GCPROTECT_END();
    }

    OBJECTHANDLE existingHandle = InterlockedCompareExchangeT(&m_Lock, lockHandle.GetValue(), NULL);

    if (existingHandle != NULL)
    {
        return existingHandle;
    }

    // Our lock instance is in the sync block now.
    // Don't release it.
    lockHandle.SuppressRelease();
    // Also, clear the thin lock info.
    // It won't be used any more, but it will look out of date.
    m_thinLock = 0u;

    return lockHandle;
}
#endif // !DACCESS_COMPILE

BOOL SyncBlock::TryGetLockInfo(DWORD *pThreadId, DWORD *pRecursionLevel)
{
    WRAPPER_NO_CONTRACT;

    if (m_Lock != (OBJECTHANDLE)NULL)
    {
        GCX_COOP();
        // Extract info from the lock object
        OBJECTREF lockObj = ObjectFromHandle(m_Lock);
        GCPROTECT_BEGIN(lockObj);

        DWORD state = CoreLibBinder::GetField(FIELD__LOCK__STATE)->GetValue32(lockObj);
        *pThreadId = CoreLibBinder::GetField(FIELD__LOCK__OWNING_THREAD_ID)->GetValue32(lockObj);
        *pRecursionLevel = CoreLibBinder::GetField(FIELD__LOCK__RECURSION_COUNT)->GetValue32(lockObj);

        return state & 1;

        GCPROTECT_END();
    }
    else if (m_thinLock != 0u)
    {
        // Extract info from the thin lock
        DWORD threadId = m_thinLock & SBLK_MASK_LOCK_THREADID;
        *pThreadId = threadId;
        *pRecursionLevel = (m_thinLock & SBLK_MASK_LOCK_RECLEVEL) >> SBLK_RECLEVEL_SHIFT;

        return (threadId != 0);
    }
    else
    {
        // No lock info available
        *pThreadId = 0;
        *pRecursionLevel = 0;
        return FALSE;
    }
}

#if defined(HOST_64BIT) && defined(_DEBUG)
void ObjHeader::IllegalAlignPad()
{
    WRAPPER_NO_CONTRACT;
#ifdef LOGGING
    void** object = ((void**) this) + 1;
    STRESS_LOG1(LF_ASSERT, LL_ALWAYS, "\n\n******** Illegal ObjHeader m_alignpad not 0, m_alignpad value: %d\n", m_alignpad);
#endif
    _ASSERTE(m_alignpad == 0);
}
#endif // HOST_64BIT && _DEBUG

