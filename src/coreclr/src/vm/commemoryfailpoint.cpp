// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Class: COMMemoryFailPoint
**
**
** Purpose: Native methods for System.Runtime.MemoryFailPoint.
** These are to implement memory gates to limit allocations
** when progress will likely result in an OOM.
**
===========================================================*/
#include "common.h"

#include "frames.h"
#include "commemoryfailpoint.h"

// Need to know the maximum segment size for both the normal GC heap and the
// large object heap, as well as the top user-accessible address within the
// address space (ie, theoretically 2^31 - 1 on a 32 bit machine, but a tad 
// lower in practice).  This will help out with 32 bit machines running in 
// 3 GB mode.
FCIMPL2(void, COMMemoryFailPoint::GetMemorySettings, UINT64* pMaxGCSegmentSize, UINT64* pTopOfMemory)
{
    FCALL_CONTRACT;

    IGCHeap * pGC = GCHeapUtilities::GetGCHeap();
    size_t segment_size = pGC->GetValidSegmentSize(false);
    size_t large_segment_size = pGC->GetValidSegmentSize(true);
    _ASSERTE(segment_size < SIZE_T_MAX && large_segment_size < SIZE_T_MAX);
    if (segment_size > large_segment_size)
        *pMaxGCSegmentSize = (UINT64) segment_size;
    else
        *pMaxGCSegmentSize = (UINT64) large_segment_size;

    // GetTopMemoryAddress returns a void*, which can't be cast
    // directly to a UINT64 without causing an error from GCC.
    void * topOfMem = GetTopMemoryAddress();
    *pTopOfMemory = (UINT64) (size_t) topOfMem;
}
FCIMPLEND
