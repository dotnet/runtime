// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: stubhelpers.cpp
// 

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
#include "winrttypenameconverter.h"
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
	pObjUNSAFE->Validate(/*bDeep=*/ TRUE, /*bVerifyNextHeader=*/ FALSE, /*bVerifySyncBlock=*/ TRUE);

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

FCIMPL1_V(double, StubHelpers::DateMarshaler__ConvertToNative,  INT64 managedDate)
{
    FCALL_CONTRACT;

    double retval = 0.0;
    HELPER_METHOD_FRAME_BEGIN_RET_0();
    retval = COMDateTime::TicksToDoubleDate(managedDate);
    HELPER_METHOD_FRAME_END();
    return retval;
}
FCIMPLEND

FCIMPL1_V(INT64, StubHelpers::DateMarshaler__ConvertToManaged, double nativeDate)
{
    FCALL_CONTRACT;

    INT64 retval = 0;
    HELPER_METHOD_FRAME_BEGIN_RET_0();
    retval = COMDateTime::DoubleDateToTicks(nativeDate);
    HELPER_METHOD_FRAME_END();
    return retval;
}
FCIMPLEND

FCIMPL4(void, StubHelpers::ValueClassMarshaler__ConvertToNative, LPVOID pDest, LPVOID pSrc, MethodTable* pMT, OBJECTREF *ppCleanupWorkListOnStack)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_0();
    FmtValueTypeUpdateNative(&pSrc, pMT, (BYTE*)pDest, ppCleanupWorkListOnStack);
    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

FCIMPL3(void, StubHelpers::ValueClassMarshaler__ConvertToManaged, LPVOID pDest, LPVOID pSrc, MethodTable* pMT)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_0();
    FmtValueTypeUpdateCLR(&pDest, pMT, (BYTE*)pSrc);
    HELPER_METHOD_FRAME_END_POLL();
}
FCIMPLEND

FCIMPL2(void, StubHelpers::ValueClassMarshaler__ClearNative, LPVOID pDest, MethodTable* pMT)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_0();
    FmtClassDestroyNative(pDest, pMT);
    HELPER_METHOD_FRAME_END_POLL();
}
FCIMPLEND

#ifdef FEATURE_COMINTEROP

FORCEINLINE static void GetCOMIPFromRCW_ClearFP()
{
    LIMITED_METHOD_CONTRACT;

#ifdef _TARGET_X86_
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
#endif // _TARGET_X86_
}

FORCEINLINE static SOleTlsData *GetOrCreateOleTlsData()
{
    LIMITED_METHOD_CONTRACT;

    SOleTlsData *pOleTlsData;
#ifdef _TARGET_X86_
    // This saves 1 memory instruction over NtCurretTeb()->ReservedForOle because
    // NtCurrentTeb() reads _TEB.NtTib.Self which is the same as what FS:0 already
    // points to.
    pOleTlsData = (SOleTlsData *)(ULONG_PTR)__readfsdword(offsetof(TEB, ReservedForOle));
#else // _TARGET_X86_
    pOleTlsData = (SOleTlsData *)NtCurrentTeb()->ReservedForOle;
#endif // _TARGET_X86_
    if (pOleTlsData == NULL)
    {
        pOleTlsData = (SOleTlsData *)SetupOleContext();
    }
    return pOleTlsData;
}

FORCEINLINE static void *GetCOMIPFromRCW_GetTargetNoInterception(IUnknown *pUnk, ComPlusCallInfo *pComInfo)
{
    LIMITED_METHOD_CONTRACT;

    LPVOID *lpVtbl = *(LPVOID **)pUnk;
    return lpVtbl[pComInfo->m_cachedComSlot];
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

    // also search the auxiliary cache if it's available
    RCWAuxiliaryData *pAuxData = pRCW->m_pAuxiliaryData;
    if (pAuxData != NULL)
    {
        LPVOID pCtxCookie = (pRCW->IsFreeThreaded() ? NULL : pOleTlsData->pCurrentCtx);
        return pAuxData->FindInterfacePointer(pItfMT, pCtxCookie);
    }

    return NULL;
}

