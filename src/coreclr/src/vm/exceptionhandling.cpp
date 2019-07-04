// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//

#include "common.h"

#ifdef WIN64EXCEPTIONS
#include "exceptionhandling.h"
#include "dbginterface.h"
#include "asmconstants.h"
#include "eetoprofinterfacewrapper.inl"
#include "eedbginterfaceimpl.inl"
#include "eventtrace.h"
#include "virtualcallstub.h"
#include "utilcode.h"

#if defined(_TARGET_X86_)
#define USE_CURRENT_CONTEXT_IN_FILTER
#endif // _TARGET_X86_

#if defined(_TARGET_ARM_) || defined(_TARGET_X86_)
#define VSD_STUB_CAN_THROW_AV
#endif // _TARGET_ARM_ || _TARGET_X86_

#if defined(_TARGET_ARM_) || defined(_TARGET_ARM64_)
// ARM/ARM64 uses Caller-SP to locate PSPSym in the funclet frame.
#define USE_CALLER_SP_IN_FUNCLET
#endif // _TARGET_ARM_ || _TARGET_ARM64_

#if defined(_TARGET_ARM_) || defined(_TARGET_ARM64_) || defined(_TARGET_X86_)
#define ADJUST_PC_UNWOUND_TO_CALL
#define STACK_RANGE_BOUNDS_ARE_CALLER_SP
#define USE_FUNCLET_CALL_HELPER
// For ARM/ARM64, EstablisherFrame is Caller-SP (SP just before executing call instruction).
// This has been confirmed by AaronGi from the kernel team for Windows.
//
// For x86/Linux, RtlVirtualUnwind sets EstablisherFrame as Caller-SP.
#define ESTABLISHER_FRAME_ADDRESS_IS_CALLER_SP
#endif // _TARGET_ARM_ || _TARGET_ARM64_ || _TARGET_X86_

#ifndef FEATURE_PAL
void NOINLINE
ClrUnwindEx(EXCEPTION_RECORD* pExceptionRecord,
                 UINT_PTR          ReturnValue,
                 UINT_PTR          TargetIP,
                 UINT_PTR          TargetFrameSp);
#endif // !FEATURE_PAL

#ifdef USE_CURRENT_CONTEXT_IN_FILTER
inline void CaptureNonvolatileRegisters(PKNONVOLATILE_CONTEXT pNonvolatileContext, PCONTEXT pContext)
{
#define CALLEE_SAVED_REGISTER(reg) pNonvolatileContext->reg = pContext->reg;
    ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER
}

inline void RestoreNonvolatileRegisters(PCONTEXT pContext, PKNONVOLATILE_CONTEXT pNonvolatileContext)
{
#define CALLEE_SAVED_REGISTER(reg) pContext->reg = pNonvolatileContext->reg;
    ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER
}

inline void RestoreNonvolatileRegisterPointers(PT_KNONVOLATILE_CONTEXT_POINTERS pContextPointers, PKNONVOLATILE_CONTEXT pNonvolatileContext)
{
#define CALLEE_SAVED_REGISTER(reg) pContextPointers->reg = &pNonvolatileContext->reg;
    ENUM_CALLEE_SAVED_REGISTERS();
#undef CALLEE_SAVED_REGISTER
}
#endif
#ifndef DACCESS_COMPILE

// o Functions and funclets are tightly associated.  In fact, they are laid out in contiguous memory.
//   They also present some interesting issues with respect to EH because we will see callstacks with
//   both functions and funclets, but need to logically treat them as the original single IL function
//   described them.
//
// o All funclets are ripped out of line from the main function.  Finally clause are pulled out of
//   line and replaced by calls to the funclets.  Catch clauses, however, are simply pulled out of
//   line.  !!!This causes a loss of nesting information in clause offsets.!!! A canonical example of
//   two different functions which look identical due to clause removal is as shown in the code
//   snippets below.  The reason they look identical in the face of out-of-line funclets is that the
//   region bounds for the "try A" region collapse and become identical to the region bounds for
//   region "try B".  This will look identical to the region information for Bar because Bar must
//   have a separate entry for each catch clause, both of which will have the same try-region bounds.
//
//   void Foo()                           void Bar()
//   {                                    {
//      try A                                try C
//      {                                    {
//         try B                                BAR_BLK_1
//         {                                 }
//            FOO_BLK_1                      catch C
//         }                                 {
//         catch B                              BAR_BLK_2
//         {                                 }
//            FOO_BLK_2                      catch D
//         }                                 {
//      }                                       BAR_BLK_3
//      catch A                              }
//      {                                 }
//         FOO_BLK_3
//      }
//   }
//
//  O The solution is to duplicate all clauses that logically cover the funclet in its parent
//    method, but with the try-region covering the entire out-of-line funclet code range.  This will
//    differentiate the canonical example above because the CatchB funclet will have a try-clause
//    covering it whose associated handler is CatchA.  In Bar, there is no such duplication of any clauses.
//
//  o The behavior of the personality routine depends upon the JIT to properly order the clauses from
//    inside-out.  This allows us to properly handle a situation where our control PC is covered by clauses
//    that should not be considered because a more nested clause will catch the exception and resume within
//    the scope of the outer clauses.
//
//  o This sort of clause duplication for funclets should be done for all clause types, not just catches.
//    Unfortunately, I cannot articulate why at the moment.
//
#ifdef _DEBUG
void DumpClauses(IJitManager* pJitMan, const METHODTOKEN& MethToken, UINT_PTR uMethodStartPC, UINT_PTR dwControlPc);
static void DoEHLog(DWORD lvl, __in_z const char *fmt, ...);
#define EH_LOG(expr)  { DoEHLog expr ; }
#else
#define EH_LOG(expr)
#endif

TrackerAllocator    g_theTrackerAllocator;
uint32_t            g_exceptionCount;

bool FixNonvolatileRegisters(UINT_PTR  uOriginalSP,
                             Thread*   pThread,
                             CONTEXT*  pContextRecord,
                             bool      fAborting
                             );

void FixContext(PCONTEXT pContextRecord)
{
#define FIXUPREG(reg, value)                                                                \
    do {                                                                                    \
        STRESS_LOG2(LF_GCROOTS, LL_INFO100, "Updating " #reg " %p to %p\n",                 \
                pContextRecord->reg,                                                        \
                (value));                                                                   \
        pContextRecord->reg = (value);                                                      \
    } while (0)

#ifdef _TARGET_X86_
    size_t resumeSp = EECodeManager::GetResumeSp(pContextRecord);
    FIXUPREG(Esp, resumeSp);
#endif // _TARGET_X86_

#undef FIXUPREG
}

MethodDesc * GetUserMethodForILStub(Thread * pThread, UINT_PTR uStubSP, MethodDesc * pILStubMD, Frame ** ppFrameOut);

#ifdef FEATURE_PAL
BOOL HandleHardwareException(PAL_SEHException* ex);
BOOL IsSafeToHandleHardwareException(PCONTEXT contextRecord, PEXCEPTION_RECORD exceptionRecord);
#endif // FEATURE_PAL

static ExceptionTracker* GetTrackerMemory()
{
    CONTRACTL
    {
        GC_TRIGGERS;
        NOTHROW;
        MODE_ANY;
    }
    CONTRACTL_END;

    return g_theTrackerAllocator.GetTrackerMemory();
}

void FreeTrackerMemory(ExceptionTracker* pTracker, TrackerMemoryType mem)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        MODE_ANY;
    }
    CONTRACTL_END;

    if (mem & memManaged)
    {
        pTracker->ReleaseResources();
    }

    if (mem & memUnmanaged)
    {
        g_theTrackerAllocator.FreeTrackerMemory(pTracker);
    }
}

static inline void UpdatePerformanceMetrics(CrawlFrame *pcfThisFrame, BOOL bIsRethrownException, BOOL bIsNewException)
{
    WRAPPER_NO_CONTRACT;
    g_exceptionCount++;

    // Fire an exception thrown ETW event when an exception occurs
    ETW::ExceptionLog::ExceptionThrown(pcfThisFrame, bIsRethrownException, bIsNewException);
}

#ifdef FEATURE_PAL
static LONG volatile g_termination_triggered = 0;

void HandleTerminationRequest(int terminationExitCode)
{
    // We set a non-zero exit code to indicate the process didn't terminate cleanly.
    // This value can be changed by the user by setting Environment.ExitCode in the
    // ProcessExit event. We only start termination on the first SIGTERM signal
    // to ensure we don't overwrite an exit code already set in ProcessExit.
    if (InterlockedCompareExchange(&g_termination_triggered, 1, 0) == 0)
    {
        SetLatchedExitCode(terminationExitCode);

        ForceEEShutdown(SCA_ExitProcessWhenShutdownComplete);
    }
}
#endif

void InitializeExceptionHandling()
{
    EH_LOG((LL_INFO100, "InitializeExceptionHandling(): ExceptionTracker size: 0x%x bytes\n", sizeof(ExceptionTracker)));

    InitSavedExceptionInfo();

    CLRAddVectoredHandlers();

    g_theTrackerAllocator.Init();

    // Initialize the lock used for synchronizing access to the stacktrace in the exception object
    g_StackTraceArrayLock.Init(LOCK_TYPE_DEFAULT, TRUE);

#ifdef FEATURE_PAL
    // Register handler of hardware exceptions like null reference in PAL
    PAL_SetHardwareExceptionHandler(HandleHardwareException, IsSafeToHandleHardwareException);

    // Register handler for determining whether the specified IP has code that is a GC marker for GCCover
    PAL_SetGetGcMarkerExceptionCode(GetGcMarkerExceptionCode);

    // Register handler for termination requests (e.g. SIGTERM)
    PAL_SetTerminationRequestHandler(HandleTerminationRequest);
#endif // FEATURE_PAL
}

struct UpdateObjectRefInResumeContextCallbackState
{
    UINT_PTR uResumeSP;
    Frame *pHighestFrameWithRegisters;
    TADDR uResumeFrameFP;
    TADDR uICFCalleeSavedFP;
    
#ifdef _DEBUG
    UINT nFrames;
    bool fFound;
#endif
};

// Stack unwind callback for UpdateObjectRefInResumeContext().
StackWalkAction UpdateObjectRefInResumeContextCallback(CrawlFrame* pCF, LPVOID pData)
{
    CONTRACTL
    {
        MODE_ANY;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    UpdateObjectRefInResumeContextCallbackState *pState = (UpdateObjectRefInResumeContextCallbackState*)pData;
    CONTEXT* pSrcContext = pCF->GetRegisterSet()->pCurrentContext;

    INDEBUG(pState->nFrames++);

    // Check to see if we have reached the resume frame.
    if (pCF->IsFrameless())
    {
        // At this point, we are trying to find the managed frame containing the catch handler to be invoked.
        // This is done by comparing the SP of the managed frame for which this callback was invoked with the
        // SP the OS passed to our personality routine for the current managed frame. If they match, then we have
        // reached the target frame.
        // 
        // It is possible that a managed frame may execute a PInvoke after performing a stackalloc:
        // 
        // 1) The ARM JIT will always inline the PInvoke in the managed frame, whether or not the frame
        //    contains EH. As a result, the ICF will live in the same frame which performs stackalloc.
        //    
        // 2) JIT64 will only inline the PInvoke in the managed frame if the frame *does not* contain EH. If it does,
        //    then pinvoke will be performed via an ILStub and thus, stackalloc will be performed in a frame different
        //    from the one (ILStub) that contains the ICF.
        //    
        // Thus, for the scenario where the catch handler lives in the frame that performed stackalloc, in case of
        // ARM JIT, the SP returned by the OS will be the SP *after* the stackalloc has happened. However,
        // the stackwalker will invoke this callback with the CrawlFrameSP that was initialized at the time ICF was setup, i.e.,
        // it will be the SP after the prolog has executed (refer to InlinedCallFrame::UpdateRegDisplay). 
        // 
        // Thus, checking only the SP will not work for this scenario when using the ARM JIT.
        // 
        // To address this case, the callback data also contains the frame pointer (FP) passed by the OS. This will
        // be the value that is saved in the "CalleeSavedFP" field of the InlinedCallFrame during ICF 
        // initialization. When the stackwalker sees an ICF and invokes this callback, we copy the value of "CalleeSavedFP" in the data
        // structure passed to this callback. 
        // 
        // Later, when the stackwalker invokes the callback for the managed frame containing the ICF, and the check
        // for SP comaprison fails, we will compare the FP value we got from the ICF with the FP value the OS passed
        // to us. If they match, then we have reached the resume frame.
        // 
        // Note: This problem/scenario is not applicable to JIT64 since it does not perform pinvoke inlining if the
        // method containing pinvoke also contains EH. Thus, the SP check will never fail for it.
        if (pState->uResumeSP == GetSP(pSrcContext))
        {
            INDEBUG(pState->fFound = true);

            return SWA_ABORT;
        }
        
        // Perform the FP check, as explained above.
        if ((pState->uICFCalleeSavedFP !=0) && (pState->uICFCalleeSavedFP == pState->uResumeFrameFP))
        {
            // FP from ICF is the one that was also copied to the FP register in InlinedCallFrame::UpdateRegDisplay.
            _ASSERTE(pState->uICFCalleeSavedFP == GetFP(pSrcContext));
            
            INDEBUG(pState->fFound = true);

            return SWA_ABORT;
        }
        
        // Reset the ICF FP in callback data
        pState->uICFCalleeSavedFP = 0;
    }
    else
    {
        Frame *pFrame = pCF->GetFrame();

        if (pFrame->NeedsUpdateRegDisplay())
        {
            CONSISTENCY_CHECK(pFrame >= pState->pHighestFrameWithRegisters);
            pState->pHighestFrameWithRegisters = pFrame;
            
            // Is this an InlinedCallFrame?
            if (pFrame->GetVTablePtr() == InlinedCallFrame::GetMethodFrameVPtr())
            {
                // If we are here, then ICF is expected to be active.
                _ASSERTE(InlinedCallFrame::FrameHasActiveCall(pFrame));
                
                // Copy the CalleeSavedFP to the data structure that is passed this callback
                // by the stackwalker. This is the value of frame pointer when ICF is setup
                // in a managed frame.
                // 
                // Setting this value here is based upon the assumption (which holds true on X64 and ARM) that
                // the stackwalker invokes the callback for explicit frames before their
                // container/corresponding managed frame.
                pState->uICFCalleeSavedFP = ((PTR_InlinedCallFrame)pFrame)->GetCalleeSavedFP();
            }
            else
            {
                // For any other frame, simply reset uICFCalleeSavedFP field
                pState->uICFCalleeSavedFP = 0;
            }
        }
    }

    return SWA_CONTINUE;
}


//
// Locates the locations of the nonvolatile registers.  This will be used to
// retrieve the latest values of the object references before we resume
// execution from an exception.
//
//static
bool ExceptionTracker::FindNonvolatileRegisterPointers(Thread* pThread, UINT_PTR uOriginalSP, REGDISPLAY* pRegDisplay, TADDR uResumeFrameFP)
{
    CONTRACTL
    {
        MODE_ANY;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    //
    // Find the highest frame below the resume frame that will update the
    // REGDISPLAY.  A normal StackWalkFrames will RtlVirtualUnwind through all
    // managed frames on the stack, so this avoids some unnecessary work.  The
    // frame we find will have all of the nonvolatile registers/other state
    // needed to start a managed unwind from that point.
    //
    Frame *pHighestFrameWithRegisters = NULL;
    Frame *pFrame = pThread->GetFrame();

    while ((UINT_PTR)pFrame < uOriginalSP)
    {
        if (pFrame->NeedsUpdateRegDisplay())
            pHighestFrameWithRegisters = pFrame;

        pFrame = pFrame->Next();
    }

    //
    // Do a stack walk from this frame.  This may find a higher frame within
    // the resume frame (ex. inlined pinvoke frame).  This will also update
    // the REGDISPLAY pointers if any intervening managed frames saved
    // nonvolatile registers.
    //

    UpdateObjectRefInResumeContextCallbackState state;

    state.uResumeSP = uOriginalSP;
    state.uResumeFrameFP = uResumeFrameFP;
    state.uICFCalleeSavedFP = 0;
    state.pHighestFrameWithRegisters = pHighestFrameWithRegisters;
        
    INDEBUG(state.nFrames = 0);
    INDEBUG(state.fFound = false);

    pThread->StackWalkFramesEx(pRegDisplay, &UpdateObjectRefInResumeContextCallback, &state, 0, pHighestFrameWithRegisters);

    // For managed exceptions, we should at least find a HelperMethodFrame (the one we put in IL_Throw()).
    // For native exceptions such as AV's, we should at least find the FaultingExceptionFrame.
    // If we don't find anything, then we must have hit an SO when we are trying to erect an HMF.
    // Bail out in such situations.
    //
    // Note that pinvoke frames may be inlined in a managed method, so we cannot use the child SP (a.k.a. the current SP)
    // to check for explicit frames "higher" on the stack ("higher" here means closer to the leaf frame).  The stackwalker
    // knows how to deal with inlined pinvoke frames, and it issues callbacks for them before issuing the callback for the
    // containing managed method.  So we have to do this check after we are done with the stackwalk.
    pHighestFrameWithRegisters = state.pHighestFrameWithRegisters;
    if (pHighestFrameWithRegisters == NULL)
    {
        return false;
    }

    CONSISTENCY_CHECK(state.nFrames);
    CONSISTENCY_CHECK(state.fFound);
    CONSISTENCY_CHECK(NULL != pHighestFrameWithRegisters);

    //
    // Now the REGDISPLAY has been unwound to the resume frame.  The
    // nonvolatile registers will either point into pHighestFrameWithRegisters,
    // an inlined pinvoke frame, or into calling managed frames.
    //

    return true;
}


//static
void ExceptionTracker::UpdateNonvolatileRegisters(CONTEXT *pContextRecord, REGDISPLAY *pRegDisplay, bool fAborting)
{
    CONTEXT* pAbortContext = NULL;
    if (fAborting)
    {
        pAbortContext = GetThread()->GetAbortContext();
    }

#ifndef FEATURE_PAL
#define HANDLE_NULL_CONTEXT_POINTER _ASSERTE(false)
#else // FEATURE_PAL
#define HANDLE_NULL_CONTEXT_POINTER
#endif // FEATURE_PAL

#define UPDATEREG(reg)                                                                      \
    do {                                                                                    \
        if (pRegDisplay->pCurrentContextPointers->reg != NULL)                              \
        {                                                                                   \
            STRESS_LOG3(LF_GCROOTS, LL_INFO100, "Updating " #reg " %p to %p from %p\n",     \
                    pContextRecord->reg,                                                    \
                    *pRegDisplay->pCurrentContextPointers->reg,                             \
                    pRegDisplay->pCurrentContextPointers->reg);                             \
            pContextRecord->reg = *pRegDisplay->pCurrentContextPointers->reg;               \
        }                                                                                   \
        else                                                                                \
        {                                                                                   \
            HANDLE_NULL_CONTEXT_POINTER;                                                    \
        }                                                                                   \
        if (pAbortContext)                                                                  \
        {                                                                                   \
            pAbortContext->reg = pContextRecord->reg;                                       \
        }                                                                                   \
    } while (0)


#if defined(_TARGET_X86_)

    UPDATEREG(Ebx);
    UPDATEREG(Esi);
    UPDATEREG(Edi);
    UPDATEREG(Ebp);

#elif defined(_TARGET_AMD64_)

    UPDATEREG(Rbx);
    UPDATEREG(Rbp);
#ifndef UNIX_AMD64_ABI
    UPDATEREG(Rsi);
    UPDATEREG(Rdi);
#endif    
    UPDATEREG(R12);
    UPDATEREG(R13);
    UPDATEREG(R14);
    UPDATEREG(R15);

#elif defined(_TARGET_ARM_)

    UPDATEREG(R4);
    UPDATEREG(R5);
    UPDATEREG(R6);
    UPDATEREG(R7);
    UPDATEREG(R8);
    UPDATEREG(R9);
    UPDATEREG(R10);
    UPDATEREG(R11);

#elif defined(_TARGET_ARM64_)

    UPDATEREG(X19);
    UPDATEREG(X20);
    UPDATEREG(X21);
    UPDATEREG(X22);
    UPDATEREG(X23);
    UPDATEREG(X24);
    UPDATEREG(X25);
    UPDATEREG(X26);
    UPDATEREG(X27);
    UPDATEREG(X28);
    UPDATEREG(Fp);

#else
    PORTABILITY_ASSERT("ExceptionTracker::UpdateNonvolatileRegisters");
#endif

#undef UPDATEREG
}


#ifndef _DEBUG
#define DebugLogExceptionRecord(pExceptionRecord)
#else // _DEBUG
#define LOG_FLAG(name)  \
    if (flags & name) \
    { \
        LOG((LF_EH, LL_INFO100, "" #name " ")); \
    } \

void DebugLogExceptionRecord(EXCEPTION_RECORD* pExceptionRecord)
{
    ULONG flags = pExceptionRecord->ExceptionFlags;

    EH_LOG((LL_INFO100, ">>exr: %p, code: %08x, addr: %p, flags: 0x%02x ", pExceptionRecord, pExceptionRecord->ExceptionCode, pExceptionRecord->ExceptionAddress, flags));

    LOG_FLAG(EXCEPTION_NONCONTINUABLE);
    LOG_FLAG(EXCEPTION_UNWINDING);
    LOG_FLAG(EXCEPTION_EXIT_UNWIND);
    LOG_FLAG(EXCEPTION_STACK_INVALID);
    LOG_FLAG(EXCEPTION_NESTED_CALL);
    LOG_FLAG(EXCEPTION_TARGET_UNWIND);
    LOG_FLAG(EXCEPTION_COLLIDED_UNWIND);

    LOG((LF_EH, LL_INFO100, "\n"));

}

LPCSTR DebugGetExceptionDispositionName(EXCEPTION_DISPOSITION disp)
{

    switch (disp)
    {
        case ExceptionContinueExecution:    return "ExceptionContinueExecution";
        case ExceptionContinueSearch:       return "ExceptionContinueSearch";
        case ExceptionNestedException:      return "ExceptionNestedException";
        case ExceptionCollidedUnwind:       return "ExceptionCollidedUnwind";
        default:
            UNREACHABLE_MSG("Invalid EXCEPTION_DISPOSITION!");
    }
}
#endif // _DEBUG

bool ExceptionTracker::IsStackOverflowException()
{
    if (m_pThread->GetThrowableAsHandle() == g_pPreallocatedStackOverflowException)
    {
        return true;
    }

    return false;
}

UINT_PTR ExceptionTracker::CallCatchHandler(CONTEXT* pContextRecord, bool* pfAborting /*= NULL*/)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        THROWS;

        PRECONDITION(CheckPointer(pContextRecord, NULL_OK));
    }
    CONTRACTL_END;

    UINT_PTR    uResumePC = 0;
    ULONG_PTR   ulRelOffset;
    StackFrame  sfStackFp       = m_sfResumeStackFrame;
    Thread*     pThread         = m_pThread;
    MethodDesc* pMD             = m_pMethodDescOfCatcher;
    bool        fIntercepted    = false;

    ThreadExceptionState* pExState = pThread->GetExceptionState();

#if defined(DEBUGGING_SUPPORTED)

    // If the exception is intercepted, use the information stored in the DebuggerExState to resume the
    // exception instead of calling the catch clause (there may not even be one).
    if (pExState->GetFlags()->DebuggerInterceptInfo())
    {
        _ASSERTE(pMD != NULL);

        // retrieve the interception information
        pExState->GetDebuggerState()->GetDebuggerInterceptInfo(NULL, NULL, (PBYTE*)&(sfStackFp.SP), &ulRelOffset, NULL);

        PCODE pStartAddress = pMD->GetNativeCode();

        EECodeInfo codeInfo(pStartAddress);
        _ASSERTE(codeInfo.IsValid());

        // Note that the value returned for ulRelOffset is actually the offset,
        // so we need to adjust it to get the actual IP.
        _ASSERTE(FitsIn<DWORD>(ulRelOffset));
        uResumePC = codeInfo.GetJitManager()->GetCodeAddressForRelOffset(codeInfo.GetMethodToken(), static_cast<DWORD>(ulRelOffset));

        // Either we haven't set m_uResumeStackFrame (for unhandled managed exceptions), or we have set it
        // and it equals to MemoryStackFp.
        _ASSERTE(m_sfResumeStackFrame.IsNull() || m_sfResumeStackFrame == sfStackFp);

        fIntercepted = true;
    }
#endif // DEBUGGING_SUPPORTED

    _ASSERTE(!sfStackFp.IsNull());

    m_sfResumeStackFrame.Clear();
    m_pMethodDescOfCatcher  = NULL;

    _ASSERTE(pContextRecord);

    //
    // call the handler
    //
    EH_LOG((LL_INFO100, "  calling catch at 0x%p\n", m_uCatchToCallPC));

    // do not call the catch clause if the exception is intercepted
    if (!fIntercepted)
    {
        _ASSERTE(m_uCatchToCallPC != 0 && m_pClauseForCatchToken != NULL);
        uResumePC = CallHandler(m_uCatchToCallPC, sfStackFp, &m_ClauseForCatch, pMD, Catch X86_ARG(pContextRecord) ARM_ARG(pContextRecord) ARM64_ARG(pContextRecord));
    }
    else
    {
        // Since the exception has been intercepted and we could resuming execution at any
        // user-specified arbitary location, reset the EH clause index and EstablisherFrame
        //  we may have saved for addressing any potential ThreadAbort raise.
        //
        // This is done since the saved EH clause index is related to the catch block executed,
        // which does not happen in interception. As user specifies where we resume execution,
        // we let that behaviour override the index and pretend as if we have no index available.
        m_dwIndexClauseForCatch = 0;
        m_sfEstablisherOfActualHandlerFrame.Clear();
        m_sfCallerOfActualHandlerFrame.Clear();
    }

    EH_LOG((LL_INFO100, "  resume address should be 0x%p\n", uResumePC));

    //
    // Our tracker may have gone away at this point, don't reference it.
    //

    return FinishSecondPass(pThread, uResumePC, sfStackFp, pContextRecord, this, pfAborting);
}

// static
UINT_PTR ExceptionTracker::FinishSecondPass(
            Thread* pThread,
            UINT_PTR uResumePC,
            StackFrame sf,
            CONTEXT* pContextRecord,
            ExceptionTracker* pTracker,
            bool* pfAborting /*= NULL*/)
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_NOTRIGGER;
        NOTHROW;
        PRECONDITION(CheckPointer(pThread, NULL_NOT_OK));
        PRECONDITION(CheckPointer((void*)uResumePC, NULL_NOT_OK));
        PRECONDITION(CheckPointer(pContextRecord, NULL_OK));
    }
    CONTRACTL_END;

    // Between the time when we pop the ExceptionTracker for the current exception and the time
    // when we actually resume execution, it is unsafe to start a funclet-skipping stackwalk.
    // So we set a flag here to indicate that we are in this time window.  The only user of this
    // information right now is the profiler.
    ThreadExceptionFlagHolder tefHolder(ThreadExceptionState::TEF_InconsistentExceptionState);

#ifdef DEBUGGING_SUPPORTED
    // This must be done before we pop the trackers.
    BOOL fIntercepted     = pThread->GetExceptionState()->GetFlags()->DebuggerInterceptInfo();
#endif // DEBUGGING_SUPPORTED

    // Since we may [re]raise ThreadAbort post the catch block execution,
    // save the index, and Establisher, of the EH clause corresponding to the handler
    // we just executed before we release the tracker. This will be used to ensure that reraise
    // proceeds forward and not get stuck in a loop. Refer to
    // ExceptionTracker::ProcessManagedCallFrame for details.
    DWORD ehClauseCurrentHandlerIndex = pTracker->GetCatchHandlerExceptionClauseIndex();
    StackFrame sfEstablisherOfActualHandlerFrame = pTracker->GetEstablisherOfActualHandlingFrame();

    EH_LOG((LL_INFO100, "second pass finished\n"));
    EH_LOG((LL_INFO100, "cleaning up ExceptionTracker state\n"));
    
    // Release the exception trackers till the current (specified) frame.
    ExceptionTracker::PopTrackers(sf, true);

    // This will set the last thrown to be either null if we have handled all the exceptions in the nested chain or
    // to whatever the current exception is.
    //
    // In a case when we're nested inside another catch block, the domain in which we're executing may not be the
    // same as the one the domain of the throwable that was just made the current throwable above. Therefore, we
    // make a special effort to preserve the domain of the throwable as we update the the last thrown object.
    //
    // If an exception is active, we dont want to reset the LastThrownObject to NULL as the active exception
    // might be represented by a tracker created in the second pass (refer to
    // CEHelper::SetupCorruptionSeverityForActiveExceptionInUnwindPass to understand how exception trackers can be
    // created in the 2nd pass on 64bit) that does not have a throwable attached to it. Thus, if this exception
    // is caught in the VM and it attempts to get the LastThrownObject using GET_THROWABLE macro, then it should be available.
    //
    // But, if the active exception tracker remains consistent in the 2nd pass (which will happen if the exception is caught
    // in managed code), then the call to SafeUpdateLastThrownObject below will automatically update the LTO as per the
    // active exception.
    if (!pThread->GetExceptionState()->IsExceptionInProgress())
    {
        pThread->SafeSetLastThrownObject(NULL);
    }

    // Sync managed exception state, for the managed thread, based upon any active exception tracker
    pThread->SyncManagedExceptionState(false);

    //
    // If we are aborting, we should not resume execution.  Instead, we raise another
    // exception.  However, we do this by resuming execution at our thread redirecter
    // function (RedirectForThrowControl), which is the same process we use for async
    // thread stops.  This redirecter function will cover the stack frame and register
    // stack frame and then throw an exception.  When we first see the exception thrown
    // by this redirecter, we fixup the context for the thread stackwalk by copying
    // pThread->m_OSContext into the dispatcher context and restarting the exception
    // dispatch.  As a result, we need to save off the "correct" resume context before
    // we resume so the exception processing can work properly after redirect.  A side
    // benefit of this mechanism is that it makes synchronous and async thread abort
    // use exactly the same codepaths.
    //
    UINT_PTR uAbortAddr = 0;

#if defined(DEBUGGING_SUPPORTED)
    // Don't honour thread abort requests at this time for intercepted exceptions.
    if (fIntercepted)
    {
        uAbortAddr = 0;
    }
    else
#endif // !DEBUGGING_SUPPORTED
    {
        CopyOSContext(pThread->m_OSContext, pContextRecord);
        SetIP(pThread->m_OSContext, (PCODE)uResumePC);
        uAbortAddr = (UINT_PTR)COMPlusCheckForAbort(uResumePC);
    }

    if (uAbortAddr)
    {
        if (pfAborting != NULL)
        {
            *pfAborting = true;
        }

        EH_LOG((LL_INFO100, "thread abort in progress, resuming thread under control...\n"));

        // We are aborting, so keep the reference to the current EH clause index.
        // We will use this when the exception is reraised and we begin commencing
        // exception dispatch. This is done in ExceptionTracker::ProcessOSExceptionNotification.
        // 
        // The "if" condition below can be false if the exception has been intercepted (refer to
        // ExceptionTracker::CallCatchHandler for details)
        if ((ehClauseCurrentHandlerIndex > 0) && (!sfEstablisherOfActualHandlerFrame.IsNull()))
        {
            pThread->m_dwIndexClauseForCatch = ehClauseCurrentHandlerIndex;
            pThread->m_sfEstablisherOfActualHandlerFrame = sfEstablisherOfActualHandlerFrame;
        }
        
        CONSISTENCY_CHECK(CheckPointer(pContextRecord));

        STRESS_LOG1(LF_EH, LL_INFO10, "resume under control: ip: %p\n", uResumePC);

#ifdef _TARGET_AMD64_
        pContextRecord->Rcx = uResumePC;
#elif defined(_TARGET_ARM_) || defined(_TARGET_ARM64_)
        // On ARM & ARM64, we save off the original PC in Lr. This is the same as done
        // in HandleManagedFault for H/W generated exceptions.
        pContextRecord->Lr = uResumePC;
#endif

        uResumePC = uAbortAddr;
    }

    CONSISTENCY_CHECK(pThread->DetermineIfGuardPagePresent());

    EH_LOG((LL_INFO100, "FinishSecondPass complete, uResumePC = %p, current SP = %p\n", uResumePC, GetCurrentSP()));
    return uResumePC;
}

