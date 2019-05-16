// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
 * Wraps handle table to implement various handle types (Strong, Weak, etc.)
 *

 *
 */

#include "common.h"

#include "gcenv.h"

#include "gc.h"
#include "gcscan.h"

#include "objecthandle.h"
#include "handletablepriv.h"

#include "gchandletableimpl.h"

HandleTableMap g_HandleTableMap;

// Array of contexts used while scanning dependent handles for promotion. There are as many contexts as GC
// heaps and they're allocated by Ref_Initialize and initialized during each GC by GcDhInitialScan.
DhContext *g_pDependentHandleContexts;

#ifndef DACCESS_COMPILE

//----------------------------------------------------------------------------

/*
 * struct VARSCANINFO
 *
 * used when tracing variable-strength handles.
 */
struct VARSCANINFO
{
    uintptr_t      lEnableMask; // mask of types to trace
    HANDLESCANPROC pfnTrace;    // tracing function to use
    uintptr_t      lp2;         // second parameter
};


//----------------------------------------------------------------------------

/*
 * Scan callback for tracing variable-strength handles.
 *
 * This callback is called to trace individual objects referred to by handles
 * in the variable-strength table.
 */
void CALLBACK VariableTraceDispatcher(_UNCHECKED_OBJECTREF *pObjRef, uintptr_t *pExtraInfo, uintptr_t lp1, uintptr_t lp2)
{
    WRAPPER_NO_CONTRACT;

    // lp2 is a pointer to our VARSCANINFO
    struct VARSCANINFO *pInfo = (struct VARSCANINFO *)lp2;

    // is the handle's dynamic type one we're currently scanning?
    if ((*pExtraInfo & pInfo->lEnableMask) != 0)
    {
        // yes - call the tracing function for this handle
        pInfo->pfnTrace(pObjRef, NULL, lp1, pInfo->lp2);
    }
}

#if defined(FEATURE_COMINTEROP) || defined(FEATURE_REDHAWK)
/*
 * Scan callback for tracing ref-counted handles.
 *
 * This callback is called to trace individual objects referred to by handles
 * in the refcounted table.
 */
void CALLBACK PromoteRefCounted(_UNCHECKED_OBJECTREF *pObjRef, uintptr_t *pExtraInfo, uintptr_t lp1, uintptr_t lp2)
{
    WRAPPER_NO_CONTRACT;
    UNREFERENCED_PARAMETER(pExtraInfo);

    // there are too many races when asychnronously scanning ref-counted handles so we no longer support it
    _ASSERTE(!((ScanContext*)lp1)->concurrent);

    LOG((LF_GC, LL_INFO1000, LOG_HANDLE_OBJECT_CLASS("", pObjRef, "causes promotion of ", *pObjRef)));

    Object *pObj = VolatileLoad((PTR_Object*)pObjRef);

#ifdef _DEBUG
    Object *pOldObj = pObj;
#endif

    if (!HndIsNullOrDestroyedHandle(pObj) && !g_theGCHeap->IsPromoted(pObj))
    {
        if (GCToEEInterface::RefCountedHandleCallbacks(pObj))
        {
            _ASSERTE(lp2);
            promote_func* callback = (promote_func*) lp2;
            callback(&pObj, (ScanContext *)lp1, 0);
        }
    }
    
    // Assert this object wasn't relocated since we are passing a temporary object's address.
    _ASSERTE(pOldObj == pObj);
}
#endif // FEATURE_COMINTEROP || FEATURE_REDHAWK


// Only used by profiling/ETW.
//----------------------------------------------------------------------------

/*
 * struct DIAG_DEPSCANINFO
 *
 * used when tracing dependent handles for profiling/ETW.
 */
struct DIAG_DEPSCANINFO
{
    HANDLESCANPROC pfnTrace;    // tracing function to use
    uintptr_t      pfnProfilingOrETW;
};

void CALLBACK TraceDependentHandle(_UNCHECKED_OBJECTREF *pObjRef, uintptr_t *pExtraInfo, uintptr_t lp1, uintptr_t lp2)
{
    WRAPPER_NO_CONTRACT;

    if (pObjRef == NULL || pExtraInfo == NULL)
        return;

    // At this point, it's possible that either or both of the primary and secondary
    // objects are NULL.  However, if the secondary object is non-NULL, then the primary
    // object should also be non-NULL.
    _ASSERTE(*pExtraInfo == 0 || *pObjRef != NULL);

    struct DIAG_DEPSCANINFO *pInfo = (struct DIAG_DEPSCANINFO*)lp2;

    HANDLESCANPROC pfnTrace = pInfo->pfnTrace;

    // is the handle's secondary object non-NULL?
    if ((*pObjRef != NULL) && (*pExtraInfo != 0))
    {
        // yes - call the tracing function for this handle
        pfnTrace(pObjRef, NULL, lp1, (uintptr_t)(pInfo->pfnProfilingOrETW));
    }
}

void CALLBACK UpdateDependentHandle(_UNCHECKED_OBJECTREF *pObjRef, uintptr_t *pExtraInfo, uintptr_t lp1, uintptr_t lp2)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(pExtraInfo);

    Object **pPrimaryRef = (Object **)pObjRef;
    Object **pSecondaryRef = (Object **)pExtraInfo;
  
    LOG((LF_GC|LF_ENC, LL_INFO10000, LOG_HANDLE_OBJECT("Querying for new location of ", 
            pPrimaryRef, "to ", *pPrimaryRef)));
    LOG((LF_GC|LF_ENC, LL_INFO10000, LOG_HANDLE_OBJECT(" and ", 
            pSecondaryRef, "to ", *pSecondaryRef)));

#ifdef _DEBUG
    Object *pOldPrimary = *pPrimaryRef;
    Object *pOldSecondary = *pSecondaryRef;
#endif

	_ASSERTE(lp2);
	promote_func* callback = (promote_func*) lp2;
	callback(pPrimaryRef, (ScanContext *)lp1, 0);
	callback(pSecondaryRef, (ScanContext *)lp1, 0);

#ifdef _DEBUG
    if (pOldPrimary != *pPrimaryRef)
        LOG((LF_GC|LF_ENC, LL_INFO10000,  "Updating " FMT_HANDLE "from" FMT_ADDR "to " FMT_OBJECT "\n", 
             DBG_ADDR(pPrimaryRef), DBG_ADDR(pOldPrimary), DBG_ADDR(*pPrimaryRef)));
    else
        LOG((LF_GC|LF_ENC, LL_INFO10000, "Updating " FMT_HANDLE "- " FMT_OBJECT "did not move\n", 
             DBG_ADDR(pPrimaryRef), DBG_ADDR(*pPrimaryRef)));
    if (pOldSecondary != *pSecondaryRef)
        LOG((LF_GC|LF_ENC, LL_INFO10000,  "Updating " FMT_HANDLE "from" FMT_ADDR "to " FMT_OBJECT "\n", 
             DBG_ADDR(pSecondaryRef), DBG_ADDR(pOldSecondary), DBG_ADDR(*pSecondaryRef)));
    else
        LOG((LF_GC|LF_ENC, LL_INFO10000, "Updating " FMT_HANDLE "- " FMT_OBJECT "did not move\n", 
             DBG_ADDR(pSecondaryRef), DBG_ADDR(*pSecondaryRef)));
#endif
}

void CALLBACK PromoteDependentHandle(_UNCHECKED_OBJECTREF *pObjRef, uintptr_t *pExtraInfo, uintptr_t lp1, uintptr_t lp2)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(pExtraInfo);
    
    Object **pPrimaryRef = (Object **)pObjRef;
    Object **pSecondaryRef = (Object **)pExtraInfo;
    LOG((LF_GC|LF_ENC, LL_INFO1000, "Checking promotion of DependentHandle"));
    LOG((LF_GC|LF_ENC, LL_INFO1000, LOG_HANDLE_OBJECT_CLASS("\tPrimary:\t", pObjRef, "to ", *pObjRef)));
    LOG((LF_GC|LF_ENC, LL_INFO1000, LOG_HANDLE_OBJECT_CLASS("\tSecondary\t", pSecondaryRef, "to ", *pSecondaryRef)));

    ScanContext *sc = (ScanContext*)lp1;
    DhContext *pDhContext = Ref_GetDependentHandleContext(sc);

    if (*pObjRef && g_theGCHeap->IsPromoted(*pPrimaryRef))
    {
        if (!g_theGCHeap->IsPromoted(*pSecondaryRef))
        {
            LOG((LF_GC|LF_ENC, LL_INFO10000, "\tPromoting secondary " LOG_OBJECT_CLASS(*pSecondaryRef)));
            _ASSERTE(lp2);
            promote_func* callback = (promote_func*) lp2;
            callback(pSecondaryRef, (ScanContext *)lp1, 0);
            // need to rescan because we might have promoted an object that itself has added fields and this
            // promotion might be all that is pinning that object. If we've already scanned that dependent
            // handle relationship, we could lose it secondary object.
            pDhContext->m_fPromoted = true;
        }
    }
    else if (*pObjRef)
    {
        // If we see a non-cleared primary which hasn't been promoted, record the fact. We will only require a
        // rescan if this flag has been set (if it's clear then the previous scan found only clear and
        // promoted handles, so there's no chance of finding an additional handle being promoted on a
        // subsequent scan).
        pDhContext->m_fUnpromotedPrimaries = true;
    }
}
    
