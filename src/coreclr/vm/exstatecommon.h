// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//

#ifndef __ExStateCommon_h__
#define __ExStateCommon_h__

#include "stackframe.h"

class ExceptionFlags;

#ifdef DEBUGGING_SUPPORTED
//---------------------------------------------------------------------------------------
//
// This class stores information necessary to intercept an exception.  It's basically a communication channel
// between the debugger and the EH subsystem.  Each internal exception tracking structure
// (ExInfo on x86 and ExceptionTracker on WIN64) contains one DebuggerExState.
//
// Notes:
//    This class actually stores more information on x86 than on WIN64 because the x86 EH subsystem
//    has more work to do when unwinding the stack.  WIN64 just asks the OS to do it.
//

class DebuggerExState
{
public:

    //---------------------------------------------------------------------------------------
    //
    // constructor
    //

    DebuggerExState()
    {
        Init();
    }

    //---------------------------------------------------------------------------------------
    //
    // This function is simply used to initialize all the fields in the DebuggerExState.
    //

    void Init()
    {
        m_sfDebuggerIndicatedFramePointer = StackFrame();
        m_pDebuggerInterceptFunc = NULL;
        m_sfDebuggerInterceptFramePointer = StackFrame();
        m_pDebuggerContext = NULL;
        m_pDebuggerInterceptNativeOffset = 0;

  #ifndef FEATURE_EH_FUNCLETS
        // x86-specific fields
        m_pDebuggerInterceptFrame = EXCEPTION_CHAIN_END;
  #endif // !FEATURE_EH_FUNCLETS
        m_dDebuggerInterceptHandlerDepth  = 0;
    }

    //---------------------------------------------------------------------------------------
    //
    // Retrieves the opaque token stored by the debugger.
    //
    // Return Value:
    //    the stored opaque token for the debugger
    //

    void* GetDebuggerInterceptContext()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pDebuggerContext;
    }

    //---------------------------------------------------------------------------------------
    //
    // Stores an opaque token which is only used by the debugger.
    //
    // Arguments:
    //    pContext - the token to be stored
    //

    void SetDebuggerInterceptContext(void* pContext)
    {
        LIMITED_METHOD_CONTRACT;
        m_pDebuggerContext = pContext;
    }

    //---------------------------------------------------------------------------------------
    //
    // Marks the current stack frame visited by the EH subsystem during the first pass.
    // This marker moves closer to the root of the stack while each stack frame is examined in the first pass.
    // This continues until the end of the first pass.
    //
    // Arguments:
    //    stackPointer  - SP of the current stack frame
    //    bStorePointer - BSP of the current stack frame
    //

    void SetDebuggerIndicatedFramePointer(void* stackPointer)
    {
        LIMITED_METHOD_CONTRACT;
        m_sfDebuggerIndicatedFramePointer = StackFrame((UINT_PTR)stackPointer);
    }

    // This function stores the information necessary to intercept an exception in the DebuggerExState.
    BOOL SetDebuggerInterceptInfo(IJitManager *pJitManager,
                                  Thread *pThread,
                                  const METHODTOKEN& methodToken,
                                  MethodDesc *pMethDesc,
                                  ULONG_PTR natOffset,
                                  StackFrame sfDebuggerInterceptFramePointer,
                                  ExceptionFlags* pFlags);

    //---------------------------------------------------------------------------------------
    //
    // This function is basically just a getter to retrieve the information stored on the DebuggerExState.
    // Refer to the comments for individual fields for more information.
    //
    // Arguments:
    //    pEstablisherFrame - m_pDebuggerInterceptFrame
    //    ppFunc            - m_pDebuggerInterceptFunc
    //    pdHandler         - m_dDebuggerInterceptHandlerDepth
    //    ppStack           - the SP of m_sfDebuggerInterceptFramePointer
    //    ppBStore          - the BSP of m_sfDebuggerInterceptFramePointer
    //    pNativeOffset     - m_pDebuggerInterceptNativeOffset;
    //    ppFrame           - always set to NULL
    //
    // Notes:
    //    Everything is an out parameter.
    //
    //    Apparently ppFrame is actually used on x86 to set tct.pBottomFrame to NULL.
    //

    void GetDebuggerInterceptInfo(
 #ifndef FEATURE_EH_FUNCLETS
                                  PEXCEPTION_REGISTRATION_RECORD *pEstablisherFrame,
 #endif // !FEATURE_EH_FUNCLETS
                                  MethodDesc **ppFunc,
                                  int *pdHandler,
                                  BYTE **ppStack,
                                  ULONG_PTR *pNativeOffset,
                                  Frame **ppFrame)
    {
        LIMITED_METHOD_CONTRACT;

#ifndef FEATURE_EH_FUNCLETS
        if (pEstablisherFrame != NULL)
        {
            *pEstablisherFrame = m_pDebuggerInterceptFrame;
        }
#endif // !FEATURE_EH_FUNCLETS

        if (ppFunc != NULL)
        {
            *ppFunc = m_pDebuggerInterceptFunc;
        }

        if (pdHandler != NULL)
        {
            *pdHandler = m_dDebuggerInterceptHandlerDepth;
        }

        if (ppStack != NULL)
        {
            *ppStack = (BYTE *)m_sfDebuggerInterceptFramePointer.SP;
        }

        if (pNativeOffset != NULL)
        {
            *pNativeOffset = m_pDebuggerInterceptNativeOffset;
        }

        if (ppFrame != NULL)
        {
            *ppFrame = NULL;
        }
    }

