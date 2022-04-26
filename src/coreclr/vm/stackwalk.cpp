// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// STACKWALK.CPP



#include "common.h"
#include "frames.h"
#include "threads.h"
#include "stackwalk.h"
#include "excep.h"
#include "eetwain.h"
#include "codeman.h"
#include "eeconfig.h"
#include "dbginterface.h"
#include "generics.h"
#ifdef FEATURE_INTERPRETER
#include "interpreter.h"
#endif // FEATURE_INTERPRETER

#include "gcinfodecoder.h"

#ifdef FEATURE_EH_FUNCLETS
#define PROCESS_EXPLICIT_FRAME_BEFORE_MANAGED_FRAME
#endif

CrawlFrame::CrawlFrame()
{
    LIMITED_METHOD_DAC_CONTRACT;
    pCurGSCookie = NULL;
    pFirstGSCookie = NULL;
    isCachedMethod = FALSE;
}

Assembly* CrawlFrame::GetAssembly()
{
    WRAPPER_NO_CONTRACT;

    Assembly *pAssembly = NULL;
    Frame *pF = GetFrame();

    if (pF != NULL)
        pAssembly = pF->GetAssembly();

    if (pAssembly == NULL && pFunc != NULL)
        pAssembly = pFunc->GetModule()->GetAssembly();

    return pAssembly;
}

BOOL CrawlFrame::IsInCalleesFrames(LPVOID stackPointer)
{
    LIMITED_METHOD_CONTRACT;
#ifdef FEATURE_INTERPRETER
    Frame* pFrm = GetFrame();
    if (pFrm != NULL && pFrm->GetVTablePtr() == InterpreterFrame::GetMethodFrameVPtr())
    {
#ifdef DACCESS_COMPILE
        // TBD: DACize the interpreter.
        return NULL;
#else
        return dac_cast<PTR_InterpreterFrame>(pFrm)->GetInterpreter()->IsInCalleesFrames(stackPointer);
#endif
    }
    else if (pFunc != NULL)
    {
        return ::IsInCalleesFrames(GetRegisterSet(), stackPointer);
    }
    else
    {
        return FALSE;
    }
#else
    return ::IsInCalleesFrames(GetRegisterSet(), stackPointer);
#endif
}

#ifdef FEATURE_INTERPRETER
MethodDesc* CrawlFrame::GetFunction()
{
    LIMITED_METHOD_DAC_CONTRACT;
    if (pFunc != NULL)
    {
        return pFunc;
    }
    else
    {
        Frame* pFrm = GetFrame();
        if (pFrm != NULL && pFrm->GetVTablePtr() == InterpreterFrame::GetMethodFrameVPtr())
        {
#ifdef DACCESS_COMPILE
            // TBD: DACize the interpreter.
            return NULL;
#else
            return dac_cast<PTR_InterpreterFrame>(pFrm)->GetInterpreter()->GetMethodDesc();
#endif
        }
        else
        {
            return NULL;
        }
    }
}
#endif // FEATURE_INTERPRETER

OBJECTREF CrawlFrame::GetThisPointer()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    if (!pFunc || pFunc->IsStatic() || pFunc->GetMethodTable()->IsValueType())
        return NULL;

    // As discussed in the specification comment at the declaration, the precondition, unfortunately,
    // differs by architecture.  @TODO: fix this.
#if defined(TARGET_X86)
    _ASSERTE_MSG((pFunc->IsSharedByGenericInstantiations() && pFunc->AcquiresInstMethodTableFromThis())
                 || pFunc->IsSynchronized(),
                 "Precondition");
#else
    _ASSERTE_MSG(pFunc->IsSharedByGenericInstantiations() && pFunc->AcquiresInstMethodTableFromThis(), "Precondition");
#endif

    if (isFrameless)
    {
        return GetCodeManager()->GetInstance(pRD,
                                            &codeInfo);
    }
    else
    {
        _ASSERTE(pFrame);
        _ASSERTE(pFunc);
        /*ISSUE: we already know that we have (at least) a method */
        /*       might need adjustment as soon as we solved the
                 jit-helper frame question
        */
        //<TODO>@TODO: What about other calling conventions?
//        _ASSERT(pFunc()->GetCallSig()->CALLING CONVENTION);</TODO>

#ifdef TARGET_AMD64
        // @TODO: PORT: we need to find the this pointer without triggering a GC
        //              or find a way to make this method GC_TRIGGERS
        return NULL;
#else
        return (dac_cast<PTR_FramedMethodFrame>(pFrame))->GetThis();
#endif // TARGET_AMD64
    }
}


//-----------------------------------------------------------------------------
// Get the "Ambient SP" from a  CrawlFrame.
// This will be null if there is no Ambient SP (eg, in the prolog / epilog,
// or on certain platforms),
//-----------------------------------------------------------------------------
TADDR CrawlFrame::GetAmbientSPFromCrawlFrame()
{
    SUPPORTS_DAC;
#if defined(TARGET_X86)
    // we set nesting level to zero because it won't be used for esp-framed methods,
    // and zero is at least valid for ebp based methods (where we won't use the ambient esp anyways)
    DWORD nestingLevel = 0;
    return GetCodeManager()->GetAmbientSP(
        GetRegisterSet(),
        GetCodeInfo(),
        GetRelOffset(),
        nestingLevel,
        GetCodeManState()
        );

#elif defined(TARGET_ARM)
    return GetRegisterSet()->pCurrentContext->Sp;
#else
    return NULL;
#endif
}


PTR_VOID CrawlFrame::GetParamTypeArg()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    if (isFrameless)
    {
        return GetCodeManager()->GetParamTypeArg(pRD,
                                                &codeInfo);
    }
    else
    {
#ifdef FEATURE_INTERPRETER
        if (pFrame != NULL && pFrame->GetVTablePtr() == InterpreterFrame::GetMethodFrameVPtr())
        {
#ifdef DACCESS_COMPILE
            // TBD: DACize the interpreter.
            return NULL;
#else
            return dac_cast<PTR_InterpreterFrame>(pFrame)->GetInterpreter()->GetParamTypeArg();
#endif
        }
        // Otherwise...
#endif // FEATURE_INTERPRETER

        if (!pFunc || !pFunc->RequiresInstArg())
        {
            return NULL;
        }

#ifdef HOST_64BIT
        if (!pFunc->IsSharedByGenericInstantiations() ||
            !(pFunc->RequiresInstMethodTableArg() || pFunc->RequiresInstMethodDescArg()))
        {
            // win64 can only return the param type arg if the method is shared code
            // and actually has a param type arg
            return NULL;
        }
#endif // HOST_64BIT

        _ASSERTE(pFrame);
        _ASSERTE(pFunc);
        return (dac_cast<PTR_FramedMethodFrame>(pFrame))->GetParamTypeArg();
    }
}



// [pClassInstantiation] : Always filled in, though may be set to NULL if no inst.
// [pMethodInst] : Always filled in, though may be set to NULL if no inst.
void CrawlFrame::GetExactGenericInstantiations(Instantiation *pClassInst,
                                               Instantiation *pMethodInst)
{

    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pClassInst));
        PRECONDITION(CheckPointer(pMethodInst));
    } CONTRACTL_END;

    TypeHandle specificClass;
    MethodDesc* specificMethod;

    BOOL ret = Generics::GetExactInstantiationsOfMethodAndItsClassFromCallInformation(
        GetFunction(),
        GetExactGenericArgsToken(),
        &specificClass,
        &specificMethod);

    if (!ret)
    {
        _ASSERTE(!"Cannot return exact class instantiation when we are requested to.");
    }

    *pClassInst = specificMethod->GetExactClassInstantiation(specificClass);
    *pMethodInst = specificMethod->GetMethodInstantiation();
}

PTR_VOID CrawlFrame::GetExactGenericArgsToken()
{

    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    MethodDesc* pFunc = GetFunction();

    if (!pFunc || !pFunc->IsSharedByGenericInstantiations())
        return NULL;

    if (pFunc->AcquiresInstMethodTableFromThis())
    {
        OBJECTREF obj = GetThisPointer();
        if (obj == NULL)
            return NULL;
        return obj->GetMethodTable();
    }
    else
    {
        _ASSERTE(pFunc->RequiresInstArg());
        return GetParamTypeArg();
    }
}

    /* Is this frame at a safe spot for GC?
     */
bool CrawlFrame::IsGcSafe()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    return GetCodeManager()->IsGcSafe(&codeInfo, GetRelOffset());
}

#if defined(TARGET_ARM) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64)
bool CrawlFrame::HasTailCalls()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    return GetCodeManager()->HasTailCalls(&codeInfo);
}
#endif // TARGET_ARM || TARGET_ARM64 || TARGET_LOONGARCH64

inline void CrawlFrame::GotoNextFrame()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    //
    // Update app domain if this frame caused a transition
    //

    pFrame = pFrame->Next();

    if (pFrame != FRAME_TOP)
    {
        SetCurGSCookie(Frame::SafeGetGSCookiePtr(pFrame));
    }
}

//******************************************************************************

// For asynchronous stackwalks, the thread being walked may not be suspended.
// It could cause a buffer-overrun while the stack-walk is in progress.
// To detect this, we can only use data that is guarded by a GSCookie
// that has been recently checked.
// This function should be called after doing any time-consuming activity
// during stack-walking to reduce the window in which a buffer-overrun
// could cause an problems.
//
// To keep things simple, we do this checking even for synchronous stack-walks.
void CrawlFrame::CheckGSCookies()
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

#if !defined(DACCESS_COMPILE)
    if (pFirstGSCookie == NULL)
        return;

    if (*pFirstGSCookie != GetProcessGSCookie())
        DoJITFailFast();

    if(*pCurGSCookie   != GetProcessGSCookie())
        DoJITFailFast();
#endif // !DACCESS_COMPILE
}

void CrawlFrame::SetCurGSCookie(GSCookie * pGSCookie)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

#if !defined(DACCESS_COMPILE)
    if (pGSCookie == NULL)
        DoJITFailFast();

    pCurGSCookie = pGSCookie;
    if (pFirstGSCookie == NULL)
        pFirstGSCookie = pGSCookie;

    CheckGSCookies();
#endif // !DACCESS_COMPILE
}

#if defined(FEATURE_EH_FUNCLETS)
bool CrawlFrame::IsFilterFunclet()
{
    WRAPPER_NO_CONTRACT;

    if (!IsFrameless())
    {
        return false;
    }

    if (!isFilterFuncletCached)
    {
        isFilterFunclet = GetJitManager()->IsFilterFunclet(&codeInfo) != 0;
        isFilterFuncletCached = true;
    }

    return isFilterFunclet;
}

#endif // FEATURE_EH_FUNCLETS

//******************************************************************************
#if defined(ELIMINATE_FEF)
//******************************************************************************
// Advance to the next ExInfo.  Typically done when an ExInfo has been used and
//  should not be used again.
//******************************************************************************
void ExInfoWalker::WalkOne()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    if (m_pExInfo)
    {
        LOG((LF_EH, LL_INFO10000, "ExInfoWalker::WalkOne: advancing ExInfo chain: pExInfo:%p, pContext:%p; prev:%p, pContext:%p\n",
              m_pExInfo, m_pExInfo->m_pContext, m_pExInfo->m_pPrevNestedInfo, m_pExInfo->m_pPrevNestedInfo?m_pExInfo->m_pPrevNestedInfo->m_pContext:0));
        m_pExInfo = m_pExInfo->m_pPrevNestedInfo;
    }
} // void ExInfoWalker::WalkOne()

//******************************************************************************
// Attempt to find an ExInfo with a pContext that is higher (older) than
//  a given minimum location.  (It is the pContext's SP that is relevant.)
//******************************************************************************
void ExInfoWalker::WalkToPosition(
    TADDR       taMinimum,                  // Starting point of stack walk.
    BOOL        bPopFrames)                 // If true, ResetUseExInfoForStackwalk on each exinfo.
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    while (m_pExInfo &&
           ((GetSPFromContext() < taMinimum) ||
            (GetSPFromContext() == NULL)) )
    {
        // Try the next ExInfo, if there is one.
        LOG((LF_EH, LL_INFO10000,
             "ExInfoWalker::WalkToPosition: searching ExInfo chain: m_pExInfo:%p, pContext:%p; \
              prev:%p, pContext:%p; pStartFrame:%p\n",
              m_pExInfo,
              m_pExInfo->m_pContext,
              m_pExInfo->m_pPrevNestedInfo,
              (m_pExInfo->m_pPrevNestedInfo ? m_pExInfo->m_pPrevNestedInfo->m_pContext : 0),
              taMinimum));

        if (bPopFrames)
        {   // If caller asked for it, reset the bit which indicates that this ExInfo marks a fault from managed code.
            //  This is done so that the fault can be effectively "unwound" from the stack, similarly to how Frames
            //  are unlinked from the Frame chain.
            m_pExInfo->m_ExceptionFlags.ResetUseExInfoForStackwalk();
        }
        m_pExInfo = m_pExInfo->m_pPrevNestedInfo;
    }
    // At this point, m_pExInfo is NULL, or points to a pContext that is greater than taMinimum.
} // void ExInfoWalker::WalkToPosition()

//******************************************************************************
// Attempt to find an ExInfo with a pContext that has an IP in managed code.
//******************************************************************************
void ExInfoWalker::WalkToManaged()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;


    while (m_pExInfo)
    {
        // See if the current ExInfo has a CONTEXT that "returns" to managed code, and, if so, exit the loop.
        if (m_pExInfo->m_ExceptionFlags.UseExInfoForStackwalk() &&
            GetContext() &&
            ExecutionManager::IsManagedCode(GetIP(GetContext())))
        {
                break;
        }
        // No, so skip to next, if any.
        LOG((LF_EH, LL_INFO1000, "ExInfoWalker::WalkToManaged: searching for ExInfo->managed: m_pExInfo:%p, pContext:%p, sp:%p; prev:%p, pContext:%p\n",
              m_pExInfo,
              GetContext(),
              GetSPFromContext(),
              m_pExInfo->m_pPrevNestedInfo,
              m_pExInfo->m_pPrevNestedInfo?m_pExInfo->m_pPrevNestedInfo->m_pContext:0));
        m_pExInfo = m_pExInfo->m_pPrevNestedInfo;
    }
    // At this point, m_pExInfo is NULL, or points to a pContext that has an IP in managed code.
} // void ExInfoWalker::WalkToManaged()
#endif // defined(ELIMINATE_FEF)

#ifdef FEATURE_EH_FUNCLETS
// static
UINT_PTR Thread::VirtualUnwindCallFrame(PREGDISPLAY pRD, EECodeInfo* pCodeInfo /*= NULL*/)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;

        PRECONDITION(GetControlPC(pRD) == GetIP(pRD->pCurrentContext));
    }
    CONTRACTL_END;

    if (pRD->IsCallerContextValid)
    {
        // We already have the caller's frame context
        // We just switch the pointers
        PT_CONTEXT temp      = pRD->pCurrentContext;
        pRD->pCurrentContext = pRD->pCallerContext;
        pRD->pCallerContext  = temp;

        PT_KNONVOLATILE_CONTEXT_POINTERS tempPtrs = pRD->pCurrentContextPointers;
        pRD->pCurrentContextPointers            = pRD->pCallerContextPointers;
        pRD->pCallerContextPointers             = tempPtrs;
    }
    else
    {
        VirtualUnwindCallFrame(pRD->pCurrentContext, pRD->pCurrentContextPointers, pCodeInfo);
    }

    SyncRegDisplayToCurrentContext(pRD);
    pRD->IsCallerContextValid = FALSE;
    pRD->IsCallerSPValid      = FALSE;        // Don't add usage of this field.  This is only temporary.

    return pRD->ControlPC;
}


// static
PCODE Thread::VirtualUnwindCallFrame(T_CONTEXT* pContext,
                                        T_KNONVOLATILE_CONTEXT_POINTERS* pContextPointers /*= NULL*/,
                                        EECodeInfo * pCodeInfo /*= NULL*/)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pContext, NULL_NOT_OK));
        PRECONDITION(CheckPointer(pContextPointers, NULL_OK));
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

    PCODE           uControlPc = GetIP(pContext);

