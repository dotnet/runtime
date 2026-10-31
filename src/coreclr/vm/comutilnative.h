// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
** Header:  COMUtilNative
**
** Purpose: A dumping ground for classes which aren't large
** enough to get their own file in the VM.
===========================================================*/

#ifndef _COMUTILNATIVE_H_
#define _COMUTILNATIVE_H_

#include "object.h"
#include "util.hpp"
#include "cgensys.h"
#include "fcall.h"
#include "qcall.h"
#include "windows.h"

//
//
// EXCEPTION NATIVE
//
//

class ExceptionNative
{
public:
    FCDECL1(static FC_BOOL_RET, IsTransient, INT32 hresult);

#ifdef FEATURE_COMINTEROP
    // NOTE: caller cleans up any partially initialized BSTRs in pED
    static void      GetExceptionData(OBJECTREF, ExceptionData *);
#endif
};

extern "C"
{
    FCDECL1(FC_BOOL_RET, Exception_IsImmutableAgileException, Object* pExceptionUNSAFE);
    FCDECL0(VOID, Exception_PrepareForForeignExceptionRaise);
    // Note: these are on the PInvoke class to hide these from the user.
    FCDECL0(EXCEPTION_POINTERS*, Marshal_GetExceptionPointers);
    FCDECL0(INT32, Marshal_GetExceptionCode);
    FCDECL0(UINT32, Exception_GetExceptionCount);
}

extern "C" void QCALLTYPE ExceptionNative_GetFrozenStackTrace(QCall::ObjectHandleOnStack exception, QCall::ObjectHandleOnStack ret, QCallExceptionStatus* qcallError);

enum class ExceptionMessageKind {
    ThreadAbort = 1,
    ThreadInterrupted = 2,
    OutOfMemory = 3
};
extern "C" void QCALLTYPE ExceptionNative_GetMessageFromNativeResources(ExceptionMessageKind kind, QCall::StringHandleOnStack retMesg, QCallExceptionStatus* qcallError);

extern "C" void QCALLTYPE ExceptionNative_GetMethodFromStackTrace(QCall::ObjectHandleOnStack array, QCall::ObjectHandleOnStack retMethodInfo, QCallExceptionStatus* qcallError);

extern "C" void QCALLTYPE ExceptionNative_ThrowAmbiguousResolutionException(
    MethodTable* pTargetClass,
    MethodTable* pInterfaceMT,
    MethodDesc* pInterfaceMD,
    QCallExceptionStatus* qcallError);

extern "C" void QCALLTYPE ExceptionNative_ThrowEntryPointNotFoundException(
    MethodTable* pTargetClass,
    MethodTable* pInterfaceMT,
    MethodDesc* pInterfaceMD,
    QCallExceptionStatus* qcallError);

extern "C" void QCALLTYPE ExceptionNative_ThrowMethodAccessException(MethodDesc* caller, MethodDesc* callee, QCallExceptionStatus* qcallError);
extern "C" void QCALLTYPE ExceptionNative_ThrowFieldAccessException(MethodDesc* caller, FieldDesc* callee, QCallExceptionStatus* qcallError);
extern "C" void QCALLTYPE ExceptionNative_ThrowClassAccessException(MethodDesc* caller, EnregisteredTypeHandle callee, QCallExceptionStatus* qcallError);

//
// Buffer
//
extern "C"
{
    FCDECL3(VOID, Buffer_BulkMoveWithWriteBarrierInternal, void *dst, void *src, size_t byteCount);
}

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
    GCGenerationInfo generationInfo[5];
    UINT64 pauseDurations[2];
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

