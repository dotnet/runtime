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
#ifdef VERIFY_HEAP
    struct ByrefValidationEntry
    {
        void       *pByref; // pointer to GC heap
        MethodDesc *pMD;    // interop MD this byref was passed to
    };

    static CQuickArray<ByrefValidationEntry> s_ByrefValidationEntries;
    static SIZE_T                            s_ByrefValidationIndex;
    static CrstStatic                        s_ByrefValidationLock;

    static void ValidateObjectInternal(Object *pObjUNSAFE, BOOL fValidateNextObj);
    static MethodDesc *ResolveInteropMethod(Object *pThisUNSAFE, MethodDesc *pMD);
    static void FormatValidationMessage(MethodDesc *pMD, SString &ssErrorString);

public:
    static void Init();
    static void ProcessByrefValidationList();
#else // VERIFY_HEAP
public:
    static void Init() { LIMITED_METHOD_CONTRACT; }
#endif // VERIFY_HEAP

    //-------------------------------------------------------
    // PInvoke stub helpers
    //-------------------------------------------------------

#ifdef FEATURE_COMINTEROP
    static FCDECL4(IUnknown*,       GetCOMIPFromRCW,                    Object* pSrcUNSAFE, MethodDesc* pMD, void **ppTarget, CLR_BOOL* pfNeedsRelease);
#endif // FEATURE_COMINTEROP

    static FCDECL0(void,            SetLastError            );
    static FCDECL0(void,            ClearLastError          );
    static FCDECL1(void*,           GetDelegateTarget,      DelegateObject *pThisUNSAFE);

    static FCDECL2(FC_BOOL_RET,     TryGetStringTrailByte,  StringObject* thisRefUNSAFE, UINT8 *pbData);

    static FCDECL1(Object*,         GetHRExceptionObject,   HRESULT hr);

#ifdef FEATURE_COMINTEROP
    static FCDECL3(Object*,         GetCOMHRExceptionObject, HRESULT hr, MethodDesc *pMD, Object *unsafe_pThis);
#endif // FEATURE_COMINTEROP

    static FCDECL1(Object*,         AllocateInternal,       EnregisteredTypeHandle typeHnd);
    static FCDECL3(void,            MarshalToUnmanagedVaListInternal, va_list va, DWORD cbVaListSize, const VARARGS* pArgIterator);
    static FCDECL2(void,            MarshalToManagedVaListInternal, va_list va, VARARGS* pArgIterator);
    static FCDECL0(void*,           GetStubContext);
    static FCDECL2(void,            LogPinnedArgument, MethodDesc *localDesc, Object *nativeArg);
    static FCDECL1(DWORD,           CalcVaListSize, VARARGS *varargs);
    static FCDECL3(void,            ValidateObject, Object *pObjUNSAFE, MethodDesc *pMD, Object *pThisUNSAFE);
    static FCDECL3(void,            ValidateByref, void *pByref, MethodDesc *pMD, Object *pThisUNSAFE);

#ifdef PROFILING_SUPPORTED
    //-------------------------------------------------------
    // Profiler helper
    //-------------------------------------------------------
    static FCDECL3(SIZE_T,          ProfilerBeginTransitionCallback,    SIZE_T pSecretParam, Thread* pThread, Object* unsafe_pThis);
    static FCDECL2(void,            ProfilerEndTransitionCallback,      MethodDesc* pRealMD, Thread* pThread);
#endif

#ifdef	FEATURE_ARRAYSTUB_AS_IL
    static FCDECL2(void,            ArrayTypeCheck,             Object*, PtrArray*);
#endif

#ifdef FEATURE_MULTICASTSTUB_AS_IL
    static FCDECL2(void,            MulticastDebuggerTraceHelper, Object*, INT32);
#endif

    static FCDECL0(void*,           NextCallReturnAddress);
};

extern "C" void* QCALLTYPE StubHelpers_CreateCustomMarshalerHelper(MethodDesc* pMD, mdToken paramToken, TypeHandle hndManagedType);

#ifdef FEATURE_COMINTEROP
extern "C" void QCALLTYPE ObjectMarshaler_ConvertToNative(QCall::ObjectHandleOnStack pSrcUNSAFE, VARIANT* pDest);
extern "C" void QCALLTYPE ObjectMarshaler_ConvertToManaged(VARIANT* pSrc, QCall::ObjectHandleOnStack retObject);

extern "C" IUnknown* QCALLTYPE InterfaceMarshaler_ConvertToNative(QCall::ObjectHandleOnStack pObjUNSAFE, MethodTable* pItfMT, MethodTable* pClsMT, DWORD dwFlags);
extern "C" void QCALLTYPE InterfaceMarshaler_ConvertToManaged(IUnknown** ppUnk, MethodTable* pItfMT, MethodTable* pClsMT, DWORD dwFlags, QCall::ObjectHandleOnStack retObject);
#endif

extern "C" void QCALLTYPE StubHelpers_SetStringTrailByte(QCall::StringHandleOnStack str, UINT8 bData);
extern "C" void QCALLTYPE StubHelpers_ThrowInteropParamException(INT resID, INT paramIdx);

#endif  // __STUBHELPERS_h__
