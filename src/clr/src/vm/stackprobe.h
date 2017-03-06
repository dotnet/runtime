// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//
//-----------------------------------------------------------------------------
// Stack Probe Header
// Used to setup stack guards
//-----------------------------------------------------------------------------

#ifndef __STACKPROBE_h__
#define __STACKPROBE_h__

//-----------------------------------------------------------------------------
// Stack Guards.
//
// The idea is to force stack overflows to occur at convenient spots.
// * Fire at RequiresNPagesStack (beggining of func) if this functions locals
// cause overflow. Note that in a debug mode, initing the locals to garbage
// will cause the overflow before this macro is executed.
//
// * Fire at CheckStack (end of func) if either our nested function calls
// cause or use of _alloca cause the stack overflow. Note that this macro
// is debug only, so release builds won't catch on this
//
// Some comments:
// - Stack grows *down*,
// - Ideally, all funcs would have EBP frame and we'd use EBP instead of ESP,
//    however, we use the 'this' ptr to get the stack ptr, since the guard
//    is declared on the stack.
//
// Comments about inlining assembly w/ Macros:
// - Must use cstyle comments /* ... */
// - No semi colons, need __asm keyword at the start of each line
//-----------------------------------------------------------------------------

//-----------------------------------------------------------------------------
// *How* to use stack guards.
//
// See, in a CLR enlistment, src\ndp\clr\doc\OtherDevDocs\untriaged\clrdev_web\
//
//-----------------------------------------------------------------------------

//-----------------------------------------------------------------------------
// Stack guards have 3 compiler states:
//#define FEATURE_STACK_PROBE
// (All) All stack guard code is completely removed by the preprocessor if 
// not defined. This is used for CoreCLR.
//
//#define STACK_GUARDS_DEBUG
// (DEBUG) Full stack guard debugging including cookies, tracking ips, and
// chaining. More heavy weight, recommended for a debug build only
//
//#define STACK_GUARDS_RELEASE
// (RELEASE) Light stack guard code. For golden builds. Forces Stack Overflow
// to happen at "convenient" times. No debugging help.
//-----------------------------------------------------------------------------

#include "genericstackprobe.h"
#include "utilcode.h"

/* defining VM_NO_SO_INFRASTRUCTURE_CODE for VM code
 * This macro can be used to have code which will be present 
 * only for code inside VM directory when SO infrastructure code is not built.
 * Eg. Currently it is used in macro EX_END_HOOK.
 * For VM code EX_HOOK calls CLREXception::HandleState::SetupCatch().
 * When Stack guards are disabled we will tear down the process in 
 * CLREXception::HandleState::SetupCatch() if there is a StackOverflow.
 * So we should not reach EX_END_HOOK when there is StackOverflow.
 * This change cannot be done for all other code because
 * CLREXception::HandleState::SetupCatch() is not called rather
 * EXception::HandleState::SetupCatch() is called which is a nop.
 */

#ifndef FEATURE_STACK_PROBE
#undef VM_NO_SO_INFRASTRUCTURE_CODE
#define VM_NO_SO_INFRASTRUCTURE_CODE(x) x
#endif


#ifdef FEATURE_STACK_PROBE

#define DEFAULT_INTERIOR_PROBE_AMOUNT 4

#define MINIMUM_STACK_REQUIREMENT (0.25)

BOOL IsBackoutCalledForEH(BYTE *origSP, BYTE *backoutSP);

//=============================================================================
// Common code
//=============================================================================
// Release version of the probe function
BOOL RetailStackProbeNoThrow(unsigned int n, Thread *pThread);
BOOL RetailStackProbeNoThrowWorker(unsigned int n, Thread *pThread);
void RetailStackProbe(unsigned int n, Thread *pThread);
void RetailStackProbeWorker(unsigned int n, Thread *pThread);
void ReportStackOverflow();

// Retail stack probe with default amount is the most common stack probe. Create
// a dedicated method for it to reduce code size.
void DefaultRetailStackProbeWorker(Thread * pThread);

void RetailStackProbe(unsigned int n);

BOOL ShouldProbeOnThisThread();

int SOTolerantBoundaryFilter(EXCEPTION_POINTERS *pExceptionInfo, DWORD * pdwSOTolerantFlags);
void SOTolerantCode_RecoverStack(DWORD dwFlags);
void SOTolerantCode_ExceptBody(DWORD * pdwFlags, Frame * pSafeForSOFrame);

#endif

#if defined(FEATURE_STACK_PROBE) && !defined(DACCESS_COMPILE)

inline bool IsStackProbingEnabled()
{
    LIMITED_METHOD_CONTRACT;
    return g_StackProbingEnabled;
}

//=============================================================================
// DEBUG
//=============================================================================
#if defined(STACK_GUARDS_DEBUG)

#include "common.h"

class BaseStackGuard;

//-----------------------------------------------------------------------------
// Need to chain together stack guard address for nested functions
// Use a TLS slot to store the head of the chain
//-----------------------------------------------------------------------------
extern DWORD g_CurrentStackGuardTlsIdx;

//-----------------------------------------------------------------------------
// Class
//-----------------------------------------------------------------------------

// Base version - has no ctor/dtor, so we can use it with SEH
//
// *** Don't declare any members here.  Put them in BaseStackGuardGeneric.
// We downcast directly from the base to the derived, using the knowledge
// that the base class and the derived class are identical for members.
//
class BaseStackGuard : public BaseStackGuardGeneric
{
protected:
    BaseStackGuard()
    {
        _ASSERTE(!"No default construction allowed");
    }

public:
    BaseStackGuard(const char *szFunction, const char *szFile, unsigned int lineNum) :
        BaseStackGuardGeneric(szFunction, szFile, lineNum)
    {
        STATIC_CONTRACT_LEAF;
    }