void CALLBACK ClearDependentHandle(_UNCHECKED_OBJECTREF *pObjRef, uintptr_t *pExtraInfo, uintptr_t /*lp1*/, uintptr_t /*lp2*/)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(pExtraInfo);

    Object **pPrimaryRef = (Object **)pObjRef;
    Object **pSecondaryRef = (Object **)pExtraInfo;
    LOG((LF_GC|LF_ENC, LL_INFO1000, "Checking referent of DependentHandle"));
    LOG((LF_GC|LF_ENC, LL_INFO1000, LOG_HANDLE_OBJECT_CLASS("\tPrimary:\t", pPrimaryRef, "to ", *pPrimaryRef)));
    LOG((LF_GC|LF_ENC, LL_INFO1000, LOG_HANDLE_OBJECT_CLASS("\tSecondary\t", pSecondaryRef, "to ", *pSecondaryRef)));

    if (!g_theGCHeap->IsPromoted(*pPrimaryRef))
    {
        LOG((LF_GC|LF_ENC, LL_INFO1000, "\tunreachable ", LOG_OBJECT_CLASS(*pPrimaryRef)));
        LOG((LF_GC|LF_ENC, LL_INFO1000, "\tunreachable ", LOG_OBJECT_CLASS(*pSecondaryRef)));
        *pPrimaryRef = NULL;
        *pSecondaryRef = NULL;
    }
    else
    {
        _ASSERTE(g_theGCHeap->IsPromoted(*pSecondaryRef));
        LOG((LF_GC|LF_ENC, LL_INFO10000, "\tPrimary is reachable " LOG_OBJECT_CLASS(*pPrimaryRef)));
        LOG((LF_GC|LF_ENC, LL_INFO10000, "\tSecondary is reachable " LOG_OBJECT_CLASS(*pSecondaryRef)));
    }
}

/*
 * Scan callback for pinning handles.
 *
 * This callback is called to pin individual objects referred to by handles in
 * the pinning table.
 */
void CALLBACK PinObject(_UNCHECKED_OBJECTREF *pObjRef, uintptr_t *pExtraInfo, uintptr_t lp1, uintptr_t lp2)
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_MODE_COOPERATIVE;
    UNREFERENCED_PARAMETER(pExtraInfo);

    // PINNING IS BAD - DON'T DO IT IF YOU CAN AVOID IT
    LOG((LF_GC, LL_WARNING, LOG_HANDLE_OBJECT_CLASS("WARNING: ", pObjRef, "causes pinning of ", *pObjRef)));

    Object **pRef = (Object **)pObjRef;
    _ASSERTE(lp2);
    promote_func* callback = (promote_func*) lp2;
    callback(pRef, (ScanContext *)lp1, GC_CALL_PINNED);
}

void CALLBACK AsyncPinObject(_UNCHECKED_OBJECTREF *pObjRef, uintptr_t *pExtraInfo, uintptr_t lp1, uintptr_t lp2)
{
    UNREFERENCED_PARAMETER(pExtraInfo);

    LOG((LF_GC, LL_WARNING, LOG_HANDLE_OBJECT_CLASS("WARNING: ", pObjRef, "causes (async) pinning of ", *pObjRef)));

    Object **pRef = (Object **)pObjRef;
    _ASSERTE(lp2);
    promote_func* callback = (promote_func*)lp2;
    callback(pRef, (ScanContext *)lp1, 0);
    Object* pPinnedObj = *pRef;
    if (!HndIsNullOrDestroyedHandle(pPinnedObj))
    {
        GCToEEInterface::WalkAsyncPinnedForPromotion(pPinnedObj, (ScanContext *)lp1, callback);
    }
}


/*
 * Scan callback for tracing strong handles.
 *
 * This callback is called to trace individual objects referred to by handles
 * in the strong table.
 */
void CALLBACK PromoteObject(_UNCHECKED_OBJECTREF *pObjRef, uintptr_t *pExtraInfo, uintptr_t lp1, uintptr_t lp2)
{
    WRAPPER_NO_CONTRACT;
    UNREFERENCED_PARAMETER(pExtraInfo);

    LOG((LF_GC, LL_INFO1000, LOG_HANDLE_OBJECT_CLASS("", pObjRef, "causes promotion of ", *pObjRef)));

    Object **ppRef = (Object **)pObjRef;
    _ASSERTE(lp2);
    promote_func* callback = (promote_func*) lp2;
    callback(ppRef, (ScanContext *)lp1, 0);
}


/*
 * Scan callback for disconnecting dead handles.
 *
 * This callback is called to check promotion of individual objects referred to by
 * handles in the weak tables.
 */
void CALLBACK CheckPromoted(_UNCHECKED_OBJECTREF *pObjRef, uintptr_t *pExtraInfo, uintptr_t lp1, uintptr_t lp2)
{
    WRAPPER_NO_CONTRACT;
    UNREFERENCED_PARAMETER(pExtraInfo);
    UNREFERENCED_PARAMETER(lp1);
    UNREFERENCED_PARAMETER(lp2);

    LOG((LF_GC, LL_INFO100000, LOG_HANDLE_OBJECT_CLASS("Checking referent of Weak-", pObjRef, "to ", *pObjRef)));

    Object **ppRef = (Object **)pObjRef;
    if (!g_theGCHeap->IsPromoted(*ppRef))
    {
        LOG((LF_GC, LL_INFO100, LOG_HANDLE_OBJECT_CLASS("Severing Weak-", pObjRef, "to unreachable ", *pObjRef)));

        *ppRef = NULL;
    }
    else
    {
        LOG((LF_GC, LL_INFO1000000, "reachable " LOG_OBJECT_CLASS(*pObjRef)));
    }
}

void CALLBACK CalculateSizedRefSize(_UNCHECKED_OBJECTREF *pObjRef, uintptr_t *pExtraInfo, uintptr_t lp1, uintptr_t lp2)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(pExtraInfo);
    
    Object **ppSizedRef = (Object **)pObjRef;
    size_t* pSize = (size_t *)pExtraInfo;
    LOG((LF_GC, LL_INFO100000, LOG_HANDLE_OBJECT_CLASS("Getting size of referent of SizedRef-", pObjRef, "to ", *pObjRef)));

    ScanContext* sc = (ScanContext *)lp1;
    promote_func* callback = (promote_func*) lp2;

    size_t sizeBegin = g_theGCHeap->GetPromotedBytes(sc->thread_number);
    callback(ppSizedRef, (ScanContext *)lp1, 0);
    size_t sizeEnd = g_theGCHeap->GetPromotedBytes(sc->thread_number);
    *pSize = sizeEnd - sizeBegin;
}

/*
 * Scan callback for updating pointers.
 *
 * This callback is called to update pointers for individual objects referred to by
 * handles in the weak and strong tables.
 */
void CALLBACK UpdatePointer(_UNCHECKED_OBJECTREF *pObjRef, uintptr_t *pExtraInfo, uintptr_t lp1, uintptr_t lp2)
{
    LIMITED_METHOD_CONTRACT;
    UNREFERENCED_PARAMETER(pExtraInfo);

    LOG((LF_GC, LL_INFO100000, LOG_HANDLE_OBJECT("Querying for new location of ", pObjRef, "to ", *pObjRef)));

    Object **ppRef = (Object **)pObjRef;

#ifdef _DEBUG
    Object *pOldLocation = *ppRef;
#endif

    _ASSERTE(lp2);
    promote_func* callback = (promote_func*) lp2;
    callback(ppRef, (ScanContext *)lp1, 0);

#ifdef _DEBUG
    if (pOldLocation != *pObjRef)
        LOG((LF_GC, LL_INFO10000,  "Updating " FMT_HANDLE "from" FMT_ADDR "to " FMT_OBJECT "\n", 
             DBG_ADDR(pObjRef), DBG_ADDR(pOldLocation), DBG_ADDR(*pObjRef)));
    else
        LOG((LF_GC, LL_INFO100000, "Updating " FMT_HANDLE "- " FMT_OBJECT "did not move\n", 
             DBG_ADDR(pObjRef), DBG_ADDR(*pObjRef)));
#endif
}


#if defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)
/*
 * Scan callback for updating pointers.
 *
 * This callback is called to update pointers for individual objects referred to by
 * handles in the weak and strong tables.
 */
