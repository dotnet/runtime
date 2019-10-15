// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// GCSample.cpp
//

//
//  This sample demonstrates:
//
//  * How to initialize GC without the rest of CoreCLR
//  * How to create a type layout information in format that the GC expects
//  * How to implement fast object allocator and write barrier 
//  * How to allocate objects and work with GC handles
//
//  An important part of the sample is the GC environment (gcenv.*) that provides methods for GC to interact 
//  with the OS and execution engine.
//
// The methods to interact with the OS should be no surprise - block memory allocation, synchronization primitives, etc.
//
// The important methods that the execution engine needs to provide to GC are:
//
// * Thread suspend/resume:
//      static void SuspendEE(SUSPEND_REASON reason);
//      static void RestartEE(bool bFinishedGC); //resume threads.
//
// * Enumeration of thread-local allocators:
//      static void GcEnumAllocContexts (enum_alloc_context_func* fn, void* param);
//
// * Scanning of stack roots:
//      static void GcScanRoots(promote_func* fn,  int condemned, int max_gen, ScanContext* sc);
//
//  The sample has trivial implementation for these methods. It is single threaded, and there are no stack roots to 
//  be reported. There are number of other callbacks that GC calls to optionally allow the execution engine to do its 
//  own bookkeeping.
//
//  For now, the sample GC environment has some cruft in it to decouple the GC from Windows and rest of CoreCLR. 
//  It is something we would like to clean up.
//

#include "common.h"

#include "gcenv.h"

#include "gc.h"
#include "objecthandle.h"

#include "gcdesc.h"

//
// The fast paths for object allocation and write barriers is performance critical. They are often
// hand written in assembly code, etc.
//
Object * AllocateObject(MethodTable * pMT)
{
    alloc_context * acontext = GetThread()->GetAllocContext();
    Object * pObject;

    size_t size = pMT->GetBaseSize();

    uint8_t* result = acontext->alloc_ptr;
    uint8_t* advance = result + size;
    if (advance <= acontext->alloc_limit)
    {
        acontext->alloc_ptr = advance;
        pObject = (Object *)result;
    }
    else
    {
        pObject = g_theGCHeap->Alloc(acontext, size, 0);
        if (pObject == NULL)
            return NULL;
    }

    pObject->RawSetMethodTable(pMT);

    return pObject;
}

#if defined(BIT64)
// Card byte shift is different on 64bit.
#define card_byte_shift     11
#else
#define card_byte_shift     10
#endif

#define card_byte(addr) (((size_t)(addr)) >> card_byte_shift)

inline void ErectWriteBarrier(Object ** dst, Object * ref)
{
    // if the dst is outside of the heap (unboxed value classes) then we
    //      simply exit
    if (((uint8_t*)dst < g_gc_lowest_address) || ((uint8_t*)dst >= g_gc_highest_address))
        return;
        
    // volatile is used here to prevent fetch of g_card_table from being reordered 
    // with g_lowest/highest_address check above. See comments in StompWriteBarrier
    uint8_t* pCardByte = (uint8_t *)*(volatile uint8_t **)(&g_gc_card_table) + card_byte((uint8_t *)dst);
    if(*pCardByte != 0xFF)
        *pCardByte = 0xFF;
}

void WriteBarrier(Object ** dst, Object * ref)
{
    *dst = ref;
    ErectWriteBarrier(dst, ref);
}

extern "C" HRESULT GC_Initialize(IGCToCLR* clrToGC, IGCHeap** gcHeap, IGCHandleManager** gcHandleManager, GcDacVars* gcDacVars);