// On CoreARM, the MemoryStackFp is ULONG when passed by RtlDispatchException,
// unlike its 64bit counterparts.
EXTERN_C EXCEPTION_DISPOSITION
ProcessCLRException(IN     PEXCEPTION_RECORD   pExceptionRecord
          WIN64_ARG(IN     ULONG64             MemoryStackFp)
      NOT_WIN64_ARG(IN     ULONG               MemoryStackFp),
                    IN OUT PCONTEXT            pContextRecord,
                    IN OUT PDISPATCHER_CONTEXT pDispatcherContext
                    )
{
    //
    // This method doesn't always return, so it will leave its
    // state on the thread if using dynamic contracts.
    //
    STATIC_CONTRACT_MODE_ANY;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_THROWS;

    // We must preserve this so that GCStress=4 eh processing doesnt kill last error.
    DWORD   dwLastError     = GetLastError();

    EXCEPTION_DISPOSITION   returnDisposition = ExceptionContinueSearch;

    STRESS_LOG5(LF_EH, LL_INFO10, "Processing exception at establisher=%p, ip=%p disp->cxr: %p, sp: %p, cxr @ exception: %p\n",
                                                        MemoryStackFp, pDispatcherContext->ControlPc,
                                                        pDispatcherContext->ContextRecord,
                                                        GetSP(pDispatcherContext->ContextRecord), pContextRecord);
    AMD64_ONLY(STRESS_LOG3(LF_EH, LL_INFO10, "                     rbx=%p, rsi=%p, rdi=%p\n", pContextRecord->Rbx, pContextRecord->Rsi, pContextRecord->Rdi));

    // sample flags early on because we may change pExceptionRecord below
    // if we are seeing a STATUS_UNWIND_CONSOLIDATE
    DWORD   dwExceptionFlags = pExceptionRecord->ExceptionFlags;
    Thread* pThread         = GetThread();

    // Stack Overflow is handled specially by the CLR EH mechanism. In fact
    // there are cases where we aren't in managed code, but aren't quite in
    // known unmanaged code yet either...
    //
    // These "boundary code" cases include:
    //  - in JIT helper methods which don't have a frame
    //  - in JIT helper methods before/during frame setup
    //  - in FCALL before/during frame setup
    //
    // In those cases on x86 we take special care to start our unwind looking
    // for a handler which is below the last explicit frame which has been
    // established on the stack as it can't reliably crawl the stack frames
    // above that.
    // NOTE: see code in the CLRVectoredExceptionHandler() routine.
    //
    // From the perspective of the EH subsystem, we can handle unwind correctly
    // even without erecting a transition frame on WIN64.  However, since the GC
    // uses the stackwalker to update object references, and since the stackwalker
    // relies on transition frame, we still cannot let an exception be handled
    // by an unprotected managed frame.
    //
    // This code below checks to see if a SO has occurred outside of managed code.
    // If it has, and if we don't have a transition frame higher up the stack, then
    // we don't handle the SO.
    if (!(dwExceptionFlags & EXCEPTION_UNWINDING))
    {
        if (pExceptionRecord->ExceptionCode == STATUS_STACK_OVERFLOW)
        {
            // We don't need to unwind the frame chain here because we have backstop
            // personality routines at the U2M boundary to handle do that.  They are
            // the personality routines of CallDescrWorker() and UMThunkStubCommon().
            //
            // See VSW 471619 for more information.

            // We should be in cooperative mode if we are going to handle the SO.
            // We track SO state for the thread.
            EEPolicy::HandleStackOverflow(SOD_ManagedFrameHandler, (void*)MemoryStackFp);
            FastInterlockAnd (&pThread->m_fPreemptiveGCDisabled, 0);
            return ExceptionContinueSearch;
        }
    }
    else
    {
        DWORD exceptionCode = pExceptionRecord->ExceptionCode;

        if ((NTSTATUS)exceptionCode == STATUS_UNWIND)
            // If exceptionCode is STATUS_UNWIND, RtlUnwind is called with a NULL ExceptionRecord,
            // therefore OS uses a faked ExceptionRecord with STATUS_UNWIND code.  Then we need to
            // look at our saved exception code.
            exceptionCode = GetCurrentExceptionCode();

        if (exceptionCode == STATUS_STACK_OVERFLOW)
        {
            return ExceptionContinueSearch;
        }
    }

    StackFrame sf((UINT_PTR)MemoryStackFp);


    {
        GCX_COOP();
        // Update the current establisher frame
        if (dwExceptionFlags & EXCEPTION_UNWINDING) 
        {
            ExceptionTracker *pCurrentTracker = pThread->GetExceptionState()->GetCurrentExceptionTracker();
            if (pCurrentTracker != NULL)
            {
                pCurrentTracker->SetCurrentEstablisherFrame(sf);
            }
        }

#ifdef _DEBUG
        Thread::ObjectRefFlush(pThread);
#endif // _DEBUG
    }


    //
    // begin Early Processing
    //
    {
#ifndef USE_REDIRECT_FOR_GCSTRESS
        if (IsGcMarker(pContextRecord, pExceptionRecord))
        {
            returnDisposition = ExceptionContinueExecution;
            goto lExit;
        }
#endif // !USE_REDIRECT_FOR_GCSTRESS

        EH_LOG((LL_INFO100, "..................................................................................\n"));
        EH_LOG((LL_INFO100, "ProcessCLRException enter, sp = 0x%p, ControlPc = 0x%p\n", MemoryStackFp, pDispatcherContext->ControlPc));
        DebugLogExceptionRecord(pExceptionRecord);

        if (STATUS_UNWIND_CONSOLIDATE == pExceptionRecord->ExceptionCode)
        {
            EH_LOG((LL_INFO100, "STATUS_UNWIND_CONSOLIDATE, retrieving stored exception record\n"));
            _ASSERTE(pExceptionRecord->NumberParameters >= 7);
            pExceptionRecord = (EXCEPTION_RECORD*)pExceptionRecord->ExceptionInformation[6];
            DebugLogExceptionRecord(pExceptionRecord);
        }

        CONSISTENCY_CHECK_MSG(!DebugIsEECxxException(pExceptionRecord), "EE C++ Exception leaked into managed code!!\n");
    }
    //
    // end Early Processing (tm) -- we're now into really processing an exception for managed code
    //

    if (!(dwExceptionFlags & EXCEPTION_UNWINDING))
    {
        // If the exception is a breakpoint, but outside of the runtime or managed code,
        //  let it go.  It is not ours, so someone else will handle it, or we'll see
        //  it again as an unhandled exception.
        if ((pExceptionRecord->ExceptionCode == STATUS_BREAKPOINT) ||
            (pExceptionRecord->ExceptionCode == STATUS_SINGLE_STEP))
        {
            // It is a breakpoint; is it from the runtime or managed code?
            PCODE ip = GetIP(pContextRecord); // IP of the fault.

            BOOL fExternalException;

            fExternalException = (!ExecutionManager::IsManagedCode(ip) &&
                                  !IsIPInModule(g_pMSCorEE, ip));

            if (fExternalException)
            {
                // The breakpoint was not ours.  Someone else can handle it.  (Or if not, we'll get it again as
                //  an unhandled exception.)
                returnDisposition = ExceptionContinueSearch;
                goto lExit;
            }
        }
    }

    {   
        BOOL bAsynchronousThreadStop = IsThreadHijackedForThreadStop(pThread, pExceptionRecord);

        // we already fixed the context in HijackHandler, so let's
        // just clear the thread state.
        pThread->ResetThrowControlForThread();

        ExceptionTracker::StackTraceState STState;

        ExceptionTracker*   pTracker  = ExceptionTracker::GetOrCreateTracker(
                                                pDispatcherContext->ControlPc,
                                                sf,
                                                pExceptionRecord,
                                                pContextRecord,
                                                bAsynchronousThreadStop,
                                                !(dwExceptionFlags & EXCEPTION_UNWINDING),
                                                &STState);
    
#ifdef FEATURE_CORRUPTING_EXCEPTIONS
        // Only setup the Corruption Severity in the first pass
        if (!(dwExceptionFlags & EXCEPTION_UNWINDING))
        {
            // Switch to COOP mode
            GCX_COOP();

            if (pTracker && pTracker->GetThrowable() != NULL)
            {
                // Setup the state in current exception tracker indicating the corruption severity
                // of the active exception.
                CEHelper::SetupCorruptionSeverityForActiveException((STState == ExceptionTracker::STS_FirstRethrowFrame), (pTracker->GetPreviousExceptionTracker() != NULL),
                                                                    CEHelper::ShouldTreatActiveExceptionAsNonCorrupting());
            }

            // Failfast if exception indicates corrupted process state            
            if (pTracker->GetCorruptionSeverity() == ProcessCorrupting)
            {
                OBJECTREF oThrowable = NULL;
                SString message;

                GCPROTECT_BEGIN(oThrowable);
                oThrowable = pTracker->GetThrowable();
                if (oThrowable != NULL)
                {
                    EX_TRY
                    {
                        GetExceptionMessage(oThrowable, message);
                    }
                    EX_CATCH
                    {
                    }
                    EX_END_CATCH(SwallowAllExceptions);
                }
                GCPROTECT_END();

                EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(pExceptionRecord->ExceptionCode, (LPCWSTR)message);
            }
        }
#endif // FEATURE_CORRUPTING_EXCEPTIONS

        {
            // Switch to COOP mode since we are going to work
            // with throwable
            GCX_COOP();
            if (pTracker->GetThrowable() != NULL)
            {
                BOOL fIsThrownExceptionAV = FALSE;
                OBJECTREF oThrowable = NULL;
                GCPROTECT_BEGIN(oThrowable);
                oThrowable = pTracker->GetThrowable();

                // Check if we are dealing with AV or not and if we are,
                // ensure that this is a real AV and not managed AV exception
                if ((pExceptionRecord->ExceptionCode == STATUS_ACCESS_VIOLATION) &&
                    (MscorlibBinder::GetException(kAccessViolationException) == oThrowable->GetMethodTable()))
                {
                    // Its an AV - set the flag
                    fIsThrownExceptionAV = TRUE;
                }

                GCPROTECT_END();

                // Did we get an AV?
                if (fIsThrownExceptionAV == TRUE)
                {
                    // Get the escalation policy action for handling AV
                    EPolicyAction actionAV = GetEEPolicy()->GetActionOnFailure(FAIL_AccessViolation);

                    // Valid actions are: eNoAction (default behviour) or eRudeExitProcess
                    _ASSERTE(((actionAV == eNoAction) || (actionAV == eRudeExitProcess)));
                    if (actionAV == eRudeExitProcess)
                    {
                        LOG((LF_EH, LL_INFO100, "ProcessCLRException: AccessViolation handler found and doing RudeExitProcess due to escalation policy (eRudeExitProcess)\n"));

                        // EEPolicy::HandleFatalError will help us RudeExit the process.
                        // RudeExitProcess due to AV is to prevent a security risk - we are ripping
                        // at the boundary, without looking for the handlers.
                        EEPOLICY_HANDLE_FATAL_ERROR(COR_E_SECURITY);
                    }
                }
            }
        }

#ifndef FEATURE_PAL // Watson is on Windows only
        // Setup bucketing details for nested exceptions (rethrow and non-rethrow) only if we are in the first pass
        if (!(dwExceptionFlags & EXCEPTION_UNWINDING))
        {
            ExceptionTracker *pPrevEHTracker = pTracker->GetPreviousExceptionTracker();
            if (pPrevEHTracker != NULL)
            {
                SetStateForWatsonBucketing((STState == ExceptionTracker::STS_FirstRethrowFrame), pPrevEHTracker->GetThrowableAsHandle());
            }
        }
#endif //!FEATURE_PAL

        CLRUnwindStatus                     status;
        
#ifdef USE_PER_FRAME_PINVOKE_INIT
        // Refer to comment in ProcessOSExceptionNotification about ICF and codegen difference.
        InlinedCallFrame *pICFSetAsLimitFrame = NULL;
#endif // USE_PER_FRAME_PINVOKE_INIT

        status = pTracker->ProcessOSExceptionNotification(
            pExceptionRecord,
            pContextRecord,
            pDispatcherContext,
            dwExceptionFlags,
            sf,
            pThread,
            STState
#ifdef USE_PER_FRAME_PINVOKE_INIT
            , (PVOID)pICFSetAsLimitFrame
#endif // USE_PER_FRAME_PINVOKE_INIT
            );

        if (FirstPassComplete == status)
        {
            EH_LOG((LL_INFO100, "first pass finished, found handler, TargetFrameSp = %p\n",
                        pDispatcherContext->EstablisherFrame));

            SetLastError(dwLastError);

#ifndef FEATURE_PAL
            //
            // At this point (the end of the 1st pass) we don't know where
            // we are going to resume to.  So, we pass in an address, which
            // lies in NULL pointer partition of the memory, as the target IP.
            //
            // Once we reach the target frame in the second pass unwind, we call
            // the catch funclet that caused us to resume execution and it
            // tells us where we are resuming to.  At that point, we patch
            // the context record with the resume IP and RtlUnwind2 finishes
            // by restoring our context at the right spot.
            //
            // If we are unable to set the resume PC for some reason, then
            // the OS will try to resume at the NULL partition address and the
            // attempt will fail due to AV, resulting in failfast, helping us
            // isolate problems in patching the IP.

            ClrUnwindEx(pExceptionRecord,
                        (UINT_PTR)pThread,
                        INVALID_RESUME_ADDRESS,
                        pDispatcherContext->EstablisherFrame);

            UNREACHABLE();
            //
            // doesn't return
            //
#else
            // On Unix, we will return ExceptionStackUnwind back to the custom
            // exception dispatch system. When it sees this disposition, it will
            // know that we want to handle the exception and will commence unwind
            // via the custom unwinder.
            return ExceptionStackUnwind;

#endif // FEATURE_PAL
        }
        else if (SecondPassComplete == status)
        {
            bool     fAborting = false;
            UINT_PTR uResumePC = (UINT_PTR)-1;
            UINT_PTR uOriginalSP = GetSP(pContextRecord);

            Frame* pLimitFrame = pTracker->GetLimitFrame();

            pDispatcherContext->ContextRecord = pContextRecord;

            // We may be in COOP mode at this point - the indefinite switch was done
            // in ExceptionTracker::ProcessManagedCallFrame.
            // 
            // However, if a finally was invoked non-exceptionally and raised an exception
            // that was caught in its parent method, unwind will result in invoking any applicable termination
            // handlers in the finally funclet and thus, also switching the mode to COOP indefinitely.
            // 
            // Since the catch block to be executed will lie in the parent method,
            // we will skip frames till we reach the parent and in the process, switch back to PREEMP mode 
            // as control goes back to the OS.
            // 
            // Upon reaching the target of unwind, we wont call ExceptionTracker::ProcessManagedCallFrame (since any 
            // handlers in finally or surrounding it will be invoked when we unwind finally funclet). Thus,
            // we may not be in COOP mode.
            // 
            // Since CallCatchHandler expects to be in COOP mode, perform the switch here.
            GCX_COOP_NO_DTOR();
            uResumePC = pTracker->CallCatchHandler(pContextRecord, &fAborting);

            {
                //
                // GC must NOT occur after the handler has returned until
                // we resume at the new address because the stackwalker
                // EnumGcRefs would try and report things as live from the
                // try body, that were probably reported dead from the
                // handler body.
                //
                // GC must NOT occur once the frames have been popped because
                // the values in the unwound CONTEXT are not GC-protected.
                //
                GCX_FORBID();

                CONSISTENCY_CHECK((UINT_PTR)-1 != uResumePC);

                // Ensure we are not resuming to the invalid target IP we had set at the end of
                // first pass
                _ASSERTE_MSG(INVALID_RESUME_ADDRESS != uResumePC, "CallCatchHandler returned invalid resume PC!");

                //
                // CallCatchHandler freed the tracker.
                //
                INDEBUG(pTracker = (ExceptionTracker*)POISONC);

                // Note that we should only fail to fix up for SO.
                bool fFixedUp = FixNonvolatileRegisters(uOriginalSP, pThread, pContextRecord, fAborting);
                _ASSERTE(fFixedUp || (pExceptionRecord->ExceptionCode == STATUS_STACK_OVERFLOW));


                CONSISTENCY_CHECK(pLimitFrame > dac_cast<PTR_VOID>(GetSP(pContextRecord)));
#ifdef USE_PER_FRAME_PINVOKE_INIT
                if (pICFSetAsLimitFrame != NULL)
                {
                    _ASSERTE(pICFSetAsLimitFrame == pLimitFrame);
                    
                    // Mark the ICF as inactive (by setting the return address as NULL).
                    // It will be marked as active at the next PInvoke callsite.
                    // 
                    // This ensures that any stackwalk post the catch handler but before
                    // the next pinvoke callsite does not see the frame as active.
                    pICFSetAsLimitFrame->Reset();
                }
#endif // USE_PER_FRAME_PINVOKE_INIT

                pThread->SetFrame(pLimitFrame);

                FixContext(pContextRecord);

                SetIP(pContextRecord, (PCODE)uResumePC);
            }

#ifdef STACK_GUARDS_DEBUG
            // We are transitioning back to managed code, so ensure that we are in
            // SO-tolerant mode before we do so.
            RestoreSOToleranceState();
#endif

            ExceptionTracker::ResumeExecution(pContextRecord,
                                              NULL
                                              );
            UNREACHABLE();        
        }
    }
    
lExit: ;

    EH_LOG((LL_INFO100, "returning %s\n", DebugGetExceptionDispositionName(returnDisposition)));
    CONSISTENCY_CHECK( !((dwExceptionFlags & EXCEPTION_TARGET_UNWIND) && (ExceptionContinueSearch == returnDisposition)));

    if ((ExceptionContinueSearch == returnDisposition))
    {
        GCX_PREEMP_NO_DTOR();
    }

    SetLastError(dwLastError);

    return returnDisposition;
}

// When we hit a native exception such as an AV in managed code, we put up a FaultingExceptionFrame which saves all the
// non-volatile registers.  The GC may update these registers if they contain object references.  However, the CONTEXT
// with which we are going to resume execution doesn't have these updated values.  Thus, we need to fix up the non-volatile
// registers in the CONTEXT with the updated ones stored in the FaultingExceptionFrame.  To do so properly, we need
// to perform a full stackwalk.
bool FixNonvolatileRegisters(UINT_PTR  uOriginalSP,
                             Thread*   pThread,
                             CONTEXT*  pContextRecord,
                             bool      fAborting
                             )
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    CONTEXT _ctx = {0};

    // Ctor will initialize it to NULL
    REGDISPLAY regdisp;

    pThread->FillRegDisplay(&regdisp, &_ctx);

    bool fFound = ExceptionTracker::FindNonvolatileRegisterPointers(pThread, uOriginalSP, &regdisp, GetFP(pContextRecord));
    if (!fFound)
    {
        return false;
    }

    {
        //
        // GC must NOT occur once the frames have been popped because
        // the values in the unwound CONTEXT are not GC-protected.
        //
        GCX_FORBID();

        ExceptionTracker::UpdateNonvolatileRegisters(pContextRecord, &regdisp, fAborting);
    }

    return true;
}




// static
void ExceptionTracker::InitializeCrawlFrameForExplicitFrame(CrawlFrame* pcfThisFrame, Frame* pFrame, MethodDesc *pMD)
{
    CONTRACTL
    {
        MODE_ANY;
        NOTHROW;
        GC_NOTRIGGER;

        PRECONDITION(pFrame != FRAME_TOP);
    }
    CONTRACTL_END;

    INDEBUG(memset(pcfThisFrame, 0xCC, sizeof(*pcfThisFrame)));

    pcfThisFrame->isFrameless = false;
    pcfThisFrame->pFrame = pFrame;
    pcfThisFrame->pFunc = pFrame->GetFunction();

    if (pFrame->GetVTablePtr() == InlinedCallFrame::GetMethodFrameVPtr() &&
        !InlinedCallFrame::FrameHasActiveCall(pFrame))
    {
        // Inactive ICFs in IL stubs contain the true interop MethodDesc which must be
        // reported in the stack trace.
        if (pMD->IsILStub() && pMD->AsDynamicMethodDesc()->HasMDContextArg())
        {
            // Report interop MethodDesc
            pcfThisFrame->pFunc = ((InlinedCallFrame *)pFrame)->GetActualInteropMethodDesc();
            _ASSERTE(pcfThisFrame->pFunc != NULL);
            _ASSERTE(pcfThisFrame->pFunc->SanityCheck());
        }
    }

    pcfThisFrame->pFirstGSCookie = NULL;
    pcfThisFrame->pCurGSCookie   = NULL;
}

// This method will initialize the RegDisplay in the CrawlFrame with the correct state for current and caller context
// See the long description of contexts and their validity in ExceptionTracker::InitializeCrawlFrame for details.
void ExceptionTracker::InitializeCurrentContextForCrawlFrame(CrawlFrame* pcfThisFrame, PT_DISPATCHER_CONTEXT pDispatcherContext,  StackFrame sfEstablisherFrame)
{
    CONTRACTL
    {
        MODE_ANY;
        NOTHROW;
        GC_NOTRIGGER;
        PRECONDITION(IsInFirstPass());
    }
    CONTRACTL_END;

    if (IsInFirstPass())
    {
        REGDISPLAY *pRD = pcfThisFrame->pRD;
        
#ifndef USE_CURRENT_CONTEXT_IN_FILTER
        INDEBUG(memset(pRD->pCurrentContext, 0xCC, sizeof(*(pRD->pCurrentContext))));
        // Ensure that clients can tell the current context isn't valid.
        SetIP(pRD->pCurrentContext, 0);
#else // !USE_CURRENT_CONTEXT_IN_FILTER
        RestoreNonvolatileRegisters(pRD->pCurrentContext, pDispatcherContext->CurrentNonVolatileContextRecord);
        RestoreNonvolatileRegisterPointers(pRD->pCurrentContextPointers, pDispatcherContext->CurrentNonVolatileContextRecord);
#endif // USE_CURRENT_CONTEXT_IN_FILTER

        *(pRD->pCallerContext)      = *(pDispatcherContext->ContextRecord);
        pRD->IsCallerContextValid   = TRUE;

        pRD->SP = sfEstablisherFrame.SP;
        pRD->ControlPC = pDispatcherContext->ControlPc;

#ifdef ESTABLISHER_FRAME_ADDRESS_IS_CALLER_SP
        pcfThisFrame->pRD->IsCallerSPValid = TRUE;
        
        // Assert our first pass assumptions for the Arm/Arm64
        _ASSERTE(sfEstablisherFrame.SP == GetSP(pDispatcherContext->ContextRecord));
#endif // ESTABLISHER_FRAME_ADDRESS_IS_CALLER_SP

    }

    EH_LOG((LL_INFO100, "ExceptionTracker::InitializeCurrentContextForCrawlFrame: DispatcherContext->ControlPC = %p; IP in DispatcherContext->ContextRecord = %p.\n",
                pDispatcherContext->ControlPc, GetIP(pDispatcherContext->ContextRecord)));
}

