// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#ifdef FEATURE_EH_FUNCLETS
#include "exceptionhandling.h"
#include "dbginterface.h"
#include "asmconstants.h"
#include "eetoprofinterfacewrapper.inl"
#include "eedbginterfaceimpl.inl"
#include "eventtrace.h"
#include "virtualcallstub.h"
#include "utilcode.h"
#include "interoplibinterface.h"
#include "corinfo.h"
#include "exceptionhandlingqcalls.h"
#include "exinfo.h"
#include "configuration.h"

#if defined(TARGET_X86)
#define USE_CURRENT_CONTEXT_IN_FILTER
#endif // TARGET_X86

#if defined(TARGET_ARM) || defined(TARGET_ARM64) || defined(TARGET_X86) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
#define ADJUST_PC_UNWOUND_TO_CALL
#define STACK_RANGE_BOUNDS_ARE_CALLER_SP
// For ARM/ARM64, EstablisherFrame is Caller-SP (SP just before executing call instruction).
// This has been confirmed by AaronGi from the kernel team for Windows.
//
// For x86/Linux, RtlVirtualUnwind sets EstablisherFrame as Caller-SP.
#define ESTABLISHER_FRAME_ADDRESS_IS_CALLER_SP
#endif // TARGET_ARM || TARGET_ARM64 || TARGET_X86 || TARGET_LOONGARCH64 || TARGET_RISCV64

#ifndef TARGET_UNIX
void NOINLINE
ClrUnwindEx(EXCEPTION_RECORD* pExceptionRecord,
                 UINT_PTR          ReturnValue,
                 UINT_PTR          TargetIP,
                 UINT_PTR          TargetFrameSp);
#ifdef TARGET_X86
EXTERN_C BOOL CallRtlUnwind(EXCEPTION_REGISTRATION_RECORD *pEstablisherFrame, PVOID callback, EXCEPTION_RECORD *pExceptionRecord, PVOID retval);
#endif
#endif // !TARGET_UNIX

bool IsCallDescrWorkerInternalReturnAddress(PCODE pCode);

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
static void DoEHLog(DWORD lvl, _In_z_ const char *fmt, ...);
#define EH_LOG(expr)  { DoEHLog expr ; }
#else
#define EH_LOG(expr)
#endif

uint32_t            g_exceptionCount;

void FixContext(PCONTEXT pContextRecord)
{
#define FIXUPREG(reg, value)                                                                \
    do {                                                                                    \
        STRESS_LOG2(LF_GCROOTS, LL_INFO100, "Updating " #reg " %p to %p\n",                 \
                pContextRecord->reg,                                                        \
                (value));                                                                   \
        pContextRecord->reg = (value);                                                      \
    } while (0)

#ifdef TARGET_X86
    size_t resumeSp = EECodeManager::GetResumeSp(pContextRecord);
    FIXUPREG(Esp, resumeSp);
#endif // TARGET_X86

#undef FIXUPREG
}

MethodDesc * GetUserMethodForILStub(Thread * pThread, UINT_PTR uStubSP, MethodDesc * pILStubMD, Frame ** ppFrameOut);

#ifdef TARGET_UNIX
BOOL HandleHardwareException(PAL_SEHException* ex);
BOOL IsSafeToHandleHardwareException(PCONTEXT contextRecord, PEXCEPTION_RECORD exceptionRecord);
#endif // TARGET_UNIX

static inline void UpdatePerformanceMetrics(CrawlFrame *pcfThisFrame, BOOL bIsRethrownException, BOOL bIsNewException)
{
    WRAPPER_NO_CONTRACT;
    InterlockedIncrement((LONG*)&g_exceptionCount);

    // Fire an exception thrown ETW event when an exception occurs
    ETW::ExceptionLog::ExceptionThrown(pcfThisFrame, bIsRethrownException, bIsNewException);
}