int __cdecl main(int argc, char* argv[])
{
    //
    // Initialize system info
    //
    if (!GCToOSInterface::Initialize())
    {
        return -1;
    }

    //
    // Initialize GC heap
    //
    GcDacVars dacVars;
    IGCHeap *pGCHeap;
    IGCHandleManager *pGCHandleManager;
    if (GC_Initialize(nullptr, &pGCHeap, &pGCHandleManager, &dacVars) != S_OK)
    {
        return -1;
    }

    if (FAILED(pGCHeap->Initialize()))
        return -1;

    //
    // Initialize handle manager
    //
    if (!pGCHandleManager->Initialize())
        return -1;

    //
    // Initialize current thread
    //
    ThreadStore::AttachCurrentThread();

    //
    // Create a Methodtable with GCDesc
    //

    class My : Object {
    public:
        Object * m_pOther1;
        int dummy_inbetween;
        Object * m_pOther2;
    };

    static struct My_MethodTable
    {
        // GCDesc
        CGCDescSeries m_series[2];
        size_t m_numSeries;

        // The actual methodtable
        MethodTable m_MT;
    }
    My_MethodTable;

    // 'My' contains the MethodTable*
    uint32_t baseSize = sizeof(My);
    // GC expects the size of ObjHeader (extra void*) to be included in the size.
    baseSize = baseSize + sizeof(ObjHeader);
    // Add padding as necessary. GC requires the object size to be at least MIN_OBJECT_SIZE.
    My_MethodTable.m_MT.m_baseSize = max(baseSize, MIN_OBJECT_SIZE);

    My_MethodTable.m_MT.m_componentSize = 0;    // Array component size
    My_MethodTable.m_MT.m_flags = MTFlag_ContainsPointers;

    My_MethodTable.m_numSeries = 2;

    // The GC walks the series backwards. It expects the offsets to be sorted in descending order.
    My_MethodTable.m_series[0].SetSeriesOffset(offsetof(My, m_pOther2));
    My_MethodTable.m_series[0].SetSeriesCount(1);
    My_MethodTable.m_series[0].seriessize -= My_MethodTable.m_MT.m_baseSize;

    My_MethodTable.m_series[1].SetSeriesOffset(offsetof(My, m_pOther1));
    My_MethodTable.m_series[1].SetSeriesCount(1);
    My_MethodTable.m_series[1].seriessize -= My_MethodTable.m_MT.m_baseSize;

    MethodTable * pMyMethodTable = &My_MethodTable.m_MT;

    // Allocate instance of MyObject
    Object * pObj = AllocateObject(pMyMethodTable);
    if (pObj == NULL)
        return -1;

    // Create strong handle and store the object into it
    OBJECTHANDLE oh = HndCreateHandle(g_HandleTableMap.pBuckets[0]->pTable[GetCurrentThreadHomeHeapNumber()], HNDTYPE_DEFAULT, pObj);
    if (oh == NULL)
        return -1;

    for (int i = 0; i < 1000000; i++)
    {
        Object * pBefore = ((My *)HndFetchHandle(oh))->m_pOther1;

        // Allocate more instances of the same object
        Object * p = AllocateObject(pMyMethodTable);
        if (p == NULL)
            return -1;

        Object * pAfter = ((My *)HndFetchHandle(oh))->m_pOther1;

        // Uncomment this assert to see how GC triggered inside AllocateObject moved objects around
        // assert(pBefore == pAfter);

        // Store the newly allocated object into a field using WriteBarrier
        WriteBarrier(&(((My *)HndFetchHandle(oh))->m_pOther1), p);
    }

    // Create weak handle that points to our object
    OBJECTHANDLE ohWeak = HndCreateHandle(g_HandleTableMap.pBuckets[0]->pTable[GetCurrentThreadHomeHeapNumber()], HNDTYPE_WEAK_DEFAULT, HndFetchHandle(oh));
    if (ohWeak == NULL)
        return -1;

    // Destroy the strong handle so that nothing will be keeping out object alive
    HndDestroyHandle(HndGetHandleTable(oh), HNDTYPE_DEFAULT, oh);

    // Explicitly trigger full GC
    pGCHeap->GarbageCollect();

    // Verify that the weak handle got cleared by the GC
    assert(HndFetchHandle(ohWeak) == NULL);

    printf("Done\n");

    return 0;
}