// static
void ExceptionTracker::InitializeCrawlFrame(CrawlFrame* pcfThisFrame, Thread* pThread, StackFrame sf, REGDISPLAY* pRD,
                                            PDISPATCHER_CONTEXT pDispatcherContext, DWORD_PTR ControlPCForEHSearch,
                                            UINT_PTR* puMethodStartPC,
                                            ExceptionTracker *pCurrentTracker)
{
    CONTRACTL
    {
        MODE_ANY;
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    INDEBUG(memset(pcfThisFrame, 0xCC, sizeof(*pcfThisFrame)));
    pcfThisFrame->pRD = pRD;

#ifdef FEATURE_INTERPRETER
    pcfThisFrame->pFrame = NULL;
#endif // FEATURE_INTERPRETER

    // Initialize the RegDisplay from DC->ContextRecord. DC->ControlPC always contains the IP
    // in the frame for which the personality routine was invoked.
    //
    // <AMD64>
    //
    // During 1st pass, DC->ContextRecord contains the context of the caller of the frame for which personality
    // routine was invoked. On the other hand, in the 2nd pass, it contains the context of the frame for which
    // personality routine was invoked.
    //
    // </AMD64>
    //
    // <ARM and ARM64>
    //
    // In the first pass on ARM & ARM64:
    //
    // 1) EstablisherFrame (passed as 'sf' to this method) represents the SP at the time
    //    the current managed method was invoked and thus, is the SP of the caller. This is
    //    the value of DispatcherContext->EstablisherFrame as well.
    // 2) DispatcherContext->ControlPC is the pc in the current managed method for which personality
    //    routine has been invoked.
    // 3) DispatcherContext->ContextRecord contains the context record of the caller (and thus, IP
    //    in the caller). Most of the times, these values will be distinct. However, recursion
    //    may result in them being the same (case "run2" of baseservices\Regression\V1\Threads\functional\CS_TryFinally.exe
    //    is an example). In such a case, we ensure that EstablisherFrame value is the same as
    //    the SP in DispatcherContext->ContextRecord (which is (1) above).
    //
    // In second pass on ARM & ARM64:
    //
    // 1) EstablisherFrame (passed as 'sf' to this method) represents the SP at the time
    //    the current managed method was invoked and thus, is the SP of the caller. This is
    //    the value of DispatcherContext->EstablisherFrame as well.
    // 2) DispatcherContext->ControlPC is the pc in the current managed method for which personality
    //    routine has been invoked.
    // 3) DispatcherContext->ContextRecord contains the context record of the current managed method
    //    for which the personality routine is invoked.
    //
    // </ARM and ARM64>
    pThread->InitRegDisplay(pcfThisFrame->pRD, pDispatcherContext->ContextRecord, true);

    bool fAdjustRegdisplayControlPC = false;

    // The "if" check below is trying to determine when we have a valid current context in DC->ContextRecord and whether, or not,
    // RegDisplay needs to be fixed up to set SP and ControlPC to have the values for the current frame for which personality routine
    // is invoked.
    //
    // We do this based upon the current pass for the exception tracker as this will also handle the case when current frame
    // and its caller have the same return address (i.e. ControlPc). This can happen in cases when, due to certain JIT optimizations, the following callstack
    //
    // A -> B -> A -> C
    //
    // Could get transformed to the one below when B is inlined in the first (left-most) A resulting in:
    //
    // A -> A -> C
    //
    // In this case, during 1st pass, when personality routine is invoked for the second A, DC->ControlPc could have the same
    // value as DC->ContextRecord->Rip even though the DC->ContextRecord actually represents caller context (of first A).
    // As a result, we will not initialize the value of SP and controlPC in RegDisplay for the current frame (frame for 
    // which personality routine was invoked - second A in the optimized scenario above) resulting in frame specific lookup (e.g. 
    // GenericArgType) to happen incorrectly (against first A).
    //
    // Thus, we should always use the pass identification in ExceptionTracker to determine when we need to perform the fixup below.
    if (pCurrentTracker->IsInFirstPass())
    {
        pCurrentTracker->InitializeCurrentContextForCrawlFrame(pcfThisFrame, pDispatcherContext, sf);
    }
    else
    {
#if defined(_TARGET_ARM_) || defined(_TARGET_ARM64_)
        // See the comment above the call to InitRegDisplay for this assertion.
        _ASSERTE(pDispatcherContext->ControlPc == GetIP(pDispatcherContext->ContextRecord));
#endif // _TARGET_ARM_ || _TARGET_ARM64_

#ifdef ESTABLISHER_FRAME_ADDRESS_IS_CALLER_SP
        // Simply setup the callerSP during the second pass in the caller context.
        // This is used in setting up the "EnclosingClauseCallerSP" in ExceptionTracker::ProcessManagedCallFrame
        // when the termination handlers are invoked.
        ::SetSP(pcfThisFrame->pRD->pCallerContext, sf.SP);
        pcfThisFrame->pRD->IsCallerSPValid = TRUE;
#endif // ESTABLISHER_FRAME_ADDRESS_IS_CALLER_SP
    }

#ifdef ADJUST_PC_UNWOUND_TO_CALL
    // Further below, we will adjust the ControlPC based upon whether we are at a callsite or not.
    // We need to do this for "RegDisplay.ControlPC" field as well so that when data structures like
    // EECodeInfo initialize themselves using this field, they will have the correct absolute value
    // that is in sync with the "relOffset" we calculate below.
    //
    // However, we do this *only* when "ControlPCForEHSearch" is the same as "DispatcherContext->ControlPC",
    // indicating we are not using the thread-abort reraise loop prevention logic.
    //
    if (pDispatcherContext->ControlPc == ControlPCForEHSearch)
    {
        // Since DispatcherContext->ControlPc is used to initialize the
        // RegDisplay.ControlPC field, assert that it is the same
        // as the ControlPC we are going to use to initialize the CrawlFrame
        // with as well.
        _ASSERTE(pcfThisFrame->pRD->ControlPC == ControlPCForEHSearch);
        fAdjustRegdisplayControlPC = true;

    }
#endif // ADJUST_PC_UNWOUND_TO_CALL

#if defined(_TARGET_ARM_)
    // Remove the Thumb bit
    ControlPCForEHSearch = ThumbCodeToDataPointer<DWORD_PTR, DWORD_PTR>(ControlPCForEHSearch);
#endif

#ifdef ADJUST_PC_UNWOUND_TO_CALL
    // If the OS indicated that the IP is a callsite, then adjust the ControlPC by decrementing it
    // by two. This is done because unwinding at callsite will make ControlPC point to the
    // instruction post the callsite. If a protected region ends "at" the callsite, then
    // not doing this adjustment will result in a one-off error that can result in us not finding
    // a handler.
    //
    // For async exceptions (e.g. AV), this will be false.
    //
    // We decrement by two to be in accordance with how the kernel does as well.
    if (pDispatcherContext->ControlPcIsUnwound)
    {
        ControlPCForEHSearch -= STACKWALK_CONTROLPC_ADJUST_OFFSET;
        if (fAdjustRegdisplayControlPC == true)
        {
            // Once the check above is removed, the assignment below should
            // be done unconditionally.
            pcfThisFrame->pRD->ControlPC = ControlPCForEHSearch;
            // On ARM & ARM64, the IP is either at the callsite (post the adjustment above)
            // or at the instruction at which async exception took place.
            pcfThisFrame->isIPadjusted = true;
        }
    }
#endif // ADJUST_PC_UNWOUND_TO_CALL

    pcfThisFrame->codeInfo.Init(ControlPCForEHSearch);
    
    if (pcfThisFrame->codeInfo.IsValid())
    {
        pcfThisFrame->isFrameless = true;
        pcfThisFrame->pFunc = pcfThisFrame->codeInfo.GetMethodDesc();

        *puMethodStartPC = pcfThisFrame->codeInfo.GetStartAddress();
    }
    else
    {
        pcfThisFrame->isFrameless = false;
        pcfThisFrame->pFunc = NULL;

        *puMethodStartPC = NULL;
    }

    pcfThisFrame->pThread = pThread;
    pcfThisFrame->hasFaulted = false;

    Frame* pTopFrame = pThread->GetFrame();
    pcfThisFrame->isIPadjusted = (FRAME_TOP != pTopFrame) && (pTopFrame->GetVTablePtr() != FaultingExceptionFrame::GetMethodFrameVPtr());
    if (pcfThisFrame->isFrameless && (pcfThisFrame->isIPadjusted == false) && (pcfThisFrame->GetRelOffset() == 0))
    {
        // If we are here, then either a hardware generated exception happened at the first instruction
        // of a managed method an exception was thrown at that location.
        //
        // Adjusting IP in such a case will lead us into unknown code  - it could be native code or some
        // other JITted code.
        //
        // Hence, we will flag that the IP is already adjusted.
        pcfThisFrame->isIPadjusted = true;

        EH_LOG((LL_INFO100, "ExceptionTracker::InitializeCrawlFrame: Exception at offset zero of the method (MethodDesc %p); setting IP as adjusted.\n",
                pcfThisFrame->pFunc));
    }

    pcfThisFrame->pFirstGSCookie = NULL;
    pcfThisFrame->pCurGSCookie   = NULL;

    pcfThisFrame->isFilterFuncletCached = FALSE;
}

bool ExceptionTracker::UpdateScannedStackRange(StackFrame sf, bool fIsFirstPass)
{
    CONTRACTL
    {
        // Since this function will modify the scanned stack range, which is also accessed during the GC stackwalk,
        // we invoke it in COOP mode so that that access to the range is synchronized.
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        THROWS;
    }
    CONTRACTL_END;

    //
    // collapse trackers if a nested exception passes a previous exception
    //

    HandleNestedExceptionEscape(sf, fIsFirstPass);

    //
    // update stack bounds
    //
    BOOL fUnwindingToFindResumeFrame = m_ExceptionFlags.UnwindingToFindResumeFrame();

    if (m_ScannedStackRange.Contains(sf))
    {
        // If we're unwinding to find the resume frame and we're examining the topmost previously scanned frame,
        // then we can't ignore it because we could resume here due to an escaped nested exception.
        if (!fUnwindingToFindResumeFrame || (m_ScannedStackRange.GetUpperBound() != sf))
        {
            // been there, done that.
            EH_LOG((LL_INFO100, "  IGNOREFRAME: This frame has been processed already\n"));
            return false;
        }
    }
    else
    {
        if (sf < m_ScannedStackRange.GetLowerBound())
        {
            m_ScannedStackRange.ExtendLowerBound(sf);
        }

        if (sf > m_ScannedStackRange.GetUpperBound())
        {
            m_ScannedStackRange.ExtendUpperBound(sf);
        }

        DebugLogTrackerRanges("  C");
    }

    return true;
}

void CheckForRudeAbort(Thread* pThread, bool fIsFirstPass)
{
    if (fIsFirstPass && pThread->IsRudeAbort())
    {
        GCX_COOP();
        OBJECTREF rudeAbortThrowable = CLRException::GetPreallocatedRudeThreadAbortException();
        if (pThread->GetThrowable() != rudeAbortThrowable)
        {
            pThread->SafeSetThrowables(rudeAbortThrowable);
        }

        if (!pThread->IsRudeAbortInitiated())
        {
            pThread->PreWorkForThreadAbort();
        }
    }
}

void ExceptionTracker::FirstPassIsComplete()
{
    m_ExceptionFlags.ResetUnwindingToFindResumeFrame();
    m_pSkipToParentFunctionMD = NULL;
}

void ExceptionTracker::SecondPassIsComplete(MethodDesc* pMD, StackFrame sfResumeStackFrame)
{
    EH_LOG((LL_INFO100, "  second pass unwind completed\n"));

    m_pMethodDescOfCatcher  = pMD;
    m_sfResumeStackFrame    = sfResumeStackFrame;
}

CLRUnwindStatus ExceptionTracker::ProcessOSExceptionNotification(
    PEXCEPTION_RECORD pExceptionRecord,
    PCONTEXT pContextRecord,
    PDISPATCHER_CONTEXT pDispatcherContext,
    DWORD dwExceptionFlags,
    StackFrame sf,
    Thread* pThread,
    StackTraceState STState
#ifdef USE_PER_FRAME_PINVOKE_INIT
    , PVOID pICFSetAsLimitFrame
#endif // USE_PER_FRAME_PINVOKE_INIT
)
{
    CONTRACTL
    {
        MODE_ANY;
        GC_TRIGGERS;
        THROWS;
    }
    CONTRACTL_END;

    CLRUnwindStatus status = UnwindPending;

    CrawlFrame cfThisFrame;
    REGDISPLAY regdisp;
    UINT_PTR   uMethodStartPC;
    UINT_PTR    uCallerSP;

    DWORD_PTR ControlPc = pDispatcherContext->ControlPc;

    ExceptionTracker::InitializeCrawlFrame(&cfThisFrame, pThread, sf, &regdisp, pDispatcherContext, ControlPc, &uMethodStartPC, this);

#ifndef ESTABLISHER_FRAME_ADDRESS_IS_CALLER_SP
    uCallerSP = EECodeManager::GetCallerSp(cfThisFrame.pRD);
#else // !ESTABLISHER_FRAME_ADDRESS_IS_CALLER_SP
    uCallerSP = sf.SP;
#endif // ESTABLISHER_FRAME_ADDRESS_IS_CALLER_SP

    EH_LOG((LL_INFO100, "ProcessCrawlFrame: PSP: " FMT_ADDR " EstablisherFrame: " FMT_ADDR "\n", DBG_ADDR(uCallerSP), DBG_ADDR(sf.SP)));

    bool fIsFirstPass   = !(dwExceptionFlags & EXCEPTION_UNWINDING);
    bool fTargetUnwind  = !!(dwExceptionFlags & EXCEPTION_TARGET_UNWIND);

    // If a thread abort was raised after a catch block's execution, we would have saved
    // the index and EstablisherFrame of the EH clause corresponding to the handler that executed. 
    // Fetch that locally and reset the state against the thread if we are in the unwind pass.
    //
    // It should be kept in mind that by the virtue of copying the information below, we will
    // have it available for the first frame seen during the unwind pass (which will be the
    // frame where ThreadAbort was raised after the catch block) for us to skip any termination
    // handlers that may be present prior to the EH clause whose index we saved.
    DWORD dwTACatchHandlerClauseIndex = pThread->m_dwIndexClauseForCatch;
    StackFrame sfEstablisherOfActualHandlerFrame = pThread->m_sfEstablisherOfActualHandlerFrame;
    if (!fIsFirstPass)
    {
        pThread->m_dwIndexClauseForCatch = 0;
        pThread->m_sfEstablisherOfActualHandlerFrame.Clear();
    }

    bool    fProcessThisFrame   = false;
    bool    fCrawlFrameIsDirty  = false;

    // <GC_FUNCLET_REFERENCE_REPORTING>
    // 
    // Refer to the detailed comment in ExceptionTracker::ProcessManagedCallFrame for more context.
    // In summary, if we have reached the target of the unwind, then we need to fix CallerSP (for 
    // GC reference reporting) if we have been asked to.
    //
    // This will be done only when we reach the frame that is handling the exception.
    //
    // </GC_FUNCLET_REFERENCE_REPORTING>
    if (fTargetUnwind && (m_fFixupCallerSPForGCReporting == true))
    {
        m_fFixupCallerSPForGCReporting = false;
        this->m_EnclosingClauseInfoForGCReporting.SetEnclosingClauseCallerSP(uCallerSP);
    }
    
#ifdef USE_PER_FRAME_PINVOKE_INIT
    // Refer to detailed comment below.
    PTR_Frame pICFForUnwindTarget = NULL;
#endif // USE_PER_FRAME_PINVOKE_INIT

    CheckForRudeAbort(pThread, fIsFirstPass);

    bool fIsFrameLess = cfThisFrame.IsFrameless();
    GSCookie* pGSCookie = NULL;
    bool fSetLastUnwoundEstablisherFrame = false;

    //
    // process any frame since the last frame we've seen
    //
    {
        GCX_COOP_THREAD_EXISTS(pThread);

        // UpdateScannedStackRange needs to be invoked in COOP mode since
        // the stack range can also be accessed during GC stackwalk.
        fProcessThisFrame = UpdateScannedStackRange(sf, fIsFirstPass);

        MethodDesc *pMD = cfThisFrame.GetFunction();

        Frame*  pFrame = GetLimitFrame(); // next frame to process
        if (pFrame != FRAME_TOP)
        {
            // The following function call sets the GS cookie pointers and checks the cookie.
            cfThisFrame.SetCurGSCookie(Frame::SafeGetGSCookiePtr(pFrame));
        }

        while (((UINT_PTR)pFrame) < uCallerSP)
        {
#ifdef USE_PER_FRAME_PINVOKE_INIT
            // InlinedCallFrames (ICF) are allocated, initialized and linked to the Frame chain
            // by the code generated by the JIT for a method containing a PInvoke.
            //
            // On X64, JIT generates code to dynamically link and unlink the ICF around
            // each PInvoke call. On ARM, on the other hand, JIT's codegen, in context of ICF,
            // is more inline with X86 and thus, it links in the ICF at the start of the method
            // and unlinks it towards the method end. Thus, ICF is present on the Frame chain
            // at any given point so long as the method containing the PInvoke is on the stack.
            //
            // Now, if the method containing ICF catches an exception, we will reset the Frame chain
            // with the LimitFrame, that is computed below, after the catch handler returns. Since this 
            // computation is done relative to the CallerSP (on both X64 and ARM), we will end up 
            // removing the ICF from the Frame chain as that will always be below (stack growing down) 
            // the CallerSP since it lives in the stack space of the current managed frame.
            //
            // As a result, if there is another PInvoke call after the catch block, it will expect
            // the ICF to be present and without one, execution will go south.
            //
            // To account for this ICF codegen difference, in the EH system we check if the current 
            // Frame is an ICF or not. If it is and lies inside the current managed method, we 
            // keep a reference to it and reset the LimitFrame to this saved reference before we
            // return back to invoke the catch handler. 
            //
            // Thus, if there is another PInvoke call post the catch handler, it will find ICF as expected.
            //
            // This is based upon the following assumptions:
            //
            // 1) There will be no other explicit Frame inserted above the ICF inside the
            //    managed method containing ICF. That is, ICF is the top-most explicit frame
            //    in the managed method (and thus, lies in the current managed frame).
            //
            // 2) There is only one ICF per managed method containing one (or more) PInvoke(s).
            //
            // 3) We only do this if the current frame is the one handling the exception. This is to
            //    address the scenario of keeping any ICF from frames lower in the stack active.
            //
            // 4) The ExceptionUnwind method of the ICF is a no-op. As noted above, we save a reference
            //    to the ICF and yet continue to process the frame chain. During unwind, this implies
            //    that we will end up invoking the ExceptionUnwind methods of all frames that lie
            //    below the caller SP of the managed frame handling the exception. And since the handling 
            //    managed frame contains an ICF, it will be the topmost frame that will lie
            //    below the callerSP for which we will invoke ExceptionUnwind. 
            //
            //    Thus, ICF::ExceptionUnwind should not do anything significant. If any of these assumptions
            //    break, then the next best thing will be to make the JIT link/unlink the frame dynamically.
            //
            // If the current method executing is from precompiled ReadyToRun code, then the above is no longer
            // applicable because each PInvoke is wrapped by calls to the JIT_PInvokeBegin and JIT_PInvokeEnd
            // helpers, which push and pop the ICF to the current thread. Unlike jitted code, the ICF is not
            // linked during the method prolog, and unlinked at the epilog (it looks more like the X64 case).
            // In that case, we need to unlink the ICF during unwinding here.

            if (fTargetUnwind && (pFrame->GetVTablePtr() == InlinedCallFrame::GetMethodFrameVPtr()))
            {
                PTR_InlinedCallFrame pICF = (PTR_InlinedCallFrame)pFrame;
                // Does it live inside the current managed method? It will iff:
                //
                // 1) ICF address is higher than the current frame's SP (which we get from DispatcherContext), AND
                // 2) ICF address is below callerSP.
                if ((GetSP(pDispatcherContext->ContextRecord) < (TADDR)pICF) && 
                    ((UINT_PTR)pICF < uCallerSP)) 
                {
                    pICFForUnwindTarget = pFrame;

                    // When unwinding an exception in ReadyToRun, the JIT_PInvokeEnd helper which unlinks the ICF from 
                    // the thread will be skipped. This is because unlike jitted code, each pinvoke is wrapped by calls
                    // to the JIT_PInvokeBegin and JIT_PInvokeEnd helpers, which push and pop the ICF on the thread. The
                    // ICF is not linked at the method prolog and unlined at the epilog when running R2R code. Since the
                    // JIT_PInvokeEnd helper will be skipped, we need to unlink the ICF here. If the executing method
                    // has another pinovoke, it will re-link the ICF again when the JIT_PInvokeBegin helper is called

                    if (ExecutionManager::IsReadyToRunCode(((InlinedCallFrame*)pFrame)->m_pCallerReturnAddress))
                    {
                        pICFForUnwindTarget = pICFForUnwindTarget->Next();
                    }
                }
            }
#endif // USE_PER_FRAME_PINVOKE_INIT

            cfThisFrame.CheckGSCookies();

            if (fProcessThisFrame)
            {
                ExceptionTracker::InitializeCrawlFrameForExplicitFrame(&cfThisFrame, pFrame, pMD);
                fCrawlFrameIsDirty = true;

                status = ProcessExplicitFrame(
                                               &cfThisFrame,
                                               sf,
                                               fIsFirstPass,
                                               STState);
                cfThisFrame.CheckGSCookies();
            }

            if (!fIsFirstPass)
            {
                //
                // notify Frame of unwind
                //
                pFrame->ExceptionUnwind();

                // If we have not yet set the initial explicit frame processed by this tracker, then 
                // set it now.
                if (m_pInitialExplicitFrame == NULL)
                {
                    m_pInitialExplicitFrame = pFrame;
                }
            }

            pFrame = pFrame->Next();
            m_pLimitFrame = pFrame;

            if (UnwindPending != status)
            {
                goto lExit;
            }
        }

        if (fCrawlFrameIsDirty)
        {
            // If crawlframe is dirty, it implies that it got modified as part of explicit frame processing. Thus, we shall
            // reinitialize it here.
            ExceptionTracker::InitializeCrawlFrame(&cfThisFrame, pThread, sf, &regdisp, pDispatcherContext, ControlPc, &uMethodStartPC, this);
        }

        if (fIsFrameLess)
        {
            pGSCookie = (GSCookie*)cfThisFrame.GetCodeManager()->GetGSCookieAddr(cfThisFrame.pRD,
                                                                                          &cfThisFrame.codeInfo,
                                                                                          &cfThisFrame.codeManState);
            if (pGSCookie)
            {
                // The following function call sets the GS cookie pointers and checks the cookie.
                cfThisFrame.SetCurGSCookie(pGSCookie);
            }

            status = HandleFunclets(&fProcessThisFrame, fIsFirstPass,
                cfThisFrame.GetFunction(), cfThisFrame.IsFunclet(), sf);
        }

        if ((!fIsFirstPass) && (!fProcessThisFrame))
        {
            // If we are unwinding and not processing the current frame, it implies that
            // this frame has been unwound for one of the following reasons:
            //
            // 1) We have already seen it due to nested exception processing, OR
            // 2) We are skipping frames to find a funclet's parent and thus, its been already
            //    unwound.
            //
            // If the current frame is NOT the target of unwind, update the last unwound
            // establisher frame. We don't do this for "target of unwind" since it has the catch handler, for a
            // duplicate EH clause reported in the funclet, that needs to be invoked and thus, may have valid 
            // references to report for GC reporting.
            //
            // If we are not skipping the managed frame, then LastUnwoundEstablisherFrame will be updated later in this method,
            // just before we return back to our caller.
            if (!fTargetUnwind)
            {
                SetLastUnwoundEstablisherFrame(sf);
                fSetLastUnwoundEstablisherFrame = true;
            }
        }

        // GCX_COOP_THREAD_EXISTS ends here and we may switch to preemp mode now (if applicable).
    }

    //
    // now process managed call frame if needed
    //
    if (fIsFrameLess)
    {
        if (fProcessThisFrame)
        {
            status = ProcessManagedCallFrame(
                                           &cfThisFrame,
                                           sf,
                                           StackFrame::FromEstablisherFrame(pDispatcherContext->EstablisherFrame),
                                           pExceptionRecord,
                                           STState,
                                           uMethodStartPC,
                                           dwExceptionFlags,
                                           dwTACatchHandlerClauseIndex,
                                           sfEstablisherOfActualHandlerFrame);

            if (pGSCookie)
            {
                cfThisFrame.CheckGSCookies();
            }
        }

        if (fTargetUnwind && (UnwindPending == status))
        {
            SecondPassIsComplete(cfThisFrame.GetFunction(), sf);
            status = SecondPassComplete;
        }
    }

lExit:

    // If we are unwinding and have returned successfully from unwinding the frame, then mark it as the last unwound frame for the current
    // exception. We don't do this if the frame is target of unwind (i.e. handling the exception) since catch block invocation may have references to be
    // reported (if a GC happens during catch block invocation).
    //
    // If an exception escapes out of a funclet (this is only possible for fault/finally/catch clauses), then we will not return here.
    // Since this implies that the funclet no longer has any valid references to report, we will need to set the LastUnwoundEstablisherFrame
    // close to the point we detect the exception has escaped the funclet. This is done in ExceptionTracker::CallHandler and marks the 
    // frame that invoked (and thus, contained) the funclet as the LastUnwoundEstablisherFrame.
    //
    // Note: Do no add any GC triggering code between the return from ProcessManagedCallFrame and setting of the LastUnwoundEstablisherFrame
    if ((!fIsFirstPass) && (!fTargetUnwind) && (!fSetLastUnwoundEstablisherFrame))
    {
        GCX_COOP();
        SetLastUnwoundEstablisherFrame(sf);
    }

    if (FirstPassComplete == status)
    {
        FirstPassIsComplete();
    }
    
    if (fTargetUnwind && (status == SecondPassComplete))
    {
#ifdef USE_PER_FRAME_PINVOKE_INIT
        // If we have got a ICF to set as the LimitFrame, do that now.
        // The Frame chain is still intact and would be updated using
        // the LimitFrame (done after the catch handler returns).
        //
        // NOTE: This should be done as the last thing before we return
        //       back to invoke the catch handler.
        if (pICFForUnwindTarget != NULL)
        {
            m_pLimitFrame = pICFForUnwindTarget;
            pICFSetAsLimitFrame = (PVOID)pICFForUnwindTarget;
        }
#endif // USE_PER_FRAME_PINVOKE_INIT

        // Since second pass is complete and we have reached
        // the frame containing the catch funclet, reset the enclosing 
        // clause SP for the catch funclet, if applicable, to be the CallerSP of the
        // current frame.
        // 
        // Refer to the detailed comment about this code
        // in ExceptionTracker::ProcessManagedCallFrame.
        if (m_fResetEnclosingClauseSPForCatchFunclet)
        {
#ifdef ESTABLISHER_FRAME_ADDRESS_IS_CALLER_SP
            // DispatcherContext->EstablisherFrame's value
            // represents the CallerSP of the current frame.
            UINT_PTR EnclosingClauseCallerSP = (UINT_PTR)pDispatcherContext->EstablisherFrame;
#else // ESTABLISHER_FRAME_ADDRESS_IS_CALLER_SP
            // Extract the CallerSP from RegDisplay
            REGDISPLAY *pRD = cfThisFrame.GetRegisterSet();
            _ASSERTE(pRD->IsCallerContextValid || pRD->IsCallerSPValid);
            UINT_PTR EnclosingClauseCallerSP = (UINT_PTR)GetSP(pRD->pCallerContext);
#endif // !ESTABLISHER_FRAME_ADDRESS_IS_CALLER_SP
            m_EnclosingClauseInfo = EnclosingClauseInfo(false, cfThisFrame.GetRelOffset(), EnclosingClauseCallerSP);
        }
        m_fResetEnclosingClauseSPForCatchFunclet = FALSE;
    }
   
    // If we are unwinding and the exception was not caught in managed code and we have reached the
    // topmost frame we saw in the first pass, then reset thread abort state if this is the last managed
    // code personality routine on the stack.
    if ((fIsFirstPass == false) && (this->GetTopmostStackFrameFromFirstPass() == sf) && (GetCatchToCallPC() == NULL))
    {
        ExceptionTracker::ResetThreadAbortStatus(pThread, &cfThisFrame, sf);
    }

    //
    // fill in the out parameter
    //
    return status;
}

// static
void ExceptionTracker::DebugLogTrackerRanges(__in_z const char *pszTag)
{
#ifdef _DEBUG
    CONTRACTL
    {
        MODE_ANY;
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACTL_END;

    Thread*             pThread     = GetThread();
    ExceptionTracker*   pTracker    = pThread ? pThread->GetExceptionState()->m_pCurrentTracker : NULL;

    int i = 0;

    while (pTracker)
    {
        EH_LOG((LL_INFO100, "%s:|%02d| %p: (%p %p) %s\n", pszTag, i, pTracker, pTracker->m_ScannedStackRange.GetLowerBound().SP, pTracker->m_ScannedStackRange.GetUpperBound().SP,
            pTracker->IsInFirstPass() ? "1st pass" : "2nd pass"
            ));
        pTracker = pTracker->m_pPrevNestedInfo;
        i++;
    }
#endif // _DEBUG
}


bool ExceptionTracker::HandleNestedExceptionEscape(StackFrame sf, bool fIsFirstPass)
{
    CONTRACTL
    {
        // Since this function can modify the scanned stack range, which is also accessed during the GC stackwalk,
        // we invoke it in COOP mode so that that access to the range is synchronized.
        MODE_COOPERATIVE;
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACTL_END;

    bool fResult = false;

    DebugLogTrackerRanges("  A");

    ExceptionTracker* pPreviousTracker   = m_pPrevNestedInfo;

    while (pPreviousTracker && pPreviousTracker->m_ScannedStackRange.IsSupersededBy(sf))
    {
        //
        // If the previous tracker (representing exception E1 and whose scanned stack range is superseded by the current frame)
        // is in the first pass AND current tracker (representing exceptio E2) has not seen the current frame AND we are here,
        // it implies that we had a nested exception while the previous tracker was in the first pass.
        //
        // This can happen in the following scenarios:
        //
        // 1) An exception escapes a managed filter (which are invoked in the first pass). However,
        //    that is not possible since any exception escaping them is swallowed by the runtime.
        //    If someone does longjmp from within the filter, then that is illegal and unsupported.
        //
        // 2) While processing an exception (E1), either us or native code caught it, triggering unwind. However, before the
        //    first managed frame was processed for unwind, another native frame (below the first managed frame on the stack)
        //    did a longjmp to go past us or raised another exception from one of their termination handlers.
        //
        //    Thus, we will never get a chance to switch our tracker for E1 to 2nd pass (which would be done when
        //    ExceptionTracker::GetOrCreateTracker will be invoked for the first managed frame) since the longjmp, or the
        //    new-exception would result in a new tracker being setup.
        //
        //    Below is an example of such a case that does longjmp
        //    ----------------------------------------------------
        //
        //    NativeA (does setjmp) -> ManagedFunc -> NativeB
        //
        //
        //    NativeB could be implemented as:
        //
        //    __try { //    raise exception } __finally { longjmp(jmp1, 1); }
        //
        //    "jmp1" is the jmp_buf setup by NativeA by calling setjmp.
        //
        //    ManagedFunc could be implemented as:
        //
        //    try {
        //     try { NativeB(); }
        //     finally { Console.WriteLine("Finally in ManagedFunc"); }
        //     }
        //     catch(Exception ex} { Console.WriteLine("Caught"); }
        //
        //
        //    In case of nested exception, we combine the stack range (see below) since we have already seen those frames
        //    in the specified pass for the previous tracker. However, in the example above, the current tracker (in 2nd pass)
        //    has not see the frames which the previous tracker (which is in the first pass) has seen.
        //
        //    On a similar note, the __finally in the example above could also do a "throw 1;". In such a case, we would expect
        //    that the catch in ManagedFunc would catch the exception (since "throw 1;" would be represented as SEHException in
        //    the runtime). However, during first pass, when the exception enters ManagedFunc, the current tracker would not have
        //    processed the ManagedFunc frame, while the previous tracker (for E1) would have. If we proceed to combine the stack
        //    ranges, we will omit examining the catch clause in ManagedFunc.
        //
        //    Thus, we cannot combine the stack range yet and must let each frame, already scanned by the previous
        //    tracker, be also processed by the current (longjmp) tracker if not already done.
        //
        //  Note: This is not a concern if the previous tracker (for exception E1) is in the second pass since any escaping exception (E2)
        //        would come out of a finally/fault funclet and the runtime's funclet skipping logic will deal with it correctly.

        if (pPreviousTracker->IsInFirstPass() && (!this->m_ScannedStackRange.Contains(sf)))
        {
            // Allow all stackframes seen by previous tracker to be seen by the current
            // tracker as well.
            if (sf <= pPreviousTracker->m_ScannedStackRange.GetUpperBound())
            {
                EH_LOG((LL_INFO100, "     - not updating current tracker bounds for escaped exception since\n"));
                EH_LOG((LL_INFO100, "     - active tracker (%p; %s) has not seen the current frame [", this, this->IsInFirstPass()?"FirstPass":"SecondPass"));
                EH_LOG((LL_INFO100, "     - SP = %p", sf.SP));
                EH_LOG((LL_INFO100, "]\n"));
                EH_LOG((LL_INFO100, "     - which the previous (%p) tracker has processed.\n", pPreviousTracker));
                return fResult;
            }
        }

        EH_LOG((LL_INFO100, "    nested exception ESCAPED\n"));
        EH_LOG((LL_INFO100, "    - updating current tracker stack bounds\n"));
        m_ScannedStackRange.CombineWith(sf, &pPreviousTracker->m_ScannedStackRange);

        //
        // Only the topmost tracker can be in the first pass.
        //
        // (Except in the case where we have an exception thrown in a filter,
        // which should never escape the filter, and thus, will never supersede
        // the previous exception.  This is why we cannot walk the entire list
        // of trackers to assert that they're all in the right mode.)
        //
        // CONSISTENCY_CHECK(!pPreviousTracker->IsInFirstPass());

        // If our modes don't match, don't actually delete the supersceded exception.
        // If we did, we would lose valueable state on which frames have been scanned
        // on the second pass if an exception is thrown during the 2nd pass.

        // Advance the current tracker pointer now, since it may be deleted below.
        pPreviousTracker = pPreviousTracker->m_pPrevNestedInfo;

        if (!fIsFirstPass)
        {

            // During unwind, at each frame we collapse exception trackers only once i.e. there cannot be multiple
            // exception trackers that are collapsed at each frame. Store the information of collapsed exception 
            // tracker in current tracker to be able to find the parent frame when nested exception escapes.
            m_csfEHClauseOfCollapsedTracker = m_pPrevNestedInfo->m_EHClauseInfo.GetCallerStackFrameForEHClause();
            m_EnclosingClauseInfoOfCollapsedTracker = m_pPrevNestedInfo->m_EnclosingClauseInfoForGCReporting;

            EH_LOG((LL_INFO100, "    - removing previous tracker\n"));

            ExceptionTracker* pTrackerToFree = m_pPrevNestedInfo;
            m_pPrevNestedInfo = pTrackerToFree->m_pPrevNestedInfo;

#if defined(DEBUGGING_SUPPORTED)
            if (g_pDebugInterface != NULL)
            {
                g_pDebugInterface->DeleteInterceptContext(pTrackerToFree->m_DebuggerExState.GetDebuggerInterceptContext());
            }
#endif // DEBUGGING_SUPPORTED

            CONSISTENCY_CHECK(pTrackerToFree->IsValid());
            FreeTrackerMemory(pTrackerToFree, memBoth);
        }

        DebugLogTrackerRanges("  B");
    }

    return fResult;
}

CLRUnwindStatus ExceptionTracker::ProcessExplicitFrame(
    CrawlFrame* pcfThisFrame,
    StackFrame sf,
    BOOL fIsFirstPass,
    StackTraceState& STState
    )
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        THROWS;
        PRECONDITION(!pcfThisFrame->IsFrameless());
        PRECONDITION(pcfThisFrame->GetFrame() != FRAME_TOP);
    }
    CONTRACTL_END;

    Frame* pFrame = pcfThisFrame->GetFrame();

    EH_LOG((LL_INFO100, "  [ ProcessExplicitFrame: pFrame: " FMT_ADDR " pMD: " FMT_ADDR " %s PASS ]\n", DBG_ADDR(pFrame), DBG_ADDR(pFrame->GetFunction()), fIsFirstPass ? "FIRST" : "SECOND"));

    if (FRAME_TOP == pFrame)
    {
        goto lExit;
    }

    if (!m_ExceptionFlags.UnwindingToFindResumeFrame())
    {
        //
        // update our exception stacktrace
        //

        BOOL bReplaceStack      = FALSE;
        BOOL bSkipLastElement   = FALSE;

        if (STS_FirstRethrowFrame == STState)
        {
            bSkipLastElement = TRUE;
        }
        else
        if (STS_NewException == STState)
        {
            bReplaceStack    = TRUE;
        }

        // Normally, we need to notify the profiler in two cases:
        // 1) a brand new exception is thrown, and
        // 2) an exception is rethrown.
        // However, in this case, if the explicit frame doesn't correspond to a MD, we don't set STState to STS_Append,
        // so the next managed call frame we process will give another ExceptionThrown() callback to the profiler.
        // So we give the callback below, only in the case when we append to the stack trace.

        MethodDesc* pMD = pcfThisFrame->GetFunction();
        if (pMD)
        {
            Thread* pThread = m_pThread;

            if (fIsFirstPass)
            {
                //
                // notify profiler of new/rethrown exception
                //
                if (bSkipLastElement || bReplaceStack)
                {
                    GCX_COOP();
                    EEToProfilerExceptionInterfaceWrapper::ExceptionThrown(pThread);
                    UpdatePerformanceMetrics(pcfThisFrame, bSkipLastElement, bReplaceStack);
                }

                //
                // Update stack trace
                //
                m_StackTraceInfo.AppendElement(CanAllocateMemory(), NULL, sf.SP, pMD, pcfThisFrame);
                m_StackTraceInfo.SaveStackTrace(CanAllocateMemory(), m_hThrowable, bReplaceStack, bSkipLastElement);

                //
                // make callback to debugger and/or profiler
                //
#if defined(DEBUGGING_SUPPORTED)
                if (ExceptionTracker::NotifyDebuggerOfStub(pThread, sf, pFrame))
                {
                    // Deliver the FirstChanceNotification after the debugger, if not already delivered.
                    if (!this->DeliveredFirstChanceNotification())
                    {
                        ExceptionNotifications::DeliverFirstChanceNotification();
                    }
                }
#endif // DEBUGGING_SUPPORTED

                STState = STS_Append;
            }
        }
    }

lExit:
    return UnwindPending;
}

CLRUnwindStatus ExceptionTracker::HandleFunclets(bool* pfProcessThisFrame, bool fIsFirstPass,
    MethodDesc * pMD, bool fFunclet, StackFrame sf)
{
    CONTRACTL
    {
        MODE_ANY;
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACTL_END;

    BOOL fUnwindingToFindResumeFrame = m_ExceptionFlags.UnwindingToFindResumeFrame();

    //
    // handle out-of-line finallys
    //

    // In the second pass, we always want to execute this code.
    // In the first pass, we only execute this code if we are not unwinding to find the resume frame.
    // We do this to avoid calling the same filter more than once.  Search for "UnwindingToFindResumeFrame"
    // to find a more elaborate comment in ProcessManagedCallFrame().

    // If we are in the first pass and we are unwinding to find the resume frame, then make sure the flag is cleared.
    if (fIsFirstPass && fUnwindingToFindResumeFrame)
    {
        m_pSkipToParentFunctionMD = NULL;
    }
    else
    {
        // <TODO>
        //      this 'skip to parent function MD' code only seems to be needed
        //      in the case where we call a finally funclet from the normal
        //      execution codepath.  Is there a better way to achieve the same
        //      goal?  Also, will recursion break us in any corner cases?
        //      [ThrowInFinallyNestedInTryTest]
        //      [GoryManagedPresentTest]
        // </TODO>

        // <TODO>
        //      this was done for AMD64, but i don't understand why AMD64 needed the workaround..
        //      (the workaround is the "double call on parent method" part.)
        // </TODO>

        //
        // If we encounter a funclet, we need to skip all call frames up
        // to and including its parent method call frame.  The reason
        // behind this is that a funclet is logically part of the parent
        // method has all the clauses that covered its logical location
        // in the parent covering its body.
        //
        if (((UINT_PTR)m_pSkipToParentFunctionMD) & 1)
        {
            EH_LOG((LL_INFO100, "  IGNOREFRAME: SKIPTOPARENT: skipping to parent\n"));
            *pfProcessThisFrame = false;
            if ((((UINT_PTR)pMD) == (((UINT_PTR)m_pSkipToParentFunctionMD) & ~((UINT_PTR)1))) && !fFunclet)
            {
                EH_LOG((LL_INFO100, "  SKIPTOPARENT: found parent for funclet pMD = %p, sf.SP = %p, will stop skipping frames\n", pMD, sf.SP));
                _ASSERTE(0 == (((UINT_PTR)sf.SP) & 1));
                m_pSkipToParentFunctionMD = (MethodDesc*)sf.SP;

                _ASSERTE(!fUnwindingToFindResumeFrame);
            }
        }
        else if (fFunclet)
        {
            EH_LOG((LL_INFO100, "  SKIPTOPARENT: found funclet pMD = %p, will start skipping frames\n", pMD));
            _ASSERTE(0 == (((UINT_PTR)pMD) & 1));
            m_pSkipToParentFunctionMD = (MethodDesc*)(((UINT_PTR)pMD) | 1);
        }
        else
        {
            if (sf.SP == ((UINT_PTR)m_pSkipToParentFunctionMD))
            {
                EH_LOG((LL_INFO100, "  IGNOREFRAME: SKIPTOPARENT: got double call on parent method\n"));
                *pfProcessThisFrame = false;
            }
            else if (m_pSkipToParentFunctionMD && (sf.SP > ((UINT_PTR)m_pSkipToParentFunctionMD)))
            {
                EH_LOG((LL_INFO100, "  SKIPTOPARENT: went past parent method\n"));
                m_pSkipToParentFunctionMD = NULL;
            }
        }
    }

    return UnwindPending;
}

CLRUnwindStatus ExceptionTracker::ProcessManagedCallFrame(
    CrawlFrame* pcfThisFrame,
    StackFrame sf,
    StackFrame sfEstablisherFrame,
    EXCEPTION_RECORD* pExceptionRecord,
    StackTraceState STState,
    UINT_PTR uMethodStartPC,
    DWORD dwExceptionFlags,
    DWORD dwTACatchHandlerClauseIndex,
    StackFrame sfEstablisherOfActualHandlerFrame
    )
{
    CONTRACTL
    {
        MODE_ANY;
        GC_TRIGGERS;
        THROWS;
        PRECONDITION(pcfThisFrame->IsFrameless());
    }
    CONTRACTL_END;

    UINT_PTR        uControlPC  = (UINT_PTR)GetControlPC(pcfThisFrame->GetRegisterSet());
    CLRUnwindStatus ReturnStatus = UnwindPending;

    MethodDesc*     pMD         = pcfThisFrame->GetFunction();

    bool fIsFirstPass = !(dwExceptionFlags & EXCEPTION_UNWINDING);
    bool fIsFunclet   = pcfThisFrame->IsFunclet();

    CONSISTENCY_CHECK(IsValid());
    CONSISTENCY_CHECK(ThrowableIsValid() || !fIsFirstPass);
    CONSISTENCY_CHECK(pMD != 0);

    EH_LOG((LL_INFO100, "  [ ProcessManagedCallFrame this=%p, %s PASS ]\n", this, (fIsFirstPass ? "FIRST" : "SECOND")));
    
    EH_LOG((LL_INFO100, "  [ method: %s%s, %s ]\n",
        (fIsFunclet ? "FUNCLET of " : ""),
        pMD->m_pszDebugMethodName, pMD->m_pszDebugClassName));

    Thread *pThread = GetThread();
    _ASSERTE (pThread);

    INDEBUG( DumpClauses(pcfThisFrame->GetJitManager(), pcfThisFrame->GetMethodToken(), uMethodStartPC, uControlPC) );

    bool fIsILStub = pMD->IsILStub();
    bool fGiveDebuggerAndProfilerNotification = !fIsILStub;
    BOOL fUnwindingToFindResumeFrame = m_ExceptionFlags.UnwindingToFindResumeFrame();

    bool fIgnoreThisFrame                       = false;
    bool fProcessThisFrameToFindResumeFrameOnly = false;

    MethodDesc * pUserMDForILStub = NULL;
    Frame * pILStubFrame = NULL;
    if (fIsILStub && !fIsFunclet)    // only make this callback on the main method body of IL stubs
        pUserMDForILStub = GetUserMethodForILStub(pThread, sf.SP, pMD, &pILStubFrame);

#ifdef FEATURE_CORRUPTING_EXCEPTIONS
    BOOL fCanMethodHandleException = TRUE;
    CorruptionSeverity currentSeverity = NotCorrupting;
    {
        // Switch to COOP mode since we are going to request throwable
        GCX_COOP();

        // We must defer to the MethodDesc of the user method instead of the IL stub
        // itself because the user can specify the policy on a per-method basis and 
        // that won't be reflected via the IL stub's MethodDesc.
        MethodDesc * pMDWithCEAttribute = (pUserMDForILStub != NULL) ? pUserMDForILStub : pMD;

        // Check if the exception can be delivered to the method? It will check if the exception
        // is a CE or not. If it is, it will check if the method can process it or not.
        currentSeverity = pThread->GetExceptionState()->GetCurrentExceptionTracker()->GetCorruptionSeverity();
        fCanMethodHandleException = CEHelper::CanMethodHandleException(currentSeverity, pMDWithCEAttribute);
    }
#endif // FEATURE_CORRUPTING_EXCEPTIONS

    // Doing rude abort.  Skip all non-constrained execution region code.
    // When rude abort is initiated, we cannot intercept any exceptions.
    if ((pThread->IsRudeAbortInitiated() && !pThread->IsWithinCer(pcfThisFrame)))
    {
        // If we are unwinding to find the real resume frame, then we cannot ignore frames yet.
        // We need to make sure we find the correct resume frame before starting to ignore frames.
        if (fUnwindingToFindResumeFrame)
        {
            fProcessThisFrameToFindResumeFrameOnly = true;
        }
        else
        {
            EH_LOG((LL_INFO100, "  IGNOREFRAME: rude abort/CE\n"));
            fIgnoreThisFrame = true;
        }
    }

    //
    // BEGIN resume frame processing code
    //
    // Often times, we'll run into the situation where the actual resume call frame
    // is not the same call frame that we see the catch clause in.  The reason for this
    // is that catch clauses get duplicated down to cover funclet code ranges.  When we
    // see a catch clause covering our control PC, but it is marked as a duplicate, we
    // need to continue to unwind until we find the same clause that isn't marked as a
    // duplicate.  This will be the correct resume frame.
    //
    // We actually achieve this skipping by observing that if we are catching at a
    // duplicated clause, all the call frames we should be skipping have already been
    // processed by a previous exception dispatch.  So if we allow the unwind to
    // continue, we will immediately bump into the ExceptionTracker for the previous
    // dispatch, and our resume frame will be the last frame seen by that Tracker.
    //
    // Note that we will have visited all the EH clauses for a particular method when we
    // see its first funclet (the funclet which is closest to the leaf).  We need to make
    // sure we don't process any EH clause again when we see other funclets or the parent
    // method until we get to the real resume frame.  The real resume frame may be another
    // funclet, which is why we can't blindly skip all funclets until we see the parent
    // method frame.
    //
    // If the exception is handled by the method, then UnwindingToFindResumeFrame takes
    // care of the skipping.  We basically skip everything when we are unwinding to find
    // the resume frame.  If the exception is not handled by the method, then we skip all the
    // funclets until we get to the parent method.  The logic to handle this is in
    // HandleFunclets().  In the first pass, HandleFunclets() only kicks
    // in if we are not unwinding to find the resume frame.
    //
    // Then on the second pass, we need to process frames up to the initial place where
    // we saw the catch clause, which means upto and including part of the resume stack
    // frame.  Then we need to skip the call frames up to the real resume stack frame
    // and resume.
    //
    // In the second pass, we have the same problem with skipping funclets as in the first
    // pass.  However, in this case, we know exactly which frame is our target unwind frame
    // (EXCEPTION_TARGET_UNWIND will be set).  So we blindly unwind until we see the parent
    // method, or until the target unwind frame.
    PTR_EXCEPTION_CLAUSE_TOKEN pLimitClauseToken     = NULL;
    if (!fIgnoreThisFrame && !fIsFirstPass && !m_sfResumeStackFrame.IsNull() && (sf >= m_sfResumeStackFrame))
    {
        EH_LOG((LL_INFO100, "  RESUMEFRAME:  sf is  %p and  m_sfResumeStackFrame: %p\n", sf.SP, m_sfResumeStackFrame.SP));
        EH_LOG((LL_INFO100, "  RESUMEFRAME:  %s initial resume frame: %p\n", (sf == m_sfResumeStackFrame) ? "REACHED" : "PASSED" , m_sfResumeStackFrame.SP));

        // process this frame to call handlers
        EH_LOG((LL_INFO100, "  RESUMEFRAME:  Found last frame to process finallys in, need to process only part of call frame\n"));
        EH_LOG((LL_INFO100, "  RESUMEFRAME:  Limit clause token: %p\n", m_pClauseForCatchToken));
        pLimitClauseToken = m_pClauseForCatchToken;

        // The limit clause is the same as the clause we're catching at.  It is used
        // as the last clause we process in the "inital resume frame".  Anything further
        // down the list of clauses is skipped along with all call frames up to the actual
        // resume frame.
        CONSISTENCY_CHECK_MSG(sf == m_sfResumeStackFrame, "Passed initial resume frame and fIgnoreThisFrame wasn't set!");
    }
    //
    // END resume frame code
    //

    if (!fIgnoreThisFrame)
    {
        BOOL                    fFoundHandler    = FALSE;
        DWORD_PTR               dwHandlerStartPC = NULL;

        BOOL bReplaceStack      = FALSE;
        BOOL bSkipLastElement   = FALSE;
        bool fUnwindFinished    = false;

        if (STS_FirstRethrowFrame == STState)
        {
            bSkipLastElement = TRUE;
        }
        else
        if (STS_NewException == STState)
        {
            bReplaceStack    = TRUE;
        }

        // We need to notify the profiler on the first pass in two cases:
        // 1) a brand new exception is thrown, and
        // 2) an exception is rethrown.
        if (fIsFirstPass && (bSkipLastElement || bReplaceStack))
        {
            GCX_COOP();
            EEToProfilerExceptionInterfaceWrapper::ExceptionThrown(pThread);
            UpdatePerformanceMetrics(pcfThisFrame, bSkipLastElement, bReplaceStack);
        }

        if (!fUnwindingToFindResumeFrame)
        {
            //
            // update our exception stacktrace, ignoring IL stubs
            //
            if (fIsFirstPass && !pMD->IsILStub())
            {
                GCX_COOP();

                m_StackTraceInfo.AppendElement(CanAllocateMemory(), uControlPC, sf.SP, pMD, pcfThisFrame);
                m_StackTraceInfo.SaveStackTrace(CanAllocateMemory(), m_hThrowable, bReplaceStack, bSkipLastElement);
            }

            //
            // make callback to debugger and/or profiler
            //
            if (fGiveDebuggerAndProfilerNotification)
            {
                if (fIsFirstPass)
                {
                    EEToProfilerExceptionInterfaceWrapper::ExceptionSearchFunctionEnter(pMD);

                    // Notfiy the debugger that we are on the first pass for a managed exception.
                    // Note that this callback is made for every managed frame.
                    EEToDebuggerExceptionInterfaceWrapper::FirstChanceManagedException(pThread, uControlPC, sf.SP);

#if defined(DEBUGGING_SUPPORTED)
                    _ASSERTE(this == pThread->GetExceptionState()->m_pCurrentTracker);

                    // check if the current exception has been intercepted.
                    if (m_ExceptionFlags.DebuggerInterceptInfo())
                    {
                        // According to the x86 implementation, we don't need to call the ExceptionSearchFunctionLeave()
                        // profiler callback.
                        StackFrame sfInterceptStackFrame;
                        m_DebuggerExState.GetDebuggerInterceptInfo(NULL, NULL,
                                reinterpret_cast<PBYTE *>(&(sfInterceptStackFrame.SP)),
                                NULL, NULL);

                        // Save the target unwind frame just like we do when we find a catch clause.
                        m_sfResumeStackFrame = sfInterceptStackFrame;
                        ReturnStatus         = FirstPassComplete;
                        goto lExit;
                    }
#endif // DEBUGGING_SUPPORTED

                    // Attempt to deliver the first chance notification to the AD only *AFTER* the debugger
                    // has done that, provided we have not already delivered it.
                    if (!this->DeliveredFirstChanceNotification())
                    {
                        ExceptionNotifications::DeliverFirstChanceNotification();
                    }
                }
                else
                {
#if defined(DEBUGGING_SUPPORTED)
                    _ASSERTE(this == pThread->GetExceptionState()->m_pCurrentTracker);

                    // check if the exception is intercepted.
                    if (m_ExceptionFlags.DebuggerInterceptInfo())
                    {
                        MethodDesc* pInterceptMD = NULL;
                        StackFrame sfInterceptStackFrame;

                        // check if we have reached the interception point yet
                        m_DebuggerExState.GetDebuggerInterceptInfo(&pInterceptMD, NULL,
                                reinterpret_cast<PBYTE *>(&(sfInterceptStackFrame.SP)),
                                NULL, NULL);

                        // If the exception has gone unhandled in the first pass, we wouldn't have a chance
                        // to set the target unwind frame.  Check for this case now.
                        if (m_sfResumeStackFrame.IsNull())
                        {
                            m_sfResumeStackFrame = sfInterceptStackFrame;
                        }
                        _ASSERTE(m_sfResumeStackFrame == sfInterceptStackFrame);

                        if ((pInterceptMD == pMD) &&
                            (sfInterceptStackFrame == sf))
                        {
                            // If we have reached the stack frame at which the exception is intercepted,
                            // then finish the second pass prematurely.
                            SecondPassIsComplete(pMD, sf);
                            ReturnStatus = SecondPassComplete;
                            goto lExit;
                        }
                    }
#endif // DEBUGGING_SUPPORTED

                    // According to the x86 implementation, we don't need to call the ExceptionUnwindFunctionEnter()
                    // profiler callback when an exception is intercepted.
                    EEToProfilerExceptionInterfaceWrapper::ExceptionUnwindFunctionEnter(pMD);
                }
            }

        }

        {
            IJitManager* pJitMan   = pcfThisFrame->GetJitManager();
            const METHODTOKEN& MethToken = pcfThisFrame->GetMethodToken();

            EH_CLAUSE_ENUMERATOR EnumState;
            unsigned             EHCount;

#ifdef FEATURE_CORRUPTING_EXCEPTIONS
            // The method cannot handle the exception (e.g. cannot handle the CE), then simply bail out
            // without examining the EH clauses in it.
            if (!fCanMethodHandleException)
            {
                LOG((LF_EH, LL_INFO100, "ProcessManagedCallFrame - CEHelper decided not to look for exception handlers in the method(MD:%p).\n", pMD));

                // Set the flag to skip this frame since the CE cannot be delivered
                _ASSERTE(currentSeverity == ProcessCorrupting);

                // Force EHClause count to be zero
                EHCount = 0;
            }
            else
#endif // FEATURE_CORRUPTING_EXCEPTIONS
            {
                EHCount = pJitMan->InitializeEHEnumeration(MethToken, &EnumState);
            }

            
            if (!fIsFirstPass)
            {
                // For a method that may have nested funclets, it is possible that a reference may be
                // dead at the point where control flow left the method but may become active once
                // a funclet is executed.
                // 
                // Upon returning from the funclet but before the next funclet is invoked, a GC
                // may happen if we are in preemptive mode. Since the GC stackwalk will commence
                // at the original IP at which control left the method, it can result in the reference
                // not being updated (since it was dead at the point control left the method) if the object
                // is moved during GC.
                // 
                // To address this, we will indefinitely switch to COOP mode while enumerating, and invoking,
                // funclets.
                //
                // This switch is also required for another scenario: we may be in unwind phase and the current frame
                // may not have any termination handlers to be invoked (i.e. it may have zero EH clauses applicable to
                // the unwind phase). If we do not switch to COOP mode for such a frame, we could remain in preemp mode.
                // Upon returning back from ProcessOSExceptionNotification in ProcessCLRException, when we attempt to
                // switch to COOP mode to update the LastUnwoundEstablisherFrame, we could get blocked due to an
                // active GC, prior to peforming the update. 
                //
                // In this case, if the GC stackwalk encounters the current frame and attempts to check if it has been
                // unwound by an exception, then while it has been unwound (especially since it had no termination handlers)
                // logically, it will not figure out as unwound and thus, GC stackwalk would attempt to report references from
                // it, which is incorrect. 
                //
                // Thus, when unwinding, we will always switch to COOP mode indefinitely, irrespective of whether
                // the frame has EH clauses to be processed or not.
                GCX_COOP_NO_DTOR();
                
                // We will also forbid any GC to happen between successive funclet invocations.
                // This will be automatically undone when the contract goes off the stack as the method
                // returns back to its caller.
                BEGINFORBIDGC();
            }

            for (unsigned i = 0; i < EHCount; i++)
            {
                EE_ILEXCEPTION_CLAUSE EHClause;
                PTR_EXCEPTION_CLAUSE_TOKEN pEHClauseToken = pJitMan->GetNextEHClause(&EnumState, &EHClause);

                EH_LOG((LL_INFO100, "  considering %s clause [%x,%x), ControlPc is %s clause (offset %x)",
                        (IsFault(&EHClause)         ? "fault"   :
                        (IsFinally(&EHClause)       ? "finally" :
                        (IsFilterHandler(&EHClause) ? "filter"  :
                        (IsTypedHandler(&EHClause)  ? "typed"   : "unknown")))),
                        EHClause.TryStartPC,
                        EHClause.TryEndPC,
                        (ClauseCoversPC(&EHClause, pcfThisFrame->GetRelOffset()) ? "inside" : "outside"),
                        pcfThisFrame->GetRelOffset()
                        ));

                LOG((LF_EH, LL_INFO100, "\n"));

                // If we have a valid EstablisherFrame for the managed frame where
                // ThreadAbort was raised after the catch block, then see if we
                // have reached that frame during the exception dispatch. If we 
                // have, then proceed to skip applicable EH clauses.
                if ((!sfEstablisherOfActualHandlerFrame.IsNull()) && (sfEstablisherFrame == sfEstablisherOfActualHandlerFrame))
                {
                    // We should have a valid index of the EH clause (corresponding to a catch block) after 
                    // which thread abort was raised?
                    _ASSERTE(dwTACatchHandlerClauseIndex > 0);
                    {
                        // Since we have the index, check if the current EH clause index
                        // is less then saved index. If it is, then it implies that
                        // we are evaluating clauses that lie "before" the EH clause
                        // for the catch block "after" which thread abort was raised.
                        //
                        // Since ThreadAbort has to make forward progress, we will
                        // skip evaluating any such EH clauses. Two things can happen:
                        //
                        // 1) We will find clauses representing handlers beyond the
                        //    catch block after which ThreadAbort was raised. Since this is 
                        //    what we want, we evaluate them.
                        //
                        // 2) There wont be any more clauses implying that the catch block
                        //    after which the exception was raised was the outermost
                        //    handler in the method. Thus, the exception will escape out,
                        //    which is semantically the correct thing to happen.
                        //
                        // The premise of this check is based upon a JIT compiler's implementation
                        // detail: when it generates EH clauses, JIT compiler will order them from
                        // top->bottom (when reading a method) and inside->out when reading nested
                        // clauses.
                        //
                        // This assumption is not new since the basic EH type-matching is reliant
                        // on this very assumption. However, now we have one more candidate that
                        // gets to rely on it.
                        //
                        // Eventually, this enables forward progress of thread abort exception.
                        if (i <= (dwTACatchHandlerClauseIndex -1))
                        {
                            EH_LOG((LL_INFO100, "  skipping the evaluation of EH clause (index=%d) since we cannot process an exception in a handler\n", i));
                            EH_LOG((LL_INFO100, "  that exists prior to the one (index=%d) after which ThreadAbort was [re]raised.\n", dwTACatchHandlerClauseIndex));
                            continue;
                        }
                    }
                }

                            
                // see comment above where we set pLimitClauseToken
                if (pEHClauseToken == pLimitClauseToken)
                {
                    EH_LOG((LL_INFO100, "  found limit clause, stopping clause enumeration\n"));

                    // <GC_FUNCLET_REFERENCE_REPORTING>
                    // 
                    // If we are here, the exception has been identified to be handled by a duplicate catch clause
                    // that is protecting the current funclet. The call to SetEnclosingClauseInfo (below)
                    // will setup the CallerSP (for GC reference reporting) to be the SP of the
                    // of the caller of current funclet (where the exception has happened, or is escaping from).
                    // 
                    // However, we need the CallerSP to be set as the SP of the caller of the
                    // actual frame that will contain (and invoke) the catch handler corresponding to
                    // the duplicate clause. But that isn't available right now and we can only know
                    // once we unwind upstack to reach the target frame. 
                    // 
                    // Thus, upon reaching the target frame and before invoking the catch handler, 
                    // we will fix up the CallerSP (for GC reporting) to be that of the caller of the 
                    // target frame that will be invoking the actual catch handler.
                    // 
                    // </GC_FUNCLET_REFERENCE_REPORTING>
                    // 
                    // for catch clauses
                    SetEnclosingClauseInfo(fIsFunclet,
                                                  pcfThisFrame->GetRelOffset(),
                                                  GetSP(pcfThisFrame->GetRegisterSet()->pCallerContext));
                    fUnwindFinished = true;
                    m_fFixupCallerSPForGCReporting = true;
                    break;
                }

                BOOL fTermHandler = IsFaultOrFinally(&EHClause);
                fFoundHandler     = FALSE;

                if (( fIsFirstPass &&  fTermHandler) ||
                    (!fIsFirstPass && !fTermHandler))
                {
                    continue;
                }

                if (ClauseCoversPC(&EHClause, pcfThisFrame->GetRelOffset()))
                {
                    EH_LOG((LL_INFO100, "  clause covers ControlPC\n"));

                    dwHandlerStartPC = pJitMan->GetCodeAddressForRelOffset(MethToken, EHClause.HandlerStartPC);

                    if (fUnwindingToFindResumeFrame)
                    {
                        CONSISTENCY_CHECK(fIsFirstPass);
                        if (!fTermHandler)
                        {
                            // m_pClauseForCatchToken can only be NULL for continuable exceptions, but we should never
                            // get here if we are handling continuable exceptions.  fUnwindingToFindResumeFrame is
                            // only true at the end of the first pass.
                            _ASSERTE(m_pClauseForCatchToken != NULL);

                            // handlers match and not duplicate?
                            EH_LOG((LL_INFO100, "  RESUMEFRAME:  catch handler: [%x,%x], this handler: [%x,%x] %s\n",
                                        m_ClauseForCatch.HandlerStartPC,
                                        m_ClauseForCatch.HandlerEndPC,
                                        EHClause.HandlerStartPC,
                                        EHClause.HandlerEndPC,
                                        IsDuplicateClause(&EHClause) ? "[duplicate]" : ""));

                            if ((m_ClauseForCatch.HandlerStartPC == EHClause.HandlerStartPC) &&
                                (m_ClauseForCatch.HandlerEndPC   == EHClause.HandlerEndPC))
                            {
                                EH_LOG((LL_INFO100, "  RESUMEFRAME:  found clause with same handler as catch\n"));
                                if (!IsDuplicateClause(&EHClause))
                                {
                                    CONSISTENCY_CHECK(fIsFirstPass);

                                    if (fProcessThisFrameToFindResumeFrameOnly)
                                    {
                                        EH_LOG((LL_INFO100, "  RESUMEFRAME:  identified real resume frame, \
                                                but rude thread abort is initiated: %p\n", sf.SP));

                                        // We have found the real resume frame.  However, rude thread abort
                                        // has been initiated.  Thus, we need to continue the first pass
                                        // as if we have not found a handler yet.  To do so, we need to
                                        // reset all the information we have saved when we find the handler.
                                        m_ExceptionFlags.ResetUnwindingToFindResumeFrame();

                                        m_uCatchToCallPC  = NULL;
                                        m_pClauseForCatchToken = NULL;

                                        m_sfResumeStackFrame.Clear();
                                        ReturnStatus = UnwindPending;
                                    }
                                    else
                                    {
                                        EH_LOG((LL_INFO100, "  RESUMEFRAME:  identified real resume frame: %p\n", sf.SP));
                                        
                                        // Save off the index and the EstablisherFrame of the EH clause of the non-duplicate handler
                                        // that decided to handle the exception. We may need it
                                        // if a ThreadAbort is raised after the catch block 
                                        // executes.
                                        m_dwIndexClauseForCatch = i + 1;
                                        m_sfEstablisherOfActualHandlerFrame = sfEstablisherFrame;
#ifndef ESTABLISHER_FRAME_ADDRESS_IS_CALLER_SP
                                        m_sfCallerOfActualHandlerFrame = EECodeManager::GetCallerSp(pcfThisFrame->pRD);
#else // !ESTABLISHER_FRAME_ADDRESS_IS_CALLER_SP
                                        // On ARM & ARM64, the EstablisherFrame is the value of SP at the time a function was called and before it's prolog
                                        // executed. Effectively, it is the SP of the caller.
                                        m_sfCallerOfActualHandlerFrame = sfEstablisherFrame.SP;                            
#endif // ESTABLISHER_FRAME_ADDRESS_IS_CALLER_SP
                                        
                                        ReturnStatus = FirstPassComplete;
                                    }
                                }
                                break;
                            }
                        }
                    }
                    else if (IsFilterHandler(&EHClause))
                    {
                        DWORD_PTR dwResult = EXCEPTION_CONTINUE_SEARCH;
                        DWORD_PTR dwFilterStartPC;

                        dwFilterStartPC = pJitMan->GetCodeAddressForRelOffset(MethToken, EHClause.FilterOffset);

                        EH_LOG((LL_INFO100, "  calling filter\n"));

                        // @todo : If user code throws a StackOveflowException and we have plenty of stack,
                        // we probably don't want to be so strict in not calling handlers. 
                        if (! IsStackOverflowException())
                        {
                            // Save the current EHClause Index and Establisher of the clause post which
                            // ThreadAbort was raised. This is done an exception handled inside a filter 
                            // reset the state that was setup before the filter was invoked.
                            // 
                            // We dont have to do this for finally/fault clauses since they execute
                            // in the second pass and by that time, we have already skipped the required
                            // EH clauses in the applicable stackframe.
                            DWORD dwPreFilterTACatchHandlerClauseIndex = dwTACatchHandlerClauseIndex;
                            StackFrame sfPreFilterEstablisherOfActualHandlerFrame = sfEstablisherOfActualHandlerFrame;
                            
                            EX_TRY
                            {
                                // We want to call filters even if the thread is aborting, so suppress abort
                                // checks while the filter runs.
                                ThreadPreventAsyncHolder preventAbort(TRUE);

                                // for filter clauses
                                SetEnclosingClauseInfo(fIsFunclet,
                                                              pcfThisFrame->GetRelOffset(),
                                                              GetSP(pcfThisFrame->GetRegisterSet()->pCallerContext));
#ifdef USE_FUNCLET_CALL_HELPER
                                // On ARM & ARM64, the OS passes us the CallerSP for the frame for which personality routine has been invoked.
                                // Since IL filters are invoked in the first pass, we pass this CallerSP to the filter funclet which will
                                // then lookup the actual frame pointer value using it since we dont have a frame pointer to pass to it
                                // directly.
                                //
                                // Assert our invariants (we had set them up in InitializeCrawlFrame):
                                REGDISPLAY *pCurRegDisplay = pcfThisFrame->GetRegisterSet();
                                
                                CONTEXT *pContext = NULL;
#ifndef USE_CURRENT_CONTEXT_IN_FILTER
                                // 1) In first pass, we dont have a valid current context IP
                                _ASSERTE(GetIP(pCurRegDisplay->pCurrentContext) == 0);
                                pContext = pCurRegDisplay->pCallerContext;
#else
                                pContext = pCurRegDisplay->pCurrentContext;
#endif // !USE_CURRENT_CONTEXT_IN_FILTER
#ifdef USE_CALLER_SP_IN_FUNCLET
                                // 2) Our caller context and caller SP are valid
                                _ASSERTE(pCurRegDisplay->IsCallerContextValid && pCurRegDisplay->IsCallerSPValid);
                                // 3) CallerSP is intact
                                _ASSERTE(GetSP(pCurRegDisplay->pCallerContext) == GetRegdisplaySP(pCurRegDisplay));
#endif // USE_CALLER_SP_IN_FUNCLET
#endif // USE_FUNCLET_CALL_HELPER
                                {
                                    // CallHandler expects to be in COOP mode.
                                    GCX_COOP();
                                    dwResult = CallHandler(dwFilterStartPC, sf, &EHClause, pMD, Filter X86_ARG(pContext) ARM_ARG(pContext) ARM64_ARG(pContext));
                                }
                            }
                            EX_CATCH
                            {
                                // We had an exception in filter invocation that remained unhandled.

                                // Sync managed exception state, for the managed thread, based upon the active exception tracker.
                                pThread->SyncManagedExceptionState(false);

                                // we've returned from the filter abruptly, now out of managed code
                                m_EHClauseInfo.SetManagedCodeEntered(FALSE);

                                EH_LOG((LL_INFO100, "  filter threw an exception\n"));

                                // notify profiler
                                EEToProfilerExceptionInterfaceWrapper::ExceptionSearchFilterLeave();
                                m_EHClauseInfo.ResetInfo();

                                // continue search
                            }
                            EX_END_CATCH(SwallowAllExceptions);
                            
                            // Reset the EH clause Index and Establisher of the TA reraise clause
                            pThread->m_dwIndexClauseForCatch = dwPreFilterTACatchHandlerClauseIndex;
                            pThread->m_sfEstablisherOfActualHandlerFrame = sfPreFilterEstablisherOfActualHandlerFrame;
                            
                            if (pThread->IsRudeAbortInitiated() && !pThread->IsWithinCer(pcfThisFrame))
                            {
                                EH_LOG((LL_INFO100, "  IGNOREFRAME: rude abort\n"));
                                goto lExit;
                            }
                        }
                        else
                        {
                            EH_LOG((LL_INFO100, "  STACKOVERFLOW: filter not called due to lack of guard page\n"));
                            // continue search
                        }

                        if (EXCEPTION_EXECUTE_HANDLER == dwResult)
                        {
                            fFoundHandler = TRUE;
                        }
                        else if (EXCEPTION_CONTINUE_SEARCH != dwResult)
                        {
                            //
                            // Behavior is undefined according to the spec.  Let's not execute the handler.
                            //
                        }
                        EH_LOG((LL_INFO100, "  filter returned %s\n", (fFoundHandler ? "EXCEPTION_EXECUTE_HANDLER" : "EXCEPTION_CONTINUE_SEARCH")));
                    }
                    else if (IsTypedHandler(&EHClause))
                    {
                        GCX_COOP();

                        TypeHandle thrownType = TypeHandle();
                        OBJECTREF  oThrowable = m_pThread->GetThrowable();
                        if (oThrowable != NULL)
                        {
                            oThrowable = PossiblyUnwrapThrowable(oThrowable, pcfThisFrame->GetAssembly());
                            thrownType = oThrowable->GetTrueTypeHandle();
                        }

                        if (!thrownType.IsNull())
                        {
                            if (EHClause.ClassToken == mdTypeRefNil)
                            {
                                // this is a catch(...)
                                fFoundHandler = TRUE;
                            }
                            else
                            {
                                TypeHandle typeHnd;
                                EX_TRY
                                {
                                    typeHnd = pJitMan->ResolveEHClause(&EHClause, pcfThisFrame);
                                }
                                EX_CATCH_EX(Exception)
                                {
                                    SString msg;
                                    GET_EXCEPTION()->GetMessage(msg);
                                    msg.Insert(msg.Begin(), W("Cannot resolve EH clause:\n"));
                                    EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(COR_E_FAILFAST, msg.GetUnicode());
                                }
                                EX_END_CATCH(RethrowTransientExceptions);

                                EH_LOG((LL_INFO100,
                                        "  clause type = %s\n",
                                        (!typeHnd.IsNull() ? typeHnd.GetMethodTable()->GetDebugClassName()
                                                           : "<couldn't resolve>")));
                                EH_LOG((LL_INFO100,
                                        "  thrown type = %s\n",
                                        thrownType.GetMethodTable()->GetDebugClassName()));

                                fFoundHandler = !typeHnd.IsNull() && ExceptionIsOfRightType(typeHnd, thrownType);
                            }
                        }
                    }
                    else
                    {
                        _ASSERTE(fTermHandler);
                        fFoundHandler = TRUE;
                    }

                    if (fFoundHandler)
                    {
                        if (fIsFirstPass)
                        {
                            _ASSERTE(IsFilterHandler(&EHClause) || IsTypedHandler(&EHClause));

                            EH_LOG((LL_INFO100, "  found catch at 0x%p, sp = 0x%p\n", dwHandlerStartPC, sf.SP));
                            m_uCatchToCallPC = dwHandlerStartPC;
                            m_pClauseForCatchToken = pEHClauseToken;
                            m_ClauseForCatch = EHClause;
                            
                            m_sfResumeStackFrame    = sf;

#if defined(DEBUGGING_SUPPORTED) || defined(PROFILING_SUPPORTED)
                            //
                            // notify the debugger and profiler
                            //
                            if (fGiveDebuggerAndProfilerNotification)
                            {
                                EEToProfilerExceptionInterfaceWrapper::ExceptionSearchCatcherFound(pMD);
                            }

                            if (fIsILStub)
                            {
                                //
                                // NotifyOfCHFFilter has two behaviors
                                //  * Notifify debugger, get interception info and unwind (function will not return)
                                //          In this case, m_sfResumeStackFrame is expected to be NULL or the frame of interception.
                                //          We NULL it out because we get the interception event after this point.
                                //  * Notifify debugger and return.
                                //      In this case the normal EH proceeds and we need to reset m_sfResumeStackFrame to the sf catch handler.
                                //  TODO: remove this call and try to report the IL catch handler in the IL stub itself.
                                m_sfResumeStackFrame.Clear();
                                EEToDebuggerExceptionInterfaceWrapper::NotifyOfCHFFilter((EXCEPTION_POINTERS*)&m_ptrs, pILStubFrame);
                                m_sfResumeStackFrame    = sf;
                            }
                            else
                            {
                                // We don't need to do anything special for continuable exceptions after calling
                                // this callback.  We are going to start unwinding anyway.
                                EEToDebuggerExceptionInterfaceWrapper::FirstChanceManagedExceptionCatcherFound(pThread, pMD, (TADDR) uMethodStartPC, sf.SP,
                                                                                                               &EHClause);
                            }

                            // If the exception is intercepted, then the target unwind frame may not be the
                            // stack frame we are currently processing, so clear it now.  We'll set it
                            // later in second pass.
                            if (pThread->GetExceptionState()->GetFlags()->DebuggerInterceptInfo())
                            {
                                m_sfResumeStackFrame.Clear();
                            }
#endif //defined(DEBUGGING_SUPPORTED) || defined(PROFILING_SUPPORTED)

                            //
                            // BEGIN resume frame code
                            //
                            EH_LOG((LL_INFO100, "  RESUMEFRAME:  initial resume stack frame: %p\n", sf.SP));

                            if (IsDuplicateClause(&EHClause))
                            {
                                EH_LOG((LL_INFO100, "  RESUMEFRAME:  need to unwind to find real resume frame\n"));
                                m_ExceptionFlags.SetUnwindingToFindResumeFrame();
                                
                                // This is a duplicate catch funclet. As a result, we will continue to let the
                                // exception dispatch proceed upstack to find the actual frame where the 
                                // funclet lives.
                                // 
                                // At the same time, we also need to save the CallerSP of the frame containing
                                // the catch funclet (like we do for other funclets). If the current frame
                                // represents a funclet that was invoked by JITted code, then we will save
                                // the caller SP of the current frame when we see it during the 2nd pass - 
                                // refer to the use of "pLimitClauseToken" in the code above.
                                // 
                                // However, that is not the callerSP of the frame containing the catch funclet
                                // as the actual frame containing the funclet (and where it will be executed)
                                // is the one that will be the target of unwind during the first pass.
                                // 
                                // To correctly get that, we will determine if the current frame is a funclet
                                // and if it was invoked from JITted code. If this is true, then current frame
                                // represents a finally funclet invoked non-exceptionally (from its parent frame
                                // or yet another funclet). In such a case, we will set a flag indicating that
                                // we need to reset the enclosing clause SP for the catch funclet and later,
                                // when 2nd pass reaches the actual frame containing the catch funclet to be 
                                // executed, we will update the enclosing clause SP if the 
                                // "m_fResetEnclosingClauseSPForCatchFunclet" flag is set, just prior to 
                                // invoking the catch funclet.
                                if (fIsFunclet)
                                {
                                    REGDISPLAY* pCurRegDisplay = pcfThisFrame->GetRegisterSet();
                                    _ASSERTE(pCurRegDisplay->IsCallerContextValid);
                                    TADDR adrReturnAddressFromFunclet = PCODEToPINSTR(GetIP(pCurRegDisplay->pCallerContext)) - STACKWALK_CONTROLPC_ADJUST_OFFSET;
                                    m_fResetEnclosingClauseSPForCatchFunclet = ExecutionManager::IsManagedCode(adrReturnAddressFromFunclet);
                                }

                                ReturnStatus = UnwindPending;
                                break;
                            }

                            EH_LOG((LL_INFO100, "  RESUMEFRAME:  no extra unwinding required, real resume frame: %p\n", sf.SP));
                            
                            // Save off the index and the EstablisherFrame of the EH clause of the non-duplicate handler
                            // that decided to handle the exception. We may need it
                            // if a ThreadAbort is raised after the catch block 
                            // executes.
                            m_dwIndexClauseForCatch = i + 1;
                            m_sfEstablisherOfActualHandlerFrame = sfEstablisherFrame;

#ifndef ESTABLISHER_FRAME_ADDRESS_IS_CALLER_SP
                            m_sfCallerOfActualHandlerFrame = EECodeManager::GetCallerSp(pcfThisFrame->pRD);
#else // !ESTABLISHER_FRAME_ADDRESS_IS_CALLER_SP
                            m_sfCallerOfActualHandlerFrame = sfEstablisherFrame.SP;                            
#endif // ESTABLISHER_FRAME_ADDRESS_IS_CALLER_SP
                            //
                            // END resume frame code
                            //

                            ReturnStatus = FirstPassComplete;
                            break;
                        }
                        else
                        {
                            EH_LOG((LL_INFO100, "  found finally/fault at 0x%p\n", dwHandlerStartPC));
                            _ASSERTE(fTermHandler);

                            // @todo : If user code throws a StackOveflowException and we have plenty of stack,
                            // we probably don't want to be so strict in not calling handlers.
                            if (!IsStackOverflowException())
                            {
                                DWORD_PTR dwStatus;

                                // for finally clauses
                                SetEnclosingClauseInfo(fIsFunclet,
                                                              pcfThisFrame->GetRelOffset(),
                                                              GetSP(pcfThisFrame->GetRegisterSet()->pCallerContext));
                                                              
                                // We have switched to indefinite COOP mode just before this loop started.
                                // Since we also forbid GC during second pass, disable it now since
                                // invocation of managed code can result in a GC.
                                ENDFORBIDGC();
                                dwStatus = CallHandler(dwHandlerStartPC, sf, &EHClause, pMD, FaultFinally X86_ARG(pcfThisFrame->GetRegisterSet()->pCurrentContext) ARM_ARG(pcfThisFrame->GetRegisterSet()->pCurrentContext) ARM64_ARG(pcfThisFrame->GetRegisterSet()->pCurrentContext));
                                
                                // Once we return from a funclet, forbid GC again (refer to comment before start of the loop for details)
                                BEGINFORBIDGC();
                            }
                            else
                            {
                                EH_LOG((LL_INFO100, "  STACKOVERFLOW: finally not called due to lack of guard page\n"));
                                // continue search
                            }

                            //
                            // will continue to find next fault/finally in this call frame
                            //
                        }
                    } // if fFoundHandler
                } // if clause covers PC
            } // foreach eh clause
        } // if stack frame is far enough away from guard page

        //
        // notify the profiler
        //
        if (fGiveDebuggerAndProfilerNotification)
        {
            if (fIsFirstPass)
            {
                if (!fUnwindingToFindResumeFrame)
                {
                    EEToProfilerExceptionInterfaceWrapper::ExceptionSearchFunctionLeave(pMD);
                }
            }
            else
            {
                if (!fUnwindFinished)
                {
                    EEToProfilerExceptionInterfaceWrapper::ExceptionUnwindFunctionLeave(pMD);
                }
            }
        }
    }   // fIgnoreThisFrame

lExit:
    return ReturnStatus;
}

#undef OPTIONAL_SO_CLEANUP_UNWIND

#define OPTIONAL_SO_CLEANUP_UNWIND(pThread, pFrame)  if (pThread->GetFrame() < pFrame) { UnwindFrameChain(pThread, pFrame); }

typedef DWORD_PTR (HandlerFn)(UINT_PTR uStackFrame, Object* pExceptionObj);

#ifdef USE_FUNCLET_CALL_HELPER
// This is an assembly helper that enables us to call into EH funclets.
EXTERN_C DWORD_PTR STDCALL CallEHFunclet(Object *pThrowable, UINT_PTR pFuncletToInvoke, UINT_PTR *pFirstNonVolReg, UINT_PTR *pFuncletCallerSP);

// This is an assembly helper that enables us to call into EH filter funclets.
EXTERN_C DWORD_PTR STDCALL CallEHFilterFunclet(Object *pThrowable, TADDR CallerSP, UINT_PTR pFuncletToInvoke, UINT_PTR *pFuncletCallerSP);

static inline UINT_PTR CastHandlerFn(HandlerFn *pfnHandler)
{
#ifdef _TARGET_ARM_
    return DataPointerToThumbCode<UINT_PTR, HandlerFn *>(pfnHandler);
#else
    return (UINT_PTR)pfnHandler;
#endif
}

static inline UINT_PTR *GetFirstNonVolatileRegisterAddress(PCONTEXT pContextRecord)
{
#if defined(_TARGET_ARM_)
    return (UINT_PTR*)&(pContextRecord->R4);
#elif defined(_TARGET_ARM64_)
    return (UINT_PTR*)&(pContextRecord->X19);
#elif defined(_TARGET_X86_)
    return (UINT_PTR*)&(pContextRecord->Edi);
#else
    PORTABILITY_ASSERT("GetFirstNonVolatileRegisterAddress");
    return NULL;
#endif
}

static inline TADDR GetFrameRestoreBase(PCONTEXT pContextRecord)
{
#if defined(_TARGET_ARM_) || defined(_TARGET_ARM64_)
    return GetSP(pContextRecord);
#elif defined(_TARGET_X86_)
    return pContextRecord->Ebp;
#else
    PORTABILITY_ASSERT("GetFrameRestoreBase");
    return NULL;
#endif
}

#endif // USE_FUNCLET_CALL_HELPER
  
DWORD_PTR ExceptionTracker::CallHandler(
    UINT_PTR               uHandlerStartPC,
    StackFrame             sf,
    EE_ILEXCEPTION_CLAUSE* pEHClause,
    MethodDesc*            pMD,
    EHFuncletType funcletType
    X86_ARG(PCONTEXT pContextRecord)
    ARM_ARG(PCONTEXT pContextRecord)
    ARM64_ARG(PCONTEXT pContextRecord)
    )
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;
    
    DWORD_PTR           dwResumePC;
    OBJECTREF           throwable;
    HandlerFn*          pfnHandler = (HandlerFn*)uHandlerStartPC;

    EH_LOG((LL_INFO100, "    calling handler at 0x%p, sp = 0x%p\n", uHandlerStartPC, sf.SP));

    Thread* pThread = GetThread();
    
    // The first parameter specifies whether we want to make callbacks before (true) or after (false)
    // calling the handler.
    MakeCallbacksRelatedToHandler(true, pThread, pMD, pEHClause, uHandlerStartPC, sf);

    _ASSERTE(pThread->DetermineIfGuardPagePresent());

    throwable = PossiblyUnwrapThrowable(pThread->GetThrowable(), pMD->GetAssembly());

    // Stores the current SP and BSP, which will be the caller SP and BSP for the funclet.
    // Note that we are making the assumption here that the SP and BSP don't change from this point
    // forward until we actually make the call to the funclet.  If it's not the case then we will need
    // some sort of assembly wrappers to help us out.
    CallerStackFrame csfFunclet = CallerStackFrame((UINT_PTR)GetCurrentSP());
    this->m_EHClauseInfo.SetManagedCodeEntered(TRUE);
    this->m_EHClauseInfo.SetCallerStackFrame(csfFunclet);

    switch(funcletType)
    {
    case EHFuncletType::Filter:
        ETW::ExceptionLog::ExceptionFilterBegin(pMD, (PVOID)uHandlerStartPC);
        break;
    case EHFuncletType::FaultFinally:
        ETW::ExceptionLog::ExceptionFinallyBegin(pMD, (PVOID)uHandlerStartPC);
        break;
    case EHFuncletType::Catch:
        ETW::ExceptionLog::ExceptionCatchBegin(pMD, (PVOID)uHandlerStartPC);
        break;
    }

#ifdef USE_FUNCLET_CALL_HELPER
    // Invoke the funclet. We pass throwable only when invoking the catch block.
    // Since the actual caller of the funclet is the assembly helper, pass the reference
    // to the CallerStackFrame instance so that it can be updated.
    CallerStackFrame* pCallerStackFrame = this->m_EHClauseInfo.GetCallerStackFrameForEHClauseReference();
    UINT_PTR *pFuncletCallerSP = &(pCallerStackFrame->SP);
    if (funcletType != EHFuncletType::Filter)
    {
        dwResumePC = CallEHFunclet((funcletType == EHFuncletType::Catch)?OBJECTREFToObject(throwable):(Object *)NULL, 
                                   CastHandlerFn(pfnHandler),
                                   GetFirstNonVolatileRegisterAddress(pContextRecord),
                                   pFuncletCallerSP);
    }
    else
    {
        // For invoking IL filter funclet, we pass the CallerSP to the funclet using which
        // it will retrieve the framepointer for accessing the locals in the parent
        // method.
        dwResumePC = CallEHFilterFunclet(OBJECTREFToObject(throwable),
                                         GetFrameRestoreBase(pContextRecord),
                                         CastHandlerFn(pfnHandler),
                                         pFuncletCallerSP);
    }
#else // USE_FUNCLET_CALL_HELPER
    //
    // Invoke the funclet. 
    //    
    dwResumePC = pfnHandler(sf.SP, OBJECTREFToObject(throwable));
#endif // !USE_FUNCLET_CALL_HELPER

    switch(funcletType)
    {
    case EHFuncletType::Filter:
        ETW::ExceptionLog::ExceptionFilterEnd();
        break;
    case EHFuncletType::FaultFinally:
        ETW::ExceptionLog::ExceptionFinallyEnd();
        break;
    case EHFuncletType::Catch:
        ETW::ExceptionLog::ExceptionCatchEnd();
        ETW::ExceptionLog::ExceptionThrownEnd();
        break;
    }

    this->m_EHClauseInfo.SetManagedCodeEntered(FALSE);

    // The first parameter specifies whether we want to make callbacks before (true) or after (false)
    // calling the handler.
    MakeCallbacksRelatedToHandler(false, pThread, pMD, pEHClause, uHandlerStartPC, sf);

    return dwResumePC;
}

#undef OPTIONAL_SO_CLEANUP_UNWIND
#define OPTIONAL_SO_CLEANUP_UNWIND(pThread, pFrame)


//
// this must be done after the second pass has run, it does not
// reference anything on the stack, so it is safe to run in an
// SEH __except clause as well as a C++ catch clause.
//
// static
void ExceptionTracker::PopTrackers(
    void* pStackFrameSP
    )
{
    CONTRACTL
    {
        MODE_ANY;
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACTL_END;

    StackFrame sf((UINT_PTR)pStackFrameSP);

    // Only call into PopTrackers if we have a managed thread and we have an exception progress. 
    // Otherwise, the call below (to PopTrackers) is a noop. If this ever changes, then this short-circuit needs to be fixed.
    Thread *pCurThread = GetThread();
    if ((pCurThread != NULL) && (pCurThread->GetExceptionState()->IsExceptionInProgress()))
    {
        // Refer to the comment around ExceptionTracker::HasFrameBeenUnwoundByAnyActiveException
        // for details on the usage of this COOP switch.
        GCX_COOP();

        PopTrackers(sf, false);
    }
}

//
// during the second pass, an exception might escape out to
// unmanaged code where it is swallowed (or potentially rethrown).
// The current tracker is abandoned in this case, and if a rethrow
// does happen in unmanaged code, this is unfortunately treated as
// a brand new exception.  This is unavoidable because if two
// exceptions escape out to unmanaged code in this manner, a subsequent
// rethrow cannot be disambiguated as corresponding to the nested vs.
// the original exception.
void ExceptionTracker::PopTrackerIfEscaping(
    void* pStackPointer
    )
{
    CONTRACTL
    {
        MODE_ANY;
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACTL_END;

    Thread*                 pThread  = GetThread();
    ThreadExceptionState*   pExState = pThread->GetExceptionState();
    ExceptionTracker*       pTracker = pExState->m_pCurrentTracker;
    CONSISTENCY_CHECK((NULL == pTracker) || pTracker->IsValid());

    // If we are resuming in managed code (albeit further up the stack) we will still need this
    // tracker.  Otherwise we are either propagating into unmanaged code -- with the rethrow
    // issues mentioned above -- or we are going unhandled.
    //
    // Note that we don't distinguish unmanaged code in the EE vs. unmanaged code outside the
    // EE.  We could use the types of the Frames above us to make this distinction.  Without
    // this, the technique of EX_TRY/EX_CATCH/EX_RETHROW inside the EE will lose its tracker
    // and have to rely on LastThrownObject in the rethrow.  Along the same lines, unhandled
    // exceptions only have access to LastThrownObject.
    //
    // There may not be a current tracker if, for instance, UMThunk has dispatched into managed
    // code via CallDescr.  In that case, CallDescr may pop the tracker, leaving UMThunk with
    // nothing to do.

    if (pTracker && pTracker->m_sfResumeStackFrame.IsNull())
    {
        StackFrame sf((UINT_PTR)pStackPointer);
        StackFrame sfTopMostStackFrameFromFirstPass = pTracker->GetTopmostStackFrameFromFirstPass();

        // Refer to the comment around ExceptionTracker::HasFrameBeenUnwoundByAnyActiveException
        // for details on the usage of this COOP switch.
        GCX_COOP();
        ExceptionTracker::PopTrackers(sf, true);
    }
}

//
// static
void ExceptionTracker::PopTrackers(
    StackFrame sfResumeFrame,
    bool fPopWhenEqual
    )
{
    CONTRACTL
    {
        // Refer to the comment around ExceptionTracker::HasFrameBeenUnwoundByAnyActiveException
        // for details on the mode being COOP here.
        MODE_COOPERATIVE;
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACTL_END;

    Thread*             pThread     = GetThread();
    ExceptionTracker*   pTracker    = (pThread ? pThread->GetExceptionState()->m_pCurrentTracker : NULL);

    // NOTE:
    //
    // This method is a no-op when there is no managed Thread object. We detect such a case and short circuit out in ExceptionTrackers::PopTrackers.
    // If this ever changes, then please revisit that method and fix it up appropriately.

    // If this tracker does not have valid stack ranges and it is in the first pass,
    // then we came here likely when the tracker was being setup
    // and an exception took place.
    //
    // In such a case, we will not pop off the tracker
    if (pTracker && pTracker->m_ScannedStackRange.IsEmpty() && pTracker->IsInFirstPass())
    {
        // skip any others with empty ranges...
        do
        {
           pTracker = pTracker->m_pPrevNestedInfo;
        }
        while (pTracker && pTracker->m_ScannedStackRange.IsEmpty());

        // pTracker is now the first non-empty one, make sure it doesn't need popping
        // if it does, then someone let an exception propagate out of the exception dispatch code

        _ASSERTE(!pTracker || (pTracker->m_ScannedStackRange.GetUpperBound() > sfResumeFrame));
        return;
    }

#if defined(DEBUGGING_SUPPORTED)
    DWORD_PTR dwInterceptStackFrame = 0;

    // This method may be called on an unmanaged thread, in which case no interception can be done.
    if (pTracker)
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

    while (pTracker)
    {
#ifndef FEATURE_PAL
        // When we are about to pop off a tracker, it should
        // have a stack range setup.
        // It is not true on PAL where the scanned stack range needs to
        // be reset after unwinding a sequence of native frames.
        _ASSERTE(!pTracker->m_ScannedStackRange.IsEmpty());
#endif // FEATURE_PAL

        ExceptionTracker*   pPrev   = pTracker->m_pPrevNestedInfo;

        // <TODO>
        //      with new tracker collapsing code, we will only ever pop one of these at a time
        //      at the end of the 2nd pass.  However, CLRException::HandlerState::SetupCatch
        //      still uses this function and we still need to revisit how it interacts with
        //      ExceptionTrackers
        // </TODO>

        if ((fPopWhenEqual && (pTracker->m_ScannedStackRange.GetUpperBound() == sfResumeFrame)) ||
                              (pTracker->m_ScannedStackRange.GetUpperBound() <  sfResumeFrame))
        {
#if defined(DEBUGGING_SUPPORTED)
            if (g_pDebugInterface != NULL)
            {
                if (pTracker->m_ScannedStackRange.GetUpperBound().SP < dwInterceptStackFrame)
                {
                    g_pDebugInterface->DeleteInterceptContext(pTracker->m_DebuggerExState.GetDebuggerInterceptContext());
                }
                else
                {
                    _ASSERTE(dwInterceptStackFrame == 0 ||
                             ( dwInterceptStackFrame == sfResumeFrame.SP &&
                               dwInterceptStackFrame == pTracker->m_ScannedStackRange.GetUpperBound().SP ));
                }
            }
#endif // DEBUGGING_SUPPORTED

            ExceptionTracker* pTrackerToFree = pTracker;
            EH_LOG((LL_INFO100, "Unlinking ExceptionTracker object 0x%p, thread = 0x%p\n", pTrackerToFree, pTrackerToFree->m_pThread));
            CONSISTENCY_CHECK(pTracker->IsValid());
            pTracker = pPrev;

            // free managed tracker resources causing notification -- do this before unlinking the tracker
            // this is necessary so that we know an exception is still in flight while we give the notification
            FreeTrackerMemory(pTrackerToFree, memManaged);

            // unlink the tracker from the thread
            pThread->GetExceptionState()->m_pCurrentTracker = pTracker;
            CONSISTENCY_CHECK((NULL == pTracker) || pTracker->IsValid());

            // free unmanaged tracker resources
            FreeTrackerMemory(pTrackerToFree, memUnmanaged);
        }
        else
        {
            break;
        }
    }
}

//
// static
ExceptionTracker* ExceptionTracker::GetOrCreateTracker(
    UINT_PTR ControlPc,
    StackFrame sf,
    EXCEPTION_RECORD* pExceptionRecord,
    CONTEXT* pContextRecord,
    BOOL bAsynchronousThreadStop,
    bool fIsFirstPass,
    StackTraceState* pStackTraceState
    )
{
    CONTRACT(ExceptionTracker*)
    {
        MODE_ANY;
        GC_TRIGGERS;
        NOTHROW;
        PRECONDITION(CheckPointer(pStackTraceState));
        POSTCONDITION(CheckPointer(RETVAL));
    }
    CONTRACT_END;

    Thread*                 pThread  = GetThread();
    ThreadExceptionState*   pExState = pThread->GetExceptionState();
    ExceptionTracker*       pTracker = pExState->m_pCurrentTracker;
    CONSISTENCY_CHECK((NULL == pTracker) || (pTracker->IsValid()));

    bool fCreateNewTracker = false;
    bool fIsRethrow = false;
    bool fTransitionFromSecondToFirstPass = false;

    // Initialize the out parameter.
    *pStackTraceState = STS_Append;

    if (NULL != pTracker)
    {
        fTransitionFromSecondToFirstPass = fIsFirstPass && !pTracker->IsInFirstPass();

#ifndef FEATURE_PAL
        // We don't check this on PAL where the scanned stack range needs to
        // be reset after unwinding a sequence of native frames.
        CONSISTENCY_CHECK(!pTracker->m_ScannedStackRange.IsEmpty());
#endif // FEATURE_PAL

        if (pTracker->m_ExceptionFlags.IsRethrown())
        {
            EH_LOG((LL_INFO100, ">>continued processing of RETHROWN exception\n"));
            // this is the first time we've seen a rethrown exception, reuse the tracker and reset some state

            fCreateNewTracker = true;
            fIsRethrow = true;
        }
        else
        if ((pTracker->m_ptrs.ExceptionRecord != pExceptionRecord) && fIsFirstPass)
        {
            EH_LOG((LL_INFO100, ">>NEW exception (exception records do not match)\n"));
            fCreateNewTracker = true;
        }
        else
        if (sf >= pTracker->m_ScannedStackRange.GetUpperBound())
        {
            // We can't have a transition from 1st pass to 2nd pass in this case.
            _ASSERTE( ( sf == pTracker->m_ScannedStackRange.GetUpperBound() ) ||
                      ( fIsFirstPass || !pTracker->IsInFirstPass() ) );

            if (fTransitionFromSecondToFirstPass)
            {
                // We just transition from 2nd pass to 1st pass without knowing it.
                // This means that some unmanaged frame outside of the EE catches the previous exception,
                // so we should trash the current tracker and create a new one.
                EH_LOG((LL_INFO100, ">>NEW exception (the previous second pass finishes at some unmanaged frame outside of the EE)\n"));
                {
                    GCX_COOP();
                    ExceptionTracker::PopTrackers(sf, false);
                }

                fCreateNewTracker = true;
            }
            else
            {
                EH_LOG((LL_INFO100, ">>continued processing of PREVIOUS exception\n"));
                // previously seen exception, reuse the tracker

                *pStackTraceState = STS_Append;
            }
        }
        else
        if (pTracker->m_ScannedStackRange.Contains(sf))
        {
            EH_LOG((LL_INFO100, ">>continued processing of PREVIOUS exception (revisiting previously processed frames)\n"));
        }
        else
        {
            // nested exception
            EH_LOG((LL_INFO100, ">>new NESTED exception\n"));
            fCreateNewTracker = true;
        }
    }
    else
    {
        EH_LOG((LL_INFO100, ">>NEW exception\n"));
        fCreateNewTracker = true;
    }

    if (fCreateNewTracker)
    {
#ifdef _DEBUG
        if (STATUS_STACK_OVERFLOW == pExceptionRecord->ExceptionCode)
        {
            CONSISTENCY_CHECK(pExceptionRecord->NumberParameters >= 2);
            UINT_PTR uFaultAddress = pExceptionRecord->ExceptionInformation[1];
            UINT_PTR uStackLimit   = (UINT_PTR)pThread->GetCachedStackLimit();

            EH_LOG((LL_INFO100, "STATUS_STACK_OVERFLOW accessing address %p %s\n",
                    uFaultAddress));

            UINT_PTR uDispatchStackAvailable;

            uDispatchStackAvailable = uFaultAddress - uStackLimit - HARD_GUARD_REGION_SIZE;

            EH_LOG((LL_INFO100, "%x bytes available for SO processing\n", uDispatchStackAvailable));
        }
        else if ((IsComPlusException(pExceptionRecord)) &&
                 (pThread->GetThrowableAsHandle() == g_pPreallocatedStackOverflowException))
        {
            EH_LOG((LL_INFO100, "STACKOVERFLOW: StackOverflowException manually thrown\n"));
        }
#endif // _DEBUG

        ExceptionTracker*   pNewTracker;

        pNewTracker = GetTrackerMemory();
        if (!pNewTracker)
        {
            if (NULL != pExState->m_OOMTracker.m_pThread)
            {
                // Fatal error:  we spun and could not allocate another tracker
                // and our existing emergency tracker is in use.
                EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
            }

            pNewTracker = &pExState->m_OOMTracker;
        }

        new (pNewTracker) ExceptionTracker(ControlPc,
                                           pExceptionRecord,
                                           pContextRecord);

        CONSISTENCY_CHECK(pNewTracker->IsValid());
        CONSISTENCY_CHECK(pThread == pNewTracker->m_pThread);

        EH_LOG((LL_INFO100, "___________________________________________\n"));
        EH_LOG((LL_INFO100, "creating new tracker object 0x%p, thread = 0x%p\n", pNewTracker, pThread));

        GCX_COOP();

        // We always create a throwable in the first pass when we first see an exception.
        //
        // On 64bit, every time the exception passes beyond a boundary (e.g. RPInvoke call, or CallDescrWorker call),
        // the exception trackers that were created below (stack growing down) that boundary are released, during the 2nd pass,
        // if the exception was not caught in managed code. This is because the catcher is in native code and managed exception
        // data structures are for use of VM only when the exception is caught in managed code. Also, passing by such 
        // boundaries is our only opportunity to release such internal structures and not leak the memory.
        //
        // However, in certain case, release of exception trackers at each boundary can prove to be a bit aggressive.
        // Take the example below where "VM" prefix refers to a VM frame and "M" prefix refers to a managed frame on the stack.
        //
        // VM1 -> M1 - VM2 - (via RPinvoke) -> M2
        //
        // Let M2 throw E2 that remains unhandled in managed code (i.e. M1 also does not catch it) but is caught in VM1.
        // Note that the acting of throwing an exception also sets it as the LastThrownObject (LTO) against the thread.
        //
        // Since this is native code (as mentioned in the comments above, there is no distinction made between VM native
        // code and external native code) that caught the exception, when the unwind goes past the "Reverse Pinvoke" boundary,
        // its personality routine will release the tracker for E2. Thus, only the LTO (which is off the Thread object and not
        // the exception tracker) is indicative of type of the last exception thrown.
        //
        // As the unwind goes up the stack, we come across M1 and, since the original tracker was released, we create a new 
        // tracker in the 2nd pass that does not contain details like the active exception object. A managed finally executes in M1 
        // that throws and catches E1 inside the finally block. Thus, LTO is updated to indicate E1 as the last exception thrown. 
        // When the exception is caught in VM1 and VM attempts to get LTO, it gets E1, which is incorrect as it was handled within the finally. 
        // Semantically, it should have got E2 as the LTO. 
        //
        // To address, this we will *also* create a throwable during second pass for most exceptions
        // since most of them have had the corresponding first pass. If we are processing
        // an exception's second pass, we would have processed its first pass as well and thus, already
        // created a throwable that would be setup as the LastThrownObject (LTO) against the Thread.
        //
        // The only exception to this rule is the longjump - this exception only has second pass
        // Thus, if we are in second pass and exception in question is longjump, then do not create a throwable.
        //
        // In the case of the scenario above, when we attempt to create a new exception tracker, during the unwind,
        // for M1, we will also setup E2 as the throwable in the tracker. As a result, when the finally in M1 throws
        // and catches the exception, the LTO is correctly updated against the thread (see SafeUpdateLastThrownObject)
        // and thus, when VM requests for the LTO, it gets E2 as expected.
        bool fCreateThrowableForCurrentPass = true;
        if (pExceptionRecord->ExceptionCode == STATUS_LONGJUMP)
        {
            // Long jump is only in second pass of exception dispatch
            _ASSERTE(!fIsFirstPass);
            fCreateThrowableForCurrentPass = false;
        }
        
        // When dealing with SQL Hosting like scenario, a real SO
        // may be caught in native code. As a result, CRT will perform
        // STATUS_UNWIND_CONSOLIDATE that will result in replacing
        // the exception record in ProcessCLRException. This replaced
        // exception record will point to the exception record for original
        // SO for which we will not have created a throwable in the first pass
        // due to the SO-specific early exit code in ProcessCLRException.
        //
        // Thus, if we see that we are here for SO in the 2nd pass, then
        // we shouldn't attempt to create a throwable.
        if ((!fIsFirstPass) && (pExceptionRecord->ExceptionCode == STATUS_STACK_OVERFLOW))
        {
            fCreateThrowableForCurrentPass = false;
        }
        
#ifdef _DEBUG
        if ((!fIsFirstPass) && (fCreateThrowableForCurrentPass == true))
        {
            // We should have a LTO available if we are creating
            // a throwable during second pass.
            _ASSERTE(pThread->LastThrownObjectHandle() != NULL);
        }
#endif // _DEBUG        
        
        bool        fCreateThrowable = (fCreateThrowableForCurrentPass || (bAsynchronousThreadStop && !pThread->IsAsyncPrevented()));
        OBJECTREF   oThrowable  = NULL;

        if (fCreateThrowable)
        {
            if (fIsRethrow)
            {
                oThrowable = ObjectFromHandle(pTracker->m_hThrowable);
            }
            else
            {
                // this can take a nested exception
                oThrowable = CreateThrowable(pExceptionRecord, bAsynchronousThreadStop);
            }
        }

        GCX_FORBID();   // we haven't protected oThrowable

        if (pExState->m_pCurrentTracker != pNewTracker) // OOM can make this false
        {
            pNewTracker->m_pPrevNestedInfo = pExState->m_pCurrentTracker;
            pTracker = pNewTracker;
            pThread->GetExceptionState()->m_pCurrentTracker = pTracker;
        }

        if (fCreateThrowable)
        {
            CONSISTENCY_CHECK(oThrowable != NULL);
            CONSISTENCY_CHECK(NULL == pTracker->m_hThrowable);

            pThread->SafeSetThrowables(oThrowable);

            if (pTracker->CanAllocateMemory())
            {
                pTracker->m_StackTraceInfo.AllocateStackTrace();
            }
        }
        INDEBUG(oThrowable = NULL);

        if (fIsRethrow)
        {
            *pStackTraceState = STS_FirstRethrowFrame;
        }
        else
        {
            *pStackTraceState = STS_NewException;
        }

        _ASSERTE(pTracker->m_pLimitFrame == NULL);
        pTracker->ResetLimitFrame();
    }

    if (!fIsFirstPass)
    {
        {
            // Refer to the comment around ExceptionTracker::HasFrameBeenUnwoundByAnyActiveException
            // for details on the usage of this COOP switch.
            GCX_COOP();

            if (pTracker->IsInFirstPass())
            {
                CONSISTENCY_CHECK_MSG(fCreateNewTracker || pTracker->m_ScannedStackRange.Contains(sf),
                                      "Tracker did not receive a first pass!");

                // Save the topmost StackFrame the tracker saw in the first pass before we reset the
                // scanned stack range.
                pTracker->m_sfFirstPassTopmostFrame = pTracker->m_ScannedStackRange.GetUpperBound();

                // We have to detect this transition because otherwise we break when unmanaged code
                // catches our exceptions.
                EH_LOG((LL_INFO100, ">>tracker transitioned to second pass\n"));
                pTracker->m_ScannedStackRange.Reset();

                pTracker->m_ExceptionFlags.SetUnwindHasStarted();
                if (pTracker->m_ExceptionFlags.UnwindingToFindResumeFrame())
                {
                    // UnwindingToFindResumeFrame means that in the first pass, we determine that a method
                    // catches the exception, but the method frame we are inspecting is a funclet method frame
                    // and is not the correct frame to resume execution.  We need to resume to the correct
                    // method frame before starting the second pass.  The correct method frame is most likely
                    // the parent method frame, but it can also be another funclet method frame.
                    //
                    // If the exception transitions from first pass to second pass before we find the parent
                    // method frame, there is only one possibility: some other thread has initiated a rude
                    // abort on the current thread, causing us to skip processing of all method frames.
                    _ASSERTE(pThread->IsRudeAbortInitiated());
                }
                // Lean on the safe side and just reset everything unconditionally.
                pTracker->FirstPassIsComplete();

                EEToDebuggerExceptionInterfaceWrapper::ManagedExceptionUnwindBegin(pThread);

                pTracker->ResetLimitFrame();
            }
            else
            {
                // In the second pass, there's a possibility that UMThunkUnwindFrameChainHandler() has
                // popped some frames off the frame chain underneath us.  Check for this case here.
                if (pTracker->m_pLimitFrame < pThread->GetFrame())
                {
                    pTracker->ResetLimitFrame();
                }
            }
        }

#ifdef FEATURE_CORRUPTING_EXCEPTIONS
        if (fCreateNewTracker)
        {
            // Exception tracker should be in the 2nd pass right now
            _ASSERTE(!pTracker->IsInFirstPass());

            // The corruption severity of a newly created tracker is NotSet
            _ASSERTE(pTracker->GetCorruptionSeverity() == NotSet);

            // See comment in CEHelper::SetupCorruptionSeverityForActiveExceptionInUnwindPass for details
            CEHelper::SetupCorruptionSeverityForActiveExceptionInUnwindPass(pThread, pTracker, FALSE, pExceptionRecord->ExceptionCode);
        }
#endif // FEATURE_CORRUPTING_EXCEPTIONS
    }

    _ASSERTE(pTracker->m_pLimitFrame >= pThread->GetFrame());

    RETURN pTracker;
}

void ExceptionTracker::ResetLimitFrame()
{
    WRAPPER_NO_CONTRACT;

    m_pLimitFrame = m_pThread->GetFrame();
}

//
// static
void ExceptionTracker::ResumeExecution(
    CONTEXT*            pContextRecord,
    EXCEPTION_RECORD*   pExceptionRecord
    )
{
    //
    // This method never returns, so it will leave its
    // state on the thread if useing dynamic contracts.
    //
    STATIC_CONTRACT_MODE_COOPERATIVE;
    STATIC_CONTRACT_GC_NOTRIGGER;
    STATIC_CONTRACT_NOTHROW;

    AMD64_ONLY(STRESS_LOG4(LF_GCROOTS, LL_INFO100, "Resuming after exception at %p, rbx=%p, rsi=%p, rdi=%p\n",
            GetIP(pContextRecord),
            pContextRecord->Rbx,
            pContextRecord->Rsi,
            pContextRecord->Rdi));

    EH_LOG((LL_INFO100, "resuming execution at 0x%p\n", GetIP(pContextRecord)));
    EH_LOG((LL_INFO100, "^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^\n"));

    RtlRestoreContext(pContextRecord, pExceptionRecord);

    UNREACHABLE();
    //
    // doesn't return
    //
}

//
// static
OBJECTREF ExceptionTracker::CreateThrowable(
    PEXCEPTION_RECORD pExceptionRecord,
    BOOL bAsynchronousThreadStop
    )
{
    CONTRACTL
    {
        MODE_COOPERATIVE;
        GC_TRIGGERS;
        NOTHROW;
    }
    CONTRACTL_END;

    OBJECTREF   oThrowable  = NULL;
    Thread*     pThread     = GetThread();


    if ((!bAsynchronousThreadStop) && IsComPlusException(pExceptionRecord))
    {
        oThrowable = pThread->LastThrownObject();
    }
    else
    {
        oThrowable = CreateCOMPlusExceptionObject(pThread, pExceptionRecord, bAsynchronousThreadStop);
    }

    return oThrowable;
}

//
//static
BOOL ExceptionTracker::ClauseCoversPC(
    EE_ILEXCEPTION_CLAUSE* pEHClause,
    DWORD dwOffset
    )
{
    // TryStartPC and TryEndPC are offsets relative to the start
    // of the method so we can just compare them to the offset returned
    // by JitCodeToMethodInfo.
    //
    return ((pEHClause->TryStartPC <= dwOffset) && (dwOffset < pEHClause->TryEndPC));
}

#if defined(DEBUGGING_SUPPORTED)
BOOL ExceptionTracker::NotifyDebuggerOfStub(Thread* pThread, StackFrame sf, Frame* pCurrentFrame)
{
    LIMITED_METHOD_CONTRACT;

    BOOL fDeliveredFirstChanceNotification = FALSE;

    // <TODO>
    // Remove this once SIS is fully enabled.
    // </TODO>
    extern bool g_EnableSIS;

    if (g_EnableSIS)
    {
        _ASSERTE(GetThread() == pThread);

        GCX_COOP();

        // For debugger, we may want to notify 1st chance exceptions if they're coming out of a stub.
        // We recognize stubs as Frames with a M2U transition type. The debugger's stackwalker also
        // recognizes these frames and publishes ICorDebugInternalFrames in the stackwalk. It's
        // important to use pFrame as the stack address so that the Exception callback matches up
        // w/ the ICorDebugInternlFrame stack range.
        if (CORDebuggerAttached())
        {
            if (pCurrentFrame->GetTransitionType() == Frame::TT_M2U)
            {
                // Use -1 for the backing store pointer whenever we use the address of a frame as the stack pointer.
                EEToDebuggerExceptionInterfaceWrapper::FirstChanceManagedException(pThread,
                                                                                   (SIZE_T)0,
                                                                                   (SIZE_T)pCurrentFrame);
                fDeliveredFirstChanceNotification = TRUE;
            }
        }
    }

    return fDeliveredFirstChanceNotification;
}

bool ExceptionTracker::IsFilterStartOffset(EE_ILEXCEPTION_CLAUSE* pEHClause, DWORD_PTR dwHandlerStartPC)
{
    EECodeInfo codeInfo((PCODE)dwHandlerStartPC);
    _ASSERTE(codeInfo.IsValid());

    return pEHClause->FilterOffset == codeInfo.GetRelOffset();
}

void ExceptionTracker::MakeCallbacksRelatedToHandler(
    bool fBeforeCallingHandler,
    Thread*                pThread,
    MethodDesc*            pMD,
    EE_ILEXCEPTION_CLAUSE* pEHClause,
    DWORD_PTR              dwHandlerStartPC,
    StackFrame             sf
    )
{
    // Here we need to make an extra check for filter handlers because we could be calling the catch handler
    // associated with a filter handler and yet the EH clause we have saved is for the filter handler.
    BOOL fIsFilterHandler         = IsFilterHandler(pEHClause) && ExceptionTracker::IsFilterStartOffset(pEHClause, dwHandlerStartPC);
    BOOL fIsFaultOrFinallyHandler = IsFaultOrFinally(pEHClause);

    if (fBeforeCallingHandler)
    {
        StackFrame sfToStore = sf;
        if ((this->m_pPrevNestedInfo != NULL) &&
            (this->m_pPrevNestedInfo->m_EnclosingClauseInfo == this->m_EnclosingClauseInfo))
        {
            // If this is a nested exception which has the same enclosing clause as the previous exception,
            // we should just propagate the clause info from the previous exception.
            sfToStore = this->m_pPrevNestedInfo->m_EHClauseInfo.GetStackFrameForEHClause();
        }
        m_EHClauseInfo.SetInfo(COR_PRF_CLAUSE_NONE, (UINT_PTR)dwHandlerStartPC, sfToStore);

        if (pMD->IsILStub())
        {
            return;
        }

        if (fIsFilterHandler)
        {
            m_EHClauseInfo.SetEHClauseType(COR_PRF_CLAUSE_FILTER);
            EEToDebuggerExceptionInterfaceWrapper::ExceptionFilter(pMD, (TADDR) dwHandlerStartPC, pEHClause->FilterOffset, (BYTE*)sf.SP);

            EEToProfilerExceptionInterfaceWrapper::ExceptionSearchFilterEnter(pMD);
        }
        else
        {
            EEToDebuggerExceptionInterfaceWrapper::ExceptionHandle(pMD, (TADDR) dwHandlerStartPC, pEHClause->HandlerStartPC, (BYTE*)sf.SP);

            if (fIsFaultOrFinallyHandler)
            {
                m_EHClauseInfo.SetEHClauseType(COR_PRF_CLAUSE_FINALLY);
                EEToProfilerExceptionInterfaceWrapper::ExceptionUnwindFinallyEnter(pMD);
            }
            else
            {
                m_EHClauseInfo.SetEHClauseType(COR_PRF_CLAUSE_CATCH);
                EEToProfilerExceptionInterfaceWrapper::ExceptionCatcherEnter(pThread, pMD);

                DACNotify::DoExceptionCatcherEnterNotification(pMD, pEHClause->HandlerStartPC);
            }
        }
    }
    else
    {
        if (pMD->IsILStub())
        {
            return;
        }

        if (fIsFilterHandler)
        {
            EEToProfilerExceptionInterfaceWrapper::ExceptionSearchFilterLeave();
        }
        else
        {
            if (fIsFaultOrFinallyHandler)
            {
                EEToProfilerExceptionInterfaceWrapper::ExceptionUnwindFinallyLeave();
            }
            else
            {
                EEToProfilerExceptionInterfaceWrapper::ExceptionCatcherLeave();
            }
        }
        m_EHClauseInfo.ResetInfo();
    }
}

#ifdef DEBUGGER_EXCEPTION_INTERCEPTION_SUPPORTED
//---------------------------------------------------------------------------------------
//
// This function is called by DefaultCatchHandler() to intercept an exception and start an unwind.
//
// Arguments:
//    pCurrentEstablisherFrame  - unused on WIN64
//    pExceptionRecord          - EXCEPTION_RECORD of the exception being intercepted
//
// Return Value:
//    ExceptionContinueSearch if the exception cannot be intercepted
//
// Notes:
//    If the exception is intercepted, this function never returns.
//

EXCEPTION_DISPOSITION ClrDebuggerDoUnwindAndIntercept(X86_FIRST_ARG(EXCEPTION_REGISTRATION_RECORD* pCurrentEstablisherFrame)
                                                      EXCEPTION_RECORD* pExceptionRecord)
{
    if (!CheckThreadExceptionStateForInterception())
    {
        return ExceptionContinueSearch;
    }

    Thread*               pThread  = GetThread();
    ThreadExceptionState* pExState = pThread->GetExceptionState();

    UINT_PTR uInterceptStackFrame  = 0;

    pExState->GetDebuggerState()->GetDebuggerInterceptInfo(NULL, NULL,
                                                           (PBYTE*)&uInterceptStackFrame,
                                                           NULL, NULL);

    ClrUnwindEx(pExceptionRecord, (UINT_PTR)pThread, INVALID_RESUME_ADDRESS, uInterceptStackFrame);

    UNREACHABLE();
}
#endif // DEBUGGER_EXCEPTION_INTERCEPTION_SUPPORTED
#endif // DEBUGGING_SUPPORTED

#ifdef _DEBUG
inline bool ExceptionTracker::IsValid()
{
    bool fRetVal = false;

    EX_TRY
    {
        Thread* pThisThread = GetThread();
        if (m_pThread == pThisThread)
        {
            fRetVal = true;
        }
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);

    if (!fRetVal)
    {
        EH_LOG((LL_ERROR, "ExceptionTracker::IsValid() failed!  this = 0x%p\n", this));
    }

    return fRetVal;
}
BOOL ExceptionTracker::ThrowableIsValid()
{
    GCX_COOP();
    CONSISTENCY_CHECK(IsValid());

    BOOL isValid     = FALSE;


    isValid = (m_pThread->GetThrowable() != NULL);

    return isValid;
}
//
// static
UINT_PTR ExceptionTracker::DebugComputeNestingLevel()
{
    UINT_PTR uNestingLevel = 0;
    Thread* pThread = GetThread();

    if (pThread)
    {
        ExceptionTracker* pTracker;
        pTracker = pThread->GetExceptionState()->m_pCurrentTracker;

        while (pTracker)
        {
            uNestingLevel++;
            pTracker = pTracker->m_pPrevNestedInfo;
        };
    }

    return uNestingLevel;
}
void DumpClauses(IJitManager* pJitMan, const METHODTOKEN& MethToken, UINT_PTR uMethodStartPC, UINT_PTR dwControlPc)
{
    EH_CLAUSE_ENUMERATOR    EnumState;
    unsigned                EHCount;

    EH_LOG((LL_INFO1000, "  | uMethodStartPC: %p, ControlPc at offset %x\n", uMethodStartPC, dwControlPc - uMethodStartPC));

    EHCount = pJitMan->InitializeEHEnumeration(MethToken, &EnumState);
    for (unsigned i = 0; i < EHCount; i++)
    {
        EE_ILEXCEPTION_CLAUSE EHClause;
        pJitMan->GetNextEHClause(&EnumState, &EHClause);

        EH_LOG((LL_INFO1000, "  | %s clause [%x, %x], handler: [%x, %x] %s",
                (IsFault(&EHClause)         ? "fault"   :
                (IsFinally(&EHClause)       ? "finally" :
                (IsFilterHandler(&EHClause) ? "filter"  :
                (IsTypedHandler(&EHClause)  ? "typed"   : "unknown")))),
                EHClause.TryStartPC       , // + uMethodStartPC,
                EHClause.TryEndPC         , // + uMethodStartPC,
                EHClause.HandlerStartPC   , // + uMethodStartPC,
                EHClause.HandlerEndPC     , // + uMethodStartPC
                (IsDuplicateClause(&EHClause) ? "[duplicate]" : "")
                ));

        if (IsFilterHandler(&EHClause))
        {
            LOG((LF_EH, LL_INFO1000, " filter: [%x, ...]",
                    EHClause.FilterOffset));// + uMethodStartPC
        }

        LOG((LF_EH, LL_INFO1000, "\n"));
    }

}

#define STACK_ALLOC_ARRAY(numElements, type) \
    ((type *)_alloca((numElements)*(sizeof(type))))

static void DoEHLog(
    DWORD lvl,
    __in_z const char *fmt,
    ...
    )
{
    if (!LoggingOn(LF_EH, lvl))
        return;

    va_list  args;
    va_start(args, fmt);

    UINT_PTR nestinglevel = ExceptionTracker::DebugComputeNestingLevel();
    if (nestinglevel)
    {
        _ASSERTE(FitsIn<UINT_PTR>(2 * nestinglevel));
        UINT_PTR   cch      = 2 * nestinglevel;
        char* pPadding = STACK_ALLOC_ARRAY(cch + 1, char);
        memset(pPadding, '.', cch);
        pPadding[cch] = 0;

        LOG((LF_EH, lvl, pPadding));
    }

    LogSpewValist(LF_EH, lvl, fmt, args);
    va_end(args);
}
#endif // _DEBUG

#ifdef FEATURE_PAL

//---------------------------------------------------------------------------------------
//
// This functions performs an unwind procedure for a managed exception. The stack is unwound
// until the target frame is reached. For each frame we use its PC value to find
// a handler using information that has been built by JIT.
//
// Arguments:
//      ex                       - the PAL_SEHException representing the managed exception
//      unwindStartContext       - the context that the unwind should start at. Either the original exception
//                                 context (when the exception didn't cross native frames) or the first managed
//                                 frame after crossing native frames.
//
VOID UnwindManagedExceptionPass2(PAL_SEHException& ex, CONTEXT* unwindStartContext)
{
    UINT_PTR controlPc;
    PVOID sp;
    EXCEPTION_DISPOSITION disposition;
    CONTEXT* currentFrameContext;
    CONTEXT* callerFrameContext;
    CONTEXT contextStorage;
    DISPATCHER_CONTEXT dispatcherContext;
    EECodeInfo codeInfo;
    UINT_PTR establisherFrame = NULL;
    PVOID handlerData;

    // Indicate that we are performing second pass.
    ex.GetExceptionRecord()->ExceptionFlags = EXCEPTION_UNWINDING;

    currentFrameContext = unwindStartContext;
    callerFrameContext = &contextStorage;

    memset(&dispatcherContext, 0, sizeof(DISPATCHER_CONTEXT));
    disposition = ExceptionContinueSearch;

    do
    {
        controlPc = GetIP(currentFrameContext);

        codeInfo.Init(controlPc);

        dispatcherContext.FunctionEntry = codeInfo.GetFunctionEntry();
        dispatcherContext.ControlPc = controlPc;
        dispatcherContext.ImageBase = codeInfo.GetModuleBase();
#ifdef ADJUST_PC_UNWOUND_TO_CALL
        dispatcherContext.ControlPcIsUnwound = !!(currentFrameContext->ContextFlags & CONTEXT_UNWOUND_TO_CALL);
#endif
        // Check whether we have a function table entry for the current controlPC.
        // If yes, then call RtlVirtualUnwind to get the establisher frame pointer.
        if (dispatcherContext.FunctionEntry != NULL)
        {
            // Create a copy of the current context because we don't want
            // the current context record to be updated by RtlVirtualUnwind.
            memcpy(callerFrameContext, currentFrameContext, sizeof(CONTEXT));
            RtlVirtualUnwind(UNW_FLAG_EHANDLER,
                dispatcherContext.ImageBase,
                dispatcherContext.ControlPc,
                dispatcherContext.FunctionEntry,
                callerFrameContext,
                &handlerData,
                &establisherFrame,
                NULL);

            // Make sure that the establisher frame pointer is within stack boundaries
            // and we did not go below that target frame.
            // TODO: make sure the establisher frame is properly aligned.
            if (!Thread::IsAddressInCurrentStack((void*)establisherFrame) || establisherFrame > ex.TargetFrameSp)
            {
                // TODO: add better error handling
                UNREACHABLE();
            }

            dispatcherContext.EstablisherFrame = establisherFrame;
            dispatcherContext.ContextRecord = currentFrameContext;

            EXCEPTION_RECORD* exceptionRecord = ex.GetExceptionRecord();

            if (establisherFrame == ex.TargetFrameSp)
            {
                // We have reached the frame that will handle the exception.
                ex.GetExceptionRecord()->ExceptionFlags |= EXCEPTION_TARGET_UNWIND;
                ExceptionTracker* pTracker = GetThread()->GetExceptionState()->GetCurrentExceptionTracker();
                pTracker->TakeExceptionPointersOwnership(&ex);
            }

            // Perform unwinding of the current frame
            disposition = ProcessCLRException(exceptionRecord,
                establisherFrame,
                currentFrameContext,
                &dispatcherContext);

            if (disposition == ExceptionContinueSearch)
            {
                // Exception handler not found. Try the parent frame.
                CONTEXT* temp = currentFrameContext;
                currentFrameContext = callerFrameContext;
                callerFrameContext = temp;
            }
            else
            {
                UNREACHABLE();
            }
        }
        else
        {
            Thread::VirtualUnwindLeafCallFrame(currentFrameContext);
        }

        controlPc = GetIP(currentFrameContext);
        sp = (PVOID)GetSP(currentFrameContext);

        // Check whether we are crossing managed-to-native boundary
        if (!ExecutionManager::IsManagedCode(controlPc))
        {
            // Return back to the UnwindManagedExceptionPass1 and let it unwind the native frames
            {
                GCX_COOP();
                // Pop all frames that are below the block of native frames and that would be
                // in the unwound part of the stack when UnwindManagedExceptionPass2 is resumed 
                // at the next managed frame.

                UnwindFrameChain(GetThread(), sp);
                // We are going to reclaim the stack range that was scanned by the exception tracker
                // until now. We need to reset the explicit frames range so that if GC fires before
                // we recreate the tracker at the first managed frame after unwinding the native 
                // frames, it doesn't attempt to scan the reclaimed stack range.
                // We also need to reset the scanned stack range since the scanned frames will be
                // obsolete after the unwind of the native frames completes.
                ExceptionTracker* pTracker = GetThread()->GetExceptionState()->GetCurrentExceptionTracker();
                pTracker->CleanupBeforeNativeFramesUnwind();
            }

            // Now we need to unwind the native frames until we reach managed frames again or the exception is
            // handled in the native code.
            STRESS_LOG2(LF_EH, LL_INFO100, "Unwinding native frames starting at IP = %p, SP = %p \n", controlPc, sp);
            PAL_ThrowExceptionFromContext(currentFrameContext, &ex);
            UNREACHABLE();
        }

    } while (Thread::IsAddressInCurrentStack(sp) && (establisherFrame != ex.TargetFrameSp));

    _ASSERTE(!"UnwindManagedExceptionPass2: Unwinding failed. Reached the end of the stack");
    EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
}

//---------------------------------------------------------------------------------------
//
// This functions performs dispatching of a managed exception.
// It tries to find an exception handler by examining each frame in the call stack.
// The search is started from the managed frame caused the exception to be thrown.
// For each frame we use its PC value to find a handler using information that
// has been built by JIT. If an exception handler is found then this function initiates
// the second pass to unwind the stack and execute the handler.
//
// Arguments:
//      ex           - a PAL_SEHException that stores information about the managed
//                     exception that needs to be dispatched.
//      frameContext - the context of the first managed frame of the exception call stack
//
VOID DECLSPEC_NORETURN UnwindManagedExceptionPass1(PAL_SEHException& ex, CONTEXT* frameContext)
{
    CONTEXT unwindStartContext;
    EXCEPTION_DISPOSITION disposition;
    DISPATCHER_CONTEXT dispatcherContext;
    EECodeInfo codeInfo;
    UINT_PTR controlPc;
    UINT_PTR establisherFrame = NULL;
    PVOID handlerData;

#ifdef FEATURE_HIJACK
    GetThread()->UnhijackThread();
#endif

    controlPc = GetIP(frameContext);
    unwindStartContext = *frameContext;

    if (!ExecutionManager::IsManagedCode(GetIP(ex.GetContextRecord())))
    {
        // This is the first time we see the managed exception, set its context to the managed frame that has caused
        // the exception to be thrown
        *ex.GetContextRecord() = *frameContext;
        ex.GetExceptionRecord()->ExceptionAddress = (VOID*)controlPc;
    }

    ex.GetExceptionRecord()->ExceptionFlags = 0;

    memset(&dispatcherContext, 0, sizeof(DISPATCHER_CONTEXT));
    disposition = ExceptionContinueSearch;

    do
    {
        codeInfo.Init(controlPc);
        dispatcherContext.FunctionEntry = codeInfo.GetFunctionEntry();
        dispatcherContext.ControlPc = controlPc;
        dispatcherContext.ImageBase = codeInfo.GetModuleBase();
#ifdef ADJUST_PC_UNWOUND_TO_CALL
        dispatcherContext.ControlPcIsUnwound = !!(frameContext->ContextFlags & CONTEXT_UNWOUND_TO_CALL);
#endif

        // Check whether we have a function table entry for the current controlPC.
        // If yes, then call RtlVirtualUnwind to get the establisher frame pointer
        // and then check whether an exception handler exists for the frame.
        if (dispatcherContext.FunctionEntry != NULL)
        {
#ifdef USE_CURRENT_CONTEXT_IN_FILTER
            KNONVOLATILE_CONTEXT currentNonVolatileContext;
            CaptureNonvolatileRegisters(&currentNonVolatileContext, frameContext);
#endif // USE_CURRENT_CONTEXT_IN_FILTER

            RtlVirtualUnwind(UNW_FLAG_EHANDLER,
                dispatcherContext.ImageBase,
                dispatcherContext.ControlPc,
                dispatcherContext.FunctionEntry,
                frameContext,
                &handlerData,
                &establisherFrame,
                NULL);

            // Make sure that the establisher frame pointer is within stack boundaries.
            // TODO: make sure the establisher frame is properly aligned.
            if (!Thread::IsAddressInCurrentStack((void*)establisherFrame))
            {
                // TODO: add better error handling
                UNREACHABLE();
            }

            dispatcherContext.EstablisherFrame = establisherFrame;
#ifdef USE_CURRENT_CONTEXT_IN_FILTER
            dispatcherContext.CurrentNonVolatileContextRecord = &currentNonVolatileContext;
#endif // USE_CURRENT_CONTEXT_IN_FILTER
            dispatcherContext.ContextRecord = frameContext;

            // Find exception handler in the current frame
            disposition = ProcessCLRException(ex.GetExceptionRecord(),
                establisherFrame,
                ex.GetContextRecord(),
                &dispatcherContext);

            if (disposition == ExceptionContinueSearch)
            {
                // Exception handler not found. Try the parent frame.
                controlPc = GetIP(frameContext);
            }
            else if (disposition == ExceptionStackUnwind)
            {
                // The first pass is complete. We have found the frame that
                // will handle the exception. Start the second pass.
                ex.TargetFrameSp = establisherFrame;
                UnwindManagedExceptionPass2(ex, &unwindStartContext);
            }
            else
            {
                // TODO: This needs to implemented. Make it fail for now.
                UNREACHABLE();
            }
        }
        else
        {
            controlPc = Thread::VirtualUnwindLeafCallFrame(frameContext);
        }

        // Check whether we are crossing managed-to-native boundary
        while (!ExecutionManager::IsManagedCode(controlPc))
        {
            UINT_PTR sp = GetSP(frameContext);

            BOOL success = PAL_VirtualUnwind(frameContext, NULL);
            if (!success)
            {
                _ASSERTE(!"UnwindManagedExceptionPass1: PAL_VirtualUnwind failed");
                EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
            }

            controlPc = GetIP(frameContext);

            STRESS_LOG2(LF_EH, LL_INFO100, "Processing exception at native frame: IP = %p, SP = %p \n", controlPc, sp);

            if (controlPc == 0)
            {
                if (!GetThread()->HasThreadStateNC(Thread::TSNC_ProcessedUnhandledException))
                {
                    LONG disposition = InternalUnhandledExceptionFilter_Worker(&ex.ExceptionPointers);
                    _ASSERTE(disposition == EXCEPTION_CONTINUE_SEARCH);
                }
                TerminateProcess(GetCurrentProcess(), 1);
                UNREACHABLE();
            }

            UINT_PTR parentSp = GetSP(frameContext);

            // Find all holders on this frame that are in scopes embedded in each other and call their filters.
            NativeExceptionHolderBase* holder = nullptr;
            while ((holder = NativeExceptionHolderBase::FindNextHolder(holder, (void*)sp, (void*)parentSp)) != nullptr)
            {
                EXCEPTION_DISPOSITION disposition =  holder->InvokeFilter(ex);
                if (disposition == EXCEPTION_EXECUTE_HANDLER)
                {
                    // Switch to pass 2
                    STRESS_LOG1(LF_EH, LL_INFO100, "First pass finished, found native handler, TargetFrameSp = %p\n", sp);

                    ex.TargetFrameSp = sp;
                    UnwindManagedExceptionPass2(ex, &unwindStartContext);
                    UNREACHABLE();
                }

                // The EXCEPTION_CONTINUE_EXECUTION is not supported and should never be returned by a filter
                _ASSERTE(disposition == EXCEPTION_CONTINUE_SEARCH);
            }
        }

    } while (Thread::IsAddressInCurrentStack((void*)GetSP(frameContext)));

    _ASSERTE(!"UnwindManagedExceptionPass1: Failed to find a handler. Reached the end of the stack");
    EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
    UNREACHABLE();
}

VOID DECLSPEC_NORETURN DispatchManagedException(PAL_SEHException& ex, bool isHardwareException)
{
    do
    {
        try
        {
            // Unwind the context to the first managed frame
            CONTEXT frameContext;

            // If the exception is hardware exceptions, we use the exception's context record directly
            if (isHardwareException)
            {
                frameContext = *ex.GetContextRecord();
            }
            else
            {
                RtlCaptureContext(&frameContext);
                UINT_PTR currentSP = GetSP(&frameContext);

                if (Thread::VirtualUnwindToFirstManagedCallFrame(&frameContext) == 0)
                {
                    // There are no managed frames on the stack, so we need to continue unwinding using C++ exception
                    // handling
                    break;
                }

                UINT_PTR firstManagedFrameSP = GetSP(&frameContext);

                // Check if there is any exception holder in the skipped frames. If there is one, we need to unwind them
                // using the C++ handling. This is a special case when the UNINSTALL_MANAGED_EXCEPTION_DISPATCHER was
                // not at the managed to native boundary.
                if (NativeExceptionHolderBase::FindNextHolder(nullptr, (void*)currentSP, (void*)firstManagedFrameSP) != nullptr)
                {
                    break;
                }
            }

            if (ex.IsFirstPass())
            {
                UnwindManagedExceptionPass1(ex, &frameContext);
            }
            else
            {
                // This is a continuation of pass 2 after native frames unwinding.
                UnwindManagedExceptionPass2(ex, &frameContext);
            }
            UNREACHABLE();
        }
        catch (PAL_SEHException& ex2)
        {
            isHardwareException = false;
            ex = std::move(ex2);
        }

    }
    while (true);

    // Ensure that the corruption severity is set for exceptions that didn't pass through managed frames
    // yet and so there is no exception tracker.
    if (ex.IsFirstPass())
    {
        // Get the thread and the thread exception state - they must exist at this point
        Thread *pCurThread = GetThread();
        _ASSERTE(pCurThread != NULL);

        ThreadExceptionState * pCurTES = pCurThread->GetExceptionState();
        _ASSERTE(pCurTES != NULL);

#ifdef FEATURE_CORRUPTING_EXCEPTIONS
        ExceptionTracker* pEHTracker = pCurTES->GetCurrentExceptionTracker();
        if (pEHTracker == NULL)
        {
            CorruptionSeverity severity = NotCorrupting;
            if (CEHelper::IsProcessCorruptedStateException(ex.GetExceptionRecord()->ExceptionCode))
            {
                severity = ProcessCorrupting;
            }

            pCurTES->SetLastActiveExceptionCorruptionSeverity(severity);
        }
#endif // FEATURE_CORRUPTING_EXCEPTIONS
    }

    throw std::move(ex);
}

#if defined(_TARGET_AMD64_) || defined(_TARGET_X86_)

/*++
Function :
    GetRegisterAddressByIndex

    Get address of a register in a context

Parameters:
    PCONTEXT pContext : context containing the registers
    UINT index :        index of the register (Rax=0 .. R15=15)

Return value :
    Pointer to the context member represeting the register
--*/
VOID* GetRegisterAddressByIndex(PCONTEXT pContext, UINT index)
{
    return getRegAddr(index, pContext);
}

/*++
Function :
    GetRegisterValueByIndex

    Get value of a register in a context

Parameters:
    PCONTEXT pContext : context containing the registers
    UINT index :        index of the register (Rax=0 .. R15=15)

Return value :
    Value of the context member represeting the register
--*/
DWORD64 GetRegisterValueByIndex(PCONTEXT pContext, UINT index)
{
    _ASSERTE(index < 16);
    return *(DWORD64*)GetRegisterAddressByIndex(pContext, index);
}

/*++
Function :
    GetModRMOperandValue

    Get value of an instruction operand represented by the ModR/M field

Parameters:
    BYTE rex :              REX prefix, 0 if there was none
    BYTE* ip :              instruction pointer pointing to the ModR/M field
    PCONTEXT pContext :     context containing the registers
    bool is8Bit :           true if the operand size is 8 bit
    bool hasOpSizePrefix :  true if the instruction has op size prefix (0x66)

Return value :
    Value of the context member represeting the register
--*/
DWORD64 GetModRMOperandValue(BYTE rex, BYTE* ip, PCONTEXT pContext, bool is8Bit, bool hasOpSizePrefix)
{
    DWORD64 result;

    BYTE rex_b = (rex & 0x1);       // high bit to modrm r/m field or SIB base field
    BYTE rex_x = (rex & 0x2) >> 1;  // high bit to sib index field
    BYTE rex_r = (rex & 0x4) >> 2;  // high bit to modrm reg field
    BYTE rex_w = (rex & 0x8) >> 3;  // 1 = 64 bit operand size, 0 = operand size determined by hasOpSizePrefix

    BYTE modrm = *ip++;

    _ASSERTE(modrm != 0);

    BYTE mod = (modrm & 0xC0) >> 6;
    BYTE reg = (modrm & 0x38) >> 3;
    BYTE rm = (modrm & 0x07);

    reg |= (rex_r << 3);
    BYTE rmIndex = rm | (rex_b << 3);

    // 8 bit idiv without the REX prefix uses registers AH, CH, DH, BH for rm 4..8
    // which is an exception from the regular register indexes.
    bool isAhChDhBh = is8Bit && (rex == 0) && (rm >= 4);

    // See: Tables A-15,16,17 in AMD Dev Manual 3 for information
    //      about how the ModRM/SIB/REX bytes interact.

    switch (mod)
    {
    case 0:
    case 1:
    case 2:
        if (rm == 4) // we have an SIB byte following
        {
            //
            // Get values from the SIB byte
            //
            BYTE sib = *ip++;

            _ASSERTE(sib != 0);

            BYTE ss = (sib & 0xC0) >> 6;
            BYTE index = (sib & 0x38) >> 3;
            BYTE base = (sib & 0x07);

            index |= (rex_x << 3);
            base |= (rex_b << 3);

            //
            // Get starting value
            //
            if ((mod == 0) && (base == 5))
            {
                result = 0;
            }
            else
            {
                result = GetRegisterValueByIndex(pContext, base);
            }

            //
            // Add in the [index]
            //
            if (index != 4)
            {
                result += GetRegisterValueByIndex(pContext, index) << ss;
            }

            //
            // Finally add in the offset
            //
            if (mod == 0)
            {
                if (base == 5)
                {
                    result += *((INT32*)ip);
                }
            }
            else if (mod == 1)
            {
                result += *((INT8*)ip);
            }
            else // mod == 2
            {
                result += *((INT32*)ip);
            }

        }
        else
        {
            //
            // Get the value we need from the register.
            //

            // Check for RIP-relative addressing mode for AMD64
            // Check for Displacement only addressing mode for x86
            if ((mod == 0) && (rm == 5))
            {
#if defined(_TARGET_AMD64_)
                result = (DWORD64)ip + sizeof(INT32) + *(INT32*)ip;
#else
                result = (DWORD64)(*(DWORD*)ip);
#endif // _TARGET_AMD64_
            }
            else
            {
                result = GetRegisterValueByIndex(pContext, rmIndex);

                if (mod == 1)
                {
                    result += *((INT8*)ip);
                }
                else if (mod == 2)
                {
                    result += *((INT32*)ip);
                }
            }
        }

        break;

    case 3:
    default:
        // The operand is stored in a register.
        if (isAhChDhBh)
        {
            // 8 bit idiv without the REX prefix uses registers AH, CH, DH or BH for rm 4..8.
            // So we shift the register index to get the real register index.
            rmIndex -= 4;
        }

        result = (DWORD64)GetRegisterAddressByIndex(pContext, rmIndex);

        if (isAhChDhBh)
        {
            // Move one byte higher to get an address of the AH, CH, DH or BH
            result++;
        }

        break;

    }

    //
    // Now dereference thru the result to get the resulting value.
    //
    if (is8Bit)
    {
        result = *((BYTE*)result);
    }
    else if (rex_w != 0)
    {
        result = *((DWORD64*)result);
    }
    else if (hasOpSizePrefix)
    {
        result = *((USHORT*)result);
    }
    else
    {
        result = *((UINT32*)result);
    }

    return result;
}

/*++
Function :
    SkipPrefixes

    Skip all prefixes until the instruction code or the REX prefix is found

Parameters:
    BYTE** ip :             Pointer to the current instruction pointer. Updated 
                            as the function walks the codes.
    bool* hasOpSizePrefix : Pointer to bool, on exit set to true if a op size prefix
                            was found.

Return value :
    Code of the REX prefix or the instruction code after the prefixes.
--*/
BYTE SkipPrefixes(BYTE **ip, bool* hasOpSizePrefix)
{
    *hasOpSizePrefix = false;

    while (true)
    {
        BYTE code = *(*ip)++;

        switch (code)
        {
        case 0x66: // Operand-Size
            *hasOpSizePrefix = true;
            break;
            
            // Segment overrides
        case 0x26: // ES
        case 0x2E: // CS
        case 0x36: // SS
        case 0x3E: // DS 
        case 0x64: // FS
        case 0x65: // GS

            // Size overrides
        case 0x67: // Address-Size

            // Lock
        case 0xf0:

            // String REP prefixes
        case 0xf2: // REPNE/REPNZ
        case 0xf3:
            break;

        default:
            // Return address of the nonprefix code
            return code;
        }
    }
}

/*++
Function :
    IsDivByZeroAnIntegerOverflow

    Check if a division by zero exception is in fact a division overflow. The
    x64 processor generate the same exception in both cases for the IDIV / DIV
    instruction. So we need to decode the instruction argument and check
    whether it was zero or not.

Parameters:
    PCONTEXT pContext :           context containing the registers
    PEXCEPTION_RECORD pExRecord : exception record of the exception

Return value :
    true if the division error was an overflow
--*/
bool IsDivByZeroAnIntegerOverflow(PCONTEXT pContext)
{
    BYTE * ip = (BYTE *)GetIP(pContext);
    BYTE rex = 0;
    bool hasOpSizePrefix = false;

    BYTE code = SkipPrefixes(&ip, &hasOpSizePrefix);

    // The REX prefix must directly preceed the instruction code
    if ((code & 0xF0) == 0x40)
    {
        rex = code;
        code = *ip++;
    }

    DWORD64 divisor = 0;

    // Check if the instruction is IDIV or DIV. The instruction code includes the three
    // 'reg' bits in the ModRM byte. These are 7 for IDIV and 6 for DIV
    BYTE regBits = (*ip & 0x38) >> 3;
    if ((code == 0xF7 || code == 0xF6) && (regBits == 7 || regBits == 6))
    {
        bool is8Bit = (code == 0xF6);
        divisor = GetModRMOperandValue(rex, ip, pContext, is8Bit, hasOpSizePrefix);
    }
    else
    {
        _ASSERTE(!"Invalid instruction (expected IDIV or DIV)");
    }

    // If the division operand is zero, it was division by zero. Otherwise the failure 
    // must have been an overflow.
    return divisor != 0;
}
#endif // _TARGET_AMD64_ || _TARGET_X86_

BOOL IsSafeToCallExecutionManager()
{
    Thread *pThread = GetThread();

    // It is safe to call the ExecutionManager::IsManagedCode only if the current thread is in
    // the cooperative mode. Otherwise ExecutionManager::IsManagedCode could deadlock if 
    // the exception happened when the thread was holding the ExecutionManager's writer lock.
    // When the thread is in preemptive mode, we know for sure that it is not executing managed code.
    // Unfortunately, when running GC stress mode that invokes GC after every jitted or NGENed
    // instruction, we need to relax that to enable instrumentation of PInvoke stubs that switch to
    // preemptive GC mode at some point.
    return ((pThread != NULL) && pThread->PreemptiveGCDisabled()) || 
           GCStress<cfg_instr_jit>::IsEnabled() || 
           GCStress<cfg_instr_ngen>::IsEnabled();
}

#ifdef VSD_STUB_CAN_THROW_AV
//Return TRUE if pContext->Pc is in VirtualStub
static BOOL IsIPinVirtualStub(PCODE f_IP)
{
    LIMITED_METHOD_CONTRACT;

    Thread * pThread = GetThread();

    // We may not have a managed thread object. Example is an AV on the helper thread.
    // (perhaps during StubManager::IsStub)
    if (pThread == NULL)
    {
        return FALSE;
    }

    VirtualCallStubManager::StubKind sk;
    VirtualCallStubManager::FindStubManager(f_IP, &sk, FALSE /* usePredictStubKind */);

    if (sk == VirtualCallStubManager::SK_DISPATCH)
    {
        return TRUE;
    }
    else if (sk == VirtualCallStubManager::SK_RESOLVE)
    {
        return TRUE;
    }

    else {
        return FALSE;
    }
}
#endif // VSD_STUB_CAN_THROW_AV

BOOL IsSafeToHandleHardwareException(PCONTEXT contextRecord, PEXCEPTION_RECORD exceptionRecord)
{
    PCODE controlPc = GetIP(contextRecord);
    return g_fEEStarted && (
        exceptionRecord->ExceptionCode == STATUS_BREAKPOINT || 
        exceptionRecord->ExceptionCode == STATUS_SINGLE_STEP ||
        (IsSafeToCallExecutionManager() && ExecutionManager::IsManagedCode(controlPc)) ||
#ifdef VSD_STUB_CAN_THROW_AV
        IsIPinVirtualStub(controlPc) ||  // access violation comes from DispatchStub of Interface call
#endif // VSD_STUB_CAN_THROW_AV
        IsIPInMarkedJitHelper(controlPc));
}

#ifdef FEATURE_EMULATE_SINGLESTEP
static inline BOOL HandleSingleStep(PCONTEXT pContext, PEXCEPTION_RECORD pExceptionRecord, Thread *pThread)
{
    // On ARM we don't have any reliable hardware support for single stepping so it is emulated in software.
    // The implementation will end up throwing an EXCEPTION_BREAKPOINT rather than an EXCEPTION_SINGLE_STEP
    // and leaves other aspects of the thread context in an invalid state. Therefore we use this opportunity
    // to fixup the state before any other part of the system uses it (we do it here since only the debugger
    // uses single step functionality).

    // First ask the emulation itself whether this exception occurred while single stepping was enabled. If so
    // it will fix up the context to be consistent again and return true. If so and the exception was
    // EXCEPTION_BREAKPOINT then we translate it to EXCEPTION_SINGLE_STEP (otherwise we leave it be, e.g. the
    // instruction stepped caused an access violation).
    if (pThread->HandleSingleStep(pContext, pExceptionRecord->ExceptionCode) && (pExceptionRecord->ExceptionCode == EXCEPTION_BREAKPOINT))
    {
        pExceptionRecord->ExceptionCode = EXCEPTION_SINGLE_STEP;
        pExceptionRecord->ExceptionAddress = (void *)GetIP(pContext);
        return TRUE;
    }
    return FALSE;
}
#endif // FEATURE_EMULATE_SINGLESTEP

BOOL HandleHardwareException(PAL_SEHException* ex)
{
    _ASSERTE(IsSafeToHandleHardwareException(ex->GetContextRecord(), ex->GetExceptionRecord()));

    if (ex->GetExceptionRecord()->ExceptionCode != STATUS_BREAKPOINT && ex->GetExceptionRecord()->ExceptionCode != STATUS_SINGLE_STEP)
    {
        // A hardware exception is handled only if it happened in a jitted code or 
        // in one of the JIT helper functions (JIT_MemSet, ...)
        PCODE controlPc = GetIP(ex->GetContextRecord());
        if (ExecutionManager::IsManagedCode(controlPc) && IsGcMarker(ex->GetContextRecord(), ex->GetExceptionRecord()))
        {
            // Exception was handled, let the signal handler return to the exception context. Some registers in the context can
            // have been modified by the GC.
            return TRUE;
        }

#if defined(_TARGET_AMD64_) || defined(_TARGET_X86_)
        // It is possible that an overflow was mapped to a divide-by-zero exception. 
        // This happens when we try to divide the maximum negative value of a
        // signed integer with -1. 
        //
        // Thus, we will attempt to decode the instruction @ RIP to determine if that
        // is the case using the faulting context.
        if ((ex->GetExceptionRecord()->ExceptionCode == EXCEPTION_INT_DIVIDE_BY_ZERO) &&
            IsDivByZeroAnIntegerOverflow(ex->GetContextRecord()))
        {
            // The exception was an integer overflow, so augment the exception code.
            ex->GetExceptionRecord()->ExceptionCode = EXCEPTION_INT_OVERFLOW;
        }
#endif // _TARGET_AMD64_ || _TARGET_X86_

        // Create frame necessary for the exception handling
        FrameWithCookie<FaultingExceptionFrame> fef;
        *((&fef)->GetGSCookiePtr()) = GetProcessGSCookie();
        {
            GCX_COOP();     // Must be cooperative to modify frame chain.
            if (IsIPInMarkedJitHelper(controlPc))
            {
                // For JIT helpers, we need to set the frame to point to the
                // managed code that called the helper, otherwise the stack
                // walker would skip all the managed frames upto the next
                // explicit frame.
                PAL_VirtualUnwind(ex->GetContextRecord(), NULL);
                ex->GetExceptionRecord()->ExceptionAddress = (PVOID)GetIP(ex->GetContextRecord());
            }
#ifdef VSD_STUB_CAN_THROW_AV
            else if (IsIPinVirtualStub(controlPc)) 
            {
                AdjustContextForVirtualStub(ex->GetExceptionRecord(), ex->GetContextRecord());
            }
#endif // VSD_STUB_CAN_THROW_AV
            fef.InitAndLink(ex->GetContextRecord());
        }

        DispatchManagedException(*ex, true /* isHardwareException */);
        UNREACHABLE();
    }
    else
    {
        // This is a breakpoint or single step stop, we report it to the debugger.
        Thread *pThread = GetThread();
        if (pThread != NULL && g_pDebugInterface != NULL)
        {
            // On ARM and ARM64 Linux exception point to the break instruction.
            // See https://static.docs.arm.com/ddi0487/db/DDI0487D_b_armv8_arm.pdf#page=6916&zoom=100,0,152
            // at aarch64/exceptions/debug/AArch64.SoftwareBreakpoint
            // However, the rest of the code expects that it points to an instruction after the break.
#if defined(__linux__) && (defined(_TARGET_ARM_) || defined(_TARGET_ARM64_))
            if (ex->GetExceptionRecord()->ExceptionCode == STATUS_BREAKPOINT)
            {
                SetIP(ex->GetContextRecord(), GetIP(ex->GetContextRecord()) + CORDbg_BREAK_INSTRUCTION_SIZE);
                ex->GetExceptionRecord()->ExceptionAddress = (void *)GetIP(ex->GetContextRecord());
            }
#endif

#ifdef FEATURE_EMULATE_SINGLESTEP
            HandleSingleStep(ex->GetContextRecord(), ex->GetExceptionRecord(), pThread);
#endif
            if (ex->GetExceptionRecord()->ExceptionCode == STATUS_BREAKPOINT)
            {
                // If this is breakpoint context, it is set up to point to an instruction after the break instruction.
                // But debugger expects to see context that points to the break instruction, that's why we correct it.
                SetIP(ex->GetContextRecord(), GetIP(ex->GetContextRecord()) - CORDbg_BREAK_INSTRUCTION_SIZE);
                ex->GetExceptionRecord()->ExceptionAddress = (void *)GetIP(ex->GetContextRecord());
            }

            if (g_pDebugInterface->FirstChanceNativeException(ex->GetExceptionRecord(),
                ex->GetContextRecord(),
                ex->GetExceptionRecord()->ExceptionCode,
                pThread))
            {
                // Exception was handled, let the signal handler return to the exception context. Some registers in the context can
                // have been modified by the debugger.
                return TRUE;
            }
        }
    }

    return FALSE;
}

#endif // FEATURE_PAL

#ifndef FEATURE_PAL
void ClrUnwindEx(EXCEPTION_RECORD* pExceptionRecord, UINT_PTR ReturnValue, UINT_PTR TargetIP, UINT_PTR TargetFrameSp)
{
    PVOID TargetFrame = (PVOID)TargetFrameSp;

    CONTEXT ctx;
    RtlUnwindEx(TargetFrame,
                (PVOID)TargetIP,
                pExceptionRecord,
                (PVOID)ReturnValue, // ReturnValue
                &ctx,
                NULL);      // HistoryTable

    // doesn't return
    UNREACHABLE();
}
#endif // !FEATURE_PAL

void TrackerAllocator::Init()
{
    void* pvFirstPage = (void*)new BYTE[TRACKER_ALLOCATOR_PAGE_SIZE];

    ZeroMemory(pvFirstPage, TRACKER_ALLOCATOR_PAGE_SIZE);

    m_pFirstPage = (Page*)pvFirstPage;

    _ASSERTE(NULL == m_pFirstPage->m_header.m_pNext);
    _ASSERTE(0    == m_pFirstPage->m_header.m_idxFirstFree);

    m_pCrst = new Crst(CrstException, CRST_UNSAFE_ANYMODE);

    EH_LOG((LL_INFO100, "TrackerAllocator::Init() succeeded..\n"));
}

void TrackerAllocator::Terminate()
{
    Page* pPage = m_pFirstPage;

    while (pPage)
    {
        Page* pDeleteMe = pPage;
        pPage = pPage->m_header.m_pNext;
        delete [] pDeleteMe;
    }
    delete m_pCrst;
}

ExceptionTracker* TrackerAllocator::GetTrackerMemory()
{
    CONTRACT(ExceptionTracker*)
    {
        GC_TRIGGERS;
        NOTHROW;
        MODE_ANY;
        POSTCONDITION(CheckPointer(RETVAL, NULL_OK));
    }
    CONTRACT_END;

    _ASSERTE(NULL != m_pFirstPage);

    Page* pPage = m_pFirstPage;

    ExceptionTracker* pTracker = NULL;

    for (int i = 0; i < TRACKER_ALLOCATOR_MAX_OOM_SPINS; i++)
    {
        { // open lock scope
            CrstHolder  ch(m_pCrst);

            while (pPage)
            {
                int idx;
                for (idx = 0; idx < NUM_TRACKERS_PER_PAGE; idx++)
                {
                    pTracker = &(pPage->m_rgTrackers[idx]);
                    if (pTracker->m_pThread == NULL)
                    {
                        break;
                    }
                }

                if (idx < NUM_TRACKERS_PER_PAGE)
                {
                    break;
                }
                else
                {
                    if (NULL == pPage->m_header.m_pNext)
                    {
                        Page* pNewPage = (Page*) new (nothrow) BYTE[TRACKER_ALLOCATOR_PAGE_SIZE];

                        if (pNewPage)
                        {
                            STRESS_LOG0(LF_EH, LL_INFO10, "TrackerAllocator:  allocated page\n");
                            pPage->m_header.m_pNext = pNewPage;
                            ZeroMemory(pPage->m_header.m_pNext, TRACKER_ALLOCATOR_PAGE_SIZE);
                        }
                        else
                        {
                            STRESS_LOG0(LF_EH, LL_WARNING, "TrackerAllocator:  failed to allocate a page\n");
                            pTracker = NULL;
                        }
                    }

                    pPage = pPage->m_header.m_pNext;
                }
            }

            if (pTracker)
            {
                Thread* pThread  = GetThread();
                _ASSERTE(NULL != pPage);
                ZeroMemory(pTracker, sizeof(*pTracker));
                pTracker->m_pThread = pThread;
                EH_LOG((LL_INFO100, "TrackerAllocator: allocating tracker 0x%p, thread = 0x%p\n", pTracker, pTracker->m_pThread));
                break;
            }
        } // end lock scope

        //
        // We could not allocate a new page of memory.  This is a fatal error if it happens twice (nested)
        // on the same thread because we have only one m_OOMTracker.  We will spin hoping for another thread
        // to give back to the pool or for the allocation to succeed.
        //

        ClrSleepEx(TRACKER_ALLOCATOR_OOM_SPIN_DELAY, FALSE);
        STRESS_LOG1(LF_EH, LL_WARNING, "TrackerAllocator:  retry #%d\n", i);
    }

    RETURN pTracker;
}

void TrackerAllocator::FreeTrackerMemory(ExceptionTracker* pTracker)
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        MODE_ANY;
    }
    CONTRACTL_END;

    // mark this entry as free
    EH_LOG((LL_INFO100, "TrackerAllocator: freeing tracker 0x%p, thread = 0x%p\n", pTracker, pTracker->m_pThread));
    CONSISTENCY_CHECK(pTracker->IsValid());
    FastInterlockExchangePointer(&(pTracker->m_pThread), NULL);
}

#ifndef FEATURE_PAL
// This is Windows specific implementation as it is based upon the notion of collided unwind that is specific
// to Windows 64bit.
//
// If pContext is not NULL, then this function copies pContext to pDispatcherContext->ContextRecord.  If pContext
// is NULL, then this function assumes that pDispatcherContext->ContextRecord has already been fixed up.  In any
// case, this function then starts to update the various fields in pDispatcherContext.
//
// In order to redirect the unwind, the OS requires us to provide a personality routine for the code at the
// new context we are providing. If RtlVirtualUnwind can't determine the personality routine and using
// the default managed code personality routine isn't appropriate (maybe you aren't returning to managed code)
// specify pUnwindPersonalityRoutine. For instance the debugger uses this to unwind from ExceptionHijack back
// to RaiseException in win32 and specifies an empty personality routine. For more details about this
// see the comments in the code below.
//
// <AMD64-specific>
// AMD64 is more "advanced", in that the DISPATCHER_CONTEXT contains a field for the TargetIp.  So we don't have
// to use the control PC in pDispatcherContext->ContextRecord to indicate the target IP for the unwind.  However,
// this also means that pDispatcherContext->ContextRecord is expected to be consistent.
// </AMD64-specific>
//
// For more information, refer to vctools\crt\crtw32\misc\{ia64|amd64}\chandler.c for __C_specific_handler() and
// nt\base\ntos\rtl\{ia64|amd64}\exdsptch.c for RtlUnwindEx().
void FixupDispatcherContext(DISPATCHER_CONTEXT* pDispatcherContext, CONTEXT* pContext, LPVOID originalControlPC, PEXCEPTION_ROUTINE pUnwindPersonalityRoutine)
{
    if (pContext)
    {
        STRESS_LOG1(LF_EH, LL_INFO10, "FDC: pContext: %p\n", pContext);
        CopyOSContext(pDispatcherContext->ContextRecord, pContext);
    }

    pDispatcherContext->ControlPc             = (UINT_PTR) GetIP(pDispatcherContext->ContextRecord);

#if defined(_TARGET_ARM_) || defined(_TARGET_ARM64_)
    // Since this routine is used to fixup contexts for async exceptions,
    // clear the CONTEXT_UNWOUND_TO_CALL flag since, semantically, frames
    // where such exceptions have happened do not have callsites. On a similar
    // note, also clear out the ControlPcIsUnwound field. Post discussion with
    // AaronGi from the kernel team, it's safe for us to have both of these
    // cleared.
    //
    // The OS will pick this up with the rest of the DispatcherContext state
    // when it processes collided unwind and thus, when our managed personality
    // routine is invoked, ExceptionTracker::InitializeCrawlFrame will adjust
    // ControlPC correctly.
    pDispatcherContext->ContextRecord->ContextFlags &= ~CONTEXT_UNWOUND_TO_CALL;
    pDispatcherContext->ControlPcIsUnwound = FALSE;
    
    // Also, clear out the debug-registers flag so that when this context is used by the
    // OS, it does not end up setting bogus access breakpoints. The kernel team will also
    // be fixing it at their end, in their implementation of collided unwind.
    pDispatcherContext->ContextRecord->ContextFlags &= ~CONTEXT_DEBUG_REGISTERS;
    
#ifdef _TARGET_ARM_
    // But keep the architecture flag set (its part of CONTEXT_DEBUG_REGISTERS)
    pDispatcherContext->ContextRecord->ContextFlags |= CONTEXT_ARM;
#else // _TARGET_ARM64_
    // But keep the architecture flag set (its part of CONTEXT_DEBUG_REGISTERS)
    pDispatcherContext->ContextRecord->ContextFlags |= CONTEXT_ARM64;
#endif // _TARGET_ARM_

#endif // _TARGET_ARM_ || _TARGET_ARM64_

    INDEBUG(pDispatcherContext->FunctionEntry = (PT_RUNTIME_FUNCTION)INVALID_POINTER_CD);
    INDEBUG(pDispatcherContext->ImageBase     = INVALID_POINTER_CD);

    pDispatcherContext->FunctionEntry = RtlLookupFunctionEntry(pDispatcherContext->ControlPc,
                                                               &(pDispatcherContext->ImageBase),
                                                               NULL
                                                               );

    _ASSERTE(((PT_RUNTIME_FUNCTION)INVALID_POINTER_CD) != pDispatcherContext->FunctionEntry);
    _ASSERTE(INVALID_POINTER_CD != pDispatcherContext->ImageBase);

    //
    // need to find the establisher frame by virtually unwinding
    //
    CONTEXT tempContext;
    PVOID   HandlerData;
    
    CopyOSContext(&tempContext, pDispatcherContext->ContextRecord);
    
    // RtlVirtualUnwind returns the language specific handler for the ControlPC in question
    // on ARM and AMD64.
    pDispatcherContext->LanguageHandler = RtlVirtualUnwind(
                     NULL,     // HandlerType
                     pDispatcherContext->ImageBase,
                     pDispatcherContext->ControlPc,
                     pDispatcherContext->FunctionEntry,
                     &tempContext,
                     &HandlerData,
                     &(pDispatcherContext->EstablisherFrame),
                     NULL);

    pDispatcherContext->HandlerData     = NULL;
    pDispatcherContext->HistoryTable    = NULL;
    

    // Why does the OS consider it invalid to have a NULL personality routine (or, why does
    // the OS assume that DispatcherContext returned from ExceptionCollidedUnwind will always
    // have a valid personality routine)?
    // 
    // 
    // We force the OS to pickup the DispatcherContext (that we fixed above) by returning
    // ExceptionCollidedUnwind. Per Dave Cutler, the only entity which is allowed to return
    // this exception disposition is the personality routine of the assembly helper which is used
    // to invoke the user (stack-based) personality routines. For such invocations made by the
    // OS assembly helper, the DispatcherContext it saves before invoking the user personality routine
    // will always have a valid personality routine reference and thus, when a real collided unwind happens
    // and this exception disposition is returned, OS exception dispatch will have a valid personality routine
    // to invoke.
    // 
    // By using this exception disposition to make the OS walk stacks we broke (for async exceptions), we are
    // simply abusing the semantic of this disposition. However, since we must use it, we should also check
    // that we are returning a valid personality routine reference back to the OS.
    if(pDispatcherContext->LanguageHandler == NULL)
    {
        if (pUnwindPersonalityRoutine != NULL)
        {
            pDispatcherContext->LanguageHandler = pUnwindPersonalityRoutine;
        }
        else
        {
            // We would be here only for fixing up context for an async exception in managed code.
            // This implies that we should have got a personality routine returned from the call to
            // RtlVirtualUnwind above.
            // 
            // However, if the ControlPC happened to be in the prolog or epilog of a managed method, 
            // then RtlVirtualUnwind will always return NULL. We cannot return this NULL back to the 
            // OS as it is an invalid value which the OS does not expect (and attempting to do so will
            // result in the kernel exception dispatch going haywire).
#if defined(_DEBUG)        
            // We should be in jitted code
            TADDR adrRedirectedIP = PCODEToPINSTR(pDispatcherContext->ControlPc);
            _ASSERTE(ExecutionManager::IsManagedCode(adrRedirectedIP));
#endif // _DEBUG

            // Set the personality routine to be returned as the one which is conventionally
            // invoked for exception dispatch.
            pDispatcherContext->LanguageHandler = (PEXCEPTION_ROUTINE)GetEEFuncEntryPoint(ProcessCLRException);
            STRESS_LOG1(LF_EH, LL_INFO10, "FDC: ControlPC was in prolog/epilog, so setting DC->LanguageHandler to %p\n", pDispatcherContext->LanguageHandler);
        }
    }
    
    _ASSERTE(pDispatcherContext->LanguageHandler != NULL);
}


// See the comment above for the overloaded version of this function.
void FixupDispatcherContext(DISPATCHER_CONTEXT* pDispatcherContext, CONTEXT* pContext, CONTEXT* pOriginalContext, PEXCEPTION_ROUTINE pUnwindPersonalityRoutine = NULL)
{
    _ASSERTE(pOriginalContext != NULL);
    FixupDispatcherContext(pDispatcherContext, pContext, (LPVOID)::GetIP(pOriginalContext), pUnwindPersonalityRoutine);
}


BOOL FirstCallToHandler (
        DISPATCHER_CONTEXT *pDispatcherContext,
        CONTEXT **ppContextRecord)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    FaultingExceptionFrame *pFrame = GetFrameFromRedirectedStubStackFrame(pDispatcherContext);

    BOOL *pfFilterExecuted = pFrame->GetFilterExecutedFlag();
    BOOL fFilterExecuted   = *pfFilterExecuted;

    STRESS_LOG4(LF_EH, LL_INFO10, "FirstCallToHandler: Fixing exception context for redirect stub, sp %p, establisher %p, flag %p -> %u\n",
            GetSP(pDispatcherContext->ContextRecord),
            pDispatcherContext->EstablisherFrame,
            pfFilterExecuted,
            fFilterExecuted);

    *ppContextRecord  = pFrame->GetExceptionContext();
    *pfFilterExecuted = TRUE;

    return !fFilterExecuted;
}


EXTERN_C EXCEPTION_DISPOSITION
HijackHandler(IN     PEXCEPTION_RECORD   pExceptionRecord
    WIN64_ARG(IN     ULONG64             MemoryStackFp)
NOT_WIN64_ARG(IN     ULONG               MemoryStackFp),
              IN OUT PCONTEXT            pContextRecord,
              IN OUT PDISPATCHER_CONTEXT pDispatcherContext
             )
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        MODE_ANY;
    }
    CONTRACTL_END;

    STRESS_LOG4(LF_EH, LL_INFO10, "HijackHandler: establisher: %p, disp->cxr: %p, sp %p, cxr @ exception: %p\n",
        pDispatcherContext->EstablisherFrame,
        pDispatcherContext->ContextRecord,
        GetSP(pDispatcherContext->ContextRecord),
        pContextRecord);

    Thread* pThread = GetThread();
    CONTEXT *pNewContext = NULL;

    if (FirstCallToHandler(pDispatcherContext, &pNewContext))
    {
        //
        // We've pushed a Frame, but it is not initialized yet, so we
        // must not be in preemptive mode
        //
        CONSISTENCY_CHECK(pThread->PreemptiveGCDisabled());

        //
        // AdjustContextForThreadStop will reset the ThrowControlForThread state
        // on the thread, but we don't want to do that just yet.  We need that
        // information in our personality routine, so we will reset it back to
        // InducedThreadStop and then clear it in our personality routine.
        //
        CONSISTENCY_CHECK(IsThreadHijackedForThreadStop(pThread, pExceptionRecord));
        AdjustContextForThreadStop(pThread, pNewContext);
        pThread->SetThrowControlForThread(Thread::InducedThreadStop);
    }

    FixupDispatcherContext(pDispatcherContext, pNewContext, pContextRecord);

    STRESS_LOG4(LF_EH, LL_INFO10, "HijackHandler: new establisher: %p, disp->cxr: %p, new ip: %p, new sp: %p\n",
        pDispatcherContext->EstablisherFrame,
        pDispatcherContext->ContextRecord,
        GetIP(pDispatcherContext->ContextRecord),
        GetSP(pDispatcherContext->ContextRecord));

    // Returning ExceptionCollidedUnwind will cause the OS to take our new context record
    // and dispatcher context and restart the exception dispatching on this call frame,
    // which is exactly the behavior we want in order to restore our thread's unwindability
    // (which was broken when we whacked the IP to get control over the thread)
    return ExceptionCollidedUnwind;
}