    UINT_PTR *Marker() { return m_pMarker; }

    unsigned int Depth() { return m_depth; }

    const char *FunctionName() { return m_szFunction; }

    BOOL IsProbeGuard()
    {
        return (m_isBoundaryGuard == FALSE);
    }

    BOOL IsBoundaryGuard()
    {
        return (m_isBoundaryGuard == TRUE);
    }

    inline BOOL ShouldCheckPreviousCookieIntegrity();
    inline BOOL ShouldCheckThisCookieIntegrity();

    BOOL RequiresNStackPages(unsigned int n, BOOL fThrowOnSO = TRUE);
    BOOL RequiresNStackPagesThrowing(unsigned int n);
    BOOL RequiresNStackPagesNoThrow(unsigned int n);
private:    
    BOOL RequiresNStackPagesInternal(unsigned int n, BOOL fThrowOnSO = TRUE);
public:
    BOOL DoProbe(unsigned int n, BOOL fThrowOnSO);
    void CheckStack();

    static void RestoreCurrentGuard(BOOL fWasDisabled = FALSE);
    void PopGuardForEH();

    // Different error messages for the different times we detemine there's a problem.
    void HandleOverwrittenThisStackGuard(__in_z char *stackID);
    void HandleOverwrittenPreviousStackGuard(int shortFall, __in_z char *stackID);
    void HandleOverwrittenCurrentStackGuard(int shortFall, __in_z char *stackID);
    static void HandleOverwrittenCurrentStackGuard(void *pGuard, int shortFall, __in_z char *stackID);

    void CheckMarkerIntegrity();
    void RestorePreviousGuard();
    void ProtectMarkerPageInDebugger();
    void UndoPageProtectionInDebugger();
    static void ProtectMarkerPageInDebugger(void *pGuard);
    static void UndoPageProtectionInDebugger(void *pGuard);

    inline HRESULT PrepGuard()
    {
        WRAPPER_NO_CONTRACT;

        // See if it has already been prepped...
        if (ClrFlsGetValue(g_CurrentStackGuardTlsIdx) != NULL)
            return S_OK;

        // Let's see if we'll be able to put in a guard page
        ClrFlsSetValue(g_CurrentStackGuardTlsIdx, 
(void*)-1);

        if (ClrFlsGetValue(g_CurrentStackGuardTlsIdx) != (void*)-1)
            return E_OUTOFMEMORY;

        return S_OK;

    }

    inline static BaseStackGuard* GetCurrentGuard()
    {
        WRAPPER_NO_CONTRACT;
        if (g_CurrentStackGuardTlsIdx != -1)
            return (BaseStackGuard*) ClrFlsGetValue(g_CurrentStackGuardTlsIdx);
        else
            return NULL;
    }

    inline static BOOL IsGuard(BaseStackGuard *probe)
    {
        return (probe != NULL);
    }
    static void SetCurrentGuard(BaseStackGuard* pGuard);
    static void ResetCurrentGuard(BaseStackGuard* pGuard);

    inline static BOOL IsProbeGuard(BaseStackGuard *probe)
    {
        LIMITED_METHOD_CONTRACT;
        return (IsGuard(probe) != NULL && probe->IsProbeGuard());
    }

    inline static BOOL IsBoundaryGuard(BaseStackGuard *probe)
    {
        LIMITED_METHOD_CONTRACT;
        return (IsGuard(probe) != NULL && probe->IsBoundaryGuard());
    }

    static void InitProbeReportingToFaultInjectionFramework();
    BOOL ReportProbeToFaultInjectionFramework();

    static void Terminate();


    static HMODULE  m_hProbeCallBack;
    typedef BOOL (*ProbeCallbackType)(unsigned, const char *);
    static ProbeCallbackType m_pfnProbeCallback;

};


// Derived version, add a dtor that automatically calls Check_Stack, move convenient, but can't use with SEH.
class AutoCleanupStackGuard : public BaseStackGuard
{
protected:
    AutoCleanupStackGuard()
    {
        _ASSERTE(!"No default construction allowed");
    }

public:
    DEBUG_NOINLINE AutoCleanupStackGuard(const char *szFunction, const char *szFile, unsigned int lineNum) :
        BaseStackGuard(szFunction, szFile, lineNum)
    {
        SCAN_SCOPE_BEGIN;
        // This CANNOT be a STATIC_CONTRACT_SO_INTOLERANT b/c that isn't
        // really just a static contract, it is actually calls EnsureSOIntolerantOK
        // as well. Instead we just use the annotation.
        ANNOTATION_FN_SO_INTOLERANT;
    }

    DEBUG_NOINLINE ~AutoCleanupStackGuard()
    {
        SCAN_SCOPE_END;
        CheckStack();
    }
};

class DebugSOIntolerantTransitionHandlerBeginOnly
{
    BOOL m_prevSOTolerantState;
    ClrDebugState* m_clrDebugState;
    char *m_ctorSP;

  public:
    DEBUG_NOINLINE DebugSOIntolerantTransitionHandlerBeginOnly(EEThreadHandle thread);
    DEBUG_NOINLINE ~DebugSOIntolerantTransitionHandlerBeginOnly();
};



extern DWORD g_InteriorProbeAmount;

//=============================================================================
// Macros for transition into SO_INTOLERANT code
//=============================================================================

