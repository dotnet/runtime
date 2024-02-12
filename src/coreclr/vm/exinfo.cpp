// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//

#include "common.h"
#include "exinfo.h"
#include "dbginterface.h"

#ifdef FEATURE_EH_FUNCLETS
#include "eetoprofinterfacewrapper.inl"
#include "eedbginterfaceimpl.inl"
#endif

#ifndef FEATURE_EH_FUNCLETS
#ifndef DACCESS_COMPILE
//
// Destroy the handle within an ExInfo. This respects the fact that we can have preallocated global handles living
// in ExInfo's.
//
void ExInfo::DestroyExceptionHandle(void)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Never, ever destroy a preallocated exception handle.
    if ((m_hThrowable != NULL) && !CLRException::IsPreallocatedExceptionHandle(m_hThrowable))
    {
        DestroyHandle(m_hThrowable);
    }

    m_hThrowable = NULL;
}

//
// CopyAndClearSource copies the contents of the given ExInfo into the current ExInfo, then re-initializes the
// given ExInfo.
//
void ExInfo::CopyAndClearSource(ExInfo *from)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        if (GetThreadNULLOk() != NULL) MODE_COOPERATIVE; else MODE_ANY;
        FORBID_FAULT;
    }
    CONTRACTL_END;

#ifdef TARGET_X86
    LOG((LF_EH, LL_INFO100, "In ExInfo::CopyAndClearSource: m_dEsp=%08x, %08x <- [%08x], stackAddress = 0x%p <- 0x%p\n",
         from->m_dEsp, &(this->m_dEsp), &from->m_dEsp, this->m_StackAddress, from->m_StackAddress));
#endif // TARGET_X86

    // If we have a handle to an exception object in this ExInfo already, then go ahead and destroy it before we
    // loose it.
    DestroyExceptionHandle();

    // The stack address is handled differently. Save the original value.
    void* stackAddress = this->m_StackAddress;

    // Blast the entire record. Note: we're copying the handle from the source ExInfo to this object. That's okay,
    // since we're going to clear out the source ExInfo right below this.
    memcpy(this, from, sizeof(ExInfo));

    // Preserve the stack address. It should never change.
    m_StackAddress = stackAddress;

    // This ExInfo just took ownership of the handle to the exception object, so clear out that handle in the
    // source ExInfo.
    from->m_hThrowable = NULL;

    // Finally, initialize the source ExInfo.
    from->Init();

#ifndef TARGET_UNIX
    // Clear the Watson Bucketing information as well since they
    // have been transferred over by the "memcpy" above.
    from->GetWatsonBucketTracker()->Init();
#endif // TARGET_UNIX
}

void ExInfo::Init()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        FORBID_FAULT;
    }
    CONTRACTL_END;

    m_ExceptionFlags.Init();
    m_StackTraceInfo.Init();
    m_DebuggerExState.Init();

    m_pSearchBoundary = NULL;
	STRESS_LOG3(LF_EH, LL_INFO10000, "ExInfo::Init: setting ExInfo:0x%p m_pBottomMostHandler from 0x%p to 0x%p\n",
		this, m_pBottomMostHandler, NULL);
    m_pBottomMostHandler = NULL;
    m_pPrevNestedInfo = NULL;
    m_ExceptionCode = 0xcccccccc;
    m_pExceptionRecord = NULL;
    m_pContext = NULL;
    m_pShadowSP = NULL;
    m_StackAddress = this;
    DestroyExceptionHandle();
    m_hThrowable = NULL;
    m_ExceptionCode = 0;

    // By default, mark the tracker as not having delivered the first
    // chance exception notification
    m_fDeliveredFirstChanceNotification = FALSE;

    m_pTopMostHandlerDuringSO = NULL;

#if defined(TARGET_X86) && defined(DEBUGGING_SUPPORTED)
    m_InterceptionContext.Init();
    m_ValidInterceptionContext = FALSE;
#endif //TARGET_X86 && DEBUGGING_SUPPORTED
}

ExInfo::ExInfo()
{
    WRAPPER_NO_CONTRACT;

    m_hThrowable = NULL;
    Init();

#ifndef TARGET_UNIX
    // Init the WatsonBucketTracker
    m_WatsonBucketTracker.Init();
#endif // TARGET_UNIX
}

