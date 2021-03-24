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
#undef GetCurrentTime

//
//
// EXCEPTION NATIVE
//
//

void FreeExceptionData(ExceptionData *pedata);

class ExceptionNative
{
private:
    enum ExceptionMessageKind {
        ThreadAbort = 1,
        ThreadInterrupted = 2,
        OutOfMemory = 3
    };

public:
    static FCDECL1(FC_BOOL_RET, IsImmutableAgileException, Object* pExceptionUNSAFE);
    static FCDECL1(FC_BOOL_RET, IsTransient, INT32 hresult);
    static FCDECL3(StringObject *, StripFileInfo, Object *orefExcepUNSAFE, StringObject *orefStrUNSAFE, CLR_BOOL isRemoteStackTrace);
    static void QCALLTYPE GetMessageFromNativeResources(ExceptionMessageKind kind, QCall::StringHandleOnStack retMesg);
    static FCDECL0(VOID, PrepareForForeignExceptionRaise);
    static FCDECL3(VOID, GetStackTracesDeepCopy, Object* pExceptionObjectUnsafe, Object **pStackTraceUnsafe, Object **pDynamicMethodsUnsafe);
    static FCDECL3(VOID, SaveStackTracesFromDeepCopy, Object* pExceptionObjectUnsafe, Object *pStackTraceUnsafe, Object *pDynamicMethodsUnsafe);


    // NOTE: caller cleans up any partially initialized BSTRs in pED
    static void      GetExceptionData(OBJECTREF, ExceptionData *);

    // Note: these are on the PInvoke class to hide these from the user.
    static FCDECL0(EXCEPTION_POINTERS*, GetExceptionPointers);
    static FCDECL0(INT32, GetExceptionCode);
    static FCDECL0(UINT32, GetExceptionCount);
};

//
// Buffer
//
class Buffer
{
public:
    static FCDECL3(VOID, BulkMoveWithWriteBarrier, void *dst, void *src, size_t byteCount);

    static void QCALLTYPE MemMove(void *dst, void *src, size_t length);
    static void QCALLTYPE Clear(void *dst, size_t length);
};

const UINT MEM_PRESSURE_COUNT = 4;

struct GCGenerationInfo
{
    UINT64 sizeBefore;
    UINT64 fragmentationBefore;
    UINT64 sizeAfter;
    UINT64 fragmentationAfter;
};

#if defined(TARGET_X86) && !defined(TARGET_UNIX)
#include "pshpack4.h"
#ifdef _MSC_VER 
#pragma warning(push)
#pragma warning(disable:4121) // alignment of a member was sensitive to packing
#endif
#endif
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
    GCGenerationInfo generationInfo0;
    GCGenerationInfo generationInfo1;
    GCGenerationInfo generationInfo2;
    GCGenerationInfo generationInfo3;
    GCGenerationInfo generationInfo4;
    UINT64 pauseDuration0;
    UINT64 pauseDuration1;
};
#if defined(TARGET_X86) && !defined(TARGET_UNIX)
#ifdef _MSC_VER
#pragma warning(pop)
#endif
#include "poppack.h"
#endif

#ifdef USE_CHECKED_OBJECTREFS
typedef REF<GCMemoryInfoData> GCMEMORYINFODATA;
typedef REF<GCMemoryInfoData> GCMEMORYINFODATAREF;
#else // USE_CHECKED_OBJECTREFS
typedef GCMemoryInfoData * GCMEMORYINFODATA;
typedef GCMemoryInfoData * GCMEMORYINFODATAREF;
#endif // USE_CHECKED_OBJECTREFS


class GCInterface {
private:
    static INT32    m_gc_counts[3];

    static UINT64   m_addPressure[MEM_PRESSURE_COUNT];
    static UINT64   m_remPressure[MEM_PRESSURE_COUNT];
    static UINT     m_iteration;

public:
    static FORCEINLINE UINT64 InterlockedAdd(UINT64 *pAugend, UINT64 addend);
    static FORCEINLINE UINT64 InterlockedSub(UINT64 *pMinuend, UINT64 subtrahend);

    static FCDECL2(void,    GetMemoryInfo, Object* objUNSAFE, int kind);
    static FCDECL0(UINT32,  GetMemoryLoad);
    static FCDECL0(int,     GetGcLatencyMode);
    static FCDECL1(int,     SetGcLatencyMode, int newLatencyMode);
    static FCDECL0(int,     GetLOHCompactionMode);
    static FCDECL1(void,    SetLOHCompactionMode, int newLOHCompactionyMode);
    static FCDECL2(FC_BOOL_RET, RegisterForFullGCNotification, UINT32 gen2Percentage, UINT32 lohPercentage);
    static FCDECL0(FC_BOOL_RET, CancelFullGCNotification);
    static FCDECL1(int,     WaitForFullGCApproach, int millisecondsTimeout);
    static FCDECL1(int,     WaitForFullGCComplete, int millisecondsTimeout);
    static FCDECL1(int,     GetGenerationWR, LPVOID handle);
    static FCDECL1(int,     GetGeneration, Object* objUNSAFE);
    static FCDECL0(UINT64,  GetSegmentSize);
    static FCDECL0(int,     GetLastGCPercentTimeInGC);
    static FCDECL1(UINT64,  GetGenerationSize, int gen);
    static
    INT64 QCALLTYPE GetTotalMemory();

