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
#include "gchelpersnative.h"
#undef GetCurrentTime

//
//
// EXCEPTION NATIVE
//
//

#ifdef FEATURE_COMINTEROP
void FreeExceptionData(ExceptionData *pedata);
#endif

class ExceptionNative
{
public:
    static FCDECL1(FC_BOOL_RET, IsImmutableAgileException, Object* pExceptionUNSAFE);
    static FCDECL1(FC_BOOL_RET, IsTransient, INT32 hresult);
    static FCDECL0(VOID, PrepareForForeignExceptionRaise);

#ifdef FEATURE_COMINTEROP
    // NOTE: caller cleans up any partially initialized BSTRs in pED
    static void      GetExceptionData(OBJECTREF, ExceptionData *);
#endif

    // Note: these are on the PInvoke class to hide these from the user.
    static FCDECL0(EXCEPTION_POINTERS*, GetExceptionPointers);
    static FCDECL0(INT32, GetExceptionCode);
    static FCDECL0(UINT32, GetExceptionCount);
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
    static FCDECL3(VOID, BulkMoveWithWriteBarrier, void *dst, void *src, size_t byteCount);
};

extern "C" void QCALLTYPE Buffer_Clear(void *dst, size_t length);
extern "C" void QCALLTYPE Buffer_MemMove(void *dst, void *src, size_t length);

//
// Object
//
class ObjectNative
{
public:
    static FCDECL1(INT32, TryGetHashCode, Object* vThisRef);
    static FCDECL2(FC_BOOL_RET, ContentEquals, Object *pThisRef, Object *pCompareRef);
};

extern "C" INT32 QCALLTYPE ObjectNative_GetHashCodeSlow(QCall::ObjectHandleOnStack objHandle);
extern "C" void QCALLTYPE ObjectNative_AllocateUninitializedClone(QCall::ObjectHandleOnStack objHandle);

class COMInterlocked
{
public:
        static FCDECL2(INT32, Exchange32, INT32 *location, INT32 value);
        static FCDECL2_IV(INT64, Exchange64, INT64 *location, INT64 value);
        static FCDECL3(INT32, CompareExchange32, INT32* location, INT32 value, INT32 comparand);
        static FCDECL3_IVV(INT64, CompareExchange64, INT64* location, INT64 value, INT64 comparand);
        static FCDECL2(LPVOID, ExchangeObject, LPVOID* location, LPVOID value);
        static FCDECL3(LPVOID, CompareExchangeObject, LPVOID* location, LPVOID value, LPVOID comparand);
        static FCDECL2(INT32, ExchangeAdd32, INT32 *location, INT32 value);
        static FCDECL2_IV(INT64, ExchangeAdd64, INT64 *location, INT64 value);
};

extern "C" void QCALLTYPE Interlocked_MemoryBarrierProcessWide();

class MethodTableNative {
public:
    static FCDECL1(UINT32, GetNumInstanceFieldBytes, MethodTable* mt);
    static FCDECL1(CorElementType, GetPrimitiveCorElementType, MethodTable* mt);
    static FCDECL2(MethodTable*, GetMethodTableMatchingParentClass, MethodTable* mt, MethodTable* parent);
    static FCDECL1(MethodTable*, InstantiationArg0, MethodTable* mt);
    static FCDECL1(OBJECTHANDLE, GetLoaderAllocatorHandle, MethodTable* mt);
};

extern "C" BOOL QCALLTYPE MethodTable_AreTypesEquivalent(MethodTable* mta, MethodTable* mtb);
extern "C" BOOL QCALLTYPE MethodTable_CanCompareBitsOrUseFastGetHashCode(MethodTable* mt);
extern "C" BOOL QCALLTYPE TypeHandle_CanCastTo_NoCacheLookup(void* fromTypeHnd, void* toTypeHnd);
extern "C" INT32 QCALLTYPE TypeHandle_GetCorElementType(void* typeHnd);
extern "C" INT32 QCALLTYPE ValueType_GetHashCodeStrategy(MethodTable* mt, QCall::ObjectHandleOnStack objHandle, UINT32* fieldOffset, UINT32* fieldSize, MethodTable** fieldMT);

BOOL CanCompareBitsOrUseFastGetHashCode(MethodTable* mt);

extern "C" BOOL QCALLTYPE Stream_HasOverriddenSlow(MethodTable* pMT, BOOL isRead);

#endif // _COMUTILNATIVE_H_