void CALLBACK ScanPointerForProfilerAndETW(_UNCHECKED_OBJECTREF *pObjRef, uintptr_t *pExtraInfo, uintptr_t lp1, uintptr_t lp2)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;
    UNREFERENCED_PARAMETER(pExtraInfo);
    handle_scan_fn fn = (handle_scan_fn)lp2;

    LOG((LF_GC | LF_CORPROF, LL_INFO100000, LOG_HANDLE_OBJECT_CLASS("Notifying profiler of ", pObjRef, "to ", *pObjRef)));

    // Get the baseobject (which can subsequently be cast into an OBJECTREF == ObjectID
    Object **pRef = (Object **)pObjRef;

    // Get a hold of the heap ID that's tacked onto the end of the scancontext struct.
    ScanContext *pSC = (ScanContext *)lp1;

    uint32_t rootFlags = 0;
    bool isDependent = false;

    OBJECTHANDLE handle = (OBJECTHANDLE)(pRef);
    switch (HandleFetchType(handle))
    {
    case    HNDTYPE_DEPENDENT:
        isDependent = true;
        break;
    case    HNDTYPE_WEAK_SHORT:
    case    HNDTYPE_WEAK_LONG:
#ifdef FEATURE_COMINTEROP
    case    HNDTYPE_WEAK_WINRT:
#endif // FEATURE_COMINTEROP
        rootFlags |= kEtwGCRootFlagsWeakRef;
        break;

    case    HNDTYPE_STRONG:
    case    HNDTYPE_SIZEDREF:
        break;

    case    HNDTYPE_PINNED:
    case    HNDTYPE_ASYNCPINNED:
        rootFlags |= kEtwGCRootFlagsPinning;
        break;

    case    HNDTYPE_VARIABLE:
#ifdef FEATURE_REDHAWK
    {
        // Set the appropriate ETW flags for the current strength of this variable handle
        uint32_t nVarHandleType = GetVariableHandleType(handle);
        if (((nVarHandleType & VHT_WEAK_SHORT) != 0) ||
            ((nVarHandleType & VHT_WEAK_LONG) != 0))
        {
            rootFlags |= kEtwGCRootFlagsWeakRef;
        }
        if ((nVarHandleType & VHT_PINNED) != 0)
        {
            rootFlags |= kEtwGCRootFlagsPinning;
        }

        // No special ETW flag for strong handles (VHT_STRONG)
    }
#else
        _ASSERTE(!"Variable handle encountered");
#endif
        break;

#if defined(FEATURE_COMINTEROP) && !defined(FEATURE_REDHAWK)
    case    HNDTYPE_REFCOUNTED:
        rootFlags |= kEtwGCRootFlagsRefCounted;
        if (*pRef != NULL)
        {
            if (!GCToEEInterface::RefCountedHandleCallbacks(*pRef))
                rootFlags |= kEtwGCRootFlagsWeakRef;
        }
        break;
#endif // FEATURE_COMINTEROP || FEATURE_REDHAWK
    }

    _UNCHECKED_OBJECTREF pSec = NULL;

    if (isDependent)
    {
        pSec = (_UNCHECKED_OBJECTREF)HndGetHandleExtraInfo(handle);
    }

    fn(pRef, pSec, rootFlags, pSC, isDependent);
}
#endif // defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)

/*
 * Scan callback for updating pointers.
 *
 * This callback is called to update pointers for individual objects referred to by
 * handles in the pinned table.
 */
void CALLBACK UpdatePointerPinned(_UNCHECKED_OBJECTREF *pObjRef, uintptr_t *pExtraInfo, uintptr_t lp1, uintptr_t lp2)
{
    LIMITED_METHOD_CONTRACT;
    UNREFERENCED_PARAMETER(pExtraInfo);

    Object **ppRef = (Object **)pObjRef;

    _ASSERTE(lp2);
    promote_func* callback = (promote_func*) lp2;
    callback(ppRef, (ScanContext *)lp1, GC_CALL_PINNED);
    
    LOG((LF_GC, LL_INFO100000, LOG_HANDLE_OBJECT("Updating ", pObjRef, "to pinned ", *pObjRef)));
}


//----------------------------------------------------------------------------

// flags describing the handle types
static const uint32_t s_rgTypeFlags[] =
{
    HNDF_NORMAL,    // HNDTYPE_WEAK_SHORT
    HNDF_NORMAL,    // HNDTYPE_WEAK_LONG
    HNDF_NORMAL,    // HNDTYPE_STRONG
    HNDF_NORMAL,    // HNDTYPE_PINNED
    HNDF_EXTRAINFO, // HNDTYPE_VARIABLE
    HNDF_NORMAL,    // HNDTYPE_REFCOUNTED
    HNDF_EXTRAINFO, // HNDTYPE_DEPENDENT
    HNDF_NORMAL,    // HNDTYPE_ASYNCPINNED
    HNDF_EXTRAINFO, // HNDTYPE_SIZEDREF
    HNDF_EXTRAINFO, // HNDTYPE_WEAK_WINRT
};

int getNumberOfSlots()
{
    WRAPPER_NO_CONTRACT;

    // when Ref_Initialize called, IGCHeap::GetNumberOfHeaps() is still 0, so use #procs as a workaround
    // it is legal since even if later #heaps < #procs we create handles by thread home heap
    // and just have extra unused slots in HandleTableBuckets, which does not take a lot of space
    if (!IsServerHeap())
        return 1;

    return GCToOSInterface::GetTotalProcessorCount();
}

class HandleTableBucketHolder
{
private:
    HandleTableBucket* m_bucket;
    int m_slots;
    BOOL m_SuppressRelease;
public:
    HandleTableBucketHolder(HandleTableBucket* bucket, int slots);
    ~HandleTableBucketHolder();

    void SuppressRelease()
    {
        m_SuppressRelease = TRUE;
    }
};

HandleTableBucketHolder::HandleTableBucketHolder(HandleTableBucket* bucket, int slots)
    :m_bucket(bucket), m_slots(slots), m_SuppressRelease(FALSE)
{
}

HandleTableBucketHolder::~HandleTableBucketHolder()
{
    if (m_SuppressRelease)
    {
        return;
    }
    if (m_bucket->pTable)
    {
        for (int n = 0; n < m_slots; n ++)
        {
            if (m_bucket->pTable[n])
            {
                HndDestroyHandleTable(m_bucket->pTable[n]);
            }
        }
        delete [] m_bucket->pTable;
    }

    // we do not own m_bucket, so we shouldn't delete it here.
}

bool Ref_Initialize()
{
    CONTRACTL
    {
        NOTHROW;
        WRAPPER(GC_NOTRIGGER);
        INJECT_FAULT(return false);
    }
    CONTRACTL_END;

    // sanity
    _ASSERTE(g_HandleTableMap.pBuckets == NULL);

    // Create an array of INITIAL_HANDLE_TABLE_ARRAY_SIZE HandleTableBuckets to hold the handle table sets
    HandleTableBucket** pBuckets = new (nothrow) HandleTableBucket * [ INITIAL_HANDLE_TABLE_ARRAY_SIZE ];
    if (pBuckets == NULL)
        return false;

    ZeroMemory(pBuckets, INITIAL_HANDLE_TABLE_ARRAY_SIZE * sizeof (HandleTableBucket *));

    g_gcGlobalHandleStore = new (nothrow) GCHandleStore();
    if (g_gcGlobalHandleStore == NULL)
    {
        delete[] pBuckets;
        return false;
    }

    // Initialize the bucket in the global handle store
    HandleTableBucket* pBucket = &g_gcGlobalHandleStore->_underlyingBucket;

    pBucket->HandleTableIndex = 0;

    int n_slots = getNumberOfSlots();

    HandleTableBucketHolder bucketHolder(pBucket, n_slots);

    // create the handle table set for the first bucket
    pBucket->pTable = new (nothrow) HHANDLETABLE[n_slots];
    if (pBucket->pTable == NULL)
        goto CleanupAndFail;

    ZeroMemory(pBucket->pTable,
        n_slots * sizeof(HHANDLETABLE));
    for (int uCPUindex = 0; uCPUindex < n_slots; uCPUindex++)
    {
        pBucket->pTable[uCPUindex] = HndCreateHandleTable(s_rgTypeFlags, _countof(s_rgTypeFlags));
        if (pBucket->pTable[uCPUindex] == NULL)
            goto CleanupAndFail;

        HndSetHandleTableIndex(pBucket->pTable[uCPUindex], 0);
    }

    pBuckets[0] = pBucket;
    bucketHolder.SuppressRelease();

    g_HandleTableMap.pBuckets = pBuckets;
    g_HandleTableMap.dwMaxIndex = INITIAL_HANDLE_TABLE_ARRAY_SIZE;
    g_HandleTableMap.pNext = NULL;

    // Allocate contexts used during dependent handle promotion scanning. There's one of these for every GC
    // heap since they're scanned in parallel.
    g_pDependentHandleContexts = new (nothrow) DhContext[n_slots];
    if (g_pDependentHandleContexts == NULL)
        goto CleanupAndFail;

    return true;

CleanupAndFail:
    if (pBuckets != NULL)
        delete[] pBuckets;

    if (g_gcGlobalHandleStore != NULL)
        delete g_gcGlobalHandleStore;

    return false;
}

void Ref_Shutdown()
{
    WRAPPER_NO_CONTRACT;

    if (g_pDependentHandleContexts)
    {
        delete [] g_pDependentHandleContexts;
        g_pDependentHandleContexts = NULL;
    }

    // are there any handle tables?
    if (g_HandleTableMap.pBuckets)
    {
        // don't destroy any of the indexed handle tables; they should
        // be destroyed externally.

        // destroy the handle table bucket array
        HandleTableMap *walk = &g_HandleTableMap;
        while (walk) {
            delete [] walk->pBuckets;
            walk = walk->pNext;
        }

        // null out the handle table array
        g_HandleTableMap.pNext = NULL;
        g_HandleTableMap.dwMaxIndex = 0;

        // null out the global table handle
        g_HandleTableMap.pBuckets = NULL;
    }
}