void InitializeExceptionHandling()
{
    EH_LOG((LL_INFO100, "InitializeExceptionHandling(): ExInfo size: 0x%x bytes\n", sizeof(ExInfo)));

    CLRAddVectoredHandlers();

#ifdef TARGET_UNIX
    // Register handler of hardware exceptions like null reference in PAL
    PAL_SetHardwareExceptionHandler(HandleHardwareException, IsSafeToHandleHardwareException);

    // Register handler for determining whether the specified IP has code that is a GC marker for GCCover
    PAL_SetGetGcMarkerExceptionCode(GetGcMarkerExceptionCode);
#endif // TARGET_UNIX
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
            if (pFrame->GetFrameIdentifier() == FrameIdentifier::InlinedCallFrame)
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


//static
void ExInfo::UpdateNonvolatileRegisters(CONTEXT *pContextRecord, REGDISPLAY *pRegDisplay, bool fAborting)
{
    CONTEXT* pAbortContext = NULL;
    if (fAborting)
    {
        pAbortContext = GetThread()->GetAbortContext();
    }

    // Windows/x86 doesn't have unwinding mechanism for native code. RtlUnwind in
    // ProcessCLRException leaves us with the original exception context. Thus we
    // rely solely on our frames and managed code unwinding. This also means that
    // if we pass through InlinedCallFrame we end up with empty context pointers.
#if defined(TARGET_UNIX) || defined(TARGET_X86)
#define HANDLE_NULL_CONTEXT_POINTER
#else // TARGET_UNIX || TARGET_X86
#define HANDLE_NULL_CONTEXT_POINTER _ASSERTE(false)
#endif // TARGET_UNIX || TARGET_X86

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


#if defined(TARGET_X86)

    UPDATEREG(Ebx);
    UPDATEREG(Esi);
    UPDATEREG(Edi);
    UPDATEREG(Ebp);

#elif defined(TARGET_AMD64)

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

#elif defined(TARGET_ARM)

    UPDATEREG(R4);
    UPDATEREG(R5);
    UPDATEREG(R6);
    UPDATEREG(R7);
    UPDATEREG(R8);
    UPDATEREG(R9);
    UPDATEREG(R10);
    UPDATEREG(R11);

#elif defined(TARGET_ARM64)

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

#elif defined(TARGET_LOONGARCH64)

    UPDATEREG(S0);
    UPDATEREG(S1);
    UPDATEREG(S2);
    UPDATEREG(S3);
    UPDATEREG(S4);
    UPDATEREG(S5);
    UPDATEREG(S6);
    UPDATEREG(S7);
    UPDATEREG(S8);
    UPDATEREG(Fp);

#elif defined(TARGET_RISCV64)

    UPDATEREG(S1);
    UPDATEREG(S2);
    UPDATEREG(S3);
    UPDATEREG(S4);
    UPDATEREG(S5);
    UPDATEREG(S6);
    UPDATEREG(S7);
    UPDATEREG(S8);
    UPDATEREG(S9);
    UPDATEREG(S10);
    UPDATEREG(S11);
    UPDATEREG(Fp);

#else
    PORTABILITY_ASSERT("ExInfo::UpdateNonvolatileRegisters");
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

void CleanUpForSecondPass(Thread* pThread, bool fIsSO, LPVOID MemoryStackFpForFrameChain, LPVOID MemoryStackFp);

static void PopExplicitFrames(Thread *pThread, void *targetSp, void *targetCallerSp, bool popGCFrames = true)
{
#if defined(TARGET_X86) && defined(TARGET_WINDOWS) && defined(FEATURE_EH_FUNCLETS)
    PopSEHRecords((void*)targetSp);
#endif

    Frame* pFrame = pThread->GetFrame();
    while (pFrame < targetSp)
    {
        pFrame->ExceptionUnwind();
        pFrame->Pop(pThread);
        pFrame = pThread->GetFrame();
    }

    // Check if the pFrame is an active InlinedCallFrame inside of the target frame. It needs to be popped or inactivated depending
    // on the target architecture / ready to run
    if ((pFrame < targetCallerSp) && InlinedCallFrame::FrameHasActiveCall(pFrame))
    {
        InlinedCallFrame* pInlinedCallFrame = (InlinedCallFrame*)pFrame;
        // When unwinding an exception in ReadyToRun, the JIT_PInvokeEnd helper which unlinks the ICF from
        // the thread will be skipped. This is because unlike jitted code, each pinvoke is wrapped by calls
        // to the JIT_PInvokeBegin and JIT_PInvokeEnd helpers, which push and pop the ICF on the thread. The
        // ICF is not linked at the method prolog and unlined at the epilog when running R2R code. Since the
        // JIT_PInvokeEnd helper will be skipped, we need to unlink the ICF here. If the executing method
        // has another pinvoke, it will re-link the ICF again when the JIT_PInvokeBegin helper is called.
        TADDR returnAddress = pInlinedCallFrame->m_pCallerReturnAddress;
#ifdef USE_PER_FRAME_PINVOKE_INIT
        // If we're setting up the frame for each P/Invoke for the given platform,
        // then we do this for all P/Invokes except ones in IL stubs.
        // IL stubs link the frame in for the whole stub, so if an exception is thrown during marshalling,
        // the ICF will be on the frame chain and inactive.
        if (!ExecutionManager::GetCodeMethodDesc(returnAddress)->IsILStub())
#else
        // If we aren't setting up the frame for each P/Invoke (instead setting up once per method),
        // then ReadyToRun code is the only code using the per-P/Invoke logic.
        if (ExecutionManager::IsReadyToRunCode(returnAddress))
#endif
        {
            pFrame->ExceptionUnwind();
            pFrame->Pop(pThread);
        }
        else
        {
            pInlinedCallFrame->Reset();
        }
    }

    if (popGCFrames)
    {
        GCFrame* pGCFrame = pThread->GetGCFrame();
        while ((pGCFrame != GCFRAME_TOP) && pGCFrame < targetSp)
        {
            pGCFrame->Pop();
            pGCFrame = pThread->GetGCFrame();
        }
    }
}

#if defined(HOST_WINDOWS) && defined(HOST_X86)
static void DispatchLongJmp(IN     PEXCEPTION_RECORD   pExceptionRecord,
                            IN     PVOID               pEstablisherFrame,
                            IN OUT PCONTEXT            pContextRecord)
{
    // Pop all the SEH frames including the one that is currently called
    // to prevent setjmp from trying to unwind it again when we inject our
    // longjmp.
    SetCurrentSEHRecord(((PEXCEPTION_REGISTRATION_RECORD)pEstablisherFrame)->Next);

#pragma warning(push)
#pragma warning(disable:4611) // interaction between 'function' and C++ object destruction is non-portable
    jmp_buf jumpBuffer;
    if (setjmp(jumpBuffer))
    {
        // We reach this point after the finally handlers were called and
        // the unwinding should continue below the managed frame.
        return;
    }
#pragma warning(pop)

    // Emulate the parameters that are passed on 64-bit systems and expected by
    // DispatchManagedException.
    EXCEPTION_RECORD newExceptionRecord = *pExceptionRecord;
    newExceptionRecord.NumberParameters = 2;
    newExceptionRecord.ExceptionInformation[0] = (DWORD_PTR)&jumpBuffer;
    newExceptionRecord.ExceptionInformation[1] = 1;

    OBJECTREF oref = ExInfo::CreateThrowable(&newExceptionRecord, FALSE);
    DispatchManagedException(oref, pContextRecord, &newExceptionRecord);
    UNREACHABLE();
}
#endif

EXTERN_C EXCEPTION_DISPOSITION __cdecl
ProcessCLRException(IN     PEXCEPTION_RECORD   pExceptionRecord,
                    IN     PVOID               pEstablisherFrame,
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

    Thread* pThread         = GetThread();

    // On x86 we don't have dispatcher context
#ifndef TARGET_X86
    // Skip native frames of asm helpers that have the ProcessCLRException set as their personality routine.
    // There is nothing to do for those with the new exception handling.
    if (!ExecutionManager::IsManagedCode((PCODE)pDispatcherContext->ControlPc))
    {
        return ExceptionContinueSearch;
    }
#endif

    if (pThread->HasThreadStateNC(Thread::TSNC_UnhandledException2ndPass) && !(pExceptionRecord->ExceptionFlags & EXCEPTION_UNWINDING))
    {
        // We are in the 1st pass of exception handling, but the thread mark says that it has already executed 2nd pass
        // of unhandled exception handling. That means that some external native code on top of the stack has caught the
        // exception that runtime considered to be unhandled, and a new native exception was thrown on the current thread.
        // We need to reset the flags below so that we no longer block exception handling for the managed frames.
        pThread->ResetThreadStateNC(Thread::TSNC_UnhandledException2ndPass);
        pThread->ResetThreadStateNC(Thread::TSNC_ProcessedUnhandledException);
    }

    // Also skip all frames when processing unhandled exceptions. That allows them to reach the host app
    // level and let 3rd party the chance to handle them.
    if (pThread->HasThreadStateNC(Thread::TSNC_ProcessedUnhandledException))
    {
        if (pExceptionRecord->ExceptionFlags & EXCEPTION_UNWINDING)
        {
            if (!pThread->HasThreadStateNC(Thread::TSNC_UnhandledException2ndPass))
            {
                pThread->SetThreadStateNC(Thread::TSNC_UnhandledException2ndPass);
                GCX_COOP();
                // The 3rd argument passes to PopExplicitFrame is normally the parent SP to correctly handle InlinedCallFrame embbeded
                // in parent managed frame. But at this point there are no further managed frames are on the stack, so we can pass NULL.
                // Also don't pop the GC frames, their destructor will pop them as the exception propagates.
                // NOTE: this needs to be popped in the 2nd pass to ensure that crash dumps and Watson get the dump with these still
                // present.
                ExInfo *pExInfo = (ExInfo*)pThread->GetExceptionState()->GetCurrentExceptionTracker();
                void *sp = (void*)GetRegdisplaySP(pExInfo->m_frameIter.m_crawl.GetRegisterSet());
                PopExplicitFrames(pThread, sp, NULL /* targetCallerSp */, false /* popGCFrames */);
                ExInfo::PopExInfos(pThread, sp);
            }
        }

        return ExceptionContinueSearch;
    }

#ifndef HOST_UNIX
    // First pass (searching)
    if (!(pExceptionRecord->ExceptionFlags & EXCEPTION_UNWINDING))
    {
        // If the exception is a breakpoint, let it go. The managed exception handling
        // doesn't process breakpoints.
        if ((pExceptionRecord->ExceptionCode == STATUS_BREAKPOINT) ||
            (pExceptionRecord->ExceptionCode == STATUS_SINGLE_STEP))
        {
            return ExceptionContinueSearch;
        }

        // Failfast if exception indicates corrupted process state
        if (IsProcessCorruptedStateException(pExceptionRecord->ExceptionCode, /* throwable */ NULL))
        {
            EEPOLICY_HANDLE_FATAL_ERROR(pExceptionRecord->ExceptionCode);
        }

#ifdef TARGET_X86
        CallRtlUnwind((PEXCEPTION_REGISTRATION_RECORD)pEstablisherFrame, NULL, pExceptionRecord, 0);
#else
        ClrUnwindEx(pExceptionRecord,
                    (UINT_PTR)pThread,
                    INVALID_RESUME_ADDRESS,
                    pDispatcherContext->EstablisherFrame);
        UNREACHABLE();
#endif
    }

    // Second pass (unwinding)
    GCX_COOP_NO_DTOR();
    ThreadExceptionState* pExState = pThread->GetExceptionState();
    ExInfo *pPrevExInfo = (ExInfo*)pExState->GetCurrentExceptionTracker();
    if (pPrevExInfo != NULL && pPrevExInfo->m_DebuggerExState.GetDebuggerInterceptContext() != NULL)
    {
        ContinueExceptionInterceptionUnwind();
        UNREACHABLE();
    }
#if defined(HOST_WINDOWS) && defined(HOST_X86)
    else if (pExceptionRecord->ExceptionCode == STATUS_LONGJUMP)
    {
        DispatchLongJmp(pExceptionRecord, pEstablisherFrame, pContextRecord);
        GCX_COOP_NO_DTOR_END();
        return ExceptionContinueSearch;
    }
#endif
    else
    {
        OBJECTREF oref = ExInfo::CreateThrowable(pExceptionRecord, FALSE);
        DispatchManagedException(oref, pContextRecord, pExceptionRecord);
    }
#endif // !HOST_UNIX

    EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(COR_E_EXECUTIONENGINE, _T("SEH exception leaked into managed code"));
    UNREACHABLE();
}


#if defined(DEBUGGING_SUPPORTED)
BOOL NotifyDebuggerOfStub(Thread* pThread, Frame* pCurrentFrame)
{
    LIMITED_METHOD_CONTRACT;

    BOOL fDeliveredFirstChanceNotification = FALSE;

    _ASSERTE(GetThreadNULLOk() == pThread);

    GCX_COOP();

    // For debugger, we may want to notify 1st chance exceptions if they're coming out of a stub.
    // We recognize stubs as Frames with a M2U transition type. The debugger's stackwalker also
    // recognizes these frames and publishes ICorDebugInternalFrames in the stackwalk. It's
    // important to use pFrame as the stack address so that the Exception callback matches up
    // w/ the ICorDebugInternalFrame stack range.
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

    return fDeliveredFirstChanceNotification;
}
#endif // DEBUGGING_SUPPORTED

#undef OPTIONAL_SO_CLEANUP_UNWIND

#define OPTIONAL_SO_CLEANUP_UNWIND(pThread, pFrame)  if (pThread->GetFrame() < pFrame) { UnwindFrameChain(pThread, pFrame); }

#undef OPTIONAL_SO_CLEANUP_UNWIND
#define OPTIONAL_SO_CLEANUP_UNWIND(pThread, pFrame)


//
// this must be done after the second pass has run, it does not
// reference anything on the stack, so it is safe to run in an
// SEH __except clause as well as a C++ catch clause.
//
// static
void ExInfo::PopTrackers(
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
    Thread *pCurThread = GetThreadNULLOk();
    if ((pCurThread != NULL) && (pCurThread->GetExceptionState()->IsExceptionInProgress()))
    {
        // Refer to the comment around ExInfo::HasFrameBeenUnwoundByAnyActiveException
        // for details on the usage of this COOP switch.
        GCX_COOP();

        PopTrackers(sf, false);
    }
}

//
// static
void ExInfo::PopTrackers(
    StackFrame sfResumeFrame,
    bool fPopWhenEqual
    )
{
    CONTRACTL
    {
        // Refer to the comment around ExInfo::HasFrameBeenUnwoundByAnyActiveException
        // for details on the mode being COOP here.
        MODE_COOPERATIVE;
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACTL_END;

    return;
}


//
// static
OBJECTREF ExInfo::CreateThrowable(
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

#if defined(DEBUGGING_SUPPORTED)

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


    GCX_COOP();

    ExInfo* pExInfo = (ExInfo*)pExState->GetCurrentExceptionTracker();
    _ASSERTE(pExInfo != NULL);

    PREPARE_NONVIRTUAL_CALLSITE(METHOD__EH__UNWIND_AND_INTERCEPT);
    DECLARE_ARGHOLDER_ARRAY(args, 2);
    args[ARGNUM_0] = PTR_TO_ARGHOLDER(pExInfo);
    args[ARGNUM_1] = PTR_TO_ARGHOLDER(uInterceptStackFrame);
    pThread->IncPreventAbort();

    //Ex.RhUnwindAndIntercept(throwable, &exInfo)
    CRITICAL_CALLSITE;
    CALL_MANAGED_METHOD_NORET(args)

    UNREACHABLE();
}
#endif // DEBUGGER_EXCEPTION_INTERCEPTION_SUPPORTED
#endif // DEBUGGING_SUPPORTED

#ifdef _DEBUG
//
// static
UINT_PTR ExInfo::DebugComputeNestingLevel()
{
    UINT_PTR uNestingLevel = 0;
    Thread* pThread = GetThreadNULLOk();

    if (pThread)
    {
        PTR_ExInfo pTracker;
        pTracker = pThread->GetExceptionState()->GetCurrentExceptionTracker();

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

        EH_LOG((LL_INFO1000, "  | %s clause [%x, %x], handler: [%x, %x]",
                (IsFault(&EHClause)         ? "fault"   :
                (IsFinally(&EHClause)       ? "finally" :
                (IsFilterHandler(&EHClause) ? "filter"  :
                (IsTypedHandler(&EHClause)  ? "typed"   : "unknown")))),
                EHClause.TryStartPC       , // + uMethodStartPC,
                EHClause.TryEndPC         , // + uMethodStartPC,
                EHClause.HandlerStartPC   , // + uMethodStartPC,
                EHClause.HandlerEndPC       // + uMethodStartPC
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
    _In_z_ const char *fmt,
    ...
    )
{
    if (!LoggingOn(LF_EH, lvl))
        return;

    va_list  args;
    va_start(args, fmt);

    UINT_PTR nestinglevel = ExInfo::DebugComputeNestingLevel();
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

#ifdef TARGET_UNIX

//---------------------------------------------------------------------------------------
//
// Function to update the current context for exception propagation.
//
// Arguments:
//      callback        - the exception propagation callback
//      callbackCtx     - the exception propagation callback context
//      currentContext  - the current context to update.
//
static VOID UpdateContextForPropagationCallback(
    Interop::ManagedToNativeExceptionCallback callback,
    void *callbackCtx,
    CONTEXT* startContext)
{
    _ASSERTE(callback != NULL);

#ifdef TARGET_AMD64

    // Don't restore the stack pointer to exact same context. Leave the
    // return IP on the stack to let the unwinder work if the callback throws
    // an exception as opposed to failing fast.
    startContext->Rsp -= sizeof(void*);

    // Pass the context for the callback as the first argument.
    startContext->Rdi = (DWORD64)callbackCtx;

#elif defined(TARGET_ARM64)

    // Reset the linked return register to the current function to let the
    // unwinder work if the callback throws an exception as opposed to failing fast.
    startContext->Lr = GetIP(startContext);

    // Pass the context for the callback as the first argument.
    startContext->X0 = (DWORD64)callbackCtx;

#elif defined(TARGET_ARM)

    // Reset the linked return register to the current function to let the
    // unwinder work if the callback throws an exception as opposed to failing fast.
    startContext->Lr = GetIP(startContext);

    // Pass the context for the callback as the first argument.
    startContext->R0 = (DWORD)callbackCtx;

#else

    EEPOLICY_HANDLE_FATAL_ERROR_WITH_MESSAGE(
        COR_E_FAILFAST,
        W("Managed exception propagation not supported for platform."));

#endif

    // The last thing to do is set the supplied callback function.
    SetIP(startContext, (PCODE)callback);
}

//---------------------------------------------------------------------------------------
//
// Function to update the current context for exception propagation.
//
// Arguments:
//      exception       - the PAL_SEHException representing the propagating exception.
//      currentContext  - the current context to update.
//
static VOID UpdateContextForPropagationCallback(
    PAL_SEHException& ex,
    CONTEXT* startContext)
{
    UpdateContextForPropagationCallback(ex.ManagedToNativeExceptionCallback, ex.ManagedToNativeExceptionCallbackContext, startContext);
}

extern void* g_hostingApiReturnAddress;

VOID DECLSPEC_NORETURN DispatchManagedException(PAL_SEHException& ex, bool isHardwareException)
{
    if (!isHardwareException)
    {
        RtlCaptureContext(ex.GetContextRecord());
    }
    GCX_COOP();
    OBJECTREF throwable = ExInfo::CreateThrowable(ex.GetExceptionRecord(), FALSE);
    DispatchManagedException(throwable, ex.GetContextRecord());
}

#if defined(TARGET_AMD64) || defined(TARGET_X86)

/*++
Function :
    GetRegisterAddressByIndex

    Get address of a register in a context

Parameters:
    PCONTEXT pContext : context containing the registers
    UINT index :        index of the register (Rax=0 .. R15=15)

Return value :
    Pointer to the context member represetting the register
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
    Value of the context member represetting the register
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
    Value of the context member represetting the register
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
#if defined(TARGET_AMD64)
                result = (DWORD64)ip + sizeof(INT32) + *(INT32*)ip;
#else
                result = (DWORD64)(*(DWORD*)ip);
#endif // TARGET_AMD64
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

    // The REX prefix must directly precede the instruction code
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
#endif // TARGET_AMD64 || TARGET_X86

BOOL IsSafeToCallExecutionManager()
{
    Thread *pThread = GetThreadNULLOk();

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

BOOL IsSafeToHandleHardwareException(PCONTEXT contextRecord, PEXCEPTION_RECORD exceptionRecord)
{
#ifdef FEATURE_EMULATE_SINGLESTEP
    Thread *pThread = GetThreadNULLOk();
    if (pThread && pThread->IsSingleStepEnabled() &&
        exceptionRecord->ExceptionCode != STATUS_BREAKPOINT &&
        exceptionRecord->ExceptionCode != STATUS_SINGLE_STEP &&
        exceptionRecord->ExceptionCode != STATUS_STACK_OVERFLOW)
    {
        // tried to consolidate the code and only call HandleSingleStep here but
        // for some reason not investigated the debugger tests failed with this change
        pThread->HandleSingleStep(contextRecord, exceptionRecord->ExceptionCode);
    }
#endif

    PCODE controlPc = GetIP(contextRecord);

    if (IsIPInWriteBarrierCodeCopy(controlPc))
    {
        // Pretend we were executing the barrier function at its original location
        controlPc = AdjustWriteBarrierIP(controlPc);
    }

    return g_fEEStarted && (
        exceptionRecord->ExceptionCode == STATUS_BREAKPOINT ||
        exceptionRecord->ExceptionCode == STATUS_SINGLE_STEP ||
        exceptionRecord->ExceptionCode == STATUS_STACK_OVERFLOW ||
        (IsSafeToCallExecutionManager() && ExecutionManager::IsManagedCode(controlPc)) ||
        IsIPinVirtualStub(controlPc) ||  // access violation comes from DispatchStub of Interface call
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

    if (ex->GetExceptionRecord()->ExceptionCode == EXCEPTION_STACK_OVERFLOW)
    {
        GetThread()->SetExecutingOnAltStack();
        Thread::VirtualUnwindToFirstManagedCallFrame(ex->GetContextRecord());
        EEPolicy::HandleFatalStackOverflow(&ex->ExceptionPointers, FALSE);
        UNREACHABLE();
    }

    if (ex->GetExceptionRecord()->ExceptionCode != STATUS_BREAKPOINT && ex->GetExceptionRecord()->ExceptionCode != STATUS_SINGLE_STEP)
    {
        // A hardware exception is handled only if it happened in a jitted code or
        // in one of the JIT helper functions
        PCODE controlPc = GetIP(ex->GetContextRecord());
        if (ExecutionManager::IsManagedCode(controlPc) && IsGcMarker(ex->GetContextRecord(), ex->GetExceptionRecord()))
        {
            // Exception was handled, let the signal handler return to the exception context. Some registers in the context can
            // have been modified by the GC.
            return TRUE;
        }

#if defined(TARGET_AMD64) || defined(TARGET_X86)
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
#endif // TARGET_AMD64 || TARGET_X86

        // Create frame necessary for the exception handling
        FaultingExceptionFrame fef;
        {
            GCX_COOP();     // Must be cooperative to modify frame chain.

            if (IsIPInWriteBarrierCodeCopy(controlPc))
            {
                // Pretend we were executing the barrier function at its original location so that the unwinder can unwind the frame
                controlPc = AdjustWriteBarrierIP(controlPc);
                SetIP(ex->GetContextRecord(), controlPc);
            }

            if (IsIPInMarkedJitHelper(controlPc))
            {
                // For JIT helpers, we need to set the frame to point to the
                // managed code that called the helper, otherwise the stack
                // walker would skip all the managed frames upto the next
                // explicit frame.
                PAL_VirtualUnwind(ex->GetContextRecord(), NULL);
                ex->GetExceptionRecord()->ExceptionAddress = (PVOID)GetIP(ex->GetContextRecord());
            }
            else
            {
                AdjustContextForVirtualStub(ex->GetExceptionRecord(), ex->GetContextRecord());
            }
            fef.InitAndLink(ex->GetContextRecord());
        }

        Thread *pThread = GetThread();

        ExInfo exInfo(pThread, ex->GetExceptionRecord(), ex->GetContextRecord(), ExKind::HardwareFault);

        DWORD exceptionCode = ex->GetExceptionRecord()->ExceptionCode;
        if (exceptionCode == STATUS_ACCESS_VIOLATION)
        {
            if (ex->GetExceptionRecord()->ExceptionInformation[1] < NULL_AREA_SIZE)
            {
                exceptionCode = 0; //STATUS_NATIVEAOT_NULL_REFERENCE;
            }
        }

        if (!ex->RecordsOnStack)
        {
            exInfo.TakeExceptionPointersOwnership(ex);
        }

        GCPROTECT_BEGIN(exInfo.m_exception);
        PREPARE_NONVIRTUAL_CALLSITE(METHOD__EH__RH_THROWHW_EX);
        DECLARE_ARGHOLDER_ARRAY(args, 2);
        args[ARGNUM_0] = DWORD_TO_ARGHOLDER(exceptionCode);
        args[ARGNUM_1] = PTR_TO_ARGHOLDER(&exInfo);

        pThread->IncPreventAbort();

        //Ex.RhThrowHwEx(exceptionCode, &exInfo)
        CALL_MANAGED_METHOD_NORET(args)

        GCPROTECT_END();

        UNREACHABLE();
    }
    else
    {
        // This is a breakpoint or single step stop, we report it to the debugger.
        Thread *pThread = GetThreadNULLOk();
        if (pThread != NULL && g_pDebugInterface != NULL)
        {
#if (defined(TARGET_ARM) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64)) || defined(TARGET_RISCV64)
            // On ARM and ARM64 and LOONGARCH64 exception point to the break instruction.
            // See https://static.docs.arm.com/ddi0487/db/DDI0487D_b_armv8_arm.pdf#page=6916&zoom=100,0,152
            // at aarch64/exceptions/debug/AArch64.SoftwareBreakpoint
            // However, the rest of the code expects that it points to an instruction after the break.
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

#endif // TARGET_UNIX

void FirstChanceExceptionNotification()
{
#ifdef TARGET_WINDOWS
    // Throw an SEH exception and immediately catch it. This is used to notify debuggers and other tools
    // that an exception has been thrown.
    if (minipal_is_native_debugger_present())
    {
        __try
        {
            RaiseException(EXCEPTION_COMPLUS, 0, 0, NULL);
        }
        __except (EXCEPTION_EXECUTE_HANDLER)
        {
            // Do nothing, we just want to notify the debugger.
        }
    }
#endif // TARGET_WINDOWS
}

VOID DECLSPEC_NORETURN DispatchManagedException(OBJECTREF throwable, CONTEXT* pExceptionContext, EXCEPTION_RECORD* pExceptionRecord)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    GCPROTECT_BEGIN(throwable);

   _ASSERTE(IsException(throwable->GetMethodTable()));

    Thread *pThread = GetThread();

    ULONG_PTR hr = GetHRFromThrowable(throwable);

    EXCEPTION_RECORD newExceptionRecord;
    if (pExceptionRecord != NULL)
    {
        newExceptionRecord = *pExceptionRecord;
    }
    else
    {
        newExceptionRecord.ExceptionCode = EXCEPTION_COMPLUS;
        newExceptionRecord.ExceptionFlags = EXCEPTION_NONCONTINUABLE | EXCEPTION_SOFTWARE_ORIGINATE;
        newExceptionRecord.ExceptionAddress = (void *)(void (*)(OBJECTREF))&DispatchManagedException;
        newExceptionRecord.NumberParameters = MarkAsThrownByUs(newExceptionRecord.ExceptionInformation, hr);
        newExceptionRecord.ExceptionRecord = NULL;
    }

    ExInfo exInfo(pThread, &newExceptionRecord, pExceptionContext, ExKind::Throw);

#ifdef HOST_WINDOWS
    // On Windows, this enables the possibility to propagate a longjmp across managed frames. Longjmp
    // behaves like a SEH exception, but only runs the second (unwinding) pass.
    // NOTE: This is a best effort purely for backward compatibility with the legacy exception handling.
    // Skipping over managed frames using setjmp/longjmp is
    // is unsupported and it is not guaranteed to work reliably in all cases.
    // https://learn.microsoft.com/dotnet/standard/native-interop/exceptions-interoperability#setjmplongjmp-behaviors
    if ((pExceptionRecord != NULL) && (pExceptionRecord->ExceptionCode == STATUS_LONGJUMP))
    {
        // longjmp over managed frames. The EXCEPTION_RECORD::ExceptionInformation store the
        // jmp_buf and the return value for STATUS_LONGJUMP, so we extract it here. When the
        // exception handling code moves out of the managed frames, we call the longjmp with
        // these arguments again to continue its propagation.
        exInfo.m_pLongJmpBuf = (jmp_buf*)pExceptionRecord->ExceptionInformation[0];
        exInfo.m_longJmpReturnValue = (int)pExceptionRecord->ExceptionInformation[1];
    }
#endif // HOST_WINDOWS

    if (pThread->IsAbortInitiated () && IsExceptionOfType(kThreadAbortException,&throwable))
    {
        pThread->ResetPreparingAbort();

        if (pThread->GetFrame() == FRAME_TOP)
        {
            // There is no more managed code on stack.
            pThread->ResetAbort();
        }
    }

    GCPROTECT_BEGIN(exInfo.m_exception);

    PREPARE_NONVIRTUAL_CALLSITE(METHOD__EH__RH_THROW_EX);
    DECLARE_ARGHOLDER_ARRAY(args, 2);
    args[ARGNUM_0] = OBJECTREF_TO_ARGHOLDER(throwable);
    args[ARGNUM_1] = PTR_TO_ARGHOLDER(&exInfo);

    pThread->IncPreventAbort();

    //Ex.RhThrowEx(throwable, &exInfo)
    CRITICAL_CALLSITE;
    CALL_MANAGED_METHOD_NORET(args)

    GCPROTECT_END();
    GCPROTECT_END();

    UNREACHABLE();
}

VOID DECLSPEC_NORETURN DispatchManagedException(OBJECTREF throwable)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    CONTEXT exceptionContext;
    ClrCaptureContext(&exceptionContext);

    DispatchManagedException(throwable, &exceptionContext);
    UNREACHABLE();
}

VOID DECLSPEC_NORETURN DispatchManagedException(RuntimeExceptionKind reKind)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    EEException ex(reKind);
    OBJECTREF throwable = ex.CreateThrowable();

    DispatchManagedException(throwable);
}

VOID DECLSPEC_NORETURN DispatchRethrownManagedException(CONTEXT* pExceptionContext)
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    Thread *pThread = GetThread();

    ExInfo *pActiveExInfo = (ExInfo*)pThread->GetExceptionState()->GetCurrentExceptionTracker();

    ExInfo exInfo(pThread, pActiveExInfo->m_ptrs.ExceptionRecord, pExceptionContext, ExKind::None);

    GCPROTECT_BEGIN(exInfo.m_exception);
    PREPARE_NONVIRTUAL_CALLSITE(METHOD__EH__RH_RETHROW);
    DECLARE_ARGHOLDER_ARRAY(args, 2);

    args[ARGNUM_0] = PTR_TO_ARGHOLDER(pActiveExInfo);
    args[ARGNUM_1] = PTR_TO_ARGHOLDER(&exInfo);

    pThread->IncPreventAbort();

    //Ex.RhRethrow(ref ExInfo activeExInfo, ref ExInfo exInfo)
    CALL_MANAGED_METHOD_NORET(args)
    GCPROTECT_END();

    UNREACHABLE();
}

VOID DECLSPEC_NORETURN DispatchRethrownManagedException()
{
    STATIC_CONTRACT_THROWS;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_MODE_COOPERATIVE;

    CONTEXT exceptionContext;
    ClrCaptureContext(&exceptionContext);

    DispatchRethrownManagedException(&exceptionContext);
}

#ifndef TARGET_UNIX
void ClrUnwindEx(EXCEPTION_RECORD* pExceptionRecord, UINT_PTR ReturnValue, UINT_PTR TargetIP, UINT_PTR TargetFrameSp)
{
    RtlUnwind((PVOID)TargetFrameSp, // TargetFrame
              (PVOID)TargetIP,
              pExceptionRecord,
              (PVOID)ReturnValue);

    // doesn't return
    UNREACHABLE();
}
#endif // !TARGET_UNIX

#if defined(TARGET_WINDOWS) && !defined(TARGET_X86)
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
void FixupDispatcherContext(DISPATCHER_CONTEXT* pDispatcherContext, CONTEXT* pContext, PEXCEPTION_ROUTINE pUnwindPersonalityRoutine = NULL)
{
    if (pContext)
    {
        STRESS_LOG1(LF_EH, LL_INFO10, "FDC: pContext: %p\n", pContext);
        CopyOSContext(pDispatcherContext->ContextRecord, pContext);
    }

    pDispatcherContext->ControlPc = (UINT_PTR) GetIP(pDispatcherContext->ContextRecord);

#if defined(TARGET_ARM64)
    // Since this routine is used to fixup contexts for async exceptions,
    // clear the CONTEXT_UNWOUND_TO_CALL flag since, semantically, frames
    // where such exceptions have happened do not have callsites. On a similar
    // note, also clear out the ControlPcIsUnwound field. Post discussion with
    // AaronGi from the kernel team, it's safe for us to have both of these
    // cleared.
    //
    // The OS will pick this up with the rest of the DispatcherContext state
    // when it processes collided unwind and thus, when our managed personality
    // routine is invoked, ExInfo::InitializeCrawlFrame will adjust
    // ControlPC correctly.
    pDispatcherContext->ContextRecord->ContextFlags &= ~CONTEXT_UNWOUND_TO_CALL;
    pDispatcherContext->ControlPcIsUnwound = FALSE;

    // Also, clear out the debug-registers flag so that when this context is used by the
    // OS, it does not end up setting bogus access breakpoints. The kernel team will also
    // be fixing it at their end, in their implementation of collided unwind.
    pDispatcherContext->ContextRecord->ContextFlags &= ~CONTEXT_DEBUG_REGISTERS;

    // But keep the architecture flag set (its part of CONTEXT_DEBUG_REGISTERS)
    pDispatcherContext->ContextRecord->ContextFlags |= CONTEXT_ARM64;

#endif // TARGET_ARM64

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
HijackHandler(IN     PEXCEPTION_RECORD   pExceptionRecord,
              IN     PVOID               pEstablisherFrame,
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

    FixupDispatcherContext(pDispatcherContext, pNewContext);

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

#endif // TARGET_WINDOWS && !TARGET_X86

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
BOOL IsSafeToUnwindFrameChain(Thread* pThread, LPVOID MemoryStackFpForFrameChain)
{
    // Look for the last Frame to be removed that marks a managed-to-unmanaged transition
    Frame* pLastFrameOfInterest = FRAME_TOP;
    for (Frame* pf = pThread->m_pFrame; pf < MemoryStackFpForFrameChain; pf = pf->PtrNextFrame())
    {
        PCODE retAddr = pf->GetReturnAddress();
        if (retAddr != (PCODE)NULL && ExecutionManager::IsManagedCode(retAddr))
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
    ctx.ContextFlags = CONTEXT_CONTROL;
    SetIP(&ctx, 0);
    SetSP(&ctx, 0);
    FillRegDisplay(&rd, &ctx);
    pLastFrameOfInterest->UpdateRegDisplay(&rd);

    // We're safe only if the managed method will be unwound also
    LPVOID managedSP = dac_cast<PTR_VOID>(GetRegdisplaySP(&rd));

    if (managedSP <= MemoryStackFpForFrameChain)
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
    // ARM and ARM64 it's the caller's stack pointer.  It makes no difference, since there
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
        ExInfo::PopExInfos(pThread, MemoryStackFp);
    }
}

#ifdef TARGET_UNIX

typedef enum
{
    _URC_FATAL_PHASE1_ERROR = 3,
} _Unwind_Reason_Code;
typedef enum
{
} _Unwind_Action;
struct _Unwind_Context;
struct _Unwind_Exception;

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

    DefaultCatchHandler(NULL /*pExceptionInfo*/, NULL /*Throwable*/, TRUE /*useLastThrownObject*/);

    EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
    return _URC_FATAL_PHASE1_ERROR;
}

#else // TARGET_UNIX

EXTERN_C EXCEPTION_DISPOSITION __cdecl
UMThunkUnwindFrameChainHandler(IN     PEXCEPTION_RECORD   pExceptionRecord,
                               IN     PVOID               pEstablisherFrame,
                               IN OUT PCONTEXT            pContextRecord,
                               IN OUT PDISPATCHER_CONTEXT pDispatcherContext
                              )
{
    Thread* pThread = GetThreadNULLOk();
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
        CleanUpForSecondPass(pThread, fIsSO, pEstablisherFrame, pEstablisherFrame);
    }

    // The asm stub put us into COOP mode, but we're about to scan unmanaged call frames
    // so unmanaged filters/handlers/etc can run and we must be in PREEMP mode for that.
    if (pThread->PreemptiveGCDisabled())
    {
        if (fIsSO)
        {
            // We don't have stack to do full-version EnablePreemptiveGC.
            InterlockedAnd((LONG*)&pThread->m_fPreemptiveGCDisabled, 0);
        }
        else
        {
            pThread->EnablePreemptiveGC();
        }
    }

    return ExceptionContinueSearch;
}

EXTERN_C EXCEPTION_DISPOSITION __cdecl
UMEntryPrestubUnwindFrameChainHandler(
                IN     PEXCEPTION_RECORD   pExceptionRecord,
                IN     PVOID               pEstablisherFrame,
                IN OUT PCONTEXT            pContextRecord,
                IN OUT PDISPATCHER_CONTEXT pDispatcherContext
            )
{
    EXCEPTION_DISPOSITION disposition = UMThunkUnwindFrameChainHandler(
                pExceptionRecord,
                pEstablisherFrame,
                pContextRecord,
                pDispatcherContext
                );

    return disposition;
}

// This is the personality routine setup for the assembly helper (CallDescrWorker) that calls into
// managed code.
EXTERN_C EXCEPTION_DISPOSITION __cdecl
CallDescrWorkerUnwindFrameChainHandler(IN     PEXCEPTION_RECORD   pExceptionRecord,
                                       IN     PVOID               pEstablisherFrame,
                                       IN OUT PCONTEXT            pContextRecord,
                                       IN OUT PDISPATCHER_CONTEXT pDispatcherContext
                                      )
{

    Thread* pThread = GetThread();
    if (pExceptionRecord->ExceptionCode == STATUS_STACK_OVERFLOW)
    {
        if (IS_UNWINDING(pExceptionRecord->ExceptionFlags))
        {
            GCX_COOP_NO_DTOR();
            CleanUpForSecondPass(pThread, true, pEstablisherFrame, pEstablisherFrame);
        }

        InterlockedAnd((LONG*)&pThread->m_fPreemptiveGCDisabled, 0);
        // We'll let the SO infrastructure handle this exception... at that point, we
        // know that we'll have enough stack to do it.
    }
    else if (IS_UNWINDING(pExceptionRecord->ExceptionFlags))
    {
        CleanUpForSecondPass(pThread, false, pEstablisherFrame, pEstablisherFrame);
    }

    return ExceptionContinueSearch;
}

#endif // TARGET_UNIX

#ifdef FEATURE_COMINTEROP
EXTERN_C EXCEPTION_DISPOSITION __cdecl
ReverseComUnwindFrameChainHandler(IN     PEXCEPTION_RECORD   pExceptionRecord,
                                  IN     PVOID               pEstablisherFrame,
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

#if !defined(TARGET_UNIX) && !defined(TARGET_X86)
EXTERN_C EXCEPTION_DISPOSITION __cdecl
FixRedirectContextHandler(
                  IN     PEXCEPTION_RECORD   pExceptionRecord,
                  IN     PVOID               pEstablisherFrame,
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

    FixupDispatcherContext(pDispatcherContext, pRedirectedContext);

    // Returning ExceptionCollidedUnwind will cause the OS to take our new context record
    // and dispatcher context and restart the exception dispatching on this call frame,
    // which is exactly the behavior we want in order to restore our thread's unwindability
    // (which was broken when we whacked the IP to get control over the thread)
    return ExceptionCollidedUnwind;
}
#endif // !TARGET_UNIX && !TARGET_X86
#endif // DACCESS_COMPILE

void ExInfo::StackRange::Reset()
{
    LIMITED_METHOD_CONTRACT;

    m_sfLowBound.SetMaxVal();
    m_sfHighBound.Clear();
}

bool ExInfo::StackRange::IsEmpty()
{
    LIMITED_METHOD_CONTRACT;
    return (m_sfLowBound.IsMaxVal() &&
            m_sfHighBound.IsNull());
}

bool ExInfo::StackRange::IsSupersededBy(StackFrame sf)
{
    LIMITED_METHOD_CONTRACT;
    CONSISTENCY_CHECK(IsConsistent());

    return (sf >= m_sfLowBound);
}

void ExInfo::StackRange::CombineWith(StackFrame sfCurrent, StackRange* pPreviousRange)
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
#ifdef TARGET_UNIX
        // When the current range is empty, copy the low bound too. Otherwise a degenerate range would get
        // created and tests for stack frame in the stack range would always fail.
        // TODO: Check if we could enable it for non-PAL as well.
        if (IsEmpty())
        {
            m_sfLowBound = pPreviousRange->m_sfLowBound;
        }
#endif // TARGET_UNIX
        m_sfHighBound = pPreviousRange->m_sfHighBound;
    }
}

bool ExInfo::StackRange::Contains(StackFrame sf)
{
    LIMITED_METHOD_CONTRACT;
    CONSISTENCY_CHECK(IsConsistent());

    return ((m_sfLowBound <= sf) &&
                            (sf <= m_sfHighBound));
}

void ExInfo::StackRange::ExtendUpperBound(StackFrame sf)
{
    LIMITED_METHOD_CONTRACT;
    CONSISTENCY_CHECK(IsConsistent());
    CONSISTENCY_CHECK((sf >= m_sfHighBound));

    m_sfHighBound = sf;
}

void ExInfo::StackRange::ExtendLowerBound(StackFrame sf)
{
    LIMITED_METHOD_CONTRACT;
    CONSISTENCY_CHECK(IsConsistent());
    CONSISTENCY_CHECK((sf <= m_sfLowBound));

    m_sfLowBound = sf;
}

void ExInfo::StackRange::TrimLowerBound(StackFrame sf)
{
    LIMITED_METHOD_CONTRACT;
    CONSISTENCY_CHECK(IsConsistent());
    CONSISTENCY_CHECK(sf >= m_sfLowBound);

    m_sfLowBound = sf;
}

StackFrame ExInfo::StackRange::GetLowerBound()
{
    LIMITED_METHOD_CONTRACT;
    CONSISTENCY_CHECK(IsConsistent());

    return m_sfLowBound;
}

StackFrame ExInfo::StackRange::GetUpperBound()
{
    LIMITED_METHOD_CONTRACT;
    CONSISTENCY_CHECK(IsConsistent());

    return m_sfHighBound;
}

#ifdef _DEBUG
bool ExInfo::StackRange::IsDisjointWithAndLowerThan(StackRange* pOtherRange)
{
    CONSISTENCY_CHECK(IsConsistent());
    CONSISTENCY_CHECK(pOtherRange->IsConsistent());

    return m_sfHighBound < pOtherRange->m_sfLowBound;
}

#endif // _DEBUG


#ifdef _DEBUG
bool ExInfo::StackRange::IsConsistent()
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


// Determine if the given StackFrame is in the stack region unwound by the specified ExInfo.
// This is used by the stackwalker to skip funclets.  Refer to the calls to this method in StackWalkFramesEx()
// for more information.
//
// Effectively, this will make the stackwalker skip all the frames until it reaches the frame
// containing the funclet. Details of the skipping logic are described in the method implementation.
//
// static
bool ExInfo::IsInStackRegionUnwoundBySpecifiedException(CrawlFrame * pCF, PTR_ExInfo pExceptionTracker)
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

    // The new exception handling sets the ranges always to the SP of the unwound frame
    return (sfLowerBound < csfToCheck) && (csfToCheck <= sfUpperBound);
}

// Returns a bool indicating if the specified CrawlFrame has been unwound by the active exception.
bool ExInfo::IsInStackRegionUnwoundByCurrentException(CrawlFrame * pCF)
{
    LIMITED_METHOD_CONTRACT;

    Thread * pThread = pCF->pThread;

    PTR_ExInfo pCurrentTracker = pThread->GetExceptionState()->GetCurrentExceptionTracker();
    return ExInfo::IsInStackRegionUnwoundBySpecifiedException(pCF, pCurrentTracker);
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
bool ExInfo::HasFrameBeenUnwoundByAnyActiveException(CrawlFrame * pCF)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(pCF != NULL);

    // Enumerate all (nested) exception trackers and see if any of them has unwound the
    // specified CrawlFrame.
    Thread * pTargetThread = pCF->pThread;
    bool fHasFrameBeenUnwound = false;

    {
        CallerStackFrame csfToCheck;
        if (pCF->IsFrameless())
        {
            csfToCheck = CallerStackFrame::FromRegDisplay(pCF->GetRegisterSet());
        }
        else
        {
            csfToCheck = CallerStackFrame((UINT_PTR)pCF->GetFrame());
        }
        STRESS_LOG4(LF_EH|LF_GCROOTS, LL_INFO100, "CrawlFrame (%p): Frameless: %s %s: %p\n",
                    pCF, pCF->IsFrameless() ? "Yes" : "No", pCF->IsFrameless() ? "CallerSP" : "Address", csfToCheck.SP);
    }

    PTR_ExInfo pTopExInfo = (PTR_ExInfo)pTargetThread->GetExceptionState()->GetCurrentExceptionTracker();
    for (PTR_ExInfo pCurrentExInfo = pTopExInfo; pCurrentExInfo != NULL; pCurrentExInfo = dac_cast<PTR_ExInfo>(pCurrentExInfo->m_pPrevNestedInfo))
    {
        STRESS_LOG2(LF_EH|LF_GCROOTS, LL_INFO100, "Checking lower bound %p, upper bound %p\n", (void*)pCurrentExInfo->m_ScannedStackRange.GetLowerBound().SP, (void*)pCurrentExInfo->m_ScannedStackRange.GetUpperBound().SP);
        if (ExInfo::IsInStackRegionUnwoundBySpecifiedException(pCF, pCurrentExInfo))
        {
            fHasFrameBeenUnwound = true;
            break;
        }
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
// ExInfo::IsUnwoundToTargetParentFrame().  The comparison logic is very platform-dependent.
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
StackFrame ExInfo::GetStackFrameForParentCheck(CrawlFrame * pCF)
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
bool ExInfo::IsUnwoundToTargetParentFrame(CrawlFrame * pCF, StackFrame sfParent)
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
bool ExInfo::IsUnwoundToTargetParentFrame(StackFrame sfToCheck, StackFrame sfParent)
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
// relies on the ExInfos, which are collapsed in the second pass when a nested exception escapes.
// When this happens, we'll lose information on the funclet represented by the collapsed tracker.
// </WARNING>
//
// Return Value:
// StackFrame.IsNull()   - no skipping is necessary
// StackFrame.IsMaxVal() - skip one frame and then ask again
// Anything else         - skip to the method frame indicated by the return value and ask again
//
// static
StackFrame ExInfo::FindParentStackFrameForStackWalk(CrawlFrame* pCF, bool fForGCReporting /*= false */)
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
        return FindParentStackFrameHelper(pCF, NULL, NULL, fForGCReporting);
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
StackFrame ExInfo::FindParentStackFrameEx(CrawlFrame* pCF,
                                          DWORD*      pParentOffset)
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
    StackFrame sfResult = ExInfo::FindParentStackFrameHelper(pCF, &fRealParent, pParentOffset);

    if (fRealParent)
    {
        // If the enclosing method is the parent method, then we are done.
        return sfResult;
    }
    else
    {
        // Otherwise we need to do a full stackwalk to find the parent method frame.
        // This should only happen if we are calling a filter inside a funclet.
        return ExInfo::RareFindParentStackFrame(pCF, pParentOffset);
    }
}

// static
StackFrame ExInfo::GetCallerSPOfParentOfNonExceptionallyInvokedFunclet(CrawlFrame *pCF)
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
StackFrame ExInfo::FindParentStackFrameHelper(CrawlFrame* pCF,
                                              bool*       pfRealParent,
                                              DWORD*      pParentOffset,
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
    }
    CONTRACTL_END;

    StackFrame sfResult;
    REGDISPLAY* pRegDisplay = pCF->GetRegisterSet();

    // At this point, we need a valid caller SP and the CallerStackFrame::FromRegDisplay
    // asserts that the RegDisplay contains one.
    CallerStackFrame csfCurrent = CallerStackFrame::FromRegDisplay(pRegDisplay);
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
        PCODE callerIP = dac_cast<PCODE>(GetIP(pRegDisplay->pCallerContext));
        BOOL fIsCallerInVM = FALSE;

        // Check if the caller IP is in runtime native code.  If it is not, then it is an out-of-line finally.
        // Normally, the caller of a finally is CallEHFunclet, or InterpreterFrame::DummyCallerIP
        // for the interpreter.
#ifdef TARGET_UNIX
        fIsCallerInVM = !ExecutionManager::IsManagedCode(callerIP);
#else
#if defined(DACCESS_COMPILE)
        PTR_VOID eeBase = DacGlobalBase();
#else  // !DACCESS_COMPILE
        PTR_VOID eeBase = GetClrModuleBase();
#endif // !DACCESS_COMPILE
        fIsCallerInVM = IsIPInModule(eeBase, callerIP);
#endif // TARGET_UNIX

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
                StackFrame sfCallerSPOfFuncletParent = ExInfo::GetCallerSPOfParentOfNonExceptionallyInvokedFunclet(pCF);
                return sfCallerSPOfFuncletParent;
            }
        }
    }

    for (PTR_ExInfo pCurrentExInfo = (PTR_ExInfo)pThread->GetExceptionState()->GetCurrentExceptionTracker();
        pCurrentExInfo != NULL;
        pCurrentExInfo = (PTR_ExInfo)pCurrentExInfo->m_pPrevNestedInfo)
    {
        // Check if the ExInfo has just been created.
        if (pCurrentExInfo->m_ScannedStackRange.IsEmpty())
        {
            continue;
        }

        CallerStackFrame csfFunclet = pCurrentExInfo->m_csfEHClause;
        if (csfCurrent == csfFunclet)
        {
            sfResult = (StackFrame)pCurrentExInfo->m_csfEnclosingClause;
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
StackWalkAction ExInfo::RareFindParentStackFrameCallback(CrawlFrame* pCF, LPVOID pData)
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
        pState->m_sfParent = ExInfo::FindParentStackFrameHelper(pCF, NULL, NULL);
    }

    // If we still need to skip, then continue the stackwalk.
    if (!pState->m_sfParent.IsNull())
    {
        return SWA_CONTINUE;
    }

    // At this point, we are done.
    pState->m_sfParent        = ExInfo::GetStackFrameForParentCheck(pCF);
    pState->m_dwParentOffset  = pCF->GetRelOffset();

    _ASSERTE(pRegDisplay->IsCallerContextValid);
    pState->m_uParentCallerSP = GetSP(pRegDisplay->pCallerContext);

    return SWA_ABORT;
}

