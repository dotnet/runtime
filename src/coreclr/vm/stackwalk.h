// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


/* This is a poor man's implementation of virtual methods. */
/* The purpose of pCrawlFrame is to abstract (at least for the most common cases
   from the fact that not all methods are "framed" (basically all methods in
   "native" code are "unframed"). That way the job for the enumerator callbacks
   becomes much simpler (i.e. more transparent and hopefully less error prone).
   Two call-backs still need to distinguish between the two types: GC and exception.
   Both of these call-backs need to do really different things; for frameless methods
   they need to go through the codemanager and use the resp. apis.

   The reason for not implementing virtual methods on crawlFrame is solely because of
   the way exception handling is implemented (it does a "long jump" and bypasses
   the enumerator (stackWalker) when it finds a matching frame. By doing so couldn't
   properly destruct the dynamically created instance of CrawlFrame.
*/

#ifndef __stackwalk_h__
#define __stackwalk_h__

#include "eetwain.h"
#include "stackwalktypes.h"

class Frame;
class CrawlFrame;
class ICodeManager;
class IJitManager;
struct EE_ILEXCEPTION;
class AppDomain;

// This define controls handling of faults in managed code.  If it is defined,
//  the exception is handled (retried, actually), with a FaultingExceptionFrame
//  on the stack.  The FEF is used for unwinding.  If not defined, the unwinding
//  uses the exception context.
#define USE_FEF // to mark where code needs to be changed to eliminate the FEF
#if defined(TARGET_X86) && !defined(TARGET_UNIX)
 #undef USE_FEF // Turn off the FEF use on x86.
 #define ELIMINATE_FEF
#else
 #if defined(ELIMINATE_FEF)
  #undef ELIMINATE_FEF
 #endif
#endif // TARGET_X86 && !TARGET_UNIX

#if defined(FEATURE_EH_FUNCLETS)
#define RECORD_RESUMABLE_FRAME_SP
#endif

//************************************************************************
// Enumerate all functions.
//************************************************************************

/* This enumerator is meant to be used for the most common cases, i.e. to
   enumerate just all the functions of the requested thread. It is just a
   cover for the "real" enumerator.
 */

StackWalkAction StackWalkFunctions(Thread * thread, PSTACKWALKFRAMESCALLBACK pCallback, VOID * pData);

/*<TODO>@ISSUE: Maybe use a define instead?</TODO>
#define StackWalkFunctions(thread, callBack, userdata) thread->StackWalkFrames(METHODSONLY, (callBack),(userData))
*/

namespace AsmOffsetsAsserts
{
    class AsmOffsets;
};

#ifdef FEATURE_EH_FUNCLETS
extern "C" void QCALLTYPE AppendExceptionStackFrame(QCall::ObjectHandleOnStack exceptionObj, SIZE_T ip, SIZE_T sp, int flags, ExInfo *pExInfo);
#endif

class CrawlFrame
{
public:
    friend class AsmOffsetsAsserts::AsmOffsets;
#ifdef TARGET_X86
    friend StackWalkAction TAStackCrawlCallBack(CrawlFrame* pCf, void* data);
#endif // TARGET_X86

    //************************************************************************
    // Functions available for the callbacks (using the current pCrawlFrame)
    //************************************************************************

    /* Widely used/benign functions */

    /* Is this a function? */
    /* Returns either a MethodDesc* or NULL for "non-function" frames */
            //<TODO>@TODO: what will it return for transition frames?</TODO>

#ifdef FEATURE_INTERPRETER
    MethodDesc *GetFunction();
#else // FEATURE_INTERPRETER
    inline MethodDesc *GetFunction()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return pFunc;
    }
