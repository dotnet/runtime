//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

/*
 * GCSCAN.CPP 
 *
 * GC Root Scanning
 *

 *
 */

#include "common.h"

#include "gcenv.h"

#include "gcscan.h"
#include "gc.h"
#include "objecthandle.h"

//#define CATCH_GC  //catches exception during GC
#ifdef DACCESS_COMPILE
SVAL_IMPL_INIT(int32_t, GCScan, m_GcStructuresInvalidCnt, 1);
#else //DACCESS_COMPILE
VOLATILE(int32_t) GCScan::m_GcStructuresInvalidCnt = 1;
#endif //DACCESS_COMPILE

bool GCScan::GetGcRuntimeStructuresValid ()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;
    _ASSERTE ((int32_t)m_GcStructuresInvalidCnt >= 0);
    return (int32_t)m_GcStructuresInvalidCnt == 0;
}

#ifdef DACCESS_COMPILE

#ifndef FEATURE_REDHAWK
void
GCScan::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    UNREFERENCED_PARAMETER(flags);
    m_GcStructuresInvalidCnt.EnumMem();
}
#endif

#else

//
// Dependent handle promotion scan support
//

// This method is called first during the mark phase. It's job is to set up the context for further scanning
// (remembering the scan parameters the GC gives us and initializing some state variables we use to determine
// whether further scans will be required or not).
//
// This scan is not guaranteed to return complete results due to the GC context in which we are called. In
// particular it is possible, due to either a mark stack overflow or unsynchronized operation in server GC
// mode, that not all reachable objects will be reported as promoted yet. However, the operations we perform
// will still be correct and this scan allows us to spot a common optimization where no dependent handles are
// due for retirement in this particular GC. This is an important optimization to take advantage of since
// synchronizing the GC to calculate complete results is a costly operation.
void GCScan::GcDhInitialScan(promote_func* fn, int condemned, int max_gen, ScanContext* sc)
{
    // We allocate space for dependent handle scanning context during Ref_Initialize. Under server GC there
    // are actually as many contexts as heaps (and CPUs). Ref_GetDependentHandleContext() retrieves the
    // correct context for the current GC thread based on the ScanContext passed to us by the GC.
    DhContext *pDhContext = Ref_GetDependentHandleContext(sc);

    // Record GC callback parameters in the DH context so that the GC doesn't continually have to pass the
    // same data to each call.
    pDhContext->m_pfnPromoteFunction = fn;
    pDhContext->m_iCondemned = condemned;
    pDhContext->m_iMaxGen = max_gen;
    pDhContext->m_pScanContext = sc;

    // Look for dependent handle whose primary has been promoted but whose secondary has not. Promote the
    // secondary in those cases. Additionally this scan sets the m_fUnpromotedPrimaries and m_fPromoted state
    // flags in the DH context. The m_fUnpromotedPrimaries flag is the most interesting here: if this flag is
    // false after the scan then it doesn't matter how many object promotions might currently be missing since
    // there are no secondary objects that are currently unpromoted anyway. This is the (hopefully common)
    // circumstance under which we don't have to perform any costly additional re-scans.
    Ref_ScanDependentHandlesForPromotion(pDhContext);
}

// This method is called after GcDhInitialScan and before each subsequent scan (GcDhReScan below). It
// determines whether any handles are left that have unpromoted secondaries.
bool GCScan::GcDhUnpromotedHandlesExist(ScanContext* sc)
{
    WRAPPER_NO_CONTRACT;
    // Locate our dependent handle context based on the GC context.
    DhContext *pDhContext = Ref_GetDependentHandleContext(sc);

    return pDhContext->m_fUnpromotedPrimaries;
}

// Perform a re-scan of dependent handles, promoting secondaries associated with newly promoted primaries as
// above. We may still need to call this multiple times since promotion of a secondary late in the table could
// promote a primary earlier in the table. Also, GC graph promotions are not guaranteed to be complete by the
// time the promotion callback returns (the mark stack can overflow). As a result the GC might have to call
// this method in a loop. The scan records state that let's us know when to terminate (no further handles to
// be promoted or no promotions in the last scan). Returns true if at least one object was promoted as a
// result of the scan.
bool GCScan::GcDhReScan(ScanContext* sc)
{
    // Locate our dependent handle context based on the GC context.
    DhContext *pDhContext = Ref_GetDependentHandleContext(sc);

    return Ref_ScanDependentHandlesForPromotion(pDhContext);
}

