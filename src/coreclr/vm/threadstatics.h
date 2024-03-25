// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Thread local storage is designed to be as efficient as possible.
// This leads to several different access patterns.
//
// Access pattern for a TLS static
// 0. Get the TLS index somehow
// 1. Get TLS pointer to OS managed TLS block for the current thread ie. pThreadStatics = &t_ThreadStatics
// 2. Determine the TLSIndexType of the TLS index. Currently the only TLS access type in use is TLSIndexType::Standard, but over the .NET 9 period we expect to add a couple more
//   If TLSIndexType == TLSIndexType::Standard
//   - Compare pThreadStatics->cTLSData against the index offset of the TLSIndex.
//   - If in range, multiply the index offset by the size of a a pointer and add it to pThreadStatics->pTLSArrayData, then dereference
//   - If not found
//     - Slow path look in the pThreadStatics->pInFlightData, if found there, return
//     - If not found there, trigger allocation behavior to grow TLS data, etc.
//
// Normal thread-local statics lifetime management
// -------------------------------------
//   - Each entry in the TLS table which is not collectible shall be reported to the GC during promotion and
//     relocation. There are no GCHandle or other structures which keep TLS data alive
//
// Collectible thread-local statics lifetime management
// -------------------------------------
// Lifetime management is substantially  complicated due the issue that it is possible for either a thread or a
// collectible type to be collected first. Thus the collection algorithm is as follows.
//   - The system shall maintain a global mapping of TLS indices to MethodTable structures
//   - When a native LoaderAllocator is being cleaned up, before the WeakTrackResurrection GCHandle that 
//     points at the the managed LoaderAllocator object is destroyed, the mapping from TLS indices to 
//     collectible LoaderAllocator structures shall be cleared of all relevant entries (and the current
//     GC index shall be stored in the TLS to MethodTable mapping)
//   - When a GC promotion or relocation scan occurs, for every TLS index which was freed to point at a GC
//     index the relevant entry in the TLS table shall be set to NULL in preparation for that entry in the
//     table being reused in the future. In addition, if the TLS index refers to a MethodTable which is in
//     a collectible assembly, and the associated LoaderAllocator has been freed, then set the relevant
//     entry to NULL.
//   - When allocating new entries from the TLS mapping table for new collectible thread local structures,
//     do not re-use an entry in the table until at least 2 GCs have occurred. This is to allow every
//     thread to have NULL'd out the relevant entry in its thread local table.
//   - When allocating new TLS entries for collectible TLS statics on a per-thread basis allocate a
//     LOADERHANDLE for each object allocated, and associate it with the TLS index on that thread.
//   - When cleaning up a thread, for each collectible thread static which is still allocated, we will have
//     a LOADERHANDLE. If the collectible type still has a live managed LoaderAllocator free the
//     LOADERHANDLE.
//   - In each relocation scan, report all live collectible entries to the GC.
// 
#ifndef __THREADLOCALSTORAGE_H__
#define __THREADLOCALSTORAGE_H__

class Thread;

enum class TLSIndexType
{
    Standard, // IndexOffset for this form of TLSIndex is scaled by sizeof(void*) and then added to ThreadLocalData::pTLSArrayData to get the final address
}

struct TLSIndex
{
    TLSIndex() : TLSIndexRawIndex(0xFFFFFFFF) { }
    TLSIndex(uint32_t rawIndex) : TLSIndexRawIndex(rawIndex) { }
    uint32_t TLSIndexRawIndex;
    int32_t GetIndexOffset() const { LIMITED_METHOD_DAC_CONTRACT; return TLSIndexRawIndex & 0xFFFFFF; }
    TLSIndexType GetIndexType() const { LIMITED_METHOD_DAC_CONTRACT; return (TLSIndexType)(TLSIndexRawIndex >> 24); }
    bool IsAllocated() const { LIMITED_METHOD_DAC_CONTRACT; return TLSIndexRawIndex != 0xFFFFFFFF;}
    static TLSIndex Unallocated() { LIMITED_METHOD_DAC_CONTRACT; return TLSIndex(0xFFFFFFFF); }
    bool operator == (TLSIndex index) const { LIMITED_METHOD_DAC_CONTRACT; return TLSIndexRawIndex == index.TLSIndexRawIndex; }
    bool operator != (TLSIndex index) const { LIMITED_METHOD_DAC_CONTRACT; return TLSIndexRawIndex != index.TLSIndexRawIndex; }
};

// Used to store access to TLS data for a single index when the TLS is accessed while the class constructor is running
struct InFlightTLSData;
typedef DPTR(InFlightTLSData) PTR_InFlightTLSData;