#ifndef FEATURE_REDHAWK
bool Ref_InitializeHandleTableBucket(HandleTableBucket* bucket)
{
    CONTRACTL
    {
        NOTHROW;
        WRAPPER(GC_TRIGGERS);
        INJECT_FAULT(return false);
    }
    CONTRACTL_END;

    HandleTableBucket *result = bucket;
    HandleTableMap *walk = &g_HandleTableMap;

    HandleTableMap *last = NULL;
    uint32_t offset = 0;

    result->pTable = NULL;

    // create handle table set for the bucket
    int n_slots = getNumberOfSlots();

    HandleTableBucketHolder bucketHolder(result, n_slots);

    result->pTable = new (nothrow) HHANDLETABLE[n_slots];
    if (!result->pTable)
    {
        return false;
    }

    ZeroMemory(result->pTable, n_slots * sizeof(HHANDLETABLE));

    for (int uCPUindex=0; uCPUindex < n_slots; uCPUindex++) {
        result->pTable[uCPUindex] = HndCreateHandleTable(s_rgTypeFlags, _countof(s_rgTypeFlags));
        if (!result->pTable[uCPUindex])
            return false;
    }

    for (;;) {
        // Do we have free slot
        while (walk) {
            for (uint32_t i = 0; i < INITIAL_HANDLE_TABLE_ARRAY_SIZE; i ++) {
                if (walk->pBuckets[i] == 0) {
                    for (int uCPUindex=0; uCPUindex < n_slots; uCPUindex++)
                        HndSetHandleTableIndex(result->pTable[uCPUindex], i+offset);

                    result->HandleTableIndex = i+offset;
                    if (Interlocked::CompareExchangePointer(&walk->pBuckets[i], result, NULL) == 0) {
                        // Get a free slot.
                        bucketHolder.SuppressRelease();
                        return true;
                    }
                }
            }
            last = walk;
            offset = walk->dwMaxIndex;
            walk = walk->pNext;
        }

        // No free slot.
        // Let's create a new node
        HandleTableMap *newMap = new (nothrow) HandleTableMap;
        if (!newMap)
        {
            return false;
        }

        newMap->pBuckets = new (nothrow) HandleTableBucket * [ INITIAL_HANDLE_TABLE_ARRAY_SIZE ];
        if (!newMap->pBuckets)
        {
            delete newMap;
            return false;
        }

        newMap->dwMaxIndex = last->dwMaxIndex + INITIAL_HANDLE_TABLE_ARRAY_SIZE;
        newMap->pNext = NULL;
        ZeroMemory(newMap->pBuckets,
                INITIAL_HANDLE_TABLE_ARRAY_SIZE * sizeof (HandleTableBucket *));

        if (Interlocked::CompareExchangePointer(&last->pNext, newMap, NULL) != NULL) 
        {
            // This thread loses.
            delete [] newMap->pBuckets;
            delete newMap;
        }
        walk = last->pNext;
        offset = last->dwMaxIndex;
    }
}
#endif // !FEATURE_REDHAWK

void Ref_RemoveHandleTableBucket(HandleTableBucket *pBucket)
{
    LIMITED_METHOD_CONTRACT;

    size_t          index   = pBucket->HandleTableIndex;
    HandleTableMap* walk    = &g_HandleTableMap;
    size_t          offset  = 0;

    while (walk) 
    {
        if ((index < walk->dwMaxIndex) && (index >= offset)) 
        {
            // During AppDomain unloading, we first remove a handle table and then destroy
            // the table.  As soon as the table is removed, the slot can be reused.
            if (walk->pBuckets[index - offset] == pBucket)
            {
                walk->pBuckets[index - offset] = NULL;
                return;
            }
        }
        offset = walk->dwMaxIndex;
        walk   = walk->pNext;
    }

    // Didn't find it.  This will happen typically from Ref_DestroyHandleTableBucket if 
    // we explicitly call Ref_RemoveHandleTableBucket first.
}


void Ref_DestroyHandleTableBucket(HandleTableBucket *pBucket)
{
    WRAPPER_NO_CONTRACT;

    Ref_RemoveHandleTableBucket(pBucket);
    for (int uCPUindex=0; uCPUindex < getNumberOfSlots(); uCPUindex++)
    {
        HndDestroyHandleTable(pBucket->pTable[uCPUindex]);
    }
    delete [] pBucket->pTable;
}

int getSlotNumber(ScanContext* sc)
{
    WRAPPER_NO_CONTRACT;

    return (IsServerHeap() ? sc->thread_number : 0);
}

// <TODO> - reexpress as complete only like hndtable does now!!! -fmh</REVISIT_TODO>
void Ref_EndSynchronousGC(uint32_t condemned, uint32_t maxgen)
{
    LIMITED_METHOD_CONTRACT;
    UNREFERENCED_PARAMETER(condemned);
    UNREFERENCED_PARAMETER(maxgen);

// NOT used, must be modified for MTHTS (scalable HandleTable scan) if planned to use:
// need to pass ScanContext info to split HT bucket by threads, or to be performed under t_join::join
/*
    // tell the table we finished a GC
    HandleTableMap *walk = &g_HandleTableMap;
    while (walk) {
        for (uint32_t i = 0; i < INITIAL_HANDLE_TABLE_ARRAY_SIZE; i ++) {
            HHANDLETABLE hTable = walk->pTable[i];
            if (hTable)
                HndNotifyGcCycleComplete(hTable, condemned, maxgen);
        }
        walk = walk->pNext;
    }
*/    
}

void SetDependentHandleSecondary(OBJECTHANDLE handle, OBJECTREF objref)
{ 
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    // sanity
    _ASSERTE(handle);

#ifdef _DEBUG
    // Make sure the objref is valid before it is assigned to a handle
    ValidateAssignObjrefForHandle(objref);
#endif
    // unwrap the objectref we were given
    _UNCHECKED_OBJECTREF value = OBJECTREF_TO_UNCHECKED_OBJECTREF(objref);

    // if we are doing a non-NULL pointer store then invoke the write-barrier
    if (value)
        HndWriteBarrier(handle, objref);

    // store the pointer
    HndSetHandleExtraInfo(handle, HNDTYPE_DEPENDENT, (uintptr_t)value);
}


//----------------------------------------------------------------------------

/*
* GetVariableHandleType.
*
* Retrieves the dynamic type of a variable-strength handle.
*/
uint32_t GetVariableHandleType(OBJECTHANDLE handle)
{
    WRAPPER_NO_CONTRACT;

    return (uint32_t)HndGetHandleExtraInfo(handle);
}

/*
 * UpdateVariableHandleType.
 *
 * Changes the dynamic type of a variable-strength handle.
 *
 * N.B. This routine is not a macro since we do validation in RETAIL.
 * We always validate the type here because it can come from external callers.
 */
void UpdateVariableHandleType(OBJECTHANDLE handle, uint32_t type)
{
    WRAPPER_NO_CONTRACT;

    // verify that we are being asked to set a valid type
    if (!IS_VALID_VHT_VALUE(type))
    {
        // bogus value passed in
        _ASSERTE(FALSE);
        return;
    }

    // <REVISIT_TODO> (francish)  CONCURRENT GC NOTE</REVISIT_TODO>
    //
    // If/when concurrent GC is implemented, we need to make sure variable handles
    // DON'T change type during an asynchronous scan, OR that we properly recover
    // from the change.  Some changes are benign, but for example changing to or
    // from a pinning handle in the middle of a scan would not be fun.
    //

    // store the type in the handle's extra info
    HndSetHandleExtraInfo(handle, HNDTYPE_VARIABLE, (uintptr_t)type);
}

/*
* CompareExchangeVariableHandleType.
*
* Changes the dynamic type of a variable-strength handle. Unlike UpdateVariableHandleType we assume that the
* types have already been validated.
*/
uint32_t CompareExchangeVariableHandleType(OBJECTHANDLE handle, uint32_t oldType, uint32_t newType)
{
    WRAPPER_NO_CONTRACT;

    // verify that we are being asked to get/set valid types
    _ASSERTE(IS_VALID_VHT_VALUE(oldType) && IS_VALID_VHT_VALUE(newType));

    // attempt to store the type in the handle's extra info
    return (uint32_t)HndCompareExchangeHandleExtraInfo(handle, HNDTYPE_VARIABLE, (uintptr_t)oldType, (uintptr_t)newType);
}


/*
 * TraceVariableHandles.
 *
 * Convenience function for tracing variable-strength handles.
 * Wraps HndScanHandlesForGC.
 */
void TraceVariableHandles(HANDLESCANPROC pfnTrace, uintptr_t lp1, uintptr_t lp2, uint32_t uEnableMask, uint32_t condemned, uint32_t maxgen, uint32_t flags)
{
    WRAPPER_NO_CONTRACT;

    // set up to scan variable handles with the specified mask and trace function
    uint32_t               type = HNDTYPE_VARIABLE;
    struct VARSCANINFO info = { (uintptr_t)uEnableMask, pfnTrace, lp2 };

    HandleTableMap *walk = &g_HandleTableMap;
    while (walk) {
        for (uint32_t i = 0; i < INITIAL_HANDLE_TABLE_ARRAY_SIZE; i++)
            if (walk->pBuckets[i] != NULL)
            {
                HHANDLETABLE hTable = walk->pBuckets[i]->pTable[getSlotNumber((ScanContext*) lp1)];
                if (hTable)
                {
                    HndScanHandlesForGC(hTable, VariableTraceDispatcher,
                                        lp1, (uintptr_t)&info, &type, 1, condemned, maxgen, HNDGCF_EXTRAINFO | flags);
                }
            }
        walk = walk->pNext;
    }
}