// Like GetCOMIPFromRCW_GetIUnknownFromRCWCache but also computes the target. This is a couple of instructions
// faster than GetCOMIPFromRCW_GetIUnknownFromRCWCache + GetCOMIPFromRCW_GetTargetNoInterception.
FORCEINLINE static IUnknown *GetCOMIPFromRCW_GetIUnknownFromRCWCache_NoInterception(RCW *pRCW, ComPlusCallInfo *pComInfo, void **ppTarget)
{
    LIMITED_METHOD_CONTRACT;

    // The code in this helper is the "fast path" that used to be generated directly
    // to compiled ML stubs. The idea is to aim for an efficient RCW cache hit.
    SOleTlsData *pOleTlsData = GetOrCreateOleTlsData();
    MethodTable *pItfMT = pComInfo->m_pInterfaceMT;
    
    // test for free-threaded after testing for context match to optimize for apartment-bound objects
    if (pOleTlsData->pCurrentCtx == pRCW->GetWrapperCtxCookie() || pRCW->IsFreeThreaded())
    {
        for (int i = 0; i < INTERFACE_ENTRY_CACHE_SIZE; i++)
        {
            if (pRCW->m_aInterfaceEntries[i].m_pMT == pItfMT)
            {
                IUnknown *pUnk = pRCW->m_aInterfaceEntries[i].m_pUnknown;
                _ASSERTE(pUnk != NULL);
                *ppTarget = GetCOMIPFromRCW_GetTargetNoInterception(pUnk, pComInfo);
                return pUnk;
            }
        }
    }

    // also search the auxiliary cache if it's available
    RCWAuxiliaryData *pAuxData = pRCW->m_pAuxiliaryData;
    if (pAuxData != NULL)
    {
        LPVOID pCtxCookie = (pRCW->IsFreeThreaded() ? NULL : pOleTlsData->pCurrentCtx);
        
        IUnknown *pUnk = pAuxData->FindInterfacePointer(pItfMT, pCtxCookie);
        if (pUnk != NULL)
        {
            *ppTarget = GetCOMIPFromRCW_GetTargetNoInterception(pUnk, pComInfo);
            return pUnk;
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

    if (pFCall == StubHelpers::GetCOMIPFromRCW_WinRT ||
        pFCall == StubHelpers::GetCOMIPFromRCW_WinRTSharedGeneric ||
        pFCall == StubHelpers::GetCOMIPFromRCW_WinRTDelegate)
    {
        pRetUnk.Release();
    }


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

// This helper can handle any CLR->COM call (classic COM, WinRT, WinRT delegate, WinRT generic), it supports hosting,
// and clears FP state on x86 for compatibility with VB6.
FCIMPL4(IUnknown*, StubHelpers::GetCOMIPFromRCW, Object* pSrcUNSAFE, MethodDesc* pMD, void **ppTarget, CLR_BOOL* pfNeedsRelease)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(pMD->IsComPlusCall() || pMD->IsGenericComPlusCall() || pMD->IsEEImpl());
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

// This helper can handle only non-generic WinRT calls, does not support hosting/interception, and does not clear FP state.
FCIMPL3(IUnknown*, StubHelpers::GetCOMIPFromRCW_WinRT, Object* pSrcUNSAFE, MethodDesc* pMD, void** ppTarget)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(pMD->IsComPlusCall());
    }
    CONTRACTL_END;

    OBJECTREF pSrc = ObjectToOBJECTREF(pSrcUNSAFE);

    ComPlusCallInfo *pComInfo = ((ComPlusCallMethodDesc *)pMD)->m_pComPlusCallInfo;
    RCW *pRCW = pSrc->PassiveGetSyncBlock()->GetInteropInfoNoCreate()->GetRawRCW();
    if (pRCW != NULL)
    {
        IUnknown *pUnk = GetCOMIPFromRCW_GetIUnknownFromRCWCache_NoInterception(pRCW, pComInfo, ppTarget);
        if (pUnk != NULL)
        {
            return pUnk;
        }
    }

    /* if we didn't find the COM interface pointer in the cache we will have to erect an HMF */
    FC_INNER_RETURN(IUnknown*, GetCOMIPFromRCWHelper(StubHelpers::GetCOMIPFromRCW_WinRT, pSrc, pMD, ppTarget));
}
FCIMPLEND

// This helper can handle only generic WinRT calls, does not support hosting, and does not clear FP state.
FCIMPL3(IUnknown*, StubHelpers::GetCOMIPFromRCW_WinRTSharedGeneric, Object* pSrcUNSAFE, MethodDesc* pMD, void** ppTarget)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(pMD->IsGenericComPlusCall());
    }
    CONTRACTL_END;

    OBJECTREF pSrc = ObjectToOBJECTREF(pSrcUNSAFE);

    ComPlusCallInfo *pComInfo = pMD->AsInstantiatedMethodDesc()->IMD_GetComPlusCallInfo();
    RCW *pRCW = pSrc->PassiveGetSyncBlock()->GetInteropInfoNoCreate()->GetRawRCW();
    if (pRCW != NULL)
    {
        IUnknown *pUnk = GetCOMIPFromRCW_GetIUnknownFromRCWCache_NoInterception(pRCW, pComInfo, ppTarget);
        if (pUnk != NULL)
        {
            return pUnk;
        }
    }

    /* if we didn't find the COM interface pointer in the cache we will have to erect an HMF */
    FC_INNER_RETURN(IUnknown*, GetCOMIPFromRCWHelper(StubHelpers::GetCOMIPFromRCW_WinRTSharedGeneric, pSrc, pMD, ppTarget));
}
FCIMPLEND