EXTERN_C VOID FixContextForFaultingExceptionFrame (
        EXCEPTION_RECORD* pExceptionRecord,
        CONTEXT *pContextRecord);

EXTERN_C EXCEPTION_DISPOSITION
FixContextHandler(IN     PEXCEPTION_RECORD   pExceptionRecord
        WIN64_ARG(IN     ULONG64             MemoryStackFp)
    NOT_WIN64_ARG(IN     ULONG               MemoryStackFp),
                  IN OUT PCONTEXT            pContextRecord,
                  IN OUT PDISPATCHER_CONTEXT pDispatcherContext
                 )
{
    CONTEXT* pNewContext = NULL;

    if (FirstCallToHandler(pDispatcherContext, &pNewContext))
    {
        //
        // We've pushed a Frame, but it is not initialized yet, so we
        // must not be in preemptive mode
        //
        CONSISTENCY_CHECK(GetThread()->PreemptiveGCDisabled());

        FixContextForFaultingExceptionFrame(pExceptionRecord, pNewContext);
    }

    FixupDispatcherContext(pDispatcherContext, pNewContext, pContextRecord);

    // Returning ExceptionCollidedUnwind will cause the OS to take our new context record
    // and dispatcher context and restart the exception dispatching on this call frame,
    // which is exactly the behavior we want in order to restore our thread's unwindability
    // (which was broken when we whacked the IP to get control over the thread)
    return ExceptionCollidedUnwind;
}
#endif // !FEATURE_PAL

