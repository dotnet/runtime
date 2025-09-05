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
#include "../debug/ee/debugger.h"

#ifdef FEATURE_COMINTEROP
#include <oletls.h>
#include "olecontexthelpers.h"
#include "runtimecallablewrapper.h"
#include "comcallablewrapper.h"
#include "clrtocomcall.h"
#include "cominterfacemarshaler.h"
#endif

#ifdef VERIFY_HEAP

struct ByrefValidationEntry final
{
    void       *pByref; // pointer to GC heap
    MethodDesc *pMD;    // interop MD this byref was passed to
};

static CQuickArray<ByrefValidationEntry> s_ByrefValidationEntries;
static SIZE_T                            s_ByrefValidationIndex = 0;
static CrstStatic                        s_ByrefValidationLock;

static void ValidateObjectInternal(Object *pObjUNSAFE, BOOL fValidateNextObj)
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

static void FormatValidationMessage(MethodDesc *pMD, SString &ssErrorString)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    ssErrorString.AppendUTF8("Detected managed heap corruption, likely culprit is interop call through ");

    if (pMD == NULL)
    {
        // the only case where we don't have interop MD is CALLI
        ssErrorString.AppendUTF8("CALLI.");
    }
    else
    {
        ssErrorString.AppendUTF8("method '");

        StackSString ssClassName;
        pMD->GetMethodTable()->_GetFullyQualifiedNameForClass(ssClassName);

        ssErrorString.Append(ssClassName);
        ssErrorString.AppendUTF8(NAMESPACE_SEPARATOR_CHAR);
        ssErrorString.AppendUTF8(pMD->GetName());

        ssErrorString.AppendUTF8("'.");
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

// static
void StubHelpers::Init()
{
    WRAPPER_NO_CONTRACT;
#ifdef VERIFY_HEAP
    s_ByrefValidationLock.Init(CrstPinnedByrefValidation);
#endif // VERIFY_HEAP
}

#ifdef FEATURE_COMINTEROP

// Clears FP state on x86 for compatibility with VB6.
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

FORCEINLINE static SOleTlsData* TryGetOleTlsData()
{
    LIMITED_METHOD_CONTRACT;

#ifdef TARGET_X86
    // This saves 1 memory instruction over NtCurretTeb()->ReservedForOle because
    // NtCurrentTeb() reads _TEB.NtTib.Self which is the same as what FS:0 already
    // points to.
    return (SOleTlsData*)(ULONG_PTR)__readfsdword(offsetof(TEB, ReservedForOle));
#else // TARGET_X86
    return (SOleTlsData*)ClrTeb::GetOleReservedPtr();
#endif // TARGET_X86
}

static SOleTlsData* GetOrCreateOleTlsData()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACT_END;

    SOleTlsData* pOleTlsData = TryGetOleTlsData();
    if (pOleTlsData == NULL)
        pOleTlsData = (SOleTlsData*)SetupOleContext();

    return pOleTlsData;
}

FORCEINLINE static void* GetCOMIPFromRCW_GetTarget(IUnknown *pUnk, CLRToCOMCallInfo *pComInfo)
{
    LIMITED_METHOD_CONTRACT;

    LPVOID* lpVtbl = *(LPVOID **)pUnk;
    LPVOID tgt = lpVtbl[pComInfo->m_cachedComSlot];
    if (tgt != NULL)
        GetCOMIPFromRCW_ClearFP();

    return tgt;
}

FORCEINLINE static IUnknown* GetCOMIPFromRCW_GetTargetFromRCWCache(SOleTlsData* pOleTlsData, RCW* pRCW, CLRToCOMCallInfo* pComInfo, void** ppTarget)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(pOleTlsData != NULL);
    _ASSERTE(pRCW != NULL);
    _ASSERTE(pComInfo != NULL);
    _ASSERTE(ppTarget != NULL);

    // test for free-threaded after testing for context match to optimize for apartment-bound objects
    if (pOleTlsData->pCurrentCtx == pRCW->GetWrapperCtxCookie() || pRCW->IsFreeThreaded())
    {
        for (int i = 0; i < INTERFACE_ENTRY_CACHE_SIZE; i++)
        {
            if (pRCW->m_aInterfaceEntries[i].m_pMT == pComInfo->m_pInterfaceMT)
            {
                IUnknown* pUnk = pRCW->m_aInterfaceEntries[i].m_pUnknown;
                if (pUnk != NULL)
                {
                    void* targetMaybe = GetCOMIPFromRCW_GetTarget(pUnk, pComInfo);
                    if (targetMaybe != NULL)
                    {
                        *ppTarget = targetMaybe;
                        return pUnk;
                    }
                }
            }
        }
    }

    return NULL;
}