private:
    // This frame pointer marks the latest stack frame examined by the EH subsystem in the first pass.
    // An exception cannot be intercepted closer to the root than this frame pointer.
    StackFrame      m_sfDebuggerIndicatedFramePointer;

    // the method in which we are going to resume execution
    MethodDesc*     m_pDebuggerInterceptFunc;

    // the frame pointer of the stack frame where we are intercepting the exception
    StackFrame      m_sfDebuggerInterceptFramePointer;

    // opaque token used by the debugger
    void*           m_pDebuggerContext;

    // the native offset at which to resume execution
    ULONG_PTR       m_pDebuggerInterceptNativeOffset;

    // The remaining fields are only used on x86.
#ifndef FEATURE_EH_FUNCLETS
    // the exception registration record covering the stack range containing the interception point
    PEXCEPTION_REGISTRATION_RECORD m_pDebuggerInterceptFrame;
#endif // !FEATURE_EH_FUNCLETS

    // the nesting level at which we want to resume execution
    int             m_dDebuggerInterceptHandlerDepth;
};
#endif // DEBUGGING_SUPPORTED

class EHClauseInfo
{
public:
    EHClauseInfo()
    {
        LIMITED_METHOD_CONTRACT;

        // For the profiler, other clause fields are not valid if m_ClauseType is COR_PRF_CLAUSE_NONE.
        m_ClauseType           = COR_PRF_CLAUSE_NONE;
        m_IPForEHClause        = 0;
        m_sfForEHClause.Clear();
        m_csfEHClause.Clear();
        m_fManagedCodeEntered  = FALSE;
    }

    void SetEHClauseType(COR_PRF_CLAUSE_TYPE EHClauseType)
    {
        LIMITED_METHOD_CONTRACT;
        m_ClauseType = EHClauseType;
    }

    void SetInfo(COR_PRF_CLAUSE_TYPE EHClauseType,
                 UINT_PTR            uIPForEHClause,
                 StackFrame          sfForEHClause)
    {
        LIMITED_METHOD_CONTRACT;

        m_ClauseType    = EHClauseType;
        m_IPForEHClause = uIPForEHClause;
        m_sfForEHClause = sfForEHClause;
    }

    void ResetInfo()
    {
        LIMITED_METHOD_CONTRACT;

        // For the profiler, other clause fields are not valid if m_ClauseType is COR_PRF_CLAUSE_NONE.
        m_ClauseType           = COR_PRF_CLAUSE_NONE;
        m_IPForEHClause        = 0;
        m_sfForEHClause.Clear();
        m_csfEHClause.Clear();
    }

    void SetManagedCodeEntered(BOOL fEntered)
    {
        LIMITED_METHOD_CONTRACT;
        m_fManagedCodeEntered = fEntered;
    }

