//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

/*
 * Generational GC handle manager.  Main Entrypoint Layer.
 *
 * Implements generic support for external roots into a GC heap.
 *

 *
 */

#include "common.h"

#include "gcenv.h"

#include "gc.h"

#include "objecthandle.h"
#include "handletablepriv.h"

#ifndef FEATURE_REDHAWK
#include "nativeoverlapped.h"
#endif

/****************************************************************************
 *
 * FORWARD DECLARATIONS
 *
 ****************************************************************************/

#ifdef _DEBUG
void DEBUG_PostGCScanHandler(HandleTable *pTable, const uint32_t *types, uint32_t typeCount, uint32_t condemned, uint32_t maxgen, ScanCallbackInfo *info);
void DEBUG_LogScanningStatistics(HandleTable *pTable, uint32_t level);
#endif

/*--------------------------------------------------------------------------*/



/****************************************************************************
 *
 * HELPER ROUTINES
 *
 ****************************************************************************/

/*
 * Table
 *
 * Gets and validates the table pointer from a table handle.
 *
 */
__inline PTR_HandleTable Table(HHANDLETABLE hTable)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    // convert the handle to a pointer
    PTR_HandleTable pTable = (PTR_HandleTable)hTable;

    // sanity
    _ASSERTE(pTable);

    // return the table pointer
    return pTable;
}

/*--------------------------------------------------------------------------*/



/****************************************************************************
 *
 * MAIN ENTRYPOINTS
 *
 ****************************************************************************/
#ifndef DACCESS_COMPILE
/*
 * HndCreateHandleTable
 *
 * Alocates and initializes a handle table.
 *
 */
HHANDLETABLE HndCreateHandleTable(const uint32_t *pTypeFlags, uint32_t uTypeCount, ADIndex uADIndex)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        INJECT_FAULT(return NULL);
    }
    CONTRACTL_END;

    // sanity
    _ASSERTE(uTypeCount);

    // verify that we can handle the specified number of types
    // may need to increase HANDLE_MAX_INTERNAL_TYPES (by 4)
    _ASSERTE(uTypeCount <= HANDLE_MAX_PUBLIC_TYPES);

    // verify that segment header layout we're using fits expected size
    _ASSERTE(sizeof(_TableSegmentHeader) <= HANDLE_HEADER_SIZE);
    // if you hit this then TABLE LAYOUT IS BROKEN

    // compute the size of the handle table allocation
    uint32_t dwSize = sizeof(HandleTable) + (uTypeCount * sizeof(HandleTypeCache));

    // allocate the table
    HandleTable *pTable = (HandleTable *) new (nothrow) uint8_t[dwSize];
    if (pTable == NULL)
        return NULL;

    memset (pTable, 0, dwSize);

    // allocate the initial handle segment
    pTable->pSegmentList = SegmentAlloc(pTable);

    // if that failed then we are also out of business
    if (!pTable->pSegmentList)
    {
        // free the table's memory and get out
        delete [] (uint8_t*)pTable;
        return NULL;
    }

    // initialize the table's lock
    // We need to allow CRST_UNSAFE_SAMELEVEL, because
    // during AD unload, we need to move some TableSegment from unloaded domain to default domain.
    // We need to take both locks for the two HandleTable's to avoid racing with concurrent gc thread.
    if (!pTable->Lock.InitNoThrow(CrstHandleTable, CrstFlags(CRST_REENTRANCY | CRST_UNSAFE_ANYMODE | CRST_DEBUGGER_THREAD | CRST_UNSAFE_SAMELEVEL)))
    {
        SegmentFree(pTable->pSegmentList);
        delete [] (uint8_t*)pTable;
        return NULL;
    }

    // remember how many types we are supporting
    pTable->uTypeCount = uTypeCount;

    // Store user data
    pTable->uTableIndex = (uint32_t) -1;
    pTable->uADIndex = uADIndex;

    // loop over various arrays an initialize them
    uint32_t u;

    // initialize the type flags for the types we were passed
    for (u = 0; u < uTypeCount; u++)
        pTable->rgTypeFlags[u] = pTypeFlags[u];

    // preinit the rest to HNDF_NORMAL
    while (u < HANDLE_MAX_INTERNAL_TYPES)
        pTable->rgTypeFlags[u++] = HNDF_NORMAL;

    // initialize the main cache
    for (u = 0; u < uTypeCount; u++)
    {
        // at init time, the only non-zero field in a type cache is the free index
        pTable->rgMainCache[u].lFreeIndex = HANDLES_PER_CACHE_BANK;
    }

#ifdef _DEBUG
    // set up scanning stats
    pTable->_DEBUG_iMaxGen = -1;
#endif

    // all done - return the newly created table
    return (HHANDLETABLE)pTable;
}


/*
 * HndDestroyHandleTable
 *
 * Cleans up and frees the specified handle table.
 *
 */
void HndDestroyHandleTable(HHANDLETABLE hTable)
{
    WRAPPER_NO_CONTRACT;

    // fetch the handle table pointer
    HandleTable *pTable = Table(hTable);

    // decrement handle count by number of handles in this table
    COUNTER_ONLY(GetPerfCounters().m_GC.cHandles -= HndCountHandles(hTable));

    // We are going to free the memory for this HandleTable.
    // Let us reset the copy in g_pHandleTableArray to NULL.
    // Otherwise, GC will think this HandleTable is still available.

    // free the lock
    pTable->Lock.Destroy();

    // fetch the segment list and null out the list pointer
    TableSegment *pSegment = pTable->pSegmentList;
    pTable->pSegmentList = NULL;

    // walk the segment list, freeing the segments as we go
    while (pSegment)
    {
        // fetch the next segment
        TableSegment *pNextSegment = pSegment->pNextSegment;

        // free the current one and advance to the next
        SegmentFree(pSegment);
        pSegment = pNextSegment;
    }

    // free the table's memory
    delete [] (uint8_t*) pTable;
}
/*
 * HndSetHandleTableIndex
 *
 * Sets the index associated with a handle table at creation
 */