#if !defined(DACCESS_COMPILE)
    UINT_PTR            uImageBase;
    PT_RUNTIME_FUNCTION pFunctionEntry;

    if (pCodeInfo == NULL)
    {
#ifndef TARGET_UNIX
        pFunctionEntry = RtlLookupFunctionEntry(uControlPc,
                                            ARM_ONLY((DWORD*))(&uImageBase),
                                            NULL);
#else // !TARGET_UNIX
        EECodeInfo codeInfo;

        codeInfo.Init(uControlPc);
        pFunctionEntry = codeInfo.GetFunctionEntry();
        uImageBase = (UINT_PTR)codeInfo.GetModuleBase();
#endif // !TARGET_UNIX
    }
    else
    {
        pFunctionEntry      = pCodeInfo->GetFunctionEntry();
        uImageBase          = (UINT_PTR)pCodeInfo->GetModuleBase();

        // RUNTIME_FUNCTION of cold code just points to the RUNTIME_FUNCTION of hot code. The unwinder
        // expects this indirection to be resolved, so we use RUNTIME_FUNCTION of the hot code even
        // if we are in cold code.

#if defined(_DEBUG) && !defined(TARGET_UNIX)
        UINT_PTR            uImageBaseFromOS;
        PT_RUNTIME_FUNCTION pFunctionEntryFromOS;

        pFunctionEntryFromOS  = RtlLookupFunctionEntry(uControlPc,
                                                       ARM_ONLY((DWORD*))(&uImageBaseFromOS),
                                                       NULL);

        // Note that he address returned from the OS is different from the one we have computed
        // when unwind info is registered using RtlAddGrowableFunctionTable. Compare RUNTIME_FUNCTION content.
        _ASSERTE( (uImageBase == uImageBaseFromOS) && (memcmp(pFunctionEntry, pFunctionEntryFromOS, sizeof(RUNTIME_FUNCTION)) == 0) );
#endif // _DEBUG && !TARGET_UNIX
    }

    if (pFunctionEntry)
    {
        uControlPc = VirtualUnwindNonLeafCallFrame(pContext, pContextPointers, pFunctionEntry, uImageBase);
    }
    else
    {
        uControlPc = VirtualUnwindLeafCallFrame(pContext);
    }
#else  // DACCESS_COMPILE
    // We can't use RtlVirtualUnwind() from out-of-process.  Instead, we call code:DacUnwindStackFrame,
    // which is similar to StackWalk64().
    if (DacUnwindStackFrame(pContext, pContextPointers) == TRUE)
    {
        uControlPc = GetIP(pContext);
    }
    else
    {
        ThrowHR(CORDBG_E_TARGET_INCONSISTENT);
    }
#endif // !DACCESS_COMPILE

    return uControlPc;
}

#ifndef DACCESS_COMPILE

// static
PCODE Thread::VirtualUnwindLeafCallFrame(T_CONTEXT* pContext)
{
    PCODE uControlPc;

#if defined(_DEBUG) && !defined(TARGET_UNIX)
    UINT_PTR uImageBase;

    PT_RUNTIME_FUNCTION pFunctionEntry  = RtlLookupFunctionEntry((UINT_PTR)GetIP(pContext),
                                                                ARM_ONLY((DWORD*))(&uImageBase),
                                                                NULL);

    CONSISTENCY_CHECK(NULL == pFunctionEntry);
#endif // _DEBUG && !TARGET_UNIX

#if defined(TARGET_AMD64)

    uControlPc = *(ULONGLONG*)pContext->Rsp;
    pContext->Rsp += sizeof(ULONGLONG);

#elif defined(TARGET_ARM) || defined(TARGET_ARM64)

    uControlPc = TADDR(pContext->Lr);

#elif defined(TARGET_LOONGARCH64)
    uControlPc = TADDR(pContext->Ra);

#else
    PORTABILITY_ASSERT("Thread::VirtualUnwindLeafCallFrame");
    uControlPc = NULL;
#endif

    SetIP(pContext, uControlPc);


    return uControlPc;
}

// static
PCODE Thread::VirtualUnwindNonLeafCallFrame(T_CONTEXT* pContext, KNONVOLATILE_CONTEXT_POINTERS* pContextPointers,
    PT_RUNTIME_FUNCTION pFunctionEntry, UINT_PTR uImageBase)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(CheckPointer(pContext, NULL_NOT_OK));
        PRECONDITION(CheckPointer(pContextPointers, NULL_OK));
        PRECONDITION(CheckPointer(pFunctionEntry, NULL_OK));
    }
    CONTRACTL_END;

    PCODE           uControlPc = GetIP(pContext);
#ifdef HOST_64BIT
    UINT64              EstablisherFrame;
#else  // HOST_64BIT
    DWORD               EstablisherFrame;
#endif // HOST_64BIT
    PVOID               HandlerData;

    if (NULL == pFunctionEntry)
    {
#ifndef TARGET_UNIX
        pFunctionEntry  = RtlLookupFunctionEntry(uControlPc,
                                                 ARM_ONLY((DWORD*))(&uImageBase),
                                                 NULL);
#endif
        if (NULL == pFunctionEntry)
        {
            return NULL;
        }
    }

    RtlVirtualUnwind(NULL,
                     uImageBase,
                     uControlPc,
                     pFunctionEntry,
                     pContext,
                     &HandlerData,
                     &EstablisherFrame,
                     pContextPointers);

    uControlPc = GetIP(pContext);
    return uControlPc;
}

// static
UINT_PTR Thread::VirtualUnwindToFirstManagedCallFrame(T_CONTEXT* pContext)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    PCODE uControlPc = GetIP(pContext);

    // unwind out of this function and out of our caller to
    // get our caller's PSP, or our caller's caller's SP.
    while (!ExecutionManager::IsManagedCode(uControlPc))
    {
        if (IsIPInWriteBarrierCodeCopy(uControlPc))
        {
            // Pretend we were executing the barrier function at its original location so that the unwinder can unwind the frame
            uControlPc = AdjustWriteBarrierIP(uControlPc);
            SetIP(pContext, uControlPc);
        }

#ifndef TARGET_UNIX
        uControlPc = VirtualUnwindCallFrame(pContext);
#else // !TARGET_UNIX

        if (AdjustContextForVirtualStub(NULL, pContext))
        {
            uControlPc = GetIP(pContext);
            break;
        }

        BOOL success = PAL_VirtualUnwind(pContext, NULL);
        if (!success)
        {
            _ASSERTE(!"Thread::VirtualUnwindToFirstManagedCallFrame: PAL_VirtualUnwind failed");
            EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
        }

        uControlPc = GetIP(pContext);

        if (uControlPc == 0)
        {
            break;
        }
#endif // !TARGET_UNIX
    }

    return uControlPc;
}

#endif // !DACCESS_COMPILE
#endif // FEATURE_EH_FUNCLETS

#ifdef _DEBUG
void Thread::DebugLogStackWalkInfo(CrawlFrame* pCF, _In_z_ LPCSTR pszTag, UINT32 uFramesProcessed)
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;
    if (pCF->isFrameless)
    {
        LPCSTR pszType = "";

#ifdef FEATURE_EH_FUNCLETS
        if (pCF->IsFunclet())
        {
            pszType = "[funclet]";
        }
        else
#endif // FEATURE_EH_FUNCLETS
        if (pCF->pFunc->IsNoMetadata())
        {
            pszType = "[no metadata]";
        }

        LOG((LF_GCROOTS, LL_INFO10000, "STACKWALK: [%03x] %s: FRAMELESS: PC=" FMT_ADDR " SP=" FMT_ADDR " method=%s %s\n",
                uFramesProcessed,
                pszTag,
                DBG_ADDR(GetControlPC(pCF->pRD)),
                DBG_ADDR(GetRegdisplaySP(pCF->pRD)),
                pCF->pFunc->m_pszDebugMethodName,
                pszType));
    }
    else if (pCF->isNativeMarker)
    {
        LOG((LF_GCROOTS, LL_INFO10000, "STACKWALK: [%03x] %s: NATIVE   : PC=" FMT_ADDR " SP=" FMT_ADDR "\n",
                uFramesProcessed,
                pszTag,
                DBG_ADDR(GetControlPC(pCF->pRD)),
                DBG_ADDR(GetRegdisplaySP(pCF->pRD))));
    }
    else if (pCF->isNoFrameTransition)
    {
        LOG((LF_GCROOTS, LL_INFO10000, "STACKWALK: [%03x] %s: NO_FRAME : PC=" FMT_ADDR " SP=" FMT_ADDR "\n",
                uFramesProcessed,
                pszTag,
                DBG_ADDR(GetControlPC(pCF->pRD)),
                DBG_ADDR(GetRegdisplaySP(pCF->pRD))));
    }
    else
    {
        LOG((LF_GCROOTS, LL_INFO10000, "STACKWALK: [%03x] %s: EXPLICIT : PC=" FMT_ADDR " SP=" FMT_ADDR " Frame=" FMT_ADDR" vtbl=" FMT_ADDR "\n",
            uFramesProcessed,
            pszTag,
            DBG_ADDR(GetControlPC(pCF->pRD)),
            DBG_ADDR(GetRegdisplaySP(pCF->pRD)),
            DBG_ADDR(pCF->pFrame),
            DBG_ADDR((pCF->pFrame != FRAME_TOP) ? pCF->pFrame->GetVTablePtr() : NULL)));
    }
}
#endif // _DEBUG

StackWalkAction Thread::MakeStackwalkerCallback(
    CrawlFrame* pCF,
    PSTACKWALKFRAMESCALLBACK pCallback,
    VOID* pData
    DEBUG_ARG(UINT32 uFramesProcessed))
{
    INDEBUG(DebugLogStackWalkInfo(pCF, "CALLBACK", uFramesProcessed));

    // Since we may be asynchronously walking another thread's stack,
    // check (frequently) for stack-buffer-overrun corruptions
    pCF->CheckGSCookies();

    // Since the stackwalker callback may execute arbitrary managed code and possibly
    // not even return (in the case of exception unwinding), explicitly clear the
    // stackwalker thread state indicator around the callback.

    CLEAR_THREAD_TYPE_STACKWALKER();

    StackWalkAction swa = pCallback(pCF, (VOID*)pData);

    SET_THREAD_TYPE_STACKWALKER(this);

    pCF->CheckGSCookies();

#ifdef _DEBUG
    if (swa == SWA_ABORT)
    {
        LOG((LF_GCROOTS, LL_INFO10000, "STACKWALK: SWA_ABORT: callback aborted the stackwalk\n"));
    }
#endif // _DEBUG

    return swa;
}


#if !defined(DACCESS_COMPILE) && defined(TARGET_X86) && !defined(FEATURE_EH_FUNCLETS)
#define STACKWALKER_MAY_POP_FRAMES
#endif


StackWalkAction Thread::StackWalkFramesEx(
                    PREGDISPLAY pRD,        // virtual register set at crawl start
                    PSTACKWALKFRAMESCALLBACK pCallback,
                    VOID *pData,
                    unsigned flags,
                    PTR_Frame pStartFrame
                )
{
    // Note: there are cases (i.e., exception handling) where we may never return from this function. This means
    // that any C++ destructors pushed in this function will never execute, and it means that this function can
    // never have a dynamic contract.
    STATIC_CONTRACT_WRAPPER;
    SCAN_IGNORE_THROW;            // see contract above
    SCAN_IGNORE_TRIGGER;          // see contract above

    _ASSERTE(pRD);
    _ASSERTE(pCallback);

    // when POPFRAMES we don't want to allow GC trigger.
    // The only method that guarantees this now is COMPlusUnwindCallback
#ifdef STACKWALKER_MAY_POP_FRAMES
    ASSERT(!(flags & POPFRAMES) || pCallback == (PSTACKWALKFRAMESCALLBACK) COMPlusUnwindCallback);
    ASSERT(!(flags & POPFRAMES) || pRD->pContextForUnwind != NULL);
    ASSERT(!(flags & POPFRAMES) || (this == GetThread() && PreemptiveGCDisabled()));
#else // STACKWALKER_MAY_POP_FRAMES
    ASSERT(!(flags & POPFRAMES));
#endif // STACKWALKER_MAY_POP_FRAMES

    // We haven't set the stackwalker thread type flag yet, so it shouldn't be set. Only
    // exception to this is if the current call is made by a hijacking profiler which
    // redirected this thread while it was previously in the middle of another stack walk
#ifdef PROFILING_SUPPORTED
    _ASSERTE(CORProfilerStackSnapshotEnabled() || !IsStackWalkerThread());
#else
    _ASSERTE(!IsStackWalkerThread());
#endif

    StackWalkAction retVal = SWA_FAILED;

    {
        // SCOPE: Remember that we're walking the stack.
        //
        // Normally, we'd use a StackWalkerWalkingThreadHolder to temporarily set this
        // flag in the thread state, but we can't in this function, since C++ destructors
        // are forbidden when this is called for exception handling (which causes
        // MakeStackwalkerCallback() not to return). Note that in exception handling
        // cases, we will have already cleared the stack walker thread state indicator inside
        // MakeStackwalkerCallback(), so we will be properly cleaned up.
#if !defined(DACCESS_COMPILE)
        Thread* pStackWalkThreadOrig = t_pStackWalkerWalkingThread;
#endif
        SET_THREAD_TYPE_STACKWALKER(this);

        StackFrameIterator iter;
        if (iter.Init(this, pStartFrame, pRD, flags) == TRUE)
        {
            while (iter.IsValid())
            {
                retVal = MakeStackwalkerCallback(&iter.m_crawl, pCallback, pData DEBUG_ARG(iter.m_uFramesProcessed));
                if (retVal == SWA_ABORT)
                {
                    break;
                }

                retVal = iter.Next();
                if (retVal == SWA_FAILED)
                {
                    break;
                }
            }
        }

        SET_THREAD_TYPE_STACKWALKER(pStackWalkThreadOrig);
    }

    return retVal;
} // StackWalkAction Thread::StackWalkFramesEx()

StackWalkAction Thread::StackWalkFrames(PSTACKWALKFRAMESCALLBACK pCallback,
                               VOID *pData,
                               unsigned flags,
                               PTR_Frame pStartFrame)
{
    // Note: there are cases (i.e., exception handling) where we may never return from this function. This means
    // that any C++ destructors pushed in this function will never execute, and it means that this function can
    // never have a dynamic contract.
    STATIC_CONTRACT_WRAPPER;
    _ASSERTE((flags & THREAD_IS_SUSPENDED) == 0 || (flags & ALLOW_ASYNC_STACK_WALK));

    T_CONTEXT ctx;
    REGDISPLAY rd;
    bool fUseInitRegDisplay;

#ifndef DACCESS_COMPILE
    _ASSERTE(GetThreadNULLOk() == this || (flags & ALLOW_ASYNC_STACK_WALK));
    BOOL fDebuggerHasInitialContext = (GetFilterContext() != NULL);
    BOOL fProfilerHasInitialContext = (GetProfilerFilterContext() != NULL);

    // If this walk is seeded by a profiler, then the walk better be done by the profiler
    _ASSERTE(!fProfilerHasInitialContext || (flags & PROFILER_DO_STACK_SNAPSHOT));

    fUseInitRegDisplay              = fDebuggerHasInitialContext || fProfilerHasInitialContext;
#else
    fUseInitRegDisplay = true;
#endif

    if(fUseInitRegDisplay)
    {
        if (GetProfilerFilterContext() != NULL)
        {
            if (!InitRegDisplay(&rd, GetProfilerFilterContext(), TRUE))
            {
                LOG((LF_CORPROF, LL_INFO100, "**PROF: InitRegDisplay(&rd, GetProfilerFilterContext() failure leads to SWA_FAILED.\n"));
                return SWA_FAILED;
            }
        }
        else
        {
            if (!InitRegDisplay(&rd, &ctx, FALSE))
            {
                LOG((LF_CORPROF, LL_INFO100, "**PROF: InitRegDisplay(&rd, &ctx, FALSE) failure leads to SWA_FAILED.\n"));
                return SWA_FAILED;
            }
        }
    }
    else
    {
        // Initialize the context
        memset(&ctx, 0x00, sizeof(T_CONTEXT));
        SetIP(&ctx, 0);
        SetSP(&ctx, 0);
        SetFP(&ctx, 0);
        LOG((LF_GCROOTS, LL_INFO100000, "STACKWALK    starting with partial context\n"));
        FillRegDisplay(&rd, &ctx);
    }

#ifdef STACKWALKER_MAY_POP_FRAMES
    if (flags & POPFRAMES)
        rd.pContextForUnwind = &ctx;
#endif

    return StackWalkFramesEx(&rd, pCallback, pData, flags, pStartFrame);
}

