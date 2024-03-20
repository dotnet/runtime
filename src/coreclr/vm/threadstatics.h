// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Thread local storage is designed to be as efficient as possible.
// This leads to several different access patterns.
//
// There shall be a global TLS data structure used for all threads. This is initialized before any managed code is permitted to run on a thread
// struct TLSArray
// {
//     int32_t cTLSData; // Size in bytes of offset into the TLS array which is valid
//     void* pTLSArrayData; // Points at the Thread local array data.
// };
//
// Used to store access to TLS data for a single index when the TLS is accessed while the class constructor is running
// struct InFlightTLSData
// {
//     InFlightTLSData* pNext; // Points at the next in-flight TLS data
//     TLSIndex tlsIndex; // The TLS index for the static
//     void* pTLSData; // The TLS data for the static
// };
//
// struct ThreadLocalLoaderAllocator
// {
//     ThreadLocalLoaderAllocator* pNext; // Points at the next thread local loader allocator
//     LoaderAllocator* pLoaderAllocator; // The loader allocator that has a TLS used in this thread
//     bool ReportToGC(PromoteFunction* fn, ScanContext* sc, int flags); // Reports the thread local loader allocator state to the GC, returns true if the ThreadLocalLoaderAllocator structure should be removed from the linked list. This is what allows the GC statics for collectible types to actually exist on the nonGC thread local storage array
// };
//
// struct ThreadLocalData
// {
//     TLSArray nongcArray; // Array for nonGC data, as well as collectible GC static. cTLSData is initialized to PRE_ALLOCATED_TLS_NONGC_SLOT_COUNT * sizeof(void*) - 1, and pTLSArrayData points at memory of size PRE_ALLOCATED_TLS_NONGC_SLOT_COUNT * sizeof(void*) at thread startup
//     TLSArray gcArray; // Array for non-collectible GC pointers. cTLSData is initialized to PRE_ALLOCATED_TLS_GC_SLOT_COUNT * sizeof(OBJECTREF) + sizeof(void*) * 2 - 1, and pTLSArrayData points at a managed object[], initialized to an object array of size PRE_ALLOCATED_TLS_GC_SLOT_COUNT at thread startup
//     InFlightTLSData* pNext; // Points at the next in-flight TLS data
// };
//
// struct TLSIndex
// {
//     int32_t TLSIndexRawIndex;
//     int32_t GetByteIndex() { return TLSIndexRawIndex & 0xFFFFFF; }
//     int8_t GetTLSArrayOffset() { return TLSIndexRawIndex >> 24; }
// };
//
// thread_local ThreadLocalData t_ThreadStatics;
// SArray<MethodTable*>* g_pNonGCTLSIndexToMethodTable;
// int g_maxNonGCTlsSize;
// SArray<MethodTable*>* g_pGCTLSIndexToMethodTable;
// int g_maxGCTlsSlots;
//
// Access pattern for a TLS static
// 0. Get the TLS index somehow
// 1. Get TLS pointer to OS managed TLS block for the current thread ie. pThreadLocalData = &t_ThreadStatics
// 2. Get the TLSArray for the TLS index (pTLSArray = ((uint8_t*)pThreadLocalData) + index.GetTLSArrayOffset())
// 3. Read 1 integer value (cTLSData=pThreadLocalData->cTLSData)
// 4. Compare cTLSData against the index we're looking up (if (cTLSData < index.GetByteIndex()))
// 5. If the index is not within range, jump to step 10.
// 6. Read 1 pointer value from TLS block (pTLSArrayData=pThreadLocalData->pTLSArrayData)
// 7. Read 1 pointer from within the TLS Array. (pTLSBaseAddress = *(intptr_t*)(((uint8_t*)pTLSArrayData) + index.GetByteIndex());
// 8. If pointer is NULL jump to step 10 (if pTLSBaseAddress == NULL)
// 9. Return pTLSBaseAddress
// 10. Tail-call a helper (return GetThreadLocalStaticBase(index))
//
// The Runtime shall define a couple of well known TLS indices. These are used for the most common
// TLS statics, and are used to avoid the overhead of checking for the index being in range, and
// the class constructor for having run, so that we can skip steps 3, 4, 5, and 8. It shall do this
// by allocating the associated memory before permitting any code to run on the thread.
//
// Psuedocode for
// ref byte GetThreadLocalStaticBase(uint index)
// {
//     Do the access pattern above, but if the TLS array is too small, allocate a new one, and if the base pointer is NULL, call the class constructor for the static.
//     if After all that the base pointer is still NULL, walk the InFlightTLSData chain to see if it exists in there.
//     If the InFlightTLSData chain has a value
//         check to see if the class constructor has run. If it has completed, update the base pointer in the TLS array, and delete the InFlightTLSData entry.
//         return the found value
//     ELSE
//         allocate a new InFlightTLSData entry, and return the address of the pTLSData field.
// }
//
// Rationale for basic decisions here
// 1. We want access to TLS statics to be as fast as possible, especially for extremely common
//    thread statics like the ones used for async, and memory allocation.
// 2. We want access to TLS statics for shared generic types to be nearly fully inlineable. This
//    is why the variation between collectible and non-collectible gc statics access is handled by
//    a single byte in the index itself. The intent is that access to statics shall be as simple as
//    reading the index from a MethodTable, and then using a very straightforward pattern from there.


#ifndef __THREADLOCALSTORAGE_H__
#define __THREADLOCALSTORAGE_H__

class Thread;

struct TLSIndex
{
    TLSIndex() : TLSIndexRawIndex(0xFFFFFFFF) { }
    TLSIndex(uint32_t rawIndex) : TLSIndexRawIndex(rawIndex) { }
    uint32_t TLSIndexRawIndex;
    int32_t GetByteIndex() const { LIMITED_METHOD_DAC_CONTRACT; return TLSIndexRawIndex & 0xFFFFFF; }
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