// This helper can handle only delegate WinRT calls, does not support hosting, and does not clear FP state.
FCIMPL3(IUnknown*, StubHelpers::GetCOMIPFromRCW_WinRTDelegate, Object* pSrcUNSAFE, MethodDesc* pMD, void** ppTarget)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(pMD->IsEEImpl());
    }
    CONTRACTL_END;

    OBJECTREF pSrc = ObjectToOBJECTREF(pSrcUNSAFE);

    ComPlusCallInfo *pComInfo = ((DelegateEEClass *)pMD->GetClass())->m_pComPlusCallInfo;
    RCW *pRCW = pSrc->PassiveGetSyncBlock()->GetInteropInfoNoCreate()->GetRawRCW();
    if (pRCW != NULL)
    {
        IUnknown *pUnk = GetCOMIPFromRCW_GetIUnknownFromRCWCache_NoInterception(pRCW, pComInfo, ppTarget);
        if (pUnk != NULL)
        {
            return pUnk;
        }
    }

    /* if we didn't find the COM interface pointer in the cache we will have to erect an HMF */
    FC_INNER_RETURN(IUnknown*, GetCOMIPFromRCWHelper(StubHelpers::GetCOMIPFromRCW_WinRTDelegate, pSrc, pMD, ppTarget));
}
FCIMPLEND

#include <optdefault.h>


NOINLINE static FC_BOOL_RET ShouldCallWinRTInterfaceHelper(RCW *pRCW, MethodTable *pItfMT)
{
    FC_INNER_PROLOG(StubHelpers::ShouldCallWinRTInterface);

    bool result = false;
    
    HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB(Frame::FRAME_ATTR_EXACT_DEPTH|Frame::FRAME_ATTR_CAPTURE_DEPTH_2);

    // call the GC-triggering version
    result = pRCW->SupportsWinRTInteropInterface(pItfMT);

    HELPER_METHOD_FRAME_END();
    FC_INNER_EPILOG();

    FC_RETURN_BOOL(result);
}

FCIMPL2(FC_BOOL_RET, StubHelpers::ShouldCallWinRTInterface, Object *pSrcUNSAFE, MethodDesc *pMD)
{
    FCALL_CONTRACT;

    OBJECTREF pSrc = ObjectToOBJECTREF(pSrcUNSAFE);

    ComPlusCallInfo *pComInfo = ComPlusCallInfo::FromMethodDesc(pMD);
    RCW *pRCW = pSrc->PassiveGetSyncBlock()->GetInteropInfoNoCreate()->GetRawRCW();
    if (pRCW == NULL)
    {
        // Pretend that we are not redirected WinRT type
        // We'll throw InvalidComObjectException later in GetComIPFromRCW
        return false;
    }

    TypeHandle::CastResult result = pRCW->SupportsWinRTInteropInterfaceNoGC(pComInfo->m_pInterfaceMT);
    switch (result)
    {
        case TypeHandle::CanCast:    FC_RETURN_BOOL(true);
        case TypeHandle::CannotCast: FC_RETURN_BOOL(false);
    }

    FC_INNER_RETURN(FC_BOOL_RET, ShouldCallWinRTInterfaceHelper(pRCW, pComInfo->m_pInterfaceMT));
}
FCIMPLEND

NOINLINE static DelegateObject *GetTargetForAmbiguousVariantCallHelper(RCW *pRCW, MethodTable *pMT, BOOL fIsEnumerable, CLR_BOOL *pfUseString)
{
    FC_INNER_PROLOG(StubHelpers::GetTargetForAmbiguousVariantCall);

    DelegateObject *pRetVal = NULL;
    
    HELPER_METHOD_FRAME_BEGIN_RET_ATTRIB(Frame::FRAME_ATTR_EXACT_DEPTH|Frame::FRAME_ATTR_CAPTURE_DEPTH_2);

    // Note that if the call succeeds, it will set the right OBJECTHANDLE/flags on the RCW so we won't have to do this
    // next time. If the call fails, we don't care because it is an error and an exception will be thrown later.
    SafeComHolder<IUnknown> pUnk = pRCW->GetComIPFromRCW(pMT);

    WinRTInterfaceRedirector::WinRTLegalStructureBaseType baseType = WinRTInterfaceRedirector::GetStructureBaseType(pMT->GetInstantiation());

    BOOL fUseString = FALSE;
    BOOL fUseT = FALSE;
    pRetVal = (DelegateObject *)OBJECTREFToObject(pRCW->GetTargetForAmbiguousVariantCall(fIsEnumerable, baseType, &fUseString, &fUseT));

    *pfUseString = !!fUseString;

    HELPER_METHOD_FRAME_END();
    FC_INNER_EPILOG();

    return pRetVal;
}