void HndSetHandleTableIndex(HHANDLETABLE hTable, uint32_t uTableIndex)
{
    WRAPPER_NO_CONTRACT;

    // fetch the handle table pointer
    HandleTable *pTable = Table(hTable);

    pTable->uTableIndex = uTableIndex;
}
#endif // !DACCESS_COMPILE

/*
 * HndGetHandleTableIndex
 *
 * Retrieves the index associated with a handle table at creation
 */
uint32_t HndGetHandleTableIndex(HHANDLETABLE hTable)
{
    WRAPPER_NO_CONTRACT;

    // fetch the handle table pointer
    HandleTable *pTable = Table(hTable);

    _ASSERTE (pTable->uTableIndex != (uint32_t) -1);  // We have not set uTableIndex yet.
    return pTable->uTableIndex;
}

/*
 * HndGetHandleTableIndex
 *
 * Retrieves the AppDomain index associated with a handle table at creation
 */
ADIndex HndGetHandleTableADIndex(HHANDLETABLE hTable)
{
    WRAPPER_NO_CONTRACT;

    // fetch the handle table pointer
    HandleTable *pTable = Table(hTable);

    return pTable->uADIndex;
}

/*
 * HndGetHandleTableIndex
 *
 * Retrieves the AppDomain index associated with a handle table at creation
 */
ADIndex HndGetHandleADIndex(OBJECTHANDLE handle)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    // fetch the handle table pointer
    HandleTable *pTable = Table(HndGetHandleTable(handle));

    return pTable->uADIndex;
}

#ifndef DACCESS_COMPILE
/*
 * HndCreateHandle
 *
 * Entrypoint for allocating an individual handle.
 *
 */
OBJECTHANDLE HndCreateHandle(HHANDLETABLE hTable, uint32_t uType, OBJECTREF object, uintptr_t lExtraInfo)
{
    CONTRACTL
    {
#ifdef FEATURE_REDHAWK
        // Redhawk returns NULL on failure.
        NOTHROW;
#else
        THROWS;
#endif
        GC_NOTRIGGER;
        if (object != NULL) 
        { 
            MODE_COOPERATIVE; 
        }
        else
        {
            MODE_ANY;
        }
        SO_INTOLERANT;
    }
    CONTRACTL_END;

#if defined( _DEBUG) && !defined(FEATURE_REDHAWK)
    if (g_pConfig->ShouldInjectFault(INJECTFAULT_HANDLETABLE))
    {
        FAULT_NOT_FATAL();
        char *a = new char;
        delete a;
    }
#endif // _DEBUG && !FEATURE_REDHAWK

    VALIDATEOBJECTREF(object);

    // fetch the handle table pointer
    HandleTable *pTable = Table(hTable);

    // sanity check the type index
    _ASSERTE(uType < pTable->uTypeCount);

    // get a handle from the table's cache
    OBJECTHANDLE handle = TableAllocSingleHandleFromCache(pTable, uType);

    // did the allocation succeed?
    if (!handle)
    {
#ifdef FEATURE_REDHAWK
        return NULL;
#else
        ThrowOutOfMemory();
#endif
    }

#ifdef DEBUG_DestroyedHandleValue
    if (*(_UNCHECKED_OBJECTREF *)handle == DEBUG_DestroyedHandleValue)
        *(_UNCHECKED_OBJECTREF *)handle = NULL;
#endif

    // yep - the handle better not point at anything yet
    _ASSERTE(*(_UNCHECKED_OBJECTREF *)handle == NULL);

    // we are not holding the lock - check to see if there is nonzero extra info
    if (lExtraInfo)
    {
        // initialize the user data BEFORE assigning the referent
        // this ensures proper behavior if we are currently scanning
        HandleQuickSetUserData(handle, lExtraInfo);
    }

    // store the reference
    HndAssignHandle(handle, object);

    // update perf-counters: track number of handles
    COUNTER_ONLY(GetPerfCounters().m_GC.cHandles ++);

#ifdef GC_PROFILING
    {
        BEGIN_PIN_PROFILER(CORProfilerTrackGC());
        g_profControlBlock.pProfInterface->HandleCreated((uintptr_t)handle, (ObjectID)OBJECTREF_TO_UNCHECKED_OBJECTREF(object));
        END_PIN_PROFILER();
    }
#endif //GC_PROFILING

    STRESS_LOG2(LF_GC, LL_INFO1000, "CreateHandle: %p, type=%d\n", handle, uType);

    // return the result
    return handle;
}
#endif // !DACCESS_COMPILE

#ifdef _DEBUG
void ValidateFetchObjrefForHandle(OBJECTREF objref, ADIndex appDomainIndex)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_SO_TOLERANT;
    STATIC_CONTRACT_MODE_COOPERATIVE;
    STATIC_CONTRACT_DEBUG_ONLY;

    BEGIN_DEBUG_ONLY_CODE;
    VALIDATEOBJECTREF (objref);

    AppDomain *pDomain = SystemDomain::GetAppDomainAtIndex(appDomainIndex);

    // Access to a handle in unloaded domain is not allowed
    _ASSERTE(pDomain != NULL);
    _ASSERTE(!pDomain->NoAccessToHandleTable());

#if CHECK_APP_DOMAIN_LEAKS
    if (g_pConfig->AppDomainLeaks())
    {
        if (appDomainIndex.m_dwIndex)
            objref->TryAssignAppDomain(pDomain);
        else if (objref != 0)
            objref->TrySetAppDomainAgile();
    }
#endif
    END_DEBUG_ONLY_CODE;
}

void ValidateAssignObjrefForHandle(OBJECTREF objref, ADIndex appDomainIndex)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_SO_TOLERANT;
    STATIC_CONTRACT_MODE_COOPERATIVE;
    STATIC_CONTRACT_DEBUG_ONLY;

    BEGIN_DEBUG_ONLY_CODE;

    VALIDATEOBJECTREF (objref);

    AppDomain *pDomain = SystemDomain::GetAppDomainAtIndex(appDomainIndex);

    // Access to a handle in unloaded domain is not allowed
    _ASSERTE(pDomain != NULL);
    _ASSERTE(!pDomain->NoAccessToHandleTable());

