// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//


#ifndef __ExInfo_h__
#define __ExInfo_h__
#if !defined(FEATURE_EH_FUNCLETS)

#include "exstatecommon.h"

typedef DPTR(class ExInfo) PTR_ExInfo;
class ExInfo
{
    friend class ThreadExceptionState;
    friend class ClrDataExceptionState;

public:

    BOOL    IsHeapAllocated()
    {
        LIMITED_METHOD_CONTRACT;
        return m_StackAddress != (void *) this;
    }

    void CopyAndClearSource(ExInfo *from);

    void UnwindExInfo(VOID* limit);

    // Q: Why does this thing take an EXCEPTION_RECORD rather than an ExceptionCode?
    // A: Because m_ExceptionCode and Ex_WasThrownByUs have to be kept
    //    in sync and this function needs the exception parms inside the record to figure
    //    out the "IsTagged" part.
    void SetExceptionCode(const EXCEPTION_RECORD *pCER);

    DWORD GetExceptionCode()
    {
        LIMITED_METHOD_CONTRACT;
        return m_ExceptionCode;
    }

public:  // @TODO: make more of these private!
    // Note: the debugger assumes that m_pThrowable is a strong
    // reference so it can check it for NULL with preemptive GC
    // enabled.
    OBJECTHANDLE    m_hThrowable;       // thrown exception
    PTR_Frame       m_pSearchBoundary;  // topmost frame for current managed frame group
private:
    DWORD           m_ExceptionCode;    // After a catch of a COM+ exception, pointers/context are trashed.
public:
    PTR_EXCEPTION_REGISTRATION_RECORD m_pBottomMostHandler; // most recent EH record registered

    // Reference to the topmost handler we saw during an SO that goes past us
    PTR_EXCEPTION_REGISTRATION_RECORD m_pTopMostHandlerDuringSO;

    LPVOID              m_dEsp;             // Esp when  fault occurred, OR esp to restore on endcatch

    StackTraceInfo      m_StackTraceInfo;

    PTR_ExInfo          m_pPrevNestedInfo;  // pointer to nested info if are handling nested exception

    size_t*             m_pShadowSP;        // Zero this after endcatch

    PTR_EXCEPTION_RECORD    m_pExceptionRecord;
    PTR_EXCEPTION_POINTERS  m_pExceptionPointers;
    PTR_CONTEXT             m_pContext;

    // We have a rare case where (re-entry to the EE from an unmanaged filter) where we
    // need to create a new ExInfo ... but don't have a nested handler for it.  The handlers
    // use stack addresses to figure out their correct lifetimes.  This stack location is
    // used for that.  For most records, it will be the stack address of the ExInfo ... but
    // for some records, it will be a pseudo stack location -- the place where we think
    // the record should have been (except for the re-entry case).
    //
    //
    //
    void* m_StackAddress; // A pseudo or real stack location for this record.

#ifndef TARGET_UNIX
private:
    EHWatsonBucketTracker m_WatsonBucketTracker;
public:
    inline PTR_EHWatsonBucketTracker GetWatsonBucketTracker()
    {
        LIMITED_METHOD_CONTRACT;
        return PTR_EHWatsonBucketTracker(PTR_HOST_MEMBER_TADDR(ExInfo, this, m_WatsonBucketTracker));
    }
#endif

private:
    BOOL                    m_fDeliveredFirstChanceNotification;
public:
    inline BOOL DeliveredFirstChanceNotification()
    {
        LIMITED_METHOD_CONTRACT;

        return m_fDeliveredFirstChanceNotification;
    }

    inline void SetFirstChanceNotificationStatus(BOOL fDelivered)
    {
        LIMITED_METHOD_CONTRACT;

        m_fDeliveredFirstChanceNotification = fDelivered;
    }

    // Returns the exception tracker previous to the current
    inline PTR_ExInfo GetPreviousExceptionTracker()
    {
        LIMITED_METHOD_CONTRACT;

        return m_pPrevNestedInfo;
    }

    // Returns the throwable associated with the tracker
    inline OBJECTREF GetThrowable()
    {
        LIMITED_METHOD_CONTRACT;

        return (m_hThrowable != NULL)?ObjectFromHandle(m_hThrowable):NULL;
    }

    // Returns the throwble associated with the tracker as handle
    inline OBJECTHANDLE GetThrowableAsHandle()
    {
        LIMITED_METHOD_CONTRACT;

        return m_hThrowable;
    }

public:

    DebuggerExState     m_DebuggerExState;
    EHClauseInfo        m_EHClauseInfo;
    ExceptionFlags      m_ExceptionFlags;

#if defined(TARGET_X86) && defined(DEBUGGING_SUPPORTED)
    EHContext           m_InterceptionContext;
    BOOL                m_ValidInterceptionContext;
#endif

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

    void Init();
    ExInfo() DAC_EMPTY();