// Performs a run-time check to see how an ambiguous variant call on an RCW should be handled. Returns a delegate which should
// be called, or sets *pfUseString to true which means that the caller should use the <string> instantiation. If NULL is returned
// and *pfUseString is false, the caller should attempt to handle the call as usual.
FCIMPL3(DelegateObject*, StubHelpers::GetTargetForAmbiguousVariantCall, Object *pSrcUNSAFE, MethodTable *pMT, CLR_BOOL *pfUseString)
{
    FCALL_CONTRACT;

    OBJECTREF pSrc = ObjectToOBJECTREF(pSrcUNSAFE);

    RCW *pRCW = pSrc->PassiveGetSyncBlock()->GetInteropInfoNoCreate()->GetRawRCW();
    if (pRCW == NULL)
    {
        // ignore this - the call we'll attempt to make later will throw the right exception 
        *pfUseString = false;
        return NULL;
    }

    BOOL fIsEnumerable = pMT->HasSameTypeDefAs(MscorlibBinder::GetExistingClass(CLASS__IENUMERABLEGENERIC));
    _ASSERTE(fIsEnumerable || pMT->HasSameTypeDefAs(MscorlibBinder::GetExistingClass(CLASS__IREADONLYLISTGENERIC)));

    WinRTInterfaceRedirector::WinRTLegalStructureBaseType baseType = WinRTInterfaceRedirector::GetStructureBaseType(pMT->GetInstantiation());

    BOOL fUseString = FALSE;
    BOOL fUseT = FALSE;
    DelegateObject *pRetVal = (DelegateObject *)OBJECTREFToObject(pRCW->GetTargetForAmbiguousVariantCall(fIsEnumerable, baseType, &fUseString, &fUseT));

    if (pRetVal != NULL || fUseT || fUseString)
    {
        *pfUseString = !!fUseString;
        return pRetVal;
    }

    // we haven't seen QI for the interface yet, trigger it now
    FC_INNER_RETURN(DelegateObject*, GetTargetForAmbiguousVariantCallHelper(pRCW, pMT, fIsEnumerable, pfUseString));
}
FCIMPLEND

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

void QCALLTYPE StubHelpers::InterfaceMarshaler__ClearNative(IUnknown * pUnk)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    ULONG cbRef = SafeReleasePreemp(pUnk);
    LogInteropRelease(pUnk, cbRef, "InterfaceMarshalerBase::ClearNative: In/Out release");

    END_QCALL;
}
#include <optdefault.h>




FCIMPL1(StringObject*, StubHelpers::UriMarshaler__GetRawUriFromNative, ABI::Windows::Foundation::IUriRuntimeClass* pIUriRC)
{
    FCALL_CONTRACT;

    if (NULL == pIUriRC)
    {
        return NULL;
    }

    STRINGREF strRef = NULL;
    UINT32 cchRawUri = 0;
    LPCWSTR pwszRawUri = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_1(strRef);

    WinRtString hsRawUriName;

    {
        GCX_PREEMP();

        // Get the RawUri string from the WinRT URI object
        IfFailThrow(pIUriRC->get_RawUri(hsRawUriName.Address()));

        pwszRawUri = hsRawUriName.GetRawBuffer(&cchRawUri);
    }

    strRef = StringObject::NewString(pwszRawUri, cchRawUri);

    HELPER_METHOD_FRAME_END();

    return STRINGREFToObject(strRef);
}
FCIMPLEND

FCIMPL2(IUnknown*, StubHelpers::UriMarshaler__CreateNativeUriInstance, WCHAR* pRawUri, UINT strLen)
{
    CONTRACTL {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pRawUri));
    }
    CONTRACTL_END;

    ABI::Windows::Foundation::IUriRuntimeClass* pIUriRC = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_0();

    GCX_PREEMP();
    pIUriRC = CreateWinRTUri(pRawUri, strLen);

    HELPER_METHOD_FRAME_END();

    return pIUriRC;
}
FCIMPLEND

// A helper to convert an IP to object using special flags.
FCIMPL1(Object *, StubHelpers::InterfaceMarshaler__ConvertToManagedWithoutUnboxing, IUnknown *pNative)
{
    FCALL_CONTRACT;

    OBJECTREF oref = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_1(oref);

    //
    // Get a wrapper for pNative
    // Note that we need to skip WinRT unboxing at this point because
    // 1. We never know whether GetObjectRefFromComIP went through unboxing path. 
    // For example, user could just pass a IUnknown * to T and we'll happily convert that to T
    // 2. If for some reason we end up getting something that does not implement IReference<T>, 
    // we'll get a nice message later when we do the cast in CLRIReferenceImpl.UnboxHelper
    //
    GetObjectRefFromComIP(
        &oref,
        pNative,                                        // pUnk
        g_pBaseCOMObject,                               // Use __ComObject
        NULL,                                           // pItfMT
        ObjFromComIP::CLASS_IS_HINT |                   // No cast check - we'll do cast later 
        ObjFromComIP::UNIQUE_OBJECT |                   // Do not cache the object - To ensure that the unboxing code is called on this object always
                                                        // and the object is not retrieved from the cache as an __ComObject.
                                                        // Don't call GetRuntimeClassName - I just want a RCW of __ComObject
        ObjFromComIP::IGNORE_WINRT_AND_SKIP_UNBOXING    // Skip unboxing
        );
        
    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(oref);
}
FCIMPLEND