#if CHECK_APP_DOMAIN_LEAKS
    if (g_pConfig->AppDomainLeaks())
    {
        if (appDomainIndex.m_dwIndex)
            objref->TryAssignAppDomain(pDomain);
        else if (objref != 0)
            objref->TrySetAppDomainAgile();
    }
#endif
    END_DEBUG_ONLY_CODE;
}

void ValidateAppDomainForHandle(OBJECTHANDLE handle)
{
    STATIC_CONTRACT_DEBUG_ONLY;
    STATIC_CONTRACT_NOTHROW;

#ifdef DEBUG_DestroyedHandleValue
    // Verify that we are not trying to access freed handle.
    _ASSERTE("Attempt to access destroyed handle." && *(_UNCHECKED_OBJECTREF *)handle != DEBUG_DestroyedHandleValue);
#endif
#ifdef DACCESS_COMPILE
    UNREFERENCED_PARAMETER(handle);
#else
    BEGIN_DEBUG_ONLY_CODE;
    ADIndex id = HndGetHandleADIndex(handle);
    AppDomain *pUnloadingDomain = SystemDomain::AppDomainBeingUnloaded();
    if (!pUnloadingDomain || pUnloadingDomain->GetIndex() != id)
    {
        return;
    }
    if (!pUnloadingDomain->NoAccessToHandleTable())
    {
        return;
    }
    _ASSERTE (!"Access to a handle in unloaded domain is not allowed");
    END_DEBUG_ONLY_CODE;
#endif // !DACCESS_COMPILE
}
#endif


#ifndef DACCESS_COMPILE
/*
 * HndDestroyHandle
 *
 * Entrypoint for freeing an individual handle.
 *
 */
void HndDestroyHandle(HHANDLETABLE hTable, uint32_t uType, OBJECTHANDLE handle)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_TOLERANT; 
        CAN_TAKE_LOCK;     // because of TableFreeSingleHandleToCache
    }
    CONTRACTL_END;

    STRESS_LOG2(LF_GC, LL_INFO1000, "DestroyHandle: *%p->%p\n", handle, *(_UNCHECKED_OBJECTREF *)handle);

    FireEtwDestroyGCHandle((void*) handle, GetClrInstanceId());
    FireEtwPrvDestroyGCHandle((void*) handle, GetClrInstanceId());

    // sanity check handle we are being asked to free
    _ASSERTE(handle);

#ifdef _DEBUG
    ValidateAppDomainForHandle(handle);
#endif

    // fetch the handle table pointer
    HandleTable *pTable = Table(hTable);

#ifdef GC_PROFILING
    {
        BEGIN_PIN_PROFILER(CORProfilerTrackGC());
        g_profControlBlock.pProfInterface->HandleDestroyed((uintptr_t)handle);
        END_PIN_PROFILER();
    }        
#endif //GC_PROFILING

    // update perf-counters: track number of handles
    COUNTER_ONLY(GetPerfCounters().m_GC.cHandles --);

    // sanity check the type index
    _ASSERTE(uType < pTable->uTypeCount);

    _ASSERTE(HandleFetchType(handle) == uType);

    // return the handle to the table's cache
    TableFreeSingleHandleToCache(pTable, uType, handle);
}


/*
 * HndDestroyHandleOfUnknownType
 *
 * Entrypoint for freeing an individual handle whose type is unknown.
 *
 */
void HndDestroyHandleOfUnknownType(HHANDLETABLE hTable, OBJECTHANDLE handle)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;

    // sanity check handle we are being asked to free
    _ASSERTE(handle);

#ifdef FEATURE_COMINTEROP
    // If we're being asked to destroy a WinRT weak handle, that will cause a leak
    // of the IWeakReference* that it holds in its extra data. Instead of using this
    // API use DestroyWinRTWeakHandle instead.
    _ASSERTE(HandleFetchType(handle) != HNDTYPE_WEAK_WINRT);
#endif // FEATURE_COMINTEROP

    // fetch the type and then free normally
    HndDestroyHandle(hTable, HandleFetchType(handle), handle);
}


/*
 * HndCreateHandles
 *
 * Entrypoint for allocating handles in bulk.
 *
 */
uint32_t HndCreateHandles(HHANDLETABLE hTable, uint32_t uType, OBJECTHANDLE *pHandles, uint32_t uCount)
{
    WRAPPER_NO_CONTRACT;

    // fetch the handle table pointer
    HandleTable *pTable = Table(hTable);

    // sanity check the type index
    _ASSERTE(uType < pTable->uTypeCount);

    // keep track of the number of handles we've allocated
    uint32_t uSatisfied = 0;

    // if this is a large number of handles then bypass the cache
    if (uCount > SMALL_ALLOC_COUNT)
    {
        CrstHolder ch(&pTable->Lock);

        // allocate handles in bulk from the main handle table
        uSatisfied = TableAllocBulkHandles(pTable, uType, pHandles, uCount);
    }

    // do we still need to get some handles?
    if (uSatisfied < uCount)
    {
        // get some handles from the cache
        uSatisfied += TableAllocHandlesFromCache(pTable, uType, pHandles + uSatisfied, uCount - uSatisfied);
    }

    // update perf-counters: track number of handles
    COUNTER_ONLY(GetPerfCounters().m_GC.cHandles += uSatisfied);

#ifdef GC_PROFILING
    {
        BEGIN_PIN_PROFILER(CORProfilerTrackGC());
        for (uint32_t i = 0; i < uSatisfied; i++)
            g_profControlBlock.pProfInterface->HandleCreated((uintptr_t)pHandles[i], 0);
        END_PIN_PROFILER();
    }
#endif //GC_PROFILING

    // return the number of handles we allocated
    return uSatisfied;
}


/*
 * HndDestroyHandles
 *
 * Entrypoint for freeing handles in bulk.
 *
 */