extern "C"
{
    FCDECL0(INT64,   GC__GetTotalPauseDuration);
    FCDECL2(void,    GC_GetMemoryInfo, Object* objUNSAFE, int kind);
    FCDECL0(UINT32,  GC_GetMemoryLoad);
    FCDECL0(int,     GCSettings_GetGCLatencyMode);
    FCDECL1(int,     GCSettings_SetGCLatencyMode, int newLatencyMode);
    FCDECL0(int,     GCSettings_GetLOHCompactionMode);
    FCDECL1(void,    GCSettings_SetLOHCompactionMode, int newLOHCompactionyMode);
    FCDECL2(FC_BOOL_RET, GC__RegisterForFullGCNotification, UINT32 gen2Percentage, UINT32 lohPercentage);
    FCDECL0(FC_BOOL_RET, GC__CancelFullGCNotification);
    FCDECL1(int,     GC_GetGenerationInternal, Object* objUNSAFE);
    FCDECL0(UINT64,  GC_GetSegmentSize);
    FCDECL0(int,     GC_GetLastGCPercentTimeInGC);
    FCDECL1(UINT64,  GC_GetGenerationSize, int gen);
    FCDECL0(int,     GC_GetMaxGeneration);
    FCDECL0(FC_BOOL_RET, GCSettings_get_IsServerGC);
    FCDECL1(void,    GC_SuppressFinalizeInternal, Object *obj);
    FCDECL2(int,     GC__CollectionCount, INT32 generation, INT32 getSpecialGCCount);
    FCDECL0(INT64,    GC_GetAllocatedBytesForCurrentThread);
    FCDECL0(INT64,    GC_GetTotalAllocatedBytesApproximate);
}

extern "C" INT64 QCALLTYPE GCInterface_GetTotalAllocatedBytesPrecise(QCallExceptionStatus* qcallError);

extern "C" void QCALLTYPE GCInterface_AllocateNewArray(void* typeHandlePtr, INT32 length, INT32 flags, QCall::ObjectHandleOnStack ret, QCallExceptionStatus* qcallError);

extern "C" INT64 QCALLTYPE GCInterface_GetTotalMemory(QCallExceptionStatus* qcallError);

extern "C" void QCALLTYPE GCInterface_Collect(INT32 generation, INT32 mode, CLR_BOOL lowMemoryPressure, QCallExceptionStatus* qcallError);

extern "C" void* QCALLTYPE GCInterface_GetNextFinalizableObject(QCall::ObjectHandleOnStack pObj, QCallExceptionStatus* qcallError);

extern "C" void QCALLTYPE GCInterface_WaitForPendingFinalizers(QCallExceptionStatus* qcallError);
extern "C" void* QCALLTYPE GCInterface_RegisterFrozenSegment(void *pSection, SIZE_T sizeSection, QCallExceptionStatus* qcallError);

extern "C" void QCALLTYPE GCInterface_UnregisterFrozenSegment(void *segmentHandle, QCallExceptionStatus* qcallError);

extern "C" int QCALLTYPE GCInterface_WaitForFullGCApproach(int millisecondsTimeout, QCallExceptionStatus* qcallError);

extern "C" int QCALLTYPE GCInterface_WaitForFullGCComplete(int millisecondsTimeout, QCallExceptionStatus* qcallError);

extern "C" int QCALLTYPE GCInterface_StartNoGCRegion(INT64 totalSize, BOOL lohSizeKnown, INT64 lohSize, BOOL disallowFullBlockingGC, QCallExceptionStatus* qcallError);

extern "C" int QCALLTYPE GCInterface_EndNoGCRegion(QCallExceptionStatus* qcallError);

extern "C" void QCALLTYPE GCInterface_AddMemoryPressure(UINT64 bytesAllocated, QCallExceptionStatus* qcallError);

extern "C" void QCALLTYPE GCInterface_RemoveMemoryPressure(UINT64 bytesAllocated, QCallExceptionStatus* qcallError);

extern "C" void QCALLTYPE GCInterface_ReRegisterForFinalize(QCall::ObjectHandleOnStack pObj, QCallExceptionStatus* qcallError);

extern "C" void QCALLTYPE GCInterface_EnumerateConfigurationValues(void* configurationContext, EnumerateConfigurationValuesCallback callback, QCallExceptionStatus* qcallError);

extern "C" int  QCALLTYPE GCInterface_RefreshMemoryLimit(GCHeapHardLimitInfo heapHardLimitInfo, QCallExceptionStatus* qcallError);

extern "C" enable_no_gc_region_callback_status QCALLTYPE GCInterface_EnableNoGCRegionCallback(NoGCRegionCallbackFinalizerWorkItem* callback, INT64 totalSize, QCallExceptionStatus* qcallError);

extern "C" uint64_t QCALLTYPE GCInterface_GetGenerationBudget(int generation, QCallExceptionStatus* qcallError);

//
// EnvironmentNative
//
extern "C"
{
    // Functions on the System.Environment class
    FCDECL1(VOID,Environment_set_ExitCode,INT32 exitcode);
    FCDECL0(INT32, Environment_get_ExitCode);
}

extern "C" void QCALLTYPE Environment_Exit(INT32 exitcode, QCallExceptionStatus* qcallError);

