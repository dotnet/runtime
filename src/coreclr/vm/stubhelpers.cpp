// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: stubhelpers.cpp
//

#include "common.h"

#include "mlinfo.h"
#include "stubhelpers.h"
#include "jitinterface.h"
#include "dllimport.h"
#include "fieldmarshaler.h"
#include "comdelegate.h"
#include "eventtrace.h"
#include "comdatetime.h"
#include "gcheaputilities.h"
#include "interoputil.h"

#ifdef FEATURE_COMINTEROP
#include <oletls.h>
#include "olecontexthelpers.h"
#include "runtimecallablewrapper.h"
#include "comcallablewrapper.h"
#include "clrtocomcall.h"
#include "cominterfacemarshaler.h"
#endif

#ifdef VERIFY_HEAP

CQuickArray<StubHelpers::ByrefValidationEntry> StubHelpers::s_ByrefValidationEntries;
SIZE_T StubHelpers::s_ByrefValidationIndex = 0;
CrstStatic StubHelpers::s_ByrefValidationLock;

// static
void StubHelpers::Init()
{
    WRAPPER_NO_CONTRACT;
    s_ByrefValidationLock.Init(CrstPinnedByrefValidation);
}

// static
void StubHelpers::ValidateObjectInternal(Object *pObjUNSAFE, BOOL fValidateNextObj)
{
	CONTRACTL
	{
	NOTHROW;
	GC_NOTRIGGER;
	MODE_ANY;
}
	CONTRACTL_END;

	_ASSERTE(GCHeapUtilities::GetGCHeap()->RuntimeStructuresValid());

	// validate the object - there's no need to validate next object's
	// header since we validate the next object explicitly below
	if (pObjUNSAFE)
	{
		pObjUNSAFE->Validate(/*bDeep=*/ TRUE, /*bVerifyNextHeader=*/ FALSE, /*bVerifySyncBlock=*/ TRUE);
	}

	// and the next object as required
	if (fValidateNextObj)
	{
		Object *nextObj = GCHeapUtilities::GetGCHeap()->NextObj(pObjUNSAFE);
		if (nextObj != NULL)
		{
			// Note that the MethodTable of the object (i.e. the pointer at offset 0) can change from
			// g_pFreeObjectMethodTable to NULL, from NULL to <legal-value>, or possibly also from
			// g_pFreeObjectMethodTable to <legal-value> concurrently while executing this function.
			// Once <legal-value> is seen, we believe that the object should pass the Validate check.
			// We have to be careful and read the pointer only once to avoid "phantom reads".
			MethodTable *pMT = VolatileLoad(nextObj->GetMethodTablePtr());
			if (pMT != NULL && pMT != g_pFreeObjectMethodTable)
			{
				// do *not* verify the next object's syncblock - the next object is not guaranteed to
				// be "alive" so the finalizer thread may have already released its syncblock
				nextObj->Validate(/*bDeep=*/ TRUE, /*bVerifyNextHeader=*/ FALSE, /*bVerifySyncBlock=*/ FALSE);
			}
		}
	}
}

// static
MethodDesc *StubHelpers::ResolveInteropMethod(Object *pThisUNSAFE, MethodDesc *pMD)
{
    WRAPPER_NO_CONTRACT;

    if (pMD == NULL && pThisUNSAFE != NULL)
    {
        // if this is a call via delegate, get its Invoke method
        MethodTable *pMT = pThisUNSAFE->GetMethodTable();

        _ASSERTE(pMT->IsDelegate());
        return ((DelegateEEClass *)pMT->GetClass())->GetInvokeMethod();
    }
    return pMD;
}

// static
void StubHelpers::FormatValidationMessage(MethodDesc *pMD, SString &ssErrorString)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    ssErrorString.Append(W("Detected managed heap corruption, likely culprit is interop call through "));

    if (pMD == NULL)
    {
        // the only case where we don't have interop MD is CALLI
        ssErrorString.Append(W("CALLI."));
    }
    else
    {
        ssErrorString.Append(W("method '"));

        StackSString ssClassName;
        pMD->GetMethodTable()->_GetFullyQualifiedNameForClass(ssClassName);

        ssErrorString.Append(ssClassName);
        ssErrorString.Append(NAMESPACE_SEPARATOR_CHAR);
        ssErrorString.AppendUTF8(pMD->GetName());

        ssErrorString.Append(W("'."));
    }
}