/*
 * Scan for dead weak pointers
 */

void GCScan::GcWeakPtrScan( promote_func* fn, int condemned, int max_gen, ScanContext* sc )
{
    // Clear out weak pointers that are no longer live.
    Ref_CheckReachable(condemned, max_gen, (uintptr_t)sc);

    // Clear any secondary objects whose primary object is now definitely dead.
    Ref_ScanDependentHandlesForClearing(condemned, max_gen, sc, fn);
}

static void CALLBACK CheckPromoted(_UNCHECKED_OBJECTREF *pObjRef, uintptr_t * /*pExtraInfo*/, uintptr_t /*lp1*/, uintptr_t /*lp2*/)
{
    LIMITED_METHOD_CONTRACT;

    LOG((LF_GC, LL_INFO100000, LOG_HANDLE_OBJECT_CLASS("Checking referent of Weak-", pObjRef, "to ", *pObjRef)));

    Object **pRef = (Object **)pObjRef;
    if (!GCHeap::GetGCHeap()->IsPromoted(*pRef))
    {
        LOG((LF_GC, LL_INFO100, LOG_HANDLE_OBJECT_CLASS("Severing Weak-", pObjRef, "to unreachable ", *pObjRef)));

        *pRef = NULL;
    }
    else
    {
        LOG((LF_GC, LL_INFO1000000, "reachable " LOG_OBJECT_CLASS(*pObjRef)));
    }
}

void GCScan::GcWeakPtrScanBySingleThread( int condemned, int max_gen, ScanContext* sc )
{
    UNREFERENCED_PARAMETER(condemned);
    UNREFERENCED_PARAMETER(max_gen);
    GCToEEInterface::SyncBlockCacheWeakPtrScan(&CheckPromoted, (uintptr_t)sc, 0);
}

void GCScan::GcScanSizedRefs(promote_func* fn, int condemned, int max_gen, ScanContext* sc)
{
    Ref_ScanSizedRefHandles(condemned, max_gen, sc, fn);
}

void GCScan::GcShortWeakPtrScan(promote_func* fn,  int condemned, int max_gen, 
                                     ScanContext* sc)
{
    UNREFERENCED_PARAMETER(fn);
    Ref_CheckAlive(condemned, max_gen, (uintptr_t)sc);
}

/*
 * Scan all stack roots in this 'namespace'
 */
 
void GCScan::GcScanRoots(promote_func* fn,  int condemned, int max_gen, 
                             ScanContext* sc)
{
#if defined ( _DEBUG) && defined (CATCH_GC)
    //note that we can't use EX_TRY because the gc_thread isn't known
    PAL_TRY
#endif // _DEBUG && CATCH_GC
    {
        GCToEEInterface::GcScanRoots(fn, condemned, max_gen, sc);
    }
#if defined ( _DEBUG) && defined (CATCH_GC)
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        _ASSERTE (!"We got an exception during scan roots");
    }
    PAL_ENDTRY
#endif //_DEBUG
}

/*
 * Scan all handle roots in this 'namespace'
 */


void GCScan::GcScanHandles (promote_func* fn,  int condemned, int max_gen, 
                                ScanContext* sc)
{

#if defined ( _DEBUG) && defined (CATCH_GC)
    //note that we can't use EX_TRY because the gc_thread isn't known
    PAL_TRY
#endif // _DEBUG && CATCH_GC
    {
        STRESS_LOG1(LF_GC|LF_GCROOTS, LL_INFO10, "GcScanHandles (Promotion Phase = %d)\n", sc->promotion);
        if (sc->promotion)
        {
            Ref_TracePinningRoots(condemned, max_gen, sc, fn);
            Ref_TraceNormalRoots(condemned, max_gen, sc, fn);
        }
        else
        {
            Ref_UpdatePointers(condemned, max_gen, sc, fn);
            Ref_UpdatePinnedPointers(condemned, max_gen, sc, fn);
            Ref_ScanDependentHandlesForRelocation(condemned, max_gen, sc, fn);
        }
    }
    
#if defined ( _DEBUG) && defined (CATCH_GC)
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        _ASSERTE (!"We got an exception during scan roots");
    }
    PAL_ENDTRY