extern "C" void QCALLTYPE Environment_FailFast(QCall::StackCrawlMarkHandle mark, PCWSTR message, QCall::ObjectHandleOnStack exception, PCWSTR errorSource, QCallExceptionStatus* qcallError);

// Returns the number of logical processors that can be used by managed code
extern "C" INT32 QCALLTYPE Environment_GetProcessorCount(QCallExceptionStatus* qcallError);

extern "C" void QCALLTYPE GetTypeLoadExceptionMessage(UINT32 resId, QCall::StringHandleOnStack retString, QCallExceptionStatus* qcallError);

extern "C" void QCALLTYPE GetFileLoadExceptionMessage(UINT32 hr, QCall::StringHandleOnStack retString, QCallExceptionStatus* qcallError);

extern "C" void QCALLTYPE FileLoadException_GetMessageForHR(UINT32 hresult, QCall::StringHandleOnStack retString, QCallExceptionStatus* qcallError);

extern "C"
{
    FCDECL1(INT32, RuntimeHelpers_TryGetHashCode, Object* vThisRef);
    FCDECL2(FC_BOOL_RET, RuntimeHelpers_ContentEquals, Object *pThisRef, Object *pCompareRef);
}

extern "C" INT32 QCALLTYPE ObjectNative_GetHashCodeSlow(QCall::ObjectHandleOnStack objHandle, QCallExceptionStatus* qcallError);
extern "C" void QCALLTYPE ObjectNative_AllocateUninitializedClone(QCall::ObjectHandleOnStack objHandle, QCallExceptionStatus* qcallError);

extern "C"
{
    FCDECL2(INT32, Interlocked_Exchange32, INT32 *location, INT32 value);
    FCDECL2_IV(INT64, Interlocked_Exchange64, INT64 *location, INT64 value);
    FCDECL3(INT32, Interlocked_CompareExchange32, INT32* location, INT32 value, INT32 comparand);
#ifdef TARGET_WASM
    decltype(Interlocked_CompareExchange32) Interlocked_CompareExchange32Pointer;
#endif // TARGET_WASM
    FCDECL3_IVV(INT64, Interlocked_CompareExchange64, INT64* location, INT64 value, INT64 comparand);
    FCDECL2(LPVOID, Interlocked_ExchangeObject, LPVOID* location, LPVOID value);
    FCDECL3(LPVOID, Interlocked_CompareExchangeObject, LPVOID* location, LPVOID value, LPVOID comparand);
    FCDECL2(INT32, Interlocked_ExchangeAdd32, INT32 *location, INT32 value);
    FCDECL2_IV(INT64, Interlocked_ExchangeAdd64, INT64 *location, INT64 value);
}

extern "C"
{
    FCDECL1(UINT32, MethodTable_GetNumInstanceFieldBytes, MethodTable* mt);
    FCDECL1(CorElementType, MethodTable_GetPrimitiveCorElementType, MethodTable* mt);
    FCDECL2(MethodTable*, MethodTable_GetMethodTableMatchingParentClass, MethodTable* mt, MethodTable* parent);
    FCDECL1(OBJECTHANDLE, MethodTable_GetLoaderAllocatorHandle, MethodTable* mt);
}

extern "C" BOOL QCALLTYPE MethodTable_AreTypesEquivalent(MethodTable* mta, MethodTable* mtb, QCallExceptionStatus* qcallError);
extern "C" BOOL QCALLTYPE MethodTable_CanCompareBitsOrUseFastGetHashCode(MethodTable* mt, QCallExceptionStatus* qcallError);
extern "C" BOOL QCALLTYPE TypeHandle_CanCastTo_NoCacheLookup(void* fromTypeHnd, void* toTypeHnd, QCallExceptionStatus* qcallError);
extern "C" INT32 QCALLTYPE TypeHandle_GetCorElementType(void* typeHnd);
extern "C" INT32 QCALLTYPE ValueType_GetHashCodeStrategy(MethodTable* mt, QCall::ObjectHandleOnStack objHandle, UINT32* fieldOffset, UINT32* fieldSize, MethodTable** fieldMT, QCallExceptionStatus* qcallError);

BOOL CanCompareBitsOrUseFastGetHashCode(MethodTable* mt);

extern "C" BOOL QCALLTYPE Stream_HasOverriddenSlow(MethodTable* pMT, BOOL isRead, QCallExceptionStatus* qcallError);

#endif // _COMUTILNATIVE_H_