// static
void StubHelpers::ProcessByrefValidationList()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    StackSString errorString;
    ByrefValidationEntry entry = { NULL, NULL };

    EX_TRY
    {
        AVInRuntimeImplOkayHolder AVOkay;

        // Process all byref validation entries we have saved since the last GC. Note that EE is suspended at
        // this point so we don't have to take locks and we can safely call code:GCHeap.GetContainingObject.
        for (SIZE_T i = 0; i < s_ByrefValidationIndex; i++)
        {
            entry = s_ByrefValidationEntries[i];

            Object *pObjUNSAFE = GCHeapUtilities::GetGCHeap()->GetContainingObject(entry.pByref, false);
            ValidateObjectInternal(pObjUNSAFE, TRUE);
        }
    }
    EX_CATCH
    {
        EX_TRY
        {
            FormatValidationMessage(entry.pMD, errorString);
            EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(COR_E_EXECUTIONENGINE, errorString.GetUnicode());
        }
        EX_CATCH
        {
            EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
        }
        EX_END_CATCH_UNREACHABLE;
    }
    EX_END_CATCH_UNREACHABLE;

    s_ByrefValidationIndex = 0;
}

#endif // VERIFY_HEAP

#ifdef FEATURE_COMINTEROP

FORCEINLINE static void GetCOMIPFromRCW_ClearFP()
{
    LIMITED_METHOD_CONTRACT;

#ifdef TARGET_X86
    // As per ASURT 146699 we need to clear FP state before calling to COM
    // the following sequence was previously generated to compiled ML stubs
    // and is faster than _clearfp().
    __asm
    {
        fnstsw ax
        and    eax, 0x3F
        jz     NoNeedToClear
        fnclex
NoNeedToClear:
    }
#endif // TARGET_X86
}

FORCEINLINE static SOleTlsData *GetOrCreateOleTlsData()
{
    LIMITED_METHOD_CONTRACT;

    SOleTlsData *pOleTlsData;
#ifdef TARGET_X86
    // This saves 1 memory instruction over NtCurretTeb()->ReservedForOle because
    // NtCurrentTeb() reads _TEB.NtTib.Self which is the same as what FS:0 already
    // points to.
    pOleTlsData = (SOleTlsData *)(ULONG_PTR)__readfsdword(offsetof(TEB, ReservedForOle));
#else // TARGET_X86
    pOleTlsData = (SOleTlsData *)NtCurrentTeb()->ReservedForOle;
#endif // TARGET_X86
    if (pOleTlsData == NULL)
    {
        pOleTlsData = (SOleTlsData *)SetupOleContext();
    }
    return pOleTlsData;
}

FORCEINLINE static IUnknown *GetCOMIPFromRCW_GetIUnknownFromRCWCache(RCW *pRCW, MethodTable * pItfMT)
{
    LIMITED_METHOD_CONTRACT;

    // The code in this helper is the "fast path" that used to be generated directly
    // to compiled ML stubs. The idea is to aim for an efficient RCW cache hit.
    SOleTlsData * pOleTlsData = GetOrCreateOleTlsData();

    // test for free-threaded after testing for context match to optimize for apartment-bound objects
    if (pOleTlsData->pCurrentCtx == pRCW->GetWrapperCtxCookie() || pRCW->IsFreeThreaded())
    {
        for (int i = 0; i < INTERFACE_ENTRY_CACHE_SIZE; i++)
        {
            if (pRCW->m_aInterfaceEntries[i].m_pMT == pItfMT)
            {
                return pRCW->m_aInterfaceEntries[i].m_pUnknown;
            }
        }
    }

    return NULL;
}

