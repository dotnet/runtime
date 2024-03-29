// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//


#include "common.h"
#include "exstate.h"
#include "exinfo.h"

#ifdef _DEBUG
#include "comutilnative.h"      // for assertions only
#endif

OBJECTHANDLE ThreadExceptionState::GetThrowableAsHandle()
{
    WRAPPER_NO_CONTRACT;

#ifdef FEATURE_EH_FUNCLETS
    if (m_pCurrentTracker)
    {
        return m_pCurrentTracker->m_hThrowable;
    }

    return NULL;
#else // FEATURE_EH_FUNCLETS
    return m_currentExInfo.m_hThrowable;
#endif // FEATURE_EH_FUNCLETS
}


ThreadExceptionState::ThreadExceptionState()
{
#ifdef FEATURE_EH_FUNCLETS
    m_pCurrentTracker = NULL;
#endif // FEATURE_EH_FUNCLETS

    m_flag = TEF_None;

#ifndef TARGET_UNIX
    // Init the UE Watson BucketTracker
    m_UEWatsonBucketTracker.Init();
#endif // !TARGET_UNIX
}

ThreadExceptionState::~ThreadExceptionState()
{
#ifndef TARGET_UNIX
    // Init the UE Watson BucketTracker
    m_UEWatsonBucketTracker.ClearWatsonBucketDetails();
#endif // !TARGET_UNIX
}

#if defined(_DEBUG)
void ThreadExceptionState::AssertStackTraceInfo(StackTraceInfo *pSTI)
{
    LIMITED_METHOD_CONTRACT;
#if defined(FEATURE_EH_FUNCLETS)

    _ASSERTE(pSTI == &(m_pCurrentTracker->m_StackTraceInfo) || pSTI == &(m_OOMTracker.m_StackTraceInfo));

#else  // !FEATURE_EH_FUNCLETS

    _ASSERTE(pSTI == &(m_currentExInfo.m_StackTraceInfo));

#endif // !FEATURE_EH_FUNCLETS
} // void ThreadExceptionState::AssertStackTraceInfo()
#endif // _debug

#ifndef DACCESS_COMPILE

Thread* ThreadExceptionState::GetMyThread()
{
    return (Thread*)(((BYTE*)this) - offsetof(Thread, m_ExceptionState));
}


void ThreadExceptionState::FreeAllStackTraces()
{
    WRAPPER_NO_CONTRACT;

#ifdef FEATURE_EH_FUNCLETS
    ExceptionTrackerBase* pNode = m_pCurrentTracker;
#else // FEATURE_EH_FUNCLETS
    ExInfo*           pNode = &m_currentExInfo;
#endif // FEATURE_EH_FUNCLETS

    for ( ;
          pNode != NULL;
          pNode = pNode->m_pPrevNestedInfo)
    {
        pNode->m_StackTraceInfo.FreeStackTrace();
    }
}

OBJECTREF ThreadExceptionState::GetThrowable()
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

#ifdef FEATURE_EH_FUNCLETS
    if (m_pCurrentTracker && m_pCurrentTracker->m_hThrowable)
    {
        return ObjectFromHandle(m_pCurrentTracker->m_hThrowable);
    }
#else // FEATURE_EH_FUNCLETS
    if (m_currentExInfo.m_hThrowable)
    {
        return ObjectFromHandle(m_currentExInfo.m_hThrowable);
    }
#endif // FEATURE_EH_FUNCLETS

    return NULL;
}