StackWalkAction StackWalkFunctions(Thread * thread,
                                   PSTACKWALKFRAMESCALLBACK pCallback,
                                   VOID * pData)
{
    // Note: there are cases (i.e., exception handling) where we may never return from this function. This means
    // that any C++ destructors pushed in this function will never execute, and it means that this function can
    // never have a dynamic contract.
    STATIC_CONTRACT_WRAPPER;

    return thread->StackWalkFrames(pCallback, pData, FUNCTIONSONLY);
}

// ----------------------------------------------------------------------------
// StackFrameIterator::StackFrameIterator
//
// Description:
//    This constructor is for the usage pattern of creating an uninitialized StackFrameIterator and then
//    calling Init() on it.
//
// Assumptions:
//    * The caller needs to call Init() with the correct arguments before using the StackFrameIterator.
//

StackFrameIterator::StackFrameIterator()
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;
    CommonCtor(NULL, NULL, 0xbaadf00d);
} // StackFrameIterator::StackFrameIterator()

// ----------------------------------------------------------------------------
// StackFrameIterator::StackFrameIterator
//
// Description:
//    This constructor is for the usage pattern of creating an initialized StackFrameIterator and then
//    calling ResetRegDisp() on it.
//
// Arguments:
//    * pThread - the thread to walk
//    * pFrame  - the starting explicit frame; NULL means use the top explicit frame from the frame chain
//    * flags   - the stackwalk flags
//
// Assumptions:
//    * The caller can call ResetRegDisp() to use the StackFrameIterator without calling Init() first.
//

StackFrameIterator::StackFrameIterator(Thread * pThread, PTR_Frame pFrame, ULONG32 flags)
{
    SUPPORTS_DAC;
    CommonCtor(pThread, pFrame, flags);
} // StackFrameIterator::StackFrameIterator()

// ----------------------------------------------------------------------------
// StackFrameIterator::CommonCtor
//
// Description:
//    This is a helper for the two constructors.
//
// Arguments:
//    * pThread - the thread to walk
//    * pFrame  - the starting explicit frame; NULL means use the top explicit frame from the frame chain
//    * flags   - the stackwalk flags
//

void StackFrameIterator::CommonCtor(Thread * pThread, PTR_Frame pFrame, ULONG32 flags)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    INDEBUG(m_uFramesProcessed = 0);

    m_frameState = SFITER_UNINITIALIZED;
    m_pThread    = pThread;

    m_pStartFrame = pFrame;
#if defined(_DEBUG)
    if (m_pStartFrame != NULL)
    {
        m_pRealStartFrame = m_pStartFrame;
    }
    else if (m_pThread != NULL)
    {
        m_pRealStartFrame = m_pThread->GetFrame();
    }
    else
    {
        m_pRealStartFrame = NULL;
    }
#endif // _DEBUG

    m_flags        = flags;
    m_codeManFlags = (ICodeManagerFlags)0;

    m_pCachedGSCookie = NULL;

#if defined(FEATURE_EH_FUNCLETS)
    m_sfParent = StackFrame();
    ResetGCRefReportingState();
    m_fDidFuncletReportGCReferences = true;
#endif // FEATURE_EH_FUNCLETS

#if defined(RECORD_RESUMABLE_FRAME_SP)
    m_pvResumableFrameTargetSP = NULL;
#endif
} // StackFrameIterator::CommonCtor()

//---------------------------------------------------------------------------------------
//
// Initialize the iterator.  Note that the iterator has thread-affinity,
// and the stackwalk flags cannot be changed once the iterator is created.
// Depending on the flags, initialization may involve unwinding to a frame of interest.
// The unwinding could fail.
//
// Arguments:
//    pThread  - the thread to walk
//    pFrame   - the starting explicit frame; NULL means use the top explicit frame from
//               pThread->GetFrame()
//    pRegDisp - the initial REGDISPLAY
//    flags    - the stackwalk flags
//
// Return Value:
//    Returns true if the initialization is successful.  The initialization could fail because
//    we fail to unwind.
//
// Notes:
//    Do not do anything funky between initializing a StackFrameIterator and actually using it.
//    In particular, do not resume the thread.  We only unhijack the thread once in Init().
//    Refer to StackWalkFramesEx() for the typical usage pattern.
//

BOOL StackFrameIterator::Init(Thread *    pThread,
                              PTR_Frame   pFrame,
                              PREGDISPLAY pRegDisp,
                              ULONG32     flags)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    _ASSERTE(pThread  != NULL);
    _ASSERTE(pRegDisp != NULL);

#ifdef FEATURE_EH_FUNCLETS
    _ASSERTE(!(flags & POPFRAMES));
    _ASSERTE(pRegDisp->pCurrentContext);
#endif // FEATURE_EH_FUNCLETS

    BEGIN_FORBID_TYPELOAD();

#ifdef FEATURE_HIJACK
    // We can't crawl the stack of a thread that currently has a hijack pending
    // (since the hijack routine won't be recognized by any code manager). So we
    // undo any hijack, the EE will re-attempt it later.

#if !defined(DACCESS_COMPILE)
    // OOP stackwalks need to deal with hijacked threads in a special way.
    pThread->UnhijackThread();
#endif // !DACCESS_COMPILE

#endif // FEATURE_HIJACK

    // FRAME_TOP and NULL must be distinct values. This assert
    // will fire if someone changes this.
    static_assert_no_msg(FRAME_TOP_VALUE != NULL);

    m_frameState = SFITER_UNINITIALIZED;

    m_pThread = pThread;
    m_flags   = flags;

    ResetCrawlFrame();

    m_pStartFrame = pFrame;
    if (m_pStartFrame)
    {
        m_crawl.pFrame = m_pStartFrame;
    }
    else
    {
        m_crawl.pFrame = m_pThread->GetFrame();
        _ASSERTE(m_crawl.pFrame != NULL);
    }
    INDEBUG(m_pRealStartFrame = m_crawl.pFrame);

    if (m_crawl.pFrame != FRAME_TOP && !(m_flags & SKIP_GSCOOKIE_CHECK))
    {
        m_crawl.SetCurGSCookie(Frame::SafeGetGSCookiePtr(m_crawl.pFrame));
    }

    m_crawl.pRD = pRegDisp;

    m_codeManFlags = (ICodeManagerFlags)((flags & QUICKUNWIND) ? 0 : UpdateAllRegs);
    m_scanFlag = ExecutionManager::GetScanFlags();

#if defined(ELIMINATE_FEF)
    // Walk the ExInfo chain, past any specified starting frame.
    m_exInfoWalk.Init(&(pThread->GetExceptionState()->m_currentExInfo));
    // false means don't reset UseExInfoForStackwalk
    m_exInfoWalk.WalkToPosition(dac_cast<TADDR>(m_pStartFrame), false);
#endif // ELIMINATE_FEF

    //
    // These fields are used in the iteration and will be updated on a per-frame basis:
    //
    // EECodeInfo     m_cachedCodeInfo;
    //
    // GSCookie *     m_pCachedGSCookie;
    //
    // StackFrame     m_sfParent;
    //
    // LPVOID         m_pvResumableFrameTargetSP;
    //

    // process the REGDISPLAY and stop at the first frame
    ProcessIp(GetControlPC(m_crawl.pRD));
    ProcessCurrentFrame();

    // advance to the next frame which matches the stackwalk flags
    StackWalkAction retVal = Filter();

    END_FORBID_TYPELOAD();

    return (retVal == SWA_CONTINUE);
} // StackFrameIterator::Init()

//---------------------------------------------------------------------------------------
//
// Reset the stackwalk iterator with the specified REGDISPLAY.
// The caller is responsible for making sure the REGDISPLAY is valid.
// This function is very similar to Init(), except that this function takes a REGDISPLAY
// to seed the stackwalk.  This function may also unwind depending on the flags, and the
// unwinding may fail.
//
// Arguments:
//    pRegDisp - new REGDISPLAY
//    bool     - whether the REGDISPLAY is for the leaf frame
//
// Return Value:
//    Returns true if the reset is successful.  The reset could fail because
//    we fail to unwind.
//
// Assumptions:
//    The REGDISPLAY is valid for the thread which the iterator has affinity to.
//

BOOL StackFrameIterator::ResetRegDisp(PREGDISPLAY pRegDisp,
                                      bool        fIsFirst)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    // It is invalid to reset a stackwalk if we are popping frames along the way.
    ASSERT(!(m_flags & POPFRAMES));

    BEGIN_FORBID_TYPELOAD();

    m_frameState = SFITER_UNINITIALIZED;

    // Make sure the StackFrameIterator has been initialized properly.
    _ASSERTE(m_pThread != NULL);
    _ASSERTE(m_flags != 0xbaadf00d);

    ResetCrawlFrame();

    m_crawl.isFirst = fIsFirst;

    if (m_pStartFrame)
    {
        m_crawl.pFrame = m_pStartFrame;
    }
    else
    {
        m_crawl.pFrame = m_pThread->GetFrame();
        _ASSERTE(m_crawl.pFrame != NULL);
    }

    if (m_crawl.pFrame != FRAME_TOP && !(m_flags & SKIP_GSCOOKIE_CHECK))
    {
        m_crawl.SetCurGSCookie(Frame::SafeGetGSCookiePtr(m_crawl.pFrame));
    }

    m_crawl.pRD = pRegDisp;

    m_codeManFlags = (ICodeManagerFlags)((m_flags & QUICKUNWIND) ? 0 : UpdateAllRegs);

    // make sure the REGDISPLAY is synchronized with the CONTEXT
    UpdateRegDisp();

    PCODE curPc = GetControlPC(pRegDisp);
    ProcessIp(curPc);

    // loop the frame chain to find the closet explicit frame which is lower than the specificed REGDISPLAY
    // (stack grows up towards lower address)
    if (m_crawl.pFrame != FRAME_TOP)
    {
        TADDR curSP = GetRegdisplaySP(m_crawl.pRD);

#ifdef PROCESS_EXPLICIT_FRAME_BEFORE_MANAGED_FRAME
        if (m_crawl.IsFrameless())
        {
            // On 64-bit and ARM, we stop at the explicit frames contained in a managed stack frame
            // before the managed stack frame itself.
            EECodeManager::EnsureCallerContextIsValid(m_crawl.pRD, NULL);
            curSP = GetSP(m_crawl.pRD->pCallerContext);
        }
#endif // PROCESS_EXPLICIT_FRAME_BEFORE_MANAGED_FRAME

#if defined(TARGET_X86)
        // special processing on x86; see below for more information
        TADDR curEBP = GetRegdisplayFP(m_crawl.pRD);

        CONTEXT    tmpCtx;
        REGDISPLAY tmpRD;
        CopyRegDisplay(m_crawl.pRD, &tmpRD, &tmpCtx);
#endif // TARGET_X86

        //
        // The basic idea is to loop the frame chain until we find an explicit frame whose address is below
        // (close to the root) the SP in the specified REGDISPLAY.  This works well on WIN64 platforms.
        // However, on x86, in M2U transitions, the Windows debuggers will pass us an incorrect REGDISPLAY
        // for the managed stack frame at the M2U boundary.  The REGDISPLAY is obtained by unwinding the
        // marshaling stub, and it contains an SP which is actually higher (closer to the leaf) than the
        // address of the transition frame.  It is as if the explicit frame is not contained in the stack
        // frame of any method.  Here's an example:
        //
        // ChildEBP
        // 0012e884 ntdll32!DbgBreakPoint
        // 0012e89c CLRStub[StubLinkStub]@1f0ac1e
        // 0012e8a4     invalid ESP of Foo() according to the REGDISPLAY specified by the debuggers
        // 0012e8b4     address of transition frame (NDirectMethodFrameStandalone)
        // 0012e8c8     real ESP of Foo() according to the transition frame
        // 0012e8d8 managed!Dummy.Foo()+0x20
        //
        // The original implementation of ResetRegDisp() compares the return address of the transition frame
        // and the IP in the specified REGDISPLAY to work around this problem.  However, even this comparison
        // is not enough because we may have recursive pinvoke calls on the stack (albeit an unlikely
        // scenario).  So in addition to the IP comparison, we also check EBP.  Note that this does not
        // require managed stack frames to be EBP-framed.
        //

        while (m_crawl.pFrame != FRAME_TOP)
        {
            // this check is sufficient on WIN64
            if (dac_cast<TADDR>(m_crawl.pFrame) >= curSP)
            {
#if defined(TARGET_X86)
                // check the IP
                if (m_crawl.pFrame->GetReturnAddress() != curPc)
                {
                    break;
                }
                else
                {
                    // unwind the REGDISPLAY using the transition frame and check the EBP
                    m_crawl.pFrame->UpdateRegDisplay(&tmpRD);
                    if (GetRegdisplayFP(&tmpRD) != curEBP)
                    {
                        break;
                    }
                }
#else  // !TARGET_X86
                break;
#endif // !TARGET_X86
            }

            // if the REGDISPLAY represents the managed stack frame at a M2U transition boundary,
            // update the flags on the CrawlFrame and the REGDISPLAY
            PCODE frameRetAddr = m_crawl.pFrame->GetReturnAddress();
            if (frameRetAddr == curPc)
            {
                unsigned uFrameAttribs = m_crawl.pFrame->GetFrameAttribs();

                m_crawl.isFirst       = ((uFrameAttribs & Frame::FRAME_ATTR_RESUMABLE) != 0);
                m_crawl.isInterrupted = ((uFrameAttribs & Frame::FRAME_ATTR_EXCEPTION) != 0);

                if (m_crawl.isInterrupted)
                {
                    m_crawl.hasFaulted   = ((uFrameAttribs & Frame::FRAME_ATTR_FAULTED) != 0);
                    m_crawl.isIPadjusted = ((uFrameAttribs & Frame::FRAME_ATTR_OUT_OF_LINE) != 0);
                }

                m_crawl.pFrame->UpdateRegDisplay(m_crawl.pRD);

                _ASSERTE(curPc == GetControlPC(m_crawl.pRD));
            }

            m_crawl.GotoNextFrame();
        }
    }

#if defined(ELIMINATE_FEF)
    // Similarly, we need to walk the ExInfos.
    m_exInfoWalk.Init(&(m_crawl.pThread->GetExceptionState()->m_currentExInfo));
    // false means don't reset UseExInfoForStackwalk
    m_exInfoWalk.WalkToPosition(GetRegdisplaySP(m_crawl.pRD), false);
#endif // ELIMINATE_FEF

    // now that everything is at where it should be, update the CrawlFrame
    ProcessCurrentFrame();

    // advance to the next frame which matches the stackwalk flags
    StackWalkAction retVal = Filter();

    END_FORBID_TYPELOAD();

    return (retVal == SWA_CONTINUE);
} // StackFrameIterator::ResetRegDisp()


//---------------------------------------------------------------------------------------
//
// Reset the CrawlFrame owned by the iterator.  Used by both Init() and ResetRegDisp().
//
// Assumptions:
//    this->m_pThread and this->m_flags have been initialized.
//
// Notes:
//    In addition, the following fields are not reset.  The caller must update them:
//    pFrame, pFunc, pAppDomain, pRD
//
//    Fields updated by ProcessIp():
//    isFrameless, and codeInfo
//
//    Fields updated by ProcessCurrentFrame():
//    codeManState
//

void StackFrameIterator::ResetCrawlFrame()
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    INDEBUG(memset(&(m_crawl.pFunc), 0xCC, sizeof(m_crawl.pFunc)));

    m_crawl.isFirst = true;
    m_crawl.isInterrupted = false;
    m_crawl.hasFaulted = false;
    m_crawl.isIPadjusted = false;

    m_crawl.isNativeMarker = false;
    m_crawl.isProfilerDoStackSnapshot = !!(this->m_flags & PROFILER_DO_STACK_SNAPSHOT);
    m_crawl.isNoFrameTransition = false;

    m_crawl.taNoFrameTransitionMarker = NULL;

#if defined(FEATURE_EH_FUNCLETS)
    m_crawl.isFilterFunclet       = false;
    m_crawl.isFilterFuncletCached = false;
    m_crawl.fShouldParentToFuncletSkipReportingGCReferences = false;
    m_crawl.fShouldParentFrameUseUnwindTargetPCforGCReporting = false;