void HndDestroyHandles(HHANDLETABLE hTable, uint32_t uType, const OBJECTHANDLE *pHandles, uint32_t uCount)
{
    WRAPPER_NO_CONTRACT;

#ifdef _DEBUG
    ValidateAppDomainForHandle(pHandles[0]);
#endif
    
    // fetch the handle table pointer
    HandleTable *pTable = Table(hTable);

    // sanity check the type index
    _ASSERTE(uType < pTable->uTypeCount);

#ifdef GC_PROFILING
    {
        BEGIN_PIN_PROFILER(CORProfilerTrackGC());
        for (uint32_t i = 0; i < uCount; i++)
            g_profControlBlock.pProfInterface->HandleDestroyed((uintptr_t)pHandles[i]);
        END_PIN_PROFILER();
    }
#endif

    // update perf-counters: track number of handles
    COUNTER_ONLY(GetPerfCounters().m_GC.cHandles -= uCount);

    // is this a small number of handles?
    if (uCount <= SMALL_ALLOC_COUNT)
    {
        // yes - free them via the handle cache
        TableFreeHandlesToCache(pTable, uType, pHandles, uCount);
        return;
    }

    // acquire the handle manager lock
    {
        CrstHolder ch(&pTable->Lock);
    
        // free the unsorted handles in bulk to the main handle table
        TableFreeBulkUnpreparedHandles(pTable, uType, pHandles, uCount);
    }
}

/*
 * HndSetHandleExtraInfo
 *
 * Stores owner data with handle.
 *
 */
void HndSetHandleExtraInfo(OBJECTHANDLE handle, uint32_t uType, uintptr_t lExtraInfo)
{
    WRAPPER_NO_CONTRACT;

    // fetch the user data slot for this handle if we have the right type
    uintptr_t *pUserData = HandleValidateAndFetchUserDataPointer(handle, uType);

    // is there a slot?
    if (pUserData)
    {
        // yes - store the info
        *pUserData = lExtraInfo;
    }
}

/*
* HndCompareExchangeHandleExtraInfo
*
* Stores owner data with handle.
*
*/
uintptr_t HndCompareExchangeHandleExtraInfo(OBJECTHANDLE handle, uint32_t uType, uintptr_t lOldExtraInfo, uintptr_t lNewExtraInfo)
{
    WRAPPER_NO_CONTRACT;

    // fetch the user data slot for this handle if we have the right type
    uintptr_t *pUserData = HandleValidateAndFetchUserDataPointer(handle, uType);

    // is there a slot?
    if (pUserData)
    {
        // yes - attempt to store the info
        return (uintptr_t)Interlocked::CompareExchangePointer((void**)pUserData, (void*)lNewExtraInfo, (void*)lOldExtraInfo);
    }

    _ASSERTE(!"Shouldn't be trying to call HndCompareExchangeHandleExtraInfo on handle types without extra info");
    return NULL;
}
#endif // !DACCESS_COMPILE

/*
 * HndGetHandleExtraInfo
 *
 * Retrieves owner data from handle.
 *
 */
uintptr_t HndGetHandleExtraInfo(OBJECTHANDLE handle)
{
    WRAPPER_NO_CONTRACT;

    // assume zero until we actually get it
    uintptr_t lExtraInfo = 0L;

    // fetch the user data slot for this handle
    PTR_uintptr_t pUserData = HandleQuickFetchUserDataPointer(handle);

    // if we did then copy the value
    if (pUserData)
    {
        lExtraInfo = *(pUserData);
    }

    // return the value to our caller
    return lExtraInfo;
}

/*
 * HndGetHandleTable
 *
 * Returns the containing table of a handle.
 * 
 */
HHANDLETABLE HndGetHandleTable(OBJECTHANDLE handle)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    PTR_HandleTable pTable = HandleFetchHandleTable(handle);

    return (HHANDLETABLE)pTable;
}

void HndLogSetEvent(OBJECTHANDLE handle, _UNCHECKED_OBJECTREF value)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_SO_TOLERANT;
    STATIC_CONTRACT_MODE_COOPERATIVE;

#if !defined(DACCESS_COMPILE) && defined(FEATURE_EVENT_TRACE)
    if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_Context, SetGCHandle) ||
        ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_Context, SetGCHandle))
    {
        uint32_t hndType = HandleFetchType(handle);
        ADIndex appDomainIndex = HndGetHandleADIndex(handle);   
        AppDomain* pAppDomain = SystemDomain::GetAppDomainAtIndex(appDomainIndex);
        uint32_t generation = value != 0 ? GCHeap::GetGCHeap()->WhichGeneration(value) : 0;
        FireEtwSetGCHandle((void*) handle, value, hndType, generation, (int64_t) pAppDomain, GetClrInstanceId());
        FireEtwPrvSetGCHandle((void*) handle, value, hndType, generation, (int64_t) pAppDomain, GetClrInstanceId());

#ifndef FEATURE_REDHAWK
        // Also fire the things pinned by Async pinned handles
        if (hndType == HNDTYPE_ASYNCPINNED)
        {
            if (value->GetMethodTable() == g_pOverlappedDataClass)
            {
                OverlappedDataObject* overlapped = (OverlappedDataObject*) value;
                if (overlapped->m_isArray)
                {
                    ArrayBase* pUserObject = (ArrayBase*)OBJECTREFToObject(overlapped->m_userObject);
                    Object **ppObj = (Object**)pUserObject->GetDataPtr(TRUE);
                    size_t num = pUserObject->GetNumComponents();
                    for (size_t i = 0; i < num; i ++)
                    {
                        value = ppObj[i];
                        uint32_t generation = value != 0 ? GCHeap::GetGCHeap()->WhichGeneration(value) : 0;
                        FireEtwSetGCHandle(overlapped, value, HNDTYPE_PINNED, generation, (int64_t) pAppDomain, GetClrInstanceId());
                    }
                }
                else
                {
                    value = OBJECTREF_TO_UNCHECKED_OBJECTREF(overlapped->m_userObject);
                    uint32_t generation = value != 0 ? GCHeap::GetGCHeap()->WhichGeneration(value) : 0;
                    FireEtwSetGCHandle(overlapped, value, HNDTYPE_PINNED, generation, (int64_t) pAppDomain, GetClrInstanceId());
                }
            }
        }
#endif // FEATURE_REDHAWK
    }
#else
    UNREFERENCED_PARAMETER(handle);
    UNREFERENCED_PARAMETER(value);
#endif
}

/*
 * HndWriteBarrier
 *
 * Resets the generation number for the handle's clump to zero.
 *
 */