FCIMPL2(StringObject *, StubHelpers::WinRTTypeNameConverter__ConvertToWinRTTypeName, 
    ReflectClassBaseObject *pTypeUNSAFE, CLR_BOOL *pbIsWinRTPrimitive)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pTypeUNSAFE));
        PRECONDITION(CheckPointer(pbIsWinRTPrimitive));
    }
    CONTRACTL_END;

    REFLECTCLASSBASEREF refClass = (REFLECTCLASSBASEREF) pTypeUNSAFE;
    STRINGREF refString= NULL;
    HELPER_METHOD_FRAME_BEGIN_RET_2(refClass, refString);

    SString strWinRTTypeName;    
    bool bIsPrimitive;
    if (WinRTTypeNameConverter::AppendWinRTTypeNameForManagedType(
        refClass->GetType(),    // thManagedType
        strWinRTTypeName,       // strWinRTTypeName to append
        FALSE,                  // for type conversion, not for GetRuntimeClassName
        &bIsPrimitive
        ))
    {
        *pbIsWinRTPrimitive = bIsPrimitive;
        refString = AllocateString(strWinRTTypeName);
    }
    else
    {
        *pbIsWinRTPrimitive = FALSE;
        refString = NULL;
    }

    HELPER_METHOD_FRAME_END();

    return STRINGREFToObject(refString);
}
FCIMPLEND

FCIMPL2(ReflectClassBaseObject *, StubHelpers::WinRTTypeNameConverter__GetTypeFromWinRTTypeName, StringObject *pWinRTTypeNameUNSAFE, CLR_BOOL *pbIsPrimitive)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(CheckPointer(pWinRTTypeNameUNSAFE));
    }
    CONTRACTL_END;

    OBJECTREF refClass = NULL;
    STRINGREF refString = ObjectToSTRINGREF(pWinRTTypeNameUNSAFE);
    HELPER_METHOD_FRAME_BEGIN_RET_2(refClass, refString);

    bool isPrimitive;
    TypeHandle th = WinRTTypeNameConverter::LoadManagedTypeForWinRTTypeName(refString->GetBuffer(), /* pLoadBinder */ nullptr, &isPrimitive);
    *pbIsPrimitive = isPrimitive;
    
    refClass = th.GetManagedClassObject();
    
    HELPER_METHOD_FRAME_END();

    return (ReflectClassBaseObject *)OBJECTREFToObject(refClass);    
}
FCIMPLEND

FCIMPL1(MethodDesc*, StubHelpers::GetDelegateInvokeMethod, DelegateObject *pThisUNSAFE)
{
    FCALL_CONTRACT;

    MethodDesc *pMD = NULL;
    
    OBJECTREF pThis = ObjectToOBJECTREF(pThisUNSAFE);
    HELPER_METHOD_FRAME_BEGIN_RET_1(pThis);

    MethodTable *pDelMT = pThis->GetMethodTable();

    pMD = COMDelegate::FindDelegateInvokeMethod(pDelMT);
    if (pMD->IsSharedByGenericInstantiations())
    {
        // we need the exact MethodDesc
        pMD = InstantiatedMethodDesc::FindOrCreateExactClassMethod(pDelMT, pMD);
    }

    HELPER_METHOD_FRAME_END();

    _ASSERTE(pMD);
    return pMD;
}
FCIMPLEND

// Called from COM-to-CLR factory method stubs to get the return value (the delegating interface pointer
// corresponding to the default WinRT interface of the class which we are constructing).
FCIMPL2(IInspectable *, StubHelpers::GetWinRTFactoryReturnValue, Object *pThisUNSAFE, PCODE pCtorEntry)
{
    FCALL_CONTRACT;

    IInspectable *pInsp = NULL;

    OBJECTREF pThis = ObjectToOBJECTREF(pThisUNSAFE);
    HELPER_METHOD_FRAME_BEGIN_RET_1(pThis);

    // COM-to-CLR stubs use the target method entry point as their stub context
    MethodDesc *pCtorMD = Entry2MethodDesc(pCtorEntry, NULL);
    MethodTable *pClassMT = pCtorMD->GetMethodTable();

    // make sure that we talk to the right CCW
    ComCallWrapperTemplate *pTemplate = ComCallWrapperTemplate::GetTemplate(TypeHandle(pClassMT));
    CCWHolder pWrap = ComCallWrapper::InlineGetWrapper(&pThis, pTemplate);

    MethodTable *pDefaultItf = pClassMT->GetDefaultWinRTInterface();
    const IID &riid = (pDefaultItf == NULL ? IID_IInspectable : IID_NULL);
    
    pInsp = static_cast<IInspectable *>(ComCallWrapper::GetComIPFromCCW(pWrap, riid, pDefaultItf, 
        GetComIPFromCCW::CheckVisibility));

    HELPER_METHOD_FRAME_END();

    return pInsp;
}
FCIMPLEND