FORCEINLINE static void *GetCOMIPFromRCW_GetTarget(IUnknown *pUnk, ComPlusCallInfo *pComInfo)
{
    LIMITED_METHOD_CONTRACT;


    LPVOID *lpVtbl = *(LPVOID **)pUnk;
    return lpVtbl[pComInfo->m_cachedComSlot];
}

NOINLINE static IUnknown* GetCOMIPFromRCWHelper(LPVOID pFCall, OBJECTREF pSrc, MethodDesc* pMD, void **ppTarget)
{
    FC_INNER_PROLOG(pFCall);

    IUnknown *pIntf = NULL;

    // This is only called in IL stubs which are in CER, so we don't need to worry about ThreadAbort
    HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB_1(Frame::FRAME_ATTR_NO_THREAD_ABORT|Frame::FRAME_ATTR_EXACT_DEPTH|Frame::FRAME_ATTR_CAPTURE_DEPTH_2, pSrc);

    SafeComHolder<IUnknown> pRetUnk;

    ComPlusCallInfo *pComInfo = ComPlusCallInfo::FromMethodDesc(pMD);
    pRetUnk = ComObject::GetComIPFromRCWThrowing(&pSrc, pComInfo->m_pInterfaceMT);

    *ppTarget = GetCOMIPFromRCW_GetTarget(pRetUnk, pComInfo);
    _ASSERTE(*ppTarget != NULL);

    GetCOMIPFromRCW_ClearFP();

    pIntf = pRetUnk.Extract();

    // No exception will be thrown here (including thread abort as it is delayed in IL stubs)
    HELPER_METHOD_FRAME_END();

    FC_INNER_EPILOG();
    return pIntf;
}

//==================================================================================================================
// The GetCOMIPFromRCW helper exists in four specialized versions to optimize CLR->COM perf. Please be careful when
// changing this code as one of these methods is executed as part of every CLR->COM call so every instruction counts.
//==================================================================================================================


#include <optsmallperfcritical.h>

// This helper can handle any CLR->COM call, it supports hosting,
// and clears FP state on x86 for compatibility with VB6.
FCIMPL4(IUnknown*, StubHelpers::GetCOMIPFromRCW, Object* pSrcUNSAFE, MethodDesc* pMD, void **ppTarget, CLR_BOOL* pfNeedsRelease)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(pMD->IsComPlusCall() || pMD->IsEEImpl());
    }
    CONTRACTL_END;

    OBJECTREF pSrc = ObjectToOBJECTREF(pSrcUNSAFE);
    *pfNeedsRelease = false;

    ComPlusCallInfo *pComInfo = ComPlusCallInfo::FromMethodDesc(pMD);
    RCW *pRCW = pSrc->PassiveGetSyncBlock()->GetInteropInfoNoCreate()->GetRawRCW();
    if (pRCW != NULL)
    {

        IUnknown * pUnk = GetCOMIPFromRCW_GetIUnknownFromRCWCache(pRCW, pComInfo->m_pInterfaceMT);
        if (pUnk != NULL)
        {
            *ppTarget = GetCOMIPFromRCW_GetTarget(pUnk, pComInfo);
            if (*ppTarget != NULL)
            {
                GetCOMIPFromRCW_ClearFP();
                return pUnk;
            }
        }
    }

    /* if we didn't find the COM interface pointer in the cache we will have to erect an HMF */
    *pfNeedsRelease = true;
    FC_INNER_RETURN(IUnknown*, GetCOMIPFromRCWHelper(StubHelpers::GetCOMIPFromRCW, pSrc, pMD, ppTarget));
}
FCIMPLEND

#include <optdefault.h>