#endif // FEATURE_EH_FUNCLETS

    m_crawl.pThread = this->m_pThread;

    m_crawl.isCachedMethod  = false;
    m_crawl.stackWalkCache.ClearEntry();

    m_crawl.pCurGSCookie   = NULL;
    m_crawl.pFirstGSCookie = NULL;
}

//---------------------------------------------------------------------------------------
//
// This function represents whether the iterator has reached the root of the stack or not.
// It can be used as the loop-terminating condition for the iterator.
//
// Return Value:
//    Returns true if there is more frames on the stack to walk.
//

BOOL StackFrameIterator::IsValid(void)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    // There is more to iterate if the stackwalk is currently in managed code,
    //  or if there are frames left.
    // If there is an ExInfo with a pContext, it may substitute for a Frame,
    //  if the ExInfo is due to an exception in managed code.
    if (!m_crawl.isFrameless && m_crawl.pFrame == FRAME_TOP)
    {
        // if we are stopped at a native marker frame, we can still advance at least once more
        if (m_frameState == SFITER_NATIVE_MARKER_FRAME)
        {
            _ASSERTE(m_crawl.isNativeMarker);
            return TRUE;
        }

#if defined(ELIMINATE_FEF)
        // Not in managed code, and no frames left -- check for an ExInfo.
        // @todo: check for exception?
        m_exInfoWalk.WalkToManaged();
        if (m_exInfoWalk.GetContext())
            return TRUE;
#endif // ELIMINATE_FEF

#ifdef _DEBUG
        // Try to ensure that the frame chain did not change underneath us.
        // In particular, is thread's starting frame the same as it was when
        // we started?
        BOOL bIsRealStartFrameUnchanged =
            (m_pStartFrame != NULL)
            || (m_flags & POPFRAMES)
            || (m_pRealStartFrame == m_pThread->GetFrame());

#ifdef FEATURE_HIJACK
        // In GCStress >= 4 two threads could race on triggering GC;
        // if the one that just made p/invoke call is second and hits the trap instruction
        // before call to synchronize with GC, it will push a frame [ResumableFrame on Unix
        // and RedirectedThreadFrame on Windows] concurrently with GC stackwalking.
        // In normal case (no GCStress), after p/invoke, IL_STUB will check if GC is in progress and synchronize.
        // NOTE: This condition needs to be evaluated after the previous one to prevent a subtle race condition
        // (https://github.com/dotnet/runtime/issues/11678)
        if (bIsRealStartFrameUnchanged == FALSE)
        {
            _ASSERTE(GCStress<cfg_instr>::IsEnabled());
            _ASSERTE(m_pRealStartFrame != NULL);
            _ASSERTE(m_pRealStartFrame != FRAME_TOP);
            _ASSERTE(m_pRealStartFrame->GetVTablePtr() == InlinedCallFrame::GetMethodFrameVPtr());
            _ASSERTE(m_pThread->GetFrame() != NULL);
            _ASSERTE(m_pThread->GetFrame() != FRAME_TOP);
            bIsRealStartFrameUnchanged = (m_pThread->GetFrame()->GetVTablePtr() == ResumableFrame::GetMethodFrameVPtr())
                || (m_pThread->GetFrame()->GetVTablePtr() == RedirectedThreadFrame::GetMethodFrameVPtr());
        }
#endif // FEATURE_HIJACK

        _ASSERTE(bIsRealStartFrameUnchanged);

#endif //_DEBUG

        return FALSE;
    }

    return TRUE;
} // StackFrameIterator::IsValid()

//---------------------------------------------------------------------------------------
//
// Advance to the next frame according to the stackwalk flags.  If the iterator is stopped
// at some place not specified by the stackwalk flags, this function will automatically advance
// to the next frame.
//
// Return Value:
//    SWA_CONTINUE (== SWA_DONE) if the iterator is successful in advancing to the next frame
//    SWA_FAILED if an operation performed by the iterator fails
//
// Notes:
//    This function returns SWA_DONE when advancing from the last frame to becoming invalid.
//    It returns SWA_FAILED if the iterator is invalid.
//

StackWalkAction StackFrameIterator::Next(void)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    if (!IsValid())
    {
        return SWA_FAILED;
    }

    BEGIN_FORBID_TYPELOAD();

    StackWalkAction retVal = NextRaw();
    if (retVal == SWA_CONTINUE)
    {
        retVal = Filter();
    }

    END_FORBID_TYPELOAD();
    return retVal;
}

//---------------------------------------------------------------------------------------
//
// Check whether we should stop at the current frame given the stackwalk flags.
// If not, continue advancing to the next frame.
//
// Return Value:
//    Returns SWA_CONTINUE (== SWA_DONE) if the iterator is invalid or if no automatic advancing is done.
//    Otherwise returns whatever the last call to NextRaw() returns.
//

StackWalkAction StackFrameIterator::Filter(void)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    bool fStop            = false;
    bool fSkippingFunclet = false;

#if defined(FEATURE_EH_FUNCLETS)
    bool fRecheckCurrentFrame = false;
    bool fSkipFuncletCallback = true;