// Called from CLR-to-COM factory method stubs to get the outer IInspectable to pass
// to the underlying factory object.
FCIMPL2(IInspectable *, StubHelpers::GetOuterInspectable, Object *pThisUNSAFE, MethodDesc *pCtorMD)
{
    FCALL_CONTRACT;

    IInspectable *pInsp = NULL;

    OBJECTREF pThis = ObjectToOBJECTREF(pThisUNSAFE);

    if (pThis->GetMethodTable() != pCtorMD->GetMethodTable())
    {
        // this is a composition scenario
        HELPER_METHOD_FRAME_BEGIN_RET_1(pThis);

        // we don't have the "outer" yet, marshal the object
        pInsp = static_cast<IInspectable *>
            (MarshalObjectToInterface(&pThis, NULL, NULL, ItfMarshalInfo::ITF_MARSHAL_INSP_ITF | ItfMarshalInfo::ITF_MARSHAL_USE_BASIC_ITF));

        HELPER_METHOD_FRAME_END();
    }

    return pInsp;
}
FCIMPLEND

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

FCIMPL1(FC_BOOL_RET, StubHelpers::IsQCall, NDirectMethodDesc* pNMD)
{
    FCALL_CONTRACT;
    FC_RETURN_BOOL(pNMD->IsQCall());
}
FCIMPLEND

NOINLINE static void InitDeclaringTypeHelper(MethodTable *pMT)
{
    FC_INNER_PROLOG(StubHelpers::InitDeclaringType);

    HELPER_METHOD_FRAME_BEGIN_ATTRIB(Frame::FRAME_ATTR_EXACT_DEPTH|Frame::FRAME_ATTR_CAPTURE_DEPTH_2);
    pMT->CheckRunClassInitThrowing();
    HELPER_METHOD_FRAME_END();

    FC_INNER_EPILOG();
}

// Triggers cctor of pNMD's declarer, similar to code:JIT_InitClass.
#include <optsmallperfcritical.h>
FCIMPL1(void, StubHelpers::InitDeclaringType, NDirectMethodDesc* pNMD)
{
    FCALL_CONTRACT;

    MethodTable *pMT = pNMD->GetMethodTable();
    _ASSERTE(!pMT->IsClassPreInited());

    if (pMT->GetDomainLocalModule()->IsClassInitialized(pMT))
        return;

    FC_INNER_RETURN_VOID(InitDeclaringTypeHelper(pMT));
}
FCIMPLEND
#include <optdefault.h>

FCIMPL1(void*, StubHelpers::GetNDirectTarget, NDirectMethodDesc* pNMD)
{
    FCALL_CONTRACT;

    FCUnique(0xa2);
    return pNMD->GetNDirectTarget();
}
FCIMPLEND

FCIMPL2(void*, StubHelpers::GetDelegateTarget, DelegateObject *pThisUNSAFE, UINT_PTR *ppStubArg)
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

#if defined(_TARGET_X86_)
    // On x86 we wrap the call with a thunk that handles host notifications.
    SyncBlock *pSyncBlock = orefThis->PassiveGetSyncBlock();
    if (pSyncBlock != NULL)
    {
        InteropSyncBlockInfo *pInteropInfo = pSyncBlock->GetInteropInfoNoCreate();
        if (pInteropInfo != NULL)
        {
            // we return entry point to a stub that wraps the real target
            Stub *pInterceptStub = pInteropInfo->GetInterceptStub();
            if (pInterceptStub != NULL)
            {
                pEntryPoint = pInterceptStub->GetEntryPoint();
            }
        }
    }
#endif // _TARGET_X86_

#if defined(_WIN64)
    UINT_PTR target = (UINT_PTR)orefThis->GetMethodPtrAux();

    // See code:GenericPInvokeCalliHelper
    // The lowest bit is used to distinguish between MD and target on 64-bit.
    target = (target << 1) | 1;

    // On 64-bit we pass the real target to the stub-for-host through this out argument,
    // see IL code gen in NDirectStubLinker::DoNDirect for details.
    *ppStubArg = target;

#elif defined(_TARGET_ARM_)
    // @ARMTODO: Nothing to do for ARM yet since we don't support the hosted path.
#endif // _WIN64, _TARGET_ARM_

    if (pEntryPoint == NULL)
    {
        pEntryPoint = orefThis->GetMethodPtrAux();
    }

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

