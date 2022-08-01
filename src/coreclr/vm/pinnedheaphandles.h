#pragma once

// The pinned heap handle bucket class is used to contain handles allocated
// from an array contained in the pinned heap.
class PinnedHeapHandleBucket
{
public:
    // Constructor and desctructor.
    PinnedHeapHandleBucket(PinnedHeapHandleBucket *pNext,  PTRARRAYREF pinnedHandleArrayObj, DWORD Size, LoaderAllocator *pLoaderAllocator);
    ~PinnedHeapHandleBucket();

    // This returns the next bucket.
    PinnedHeapHandleBucket *GetNext()
    {
        LIMITED_METHOD_CONTRACT;

        return m_pNext;
    }

    // This returns the number of remaining handle slots.
    DWORD GetNumRemainingHandles()
    {
        LIMITED_METHOD_CONTRACT;

        return m_ArraySize - m_CurrentPos;
    }

    void ConsumeRemaining()
    {
        LIMITED_METHOD_CONTRACT;

        m_CurrentPos = m_ArraySize;
    }

    OBJECTREF *TryAllocateEmbeddedFreeHandle();

    // Allocate handles from the bucket.
    OBJECTREF* AllocateHandles(DWORD nRequested);
    OBJECTREF* CurrentPos()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pArrayDataPtr + m_CurrentPos;
    }

    void EnumStaticGCRefs(promote_func* fn, ScanContext* sc);

private:
    PinnedHeapHandleBucket *m_pNext;
    int m_ArraySize;
    int m_CurrentPos;
    int m_CurrentEmbeddedFreePos;
    bool m_collectible;
    OBJECTHANDLE m_hndHandleArray;
    OBJECTREF *m_pArrayDataPtr;
};



// The pinned heap handle table is used to allocate handles that are pointers
// to objects stored in an array in the pinned object heap.
class PinnedHeapHandleTable
{
public:
    // Constructor and desctructor.
    PinnedHeapHandleTable(LoaderAllocator *pDomain, DWORD InitialBucketSize);
    ~PinnedHeapHandleTable();

    // Allocate handles from the pinned heap handle table.
    OBJECTREF* AllocateHandles(DWORD nRequested);

    // Release object handles allocated using AllocateHandles().
    void ReleaseHandles(OBJECTREF *pObjRef, DWORD nReleased);

    void EnumStaticGCRefs(promote_func* fn, ScanContext* sc);

private:
    void ReleaseHandlesLocked(OBJECTREF *pObjRef, DWORD nReleased);

    // The buckets of object handles.
    // synchronized by m_Crst
    PinnedHeapHandleBucket *m_pHead;

    // We need to know the containing LoaderAllocator so we know where to allocate handles
    LoaderAllocator *m_pLoaderAllocator;

    // The size of the PinnedHeapHandleBucket.
    // synchronized by m_Crst
    DWORD m_NextBucketSize;

    // for finding and re-using embedded free items in the list
    // these fields are synchronized by m_Crst
    PinnedHeapHandleBucket *m_pFreeSearchHint;
    DWORD m_cEmbeddedFree;

    CrstExplicitInit m_Crst;
};

class PinnedHeapHandleBlockHolder;
void PinnedHeapHandleBlockHolder__StaticFree(PinnedHeapHandleBlockHolder*);


class PinnedHeapHandleBlockHolder:public Holder<PinnedHeapHandleBlockHolder*,DoNothing,PinnedHeapHandleBlockHolder__StaticFree>

{
    PinnedHeapHandleTable* m_pTable;
    DWORD m_Count;
    OBJECTREF* m_Data;
public:
    FORCEINLINE PinnedHeapHandleBlockHolder(PinnedHeapHandleTable* pOwner, DWORD nCount)
    {
        WRAPPER_NO_CONTRACT;
        m_Data = pOwner->AllocateHandles(nCount);
        m_Count=nCount;
        m_pTable=pOwner;
    };

    FORCEINLINE void FreeData()
    {
        WRAPPER_NO_CONTRACT;
        for (DWORD i=0;i< m_Count;i++)
            ClearObjectReference(m_Data+i);
        m_pTable->ReleaseHandles(m_Data, m_Count);
    };
    FORCEINLINE OBJECTREF* operator[] (DWORD idx)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(idx<m_Count);
        return &(m_Data[idx]);
    }
};

FORCEINLINE  void PinnedHeapHandleBlockHolder__StaticFree(PinnedHeapHandleBlockHolder* pHolder)
{
    WRAPPER_NO_CONTRACT;
    pHolder->FreeData();
};