void ThreadExceptionState::SetThrowable(OBJECTREF throwable DEBUG_ARG(SetThrowableErrorChecking stecFlags))
{
    CONTRACTL
    {
        if ((throwable == NULL) || CLRException::IsPreallocatedExceptionObject(throwable)) NOTHROW; else THROWS; // From CreateHandle
        GC_NOTRIGGER;
        if (throwable == NULL) MODE_ANY; else MODE_COOPERATIVE;
    }
    CONTRACTL_END;

#ifdef FEATURE_EH_FUNCLETS
    if (m_pCurrentTracker)
    {
        m_pCurrentTracker->DestroyExceptionHandle();
    }
#else // FEATURE_EH_FUNCLETS
    m_currentExInfo.DestroyExceptionHandle();
#endif // FEATURE_EH_FUNCLETS

    if (throwable != NULL)
    {
        // Non-compliant exceptions are always wrapped.
        // The use of the ExceptionNative:: helper here (rather than the global ::IsException helper)
        // is hokey, but we need a GC_NOTRIGGER version and it's only for an ASSERT.
        _ASSERTE(IsException(throwable->GetMethodTable()));

        OBJECTHANDLE hNewThrowable;

        // If we're tracking one of the preallocated exception objects, then just use the global handle that
        // matches it rather than creating a new one.
        if (CLRException::IsPreallocatedExceptionObject(throwable))
        {
            hNewThrowable = CLRException::GetPreallocatedHandleForObject(throwable);
        }
        else
        {
            AppDomain* pDomain = GetMyThread()->GetDomain();
            PREFIX_ASSUME(pDomain != NULL);
            hNewThrowable = pDomain->CreateHandle(throwable);
        }

#ifdef FEATURE_EH_FUNCLETS
#ifdef _DEBUG
        //
        // Fatal stack overflow policy ends up short-circuiting the normal exception handling
        // flow such that there could be no Tracker for this SO that is in flight.  In this
        // situation there is no place to store the throwable in the exception state, and instead
        // it is presumed that the handle to the SO exception is elsewhere.  (Current knowledge
        // as of 7/15/05 is that it is stored in Thread::m_LastThrownObjectHandle;
        //
        if (stecFlags != STEC_CurrentTrackerEqualNullOkHackForFatalStackOverflow
#ifdef FEATURE_INTERPRETER
            && stecFlags != STEC_CurrentTrackerEqualNullOkForInterpreter
#endif // FEATURE_INTERPRETER
            )
        {
            CONSISTENCY_CHECK(CheckPointer(m_pCurrentTracker));
        }
#endif

        if (m_pCurrentTracker != NULL)
        {
            m_pCurrentTracker->m_hThrowable = hNewThrowable;
        }
#else // FEATURE_EH_FUNCLETS
        m_currentExInfo.m_hThrowable = hNewThrowable;
#endif // FEATURE_EH_FUNCLETS
    }
}

DWORD ThreadExceptionState::GetExceptionCode()
{
    LIMITED_METHOD_CONTRACT;

#ifdef FEATURE_EH_FUNCLETS
    _ASSERTE(m_pCurrentTracker);
    return m_pCurrentTracker->m_ExceptionCode;
#else // FEATURE_EH_FUNCLETS
    return m_currentExInfo.m_ExceptionCode;
#endif // FEATURE_EH_FUNCLETS
}

BOOL ThreadExceptionState::IsComPlusException()
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_FORBID_FAULT;

    if (GetExceptionCode() != EXCEPTION_COMPLUS)
    {
        return FALSE;
    }

    _ASSERTE(IsInstanceTaggedSEHCode(GetExceptionCode()));



    return GetFlags()->WasThrownByUs();
}


#endif // !DACCESS_COMPILE

BOOL ThreadExceptionState::IsExceptionInProgress()
{
    LIMITED_METHOD_DAC_CONTRACT;

#ifdef FEATURE_EH_FUNCLETS
    return (m_pCurrentTracker != NULL);
#else // FEATURE_EH_FUNCLETS
    return (m_currentExInfo.m_pBottomMostHandler != NULL);
#endif // FEATURE_EH_FUNCLETS
}

#if !defined(DACCESS_COMPILE)

EXCEPTION_POINTERS* ThreadExceptionState::GetExceptionPointers()
{
    LIMITED_METHOD_CONTRACT;

#ifdef FEATURE_EH_FUNCLETS
    if (m_pCurrentTracker)
    {
        return (EXCEPTION_POINTERS*)&(m_pCurrentTracker->m_ptrs);
    }
    else
    {
        return NULL;
    }
#else // FEATURE_EH_FUNCLETS
    return m_currentExInfo.m_pExceptionPointers;
#endif // FEATURE_EH_FUNCLETS
}

//-----------------------------------------------------------------------------
// SetExceptionPointers -- accessor to set pointer to EXCEPTION_POINTERS
//   member.
//
//  only x86
//
#if !defined(FEATURE_EH_FUNCLETS)
void ThreadExceptionState::SetExceptionPointers(
    EXCEPTION_POINTERS *pExceptionPointers) // Value to set
{
    m_currentExInfo.m_pExceptionPointers = pExceptionPointers;
} // void ThreadExceptionState::SetExceptionPointers()
#endif

#endif // !DACCESS_COMPILE

PTR_EXCEPTION_RECORD ThreadExceptionState::GetExceptionRecord()
{
    LIMITED_METHOD_DAC_CONTRACT;

#ifdef FEATURE_EH_FUNCLETS
    if (m_pCurrentTracker)
    {
        return m_pCurrentTracker->m_ptrs.ExceptionRecord;
    }
    else
    {
        return NULL;
    }
#else // FEATURE_EH_FUNCLETS
    return m_currentExInfo.m_pExceptionRecord;
#endif // FEATURE_EH_FUNCLETS
}