void HndWriteBarrier(OBJECTHANDLE handle, OBJECTREF objref)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_SO_TOLERANT;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    // unwrap the objectref we were given
    _UNCHECKED_OBJECTREF value = OBJECTREF_TO_UNCHECKED_OBJECTREF(objref);
    
    _ASSERTE (objref != NULL);

    // find the write barrier for this handle
    uint8_t *barrier = (uint8_t *)((uintptr_t)handle & HANDLE_SEGMENT_ALIGN_MASK);
    
    // sanity
    _ASSERTE(barrier);
    
    // find the offset of this handle into the segment
    uintptr_t offset = (uintptr_t)handle & HANDLE_SEGMENT_CONTENT_MASK;
    
    // make sure it is in the handle area and not the header
    _ASSERTE(offset >= HANDLE_HEADER_SIZE);
    
    // compute the clump index for this handle
    offset = (offset - HANDLE_HEADER_SIZE) / (HANDLE_SIZE * HANDLE_HANDLES_PER_CLUMP);

    // Be careful to read and write the age byte via volatile operations. Otherwise the compiler has been
    // observed to translate the read + conditional write sequence below into an unconditional read/write
    // (utilizing a conditional register move to determine whether the write is an update or simply writes
    // back what was read). This is a legal transformation for non-volatile accesses but obviously leads to a
    // race condition where we can lose an update (see the comment below for the race condition).
    volatile uint8_t * pClumpAge = barrier + offset;

    // if this age is smaller than age of the clump, update the clump age
    if (*pClumpAge != 0) // Perf optimization: if clumpAge is 0, nothing more to do
    {
        // find out generation
        int generation = GCHeap::GetGCHeap()->WhichGeneration(value);
        uint32_t uType = HandleFetchType(handle);

#ifndef FEATURE_REDHAWK
        //OverlappedData need special treatment: because all user data pointed by it needs to be reported by this handle,
        //its age is consider to be min age of the user data, to be simple, we just make it 0
        if (uType == HNDTYPE_ASYNCPINNED && objref->GetGCSafeMethodTable () == g_pOverlappedDataClass)
        {
            generation = 0;
        }
#endif // !FEATURE_REDHAWK
        
        if (uType == HNDTYPE_DEPENDENT)
        {
            generation = 0;
        }

        if (*pClumpAge > (uint8_t) generation)
        {
            // We have to be careful here. HndWriteBarrier is not under any synchronization
            // Consider the scenario where 2 threads are hitting the line below at the same
            // time. Only one will win. If the winner has an older age than the loser, we
            // just created a potential GC hole  (The clump will not be reporting the 
            // youngest handle in the clump, thus GC may skip the clump). To fix this
            // we just set the clump age to 0, which means that whoever wins the race
            // results are the same, as GC will always look at the clump
            *pClumpAge = (uint8_t)0;
        }
    }
}

/*
 * HndEnumHandles
 *
 * Enumerates all handles of the specified type in the handle table.
 *
 * This entrypoint is provided for utility code (debugger support etc) that
 * needs to enumerate all roots in the handle table.
 *
 */
void HndEnumHandles(HHANDLETABLE hTable, const uint32_t *puType, uint32_t uTypeCount,
                    HANDLESCANPROC pfnEnum, uintptr_t lParam1, uintptr_t lParam2, bool fAsync)
{
    WRAPPER_NO_CONTRACT;

    // fetch the handle table pointer
    PTR_HandleTable pTable = Table(hTable);

    // per-block scanning callback
    BLOCKSCANPROC pfnBlock;

    // do we need to support user data?
    BOOL fEnumUserData = TypesRequireUserDataScanning(pTable, puType, uTypeCount);

    if (fEnumUserData)
    {
        // scan all handles with user data
        pfnBlock = BlockScanBlocksWithUserData;
    }
    else
    {
        // scan all handles without user data
        pfnBlock = BlockScanBlocksWithoutUserData;
    }

    // set up parameters for handle enumeration
    ScanCallbackInfo info;

    info.uFlags          = (fAsync? HNDGCF_ASYNC : HNDGCF_NORMAL);
    info.fEnumUserData   = fEnumUserData;
    info.dwAgeMask       = 0;
    info.pCurrentSegment = NULL;
    info.pfnScan         = pfnEnum;
    info.param1          = lParam1;
    info.param2          = lParam2;

    // choose a scanning method based on the async flag
    TABLESCANPROC pfnScanTable = TableScanHandles;
    if (fAsync)
        pfnScanTable = xxxTableScanHandlesAsync;

    {
        // acquire the handle manager lock
        CrstHolderWithState ch(&pTable->Lock);

        // scan the table
        pfnScanTable(pTable, puType, uTypeCount, FullSegmentIterator, pfnBlock, &info, &ch);
    }
}

/*
 * HndScanHandlesForGC
 *
 * Multiple type scanning entrypoint for GC.
 *
 * This entrypoint is provided for GC-time scnas of the handle table ONLY.  It
 * enables ephemeral scanning of the table, and optionally ages the write barrier
 * as it scans.
 *
 */