//*******************************************************************************
// When we hit an endcatch or an unwind and have nested handler info, either
//   1) we have contained a nested exception and will continue handling the original
//      exception
//  - or -
//   2) the nested exception was not contained, and was thrown beyond the original
//      bounds where the first exception occurred.
//
// The way we can tell this is from the stack pointer. The topmost nested handler is
//  installed at the point where the exception occurred.  For a nested exception to be
//  contained, it must be caught within the scope of any code that is called after
//  the nested handler is installed.  (remember: after is a lower stack address.)
//
// If it is caught by anything earlier on the stack, it was not contained, and we
//  unwind the nested handlers until we get to one that is higher on the stack
//  than the esp we will unwind to.
//
// If we still have a nested handler, then we have successfully handled a nested
//  exception and should restore the exception settings that we saved so that
//  processing of the original exception can continue.
// Otherwise the nested exception has gone beyond where the original exception was
//  thrown and therefore replaces the original exception.
//
// We will always remove the current exception info from the chain.
//
void ExInfo::UnwindExInfo(VOID* limit)
{
    CONTRACTL
    {
        NOTHROW; // This function does not throw.
        GC_NOTRIGGER;
        if (GetThreadNULLOk() != NULL) MODE_COOPERATIVE; else MODE_ANY;
    }
    CONTRACTL_END;

    // We must be in cooperative mode to do the chaining below
#ifdef DEBUGGING_SUPPORTED
    // The debugger thread will be using this, even though it has no
    // Thread object associated with it.
    _ASSERTE((GetThreadNULLOk() != NULL && GetThread()->PreemptiveGCDisabled()) ||
             ((g_pDebugInterface != NULL) && (g_pDebugInterface->GetRCThreadId() == GetCurrentThreadId())));
#endif // DEBUGGING_SUPPORTED

    LOG((LF_EH, LL_INFO100, "UnwindExInfo: unwind limit is 0x%p, prevNested is 0x%p\n", limit, m_pPrevNestedInfo));

    ExInfo *pPrevNestedInfo = m_pPrevNestedInfo;

    // At first glance, you would think that each nested exception has
    // been unwound by it's corresponding NestedExceptionHandler.  But that's
    // not necessarily the case.  The following assertion cannot be made here,
    // and the loop is necessary.
    //
    //_ASSERTE(pPrevNestedInfo == 0 || (DWORD)pPrevNestedInfo >= limit);
    //
    // Make sure we've unwound any nested exceptions that we're going to skip over.
    //
    while (pPrevNestedInfo && pPrevNestedInfo->m_StackAddress < limit)
    {
        STRESS_LOG1(LF_EH, LL_INFO100, "UnwindExInfo: PopExInfo(): popping nested ExInfo at 0x%p\n", pPrevNestedInfo->m_StackAddress);

        if (pPrevNestedInfo->m_hThrowable != NULL)
        {
            pPrevNestedInfo->DestroyExceptionHandle();
        }

        #ifndef TARGET_UNIX
        // Free the Watson bucket details when ExInfo
        // is being released
        pPrevNestedInfo->GetWatsonBucketTracker()->ClearWatsonBucketDetails();
        #endif // TARGET_UNIX

        pPrevNestedInfo->m_StackTraceInfo.FreeStackTrace();

        #ifdef DEBUGGING_SUPPORTED
        if (g_pDebugInterface != NULL)
        {
            g_pDebugInterface->DeleteInterceptContext(pPrevNestedInfo->m_DebuggerExState.GetDebuggerInterceptContext());
        }
        #endif

        // Get the next nested handler detail...
        ExInfo* pPrev = pPrevNestedInfo->m_pPrevNestedInfo;

        if (pPrevNestedInfo->IsHeapAllocated())
        {
            delete pPrevNestedInfo;
        }

        pPrevNestedInfo = pPrev;
    }

    // either clear the one we're about to copy over or the topmost one
    m_StackTraceInfo.FreeStackTrace();

    if (pPrevNestedInfo)
    {
        // found nested handler info that is above the esp restore point so successfully caught nested
        STRESS_LOG2(LF_EH, LL_INFO100, "UnwindExInfo: resetting nested ExInfo to 0x%p stackaddress:0x%p\n", pPrevNestedInfo, pPrevNestedInfo->m_StackAddress);

        // Remember if this ExInfo is heap allocated or not.
        BOOL isHeapAllocated = pPrevNestedInfo->IsHeapAllocated();

        // Copy pPrevNestedInfo to 'this', clearing pPrevNestedInfo in the process.
        CopyAndClearSource(pPrevNestedInfo);

        if (isHeapAllocated)
        {
            delete pPrevNestedInfo;         // Now delete the old record if we needed to.
        }
    }
    else
    {
        STRESS_LOG0(LF_EH, LL_INFO100, "UnwindExInfo: clearing topmost ExInfo\n");

        // We just do a basic Init of the current top ExInfo here.
        Init();

        #ifndef TARGET_UNIX
        // Init the Watson buckets as well
        GetWatsonBucketTracker()->ClearWatsonBucketDetails();
        #endif // TARGET_UNIX
    }
}
#endif // DACCESS_COMPILE