FORCEINLINE DWORD DefaultEntryProbeAmount() { return g_EntryPointProbeAmount; }

#define BEGIN_SO_INTOLERANT_CODE(pThread)                                                   \
    BEGIN_SO_INTOLERANT_CODE_FOR(pThread, g_EntryPointProbeAmount)                          \

#define BEGIN_SO_INTOLERANT_CODE_FOR(pThread, n)                                            \
    {                                                                                       \
    /*_ASSERTE(pThread); */                                                                 \
    AutoCleanupStackGuard stack_guard_XXX(__FUNCTION__, __FILE__, __LINE__);                \
    stack_guard_XXX.RequiresNStackPagesThrowing(ADJUST_PROBE(n));                           \
    /* work around unreachable code warning */                                              \
        if (true)                                                                           \
        {                                                                                   \
        DebugSOIntolerantTransitionHandler __soIntolerantTransitionHandler;                 \
        ANNOTATION_SO_PROBE_BEGIN(DEFAULT_ENTRY_PROBE_AMOUNT);                              \
        /* work around unreachable code warning */                                          \
            if (true)                                                                       \
            {                                                                               \
                DEBUG_ASSURE_NO_RETURN_BEGIN(SO_INTOLERANT)

#define BEGIN_SO_INTOLERANT_CODE_NOTHROW(pThread, ActionOnSO)                               \
    {                                                                                       \
    /*_ASSERTE(pThread || IsGCSpecialThread());*/                                           \
    AutoCleanupStackGuard stack_guard_XXX(__FUNCTION__, __FILE__, __LINE__);                \
    if (! stack_guard_XXX.RequiresNStackPagesNoThrow(ADJUST_PROBE(g_EntryPointProbeAmount)))\
    {                                                                                       \
        stack_guard_XXX.SetNoException();                                                   \
        ActionOnSO;                                                                         \
    }                                                                                       \
    /* work around unreachable code warning */                                              \
    else                                                                                    \
    {                                                                                       \
        DebugSOIntolerantTransitionHandler __soIntolerantTransitionHandler;                 \
        ANNOTATION_SO_PROBE_BEGIN(DEFAULT_ENTRY_PROBE_AMOUNT);                              \
        /* work around unreachable code warning */                                          \
        if (true)                                                                           \
        {                                                                                   \
            DEBUG_ASSURE_NO_RETURN_BEGIN(SO_INTOLERANT)


// This is defined just for using in the InternalSetupForComCall macro which
// doesn't have a corresponding end macro because no exception will pass through it
// It should not be used in any situation where an exception could pass through
// the transition.
#define SO_INTOLERANT_CODE_NOTHROW(pThread, ActionOnSO)                                     \
    AutoCleanupStackGuard stack_guard_XXX(__FUNCTION__, __FILE__, __LINE__);                \
    if (! stack_guard_XXX.RequiresNStackPagesNoThrow(ADJUST_PROBE(g_EntryPointProbeAmount)))\
    {                                                                                       \
        ActionOnSO;                                                                         \
    }                                                                                       \
    stack_guard_XXX.SetNoException();                                                       \
    DebugSOIntolerantTransitionHandlerBeginOnly __soIntolerantTransitionHandler(pThread);   \
    ANNOTATION_SO_PROBE_BEGIN(DEFAULT_ENTRY_PROBE_AMOUNT);


// For some codepaths used during the handling of an SO, we need to guarantee a
// minimal stack consumption to avoid an SO on that codepath.  These are typically host
// APIS such as allocation.  The host is going to use < 1/4 page, so make sure
// we have that amount before calling.  Then use the BACKOUT_VALIDATION to ensure
// that we don't overrun it.  We call ReportStackOverflow, which will generate a hard
// SO if we have less than a page left.

#define MINIMAL_STACK_PROBE_CHECK_THREAD(pThread)                                               \
    if (IsStackProbingEnabled())                                                                \
    {                                                                                           \
        Thread *__pThread = pThread;                                                            \
        if (__pThread && ! __pThread->IsStackSpaceAvailable(MINIMUM_STACK_REQUIREMENT))         \
        {                                                                                       \
            ReportStackOverflow();                                                              \
        }                                                                                       \
    }                                                                                           \
    CONTRACT_VIOLATION(SOToleranceViolation);

// We don't use the DebugSOIntolerantTransitionHandler here because we don't need to transition into
// SO-intolerant code.   We're already there.  We also don't need to annotate as having probed,
// because this only matters for entry point functions.
// We have a way to separate the declaration from the actual probing for cases where need
// to do a test, such as IsGCThread(), to decide if should probe.
#define DECLARE_INTERIOR_STACK_PROBE                                            \
    {                                                                           \
        AutoCleanupStackGuard stack_guard_XXX(__FUNCTION__, __FILE__, __LINE__);\
        DEBUG_ASSURE_NO_RETURN_BEGIN(STACK_PROBE)


// A function containing an interior probe is implicilty SO-Intolerant because we
// assume that it is not behind a probe.  So confirm that we are in the correct state.
#define DO_INTERIOR_STACK_PROBE_FOR(pThread, n)                                 \
    _ASSERTE(pThread != NULL);                                                  \
    stack_guard_XXX.RequiresNStackPagesThrowing(ADJUST_PROBE(n));               \
    EnsureSOIntolerantOK(__FUNCTION__, __FILE__, __LINE__);

#define DO_INTERIOR_STACK_PROBE_FOR_CHECK_THREAD(n)                             \
    if (ShouldProbeOnThisThread())                                              \
    {                                                                           \
        DO_INTERIOR_STACK_PROBE_FOR(GetThread(), g_InteriorProbeAmount);        \
    }

// A function containing an interior probe is implicilty SO-Intolerant because we
// assume that it is not behind a probe.  So confirm that we are in the correct state.
#define DO_INTERIOR_STACK_PROBE_FOR_NOTHROW(pThread, n, actionOnSO)             \
        _ASSERTE(pThread != NULL);                                              \
        if (! stack_guard_XXX.RequiresNStackPagesNoThrow(ADJUST_PROBE(n)))      \
        {                                                                       \
            stack_guard_XXX.SetNoException();                                   \
            actionOnSO;                                                         \
        }                                                                       \
        EnsureSOIntolerantOK(__FUNCTION__, __FILE__, __LINE__);

#define DO_INTERIOR_STACK_PROBE_FOR_NOTHROW_CHECK_THREAD(n, actionOnSO)         \
    if (ShouldProbeOnThisThread())                                              \
    {                                                                           \
        DO_INTERIOR_STACK_PROBE_FOR_NOTHROW(GetThread(), n, actionOnSO);        \
    }


#define INTERIOR_STACK_PROBE_FOR(pThread, n) \
    DECLARE_INTERIOR_STACK_PROBE; \
    DO_INTERIOR_STACK_PROBE_FOR(pThread, n)

#define INTERIOR_STACK_PROBE_FOR_CHECK_THREAD(n) \
    DECLARE_INTERIOR_STACK_PROBE; \
    DO_INTERIOR_STACK_PROBE_FOR_CHECK_THREAD(n)

#define INTERIOR_STACK_PROBE_FOR_NOTHROW(pThread, n, ActionOnSO) \
    DECLARE_INTERIOR_STACK_PROBE; \
    DO_INTERIOR_STACK_PROBE_FOR_NOTHROW(pThread, n, ActionOnSO)

#define INTERIOR_STACK_PROBE_FOR_NOTHROW_CHECK_THREAD(n, ActionOnSO) \
    DECLARE_INTERIOR_STACK_PROBE; \
    DO_INTERIOR_STACK_PROBE_FOR_NOTHROW_CHECK_THREAD(n, ActionOnSO)


#define INTERIOR_STACK_PROBE(pThread) \
    INTERIOR_STACK_PROBE_FOR(pThread, g_InteriorProbeAmount)

#define INTERIOR_STACK_PROBE_CHECK_THREAD \
    INTERIOR_STACK_PROBE_FOR_CHECK_THREAD(g_InteriorProbeAmount)

#define INTERIOR_STACK_PROBE_NOTHROW(pThread, ActionOnSO) \
    INTERIOR_STACK_PROBE_FOR_NOTHROW(pThread, g_InteriorProbeAmount, ActionOnSO)

#define INTERIOR_STACK_PROBE_NOTHROW_CHECK_THREAD(ActionOnSO) \
    INTERIOR_STACK_PROBE_FOR_NOTHROW_CHECK_THREAD(g_InteriorProbeAmount, ActionOnSO)


#define END_INTERIOR_STACK_PROBE                                                \
        DEBUG_ASSURE_NO_RETURN_END(STACK_PROBE)                                 \
        stack_guard_XXX.SetNoException();                                       \
    }

#define RETURN_FROM_INTERIOR_PROBE(x)                                           \
        DEBUG_OK_TO_RETURN_BEGIN(STACK_PROBE)                                   \
        stack_guard_XXX.SetNoException();                                       \
        RETURN(x);                                                              \
        DEBUG_OK_TO_RETURN_END(STACK_PROBE)


// This is used for EH code where we are about to throw.
// To avoid taking an SO during EH processing, want to include it in our probe limits
// So we will just do a big probe and then throw.
#define STACK_PROBE_FOR_THROW(pThread)                                                  \
    AutoCleanupStackGuard stack_guard_XXX(__FUNCTION__, __FILE__, __LINE__);            \
    if (pThread != NULL)                                                                \
    {                                                                                   \
        DO_INTERIOR_STACK_PROBE_FOR(pThread, ADJUST_PROBE(DEFAULT_ENTRY_PROBE_AMOUNT)); \
    }

// This is used for throws where we cannot use a dtor-based probe.
#define PUSH_STACK_PROBE_FOR_THROW(pThread)                                     \
    BaseStackGuard stack_guard_XXX(__FUNCTION__, __FILE__, __LINE__);           \
    stack_guard_XXX.RequiresNStackPagesThrowing(ADJUST_PROBE(g_EntryPointProbeAmount));

#define SAVE_ADDRESS_OF_STACK_PROBE_FOR_THROW(pGuard)                           \
    pGuard = &stack_guard_XXX;

#define RESET_EXCEPTION_FROM_STACK_PROBE_FOR_THROW(pGuard)                           \
        pGuard->SetNoException ();

#define POP_STACK_PROBE_FOR_THROW(pGuard) \
    pGuard->CheckStack();

//=============================================================================
// Macros for transition into SO_TOLERANT code
//=============================================================================
// @todo : put this assert in when all probes are in place.
// _ASSERTE(! pThread->IsSOTolerant());

//*********************************************************************************

// A boundary stack guard is pushed onto the probe stack when we leave the EE and
// popped when we return.  It is used for 1) restoring the original probe's cookie
// when we return, as managed code could trash it and 2) marking a boundary so that
// we know not to check for over-written probes before it when install a real probe.
//
class BoundaryStackGuard : public BaseStackGuard
{
protected:
    BoundaryStackGuard()
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(!"No default construction allowed");
    }

public:
    DEBUG_NOINLINE BoundaryStackGuard(const char *szFunction, const char *szFile, unsigned int lineNum)
        : BaseStackGuard(szFunction, szFile, lineNum)
    {
        SCAN_SCOPE_BEGIN;
        ANNOTATION_FN_SO_TOLERANT;

        m_isBoundaryGuard = TRUE;
    }

    DEBUG_NOINLINE void Push();
    DEBUG_NOINLINE void Pop();

    DEBUG_NOINLINE void SetNoExceptionNoPop()
    {
        SCAN_SCOPE_END;
        SetNoException();
    }

};

// Derived version, add a dtor that automatically calls Pop, more convenient, but can't use with SEH.
class AutoCleanupBoundaryStackGuard : public BoundaryStackGuard
{
protected:
    AutoCleanupBoundaryStackGuard()
    {
        _ASSERTE(!"No default construction allowed");
    }

public:
    DEBUG_NOINLINE AutoCleanupBoundaryStackGuard(const char *szFunction, const char *szFile, unsigned int lineNum) :
        BoundaryStackGuard(szFunction, szFile, lineNum)
    {
        SCAN_SCOPE_BEGIN;
        ANNOTATION_FN_SO_TOLERANT;
    }

    DEBUG_NOINLINE ~AutoCleanupBoundaryStackGuard()
    {
        SCAN_SCOPE_END;
        Pop();
    }
};


class DebugSOTolerantTransitionHandler
{
    BOOL m_prevSOTolerantState;
    ClrDebugState* m_clrDebugState;

  public:
    void EnterSOTolerantCode(Thread *pThread);
    void ReturnFromSOTolerantCode();
};

class AutoCleanupDebugSOTolerantTransitionHandler : DebugSOTolerantTransitionHandler
{
    BOOL m_prevSOTolerantState;
    ClrDebugState* m_clrDebugState;

  public:
    DEBUG_NOINLINE AutoCleanupDebugSOTolerantTransitionHandler(Thread *pThread)
    {
        SCAN_SCOPE_BEGIN;
        ANNOTATION_FN_SO_INTOLERANT;

        EnterSOTolerantCode(pThread);
    }
    DEBUG_NOINLINE ~AutoCleanupDebugSOTolerantTransitionHandler()
    {
        SCAN_SCOPE_END;

        ReturnFromSOTolerantCode();
    }
};


// When we enter SO-tolerant code, we
// 1) probe to make sure that we will have enough stack to run our backout code.  We don't
//    need to check that the cookie was overrun because we only care that we had enough stack.
//    But we do anyway, to pop off the guard.s
//    The backout code infrastcture ensures that we stay below the BACKOUT_CODE_STACK_LIMIT.
// 2) Install a boundary guard, which will preserve our cookie and prevent spurious checks if
//    we call back into the EE.
// 3) Formally transition into SO-tolerant code so that we can make sure we are probing if we call
//    back into the EE.
//

#undef OPTIONAL_SO_CLEANUP_UNWIND
#define OPTIONAL_SO_CLEANUP_UNWIND(pThread, pFrame)

#define BSTC_RECOVER_STACK              0x1
#define BSTC_IS_SO                      0x2
#define BSTC_IS_SOFT_SO                 0x4
#define BSTC_TRIGGERING_UNWIND_FOR_SO   0x8

#define BEGIN_SO_TOLERANT_CODE(pThread)                                                     \
    { /* add an outer scope so that we'll restore our state as soon as we return */         \
        Thread * const __pThread = pThread;                                                 \
        DWORD __dwFlags = 0;                                                                \
        Frame * __pSafeForSOFrame = __pThread ? __pThread->GetFrame() : NULL;               \
        SCAN_BLOCKMARKER();                                                                 \
        SCAN_BLOCKMARKER_MARK();                                                            \
        BoundaryStackGuard boundary_guard_XXX(__FUNCTION__, __FILE__, __LINE__);            \
        boundary_guard_XXX.Push();                                                          \
        DebugSOTolerantTransitionHandler __soTolerantTransitionHandler;                     \
        __soTolerantTransitionHandler.EnterSOTolerantCode(__pThread);                       \
        __try                                                                               \
        {                                                                                   \
            SCAN_EHMARKER();                                                                \
            __try                                                                           \
            {                                                                               \
                SCAN_EHMARKER_TRY();                                                        \
                DEBUG_ASSURE_NO_RETURN_BEGIN(STACK_PROBE)                                   \
                __try                                                                       \
                {                                                                           


// We need to catch any hard SO that comes through in order to get our stack back and make sure that we can run our backout code.
// Also can't allow a hard SO to propogate into SO-intolerant code, as we can't tell where it came from and would have to rip the process.
// So install a filter and catch hard SO and rethrow a C++ SO.  Note that we don't check the host policy here it only applies to exceptions
// that will leak back into managed code.
#define END_SO_TOLERANT_CODE                                                                \
                }                                                                           \
                __finally                                                                   \
                {                                                                           \
                    STATIC_CONTRACT_SO_TOLERANT;                                            \
                    if (__dwFlags & BSTC_TRIGGERING_UNWIND_FOR_SO)                          \
                    {                                                                       \
                        OPTIONAL_SO_CLEANUP_UNWIND(__pThread, __pSafeForSOFrame)            \
                    }                                                                       \
                }                                                                           \
                DEBUG_ASSURE_NO_RETURN_END(STACK_PROBE)                                     \
                boundary_guard_XXX.SetNoException();                                        \
                SCAN_EHMARKER_END_TRY();                                                    \
            }                                                                               \
            __except(SOTolerantBoundaryFilter(GetExceptionInformation(), &__dwFlags))       \
            {                                                                               \
                SCAN_EHMARKER_CATCH();                                                      \
                __soTolerantTransitionHandler.ReturnFromSOTolerantCode();                   \
                SOTolerantCode_ExceptBody(&__dwFlags, __pSafeForSOFrame);                   \
                SCAN_EHMARKER_END_CATCH();                                                  \
            }                                                                               \
            /* This will correctly set the annotation back to SOIntolerant if needed */     \
            SCAN_BLOCKMARKER_USE();                                                         \
            if (__dwFlags & BSTC_RECOVER_STACK)                                             \
            {                                                                               \
                SOTolerantCode_RecoverStack(__dwFlags);                                     \
            }                                                                               \
        }                                                                                   \
        __finally                                                                           \
        {                                                                                   \
            __soTolerantTransitionHandler.ReturnFromSOTolerantCode();                       \
            boundary_guard_XXX.Pop();                                                       \
        }                                                                                   \
        /* This is actually attached to the SCAN_BLOCKMARKER_USE() in the try scope */      \
        /* but should hopefully chain the right annotations for a call to a __finally */    \
        SCAN_BLOCKMARKER_END_USE();                                                         \
    }                                                                                       

extern unsigned __int64 getTimeStamp();

INDEBUG(void AddHostCallsStaticMarker();)

// This is used for calling into host
// We only need to install the boundary guard, and transition into SO-tolerant code.
#define BEGIN_SO_TOLERANT_CODE_CALLING_HOST(pThread)                                        \
    {                                                                                       \
        ULONGLONG __entryTime = 0;                                                          \
        __int64 __entryTimeStamp = 0;                                                       \
        _ASSERTE(CanThisThreadCallIntoHost());                                              \
        _ASSERTE((pThread == NULL) ||                                                       \
                (pThread->GetClrDebugState() == NULL) ||                                    \
                ((pThread->GetClrDebugState()->ViolationMask() &                            \
                                (HostViolation|BadDebugState)) != 0) ||                     \
                (pThread->GetClrDebugState()->IsHostCaller()));                             \
        INDEBUG(AddHostCallsStaticMarker();)                                                \
        _ASSERTE(pThread == NULL || !pThread->IsInForbidSuspendRegion());                   \
        {                                                                                   \
        AutoCleanupBoundaryStackGuard boundary_guard_XXX(__FUNCTION__, __FILE__, __LINE__); \
        boundary_guard_XXX.Push();                                                          \
        AutoCleanupDebugSOTolerantTransitionHandler __soTolerantTransitionHandler(pThread); \
        DEBUG_ASSURE_NO_RETURN_BEGIN(STACK_PROBE);                                          \

#define END_SO_TOLERANT_CODE_CALLING_HOST                                                   \
            DEBUG_ASSURE_NO_RETURN_END(STACK_PROBE)                                         \
            boundary_guard_XXX.SetNoExceptionNoPop();                                       \
        }                                                                                   \
    }

//-----------------------------------------------------------------------------
// Startup & Shutdown stack guard subsystem
//-----------------------------------------------------------------------------
void InitStackProbes();
void TerminateStackProbes();

#elif defined(STACK_GUARDS_RELEASE)
//=============================================================================
// Release - really streamlined,
//=============================================================================

void InitStackProbesRetail();
inline void InitStackProbes()
{
    InitStackProbesRetail();
}

inline void TerminateStackProbes()
{
    LIMITED_METHOD_CONTRACT;
}


//=============================================================================
// Macros for transition into SO_INTOLERANT code
//=============================================================================

FORCEINLINE DWORD DefaultEntryProbeAmount() { return DEFAULT_ENTRY_PROBE_AMOUNT; }

#define BEGIN_SO_INTOLERANT_CODE(pThread)                                           \
{                                                                                   \
    if (IsStackProbingEnabled()) DefaultRetailStackProbeWorker(pThread);            \
    /* match with the else used in other macros */                                  \
    if (true) {                                                                     \
        SOIntolerantTransitionHandler __soIntolerantTransitionHandler;              \
        /* work around unreachable code warning */                                  \
        if (true) {                                                                 \
            DEBUG_ASSURE_NO_RETURN_BEGIN(SO_INTOLERANT)

#define BEGIN_SO_INTOLERANT_CODE_FOR(pThread, n)                                    \
{                                                                                   \
    if (IsStackProbingEnabled()) RetailStackProbeWorker(ADJUST_PROBE(n), pThread);  \
    /* match with the else used in other macros */                                  \
    if (true) {                                                                     \
        SOIntolerantTransitionHandler __soIntolerantTransitionHandler;              \
        /* work around unreachable code warning */                                  \
        if (true) {                                                                 \
            DEBUG_ASSURE_NO_RETURN_BEGIN(SO_INTOLERANT)

#define BEGIN_SO_INTOLERANT_CODE_NOTHROW(pThread, ActionOnSO) \
{                                                                                   \
    if (IsStackProbingEnabled() && !RetailStackProbeNoThrowWorker(ADJUST_PROBE(DEFAULT_ENTRY_PROBE_AMOUNT), pThread)) \
    { \
        ActionOnSO; \
    } else { \
        SOIntolerantTransitionHandler __soIntolerantTransitionHandler;              \
        /* work around unreachable code warning */                                          \
        if (true) {                                                             \
            DEBUG_ASSURE_NO_RETURN_BEGIN(SO_INTOLERANT)


// This is defined just for using in the InternalSetupForComCall macro which
// doesn't have a corresponding end macro because no exception will pass through it
// It should not be used in any situation where an exception could pass through
// the transition.
#define SO_INTOLERANT_CODE_NOTHROW(pThread, ActionOnSO) \
    if (IsStackProbingEnabled() && !RetailStackProbeNoThrowWorker(ADJUST_PROBE(DEFAULT_ENTRY_PROBE_AMOUNT), pThread)) \
    { \
        ActionOnSO; \
    } \

#define MINIMAL_STACK_PROBE_CHECK_THREAD(pThread)                                               \
        if (IsStackProbingEnabled())                                                            \
        {                                                                                       \
            Thread *__pThread = pThread;                                                        \
            if (__pThread && ! __pThread->IsStackSpaceAvailable(MINIMUM_STACK_REQUIREMENT))     \
            {                                                                                   \
                ReportStackOverflow();                                                          \
            }                                                                                   \
        }

#define DECLARE_INTERIOR_STACK_PROBE


#define DO_INTERIOR_STACK_PROBE_FOR(pThread, n)                                 \
    if (IsStackProbingEnabled())                                                \
    {                                                                           \
        RetailStackProbeWorker(ADJUST_PROBE(n), pThread);                       \
    }

#define DO_INTERIOR_STACK_PROBE_FOR_CHECK_THREAD(n)                             \
    if (IsStackProbingEnabled() && ShouldProbeOnThisThread())                   \
    {                                                                           \
        RetailStackProbeWorker(ADJUST_PROBE(n), GetThread());                   \
    }

#define DO_INTERIOR_STACK_PROBE_FOR_NOTHROW(pThread, n, ActionOnSO)             \
    if (IsStackProbingEnabled())                                                \
    {                                                                           \
        if (!RetailStackProbeNoThrowWorker(ADJUST_PROBE(n), pThread))           \
        {                                                                       \
            ActionOnSO;                                                         \
        }                                                                       \
    }

#define DO_INTERIOR_STACK_PROBE_FOR_NOTHROW_CHECK_THREAD(n, ActionOnSO)         \
    if (IsStackProbingEnabled() && ShouldProbeOnThisThread())                   \
    {                                                                           \
        if (!RetailStackProbeNoThrowWorker(ADJUST_PROBE(n), GetThread()))       \
        {                                                                       \
            ActionOnSO;                                                         \
        }                                                                       \
    }


#define INTERIOR_STACK_PROBE_FOR(pThread, n) \
    DECLARE_INTERIOR_STACK_PROBE; \
    DO_INTERIOR_STACK_PROBE_FOR(pThread, n)

#define INTERIOR_STACK_PROBE_FOR_CHECK_THREAD(n) \
    DECLARE_INTERIOR_STACK_PROBE; \
    DO_INTERIOR_STACK_PROBE_FOR_CHECK_THREAD(n)

#define INTERIOR_STACK_PROBE_FOR_NOTHROW(pThread, n, ActionOnSO) \
    DECLARE_INTERIOR_STACK_PROBE; \
    DO_INTERIOR_STACK_PROBE_FOR_NOTHROW(pThread, n, ActionOnSO)

#define INTERIOR_STACK_PROBE_FOR_NOTHROW_CHECK_THREAD(n, ActionOnSO) \
    DECLARE_INTERIOR_STACK_PROBE; \
    DO_INTERIOR_STACK_PROBE_FOR_NOTHROW_CHECK_THREAD(n, ActionOnSO)


#define INTERIOR_STACK_PROBE(pThread) \
    INTERIOR_STACK_PROBE_FOR(pThread, DEFAULT_INTERIOR_PROBE_AMOUNT)

#define INTERIOR_STACK_PROBE_CHECK_THREAD \
    INTERIOR_STACK_PROBE_FOR_CHECK_THREAD(DEFAULT_INTERIOR_PROBE_AMOUNT)

#define INTERIOR_STACK_PROBE_NOTHROW(pThread, ActionOnSO) \
    INTERIOR_STACK_PROBE_FOR_NOTHROW(pThread, DEFAULT_INTERIOR_PROBE_AMOUNT, ActionOnSO)

#define INTERIOR_STACK_PROBE_NOTHROW_CHECK_THREAD(ActionOnSO) \
    INTERIOR_STACK_PROBE_FOR_NOTHROW_CHECK_THREAD(DEFAULT_INTERIOR_PROBE_AMOUNT, ActionOnSO)


#define END_INTERIOR_STACK_PROBE

#define RETURN_FROM_INTERIOR_PROBE(x) RETURN(x)


// This is used for EH code where we are about to throw
// To avoid taking an SO during EH processing, want to include it in our probe limits
// So we will just do a big probe and then throw.
#define STACK_PROBE_FOR_THROW(pThread)                                      \
    if (pThread != NULL)                                                    \
    {                                                                       \
        RetailStackProbe(ADJUST_PROBE(DEFAULT_ENTRY_PROBE_AMOUNT), pThread); \
    }                                                                       \

#define PUSH_STACK_PROBE_FOR_THROW(pThread)                                     \
    RetailStackProbe(ADJUST_PROBE(DEFAULT_ENTRY_PROBE_AMOUNT), pThread);

#define SAVE_ADDRESS_OF_STACK_PROBE_FOR_THROW(pGuard)

#define POP_STACK_PROBE_FOR_THROW(pGuard)


//=============================================================================
// Macros for transition into SO_TOLERANT code
//=============================================================================

#undef OPTIONAL_SO_CLEANUP_UNWIND
#define OPTIONAL_SO_CLEANUP_UNWIND(pThread, pFrame)

#define BSTC_RECOVER_STACK              0x1
#define BSTC_IS_SO                      0x2
#define BSTC_IS_SOFT_SO                 0x4
#define BSTC_TRIGGERING_UNWIND_FOR_SO   0x8


#define BEGIN_SO_TOLERANT_CODE(pThread)                                                     \
{                                                                                           \
    Thread * __pThread = pThread;                                                           \
    DWORD __dwFlags = 0;                                                                    \
    Frame * __pSafeForSOFrame = __pThread ? __pThread->GetFrame() : NULL;                   \
    SCAN_BLOCKMARKER();                                                                     \
    SCAN_BLOCKMARKER_MARK();                                                                \
    SCAN_EHMARKER();                                                                        \
    __try                                                                                   \
    {                                                                                       \
        SCAN_EHMARKER_TRY()                                                                 \
        __try                                                                               \
        {

// We need to catch any hard SO that comes through in order to get our stack back and make sure that we can run our backout code.
// Also can't allow a hard SO to propogate into SO-intolerant code, as we can't tell where it came from and would have to rip the process.
// So install a filter and catch hard SO and rethrow a C++ SO.
#define END_SO_TOLERANT_CODE                                                                 \
        }                                                                                    \
        __finally                                                                            \
        {                                                                                    \
            STATIC_CONTRACT_SO_TOLERANT;                                                     \
            if (__dwFlags & BSTC_TRIGGERING_UNWIND_FOR_SO)                                   \
            {                                                                                \
                OPTIONAL_SO_CLEANUP_UNWIND(__pThread, __pSafeForSOFrame)                     \
            }                                                                                \
        }                                                                                    \
        SCAN_EHMARKER_END_TRY();                                                             \
    }                                                                                        \
    __except(SOTolerantBoundaryFilter(GetExceptionInformation(), &__dwFlags))                \
    {                                                                                        \
        SCAN_EHMARKER_CATCH();                                                               \
        SOTolerantCode_ExceptBody(&__dwFlags, __pSafeForSOFrame);                            \
        SCAN_EHMARKER_END_CATCH();                                                           \
    }                                                                                        \
    SCAN_BLOCKMARKER_USE();                                                                  \
    if (__dwFlags & BSTC_RECOVER_STACK)                                                      \
    {                                                                                        \
        SOTolerantCode_RecoverStack(__dwFlags);                                              \
    }                                                                                        \
    SCAN_BLOCKMARKER_END_USE();                                                              \
}

#define BEGIN_SO_TOLERANT_CODE_CALLING_HOST(pThread)                                         \
    {                                                                                        \

#define END_SO_TOLERANT_CODE_CALLING_HOST                                                    \
    }

#endif

#else // FEATURE_STACK_PROBE && !DACCESS_COMPILE

inline void InitStackProbes()
{
    LIMITED_METHOD_CONTRACT;
}

inline void TerminateStackProbes()
{
    LIMITED_METHOD_CONTRACT;
}

#define BEGIN_SO_INTOLERANT_CODE(pThread)
#define BEGIN_SO_INTOLERANT_CODE_FOR(pThread, n)
#define BEGIN_SO_INTOLERANT_CODE_NOTHROW(pThread, ActionOnSO)
#define SO_INTOLERANT_CODE_NOTHROW(pThread, ActionOnSO)
#define MINIMAL_STACK_PROBE_CHECK_THREAD(pThread)

#define DECLARE_INTERIOR_STACK_PROBE

#define DO_INTERIOR_STACK_PROBE_FOR(pThread, n)
#define DO_INTERIOR_STACK_PROBE_FOR_CHECK_THREAD(n)
#define DO_INTERIOR_STACK_PROBE_FOR_NOTHROW(pThread, n, ActionOnSO)
#define DO_INTERIOR_STACK_PROBE_FOR_NOTHROW_CHECK_THREAD(n, ActionOnSO)

#define INTERIOR_STACK_PROBE_FOR(pThread, n)
#define INTERIOR_STACK_PROBE_FOR_CHECK_THREAD(n)
#define INTERIOR_STACK_PROBE_FOR_NOTHROW(pThread, n, ActionOnSO)
#define INTERIOR_STACK_PROBE_FOR_NOTHROW_CHECK_THREAD(n, ActionOnSO)

#define INTERIOR_STACK_PROBE(pThread)
#define INTERIOR_STACK_PROBE_CHECK_THREAD
#define INTERIOR_STACK_PROBE_NOTHROW(pThread, ActionOnSO)
#define INTERIOR_STACK_PROBE_NOTHROW_CHECK_THREAD(ActionOnSO)

#define END_INTERIOR_STACK_PROBE
#define RETURN_FROM_INTERIOR_PROBE(x) RETURN(x)

#define STACK_PROBE_FOR_THROW(pThread)
#define PUSH_STACK_PROBE_FOR_THROW(pThread)
#define SAVE_ADDRESS_OF_STACK_PROBE_FOR_THROW(pGuard)
#define POP_STACK_PROBE_FOR_THROW(pGuard)

#define BEGIN_SO_TOLERANT_CODE(pThread)
#define END_SO_TOLERANT_CODE
#define RETURN_FROM_SO_TOLERANT_CODE_HAS_CATCH
#define BEGIN_SO_TOLERANT_CODE_CALLING_HOST(pThread) \
    _ASSERTE(CanThisThreadCallIntoHost());
    
#define END_SO_TOLERANT_CODE_CALLING_HOST

#endif // FEATURE_STACK_PROBE && !DACCESS_COMPILE

#endif  // __STACKPROBE_h__
