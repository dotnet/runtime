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
    static FCDECL3(IUnknown*,       GetCOMIPFromRCW,    Object* pSrcUNSAFE, MethodDesc* pMD, void **ppTarget);
#endif // FEATURE_COMINTEROP

    static FCDECL0(void,            SetLastError            );
    static FCDECL0(void,            ClearLastError          );
    static FCDECL1(void*,           GetDelegateTarget,      DelegateObject *pThisUNSAFE);

    static FCDECL2(FC_BOOL_RET,     TryGetStringTrailByte,  StringObject* thisRefUNSAFE, UINT8 *pbData);

    static FCDECL2(void,            LogPinnedArgument, MethodDesc *localDesc, Object *nativeArg);
    static FCDECL1(DWORD,           CalcVaListSize, VARARGS *varargs);
};

extern "C" void QCALLTYPE StubHelpers_CreateCustomMarshaler(MethodDesc* pMD, mdToken paramToken, TypeHandle hndManagedType, QCall::ObjectHandleOnStack retObject);

#ifdef PROFILING_SUPPORTED
extern "C" void* QCALLTYPE StubHelpers_ProfilerBeginTransitionCallback(MethodDesc* pTargetMD);
extern "C" void QCALLTYPE StubHelpers_ProfilerEndTransitionCallback(MethodDesc* pTargetMD);
#endif

#ifdef FEATURE_COMINTEROP
extern "C" IUnknown* QCALLTYPE StubHelpers_GetCOMIPFromRCWSlow(QCall::ObjectHandleOnStack pSrc, MethodDesc* pMD, void** ppTarget);

extern "C" void QCALLTYPE ObjectMarshaler_ConvertToNative(QCall::ObjectHandleOnStack pSrcUNSAFE, VARIANT* pDest);
extern "C" void QCALLTYPE ObjectMarshaler_ConvertToManaged(VARIANT* pSrc, QCall::ObjectHandleOnStack retObject);

extern "C" IUnknown* QCALLTYPE InterfaceMarshaler_ConvertToNative(QCall::ObjectHandleOnStack pObjUNSAFE, MethodTable* pItfMT, MethodTable* pClsMT, DWORD dwFlags);
extern "C" void QCALLTYPE InterfaceMarshaler_ConvertToManaged(IUnknown** ppUnk, MethodTable* pItfMT, MethodTable* pClsMT, DWORD dwFlags, QCall::ObjectHandleOnStack retObject);
#endif

extern "C" void QCALLTYPE StubHelpers_SetStringTrailByte(QCall::StringHandleOnStack str, UINT8 bData);
extern "C" void QCALLTYPE StubHelpers_ThrowInteropParamException(INT resID, INT paramIdx);

extern "C" void QCALLTYPE StubHelpers_MarshalToManagedVaList(va_list va, VARARGS* pArgIterator);
extern "C" void QCALLTYPE StubHelpers_MarshalToUnmanagedVaList(va_list va, DWORD cbVaListSize, const VARARGS* pArgIterator);

extern "C" void QCALLTYPE StubHelpers_ValidateObject(QCall::ObjectHandleOnStack pObj, MethodDesc *pMD);
extern "C" void QCALLTYPE StubHelpers_ValidateByref(void *pByref, MethodDesc *pMD);

extern "C" void QCALLTYPE StubHelpers_MulticastDebuggerTraceHelper(QCall::ObjectHandleOnStack element, INT32 count);

#endif  // __STUBHELPERS_h__