// static
StackFrame ExInfo::RareFindParentStackFrame(CrawlFrame* pCF,
                                            DWORD*      pParentOffset)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION( pCF != NULL );
        PRECONDITION( pCF->IsFunclet() );
        PRECONDITION( CheckPointer(pParentOffset, NULL_OK) );
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

    pThread->StackWalkFramesEx(&rd, &ExInfo::RareFindParentStackFrameCallback, &state, 0, pFrame);

    if (pParentOffset != NULL)
    {
        *pParentOffset = state.m_dwParentOffset;
    }
    return state.m_sfParent;
}

ExInfo::StackRange::StackRange()
{
    WRAPPER_NO_CONTRACT;

#ifndef DACCESS_COMPILE
    Reset();
#endif // DACCESS_COMPILE
}

#ifdef DACCESS_COMPILE
void ExInfo::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    // ExInfo is embedded so don't enum 'this'.
    OBJECTHANDLE_EnumMemoryRegions(m_hThrowable);
    m_ptrs.ExceptionRecord.EnumMem();
    m_ptrs.ContextRecord.EnumMem();
}
#endif // DACCESS_COMPILE

#ifndef DACCESS_COMPILE
// Mark the pinvoke frame as invoking CallCatchFunclet (and similar) for collided unwind detection
void MarkInlinedCallFrameAsFuncletCall(Frame* pFrame)
{
    _ASSERTE(pFrame->GetFrameIdentifier() == FrameIdentifier::InlinedCallFrame);
    InlinedCallFrame* pInlinedCallFrame = (InlinedCallFrame*)pFrame;
    pInlinedCallFrame->m_Datum = (PTR_NDirectMethodDesc)((TADDR)pInlinedCallFrame->m_Datum | (TADDR)InlinedCallFrameMarker::ExceptionHandlingHelper | (TADDR)InlinedCallFrameMarker::SecondPassFuncletCaller);
}

