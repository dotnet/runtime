// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// gchost.cpp
//
// This module contains the implementation for the IGCController interface.
// This interface is published through the gchost.idl file.  It allows a host
// environment to set config values for the GC.
//

//
//*****************************************************************************

//********** Includes *********************************************************

#include "common.h"
#include "vars.hpp"
#include "eeconfig.h"
#include "perfcounters.h"
#include "gchost.h"
#include "corhost.h"
#include "excep.h"
#include "field.h"
#include "gc.h"

#if !defined(FEATURE_CORECLR)
inline size_t SizeInKBytes(size_t cbSize)
{
    LIMITED_METHOD_CONTRACT;

    size_t cb = (cbSize % 1024) ? 1 : 0;
    return ((cbSize / 1024) + cb);
}

// IGCController

HRESULT CorGCHost::_SetGCSegmentSize(SIZE_T SegmentSize)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    // Sanity check the value, it must be a power of two and big enough.
    if (!GCHeap::IsValidSegmentSize(SegmentSize))
    {
        hr = E_INVALIDARG;
    }
    else
    {
        Host_SegmentSize = SegmentSize;
        Host_fSegmentSizeSet = TRUE;
    }

    return (hr);
}

HRESULT CorGCHost::_SetGCMaxGen0Size(SIZE_T MaxGen0Size)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    // Sanity check the value is at least large enough.
    if (!GCHeap::IsValidGen0MaxSize(MaxGen0Size))
    {
        hr = E_INVALIDARG;
    }
    else
    {
        Host_MaxGen0Size = MaxGen0Size;
        Host_fMaxGen0SizeSet = TRUE;
    }

    return (hr);
}

HRESULT CorGCHost::SetGCStartupLimits(
    DWORD SegmentSize,
    DWORD MaxGen0Size)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    // Set default overrides if specified by caller.
    if (SegmentSize != (DWORD) ~0 && SegmentSize > 0)
    {
        hr = _SetGCSegmentSize(SegmentSize);
    }

    if (SUCCEEDED(hr) && MaxGen0Size != (DWORD) ~0 && MaxGen0Size > 0)
    {
        hr = _SetGCMaxGen0Size(MaxGen0Size);
    }

    return (hr);
}

HRESULT CorGCHost::SetGCStartupLimitsEx( 
    SIZE_T SegmentSize,
    SIZE_T MaxGen0Size)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    // Set default overrides if specified by caller.
    if (SegmentSize != (SIZE_T) ~0 && SegmentSize > 0)
    {
        hr = _SetGCSegmentSize(SegmentSize);
    }

    if (SUCCEEDED(hr) && MaxGen0Size != (SIZE_T) ~0 && MaxGen0Size > 0)
    {
        hr = _SetGCMaxGen0Size(MaxGen0Size);
    }

    return (hr);
}

// Collect the requested generation.
HRESULT CorGCHost::Collect( 
    LONG       Generation)
{
    STATIC_CONTRACT_SO_TOLERANT;
    
    HRESULT     hr = E_FAIL;
    
    if (Generation > (int) GCHeap::GetGCHeap()->GetMaxGeneration())
        hr = E_INVALIDARG;
    else
    {
        // Set up a Thread object if this is called on a native thread.
        Thread *pThread;
        pThread = GetThread();
        if (pThread == NULL)
            pThread = SetupThreadNoThrow(&hr);

        if (pThread != NULL)
        {
            // Put thread into co-operative mode, which is how GC must run.
            GCX_COOP();
        
            BEGIN_SO_INTOLERANT_CODE_NO_THROW_CHECK_THREAD(return COR_E_STACKOVERFLOW);

            EX_TRY
            {			
                hr = GCHeap::GetGCHeap()->GarbageCollect(Generation);
            }
            EX_CATCH
            {
                hr = GET_EXCEPTION()->GetHR();
            }
            EX_END_CATCH(SwallowAllExceptions);

            END_SO_INTOLERANT_CODE;
        }
    }
    return (hr);
}


// Return GC counters in the gchost format.
HRESULT CorGCHost::GetStats( 
    COR_GC_STATS *pStats)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

#if defined(ENABLE_PERF_COUNTERS)

    Perf_GC     *pgc = &GetPerfCounters().m_GC;

    if (!pStats)
        return (E_INVALIDARG);

    if (pStats->Flags & COR_GC_COUNTS)
    {
        pStats->ExplicitGCCount = pgc->cInducedGCs;
        for (int idx=0; idx<3; idx++)
        {
            pStats->GenCollectionsTaken[idx] = pgc->cGenCollections[idx];
        }
    }
    
    if (pStats->Flags & COR_GC_MEMORYUSAGE)
    {
        pStats->CommittedKBytes = SizeInKBytes(pgc->cTotalCommittedBytes);
        pStats->ReservedKBytes = SizeInKBytes(pgc->cTotalReservedBytes);
        pStats->Gen0HeapSizeKBytes = SizeInKBytes(pgc->cGenHeapSize[0]);
        pStats->Gen1HeapSizeKBytes = SizeInKBytes(pgc->cGenHeapSize[1]);
        pStats->Gen2HeapSizeKBytes = SizeInKBytes(pgc->cGenHeapSize[2]);
        pStats->LargeObjectHeapSizeKBytes = SizeInKBytes(pgc->cLrgObjSize);
        pStats->KBytesPromotedFromGen0 = SizeInKBytes(pgc->cbPromotedMem[0]);
        pStats->KBytesPromotedFromGen1 = SizeInKBytes(pgc->cbPromotedMem[1]);
    }
    return (S_OK);
#else
    return (E_NOTIMPL);
#endif // ENABLE_PERF_COUNTERS
}

// Return per-thread allocation information.
HRESULT CorGCHost::GetThreadStats( 
    DWORD *pFiberCookie,
    COR_GC_THREAD_STATS *pStats)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    Thread      *pThread;

    // Get the thread from the caller or the current thread.
    if (!pFiberCookie)
        pThread = GetThread();
    else
        pThread = (Thread *) pFiberCookie;
    if (!pThread)
        return (E_INVALIDARG);
    
    return pThread->GetMemStats (pStats);
}

// Return per-thread allocation information.
HRESULT CorGCHost::SetVirtualMemLimit(
    SIZE_T sztMaxVirtualMemMB)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    GCHeap::GetGCHeap()->SetReservedVMLimit (sztMaxVirtualMemMB);
    return (S_OK);
}
#endif // !defined(FEATURE_CORECLR)


