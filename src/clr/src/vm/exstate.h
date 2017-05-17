// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//

#ifndef __ExState_h__
#define __ExState_h__ 

class ExceptionFlags;
class DebuggerExState;
class EHClauseInfo;

#include "exceptionhandling.h"

#if !defined(WIN64EXCEPTIONS)
// ExInfo contains definitions for 32bit
#include "exinfo.h"
#endif // !defined(WIN64EXCEPTIONS)

#if !defined(DACCESS_COMPILE)
#define PRESERVE_WATSON_ACROSS_CONTEXTS 1
#endif

extern StackWalkAction COMPlusUnwindCallback(CrawlFrame *pCf, ThrowCallbackType *pData);

//
// This class serves as a forwarding and abstraction layer for the EH subsystem.
// Since we have two different implementations, this class is needed to unify 
// the EE's view of EH.  Ideally, this is just a step along the way to a unified
// EH subsystem.
//
typedef DPTR(class ThreadExceptionState) PTR_ThreadExceptionState;
class ThreadExceptionState
{
    friend class ClrDataExceptionState;
    friend class CheckAsmOffsets;
    friend class StackFrameIterator;

#ifdef DACCESS_COMPILE
    friend class ClrDataAccess;
#endif // DACCESS_COMPILE

    // ProfToEEInterfaceImpl::GetNotifiedExceptionClauseInfo needs access so that it can fetch the
    // ExceptionTracker or the ExInfo as appropriate for the platform
    friend class ProfToEEInterfaceImpl;

#ifdef WIN64EXCEPTIONS
    friend class ExceptionTracker;
#else
    friend class ExInfo;
#endif // WIN64EXCEPTIONS

public:
    
    void FreeAllStackTraces();
    void ClearThrowablesForUnload(IGCHandleStore* handleStore);

#ifdef _DEBUG
    typedef enum 
    {
        STEC_All,
        STEC_CurrentTrackerEqualNullOkHackForFatalStackOverflow,
#ifdef FEATURE_INTERPRETER
        STEC_CurrentTrackerEqualNullOkForInterpreter,
#endif // FEATURE_INTERPRETER
    } SetThrowableErrorChecking;
#endif

    void                SetThrowable(OBJECTREF throwable DEBUG_ARG(SetThrowableErrorChecking stecFlags = STEC_All));
    OBJECTREF           GetThrowable();
    OBJECTHANDLE        GetThrowableAsHandle();
    DWORD               GetExceptionCode();
    BOOL                IsComPlusException();
    EXCEPTION_POINTERS* GetExceptionPointers();
    PTR_EXCEPTION_RECORD GetExceptionRecord();
    PTR_CONTEXT          GetContextRecord();
    BOOL                IsExceptionInProgress();
    void                GetLeafFrameInfo(StackTraceElement* pStackTrace);

    ExceptionFlags*     GetFlags();

    ThreadExceptionState();
    ~ThreadExceptionState();

#if !defined(WIN64EXCEPTIONS)
      void              SetExceptionPointers(EXCEPTION_POINTERS *pExceptionPointers);
#endif


#ifdef DEBUGGING_SUPPORTED    
    // DebuggerExState stores information necessary for intercepting an exception
    DebuggerExState*    GetDebuggerState();

    // check to see if the current exception is interceptable
    BOOL                IsDebuggerInterceptable();
#endif // DEBUGGING_SUPPORTED

    EHClauseInfo*       GetCurrentEHClauseInfo();

#ifdef DACCESS_COMPILE
    void EnumChainMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif // DACCESS_COMPILE
    
    // After unwinding from an SO, there may be stale exception state.
    void ClearExceptionStateAfterSO(void* pStackFrameSP);

    enum ThreadExceptionFlag
    {
        TEF_None                          = 0x00000000,

        // Right now this flag is only used on WIN64.  We set this flag near the end of the second pass when we pop 
        // the ExceptionTracker for the current exception but before we actually resume execution.  It is unsafe
        // to start a funclet-skipping stackwalk in this time window.
        TEF_InconsistentExceptionState    = 0x00000001,

        TEF_ForeignExceptionRaise         = 0x00000002,
    };

    void SetThreadExceptionFlag(ThreadExceptionFlag flag);
    void ResetThreadExceptionFlag(ThreadExceptionFlag flag);
    BOOL HasThreadExceptionFlag(ThreadExceptionFlag flag);

    inline void SetRaisingForeignException()
    {
        LIMITED_METHOD_CONTRACT;
        SetThreadExceptionFlag(TEF_ForeignExceptionRaise);
    }

    inline BOOL IsRaisingForeignException()
    {
        LIMITED_METHOD_CONTRACT;
        return HasThreadExceptionFlag(TEF_ForeignExceptionRaise);
    }