// Mark the pinvoke frame as invoking any exception handling helper
void MarkInlinedCallFrameAsEHHelperCall(Frame* pFrame)
{
    _ASSERTE(pFrame->GetFrameIdentifier() == FrameIdentifier::InlinedCallFrame);
    InlinedCallFrame* pInlinedCallFrame = (InlinedCallFrame*)pFrame;
    pInlinedCallFrame->m_Datum = (PTR_NDirectMethodDesc)((TADDR)pInlinedCallFrame->m_Datum | (TADDR)InlinedCallFrameMarker::ExceptionHandlingHelper);
}

static TADDR GetSpForDiagnosticReporting(REGDISPLAY *pRD)
{
#ifdef ESTABLISHER_FRAME_ADDRESS_IS_CALLER_SP
    TADDR sp = CallerStackFrame::FromRegDisplay(pRD).SP;
#if defined(FEATURE_EH_FUNCLETS) && defined(TARGET_X86)
    sp -= sizeof(TADDR); // For X86 with funclets we want the address 1 pointer into the callee.
#endif // defined(FEATURE_EH_FUNCLETS) && defined(TARGET_X86)
    return sp;
#else
    return GetSP(pRD->pCurrentContext);
#endif
}

extern "C" void QCALLTYPE AppendExceptionStackFrame(QCall::ObjectHandleOnStack exceptionObj, SIZE_T ip, SIZE_T sp, int flags, ExInfo *pExInfo)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;

    Thread* pThread = GET_THREAD();

    {
        GCX_COOP_THREAD_EXISTS(pThread);

        Frame* pFrame = pThread->GetFrame();
        MarkInlinedCallFrameAsEHHelperCall(pFrame);

        if ((flags & RH_EH_FIRST_RETHROW_FRAME) == 0)
        {
            MethodDesc *pMD = pExInfo->m_frameIter.m_crawl.GetFunction();
#if _DEBUG
            EECodeInfo codeInfo(ip);
            _ASSERTE(codeInfo.IsValid());
            _ASSERTE(pMD == codeInfo.GetMethodDesc());
#endif // _DEBUG

            StackTraceInfo::AppendElement(pExInfo->m_hThrowable, ip, sp, pMD, &pExInfo->m_frameIter.m_crawl);
        }
    }

    // Notify the debugger that we are on the first pass for a managed exception.
    // Note that this callback is made for every managed frame.
    TADDR spForDebugger = GetSpForDiagnosticReporting(pExInfo->m_frameIter.m_crawl.GetRegisterSet());
    EEToDebuggerExceptionInterfaceWrapper::FirstChanceManagedException(pThread, ip, spForDebugger);

    if (!pExInfo->DeliveredFirstChanceNotification())
    {
        ExceptionNotifications::DeliverFirstChanceNotification();
    }

    END_QCALL;
}