#endif


    Assembly *GetAssembly();

    /* Returns either a Frame * (for "framed items) or
       Returns NULL for frameless functions
     */
    inline Frame* GetFrame()       // will return NULL for "frameless methods"
    {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE((int)isFrameless != 0xcc);

        if (isFrameless)
            return NULL;
        else
            return pFrame;
    }

    BOOL IsInCalleesFrames(LPVOID stackPointer);

    // Fetch the extra type argument passed in some cases
    PTR_VOID GetParamTypeArg();

    /* Returns the "this" pointer of the method of the current frame -- at least in some cases.
       Returns NULL if the current frame does not have a method, or that method is not an instance method of a class type.
       Otherwise, the semantics currently depend, unfortunately, on the architecture.  On non-x86 architectures,
       should only be called for methods where the generic instantiation context is found via the this pointer (so that
       this information will be encoded in the GC Info).  On x86, can be called for this case, or if the method
       is synchronized.
     */
    OBJECTREF GetThisPointer();

    /*
        Returns ambient Stack pointer for this crawlframe.
        Must be a frameless method.
        Returns NULL if not available (includes prolog + epilog).
        This is safe to call on all methods, but it may return
        garbage if the method does not have an ambient SP (eg, ebp-based methods).
        x86 is the only platform using ambient SP.
    */
    TADDR GetAmbientSPFromCrawlFrame();

    void GetExactGenericInstantiations(Instantiation *pClassInst,
                                       Instantiation *pMethodInst);

    /* Returns extra information required to reconstruct exact generic parameters,
       if any.
       Returns NULL if
            - no extra information is required (i.e. the code is non-shared, which
              you can tell from the MethodDesc)
            - the extra information is not available (i.e. optimized away or codegen problem)
       Returns a MethodTable if the pMD returned by GetFunction satisfies RequiresInstMethodTableArg,
       and returns a MethodDesc if the pMD returned by GetFunction satisfies RequiresInstMethodDescArg.
       These together carry the exact instantiation information.
     */
    PTR_VOID GetExactGenericArgsToken();

    inline CodeManState * GetCodeManState() { LIMITED_METHOD_DAC_CONTRACT; return & codeManState; }
    /*
       IF YOU USE ANY OF THE SUBSEQUENT FUNCTIONS, YOU NEED TO REALLY UNDERSTAND THE
       STACK-WALKER (INCLUDING UNWINDING OF METHODS IN MANAGED NATIVE CODE)!
       YOU ALSO NEED TO UNDERSTAND THAT THESE FUNCTIONS MIGHT CHANGE ON AN AS-NEED BASIS.
     */

    /* The rest are meant to be used only by the exception catcher and the GC call-back  */

    /* Is currently a frame available? */
    /* conceptually returns (GetFrame(pCrawlFrame) == NULL)
     */
    inline bool IsFrameless()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE((int)isFrameless != 0xcc);

        return isFrameless;
    }


    /* Is it the current active (top-most) frame
     */
    inline bool IsActiveFrame()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE((int)isFirst != 0xcc);

        return isFirst;
    }

    /* Is it the current active function (top-most frame)
       asserts for non-functions, should be used for managed native code only
     */
    inline bool IsActiveFunc()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE((int)isFirst != 0xcc);

        return (pFunc && isFirst);
    }

    /* Is it the current active function (top-most frame)
       which faulted or threw an exception ?
       asserts for non-functions, should be used for managed native code only
     */
    bool IsInterrupted()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE((int)isInterrupted != 0xcc);

        return (pFunc && isInterrupted /* && isFrameless?? */);
    }

    /* Is it the current active function (top-most frame) which faulted ?
       asserts for non-functions, should be used for managed native code only
     */
    bool HasFaulted()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE((int)hasFaulted != 0xcc);

        return (pFunc && hasFaulted /* && isFrameless?? */);
    }

    /* Is this CrawlFrame just marking that we're in native code?
       Such frames are only provided when the stackwalk is inited w/ NOTIFY_ON_U2M_TRANSITIONS.
       The only use of these crawlframes is to get the Regdisplay.
     */
    bool IsNativeMarker()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE((int)isNativeMarker != 0xcc);

        return isNativeMarker;
    }

    /* x86 does not always push a FaultingExceptionFrame on the stack when there is a native exception
       (e.g. a breakpoint).  In this case, it relies on the CONTEXT stored on the ExInfo to resume
       the stackwalk at the managed stack frame which has faulted.

       This flag is set when the stackwalker is stopped at such a no-explicit-frame transition.  Conceptually
       this is just like stopping at a transition frame.  Note that the stackwalker only stops at no-frame
       transition if NOTIFY_ON_NO_FRAME_TRANSITIONS is set.
     */
    bool IsNoFrameTransition()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE((int)isNoFrameTransition != 0xcc);

        return isNoFrameTransition;
    }

    // A no-frame transition is one protected by an ExInfo.  It's an optimization on x86 to avoid pushing a
    // FaultingExceptionFrame (FEF).  Thus, for all intents and purposes, we should treat a no-frame
    // transition as a FEF.  This function returns a stack address for the no-frame transition to substitute
    // as the frame address of a FEF.  It's currently only used by the debugger stackwalker.
    TADDR GetNoFrameTransitionMarker()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE((int)isNoFrameTransition != 0xcc);

        return (isNoFrameTransition ? taNoFrameTransitionMarker : NULL);
    }

    /* Has the IP been adjusted to a point where it is safe to do GC ?
       (for OutOfLineThrownExceptionFrame)
       asserts for non-functions, should be used for managed native code only
     */
    bool IsIPadjusted()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE((int)isIPadjusted != 0xcc);

        return (pFunc && isIPadjusted /* && isFrameless?? */);
    }

    /* Gets the ICodeMangerFlags for the current frame */

    unsigned GetCodeManagerFlags()
    {
        CONTRACTL {
            NOTHROW;
            GC_NOTRIGGER;
            SUPPORTS_DAC;
        } CONTRACTL_END;

        unsigned flags = 0;

        if (IsActiveFunc())
            flags |= ActiveStackFrame;

        if (IsInterrupted())
        {
            flags |= ExecutionAborted;

            if (!HasFaulted() && !IsIPadjusted())
            {
                _ASSERTE(!(flags & ActiveStackFrame));
                flags |= AbortingCall;
            }
        }

#if defined(FEATURE_EH_FUNCLETS)
        if (ShouldParentToFuncletSkipReportingGCReferences())
        {
            flags |= ParentOfFuncletStackFrame;
        }
#endif // defined(FEATURE_EH_FUNCLETS)

        return flags;
    }

    /* Is this frame at a safe spot for GC?
     */
    bool IsGcSafe();