    inline void ResetRaisingForeignException()
    {
        LIMITED_METHOD_CONTRACT;
        ResetThreadExceptionFlag(TEF_ForeignExceptionRaise);
    }

#if defined(_DEBUG)
    void AssertStackTraceInfo(StackTraceInfo *pSTI);
#endif // _debug

private:
    Thread* GetMyThread();

#ifdef WIN64EXCEPTIONS
    PTR_ExceptionTracker    m_pCurrentTracker;
    ExceptionTracker        m_OOMTracker;
public:
    PTR_ExceptionTracker    GetCurrentExceptionTracker()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pCurrentTracker;
    }
#else
    ExInfo                  m_currentExInfo;
public:
    PTR_ExInfo                 GetCurrentExceptionTracker()
    {
        LIMITED_METHOD_CONTRACT;
        return PTR_ExInfo(PTR_HOST_MEMBER_TADDR(ThreadExceptionState, this, m_currentExInfo));
    }
#endif

#ifdef FEATURE_CORRUPTING_EXCEPTIONS
private:
    CorruptionSeverity      m_LastActiveExceptionCorruptionSeverity;
    BOOL                    m_fCanReflectionTargetHandleException;

public:
    // Returns the corruption severity of the last active exception
    inline CorruptionSeverity GetLastActiveExceptionCorruptionSeverity()
    {
        LIMITED_METHOD_CONTRACT;

        return (CorruptionSeverity)GET_CORRUPTION_SEVERITY(m_LastActiveExceptionCorruptionSeverity);
    }

    // Set the corruption severity of the last active exception
    inline void SetLastActiveExceptionCorruptionSeverity(CorruptionSeverity severityToSet)
    {
        LIMITED_METHOD_CONTRACT;

        m_LastActiveExceptionCorruptionSeverity = severityToSet;
    }

    // Returns a bool indicating if the last active exception's corruption severity should 
    // be used when exception is reraised (e.g. Reflection Invocation, AD transition, etc)
    inline BOOL ShouldLastActiveExceptionCorruptionSeverityBeReused()
    {
        LIMITED_METHOD_CONTRACT;

        return CAN_REUSE_CORRUPTION_SEVERITY(m_LastActiveExceptionCorruptionSeverity);
    }

    // Returns a BOOL to indicate if reflection target can handle CSE or not.
    // This is used in DispatchInfo::CanIDispatchTargetHandleException.
    inline BOOL CanReflectionTargetHandleException()
    {
        LIMITED_METHOD_CONTRACT;

        return m_fCanReflectionTargetHandleException;
    }

    // Sets a BOOL indicate if the Reflection invocation target can handle exception or not.
    // Used in ReflectionInvocation.cpp.
    inline void SetCanReflectionTargetHandleException(BOOL fCanReflectionTargetHandleException)
    {
        LIMITED_METHOD_CONTRACT;

        m_fCanReflectionTargetHandleException = fCanReflectionTargetHandleException;
    }
#endif // FEATURE_CORRUPTING_EXCEPTIONS

private:   
    ThreadExceptionFlag      m_flag;

#ifndef FEATURE_PAL        
private:
    EHWatsonBucketTracker    m_UEWatsonBucketTracker;
public:
    PTR_EHWatsonBucketTracker GetUEWatsonBucketTracker()
    {
        LIMITED_METHOD_CONTRACT;
        return PTR_EHWatsonBucketTracker(PTR_HOST_MEMBER_TADDR(ThreadExceptionState, this, m_UEWatsonBucketTracker));
    }
#endif // !FEATURE_PAL        

private:

#ifndef WIN64EXCEPTIONS
    
    //
    // @NICE: Ideally, these friends shouldn't all be enumerated like this.  If they were all part of the same
    // class, that would be nice.  I'm trying to avoid adding x86-specific accessors to this class as well as
    // trying to limit the visibility of the ExInfo struct since Win64 doesn't use ExInfo.
    //
    friend EXCEPTION_DISPOSITION COMPlusAfterUnwind(
            EXCEPTION_RECORD *pExceptionRecord,
            EXCEPTION_REGISTRATION_RECORD *pEstablisherFrame,
            ThrowCallbackType& tct);
    friend EXCEPTION_DISPOSITION COMPlusAfterUnwind(
            EXCEPTION_RECORD *pExceptionRecord,
            EXCEPTION_REGISTRATION_RECORD *pEstablisherFrame,
            ThrowCallbackType& tct,
            Frame *pStartFrame);

    friend EXCEPTION_HANDLER_IMPL(COMPlusFrameHandler);

    friend EXCEPTION_DISPOSITION __cdecl
    CPFH_RealFirstPassHandler(EXCEPTION_RECORD *pExceptionRecord,
                              EXCEPTION_REGISTRATION_RECORD *pEstablisherFrame,
                              CONTEXT *pContext,
                              void *pDispatcherContext,
                              BOOL bAsynchronousThreadStop,
                              BOOL fPGCDisabledOnEntry);

    friend EXCEPTION_DISPOSITION __cdecl
    CPFH_UnwindHandler(EXCEPTION_RECORD *pExceptionRecord,
                       EXCEPTION_REGISTRATION_RECORD *pEstablisherFrame,
                       CONTEXT *pContext,
                       void *pDispatcherContext);
    
    friend void CPFH_UnwindFrames1(Thread* pThread,
                                   EXCEPTION_REGISTRATION_RECORD* pEstablisherFrame,
                                   DWORD exceptionCode);

