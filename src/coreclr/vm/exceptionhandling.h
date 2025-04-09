// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//

#ifndef __EXCEPTION_HANDLING_h__
#define __EXCEPTION_HANDLING_h__

#ifdef FEATURE_EH_FUNCLETS

#include "eexcp.h"
#include "exstatecommon.h"

// This address lies in the NULL pointer partition of the process memory.
// Accessing it will result in AV.
#define INVALID_RESUME_ADDRESS 0x000000000000bad0

EXTERN_C EXCEPTION_DISPOSITION __cdecl
ProcessCLRException(IN     PEXCEPTION_RECORD     pExceptionRecord,
                    IN     PVOID                 pEstablisherFrame,
                    IN OUT PT_CONTEXT            pContextRecord,
                    IN OUT PT_DISPATCHER_CONTEXT pDispatcherContext);

VOID DECLSPEC_NORETURN DispatchManagedException(OBJECTREF throwable, CONTEXT *pExceptionContext, EXCEPTION_RECORD *pExceptionRecord = NULL);
VOID DECLSPEC_NORETURN DispatchManagedException(OBJECTREF throwable);
VOID DECLSPEC_NORETURN DispatchManagedException(RuntimeExceptionKind reKind);

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

struct ExInfo;
typedef DPTR(ExInfo) PTR_ExInfo;

// These values are or-ed into the InlinedCallFrame::m_Datum field.
// The bit 0 is used for unrelated purposes (see comments on the
// InlinedCallFrame::m_Datum field for details).
enum class InlinedCallFrameMarker
{
#ifdef HOST_64BIT
    ExceptionHandlingHelper = 2,
    SecondPassFuncletCaller = 4,
#else // HOST_64BIT
    ExceptionHandlingHelper = 1,
    SecondPassFuncletCaller = 2,
#endif // HOST_64BIT
    Mask = ExceptionHandlingHelper | SecondPassFuncletCaller
};

typedef DPTR(struct ExceptionTrackerBase) PTR_ExceptionTrackerBase;

struct ExceptionTrackerBase
{
    struct DAC_EXCEPTION_POINTERS
    {
        PTR_EXCEPTION_RECORD    ExceptionRecord;
        PTR_CONTEXT             ContextRecord;
    };

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

    // Previous ExInfo in the chain of exceptions rethrown from their catch / finally handlers
    PTR_ExceptionTrackerBase m_pPrevNestedInfo;
    // thrown exception object handle
    OBJECTHANDLE    m_hThrowable;
    // EXCEPTION_RECORD and CONTEXT_RECORD describing the exception and its location
    DAC_EXCEPTION_POINTERS m_ptrs;
    // Information for the funclet we are calling
    EHClauseInfo   m_EHClauseInfo;
    // Flags representing exception handling state (exception is rethrown, unwind has started, various debugger notifications sent etc)
    ExceptionFlags m_ExceptionFlags;
    // Set to TRUE when the first chance notification was delivered for the current exception
    BOOL           m_fDeliveredFirstChanceNotification;
    // Code of the current exception
    DWORD          m_ExceptionCode;
#ifdef DEBUGGING_SUPPORTED
    // Stores information necessary to intercept an exception
    DebuggerExState m_DebuggerExState;
#endif // DEBUGGING_SUPPORTED
    // Low and high bounds of the stack unwound by the exception.
    // In the new EH implementation, they are updated during 2nd pass only.
    StackRange      m_ScannedStackRange;

#ifndef TARGET_UNIX
protected:
    EHWatsonBucketTracker m_WatsonBucketTracker;
public:
    inline PTR_EHWatsonBucketTracker GetWatsonBucketTracker()
    {
        LIMITED_METHOD_CONTRACT;
        return PTR_EHWatsonBucketTracker(PTR_HOST_MEMBER_TADDR(ExceptionTrackerBase, this, m_WatsonBucketTracker));
    }
#endif // !TARGET_UNIX

    ExceptionTrackerBase(PTR_EXCEPTION_RECORD pExceptionRecord, PTR_CONTEXT pExceptionContext, PTR_ExceptionTrackerBase pPrevNestedInfo) :
        m_pPrevNestedInfo(pPrevNestedInfo),
        m_hThrowable{},
        m_ptrs({pExceptionRecord, pExceptionContext}),
        m_fDeliveredFirstChanceNotification(FALSE),
        m_ExceptionCode((pExceptionRecord != PTR_NULL) ? pExceptionRecord->ExceptionCode : 0)
    {
#ifndef TARGET_UNIX
        // Init the WatsonBucketTracker
        m_WatsonBucketTracker.Init();
#endif // !TARGET_UNIX
    }