    void DestroyExceptionHandle();

private:
    // Don't allow this
    ExInfo& operator=(const ExInfo &from);
};

#if defined(TARGET_X86)
PTR_ExInfo GetEHTrackerForPreallocatedException(OBJECTREF oPreAllocThrowable, PTR_ExInfo pStartingEHTracker);
#endif // TARGET_X86

#else // !FEATURE_EH_FUNCLETS

#include "exceptionhandling.h"

enum RhEHClauseKind
{
    RH_EH_CLAUSE_TYPED = 0,
    RH_EH_CLAUSE_FAULT = 1,
    RH_EH_CLAUSE_FILTER = 2,
    RH_EH_CLAUSE_UNUSED = 3,
};

struct RhEHClause
{
    RhEHClauseKind _clauseKind;
    unsigned _tryStartOffset;
    unsigned _tryEndOffset;
    BYTE *_filterAddress;
    BYTE *_handlerAddress;
    void *_pTargetType;
    BOOL _isSameTry;
};

enum class ExKind : uint8_t
{
    None = 0,
    Throw = 1,
    HardwareFault = 2,
    KindMask = 3,

    RethrowFlag = 4,

    SupersededFlag = 8,

    InstructionFaultFlag = 0x10
};

struct PAL_SEHException;

struct ExInfo : public ExceptionTrackerBase
{
    ExInfo(Thread *pThread, EXCEPTION_RECORD *pExceptionRecord, CONTEXT *pExceptionContext, ExKind exceptionKind);

    // Releases all the resources owned by the ExInfo
    void ReleaseResources();

    // Make debugger and profiler callbacks before and after an exception handler (catch, finally, filter) is called
    void MakeCallbacksRelatedToHandler(
        bool fBeforeCallingHandler,
        Thread*                pThread,
        MethodDesc*            pMD,
        EE_ILEXCEPTION_CLAUSE* pEHClause,
        DWORD_PTR              dwHandlerStartPC,
        StackFrame             sf);

    static void PopExInfos(Thread *pThread, void *targetSp);

    // Padding to make the ExInfo offsets that the managed EH code needs to access
    // the same for debug / release and Unix / Windows.
#ifdef TARGET_UNIX
    // sizeof(EHWatsonBucketTracker)
    BYTE m_padding[2 * sizeof(void*) + sizeof(DWORD)];
#else // TARGET_UNIX
#ifndef _DEBUG
    //  sizeof(EHWatsonBucketTracker::m_DebugFlags)
    BYTE m_padding[sizeof(DWORD)];
#endif // _DEBUG
#endif // TARGET_UNIX

    // Context used by the stack frame iterator
    CONTEXT* m_pExContext;
    // actual exception object reference
    OBJECTREF m_exception;
    // Kind of the exception (software, hardware, rethrown)
    ExKind m_kind;
    // Exception handling pass (1 or 2)
    uint8_t m_passNumber;
    // Index of the current exception handling clause
    uint32_t m_idxCurClause;
    // Stack frame iterator used to walk stack frames while handling the exception
    StackFrameIterator m_frameIter;
    volatile size_t m_notifyDebuggerSP;
    // Initial explicit frame
    Frame* m_pFrame;

    // Stack frame of the caller of the currently running exception handling clause (catch, finally, filter)
    CallerStackFrame    m_csfEHClause;
    // Stack frame of the caller of the code that encloses the currently running exception handling clause
    CallerStackFrame    m_csfEnclosingClause;
    // Stack frame of the caller of the catch handler
    StackFrame          m_sfCallerOfActualHandlerFrame;
    // The exception handling clause for the catch handler that was identified during pass 1
    EE_ILEXCEPTION_CLAUSE m_ClauseForCatch;

#ifdef TARGET_UNIX
    // Set to TRUE to take ownership of the EXCEPTION_RECORD and CONTEXT_RECORD in the m_ptrs. When set, the
    // memory of those records is freed using PAL_FreeExceptionRecords when the ExInfo is destroyed.
    BOOL m_fOwnsExceptionPointers;
    // Exception propagation callback and context for ObjectiveC exception propagation support
    void(*m_propagateExceptionCallback)(void* context);
    void *m_propagateExceptionContext;
#endif // TARGET_UNIX

    // The following fields are for profiler / debugger use only
    EE_ILEXCEPTION_CLAUSE m_CurrentClause;
    // Method to report to the debugger / profiler when stack frame iterator leaves a frame
    MethodDesc    *m_pMDToReportFunctionLeave;
    // CONTEXT and REGDISPLAY used by the StackFrameIterator for stack walking
    CONTEXT        m_exContext;
    REGDISPLAY     m_regDisplay;

#if defined(TARGET_UNIX)
    void TakeExceptionPointersOwnership(PAL_SEHException* ex);
#endif // TARGET_UNIX

};

#endif // !FEATURE_EH_FUNCLETS
#endif // __ExInfo_h__