/*
  loop scan version of TraceVariableHandles for single-thread-managed Ref_* functions
  should be kept in sync with the code above
*/
void TraceVariableHandlesBySingleThread(HANDLESCANPROC pfnTrace, uintptr_t lp1, uintptr_t lp2, uint32_t uEnableMask, uint32_t condemned, uint32_t maxgen, uint32_t flags)
{
    WRAPPER_NO_CONTRACT;

    // set up to scan variable handles with the specified mask and trace function
    uint32_t type = HNDTYPE_VARIABLE;
    struct VARSCANINFO info = { (uintptr_t)uEnableMask, pfnTrace, lp2 };

    HandleTableMap *walk = &g_HandleTableMap;
    while (walk) {
        for (uint32_t i = 0; i < INITIAL_HANDLE_TABLE_ARRAY_SIZE; i ++)
            if (walk->pBuckets[i] != NULL)
            {
                  // this is the one of Ref_* function performed by single thread in MULTI_HEAPS case, so we need to loop through all HT of the bucket
                for (int uCPUindex=0; uCPUindex < getNumberOfSlots(); uCPUindex++)
                {
                   HHANDLETABLE hTable = walk->pBuckets[i]->pTable[uCPUindex];
                    if (hTable)
                        HndScanHandlesForGC(hTable, VariableTraceDispatcher,
                                        lp1, (uintptr_t)&info, &type, 1, condemned, maxgen, HNDGCF_EXTRAINFO | flags);
                }
            }
        walk = walk->pNext;
    }
}

//----------------------------------------------------------------------------

void Ref_TracePinningRoots(uint32_t condemned, uint32_t maxgen, ScanContext* sc, Ref_promote_func* fn)
{
    WRAPPER_NO_CONTRACT;

    LOG((LF_GC, LL_INFO10000, "Pinning referents of pinned handles in generation %u\n", condemned));

    // pin objects pointed to by pinning handles
    uint32_t types[2] = {HNDTYPE_PINNED, HNDTYPE_ASYNCPINNED};
    uint32_t flags = sc->concurrent ? HNDGCF_ASYNC : HNDGCF_NORMAL;

    HandleTableMap *walk = &g_HandleTableMap;
    while (walk) {
        for (uint32_t i = 0; i < INITIAL_HANDLE_TABLE_ARRAY_SIZE; i ++)
            if (walk->pBuckets[i] != NULL)
            {
                HHANDLETABLE hTable = walk->pBuckets[i]->pTable[getSlotNumber((ScanContext*) sc)];
                if (hTable)
                {
                    // Pinned handles and async pinned handles are scanned in separate passes, since async pinned
                    // handles may require a callback into the EE in order to fully trace an async pinned
                    // object's object graph.
                    HndScanHandlesForGC(hTable, PinObject, uintptr_t(sc), uintptr_t(fn), &types[0], 1, condemned, maxgen, flags);
                    HndScanHandlesForGC(hTable, AsyncPinObject, uintptr_t(sc), uintptr_t(fn), &types[1], 1, condemned, maxgen, flags);
                }
            }
        walk = walk->pNext;
    }

    // pin objects pointed to by variable handles whose dynamic type is VHT_PINNED
    TraceVariableHandles(PinObject, uintptr_t(sc), uintptr_t(fn), VHT_PINNED, condemned, maxgen, flags);
}


void Ref_TraceNormalRoots(uint32_t condemned, uint32_t maxgen, ScanContext* sc, Ref_promote_func* fn)
{
    WRAPPER_NO_CONTRACT;

    LOG((LF_GC, LL_INFO10000, "Promoting referents of strong handles in generation %u\n", condemned));

    // promote objects pointed to by strong handles
    // during ephemeral GCs we also want to promote the ones pointed to by sizedref handles.
    uint32_t types[2] = {HNDTYPE_STRONG, HNDTYPE_SIZEDREF};
    uint32_t uTypeCount = (((condemned >= maxgen) && !g_theGCHeap->IsConcurrentGCInProgress()) ? 1 : _countof(types));
    uint32_t flags = (sc->concurrent) ? HNDGCF_ASYNC : HNDGCF_NORMAL;

    HandleTableMap *walk = &g_HandleTableMap;
    while (walk) {
        for (uint32_t i = 0; i < INITIAL_HANDLE_TABLE_ARRAY_SIZE; i ++)
            if (walk->pBuckets[i] != NULL)
            {
                HHANDLETABLE hTable = walk->pBuckets[i]->pTable[getSlotNumber(sc)];
                if (hTable)
                {
                    HndScanHandlesForGC(hTable, PromoteObject, uintptr_t(sc), uintptr_t(fn), types, uTypeCount, condemned, maxgen, flags);
                }
            }
        walk = walk->pNext;
    }

    // promote objects pointed to by variable handles whose dynamic type is VHT_STRONG
    TraceVariableHandles(PromoteObject, uintptr_t(sc), uintptr_t(fn), VHT_STRONG, condemned, maxgen, flags);

#if defined(FEATURE_COMINTEROP) || defined(FEATURE_REDHAWK)
    // don't scan ref-counted handles during concurrent phase as the clean-up of CCWs can race with AD unload and cause AV's
    if (!sc->concurrent)
    {
        // promote ref-counted handles
        uint32_t type = HNDTYPE_REFCOUNTED;

        walk = &g_HandleTableMap;
        while (walk) {
            for (uint32_t i = 0; i < INITIAL_HANDLE_TABLE_ARRAY_SIZE; i ++)
                if (walk->pBuckets[i] != NULL)
                {
                    HHANDLETABLE hTable = walk->pBuckets[i]->pTable[getSlotNumber(sc)];
                    if (hTable)
                        HndScanHandlesForGC(hTable, PromoteRefCounted, uintptr_t(sc), uintptr_t(fn), &type, 1, condemned, maxgen, flags );
                }
            walk = walk->pNext;
        }
    }
#endif // FEATURE_COMINTEROP || FEATURE_REDHAWK
}


void Ref_TraceRefCountHandles(HANDLESCANPROC callback, uintptr_t lParam1, uintptr_t lParam2)
{
#ifdef FEATURE_COMINTEROP
    int max_slots = getNumberOfSlots();
    uint32_t handleType = HNDTYPE_REFCOUNTED;

    HandleTableMap *walk = &g_HandleTableMap;
    while (walk)
    {
        for (uint32_t i = 0; i < INITIAL_HANDLE_TABLE_ARRAY_SIZE; i++)
        {
            if (walk->pBuckets[i] != NULL)
            {
                for (int j = 0; j < max_slots; j++)
                {
                    HHANDLETABLE hTable = walk->pBuckets[i]->pTable[j];
                    if (hTable)
                        HndEnumHandles(hTable, &handleType, 1, callback, lParam1, lParam2, false);
                }
            }
        }
        walk = walk->pNext;
    }
#else
    UNREFERENCED_PARAMETER(callback);
    UNREFERENCED_PARAMETER(lParam1);
    UNREFERENCED_PARAMETER(lParam2);
#endif // FEATURE_COMINTEROP
}




void Ref_CheckReachable(uint32_t condemned, uint32_t maxgen, uintptr_t lp1)
{
    WRAPPER_NO_CONTRACT;

    LOG((LF_GC, LL_INFO10000, "Checking reachability of referents of long-weak handles in generation %u\n", condemned));

    // these are the handle types that need to be checked
    uint32_t types[] =
    {
        HNDTYPE_WEAK_LONG,
#if defined(FEATURE_COMINTEROP) || defined(FEATURE_REDHAWK)
        HNDTYPE_REFCOUNTED,
#endif // FEATURE_COMINTEROP || FEATURE_REDHAWK
    };

    // check objects pointed to by short weak handles
    uint32_t flags = (((ScanContext*) lp1)->concurrent) ? HNDGCF_ASYNC : HNDGCF_NORMAL;
    int uCPUindex = getSlotNumber((ScanContext*) lp1);

    HandleTableMap *walk = &g_HandleTableMap;
    while (walk) {
        for (uint32_t i = 0; i < INITIAL_HANDLE_TABLE_ARRAY_SIZE; i ++)
        {
            if (walk->pBuckets[i] != NULL)
           {
                HHANDLETABLE hTable = walk->pBuckets[i]->pTable[uCPUindex];
                if (hTable)
                    HndScanHandlesForGC(hTable, CheckPromoted, lp1, 0, types, _countof(types), condemned, maxgen, flags);
        }
        }
        walk = walk->pNext;
    }

    // check objects pointed to by variable handles whose dynamic type is VHT_WEAK_LONG
    TraceVariableHandles(CheckPromoted, lp1, 0, VHT_WEAK_LONG, condemned, maxgen, flags);
}

//
// Dependent handles manages the relationship between primary and secondary objects, where the lifetime of
// the secondary object is dependent upon that of the primary. The handle itself holds the primary instance,
// while the extra handle info holds the secondary object. The secondary object should always be promoted
// when the primary is, and the handle should be cleared if the primary is not promoted. Can't use ordinary
// strong handle to refer to the secondary as this could case a cycle in the graph if the secondary somehow
// pointed back to the primary. Can't use weak handle because that would not keep the secondary object alive.
//
// The result is that a dependenHandle has the EFFECT of 
//    * long weak handles in both the primary and secondary objects
//    * a strong reference from the primary object to the secondary one
//
// Dependent handles are currently used for
// 
//    * managing fields added to EnC classes, where the handle itself holds the this pointer and the
//        secondary object represents the new field that was added.
//    * it is exposed to managed code (as System.Runtime.CompilerServices.DependentHandle) and is used in the
//        implementation of ConditionWeakTable.
//