    static
    void QCALLTYPE Collect(INT32 generation, INT32 mode);

    static
    void QCALLTYPE WaitForPendingFinalizers();

    static FCDECL0(int,     GetMaxGeneration);
    static FCDECL1(void,    KeepAlive, Object *obj);
    static FCDECL1(void,    SuppressFinalize, Object *obj);
    static FCDECL1(void,    ReRegisterForFinalize, Object *obj);
    static FCDECL2(int,     CollectionCount, INT32 generation, INT32 getSpecialGCCount);

    static FCDECL0(INT64,    GetAllocatedBytesForCurrentThread);
    static FCDECL1(INT64,    GetTotalAllocatedBytes, CLR_BOOL precise);

    static FCDECL3(Object*, AllocateNewArray, void* elementTypeHandle, INT32 length, INT32 flags);

#ifdef FEATURE_BASICFREEZE
    static
    void* QCALLTYPE RegisterFrozenSegment(void *pSection, SIZE_T sizeSection);

    static
    void QCALLTYPE UnregisterFrozenSegment(void *segmentHandle);
#endif // FEATURE_BASICFREEZE

    static
    int QCALLTYPE StartNoGCRegion(INT64 totalSize, BOOL lohSizeKnown, INT64 lohSize, BOOL disallowFullBlockingGC);

    static
    int QCALLTYPE EndNoGCRegion();

    static
    void QCALLTYPE _AddMemoryPressure(UINT64 bytesAllocated);

    static
    void QCALLTYPE _RemoveMemoryPressure(UINT64 bytesAllocated);

    NOINLINE static void SendEtwRemoveMemoryPressureEvent(UINT64 bytesAllocated);
    static void SendEtwAddMemoryPressureEvent(UINT64 bytesAllocated);

    static void CheckCollectionCount();
    static void RemoveMemoryPressure(UINT64 bytesAllocated);
    static void AddMemoryPressure(UINT64 bytesAllocated);

private:
    // Out-of-line helper to avoid EH prolog/epilog in functions that otherwise don't throw.
    NOINLINE static void GarbageCollectModeAny(int generation);
};

class COMInterlocked
{
public:
        static FCDECL2(INT32, Exchange, INT32 *location, INT32 value);
        static FCDECL2_IV(INT64,   Exchange64, INT64 *location, INT64 value);
        static FCDECL3(INT32, CompareExchange,        INT32* location, INT32 value, INT32 comparand);
        static FCDECL3_IVV(INT64, CompareExchange64,        INT64* location, INT64 value, INT64 comparand);
        static FCDECL2_IV(float, ExchangeFloat, float *location, float value);
        static FCDECL2_IV(double, ExchangeDouble, double *location, double value);
        static FCDECL3_IVV(float, CompareExchangeFloat, float *location, float value, float comparand);
        static FCDECL3_IVV(double, CompareExchangeDouble, double *location, double value, double comparand);
        static FCDECL2(LPVOID, ExchangeObject, LPVOID* location, LPVOID value);
        static FCDECL3(LPVOID, CompareExchangeObject, LPVOID* location, LPVOID value, LPVOID comparand);
        static FCDECL2(INT32, ExchangeAdd32, INT32 *location, INT32 value);
        static FCDECL2_IV(INT64, ExchangeAdd64, INT64 *location, INT64 value);

        static FCDECL0(void, FCMemoryBarrier);
        static FCDECL0(void, FCMemoryBarrierLoad);
        static void QCALLTYPE MemoryBarrierProcessWide();
};

class ValueTypeHelper {
public:
    static FCDECL1(FC_BOOL_RET, CanCompareBits, Object* obj);
    static FCDECL2(FC_BOOL_RET, FastEqualsCheck, Object* obj1, Object* obj2);
    static FCDECL1(INT32, GetHashCode, Object* objRef);
    static FCDECL1(INT32, GetHashCodeOfPtr, LPVOID ptr);
};

class StreamNative {
public:
    static FCDECL1(FC_BOOL_RET, HasOverriddenBeginEndRead, Object *stream);
    static FCDECL1(FC_BOOL_RET, HasOverriddenBeginEndWrite, Object *stream);
};

#endif // _COMUTILNATIVE_H_