#endif // defined(FEATURE_EH_FUNCLETS)

    StackWalkAction retVal = SWA_CONTINUE;

    while (IsValid())
    {
        fStop = false;
        fSkippingFunclet = false;

#if defined(FEATURE_EH_FUNCLETS)
        ExceptionTracker* pTracker = m_crawl.pThread->GetExceptionState()->GetCurrentExceptionTracker();
        fRecheckCurrentFrame = false;
        fSkipFuncletCallback = true;

        // by default, there is no funclet for the current frame
        // that reported GC references
        m_crawl.fShouldParentToFuncletSkipReportingGCReferences = false;

        // By default, assume that we are going to report GC references for this
        // CrawlFrame
        m_crawl.fShouldCrawlframeReportGCReferences = true;

        // By default, assume that parent frame is going to report GC references from
        // the actual location reported by the stack walk.
        m_crawl.fShouldParentFrameUseUnwindTargetPCforGCReporting = false;

        if (!m_sfParent.IsNull())
        {
            // we are now skipping frames to get to the funclet's parent
            fSkippingFunclet = true;
        }
#endif // FEATURE_EH_FUNCLETS

        switch (m_frameState)
        {
            case SFITER_FRAMELESS_METHOD:
#if defined(FEATURE_EH_FUNCLETS)
ProcessFuncletsForGCReporting:
                do
                {
                    // When enumerating GC references for "liveness" reporting, depending upon the architecture,
                    // the responsibility of who reports what varies:
                    //
                    // 1) On ARM, ARM64, and X64 (using RyuJIT), the funclet reports all references belonging
                    //    to itself and its parent method. This is indicated by the WantsReportOnlyLeaf flag being
                    //    set in the GC information for a function.
                    //
                    // 2) X64 (using JIT64) has the reporting distributed between the funclets and the parent method.
                    //    If some reference(s) get double reported, JIT64 can handle that by playing conservative.
                    //    JIT64 does NOT set the WantsReportOnlyLeaf flag in the function GC information.
                    //
                    // 3) On ARM, the reporting is done by funclets (if present). Otherwise, the primary method
                    //    does it.
                    //
                    // 4) x86 behaves like (1)
                    //
                    // For non-x86, the GcStackCrawlCallBack is invoked with a new flag indicating that
                    // the stackwalk is being done for GC reporting purposes - this flag is GC_FUNCLET_REFERENCE_REPORTING.
                    // The presence of this flag influences how the stackwalker will enumerate frames; which frames will
                    // result in the callback being invoked; etc. The idea is that we want to report only the
                    // relevant frames via the callback that are active on the callstack. This removes the need to
                    // double report (even though JIT64 does it), reporting of dead frames, and makes the
                    // design of reference reporting more consistent (and easier to understand) across architectures.
                    //
                    // The algorithm is as follows (at a conceptual level):
                    //
                    // 1) For each enumerated managed (frameless) frame, check if it is a funclet or not.
                    //  1.1) If it is not a funclet, pass the frame to the callback and goto (2).
                    //  1.2) If it is a funclet, we preserve the callerSP of the parent frame where the funclet was invoked from.
                    //       Pass the funclet to the callback.
                    //  1.3) For filter funclets, we enumerate all frames until we reach the parent. Once the parent is reached,
                    //       pass it to the callback with a flag indicating that its corresponding funclet has already performed
                    //       the reporting.
                    //  1.4) For non-filter funclets, we skip all the frames until we reach the parent. Once the parent is reached,
                    //       pass it to the callback with a flag indicating that its corresponding funclet has already performed
                    //       the reporting.
                    //  1.5) If we see non-filter funclets while processing a filter funclet, then goto (1.4). Once we have reached the
                    //       parent of the non-filter funclet, resume filter funclet processing as described in (1.3).
                    // 2) If another frame is enumerated, goto (1). Otherwise, stackwalk is complete.
                    //
                    // Note: When a flag is passed to the callback indicating that the funclet for a parent frame has already
                    //       reported the references, RyuJIT will simply do nothing and return from the callback.
                    //       JIT64, on the other hand, will ignore the flag and perform reporting (again).
                    //
                    // Note: For non-filter funclets there is a small window during unwind where we have conceptually unwound past a
                    //       funclet but have not yet reached the parent/handling frame.  In this case we might need the parent to
                    //       report its GC roots.  See comments around use of m_fDidFuncletReportGCReferences for more details.
                    //
                    // Needless to say, all applicable (read: active) explicit frames are also processed.

                    // Check if we are in the mode of enumerating GC references (or not)
                    if (m_flags & GC_FUNCLET_REFERENCE_REPORTING)
                    {
#ifdef TARGET_UNIX
                        // For interleaved exception handling on non-windows systems, we need to find out if the current frame
                        // was a caller of an already executed exception handler based on the previous exception trackers.
                        // The handler funclet frames are already gone from the stack, so the exception trackers are the
                        // only source of evidence about it.
                        // This is different from Windows where the full stack is preserved until an exception is fully handled
                        // and so we can detect it just from walking the stack.
                        // The filter funclet frames are different, they behave the same way on Windows and Unix. They can be present
                        // on the stack when we reach their parent frame if the filter hasn't finished running yet or they can be
                        // gone if the filter completed running, either succesfully or with unhandled exception.
                        // So the special handling below ignores trackers belonging to filter clauses.
                        bool fProcessingFilterFunclet = !m_sfFuncletParent.IsNull() && !(m_fProcessNonFilterFunclet || m_fProcessIntermediaryNonFilterFunclet);
                        if (!fRecheckCurrentFrame && !fSkippingFunclet && (pTracker != NULL) && !fProcessingFilterFunclet)
                        {
                            // The stack walker is not skipping frames now, which means it didn't find a funclet frame that
                            // would require skipping the current frame. If we find a tracker with caller of actual handling
                            // frame matching the current frame, it means that the funclet stack frame was reclaimed.
                            StackFrame sfFuncletParent;
                            ExceptionTracker* pCurrTracker = pTracker;

                            bool hasFuncletStarted = pTracker->GetEHClauseInfo()->IsManagedCodeEntered();

                            while (pCurrTracker != NULL)
                            {
                                // Ignore exception trackers for filter clauses, since their frames are handled the same way as on Windows
                                if (pCurrTracker->GetEHClauseInfo()->GetClauseType() != COR_PRF_CLAUSE_FILTER)
                                {
                                    if (hasFuncletStarted)
                                    {
                                        sfFuncletParent = pCurrTracker->GetCallerOfEnclosingClause();
                                        if (!sfFuncletParent.IsNull() && ExceptionTracker::IsUnwoundToTargetParentFrame(&m_crawl, sfFuncletParent))
                                        {
                                            break;
                                        }
                                    }

                                    sfFuncletParent = pCurrTracker->GetCallerOfCollapsedEnclosingClause();
                                    if (!sfFuncletParent.IsNull() && ExceptionTracker::IsUnwoundToTargetParentFrame(&m_crawl, sfFuncletParent))
                                    {
                                        break;
                                    }
                                }

                                // Funclets handling exception for trackers older than the current one were always started,
                                // since the current tracker was created due to an exception in the funclet belonging to
                                // the previous tracker.
                                hasFuncletStarted = true;
                                pCurrTracker = pCurrTracker->GetPreviousExceptionTracker();
                            }

                            if (pCurrTracker != NULL)
                            {
                                // The current frame is a parent of a funclet that was already unwound and removed from the stack
                                // Set the members the same way we would set them on Windows when we
                                // would detect this just from stack walking.
                                m_sfParent = sfFuncletParent;
                                m_sfFuncletParent = sfFuncletParent;
                                m_fProcessNonFilterFunclet = true;
                                m_fDidFuncletReportGCReferences = false;
                                fSkippingFunclet = true;
                            }
                        }
#endif // TARGET_UNIX

                        fRecheckCurrentFrame = false;
                        // Do we already have a reference to a funclet parent?
                        if (!m_sfFuncletParent.IsNull())
                        {
                            // Have we been processing a filter funclet without encountering any non-filter funclets?
                            if ((m_fProcessNonFilterFunclet == false) && (m_fProcessIntermediaryNonFilterFunclet == false))
                            {
                                // Yes, we have. Check the current frame and if it is the parent we are looking for,
                                // clear the flag indicating that its funclet has already reported the GC references (see
                                // below comment for Dev11 376329 explaining why we do this).
                                if (ExceptionTracker::IsUnwoundToTargetParentFrame(&m_crawl, m_sfFuncletParent))
                                {
                                    STRESS_LOG2(LF_GCROOTS, LL_INFO100,
                                    "STACKWALK: Reached parent of filter funclet @ CallerSP: %p, m_crawl.pFunc = %p\n",
                                    m_sfFuncletParent.SP, m_crawl.pFunc);

                                    // Dev11 376329 - ARM: GC hole during filter funclet dispatch.
                                    // Filters are invoked during the first pass so we cannot skip
                                    // reporting the parent frame since it's still live.  Normally
                                    // this would cause double reporting, however for filters the JIT
                                    // will report all GC roots as pinned to alleviate this problem.
                                    // Note that JIT64 does not have this problem since it always
                                    // reports the parent frame (this flag is essentially ignored)
                                    // so it's safe to make this change for all (non-x86) architectures.
                                    m_crawl.fShouldParentToFuncletSkipReportingGCReferences = false;
                                    ResetGCRefReportingState();

                                    // We have reached the parent of the filter funclet.
                                    // It is possible this is another funclet (e.g. a catch/fault/finally),
                                    // so reexamine this frame and see if it needs any skipping.
                                    fRecheckCurrentFrame = true;
                                }
                                else
                                {
                                    // When processing filter funclets, until we reach the parent frame
                                    // we should be seeing only non--filter-funclet frames. This is because
                                    // exceptions cannot escape filter funclets. Thus, there can be no frameless frames
                                    // between the filter funclet and its parent.
                                    _ASSERTE(!m_crawl.IsFilterFunclet());
                                    if (m_crawl.IsFunclet())
                                    {
                                        // This is a non-filter funclet encountered when processing a filter funclet.
                                        // In such a case, we will deliver a callback for it and skip frames until we reach
                                        // its parent. Once there, we will resume frame enumeration for finding
                                        // parent of the filter funclet we were originally processing.
                                        m_sfIntermediaryFuncletParent = ExceptionTracker::FindParentStackFrameForStackWalk(&m_crawl, true);
                                        _ASSERTE(!m_sfIntermediaryFuncletParent.IsNull());
                                        m_fProcessIntermediaryNonFilterFunclet = true;

                                        // Set the parent frame so that the funclet skipping logic (further below)
                                        // can use it.
                                        m_sfParent = m_sfIntermediaryFuncletParent;
                                        fSkipFuncletCallback = false;
                                    }
                                }
                            }
                        }
                        else
                        {
                            _ASSERTE(m_sfFuncletParent.IsNull());

                            // We don't have any funclet parent reference. Check if the current frame represents a funclet.
                            if (m_crawl.IsFunclet())
                            {
                                // Get a reference to the funclet's parent frame.
                                m_sfFuncletParent = ExceptionTracker::FindParentStackFrameForStackWalk(&m_crawl, true);

                                if (m_sfFuncletParent.IsNull())
                                {
                                    // This can only happen if the funclet (and its parent) have been unwound.
                                    _ASSERTE(ExceptionTracker::HasFrameBeenUnwoundByAnyActiveException(&m_crawl));
                                }
                                else
                                {
                                    // We should have found the funclet's parent stackframe
                                    _ASSERTE(!m_sfFuncletParent.IsNull());

                                    bool fIsFilterFunclet = m_crawl.IsFilterFunclet();

                                    STRESS_LOG4(LF_GCROOTS, LL_INFO100,
                                    "STACKWALK: Found %sFilter funclet @ SP: %p, m_crawl.pFunc = %p; FuncletParentCallerSP: %p\n",
                                    (fIsFilterFunclet) ? "" : "Non-", GetRegdisplaySP(m_crawl.GetRegisterSet()), m_crawl.pFunc, m_sfFuncletParent.SP);

                                    if (!fIsFilterFunclet)
                                    {
                                        m_fProcessNonFilterFunclet = true;

                                        // Set the parent frame so that the funclet skipping logic (further below)
                                        // can use it.
                                        m_sfParent = m_sfFuncletParent;

                                        // For non-filter funclets, we will make the callback for the funclet
                                        // but skip all the frames until we reach the parent method. When we do,
                                        // we will make a callback for it as well and then continue to make callbacks
                                        // for all upstack frames, until we reach another funclet or the top of the stack
                                        // is reached.
                                        fSkipFuncletCallback = false;
                                    }
                                    else
                                    {
                                        _ASSERTE(fIsFilterFunclet);
                                        m_fProcessNonFilterFunclet = false;

                                        // Nothing more to do as we have come across a filter funclet. In this case, we will:
                                        //
                                        // 1) Get a reference to the parent frame
                                        // 2) Report the funclet
                                        // 3) Continue to report the parent frame, along with a flag that funclet has been reported (see above)
                                        // 4) Continue to report all upstack frames
                                    }
                                }
                            } // end if (m_crawl.IsFunclet())
                        }
                    } // end if (m_flags & GC_FUNCLET_REFERENCE_REPORTING)
                }
                while(fRecheckCurrentFrame == true);

                if ((m_fProcessNonFilterFunclet == true) || (m_fProcessIntermediaryNonFilterFunclet == true) || (m_flags & (FUNCTIONSONLY | SKIPFUNCLETS)))
                {
                    bool fSkipFrameDueToUnwind = false;

                    if (m_flags & GC_FUNCLET_REFERENCE_REPORTING)
                    {
                        // When a nested exception escapes, it will unwind past a funclet.  In addition, it will
                        // unwind the frame chain up to the funclet.  When that happens, we'll basically lose
                        // all the stack frames higher than and equal to the funclet.  We can't skip funclets in
                        // the usual way because the first frame we see won't be a funclet.  It will be something
                        // which has conceptually been unwound.  We need to use the information on the
                        // ExceptionTracker to determine if a stack frame is in the unwound stack region.
                        //
                        // If we are enumerating frames for GC reporting and we determined that
                        // the current frame needs to be reported, ensure that it has not already
                        // been unwound by the active exception. If it has been, then we will set a flag
                        // indicating that its references need not be reported. The CrawlFrame, however,
                        // will still be passed to the GC stackwalk callback in case it represents a dynamic
                        // method, to allow the GC to keep that method alive.
                        if (ExceptionTracker::HasFrameBeenUnwoundByAnyActiveException(&m_crawl))
                        {
                            // Invoke the GC callback for this crawlframe (to keep any dynamic methods alive) but do not report its references.
                            m_crawl.fShouldCrawlframeReportGCReferences = false;
                            fSkipFrameDueToUnwind = true;

                            if (m_crawl.IsFunclet() && !fSkippingFunclet)
                            {
                                // we have come across a funclet that has been unwound and we haven't yet started to
                                // look for its parent.  in such a case, the funclet will not have anything to report
                                // so set the corresponding flag to indicate so.

                                _ASSERTE(m_fDidFuncletReportGCReferences);
                                m_fDidFuncletReportGCReferences = false;

                                STRESS_LOG0(LF_GCROOTS, LL_INFO100, "Unwound funclet will skip reporting references\n");
                            }
                        }
                    }
                    else if (m_flags & (FUNCTIONSONLY | SKIPFUNCLETS))
                    {
                        if (ExceptionTracker::IsInStackRegionUnwoundByCurrentException(&m_crawl))
                        {
                            // don't stop here
                            fSkipFrameDueToUnwind = true;
                        }
                    }

                    if (fSkipFrameDueToUnwind)
                    {
                        if (m_flags & GC_FUNCLET_REFERENCE_REPORTING)
                        {
                            // Check if we are skipping frames.
                            if (!m_sfParent.IsNull())
                            {
                                // Check if our have reached our target method frame.
                                // IsMaxVal() is a special value to indicate that we should skip one frame.
                                if (m_sfParent.IsMaxVal() ||
                                    ExceptionTracker::IsUnwoundToTargetParentFrame(&m_crawl, m_sfParent))
                                {
                                    // Reset flag as we have reached target method frame so no more skipping required
                                    fSkippingFunclet = false;

                                    // We've finished skipping as told.  Now check again.

                                    if ((m_fProcessIntermediaryNonFilterFunclet == true) || (m_fProcessNonFilterFunclet == true))
                                    {
                                        STRESS_LOG2(LF_GCROOTS, LL_INFO100,
                                        "STACKWALK: Reached parent of non-filter funclet @ CallerSP: %p, m_crawl.pFunc = %p\n",
                                        m_sfParent.SP, m_crawl.pFunc);

                                        // landing here indicates that the funclet's parent has been unwound so
                                        // this will always be true, no need to predicate on the state of the funclet
                                        m_crawl.fShouldParentToFuncletSkipReportingGCReferences = true;

                                        // we've reached the parent so reset our state
                                        m_fDidFuncletReportGCReferences = true;

                                        ResetGCRefReportingState(m_fProcessIntermediaryNonFilterFunclet);
                                    }

                                    m_sfParent.Clear();

                                    if (m_crawl.IsFunclet())
                                    {
                                        // We've hit a funclet.
                                        // Since we are in GC reference reporting mode,
                                        // then avoid code duplication and go to
                                        // funclet processing.
                                        fRecheckCurrentFrame = true;
                                        goto ProcessFuncletsForGCReporting;
                                    }
                                }
                            }
                        } // end if (m_flags & GC_FUNCLET_REFERENCE_REPORTING)

                        if (m_crawl.fShouldCrawlframeReportGCReferences)
                        {
                            // Skip the callback for this frame - we don't do this for unwound frames encountered
                            // in GC stackwalk since they may represent dynamic methods whose resolver objects
                            // the GC may need to keep alive.
                            break;
                        }
                    }
                    else
                    {
                        _ASSERTE(!fSkipFrameDueToUnwind);

                        // Check if we are skipping frames.
                        if (!m_sfParent.IsNull())
                        {
                            // Check if we have reached our target method frame.
                            // IsMaxVal() is a special value to indicate that we should skip one frame.
                            if (m_sfParent.IsMaxVal() ||
                                ExceptionTracker::IsUnwoundToTargetParentFrame(&m_crawl, m_sfParent))
                            {
                                // We've finished skipping as told.  Now check again.
                                if ((m_fProcessIntermediaryNonFilterFunclet == true) || (m_fProcessNonFilterFunclet == true))
                                {
                                    // If we are here, we should be in GC reference reporting mode.
                                    _ASSERTE(m_flags & GC_FUNCLET_REFERENCE_REPORTING);

                                    STRESS_LOG2(LF_GCROOTS, LL_INFO100,
                                    "STACKWALK: Reached parent of non-filter funclet @ CallerSP: %p, m_crawl.pFunc = %p\n",
                                    m_sfParent.SP, m_crawl.pFunc);

                                    // by default a funclet's parent won't report its GC roots since they would have already
                                    // been reported by the funclet.  however there is a small window during unwind before
                                    // control returns to the OS where we might require the parent to report.  more below.
                                    bool shouldSkipReporting = true;

                                    if (!m_fDidFuncletReportGCReferences)
                                    {
                                        // we have reached the parent frame of the funclet which didn't report roots since it was already unwound.
                                        // check if the parent frame of the funclet is also handling an exception. if it is, then we will need to
                                        // report roots for it since the catch handler may use references inside it.

                                        STRESS_LOG0(LF_GCROOTS, LL_INFO100,
                                        "STACKWALK: Reached parent of funclet which didn't report GC roots, since funclet is already unwound.\n");

                                        if (pTracker->GetCallerOfActualHandlingFrame() == m_sfFuncletParent)
                                        {
                                            // we should not skip reporting for this parent frame
                                            shouldSkipReporting = false;

                                            // now that we've found the parent that will report roots reset our state.
                                            m_fDidFuncletReportGCReferences = true;

                                            // After funclet gets unwound parent will begin to report gc references. Reporting GC references
                                            // using the IP of throw in parent method can crash application. Parent could have locals objects
                                            // which might not have been reported by funclet as live and would have already been collected
                                            // when funclet was on stack. Now if parent starts using IP of throw to report gc references it
                                            // would report garbage values as live objects. So instead parent can use the IP of the resume
                                            // address of catch funclet to report live GC references.
                                            m_crawl.fShouldParentFrameUseUnwindTargetPCforGCReporting = true;
                                            // Store catch clause info. Helps retrieve IP of resume address.
                                            m_crawl.ehClauseForCatch = pTracker->GetEHClauseForCatch();

                                            STRESS_LOG3(LF_GCROOTS, LL_INFO100,
                                            "STACKWALK: Parent of funclet which didn't report GC roots is handling an exception at 0x%p"
                                            "(EH handler range [%x, %x) ), so we need to specially report roots to ensure variables alive"
                                            " in its handler stay live.\n",
                                            pTracker->GetCatchToCallPC(), m_crawl.ehClauseForCatch.HandlerStartPC,
                                            m_crawl.ehClauseForCatch.HandlerEndPC);
                                        }
                                        else if (!m_crawl.IsFunclet())
                                        {
                                            // we've reached the parent and it's not handling an exception, it's also not
                                            // a funclet so reset our state.  note that we cannot reset the state when the
                                            // parent is a funclet since the leaf funclet didn't report any references and
                                            // we might have a catch handler below us that might contain GC roots.
                                            m_fDidFuncletReportGCReferences = true;
                                        }

                                        STRESS_LOG4(LF_GCROOTS, LL_INFO100,
                                        "Funclet didn't report references: handling frame: %p, m_sfFuncletParent = %p, is funclet: %d, skip reporting %d\n",
                                        pTracker->GetEstablisherOfActualHandlingFrame().SP, m_sfFuncletParent.SP, m_crawl.IsFunclet(), shouldSkipReporting);
                                    }
                                    m_crawl.fShouldParentToFuncletSkipReportingGCReferences = shouldSkipReporting;

                                    ResetGCRefReportingState(m_fProcessIntermediaryNonFilterFunclet);
                                }

                                m_sfParent.Clear();
                            }
                        } // end if (!m_sfParent.IsNull())

                        if (m_sfParent.IsNull() && m_crawl.IsFunclet())
                        {
                            // We've hit a funclet.
                            if (m_flags & GC_FUNCLET_REFERENCE_REPORTING)
                            {
                                // If we are in GC reference reporting mode,
                                // then avoid code duplication and go to
                                // funclet processing.
                                fRecheckCurrentFrame = true;
                                goto ProcessFuncletsForGCReporting;
                            }
                            else
                            {
                                // Start skipping frames.
                                m_sfParent = ExceptionTracker::FindParentStackFrameForStackWalk(&m_crawl);
                            }

                            // m_sfParent can be NULL if the current funclet is a filter,
                            // in which case we shouldn't skip the frames.
                        }

                        // If we're skipping frames due to a funclet on the stack
                        // or this is an IL stub (which don't get reported when
                        // FUNCTIONSONLY is set) we skip the callback.
                        //
                        // The only exception is the GC reference reporting mode -
                        // for it, we will callback for the funclet so that references
                        // are reported and then continue to skip all frames between the funclet
                        // and its parent, eventually making a callback for the parent as well.
                        if (m_flags & (FUNCTIONSONLY | SKIPFUNCLETS))
                        {
                            if (!m_sfParent.IsNull() || m_crawl.pFunc->IsILStub())
                            {
                                STRESS_LOG4(LF_GCROOTS, LL_INFO100,
                                    "STACKWALK: %s: not making callback for this frame, SPOfParent = %p, \
                                    isILStub = %d, m_crawl.pFunc = %pM\n",
                                    (!m_sfParent.IsNull() ? "SKIPPING_TO_FUNCLET_PARENT" : "IS_IL_STUB"),
                                    m_sfParent.SP,
                                    (m_crawl.pFunc->IsILStub() ? 1 : 0),
                                    m_crawl.pFunc);

                                // don't stop here
                                break;
                            }
                        }
                        else if (fSkipFuncletCallback && (m_flags & GC_FUNCLET_REFERENCE_REPORTING))
                        {
                            if (!m_sfParent.IsNull())
                            {
                                STRESS_LOG4(LF_GCROOTS, LL_INFO100,
                                     "STACKWALK: %s: not making callback for this frame, SPOfParent = %p, \
                                     isILStub = %d, m_crawl.pFunc = %pM\n",
                                     (!m_sfParent.IsNull() ? "SKIPPING_TO_FUNCLET_PARENT" : "IS_IL_STUB"),
                                     m_sfParent.SP,
                                     (m_crawl.pFunc->IsILStub() ? 1 : 0),
                                     m_crawl.pFunc);

                                // don't stop here
                                break;
                            }
                        }
                    }
                }
                else if (m_flags & GC_FUNCLET_REFERENCE_REPORTING)
                {
                    // If we are enumerating frames for GC reporting and we determined that
                    // the current frame needs to be reported, ensure that it has not already
                    // been unwound by the active exception. If it has been, then we will
                    // simply skip it and not deliver a callback for it.
                    if (ExceptionTracker::HasFrameBeenUnwoundByAnyActiveException(&m_crawl))
                    {
                        // Invoke the GC callback for this crawlframe (to keep any dynamic methods alive) but do not report its references.
                        m_crawl.fShouldCrawlframeReportGCReferences = false;
                    }
                }

#else // FEATURE_EH_FUNCLETS
                // Skip IL stubs
                if (m_flags & FUNCTIONSONLY)
                {
                    if (m_crawl.pFunc->IsILStub())
                    {
                        LOG((LF_GCROOTS, LL_INFO100000,
                             "STACKWALK: IS_IL_STUB: not making callback for this frame, m_crawl.pFunc = %s\n",
                             m_crawl.pFunc->m_pszDebugMethodName));

                        // don't stop here
                        break;
                    }
                }
#endif // FEATURE_EH_FUNCLETS

                fStop = true;
                break;

            case SFITER_FRAME_FUNCTION:
                //
                // fall through
                //

            case SFITER_SKIPPED_FRAME_FUNCTION:
                if (!fSkippingFunclet)
                {
#if defined(FEATURE_EH_FUNCLETS)
                    if (m_flags & GC_FUNCLET_REFERENCE_REPORTING)
                    {
                        // If we are enumerating frames for GC reporting and we determined that
                        // the current frame needs to be reported, ensure that it has not already
                        // been unwound by the active exception. If it has been, then we will
                        // simply skip it and not deliver a callback for it.
                        if (ExceptionTracker::HasFrameBeenUnwoundByAnyActiveException(&m_crawl))
                        {
                            // Invoke the GC callback for this crawlframe (to keep any dynamic methods alive) but do not report its references.
                            m_crawl.fShouldCrawlframeReportGCReferences = false;
                        }
                    }
                    else if (m_flags & (FUNCTIONSONLY | SKIPFUNCLETS))
                    {
                        // See the comment above for IsInStackRegionUnwoundByCurrentException().
                        if (ExceptionTracker::IsInStackRegionUnwoundByCurrentException(&m_crawl))
                        {
                            // don't stop here
                            break;
                        }
                    }
#endif // FEATURE_EH_FUNCLETS
                    if ( (m_crawl.pFunc != NULL) || !(m_flags & FUNCTIONSONLY) )
                    {
                        fStop = true;
                    }
                }
                break;

            case SFITER_NO_FRAME_TRANSITION:
                if (!fSkippingFunclet)
                {
                    if (m_flags & NOTIFY_ON_NO_FRAME_TRANSITIONS)
                    {
                        _ASSERTE(m_crawl.isNoFrameTransition == true);
                        fStop = true;
                    }
                }
                break;

            case SFITER_NATIVE_MARKER_FRAME:
                if (!fSkippingFunclet)
                {
                    if (m_flags & NOTIFY_ON_U2M_TRANSITIONS)
                    {
                        _ASSERTE(m_crawl.isNativeMarker == true);
                        fStop = true;
                    }
                }
                break;

            case SFITER_INITIAL_NATIVE_CONTEXT:
                if (!fSkippingFunclet)
                {
                    if (m_flags & NOTIFY_ON_INITIAL_NATIVE_CONTEXT)
                    {
                        fStop = true;
                    }
                }
                break;

            default:
                UNREACHABLE();
        }

        if (fStop)
        {
            break;
        }
        else
        {
            INDEBUG(m_crawl.pThread->DebugLogStackWalkInfo(&m_crawl, "FILTER  ", m_uFramesProcessed));
            retVal = NextRaw();
            if (retVal != SWA_CONTINUE)
            {
                break;
            }
        }
    }

    return retVal;
}