#ifdef FEATURE_COMINTEROP
class COMInterfaceMarshalerCallback : public ICOMInterfaceMarshalerCallback
{
public :
    COMInterfaceMarshalerCallback(Thread *pThread, LPVOID pCtxCookie)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_TRIGGERS;
            MODE_ANY;
        }
        CONTRACTL_END;

        _ASSERTE(pThread != NULL);
        _ASSERTE(pCtxCookie != NULL);
        
        m_bIsFreeThreaded = false;
        m_pThread = pThread;
        m_pCtxCookie = pCtxCookie;

        m_bIsDCOMProxy = false;
    }

    virtual void OnRCWCreated(RCW *pRCW)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(pRCW != NULL);
        
        if (pRCW->IsFreeThreaded())
            m_bIsFreeThreaded = true;

        if (pRCW->IsDCOMProxy())
            m_bIsDCOMProxy = true;
    }

    // Return true if ComInterfaceMarshaler should use this RCW
    // Return false if ComInterfaceMarshaler should just skip this RCW and proceed
    // to create a duplicate one instead
    virtual bool ShouldUseThisRCW(RCW *pRCW)
    {
        LIMITED_METHOD_CONTRACT;
        
        _ASSERTE(pRCW->SupportsIInspectable());

        // Is this a free threaded RCW or a context-bound RCW created in the same context
        if (pRCW->IsFreeThreaded() || 
            pRCW->GetWrapperCtxCookie() == m_pCtxCookie)
        {
            return true;
        }
        else
        {
            //
            // Now we get back a WinRT factory RCW created in a different context. This means the
            // factory is a singleton, and the returned IActivationFactory could be either one of 
            // the following:
            // 1) A raw pointer, and it acts like a free threaded object
            // 2) A proxy that is used across different contexts. It might maintain a list of contexts
            // that it is marshaled to, and will fail to be called if it is not marshaled to this 
            // context yet.
            //
            // In this case, it is unsafe to use this RCW in this context and we should proceed
            // to create a duplicated one instead. It might make sense to have a context-sensitive
            // RCW cache but I don't think this case will be common enough to justify it
            //
            return false;
        }
    }
    
    virtual void OnRCWCacheHit(RCW *pRCW)
    {
        LIMITED_METHOD_CONTRACT;    

        if (pRCW->IsFreeThreaded())
            m_bIsFreeThreaded = true;        

        if (pRCW->IsDCOMProxy())
            m_bIsDCOMProxy = true;
    }

    bool IsFreeThreaded()
    {
        LIMITED_METHOD_CONTRACT;

        return m_bIsFreeThreaded;
    }
    
    bool IsDCOMProxy()
    {
        LIMITED_METHOD_CONTRACT;

        return m_bIsDCOMProxy;
    }

private :
    Thread *m_pThread;          // Current thread
    LPVOID m_pCtxCookie;        // Current context cookie
    bool   m_bIsFreeThreaded;   // Whether we got back the RCW from a different context
    bool   m_bIsDCOMProxy;      // Is this a proxy to an object in a different process
};

//
// Retrieve cached WinRT factory RCW or create a new one, according to the MethodDesc of the .ctor
//
FCIMPL1(Object*, StubHelpers::GetWinRTFactoryObject, MethodDesc *pCMD)
{
    FCALL_CONTRACT;

    OBJECTREF refFactory = NULL;

    HELPER_METHOD_FRAME_BEGIN_RET_1(refFactory);
    
    MethodTable *pMTOfTypeToCreate = pCMD->GetMethodTable();
    AppDomain   *pDomain = GetAppDomain();

    //
    // Look up cached WinRT factory according to type to create + current context cookie
    // For each type in AppDomain, we cache only the last WinRT factory object 
    // We don't cache factory per context in order to avoid explosion of objects if there are 
    // multiple STA apartments
    //
    // Note that if cached WinRT factory is FTM, we'll get it back regardless of the supplied cookie
    //
    LPVOID lpCtxCookie = GetCurrentCtxCookie();
    refFactory = pDomain->LookupWinRTFactoryObject(pMTOfTypeToCreate, lpCtxCookie);
    if (refFactory == NULL)
    {   
        //
        // Didn't find a cached factory that matches the context
        // Time to create a new factory and wrap it in a RCW
        // 

        //
        // Creates a callback to checks for singleton WinRT factory during RCW creation
        //
        // If we get back an existing RCW from a different context, this callback
        // will make the RCW a context-agile (but not free-threaded) RCW. Being context-agile
        // in this case means RCW will not make any context transition. As long as we are only
        // calling this RCW from where we got it back (using IInspectable* as identity), we should
        // be fine (as we are supposed to call that pointer directly anyway)
        //
        // See code:COMInterfaceMarshalerCallback for more details
        //
        COMInterfaceMarshalerCallback callback(GET_THREAD(), lpCtxCookie);

        //
        // Get the activation factory instance for this WinRT type and create a RCW for it
        //
        GetNativeWinRTFactoryObject(
            pMTOfTypeToCreate,
            GET_THREAD(),
            ComPlusCallInfo::FromMethodDesc(pCMD)->m_pInterfaceMT,  // Factory interface
            FALSE,      // Don't need a unique RCW
                        // it is only needed in WindowsRuntimeMarshal.GetActivationFactory API
            &callback,
            &refFactory);

        //
        // If this is free-threaded factory RCW, set lpCtxCookie = NULL, which means
        // this RCW can be used anywhere
        // Otherwise, we can only use this RCW from current thread
        //
        if (callback.IsFreeThreaded())
            lpCtxCookie = NULL;
        
        // Cache the result in the AD-wide cache, unless this is a proxy to a DCOM object.
        // Out of process WinRT servers can have lifetimes independent of the application,
        // and the cache may wind up with stale pointers if we save proxies to OOP factories.
        if (!callback.IsDCOMProxy())
        {
            pDomain->CacheWinRTFactoryObject(pMTOfTypeToCreate, &refFactory, lpCtxCookie);
        }
    }

    HELPER_METHOD_FRAME_END();

    return OBJECTREFToObject(refFactory);
}
FCIMPLEND