void ExecuteFunctionBelowContext(PCODE functionPtr, CONTEXT *pContext, size_t targetSSP, size_t arg1, size_t arg2)
{
    UINT_PTR targetSp = GetSP(pContext);
#if defined(HOST_AMD64)
    ULONG64* returnAddress = (ULONG64*)(targetSp - 8);
    *returnAddress = pContext->Rip;
#ifdef HOST_WINDOWS
    if (targetSSP != 0)
    {
        targetSSP -= sizeof(size_t);
    }
#endif // HOST_WINDOWS
    SetSP(pContext, targetSp - 8);
#elif defined(HOST_X86)

#ifdef HOST_WINDOWS
    // Disarm the managed code SEH handler installed in CallDescrWorkerInternal
    if (IsCallDescrWorkerInternalReturnAddress(pContext->Eip))
    {
        PEXCEPTION_REGISTRATION_RECORD currentContext = GetCurrentSEHRecord();
        if (currentContext->Handler == (PEXCEPTION_ROUTINE)ProcessCLRException)
            currentContext->Handler = (PEXCEPTION_ROUTINE)CallDescrWorkerUnwindFrameChainHandler;
    }
#endif

    ULONG32* returnAddress = (ULONG32*)(targetSp - 4);
    *returnAddress = pContext->Eip;
    SetSP(pContext, targetSp - 4);
#elif defined(HOST_ARM64)
    pContext->Lr = GetIP(pContext);
#elif defined(HOST_ARM)
    pContext->Lr = GetIP(pContext);
#elif defined(HOST_RISCV64) || defined(HOST_LOONGARCH64)
    pContext->Ra = GetIP(pContext);
#endif

    SetFirstArgReg(pContext, arg1);
    SetSecondArgReg(pContext, arg2);
    SetIP(pContext, functionPtr);

    ClrRestoreNonvolatileContext(pContext, targetSSP);
    UNREACHABLE();
}

#ifdef HOST_WINDOWS
VOID DECLSPEC_NORETURN __fastcall PropagateLongJmpThroughNativeFrames(jmp_buf *pJmpBuf, int retVal)
{
    WRAPPER_NO_CONTRACT;
    GCX_PREEMP_NO_DTOR();
    longjmp(*pJmpBuf, retVal);
    UNREACHABLE();
}

// This is a personality routine that the RtlRestoreContext calls when it is called with
// pExceptionRecord->ExceptionCode == STATUS_UNWIND_CONSOLIDATE.
// Before calling this function, it creates a machine frame that hides all the frames
// upto the frame described by the pContextRecord. This allows us to raise the exception
// from the target context without removing the frames from the stack. Those frames
// can contain e.g. a C++ exception object that needs to be preserved during the exception
// propagation.
#if !defined(HOST_X86)
EXTERN_C EXCEPTION_DISPOSITION
PropagateForeignExceptionThroughNativeFrames(IN     PEXCEPTION_RECORD   pExceptionRecord,
                    IN     PVOID               pEstablisherFrame,
                    IN OUT PCONTEXT            pContextRecord,
                    IN OUT PDISPATCHER_CONTEXT pDispatcherContext
                    )
{
    STATIC_CONTRACT_MODE_COOPERATIVE;
    STATIC_CONTRACT_GC_TRIGGERS;
    STATIC_CONTRACT_THROWS;

    _ASSERTE(pExceptionRecord->NumberParameters == 2);
    EXCEPTION_RECORD *pExceptionToPropagateRecord = (EXCEPTION_RECORD*)pExceptionRecord->ExceptionInformation[1];
    GCX_PREEMP_NO_DTOR();
    RaiseException(pExceptionToPropagateRecord->ExceptionCode, pExceptionToPropagateRecord->ExceptionFlags, pExceptionToPropagateRecord->NumberParameters, pExceptionToPropagateRecord->ExceptionInformation);
    UNREACHABLE();
}
#endif

#endif // HOST_WINDOWS

extern "C" void * QCALLTYPE CallCatchFunclet(QCall::ObjectHandleOnStack exceptionObj, BYTE* pHandlerIP, REGDISPLAY* pvRegDisplay, ExInfo* exInfo)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;
    GCX_COOP_NO_DTOR();

    Thread* pThread = GET_THREAD();
    pThread->DecPreventAbort();

    Frame* pFrame = pThread->GetFrame();
    MarkInlinedCallFrameAsFuncletCall(pFrame);
    exInfo->m_ScannedStackRange.ExtendUpperBound(exInfo->m_frameIter.m_crawl.GetRegisterSet()->SP);
    DWORD_PTR dwResumePC = 0;
    UINT_PTR callerTargetSp = 0;
#if defined(HOST_AMD64) && defined(HOST_WINDOWS)
    size_t targetSSP = exInfo->m_frameIter.m_crawl.GetRegisterSet()->SSP;
    // Verify the SSP points to the slot that matches the ControlPC of the frame containing the catch funclet.
    // But don't check in case the target is in the interpreter loop, because the ControlPC doesn't match the shadow stack entry
    // in that case. The shadow stack contains the return address of the DispatchManagedException call, but the ControlPC is the
    // value captured to the exception context before the DispatchManagedException call.
    _ASSERTE(targetSSP == 0 ||
        (pHandlerIP != NULL) && (exInfo->m_frameIter.m_crawl.GetCodeManager() == ExecutionManager::GetInterpreterCodeManager()) ||
        (*(size_t*)(targetSSP-8) == exInfo->m_frameIter.m_crawl.GetRegisterSet()->ControlPC));
#else
    size_t targetSSP = 0;
#endif

    ICodeManager* pCodeManager = NULL;

    if (pHandlerIP != NULL)
    {
        pCodeManager = exInfo->m_frameIter.m_crawl.GetCodeManager();
#ifdef _DEBUG
        pCodeManager->EnsureCallerContextIsValid(pvRegDisplay);
        _ASSERTE(exInfo->m_sfCallerOfActualHandlerFrame == GetSP(pvRegDisplay->pCallerContext));
#endif
        OBJECTREF throwable = exceptionObj.Get();
        throwable = PossiblyUnwrapThrowable(throwable, exInfo->m_frameIter.m_crawl.GetAssembly());

        exInfo->m_csfEnclosingClause = CallerStackFrame::FromRegDisplay(exInfo->m_frameIter.m_crawl.GetRegisterSet());

        MethodDesc *pMD = exInfo->m_frameIter.m_crawl.GetFunction();
        // Profiler, debugger and ETW events
        TADDR spForDebugger = GetSpForDiagnosticReporting(pvRegDisplay);
        exInfo->MakeCallbacksRelatedToHandler(true, pThread, pMD, &exInfo->m_ClauseForCatch, (DWORD_PTR)pHandlerIP, spForDebugger);

        EH_LOG((LL_INFO100, "Calling catch funclet at %p\n", pHandlerIP));

        dwResumePC = pCodeManager->CallFunclet(throwable, pHandlerIP, pvRegDisplay, exInfo, false /* isFilterFunclet */);

        FixContext(pvRegDisplay->pCurrentContext);

        // Profiler, debugger and ETW events
        exInfo->MakeCallbacksRelatedToHandler(false, pThread, pMD, &exInfo->m_ClauseForCatch, (DWORD_PTR)pHandlerIP, spForDebugger);
        SetIP(pvRegDisplay->pCurrentContext, dwResumePC);
        callerTargetSp = CallerStackFrame::FromRegDisplay(pvRegDisplay).SP;
    }

    UINT_PTR targetSp = GetSP(pvRegDisplay->pCurrentContext);
    PopExplicitFrames(pThread, (void*)targetSp, (void*)callerTargetSp);

    ExInfo* pExInfo = (PTR_ExInfo)pThread->GetExceptionState()->GetCurrentExceptionTracker();

#ifdef HOST_WINDOWS
    jmp_buf* pLongJmpBuf = pExInfo->m_pLongJmpBuf;
    int longJmpReturnValue = pExInfo->m_longJmpReturnValue;
    EXCEPTION_RECORD lastExceptionRecord = *pExInfo->m_ptrs.ExceptionRecord;
#endif // HOST_WINDOWS

#ifdef HOST_UNIX
    Interop::ManagedToNativeExceptionCallback propagateExceptionCallback = pExInfo->m_propagateExceptionCallback;
    void* propagateExceptionContext = pExInfo->m_propagateExceptionContext;
#endif // HOST_UNIX

#ifdef DEBUGGING_SUPPORTED
    // This must be done before we pop the trackers.
    BOOL fIntercepted = pThread->GetExceptionState()->GetFlags()->DebuggerInterceptInfo();
    if (fIntercepted)
    {
        _ASSERTE(pHandlerIP == NULL);
        // retrieve the interception information
        MethodDesc *pInterceptMD = NULL;
        StackFrame sfInterceptStackFrame;
        UINT_PTR    uResumePC = 0;
        ULONG_PTR   ulRelOffset;

        pThread->GetExceptionState()->GetDebuggerState()->GetDebuggerInterceptInfo(&pInterceptMD, NULL, (PBYTE*)&(sfInterceptStackFrame.SP), &ulRelOffset, NULL);
        if (sfInterceptStackFrame.SP == GetSP(pvRegDisplay->pCurrentContext))
        {
            PCODE pStartAddress = pInterceptMD->GetNativeCode();

            EECodeInfo codeInfo(pStartAddress);
            _ASSERTE(codeInfo.IsValid());

            // Note that the value returned for ulRelOffset is actually the offset,
            // so we need to adjust it to get the actual IP.
            _ASSERTE(FitsIn<DWORD>(ulRelOffset));
            uResumePC = codeInfo.GetJitManager()->GetCodeAddressForRelOffset(codeInfo.GetMethodToken(), static_cast<DWORD>(ulRelOffset));

            SetIP(pvRegDisplay->pCurrentContext, uResumePC);
        }
        else
        {
            fIntercepted = FALSE;
        }
    }
#endif // DEBUGGING_SUPPORTED

    ExInfo::PopExInfos(pThread, (void*)targetSp);

    if (!pThread->GetExceptionState()->IsExceptionInProgress())
    {
        pThread->SafeSetLastThrownObject(NULL);
    }

    // Sync managed exception state, for the managed thread, based upon any active exception tracker
    pThread->SyncManagedExceptionState(false);

    ExInfo::UpdateNonvolatileRegisters(pvRegDisplay->pCurrentContext, pvRegDisplay, FALSE);
    if (pHandlerIP != NULL)
    {
        pCodeManager->ResumeAfterCatch(pvRegDisplay->pCurrentContext, targetSSP, fIntercepted);
    }
    else
    {
        if (fIntercepted)
        {
            ClrRestoreNonvolatileContext(pvRegDisplay->pCurrentContext, targetSSP);
        }
#ifdef HOST_UNIX
        if (propagateExceptionCallback)
        {
            // A propagation callback was supplied.
            STRESS_LOG3(LF_EH, LL_INFO100, "Deferring exception propagation to Callback = %p, IP = %p, SP = %p \n", propagateExceptionCallback, GetIP(pvRegDisplay->pCurrentContext), GetSP(pvRegDisplay->pCurrentContext));

            UpdateContextForPropagationCallback(propagateExceptionCallback, propagateExceptionContext, pvRegDisplay->pCurrentContext);
            GCX_PREEMP_NO_DTOR();
            ClrRestoreNonvolatileContext(pvRegDisplay->pCurrentContext, targetSSP);
        }
#endif // HOST_UNIX
        // Throw exception from the caller context

#ifdef HOST_WINDOWS
        if ((pLongJmpBuf == NULL) && !IsComPlusException(&lastExceptionRecord) && MapWin32FaultToCOMPlusException(&lastExceptionRecord) == kSEHException)
        {
#if defined(HOST_X86)
            PopSEHRecords((void *)GetSP(pvRegDisplay->pCurrentContext));
            GCX_PREEMP_NO_DTOR();
            RaiseException(lastExceptionRecord.ExceptionCode, lastExceptionRecord.ExceptionFlags, lastExceptionRecord.NumberParameters, lastExceptionRecord.ExceptionInformation);
            UNREACHABLE();
#else
            // Propagate an external exception to the caller context. This is done in a special way, since the native stack
            // frames below the caller context may contain e.g. C++ exception object that the external exception references.
            // So we rely on a special mode of the RtlRestoreContext with EXCEPTION_RECORD passed in with STATUS_UNWIND_CONSOLIDATE
            // exception code to create a machine frame that hides all the frames upto the caller context before rasing the exception.
            EXCEPTION_RECORD exceptionRecord;
            exceptionRecord.ExceptionCode = STATUS_UNWIND_CONSOLIDATE;
            exceptionRecord.NumberParameters = 2;
            exceptionRecord.ExceptionInformation[0] = (ULONG_PTR)PropagateForeignExceptionThroughNativeFrames;
            exceptionRecord.ExceptionInformation[1] = (ULONG_PTR)&lastExceptionRecord;
            RtlRestoreContext(pvRegDisplay->pCurrentContext, &exceptionRecord);
#endif
        }
#endif // HOST_WINDOWS

#ifdef HOST_WINDOWS
        if (pLongJmpBuf != NULL)
        {
            STRESS_LOG2(LF_EH, LL_INFO100, "Resuming propagation of longjmp through native frames at IP=%p, SP=%p\n", GetIP(pvRegDisplay->pCurrentContext), GetSP(pvRegDisplay->pCurrentContext));
#ifdef HOST_X86
            // On x86 we don't jump to the original longjmp target. Instead we return to DispatchLongJmp called
            // from ProcessCLRException which in turn return ExceptionContinueSearch to continue unwinding the
            // original longjmp call.
            longjmp(*pLongJmpBuf, longJmpReturnValue);
#else
            ExecuteFunctionBelowContext((PCODE)PropagateLongJmpThroughNativeFrames, pvRegDisplay->pCurrentContext, targetSSP, (size_t)pLongJmpBuf, longJmpReturnValue);
#endif
        }
        else
#endif
        {
            STRESS_LOG2(LF_EH, LL_INFO100, "Resuming propagation of managed exception through native frames at IP=%p, SP=%p\n", GetIP(pvRegDisplay->pCurrentContext), GetSP(pvRegDisplay->pCurrentContext));
            ExecuteFunctionBelowContext((PCODE)PropagateExceptionThroughNativeFrames, pvRegDisplay->pCurrentContext, targetSSP, (size_t)OBJECTREFToObject(exceptionObj.Get()));
        }
#undef FIRST_ARG_REG
    }
    END_QCALL;
    return NULL;
}