    void SetCallerStackFrame(CallerStackFrame csfEHClause)
    {
        LIMITED_METHOD_CONTRACT;
        m_csfEHClause = csfEHClause;
    }

    COR_PRF_CLAUSE_TYPE GetClauseType()     { LIMITED_METHOD_CONTRACT; return m_ClauseType;           }

    UINT_PTR GetIPForEHClause()             { LIMITED_METHOD_CONTRACT; return m_IPForEHClause;        }
    UINT_PTR GetFramePointerForEHClause()   { LIMITED_METHOD_CONTRACT; return m_sfForEHClause.SP;     }

    BOOL     IsManagedCodeEntered()         { LIMITED_METHOD_CONTRACT; return m_fManagedCodeEntered;  }

    StackFrame GetStackFrameForEHClause()            { LIMITED_METHOD_CONTRACT; return m_sfForEHClause; }
    CallerStackFrame GetCallerStackFrameForEHClause(){ LIMITED_METHOD_CONTRACT; return m_csfEHClause;   }

    // On some platforms, we make the call to the funclets via an assembly helper. The reference to the field
    // containing the stack pointer is passed to the assembly helper so that it can update
    // it with correct SP value once its prolog has executed.
    //
    // This method is used to get the field reference
    CallerStackFrame* GetCallerStackFrameForEHClauseReference()
    {
        LIMITED_METHOD_CONTRACT;
        return &m_csfEHClause;
    }

private:
    UINT_PTR         m_IPForEHClause;   // the entry point of the current notified exception clause
    StackFrame       m_sfForEHClause;   // the associated frame pointer of the current notified exception clause
    CallerStackFrame m_csfEHClause;     // the caller SP of the funclet; only used on WIN64

    COR_PRF_CLAUSE_TYPE m_ClauseType;   // this has a value from COR_PRF_CLAUSE_TYPE while an exception notification is pending
    BOOL m_fManagedCodeEntered;         // this flag indicates that we have called the managed code for the current EH clause
};

class ExceptionFlags
{
public:
    ExceptionFlags()
    {
        Init();
    }

#if defined(FEATURE_EH_FUNCLETS)
    ExceptionFlags(bool fReadOnly)
    {
        Init();
#ifdef _DEBUG
        if (fReadOnly)
        {
            m_flags |= Ex_FlagsAreReadOnly;
            m_debugFlags |= Ex_FlagsAreReadOnly;
        }
#endif // _DEBUG
    }
#endif // defined(FEATURE_EH_FUNCLETS)

    void AssertIfReadOnly()
    {
        SUPPORTS_DAC;

#if defined(FEATURE_EH_FUNCLETS) && defined(_DEBUG)
        if ((m_flags & Ex_FlagsAreReadOnly) || (m_debugFlags & Ex_FlagsAreReadOnly))
        {
            _ASSERTE(!"Tried to update read-only flags!");
        }
#endif // defined(FEATURE_EH_FUNCLETS) && defined(_DEBUG)
    }

    void Init()
    {
        m_flags = 0;
#ifdef _DEBUG
        m_debugFlags = 0;
#endif // _DEBUG
    }

    BOOL IsRethrown()      { LIMITED_METHOD_CONTRACT; return m_flags & Ex_IsRethrown; }
    void SetIsRethrown()   { LIMITED_METHOD_CONTRACT; AssertIfReadOnly(); m_flags |= Ex_IsRethrown; }
    void ResetIsRethrown() { LIMITED_METHOD_CONTRACT; AssertIfReadOnly(); m_flags &= ~Ex_IsRethrown; }

    BOOL UnwindHasStarted()      { LIMITED_METHOD_CONTRACT; return m_flags & Ex_UnwindHasStarted; }
    void SetUnwindHasStarted()   { LIMITED_METHOD_CONTRACT; AssertIfReadOnly(); m_flags |= Ex_UnwindHasStarted; }
    void ResetUnwindHasStarted() { LIMITED_METHOD_CONTRACT; AssertIfReadOnly(); m_flags &= ~Ex_UnwindHasStarted; }