#ifdef _DEBUG
// IsSafeToUnwindFrameChain:
// Arguments:
//   pThread  the Thread* being unwound
//   MemoryStackFpForFrameChain  the stack limit to unwind the Frames
// Returns
//   FALSE  if the value MemoryStackFpForFrameChain falls between a M2U transition frame
//          and its corresponding managed method stack pointer
//   TRUE   otherwise.
//
// If the managed method will *NOT* be unwound by the current exception
// pass we have an error: with no Frame on the stack to report it, the
// managed method will not be included in the next stack walk.
// An example of running into this issue was DDBug 1133, where
// TransparentProxyStubIA64 had a personality routine that removed a
// transition frame.  As a consequence the managed method did not
// participate in the stack walk until the exception handler was called.  At
// that time the stack walking code was able to see the managed method again
// but by this time all references from this managed method were stale.
BOOL IsSafeToUnwindFrameChain(Thread* pThread, LPVOID MemoryStackFpForFrameChain)
{
    // Look for the last Frame to be removed that marks a managed-to-unmanaged transition
    Frame* pLastFrameOfInterest = FRAME_TOP;
    for (Frame* pf = pThread->m_pFrame; pf < MemoryStackFpForFrameChain; pf = pf->PtrNextFrame())
    {
        PCODE retAddr = pf->GetReturnAddress();
        if (retAddr != NULL && ExecutionManager::IsManagedCode(retAddr))
        {
            pLastFrameOfInterest = pf;
        }
    }

    // If there is none it's safe to remove all these Frames
    if (pLastFrameOfInterest == FRAME_TOP)
    {
        return TRUE;
    }

    // Otherwise "unwind" to managed method
    REGDISPLAY rd;
    CONTEXT ctx;
    SetIP(&ctx, 0);
    SetSP(&ctx, 0);
    FillRegDisplay(&rd, &ctx);
    pLastFrameOfInterest->UpdateRegDisplay(&rd);

    // We're safe only if the managed method will be unwound also
    LPVOID managedSP = dac_cast<PTR_VOID>(GetRegdisplaySP(&rd));

    if (managedSP < MemoryStackFpForFrameChain)
    {
        return TRUE;
    }
    else
    {
        return FALSE;
    }

}
#endif // _DEBUG


