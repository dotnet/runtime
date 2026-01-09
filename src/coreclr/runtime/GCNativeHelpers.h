// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __GCNATIVEHELPERS_H__
#define __GCNATIVEHELPERS_H__

extern "C" INT64 QCALLTYPE GC_GetCurrentObjSize();
extern "C" INT64 QCALLTYPE GC_GetNow();
extern "C" INT64 QCALLTYPE GC_GetLastGCStartTime(INT32 generation);
extern "C" INT64 QCALLTYPE GC_GetLastGCDuration(INT32 generation);
extern "C" void QCALLTYPE GC_SendEtwAddMemoryPressureEvent(UINT64 bytesAllocated);
extern "C" void QCALLTYPE GC_SendEtwRemoveMemoryPressureEvent(UINT64 bytesAllocated);

#endif // __GCNATIVEHELPERS_H__