#ifdef DACCESS_COMPILE

void
ExInfo::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    // ExInfo is embedded so don't enum 'this'.
    OBJECTHANDLE_EnumMemoryRegions(m_hThrowable);

    m_pExceptionRecord.EnumMem();
    m_pContext.EnumMem();
}

#endif // #ifdef DACCESS_COMPILE


void ExInfo::SetExceptionCode(const EXCEPTION_RECORD *pCER)
{
#ifndef DACCESS_COMPILE
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    _ASSERTE(pCER != NULL);
    m_ExceptionCode = pCER->ExceptionCode;

    if (IsInstanceTaggedSEHCode(pCER->ExceptionCode) && ::WasThrownByUs(pCER, pCER->ExceptionCode))
    {
        m_ExceptionFlags.SetWasThrownByUs();
    }
    else
    {
        m_ExceptionFlags.ResetWasThrownByUs();
    }
#else // DACCESS_COMPILE
    // This method is invoked by the X86 version of CLR's exception handler for
    // managed code. There is no reason why DAC would be invoking this.
    DacError(E_UNEXPECTED);
#endif // !DACCESS_COMPILE
}
#else // !FEATURE_EH_FUNCLETS

#ifndef DACCESS_COMPILE

ExInfo::ExInfo(Thread *pThread, EXCEPTION_RECORD *pExceptionRecord, CONTEXT *pExceptionContext, ExKind exceptionKind) :
    ExceptionTrackerBase(pExceptionRecord, pExceptionContext, pThread->GetExceptionState()->GetCurrentExceptionTracker()),
    m_pExContext(&m_exContext),
    m_exception((Object*)NULL),
    m_kind(exceptionKind),
    m_passNumber(1),
    m_idxCurClause(0xffffffff),
    m_notifyDebuggerSP(NULL),
    m_pFrame(pThread->GetFrame()),
    m_ClauseForCatch({}),
#ifdef HOST_UNIX
    m_fOwnsExceptionPointers(FALSE),
    m_propagateExceptionCallback(NULL),
    m_propagateExceptionContext(NULL),
#endif // HOST_UNIX
    m_CurrentClause({}),
    m_pMDToReportFunctionLeave(NULL)
{
    m_StackTraceInfo.AllocateStackTrace();
    pThread->GetExceptionState()->m_pCurrentTracker = this;
    memcpy(&m_exContext, pExceptionContext, sizeof(CONTEXT));
    m_exContext.ContextFlags = m_exContext.ContextFlags & (CONTEXT_FULL | CONTEXT_EXCEPTION_ACTIVE);
    if (exceptionKind == ExKind::HardwareFault)
    {
        // Hardware exception handling needs to start on the FaultingExceptionFrame
        SetIP(&m_exContext, 0);
    }
}

#if defined(TARGET_UNIX)
void ExInfo::TakeExceptionPointersOwnership(PAL_SEHException* ex)
{
    _ASSERTE(ex->GetExceptionRecord() == m_ptrs.ExceptionRecord);
    _ASSERTE(ex->GetContextRecord() == m_ptrs.ContextRecord);
    ex->Clear();
    m_fOwnsExceptionPointers = TRUE;
}
#endif // TARGET_UNIX

void ExInfo::ReleaseResources()
{
    if (m_hThrowable)
    {
        if (!CLRException::IsPreallocatedExceptionHandle(m_hThrowable))
        {
            DestroyHandle(m_hThrowable);
        }
        m_hThrowable = NULL;
    }
    m_StackTraceInfo.FreeStackTrace();

#ifndef TARGET_UNIX
    // Clear any held Watson Bucketing details
    GetWatsonBucketTracker()->ClearWatsonBucketDetails();
#else // !TARGET_UNIX
    if (m_fOwnsExceptionPointers)
    {
        PAL_FreeExceptionRecords(m_ptrs.ExceptionRecord, m_ptrs.ContextRecord);
        m_fOwnsExceptionPointers = FALSE;
    }
#endif // !TARGET_UNIX
}

// static
void ExInfo::PopExInfos(Thread *pThread, void *targetSp)
{
    ExInfo *pExInfo = (PTR_ExInfo)pThread->GetExceptionState()->GetCurrentExceptionTracker();
    while (pExInfo && pExInfo < (void*)targetSp)
    {
        pExInfo->ReleaseResources();
        pExInfo = (PTR_ExInfo)pExInfo->m_pPrevNestedInfo;
    }
    pThread->GetExceptionState()->m_pCurrentTracker = pExInfo;
}