void CleanUpForSecondPass(Thread* pThread, bool fIsSO, LPVOID MemoryStackFpForFrameChain, LPVOID MemoryStackFp)
{
    WRAPPER_NO_CONTRACT;

    EH_LOG((LL_INFO100, "Exception is going into unmanaged code, unwinding frame chain to %p\n", MemoryStackFpForFrameChain));

    // On AMD64 the establisher pointer is the live stack pointer, but on
    // IA64 and ARM it's the caller's stack pointer.  It makes no difference, since there
    // is no Frame anywhere in CallDescrWorker's region of stack.

    // First make sure that unwinding the frame chain does not remove any transition frames
    // that report managed methods that will not be unwound.
    // If this assert fires it's probably the personality routine of some assembly code that
    // incorrectly removed a transition frame (more details in IsSafeToUnwindFrameChain)
    // [Do not perform the IsSafeToUnwindFrameChain() check in the SO case, since
    // IsSafeToUnwindFrameChain() requires a large amount of stack space.]
    _ASSERTE(fIsSO || IsSafeToUnwindFrameChain(pThread, (Frame*)MemoryStackFpForFrameChain));

    UnwindFrameChain(pThread, (Frame*)MemoryStackFpForFrameChain);

    // Only pop the trackers if this is not an SO.  It's not safe to pop the trackers during EH for an SO.
    // Instead, we rely on the END_SO_TOLERANT_CODE macro to call ClearExceptionStateAfterSO().  Of course,
    // we may leak in the UMThunkStubCommon() case where we don't have this macro lower on the stack
    // (stack grows up).
    if (!fIsSO)
    {
        ExceptionTracker::PopTrackerIfEscaping((void*)MemoryStackFp);
    }
}