#ifdef _TARGET_X86_
    friend LPVOID COMPlusEndCatchWorker(Thread * pThread);
#endif

    friend StackWalkAction COMPlusThrowCallback(CrawlFrame *pCf, ThrowCallbackType *pData);

    friend StackWalkAction COMPlusUnwindCallback(CrawlFrame *pCf, ThrowCallbackType *pData);

#if defined(_TARGET_X86_)
    friend void ResumeAtJitEH(CrawlFrame* pCf, BYTE* startPC, EE_ILEXCEPTION_CLAUSE *EHClausePtr, 
                                   DWORD nestingLevel, Thread *pThread, BOOL unwindStack);
#endif // _TARGET_X86_                                   

    friend _EXCEPTION_HANDLER_DECL(COMPlusNestedExceptionHandler);

    friend void COMPlusCooperativeTransitionHandler(Frame* pFrame);

    friend bool ShouldHandleManagedFault(
                        EXCEPTION_RECORD*               pExceptionRecord,
                        CONTEXT*                        pContext,
                        EXCEPTION_REGISTRATION_RECORD*  pEstablisherFrame,
                        Thread*                         pThread);

    friend class Thread;
    // It it the following method that needs to be a friend.  But the prototype pulls in a lot more stuff,
    //  so just make the Thread class a friend.
    // friend StackWalkAction Thread::StackWalkFramesEx(PREGDISPLAY pRD, PSTACKWALKFRAMESCALLBACK pCallback,
    //                 VOID *pData, unsigned flags, Frame *pStartFrame);

#endif // WIN64EXCEPTIONS

};


// <WARNING>
// This holder is not thread safe.
// </WARNING>
class ThreadExceptionFlagHolder
{
public:
    ThreadExceptionFlagHolder(ThreadExceptionState::ThreadExceptionFlag flag);
    ~ThreadExceptionFlagHolder();

private:
    ThreadExceptionState*                       m_pExState;
    ThreadExceptionState::ThreadExceptionFlag   m_flag;
};

extern BOOL IsWatsonEnabled();

#ifndef FEATURE_PAL
// This preprocessor definition is used to capture watson buckets
// at AppDomain transition boundary in END_DOMAIN_TRANSITION macro.
//
// This essentially copies the watson bucket details from the current exception tracker
// to the UE watson bucket tracker only if it is preallocated exception and NOT
// thread abort. For preallocated thread abort, UE Watson bucket tracker would already have the
// bucket details.
//
// It also captures buckets for non-preallocated exceptions (including non-preallocated threadabort) since
// the object would have the IP inside it.
#define CAPTURE_BUCKETS_AT_TRANSITION(pThread, oThrowable)                                                                                  \
        if (IsWatsonEnabled())                                                                                                              \
        {                                                                                                                                   \
            /* Switch to COOP mode */                                                                                                       \
            GCX_COOP();                                                                                                                     \
                                                                                                                                            \
            /* oThrowable is actually GET_THROWABLE macro; so extract the actual throwable for once and for all */                          \
            OBJECTREF throwable = oThrowable;                                                                                               \
            if (CLRException::IsPreallocatedExceptionObject(throwable))                                                                     \
            {                                                                                                                               \
                if (pThread->GetExceptionState()->GetCurrentExceptionTracker() != NULL)                                                     \
                {                                                                                                                           \
                    if (!IsThrowableThreadAbortException(throwable))                                                                        \
                    {                                                                                                                       \
                        PTR_EHWatsonBucketTracker pWatsonBucketTracker = GetWatsonBucketTrackerForPreallocatedException(throwable, FALSE);  \
                        if (pWatsonBucketTracker != NULL)                                                                                   \
                        {                                                                                                                   \
                            pThread->GetExceptionState()->GetUEWatsonBucketTracker()->CopyEHWatsonBucketTracker(*(pWatsonBucketTracker));   \
                            pThread->GetExceptionState()->GetUEWatsonBucketTracker()->CaptureUnhandledInfoForWatson(TypeOfReportedError::UnhandledException, pThread, NULL); \
                            DEBUG_STMT(pThread->GetExceptionState()->GetUEWatsonBucketTracker()->SetCapturedAtADTransition();)              \
                        }                                                                                                                   \
                    }                                                                                                                       \
                }                                                                                                                           \
            }                                                                                                                               \
            else                                                                                                                            \
            {                                                                                                                               \
                SetupWatsonBucketsForNonPreallocatedExceptions(throwable);                                                                  \
            }                                                                                                                               \
        }                                                                   
#else // !FEATURE_PAL
#define CAPTURE_BUCKETS_AT_TRANSITION(pThread, oThrowable)
#endif // FEATURE_PAL

#endif // __ExState_h__