//---------------------------------------------------------------------------------------
//
// Advance to the next frame and stop, regardless of the stackwalk flags.
//
// Return Value:
//    SWA_CONTINUE (== SWA_DONE) if the iterator is successful in advancing to the next frame
//    SWA_FAILED if an operation performed by the iterator fails
//
// Assumptions:
//    The caller has checked that the iterator is valid.
//
// Notes:
//    This function returns SWA_DONE when advancing from the last frame to becoming invalid.
//

StackWalkAction StackFrameIterator::NextRaw(void)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    _ASSERTE(IsValid());

    INDEBUG(m_uFramesProcessed++);

    StackWalkAction retVal = SWA_CONTINUE;

    if (m_frameState == SFITER_SKIPPED_FRAME_FUNCTION)
    {
#if !defined(TARGET_X86) && defined(_DEBUG)
        // make sure we're not skipping a different transition
        if (m_crawl.pFrame->NeedsUpdateRegDisplay())
        {
            CONSISTENCY_CHECK(m_crawl.pFrame->IsTransitionToNativeFrame());
            if (m_crawl.pFrame->GetVTablePtr() == InlinedCallFrame::GetMethodFrameVPtr())
            {
                // ControlPC may be different as the InlinedCallFrame stays active throughout
                // the STOP_FOR_GC callout but we can use the stack/frame pointer for the assert.
                PTR_InlinedCallFrame pICF = dac_cast<PTR_InlinedCallFrame>(m_crawl.pFrame);
                CONSISTENCY_CHECK((GetRegdisplaySP(m_crawl.pRD) == (TADDR)pICF->GetCallSiteSP())
                || (GetFP(m_crawl.pRD->pCurrentContext) == pICF->GetCalleeSavedFP()));
            }
            else
            {
                CONSISTENCY_CHECK(GetControlPC(m_crawl.pRD) == m_crawl.pFrame->GetReturnAddress());
            }
        }
#endif // !defined(TARGET_X86) && defined(_DEBUG)

#if defined(STACKWALKER_MAY_POP_FRAMES)
        if (m_flags & POPFRAMES)
        {
            _ASSERTE(m_crawl.pFrame == m_crawl.pThread->GetFrame());

            // If we got here, the current frame chose not to handle the
            // exception. Give it a chance to do any termination work
            // before we pop it off.

            CLEAR_THREAD_TYPE_STACKWALKER();
            END_FORBID_TYPELOAD();

            m_crawl.pFrame->ExceptionUnwind();

            BEGIN_FORBID_TYPELOAD();
            SET_THREAD_TYPE_STACKWALKER(m_pThread);

            // Pop off this frame and go on to the next one.
            m_crawl.GotoNextFrame();

            // When StackWalkFramesEx is originally called, we ensure
            // that if POPFRAMES is set that the thread is in COOP mode
            // and that running thread is walking itself. Thus, this
            // COOP assertion is safe.
            BEGIN_GCX_ASSERT_COOP;
            m_crawl.pThread->SetFrame(m_crawl.pFrame);
            END_GCX_ASSERT_COOP;
        }
        else
#endif // STACKWALKER_MAY_POP_FRAMES
        {
            // go to the next frame
            m_crawl.GotoNextFrame();
        }

        // check for skipped frames again
        if (CheckForSkippedFrames())
        {
            // there are more skipped explicit frames
            _ASSERTE(m_frameState == SFITER_SKIPPED_FRAME_FUNCTION);
            goto Cleanup;
        }
        else
        {
#ifndef PROCESS_EXPLICIT_FRAME_BEFORE_MANAGED_FRAME
            // On x86, we process a managed stack frame before processing any explicit frames contained in it.
            // So when we are done with the skipped explicit frame, we have already processed the managed
            // stack frame, and it is time to move onto the next stack frame.
            PostProcessingForManagedFrames();
            if (m_frameState == SFITER_NATIVE_MARKER_FRAME)
            {
                goto Cleanup;
            }
#else // !PROCESS_EXPLICIT_FRAME_BEFORE_MANAGED_FRAME
            // We are done handling the skipped explicit frame at this point.  So move on to the
            // managed stack frame.
            m_crawl.isFrameless = true;
            m_crawl.codeInfo    = m_cachedCodeInfo;
            m_crawl.pFunc       = m_crawl.codeInfo.GetMethodDesc();


            PreProcessingForManagedFrames();
            goto Cleanup;
#endif // PROCESS_EXPLICIT_FRAME_BEFORE_MANAGED_FRAME
        }
    }
    else if (m_frameState == SFITER_FRAMELESS_METHOD)
    {
        // Now find out if we need to leave monitors

#ifdef TARGET_X86
        //
        // For non-x86 platforms, the JIT generates try/finally to leave monitors; for x86, the VM handles the monitor
        //
#if defined(STACKWALKER_MAY_POP_FRAMES)
        if (m_flags & POPFRAMES)
        {
            BEGIN_GCX_ASSERT_COOP;

            if (m_crawl.pFunc->IsSynchronized())
            {
                MethodDesc *   pMD = m_crawl.pFunc;
                OBJECTREF      orUnwind = NULL;

                if (m_crawl.GetCodeManager()->IsInSynchronizedRegion(m_crawl.GetRelOffset(),
                                                                    m_crawl.GetGCInfoToken(),
                                                                    m_crawl.GetCodeManagerFlags()))
                {
                    if (pMD->IsStatic())
                    {
                        MethodTable * pMT = pMD->GetMethodTable();
                        orUnwind = pMT->GetManagedClassObjectIfExists();

                        _ASSERTE(orUnwind != NULL);
                    }
                    else
                    {
                        orUnwind = m_crawl.GetCodeManager()->GetInstance(
                                                m_crawl.pRD,
                                                m_crawl.GetCodeInfo());
                    }

                    _ASSERTE(orUnwind != NULL);
                    VALIDATEOBJECTREF(orUnwind);

                    if (orUnwind != NULL)
                    {
                        orUnwind->LeaveObjMonitorAtException();
                    }
                }
            }

            END_GCX_ASSERT_COOP;
        }
#endif // STACKWALKER_MAY_POP_FRAMES
#endif // TARGET_X86

#if !defined(ELIMINATE_FEF)
        // FaultingExceptionFrame is special case where it gets
        // pushed on the stack after the frame is running
        _ASSERTE((m_crawl.pFrame == FRAME_TOP) ||
                 ((TADDR)GetRegdisplaySP(m_crawl.pRD) < dac_cast<TADDR>(m_crawl.pFrame)) ||
                 (m_crawl.pFrame->GetVTablePtr() == FaultingExceptionFrame::GetMethodFrameVPtr()));
#endif // !defined(ELIMINATE_FEF)

        // Get rid of the frame (actually, it isn't really popped)

        LOG((LF_GCROOTS, LL_EVERYTHING, "STACKWALK: [%03x] about to unwind for '%s', SP:" FMT_ADDR ", IP:" FMT_ADDR "\n",
             m_uFramesProcessed,
             m_crawl.pFunc->m_pszDebugMethodName,
             DBG_ADDR(GetRegdisplaySP(m_crawl.pRD)),
             DBG_ADDR(GetControlPC(m_crawl.pRD))));

#if !defined(DACCESS_COMPILE) && defined(HAS_QUICKUNWIND)
        StackwalkCacheEntry *pCacheEntry = m_crawl.GetStackwalkCacheEntry();
        if (pCacheEntry != NULL)
        {
            _ASSERTE(m_crawl.stackWalkCache.Enabled() && (m_flags & LIGHTUNWIND));

            // lightened schema: take stack unwind info from stackwalk cache
            EECodeManager::QuickUnwindStackFrame(m_crawl.pRD, pCacheEntry, EECodeManager::UnwindCurrentStackFrame);
        }
        else
#endif // !DACCESS_COMPILE && HAS_QUICKUNWIND
        {
#if !defined(DACCESS_COMPILE)
            // non-optimized stack unwind schema, doesn't use StackwalkCache
            UINT_PTR curSP = (UINT_PTR)GetRegdisplaySP(m_crawl.pRD);
            UINT_PTR curIP = (UINT_PTR)GetControlPC(m_crawl.pRD);
#endif // !DACCESS_COMPILE

            bool fInsertCacheEntry = m_crawl.stackWalkCache.Enabled() &&
                                     (m_flags & LIGHTUNWIND) &&
                                     (m_pCachedGSCookie == NULL);

            // Is this a dynamic method. Dynamic methods can be GC collected and so IP to method mapping
            // is not persistent. Therefore do not cache information for this frame.
            BOOL isCollectableMethod = ExecutionManager::IsCollectibleMethod(m_crawl.GetMethodToken());
            if(isCollectableMethod)
                fInsertCacheEntry = FALSE;

            StackwalkCacheUnwindInfo unwindInfo;

            if (!m_crawl.GetCodeManager()->UnwindStackFrame(
                                m_crawl.pRD,
                                &m_cachedCodeInfo,
                                m_codeManFlags
                                    | m_crawl.GetCodeManagerFlags()
                                    | ((m_flags & PROFILER_DO_STACK_SNAPSHOT) ?  SpeculativeStackwalk : 0),
                                                      &m_crawl.codeManState,
                                (fInsertCacheEntry ? &unwindInfo : NULL)))
            {
                LOG((LF_CORPROF, LL_INFO100, "**PROF: m_crawl.GetCodeManager()->UnwindStackFrame failure leads to SWA_FAILED.\n"));
                retVal = SWA_FAILED;
                goto Cleanup;
            }

#if !defined(DACCESS_COMPILE)
            // store into hashtable if fits, otherwise just use old schema
            if (fInsertCacheEntry)
            {
                //
                //  information we add to cache, consists of two parts:
                //  1. SPOffset - locals, etc. of current method, adding which to current ESP we get to retAddr ptr
                //  2. argSize - size of pushed function arguments, the rest we need to add to get new ESP
                //  we have to store two parts of ESP delta, since we need to update pPC also, and so require retAddr ptr
                //
                //  newSP = oldSP + SPOffset + sizeof(PTR) + argSize
                //
                UINT_PTR SPOffset = (UINT_PTR)GetRegdisplayStackMark(m_crawl.pRD) - curSP;
                UINT_PTR argSize  = (UINT_PTR)GetRegdisplaySP(m_crawl.pRD) - curSP - SPOffset - sizeof(void*);

                StackwalkCacheEntry cacheEntry = {0};
                if (cacheEntry.Init(
                            curIP,
                            SPOffset,
                            &unwindInfo,
                            argSize))
                {
                    m_crawl.stackWalkCache.Insert(&cacheEntry);
                }
            }
#endif // !DACCESS_COMPILE
        }

#define FAIL_IF_SPECULATIVE_WALK(condition)             \
        if (m_flags & PROFILER_DO_STACK_SNAPSHOT)       \
        {                                               \
            if (!(condition))                           \
            {                                           \
                LOG((LF_CORPROF, LL_INFO100, "**PROF: " #condition " failure leads to SWA_FAILED.\n")); \
                retVal = SWA_FAILED;                    \
                goto Cleanup;                           \
            }                                           \
        }                                               \
        else                                            \
        {                                               \
            _ASSERTE(condition);                        \
        }

        // When the stackwalk is seeded with a profiler context, the context
        // might be bogus.  Check the stack pointer and the program counter for validity here.
        // (Note that these checks are not strictly necessary since we are able
        // to recover from AVs during profiler stackwalk.)

        PTR_VOID newSP = PTR_VOID((TADDR)GetRegdisplaySP(m_crawl.pRD));
#ifndef NO_FIXED_STACK_LIMIT
        FAIL_IF_SPECULATIVE_WALK(m_crawl.pThread->IsExecutingOnAltStack() || newSP >= m_crawl.pThread->GetCachedStackLimit());
#endif // !NO_FIXED_STACK_LIMIT
        FAIL_IF_SPECULATIVE_WALK(m_crawl.pThread->IsExecutingOnAltStack() || newSP < m_crawl.pThread->GetCachedStackBase());

#undef FAIL_IF_SPECULATIVE_WALK

        LOG((LF_GCROOTS, LL_EVERYTHING, "STACKWALK: [%03x] finished unwind for '%s', SP:" FMT_ADDR \
             ", IP:" FMT_ADDR "\n",
             m_uFramesProcessed,
             m_crawl.pFunc->m_pszDebugMethodName,
             DBG_ADDR(GetRegdisplaySP(m_crawl.pRD)),
             DBG_ADDR(GetControlPC(m_crawl.pRD))));

        m_crawl.isFirst       = FALSE;
        m_crawl.isInterrupted = FALSE;
        m_crawl.hasFaulted    = FALSE;
        m_crawl.isIPadjusted  = FALSE;

#ifndef PROCESS_EXPLICIT_FRAME_BEFORE_MANAGED_FRAME
        // remember, x86 handles the managed stack frame before the explicit frames contained in it
        if (CheckForSkippedFrames())
        {
            _ASSERTE(m_frameState == SFITER_SKIPPED_FRAME_FUNCTION);
            goto Cleanup;
        }
#endif // !PROCESS_EXPLICIT_FRAME_BEFORE_MANAGED_FRAME

        PostProcessingForManagedFrames();
        if (m_frameState == SFITER_NATIVE_MARKER_FRAME)
        {
            goto Cleanup;
        }
    }
    else if (m_frameState == SFITER_FRAME_FUNCTION)
    {
        Frame* pInlinedFrame = NULL;

        if (InlinedCallFrame::FrameHasActiveCall(m_crawl.pFrame))
        {
            pInlinedFrame = m_crawl.pFrame;
        }

        unsigned uFrameAttribs = m_crawl.pFrame->GetFrameAttribs();

        // Special resumable frames make believe they are on top of the stack.
        m_crawl.isFirst = (uFrameAttribs & Frame::FRAME_ATTR_RESUMABLE) != 0;

        // If the frame is a subclass of ExceptionFrame,
        // then we know this is interrupted.
        m_crawl.isInterrupted = (uFrameAttribs & Frame::FRAME_ATTR_EXCEPTION) != 0;

        if (m_crawl.isInterrupted)
        {
            m_crawl.hasFaulted = (uFrameAttribs & Frame::FRAME_ATTR_FAULTED) != 0;
            m_crawl.isIPadjusted = (uFrameAttribs & Frame::FRAME_ATTR_OUT_OF_LINE) != 0;
            _ASSERTE(!m_crawl.hasFaulted || !m_crawl.isIPadjusted); // both cant be set together
        }

        PCODE adr = m_crawl.pFrame->GetReturnAddress();
        _ASSERTE(adr != (PCODE)POISONC);

        _ASSERTE(!pInlinedFrame || adr);

        if (adr)
        {
            ProcessIp(adr);

            _ASSERTE(m_crawl.GetCodeInfo()->IsValid() || !pInlinedFrame);

            if (m_crawl.isFrameless)
            {
                m_crawl.pFrame->UpdateRegDisplay(m_crawl.pRD);

#if defined(RECORD_RESUMABLE_FRAME_SP)
                CONSISTENCY_CHECK(NULL == m_pvResumableFrameTargetSP);

                if (m_crawl.isFirst)
                {
                    if (m_flags & THREAD_IS_SUSPENDED)
                    {
                        _ASSERTE(m_crawl.isProfilerDoStackSnapshot);

                        // abort the stackwalk, we can't proceed without risking deadlock
                        retVal = SWA_FAILED;
                        goto Cleanup;
                    }

                    // we are about to unwind, which may take a lock, so the thread
                    // better not be suspended.
                    CONSISTENCY_CHECK(!(m_flags & THREAD_IS_SUSPENDED));

#if !defined(DACCESS_COMPILE)
                    if (m_crawl.stackWalkCache.Enabled() && (m_flags & LIGHTUNWIND))
                    {
                        m_crawl.isCachedMethod = m_crawl.stackWalkCache.Lookup((UINT_PTR)adr);
                    }
#endif // DACCESS_COMPILE

                    EECodeManager::EnsureCallerContextIsValid(m_crawl.pRD, m_crawl.GetStackwalkCacheEntry());
                    m_pvResumableFrameTargetSP = (LPVOID)GetSP(m_crawl.pRD->pCallerContext);
                }
#endif // RECORD_RESUMABLE_FRAME_SP


#if defined(_DEBUG) && !defined(DACCESS_COMPILE) && !defined(FEATURE_EH_FUNCLETS)
                // We are transitioning from unmanaged code to managed code... lets do some validation of our
                // EH mechanism on platforms that we can.
                VerifyValidTransitionFromManagedCode(m_crawl.pThread, &m_crawl);
#endif // _DEBUG && !DACCESS_COMPILE &&  !FEATURE_EH_FUNCLETS
            }
        }

        if (!pInlinedFrame)
        {
#if defined(STACKWALKER_MAY_POP_FRAMES)
            if (m_flags & POPFRAMES)
            {
                // If we got here, the current frame chose not to handle the
                // exception. Give it a chance to do any termination work
                // before we pop it off.

                CLEAR_THREAD_TYPE_STACKWALKER();
                END_FORBID_TYPELOAD();

                m_crawl.pFrame->ExceptionUnwind();

                BEGIN_FORBID_TYPELOAD();
                SET_THREAD_TYPE_STACKWALKER(m_pThread);

                // Pop off this frame and go on to the next one.
                m_crawl.GotoNextFrame();

                // When StackWalkFramesEx is originally called, we ensure
                // that if POPFRAMES is set that the thread is in COOP mode
                // and that running thread is walking itself. Thus, this
                // COOP assertion is safe.
                BEGIN_GCX_ASSERT_COOP;
                m_crawl.pThread->SetFrame(m_crawl.pFrame);
                END_GCX_ASSERT_COOP;
            }
            else
#endif // STACKWALKER_MAY_POP_FRAMES
            {
                // Go to the next frame.
                m_crawl.GotoNextFrame();
            }
        }
    }
#if defined(ELIMINATE_FEF)
    else if (m_frameState == SFITER_NO_FRAME_TRANSITION)
    {
        PostProcessingForNoFrameTransition();
    }
#endif  // ELIMINATE_FEF
    else if (m_frameState == SFITER_NATIVE_MARKER_FRAME)
    {
        m_crawl.isNativeMarker = false;
    }
    else if (m_frameState == SFITER_INITIAL_NATIVE_CONTEXT)
    {
        // nothing to do here
    }
    else
    {
        _ASSERTE(m_frameState == SFITER_UNINITIALIZED);
        _ASSERTE(!"StackFrameIterator::NextRaw() called when the iterator is uninitialized.  \
                  Should never get here.");
        retVal = SWA_FAILED;
        goto Cleanup;
    }

    ProcessCurrentFrame();

Cleanup:
#if defined(_DEBUG)
    if (retVal == SWA_FAILED)
    {
        LOG((LF_GCROOTS, LL_INFO10000, "STACKWALK: SWA_FAILED: couldn't start stackwalk\n"));
    }
#endif // _DEBUG

    return retVal;
} // StackFrameIterator::NextRaw()

//---------------------------------------------------------------------------------------
//
// Synchronizing the REGDISPLAY to the current CONTEXT stored in the REGDISPLAY.
// This is an nop on non-WIN64 platforms.
//

void StackFrameIterator::UpdateRegDisp(void)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    BIT64_ONLY(SyncRegDisplayToCurrentContext(m_crawl.pRD));
} // StackFrameIterator::UpdateRegDisp()

