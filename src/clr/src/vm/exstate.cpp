//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
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
    
#ifdef WIN64EXCEPTIONS
    if (m_pCurrentTracker)
    {
        return m_pCurrentTracker->m_hThrowable;
    }

    return NULL;
#else // WIN64EXCEPTIONS
    return m_currentExInfo.m_hThrowable;
#endif // WIN64EXCEPTIONS    
}


ThreadExceptionState::ThreadExceptionState()
{
#ifdef WIN64EXCEPTIONS
    m_pCurrentTracker = NULL;
#endif // WIN64EXCEPTIONS

    m_flag = TEF_None;

#ifndef FEATURE_PAL
    // Init the UE Watson BucketTracker
    m_UEWatsonBucketTracker.Init();
#endif // !FEATURE_PAL

#ifdef FEATURE_CORRUPTING_EXCEPTIONS
    // Initialize the default exception severity to NotCorrupting
    m_LastActiveExceptionCorruptionSeverity = NotSet;
    m_fCanReflectionTargetHandleException = FALSE;
#endif // FEATURE_CORRUPTING_EXCEPTIONS

}

ThreadExceptionState::~ThreadExceptionState()
{
#ifndef FEATURE_PAL
    // Init the UE Watson BucketTracker
    m_UEWatsonBucketTracker.ClearWatsonBucketDetails();
#endif // !FEATURE_PAL
}

#if defined(_DEBUG)
void ThreadExceptionState::AssertStackTraceInfo(StackTraceInfo *pSTI)
{
    LIMITED_METHOD_CONTRACT;
#if defined(WIN64EXCEPTIONS)

    _ASSERTE(pSTI == &(m_pCurrentTracker->m_StackTraceInfo) || pSTI == &(m_OOMTracker.m_StackTraceInfo));

#else  // win64exceptions

    _ASSERTE(pSTI == &(m_currentExInfo.m_StackTraceInfo));

#endif // win64exceptions
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

#ifdef WIN64EXCEPTIONS
    ExceptionTracker* pNode = m_pCurrentTracker;
#else // WIN64EXCEPTIONS
    ExInfo*           pNode = &m_currentExInfo;
#endif // WIN64EXCEPTIONS

    for ( ;
          pNode != NULL;
          pNode = pNode->m_pPrevNestedInfo)
    {
        pNode->m_StackTraceInfo.FreeStackTrace();
    }
}

void ThreadExceptionState::ClearThrowablesForUnload(HandleTableBucket* pHndTblBucket)
{
    WRAPPER_NO_CONTRACT;

#ifdef WIN64EXCEPTIONS
    ExceptionTracker* pNode = m_pCurrentTracker;
#else // WIN64EXCEPTIONS
    ExInfo*           pNode = &m_currentExInfo;
#endif // WIN64EXCEPTIONS

    for ( ;
          pNode != NULL;
          pNode = pNode->m_pPrevNestedInfo)
    {
        if (pHndTblBucket->Contains(pNode->m_hThrowable))
        {
            pNode->DestroyExceptionHandle();
        }
    }
}


// After unwinding from an SO, there may be stale exception state.
void ThreadExceptionState::ClearExceptionStateAfterSO(void* pStackFrameSP)
{
    WRAPPER_NO_CONTRACT;

    #if defined(WIN64EXCEPTIONS)
        ExceptionTracker::PopTrackers(pStackFrameSP);
    #else
        // After unwinding from an SO, there may be stale exception state.  We need to
        //  get rid of any state that assumes the handlers that have been unwound/unlinked.
        // 
        // Because the ExState chains to entries that may be on the stack, and the
        //  stack has been unwound, it may not be safe to reference any entries
        //  other than the one of the Thread object.
        //
        // Consequently, we will simply Init() the ExInfo on the Thread object.
        m_currentExInfo.Init();
    #endif
} // void ThreadExceptionState::ClearExceptionStateAfterSO()

OBJECTREF ThreadExceptionState::GetThrowable()
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
    }
    CONTRACTL_END;
    
#ifdef WIN64EXCEPTIONS
    if (m_pCurrentTracker && m_pCurrentTracker->m_hThrowable)
    {
        return ObjectFromHandle(m_pCurrentTracker->m_hThrowable);
    }
#else // WIN64EXCEPTIONS
    if (m_currentExInfo.m_hThrowable)
    {
        return ObjectFromHandle(m_currentExInfo.m_hThrowable);
    }
#endif // WIN64EXCEPTIONS    

    return NULL;
}

void ThreadExceptionState::SetThrowable(OBJECTREF throwable DEBUG_ARG(SetThrowableErrorChecking stecFlags))
{
    CONTRACTL
    {
        if ((throwable == NULL) || CLRException::IsPreallocatedExceptionObject(throwable)) NOTHROW; else THROWS; // From CreateHandle
        GC_NOTRIGGER;
        if (throwable == NULL) MODE_ANY; else MODE_COOPERATIVE;
        SO_TOLERANT;
    }
    CONTRACTL_END;

#ifdef WIN64EXCEPTIONS
    if (m_pCurrentTracker)
    {
        m_pCurrentTracker->DestroyExceptionHandle();
    }
#else // WIN64EXCEPTIONS
    m_currentExInfo.DestroyExceptionHandle();
#endif // WIN64EXCEPTIONS
    
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
            BEGIN_SO_INTOLERANT_CODE(GetThread());
            {
                AppDomain* pDomain = GetMyThread()->GetDomain();
                PREFIX_ASSUME(pDomain != NULL);
                hNewThrowable = pDomain->CreateHandle(throwable);
            }
            END_SO_INTOLERANT_CODE;
        }