#if defined(TARGET_ARM) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    bool HasTailCalls();
#endif // TARGET_ARM || TARGET_ARM64 || TARGET_LOONGARCH64 || TARGET_RISCV64

    PREGDISPLAY GetRegisterSet()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        // We would like to make the following assertion, but it is legitimately
        // violated when we perform a crawl to find the return address for a hijack.
        // _ASSERTE(isFrameless);
        return pRD;
    }

    EECodeInfo * GetCodeInfo()
    {
        LIMITED_METHOD_DAC_CONTRACT;

        // This assumes that CrawlFrame is host-only structure with DACCESS_COMPILE
        // and thus it always returns the host address.
        return &codeInfo;
    }

    GCInfoToken GetGCInfoToken()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE((int)isFrameless != 0xcc);
        _ASSERTE(isFrameless);
        return codeInfo.GetGCInfoToken();
    }

    PTR_VOID GetGCInfo()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE((int)isFrameless != 0xcc);
        _ASSERTE(isFrameless);
        return codeInfo.GetGCInfo();
    }

    const METHODTOKEN& GetMethodToken()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE((int)isFrameless != 0xcc);
        _ASSERTE(isFrameless);
        return codeInfo.GetMethodToken();
    }

    unsigned GetRelOffset()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE((int)isFrameless != 0xcc);
        _ASSERTE(isFrameless);
        return codeInfo.GetRelOffset();
    }

    IJitManager*  GetJitManager()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE((int)isFrameless != 0xcc);
        _ASSERTE(isFrameless);
        return codeInfo.GetJitManager();
    }

    ICodeManager* GetCodeManager()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        _ASSERTE((int)isFrameless != 0xcc);
        _ASSERTE(isFrameless);
        return codeInfo.GetCodeManager();
    }

    inline StackwalkCacheEntry* GetStackwalkCacheEntry()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE (isCachedMethod != stackWalkCache.IsEmpty());
        if (isCachedMethod && stackWalkCache.m_CacheEntry.IsSafeToUseCache())
        {
            return &(stackWalkCache.m_CacheEntry);
        }
        else
        {
            return NULL;
        }
    }

    void CheckGSCookies();

    inline Thread* GetThread()
    {
        LIMITED_METHOD_CONTRACT;
        return pThread;
    }