extern "C" void QCALLTYPE ResumeAtInterceptionLocation(REGDISPLAY* pvRegDisplay)
{
    Thread* pThread = GET_THREAD();
    pThread->DecPreventAbort();

    Frame* pFrame = pThread->GetFrame();
    MarkInlinedCallFrameAsFuncletCall(pFrame);

    UINT_PTR targetSp = GetSP(pvRegDisplay->pCurrentContext);
    ExInfo *pExInfo = (PTR_ExInfo)pThread->GetExceptionState()->GetCurrentExceptionTracker();

    pExInfo->m_ScannedStackRange.ExtendUpperBound(targetSp);

    pExInfo->m_frameIter.m_crawl.GetCodeManager()->EnsureCallerContextIsValid(pvRegDisplay);
    PopExplicitFrames(pThread, (void*)targetSp, (void*)CallerStackFrame::FromRegDisplay(pvRegDisplay).SP);

    // This must be done before we pop the ExInfos.
    BOOL fIntercepted = pThread->GetExceptionState()->GetFlags()->DebuggerInterceptInfo();
    _ASSERTE(fIntercepted);

    // retrieve the interception information
    MethodDesc *pInterceptMD = NULL;
    StackFrame sfInterceptStackFrame;
    UINT_PTR    uResumePC = 0;
    ULONG_PTR   ulRelOffset;

    pThread->GetExceptionState()->GetDebuggerState()->GetDebuggerInterceptInfo(&pInterceptMD, NULL, (PBYTE*)&(sfInterceptStackFrame.SP), &ulRelOffset, NULL);

#if defined(HOST_AMD64) && defined(HOST_WINDOWS)
    TADDR targetSSP = pExInfo->m_frameIter.m_crawl.GetRegisterSet()->SSP;
#else
    TADDR targetSSP = 0;
#endif

    ExInfo::PopExInfos(pThread, (void*)targetSp);

    PCODE pStartAddress = pInterceptMD->GetNativeCode();

    EECodeInfo codeInfo(pStartAddress);
    _ASSERTE(codeInfo.IsValid());

    // Note that the value returned for ulRelOffset is actually the offset,
    // so we need to adjust it to get the actual IP.
    _ASSERTE(FitsIn<DWORD>(ulRelOffset));
    uResumePC = codeInfo.GetJitManager()->GetCodeAddressForRelOffset(codeInfo.GetMethodToken(), static_cast<DWORD>(ulRelOffset));

    SetIP(pvRegDisplay->pCurrentContext, uResumePC);

    STRESS_LOG2(LF_EH, LL_INFO100, "Resuming at interception location at IP=%p, SP=%p\n", uResumePC, GetSP(pvRegDisplay->pCurrentContext));
    ClrRestoreNonvolatileContext(pvRegDisplay->pCurrentContext, targetSSP);
}

extern "C" void QCALLTYPE CallFinallyFunclet(BYTE* pHandlerIP, REGDISPLAY* pvRegDisplay, ExInfo* exInfo)
{
    QCALL_CONTRACT;

    BEGIN_QCALL;
    GCX_COOP();
    Thread* pThread = GET_THREAD();
    pThread->DecPreventAbort();

    Frame* pFrame = pThread->GetFrame();
    MarkInlinedCallFrameAsFuncletCall(pFrame);
    exInfo->m_csfEnclosingClause = CallerStackFrame::FromRegDisplay(exInfo->m_frameIter.m_crawl.GetRegisterSet());
    exInfo->m_ScannedStackRange.ExtendUpperBound(exInfo->m_frameIter.m_crawl.GetRegisterSet()->SP);

    MethodDesc *pMD = exInfo->m_frameIter.m_crawl.GetFunction();
    // Profiler, debugger and ETW events
    TADDR spForDebugger = GetSpForDiagnosticReporting(pvRegDisplay);
    exInfo->MakeCallbacksRelatedToHandler(true, pThread, pMD, &exInfo->m_CurrentClause, (DWORD_PTR)pHandlerIP, spForDebugger);
    EH_LOG((LL_INFO100, "Calling finally funclet at %p\n", pHandlerIP));

    exInfo->m_frameIter.m_crawl.GetCodeManager()->CallFunclet(NULL, pHandlerIP, pvRegDisplay, exInfo, false /* isFilterFunclet */);

    pThread->IncPreventAbort();

    // Profiler, debugger and ETW events
    exInfo->MakeCallbacksRelatedToHandler(false, pThread, pMD, &exInfo->m_CurrentClause, (DWORD_PTR)pHandlerIP, spForDebugger);
    END_QCALL;
}

extern "C" CLR_BOOL QCALLTYPE CallFilterFunclet(QCall::ObjectHandleOnStack exceptionObj, BYTE* pFilterIP, REGDISPLAY* pvRegDisplay)
{
    QCALL_CONTRACT;

    DWORD_PTR dwResult = 0;

    BEGIN_QCALL;
    GCX_COOP();

    Thread* pThread = GET_THREAD();
    Frame* pFrame = pThread->GetFrame();
    MarkInlinedCallFrameAsEHHelperCall(pFrame);

    ExInfo* pExInfo = (ExInfo*)pThread->GetExceptionState()->GetCurrentExceptionTracker();
    OBJECTREF throwable = exceptionObj.Get();
    throwable = PossiblyUnwrapThrowable(throwable, pExInfo->m_frameIter.m_crawl.GetAssembly());

    pExInfo->m_csfEnclosingClause = CallerStackFrame::FromRegDisplay(pExInfo->m_frameIter.m_crawl.GetRegisterSet());
    MethodDesc *pMD = pExInfo->m_frameIter.m_crawl.GetFunction();
    // Profiler, debugger and ETW events
    TADDR spForDebugger = GetSpForDiagnosticReporting(pvRegDisplay);
    pExInfo->MakeCallbacksRelatedToHandler(true, pThread, pMD, &pExInfo->m_CurrentClause, (DWORD_PTR)pFilterIP, spForDebugger);
    EH_LOG((LL_INFO100, "Calling filter funclet at %p\n", pFilterIP));

    EX_TRY
    {
        dwResult = pExInfo->m_frameIter.m_crawl.GetCodeManager()->CallFunclet(throwable, pFilterIP, pvRegDisplay, pExInfo, true /* isFilterFunclet */);
    }
    EX_CATCH
    {
        // Exceptions that occur in the filter funclet are swallowed and the return value is simulated
        // to be false.
        dwResult = EXCEPTION_CONTINUE_SEARCH;
        EH_LOG((LL_INFO100, "Filter funclet has thrown an exception\n"));
    }
    EX_END_CATCH

    // Profiler, debugger and ETW events
    pExInfo->MakeCallbacksRelatedToHandler(false, pThread, pMD, &pExInfo->m_CurrentClause, (DWORD_PTR)pFilterIP, spForDebugger);
    END_QCALL;

    return dwResult == EXCEPTION_EXECUTE_HANDLER;
}


struct ExtendedEHClauseEnumerator : EH_CLAUSE_ENUMERATOR
{
    StackFrameIterator *pFrameIter;
    unsigned EHCount;
};

extern "C" CLR_BOOL QCALLTYPE EHEnumInitFromStackFrameIterator(StackFrameIterator *pFrameIter, IJitManager::MethodRegionInfo* pMethodRegionInfo, EH_CLAUSE_ENUMERATOR * pEHEnum)
{
    QCALL_CONTRACT_NO_GC_TRANSITION;

    ExtendedEHClauseEnumerator *pExtendedEHEnum = (ExtendedEHClauseEnumerator*)pEHEnum;
    pExtendedEHEnum->pFrameIter = pFrameIter;

    IJitManager* pJitMan = pFrameIter->m_crawl.GetJitManager();
    const METHODTOKEN& MethToken = pFrameIter->m_crawl.GetMethodToken();
    pExtendedEHEnum->EHCount = pJitMan->InitializeEHEnumeration(MethToken, pEHEnum);
    EH_LOG((LL_INFO100, "Initialized EH enumeration, %d clauses found\n", pExtendedEHEnum->EHCount));

    if (pExtendedEHEnum->EHCount == 0)
    {
        return FALSE;
    }

    pJitMan->JitTokenToMethodRegionInfo(MethToken, pMethodRegionInfo);
    pFrameIter->UpdateIsRuntimeWrappedExceptions();
    return TRUE;
}

extern "C" CLR_BOOL QCALLTYPE EHEnumNext(EH_CLAUSE_ENUMERATOR* pEHEnum, RhEHClause* pEHClause)
{
    QCALL_CONTRACT;
    CLR_BOOL result = FALSE;

    BEGIN_QCALL;
    Thread* pThread = GET_THREAD();
    Frame* pFrame = pThread->GetFrame();
    MarkInlinedCallFrameAsEHHelperCall(pFrame);

    ExtendedEHClauseEnumerator *pExtendedEHEnum = (ExtendedEHClauseEnumerator*)pEHEnum;
    StackFrameIterator *pFrameIter = pExtendedEHEnum->pFrameIter;

    while (pEHEnum->iCurrentPos < pExtendedEHEnum->EHCount)
    {
        IJitManager* pJitMan   = pFrameIter->m_crawl.GetJitManager();
        const METHODTOKEN& MethToken = pFrameIter->m_crawl.GetMethodToken();

        EE_ILEXCEPTION_CLAUSE EHClause;
        memset(&EHClause, 0, sizeof(EE_ILEXCEPTION_CLAUSE));
        PTR_EXCEPTION_CLAUSE_TOKEN pEHClauseToken = pJitMan->GetNextEHClause(pEHEnum, &EHClause);
        Thread* pThread = GET_THREAD();
        ExInfo* pExInfo = (ExInfo*)pThread->GetExceptionState()->GetCurrentExceptionTracker();
        pExInfo->m_CurrentClause = EHClause;

        pEHClause->_tryStartOffset = EHClause.TryStartPC;
        pEHClause->_tryEndOffset = EHClause.TryEndPC;
        // TODO-NewEH: The GetCodeAddressForRelOffset is expensive when the code is hot/cold split. Postpone this to
        // later when we really need the address.
        if (IsFilterHandler(&EHClause))
        {
            pEHClause->_filterAddress =  (BYTE*)pJitMan->GetCodeAddressForRelOffset(MethToken, EHClause.FilterOffset);
        }
        pEHClause->_handlerAddress = (BYTE*)pJitMan->GetCodeAddressForRelOffset(MethToken, EHClause.HandlerStartPC);

        result = TRUE;
        pEHClause->_isSameTry = (EHClause.Flags & COR_ILEXCEPTION_CLAUSE_SAMETRY) != 0;

        // Clear special flags - like COR_ILEXCEPTION_CLAUSE_CACHED_CLASS
        ULONG flags = (CorExceptionFlag)(EHClause.Flags & 0x0f);

        EH_LOG((LL_INFO100, "EHEnumNext: [0x%x..0x%x)%s, handler=%p\n",
            pEHClause->_tryStartOffset, pEHClause->_tryEndOffset,
            pEHClause->_isSameTry ? ", isSameTry" : "", pEHClause->_handlerAddress));

        if (flags == COR_ILEXCEPTION_CLAUSE_NONE)
        {
            pEHClause->_clauseKind = RH_EH_CLAUSE_TYPED;
            pEHClause->_pTargetType = pJitMan->ResolveEHClause(&EHClause, &pFrameIter->m_crawl).AsMethodTable();
            EH_LOG((LL_INFO100, " typed clause, target type=%p (%s)\n",
                pEHClause->_pTargetType, ((MethodTable*)pEHClause->_pTargetType)->GetDebugClassName()));
        }
        else if (flags & COR_ILEXCEPTION_CLAUSE_FILTER)
        {
            pEHClause->_clauseKind = RH_EH_CLAUSE_FILTER;
            EH_LOG((LL_INFO100, " filter clause, filter=%p\n", pEHClause->_filterAddress));
        }
        else if (flags & COR_ILEXCEPTION_CLAUSE_FINALLY)
        {
            pEHClause->_clauseKind = RH_EH_CLAUSE_FAULT;
            EH_LOG((LL_INFO100, " finally clause\n"));
        }
        else if (flags & COR_ILEXCEPTION_CLAUSE_FAULT)
        {
            EH_LOG((LL_INFO100, " fault clause\n"));
            pEHClause->_clauseKind = RH_EH_CLAUSE_FAULT;
        }
        else
        {
            EH_LOG((LL_INFO100, " unknown clause\n"));
            result = FALSE;
        }
#ifdef HOST_WINDOWS
        // When processing longjmp, only finally clauses are considered.
        if ((pExInfo->m_pLongJmpBuf == NULL) || (flags & COR_ILEXCEPTION_CLAUSE_FINALLY) || (flags & COR_ILEXCEPTION_CLAUSE_FAULT))
#endif // HOST_WINDOWS
        {
            break;
        }
#ifdef HOST_WINDOWS
        else
        {
            EH_LOG((LL_INFO100, "EHEnumNext: clause skipped due to longjmp processing\n"));
        }
#endif // HOST_WINDOWS
    }
    END_QCALL;

    return result;
}

extern uint32_t g_exceptionCount;

MethodDesc * GetUserMethodForILStub(Thread * pThread, UINT_PTR uStubSP, MethodDesc * pILStubMD, Frame ** ppFrameOut);