void HndScanHandlesForGC(HHANDLETABLE hTable, HANDLESCANPROC scanProc, uintptr_t param1, uintptr_t param2,
                         const uint32_t *types, uint32_t typeCount, uint32_t condemned, uint32_t maxgen, uint32_t flags)
{
    WRAPPER_NO_CONTRACT;

    // fetch the table pointer
    PTR_HandleTable pTable = Table(hTable);

    // per-segment and per-block callbacks
    SEGMENTITERATOR pfnSegment;
    BLOCKSCANPROC pfnBlock = NULL;

    // do we need to support user data?
    BOOL enumUserData =
        ((flags & HNDGCF_EXTRAINFO) &&
        TypesRequireUserDataScanning(pTable, types, typeCount));

    // what type of GC are we performing?
    if (condemned >= maxgen)
    {
        // full GC - use our full-service segment iterator
        pfnSegment = FullSegmentIterator;

        // see if there is a callback
        if (scanProc)
        {
            // do we need to scan blocks with user data?
            if (enumUserData)
            {
                // scan all with user data
                pfnBlock = BlockScanBlocksWithUserData;
            }
            else
            {
                // scan all without user data
                pfnBlock = BlockScanBlocksWithoutUserData;
            }
        }
        else if (flags & HNDGCF_AGE)
        {
            // there is only aging to do
            pfnBlock = BlockAgeBlocks;
        }
    }
    else
    {
        // this is an ephemeral GC - is it g0?
        if (condemned == 0)
        {
            // yes - do bare-bones enumeration
            pfnSegment = QuickSegmentIterator;
        }
        else
        {
            // no - do normal enumeration
            pfnSegment = StandardSegmentIterator;
        }

        // see if there is a callback
        if (scanProc)
        {
            // there is a scan callback - scan the condemned generation
            pfnBlock = BlockScanBlocksEphemeral;
        }
#ifndef DACCESS_COMPILE
        else if (flags & HNDGCF_AGE)
        {
            // there is only aging to do
            pfnBlock = BlockAgeBlocksEphemeral;
        }
#endif
    }

    // set up parameters for scan callbacks
    ScanCallbackInfo info;

    info.uFlags          = flags;
    info.fEnumUserData   = enumUserData;
    info.dwAgeMask       = BuildAgeMask(condemned, maxgen);
    info.pCurrentSegment = NULL;
    info.pfnScan         = scanProc;
    info.param1          = param1;
    info.param2          = param2;

#if defined(_DEBUG) && !defined(DACCESS_COMPILE)
    info.DEBUG_BlocksScanned                = 0;
    info.DEBUG_BlocksScannedNonTrivially    = 0;
    info.DEBUG_HandleSlotsScanned           = 0;
    info.DEBUG_HandlesActuallyScanned       = 0;
#endif

    // choose a scanning method based on the async flag
    TABLESCANPROC pfnScanTable = TableScanHandles;
    if (flags & HNDGCF_ASYNC)
    {
        pfnScanTable = xxxTableScanHandlesAsync;
    }

    {
        // lock the table down for concurrent GC only
        CrstHolderWithState ch(&pTable->Lock, (flags & HNDGCF_ASYNC) != 0);

        // perform the scan
        pfnScanTable(pTable, types, typeCount, pfnSegment, pfnBlock, &info, &ch);

#if defined(_DEBUG) && !defined(DACCESS_COMPILE)
        // update our scanning statistics for this generation
        DEBUG_PostGCScanHandler(pTable, types, typeCount, condemned, maxgen, &info);
    #endif
    }
}

#ifndef DACCESS_COMPILE


/*
 * HndResetAgeMap
 *
 * Service to forceably reset the age map for a set of handles.
 *
 * Provided for GC-time resetting the handle table's write barrier.  This is not
 * normally advisable, as it increases the amount of work that will be done in
 * subsequent scans.  Under some circumstances, however, this is precisely what is
 * desired.  Generally this entrypoint should only be used under some exceptional
 * condition during garbage collection, like objects being demoted from a higher
 * generation to a lower one.
 *
 */
void HndResetAgeMap(HHANDLETABLE hTable, const uint32_t *types, uint32_t typeCount, uint32_t condemned, uint32_t maxgen, uint32_t flags)
{
    WRAPPER_NO_CONTRACT;

    // fetch the table pointer
    HandleTable *pTable = Table(hTable);

    // set up parameters for scan callbacks
    ScanCallbackInfo info;

    info.uFlags          = flags;
    info.fEnumUserData   = FALSE;
    info.dwAgeMask       = BuildAgeMask(condemned, maxgen);
    info.pCurrentSegment = NULL;
    info.pfnScan         = NULL;
    info.param1          = 0;
    info.param2          = 0;

    {
        // lock the table down
        CrstHolderWithState ch(&pTable->Lock);

        // perform the scan
        TableScanHandles(pTable, types, typeCount, QuickSegmentIterator, BlockResetAgeMapForBlocks, &info, &ch);
    }
}


/*
 * HndVerifyTable
 *
 * Service to check the correctness of the handle table for a set of handles
 *
 * Provided for checking the correctness of handle table and the gc.
 * Will validate that each handle points to a valid object.
 * Will also validate that the generation of the handle is <= generation of the object.
 * Cannot have == because the handle table only remembers the generation for a group of
 * 16 handles.
 *
 */
void HndVerifyTable(HHANDLETABLE hTable, const uint32_t *types, uint32_t typeCount, uint32_t condemned, uint32_t maxgen, uint32_t flags)
{
    WRAPPER_NO_CONTRACT;

    // fetch the table pointer
    HandleTable *pTable = Table(hTable);

    // set up parameters for scan callbacks
    ScanCallbackInfo info;

    info.uFlags          = flags;
    info.fEnumUserData   = FALSE;
    info.dwAgeMask       = BuildAgeMask(condemned, maxgen);
    info.pCurrentSegment = NULL;
    info.pfnScan         = NULL;
    info.param1          = 0;
    info.param2          = 0;

    {
        // lock the table down
        CrstHolderWithState ch(&pTable->Lock);

        // perform the scan
        TableScanHandles(pTable, types, typeCount, QuickSegmentIterator, BlockVerifyAgeMapForBlocks, &info, &ch);
    }
}


/*
 * HndNotifyGcCycleComplete
 *
 * Informs the handle table that a GC has completed.
 *
 */
void HndNotifyGcCycleComplete(HHANDLETABLE hTable, uint32_t condemned, uint32_t maxgen)
{
#ifdef _DEBUG
    WRAPPER_NO_CONTRACT;

    // fetch the handle table pointer
    HandleTable *pTable = Table(hTable);

    {
        // lock the table down
        CrstHolder ch(&pTable->Lock);

        // if this was a full GC then dump a cumulative log of scanning stats
        if (condemned >= maxgen)
            DEBUG_LogScanningStatistics(pTable, LL_INFO10);
    }
#else
    LIMITED_METHOD_CONTRACT;
    UNREFERENCED_PARAMETER(hTable);
    UNREFERENCED_PARAMETER(condemned);
    UNREFERENCED_PARAMETER(maxgen);
#endif
}

extern int getNumberOfSlots();