#endif //_DEBUG
}


#if defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)

/*
 * Scan all handle roots in this 'namespace' for profiling
 */

void GCScan::GcScanHandlesForProfilerAndETW (int max_gen, ScanContext* sc)
{
    LIMITED_METHOD_CONTRACT;

#if defined ( _DEBUG) && defined (CATCH_GC)
    //note that we can't use EX_TRY because the gc_thread isn't known
    PAL_TRY
#endif // _DEBUG && CATCH_GC
    {
        LOG((LF_GC|LF_GCROOTS, LL_INFO10, "Profiler Root Scan Phase, Handles\n"));
        Ref_ScanPointersForProfilerAndETW(max_gen, (uintptr_t)sc);
    }
    
#if defined ( _DEBUG) && defined (CATCH_GC)
    PAL_EXCEPT(EXCEPTION_EXECUTE_HANDLER)
    {
        _ASSERTE (!"We got an exception during scan roots for the profiler");
    }
    PAL_ENDTRY
#endif //_DEBUG
}

/*
 * Scan dependent handles in this 'namespace' for profiling
 */
void GCScan::GcScanDependentHandlesForProfilerAndETW (int max_gen, ProfilingScanContext* sc)
{
    LIMITED_METHOD_CONTRACT;

    LOG((LF_GC|LF_GCROOTS, LL_INFO10, "Profiler Root Scan Phase, DependentHandles\n"));
    Ref_ScanDependentHandlesForProfilerAndETW(max_gen, sc);
}

#endif // defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)

void GCScan::GcRuntimeStructuresValid (BOOL bValid)
{
    WRAPPER_NO_CONTRACT;
    if (!bValid)
    {
        int32_t result;
        result = Interlocked::Increment (&m_GcStructuresInvalidCnt);
        _ASSERTE (result > 0);
    }
    else
    {
        int32_t result;
        result = Interlocked::Decrement (&m_GcStructuresInvalidCnt);
        _ASSERTE (result >= 0);
    }
}

void GCScan::GcDemote (int condemned, int max_gen, ScanContext* sc)
{
    Ref_RejuvenateHandles (condemned, max_gen, (uintptr_t)sc);
    if (!GCHeap::IsServerHeap() || sc->thread_number == 0)
        GCToEEInterface::SyncBlockCacheDemote(max_gen);
}

void GCScan::GcPromotionsGranted (int condemned, int max_gen, ScanContext* sc)
{
    Ref_AgeHandles(condemned, max_gen, (uintptr_t)sc);
    if (!GCHeap::IsServerHeap() || sc->thread_number == 0)
        GCToEEInterface::SyncBlockCachePromotionsGranted(max_gen);
}


size_t GCScan::AskForMoreReservedMemory (size_t old_size, size_t need_size)
{
    LIMITED_METHOD_CONTRACT;

#if !defined(FEATURE_CORECLR) && !defined(FEATURE_REDHAWK)
    // call the host....

    IGCHostControl *pGCHostControl = CorHost::GetGCHostControl();

    if (pGCHostControl)
    {
        size_t new_max_limit_size = need_size;
        pGCHostControl->RequestVirtualMemLimit (old_size, 
                                                (SIZE_T*)&new_max_limit_size);
        return new_max_limit_size;
    }
#endif

    return old_size + need_size;
}

void GCScan::VerifyHandleTable(int condemned, int max_gen, ScanContext* sc)
{
    LIMITED_METHOD_CONTRACT;
    Ref_VerifyHandleTable(condemned, max_gen, sc);
}

#endif // !DACCESS_COMPILE