PTR_CONTEXT ThreadExceptionState::GetContextRecord()
{
    LIMITED_METHOD_DAC_CONTRACT;

#ifdef FEATURE_EH_FUNCLETS
    if (m_pCurrentTracker)
    {
        return m_pCurrentTracker->m_ptrs.ContextRecord;
    }
    else
    {
        return NULL;
    }
#else // FEATURE_EH_FUNCLETS
    return m_currentExInfo.m_pContext;
#endif // FEATURE_EH_FUNCLETS
}

ExceptionFlags* ThreadExceptionState::GetFlags()
{
#ifdef FEATURE_EH_FUNCLETS

    if (m_pCurrentTracker)
    {
        return &(m_pCurrentTracker->m_ExceptionFlags);
    }
    else
    {
        _ASSERTE(!"GetFlags() called when there is no current exception");
        return NULL;
    }

#else // FEATURE_EH_FUNCLETS

    return &(m_currentExInfo.m_ExceptionFlags);

#endif // FEATURE_EH_FUNCLETS
}

#if !defined(DACCESS_COMPILE)

#ifdef DEBUGGING_SUPPORTED
DebuggerExState*    ThreadExceptionState::GetDebuggerState()
{
#ifdef FEATURE_EH_FUNCLETS
    if (m_pCurrentTracker)
    {
        return &(m_pCurrentTracker->m_DebuggerExState);
    }
    else
    {
        _ASSERTE(!"unexpected use of GetDebuggerState() when no exception in flight");
#if defined(_MSC_VER)
        #pragma warning(disable : 4640)
#endif
        static DebuggerExState   m_emptyDebuggerExState;

#if defined(_MSC_VER)
        #pragma warning(default : 4640)
#endif
        return &m_emptyDebuggerExState;
    }
#else // FEATURE_EH_FUNCLETS
    return &(m_currentExInfo.m_DebuggerExState);
#endif // FEATURE_EH_FUNCLETS
}

BOOL ThreadExceptionState::IsDebuggerInterceptable()
{
    LIMITED_METHOD_CONTRACT;
    DWORD ExceptionCode = GetExceptionCode();
    return (BOOL)((ExceptionCode != STATUS_STACK_OVERFLOW) &&
                  (ExceptionCode != EXCEPTION_BREAKPOINT) &&
                  (ExceptionCode != EXCEPTION_SINGLE_STEP) &&
                  !GetFlags()->UnwindHasStarted() &&
                  !GetFlags()->DebuggerInterceptNotPossible());
}

#ifdef TARGET_X86
PEXCEPTION_REGISTRATION_RECORD GetClrSEHRecordServicingStackPointer(Thread *pThread, void *pStackPointer);
#endif // TARGET_X86

//---------------------------------------------------------------------------------------
//
// This function is called by the debugger to store information necessary to intercept the current exception.
// This information is consumed by the EH subsystem to start the unwind and resume execution without
// finding and executing a catch clause.
//
// Arguments:
//    pJitManager   - the JIT manager for the method where we are going to intercept the exception
//    pThread       - the thread on which the interception is taking place
//    methodToken   - the MethodDef token of the interception method
//    pFunc         - the MethodDesc of the interception method
//    natOffset     - the native offset at which we are going to resume execution
//    sfDebuggerInterceptFramePointer
//                  - the frame pointer of the interception method frame
//    pFlags        - flags on the current exception (ExInfo on x86 and ExceptionTracker on WIN64);
//                    to be set by this function to indicate that an interception is going on
//
// Return Value:
//    whether the operation is successful
//