#if defined(FEATURE_EH_FUNCLETS)
    bool IsFunclet()
    {
        WRAPPER_NO_CONTRACT;

        if (!IsFrameless())
            return false;

        return !!codeInfo.IsFunclet();
    }

    bool IsFilterFunclet();

    // Indicates if the funclet has already reported GC
    // references (or not). This will return true if
    // we come across the parent frame of a funclet
    // that is active on the stack.
    bool ShouldParentToFuncletSkipReportingGCReferences()
    {
        LIMITED_METHOD_CONTRACT;
        return fShouldParentToFuncletSkipReportingGCReferences;
    }

    bool ShouldCrawlframeReportGCReferences()
    {
        LIMITED_METHOD_CONTRACT;

        return fShouldCrawlframeReportGCReferences;
    }

    bool ShouldParentToFuncletUseUnwindTargetLocationForGCReporting()
    {
        LIMITED_METHOD_CONTRACT;
        return fShouldParentFrameUseUnwindTargetPCforGCReporting;
    }

    const EE_ILEXCEPTION_CLAUSE& GetEHClauseForCatch()
    {
        return ehClauseForCatch;
    }

#endif // FEATURE_EH_FUNCLETS

protected:
    // CrawlFrames are temporarily created by the enumerator.
    // Do not create one from C++. This protected constructor polices this rule.
    CrawlFrame();

    void SetCurGSCookie(GSCookie * pGSCookie);

private:

    friend class Thread;
    friend class EECodeManager;
    friend class StackFrameIterator;
#ifdef FEATURE_EH_FUNCLETS
    friend class ExceptionTracker;
    friend void QCALLTYPE AppendExceptionStackFrame(QCall::ObjectHandleOnStack exceptionObj, SIZE_T ip, SIZE_T sp, int flags, ExInfo *pExInfo);
#endif // FEATURE_EH_FUNCLETS

    CodeManState      codeManState;

    bool              isFrameless;
    bool              isFirst;

    // The next three fields are only valid for managed stack frames.  They are set using attributes
    // on explicit frames, and they are reset after processing each managed stack frame.
    bool              isInterrupted;
    bool              hasFaulted;
    bool              isIPadjusted;

    bool              isNativeMarker;
    bool              isProfilerDoStackSnapshot;
    bool              isNoFrameTransition;
    TADDR             taNoFrameTransitionMarker;    // see code:CrawlFrame.GetNoFrameTransitionMarker
    PTR_Frame         pFrame;
    MethodDesc       *pFunc;

    // the rest is only used for "frameless methods"
    PREGDISPLAY       pRD; // "thread context"/"virtual register set"

    EECodeInfo        codeInfo;
#if defined(FEATURE_EH_FUNCLETS)
    bool              isFilterFunclet;
    bool              isFilterFuncletCached;
    bool              fShouldParentToFuncletSkipReportingGCReferences;
    bool              fShouldCrawlframeReportGCReferences;
    bool              fShouldParentFrameUseUnwindTargetPCforGCReporting;
    EE_ILEXCEPTION_CLAUSE ehClauseForCatch;
#endif //FEATURE_EH_FUNCLETS
    Thread*           pThread;

    // fields used for stackwalk cache
    BOOL              isCachedMethod;
    StackwalkCache    stackWalkCache;

    GSCookie         *pCurGSCookie;
    GSCookie         *pFirstGSCookie;

    friend class Frame; // added to allow 'friend void CrawlFrame::GotoNextFrame();' declaration in class Frame, frames.h
    void GotoNextFrame();
};