struct ThreadLocalData
{
    int32_t cTLSData; // Size in bytes of offset into the TLS array which is valid
    int32_t cLoaderHandles;
    TADDR pTLSArrayData; // Points at the Thread local array data.
    Thread *pThread;
    PTR_InFlightTLSData pInFlightData; // Points at the in-flight TLS data (TLS data that exists before the class constructor finishes running)
    PTR_LOADERHANDLE pLoaderHandles;
};

typedef DPTR(ThreadLocalData) PTR_ThreadLocalData;

#ifndef DACCESS_COMPILE
#ifdef _MSC_VER
extern __declspec(thread)  ThreadLocalData t_ThreadStatics;
#else
extern __thread ThreadLocalData t_ThreadStatics;
#endif // _MSC_VER
#endif // DACCESS_COMPILE

class TLSIndexToMethodTableMap
{
    PTR_TADDR pMap;
    uint32_t m_maxIndex;
    uint32_t m_collectibleEntries;

    TADDR IsGCFlag() const { return (TADDR)0x1; }
    TADDR IsCollectibleFlag() const { return (TADDR)0x2; }
    TADDR UnwrapValue(TADDR input) const { return input & ~3; }
public:
    TLSIndexToMethodTableMap() : pMap(dac_cast<PTR_TADDR>(dac_cast<TADDR>(0))), m_maxIndex(0), m_collectibleEntries(0) { }

    PTR_MethodTable Lookup(TLSIndex index, bool *isGCStatic, bool *isCollectible) const
    {
        LIMITED_METHOD_CONTRACT;
        *isGCStatic = false;
        *isCollectible = false;
        if (index.TLSIndexRawIndex < VolatileLoad(&m_maxIndex))
        {
            TADDR rawValue = VolatileLoadWithoutBarrier(&VolatileLoad(&pMap)[index.TLSIndexRawIndex]);
            if (IsClearedValue(rawValue))
            {
                return NULL;
            }
            *isGCStatic = (rawValue & IsGCFlag()) != 0;
            *isCollectible = (rawValue & IsCollectibleFlag()) != 0;
            return (PTR_MethodTable)UnwrapValue(rawValue);
        }
        return NULL;
    }

    PTR_MethodTable LookupTlsIndexKnownToBeAllocated(TLSIndex index) const
    {
        LIMITED_METHOD_CONTRACT;
        if (index.TLSIndexRawIndex < VolatileLoad(&m_maxIndex))
        {
            TADDR rawValue = VolatileLoadWithoutBarrier(&VolatileLoad(&pMap)[index.TLSIndexRawIndex]);
            return (PTR_MethodTable)UnwrapValue(rawValue);
        }
        return NULL;
    }


    struct entry
    {
        entry(TLSIndex tlsIndex) : pMT(dac_cast<PTR_MethodTable>(dac_cast<TADDR>(0))), IsCollectible(false), IsGCStatic(false), IsClearedValue(false), ClearedMarker(0), TlsIndex(tlsIndex) { }

        PTR_MethodTable pMT;
        bool IsCollectible;
        bool IsGCStatic;
        bool IsClearedValue;
        uint8_t ClearedMarker;
        TLSIndex TlsIndex;
    };

    entry Lookup(TLSIndex index) const
    {
        LIMITED_METHOD_CONTRACT;
        entry e(index);
        if (index.TLSIndexRawIndex < VolatileLoad(&m_maxIndex))
        {
            TADDR rawValue = VolatileLoadWithoutBarrier(&VolatileLoad(&pMap)[index.TLSIndexRawIndex]);
            if (!IsClearedValue(rawValue))
            {
                e.pMT = (PTR_MethodTable)UnwrapValue(rawValue);
                e.IsCollectible = (rawValue & IsCollectibleFlag()) != 0;
                e.IsGCStatic = (rawValue & IsGCFlag()) != 0;
            }
            else
            {
                e.IsClearedValue = true;
                e.ClearedMarker = GetClearedMarker(rawValue);
            }
        }
        else
        {
            e.TlsIndex = TLSIndex(m_maxIndex);
        }
        return e;
    }

    class iterator
    {
        friend class TLSIndexToMethodTableMap;
        const TLSIndexToMethodTableMap& m_pMap;
        entry m_entry;
        iterator(const TLSIndexToMethodTableMap& pMap, uint32_t currentIndex) : m_pMap(pMap), m_entry(pMap.Lookup(TLSIndex(currentIndex))) {}
        public:
        const entry&                           operator*() const { return m_entry; }
        const entry*                           operator->() const { return &m_entry; }

        bool operator==(const iterator& other) const { return (m_entry.TlsIndex == other.m_entry.TlsIndex); }
        bool operator!=(const iterator& other) const { return (m_entry.TlsIndex != other.m_entry.TlsIndex); }

