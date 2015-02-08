//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//

//

#ifndef __EXCEPTION_HANDLING_h__
#define __EXCEPTION_HANDLING_h__

#ifdef WIN64EXCEPTIONS

// This address lies in the NULL pointer partition of the process memory.
// Accessing it will result in AV.
#define INVALID_RESUME_ADDRESS 0x000000000000bad0

#include "exstatecommon.h"

LONG WINAPI CLRVectoredExceptionHandlerShim(PEXCEPTION_POINTERS pExceptionInfo);

EXTERN_C EXCEPTION_DISPOSITION
ProcessCLRException(IN     PEXCEPTION_RECORD     pExceptionRecord
          WIN64_ARG(IN     ULONG64               MemoryStackFp)
      NOT_WIN64_ARG(IN     ULONG                 MemoryStackFp),
                    IN OUT PT_CONTEXT            pContextRecord,
                    IN OUT PT_DISPATCHER_CONTEXT pDispatcherContext);


void __declspec(noinline)
ClrUnwindEx(EXCEPTION_RECORD* pExceptionRecord,
                 UINT_PTR          ReturnValue,
                 UINT_PTR          TargetIP,
                 UINT_PTR          TargetFrameSp);

typedef DWORD_PTR   (HandlerFn)(UINT_PTR uStackFrame, Object* pExceptionObj);

enum CLRUnwindStatus { UnwindPending, FirstPassComplete, SecondPassComplete };

enum TrackerMemoryType
{
    memManaged = 0x0001,
    memUnmanaged = 0x0002,
    memBoth = 0x0003,
};

// Enum that specifies the type of EH funclet we are about to invoke
enum EHFuncletType
{
    Filter = 0x0001,
    FaultFinally = 0x0002,
    Catch = 0x0004,
};

typedef DPTR(class ExceptionTracker) PTR_ExceptionTracker;
class ExceptionTracker
{
    friend class TrackerAllocator;
    friend class ThreadExceptionState;
    friend class ClrDataExceptionState;
#ifdef DACCESS_COMPILE
    friend class ClrDataAccess;
#endif // DACCESS_COMPILE

    friend void FreeTrackerMemory(ExceptionTracker* pTracker, TrackerMemoryType mem);

private:
    class StackRange;
public:

    ExceptionTracker() :
        m_pThread(NULL),
        m_hThrowable(NULL),
        m_hCallerToken(NULL),
        m_hImpersonationToken(NULL)
    {
#ifndef DACCESS_COMPILE
        m_StackTraceInfo.Init();
#endif //  DACCESS_COMPILE

#ifndef FEATURE_PAL        
        // Init the WatsonBucketTracker
        m_WatsonBucketTracker.Init();
#endif // !FEATURE_PAL        

#ifdef FEATURE_CORRUPTING_EXCEPTIONS
        // Initialize the default exception severity to NotCorrupting
        m_CorruptionSeverity = NotSet;
#endif // FEATURE_CORRUPTING_EXCEPTIONS

#ifdef FEATURE_EXCEPTION_NOTIFICATIONS
        // By default, mark the tracker as not having delivered the first
        // chance exception notification
        m_fDeliveredFirstChanceNotification = FALSE;
#endif // FEATURE_EXCEPTION_NOTIFICATIONS

        m_sfFirstPassTopmostFrame.Clear();
        
        m_dwIndexClauseForCatch = 0;
        m_sfEstablisherOfActualHandlerFrame.Clear();
        m_sfCallerOfActualHandlerFrame.Clear();

        m_fFixupCallerSPForGCReporting = false;

        m_fResetEnclosingClauseSPForCatchFunclet = FALSE;

        m_sfCurrentEstablisherFrame.Clear();
        m_sfLastUnwoundEstablisherFrame.Clear();
        m_pInitialExplicitFrame = NULL;
        m_pLimitFrame = NULL;
    }