static CLR_BOOL CheckExceptionInterception(StackFrameIterator* pStackFrameIterator, ExInfo *pExInfo)
{
    // check if the exception is intercepted.
    CLR_BOOL isIntercepted = FALSE;
    if (pExInfo->m_ExceptionFlags.DebuggerInterceptInfo())
    {
        MethodDesc *pMD = pStackFrameIterator->m_crawl.GetFunction();
        MethodDesc* pInterceptMD = NULL;
        StackFrame sfInterceptStackFrame;

        // check if we have reached the interception point yet
        pExInfo->m_DebuggerExState.GetDebuggerInterceptInfo(&pInterceptMD, NULL,
            reinterpret_cast<PBYTE *>(&(sfInterceptStackFrame.SP)),
            NULL, NULL);

        TADDR spForDebugger = GetRegdisplaySP(pStackFrameIterator->m_crawl.GetRegisterSet());

        if ((pExInfo->m_passNumber == 1) ||
            ((pInterceptMD == pMD) && (sfInterceptStackFrame == spForDebugger)))
        {
            isIntercepted = TRUE;
        }
    }

    return isIntercepted;
}

void FailFastIfCorruptingStateException(ExInfo *pExInfo)
{
    // Failfast if exception indicates corrupted process state
    if (IsProcessCorruptedStateException(pExInfo->m_ExceptionCode, pExInfo->GetThrowable()))
    {
        OBJECTREF oThrowable = NULL;
        SString message;

        GCPROTECT_BEGIN(oThrowable);
        oThrowable = pExInfo->GetThrowable();
        if (oThrowable != NULL)
        {
            EX_TRY
            {
                GetExceptionMessage(oThrowable, message);
            }
            EX_CATCH
            {
            }
            EX_END_CATCH
        }
        GCPROTECT_END();

        EEPolicy::HandleFatalError(pExInfo->m_ExceptionCode, 0, (LPCWSTR)message, dac_cast<EXCEPTION_POINTERS*>(&pExInfo->m_ptrs));
    }
}

static bool IsTopmostDebuggerU2MCatchHandlerFrame(Frame *pFrame)
{
    return (pFrame->GetFrameIdentifier() == FrameIdentifier::DebuggerU2MCatchHandlerFrame) && (pFrame->PtrNextFrame() == FRAME_TOP);
}

static void NotifyExceptionPassStarted(StackFrameIterator *pThis, Thread *pThread, ExInfo *pExInfo)
{
    if (pExInfo->m_passNumber == 1)
    {
        GCX_COOP();
        pThread->SafeSetThrowables(pExInfo->m_exception);
        FirstChanceExceptionNotification();
        EEToProfilerExceptionInterfaceWrapper::ExceptionThrown(pThread);
    }
    else // pExInfo->m_passNumber == 2
    {
        REGDISPLAY* pRD = &pExInfo->m_regDisplay;

        // Clear the enclosing clause to indicate we have not processed any 2nd pass funclet yet.
        pExInfo->m_csfEnclosingClause.Clear();
        if (pExInfo->m_idxCurClause != 0xffffffff) //  the reverse pinvoke case doesn't have the m_idxCurClause set
        {
            pExInfo->m_frameIter.m_crawl.GetCodeManager()->EnsureCallerContextIsValid(pRD, NULL);
            pExInfo->m_sfCallerOfActualHandlerFrame = CallerStackFrame::FromRegDisplay(pRD);

            // the 1st pass has just ended, so the m_CurrentClause is the catch clause
            pExInfo->m_ClauseForCatch = pExInfo->m_CurrentClause;

            MethodDesc *pMD = pExInfo->m_frameIter.m_crawl.GetFunction();
            TADDR sp = GetRegdisplaySP(pRD);
            if (pMD->IsILStub())
            {
                MethodDesc * pUserMDForILStub = NULL;
                Frame * pILStubFrame = NULL;
                if (!pExInfo->m_frameIter.m_crawl.IsFunclet())    // only make this callback on the main method body of IL stubs
                {
                    pUserMDForILStub = GetUserMethodForILStub(pThread, sp, pMD, &pILStubFrame);
                }
                //
                // NotifyOfCHFFilter has two behaviors
                //  * Notifify debugger, get interception info and unwind (function will not return)
                //          In this case, m_sfResumeStackFrame is expected to be NULL or the frame of interception.
                //          We NULL it out because we get the interception event after this point.
                //  * Notifify debugger and return.
                //      In this case the normal EH proceeds and we need to reset m_sfResumeStackFrame to the sf catch handler.
                EEToDebuggerExceptionInterfaceWrapper::NotifyOfCHFFilter((EXCEPTION_POINTERS *)&pExInfo->m_ptrs, pILStubFrame);
            }
            else
            {
                BEGIN_PROFILER_CALLBACK(CORProfilerTrackExceptions());
                _ASSERTE(pExInfo->m_pMDToReportFunctionLeave != NULL);
                EEToProfilerExceptionInterfaceWrapper::ExceptionSearchCatcherFound(pMD);
                if (pExInfo->m_pMDToReportFunctionLeave != NULL)
                {
                    EEToProfilerExceptionInterfaceWrapper::ExceptionSearchFunctionLeave(pExInfo->m_pMDToReportFunctionLeave);
                    pExInfo->m_pMDToReportFunctionLeave = NULL;
                }
                END_PROFILER_CALLBACK();

                // We don't need to do anything special for continuable exceptions after calling
                // this callback.  We are going to start unwinding anyway.
                PCODE uMethodStartPC = pExInfo->m_frameIter.m_crawl.GetCodeInfo()->GetStartAddress();
                TADDR spForDebugger = GetSpForDiagnosticReporting(pRD);
                EEToDebuggerExceptionInterfaceWrapper::FirstChanceManagedExceptionCatcherFound(pThread, pMD, (TADDR) uMethodStartPC, spForDebugger,
                                                                                               &pExInfo->m_ClauseForCatch);
            }
            pExInfo->m_ExceptionFlags.SetUnwindHasStarted();
            EEToDebuggerExceptionInterfaceWrapper::ManagedExceptionUnwindBegin(pThread);
        }
        else
        {
            // The debugger explicitly checks that the notification refers to a FuncEvalFrame in case an exception becomes unhandled in a func eval.
            // We need to do the notification here before we start propagating the exception through native frames, since that will remove
            // all managed frames from the stack and the debugger would not see the failure location.
            if (pThis->GetFrameState() == StackFrameIterator::SFITER_FRAME_FUNCTION)
            {
                Frame* pFrame = pThis->m_crawl.GetFrame();
                // If the frame is ProtectValueClassFrame, move to the next one as we want to report the FuncEvalFrame
                if (pFrame->GetFrameIdentifier() == FrameIdentifier::ProtectValueClassFrame)
                {
                    pFrame = pFrame->PtrNextFrame();
                    _ASSERTE(pFrame != FRAME_TOP);
                }
                if ((pFrame->GetFrameIdentifier() == FrameIdentifier::FuncEvalFrame) || IsTopmostDebuggerU2MCatchHandlerFrame(pFrame))
                {
                    EEToDebuggerExceptionInterfaceWrapper::NotifyOfCHFFilter((EXCEPTION_POINTERS *)&pExInfo->m_ptrs, pFrame);
                }
            }
        }
    }
}

NOINLINE static void NotifyFunctionEnterHelper(StackFrameIterator *pThis, Thread *pThread, ExInfo *pExInfo)
{
    MethodDesc *pMD = pThis->m_crawl.GetFunction();

    if (pExInfo->m_passNumber == 1)
    {
        if (pExInfo->m_pMDToReportFunctionLeave != NULL)
        {
            EEToProfilerExceptionInterfaceWrapper::ExceptionSearchFunctionLeave(pExInfo->m_pMDToReportFunctionLeave);
        }
        EEToProfilerExceptionInterfaceWrapper::ExceptionSearchFunctionEnter(pMD);
    }
    else
    {
        if (pExInfo->m_pMDToReportFunctionLeave != NULL)
        {
            EEToProfilerExceptionInterfaceWrapper::ExceptionUnwindFunctionLeave(pExInfo->m_pMDToReportFunctionLeave);
        }
        EEToProfilerExceptionInterfaceWrapper::ExceptionUnwindFunctionEnter(pMD);
    }

    pExInfo->m_pMDToReportFunctionLeave = pMD;
}

static void NotifyFunctionEnter(StackFrameIterator *pThis, Thread *pThread, ExInfo *pExInfo)
{
    BEGIN_PROFILER_CALLBACK(CORProfilerTrackExceptions());
    // We don't need to do any notifications for the profiler if we are not tracking exceptions.
    NotifyFunctionEnterHelper(pThis, pThread, pExInfo);
    END_PROFILER_CALLBACK();
}

extern "C" CLR_BOOL QCALLTYPE SfiInit(StackFrameIterator* pThis, CONTEXT* pStackwalkCtx, CLR_BOOL instructionFault, CLR_BOOL* pfIsExceptionIntercepted)
{
    QCALL_CONTRACT;

    CLR_BOOL result = FALSE;
    Thread* pThread = GET_THREAD();
    ExInfo* pExInfo = (ExInfo*)pThread->GetExceptionState()->GetCurrentExceptionTracker();

    pThread->ResetThreadStateNC(Thread::TSNC_ProcessedUnhandledException);

    BEGIN_QCALL;

    Frame* pFrame = pThread->GetFrame();
    MarkInlinedCallFrameAsEHHelperCall(pFrame);

    // we already fixed the context in HijackHandler, so let's
    // just clear the thread state.
    pThread->ResetThrowControlForThread();

    pFrame = pExInfo->m_pInitialFrame;

    NotifyExceptionPassStarted(pThis, pThread, pExInfo);

    REGDISPLAY* pRD = &pExInfo->m_regDisplay;
    pThread->FillRegDisplay(pRD, pStackwalkCtx);

    new (pThis) StackFrameIterator();
    result = pThis->Init(pThread, pFrame, pRD, THREAD_EXECUTING_MANAGED_CODE | UNWIND_FLOATS | NOTIFY_ON_U2M_TRANSITIONS) != FALSE;

    if (result && (pExInfo->m_passNumber == 1))
    {
        GCX_COOP();
        UpdatePerformanceMetrics(&pThis->m_crawl, false, ((uint8_t)pExInfo->m_kind & (uint8_t)ExKind::RethrowFlag) == 0);

        FailFastIfCorruptingStateException(pExInfo);
    }

    // Walk the stack until it finds the first managed method
    while (result && pThis->GetFrameState() != StackFrameIterator::SFITER_FRAMELESS_METHOD)
    {
        // In the first pass, add all explicit frames that have a managed function to the exception stack trace
        if (pExInfo->m_passNumber == 1)
        {
            Frame *pFrame = pThis->m_crawl.GetFrame();
            if (pFrame != FRAME_TOP)
            {
                MethodDesc *pMD = pFrame->GetFunction();
                if (pMD != NULL)
                {
                    GCX_COOP();
                    StackTraceInfo::AppendElement(pExInfo->m_hThrowable, 0, GetRegdisplaySP(pExInfo->m_frameIter.m_crawl.GetRegisterSet()), pMD, &pExInfo->m_frameIter.m_crawl);

#if defined(DEBUGGING_SUPPORTED)
                    if (NotifyDebuggerOfStub(pThread, pFrame))
                    {
                        if (!pExInfo->DeliveredFirstChanceNotification())
                        {
                            ExceptionNotifications::DeliverFirstChanceNotification();
                        }
                    }
#endif // DEBUGGING_SUPPORTED
                }
            }
        }
        else // pass number 2
        {
            if (pThis->GetFrameState() == StackFrameIterator::SFITER_SKIPPED_FRAME_FUNCTION)
            {
                // Update context pointers using the skipped frame. This is needed when exception handling continues
                // from ProcessCLRExceptionNew, since the RtlUnwind doesn't maintain context pointers.
                // We explicitly don't do that for inlined frames as it would modify the PC/SP to point to
                // a slightly different location in the managed code calling the pinvoke and the inlined
                // call frame doesn't update the context pointers anyways.
                Frame *pSkippedFrame = pThis->m_crawl.GetFrame();
                if (pSkippedFrame->NeedsUpdateRegDisplay() && (pSkippedFrame->GetFrameIdentifier() != FrameIdentifier::InlinedCallFrame))
                {
                    pSkippedFrame->UpdateRegDisplay(pThis->m_crawl.GetRegisterSet());
                }
            }
        }
        StackWalkAction retVal = pThis->Next();
        result = (retVal != SWA_FAILED);
    }

    NotifyFunctionEnter(pThis, pThread, pExInfo);

    pExInfo->m_ScannedStackRange.ExtendLowerBound(GetRegdisplaySP(pThis->m_crawl.GetRegisterSet()));

    pThis->ResetNextExInfoForSP(pThis->m_crawl.GetRegisterSet()->SP);

    _ASSERTE(!result || pThis->GetFrameState() == StackFrameIterator::SFITER_FRAMELESS_METHOD);

    END_QCALL;

    if (result)
    {
        TADDR controlPC = pThis->m_crawl.GetRegisterSet()->ControlPC;

#if defined(HOST_AMD64) && defined(HOST_WINDOWS)
        // Get the SSP for the first managed frame. It is incremented during the stack walk so that
        // when we reach the handling frame, it contains correct SSP to set when resuming after
        // the catch handler.
        // For hardware exceptions and thread abort exceptions propagated from ThrowControlForThread,
        // the SSP is already known. For other cases, find it by scanning the shadow stack.
        if ((pExInfo->m_passNumber == 2) && (pThis->m_crawl.GetRegisterSet()->SSP == 0))
        {
            pThis->m_crawl.GetCodeInfo()->GetCodeManager()->UpdateSSP(pThis->m_crawl.GetRegisterSet());
        }
#endif

        if (!pThis->m_crawl.HasFaulted() && !pThis->m_crawl.IsIPadjusted())
        {
            controlPC -= STACKWALK_CONTROLPC_ADJUST_OFFSET;
        }
        pThis->SetAdjustedControlPC(controlPC);

        *pfIsExceptionIntercepted = CheckExceptionInterception(pThis, pExInfo);
        EH_LOG((LL_INFO100, "SfiInit (pass %d): Exception stack walking starting at IP=%p, SP=%p, method %s::%s\n",
            pExInfo->m_passNumber, controlPC, GetRegdisplaySP(pThis->m_crawl.GetRegisterSet()),
            pThis->m_crawl.GetFunction()->m_pszDebugClassName, pThis->m_crawl.GetFunction()->m_pszDebugMethodName));
    }
    else
    {
        EH_LOG((LL_INFO100, "SfiInit: No more managed frames found on stack\n"));
        // There are no managed frames on the stack, fail fast and report unhandled exception
        LONG disposition = InternalUnhandledExceptionFilter_Worker((EXCEPTION_POINTERS *)&pExInfo->m_ptrs);
#ifdef HOST_WINDOWS
        CreateCrashDumpIfEnabled(/* fSOException */ FALSE);
        GetThread()->SetThreadStateNC(Thread::TSNC_ProcessedUnhandledException);
        RaiseException(pExInfo->m_ExceptionCode, EXCEPTION_NONCONTINUABLE, pExInfo->m_ptrs.ExceptionRecord->NumberParameters, pExInfo->m_ptrs.ExceptionRecord->ExceptionInformation);
#else
        CrashDumpAndTerminateProcess(pExInfo->m_ExceptionCode);
#endif
    }

    return result;
}