// Retrieve the dependent handle context associated with the current GC scan context.
DhContext *Ref_GetDependentHandleContext(ScanContext* sc)
{
    WRAPPER_NO_CONTRACT;
    return &g_pDependentHandleContexts[getSlotNumber(sc)];
}

// Scan the dependent handle table promoting any secondary object whose associated primary object is promoted.
//
// Multiple scans may be required since (a) secondary promotions made during one scan could cause the primary
// of another handle to be promoted and (b) the GC may not have marked all promoted objects at the time it
// initially calls us.
//
// Returns true if any promotions resulted from this scan.
bool Ref_ScanDependentHandlesForPromotion(DhContext *pDhContext)
{
    LOG((LF_GC, LL_INFO10000, "Checking liveness of referents of dependent handles in generation %u\n", pDhContext->m_iCondemned));
    uint32_t type = HNDTYPE_DEPENDENT;
    uint32_t flags = (pDhContext->m_pScanContext->concurrent) ? HNDGCF_ASYNC : HNDGCF_NORMAL;
    flags |= HNDGCF_EXTRAINFO;

    // Keep a note of whether we promoted anything over the entire scan (not just the last iteration). We need
    // to return this data since under server GC promotions from this table may cause further promotions in
    // tables handled by other threads.
    bool fAnyPromotions = false;

    // Keep rescanning the table while both the following conditions are true:
    //  1) There's at least primary object left that could have been promoted.
    //  2) We performed at least one secondary promotion (which could have caused a primary promotion) on the
    //     last scan.
    // Note that even once we terminate the GC may call us again (because it has caused more objects to be
    // marked as promoted). But we scan in a loop here anyway because it is cheaper for us to loop than the GC
    // (especially on server GC where each external cycle has to be synchronized between GC worker threads).
    do
    {
        // Assume the conditions for re-scanning are both false initially. The scan callback below
        // (PromoteDependentHandle) will set the relevant flag on the first unpromoted primary it sees or
        // secondary promotion it performs.
        pDhContext->m_fUnpromotedPrimaries = false;
        pDhContext->m_fPromoted = false;

        HandleTableMap *walk = &g_HandleTableMap;
        while (walk) 
        {
            for (uint32_t i = 0; i < INITIAL_HANDLE_TABLE_ARRAY_SIZE; i ++)
            {
                if (walk->pBuckets[i] != NULL)
                {
                    HHANDLETABLE hTable = walk->pBuckets[i]->pTable[getSlotNumber(pDhContext->m_pScanContext)];
                    if (hTable)
                    {
                        HndScanHandlesForGC(hTable,
                                            PromoteDependentHandle,
                                            uintptr_t(pDhContext->m_pScanContext),
                                            uintptr_t(pDhContext->m_pfnPromoteFunction),
                                            &type, 1,
                                            pDhContext->m_iCondemned,
                                            pDhContext->m_iMaxGen,
                                            flags );
                    }
                }
            }
            walk = walk->pNext;
        }

        if (pDhContext->m_fPromoted)
            fAnyPromotions = true;

    } while (pDhContext->m_fUnpromotedPrimaries && pDhContext->m_fPromoted);

    return fAnyPromotions;
}

// Perform a scan of dependent handles for the purpose of clearing any that haven't had their primary
// promoted.
void Ref_ScanDependentHandlesForClearing(uint32_t condemned, uint32_t maxgen, ScanContext* sc, Ref_promote_func* fn)
{
    LOG((LF_GC, LL_INFO10000, "Clearing dead dependent handles in generation %u\n", condemned));
    uint32_t type = HNDTYPE_DEPENDENT;
    uint32_t flags = (sc->concurrent) ? HNDGCF_ASYNC : HNDGCF_NORMAL;
    flags |= HNDGCF_EXTRAINFO;

    HandleTableMap *walk = &g_HandleTableMap;
    while (walk) 
    {
        for (uint32_t i = 0; i < INITIAL_HANDLE_TABLE_ARRAY_SIZE; i ++)
        {
            if (walk->pBuckets[i] != NULL)
            {
                HHANDLETABLE hTable = walk->pBuckets[i]->pTable[getSlotNumber(sc)];
                if (hTable)
                {
                    HndScanHandlesForGC(hTable, ClearDependentHandle, uintptr_t(sc), uintptr_t(fn), &type, 1, condemned, maxgen, flags );
                }
            }
        }
        walk = walk->pNext;
    }
}

// Perform a scan of dependent handles for the purpose of updating handles to track relocated objects.
void Ref_ScanDependentHandlesForRelocation(uint32_t condemned, uint32_t maxgen, ScanContext* sc, Ref_promote_func* fn)
{
    LOG((LF_GC, LL_INFO10000, "Relocating moved dependent handles in generation %u\n", condemned));
    uint32_t type = HNDTYPE_DEPENDENT;
    uint32_t flags = (sc->concurrent) ? HNDGCF_ASYNC : HNDGCF_NORMAL;
    flags |= HNDGCF_EXTRAINFO;

    HandleTableMap *walk = &g_HandleTableMap;
    while (walk) 
    {
        for (uint32_t i = 0; i < INITIAL_HANDLE_TABLE_ARRAY_SIZE; i ++)
        {
            if (walk->pBuckets[i] != NULL)
            {
                HHANDLETABLE hTable = walk->pBuckets[i]->pTable[getSlotNumber(sc)];
                if (hTable)
                {
                    HndScanHandlesForGC(hTable, UpdateDependentHandle, uintptr_t(sc), uintptr_t(fn), &type, 1, condemned, maxgen, flags );
                }
            }
        }
        walk = walk->pNext;
    }
}

/*
  loop scan version of TraceVariableHandles for single-thread-managed Ref_* functions
  should be kept in sync with the code above
  Only used by profiling/ETW.
*/
void TraceDependentHandlesBySingleThread(HANDLESCANPROC pfnTrace, uintptr_t lp1, uintptr_t lp2, uint32_t condemned, uint32_t maxgen, uint32_t flags)
{
    WRAPPER_NO_CONTRACT;

    // set up to scan variable handles with the specified mask and trace function
    uint32_t type = HNDTYPE_DEPENDENT;
    struct DIAG_DEPSCANINFO info = { pfnTrace, lp2 };

    HandleTableMap *walk = &g_HandleTableMap;
    while (walk) {
        for (uint32_t i = 0; i < INITIAL_HANDLE_TABLE_ARRAY_SIZE; i ++)
            if (walk->pBuckets[i] != NULL)
            {
                // this is the one of Ref_* function performed by single thread in MULTI_HEAPS case, so we need to loop through all HT of the bucket
                for (int uCPUindex=0; uCPUindex < getNumberOfSlots(); uCPUindex++)
                {
                    HHANDLETABLE hTable = walk->pBuckets[i]->pTable[uCPUindex];
                    if (hTable)
                        HndScanHandlesForGC(hTable, TraceDependentHandle,
                                    lp1, (uintptr_t)&info, &type, 1, condemned, maxgen, HNDGCF_EXTRAINFO | flags);
                }
            }
        walk = walk->pNext;
    }
}

void ScanSizedRefByCPU(uint32_t maxgen, HANDLESCANPROC scanProc, ScanContext* sc, Ref_promote_func* fn, uint32_t flags)
{
    HandleTableMap *walk = &g_HandleTableMap;
    uint32_t type = HNDTYPE_SIZEDREF;
    int uCPUindex = getSlotNumber(sc);

    while (walk) 
    {
        for (uint32_t i = 0; i < INITIAL_HANDLE_TABLE_ARRAY_SIZE; i ++)
        {
        	if (walk->pBuckets[i] != NULL)
	        {
                HHANDLETABLE hTable = walk->pBuckets[i]->pTable[uCPUindex];
                if (hTable)
                {
                    HndScanHandlesForGC(hTable, scanProc, uintptr_t(sc), uintptr_t(fn), &type, 1, maxgen, maxgen, flags);
                }
            }
        }
        walk = walk->pNext;
    }
}

void Ref_ScanSizedRefHandles(uint32_t condemned, uint32_t maxgen, ScanContext* sc, Ref_promote_func* fn)
{
    LOG((LF_GC, LL_INFO10000, "Scanning SizedRef handles to in generation %u\n", condemned));
    UNREFERENCED_PARAMETER(condemned);
    _ASSERTE (condemned == maxgen);
    uint32_t flags = (sc->concurrent ? HNDGCF_ASYNC : HNDGCF_NORMAL) | HNDGCF_EXTRAINFO;

    ScanSizedRefByCPU(maxgen, CalculateSizedRefSize, sc, fn, flags);
}

