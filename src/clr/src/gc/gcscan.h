//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

/*
 * GCSCAN.H
 *
 * GC Root Scanning
 *

 *
 */

#ifndef _GCSCAN_H_
#define _GCSCAN_H_

#include "gc.h"

// Scanning dependent handles for promotion can become a complex operation due to cascaded dependencies and
// other issues (see the comments for GcDhInitialScan and friends in gcscan.cpp for further details). As a
// result we need to maintain a context between all the DH scanning methods called during a single mark phase.
// The structure below describes this context. We allocate one of these per GC heap at Ref_Initialize time and
// select between them based on the ScanContext passed to us by the GC during the mark phase.
struct DhContext
{
    bool            m_fUnpromotedPrimaries;     // Did last scan find at least one non-null unpromoted primary?
    bool            m_fPromoted;                // Did last scan promote at least one secondary?
    promote_func   *m_pfnPromoteFunction;       // GC promote callback to be used for all secondary promotions
    int             m_iCondemned;               // The condemned generation
    int             m_iMaxGen;                  // The maximum generation
    ScanContext    *m_pScanContext;             // The GC's scan context for this phase
};


// <TODO>
// @TODO (JSW): For compatibility with the existing GC code we use CNamespace
// as the name of this class.   I'm planning on changing it to
// something like GCDomain....
// </TODO>

typedef void enum_alloc_context_func(alloc_context*);

class CNameSpace
{
    friend struct ::_DacGlobals;

  public:

    // Called on gc start
    static void GcStartDoWork();

    static void GcScanSizedRefs(promote_func* fn, int condemned, int max_gen, ScanContext* sc);

    // Regular stack Roots
    static void GcScanRoots (promote_func* fn, int condemned, int max_gen, ScanContext* sc);

    //
    static void GcScanHandles (promote_func* fn, int condemned, int max_gen, ScanContext* sc);

    static void GcRuntimeStructuresValid (BOOL bValid);

    static bool GetGcRuntimeStructuresValid ();
#ifdef DACCESS_COMPILE    
    static void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif // DACCESS_COMPILE

#if defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)
    static void GcScanHandlesForProfilerAndETW (int max_gen, ScanContext* sc);
    static void GcScanDependentHandlesForProfilerAndETW (int max_gen, ProfilingScanContext* sc);
#endif // defined(GC_PROFILING) || defined(FEATURE_EVENT_TRACE)

    // scan for dead weak pointers
    static void GcWeakPtrScan (promote_func* fn, int condemned, int max_gen, ScanContext*sc );
    static void GcWeakPtrScanBySingleThread (int condemned, int max_gen, ScanContext*sc );

    // scan for dead weak pointers
    static void GcShortWeakPtrScan (promote_func* fn, int condemned, int max_gen, 
                                    ScanContext* sc);

    //
    // Dependent handle promotion scan support
    //

    // Perform initial (incomplete) scan which will deterimine if there's any further work required.
    static void GcDhInitialScan(promote_func* fn, int condemned, int max_gen, ScanContext* sc);

    // Called between scans to ask if any handles with an unpromoted secondary existed at the end of the last
    // scan.
    static bool GcDhUnpromotedHandlesExist(ScanContext* sc);

    // Rescan the handles for additonal primaries that have been promoted since the last scan. Return true if
    // any objects were promoted as a result.
    static bool GcDhReScan(ScanContext* sc);

    // post-promotions callback
    static void GcPromotionsGranted (int condemned, int max_gen, 
                                     ScanContext* sc);

    // post-promotions callback some roots were demoted
    static void GcDemote (int condemned, int max_gen, ScanContext* sc);

    static void GcEnumAllocContexts (enum_alloc_context_func* fn);

    static void GcFixAllocContexts (void* arg, void *heap);
    
    static size_t AskForMoreReservedMemory (size_t old_size, size_t need_size);

    static void VerifyHandleTable(int condemned, int max_gen, ScanContext* sc);
    
private:
#ifdef DACCESS_COMPILE    
    SVAL_DECL(int32_t, m_GcStructuresInvalidCnt);
#else
    static VOLATILE(int32_t) m_GcStructuresInvalidCnt;
#endif //DACCESS_COMPILE
};

#endif // _GCSCAN_H_