    BOOL UnwindingToFindResumeFrame()      { LIMITED_METHOD_CONTRACT; return m_flags & Ex_UnwindingToFindResumeFrame; }
    void SetUnwindingToFindResumeFrame()   { LIMITED_METHOD_CONTRACT; AssertIfReadOnly(); m_flags |= Ex_UnwindingToFindResumeFrame; }
    void ResetUnwindingToFindResumeFrame() { LIMITED_METHOD_CONTRACT; AssertIfReadOnly(); m_flags &= ~Ex_UnwindingToFindResumeFrame; }

    BOOL UseExInfoForStackwalk()      { LIMITED_METHOD_DAC_CONTRACT; return m_flags & Ex_UseExInfoForStackwalk; }
    void SetUseExInfoForStackwalk()   { LIMITED_METHOD_DAC_CONTRACT; AssertIfReadOnly(); m_flags |= Ex_UseExInfoForStackwalk; }
    void ResetUseExInfoForStackwalk() { LIMITED_METHOD_DAC_CONTRACT; AssertIfReadOnly(); m_flags &= ~Ex_UseExInfoForStackwalk; }

#ifdef _DEBUG
    BOOL ReversePInvokeEscapingException()      { LIMITED_METHOD_DAC_CONTRACT; return m_debugFlags & Ex_RPInvokeEscapingException; }
    void SetReversePInvokeEscapingException()   { LIMITED_METHOD_DAC_CONTRACT; AssertIfReadOnly(); m_debugFlags |= Ex_RPInvokeEscapingException; }
    void ResetReversePInvokeEscapingException() { LIMITED_METHOD_DAC_CONTRACT; AssertIfReadOnly(); m_debugFlags &= ~Ex_RPInvokeEscapingException; }
#endif // _DEBUG

#ifdef DEBUGGING_SUPPORTED
    BOOL SentDebugUserFirstChance()    { LIMITED_METHOD_CONTRACT; return m_flags & Ex_SentDebugUserFirstChance; }
    void SetSentDebugUserFirstChance() { LIMITED_METHOD_CONTRACT; AssertIfReadOnly(); m_flags |= Ex_SentDebugUserFirstChance; }

    BOOL SentDebugFirstChance()    { LIMITED_METHOD_CONTRACT; return m_flags & Ex_SentDebugFirstChance; }
    void SetSentDebugFirstChance() { LIMITED_METHOD_CONTRACT; AssertIfReadOnly(); m_flags |= Ex_SentDebugFirstChance; }

    BOOL SentDebugUnwindBegin()    { LIMITED_METHOD_CONTRACT; return m_flags & Ex_SentDebugUnwindBegin; }
    void SetSentDebugUnwindBegin() { LIMITED_METHOD_CONTRACT; AssertIfReadOnly(); m_flags |= Ex_SentDebugUnwindBegin; }

    BOOL DebugCatchHandlerFound()    { LIMITED_METHOD_CONTRACT; return m_flags & Ex_DebugCatchHandlerFound; }
    void SetDebugCatchHandlerFound() { LIMITED_METHOD_CONTRACT; AssertIfReadOnly(); m_flags |= Ex_DebugCatchHandlerFound; }

    BOOL SentDebugUnhandled()    { LIMITED_METHOD_CONTRACT; return m_flags & Ex_SentDebugUnhandled; }
    void SetSentDebugUnhandled() { LIMITED_METHOD_CONTRACT; AssertIfReadOnly(); m_flags |= Ex_SentDebugUnhandled; }

    BOOL IsUnhandled()    { LIMITED_METHOD_CONTRACT; return m_flags & Ex_IsUnhandled; }
    void SetUnhandled() { LIMITED_METHOD_CONTRACT; AssertIfReadOnly(); m_flags |= Ex_IsUnhandled; }

    BOOL DebuggerInterceptNotPossible()    { LIMITED_METHOD_CONTRACT; return m_flags & Ex_DebuggerInterceptNotPossible; }
    void SetDebuggerInterceptNotPossible() { LIMITED_METHOD_CONTRACT; AssertIfReadOnly(); m_flags |= Ex_DebuggerInterceptNotPossible; }

    BOOL DebuggerInterceptInfo()    { LIMITED_METHOD_DAC_CONTRACT; return m_flags & Ex_DebuggerInterceptInfo; }
    void SetDebuggerInterceptInfo() { LIMITED_METHOD_DAC_CONTRACT; AssertIfReadOnly(); m_flags |= Ex_DebuggerInterceptInfo; }
#endif