static bool IsFilterStartOffset(EE_ILEXCEPTION_CLAUSE* pEHClause, DWORD_PTR dwHandlerStartPC)
{
    EECodeInfo codeInfo((PCODE)dwHandlerStartPC);
    _ASSERTE(codeInfo.IsValid());

    return pEHClause->FilterOffset == codeInfo.GetRelOffset();
}

void ExInfo::MakeCallbacksRelatedToHandler(
    bool fBeforeCallingHandler,
    Thread*                pThread,
    MethodDesc*            pMD,
    EE_ILEXCEPTION_CLAUSE* pEHClause,
    DWORD_PTR              dwHandlerStartPC,
    StackFrame             sf
    )
{
#ifdef DEBUGGING_SUPPORTED
    // Here we need to make an extra check for filter handlers because we could be calling the catch handler
    // associated with a filter handler and yet the EH clause we have saved is for the filter handler.
    BOOL fIsFilterHandler         = IsFilterHandler(pEHClause) && IsFilterStartOffset(pEHClause, dwHandlerStartPC);
    BOOL fIsFaultOrFinallyHandler = IsFaultOrFinally(pEHClause);

    if (fBeforeCallingHandler)
    {
        m_EHClauseInfo.SetManagedCodeEntered(TRUE);
        StackFrame sfToStore = sf;
        if ((m_pPrevNestedInfo != NULL) &&
            (((PTR_ExInfo)m_pPrevNestedInfo)->m_csfEnclosingClause == m_csfEnclosingClause))
        {
            // If this is a nested exception which has the same enclosing clause as the previous exception,
            // we should just propagate the clause info from the previous exception.
            sfToStore = m_pPrevNestedInfo->m_EHClauseInfo.GetStackFrameForEHClause();
        }
        m_EHClauseInfo.SetInfo(COR_PRF_CLAUSE_NONE, (UINT_PTR)dwHandlerStartPC, sfToStore);

        if (pMD->IsILStub())
        {
            return;
        }

        if (fIsFilterHandler)
        {
            m_EHClauseInfo.SetEHClauseType(COR_PRF_CLAUSE_FILTER);
            EEToDebuggerExceptionInterfaceWrapper::ExceptionFilter(pMD, (TADDR) dwHandlerStartPC, pEHClause->FilterOffset, (BYTE*)sf.SP);

            EEToProfilerExceptionInterfaceWrapper::ExceptionSearchFilterEnter(pMD);
            ETW::ExceptionLog::ExceptionFilterBegin(pMD, (PVOID)dwHandlerStartPC);
        }
        else
        {
            EEToDebuggerExceptionInterfaceWrapper::ExceptionHandle(pMD, (TADDR) dwHandlerStartPC, pEHClause->HandlerStartPC, (BYTE*)sf.SP);

            if (fIsFaultOrFinallyHandler)
            {
                m_EHClauseInfo.SetEHClauseType(COR_PRF_CLAUSE_FINALLY);
                EEToProfilerExceptionInterfaceWrapper::ExceptionUnwindFinallyEnter(pMD);
                ETW::ExceptionLog::ExceptionFinallyBegin(pMD, (PVOID)dwHandlerStartPC);
            }
            else
            {
                m_EHClauseInfo.SetEHClauseType(COR_PRF_CLAUSE_CATCH);
                EEToProfilerExceptionInterfaceWrapper::ExceptionCatcherEnter(pThread, pMD);

                DACNotify::DoExceptionCatcherEnterNotification(pMD, pEHClause->HandlerStartPC);
                ETW::ExceptionLog::ExceptionCatchBegin(pMD, (PVOID)dwHandlerStartPC);
            }
        }
    }
    else
    {
        if (pMD->IsILStub())
        {
            return;
        }

        if (fIsFilterHandler)
        {
            ETW::ExceptionLog::ExceptionFilterEnd();
            EEToProfilerExceptionInterfaceWrapper::ExceptionSearchFilterLeave();
        }
        else
        {
            if (fIsFaultOrFinallyHandler)
            {
                ETW::ExceptionLog::ExceptionFinallyEnd();
                EEToProfilerExceptionInterfaceWrapper::ExceptionUnwindFinallyLeave();
            }
            else
            {
                ETW::ExceptionLog::ExceptionCatchEnd();
                ETW::ExceptionLog::ExceptionThrownEnd();
                EEToProfilerExceptionInterfaceWrapper::ExceptionCatcherLeave();
            }
        }

        m_EHClauseInfo.SetManagedCodeEntered(FALSE);
        m_EHClauseInfo.ResetInfo();
    }
#endif // DEBUGGING_SUPPORTED
}

#endif // DACCESS_COMPILE

#endif // !FEATURE_EH_FUNCLETS
