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


// See ExInfo::GetThrowableAsPseudoHandle for details on the pseudo-handle.
OBJECTHANDLE ThreadExceptionState::GetThrowableAsPseudoHandle()
{
    LIMITED_METHOD_DAC_CONTRACT;

    if (m_pCurrentTracker)
    {
        return m_pCurrentTracker->GetThrowableAsPseudoHandle();
    }

    return (OBJECTHANDLE)NULL;
}


ThreadExceptionState::ThreadExceptionState()
{
    m_pCurrentTracker = NULL;

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

OBJECTREF ThreadExceptionState::GetThrowable()
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (m_pCurrentTracker)
    {
        return m_pCurrentTracker->m_exception;
    }

    return NULL;
}

BOOL ThreadExceptionState::IsThrowableNull()
{
    LIMITED_METHOD_DAC_CONTRACT;

    if (m_pCurrentTracker == NULL)
        return TRUE;

    return m_pCurrentTracker->m_exception == NULL;
}

#ifndef DACCESS_COMPILE


DWORD ThreadExceptionState::GetExceptionCode()
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(m_pCurrentTracker);
    return m_pCurrentTracker->m_ExceptionCode;
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

    return (m_pCurrentTracker != NULL);
}

#if !defined(DACCESS_COMPILE)

EXCEPTION_POINTERS* ThreadExceptionState::GetExceptionPointers()
{
    LIMITED_METHOD_CONTRACT;

    if (m_pCurrentTracker)
    {
        return (EXCEPTION_POINTERS*)&(m_pCurrentTracker->m_ptrs);
    }
    else
    {
        return NULL;
    }
}

#endif // !DACCESS_COMPILE

PTR_EXCEPTION_RECORD ThreadExceptionState::GetExceptionRecord()
{
    LIMITED_METHOD_DAC_CONTRACT;

    if (m_pCurrentTracker)
    {
        return m_pCurrentTracker->m_ptrs.ExceptionRecord;
    }
    else
    {
        return NULL;
    }
}

PTR_CONTEXT ThreadExceptionState::GetContextRecord()
{
    LIMITED_METHOD_DAC_CONTRACT;

    if (m_pCurrentTracker)
    {
        return m_pCurrentTracker->m_ptrs.ContextRecord;
    }
    else
    {
        return NULL;
    }
}

ExceptionFlags* ThreadExceptionState::GetFlags()
{

    if (m_pCurrentTracker)
    {
        return &(m_pCurrentTracker->m_ExceptionFlags);
    }
    else
    {
        _ASSERTE(!"GetFlags() called when there is no current exception");
        return NULL;
    }

}

#if !defined(DACCESS_COMPILE)

#ifdef DEBUGGING_SUPPORTED
static DebuggerExState   s_emptyDebuggerExState;

DebuggerExState*    ThreadExceptionState::GetDebuggerState()
{
    if (m_pCurrentTracker)
    {
        return &(m_pCurrentTracker->m_DebuggerExState);
    }
    else
    {
        _ASSERTE(!"unexpected use of GetDebuggerState() when no exception in flight");
        return &s_emptyDebuggerExState;
    }
}

void ThreadExceptionState::SetDebuggerIndicatedFramePointer(LPVOID indicatedFramePointer)
{
    WRAPPER_NO_CONTRACT;
    if (m_pCurrentTracker)
    {
        m_pCurrentTracker->m_DebuggerExState.SetDebuggerIndicatedFramePointer(indicatedFramePointer);
    }
    else
    {
        _ASSERTE(!"unexpected use of SetDebuggerIndicatedFramePointer() when no exception in flight");
    }
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
//    pFlags        - flags on the current exception (ExInfo);
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
    ExInfo*           head = m_pCurrentTracker;

    if (head == NULL)
    {
        return;
    }

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



