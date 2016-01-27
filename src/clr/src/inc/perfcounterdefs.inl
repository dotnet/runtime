// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//-----------------------------------------------------------------------------
// PerfCounterDefs.inl
//
// Internal Interface for CLR to use Performance counters
//-----------------------------------------------------------------------------

#ifndef _PerfCounterDefs_inl_
#define _PerfCounterDefs_inl_

#include "perfcounterdefs.h"

inline Perf_GC::Perf_GC() {}

inline Perf_GC::Perf_GC(Perf_GC_Wow64& copyFrom)
{
    for (int index = 0; index < MAX_TRACKED_GENS; index++)
    {
        cGenCollections[index] = (size_t)copyFrom.cGenCollections[index];
        cGenHeapSize[index] = (size_t)copyFrom.cGenHeapSize[index];
    }
    for (int index = 0; index < MAX_TRACKED_GENS - 1; index++)
    {
        cbPromotedMem[index] = (size_t)copyFrom.cbPromotedMem[index];
    }

    cbPromotedFinalizationMem = (size_t) copyFrom.cbPromotedFinalizationMem;
    cProcessID = (size_t) copyFrom.cProcessID;
    cTotalCommittedBytes = (size_t) copyFrom.cTotalCommittedBytes;
    cTotalReservedBytes = (size_t) copyFrom.cTotalReservedBytes;
    cLrgObjSize = (size_t) copyFrom.cLrgObjSize;
    cSurviveFinalize = (size_t) copyFrom.cSurviveFinalize;
    cHandles = (size_t) copyFrom.cHandles;
    cbAlloc = (size_t) copyFrom.cbAlloc;
    cbLargeAlloc = (size_t) copyFrom.cbLargeAlloc;
    cInducedGCs = (size_t) copyFrom.cInducedGCs;
    timeInGC = copyFrom.timeInGC;
    timeInGCBase = copyFrom.timeInGCBase;
    cPinnedObj = (size_t) copyFrom.cPinnedObj;
    cSinkBlocks = (size_t) copyFrom.cSinkBlocks;        
}

inline Perf_Loading::Perf_Loading() {}

inline Perf_Loading::Perf_Loading(Perf_Loading_Wow64& copyFrom)
:   cClassesLoaded(copyFrom.cClassesLoaded),
    cAppDomains(copyFrom.cAppDomains),
    cAssemblies(copyFrom.cAssemblies),
    timeLoading(copyFrom.timeLoading),
    cAsmSearchLen(copyFrom.cAsmSearchLen),
    cLoadFailures (copyFrom.cLoadFailures),
    cbLoaderHeapSize ((size_t) copyFrom.cbLoaderHeapSize),
    cAppDomainsUnloaded (copyFrom.cAppDomainsUnloaded)
{
}

inline Perf_Security::Perf_Security() {};

inline Perf_Security::Perf_Security(Perf_Security_Wow64& copyFrom)
:   cTotalRTChecks(copyFrom.cTotalRTChecks),
    timeAuthorize(0),                   // Unused "reserved" field
    cLinkChecks(copyFrom.cLinkChecks),
    timeRTchecks(copyFrom.timeRTchecks),
    timeRTchecksBase(copyFrom.timeRTchecksBase),
    stackWalkDepth (copyFrom.stackWalkDepth)
{
}

inline PerfCounterIPCControlBlock::PerfCounterIPCControlBlock() {}

inline PerfCounterIPCControlBlock::PerfCounterIPCControlBlock(PerfCounterWow64IPCControlBlock& copyFrom)
:   m_cBytes (copyFrom.m_cBytes),
    m_wAttrs (copyFrom.m_wAttrs),
    m_GC (copyFrom.m_GC),
    m_Context (copyFrom.m_Context),
    m_Interop (copyFrom.m_Interop),
    m_Loading (copyFrom.m_Loading),
    m_Excep (copyFrom.m_Excep),
    m_LocksAndThreads (copyFrom.m_LocksAndThreads),
    m_Jit (copyFrom.m_Jit),
    m_Security (copyFrom.m_Security)
{
    _ASSERTE((size_t)m_cBytes == sizeof(PerfCounterWow64IPCControlBlock));
}

#endif // _PerfCounterDefs_inl_