void GcEnumObject(LPVOID pData, OBJECTREF *pObj);
StackWalkAction GcStackCrawlCallBack(CrawlFrame* pCF, VOID* pData);

#if defined(ELIMINATE_FEF)
//******************************************************************************
// This class is used to help use exception context records to resync a
//  stackwalk, when managed code has generated an exception (eg, AV, zerodiv.,,)
// Such an exception causes a transition from the managed code into unmanaged
//  OS and runtime code, but without the benefit of any Frame.  This code helps
//  the stackwalker simulate the effect that such a frame would have.
// In particular, this class has methods to walk the chain of ExInfos, looking
//  for records with pContext pointers with certain characteristics.  The
//  characteristics that are important are the location in the stack (ie, is a
//  given pContext relevant at a particular point in the stack walk), and
//  whether the pContext was generated in managed code.
//******************************************************************************
class ExInfoWalker
{
public:
    ExInfoWalker() : m_pExInfo(0) { SUPPORTS_DAC; }
    void Init (ExInfo *pExInfo) { SUPPORTS_DAC; m_pExInfo = pExInfo; }
    // Skip one ExInfo.
    void WalkOne();
    // Attempt to find an ExInfo with a pContext that is higher (older) than
    //  a given minimum location.
    void WalkToPosition(TADDR taMinimum, BOOL bPopFrames);
    // Attempt to find an ExInfo with a pContext that has an IP in managed code.
    void WalkToManaged();
    // Return current ExInfo's m_pContext, or NULL if no m_pExInfo.
    PTR_CONTEXT GetContext() { SUPPORTS_DAC; return m_pExInfo ? m_pExInfo->m_pContext : NULL; }
    // Useful to see if there is more on the ExInfo chain.
    ExInfo* GetExInfo() { SUPPORTS_DAC; return m_pExInfo; }

    // helper functions for retrieving information from the exception CONTEXT
    TADDR GetSPFromContext()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return dac_cast<TADDR>((m_pExInfo && m_pExInfo->m_pContext) ? GetSP(m_pExInfo->m_pContext) : PTR_NULL);
    }

    TADDR GetEBPFromContext()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;
        return dac_cast<TADDR>((m_pExInfo && m_pExInfo->m_pContext) ? GetFP(m_pExInfo->m_pContext) : PTR_NULL);
    }

    DWORD GetFault() { SUPPORTS_DAC; return m_pExInfo ? m_pExInfo->m_pExceptionRecord->ExceptionCode : 0; }

private:
    ExInfo      *m_pExInfo;
};  // class ExInfoWalker
#endif // ELIMINATE_FEF


//---------------------------------------------------------------------------------------
//
// This iterator class walks the stack of a managed thread.  Where the iterator stops depends on the
// stackwalk flags.
//
// Notes:
//    This class works both in-process and out-of-process (e.g. DAC).
//

class StackFrameIterator
{
    friend class AsmOffsetsAsserts::AsmOffsets;
public:
    // This constructor is for the usage pattern of creating an uninitialized StackFrameIterator and then
    // calling Init() on it.
    StackFrameIterator(void);

    // This constructor is for the usage pattern of creating an initialized StackFrameIterator and then
    // calling ResetRegDisp() on it.
    StackFrameIterator(Thread * pThread, PTR_Frame pFrame, ULONG32 flags);

    //
    // We should consider merging Init() and ResetRegDisp().
    //

    // Initialize the iterator.  Note that the iterator has thread-affinity,
    // and the stackwalk flags cannot be changed once the iterator is created.
    BOOL Init(Thread *      pThread,
              PTR_Frame     pFrame,
              PREGDISPLAY   pRegDisp,
              ULONG32       flags);

    // Reset the iterator to the specified REGDISPLAY.  The caller must ensure that the REGDISPLAY is valid.
    BOOL ResetRegDisp(PREGDISPLAY pRegDisp,
                      bool        fIsFirst);