#endif

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

    bool fReverseInterop = false;

    if (NULL == pThread)
    {
        // This is our signal for the reverse interop cases.
        fReverseInterop = true;
        pThread = GET_THREAD();
        // the secret param in this casee is the UMEntryThunk
        pRealMD = ((UMEntryThunk*)pSecretParam)->GetMethod();
    }
    else
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

        if (fReverseInterop)
        {
            ProfilerUnmanagedToManagedTransitionMD(pRealMD, COR_PRF_TRANSITION_CALL);
        }
        else
        {
            ProfilerManagedToUnmanagedTransitionMD(pRealMD, COR_PRF_TRANSITION_CALL);
        }
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
        bool fReverseInterop = false;

        if (NULL == pThread)
        {
            // if pThread is null, we are doing reverse interop
            pThread = GET_THREAD();
            fReverseInterop = true;
        }
        
        GCX_PREEMP_THREAD_EXISTS(pThread);

        if (fReverseInterop)
        {
            ProfilerManagedToUnmanagedTransitionMD(pRealMD, COR_PRF_TRANSITION_RETURN);
        }
        else
        {
            ProfilerUnmanagedToManagedTransitionMD(pRealMD, COR_PRF_TRANSITION_RETURN);
        }
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
FCIMPL4(Object*, StubHelpers::GetCOMHRExceptionObject, HRESULT hr, MethodDesc *pMD, Object *unsafe_pThis, CLR_BOOL fForWinRT)
{
    FCALL_CONTRACT;

    OBJECTREF oThrowable = NULL;

    // get 'this'
    OBJECTREF oref = ObjectToOBJECTREF(unsafe_pThis);
    
    HELPER_METHOD_FRAME_BEGIN_RET_2(oref, oThrowable);
    {
        IErrorInfo *pErrInfo = NULL;

        IRestrictedErrorInfo *pResErrorInfo = NULL;
        BOOL bHasNonCLRLanguageErrorObject = FALSE;

        if (fForWinRT)
        {
            SafeGetRestrictedErrorInfo(&pResErrorInfo);
            if (pResErrorInfo != NULL)
            {
                // If we have a restricted error Info lets try and find the corresponding errorInfo,
                // bHasNonCLRLanguageErrorObject can be TRUE|FALSE depending on whether we have an associtated LanguageExceptionObject
                // and whether it is CLR exceptionObject => bHasNonCLRLanguageErrorObject = FALSE;
                // or whether it is a non-CLRExceptionObject => bHasNonCLRLanguageErrorObject = TRUE;
                pErrInfo = GetCorrepondingErrorInfo_WinRT(hr, pResErrorInfo, &bHasNonCLRLanguageErrorObject);
            }
        }
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
                pErrInfo = GetSupportedErrorInfo(pUnk, ItfIID, !fForWinRT);
            
                DWORD cbRef = SafeRelease(pUnk);
                LogInteropRelease(pUnk, cbRef, "IUnk to QI for ISupportsErrorInfo");
            }
        }

        GetExceptionForHR(hr, pErrInfo, !fForWinRT, &oThrowable, pResErrorInfo, bHasNonCLRLanguageErrorObject);
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

    FmtClassUpdateNative(&pObj, pbNative, ppCleanupWorkListOnStack);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

FCIMPL2(void, StubHelpers::FmtClassUpdateCLRInternal, Object* pObjUNSAFE, BYTE* pbNative)
{
    FCALL_CONTRACT;

    OBJECTREF pObj = ObjectToOBJECTREF(pObjUNSAFE);
    HELPER_METHOD_FRAME_BEGIN_1(pObj);

    FmtClassUpdateCLR(&pObj, pbNative);

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND

FCIMPL2(void, StubHelpers::LayoutDestroyNativeInternal, BYTE* pbNative, MethodTable* pMT)
{
    FCALL_CONTRACT;

    HELPER_METHOD_FRAME_BEGIN_0();
    LayoutDestroyNative(pbNative, pMT);
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

#ifdef _TARGET_64BIT_
FCIMPL0(void*, StubHelpers::GetStubContextAddr)
{
    FCALL_CONTRACT;

    FCUnique(0xa1);
    UNREACHABLE_MSG("This is a JIT intrinsic!");
}
FCIMPLEND
#endif // _TARGET_64BIT_

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

    if (ObjIsInstanceOfNoGC(element, arr->GetArrayElementTypeHandle()) == TypeHandle::CanCast)
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
