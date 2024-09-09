// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Thread local storage is designed to be as efficient as possible.
// This leads to several different access patterns.
//
//
// Access pattern for a TLS static that is on a dynamically growing array
// 0. Get the TLS index somehow
// 1. Get TLS pointer to OS managed TLS block for the current thread ie. pThreadLocalData = &t_ThreadStatics
// 2. Read 1 integer value (pThreadLocalData->cCollectibleTlsData OR pThreadLocalData->cNonCollectibleTlsData)
// 3. Compare cTlsData against the index we're looking up (if (cTlsData < index.GetIndexOffset()))
// 4. If the index is not within range, jump to step 11.
// 5. Read 1 pointer value from TLS block (pThreadLocalData->pCollectibleTlsArrayData OR pThreadLocalData->pNonCollectibleTlsArrayData)
// 6. Read 1 pointer from within the TLS Array. (pTLSBaseAddress = *(intptr_t*)(((uint8_t*)pTlsArrayData) + index.GetIndexOffset());
// 7. If pointer is NULL jump to step 11 (if pTLSBaseAddress == NULL)
// 8. If TLS index not a Collectible index, return pTLSBaseAddress
// 9. if ObjectFromHandle((OBJECTHANDLE)pTLSBaseAddress) is NULL, jump to step 11
// 10. Return ObjectFromHandle((OBJECTHANDLE)pTLSBaseAddress)
// 11. Tail-call a helper (return GetThreadLocalStaticBase(index))
//
// In addition, we support accessing a TLS static that is directly on the ThreadLocalData structure. This is used for scenarios where the
// runtime native code needs to share a TLS variable between native and managed code, and for the first few TLS slots that are used by non-collectible, non-GC statics.
// We may also choose to use it for improved performance in the future, as it generates the most efficient code.
//
// Access pattern for a TLS static that is directly on the ThreadLocalData structure
// 0. Get the TLS index somehow
// 1. Get TLS pointer to OS managed TLS block for the current thread ie. pThreadLocalData = &t_ThreadStatics
// 2. Add the index offset to the start of the ThreadLocalData structure (pTLSBaseAddress = ((uint8_t*)pThreadLocalData) + index.GetIndexOffset())
//
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

enum class TLSIndexType
{
    NonCollectible, // IndexOffset for this form of TLSIndex is scaled by sizeof(OBJECTREF) and used as an index into the array at ThreadLocalData::pNonCollectibleTlsArrayData to get the final address
    Collectible, // IndexOffset for this form of TLSIndex is scaled by sizeof(void*) and then added to ThreadLocalData::pCollectibleTlsArrayData to get the final address
    DirectOnThreadLocalData, // IndexOffset for this form of TLS index is an offset into the ThreadLocalData structure itself. This is used for very high performance scenarios, and scenario where the runtime native code needs to hold a TLS pointer to a managed TLS slot. Each one of these is hand-opted into this model.
};

struct TLSIndex
{
    TLSIndex() : TLSIndexRawIndex(0xFFFFFFFF) { }
    TLSIndex(uint32_t rawIndex) : TLSIndexRawIndex(rawIndex) { }
    TLSIndex(TLSIndexType indexType, int32_t indexOffset) : TLSIndexRawIndex((((uint32_t)indexType) << 24) | (uint32_t)indexOffset) { }
    uint32_t TLSIndexRawIndex;
    int32_t GetIndexOffset() const { LIMITED_METHOD_DAC_CONTRACT; return TLSIndexRawIndex & 0xFFFFFF; }
    TLSIndexType GetTLSIndexType() const { LIMITED_METHOD_DAC_CONTRACT; return (TLSIndexType)(TLSIndexRawIndex >> 24); }
    bool IsAllocated() const { LIMITED_METHOD_DAC_CONTRACT; return TLSIndexRawIndex != 0xFFFFFFFF;}
    static TLSIndex Unallocated() { LIMITED_METHOD_DAC_CONTRACT; return TLSIndex(0xFFFFFFFF); }
    bool operator == (TLSIndex index) const { LIMITED_METHOD_DAC_CONTRACT; return TLSIndexRawIndex == index.TLSIndexRawIndex; }
    bool operator != (TLSIndex index) const { LIMITED_METHOD_DAC_CONTRACT; return TLSIndexRawIndex != index.TLSIndexRawIndex; }
};