//---------------------------------------------------------------------------------------
//
// Check whether the specified Ip is in managed code and update the CrawlFrame accordingly.
// This function updates isFrameless, JitManagerInstance.
//
// Arguments:
//    Ip - IP to be processed
//

void StackFrameIterator::ProcessIp(PCODE Ip)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SUPPORTS_DAC;
    } CONTRACTL_END;

    // Re-initialize codeInfo with new IP
    m_crawl.codeInfo.Init(Ip, m_scanFlag);

    m_crawl.isFrameless = !!m_crawl.codeInfo.IsValid();
} // StackFrameIterator::ProcessIp()

//---------------------------------------------------------------------------------------
//
// Update the CrawlFrame to represent where we have stopped.  This is called after advancing
// to a new frame.
//
// Notes:
//    This function and everything it calls must not rely on m_frameState, which could have become invalid
//    when we advance the iterator before calling this function.
//

void StackFrameIterator::ProcessCurrentFrame(void)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    bool fDone = false;

    m_crawl.CheckGSCookies();

    // Since we have advanced the iterator, the frame state represents the previous frame state,
    // not the current one.  This is important to keep in mind.  Ideally we should just assert that
    // the frame state has been set to invalid upon entry to this function, but we need the previous frame
    // state to decide if we should stop at an native stack frame.

    // If we just do a simple check for native code here, we will loop forever.
    if (m_frameState == SFITER_UNINITIALIZED)
    {
        // "!IsFrameless()" normally implies that the CrawlFrame is at an explicit frame.  Here we are using it
        // to detect whether the CONTEXT is in managed code or not.  Ideally we should have a enum on the
        // CrawlFrame to indicate the various types of "frames" the CrawlFrame can stop at.
        //
        // If the CONTEXT is in native code and the StackFrameIterator is uninitialized, then it must be
        // an initial native CONTEXT passed to the StackFrameIterator when it is created or
        // when ResetRegDisp() is called.
        if (!m_crawl.IsFrameless())
        {
            m_frameState = SFITER_INITIAL_NATIVE_CONTEXT;
            fDone = true;
        }
    }
    else
    {
        // Clear the frame state.  It will be set before we return from this function.
        m_frameState = SFITER_UNINITIALIZED;
    }

    // Check for the case of an exception in managed code, and resync the stack walk
    //  from the exception context.
#if defined(ELIMINATE_FEF)
    if (!fDone && !m_crawl.IsFrameless() && m_exInfoWalk.GetExInfo())
    {
        // We are currently walking ("lost") in unmanaged code.  We can recover
        //  from a) the next Frame record, or b) an exception context.
        // Recover from the exception context if all of these are true:
        //  - it "returns" to managed code
        //  - if is lower (newer) than the next Frame record
        //  - the stack walk has not already passed by it
        //
        // The ExInfo walker is initialized to be higher than the pStartFrame, and
        //  as we unwind managed (frameless) functions, we keep eliminating any
        //  ExInfos that are passed in the stackwalk.
        //
        // So, here we need to find the next ExInfo that "returns" to managed code,
        //  and then choose the lower of that ExInfo and the next Frame.
        m_exInfoWalk.WalkToManaged();
        TADDR pContextSP = m_exInfoWalk.GetSPFromContext();

        //@todo: check the exception code for a fault?

        // If there was a pContext that is higher than the SP and starting frame...
        if (pContextSP)
        {
            PTR_CONTEXT pContext = m_exInfoWalk.GetContext();

            LOG((LF_EH, LL_INFO10000, "STACKWALK: considering resync from pContext(%p), fault(%08X), sp(%p); \
                 pStartFrame(%p); cf.pFrame(%p), cf.SP(%p)\n",
                 pContext, m_exInfoWalk.GetFault(), pContextSP,
                 m_pStartFrame, dac_cast<TADDR>(m_crawl.pFrame), GetRegdisplaySP(m_crawl.pRD)));

            // If the pContext is lower (newer) than the CrawlFrame's Frame*, try to use
            //  the pContext.
            // There are still a few cases in which a FaultingExceptionFrame is linked in.  If
            //  the next frame is one of them, we don't want to override it.  THIS IS PROBABLY BAD!!!
            if ( (pContextSP < dac_cast<TADDR>(m_crawl.pFrame)) &&
                 ((m_crawl.GetFrame() == FRAME_TOP) ||
                  (m_crawl.GetFrame()->GetVTablePtr() != FaultingExceptionFrame::GetMethodFrameVPtr() ) ) )
            {
                //
                // If the REGDISPLAY represents an unmanaged stack frame above (closer to the leaf than) an
                // ExInfo without any intervening managed stack frame, then we will stop at the no-frame
                // transition protected by the ExInfo.  However, if the unmanaged stack frame is the one
                // immediately above the faulting managed stack frame, we want to continue the stackwalk
                // with the faulting managed stack frame.  So we do not stop in this case.
                //
                // However, just comparing EBP is not enough.  The OS exception handler
                // (KiUserExceptionDispatcher()) does not use an EBP frame.  So if we just compare the EBP
                // we will think that the OS excpetion handler is the one we want to claim.  Instead,
                // we should also check the current IP, which because of the way unwinding work and
                // how the OS exception handler behaves is actually going to be the stack limit of the
                // current thread.  This is of course a workaround and is dependent on the OS behaviour.
                //

                PCODE curPC = GetControlPC(m_crawl.pRD);
                if ((m_crawl.pRD->pEbp != NULL )                                               &&
                    (m_exInfoWalk.GetEBPFromContext() == GetRegdisplayFP(m_crawl.pRD)) &&
                    ((m_crawl.pThread->GetCachedStackLimit() <= PTR_VOID(curPC)) &&
                       (PTR_VOID(curPC) < m_crawl.pThread->GetCachedStackBase())))
                {
                    // restore the CONTEXT saved by the ExInfo and continue on to the faulting
                    // managed stack frame
                    PostProcessingForNoFrameTransition();
                }
                else
                {
                    // we stop stop at the no-frame transition
                    m_frameState = SFITER_NO_FRAME_TRANSITION;
                    m_crawl.isNoFrameTransition = true;
                    m_crawl.taNoFrameTransitionMarker = pContextSP;
                    fDone = true;
                }
            }
        }
    }
#endif // defined(ELIMINATE_FEF)

    if (!fDone)
    {
        // returns SWA_DONE if there is no more frames to walk
        if (!IsValid())
        {
            LOG((LF_GCROOTS, LL_INFO10000, "STACKWALK: SWA_DONE: reached the end of the stack\n"));
            m_frameState = SFITER_DONE;
            return;
        }

        m_crawl.codeManState.dwIsSet = 0;
#if defined(_DEBUG)
        memset((void *)m_crawl.codeManState.stateBuf, 0xCD,
               sizeof(m_crawl.codeManState.stateBuf));
#endif // _DEBUG

        if (m_crawl.isFrameless)
        {
            //------------------------------------------------------------------------
            // This must be a JITed/managed native method. There is no explicit frame.
            //------------------------------------------------------------------------

#if !defined(DACCESS_COMPILE)
            m_crawl.isCachedMethod = FALSE;
            if (m_crawl.stackWalkCache.Enabled() && (m_flags & LIGHTUNWIND))
            {
                m_crawl.isCachedMethod = m_crawl.stackWalkCache.Lookup((UINT_PTR)GetControlPC(m_crawl.pRD));
                _ASSERTE (m_crawl.isCachedMethod != m_crawl.stackWalkCache.IsEmpty());
            }
#endif // DACCESS_COMPILE


#if defined(FEATURE_EH_FUNCLETS)
            m_crawl.isFilterFuncletCached = false;
#endif // FEATURE_EH_FUNCLETS

            m_crawl.pFunc = m_crawl.codeInfo.GetMethodDesc();

            // Cache values which may be updated by CheckForSkippedFrames()
            m_cachedCodeInfo = m_crawl.codeInfo;

#ifdef PROCESS_EXPLICIT_FRAME_BEFORE_MANAGED_FRAME
            // On non-X86, we want to process the skipped explicit frames before the managed stack frame
            // containing them.
            if (CheckForSkippedFrames())
            {
                _ASSERTE(m_frameState == SFITER_SKIPPED_FRAME_FUNCTION);
            }
            else
#endif // PROCESS_EXPLICIT_FRAME_BEFORE_MANAGED_FRAME
            {
                PreProcessingForManagedFrames();
                _ASSERTE(m_frameState == SFITER_FRAMELESS_METHOD);
            }
        }
        else
        {
            INDEBUG(m_crawl.pThread->DebugLogStackWalkInfo(&m_crawl, "CONSIDER", m_uFramesProcessed));

            _ASSERTE(m_crawl.pFrame != FRAME_TOP);

            m_crawl.pFunc = m_crawl.pFrame->GetFunction();

            m_frameState = SFITER_FRAME_FUNCTION;
        }
    }

    _ASSERTE(m_frameState != SFITER_UNINITIALIZED);
} // StackFrameIterator::ProcessCurrentFrame()

//---------------------------------------------------------------------------------------
//
// If an explicit frame is allocated in a managed stack frame (e.g. an inlined pinvoke call),
// we may have skipped an explicit frame.  This function checks for them.
//
// Return Value:
//    Returns true if there are skipped frames.
//
// Notes:
//    x86 wants to stop at the skipped stack frames after the containing managed stack frame, but
//    WIN64 wants to stop before.  I don't think x86 actually has any good reason for this, except
//    because it doesn't unwind one frame ahead of time like WIN64 does.  This means that we don't
//    have the caller SP on x86.
//

BOOL StackFrameIterator::CheckForSkippedFrames(void)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

    BOOL   fHandleSkippedFrames = FALSE;
    TADDR pvReferenceSP;

    // Can the caller handle skipped frames;
    fHandleSkippedFrames = (m_flags & HANDLESKIPPEDFRAMES);

#ifndef PROCESS_EXPLICIT_FRAME_BEFORE_MANAGED_FRAME
    pvReferenceSP = GetRegdisplaySP(m_crawl.pRD);
#else // !PROCESS_EXPLICIT_FRAME_BEFORE_MANAGED_FRAME
    // Order the Frames relative to the caller SP of the methods
    // this makes it so that any Frame that is in a managed call
    // frame will be reported before its containing method.

    // This should always succeed!  If it doesn't, it's a bug somewhere else!
    EECodeManager::EnsureCallerContextIsValid(m_crawl.pRD, m_crawl.GetStackwalkCacheEntry(), &m_cachedCodeInfo);
    pvReferenceSP = GetSP(m_crawl.pRD->pCallerContext);
#endif // PROCESS_EXPLICIT_FRAME_BEFORE_MANAGED_FRAME

    if ( !( (m_crawl.pFrame != FRAME_TOP) &&
            (dac_cast<TADDR>(m_crawl.pFrame) < pvReferenceSP) )
       )
    {
        return FALSE;
    }

    LOG((LF_GCROOTS, LL_EVERYTHING, "STACKWALK: CheckForSkippedFrames\n"));

    // We might have skipped past some Frames.
    // This happens with InlinedCallFrames.
    while ( (m_crawl.pFrame != FRAME_TOP) &&
            (dac_cast<TADDR>(m_crawl.pFrame) < pvReferenceSP)
          )
    {
        BOOL fReportInteropMD =
        // If we see InlinedCallFrame in certain IL stubs, we should report the MD that
        // was passed to the stub as its secret argument. This is the true interop MD.
        // Note that code:InlinedCallFrame.GetFunction may return NULL in this case because
        // the call is made using the CALLI instruction.
            m_crawl.pFrame != FRAME_TOP &&
            m_crawl.pFrame->GetVTablePtr() == InlinedCallFrame::GetMethodFrameVPtr() &&
            m_crawl.pFunc != NULL &&
            m_crawl.pFunc->IsILStub() &&
            m_crawl.pFunc->AsDynamicMethodDesc()->HasMDContextArg();

        if (fHandleSkippedFrames
#ifdef TARGET_X86
            || // On x86 we have already reported the InlinedCallFrame, don't report it again.
            (InlinedCallFrame::FrameHasActiveCall(m_crawl.pFrame) && !fReportInteropMD)
#endif // TARGET_X86
            )
        {
            m_crawl.GotoNextFrame();
#ifdef STACKWALKER_MAY_POP_FRAMES
            if (m_flags & POPFRAMES)
            {
                // When StackWalkFramesEx is originally called, we ensure
                // that if POPFRAMES is set that the thread is in COOP mode
                // and that running thread is walking itself. Thus, this
                // COOP assertion is safe.
                BEGIN_GCX_ASSERT_COOP;
                m_crawl.pThread->SetFrame(m_crawl.pFrame);
                END_GCX_ASSERT_COOP;
            }
#endif // STACKWALKER_MAY_POP_FRAMES
        }
        else
        {
            m_crawl.isFrameless     = false;

            if (fReportInteropMD)
            {
                m_crawl.pFunc = ((PTR_InlinedCallFrame)m_crawl.pFrame)->GetActualInteropMethodDesc();
                _ASSERTE(m_crawl.pFunc != NULL);
                _ASSERTE(m_crawl.pFunc->SanityCheck());
            }
            else
            {
                m_crawl.pFunc = m_crawl.pFrame->GetFunction();
            }

            INDEBUG(m_crawl.pThread->DebugLogStackWalkInfo(&m_crawl, "CONSIDER", m_uFramesProcessed));

            m_frameState = SFITER_SKIPPED_FRAME_FUNCTION;
            return TRUE;
        }
    }

    return FALSE;
} // StackFrameIterator::CheckForSkippedFrames()