        iterator& operator++()
        {
            m_entry = m_pMap.Lookup(TLSIndex(m_entry.TlsIndex.TLSIndexRawIndex + 1));
            return *this;
        }
        iterator operator++(int) { iterator tmp = *this; ++(*this); return tmp; }
    };

    iterator begin() const
    {
        iterator it(*this, 0);
        return it;
    }

    iterator end() const
    {
        return iterator(*this, m_maxIndex);
    }

    class CollectibleEntriesCollection
    {
        friend class TLSIndexToMethodTableMap;
        const TLSIndexToMethodTableMap& m_pMap;

        CollectibleEntriesCollection(const TLSIndexToMethodTableMap& pMap) : m_pMap(pMap) {}

    public:

        class iterator
        {
            friend class CollectibleEntriesCollection;
            TLSIndexToMethodTableMap::iterator m_current;
            iterator(const TLSIndexToMethodTableMap::iterator& current) : m_current(current) {}
        public:
            const entry&                           operator*() const { return *m_current; }
            const entry*                           operator->() const { return m_current.operator->(); }

            bool operator==(const iterator& other) const { return (m_current == other.m_current); }
            bool operator!=(const iterator& other) const { return (m_current != other.m_current); }

            iterator& operator++()
            {
                TLSIndex oldIndex = m_current->TlsIndex;
                while (++m_current, m_current->TlsIndex != oldIndex)
                {
                    if (m_current->IsCollectible)
                        break;
                    oldIndex = m_current->TlsIndex;
                }
                return *this;
            }
            iterator operator++(int) { iterator tmp = *this; ++(*this); return tmp; }
        };

        iterator begin() const
        {
            iterator it(m_pMap.begin());
            if (!(it->IsCollectible))
            {
                ++it;
            }
            return it;
        }

        iterator end() const
        {
            return iterator(m_pMap.end());
        }
    };

    bool HasCollectibleEntries() const
    {
        LIMITED_METHOD_CONTRACT;
        return VolatileLoadWithoutBarrier(&m_collectibleEntries) > 0;
    }

    CollectibleEntriesCollection CollectibleEntries() const
    {
        return CollectibleEntriesCollection(*this);
    }

    static bool IsClearedValue(TADDR value)
    {
        LIMITED_METHOD_CONTRACT;
        return (value & 0x3FF) == value && value != 0;
    }

    static uint8_t GetClearedMarker(TADDR value)
    {
        LIMITED_METHOD_CONTRACT;
        return (uint8_t)((value & 0x3FF) >> 2);
    }

#ifndef DACCESS_COMPILE
    void Set(TLSIndex index, PTR_MethodTable pMT, bool isGCStatic);
    bool FindClearedIndex(uint8_t whenClearedMarkerToAvoid, TLSIndex* pIndex);
    void Clear(TLSIndex index, uint8_t whenCleared);
#endif // !DACCESS_COMPILE

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
    {
        SUPPORTS_DAC;
        DAC_ENUM_DTHIS();
        if (pMap != NULL)
        {
            DacEnumMemoryRegion(dac_cast<TADDR>(pMap), m_maxIndex * sizeof(TADDR));
        }
    }
#endif
};

typedef DPTR(TLSIndexToMethodTableMap) PTR_TLSIndexToMethodTableMap;
GPTR_DECL(TLSIndexToMethodTableMap, g_pThreadStaticTypeIndices);

PTR_VOID GetThreadLocalStaticBaseNoCreate(ThreadLocalData *pThreadLocalData, TLSIndex index);
void ScanThreadStaticRoots(ThreadLocalData *pThreadLocalData, bool forGC, promote_func* fn, ScanContext* sc);

#ifndef DACCESS_COMPILE
PTR_MethodTable LookupMethodTableForThreadStaticKnownToBeAllocated(TLSIndex index);
void InitializeThreadStaticData();
void InitializeCurrentThreadsStaticData(Thread* pThread);
void FreeThreadStaticData(ThreadLocalData *pThreadLocalData);
void GetTLSIndexForThreadStatic(MethodTable* pMT, bool gcStatic, TLSIndex* pIndex);
void FreeTLSIndicesForLoaderAllocator(LoaderAllocator *pLoaderAllocator);
void* GetThreadLocalStaticBase(TLSIndex index);
void GetThreadLocalStaticBlocksInfo (CORINFO_THREAD_STATIC_BLOCKS_INFO* pInfo);
bool CanJITOptimizeTLSAccess();
void NotifyThreadStaticGCHappened();
#else
void EnumThreadMemoryRegions(ThreadLocalData* pThreadLocalData, CLRDataEnumMemoryFlags flags);
#endif

#endif // __THREADLOCALSTORAGE_H__