// Used to store access to TLS data for a single index when the TLS is accessed while the class constructor is running
struct InFlightTLSData;
typedef DPTR(InFlightTLSData) PTR_InFlightTLSData;

#define EXTENDED_DIRECT_THREAD_LOCAL_SIZE 48

struct ThreadLocalData
{
    DAC_ALIGNAS(UINT64) // This is to ensure that the ExtendedDirectThreadLocalTLSData is aligned to be able to hold a double on arm legally
    int32_t cNonCollectibleTlsData; // Size of offset into the non-collectible TLS array which is valid, NOTE: this is relative to the start of the pNonCollectibleTlsArrayData object, not the start of the data in the array
    int32_t cCollectibleTlsData; // Size of offset into the TLS array which is valid
    PTR_Object pNonCollectibleTlsArrayData;
    DPTR(OBJECTHANDLE) pCollectibleTlsArrayData; // Points at the Thread local array data.
    PTR_Thread pThread;
    PTR_InFlightTLSData pInFlightData; // Points at the in-flight TLS data (TLS data that exists before the class constructor finishes running)
    TADDR ThreadBlockingInfo_First; // System.Threading.ThreadBlockingInfo.First, This starts the region of ThreadLocalData which is referenceable by TLSIndexType::DirectOnThreadLocalData
    BYTE ExtendedDirectThreadLocalTLSData[EXTENDED_DIRECT_THREAD_LOCAL_SIZE];
};

typedef DPTR(ThreadLocalData) PTR_ThreadLocalData;

#ifndef DACCESS_COMPILE
#ifdef _MSC_VER
extern __declspec(selectany) __declspec(thread)  ThreadLocalData t_ThreadStatics;
#else
extern __thread ThreadLocalData t_ThreadStatics;
#endif // _MSC_VER
#endif // DACCESS_COMPILE

#define NUMBER_OF_TLSOFFSETS_NOT_USED_IN_NONCOLLECTIBLE_ARRAY 2

class TLSIndexToMethodTableMap
{
    PTR_TADDR pMap;
    int32_t m_maxIndex;
    uint32_t m_collectibleEntries;
    TLSIndexType m_indexType;

    TADDR IsGCFlag() const { LIMITED_METHOD_CONTRACT; return (TADDR)0x1; }
    TADDR IsCollectibleFlag() const { LIMITED_METHOD_CONTRACT; return (TADDR)0x2; }
    TADDR UnwrapValue(TADDR input) const { LIMITED_METHOD_CONTRACT; return input & ~3; }
public:
    TLSIndexToMethodTableMap(TLSIndexType indexType) : pMap(dac_cast<PTR_TADDR>(dac_cast<TADDR>(0))), m_maxIndex(0), m_collectibleEntries(0), m_indexType(indexType) { }

