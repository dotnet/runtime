// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: GCHelpersNative.h
//

//
// Purpose: Native methods for GC
//

#ifndef _GCHELPERSNATIVE_H_
#define _GCHELPERSNATIVE_H_

#include "fcall.h"
#include "qcall.h"

const UINT MEM_PRESSURE_COUNT = 4;

struct GCGenerationInfo
{
    UINT64 sizeBefore;
    UINT64 fragmentationBefore;
    UINT64 sizeAfter;
    UINT64 fragmentationAfter;
};

#include "pshpack4.h"
class GCMemoryInfoData : public Object
{
public:
    UINT64 highMemLoadThresholdBytes;
    UINT64 totalAvailableMemoryBytes;
    UINT64 lastRecordedMemLoadBytes;
    UINT64 lastRecordedHeapSizeBytes;
    UINT64 lastRecordedFragmentationBytes;
    UINT64 totalCommittedBytes;
    UINT64 promotedBytes;
    UINT64 pinnedObjectCount;
    UINT64 finalizationPendingCount;
    UINT64 index;
    UINT32 generation;
    UINT32 pauseTimePercent;
    UINT8 isCompaction;
    UINT8 isConcurrent;
#ifndef UNIX_X86_ABI
    UINT8 padding[6];
#endif
    GCGenerationInfo generationInfo0;
    GCGenerationInfo generationInfo1;
    GCGenerationInfo generationInfo2;
    GCGenerationInfo generationInfo3;
    GCGenerationInfo generationInfo4;
    UINT64 pauseDuration0;
    UINT64 pauseDuration1;
};
#include "poppack.h"

#ifdef USE_CHECKED_OBJECTREFS
typedef REF<GCMemoryInfoData> GCMEMORYINFODATA;
typedef REF<GCMemoryInfoData> GCMEMORYINFODATAREF;
#else // USE_CHECKED_OBJECTREFS
typedef GCMemoryInfoData * GCMEMORYINFODATA;
typedef GCMemoryInfoData * GCMEMORYINFODATAREF;
#endif // USE_CHECKED_OBJECTREFS

using EnumerateConfigurationValuesCallback = void (*)(void* context, const char* name, const char* publicKey, GCConfigurationType type, int64_t data);

struct GCHeapHardLimitInfo
{
    UINT64 heapHardLimit;
    UINT64 heapHardLimitPercent;
    UINT64 heapHardLimitSOH;
    UINT64 heapHardLimitLOH;
    UINT64 heapHardLimitPOH;
    UINT64 heapHardLimitSOHPercent;
    UINT64 heapHardLimitLOHPercent;
    UINT64 heapHardLimitPOHPercent;
};

class GCInterface {
private:
    static INT32    m_gc_counts[3];

    static UINT64   m_addPressure[MEM_PRESSURE_COUNT];
    static UINT64   m_remPressure[MEM_PRESSURE_COUNT];
    static UINT     m_iteration;

public:
    static FORCEINLINE UINT64 InterlockedAdd(UINT64 *pAugend, UINT64 addend);
    static FORCEINLINE UINT64 InterlockedSub(UINT64 *pMinuend, UINT64 subtrahend);

    static FCDECL0(INT64,   GetTotalPauseDuration);
    static FCDECL2(void,    GetMemoryInfo, Object* objUNSAFE, int kind);
    static FCDECL0(UINT32,  GetMemoryLoad);
    static FCDECL0(int,     GetGcLatencyMode);
    static FCDECL1(int,     SetGcLatencyMode, int newLatencyMode);
    static FCDECL0(int,     GetLOHCompactionMode);
    static FCDECL1(void,    SetLOHCompactionMode, int newLOHCompactionyMode);
    static FCDECL2(FC_BOOL_RET, RegisterForFullGCNotification, UINT32 gen2Percentage, UINT32 lohPercentage);
    static FCDECL0(FC_BOOL_RET, CancelFullGCNotification);
    static FCDECL1(int,     GetGenerationInternal, Object* objUNSAFE);
    static FCDECL0(UINT64,  GetSegmentSize);
    static FCDECL0(int,     GetLastGCPercentTimeInGC);
    static FCDECL1(UINT64,  GetGenerationSize, int gen);