FCIMPL2(void, StubHelpers::ObjectMarshaler__ConvertToNative, Object* pSrcUNSAFE, VARIANT* pDest)
{
    FCALL_CONTRACT;

    OBJECTREF pSrc = ObjectToOBJECTREF(pSrcUNSAFE);

    HELPER_METHOD_FRAME_BEGIN_1(pSrc);
    if (pDest->vt & VT_BYREF)
    {
        OleVariant::MarshalOleRefVariantForObject(&pSrc, pDest);
    }
    else
    {
        OleVariant::MarshalOleVariantForObject(&pSrc, pDest);
    }
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

FCIMPL1(Object*, StubHelpers::ObjectMarshaler__ConvertToManaged, VARIANT* pSrc)
{
    FCALL_CONTRACT;

    OBJECTREF retVal = NULL;
    HELPER_METHOD_FRAME_BEGIN_RET_1(retVal);
    // The IL stub is going to call ObjectMarshaler__ClearNative() afterwards.
    // If it doesn't it's a bug in ILObjectMarshaler.
    OleVariant::MarshalObjectForOleVariant(pSrc, &retVal);
    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(retVal);
}
FCIMPLEND

FCIMPL1(void, StubHelpers::ObjectMarshaler__ClearNative, VARIANT* pSrc)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_0();
    SafeVariantClear(pSrc);
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

#include <optsmallperfcritical.h>
FCIMPL4(IUnknown*, StubHelpers::InterfaceMarshaler__ConvertToNative, Object* pObjUNSAFE, MethodTable* pItfMT, MethodTable* pClsMT, DWORD dwFlags)
{
    FCALL_CONTRACT;

    if (NULL == pObjUNSAFE)
    {
        return NULL;
    }

    IUnknown *pIntf = NULL;
    OBJECTREF pObj = ObjectToOBJECTREF(pObjUNSAFE);

    // This is only called in IL stubs which are in CER, so we don't need to worry about ThreadAbort
    HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB_1(Frame::FRAME_ATTR_NO_THREAD_ABORT, pObj);

    // We're going to be making some COM calls, better initialize COM.
    EnsureComStarted();

    pIntf = MarshalObjectToInterface(&pObj, pItfMT, pClsMT, dwFlags);

    // No exception will be thrown here (including thread abort as it is delayed in IL stubs)
    HELPER_METHOD_FRAME_END();

    return pIntf;
}
FCIMPLEND

FCIMPL4(Object*, StubHelpers::InterfaceMarshaler__ConvertToManaged, IUnknown **ppUnk, MethodTable *pItfMT, MethodTable *pClsMT, DWORD dwFlags)
{
    FCALL_CONTRACT;

    if (NULL == *ppUnk)
    {
        return NULL;
    }

    OBJECTREF pObj = NULL;
    HELPER_METHOD_FRAME_BEGIN_RET_1(pObj);

    // We're going to be making some COM calls, better initialize COM.
    EnsureComStarted();

    UnmarshalObjectFromInterface(&pObj, ppUnk, pItfMT, pClsMT, dwFlags);

    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(pObj);
}
FCIMPLEND

extern "C" void QCALLTYPE InterfaceMarshaler__ClearNative(IUnknown * pUnk)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    ULONG cbRef = SafeReleasePreemp(pUnk);
    LogInteropRelease(pUnk, cbRef, "InterfaceMarshalerBase::ClearNative: In/Out release");

    END_QCALL;
}
#include <optdefault.h>

#endif // FEATURE_COMINTEROP

FCIMPL0(void, StubHelpers::SetLastError)
{
    // Make sure this is the first thing we do after returning from the target, as almost everything can cause the last error to get trashed
    DWORD lastError = ::GetLastError();

    FCALL_CONTRACT;

    GetThread()->m_dwLastError = lastError;
}
FCIMPLEND

FCIMPL0(void, StubHelpers::ClearLastError)
{
    FCALL_CONTRACT;

    ::SetLastError(0);
}
FCIMPLEND

FCIMPL1(void*, StubHelpers::GetNDirectTarget, NDirectMethodDesc* pNMD)
{
    FCALL_CONTRACT;

    FCUnique(0xa2);
    return pNMD->GetNDirectTarget();
}
FCIMPLEND

FCIMPL1(void*, StubHelpers::GetDelegateTarget, DelegateObject *pThisUNSAFE)
{
    PCODE pEntryPoint = NULL;

#ifdef _DEBUG
    BEGIN_PRESERVE_LAST_ERROR;
#endif

    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pThisUNSAFE));
    }
    CONTRACTL_END;

    DELEGATEREF orefThis = (DELEGATEREF)ObjectToOBJECTREF(pThisUNSAFE);