#ifdef WIN64EXCEPTIONS
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
#else // WIN64EXCEPTIONS
        m_currentExInfo.m_hThrowable = hNewThrowable;
#endif // WIN64EXCEPTIONS
    }
}

DWORD ThreadExceptionState::GetExceptionCode()
{
    LIMITED_METHOD_CONTRACT;
    
#ifdef WIN64EXCEPTIONS
    _ASSERTE(m_pCurrentTracker);
    return m_pCurrentTracker->m_ExceptionCode;
#else // WIN64EXCEPTIONS
    return m_currentExInfo.m_ExceptionCode;
#endif // WIN64EXCEPTIONS
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
    
#ifdef WIN64EXCEPTIONS
    return (m_pCurrentTracker != NULL);
#else // WIN64EXCEPTIONS
    return (m_currentExInfo.m_pBottomMostHandler != NULL);
#endif // WIN64EXCEPTIONS
}

#if !defined(DACCESS_COMPILE)

void ThreadExceptionState::GetLeafFrameInfo(StackTraceElement* pStackTraceElement)
{
    WRAPPER_NO_CONTRACT;

#ifdef WIN64EXCEPTIONS
    m_pCurrentTracker->m_StackTraceInfo.GetLeafFrameInfo(pStackTraceElement);
#else
    m_currentExInfo.m_StackTraceInfo.GetLeafFrameInfo(pStackTraceElement);
#endif
}

EXCEPTION_POINTERS* ThreadExceptionState::GetExceptionPointers()
{
    LIMITED_METHOD_CONTRACT;
    
#ifdef WIN64EXCEPTIONS
    if (m_pCurrentTracker)
    {
        return (EXCEPTION_POINTERS*)&(m_pCurrentTracker->m_ptrs);
    }
    else
    {
        return NULL;
    }
#else // WIN64EXCEPTIONS
    return m_currentExInfo.m_pExceptionPointers;
#endif // WIN64EXCEPTIONS
}

//-----------------------------------------------------------------------------
// SetExceptionPointers -- accessor to set pointer to EXCEPTION_POINTERS
//   member.
//
//  only x86
//
#if !defined(WIN64EXCEPTIONS)
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
    
#ifdef WIN64EXCEPTIONS
    if (m_pCurrentTracker)
    {
        return m_pCurrentTracker->m_ptrs.ExceptionRecord;
    }
    else
    {
        return NULL;
    }
#else // WIN64EXCEPTIONS
    return m_currentExInfo.m_pExceptionRecord;
#endif // WIN64EXCEPTIONS
}

PTR_CONTEXT ThreadExceptionState::GetContextRecord()
{
    LIMITED_METHOD_DAC_CONTRACT;
    
#ifdef WIN64EXCEPTIONS
    if (m_pCurrentTracker)
    {
        return m_pCurrentTracker->m_ptrs.ContextRecord;
    }
    else
    {
        return NULL;
    }
#else // WIN64EXCEPTIONS
    return m_currentExInfo.m_pContext;
#endif // WIN64EXCEPTIONS
}

ExceptionFlags* ThreadExceptionState::GetFlags()
{
#ifdef WIN64EXCEPTIONS

    if (m_pCurrentTracker)
    {
        return &(m_pCurrentTracker->m_ExceptionFlags);
    }
    else
    {
        _ASSERTE(!"GetFlags() called when there is no current exception");
        return NULL;
    }

#else // WIN64EXCEPTIONS

    return &(m_currentExInfo.m_ExceptionFlags);

#endif // WIN64EXCEPTIONS
}

#if !defined(DACCESS_COMPILE)

#ifdef DEBUGGING_SUPPORTED    
DebuggerExState*    ThreadExceptionState::GetDebuggerState()
{
#ifdef WIN64EXCEPTIONS
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
#else // WIN64EXCEPTIONS
    return &(m_currentExInfo.m_DebuggerExState);
#endif // WIN64EXCEPTIONS
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

#ifdef _TARGET_X86_
PEXCEPTION_REGISTRATION_RECORD GetClrSEHRecordServicingStackPointer(Thread *pThread, void *pStackPointer);
#endif // _TARGET_X86_

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
    
#if defined(_TARGET_X86_)
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
#elif !defined(WIN64EXCEPTIONS)  
    // !_TARGET_X86_ && !WIN64EXCEPTIONS
    PORTABILITY_ASSERT("SetDebuggerInterceptInfo() (ExState.cpp) - continuable exceptions NYI\n");
    return FALSE;
#endif // !_TARGET_X86_

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

EHClauseInfo* ThreadExceptionState::GetCurrentEHClauseInfo()
{
#ifdef WIN64EXCEPTIONS
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
#else // WIN64EXCEPTIONS
    return &(m_currentExInfo.m_EHClauseInfo);
#endif // WIN64EXCEPTIONS
}

#endif // DACCESS_COMPILE

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
    _ASSERTE(pThread);

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
#ifdef WIN64EXCEPTIONS
    ExceptionTracker* head = m_pCurrentTracker;

    if (head == NULL)
    {
        return;
    }
    
#else // WIN64EXCEPTIONS
    ExInfo*           head = &m_currentExInfo;
#endif // WIN64EXCEPTIONS
    
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



