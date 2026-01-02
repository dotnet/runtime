// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "GCNativeHelpers.h"
#include "gcheaputilities.h"

extern "C" INT64 QCALLTYPE GC_GetCurrentObjSize()
{
    return (INT64)GCHeapUtilities::GetGCHeap()->GetCurrentObjSize();
}

extern "C" INT64 QCALLTYPE GC_GetNow()
{
    return (INT64)GCHeapUtilities::GetGCHeap()->GetNow();
}

extern "C" INT64 QCALLTYPE GC_GetLastGCStartTime(INT32 generation)
{
    return (INT64)GCHeapUtilities::GetGCHeap()->GetLastGCStartTime(generation);
}

extern "C" INT64 QCALLTYPE GC_GetLastGCDuration(INT32 generation)
{
    return (INT64)GCHeapUtilities::GetGCHeap()->GetLastGCDuration(generation);
}

extern "C" void QCALLTYPE GC_SendEtwAddMemoryPressureEvent(UINT64 bytesAllocated)
{
    FireEtwIncreaseMemoryPressure(bytesAllocated, GetClrInstanceId());
}

extern "C" void QCALLTYPE GC_SendEtwRemoveMemoryPressureEvent(UINT64 bytesAllocated)
{
    FireEtwDecreaseMemoryPressure(bytesAllocated, GetClrInstanceId());
}