    // @dbgtodo  inspection - This function should be removed once the Windows debuggers stop using the old DAC API.
    void SetIsFirstFrame(bool isFirst)
    {
        LIMITED_METHOD_CONTRACT;
        m_crawl.isFirst = isFirst;
    }

    // whether the iterator has reached the root of the stack or not
    BOOL IsValid(void);

    // advance to the next frame according to the stackwalk flags
    StackWalkAction Next(void);

#ifdef FEATURE_EH_FUNCLETS
    void ResetNextExInfoForSP(TADDR SP);

    ExInfo* GetNextExInfo()
    {
        return m_pNextExInfo;
    }

    void SetAdjustedControlPC(TADDR pc)
    {
        m_AdjustedControlPC = pc;
    }

    void UpdateIsRuntimeWrappedExceptions()
    {
        CONTRACTL
        {
            MODE_ANY;
            GC_TRIGGERS;
            NOTHROW;
        }
        CONTRACTL_END

#if defined(FEATURE_EH_FUNCLETS) && !defined(DACCESS_COMPILE)
        m_isRuntimeWrappedExceptions = (m_crawl.pFunc != NULL) && m_crawl.pFunc->GetModule()->IsRuntimeWrapExceptions();
#endif // FEATURE_EH_FUNCLETS && !DACCESS_COMPILE
    }

#endif // FEATURE_EH_FUNCLETS

    enum FrameState
    {
        SFITER_UNINITIALIZED,               // uninitialized
        SFITER_FRAMELESS_METHOD,            // managed stack frame
        SFITER_FRAME_FUNCTION,              // explicit frame
        SFITER_SKIPPED_FRAME_FUNCTION,      // skipped explicit frame
        SFITER_NO_FRAME_TRANSITION,         // no-frame transition (currently used for ExInfo only)
        SFITER_NATIVE_MARKER_FRAME,         // the native stack frame immediately below (stack grows up)
                                            // a managed stack region
        SFITER_INITIAL_NATIVE_CONTEXT,      // initial native seed CONTEXT
        SFITER_DONE,                        // the iterator has reached the end of the stack
    };
    FrameState GetFrameState() {LIMITED_METHOD_DAC_CONTRACT; return m_frameState;}

    CrawlFrame m_crawl;

#if defined(_DEBUG)
    // used in logging
    UINT32 m_uFramesProcessed;
#endif // _DEBUG

private:

    // For the new exception handling that uses managed code to dispatch the
    // exceptions, we need to force the stack walker to report GC references
    // in the exception handling code frames, since they are alive. This is
    // different from the old exception handling where no frames below the
    // funclets upto the parent frame are alive.
    enum class ForceGCReportingStage : BYTE
    {
        Off = 0,
        // The stack walker has hit a funclet, we are looking for the first managed 
        // frame that would be one of the managed exception handling code frames
        LookForManagedFrame = 1,
        // The stack walker has already hit a managed exception handling code frame,
        // we are looking for a marker frame which indicates the native caller of
        // the managed exception handling code
        LookForMarkerFrame = 2
    };

    // This is a helper for the two constructors.
    void CommonCtor(Thread * pThread, PTR_Frame pFrame, ULONG32 flags);

    // Reset the CrawlFrame owned by the iterator.  Used by both Init() and ResetRegDisp().
    void ResetCrawlFrame(void);

    // Check whether we should stop at the current frame given the stackwalk flags.
    // If not, continue advancing to the next frame.
    StackWalkAction Filter(void);

    // Advance to the next frame regardless of the stackwalk flags.  This is used by Next() and Filter().
    StackWalkAction NextRaw(void);

    // sync the REGDISPLAY to the current CONTEXT
    void UpdateRegDisp(void);

    // Check whether the IP is managed code.  This function updates the following fields on CrawlFrame:
    // JitManagerInstance and isFrameless.
    void ProcessIp(PCODE Ip);