    // Returns the exception tracker previous to the current
    inline PTR_ExceptionTrackerBase GetPreviousExceptionTracker()
    {
        LIMITED_METHOD_CONTRACT;

        return m_pPrevNestedInfo;
    }

    inline OBJECTREF GetThrowable()
    {
        CONTRACTL
        {
            MODE_COOPERATIVE;
            NOTHROW;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

        if (0 != m_hThrowable)
        {
            return ObjectFromHandle(m_hThrowable);
        }

        return NULL;
    }

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

    DWORD GetExceptionCode()
    {
        return m_ExceptionCode;
    }

    StackRange GetScannedStackRange()
    {
        LIMITED_METHOD_CONTRACT;

        return m_ScannedStackRange;
    }

    bool IsInFirstPass()
    {
        return !m_ExceptionFlags.UnwindHasStarted();
    }

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
#endif // !DACCESS_COMPILE

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif // DACCESS_COMPILE
};

typedef DPTR(class ExceptionTracker) PTR_ExceptionTracker;
class ExceptionTracker : public ExceptionTrackerBase
{
    friend class ThreadExceptionState;
    friend class ClrDataExceptionState;
#ifdef DACCESS_COMPILE
    friend class ClrDataAccess;
#endif // DACCESS_COMPILE

    ExceptionTracker() :
        ExceptionTrackerBase(PTR_NULL, PTR_NULL, PTR_NULL)
    {
        UNREACHABLE();
    }    

public:

    static OBJECTREF CreateThrowable(
        PEXCEPTION_RECORD pExceptionRecord,
        BOOL bAsynchronousThreadStop
        );

    INDEBUG(static UINT_PTR DebugComputeNestingLevel());

    // Return a StackFrame of the current frame for parent frame checking purposes.
    // Don't use this StackFrame in any way except to pass it back to the ExceptionTracker
    // via IsUnwoundToTargetParentFrame().
    static StackFrame GetStackFrameForParentCheck(CrawlFrame * pCF);

    static bool IsInStackRegionUnwoundBySpecifiedException(CrawlFrame * pCF, PTR_ExceptionTrackerBase pExceptionTracker);
    static bool IsInStackRegionUnwoundByCurrentException(CrawlFrame * pCF);

    static bool HasFrameBeenUnwoundByAnyActiveException(CrawlFrame * pCF);

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
                                             DWORD*      pParentOffset);

    static void
        PopTrackers(StackFrame sfResumeFrame,
                    bool fPopWhenEqual);

    static void
        PopTrackers(void* pvStackPointer);

    static void UpdateNonvolatileRegisters(T_CONTEXT* pContextRecord, REGDISPLAY *pRegDisplay, bool fAborting);

private:
    // private helpers
    static StackFrame GetCallerSPOfParentOfNonExceptionallyInvokedFunclet(CrawlFrame *pCF);

    static StackFrame FindParentStackFrameHelper(CrawlFrame* pCF,
                                                 bool*       pfRealParent,
                                                 DWORD*      pParentOffset,
                                                 bool        fForGCReporting = false);

    static StackFrame RareFindParentStackFrame(CrawlFrame* pCF,
                                               DWORD*      pParentOffset);

    static StackWalkAction RareFindParentStackFrameCallback(CrawlFrame* pCF, LPVOID pData);
};

PTR_ExceptionTrackerBase GetEHTrackerForPreallocatedException(OBJECTREF oPreAllocThrowable, PTR_ExceptionTrackerBase pStartingEHTracker);

#endif // FEATURE_EH_FUNCLETS

#if defined(TARGET_X86)
#define USE_CURRENT_CONTEXT_IN_FILTER
#endif // TARGET_X86

#if defined(TARGET_ARM) || defined(TARGET_ARM64) || defined(TARGET_X86) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
#define USE_FUNCLET_CALL_HELPER
#endif // TARGET_ARM || TARGET_ARM64 || TARGET_X86 || TARGET_LOONGARCH64 || TARGET_RISCV64

#endif  // __EXCEPTION_HANDLING_h__
