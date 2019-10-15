// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// File: stubhelpers.h
// 

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

    static FCDECL1_V(double,        DateMarshaler__ConvertToNative,  INT64 managedDate);
    static FCDECL1_V(INT64,         DateMarshaler__ConvertToManaged, double nativeDate);

    static FCDECL4(void,            ValueClassMarshaler__ConvertToNative, LPVOID pDest, LPVOID pSrc, MethodTable* pMT, OBJECTREF *ppCleanupWorkListOnStack);
    static FCDECL3(void,            ValueClassMarshaler__ConvertToManaged, LPVOID pDest, LPVOID pSrc, MethodTable* pMT);
    static FCDECL2(void,            ValueClassMarshaler__ClearNative, LPVOID pDest, MethodTable* pMT);

#ifdef FEATURE_COMINTEROP
    static FCDECL4(IUnknown*,       GetCOMIPFromRCW,                    Object* pSrcUNSAFE, MethodDesc* pMD, void **ppTarget, CLR_BOOL* pfNeedsRelease);
    static FCDECL3(IUnknown*,       GetCOMIPFromRCW_WinRT,              Object* pSrcUNSAFE, MethodDesc* pMD, void **ppTarget);
    static FCDECL3(IUnknown*,       GetCOMIPFromRCW_WinRTSharedGeneric, Object* pSrcUNSAFE, MethodDesc* pMD, void **ppTarget);
    static FCDECL3(IUnknown*,       GetCOMIPFromRCW_WinRTDelegate,      Object* pSrcUNSAFE, MethodDesc* pMD, void **ppTarget);

    static FCDECL2(FC_BOOL_RET,     ShouldCallWinRTInterface,           Object* pSrcUNSAFE, MethodDesc* pMD);
    static FCDECL3(DelegateObject*, GetTargetForAmbiguousVariantCall,   Object* pSrcUNSAFE, MethodTable* pMT, CLR_BOOL *pfUseString);
    
    static FCDECL2(void,            ObjectMarshaler__ConvertToNative, Object* pSrcUNSAFE, VARIANT* pDest);
    static FCDECL1(Object*,         ObjectMarshaler__ConvertToManaged, VARIANT* pSrc);
    static FCDECL1(void,            ObjectMarshaler__ClearNative,      VARIANT* pSrc);

    static FCDECL4(IUnknown*,       InterfaceMarshaler__ConvertToNative, Object* pObjUNSAFE, MethodTable* pItfMT, MethodTable* pClsMT, DWORD dwFlags);
    static FCDECL4(Object*,         InterfaceMarshaler__ConvertToManaged, IUnknown **ppUnk, MethodTable *pItfMT, MethodTable *pClsMT, DWORD dwFlags);
    static void QCALLTYPE           InterfaceMarshaler__ClearNative(IUnknown * pUnk);
    static FCDECL1(Object *,        InterfaceMarshaler__ConvertToManagedWithoutUnboxing, IUnknown *pNative);

    static FCDECL1(StringObject*,   UriMarshaler__GetRawUriFromNative, ABI::Windows::Foundation::IUriRuntimeClass* pIUriRC);
    static FCDECL2(IUnknown*,       UriMarshaler__CreateNativeUriInstance, __in_ecount(strLen) CLR_CHAR* pRawUriObj, UINT strLen);

    static FCDECL1(MethodDesc *,    GetDelegateInvokeMethod, DelegateObject *pThisUNSAFE);
    static FCDECL2(IInspectable *,  GetWinRTFactoryReturnValue, Object *pThisUNSAFE, PCODE pCtorEntry);
    static FCDECL2(IInspectable *,  GetOuterInspectable, Object *pThisUNSAFE, MethodDesc *pCtorMD);
#endif // FEATURE_COMINTEROP

    static FCDECL0(void,            SetLastError            );
    static FCDECL0(void,            ClearLastError          );
    static FCDECL1(FC_BOOL_RET,     IsQCall,                NDirectMethodDesc* pNMD);
    static FCDECL1(void,            InitDeclaringType,      NDirectMethodDesc* pMND);
    static FCDECL1(void*,           GetNDirectTarget,       NDirectMethodDesc* pNMD);
    static FCDECL2(void*,           GetDelegateTarget,      DelegateObject *pThisUNSAFE, UINT_PTR *ppStubArg);


    static FCDECL2(void,            ThrowInteropParamException, UINT resID, UINT paramIdx);
    static FCDECL1(Object*,         GetHRExceptionObject,   HRESULT hr);

#ifdef FEATURE_COMINTEROP
    static FCDECL4(Object*,         GetCOMHRExceptionObject, HRESULT hr, MethodDesc *pMD, Object *unsafe_pThis, CLR_BOOL fForWinRT);
#endif // FEATURE_COMINTEROP

    static FCDECL3(void*,           CreateCustomMarshalerHelper, MethodDesc* pMD, mdToken paramToken, TypeHandle hndManagedType);

    static FCDECL3(void,            FmtClassUpdateNativeInternal, Object* pObjUNSAFE, BYTE* pbNative, OBJECTREF *ppCleanupWorkListOnStack);
    static FCDECL2(void,            FmtClassUpdateCLRInternal, Object* pObjUNSAFE, BYTE* pbNative);
    static FCDECL2(void,            LayoutDestroyNativeInternal, BYTE* pbNative, MethodTable* pMT);
    static FCDECL1(Object*,         AllocateInternal,       EnregisteredTypeHandle typeHnd);
    static FCDECL3(void,            MarshalToUnmanagedVaListInternal, va_list va, DWORD cbVaListSize, const VARARGS* pArgIterator);
    static FCDECL2(void,            MarshalToManagedVaListInternal, va_list va, VARARGS* pArgIterator);
    static FCDECL0(void*,           GetStubContext);
    static FCDECL2(void,            LogPinnedArgument, MethodDesc *localDesc, Object *nativeArg);
#ifdef _TARGET_64BIT_
    static FCDECL0(void*,           GetStubContextAddr);
#endif // _TARGET_64BIT_
    static FCDECL1(DWORD,           CalcVaListSize, VARARGS *varargs);
    static FCDECL3(void,            ValidateObject, Object *pObjUNSAFE, MethodDesc *pMD, Object *pThisUNSAFE);
    static FCDECL3(void,            ValidateByref, void *pByref, MethodDesc *pMD, Object *pThisUNSAFE);

#ifdef FEATURE_COMINTEROP
    static FCDECL1(Object*,         GetWinRTFactoryObject,  MethodDesc *pCMD);    
#endif // FEATURE_COMINTEROP

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

#ifdef FEATURE_COMINTEROP
    static FCDECL2(StringObject *,              WinRTTypeNameConverter__ConvertToWinRTTypeName,  ReflectClassBaseObject *pTypeUNSAFE, CLR_BOOL *pbIsWinRTPrimitive);
    static FCDECL2(ReflectClassBaseObject *,    WinRTTypeNameConverter__GetTypeFromWinRTTypeName, StringObject *pWinRTTypeNameUNSAFE, CLR_BOOL *pbIsPrimitive);
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_MULTICASTSTUB_AS_IL
    static FCDECL2(void,            MulticastDebuggerTraceHelper, Object*, INT32);
#endif
};

#endif  // __STUBHELPERS_h__