    ExceptionTracker(DWORD_PTR             dwExceptionPc,
                     PTR_EXCEPTION_RECORD  pExceptionRecord,
                     PTR_CONTEXT           pContextRecord) :
        m_pPrevNestedInfo((ExceptionTracker*)NULL),
        m_pThread(GetThread()),
        m_hThrowable(NULL),
        m_uCatchToCallPC(NULL),
        m_pSkipToParentFunctionMD(NULL),
// these members were added for resume frame processing
        m_pClauseForCatchToken(NULL),
// end resume frame members
        m_ExceptionCode(pExceptionRecord->ExceptionCode),
        m_hCallerToken(NULL),
        m_hImpersonationToken(NULL)
    {
        m_ptrs.ExceptionRecord  = pExceptionRecord;
        m_ptrs.ContextRecord    = pContextRecord;

        m_pLimitFrame = NULL;
        
        if (IsInstanceTaggedSEHCode(pExceptionRecord->ExceptionCode) && ::WasThrownByUs(pExceptionRecord, pExceptionRecord->ExceptionCode))
        {
            m_ExceptionFlags.SetWasThrownByUs();
        }

        m_StackTraceInfo.Init();

#ifndef FEATURE_PAL        
        // Init the WatsonBucketTracker
        m_WatsonBucketTracker.Init();
#endif // !FEATURE_PAL        

#ifdef FEATURE_CORRUPTING_EXCEPTIONS
        // Initialize the default exception severity to NotCorrupting
        m_CorruptionSeverity = NotSet;
#endif // FEATURE_CORRUPTING_EXCEPTIONS

#ifdef FEATURE_EXCEPTION_NOTIFICATIONS
        // By default, mark the tracker as not having delivered the first
        // chance exception notification
        m_fDeliveredFirstChanceNotification = FALSE;
#endif // FEATURE_EXCEPTION_NOTIFICATIONS

        m_dwIndexClauseForCatch = 0;
        m_sfEstablisherOfActualHandlerFrame.Clear();
        m_sfCallerOfActualHandlerFrame.Clear();
        
        m_sfFirstPassTopmostFrame.Clear();
        
        m_fFixupCallerSPForGCReporting = false;

        m_fResetEnclosingClauseSPForCatchFunclet = FALSE;

        m_sfCurrentEstablisherFrame.Clear();
        m_sfLastUnwoundEstablisherFrame.Clear();
        m_pInitialExplicitFrame = NULL;
    }

    ~ExceptionTracker()
    {
        ReleaseResources();
    }

    enum StackTraceState
    {
        STS_Append,
        STS_FirstRethrowFrame,
        STS_NewException,
    };

    static void InitializeCrawlFrame(CrawlFrame* pcfThisFrame, Thread* pThread, StackFrame sf, REGDISPLAY* pRD, 
                                     PT_DISPATCHER_CONTEXT pDispatcherContext, DWORD_PTR ControlPCForEHSearch, 
                                     UINT_PTR* puMethodStartPC
                                     ARM_ARG(ExceptionTracker *pCurrentTracker)
                                     ARM64_ARG(ExceptionTracker *pCurrentTracker));
    static void InitializeCrawlFrameForExplicitFrame(CrawlFrame* pcfThisFrame, Frame* pFrame, MethodDesc *pMD);

#ifndef DACCESS_COMPILE
    static void ResetThreadAbortStatus(PTR_Thread pThread, CrawlFrame *pCf, StackFrame sfCurrentStackFrame);
#endif // !DACCESS_COMPILE

    CLRUnwindStatus ProcessOSExceptionNotification(
        PEXCEPTION_RECORD pExceptionRecord,
        PT_CONTEXT pContextRecord,
        PT_DISPATCHER_CONTEXT pDispatcherContext,
        DWORD dwExceptionFlags,
        StackFrame sf,
        Thread* pThread,
        StackTraceState STState ARM_ARG(PVOID pICFSetAsLimitFrame));

    CLRUnwindStatus ProcessExplicitFrame(
        CrawlFrame* pcfThisFrame,
        StackFrame sf,
        BOOL fIsFirstPass,
        StackTraceState& STState
        );