//==================================================================================================================
// The GetCOMIPFromRCW helper exists in four specialized versions to optimize CLR->COM perf. Please be careful when
// changing this code as one of these methods is executed as part of every CLR->COM call so every instruction counts.
//==================================================================================================================

#include <optsmallperfcritical.h>

// This helper can handle any CLR->COM call.
FCIMPL3(IUnknown*, StubHelpers::GetCOMIPFromRCW, Object* pSrcUNSAFE, MethodDesc* pMD, void** ppTarget)
{
    CONTRACTL
    {
        FCALL_CHECK;
        PRECONDITION(pSrcUNSAFE != NULL);
        PRECONDITION(pMD != NULL && (pMD->IsCLRToCOMCall() || pMD->IsEEImpl()));
        PRECONDITION(ppTarget != NULL);
    }
    CONTRACTL_END;

    // See the slow path as well, StubHelpers_GetCOMIPFromRCWSlow. The first part of that
    // function is identical to this one, but it handles the case where the OLE TLS
    // data hasn't been created yet.
    OBJECTREF pSrc = ObjectToOBJECTREF(pSrcUNSAFE);
    CLRToCOMCallInfo* pComInfo = CLRToCOMCallInfo::FromMethodDesc(pMD);
    RCW* pRCW = pSrc->PassiveGetSyncBlock()->GetInteropInfoNoCreate()->GetRawRCW();
    if (pRCW != NULL)
    {
        // This is the "fast path" for compiled ML stubs. The idea is to aim for an efficient RCW cache hit.
        SOleTlsData* pOleTlsData = TryGetOleTlsData();
        if (pOleTlsData != NULL)
            return GetCOMIPFromRCW_GetTargetFromRCWCache(pOleTlsData, pRCW, pComInfo, ppTarget);
    }
    return NULL;
}
FCIMPLEND

#include <optdefault.h>

extern "C" IUnknown* QCALLTYPE StubHelpers_GetCOMIPFromRCWSlow(QCall::ObjectHandleOnStack pSrc, MethodDesc* pMD, void** ppTarget)
{
    QCALL_CONTRACT;
    _ASSERTE(pMD != NULL);
    _ASSERTE(ppTarget != NULL);

    IUnknown *pIntf = NULL;
    BEGIN_QCALL;

    GCX_COOP();

    OBJECTREF objRef = pSrc.Get();
    GCPROTECT_BEGIN(objRef);

    // This snippet exists to enable OLE TLS data creation that isn't possible on the fast path.
    // It is practically identical to the StubHelpers::GetCOMIPFromRCW FCALL, but in the event the OLE TLS
    // data on this thread hasn't occurred yet, we will create it. Since this is the slow path, trying the
    // cache again isn't a problem.
    SOleTlsData* pOleTlsData = GetOrCreateOleTlsData(); // Ensure OLE TLS data is created.
    CLRToCOMCallInfo* pComInfo = CLRToCOMCallInfo::FromMethodDesc(pMD);
    RCW* pRCW = objRef->PassiveGetSyncBlock()->GetInteropInfoNoCreate()->GetRawRCW();
    if (pRCW != NULL)
    {
        IUnknown* pUnk = GetCOMIPFromRCW_GetTargetFromRCWCache(pOleTlsData, pRCW, pComInfo, ppTarget);
        if (pUnk != NULL)
            return pUnk;
    }

    // Still not in the cache and we've ensured the OLE TLS data was created.
    SafeComHolder<IUnknown> pRetUnk = ComObject::GetComIPFromRCWThrowing(&objRef, pComInfo->m_pInterfaceMT);
    *ppTarget = GetCOMIPFromRCW_GetTarget(pRetUnk, pComInfo);
    _ASSERTE(*ppTarget != NULL);

    pIntf = pRetUnk.Extract();

    GCPROTECT_END();

    END_QCALL;

    return pIntf;
}