#if defined(HOST_64BIT)
    UINT_PTR target = (UINT_PTR)orefThis->GetMethodPtrAux();

    // See code:GenericPInvokeCalliHelper
    // The lowest bit is used to distinguish between MD and target on 64-bit.
    target = (target << 1) | 1;
#endif // HOST_64BIT

    pEntryPoint = orefThis->GetMethodPtrAux();

#ifdef _DEBUG
    END_PRESERVE_LAST_ERROR;
#endif

    return (PVOID)pEntryPoint;
}
FCIMPLEND



FCIMPL2(void, StubHelpers::ThrowInteropParamException, UINT resID, UINT paramIdx)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_0();
    ::ThrowInteropParamException(resID, paramIdx);
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

#ifdef PROFILING_SUPPORTED
FCIMPL3(SIZE_T, StubHelpers::ProfilerBeginTransitionCallback, SIZE_T pSecretParam, Thread* pThread, Object* unsafe_pThis)
{
    FCALL_CONTRACT;

    // We can get here with an ngen image generated with "/prof",
    // even if the profiler doesn't want to track transitions.
    if (!CORProfilerTrackTransitions())
    {
        return NULL;
    }

    MethodDesc* pRealMD = NULL;

    BEGIN_PRESERVE_LAST_ERROR;

    // We must transition to preemptive GC mode before calling out to the profiler,
    // and the transition requires us to set up a HMF.
    DELEGATEREF dref = (DELEGATEREF)ObjectToOBJECTREF(unsafe_pThis);
    HELPER_METHOD_FRAME_BEGIN_RET_1(dref);

    if (pSecretParam == 0)
    {
        // Secret param is null.  This is the calli pinvoke case or the unmanaged delegate case.
        // We have an unmanaged target address but no MD.  For the unmanaged delegate case, we can
        // still retrieve the MD by looking at the "this" object.

        if (dref == NULL)
        {
            // calli pinvoke case
            pRealMD = NULL;
        }
        else
        {
            // unmanaged delegate case
            MethodTable* pMT = dref->GetMethodTable();
            _ASSERTE(pMT->IsDelegate());

            EEClass * pClass = pMT->GetClass();
            pRealMD = ((DelegateEEClass*)pClass)->GetInvokeMethod();
            _ASSERTE(pRealMD);
        }
    }
    else
    {
        // This is either the COM interop or the pinvoke case.
        pRealMD = (MethodDesc*)pSecretParam;
    }

    {
        GCX_PREEMP_THREAD_EXISTS(pThread);

        ProfilerManagedToUnmanagedTransitionMD(pRealMD, COR_PRF_TRANSITION_CALL);
    }

    HELPER_METHOD_FRAME_END();

    END_PRESERVE_LAST_ERROR;

    return (SIZE_T)pRealMD;
}
FCIMPLEND

FCIMPL2(void, StubHelpers::ProfilerEndTransitionCallback, MethodDesc* pRealMD, Thread* pThread)
{
    FCALL_CONTRACT;

    // We can get here with an ngen image generated with "/prof",
    // even if the profiler doesn't want to track transitions.
    if (!CORProfilerTrackTransitions())
    {
        return;
    }

    BEGIN_PRESERVE_LAST_ERROR;

    // We must transition to preemptive GC mode before calling out to the profiler,
    // and the transition requires us to set up a HMF.
    HELPER_METHOD_FRAME_BEGIN_0();
    {
        GCX_PREEMP_THREAD_EXISTS(pThread);

        ProfilerUnmanagedToManagedTransitionMD(pRealMD, COR_PRF_TRANSITION_RETURN);
    }
    HELPER_METHOD_FRAME_END();

    END_PRESERVE_LAST_ERROR;
}
FCIMPLEND
#endif // PROFILING_SUPPORTED