#ifdef FEATURE_PAL

// This is a personality routine for TheUMEntryPrestub and UMThunkStub Unix asm stubs.
// An exception propagating through these stubs is an unhandled exception.
// This function dumps managed stack trace and terminates the current process.
EXTERN_C _Unwind_Reason_Code
UnhandledExceptionHandlerUnix(
                IN int version, 
                IN _Unwind_Action action, 
                IN uint64_t exceptionClass, 
                IN struct _Unwind_Exception *exception,
                IN struct _Unwind_Context *context            
              )
{
    // Unhandled exception happened, so dump the managed stack trace and terminate the process

    DefaultCatchHandler(NULL /*pExceptionInfo*/, NULL /*Throwable*/, TRUE /*useLastThrownObject*/,
        TRUE /*isTerminating*/, FALSE /*isThreadBaseFIlter*/, FALSE /*sendAppDomainEvents*/);
    
    EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
    return _URC_FATAL_PHASE1_ERROR;
}

#else // FEATURE_PAL

EXTERN_C EXCEPTION_DISPOSITION
UMThunkUnwindFrameChainHandler(IN     PEXCEPTION_RECORD   pExceptionRecord
                     WIN64_ARG(IN     ULONG64             MemoryStackFp)
                 NOT_WIN64_ARG(IN     ULONG               MemoryStackFp),
                               IN OUT PCONTEXT            pContextRecord,
                               IN OUT PDISPATCHER_CONTEXT pDispatcherContext
                              )
{
    Thread* pThread = GetThread();
    if (pThread == NULL) {
        return ExceptionContinueSearch;
    }

    bool fIsSO = pExceptionRecord->ExceptionCode == STATUS_STACK_OVERFLOW;

    if (IS_UNWINDING(pExceptionRecord->ExceptionFlags))
    {
        if (fIsSO)
        {
            if (!pThread->PreemptiveGCDisabled())
            {
                pThread->DisablePreemptiveGC();
            }
        }
        CleanUpForSecondPass(pThread, fIsSO, (void*)MemoryStackFp, (void*)MemoryStackFp);
    }

    // The asm stub put us into COOP mode, but we're about to scan unmanaged call frames
    // so unmanaged filters/handlers/etc can run and we must be in PREEMP mode for that.
    if (pThread->PreemptiveGCDisabled())
    {
        if (fIsSO)
        {
            // We don't have stack to do full-version EnablePreemptiveGC.
            FastInterlockAnd (&pThread->m_fPreemptiveGCDisabled, 0);
        }
        else
        {
            pThread->EnablePreemptiveGC();
        }
    }

    return ExceptionContinueSearch;
}

EXTERN_C EXCEPTION_DISPOSITION
UMEntryPrestubUnwindFrameChainHandler(
                IN     PEXCEPTION_RECORD   pExceptionRecord
      WIN64_ARG(IN     ULONG64             MemoryStackFp)
  NOT_WIN64_ARG(IN     ULONG               MemoryStackFp),
                IN OUT PCONTEXT            pContextRecord,
                IN OUT PDISPATCHER_CONTEXT pDispatcherContext
            )
{
    EXCEPTION_DISPOSITION disposition = UMThunkUnwindFrameChainHandler(
                pExceptionRecord,
                MemoryStackFp,
                pContextRecord,
                pDispatcherContext
                );

    return disposition;
}

EXTERN_C EXCEPTION_DISPOSITION
UMThunkStubUnwindFrameChainHandler(
              IN     PEXCEPTION_RECORD   pExceptionRecord
    WIN64_ARG(IN     ULONG64             MemoryStackFp)
NOT_WIN64_ARG(IN     ULONG               MemoryStackFp),
              IN OUT PCONTEXT            pContextRecord,
              IN OUT PDISPATCHER_CONTEXT pDispatcherContext
            )
{

#ifdef _DEBUG
    // If the exception is escaping the last CLR personality routine on the stack,
    // then state a flag on the thread to indicate so.
    //
    // We check for thread object since this function is the personality routine of the UMThunk
    // and we can landup here even when thread creation (within the thunk) fails.
    if (GetThread() != NULL)
    {
        SetReversePInvokeEscapingUnhandledExceptionStatus(IS_UNWINDING(pExceptionRecord->ExceptionFlags),
            MemoryStackFp
            );
    }
#endif // _DEBUG

    EXCEPTION_DISPOSITION disposition = UMThunkUnwindFrameChainHandler(
                pExceptionRecord,
                MemoryStackFp,
                pContextRecord,
                pDispatcherContext
                );

    return disposition;
}


// This is the personality routine setup for the assembly helper (CallDescrWorker) that calls into 
// managed code.
EXTERN_C EXCEPTION_DISPOSITION
CallDescrWorkerUnwindFrameChainHandler(IN     PEXCEPTION_RECORD   pExceptionRecord
                             WIN64_ARG(IN     ULONG64             MemoryStackFp)
                         NOT_WIN64_ARG(IN     ULONG               MemoryStackFp),
                                       IN OUT PCONTEXT            pContextRecord,
                                       IN OUT PDISPATCHER_CONTEXT pDispatcherContext
                                      )
{

    Thread* pThread = GetThread();
    _ASSERTE(pThread);

    if (pExceptionRecord->ExceptionCode == STATUS_STACK_OVERFLOW)
    {
        if (IS_UNWINDING(pExceptionRecord->ExceptionFlags))
        {
            GCX_COOP_NO_DTOR();
            CleanUpForSecondPass(pThread, true, (void*)MemoryStackFp, (void*)MemoryStackFp);
        }

        FastInterlockAnd (&pThread->m_fPreemptiveGCDisabled, 0);
        // We'll let the SO infrastructure handle this exception... at that point, we
        // know that we'll have enough stack to do it.
        return ExceptionContinueSearch;
    }

    EXCEPTION_DISPOSITION retVal = ProcessCLRException(pExceptionRecord,
                                                       MemoryStackFp,
                                                       pContextRecord,
                                                       pDispatcherContext);

    if (retVal == ExceptionContinueSearch)
    {

        if (IS_UNWINDING(pExceptionRecord->ExceptionFlags))
        {
            CleanUpForSecondPass(pThread, false, (void*)MemoryStackFp, (void*)MemoryStackFp);
        }

        // We're scanning out from CallDescr and potentially through the EE and out to unmanaged.
        // So switch to preemptive mode.
        GCX_PREEMP_NO_DTOR();
    }

    return retVal;
}

#endif // FEATURE_PAL

#ifdef FEATURE_COMINTEROP
EXTERN_C EXCEPTION_DISPOSITION
ReverseComUnwindFrameChainHandler(IN     PEXCEPTION_RECORD   pExceptionRecord
                        WIN64_ARG(IN     ULONG64             MemoryStackFp)
                    NOT_WIN64_ARG(IN     ULONG               MemoryStackFp),
                                  IN OUT PCONTEXT            pContextRecord,
                                  IN OUT PDISPATCHER_CONTEXT pDispatcherContext
                                 )
{
    if (IS_UNWINDING(pExceptionRecord->ExceptionFlags))
    {
        ComMethodFrame::DoSecondPassHandlerCleanup(GetThread()->GetFrame());
    }
    return ExceptionContinueSearch;
}
#endif // FEATURE_COMINTEROP

#ifndef FEATURE_PAL
EXTERN_C EXCEPTION_DISPOSITION
FixRedirectContextHandler(
                  IN     PEXCEPTION_RECORD   pExceptionRecord
        WIN64_ARG(IN     ULONG64             MemoryStackFp)
    NOT_WIN64_ARG(IN     ULONG               MemoryStackFp),
                  IN OUT PCONTEXT            pContextRecord,
                  IN OUT PDISPATCHER_CONTEXT pDispatcherContext
                 )
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        NOTHROW;
        MODE_ANY;
    }
    CONTRACTL_END;

    STRESS_LOG4(LF_EH, LL_INFO10, "FixRedirectContextHandler: sp %p, establisher %p, cxr: %p, disp cxr: %p\n",
        GetSP(pDispatcherContext->ContextRecord),
        pDispatcherContext->EstablisherFrame,
        pContextRecord,
        pDispatcherContext->ContextRecord);

    CONTEXT *pRedirectedContext = GetCONTEXTFromRedirectedStubStackFrame(pDispatcherContext);

    FixupDispatcherContext(pDispatcherContext, pRedirectedContext, pContextRecord);

    // Returning ExceptionCollidedUnwind will cause the OS to take our new context record
    // and dispatcher context and restart the exception dispatching on this call frame,
    // which is exactly the behavior we want in order to restore our thread's unwindability
    // (which was broken when we whacked the IP to get control over the thread)
    return ExceptionCollidedUnwind;
}
#endif // !FEATURE_PAL
#endif // DACCESS_COMPILE

void ExceptionTracker::StackRange::Reset()
{
    LIMITED_METHOD_CONTRACT;

    m_sfLowBound.SetMaxVal();
    m_sfHighBound.Clear();
}

bool ExceptionTracker::StackRange::IsEmpty()
{
    LIMITED_METHOD_CONTRACT;
    return (m_sfLowBound.IsMaxVal() &&
            m_sfHighBound.IsNull());
}

bool ExceptionTracker::StackRange::IsSupersededBy(StackFrame sf)
{
    LIMITED_METHOD_CONTRACT;
    CONSISTENCY_CHECK(IsConsistent());

    return (sf >= m_sfLowBound);
}

void ExceptionTracker::StackRange::CombineWith(StackFrame sfCurrent, StackRange* pPreviousRange)
{
    LIMITED_METHOD_CONTRACT;

    if ((pPreviousRange->m_sfHighBound < sfCurrent) && IsEmpty())
    {
        // This case comes from an unusual situation.  It is possible for a new nested tracker to start its
        // first pass at a higher SP than any previously scanned frame in the previous "enclosing" tracker.
        // Typically this doesn't happen because the ProcessCLRException callback is made multiple times for
        // the frame where the nesting first occurs and that will ensure that the stack range of the new
        // nested exception is extended to contain the scan range of the previous tracker's scan.  However,
        // if the exception dispatch calls a C++ handler (e.g. a finally) and then that handler tries to 
        // reverse-pinvoke into the runtime, AND we trigger an exception (e.g. ThreadAbort) 
        // before we reach another managed frame (which would have the CLR personality
        // routine associated with it), the first callback to ProcessCLRException for this new exception
        // will occur on a frame that has never been seen before by the current tracker.
        //
        // So in this case, we'll see a sfCurrent that is larger than the previous tracker's high bound and
        // we'll have an empty scan range for the current tracker.  And we'll just need to pre-init the 
        // scanned stack range for the new tracker to the previous tracker's range.  This maintains the 
        // invariant that the scanned range for nested trackers completely cover the scanned range of their
        // previous tracker once they "escape" the previous tracker.
        STRESS_LOG3(LF_EH, LL_INFO100, 
            "Initializing current StackRange with previous tracker's StackRange.  sfCurrent: %p, prev low: %p, prev high: %p\n",
            sfCurrent.SP, pPreviousRange->m_sfLowBound.SP, pPreviousRange->m_sfHighBound.SP);

        *this = *pPreviousRange;
    }
    else
    {
#ifdef FEATURE_PAL
        // When the current range is empty, copy the low bound too. Otherwise a degenerate range would get
        // created and tests for stack frame in the stack range would always fail.
        // TODO: Check if we could enable it for non-PAL as well.
        if (IsEmpty())
        {
            m_sfLowBound = pPreviousRange->m_sfLowBound;
        }
#endif // FEATURE_PAL
        m_sfHighBound = pPreviousRange->m_sfHighBound;
    }
}

bool ExceptionTracker::StackRange::Contains(StackFrame sf)
{
    LIMITED_METHOD_CONTRACT;
    CONSISTENCY_CHECK(IsConsistent());

    return ((m_sfLowBound <= sf) &&
                            (sf <= m_sfHighBound));
}

void ExceptionTracker::StackRange::ExtendUpperBound(StackFrame sf)
{
    LIMITED_METHOD_CONTRACT;
    CONSISTENCY_CHECK(IsConsistent());
    CONSISTENCY_CHECK(sf > m_sfHighBound);

    m_sfHighBound = sf;
}

void ExceptionTracker::StackRange::ExtendLowerBound(StackFrame sf)
{
    LIMITED_METHOD_CONTRACT;
    CONSISTENCY_CHECK(IsConsistent());
    CONSISTENCY_CHECK(sf < m_sfLowBound);

    m_sfLowBound = sf;
}

void ExceptionTracker::StackRange::TrimLowerBound(StackFrame sf)
{
    LIMITED_METHOD_CONTRACT;
    CONSISTENCY_CHECK(IsConsistent());
    CONSISTENCY_CHECK(sf >= m_sfLowBound);

    m_sfLowBound = sf;
}

StackFrame ExceptionTracker::StackRange::GetLowerBound()
{
    LIMITED_METHOD_CONTRACT;
    CONSISTENCY_CHECK(IsConsistent());

    return m_sfLowBound;
}

StackFrame ExceptionTracker::StackRange::GetUpperBound()
{
    LIMITED_METHOD_CONTRACT;
    CONSISTENCY_CHECK(IsConsistent());

    return m_sfHighBound;
}

#ifdef _DEBUG
bool ExceptionTracker::StackRange::IsDisjointWithAndLowerThan(StackRange* pOtherRange)
{
    CONSISTENCY_CHECK(IsConsistent());
    CONSISTENCY_CHECK(pOtherRange->IsConsistent());

    return m_sfHighBound < pOtherRange->m_sfLowBound;
}

#endif // _DEBUG


#ifdef _DEBUG
bool ExceptionTracker::StackRange::IsConsistent()
{
    LIMITED_METHOD_CONTRACT;
    if (m_sfLowBound.IsMaxVal() ||
        m_sfHighBound.IsNull())
    {
        return true;
    }

    if (m_sfLowBound <= m_sfHighBound)
    {
        return true;
    }

    LOG((LF_EH, LL_ERROR, "sp: low: %p high: %p\n", m_sfLowBound.SP, m_sfHighBound.SP));

    return false;
}
#endif // _DEBUG