    BOOL WasThrownByUs()      { LIMITED_METHOD_CONTRACT; return m_flags & Ex_WasThrownByUs; }
    void SetWasThrownByUs()   { LIMITED_METHOD_CONTRACT; AssertIfReadOnly(); m_flags |= Ex_WasThrownByUs; }
    void ResetWasThrownByUs() { LIMITED_METHOD_CONTRACT; AssertIfReadOnly(); m_flags &= ~Ex_WasThrownByUs; }

    BOOL GotWatsonBucketDetails()      { LIMITED_METHOD_CONTRACT; return m_flags & Ex_GotWatsonBucketInfo; }
    void SetGotWatsonBucketDetails()   { LIMITED_METHOD_CONTRACT; AssertIfReadOnly(); m_flags |= Ex_GotWatsonBucketInfo; }
    void ResetGotWatsonBucketDetails() { LIMITED_METHOD_CONTRACT; AssertIfReadOnly(); m_flags &= ~Ex_GotWatsonBucketInfo; }

private:
    enum
    {
        Ex_IsRethrown                   = 0x00000001,
        Ex_UnwindingToFindResumeFrame   = 0x00000002,
        Ex_UnwindHasStarted             = 0x00000004,
        Ex_UseExInfoForStackwalk        = 0x00000008,        // Use this ExInfo to unwind a fault (AV, zerodiv) back to managed code?

#ifdef DEBUGGING_SUPPORTED
        Ex_SentDebugUserFirstChance     = 0x00000010,
        Ex_SentDebugFirstChance         = 0x00000020,
        Ex_SentDebugUnwindBegin         = 0x00000040,
        Ex_DebugCatchHandlerFound       = 0x00000080,
        Ex_SentDebugUnhandled           = 0x00000100,
        Ex_DebuggerInterceptInfo        = 0x00000200,
        Ex_DebuggerInterceptNotPossible = 0x00000400,
        Ex_IsUnhandled                  = 0x00000800,
#endif
        // Unused                       = 0x00001000,

        Ex_WasThrownByUs                = 0x00002000,

        Ex_GotWatsonBucketInfo          = 0x00004000,

#if defined(FEATURE_EH_FUNCLETS) && defined(_DEBUG)
        Ex_FlagsAreReadOnly             = 0x80000000
#endif // defined(FEATURE_EH_FUNCLETS) && defined(_DEBUG)

    };

    UINT32 m_flags;

#ifdef _DEBUG
    enum
    {
        Ex_RPInvokeEscapingException    = 0x40000000
    };
    UINT32 m_debugFlags;
#endif // _DEBUG
};

//------------------------------------------------------------------------------
// Error reporting (unhandled exception, fatal error, user breakpoint
class TypeOfReportedError
{
public:
    enum Type {INVALID, UnhandledException, FatalError, UserBreakpoint, NativeThreadUnhandledException, NativeBreakpoint, StackOverflowException};

    TypeOfReportedError(Type t) : m_type(t) {}

    BOOL IsUnhandledException() { LIMITED_METHOD_CONTRACT; return (m_type == UnhandledException) || (m_type == NativeThreadUnhandledException) || (m_type == StackOverflowException); }
    BOOL IsFatalError() { return (m_type == FatalError); }
    BOOL IsUserBreakpoint() {return (m_type == UserBreakpoint); }
    BOOL IsBreakpoint() {return (m_type == UserBreakpoint) || (m_type == NativeBreakpoint); }
    BOOL IsException() { LIMITED_METHOD_CONTRACT; return IsUnhandledException() || (m_type == NativeBreakpoint) || (m_type == StackOverflowException); }

    Type GetType() { return m_type; }
    void SetType(Type t) { m_type = t; }

private:
    Type m_type;
};


#ifndef TARGET_UNIX
// This class is used to track Watson bucketing information for an exception.
typedef DPTR(class EHWatsonBucketTracker) PTR_EHWatsonBucketTracker;
class EHWatsonBucketTracker
{
private:
    struct
    {
        PTR_VOID m_pUnhandledBuckets;
        UINT_PTR    m_UnhandledIp;
    } m_WatsonUnhandledInfo;

#ifdef _DEBUG
    enum
    {
        // Bucket details were captured for ThreadAbort
        Wb_CapturedForThreadAbort = 1,