FCIMPL1(Object*, StubHelpers::GetHRExceptionObject, HRESULT hr)
{
    FCALL_CONTRACT;

    OBJECTREF oThrowable = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_1(oThrowable);
    {
        // GetExceptionForHR uses equivalant logic as COMPlusThrowHR
        GetExceptionForHR(hr, &oThrowable);
    }
    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(oThrowable);
}
FCIMPLEND

#ifdef FEATURE_COMINTEROP
FCIMPL3(Object*, StubHelpers::GetCOMHRExceptionObject, HRESULT hr, MethodDesc *pMD, Object *unsafe_pThis)
{
    FCALL_CONTRACT;

    OBJECTREF oThrowable = NULL;

    // get 'this'
    OBJECTREF oref = ObjectToOBJECTREF(unsafe_pThis);

    HELPER_METHOD_FRAME_BEGIN_RET_2(oref, oThrowable);
    {
        IErrorInfo *pErrInfo = NULL;

        if (pErrInfo == NULL && pMD != NULL)
        {
            // Retrieve the interface method table.
            MethodTable *pItfMT = ComPlusCallInfo::FromMethodDesc(pMD)->m_pInterfaceMT;

            // Get IUnknown pointer for this interface on this object
            IUnknown* pUnk = ComObject::GetComIPFromRCW(&oref, pItfMT);
            if (pUnk != NULL)
            {
                // Check to see if the component supports error information for this interface.
                IID ItfIID;
                pItfMT->GetGuid(&ItfIID, TRUE);
                pErrInfo = GetSupportedErrorInfo(pUnk, ItfIID);

                DWORD cbRef = SafeRelease(pUnk);
                LogInteropRelease(pUnk, cbRef, "IUnk to QI for ISupportsErrorInfo");
            }
        }

        GetExceptionForHR(hr, pErrInfo, &oThrowable);
    }
    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(oThrowable);
}
FCIMPLEND
#endif // FEATURE_COMINTEROP