extern "C" void QCALLTYPE ObjectMarshaler_ConvertToNative(QCall::ObjectHandleOnStack pSrcUNSAFE, VARIANT* pDest)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();

    OBJECTREF pSrc = pSrcUNSAFE.Get();
    GCPROTECT_BEGIN(pSrc);

    if (pDest->vt & VT_BYREF)
    {
        OleVariant::MarshalOleRefVariantForObject(&pSrc, pDest);
    }
    else
    {
        OleVariant::MarshalOleVariantForObject(&pSrc, pDest);
    }

    GCPROTECT_END();

    END_QCALL;
}

extern "C" void QCALLTYPE ObjectMarshaler_ConvertToManaged(VARIANT* pSrc, QCall::ObjectHandleOnStack retObject)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();

    OBJECTREF retVal = NULL;
    GCPROTECT_BEGIN(retVal);

    // The IL stub is going to call ObjectMarshaler.ClearNative() afterwards.
    // If it doesn't it's a bug in ILObjectMarshaler.
    OleVariant::MarshalObjectForOleVariant(pSrc, &retVal);
    retObject.Set(retVal);

    GCPROTECT_END();

    END_QCALL;
}

#include <optsmallperfcritical.h>
extern "C" IUnknown* QCALLTYPE InterfaceMarshaler_ConvertToNative(QCall::ObjectHandleOnStack pObjUNSAFE, MethodTable* pItfMT, MethodTable* pClsMT, DWORD dwFlags)
{
    QCALL_CONTRACT;

    IUnknown *pIntf = NULL;
    BEGIN_QCALL;

    // We're going to be making some COM calls, better initialize COM.
    EnsureComStarted();

    GCX_COOP();

    OBJECTREF pObj = pObjUNSAFE.Get();
    GCPROTECT_BEGIN(pObj);

    pIntf = MarshalObjectToInterface(&pObj, pItfMT, pClsMT, dwFlags);

    GCPROTECT_END();

    END_QCALL;

    return pIntf;
}

extern "C" void QCALLTYPE InterfaceMarshaler_ConvertToManaged(IUnknown** ppUnk, MethodTable* pItfMT, MethodTable* pClsMT, DWORD dwFlags, QCall::ObjectHandleOnStack retObject)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    // We're going to be making some COM calls, better initialize COM.
    EnsureComStarted();

    GCX_COOP();

    OBJECTREF pObj = NULL;
    GCPROTECT_BEGIN(pObj);

    UnmarshalObjectFromInterface(&pObj, ppUnk, pItfMT, pClsMT, dwFlags);
    retObject.Set(pObj);

    GCPROTECT_END();

    END_QCALL;
}
#include <optdefault.h>

#endif // FEATURE_COMINTEROP

FCIMPL0(void, StubHelpers::ClearLastError)
{
    FCALL_CONTRACT;

    ::SetLastError(0);
}
FCIMPLEND

FCIMPL1(void*, StubHelpers::GetDelegateTarget, DelegateObject *pThisUNSAFE)
{
    PCODE pEntryPoint = (PCODE)NULL;

#ifdef _DEBUG
    PreserveLastErrorHolder preserveLastError;
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

    return (PVOID)pEntryPoint;
}
FCIMPLEND

#include <optsmallperfcritical.h>
FCIMPL2(FC_BOOL_RET, StubHelpers::TryGetStringTrailByte, StringObject* thisRefUNSAFE, UINT8 *pbData)
{
    FCALL_CONTRACT;

    STRINGREF thisRef = ObjectToSTRINGREF(thisRefUNSAFE);
    FC_RETURN_BOOL(thisRef->GetTrailByte(pbData));
}
FCIMPLEND
#include <optdefault.h>

