// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: stubhelpers.h
//

#ifndef __STUBHELPERS_h__
#define __STUBHELPERS_h__

#include "fcall.h"
#include "clrvarargs.h"

// Maximum number of deferred byref validation entries - we will trigger gen0 GC if we reach this number.
#define BYREF_VALIDATION_LIST_MAX_SIZE (512 * 1024)

class StubHelpers
{
public:
    static void Init();
#ifdef VERIFY_HEAP
    static void ProcessByrefValidationList();
#endif // VERIFY_HEAP

    //-------------------------------------------------------
    // PInvoke stub helpers
    //-------------------------------------------------------

#ifdef FEATURE_COMINTEROP
    FCDECL1(static MethodTable*,    GetComInterfaceFromMethodDesc, MethodDesc* pMD);
    FCDECL3(static IUnknown*,       GetCOMIPFromRCW,    Object* pSrcUNSAFE, MethodDesc* pMD, void **ppTarget);
#endif // FEATURE_COMINTEROP

    FCDECL0(static void,            SetLastError            );
    FCDECL0(static void,            ClearLastError          );

    FCDECL2(static void,            LogPinnedArgument, MethodDesc *localDesc, Object *nativeArg);
    FCDECL1(static DWORD,           CalcVaListSize, VARARGS *varargs);
};

extern "C" QCallExceptionStatus QCALLTYPE StubHelpers_CreateCustomMarshaler(MethodDesc* pMD, mdToken paramToken, TypeHandle hndManagedType, QCall::ObjectHandleOnStack retObject);

#ifdef PROFILING_SUPPORTED
extern "C" QCallExceptionStatus QCALLTYPE StubHelpers_ProfilerBeginTransitionCallback(MethodDesc* pTargetMD, void** pReturnValue);
extern "C" QCallExceptionStatus QCALLTYPE StubHelpers_ProfilerEndTransitionCallback(MethodDesc* pTargetMD);
#endif

#ifdef FEATURE_COMINTEROP
extern "C" QCallExceptionStatus QCALLTYPE StubHelpers_GetCOMIPFromRCWSlow(QCall::ObjectHandleOnStack pSrc, MethodDesc* pMD, void** ppTarget, BOOL* pfNeedsRelease, IUnknown** pReturnValue);

extern "C" QCallExceptionStatus QCALLTYPE ObjectMarshaler_ConvertToNative(QCall::ObjectHandleOnStack pSrcUNSAFE, VARIANT* pDest);
extern "C" QCallExceptionStatus QCALLTYPE ObjectMarshaler_ConvertToManaged(VARIANT* pSrc, QCall::ObjectHandleOnStack retObject);

extern "C" QCallExceptionStatus QCALLTYPE InterfaceMarshaler_ConvertToNative(QCall::ObjectHandleOnStack pObjUNSAFE, MethodTable* pItfMT, MethodTable* pClsMT, DWORD dwFlags, IUnknown** pReturnValue);
extern "C" QCallExceptionStatus QCALLTYPE InterfaceMarshaler_ConvertToManaged(IUnknown** ppUnk, MethodTable* pItfMT, MethodTable* pClsMT, DWORD dwFlags, QCall::ObjectHandleOnStack retObject);
extern "C" QCallExceptionStatus QCALLTYPE InterfaceMarshaler_GetObjectForComCallableWrapperIUnknown(IUnknown* unk, QCall::ObjectHandleOnStack retObject);
extern "C" QCallExceptionStatus QCALLTYPE InterfaceMarshaler_ValidateComVisibilityForIUnknown(IUnknown* unk);
#endif

extern "C" QCallExceptionStatus QCALLTYPE StubHelpers_ThrowInteropParamException(INT resID, INT paramIdx);

extern "C" QCallExceptionStatus QCALLTYPE StubHelpers_MarshalToManagedVaList(va_list va, VARARGS* pArgIterator);
extern "C" QCallExceptionStatus QCALLTYPE StubHelpers_MarshalToUnmanagedVaList(va_list va, DWORD cbVaListSize, const VARARGS* pArgIterator);

extern "C" QCallExceptionStatus QCALLTYPE StubHelpers_ValidateObject(QCall::ObjectHandleOnStack pObj, MethodDesc *pMD);
extern "C" QCallExceptionStatus QCALLTYPE StubHelpers_ValidateByref(void *pByref, MethodDesc *pMD);

extern "C" QCallExceptionStatus QCALLTYPE StubHelpers_MulticastDebuggerTraceHelper(QCall::ObjectHandleOnStack element, INT32 count);

extern "C" QCallExceptionStatus QCALLTYPE StubHelpers_CreateLayoutClassMarshalStubs(QCall::TypeHandle th, PCODE* pConvertToUnmanaged, PCODE* pConvertToManaged, PCODE* pFree);
#endif  // __STUBHELPERS_h__