FCIMPL3(void, StubHelpers::FmtClassUpdateNativeInternal, Object* pObjUNSAFE, BYTE* pbNative, OBJECTREF *ppCleanupWorkListOnStack)
{
    FCALL_CONTRACT;

    OBJECTREF pObj = ObjectToOBJECTREF(pObjUNSAFE);
    HELPER_METHOD_FRAME_BEGIN_1(pObj);

    MethodTable* pMT = pObj->GetMethodTable();

    if (pMT->IsBlittable())
    {
        memcpyNoGCRefs(pbNative, pObj->GetData(), pMT->GetNativeSize());
    }
    else
    {
        MethodDesc* structMarshalStub;

        {
            GCX_PREEMP();
            structMarshalStub = NDirect::CreateStructMarshalILStub(pMT);
        }

        MarshalStructViaILStub(structMarshalStub, pObj->GetData(), pbNative, StructMarshalStubs::MarshalOperation::Marshal, (void**)ppCleanupWorkListOnStack);
    }

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

FCIMPL2(void, StubHelpers::FmtClassUpdateCLRInternal, Object* pObjUNSAFE, BYTE* pbNative)
{
    FCALL_CONTRACT;

    OBJECTREF pObj = ObjectToOBJECTREF(pObjUNSAFE);
    HELPER_METHOD_FRAME_BEGIN_1(pObj);

    MethodTable* pMT = pObj->GetMethodTable();

    if (pMT->IsBlittable())
    {
        memcpyNoGCRefs(pObj->GetData(), pbNative, pMT->GetNativeSize());
    }
    else
    {
        MethodDesc* structMarshalStub;

        {
            GCX_PREEMP();
            structMarshalStub = NDirect::CreateStructMarshalILStub(pMT);
        }

        MarshalStructViaILStub(structMarshalStub, pObj->GetData(), pbNative, StructMarshalStubs::MarshalOperation::Unmarshal);
    }

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

FCIMPL2(void, StubHelpers::LayoutDestroyNativeInternal, Object* pObjUNSAFE, BYTE* pbNative)
{
    FCALL_CONTRACT;

    OBJECTREF pObj = ObjectToOBJECTREF(pObjUNSAFE);
    HELPER_METHOD_FRAME_BEGIN_1(pObj);
    MethodTable* pMT = pObj->GetMethodTable();

    if (!pMT->IsBlittable())
    {
        MethodDesc* structMarshalStub;

        {
            GCX_PREEMP();
            structMarshalStub = NDirect::CreateStructMarshalILStub(pMT);
        }

        MarshalStructViaILStub(structMarshalStub, pObj->GetData(), pbNative, StructMarshalStubs::MarshalOperation::Cleanup);
    }

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

FCIMPL1(Object*, StubHelpers::AllocateInternal, EnregisteredTypeHandle pRegisteredTypeHnd)
{
    FCALL_CONTRACT;

    TypeHandle typeHnd = TypeHandle::FromPtr(pRegisteredTypeHnd);
    OBJECTREF objRet = NULL;
    HELPER_METHOD_FRAME_BEGIN_RET_1(objRet);

    MethodTable* pMT = typeHnd.GetMethodTable();
    objRet = pMT->Allocate();

    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(objRet);
}
FCIMPLEND

FCIMPL3(void, StubHelpers::MarshalToUnmanagedVaListInternal, va_list va, DWORD cbVaListSize, const VARARGS* pArgIterator)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_0();
    VARARGS::MarshalToUnmanagedVaList(va, cbVaListSize, pArgIterator);
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

FCIMPL2(void, StubHelpers::MarshalToManagedVaListInternal, va_list va, VARARGS* pArgIterator)
{
    FCALL_CONTRACT;

    VARARGS::MarshalToManagedVaList(va, pArgIterator);
}
FCIMPLEND

FCIMPL3(void, StubHelpers::ValidateObject, Object *pObjUNSAFE, MethodDesc *pMD, Object *pThisUNSAFE)
{
    FCALL_CONTRACT;

#ifdef VERIFY_HEAP
    HELPER_METHOD_FRAME_BEGIN_0();

    StackSString errorString;
    EX_TRY
    {
        AVInRuntimeImplOkayHolder AVOkay;
		// don't validate the next object if a BGC is in progress.  we can race with background
	    // sweep which could make the next object a Free object underneath us if it's dead.
        ValidateObjectInternal(pObjUNSAFE, !(GCHeapUtilities::GetGCHeap()->IsConcurrentGCInProgress()));
    }
    EX_CATCH
    {
        FormatValidationMessage(ResolveInteropMethod(pThisUNSAFE, pMD), errorString);
        EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(COR_E_EXECUTIONENGINE, errorString.GetUnicode());
    }
    EX_END_CATCH_UNREACHABLE;

    HELPER_METHOD_FRAME_END();
#else // VERIFY_HEAP
    FCUnique(0xa3);
    UNREACHABLE_MSG("No validation support without VERIFY_HEAP");
#endif // VERIFY_HEAP
}
FCIMPLEND

FCIMPL3(void, StubHelpers::ValidateByref, void *pByref, MethodDesc *pMD, Object *pThisUNSAFE)
{
    FCALL_CONTRACT;

#ifdef VERIFY_HEAP
    // We cannot validate byrefs at this point as code:GCHeap.GetContainingObject could potentially race
    // with allocations on other threads. We'll just remember this byref along with the interop MD and
    // perform the validation on next GC (see code:StubHelpers.ProcessByrefValidationList).

    // Skip byref if is not pointing inside managed heap
    if (!GCHeapUtilities::GetGCHeap()->IsHeapPointer(pByref))
    {
        return;
    }
    ByrefValidationEntry entry;
    entry.pByref = pByref;
    entry.pMD = ResolveInteropMethod(pThisUNSAFE, pMD);

    HELPER_METHOD_FRAME_BEGIN_0();

    SIZE_T NumOfEntries = 0;
    {
        CrstHolder ch(&s_ByrefValidationLock);

        if (s_ByrefValidationIndex >= s_ByrefValidationEntries.Size())
        {
            // The validation list grows as necessary, for simplicity we never shrink it.
            SIZE_T newSize;
            if (!ClrSafeInt<SIZE_T>::multiply(s_ByrefValidationIndex, 2, newSize) ||
                !ClrSafeInt<SIZE_T>::addition(newSize, 1, newSize))
            {
                ThrowHR(COR_E_OVERFLOW);
            }

            s_ByrefValidationEntries.ReSizeThrows(newSize);
            _ASSERTE(s_ByrefValidationIndex < s_ByrefValidationEntries.Size());
        }

        s_ByrefValidationEntries[s_ByrefValidationIndex] = entry;
        NumOfEntries = ++s_ByrefValidationIndex;
    }

    if (NumOfEntries > BYREF_VALIDATION_LIST_MAX_SIZE)
    {
        // if the list is too big, trigger GC now
        GCHeapUtilities::GetGCHeap()->GarbageCollect(0);
    }

    HELPER_METHOD_FRAME_END();
#else // VERIFY_HEAP
    FCUnique(0xa4);
    UNREACHABLE_MSG("No validation support without VERIFY_HEAP");
#endif // VERIFY_HEAP
}
FCIMPLEND

FCIMPL0(void*, StubHelpers::GetStubContext)
{
    FCALL_CONTRACT;

    FCUnique(0xa0);
    UNREACHABLE_MSG_RET("This is a JIT intrinsic!");
}
FCIMPLEND

FCIMPL2(void, StubHelpers::LogPinnedArgument, MethodDesc *target, Object *pinnedArg)
{
    FCALL_CONTRACT;

    SIZE_T managedSize = 0;

    if (pinnedArg != NULL)
    {
        // Can pass null objects to interop, only check the size if the object is valid.
        managedSize = pinnedArg->GetSize();
    }

    if (target != NULL)
    {
        STRESS_LOG3(LF_STUBS, LL_INFO100, "Managed object %#X with size '%#X' pinned for interop to Method [%pM]\n", pinnedArg, managedSize, target);
    }
    else
    {
        STRESS_LOG2(LF_STUBS, LL_INFO100, "Managed object %#X pinned for interop with size '%#X'", pinnedArg, managedSize);
    }
}
FCIMPLEND

FCIMPL1(DWORD, StubHelpers::CalcVaListSize, VARARGS *varargs)
{
    FCALL_CONTRACT;

    return VARARGS::CalcVaListSize(varargs);
}
FCIMPLEND

#ifdef FEATURE_ARRAYSTUB_AS_IL
NOINLINE static void ArrayTypeCheckSlow(Object* element, PtrArray* arr)
{
    FC_INNER_PROLOG(StubHelpers::ArrayTypeCheck);
    HELPER_METHOD_FRAME_BEGIN_ATTRIB(Frame::FRAME_ATTR_EXACT_DEPTH|Frame::FRAME_ATTR_CAPTURE_DEPTH_2);

    if (!ObjIsInstanceOf(element, arr->GetArrayElementTypeHandle()))
        COMPlusThrow(kArrayTypeMismatchException);

    HELPER_METHOD_FRAME_END();

    FC_INNER_EPILOG();
}

FCIMPL2(void, StubHelpers::ArrayTypeCheck, Object* element, PtrArray* arr)
{
    FCALL_CONTRACT;

    if (ObjIsInstanceOfCached(element, arr->GetArrayElementTypeHandle()) == TypeHandle::CanCast)
        return;

    FC_INNER_RETURN_VOID(ArrayTypeCheckSlow(element, arr));
}
FCIMPLEND
#endif // FEATURE_ARRAYSTUB_AS_IL

#ifdef FEATURE_MULTICASTSTUB_AS_IL
FCIMPL2(void, StubHelpers::MulticastDebuggerTraceHelper, Object* element, INT32 count)
{
    FCALL_CONTRACT;
    FCUnique(0xa5);
}
FCIMPLEND
#endif // FEATURE_MULTICASTSTUB_AS_IL

FCIMPL0(void*, StubHelpers::NextCallReturnAddress)
{
    FCALL_CONTRACT;
    UNREACHABLE_MSG("This is a JIT intrinsic!");
}
FCIMPLEND