    static FCDECL0(int,     GetMaxGeneration);
    static FCDECL0(FC_BOOL_RET, IsServerGC);
    static FCDECL1(void,    KeepAlive, Object *obj);
    static FCDECL1(void,    SuppressFinalize, Object *obj);
    static FCDECL2(int,     CollectionCount, INT32 generation, INT32 getSpecialGCCount);

    static FCDECL0(INT64,    GetAllocatedBytesForCurrentThread);
    static FCDECL0(INT64,    GetTotalAllocatedBytesApproximate);

    NOINLINE static void SendEtwRemoveMemoryPressureEvent(UINT64 bytesAllocated);
    static void SendEtwAddMemoryPressureEvent(UINT64 bytesAllocated);

    static void CheckCollectionCount();
    static void RemoveMemoryPressure(UINT64 bytesAllocated);
    static void AddMemoryPressure(UINT64 bytesAllocated);

    static void EnumerateConfigurationValues(void* configurationContext, EnumerateConfigurationValuesCallback callback);
    static int  RefreshMemoryLimit();
    static enable_no_gc_region_callback_status EnableNoGCRegionCallback(NoGCRegionCallbackFinalizerWorkItem* callback, INT64 totalSize);
    static uint64_t GetGenerationBudget(int generation);

private:
    // Out-of-line helper to avoid EH prolog/epilog in functions that otherwise don't throw.
    NOINLINE static void GarbageCollectModeAny(int generation);
};

extern "C" INT64 QCALLTYPE GCInterface_GetTotalAllocatedBytesPrecise();

extern "C" void QCALLTYPE GCInterface_AllocateNewArray(void* typeHandlePtr, INT32 length, INT32 flags, QCall::ObjectHandleOnStack ret);

extern "C" INT64 QCALLTYPE GCInterface_GetTotalMemory();

extern "C" void QCALLTYPE GCInterface_Collect(INT32 generation, INT32 mode, CLR_BOOL lowMemoryPressure);

extern "C" void* QCALLTYPE GCInterface_GetNextFinalizableObject(QCall::ObjectHandleOnStack pObj);

extern "C" void QCALLTYPE GCInterface_WaitForPendingFinalizers();
#ifdef FEATURE_BASICFREEZE
extern "C" void* QCALLTYPE GCInterface_RegisterFrozenSegment(void *pSection, SIZE_T sizeSection);

extern "C" void QCALLTYPE GCInterface_UnregisterFrozenSegment(void *segmentHandle);
#endif // FEATURE_BASICFREEZE

extern "C" int QCALLTYPE GCInterface_WaitForFullGCApproach(int millisecondsTimeout);

extern "C" int QCALLTYPE GCInterface_WaitForFullGCComplete(int millisecondsTimeout);

extern "C" int QCALLTYPE GCInterface_StartNoGCRegion(INT64 totalSize, BOOL lohSizeKnown, INT64 lohSize, BOOL disallowFullBlockingGC);

extern "C" int QCALLTYPE GCInterface_EndNoGCRegion();

extern "C" void QCALLTYPE GCInterface_AddMemoryPressure(UINT64 bytesAllocated);

extern "C" void QCALLTYPE GCInterface_RemoveMemoryPressure(UINT64 bytesAllocated);

extern "C" void QCALLTYPE GCInterface_ReRegisterForFinalize(QCall::ObjectHandleOnStack pObj);

extern "C" void QCALLTYPE GCInterface_EnumerateConfigurationValues(void* configurationContext, EnumerateConfigurationValuesCallback callback);

extern "C" int  QCALLTYPE GCInterface_RefreshMemoryLimit(GCHeapHardLimitInfo heapHardLimitInfo);

extern "C" enable_no_gc_region_callback_status QCALLTYPE GCInterface_EnableNoGCRegionCallback(NoGCRegionCallbackFinalizerWorkItem* callback, INT64 totalSize);

extern "C" uint64_t QCALLTYPE GCInterface_GetGenerationBudget(int generation);

#endif // _GCHELPERSNATIVE_H_