        // Bucket details were captured at AD Transition
        Wb_CapturedAtADTransition = 2,

        // Bucket details were captured during Reflection invocation
        Wb_CapturedAtReflectionInvocation = 4
    };

    DWORD m_DebugFlags;
#endif // _DEBUG

public:
   EHWatsonBucketTracker();
   void Init();
   void CopyEHWatsonBucketTracker(const EHWatsonBucketTracker& srcTracker);
   void CopyBuckets(U1ARRAYREF oBuckets);
   void SaveIpForWatsonBucket(UINT_PTR ip);
   UINT_PTR RetrieveWatsonBucketIp();
   PTR_VOID RetrieveWatsonBuckets();
   void ClearWatsonBucketDetails();
   void CaptureUnhandledInfoForWatson(TypeOfReportedError tore, Thread * pThread, OBJECTREF * pThrowable);

#ifdef _DEBUG
    void ResetFlags()                  { LIMITED_METHOD_CONTRACT; m_DebugFlags = 0; }
    BOOL CapturedForThreadAbort()      { LIMITED_METHOD_CONTRACT; return m_DebugFlags & Wb_CapturedForThreadAbort; }
    void SetCapturedForThreadAbort()   { LIMITED_METHOD_CONTRACT; m_DebugFlags |= Wb_CapturedForThreadAbort; }
    void ResetCapturedForThreadAbort() { LIMITED_METHOD_CONTRACT; m_DebugFlags &= ~Wb_CapturedForThreadAbort; }

    BOOL CapturedAtADTransition()      { LIMITED_METHOD_CONTRACT; return m_DebugFlags & Wb_CapturedAtADTransition; }
    void SetCapturedAtADTransition()   { LIMITED_METHOD_CONTRACT; m_DebugFlags |= Wb_CapturedAtADTransition; }
    void ResetCapturedAtADTransition() { LIMITED_METHOD_CONTRACT; m_DebugFlags &= ~Wb_CapturedAtADTransition; }

    BOOL CapturedAtReflectionInvocation()      { LIMITED_METHOD_CONTRACT; return m_DebugFlags & Wb_CapturedAtReflectionInvocation; }
    void SetCapturedAtReflectionInvocation()   { LIMITED_METHOD_CONTRACT; m_DebugFlags |= Wb_CapturedAtReflectionInvocation; }
    void ResetCapturedAtReflectionInvocation() { LIMITED_METHOD_CONTRACT; m_DebugFlags &= ~Wb_CapturedAtReflectionInvocation; }
#endif // _DEBUG
};

void SetStateForWatsonBucketing(BOOL fIsRethrownException, OBJECTHANDLE ohOriginalException);
BOOL CopyWatsonBucketsToThrowable(PTR_VOID pUnmanagedBuckets, OBJECTREF oTargetThrowable = NULL);
void CopyWatsonBucketsFromThrowableToCurrentThrowable(U1ARRAYREF oManagedWatsonBuckets);
void CopyWatsonBucketsBetweenThrowables(U1ARRAYREF oManagedWatsonBuckets, OBJECTREF oThrowableTo = NULL);
void SetupInitialThrowBucketDetails(UINT_PTR adjustedIp);
BOOL SetupWatsonBucketsForFailFast(EXCEPTIONREF refException);
void SetupWatsonBucketsForUEF(BOOL fUseLastThrownObject);
BOOL SetupWatsonBucketsForEscapingPreallocatedExceptions();
BOOL SetupWatsonBucketsForNonPreallocatedExceptions(OBJECTREF oThrowable = NULL);
PTR_EHWatsonBucketTracker GetWatsonBucketTrackerForPreallocatedException(OBJECTREF oPreAllocThrowable, BOOL fCaptureBucketsIfNotPresent,
                                                                         BOOL fStartSearchFromPreviousTracker = FALSE);
BOOL IsThrowableThreadAbortException(OBJECTREF oThrowable);
#endif // !TARGET_UNIX

#endif // __ExStateCommon_h__