    // Update the CrawlFrame to represent where we have stopped.
    // This is called after advancing to a new frame.
    void ProcessCurrentFrame(void);

    // If an explicit frame is allocated in a managed stack frame (e.g. an inlined pinvoke call),
    // we may have skipped an explicit frame.  This function checks for them.
    BOOL CheckForSkippedFrames(void);

    // Perform the necessary tasks before stopping at a managed stack frame.  This is mostly validation work.
    void PreProcessingForManagedFrames(void);

    // Perform the necessary tasks after stopping at a managed stack frame and unwinding to its caller.
    // This includes advancing the ExInfo and checking whether the new IP is managed.
    void PostProcessingForManagedFrames(void);

    // Perform the necessary tasks after stopping at a no-frame transition.  This includes loading
    // the CONTEXT stored in the ExInfo and updating the REGDISPLAY to the faulting managed stack frame.
    void PostProcessingForNoFrameTransition(void);

#if defined(FEATURE_EH_FUNCLETS)
    void ResetGCRefReportingState(bool ResetOnlyIntermediaryState = false)
    {
        LIMITED_METHOD_CONTRACT;

        if (!ResetOnlyIntermediaryState)
        {
            m_fFuncletNotSeen = false;
            m_sfFuncletParent = StackFrame();
            m_fProcessNonFilterFunclet = false;
        }

        m_sfIntermediaryFuncletParent = StackFrame();
        m_fProcessIntermediaryNonFilterFunclet = false;
    }
#endif // defined(FEATURE_EH_FUNCLETS)

    // Iteration state.
    FrameState m_frameState;

    // Initial state.  Must be preserved for restarting.
    Thread * m_pThread;                      // Thread on which to walk.

    PTR_Frame m_pStartFrame;                  // Frame* passed to Init

    // This is the real starting explicit frame.  If m_pStartFrame is NULL,
    // then this is equal to m_pThread->GetFrame().  Otherwise this is equal to m_pStartFrame.
    INDEBUG(PTR_Frame m_pRealStartFrame);
    ULONG32               m_flags;          // StackWalkFrames flags.
    ICodeManagerFlags     m_codeManFlags;
    ExecutionManager::ScanFlag m_scanFlag;

    // the following fields are used to cache information about a managed stack frame
    // when we need to stop for skipped explicit frames
    EECodeInfo     m_cachedCodeInfo;

    GSCookie *     m_pCachedGSCookie;

#if defined(ELIMINATE_FEF)
    ExInfoWalker m_exInfoWalk;
#endif // ELIMINATE_FEF

#if defined(FEATURE_EH_FUNCLETS)
    // used in funclet-skipping
    StackFrame    m_sfParent;

    // Used in GC reference enumeration mode
    StackFrame    m_sfFuncletParent;
    bool          m_fProcessNonFilterFunclet;
    StackFrame    m_sfIntermediaryFuncletParent;
    bool          m_fProcessIntermediaryNonFilterFunclet;
    bool          m_fDidFuncletReportGCReferences;
    bool          m_isRuntimeWrappedExceptions;
#endif // FEATURE_EH_FUNCLETS
    // State of forcing of GC reference reporting for managed exception handling methods (RhExThrow, RhDispatchEx etc)
    ForceGCReportingStage m_forceReportingWhileSkipping;
    // The stack walk has moved past the first ExInfo location on the stack
    bool          m_movedPastFirstExInfo;
    // Indicates that no funclet was seen during the current stack walk yet
    bool          m_fFuncletNotSeen;
#if defined(RECORD_RESUMABLE_FRAME_SP)
    LPVOID m_pvResumableFrameTargetSP;
#endif // RECORD_RESUMABLE_FRAME_SP
#ifdef FEATURE_EH_FUNCLETS
    ExInfo* m_pNextExInfo;
    TADDR m_AdjustedControlPC;
#endif // FEATURE_EH_FUNCLETS
};

void SetUpRegdisplayForStackWalk(Thread * pThread, T_CONTEXT * pContext, REGDISPLAY * pRegdisplay);

#endif