void Ref_CheckAlive(uint32_t condemned, uint32_t maxgen, uintptr_t lp1)
{
    WRAPPER_NO_CONTRACT;

    LOG((LF_GC, LL_INFO10000, "Checking liveness of referents of short-weak handles in generation %u\n", condemned));

    // perform a multi-type scan that checks for unreachable objects
    uint32_t types[] =
    {
        HNDTYPE_WEAK_SHORT
#ifdef FEATURE_COMINTEROP
        , HNDTYPE_WEAK_WINRT
#endif // FEATURE_COMINTEROP
    };
    uint32_t flags = (((ScanContext*) lp1)->concurrent) ? HNDGCF_ASYNC : HNDGCF_NORMAL;

    int uCPUindex = getSlotNumber((ScanContext*) lp1);
    HandleTableMap *walk = &g_HandleTableMap;
    while (walk)
    {
        for (uint32_t i = 0; i < INITIAL_HANDLE_TABLE_ARRAY_SIZE; i ++)
        {
            if (walk->pBuckets[i] != NULL)
            {
                HHANDLETABLE hTable = walk->pBuckets[i]->pTable[uCPUindex];
                if (hTable)
                    HndScanHandlesForGC(hTable, CheckPromoted, lp1, 0, types, _countof(types), condemned, maxgen, flags);
            }
        }
        walk = walk->pNext;
    }
    // check objects pointed to by variable handles whose dynamic type is VHT_WEAK_SHORT
    TraceVariableHandles(CheckPromoted, lp1, 0, VHT_WEAK_SHORT, condemned, maxgen, flags);
}

static VOLATILE(int32_t) uCount = 0;

// NOTE: Please: if you update this function, update the very similar profiling function immediately below!!!
void Ref_UpdatePointers(uint32_t condemned, uint32_t maxgen, ScanContext* sc, Ref_promote_func* fn)
{
    WRAPPER_NO_CONTRACT;

    // For now, treat the syncblock as if it were short weak handles.  <REVISIT_TODO>Later, get
    // the benefits of fast allocation / free & generational awareness by supporting
    // the SyncTable as a new block type.
    // @TODO cwb: wait for compelling performance measurements.</REVISIT_TODO>
    BOOL bDo = TRUE;

    if (IsServerHeap()) 
    {
        bDo = (Interlocked::Increment(&uCount) == 1);
        Interlocked::CompareExchange (&uCount, 0, g_theGCHeap->GetNumberOfHeaps());
        _ASSERTE (uCount <= g_theGCHeap->GetNumberOfHeaps());
    }

    if (bDo)   
        GCToEEInterface::SyncBlockCacheWeakPtrScan(&UpdatePointer, uintptr_t(sc), uintptr_t(fn));

    LOG((LF_GC, LL_INFO10000, "Updating pointers to referents of non-pinning handles in generation %u\n", condemned));

    // these are the handle types that need their pointers updated
    uint32_t types[] =
    {
        HNDTYPE_WEAK_SHORT,
        HNDTYPE_WEAK_LONG,
        HNDTYPE_STRONG,
#if defined(FEATURE_COMINTEROP) || defined(FEATURE_REDHAWK)
        HNDTYPE_REFCOUNTED,
#endif // FEATURE_COMINTEROP || FEATURE_REDHAWK
#ifdef FEATURE_COMINTEROP
        HNDTYPE_WEAK_WINRT,
#endif // FEATURE_COMINTEROP
        HNDTYPE_SIZEDREF,
    };

    // perform a multi-type scan that updates pointers
    uint32_t flags = (sc->concurrent) ? HNDGCF_ASYNC : HNDGCF_NORMAL;

    HandleTableMap *walk = &g_HandleTableMap;
    while (walk) {
        for (uint32_t i = 0; i < INITIAL_HANDLE_TABLE_ARRAY_SIZE; i ++)
            if (walk->pBuckets[i] != NULL)
            {
                HHANDLETABLE hTable = walk->pBuckets[i]->pTable[getSlotNumber(sc)];
                if (hTable)
                    HndScanHandlesForGC(hTable, UpdatePointer, uintptr_t(sc), uintptr_t(fn), types, _countof(types), condemned, maxgen, flags);
            }
        walk = walk->pNext;
    }

    // update pointers in variable handles whose dynamic type is VHT_WEAK_SHORT, VHT_WEAK_LONG or VHT_STRONG
    TraceVariableHandles(UpdatePointer, uintptr_t(sc), uintptr_t(fn), VHT_WEAK_SHORT | VHT_WEAK_LONG | VHT_STRONG, condemned, maxgen, flags);
}

#if defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)

// Please update this if you change the Ref_UpdatePointers function above.
void Ref_ScanHandlesForProfilerAndETW(uint32_t maxgen, uintptr_t lp1, handle_scan_fn fn)
{
    WRAPPER_NO_CONTRACT;

    LOG((LF_GC | LF_CORPROF, LL_INFO10000, "Scanning all handle roots for profiler.\n"));

    // Don't scan the sync block because they should not be reported. They are weak handles only

    // <REVISIT_TODO>We should change the following to not report weak either
    // these are the handle types that need their pointers updated</REVISIT_TODO>
    uint32_t types[] =
    {
        HNDTYPE_WEAK_SHORT,
        HNDTYPE_WEAK_LONG,
        HNDTYPE_STRONG,
#if defined(FEATURE_COMINTEROP) || defined(FEATURE_REDHAWK)
        HNDTYPE_REFCOUNTED,
#endif // FEATURE_COMINTEROP || FEATURE_REDHAWK
#ifdef FEATURE_COMINTEROP
        HNDTYPE_WEAK_WINRT,
#endif // FEATURE_COMINTEROP
        HNDTYPE_PINNED,
//        HNDTYPE_VARIABLE,
        HNDTYPE_ASYNCPINNED,
        HNDTYPE_SIZEDREF,
    };

    uint32_t flags = HNDGCF_NORMAL;

    // perform a multi-type scan that updates pointers
    HandleTableMap *walk = &g_HandleTableMap;
    while (walk) {
        for (uint32_t i = 0; i < INITIAL_HANDLE_TABLE_ARRAY_SIZE; i ++)
            if (walk->pBuckets[i] != NULL)
                // this is the one of Ref_* function performed by single thread in MULTI_HEAPS case, so we need to loop through all HT of the bucket
                for (int uCPUindex=0; uCPUindex < getNumberOfSlots(); uCPUindex++)
                {
                    HHANDLETABLE hTable = walk->pBuckets[i]->pTable[uCPUindex];
                    if (hTable)
                        HndScanHandlesForGC(hTable, &ScanPointerForProfilerAndETW, lp1, (uintptr_t)fn, types, _countof(types), maxgen, maxgen, flags);
                }
        walk = walk->pNext;
    }

    // update pointers in variable handles whose dynamic type is VHT_WEAK_SHORT, VHT_WEAK_LONG or VHT_STRONG
    TraceVariableHandlesBySingleThread(&ScanPointerForProfilerAndETW, lp1, (uintptr_t)fn, VHT_WEAK_SHORT | VHT_WEAK_LONG | VHT_STRONG, maxgen, maxgen, flags);
}

void Ref_ScanDependentHandlesForProfilerAndETW(uint32_t maxgen, ScanContext * SC, handle_scan_fn fn)
{
    WRAPPER_NO_CONTRACT;

    LOG((LF_GC | LF_CORPROF, LL_INFO10000, "Scanning dependent handles for profiler.\n"));

    uint32_t flags = HNDGCF_NORMAL;

    uintptr_t lp1 = (uintptr_t)SC;
    TraceDependentHandlesBySingleThread(&ScanPointerForProfilerAndETW, lp1, (uintptr_t)fn, maxgen, maxgen, flags);
}

#endif // defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)

// Callback to enumerate all object references held in handles.
void CALLBACK ScanPointer(_UNCHECKED_OBJECTREF *pObjRef, uintptr_t *pExtraInfo, uintptr_t lp1, uintptr_t lp2)
{
    WRAPPER_NO_CONTRACT;
    UNREFERENCED_PARAMETER(pExtraInfo);

    Object **pRef = (Object **)pObjRef;
    _ASSERTE(lp2);
    promote_func* callback = (promote_func*)lp2;
    callback(pRef, (ScanContext *)lp1, 0);
}

// Enumerate all object references held by any of the handle tables in the system.
void Ref_ScanPointers(uint32_t condemned, uint32_t maxgen, ScanContext* sc, Ref_promote_func* fn)
{
    WRAPPER_NO_CONTRACT;

    uint32_t types[] =
    {
        HNDTYPE_WEAK_SHORT,
        HNDTYPE_WEAK_LONG,
        HNDTYPE_STRONG,
#if defined(FEATURE_COMINTEROP) || defined(FEATURE_REDHAWK)
        HNDTYPE_REFCOUNTED,
#endif // FEATURE_COMINTEROP || FEATURE_REDHAWK
        HNDTYPE_PINNED,
        HNDTYPE_ASYNCPINNED,
        HNDTYPE_SIZEDREF,
    };

    uint32_t flags = HNDGCF_NORMAL;

    // perform a multi-type scan that enumerates pointers
    for (HandleTableMap * walk = &g_HandleTableMap; 
         walk != nullptr; 
         walk = walk->pNext)
    {
        for (uint32_t i = 0; i < INITIAL_HANDLE_TABLE_ARRAY_SIZE; i++)
        {
            if (walk->pBuckets[i] != NULL)
            {
                // this is the one of Ref_* function performed by single thread in MULTI_HEAPS case, so we need to loop through all HT of the bucket
                for (int uCPUindex = 0; uCPUindex < getNumberOfSlots(); uCPUindex++)
                {
                    HHANDLETABLE hTable = walk->pBuckets[i]->pTable[uCPUindex];
                    if (hTable)
                        HndScanHandlesForGC(hTable, &ScanPointer, uintptr_t(sc), uintptr_t(fn), types, _countof(types), condemned, maxgen, flags);
                }
            }
        }
    }

    // enumerate pointers in variable handles whose dynamic type is VHT_WEAK_SHORT, VHT_WEAK_LONG or VHT_STRONG
    TraceVariableHandlesBySingleThread(&ScanPointer, uintptr_t(sc), uintptr_t(fn), VHT_WEAK_SHORT | VHT_WEAK_LONG | VHT_STRONG, condemned, maxgen, flags);
}