BOOL DebuggerExState::SetDebuggerInterceptInfo(IJitManager *pJitManager,
                                      Thread *pThread,
                                      const METHODTOKEN& methodToken,
                                      MethodDesc *pFunc,
                                      ULONG_PTR natOffset,
                                      StackFrame sfDebuggerInterceptFramePointer,
                                      ExceptionFlags* pFlags)
{
    WRAPPER_NO_CONTRACT;

    //
    // Verify parameters are non-NULL
    //
    if ((pJitManager == NULL) ||
        (pThread == NULL) ||
        (methodToken.IsNull()) ||
        (pFunc == NULL) ||
        (natOffset == (TADDR)0) ||
        (sfDebuggerInterceptFramePointer.IsNull()))
    {
        return FALSE;
    }

    //
    // You can only call this function on the currently active exception.
    //
    if (this != pThread->GetExceptionState()->GetDebuggerState())
    {
        return FALSE;
    }

    //
    // Check that the stack pointer is less than as far as we have searched so far.
    //
    if (sfDebuggerInterceptFramePointer > m_sfDebuggerIndicatedFramePointer)
    {
        return FALSE;
    }

    int nestingLevel = 0;

#ifndef FEATURE_EH_FUNCLETS
    //
    // Get the SEH frame that covers this location on the stack. Note: we pass a skip count of 1. We know that when
    // this is called, there is a nested exception handler on pThread's stack that is only there during exception
    // processing, and it won't be there when we go to do the interception. Therefore, we skip that nested record,
    // and pick the next valid record above it.
    //
    m_pDebuggerInterceptFrame = GetClrSEHRecordServicingStackPointer(pThread, (LPVOID)sfDebuggerInterceptFramePointer.SP);
    if (m_pDebuggerInterceptFrame == EXCEPTION_CHAIN_END)
    {
        return FALSE;
    }

    //
    // Now we need to search and find the function information for this entry on the stack.
    //
    nestingLevel = ComputeEnclosingHandlerNestingLevel(pJitManager,
                                                           methodToken,
                                                           natOffset);
#endif // !FEATURE_EH_FUNCLETS

    //
    // These values will override the normal information used by the EH subsystem to handle the exception.
    // They are retrieved by GetDebuggerInterceptInfo().
    //
    m_pDebuggerInterceptFunc = pFunc;
    m_dDebuggerInterceptHandlerDepth  = nestingLevel;
    m_sfDebuggerInterceptFramePointer = sfDebuggerInterceptFramePointer;
    m_pDebuggerInterceptNativeOffset  = natOffset;

    // set a flag on the exception tracking struct to indicate that an interception is in progress
    pFlags->SetDebuggerInterceptInfo();
    return TRUE;
}
#endif // DEBUGGING_SUPPORTED

#endif // DACCESS_COMPILE

EHClauseInfo* ThreadExceptionState::GetCurrentEHClauseInfo()
{
#ifdef FEATURE_EH_FUNCLETS
    if (m_pCurrentTracker)
    {
        return &(m_pCurrentTracker->m_EHClauseInfo);
    }
    else
    {
        _ASSERTE(!"unexpected use of GetCurrentEHClauseInfo() when no exception in flight");
#if defined(_MSC_VER)
        #pragma warning(disable : 4640)
#endif // defined(_MSC_VER)

        static EHClauseInfo m_emptyEHClauseInfo;

#if defined(_MSC_VER)
        #pragma warning(default : 4640)
#endif // defined(_MSC_VER)

        return &m_emptyEHClauseInfo;
    }
#else // FEATURE_EH_FUNCLETS
    return &(m_currentExInfo.m_EHClauseInfo);
#endif // FEATURE_EH_FUNCLETS
}

void ThreadExceptionState::SetThreadExceptionFlag(ThreadExceptionFlag flag)
{
    LIMITED_METHOD_CONTRACT;

    m_flag = (ThreadExceptionFlag)((DWORD)m_flag | flag);
}

void ThreadExceptionState::ResetThreadExceptionFlag(ThreadExceptionFlag flag)
{
    LIMITED_METHOD_CONTRACT;

    m_flag = (ThreadExceptionFlag)((DWORD)m_flag & ~flag);
}

BOOL ThreadExceptionState::HasThreadExceptionFlag(ThreadExceptionFlag flag)
{
    LIMITED_METHOD_CONTRACT;

    return ((DWORD)m_flag & flag);
}

ThreadExceptionFlagHolder::ThreadExceptionFlagHolder(ThreadExceptionState::ThreadExceptionFlag flag)
{
    WRAPPER_NO_CONTRACT;

    Thread* pThread = GetThread();
    m_pExState = pThread->GetExceptionState();

    m_flag = flag;
    m_pExState->SetThreadExceptionFlag(m_flag);
}

ThreadExceptionFlagHolder::~ThreadExceptionFlagHolder()
{
    WRAPPER_NO_CONTRACT;

    _ASSERTE(m_pExState);
    m_pExState->ResetThreadExceptionFlag(m_flag);
}

#ifdef DACCESS_COMPILE

void
ThreadExceptionState::EnumChainMemoryRegions(CLRDataEnumMemoryFlags flags)
{
#ifdef FEATURE_EH_FUNCLETS
    ExceptionTrackerBase* head = m_pCurrentTracker;

    if (head == NULL)
    {
        return;
    }

#else // FEATURE_EH_FUNCLETS
    ExInfo*           head = &m_currentExInfo;
#endif // FEATURE_EH_FUNCLETS

    for (;;)
    {
        head->EnumMemoryRegions(flags);

        if (!head->m_pPrevNestedInfo.IsValid())
        {
            break;
        }

        head->m_pPrevNestedInfo.EnumMem();
        head = head->m_pPrevNestedInfo;
    }
}


#endif // DACCESS_COMPILE



