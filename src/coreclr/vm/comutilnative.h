// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//

/*============================================================
**
** Header:  COMUtilNative
**
**
** Purpose: A dumping ground for classes which aren't large
** enough to get their own file in the VM.
**
**
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
    FCDECL1(static FC_BOOL_RET, IsImmutableAgileException, Object* pExceptionUNSAFE);
    FCDECL1(static FC_BOOL_RET, IsTransient, INT32 hresult);
    FCDECL0(static VOID, PrepareForForeignExceptionRaise);

#ifdef FEATURE_COMINTEROP
    // NOTE: caller cleans up any partially initialized BSTRs in pED
    static void      GetExceptionData(OBJECTREF, ExceptionData *);
#endif

    // Note: these are on the PInvoke class to hide these from the user.
    FCDECL0(static EXCEPTION_POINTERS*, GetExceptionPointers);
    FCDECL0(static INT32, GetExceptionCode);
    FCDECL0(static UINT32, GetExceptionCount);
};

extern "C" void QCALLTYPE ExceptionNative_GetFrozenStackTrace(QCall::ObjectHandleOnStack exception, QCall::ObjectHandleOnStack ret);

enum class ExceptionMessageKind {
    ThreadAbort = 1,
    ThreadInterrupted = 2,
    OutOfMemory = 3
};
extern "C" void QCALLTYPE ExceptionNative_GetMessageFromNativeResources(ExceptionMessageKind kind, QCall::StringHandleOnStack retMesg);

extern "C" void QCALLTYPE ExceptionNative_GetMethodFromStackTrace(QCall::ObjectHandleOnStack array, QCall::ObjectHandleOnStack retMethodInfo);

extern "C" void QCALLTYPE ExceptionNative_ThrowAmbiguousResolutionException(
    MethodTable* pTargetClass,
    MethodTable* pInterfaceMT,
    MethodDesc* pInterfaceMD);

extern "C" void QCALLTYPE ExceptionNative_ThrowEntryPointNotFoundException(
    MethodTable* pTargetClass,
    MethodTable* pInterfaceMT,
    MethodDesc* pInterfaceMD);

extern "C" void QCALLTYPE ExceptionNative_ThrowMethodAccessException(MethodDesc* caller, MethodDesc* callee);
extern "C" void QCALLTYPE ExceptionNative_ThrowFieldAccessException(MethodDesc* caller, FieldDesc* callee);
extern "C" void QCALLTYPE ExceptionNative_ThrowClassAccessException(MethodDesc* caller, EnregisteredTypeHandle callee);

//
// Buffer
//
class Buffer
{
public:
    FCDECL3(static VOID, BulkMoveWithWriteBarrier, void *dst, void *src, size_t byteCount);
};

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

    FCDECL0(static INT64,   GetTotalPauseDuration);
    FCDECL2(static void,    GetMemoryInfo, Object* objUNSAFE, int kind);
    FCDECL0(static UINT32,  GetMemoryLoad);
    FCDECL0(static int,     GetGcLatencyMode);
    FCDECL1(static int,     SetGcLatencyMode, int newLatencyMode);
    FCDECL0(static int,     GetLOHCompactionMode);
    FCDECL1(static void,    SetLOHCompactionMode, int newLOHCompactionyMode);
    FCDECL2(static FC_BOOL_RET, RegisterForFullGCNotification, UINT32 gen2Percentage, UINT32 lohPercentage);
    FCDECL0(static FC_BOOL_RET, CancelFullGCNotification);
    FCDECL1(static int,     GetGenerationInternal, Object* objUNSAFE);
    FCDECL0(static UINT64,  GetSegmentSize);
    FCDECL0(static int,     GetLastGCPercentTimeInGC);
    FCDECL1(static UINT64,  GetGenerationSize, int gen);

    FCDECL0(static int,     GetMaxGeneration);
    FCDECL0(static FC_BOOL_RET, IsServerGC);
    FCDECL1(static void,    SuppressFinalize, Object *obj);
    FCDECL2(static int,     CollectionCount, INT32 generation, INT32 getSpecialGCCount);

    FCDECL0(static INT64,    GetAllocatedBytesForCurrentThread);
    FCDECL0(static INT64,    GetTotalAllocatedBytesApproximate);

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

//
// EnvironmentNative
//
class EnvironmentNative
{
public:
    // Functions on the System.Environment class
    FCDECL1(static VOID,SetExitCode,INT32 exitcode);
    FCDECL0(static INT32, GetExitCode);
};

extern "C" void QCALLTYPE Environment_Exit(INT32 exitcode);

extern "C" void QCALLTYPE Environment_FailFast(QCall::StackCrawlMarkHandle mark, PCWSTR message, QCall::ObjectHandleOnStack exception, PCWSTR errorSource);

// Returns the number of logical processors that can be used by managed code
extern "C" INT32 QCALLTYPE Environment_GetProcessorCount();

extern "C" void QCALLTYPE GetTypeLoadExceptionMessage(UINT32 resId, QCall::StringHandleOnStack retString);

extern "C" void QCALLTYPE GetFileLoadExceptionMessage(UINT32 hr, QCall::StringHandleOnStack retString);

extern "C" void QCALLTYPE FileLoadException_GetMessageForHR(UINT32 hresult, QCall::StringHandleOnStack retString);

class ObjectNative
{
public:
    FCDECL1(static INT32, TryGetHashCode, Object* vThisRef);
    FCDECL2(static FC_BOOL_RET, ContentEquals, Object *pThisRef, Object *pCompareRef);
};

extern "C" INT32 QCALLTYPE ObjectNative_GetHashCodeSlow(QCall::ObjectHandleOnStack objHandle);
extern "C" void QCALLTYPE ObjectNative_AllocateUninitializedClone(QCall::ObjectHandleOnStack objHandle);

class COMInterlocked
{
public:
        FCDECL2(static INT32, Exchange32, INT32 *location, INT32 value);
        FCDECL2_IV(static INT64, Exchange64, INT64 *location, INT64 value);
        FCDECL3(static INT32, CompareExchange32, INT32* location, INT32 value, INT32 comparand);
        FCDECL3_IVV(static INT64, CompareExchange64, INT64* location, INT64 value, INT64 comparand);
        FCDECL2(static LPVOID, ExchangeObject, LPVOID* location, LPVOID value);
        FCDECL3(static LPVOID, CompareExchangeObject, LPVOID* location, LPVOID value, LPVOID comparand);
        FCDECL2(static INT32, ExchangeAdd32, INT32 *location, INT32 value);
        FCDECL2_IV(static INT64, ExchangeAdd64, INT64 *location, INT64 value);
};

class MethodTableNative {
public:
    FCDECL1(static UINT32, GetNumInstanceFieldBytes, MethodTable* mt);
    FCDECL1(static CorElementType, GetPrimitiveCorElementType, MethodTable* mt);
    FCDECL2(static MethodTable*, GetMethodTableMatchingParentClass, MethodTable* mt, MethodTable* parent);
    FCDECL1(static MethodTable*, InstantiationArg0, MethodTable* mt);
    FCDECL1(static OBJECTHANDLE, GetLoaderAllocatorHandle, MethodTable* mt);
};

extern "C" BOOL QCALLTYPE MethodTable_AreTypesEquivalent(MethodTable* mta, MethodTable* mtb);
extern "C" BOOL QCALLTYPE MethodTable_CanCompareBitsOrUseFastGetHashCode(MethodTable* mt);
extern "C" BOOL QCALLTYPE TypeHandle_CanCastTo_NoCacheLookup(void* fromTypeHnd, void* toTypeHnd);
extern "C" INT32 QCALLTYPE TypeHandle_GetCorElementType(void* typeHnd);
extern "C" INT32 QCALLTYPE ValueType_GetHashCodeStrategy(MethodTable* mt, QCall::ObjectHandleOnStack objHandle, UINT32* fieldOffset, UINT32* fieldSize, MethodTable** fieldMT);

BOOL CanCompareBitsOrUseFastGetHashCode(MethodTable* mt);

extern "C" BOOL QCALLTYPE Stream_HasOverriddenSlow(MethodTable* pMT, BOOL isRead);

#endif // _COMUTILNATIVE_H_