void Ref_UpdatePinnedPointers(uint32_t condemned, uint32_t maxgen, ScanContext* sc, Ref_promote_func* fn)
{
    WRAPPER_NO_CONTRACT;

    LOG((LF_GC, LL_INFO10000, "Updating pointers to referents of pinning handles in generation %u\n", condemned));

    // these are the handle types that need their pointers updated
    uint32_t types[2] = {HNDTYPE_PINNED, HNDTYPE_ASYNCPINNED};
    uint32_t flags = (sc->concurrent) ? HNDGCF_ASYNC : HNDGCF_NORMAL;

    HandleTableMap *walk = &g_HandleTableMap;
    while (walk) {
        for (uint32_t i = 0; i < INITIAL_HANDLE_TABLE_ARRAY_SIZE; i ++)
            if (walk->pBuckets[i] != NULL)
            {
                HHANDLETABLE hTable = walk->pBuckets[i]->pTable[getSlotNumber(sc)];
                if (hTable)
                    HndScanHandlesForGC(hTable, UpdatePointerPinned, uintptr_t(sc), uintptr_t(fn), types, _countof(types), condemned, maxgen, flags); 
            }
        walk = walk->pNext;
    }

    // update pointers in variable handles whose dynamic type is VHT_PINNED
    TraceVariableHandles(UpdatePointerPinned, uintptr_t(sc), uintptr_t(fn), VHT_PINNED, condemned, maxgen, flags);
}


void Ref_AgeHandles(uint32_t condemned, uint32_t maxgen, uintptr_t lp1)
{
    WRAPPER_NO_CONTRACT;

    LOG((LF_GC, LL_INFO10000, "Aging handles in generation %u\n", condemned));

    // these are the handle types that need their ages updated
    uint32_t types[] =
    {
        HNDTYPE_WEAK_SHORT,
        HNDTYPE_WEAK_LONG,

        HNDTYPE_STRONG,

        HNDTYPE_PINNED,
        HNDTYPE_VARIABLE,
#if defined(FEATURE_COMINTEROP) || defined(FEATURE_REDHAWK)
        HNDTYPE_REFCOUNTED,
#endif // FEATURE_COMINTEROP || FEATURE_REDHAWK
#ifdef FEATURE_COMINTEROP
        HNDTYPE_WEAK_WINRT,
#endif // FEATURE_COMINTEROP
        HNDTYPE_ASYNCPINNED,
        HNDTYPE_SIZEDREF,
    };

    int uCPUindex = getSlotNumber((ScanContext*) lp1);
    // perform a multi-type scan that ages the handles
    HandleTableMap *walk = &g_HandleTableMap;
    while (walk) {
        for (uint32_t i = 0; i < INITIAL_HANDLE_TABLE_ARRAY_SIZE; i ++)
            if (walk->pBuckets[i] != NULL)
            {
                HHANDLETABLE hTable = walk->pBuckets[i]->pTable[uCPUindex];
                if (hTable)
                    HndScanHandlesForGC(hTable, NULL, 0, 0, types, _countof(types), condemned, maxgen, HNDGCF_AGE);
            }
        walk = walk->pNext;
    }
}


void Ref_RejuvenateHandles(uint32_t condemned, uint32_t maxgen, uintptr_t lp1)
{
    WRAPPER_NO_CONTRACT;

    LOG((LF_GC, LL_INFO10000, "Rejuvenating handles.\n"));

    // these are the handle types that need their ages updated
    uint32_t types[] =
    {
        HNDTYPE_WEAK_SHORT,
        HNDTYPE_WEAK_LONG,


        HNDTYPE_STRONG,

        HNDTYPE_PINNED,
        HNDTYPE_VARIABLE,
#if defined(FEATURE_COMINTEROP) || defined(FEATURE_REDHAWK)
        HNDTYPE_REFCOUNTED,
#endif // FEATURE_COMINTEROP || FEATURE_REDHAWK
#ifdef FEATURE_COMINTEROP
        HNDTYPE_WEAK_WINRT,
#endif // FEATURE_COMINTEROP
        HNDTYPE_ASYNCPINNED,
        HNDTYPE_SIZEDREF,
    };

    int uCPUindex = getSlotNumber((ScanContext*) lp1);
    // reset the ages of these handles
    HandleTableMap *walk = &g_HandleTableMap;
    while (walk) {
        for (uint32_t i = 0; i < INITIAL_HANDLE_TABLE_ARRAY_SIZE; i ++)
            if (walk->pBuckets[i] != NULL)
            {
                HHANDLETABLE hTable = walk->pBuckets[i]->pTable[uCPUindex];
                if (hTable)
                    HndResetAgeMap(hTable, types, _countof(types), condemned, maxgen, HNDGCF_NORMAL);
            }
        walk = walk->pNext;
    }
}

void Ref_VerifyHandleTable(uint32_t condemned, uint32_t maxgen, ScanContext* sc)
{
    WRAPPER_NO_CONTRACT;

    LOG((LF_GC, LL_INFO10000, "Verifying handles.\n"));

    // these are the handle types that need to be verified
    uint32_t types[] =
    {
        HNDTYPE_WEAK_SHORT,
        HNDTYPE_WEAK_LONG,


        HNDTYPE_STRONG,

        HNDTYPE_PINNED,
        HNDTYPE_VARIABLE,
#if defined(FEATURE_COMINTEROP) || defined(FEATURE_REDHAWK)
        HNDTYPE_REFCOUNTED,
#endif // FEATURE_COMINTEROP || FEATURE_REDHAWK
#ifdef FEATURE_COMINTEROP
        HNDTYPE_WEAK_WINRT,
#endif // FEATURE_COMINTEROP
        HNDTYPE_ASYNCPINNED,
        HNDTYPE_SIZEDREF,
        HNDTYPE_DEPENDENT,
    };

    // verify these handles
    HandleTableMap *walk = &g_HandleTableMap;
    while (walk)
    {
        for (uint32_t i = 0; i < INITIAL_HANDLE_TABLE_ARRAY_SIZE; i ++)
        {
            if (walk->pBuckets[i] != NULL)
            {
                HHANDLETABLE hTable = walk->pBuckets[i]->pTable[getSlotNumber(sc)];
                if (hTable)
                    HndVerifyTable(hTable, types, _countof(types), condemned, maxgen, HNDGCF_NORMAL);
            }
        }
        walk = walk->pNext;
    }
}

int GetCurrentThreadHomeHeapNumber()
{
    WRAPPER_NO_CONTRACT;

    assert(g_theGCHeap != nullptr);
    return g_theGCHeap->GetHomeHeapNumber();
}

bool HandleTableBucket::Contains(OBJECTHANDLE handle)
{
    LIMITED_METHOD_CONTRACT;

    if (NULL == handle)
    {
        return FALSE;
    }
    
    HHANDLETABLE hTable = HndGetHandleTable(handle);
    for (int uCPUindex=0; uCPUindex < g_theGCHeap->GetNumberOfHeaps(); uCPUindex++)
    {
        if (hTable == this->pTable[uCPUindex]) 
        {
            return TRUE;
        }
    }
    return FALSE;
}

#endif // !DACCESS_COMPILE

GC_DAC_VISIBLE
OBJECTREF GetDependentHandleSecondary(OBJECTHANDLE handle)
{ 
    WRAPPER_NO_CONTRACT;

    return UNCHECKED_OBJECTREF_TO_OBJECTREF((_UNCHECKED_OBJECTREF)HndGetHandleExtraInfo(handle));
}

void PopulateHandleTableDacVars(GcDacVars* gcDacVars)
{
    static_assert(offsetof(HandleTableMap, pBuckets) == offsetof(dac_handle_table_map, pBuckets), "handle table map DAC layout mismatch");
    static_assert(offsetof(HandleTableMap, pNext) == offsetof(dac_handle_table_map, pNext), "handle table map DAC layout mismatch");
    static_assert(offsetof(HandleTableMap, dwMaxIndex) == offsetof(dac_handle_table_map, dwMaxIndex), "handle table map DAC layout mismatch");
    static_assert(offsetof(HandleTableBucket, pTable) == offsetof(dac_handle_table_bucket, pTable), "handle table bucket DAC layout mismatch");
    static_assert(offsetof(HandleTableBucket, HandleTableIndex) == offsetof(dac_handle_table_bucket, HandleTableIndex), "handle table bucket DAC layout mismatch");

#ifndef DACCESS_COMPILE
    gcDacVars->handle_table_map = reinterpret_cast<dac_handle_table_map*>(&g_HandleTableMap);
#endif // DACCESS_COMPILE
}