/*
 * HndCountHandles
 *
 * Counts the number of handles owned by the handle table that are marked as
 * "used" that are not currently residing in the handle table's cache.
 *
 * Provided to compute the correct value for the GC Handle perfcounter.
 * The caller is responsible for acquiring the handle table's lock if
 * it is necessary.
 *
 */
uint32_t HndCountHandles(HHANDLETABLE hTable)
{
    WRAPPER_NO_CONTRACT;
    // fetch the handle table pointer
    HandleTable *pTable = Table(hTable);
    
    // initialize the count of handles in the cache to 0
    uint32_t uCacheCount = 0;

    // fetch the count of handles marked as "used"
    uint32_t uCount = pTable->dwCount;

    // loop through the main cache for each handle type
    HandleTypeCache *pCache = pTable->rgMainCache;
    HandleTypeCache *pCacheEnd = pCache + pTable->uTypeCount;
    for (; pCache != pCacheEnd; ++pCache)
    {
        // get relevant indexes for the reserve bank and the free bank
        int32_t lFreeIndex = pCache->lFreeIndex;
        int32_t lReserveIndex = pCache->lReserveIndex;

        // clamp the min free index and min reserve index to be non-negative;
        // this is necessary since interlocked operations can set these variables
        // to negative values, and once negative they stay negative until the
        // cache is rebalanced
        if (lFreeIndex < 0) lFreeIndex = 0;
        if (lReserveIndex < 0) lReserveIndex = 0;

        // compute the number of handles
        uint32_t uHandleCount = (uint32_t)lReserveIndex + (HANDLES_PER_CACHE_BANK - (uint32_t)lFreeIndex);

        // add the number of handles to the total handle count and update
        // dwCount in this HandleTable
        uCacheCount += uHandleCount;
    }

    // it is not necessary to have the lock while reading the quick cache;
    // loop through the quick cache for each handle type
    OBJECTHANDLE * pQuickCache = pTable->rgQuickCache;
    OBJECTHANDLE * pQuickCacheEnd = pQuickCache + HANDLE_MAX_INTERNAL_TYPES;
    for (; pQuickCache != pQuickCacheEnd; ++pQuickCache)
        if (*pQuickCache)
            ++uCacheCount;

    // return the number of handles marked as "used" that are not
    // residing in the cache
    return (uCount - uCacheCount);
}


/*
 * HndCountAllHandles
 *
 * Counts the total number of handles that are marked as "used" that are not 
 * currently residing in some handle table's cache.
 *
 * Provided to compute the correct value for the GC Handle perfcounter.
 * The 'fUseLocks' flag specifies whether to acquire each handle table's lock 
 * while its handles are being counted.
 *
 */
uint32_t HndCountAllHandles(BOOL fUseLocks)
{
    uint32_t uCount = 0;
    int offset = 0;
    
    // get number of HandleTables per HandleTableBucket
    int n_slots = getNumberOfSlots();

    // fetch the pointer to the head of the list
    struct HandleTableMap * walk = &g_HandleTableMap;

    // walk the list
    while (walk)
    {
        int nextOffset = walk->dwMaxIndex;
        int max = nextOffset - offset;
        PTR_PTR_HandleTableBucket pBucket = walk->pBuckets;
        PTR_PTR_HandleTableBucket pLastBucket = pBucket + max;

        // loop through each slot in this node
        for (; pBucket != pLastBucket; ++pBucket)
        {
            // if there is a HandleTableBucket in this slot
            if (*pBucket)
            {
                // loop through the HandleTables inside this HandleTableBucket,
                // and accumulate the handle count of each HandleTable
                HHANDLETABLE * pTable = (*pBucket)->pTable;
                HHANDLETABLE * pLastTable = pTable + n_slots;

                // if the 'fUseLocks' flag is set, acquire the lock for this handle table before 
                // calling HndCountHandles() - this will prevent dwCount from being modified and 
                // it will also prevent any of the main caches from being rebalanced
                if (fUseLocks)
                    for (; pTable != pLastTable; ++pTable)
                    {                   
                        CrstHolder ch(&(Table(*pTable)->Lock));
                        uCount += HndCountHandles(*pTable);
                    }
                else
                    for (; pTable != pLastTable; ++pTable)
                        uCount += HndCountHandles(*pTable);
            }
        }

        offset = nextOffset;
        walk = walk->pNext;
    }

    //return the total number of handles in all HandleTables
    return uCount;
}

#ifndef FEATURE_REDHAWK
BOOL  Ref_HandleAsyncPinHandles()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        if (GetThread()) {MODE_COOPERATIVE;} else {DISABLED(MODE_COOPERATIVE);}
    }
    CONTRACTL_END;

    HandleTableBucket *pBucket = g_HandleTableMap.pBuckets[0];
    BOOL result = FALSE;
    int limit = getNumberOfSlots();
    for (int n = 0; n < limit; n ++ )
    {
        if (TableHandleAsyncPinHandles(Table(pBucket->pTable[n])))
        {
            result = TRUE;
        }
    }

    return result;
}

void  Ref_RelocateAsyncPinHandles(HandleTableBucket *pSource, HandleTableBucket *pTarget)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    int limit = getNumberOfSlots();
    for (int n = 0; n < limit; n ++ )
    {
        TableRelocateAsyncPinHandles(Table(pSource->pTable[n]), Table(pTarget->pTable[n]));
    }
}
#endif // !FEATURE_REDHAWK

BOOL Ref_ContainHandle(HandleTableBucket *pBucket, OBJECTHANDLE handle)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    int limit = getNumberOfSlots();
    for (int n = 0; n < limit; n ++ )
    {
        if (TableContainHandle(Table(pBucket->pTable[n]), handle))
            return TRUE;
    }

    return FALSE;
}
/*--------------------------------------------------------------------------*/



/****************************************************************************
 *
 * DEBUG SCANNING STATISTICS
 *
 ****************************************************************************/
#ifdef _DEBUG

