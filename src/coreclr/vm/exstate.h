// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//

#ifndef __ExState_h__
#define __ExState_h__

class ExceptionFlags;
class DebuggerExState;
class EHClauseInfo;

#include "exceptionhandling.h"
#include "cdacdata.h"

struct ExInfo;
typedef DPTR(ExInfo) PTR_ExInfo;

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
    // ExInfo
    friend class ProfToEEInterfaceImpl;

    friend struct ::cdac_data<Thread>;

    friend struct ExInfo;

public:

#ifdef _DEBUG
    typedef enum
    {
        STEC_All,
        STEC_CurrentTrackerEqualNullOkHackForFatalStackOverflow,
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

    ExceptionFlags*     GetFlags();

    ThreadExceptionState();
    ~ThreadExceptionState();

#ifdef DEBUGGING_SUPPORTED
    // DebuggerExState stores information necessary for intercepting an exception
    DebuggerExState*    GetDebuggerState();
    void SetDebuggerIndicatedFramePointer(LPVOID indicatedFramePointer);

    // check to see if the current exception is interceptable
    BOOL                IsDebuggerInterceptable();
#endif // DEBUGGING_SUPPORTED

    EHClauseInfo*       GetCurrentEHClauseInfo();

#ifdef DACCESS_COMPILE
    void EnumChainMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif // DACCESS_COMPILE

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

private:
    Thread* GetMyThread();

    PTR_ExInfo m_pCurrentTracker;
public:
    PTR_ExInfo GetCurrentExceptionTracker()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pCurrentTracker;
    }

#ifndef DACCESS_COMPILE
    void SetCurrentExceptionTracker(PTR_ExInfo pTracker)
    {
        LIMITED_METHOD_CONTRACT;
        m_pCurrentTracker = pTracker;
    }
#endif // DACCESS_COMPILE

private:
    ThreadExceptionFlag      m_flag;

#ifndef TARGET_UNIX
private:
    EHWatsonBucketTracker    m_UEWatsonBucketTracker;
public:
    PTR_EHWatsonBucketTracker GetUEWatsonBucketTracker()
    {
        LIMITED_METHOD_CONTRACT;
        return PTR_EHWatsonBucketTracker(PTR_HOST_MEMBER_TADDR(ThreadExceptionState, this, m_UEWatsonBucketTracker));
    }
#endif // !TARGET_UNIX
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

#endif // __ExState_h__