// Determine if the given StackFrame is in the stack region unwound by the specified ExceptionTracker.
// This is used by the stackwalker to skip funclets.  Refer to the calls to this method in StackWalkFramesEx()
// for more information.
//
// Effectively, this will make the stackwalker skip all the frames until it reaches the frame
// containing the funclet. Details of the skipping logic are described in the method implementation.
//
// static
bool ExceptionTracker::IsInStackRegionUnwoundBySpecifiedException(CrawlFrame * pCF, PTR_ExceptionTracker pExceptionTracker)
{
     LIMITED_METHOD_CONTRACT;

    _ASSERTE(pCF != NULL);
    
    // The tracker must be in the second pass, and its stack range must not be empty.
    if ( (pExceptionTracker == NULL) ||
         pExceptionTracker->IsInFirstPass() ||
         pExceptionTracker->m_ScannedStackRange.IsEmpty())
    {
        return false;
    }

    CallerStackFrame csfToCheck;
    if (pCF->IsFrameless())
    {
        csfToCheck = CallerStackFrame::FromRegDisplay(pCF->GetRegisterSet());
    }
    else
    {
        csfToCheck = CallerStackFrame((UINT_PTR)pCF->GetFrame());
    }

    StackFrame sfLowerBound = pExceptionTracker->m_ScannedStackRange.GetLowerBound();
    StackFrame sfUpperBound = pExceptionTracker->m_ScannedStackRange.GetUpperBound();

    //
    // Let's take an example callstack that grows from left->right:
    //
    // M5 (50) -> M4 (40) -> M3 (30) -> M2 (20) -> M1 (10) ->throw
    //
    // These are all managed frames, where M1 throws and the exception is caught
    // in M4. The numbers in the brackets are the values of the stack pointer after
    // the prolog is executed (or, in case of dynamic allocation, its SP after
    // dynamic allocation) and will be the SP at the time the callee function
    // is invoked.
    //
    // When the stackwalker is asked to skip funclets during the stackwalk,
    // it will skip all the frames on the stack until it reaches the frame
    // containing the funclet after it has identified the funclet from
    // which the skipping of frames needs to commence.
    //
    // At such a point, the exception tracker's scanned stack range's 
    // lowerbound will correspond to the frame that had the exception
    // and the upper bound will correspond to the frame that had the funclet.
    // For scenarios like security stackwalk that may be triggered out of a
    // funclet (e.g. a catch block), skipping funclets and frames in this fashion
    // is expected to lead us to the parent frame containing the funclet as it
    // will contain an object of interest (e.g. security descriptor).
    //
    // The check below ensures that we skip the frames from the one that
    // had exception to the one that is the callee of the method containing
    // the funclet of interest. In the example above, this would mean skipping
    // from M1 to M3.
    //
    // We use CallerSP of a given CrawlFrame to perform such a skip. On AMD64,
    // the first frame where CallerSP will be greater than SP of the frame
    // itself will be when we reach the lowest frame itself (i.e. M1). On a similar
    // note, the only time when CallerSP of a given CrawlFrame will be equal to the
    // upper bound is when we reach the callee of the frame containing the funclet.
    // Thus, our check for the skip range is done by the following clause:
    //
    // if ((sfLowerBound < csfToCheck) && (csfToCheck <= sfUpperBound))
    //
    // On ARM and ARM64, while the lower and upper bounds are populated using the Establisher
    // frame given by the OS during exception dispatch, they actually correspond to the
    // SP of the caller of a given frame, instead of being the SP of the given frame.
    // Thus, in the example, we will have lowerBound as 20 (corresponding to M1) and 
    // upperBound as 50 (corresponding to M4 which contains the catch funclet).
    //
    // Thus, to skip frames on ARM and ARM64 until we reach the frame containing funclet of
    // interest, the skipping will done by the following clause:
    //
    // if ((sfLowerBound <= csfToCheck) && (csfToCheck < sfUpperBound))
    //
    // The first time when CallerSP of a given CrawlFrame will be the same as lowerBound
    // is when we will reach the first frame to be skipped. Likewise, last frame whose
    // CallerSP will be less than the upperBound will be the callee of the frame
    // containing the funclet. When CallerSP is equal to the upperBound, we have reached
    // the frame containing the funclet and DO NOT want to skip it. Hence, "<"
    // in the 2nd part of the clause.

    // Remember that sfLowerBound and sfUpperBound are in the "OS format".
    // Refer to the comment for CallerStackFrame for more information.
#ifndef STACK_RANGE_BOUNDS_ARE_CALLER_SP
    if ((sfLowerBound < csfToCheck) && (csfToCheck <= sfUpperBound))
#else // !STACK_RANGE_BOUNDS_ARE_CALLER_SP
    if ((sfLowerBound <= csfToCheck) && (csfToCheck < sfUpperBound))
#endif // STACK_RANGE_BOUNDS_ARE_CALLER_SP
    {
        return true;
    }
    else
    {
        return false;
    }
}

// Returns a bool indicating if the specified CrawlFrame has been unwound by the active exception.
bool ExceptionTracker::IsInStackRegionUnwoundByCurrentException(CrawlFrame * pCF)
{
    LIMITED_METHOD_CONTRACT;

    Thread * pThread = pCF->pThread;
    PTR_ExceptionTracker pCurrentTracker = pThread->GetExceptionState()->GetCurrentExceptionTracker();
    return ExceptionTracker::IsInStackRegionUnwoundBySpecifiedException(pCF, pCurrentTracker);
}



// Returns a bool indicating if the specified CrawlFrame has been unwound by any active (e.g. nested) exceptions.
// 
// This method uses various fields of the ExceptionTracker data structure to do its work. Since this code runs on the thread
// performing the GC stackwalk, it must be ensured that these fields are not updated on another thread in parallel. Thus,
// any access to the fields in question that may result in updating them should happen in COOP mode. This provides a high-level
// synchronization with the GC thread since when GC stackwalk is active, attempt to enter COOP mode will result in the thread blocking
// and thus, attempts to update such fields will be synchronized.
//
// Currently, the following fields are used below:
//
// m_ExceptionFlags, m_ScannedStackRange, m_sfCurrentEstablisherFrame, m_sfLastUnwoundEstablisherFrame, 
// m_pInitialExplicitFrame, m_pLimitFrame, m_pPrevNestedInfo.
//
bool ExceptionTracker::HasFrameBeenUnwoundByAnyActiveException(CrawlFrame * pCF)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(pCF != NULL);

    // Enumerate all (nested) exception trackers and see if any of them has unwound the
    // specified CrawlFrame.
    Thread * pTargetThread = pCF->pThread;
    PTR_ExceptionTracker pTopTracker = pTargetThread->GetExceptionState()->GetCurrentExceptionTracker();
    PTR_ExceptionTracker pCurrentTracker = pTopTracker;
    
    bool fHasFrameBeenUnwound = false;

    while (pCurrentTracker != NULL)
    {
        bool fSkipCurrentTracker = false;

        // The tracker must be in the second pass, and its stack range must not be empty.
        if (pCurrentTracker->IsInFirstPass() ||
            pCurrentTracker->m_ScannedStackRange.IsEmpty())
        {
            fSkipCurrentTracker = true;
        }

        if (!fSkipCurrentTracker)
        {
            CallerStackFrame csfToCheck;
            bool fFrameless = false;
            if (pCF->IsFrameless())
            {
                csfToCheck = CallerStackFrame::FromRegDisplay(pCF->GetRegisterSet());
                fFrameless = true;
            }
            else
            {
                csfToCheck = CallerStackFrame((UINT_PTR)pCF->GetFrame());
            }

            STRESS_LOG4(LF_EH|LF_GCROOTS, LL_INFO100, "CrawlFrame (%p): Frameless: %s %s: %p\n",
                        pCF, fFrameless ? "Yes" : "No", fFrameless ? "CallerSP" : "Address", csfToCheck.SP);

            StackFrame sfLowerBound = pCurrentTracker->m_ScannedStackRange.GetLowerBound();
            StackFrame sfUpperBound = pCurrentTracker->m_ScannedStackRange.GetUpperBound();
            StackFrame sfCurrentEstablisherFrame = pCurrentTracker->GetCurrentEstablisherFrame();
            StackFrame sfLastUnwoundEstablisherFrame = pCurrentTracker->GetLastUnwoundEstablisherFrame();

            STRESS_LOG4(LF_EH|LF_GCROOTS, LL_INFO100, "LowerBound/UpperBound/CurrentEstablisherFrame/LastUnwoundManagedFrame: %p/%p/%p/%p\n",
                        sfLowerBound.SP, sfUpperBound.SP, sfCurrentEstablisherFrame.SP, sfLastUnwoundEstablisherFrame.SP);

            // Refer to the detailed comment in ExceptionTracker::IsInStackRegionUnwoundBySpecifiedException on the nature
            // of this check.
            //
#ifndef STACK_RANGE_BOUNDS_ARE_CALLER_SP
            if ((sfLowerBound < csfToCheck) && (csfToCheck <= sfUpperBound))
#else // !STACK_RANGE_BOUNDS_ARE_CALLER_SP
            if ((sfLowerBound <= csfToCheck) && (csfToCheck < sfUpperBound))
#endif // STACK_RANGE_BOUNDS_ARE_CALLER_SP
            {
                fHasFrameBeenUnwound = true;
                break;
            }

            //
            // The frame in question was not found to be covered by the scanned stack range of the exception tracker.
            // If the frame is managed, then it is possible that it forms the upper bound of the scanned stack range.
            // 
            // The scanned stack range is updated by our personality routine once ExceptionTracker::ProcessOSExceptionNotification is invoked.
            // However, it is possible that we have unwound a frame and returned back to the OS (in preemptive mode) and:
            //
            // 1) Either our personality routine has been invoked for the subsequent upstack managed frame but it has not yet got a chance to update
            //     the scanned stack range, OR
            // 2) We have simply returned to the kernel exception dispatch and yet to be invoked for a subsequent frame.
            //
            // In such a window, if we have been asked to check if the frame forming the upper bound of the scanned stack range has been unwound, or not,
            // then do the needful validations. 
            //
            // This is applicable to managed frames only.
            if (fFrameless)
            {
#ifndef STACK_RANGE_BOUNDS_ARE_CALLER_SP
                // On X64, if the SP of the managed frame indicates that the frame is forming the upper bound,
                // then:
                //
                // For case (1) above, sfCurrentEstablisherFrame will be the same as the callerSP of the managed frame.
                // For case (2) above, sfLastUnwoundEstablisherFrame would be the same as the managed frame's SP (or upper bound)
                //
                // For these scenarios, the frame is considered unwound.

                // For most cases which satisfy above condition GetRegdisplaySP(pCF->GetRegisterSet()) will be equal to sfUpperBound.SP. 
                // However, frames where Sp is modified after prolog ( eg. localloc) this might not be the case. For those scenarios,
                // we need to check if sfUpperBound.SP is in between GetRegdisplaySP(pCF->GetRegisterSet()) & callerSp.
                if (GetRegdisplaySP(pCF->GetRegisterSet()) <= sfUpperBound.SP && sfUpperBound < csfToCheck)
                {
                    if (csfToCheck == sfCurrentEstablisherFrame)
                    {
                        fHasFrameBeenUnwound = true;
                        break;
                    }
                    else if (sfUpperBound == sfLastUnwoundEstablisherFrame)
                    {
                        fHasFrameBeenUnwound = true;
                        break;
                    }
                }
#else // !STACK_RANGE_BOUNDS_ARE_CALLER_SP
                // On ARM, if the callerSP of the managed frame is the same as upper bound, then:
                // 
                // For case (1), sfCurrentEstablisherFrame will be above the callerSP of the managed frame (since EstablisherFrame is the caller SP for a given frame on ARM)
                // For case (2), upper bound will be the same as LastUnwoundEstablisherFrame.
                //
                // For these scenarios, the frame is considered unwound.
                if (sfUpperBound == csfToCheck)
                {
                    if (csfToCheck < sfCurrentEstablisherFrame)
                    {
                        fHasFrameBeenUnwound = true;
                        break;
                    }
                    else if (sfLastUnwoundEstablisherFrame == sfUpperBound)
                    {
                        fHasFrameBeenUnwound = true;
                        break;
                    }
                }
#endif // STACK_RANGE_BOUNDS_ARE_CALLER_SP
            }

            // The frame in question does not appear in the current tracker's scanned stack range (of managed frames).
            // If the frame is an explicit frame, then check if it equal to (or greater) than the initial explicit frame
            // of the tracker. We can do this equality comparison because explicit frames are stack allocated.
            //
            // Do keep in mind that InitialExplicitFrame is only set in the 2nd (unwind) pass, which works
            // fine for the purpose of this method since it operates on exception trackers in the second pass only.
            if (!fFrameless)
            {
                PTR_Frame pInitialExplicitFrame = pCurrentTracker->GetInitialExplicitFrame();
                PTR_Frame pLimitFrame = pCurrentTracker->GetLimitFrame();

#if !defined(DACCESS_COMPILE)                
                STRESS_LOG2(LF_EH|LF_GCROOTS, LL_INFO100, "InitialExplicitFrame: %p, LimitFrame: %p\n", pInitialExplicitFrame, pLimitFrame);
#endif // !defined(DACCESS_COMPILE)

                // Ideally, we would like to perform a comparison check to determine if the
                // frame has been unwound. This, however, is based upon the premise that
                // each explicit frame that is added to the frame chain is at a lower
                // address than this predecessor. 
                //
                // This works for frames across function calls but if we have multiple
                // explicit frames in the same function, then the compiler is free to
                // assign an address it deems fit. Thus, its totally possible for a
                // frame at the head of the frame chain to be at a higher address than
                // its predecessor. This has been observed to be true with VC++ compiler
                // in the CLR ret build.
                //
                // To address this, we loop starting from the InitialExplicitFrame until we reach
                // the LimitFrame. Since all frames starting from the InitialExplicitFrame, and prior 
                // to the LimitFrame, have been unwound, we break out of the loop if we find
                // the frame we are looking for, setting a flag indicating that the frame in question
                // was unwound.
                
                /*if ((sfInitialExplicitFrame <= csfToCheck) && (csfToCheck < sfLimitFrame))
                {
                    // The explicit frame falls in the range of explicit frames unwound by this tracker.
                    fHasFrameBeenUnwound = true;
                    break;
                }*/

                // The pInitialExplicitFrame can be NULL on Unix right after we've unwound a sequence
                // of native frames in the second pass of exception unwinding, since the pInitialExplicitFrame
                // is cleared to make sure that it doesn't point to a frame that was destroyed during the 
                // native frames unwinding. At that point, the csfToCheck could not have been unwound, 
                // so we don't need to do any check.
                if (pInitialExplicitFrame != NULL)
                {
                    PTR_Frame pFrameToCheck = (PTR_Frame)csfToCheck.SP;
                    PTR_Frame pCurrentFrame = pInitialExplicitFrame;
                    
                    {
                        while((pCurrentFrame != FRAME_TOP) && (pCurrentFrame != pLimitFrame))
                        {
                            if (pCurrentFrame == pFrameToCheck)
                            {
                                fHasFrameBeenUnwound = true;
                                break;
                            }
                        
                            pCurrentFrame = pCurrentFrame->PtrNextFrame();
                        }
                    }
                    
                    if (fHasFrameBeenUnwound == true)
                    {
                        break;
                    }
                }
            }
        }

        // Move to the next (previous) tracker
        pCurrentTracker = pCurrentTracker->GetPreviousExceptionTracker();
    }

    if (fHasFrameBeenUnwound)
        STRESS_LOG0(LF_EH|LF_GCROOTS, LL_INFO100, "Has already been unwound\n");

    return fHasFrameBeenUnwound;
}

//---------------------------------------------------------------------------------------
//
// Given the CrawlFrame of the current frame, return a StackFrame representing the current frame.
// This StackFrame should only be used in a check to see if the current frame is the parent method frame
// of a particular funclet.  Don't use the returned StackFrame in any other way except to pass it back to
// ExceptionTracker::IsUnwoundToTargetParentFrame().  The comparison logic is very platform-dependent.
//
// Arguments:
//    pCF - the CrawlFrame for the current frame
//
// Return Value:
//    Return a StackFrame for parent frame check
//
// Notes:
//    Don't use the returned StackFrame in any other way.
//

//static
StackFrame ExceptionTracker::GetStackFrameForParentCheck(CrawlFrame * pCF)
{
    WRAPPER_NO_CONTRACT;

    StackFrame sfResult;

    // Returns the CrawlFrame's caller's SP - this is used to determine if we have
    // reached the intended CrawlFrame in question (or not).

    // sfParent is returned by the EH subsystem, which uses the OS format, i.e. the initial SP before
    // any dynamic stack allocation.  The stackwalker uses the current SP, i.e. the SP after all
    // dynamic stack allocations.  Thus, we cannot do an equality check.  Instead, we get the
    // CallerStackFrame, which is the caller SP.
    sfResult = (StackFrame)CallerStackFrame::FromRegDisplay(pCF->GetRegisterSet());

    return sfResult;
}

//---------------------------------------------------------------------------------------
//
// Given the StackFrame of a parent method frame, determine if we have unwound to it during stackwalking yet.
// The StackFrame should be the return value of one of the FindParentStackFrameFor*() functions.
// Refer to the comment for UnwindStackFrame for more information.
//
// Arguments:
//    pCF       - the CrawlFrame of the current frame
//    sfParent  - the StackFrame of the target parent method frame,
//                returned by one of the FindParentStackFrameFor*() functions
//
// Return Value:
//    whether we have unwound to the target parent method frame
//

// static
bool ExceptionTracker::IsUnwoundToTargetParentFrame(CrawlFrame * pCF, StackFrame sfParent)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION( CheckPointer(pCF, NULL_NOT_OK) );
        PRECONDITION( pCF->IsFrameless() );
        PRECONDITION( pCF->GetRegisterSet()->IsCallerContextValid || pCF->GetRegisterSet()->IsCallerSPValid );
    }
    CONTRACTL_END;

    StackFrame sfToCheck = GetStackFrameForParentCheck(pCF);
    return IsUnwoundToTargetParentFrame(sfToCheck, sfParent);
}

// static
bool ExceptionTracker::IsUnwoundToTargetParentFrame(StackFrame sfToCheck, StackFrame sfParent)
{
    LIMITED_METHOD_CONTRACT;

    return (sfParent == sfToCheck);
}

// Given the CrawlFrame for a funclet frame, return the frame pointer of the enclosing funclet frame.
// For filter funclet frames and normal method frames, this function returns a NULL StackFrame.
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
//
// static
StackFrame ExceptionTracker::FindParentStackFrameForStackWalk(CrawlFrame* pCF, bool fForGCReporting /*= false */)
{
    WRAPPER_NO_CONTRACT;

    // We should never skip filter funclets. However, if we are stackwalking for GC reference
    // reporting, then we need to get the stackframe of the parent frame (where the filter was
    // invoked from) so that when we reach it, we can indicate that the filter has already
    // performed the reporting.
    // 
    // Thus, for GC reporting purposes, get filter's parent frame.
    if (pCF->IsFilterFunclet() && (!fForGCReporting))
    {
        return StackFrame();
    }
    else
    {
        return FindParentStackFrameHelper(pCF, NULL, NULL, NULL, fForGCReporting);
    }
}

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
//
// static
StackFrame ExceptionTracker::FindParentStackFrameEx(CrawlFrame* pCF,
                                                    DWORD*      pParentOffset,
                                                    UINT_PTR*   pParentCallerSP)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION( pCF != NULL );
        PRECONDITION( pCF->IsFilterFunclet() );
    }
    CONTRACTL_END;

    bool fRealParent = false;
    StackFrame sfResult = ExceptionTracker::FindParentStackFrameHelper(pCF, &fRealParent, pParentOffset, pParentCallerSP);

    if (fRealParent)
    {
        // If the enclosing method is the parent method, then we are done.
        return sfResult;
    }
    else
    {
        // Otherwise we need to do a full stackwalk to find the parent method frame.
        // This should only happen if we are calling a filter inside a funclet.
        return ExceptionTracker::RareFindParentStackFrame(pCF, pParentOffset, pParentCallerSP);
    }
}

// static
StackFrame ExceptionTracker::GetCallerSPOfParentOfNonExceptionallyInvokedFunclet(CrawlFrame *pCF)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(pCF != NULL);
        PRECONDITION(pCF->IsFunclet() && (!pCF->IsFilterFunclet()));
    }
    CONTRACTL_END;
    
    PREGDISPLAY pRD = pCF->GetRegisterSet();
    
    // Ensure that the caller Context is valid.
    _ASSERTE(pRD->IsCallerContextValid);
    
    // Make a copy of the caller context
    T_CONTEXT tempContext;
    CopyOSContext(&tempContext, pRD->pCallerContext);
    
    // Now unwind it to get the context of the caller's caller.
    EECodeInfo codeInfo(dac_cast<PCODE>(GetIP(pRD->pCallerContext)));
    Thread::VirtualUnwindCallFrame(&tempContext, NULL, &codeInfo);
    
    StackFrame sfRetVal = StackFrame((UINT_PTR)(GetSP(&tempContext)));
    _ASSERTE(!sfRetVal.IsNull() && !sfRetVal.IsMaxVal());
    
    return sfRetVal;
}

// static
StackFrame ExceptionTracker::FindParentStackFrameHelper(CrawlFrame* pCF,
                                                        bool*       pfRealParent,
                                                        DWORD*      pParentOffset,
                                                        UINT_PTR*   pParentCallerSP,
                                                        bool        fForGCReporting /* = false */)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION( pCF != NULL );
        PRECONDITION( pCF->IsFunclet() );
        PRECONDITION( CheckPointer(pfRealParent, NULL_OK) );
        PRECONDITION( CheckPointer(pParentOffset, NULL_OK) );
        PRECONDITION( CheckPointer(pParentCallerSP, NULL_OK) );
    }
    CONTRACTL_END;

    StackFrame sfResult;
    REGDISPLAY* pRegDisplay = pCF->GetRegisterSet();

    // At this point, we need a valid caller SP and the CallerStackFrame::FromRegDisplay
    // asserts that the RegDisplay contains one.
    CallerStackFrame csfCurrent = CallerStackFrame::FromRegDisplay(pRegDisplay);
    ExceptionTracker *pCurrentTracker = NULL;
    bool fIsFilterFunclet = pCF->IsFilterFunclet();

    // We can't do this on an unmanaged thread.
    Thread* pThread = pCF->pThread;
    if (pThread == NULL)
    {
        _ASSERTE(!"FindParentStackFrame() called on an unmanaged thread");
        goto lExit;
    }

    // Check for out-of-line finally funclets.  Filter funclets can't be out-of-line.
    if (!fIsFilterFunclet)
    {
        if (pRegDisplay->IsCallerContextValid)
        {
            PCODE callerIP = dac_cast<PCODE>(GetIP(pRegDisplay->pCallerContext));
            BOOL fIsCallerInVM = FALSE;

            // Check if the caller IP is in mscorwks.  If it is not, then it is an out-of-line finally.
            // Normally, the caller of a finally is ExceptionTracker::CallHandler().
#ifdef FEATURE_PAL
            fIsCallerInVM = !ExecutionManager::IsManagedCode(callerIP);
#else
#if defined(DACCESS_COMPILE)
            HMODULE_TGT hEE = DacGlobalBase();
#else  // !DACCESS_COMPILE
            HMODULE_TGT hEE = g_pMSCorEE;
#endif // !DACCESS_COMPILE
            fIsCallerInVM = IsIPInModule(hEE, callerIP);
#endif // FEATURE_PAL

            if (!fIsCallerInVM)
            {
                if (!fForGCReporting)
                {
                    sfResult.SetMaxVal();
                    goto lExit;
                }
                else
                {
                    // We have run into a non-exceptionally invoked finally funclet (aka out-of-line finally funclet).
                    // Since these funclets are invoked from JITted code, we will not find their EnclosingClauseCallerSP
                    // in an exception tracker as one does not exist (remember, these funclets are invoked "non"-exceptionally).
                    // 
                    // At this point, the caller context is that of the parent frame of the funclet. All we need is the CallerSP
                    // of that parent. We leverage a helper function that will perform an unwind against the caller context
                    // and return us the SP (of the caller of the funclet's parent).
                    StackFrame sfCallerSPOfFuncletParent = ExceptionTracker::GetCallerSPOfParentOfNonExceptionallyInvokedFunclet(pCF);
                    return sfCallerSPOfFuncletParent;
                }
            }
        }
    }

    for (pCurrentTracker = pThread->GetExceptionState()->m_pCurrentTracker;
         pCurrentTracker != NULL;
         pCurrentTracker = pCurrentTracker->m_pPrevNestedInfo)
    {
        // Check if the tracker has just been created.
        if (pCurrentTracker->m_ScannedStackRange.IsEmpty())
        {
            continue;
        }

        // Since the current frame is a non-filter funclet, determine if its caller is the same one
        // as was saved against the exception tracker before the funclet was invoked in ExceptionTracker::CallHandler.
        CallerStackFrame csfFunclet = pCurrentTracker->m_EHClauseInfo.GetCallerStackFrameForEHClause();
        if (csfCurrent == csfFunclet) 
        {
            // The EnclosingClauseCallerSP is initialized in ExceptionTracker::ProcessManagedCallFrame, just before
            // invoking the funclets. Basically, we are using the SP of the caller of the frame containing the funclet
            // to determine if we have reached the frame containing the funclet.
            EnclosingClauseInfo srcEnclosingClause = (fForGCReporting) ? pCurrentTracker->m_EnclosingClauseInfoForGCReporting
                                                                       : pCurrentTracker->m_EnclosingClauseInfo;
            sfResult = (StackFrame)(CallerStackFrame(srcEnclosingClause.GetEnclosingClauseCallerSP()));

            // Check whether the tracker has called any funclet yet.
            if (sfResult.IsNull())
            {
                continue;
            }

            // Set the relevant information.
            if (pfRealParent != NULL)
            {
                *pfRealParent = !srcEnclosingClause.EnclosingClauseIsFunclet();
            }
            if (pParentOffset != NULL)
            {
                *pParentOffset = srcEnclosingClause.GetEnclosingClauseOffset();
            }
            if (pParentCallerSP != NULL)
            {
                *pParentCallerSP = srcEnclosingClause.GetEnclosingClauseCallerSP();
            }

            break;
        }
        // Check if this tracker was collapsed with another tracker and if caller of funclet clause for collapsed exception tracker matches.
        else if (fForGCReporting && !(pCurrentTracker->m_csfEHClauseOfCollapsedTracker.IsNull()) && csfCurrent == pCurrentTracker->m_csfEHClauseOfCollapsedTracker)
        {
            EnclosingClauseInfo srcEnclosingClause = pCurrentTracker->m_EnclosingClauseInfoOfCollapsedTracker;
            sfResult = (StackFrame)(CallerStackFrame(srcEnclosingClause.GetEnclosingClauseCallerSP()));

            _ASSERTE(!sfResult.IsNull());

            break;

        }
    }

lExit: ;

    STRESS_LOG3(LF_EH|LF_GCROOTS, LL_INFO100, "Returning 0x%p as the parent stack frame for %s 0x%p\n",
                sfResult.SP, fIsFilterFunclet ? "filter funclet" : "funclet", csfCurrent.SP);

    return sfResult;
}

struct RareFindParentStackFrameCallbackState
{
    StackFrame m_sfTarget;
    StackFrame m_sfParent;
    bool       m_fFoundTarget;
    DWORD      m_dwParentOffset;
    UINT_PTR   m_uParentCallerSP;
};

// This is the callback for the stackwalk to get the parent stack frame for a filter funclet.
//
// static
StackWalkAction ExceptionTracker::RareFindParentStackFrameCallback(CrawlFrame* pCF, LPVOID pData)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    RareFindParentStackFrameCallbackState* pState = (RareFindParentStackFrameCallbackState*)pData;

    // In all cases, we don't care about explicit frame.
    if (!pCF->IsFrameless())
    {
        return SWA_CONTINUE;
    }

    REGDISPLAY* pRegDisplay = pCF->GetRegisterSet();
    StackFrame  sfCurrent   = StackFrame::FromRegDisplay(pRegDisplay);

    // Check if we have reached the target already.
    if (!pState->m_fFoundTarget)
    {
        if (sfCurrent != pState->m_sfTarget)
        {
            return SWA_CONTINUE;
        }

        pState->m_fFoundTarget = true;
    }

    // We hae reached the target, now do the normal frames skipping.
    if (!pState->m_sfParent.IsNull())
    {
        if (pState->m_sfParent.IsMaxVal() || IsUnwoundToTargetParentFrame(pCF, pState->m_sfParent))
        {
            // We have reached the specified method frame to skip to.
            // Now clear the flag and ask again.
            pState->m_sfParent.Clear();
        }
    }

    if (pState->m_sfParent.IsNull() && pCF->IsFunclet())
    {
        pState->m_sfParent = ExceptionTracker::FindParentStackFrameHelper(pCF, NULL, NULL, NULL);
    }

    // If we still need to skip, then continue the stackwalk.
    if (!pState->m_sfParent.IsNull())
    {
        return SWA_CONTINUE;
    }

    // At this point, we are done.
    pState->m_sfParent        = ExceptionTracker::GetStackFrameForParentCheck(pCF);
    pState->m_dwParentOffset  = pCF->GetRelOffset();

    _ASSERTE(pRegDisplay->IsCallerContextValid);
    pState->m_uParentCallerSP = GetSP(pRegDisplay->pCallerContext);

    return SWA_ABORT;
}

// static
StackFrame ExceptionTracker::RareFindParentStackFrame(CrawlFrame* pCF,
                                                      DWORD*      pParentOffset,
                                                      UINT_PTR*   pParentCallerSP)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION( pCF != NULL );
        PRECONDITION( pCF->IsFunclet() );
        PRECONDITION( CheckPointer(pParentOffset, NULL_OK) );
        PRECONDITION( CheckPointer(pParentCallerSP, NULL_OK) );
    }
    CONTRACTL_END;

    Thread* pThread = pCF->pThread;

    RareFindParentStackFrameCallbackState state;
    state.m_sfParent.Clear();
    state.m_sfTarget = StackFrame::FromRegDisplay(pCF->GetRegisterSet());
    state.m_fFoundTarget = false;

    PTR_Frame     pFrame = pCF->pFrame;
    T_CONTEXT    ctx;
    REGDISPLAY rd;
    CopyRegDisplay((const PREGDISPLAY)pCF->GetRegisterSet(), &rd, &ctx);

    pThread->StackWalkFramesEx(&rd, &ExceptionTracker::RareFindParentStackFrameCallback, &state, 0, pFrame);

    if (pParentOffset != NULL)
    {
        *pParentOffset = state.m_dwParentOffset;
    }
    if (pParentCallerSP != NULL)
    {
        *pParentCallerSP = state.m_uParentCallerSP;
    }
    return state.m_sfParent;
}

ExceptionTracker::StackRange::StackRange()
{
    WRAPPER_NO_CONTRACT;

#ifndef DACCESS_COMPILE
    Reset();
#endif // DACCESS_COMPILE
}

ExceptionTracker::EnclosingClauseInfo::EnclosingClauseInfo()
{
    LIMITED_METHOD_CONTRACT;

    m_fEnclosingClauseIsFunclet = false;
    m_dwEnclosingClauseOffset   = 0;
    m_uEnclosingClauseCallerSP  = 0;
}

ExceptionTracker::EnclosingClauseInfo::EnclosingClauseInfo(bool     fEnclosingClauseIsFunclet,
                                                           DWORD    dwEnclosingClauseOffset,
                                                    UINT_PTR uEnclosingClauseCallerSP)
{
    LIMITED_METHOD_CONTRACT;

    m_fEnclosingClauseIsFunclet = fEnclosingClauseIsFunclet;
    m_dwEnclosingClauseOffset   = dwEnclosingClauseOffset;
    m_uEnclosingClauseCallerSP  = uEnclosingClauseCallerSP;
}

bool ExceptionTracker::EnclosingClauseInfo::EnclosingClauseIsFunclet()
{
    LIMITED_METHOD_CONTRACT;
    return m_fEnclosingClauseIsFunclet;
}

DWORD ExceptionTracker::EnclosingClauseInfo::GetEnclosingClauseOffset()
{
    LIMITED_METHOD_CONTRACT;
    return m_dwEnclosingClauseOffset;
}

UINT_PTR ExceptionTracker::EnclosingClauseInfo::GetEnclosingClauseCallerSP()
{
    LIMITED_METHOD_CONTRACT;
    return m_uEnclosingClauseCallerSP;
}

void ExceptionTracker::EnclosingClauseInfo::SetEnclosingClauseCallerSP(UINT_PTR callerSP)
{
    LIMITED_METHOD_CONTRACT;
    m_uEnclosingClauseCallerSP = callerSP;
}

bool ExceptionTracker::EnclosingClauseInfo::operator==(const EnclosingClauseInfo & rhs)
{
    LIMITED_METHOD_CONTRACT;
    SUPPORTS_DAC;

    return ((this->m_fEnclosingClauseIsFunclet == rhs.m_fEnclosingClauseIsFunclet) &&
            (this->m_dwEnclosingClauseOffset   == rhs.m_dwEnclosingClauseOffset) &&
            (this->m_uEnclosingClauseCallerSP  == rhs.m_uEnclosingClauseCallerSP));
}

void ExceptionTracker::ReleaseResources()
{
#ifndef DACCESS_COMPILE
    if (m_hThrowable)
    {
        if (!CLRException::IsPreallocatedExceptionHandle(m_hThrowable))
        {
            DestroyHandle(m_hThrowable);
        }
        m_hThrowable = NULL;
    }
    m_StackTraceInfo.FreeStackTrace();

#ifndef FEATURE_PAL 
    // Clear any held Watson Bucketing details
    GetWatsonBucketTracker()->ClearWatsonBucketDetails();
#else // !FEATURE_PAL
    if (m_fOwnsExceptionPointers)
    {
        PAL_FreeExceptionRecords(m_ptrs.ExceptionRecord, m_ptrs.ContextRecord);
        m_fOwnsExceptionPointers = FALSE;
    }
#endif // !FEATURE_PAL
#endif // DACCESS_COMPILE
}

void ExceptionTracker::SetEnclosingClauseInfo(bool     fEnclosingClauseIsFunclet,
                                              DWORD    dwEnclosingClauseOffset,
                                              UINT_PTR uEnclosingClauseCallerSP)
{
    // Preserve the details of the current frame for GC reporting before
    // we apply the nested exception logic below.
    this->m_EnclosingClauseInfoForGCReporting = EnclosingClauseInfo(fEnclosingClauseIsFunclet,
                                                      dwEnclosingClauseOffset,
                                                      uEnclosingClauseCallerSP);
    if (this->m_pPrevNestedInfo != NULL)
    {
        PTR_ExceptionTracker pPrevTracker = this->m_pPrevNestedInfo;
        CallerStackFrame csfPrevEHClause = pPrevTracker->m_EHClauseInfo.GetCallerStackFrameForEHClause();

        // Just propagate the information if this is a nested exception.
        if (csfPrevEHClause.SP == uEnclosingClauseCallerSP)
        {
            this->m_EnclosingClauseInfo = pPrevTracker->m_EnclosingClauseInfo;
            return;
        }
    }

    this->m_EnclosingClauseInfo = EnclosingClauseInfo(fEnclosingClauseIsFunclet,
                                                      dwEnclosingClauseOffset,
                                                      uEnclosingClauseCallerSP);
}


#ifdef DACCESS_COMPILE
void ExceptionTracker::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    // ExInfo is embedded so don't enum 'this'.
    OBJECTHANDLE_EnumMemoryRegions(m_hThrowable);
    m_ptrs.ExceptionRecord.EnumMem();
    m_ptrs.ContextRecord.EnumMem();
}
#endif // DACCESS_COMPILE

#ifndef DACCESS_COMPILE
// This is a thin wrapper around ResetThreadAbortState. Its primarily used to
// instantiate CrawlFrame, when required, for walking the stack on IA64.
//
// The "when required" part are the set of conditions checked prior to the call to
// this method in ExceptionTracker::ProcessOSExceptionNotification (and asserted in
// ResetThreadabortState).
//
// Also, since CrawlFrame ctor is protected, it can only be instantiated by friend
// types (which ExceptionTracker is).

// static
void ExceptionTracker::ResetThreadAbortStatus(PTR_Thread pThread, CrawlFrame *pCf, StackFrame sfCurrentStackFrame)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(pThread != NULL);
        PRECONDITION(pCf != NULL);
        PRECONDITION(!sfCurrentStackFrame.IsNull());
    }
    CONTRACTL_END;

    if (pThread->IsAbortRequested())
    {
        ResetThreadAbortState(pThread, pCf, sfCurrentStackFrame);
    }
}
#endif //!DACCESS_COMPILE

#endif // WIN64EXCEPTIONS