    CLRUnwindStatus ProcessManagedCallFrame(
        CrawlFrame* pcfThisFrame,
        StackFrame sf,
        StackFrame sfEstablisherFrame,
        EXCEPTION_RECORD* pExceptionRecord,
        StackTraceState STState,
        UINT_PTR uMethodStartPC,
        DWORD dwExceptionFlags,
        DWORD dwTACatchHandlerClauseIndex,
        StackFrame sfEstablisherOfActualHandlerFrame
        );

    bool UpdateScannedStackRange(StackFrame sf, bool fIsFirstPass);

    void FirstPassIsComplete();
    void SecondPassIsComplete(MethodDesc* pMD, StackFrame sfResumeStackFrame);

    CLRUnwindStatus HandleFunclets(bool* pfProcessThisFrame, bool fIsFirstPass, 
        MethodDesc * pMD, bool fFunclet, StackFrame sf);

    static OBJECTREF CreateThrowable(
        PEXCEPTION_RECORD pExceptionRecord,
        BOOL bAsynchronousThreadStop
        );

    DWORD                   GetExceptionCode()      { return m_ExceptionCode;       }
    INDEBUG(inline  bool    IsValid());
    INDEBUG(static UINT_PTR DebugComputeNestingLevel());

    inline OBJECTREF GetThrowable()
    {
        CONTRACTL
        {
            MODE_COOPERATIVE;
            NOTHROW;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

        if (NULL != m_hThrowable)
        {
            return ObjectFromHandle(m_hThrowable);
        }

        return NULL;
    }

    // Return a StackFrame of the current frame for parent frame checking purposes.
    // Don't use this StackFrame in any way except to pass it back to the ExceptionTracker
    // via IsUnwoundToTargetParentFrame().
    static StackFrame GetStackFrameForParentCheck(CrawlFrame * pCF);

    static bool IsInStackRegionUnwoundBySpecifiedException(CrawlFrame * pCF, PTR_ExceptionTracker pExceptionTracker);
    static bool IsInStackRegionUnwoundByCurrentException(CrawlFrame * pCF);

    static bool HasFrameBeenUnwoundByAnyActiveException(CrawlFrame * pCF);
    void SetCurrentEstablisherFrame(StackFrame sfEstablisher)
    {
        LIMITED_METHOD_CONTRACT;

        m_sfCurrentEstablisherFrame = sfEstablisher;
    }

    StackFrame GetCurrentEstablisherFrame()
    {
        LIMITED_METHOD_CONTRACT;

        return m_sfCurrentEstablisherFrame;
    }

    void SetLastUnwoundEstablisherFrame(StackFrame sfEstablisher)
    {
        LIMITED_METHOD_CONTRACT;

        m_sfLastUnwoundEstablisherFrame = sfEstablisher;
    }

    StackFrame GetLastUnwoundEstablisherFrame()
    {
        LIMITED_METHOD_CONTRACT;

        return m_sfLastUnwoundEstablisherFrame;
    }

    PTR_Frame GetInitialExplicitFrame()
    {
        LIMITED_METHOD_CONTRACT;

        return m_pInitialExplicitFrame;
    }

    // Determines if we have unwound to the specified parent method frame.
    // Currently this is only used for funclet skipping.
    static bool IsUnwoundToTargetParentFrame(CrawlFrame * pCF, StackFrame sfParent);
    static bool IsUnwoundToTargetParentFrame(StackFrame sfToCheck, StackFrame sfParent);

    // Given the CrawlFrame for a funclet frame, return the frame pointer of the enclosing funclet frame.
    // For filter funclet frames and a normal method frames, this function returns a NULL StackFrame.
    //
    // <WARNING>
    // It is not valid to call this function on an arbitrary funclet.  You have to be doing a full stackwalk from
    // the leaf frame and skipping method frames as indicated by the return value of this function.  This function 
    // relies on the ExceptionTrackers, which are collapsed in the second pass when a nested exception escapes.  
    // When this happens, we'll lose information on the funclet represented by the collapsed tracker.
    // </WARNING>
    //
    // Return Value:
    // StackFrame.IsNull()   - no skipping is necessary
    // StackFrame.IsMaxVal() - skip one frame and then ask again
    // Anything else         - skip to the method frame indicated by the return value and ask again
    static StackFrame FindParentStackFrameForStackWalk(CrawlFrame* pCF, bool fForGCReporting = false);

    // Given the CrawlFrame for a filter funclet frame, return the frame pointer of the parent method frame.
    // It also returns the relative offset and the caller SP of the parent method frame.
    //
    // <WARNING>
    // The same warning for FindParentStackFrameForStackWalk() also applies here.  Moreoever, although
    // this function seems to be more convenient, it may potentially trigger a full stackwalk!  Do not
    // call this unless you know absolutely what you are doing.  In most cases FindParentStackFrameForStackWalk()
    // is what you need.
    // </WARNING>
    //
    // Return Value:
    // StackFrame.IsNull()   - no skipping is necessary
    // Anything else         - the StackFrame of the parent method frame
    static StackFrame FindParentStackFrameEx(CrawlFrame* pCF, 
                                             DWORD*      pParentOffset,
                                             UINT_PTR*   pParentCallerSP);

    static void
        PopTrackers(StackFrame sfResumeFrame,
                    bool fPopWhenEqual);

    static void
        PopTrackers(void* pvStackPointer);

    static void
        PopTrackerIfEscaping(void* pvStackPointer);

    static ExceptionTracker*
        GetOrCreateTracker(UINT_PTR ControlPc,
                           StackFrame sf,
                           EXCEPTION_RECORD* pExceptionRecord,
                           T_CONTEXT* pContextRecord,
                           BOOL bAsynchronousThreadStop,
                           bool fIsFirstPass,
                           StackTraceState* pSTState);

    static void
        ResumeExecution(T_CONTEXT*            pContextRecord,
                        EXCEPTION_RECORD*   pExceptionRecord
                        );

    void ResetLimitFrame();

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif // DACCESS_COMPILE

    static void DebugLogTrackerRanges(__in_z const char *pszTag);

    bool IsStackOverflowException();

private:
    DWORD_PTR
        CallHandler(UINT_PTR                dwHandlerStartPC,
                    StackFrame              sf,
                    EE_ILEXCEPTION_CLAUSE*  pEHClause,
                    MethodDesc*             pMD,
                    EHFuncletType funcletType
                    ARM_ARG(PT_CONTEXT pContextRecord)
                    ARM64_ARG(PT_CONTEXT pContextRecord)
                    );

    inline static BOOL
        ClauseCoversPC(EE_ILEXCEPTION_CLAUSE* pEHClause,
                       DWORD dwOffset);

    static bool 
        IsFilterStartOffset(EE_ILEXCEPTION_CLAUSE* pEHClause, DWORD_PTR dwHandlerStartPC);

#ifndef DACCESS_COMPILE
    void DestroyExceptionHandle()
    {
        // Never, ever destroy a preallocated exception handle.
        if ((m_hThrowable != NULL) && !CLRException::IsPreallocatedExceptionHandle(m_hThrowable))
        {
            DestroyHandle(m_hThrowable);
        }

        m_hThrowable = NULL;
    }
#endif

    void SaveStackTrace();

    inline BOOL CanAllocateMemory()
    {
        CONTRACTL
        {
            MODE_COOPERATIVE;
            NOTHROW;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

        OBJECTREF oThrowable = GetThrowable();

        return !(oThrowable == CLRException::GetPreallocatedOutOfMemoryException()) &&
               !(oThrowable == CLRException::GetPreallocatedStackOverflowException());
    }

    INDEBUG(inline  BOOL        ThrowableIsValid());

    bool HandleNestedExceptionEscape(StackFrame sf, bool fIsFirstPass);

#if defined(DEBUGGING_SUPPORTED)
    BOOL NotifyDebuggerOfStub(Thread* pThread, StackFrame sf, Frame* pCurrentFrame);

    void
        MakeCallbacksRelatedToHandler(bool fBeforeCallingHandler,
                                      Thread* pThread,
                                      MethodDesc* pMD,
                                      EE_ILEXCEPTION_CLAUSE* pEHClause,
                                      DWORD_PTR dwHandlerStartPC,
                                      StackFrame sf);
#else  // !DEBUGGING_SUPPORTED
    void
        MakeCallbacksRelatedToHandler(bool fBeforeCallingHandler,
                                      Thread* pThread,
                                      MethodDesc* pMD,
                                      EE_ILEXCEPTION_CLAUSE* pEHClause,
                                      DWORD_PTR dwHandlerStartPC,
                                      StackFrame sf) {return;}
#endif // !DEBUGGING_SUPPORTED

    // private helpers
    static StackFrame GetCallerSPOfParentOfNonExceptionallyInvokedFunclet(CrawlFrame *pCF);
    
    static StackFrame FindParentStackFrameHelper(CrawlFrame* pCF,
                                                 bool*       pfRealParent,
                                                 DWORD*      pParentOffset, 
                                                 UINT_PTR*   pParentCallerSP,
                                                 bool        fForGCReporting = false);

    static StackFrame RareFindParentStackFrame(CrawlFrame* pCF,
                                               DWORD*      pParentOffset, 
                                               UINT_PTR*   pParentCallerSP);

    static StackWalkAction RareFindParentStackFrameCallback(CrawlFrame* pCF, LPVOID pData); 

    struct DAC_EXCEPTION_POINTERS
    {
        PTR_EXCEPTION_RECORD    ExceptionRecord;
        PTR_CONTEXT             ContextRecord;
    };

public:

    static UINT_PTR FinishSecondPass(Thread* pThread, UINT_PTR uResumePC, StackFrame sf,
                                     T_CONTEXT* pContextRecord, ExceptionTracker *pTracker, bool* pfAborting = NULL);
    UINT_PTR CallCatchHandler(T_CONTEXT* pContextRecord, bool* pfAborting = NULL);

    static bool FindNonvolatileRegisterPointers(Thread* pThread, UINT_PTR uOriginalSP, REGDISPLAY* pRegDisplay, TADDR uResumeFrameFP);
    static void UpdateNonvolatileRegisters(T_CONTEXT* pContextRecord, REGDISPLAY *pRegDisplay, bool fAborting);

    PTR_Frame GetLimitFrame()
    {
        LIMITED_METHOD_CONTRACT;
        return m_pLimitFrame;
    }

    StackRange GetScannedStackRange()
    {
        LIMITED_METHOD_CONTRACT;

        return m_ScannedStackRange;
    }

    UINT_PTR GetCatchToCallPC()
    {
        LIMITED_METHOD_CONTRACT;

        return m_uCatchToCallPC;
    }

    // Returns the topmost frame seen during the first pass.
    StackFrame GetTopmostStackFrameFromFirstPass()
    {
        LIMITED_METHOD_CONTRACT;

        return m_sfFirstPassTopmostFrame;
    }

#ifdef _DEBUG
    StackFrame GetResumeStackFrame()
    {
        LIMITED_METHOD_CONTRACT;

        return m_sfResumeStackFrame;
    }

    PTR_EXCEPTION_CLAUSE_TOKEN GetCatchHandlerExceptionClauseToken()
    {
        LIMITED_METHOD_CONTRACT;

        return m_pClauseForCatchToken;
    }
#endif // _DEBUG

    DWORD GetCatchHandlerExceptionClauseIndex()
    {
        LIMITED_METHOD_CONTRACT;

        return m_dwIndexClauseForCatch;
    }

    StackFrame GetEstablisherOfActualHandlingFrame()
    {
        LIMITED_METHOD_CONTRACT;
        
        return m_sfEstablisherOfActualHandlerFrame;
    }

    StackFrame GetCallerOfActualHandlingFrame()
    {
        LIMITED_METHOD_CONTRACT;
    
        return m_sfCallerOfActualHandlerFrame;
    }

#ifndef FEATURE_PAL          
private:
    EHWatsonBucketTracker m_WatsonBucketTracker;
public:
    inline PTR_EHWatsonBucketTracker GetWatsonBucketTracker()
    {
        LIMITED_METHOD_CONTRACT;
        return PTR_EHWatsonBucketTracker(PTR_HOST_MEMBER_TADDR(ExceptionTracker, this, m_WatsonBucketTracker));
    }
#endif // !FEATURE_PAL        

#ifdef FEATURE_CORRUPTING_EXCEPTIONS
private:
    CorruptionSeverity      m_CorruptionSeverity;
public:
    inline CorruptionSeverity GetCorruptionSeverity()
    {
        LIMITED_METHOD_CONTRACT;

        return (CorruptionSeverity)GET_CORRUPTION_SEVERITY(m_CorruptionSeverity);
    }

    inline void SetCorruptionSeverity(CorruptionSeverity severityToSet)
    {
        LIMITED_METHOD_CONTRACT;

        m_CorruptionSeverity = severityToSet;
    }
#endif // FEATURE_CORRUPTING_EXCEPTIONS

#ifdef FEATURE_EXCEPTION_NOTIFICATIONS
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
#endif // FEATURE_EXCEPTION_NOTIFICATIONS

    // Returns the exception tracker previous to the current
    inline PTR_ExceptionTracker GetPreviousExceptionTracker()
    {
        LIMITED_METHOD_CONTRACT;

        return m_pPrevNestedInfo;
    }

    // Returns the throwble associated with the tracker as handle
    inline OBJECTHANDLE GetThrowableAsHandle()
    {
        LIMITED_METHOD_CONTRACT;

        return m_hThrowable;
    }

    bool IsInFirstPass()
    {
        return !m_ExceptionFlags.UnwindHasStarted();
    }
    
private: ;

    void ReleaseResources();

    void SetEnclosingClauseInfo(bool     fEnclosingClauseIsFunclet,
                                DWORD    dwEnclosingClauseOffset,
                                UINT_PTR uEnclosingClauseCallerSP);

    class StackRange
    {
    public:
        StackRange();
        void Reset();
        bool IsEmpty();
        bool IsSupersededBy(StackFrame sf);
        void CombineWith(StackFrame sfCurrent, StackRange* pPreviousRange);
        bool Contains(StackFrame sf);
        void ExtendUpperBound(StackFrame sf);
        void ExtendLowerBound(StackFrame sf);
        void TrimLowerBound(StackFrame sf);
        StackFrame GetLowerBound();
        StackFrame GetUpperBound();
        INDEBUG(bool IsDisjointWithAndLowerThan(StackRange* pOtherRange));
    private:
        INDEBUG(bool IsConsistent());

    private:
        // <TODO> can we use a smaller encoding? </TODO>
        StackFrame          m_sfLowBound;
        StackFrame          m_sfHighBound;
    };

    struct EnclosingClauseInfo
    {
    public:
        EnclosingClauseInfo();
        EnclosingClauseInfo(bool fEnclosingClauseIsFunclet, DWORD dwEnclosingClauseOffset, UINT_PTR uEnclosingClauseCallerSP);

        bool     EnclosingClauseIsFunclet();
        DWORD    GetEnclosingClauseOffset();
        UINT_PTR GetEnclosingClauseCallerSP();
        void     SetEnclosingClauseCallerSP(UINT_PTR callerSP);

        bool operator==(const EnclosingClauseInfo & rhs);

    private:
        UINT_PTR m_uEnclosingClauseCallerSP;
        DWORD    m_dwEnclosingClauseOffset;
        bool     m_fEnclosingClauseIsFunclet;
    };

    PTR_ExceptionTracker    m_pPrevNestedInfo;
    Thread*                 m_pThread;          // this is used as an IsValid/IsFree field -- if it's NULL, the allocator can
                                                // reuse its memory, if it's non-NULL, it better be a valid thread pointer

    StackRange              m_ScannedStackRange;
    DAC_EXCEPTION_POINTERS  m_ptrs;
    OBJECTHANDLE            m_hThrowable;
    StackTraceInfo          m_StackTraceInfo;
    UINT_PTR                m_uCatchToCallPC;
    BOOL           m_fResetEnclosingClauseSPForCatchFunclet;
    
    union
    {
        MethodDesc*         m_pSkipToParentFunctionMD;      // SKIPTOPARENT
        MethodDesc*         m_pMethodDescOfCatcher;
    };

    StackFrame              m_sfResumeStackFrame;           // RESUMEFRAME
    StackFrame              m_sfFirstPassTopmostFrame;      // Topmost frame seen during first pass
    PTR_EXCEPTION_CLAUSE_TOKEN m_pClauseForCatchToken;              // RESUMEFRAME
    EE_ILEXCEPTION_CLAUSE   m_ClauseForCatch;
    // Index of EH clause that will catch the exception
    DWORD                   m_dwIndexClauseForCatch;
    
    // Establisher frame of the managed frame that contains
    // the handler for the exception (corresponding
    // to the EH index we save off in m_dwIndexClauseForCatch)
    StackFrame              m_sfEstablisherOfActualHandlerFrame;
    StackFrame              m_sfCallerOfActualHandlerFrame;

    ExceptionFlags          m_ExceptionFlags;
    DWORD                   m_ExceptionCode;

    PTR_Frame               m_pLimitFrame;
    
    // Thread Security State
    HANDLE                  m_hCallerToken;
    HANDLE                  m_hImpersonationToken;

#ifdef DEBUGGING_SUPPORTED
    //
    // DEBUGGER STATE
    //
    DebuggerExState         m_DebuggerExState;
#endif // DEBUGGING_SUPPORTED

    //
    // Information for the funclet we are calling
    //
    EHClauseInfo            m_EHClauseInfo;

    // This flag indicates whether the SP we pass to a funclet is for an enclosing funclet.
    EnclosingClauseInfo     m_EnclosingClauseInfo;
    
    // This stores the actual callerSP of the frame that is about to execute the funclet.
    // It differs from "m_EnclosingClauseInfo" where upon detecting a nested exception,
    // the latter can contain the callerSP of the original funclet instead of that of the
    // current frame.
    EnclosingClauseInfo     m_EnclosingClauseInfoForGCReporting;
    bool                    m_fFixupCallerSPForGCReporting;

    StackFrame              m_sfCurrentEstablisherFrame;
    StackFrame              m_sfLastUnwoundEstablisherFrame;
    PTR_Frame               m_pInitialExplicitFrame;
};

#if defined(WIN64EXCEPTIONS)
PTR_ExceptionTracker GetEHTrackerForPreallocatedException(OBJECTREF oPreAllocThrowable, PTR_ExceptionTracker pStartingEHTracker);
#endif // WIN64EXCEPTIONS

class TrackerAllocator
{
public:
    void                Init();
    void                Terminate();
    ExceptionTracker*   GetTrackerMemory();
    void                FreeTrackerMemory(ExceptionTracker* pTracker);

private:

    struct Page;

    struct PageHeader
    {
        Page*               m_pNext;
        LONG                m_idxFirstFree;
    };

    enum
    {
        //
        // Due to the unexpected growth of the ExceptionTracker struct, 
        // OS_PAGE_SIZE does not seem appropriate anymore on x64, and
        // we should behave the same on x64 as on ia64 regardless of
        // the difference between the page sizes on the platforms.
        //
        TRACKER_ALLOCATOR_PAGE_SIZE         = 8*1024,
        TRACKER_ALLOCATOR_MAX_OOM_SPINS     = 20,
        TRACKER_ALLOCATOR_OOM_SPIN_DELAY    = 100,
        NUM_TRACKERS_PER_PAGE               = ((TRACKER_ALLOCATOR_PAGE_SIZE - sizeof(PageHeader)) / sizeof(ExceptionTracker)),
    };

    struct Page
    {
        PageHeader          m_header;
        ExceptionTracker    m_rgTrackers[NUM_TRACKERS_PER_PAGE];
    };

    static_assert_no_msg(sizeof(Page) <= TRACKER_ALLOCATOR_PAGE_SIZE);

    Page* m_pFirstPage;
    Crst* m_pCrst;
};

#endif // WIN64EXCEPTIONS

#endif  // __EXCEPTION_HANDLING_h__