void DEBUG_PostGCScanHandler(HandleTable *pTable, const uint32_t *types, uint32_t typeCount, uint32_t condemned, uint32_t maxgen, ScanCallbackInfo *info)
{
    LIMITED_METHOD_CONTRACT;
    UNREFERENCED_PARAMETER(types);

    // looks like the GC supports more generations than we expected
    _ASSERTE(condemned < MAXSTATGEN);

    // remember the highest generation we've seen
    if (pTable->_DEBUG_iMaxGen < (int)condemned)
        pTable->_DEBUG_iMaxGen = (int)condemned;

    // update the statistics
    pTable->_DEBUG_TotalBlocksScanned                [condemned] += info->DEBUG_BlocksScanned;
    pTable->_DEBUG_TotalBlocksScannedNonTrivially    [condemned] += info->DEBUG_BlocksScannedNonTrivially;
    pTable->_DEBUG_TotalHandleSlotsScanned           [condemned] += info->DEBUG_HandleSlotsScanned;
    pTable->_DEBUG_TotalHandlesActuallyScanned       [condemned] += info->DEBUG_HandlesActuallyScanned;

    // if this is an ephemeral GC then dump ephemeral stats for this scan right now
    if (condemned < maxgen)
    {
        // dump a header for the stats with the condemned generation number
        LOG((LF_GC, LL_INFO1000, "--------------------------------------------------------------\n"));
        LOG((LF_GC, LL_INFO1000, "Ephemeral Handle Scan Summary:\n"));
        LOG((LF_GC, LL_INFO1000, "    Generation            = %u\n", condemned));

        // dump the handle types we were asked to scan
        LOG((LF_GC, LL_INFO1000, "    Handle Type(s)        = %u", *types));
        for (uint32_t u = 1; u < typeCount; u++)
            LOG((LF_GC, LL_INFO1000, ",%u", types[u]));
        LOG((LF_GC, LL_INFO1000,  "\n"));

        // dump the number of blocks and slots we scanned
        uint32_t blockHandles = info->DEBUG_BlocksScanned * HANDLE_HANDLES_PER_BLOCK;
        LOG((LF_GC, LL_INFO1000, "    Blocks Scanned        = %u (%u slots)\n", info->DEBUG_BlocksScanned, blockHandles));

        // if we scanned any blocks then summarize some stats
        if (blockHandles)
        {
            uint32_t nonTrivialBlockHandles = info->DEBUG_BlocksScannedNonTrivially * HANDLE_HANDLES_PER_BLOCK;
            LOG((LF_GC, LL_INFO1000, "    Blocks Examined       = %u (%u slots)\n", info->DEBUG_BlocksScannedNonTrivially, nonTrivialBlockHandles));

            LOG((LF_GC, LL_INFO1000, "    Slots Scanned         = %u\n", info->DEBUG_HandleSlotsScanned));
            LOG((LF_GC, LL_INFO1000, "    Handles Scanned       = %u\n", info->DEBUG_HandlesActuallyScanned));

            double scanRatio = ((double)info->DEBUG_HandlesActuallyScanned / (double)blockHandles) * 100.0;

            LOG((LF_GC, LL_INFO1000, "    Handle Scanning Ratio = %1.1lf%%\n", scanRatio));
        }

        // dump a footer for the stats
        LOG((LF_GC, LL_INFO1000, "--------------------------------------------------------------\n"));
    }
}

void DEBUG_LogScanningStatistics(HandleTable *pTable, uint32_t level)
{
    WRAPPER_NO_CONTRACT;
    UNREFERENCED_PARAMETER(level);

    // have we done any GC's yet?
    if (pTable->_DEBUG_iMaxGen >= 0)
    {
        // dump a header for the stats
        LOG((LF_GC, level, "\n==============================================================\n"));
        LOG((LF_GC, level, " Cumulative Handle Scan Summary:\n"));

        // for each generation we've collected,  dump the current stats
        for (int i = 0; i <= pTable->_DEBUG_iMaxGen; i++)
        {
            int64_t totalBlocksScanned = pTable->_DEBUG_TotalBlocksScanned[i];

            // dump the generation number and the number of blocks scanned
            LOG((LF_GC, level,     "--------------------------------------------------------------\n"));
            LOG((LF_GC, level,     "    Condemned Generation      = %d\n", i));
            LOG((LF_GC, level,     "    Blocks Scanned            = %I64u\n", totalBlocksScanned));

            // if we scanned any blocks in this generation then dump some interesting numbers
            if (totalBlocksScanned)
            {
                LOG((LF_GC, level, "    Blocks Examined           = %I64u\n", pTable->_DEBUG_TotalBlocksScannedNonTrivially[i]));
                LOG((LF_GC, level, "    Slots Scanned             = %I64u\n", pTable->_DEBUG_TotalHandleSlotsScanned       [i]));
                LOG((LF_GC, level, "    Handles Scanned           = %I64u\n", pTable->_DEBUG_TotalHandlesActuallyScanned   [i]));

                double blocksScanned  = (double) totalBlocksScanned;
                double blocksExamined = (double) pTable->_DEBUG_TotalBlocksScannedNonTrivially[i];
                double slotsScanned   = (double) pTable->_DEBUG_TotalHandleSlotsScanned       [i];
                double handlesScanned = (double) pTable->_DEBUG_TotalHandlesActuallyScanned   [i];
                double totalSlots     = (double) (totalBlocksScanned * HANDLE_HANDLES_PER_BLOCK);

                LOG((LF_GC, level, "    Block Scan Ratio          = %1.1lf%%\n", (100.0 * (blocksExamined / blocksScanned)) ));
                LOG((LF_GC, level, "    Clump Scan Ratio          = %1.1lf%%\n", (100.0 * (slotsScanned   / totalSlots))    ));
                LOG((LF_GC, level, "    Scanned Clump Saturation  = %1.1lf%%\n", (100.0 * (handlesScanned / slotsScanned))  ));
                LOG((LF_GC, level, "    Overall Handle Scan Ratio = %1.1lf%%\n", (100.0 * (handlesScanned / totalSlots))    ));
            }
        }

        // dump a footer for the stats
        LOG((LF_GC, level, "==============================================================\n\n"));
    }
}

#endif // _DEBUG
#endif // !DACCESS_COMPILE


/*--------------------------------------------------------------------------*/