extern "C" void QCALLTYPE StubHelpers_SetStringTrailByte(QCall::StringHandleOnStack str, UINT8 bData)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();
    str.Get()->SetTrailByte(bData);

    END_QCALL;
}

extern "C" void QCALLTYPE StubHelpers_ThrowInteropParamException(INT resID, INT paramIdx)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;
    ::ThrowInteropParamException(resID, paramIdx);
    END_QCALL;
}

#ifdef PROFILING_SUPPORTED
extern "C" void* QCALLTYPE StubHelpers_ProfilerBeginTransitionCallback(MethodDesc* pTargetMD)
{
    PreserveLastErrorHolder preserveLastError;

    QCALL_CONTRACT;

    BEGIN_QCALL;

    ProfilerManagedToUnmanagedTransitionMD(pTargetMD, COR_PRF_TRANSITION_CALL);

    END_QCALL;

    return pTargetMD;
}

extern "C" void QCALLTYPE StubHelpers_ProfilerEndTransitionCallback(MethodDesc* pTargetMD)
{
    PreserveLastErrorHolder preserveLastError;

    QCALL_CONTRACT;

    BEGIN_QCALL;

    ProfilerUnmanagedToManagedTransitionMD(pTargetMD, COR_PRF_TRANSITION_RETURN);

    END_QCALL;
}
#endif // PROFILING_SUPPORTED

extern "C" void QCALLTYPE StubHelpers_MarshalToManagedVaList(va_list va, VARARGS* pArgIterator)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;
    VARARGS::MarshalToManagedVaList(va, pArgIterator);
    END_QCALL;
}

extern "C" void QCALLTYPE StubHelpers_MarshalToUnmanagedVaList(va_list va, DWORD cbVaListSize, const VARARGS* pArgIterator)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;
    VARARGS::MarshalToUnmanagedVaList(va, cbVaListSize, pArgIterator);
    END_QCALL;
}

extern "C" void QCALLTYPE StubHelpers_ValidateObject(QCall::ObjectHandleOnStack pObj, MethodDesc *pMD)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

#ifdef VERIFY_HEAP
    GCX_COOP();

    StackSString errorString;
    EX_TRY
    {
        AVInRuntimeImplOkayHolder AVOkay;
        // don't validate the next object if a BGC is in progress.  we can race with background
        // sweep which could make the next object a Free object underneath us if it's dead.
        ValidateObjectInternal(OBJECTREFToObject(pObj.Get()), !(GCHeapUtilities::GetGCHeap()->IsConcurrentGCInProgress()));
    }
    EX_CATCH
    {
        FormatValidationMessage(pMD, errorString);
        EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(COR_E_EXECUTIONENGINE, errorString.GetUnicode());
    }
    EX_END_CATCH_UNREACHABLE;

#else // VERIFY_HEAP
    EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(COR_E_FAILFAST, "No validation support without VERIFY_HEAP");
#endif // VERIFY_HEAP

    END_QCALL;
}

extern "C" void QCALLTYPE StubHelpers_ValidateByref(void *pByref, MethodDesc *pMD)
{
    QCALL_CONTRACT;

    // Skip byref if is not pointing inside managed heap
    if (!GCHeapUtilities::GetGCHeap()->IsHeapPointer(pByref))
    {
        return;
    }

    BEGIN_QCALL;

#ifdef VERIFY_HEAP
    GCX_COOP();

    // We cannot validate byrefs at this point as code:GCHeap.GetContainingObject could potentially race
    // with allocations on other threads. We'll just remember this byref along with the interop MD and
    // perform the validation on next GC (see code:StubHelpers.ProcessByrefValidationList).

    ByrefValidationEntry entry;
    entry.pByref = pByref;
    entry.pMD = pMD;

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
#else // VERIFY_HEAP
    EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(COR_E_FAILFAST, "No validation support without VERIFY_HEAP");
#endif // VERIFY_HEAP

    END_QCALL;
}

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

extern "C" void QCALLTYPE StubHelpers_MulticastDebuggerTraceHelper(QCall::ObjectHandleOnStack element, INT32 count)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    GCX_COOP();

    g_pDebugger->MulticastTraceNextStep((DELEGATEREF)(element.Get()), count);

    END_QCALL;
}