static StackWalkAction MoveToNextNonSkippedFrame(StackFrameIterator* pStackFrameIterator)
{
    StackWalkAction retVal;

    do
    {
        retVal = pStackFrameIterator->Next();
        if (retVal == SWA_FAILED)
        {
            break;
        }
    }
    while (pStackFrameIterator->GetFrameState() == StackFrameIterator::SFITER_SKIPPED_FRAME_FUNCTION);

    return retVal;
}

extern "C" CLR_BOOL QCALLTYPE SfiNext(StackFrameIterator* pThis, uint* uExCollideClauseIdx, CLR_BOOL* fUnwoundReversePInvoke, CLR_BOOL* pfIsExceptionIntercepted)
{
    QCALL_CONTRACT;

    StackWalkAction retVal = SWA_FAILED;
    CLR_BOOL isPropagatingToNativeCode = FALSE;
    Thread* pThread = GET_THREAD();
    ExInfo* pTopExInfo = (ExInfo*)pThread->GetExceptionState()->GetCurrentExceptionTracker();

    BEGIN_QCALL;

    Frame* pFrame = pThread->GetFrame();
    MarkInlinedCallFrameAsEHHelperCall(pFrame);

    // we already fixed the context in HijackHandler, so let's
    // just clear the thread state.
    pThread->ResetThrowControlForThread();

    ExInfo* pExInfo = pThis->GetNextExInfo();
    bool isCollided = false;

    bool doingFuncletUnwind = pThis->m_crawl.IsFunclet();
    PCODE preUnwindControlPC = pThis->m_crawl.GetRegisterSet()->ControlPC;

    retVal = pThis->Next();
    if (retVal == SWA_FAILED)
    {
        EH_LOG((LL_INFO100, "SfiNext (pass=%d): failed to get next frame", pTopExInfo->m_passNumber));
        goto Exit;
    }

#ifdef FEATURE_INTERPRETER
    if ((pThis->GetFrameState() == StackFrameIterator::SFITER_NATIVE_MARKER_FRAME) &&
        (GetIP(pThis->m_crawl.GetRegisterSet()->pCurrentContext) == InterpreterFrame::DummyCallerIP))
    {
        // The callerIP is InterpreterFrame::DummyCallerIP when we are going to unwind from the first interpreted frame belonging to an InterpreterFrame.
        // That means it is at a transition where non-interpreted code called interpreted one.
        // Move the stack frame iterator to the InterpreterFrame and extract the IP of the real caller of the interpreted code.
        retVal = pThis->Next();
        _ASSERTE(retVal != SWA_FAILED);
        _ASSERTE(pThis->m_crawl.GetFrame()->GetFrameIdentifier() == FrameIdentifier::InterpreterFrame);
        // Move to the caller of the interpreted code
        retVal = pThis->Next();
        _ASSERTE(retVal != SWA_FAILED);
    }
#endif // FEATURE_INTERPRETER

    // Check for reverse pinvoke or CallDescrWorkerInternal.
    if (pThis->GetFrameState() == StackFrameIterator::SFITER_NATIVE_MARKER_FRAME)
    {
        EECodeInfo codeInfo(preUnwindControlPC);
#ifdef USE_GC_INFO_DECODER
        GcInfoDecoder gcInfoDecoder(codeInfo.GetGCInfoToken(), DECODE_REVERSE_PINVOKE_VAR);
        isPropagatingToNativeCode = gcInfoDecoder.GetReversePInvokeFrameStackSlot() != NO_REVERSE_PINVOKE_FRAME;
#else // USE_GC_INFO_DECODER
        hdrInfo *hdrInfoBody;
        codeInfo.DecodeGCHdrInfo(&hdrInfoBody);
        isPropagatingToNativeCode = hdrInfoBody->revPInvokeOffset != INVALID_REV_PINVOKE_OFFSET;
#endif // USE_GC_INFO_DECODER
        bool isPropagatingToExternalNativeCode = false;

        EH_LOG((LL_INFO100, "SfiNext: reached native frame at IP=%p, SP=%p, isPropagatingToNativeCode=%d\n",
            GetIP(pThis->m_crawl.GetRegisterSet()->pCurrentContext), GetSP(pThis->m_crawl.GetRegisterSet()->pCurrentContext), isPropagatingToNativeCode));

        if (isPropagatingToNativeCode)
        {
#ifdef HOST_UNIX
            void* callbackCxt = NULL;
            Interop::ManagedToNativeExceptionCallback callback = Interop::GetPropagatingExceptionCallback(
                &codeInfo,
                pTopExInfo->m_hThrowable,
                &callbackCxt);

            if (callback != NULL)
            {
                pTopExInfo->m_propagateExceptionCallback = callback;
                pTopExInfo->m_propagateExceptionContext = callbackCxt;
            }
            else
            {
                isPropagatingToExternalNativeCode = true;
            }
#endif // HOST_UNIX
        }
        else
        {
            if (IsCallDescrWorkerInternalReturnAddress(GetIP(pThis->m_crawl.GetRegisterSet()->pCurrentContext)))
            {
                EH_LOG((LL_INFO100, "SfiNext: the native frame is CallDescrWorkerInternal"));
                isPropagatingToNativeCode = TRUE;
            }
            else if (doingFuncletUnwind && codeInfo.GetJitManager()->IsFilterFunclet(&codeInfo))
            {
                EH_LOG((LL_INFO100, "SfiNext: current frame is filter funclet"));
                isPropagatingToNativeCode = TRUE;
            }
            else
            {
                isPropagatingToExternalNativeCode = true;
            }
        }

        if (isPropagatingToNativeCode)
        {
            pFrame = pThis->m_crawl.GetFrame();

            // Check if there are any further managed frames on the stack or a catch for all exceptions in native code (marked by
            // DebuggerU2MCatchHandlerFrame with CatchesAllExceptions() returning true).
            // If not, the exception is unhandled.
            bool isNotHandledByRuntime = 
                (pFrame == FRAME_TOP) ||
                (IsTopmostDebuggerU2MCatchHandlerFrame(pFrame) && !((DebuggerU2MCatchHandlerFrame*)pFrame)->CatchesAllExceptions())
#ifdef HOST_UNIX
                // Don't allow propagating exceptions from managed to non-runtime native code
                || isPropagatingToExternalNativeCode
#endif
                ;

            if (isNotHandledByRuntime && IsExceptionFromManagedCode(pTopExInfo->m_ptrs.ExceptionRecord))
            {
                EH_LOG((LL_INFO100, "SfiNext (pass %d): no more managed frames on the stack, the exception is unhandled", pTopExInfo->m_passNumber));
                if (pTopExInfo->m_passNumber == 1)
                {
                    LONG disposition = InternalUnhandledExceptionFilter_Worker((EXCEPTION_POINTERS *)&pTopExInfo->m_ptrs);
#ifdef HOST_WINDOWS
                    CreateCrashDumpIfEnabled(/* fSOException */ FALSE);
#endif
                }
                else
                {
#ifdef HOST_WINDOWS
                    GetThread()->SetThreadStateNC(Thread::TSNC_ProcessedUnhandledException);
                    RaiseException(pTopExInfo->m_ExceptionCode, EXCEPTION_NONCONTINUABLE, pTopExInfo->m_ptrs.ExceptionRecord->NumberParameters, pTopExInfo->m_ptrs.ExceptionRecord->ExceptionInformation);
#else
                    CrashDumpAndTerminateProcess(pTopExInfo->m_ExceptionCode);
#endif
                }
            }

            // Unwind to the caller of the managed code
            retVal = pThis->Next();
            _ASSERTE(retVal != SWA_FAILED);
            _ASSERTE(pThis->GetFrameState() != StackFrameIterator::SFITER_SKIPPED_FRAME_FUNCTION);
            goto Exit;
        }
    }

    do
    {
        *uExCollideClauseIdx = 0xffffffff;
        if (pThis->GetFrameState() == StackFrameIterator::SFITER_DONE)
        {
            EH_LOG((LL_INFO100, "SfiNext (pass=%d): no more managed frames found on stack", pTopExInfo->m_passNumber));
            retVal = SWA_FAILED;
            goto Exit;
        }

        if (!pThis->m_crawl.IsFrameless())
        {
            // Detect collided unwind
            pFrame = pThis->m_crawl.GetFrame();

            if (InlinedCallFrame::FrameHasActiveCall(pFrame))
            {
                InlinedCallFrame* pInlinedCallFrame = (InlinedCallFrame*)pFrame;
                if (((TADDR)pInlinedCallFrame->m_Datum & (TADDR)InlinedCallFrameMarker::Mask) == ((TADDR)InlinedCallFrameMarker::ExceptionHandlingHelper | (TADDR)InlinedCallFrameMarker::SecondPassFuncletCaller))
                {
                    // passing through CallCatchFunclet et al
                    if (doingFuncletUnwind)
                    {
                        // Unwind the CallCatchFunclet
                        retVal = MoveToNextNonSkippedFrame(pThis);

                        if (retVal == SWA_FAILED)
                        {
                            _ASSERTE_MSG(FALSE, "StackFrameIterator::Next failed");
                            break;
                        }

                        if ((pThis->GetNextExInfo()->m_passNumber == 1) ||
                            (pThis->GetNextExInfo()->m_idxCurClause == 0xFFFFFFFF))
                        {
                            _ASSERTE_MSG(FALSE, "did not expect to collide with a 1st-pass ExInfo during a EH stackwalk");
                            EEPOLICY_HANDLE_FATAL_ERROR(COR_E_EXECUTIONENGINE);
                        }
                        else
                        {
                            *uExCollideClauseIdx = pExInfo->m_idxCurClause;
                            isCollided = true;
                            pExInfo->m_kind = (ExKind)((uint8_t)pExInfo->m_kind | (uint8_t)ExKind::SupersededFlag);

                            // Unwind to the frame of the prevExInfo
                            ExInfo* pPrevExInfo = pThis->GetNextExInfo();
                            EH_LOG((LL_INFO100, "SfiNext: collided with previous exception handling, skipping from IP=%p, SP=%p to IP=%p, SP=%p\n",
                                    GetControlPC(&pTopExInfo->m_regDisplay), GetRegdisplaySP(&pTopExInfo->m_regDisplay),
                                    GetControlPC(&pPrevExInfo->m_regDisplay), GetRegdisplaySP(&pPrevExInfo->m_regDisplay)));

                            pThis->SkipTo(&pPrevExInfo->m_frameIter);
                            pThis->ResetNextExInfoForSP(pThis->m_crawl.GetRegisterSet()->SP);
                            _ASSERTE_MSG(pThis->GetFrameState() == StackFrameIterator::SFITER_FRAMELESS_METHOD, "Collided unwind should have reached a frameless method");
                            break;
                        }
                    }
                }
            }
            else
            {
                if (pTopExInfo->m_passNumber == 1)
                {
                    MethodDesc *pMD = pFrame->GetFunction();
                    if (pMD != NULL)
                    {
                        GCX_COOP();
                        StackTraceInfo::AppendElement(pTopExInfo->m_hThrowable, 0, GetRegdisplaySP(pTopExInfo->m_frameIter.m_crawl.GetRegisterSet()), pMD, &pTopExInfo->m_frameIter.m_crawl);

#if defined(DEBUGGING_SUPPORTED)
                        if (NotifyDebuggerOfStub(pThread, pFrame))
                        {
                            if (!pTopExInfo->DeliveredFirstChanceNotification())
                            {
                                ExceptionNotifications::DeliverFirstChanceNotification();
                            }
                        }
#endif // DEBUGGING_SUPPORTED
                    }
                }
            }

            retVal = pThis->Next();
            doingFuncletUnwind = false;
        }
    }
    while (retVal != SWA_FAILED && (pThis->GetFrameState() != StackFrameIterator::SFITER_FRAMELESS_METHOD));

    _ASSERTE(retVal == SWA_FAILED || pThis->GetFrameState() == StackFrameIterator::SFITER_FRAMELESS_METHOD);

    if (retVal == SWA_FAILED)
    {
        EH_LOG((LL_INFO100, "SfiNext (pass=%d): failed to get next frame", pTopExInfo->m_passNumber));
    }

    if (!isCollided)
    {
        NotifyFunctionEnter(pThis, pThread, pTopExInfo);
    }

Exit:;
    END_QCALL;

    if (retVal != SWA_FAILED)
    {
        TADDR controlPC = pThis->m_crawl.GetRegisterSet()->ControlPC;
        if (!pThis->m_crawl.HasFaulted() && !pThis->m_crawl.IsIPadjusted())
        {
            controlPC -= STACKWALK_CONTROLPC_ADJUST_OFFSET;
        }
        pThis->SetAdjustedControlPC(controlPC);

        *pfIsExceptionIntercepted = CheckExceptionInterception(pThis, pTopExInfo);

        if (fUnwoundReversePInvoke)
        {
            *fUnwoundReversePInvoke = isPropagatingToNativeCode;
        }

        if (pThis->GetFrameState() == StackFrameIterator::SFITER_FRAMELESS_METHOD)
        {
            EH_LOG((LL_INFO100, "SfiNext (pass %d): returning managed frame at IP=%p, SP=%p, method %s::%s\n",
                pTopExInfo->m_passNumber, controlPC, GetRegdisplaySP(pThis->m_crawl.GetRegisterSet()),
                pThis->m_crawl.GetFunction()->m_pszDebugClassName, pThis->m_crawl.GetFunction()->m_pszDebugMethodName));
        }
        return TRUE;
    }

    return FALSE;
}

namespace AsmOffsetsAsserts
{
// Verify that the offsets into CONTEXT, REGDISPLAY, ExInfo and StackFrameIterator that the new managed exception handling
// use match between the managed code and the native ones.
#define public
#define const static constexpr

    #include "../System.Private.CoreLib/src/System/Runtime/ExceptionServices/AsmOffsets.cs"

#undef public
#undef const
};

#endif

#endif // FEATURE_EH_FUNCLETS