//---------------------------------------------------------------------------------------
//
// Perform the necessary tasks before stopping at a managed stack frame.  This is mostly validation work.
//

void StackFrameIterator::PreProcessingForManagedFrames(void)
{
    WRAPPER_NO_CONTRACT;
    SUPPORTS_DAC;

#if defined(RECORD_RESUMABLE_FRAME_SP)
    if (m_pvResumableFrameTargetSP)
    {
        // We expect that if we saw a resumable frame, the next managed
        // IP that we see will be the one the resumable frame took us to.

        // However, because we might visit intervening explicit Frames
        // that will clear the .isFirst flag, we need to set it back here.

        CONSISTENCY_CHECK(m_crawl.pRD->IsCallerContextValid);
        CONSISTENCY_CHECK((LPVOID)GetSP(m_crawl.pRD->pCallerContext) == m_pvResumableFrameTargetSP);
        m_pvResumableFrameTargetSP = NULL;
        m_crawl.isFirst = true;
    }
#endif // RECORD_RESUMABLE_FRAME_SP

#if !defined(DACCESS_COMPILE)
    m_pCachedGSCookie = (GSCookie*)m_crawl.GetCodeManager()->GetGSCookieAddr(
                                                        m_crawl.pRD,
                                                        &m_crawl.codeInfo,
                                                        &m_crawl.codeManState);
#endif // !DACCESS_COMPILE

    if (!(m_flags & SKIP_GSCOOKIE_CHECK) && m_pCachedGSCookie)
    {
        m_crawl.SetCurGSCookie(m_pCachedGSCookie);
    }

    INDEBUG(m_crawl.pThread->DebugLogStackWalkInfo(&m_crawl, "CONSIDER", m_uFramesProcessed));

#if defined(_DEBUG) && !defined(FEATURE_EH_FUNCLETS) && !defined(DACCESS_COMPILE)
    //
    // VM is responsible for synchronization on non-funclet EH model.
    //
    // m_crawl.GetThisPointer() requires full unwind
    // In GC's relocate phase, objects is not verifiable
    if ( !(m_flags & (LIGHTUNWIND | QUICKUNWIND | ALLOW_INVALID_OBJECTS)) &&
         m_crawl.pFunc->IsSynchronized() &&
         !m_crawl.pFunc->IsStatic()      &&
         m_crawl.GetCodeManager()->IsInSynchronizedRegion(m_crawl.GetRelOffset(),
                                                         m_crawl.GetGCInfoToken(),
                                                         m_crawl.GetCodeManagerFlags()))
    {
        BEGIN_GCX_ASSERT_COOP;

        OBJECTREF obj = m_crawl.GetThisPointer();

        _ASSERTE(obj != NULL);
        VALIDATEOBJECTREF(obj);

        DWORD threadId = 0;
        DWORD acquisitionCount = 0;
        _ASSERTE(obj->GetThreadOwningMonitorLock(&threadId, &acquisitionCount) &&
                 (threadId == m_crawl.pThread->GetThreadId()));

        END_GCX_ASSERT_COOP;
    }
#endif // _DEBUG && !FEATURE_EH_FUNCLETS && !DACCESS_COMPILE

    m_frameState = SFITER_FRAMELESS_METHOD;
} // StackFrameIterator::PreProcessingForManagedFrames()

//---------------------------------------------------------------------------------------
//
// Perform the necessary tasks after stopping at a managed stack frame and unwinding to its caller.
// This includes advancing the ExInfo and checking whether the new IP is managed.
//

void StackFrameIterator::PostProcessingForManagedFrames(void)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;


#if defined(ELIMINATE_FEF)
    // As with frames, we may have unwound past a ExInfo.pContext.  This
    //  can happen when unwinding from a handler that rethrew the exception.
    //  Skip any ExInfo.pContext records that may no longer be valid.
    // If Frames would be unlinked from the Frame chain, also reset the UseExInfoForStackwalk bit
    //  on the ExInfo.
    m_exInfoWalk.WalkToPosition(GetRegdisplaySP(m_crawl.pRD), (m_flags & POPFRAMES));
#endif // ELIMINATE_FEF

#ifdef TARGET_X86
    hdrInfo gcHdrInfo;
    DecodeGCHdrInfo(m_crawl.codeInfo.GetGCInfoToken(), 0, &gcHdrInfo);
    bool hasReversePInvoke = gcHdrInfo.revPInvokeOffset != INVALID_REV_PINVOKE_OFFSET;
#endif // TARGET_X86

    ProcessIp(GetControlPC(m_crawl.pRD));

    // if we have unwound to a native stack frame, stop and set the frame state accordingly
    if (!m_crawl.isFrameless)
    {
        m_frameState = SFITER_NATIVE_MARKER_FRAME;
        m_crawl.isNativeMarker = true;
    }
#ifdef TARGET_X86
    else if (hasReversePInvoke)
    {
        // The managed frame we've unwound from had reverse PInvoke frame. Since we are on a frameless
        // frame, that means that the method was called from managed code without any native frames in between. 
        // On x86, the InlinedCallFrame of the pinvoke would get skipped as we've just unwound to the pinvoke IL stub and
        // for this architecture, the inlined call frames are supposed to be processed before the managed frame they are stored in.
        // So we force the stack frame iterator to process the InlinedCallFrame before the IL stub.
        _ASSERTE(InlinedCallFrame::FrameHasActiveCall(m_crawl.pFrame));
        m_crawl.isFrameless = false;
    }
#endif    
} // StackFrameIterator::PostProcessingForManagedFrames()

//---------------------------------------------------------------------------------------
//
// Perform the necessary tasks after stopping at a no-frame transition.  This includes loading
// the CONTEXT stored in the ExInfo and updating the REGDISPLAY to the faulting managed stack frame.
//

void StackFrameIterator::PostProcessingForNoFrameTransition()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SUPPORTS_DAC;
    }
    CONTRACTL_END;

#if defined(ELIMINATE_FEF)
    PTR_CONTEXT pContext = m_exInfoWalk.GetContext();

    // Get the JitManager for the managed address.
    m_crawl.codeInfo.Init(GetIP(pContext), m_scanFlag);
    _ASSERTE(m_crawl.codeInfo.IsValid());

    STRESS_LOG4(LF_EH, LL_INFO100, "STACKWALK: resync from pContext(%p); pStartFrame(%p), \
                cf.pFrame(%p), cf.SP(%p)\n",
                dac_cast<TADDR>(pContext), dac_cast<TADDR>(m_pStartFrame), dac_cast<TADDR>(m_crawl.pFrame),
                GetRegdisplaySP(m_crawl.pRD));

    // Update the RegDisplay from the context info.
    FillRegDisplay(m_crawl.pRD, pContext);

    // Now we know where we are, and it's "frameless", aka managed.
    m_crawl.isFrameless = true;

    // Flags the same as from a FaultingExceptionFrame.
    m_crawl.isInterrupted = 1;
    m_crawl.hasFaulted = 1;
    m_crawl.isIPadjusted = 0;

#if defined(STACKWALKER_MAY_POP_FRAMES)
    // If Frames would be unlinked from the Frame chain, also reset the UseExInfoForStackwalk bit
    //  on the ExInfo.
    if (m_flags & POPFRAMES)
    {
        m_exInfoWalk.GetExInfo()->m_ExceptionFlags.ResetUseExInfoForStackwalk();
    }
#endif // STACKWALKER_MAY_POP_FRAMES

    // Done with this ExInfo.
    m_exInfoWalk.WalkOne();

    m_crawl.isNoFrameTransition = false;
    m_crawl.taNoFrameTransitionMarker = NULL;
#endif // ELIMINATE_FEF
} // StackFrameIterator::PostProcessingForNoFrameTransition()


#if defined(TARGET_AMD64) && !defined(DACCESS_COMPILE)
static CrstStatic g_StackwalkCacheLock;                // Global StackwalkCache lock; only used on AMD64
EXTERN_C void moveOWord(LPVOID src, LPVOID target);
#endif // TARGET_AMD64

/*
    copies 64-bit *src to *target, atomically accessing the data
    requires 64-bit alignment for atomic load/store
*/
inline static void atomicMoveCacheEntry(UINT64* src, UINT64* target)
{
    LIMITED_METHOD_CONTRACT;

#ifdef TARGET_X86
    // the most negative value is used a sort of integer infinity
    // value, so it have to be avoided
    _ASSERTE(*src != 0x8000000000000000);
    __asm
    {
        mov eax, src
        fild qword ptr [eax]
        mov eax, target
        fistp qword ptr [eax]
    }
#elif defined(TARGET_AMD64) && !defined(DACCESS_COMPILE)
    // On AMD64 there's no way to move 16 bytes atomically, so we need to take a lock before calling moveOWord().
    CrstHolder ch(&g_StackwalkCacheLock);
    moveOWord(src, target);
#endif
}

/*
============================================================
Here is an implementation of StackwalkCache class, used to optimize performance
of stack walking. Currently each CrawlFrame has a StackwalkCache member, which implements
functionality for caching already walked methods (see Thread::StackWalkFramesEx).
See class and corresponding types declaration at stackwalktypes.h
We do use global cache g_StackwalkCache[] with InterlockCompareExchange, fitting
each cache entry into 8 bytes.
============================================================
*/

#ifndef DACCESS_COMPILE
#define LOG_NUM_OF_CACHE_ENTRIES 10
#else
// Stack walk cache is disabled in DAC - save space
#define LOG_NUM_OF_CACHE_ENTRIES 0
#endif
#define NUM_OF_CACHE_ENTRIES (1 << LOG_NUM_OF_CACHE_ENTRIES)

static StackwalkCacheEntry g_StackwalkCache[NUM_OF_CACHE_ENTRIES] = {}; // Global StackwalkCache

#ifdef DACCESS_COMPILE
const BOOL StackwalkCache::s_Enabled = FALSE;
#else
BOOL StackwalkCache::s_Enabled = FALSE;

/*
    StackwalkCache class constructor.
    Set "enable/disable optimization" flag according to registry key.
*/
StackwalkCache::StackwalkCache()
{
    CONTRACTL {
       NOTHROW;
       GC_NOTRIGGER;
    } CONTRACTL_END;

    ClearEntry();

    static BOOL stackwalkCacheEnableChecked = FALSE;
    if (!stackwalkCacheEnableChecked)
    {
        // We can enter this block on multiple threads because of racing.
        // However, that is OK since this operation is idempotent

        s_Enabled = ((g_pConfig->DisableStackwalkCache() == 0) &&
                    // disable cache if for some reason it is not aligned
                    IS_ALIGNED((void*)&g_StackwalkCache[0], STACKWALK_CACHE_ENTRY_ALIGN_BOUNDARY));
        stackwalkCacheEnableChecked = TRUE;
    }
}

#endif // #ifndef DACCESS_COMPILE

// static
void StackwalkCache::Init()
{
#if defined(TARGET_AMD64) && !defined(DACCESS_COMPILE)
    g_StackwalkCacheLock.Init(CrstSecurityStackwalkCache, CRST_UNSAFE_ANYMODE);
#endif // TARGET_AMD64
}

/*
    Returns efficient hash table key based on provided IP.
    CPU architecture dependent.
*/
inline unsigned StackwalkCache::GetKey(UINT_PTR IP)
{
    LIMITED_METHOD_CONTRACT;
    return (unsigned)(((IP >> LOG_NUM_OF_CACHE_ENTRIES) ^ IP) & (NUM_OF_CACHE_ENTRIES-1));
}

/*
    Looks into cache and returns StackwalkCache entry, if current IP is cached.
    JIT team guarantees the same ESP offset for the same IPs for different call chains.
*/
BOOL StackwalkCache::Lookup(UINT_PTR IP)
{
    CONTRACTL {
       NOTHROW;
       GC_NOTRIGGER;
    } CONTRACTL_END;

#if defined(TARGET_X86) || defined(TARGET_AMD64)
    _ASSERTE(Enabled());
    _ASSERTE(IP);

    unsigned hkey = GetKey(IP);
    _ASSERTE(IS_ALIGNED((void*)&g_StackwalkCache[hkey], STACKWALK_CACHE_ENTRY_ALIGN_BOUNDARY));
    // Don't care about m_CacheEntry access atomicity, since it's private to this
    // stackwalk/thread
    atomicMoveCacheEntry((UINT64*)&g_StackwalkCache[hkey], (UINT64*)&m_CacheEntry);

#ifdef _DEBUG
    if (IP != m_CacheEntry.IP)
    {
        ClearEntry();
    }
#endif

    return (IP == m_CacheEntry.IP);
#else // TARGET_X86
    return FALSE;
#endif // TARGET_X86
}

/*
    Caches data provided for current IP.
*/
void StackwalkCache::Insert(StackwalkCacheEntry *pCacheEntry)
{
    CONTRACTL {
       NOTHROW;
       GC_NOTRIGGER;
    } CONTRACTL_END;

    _ASSERTE(Enabled());
    _ASSERTE(pCacheEntry);

    unsigned hkey = GetKey(pCacheEntry->IP);
    _ASSERTE(IS_ALIGNED((void*)&g_StackwalkCache[hkey], STACKWALK_CACHE_ENTRY_ALIGN_BOUNDARY));
    atomicMoveCacheEntry((UINT64*)pCacheEntry, (UINT64*)&g_StackwalkCache[hkey]);
}

// static
void StackwalkCache::Invalidate(LoaderAllocator * pLoaderAllocator)
{
    CONTRACTL {
       NOTHROW;
       GC_NOTRIGGER;
    } CONTRACTL_END;

    if (!s_Enabled)
        return;

    /* Note that we could just flush the entries corresponding to
    pDomain if we wanted to get fancy. To keep things simple for now,
    we just invalidate everything
    */

    ZeroMemory(PVOID(&g_StackwalkCache), sizeof(g_StackwalkCache));
}

//----------------------------------------------------------------------------
//
// SetUpRegdisplayForStackWalk - set up Regdisplay for a stack walk
//
// Arguments:
//    pThread - pointer to the managed thread to be crawled
//    pContext - pointer to the context
//    pRegdisplay - pointer to the REGDISPLAY to be filled
//
// Return Value:
//    None
//
//----------------------------------------------------------------------------
void SetUpRegdisplayForStackWalk(Thread * pThread, T_CONTEXT * pContext, REGDISPLAY * pRegdisplay)
{
    CONTRACTL {
       NOTHROW;
       GC_NOTRIGGER;
       SUPPORTS_DAC;
    } CONTRACTL_END;

    // @dbgtodo  filter CONTEXT- The filter CONTEXT will be removed in V3.0.
    T_CONTEXT * pFilterContext = pThread->GetFilterContext();
    _ASSERTE(!(pFilterContext && ISREDIRECTEDTHREAD(pThread)));

    if (pFilterContext != NULL)
    {
        FillRegDisplay(pRegdisplay, pFilterContext);
    }
    else
    {
        ZeroMemory(pContext, sizeof(*pContext));
        FillRegDisplay(pRegdisplay, pContext);

        if (ISREDIRECTEDTHREAD(pThread))
        {
            pThread->GetFrame()->UpdateRegDisplay(pRegdisplay);
        }
    }
}