    PTR_MethodTable Lookup(TLSIndex index, bool *isGCStatic, bool *isCollectible) const
    {
        LIMITED_METHOD_CONTRACT;
        *isGCStatic = false;
        *isCollectible = false;
        if (index.GetIndexOffset() < VolatileLoad(&m_maxIndex))
        {
            TADDR rawValue = VolatileLoadWithoutBarrier(&VolatileLoad(&pMap)[index.GetIndexOffset()]);
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
        if (index.GetIndexOffset() < VolatileLoad(&m_maxIndex))
        {
            TADDR rawValue = VolatileLoadWithoutBarrier(&VolatileLoad(&pMap)[index.GetIndexOffset()]);
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
        if (index.GetIndexOffset() < VolatileLoad(&m_maxIndex))
        {
            TADDR rawValue = VolatileLoadWithoutBarrier(&VolatileLoad(&pMap)[index.GetIndexOffset()]);
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
            e.TlsIndex = TLSIndex(m_indexType, m_maxIndex);
        }
        return e;
    }

    class iterator
    {
        friend class TLSIndexToMethodTableMap;
        const TLSIndexToMethodTableMap& m_pMap;
        entry m_entry;
        iterator(const TLSIndexToMethodTableMap& pMap, uint32_t currentIndex) : m_pMap(pMap), m_entry(pMap.Lookup(TLSIndex(pMap.m_indexType, currentIndex))) {}
        public:
        const entry&                           operator*() const { LIMITED_METHOD_CONTRACT; return m_entry; }
        const entry*                           operator->() const { LIMITED_METHOD_CONTRACT; return &m_entry; }

        bool operator==(const iterator& other) const { LIMITED_METHOD_CONTRACT; return (m_entry.TlsIndex == other.m_entry.TlsIndex); }
        bool operator!=(const iterator& other) const { LIMITED_METHOD_CONTRACT; return (m_entry.TlsIndex != other.m_entry.TlsIndex); }

        iterator& operator++()
        {
            LIMITED_METHOD_CONTRACT;
            m_entry = m_pMap.Lookup(TLSIndex(m_entry.TlsIndex.TLSIndexRawIndex + 1));
            return *this;
        }
        iterator operator++(int)
        {
            LIMITED_METHOD_CONTRACT;
            iterator tmp = *this; 
            ++(*this); 
            return tmp;
        }
    };

    iterator begin() const
    {
        LIMITED_METHOD_CONTRACT;
        iterator it(*this, 0);
        return it;
    }

    iterator end() const
    {
        LIMITED_METHOD_CONTRACT;
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
            const entry&                           operator*() const { LIMITED_METHOD_CONTRACT; return *m_current; }
            const entry*                           operator->() const { LIMITED_METHOD_CONTRACT; return m_current.operator->(); }

            bool operator==(const iterator& other) const { LIMITED_METHOD_CONTRACT; return (m_current == other.m_current); }
            bool operator!=(const iterator& other) const { LIMITED_METHOD_CONTRACT; return (m_current != other.m_current); }

            iterator& operator++()
            {
                LIMITED_METHOD_CONTRACT;
                TLSIndex oldIndex = m_current->TlsIndex;
                while (++m_current, m_current->TlsIndex != oldIndex)
                {
                    if (m_current->IsCollectible)
                        break;
                    oldIndex = m_current->TlsIndex;
                }
                return *this;
            }
            iterator operator++(int)
            {
                LIMITED_METHOD_CONTRACT;
                iterator tmp = *this;
                ++(*this);
                return tmp;
            }
        };

        iterator begin() const
        {
            LIMITED_METHOD_CONTRACT;
            iterator it(m_pMap.begin());
            if (!(it->IsCollectible))
            {
                ++it;
            }
            return it;
        }

        iterator end() const
        {
            LIMITED_METHOD_CONTRACT;
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
        LIMITED_METHOD_CONTRACT;
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

PTR_VOID GetThreadLocalStaticBaseNoCreate(Thread *pThreadLocalData, TLSIndex index);

#ifndef DACCESS_COMPILE
void ScanThreadStaticRoots(Thread* pThread, promote_func* fn, ScanContext* sc);
PTR_MethodTable LookupMethodTableForThreadStaticKnownToBeAllocated(TLSIndex index);
void InitializeThreadStaticData();
void InitializeCurrentThreadsStaticData(Thread* pThread);
void FreeLoaderAllocatorHandlesForTLSData(Thread* pThread);
void FreeThreadStaticData(Thread* pThread);
void AssertThreadStaticDataFreed();
void GetTLSIndexForThreadStatic(MethodTable* pMT, bool gcStatic, TLSIndex* pIndex, uint32_t bytesNeeded);
void FreeTLSIndicesForLoaderAllocator(LoaderAllocator *pLoaderAllocator);
void* GetThreadLocalStaticBase(TLSIndex index);
void GetThreadLocalStaticBlocksInfo (CORINFO_THREAD_STATIC_BLOCKS_INFO* pInfo);
bool CanJITOptimizeTLSAccess();
#else
void EnumThreadMemoryRegions(ThreadLocalData* pThreadLocalData, CLRDataEnumMemoryFlags flags);
#endif

#endif // __THREADLOCALSTORAGE_H__
