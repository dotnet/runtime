// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "exinfo.h"
#include "dbginterface.h"

#include "eetoprofinterfacewrapper.inl"
#include "eedbginterfaceimpl.inl"

#ifndef DACCESS_COMPILE

ExInfo::ExInfo(Thread *pThread, EXCEPTION_RECORD *pExceptionRecord, CONTEXT *pExceptionContext, ExKind exceptionKind) :
    m_pPrevNestedInfo(pThread->GetExceptionState()->GetCurrentExceptionTracker()),
    m_hThrowable{},
    m_ptrs({pExceptionRecord, pExceptionContext}),
    m_fDeliveredFirstChanceNotification(FALSE),
    m_ExceptionCode((pExceptionRecord != PTR_NULL) ? pExceptionRecord->ExceptionCode : 0),
    m_pExContext(&m_exContext),
    m_exception((Object*)NULL),
    m_kind(exceptionKind),
    m_passNumber(1),
    m_idxCurClause(0xffffffff),
    m_notifyDebuggerSP{},
    m_pFrame(pThread->GetFrame()),
    m_ClauseForCatch({}),
#ifdef HOST_UNIX
    m_fOwnsExceptionPointers(FALSE),
    m_propagateExceptionCallback(NULL),
    m_propagateExceptionContext(NULL),
#endif // HOST_UNIX
    m_CurrentClause({}),
    m_pMDToReportFunctionLeave(NULL),
    m_reportedFunctionEnterWasForFunclet(false)
#ifdef HOST_WINDOWS
    , m_pLongJmpBuf(NULL),
    m_longJmpReturnValue(0)
#endif // HOST_WINDOWS
{
    pThread->GetExceptionState()->m_pCurrentTracker = this;
    m_pInitialFrame = pThread->GetFrame();
    if (exceptionKind == ExKind::HardwareFault)
    {
        // Hardware exception handling needs to start on the FaultingExceptionFrame, so we are
        // passing in a context with zeroed out IP and SP.
        SetIP(&m_exContext, 0);
        SetSP(&m_exContext, 0);
        m_exContext.ContextFlags = CONTEXT_FULL;
    }
    else
    {
        memcpy(&m_exContext, pExceptionContext, sizeof(CONTEXT));
        m_exContext.ContextFlags = m_exContext.ContextFlags & (CONTEXT_FULL | CONTEXT_EXCEPTION_ACTIVE);
    }

#ifndef TARGET_UNIX
    // Init the WatsonBucketTracker
    m_WatsonBucketTracker.Init();
#endif // !TARGET_UNIX
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
    STRESS_LOG1(LF_EH, LL_INFO100, "Popping ExInfos below SP=%p\n", targetSp);

    ExInfo *pExInfo = (PTR_ExInfo)pThread->GetExceptionState()->GetCurrentExceptionTracker();
#if defined(DEBUGGING_SUPPORTED)
    DWORD_PTR dwInterceptStackFrame = 0;

    // This method may be called on an unmanaged thread, in which case no interception can be done.
    if (pExInfo)
    {
        ThreadExceptionState* pExState = pThread->GetExceptionState();

        // If the exception is intercepted, then pop trackers according to the stack frame at which
        // the exception is intercepted.  We must retrieve the frame pointer before we start popping trackers.
        if (pExState->GetFlags()->DebuggerInterceptInfo())
        {
            pExState->GetDebuggerState()->GetDebuggerInterceptInfo(NULL, NULL, (PBYTE*)&dwInterceptStackFrame,
                                                                   NULL, NULL);
        }
    }
#endif // DEBUGGING_SUPPORTED

    while (pExInfo && pExInfo < (void*)targetSp)
    {
#if defined(DEBUGGING_SUPPORTED)
        if (g_pDebugInterface != NULL)
        {
            if (pExInfo->m_ScannedStackRange.GetUpperBound().SP < dwInterceptStackFrame)
            {
                g_pDebugInterface->DeleteInterceptContext(pExInfo->m_DebuggerExState.GetDebuggerInterceptContext());
            }
        }
#endif // DEBUGGING_SUPPORTED

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

        if (pMD->IsDiagnosticsHidden())
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
        if (pMD->IsDiagnosticsHidden())
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
