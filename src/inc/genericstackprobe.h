// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//
//-----------------------------------------------------------------------------
// Generic Stack Probe Code
// Used to setup stack guards and probes outside the VM tree
//-----------------------------------------------------------------------------

#ifndef __GENERICSTACKPROBE_h__
#define __GENERICSTACKPROBE_h__

#include "staticcontract.h"
#include "predeftlsslot.h"

#if defined(DISABLE_CONTRACTS)
#undef FEATURE_STACK_PROBE
#endif

#if defined(FEATURE_STACK_PROBE)
#ifdef _DEBUG
#define STACK_GUARDS_DEBUG
#else
#define STACK_GUARDS_RELEASE
#endif
#endif

#ifdef FEATURE_STACK_PROBE
#define SO_INFRASTRUCTURE_CODE(x) x
#define NO_SO_INFRASTRUCTURE_CODE_ASSERTE(x)
#else
#define SO_INFRASTRUCTURE_CODE(x)
#define NO_SO_INFRASTRUCTURE_CODE_ASSERTE(x) _ASSERTE(x);
#endif

/* This macro is redefined in stackprobe.h
 * so that code expanded using this macro is present only for files
 * within VM directory. See StackProbe.h for more details
 */
#define VM_NO_SO_INFRASTRUCTURE_CODE(x)

// The types of stack validation we support in holders.
enum HolderStackValidation
{
    HSV_NoValidation,
    HSV_ValidateMinimumStackReq,
    HSV_ValidateNormalStackReq,        
};

// Used to track transitions into the profiler
#define REMOVE_STACK_GUARD_FOR_PROFILER_CALL \
        REMOVE_STACK_GUARD

// For AMD64, the stack size is 4K, same as X86, but the pointer size is 64, so the
// stack tends to grow a lot faster than X86.
#ifdef _TARGET_AMD64_
#define ADJUST_PROBE(n)  (2 * (n))
#else 
#define ADJUST_PROBE(n)  (n)
#endif

#if defined(FEATURE_STACK_PROBE)

#ifdef STACK_GUARDS_DEBUG // DAC and non-DAC - all data structures referenced in DAC'ized code
                          // must be included so we can calculate layout. SO probes are not
                          // active in the DAC but the SO probe structures contribute to layout
                          

// This class is used to place a marker upstack and verify that it was not overrun.  It is
// different from the full blown stack probes in that it does not chain with other probes or
// test for stack overflow.  Its sole purpose is to verify stack consumption.
// It is effectively an implicit probe though, because we are guaranteeing that we have
// enought stack to run and will not take an SO.  So we enter SO-intolerant code when
// we install one of these.

class StackMarkerStack;
struct ClrDebugState;

class BaseStackMarker
{
    friend StackMarkerStack;

    ClrDebugState  *m_pDebugState;  
    BOOL            m_prevWasSOTolerant;   // Were we SO-tolerant when we came in? 
    BOOL            m_fMarkerSet;          // Has the marker been set?
    BOOL            m_fTemporarilyDisabled;// Has the marker been temporarely disabled?
    BOOL            m_fAddedToStack;       // Has this BaseStackMarker been added to the stack of markers for the thread.
    float           m_numPages;
    UINT_PTR       *m_pMarker;    // Pointer to where to put our marker cookie on the stack.
    BaseStackMarker*m_pPrevious;
    BOOL            m_fProtectedStackPage;
    BOOL            m_fAllowDisabling;

    BaseStackMarker() {};   // no default construction allowed

    // These should only be called by the ClrDebugState.
    void RareDisableMarker();
    void RareReEnableMarker();        

  public:
    BaseStackMarker(float numPages, BOOL fAllowDisabling); 

    // we have this so that the check of the global can be inlined
    // and we don't make the call to CheckMarker unless we need to.
    void CheckForBackoutViolation();

    void SetMarker(float numPages);
    void CheckMarker();
    
    void ProtectMarkerPageInDebugger();
    void UndoPageProtectionInDebugger();
    
};

class StackMarkerStack
{
public:
    // Since this is used from the ClrDebugState which can't have a default constructor,
    // we need to provide an Init method to intialize the instance instead of having a constructor.
    void Init() 
    {
        m_pTopStackMarker = NULL;
        m_fDisabled = FALSE;
    }
            
    void PushStackMarker(BaseStackMarker *pStackMarker);
    BaseStackMarker *PopStackMarker();
    
    BOOL IsEmpty()
    {
        return (m_pTopStackMarker == NULL);
    } 
    BOOL IsDisabled()
    {
        return m_fDisabled;
    }

    void RareDisableStackMarkers();
    void RareReEnableStackMarkers();

private:
    BaseStackMarker     *m_pTopStackMarker;     // The top of the stack of stack markers for the current thread.
    BOOL                m_fDisabled;
};

#endif // STACK_GUARDS_DEBUG

#if !defined(DACCESS_COMPILE)

// In debug builds, we redefine DEFAULT_ENTRY_PROBE_AMOUNT to a global static
// so that we can tune the entry point probe size at runtime.
#define DEFAULT_ENTRY_PROBE_SIZE 12
#define DEFAULT_ENTRY_PROBE_AMOUNT DEFAULT_ENTRY_PROBE_SIZE

#define BACKOUT_CODE_STACK_LIMIT 4.0
#define HOLDER_CODE_NORMAL_STACK_LIMIT BACKOUT_CODE_STACK_LIMIT
#define HOLDER_CODE_MINIMUM_STACK_LIMIT 0.25

void DontCallDirectlyForceStackOverflow();
void SOBackoutViolation(const char *szFunction, const char *szFile, int lineNum); 
typedef void *EEThreadHandle;
class SOIntolerantTransitionHandler;
extern bool g_StackProbingEnabled;
extern void (*g_fpCheckForSOInSOIntolerantCode)();
extern void (*g_fpSetSOIntolerantTransitionMarker)();
extern BOOL (*g_fpDoProbe)(unsigned int n);
extern void (*g_fpHandleSoftStackOverflow)(BOOL fSkipDebugger);

// Once we enter SO-intolerant code, we can never take a hard SO as we will be 
// in an unknown state. SOIntolerantTransitionHandler is used to detect a hard SO in SO-intolerant
// code and to raise a Fatal Error if one occurs.
class SOIntolerantTransitionHandler
{
private:
    bool   m_exceptionOccurred;
    void * m_pPreviousHandler;
    
public:
    FORCEINLINE SOIntolerantTransitionHandler() 
    {
        if (g_StackProbingEnabled)
        {
            CtorImpl();
        }
    }

    FORCEINLINE ~SOIntolerantTransitionHandler()
    {
        if (g_StackProbingEnabled)
        {
            DtorImpl();
        }
    }

    NOINLINE void CtorImpl();
    NOINLINE void DtorImpl();

    void SetNoException()
    {
        m_exceptionOccurred = false;
    }

    bool DidExceptionOccur()
    {
        return m_exceptionOccurred;
    }
};


extern void (*g_fpHandleStackOverflowAfterCatch)();
void HandleStackOverflowAfterCatch();

#if defined(STACK_GUARDS_DEBUG)

#ifdef _WIN64
#define STACK_COOKIE_VALUE 0x0123456789ABCDEF
#define DISABLED_STACK_COOKIE_VALUE 0xDCDCDCDCDCDCDCDC
#else
#define STACK_COOKIE_VALUE 0x01234567
#define DISABLED_STACK_COOKIE_VALUE 0xDCDCDCDC
#endif

// This allows us to adjust the probe amount at run-time in checked builds
#undef DEFAULT_ENTRY_PROBE_AMOUNT
#define DEFAULT_ENTRY_PROBE_AMOUNT g_EntryPointProbeAmount

class BaseStackGuardGeneric;
class BaseStackGuard;

extern void (*g_fpRestoreCurrentStackGuard)(BOOL fDisabled);
extern BOOL (*g_fp_BaseStackGuard_RequiresNStackPages)(BaseStackGuardGeneric *pGuard, unsigned int n, BOOL fThrowOnSO);
extern void (*g_fp_BaseStackGuard_CheckStack)(BaseStackGuardGeneric *pGuard);
extern BOOL (*g_fpCheckNStackPagesAvailable)(unsigned int n);
extern BOOL  g_ProtectStackPagesInDebugger;
void RestoreSOToleranceState();
void EnsureSOTolerant();

extern BOOL g_EnableBackoutStackValidation;
extern DWORD g_EntryPointProbeAmount;

//-----------------------------------------------------------------------------
// Check if a cookie is still at the given marker
//-----------------------------------------------------------------------------
inline  BOOL IsMarkerOverrun(UINT_PTR *pMarker)
{
    return (*pMarker != STACK_COOKIE_VALUE);
}

class AutoCleanupStackMarker : public BaseStackMarker
{
public:
    DEBUG_NOINLINE AutoCleanupStackMarker(float numPages) : 
        BaseStackMarker(numPages, TRUE)
    {
        SCAN_SCOPE_BEGIN;
        ANNOTATION_FN_SO_INTOLERANT;
    }

    DEBUG_NOINLINE ~AutoCleanupStackMarker()
    {
        SCAN_SCOPE_END;
        CheckForBackoutViolation();
    }
};

#define VALIDATE_BACKOUT_STACK_CONSUMPTION \
    AutoCleanupStackMarker __stackMarker(ADJUST_PROBE(BACKOUT_CODE_STACK_LIMIT));

#define VALIDATE_BACKOUT_STACK_CONSUMPTION_FOR(numPages) \
    AutoCleanupStackMarker __stackMarker(ADJUST_PROBE(numPages));

#define UNSAFE_BEGIN_VALIDATE_BACKOUT_STACK_CONSUMPTION_NO_DISABLE \
    BaseStackMarker __stackMarkerNoDisable(ADJUST_PROBE(BACKOUT_CODE_STACK_LIMIT), FALSE);

#define UNSAFE_BEGIN_VALIDATE_BACKOUT_STACK_CONSUMPTION_NO_DISABLE_FOR(numPages) \
    BaseStackMarker __stackMarkerNoDisable(ADJUST_PROBE(numPages), FALSE);

#define UNSAFE_END_VALIDATE_BACKOUT_STACK_CONSUMPTION_NO_DISABLE \
        __stackMarkerNoDisable.CheckForBackoutViolation(); 

#define VALIDATE_HOLDER_STACK_CONSUMPTION_FOR_TYPE(validationType) \
    _ASSERTE(validationType != HSV_NoValidation);                  \
    AutoCleanupStackMarker __stackMarker(                          \
        ADJUST_PROBE(validationType == HSV_ValidateNormalStackReq ? HOLDER_CODE_NORMAL_STACK_LIMIT : HOLDER_CODE_MINIMUM_STACK_LIMIT));

class AutoCleanupDisableBackoutStackValidation
{
  public:
    AutoCleanupDisableBackoutStackValidation();
    ~AutoCleanupDisableBackoutStackValidation();
    
private:
    BOOL m_fAlreadyDisabled;

};

// This macros disables the backout stack validation in the current scope. It should 
// only be used in very rare situations. If you think you might have such a situation, 
// please talk to the stack overflow devs before using it.
#define DISABLE_BACKOUT_STACK_VALIDATION \
    AutoCleanupDisableBackoutStackValidation __disableBacoutStackValidation;

// In debug mode, we want to do a little more work on this transition to note the transition in the thread.
class DebugSOIntolerantTransitionHandler : public SOIntolerantTransitionHandler
{
    BOOL m_prevSOTolerantState;
    ClrDebugState* m_clrDebugState;

  public: 
    DebugSOIntolerantTransitionHandler(); 
    ~DebugSOIntolerantTransitionHandler();
};

// This is the base class structure for our probe infrastructure.  We declare it here
// so that we can properly declare instances outside of the VM tree.  But we only do the
// probes when we have a managed thread.
class BaseStackGuardGeneric
{
public:
    enum
    {
        cPartialInit,       // Not yet intialized
        cInit,              // Initialized and installed
        cUnwound,           // Unwound on a normal path (used for debugging)
        cEHUnwound          // Unwound on an exception path (used for debugging)
    } m_eInitialized;
        
    // *** Following fields must not move.  The fault injection framework depends on them.
    BaseStackGuard *m_pPrevGuard; // Previous guard for this thread.
    UINT_PTR       *m_pMarker;    // Pointer to where to put our marker cookie on the stack.
    unsigned int    m_numPages;        // space needed, specified in number of pages
    BOOL            m_isBoundaryGuard;  // used to mark when we've left the EE
    BOOL            m_fDisabled;       // Used to enable/disable stack guard


    // *** End of fault injection-dependent fields

    // The following fields are really here to provide us with some nice debugging information.
    const char     *m_szFunction;
    const char     *m_szFile;
    unsigned int    m_lineNum;
    const char     *m_szNextFunction;       // Name of the probe that came after us.
    const char     *m_szNextFile;
    unsigned int    m_nextLineNum;
    DWORD           m_UniqueId;
    unsigned int    m_depth;                // How deep is this guard in the list of guards for this thread?
    BOOL            m_fProtectedStackPage;  // TRUE if we protected a stack page with PAGE_NOACCESS.
    BOOL            m_fEHInProgress;        // Is an EH in progress?  This is cleared on a catch.
    BOOL            m_exceptionOccurred;     // Did an exception occur through this probe?

protected:
    BaseStackGuardGeneric()
    {
    }

public:
    BaseStackGuardGeneric(const char *szFunction, const char *szFile, unsigned int lineNum) :
        m_pPrevGuard(NULL), m_pMarker(NULL), 
        m_szFunction(szFunction), m_szFile(szFile), m_lineNum(lineNum),
        m_szNextFunction(NULL), m_szNextFile(NULL), m_nextLineNum(0),
        m_fProtectedStackPage(FALSE), m_UniqueId(-1), m_numPages(0), 
        m_eInitialized(cPartialInit), m_fDisabled(FALSE),
        m_isBoundaryGuard(FALSE),
        m_fEHInProgress(FALSE),     
        m_exceptionOccurred(FALSE)
    { 
        STATIC_CONTRACT_LEAF;
    }

    BOOL RequiresNStackPages(unsigned int n, BOOL fThrowOnSO = TRUE)
    {
        if (g_fp_BaseStackGuard_RequiresNStackPages == NULL)
        {
            return TRUE;
        }
        return g_fp_BaseStackGuard_RequiresNStackPages(this, n, fThrowOnSO);
    }

    BOOL RequiresNStackPagesThrowing(unsigned int n)
    {
        if (g_fp_BaseStackGuard_RequiresNStackPages == NULL)
        {
            return TRUE;
        }
        return g_fp_BaseStackGuard_RequiresNStackPages(this, n, TRUE);
    }

    BOOL RequiresNStackPagesNoThrow(unsigned int n)
    {
        if (g_fp_BaseStackGuard_RequiresNStackPages == NULL)
        {
            return TRUE;
        }
        return g_fp_BaseStackGuard_RequiresNStackPages(this, n, FALSE);
    }

    void CheckStack()
    {
        if (m_eInitialized == cInit)
        {
            g_fp_BaseStackGuard_CheckStack(this);
        }
    }

    void SetNoException()
    {
        m_exceptionOccurred = FALSE;
    }

    BOOL DidExceptionOccur()
    {
        return m_exceptionOccurred;
    }

    BOOL Enabled()
    {
        return !m_fDisabled;
    }

    void DisableGuard()
    {
        // As long as we don't have threads mucking with other thread's stack
        // guards, we don't need to synchronize this.
        m_fDisabled = TRUE;
    }

    void EnableGuard()
    {
        // As long as we don't have threads mucking with other thread's stack
        // guards, we don't need to synchronize this.
        m_fDisabled = FALSE;
    }

    
};

class StackGuardDisabler
{
    BOOL m_fDisabledGuard;

public:
    StackGuardDisabler();
    ~StackGuardDisabler();
    void NeverRestoreGuard();

};



// Derived version, add a dtor that automatically calls Check_Stack, move convenient, but can't use with SEH.
class AutoCleanupStackGuardGeneric : public BaseStackGuardGeneric
{
protected:
    AutoCleanupStackGuardGeneric()
    {
    }
    
public:
    AutoCleanupStackGuardGeneric(const char *szFunction, const char *szFile, unsigned int lineNum) :
        BaseStackGuardGeneric(szFunction, szFile, lineNum)
    { 
        STATIC_CONTRACT_LEAF;
    }

    ~AutoCleanupStackGuardGeneric()
    { 
        STATIC_CONTRACT_WRAPPER;
        CheckStack(); 
    }
};


// Used to remove stack guard... (kind of like a poor man's BEGIN_SO_TOLERANT
#define REMOVE_STACK_GUARD \
        StackGuardDisabler __guardDisable;

// Used to transition into intolerant code when handling a SO
#define BEGIN_SO_INTOLERANT_CODE_NOPROBE                                                  \
    {                                                                                     \
        DebugSOIntolerantTransitionHandler __soIntolerantTransitionHandler;               \
        /* work around unreachable code warning */                                        \
        if (true)                                                                         \
        {                                                                                 \
            DEBUG_ASSURE_NO_RETURN_BEGIN(SO_INTOLERANT)

#define END_SO_INTOLERANT_CODE_NOPROBE                              \
            ;                                                       \
            DEBUG_ASSURE_NO_RETURN_END(SO_INTOLERANT)               \
        }                                                           \
        __soIntolerantTransitionHandler.SetNoException();           \
    }                                                               \
            


#define BEGIN_SO_INTOLERANT_CODE_NO_THROW_CHECK_THREAD(ActionOnSO)                        \
    {                                                                                       \
        AutoCleanupStackGuardGeneric stack_guard_XXX(__FUNCTION__, __FILE__, __LINE__);         \
        if (! stack_guard_XXX.RequiresNStackPagesNoThrow(ADJUST_PROBE(g_EntryPointProbeAmount))) \
        {                                                                                   \
            ActionOnSO;                                                                     \
        }                                                                                   \
        else                                                                                \
        {                                                                                   \
            DebugSOIntolerantTransitionHandler __soIntolerantTransitionHandler;             \
            ANNOTATION_SO_PROBE_BEGIN(DEFAULT_ENTRY_PROBE_AMOUNT);                          \
            if (true)                                                                       \
            {                                                                               \
                DEBUG_ASSURE_NO_RETURN_BEGIN(SO_INTOLERANT)


#define END_SO_INTOLERANT_CODE                                                              \
                ;                                                                           \
                DEBUG_ASSURE_NO_RETURN_END(SO_INTOLERANT)                                   \
            }                                                                               \
            ANNOTATION_SO_PROBE_END;                                                        \
            __soIntolerantTransitionHandler.SetNoException();                               \
            stack_guard_XXX.SetNoException();                                               \
        }                                                                                   \
    }                                                                                       \


#define BEGIN_SO_INTOLERANT_CODE_NO_THROW_CHECK_THREAD_FORCE_SO()                           \
    EnsureSOTolerant();                                                                     \
    BEGIN_SO_INTOLERANT_CODE_NO_THROW_CHECK_THREAD(DontCallDirectlyForceStackOverflow());   \
    

// Restores the SO-tolerance state and the marker for the current guard if any
#define RESTORE_SO_TOLERANCE_STATE \
    RestoreSOToleranceState();

#define HANDLE_STACKOVERFLOW_AFTER_CATCH \
    HandleStackOverflowAfterCatch()

#elif defined(STACK_GUARDS_RELEASE)

#define VALIDATE_BACKOUT_STACK_CONSUMPTION
#define VALIDATE_BACKOUT_STACK_CONSUMPTION_FOR
#define UNSAFE_BEGIN_VALIDATE_BACKOUT_STACK_CONSUMPTION_NO_DISABLE
#define UNSAFE_BEGIN_VALIDATE_BACKOUT_STACK_CONSUMPTION_NO_DISABLE_FOR(numPages)
#define UNSAFE_END_VALIDATE_BACKOUT_STACK_CONSUMPTION_NO_DISABLE
#define VALIDATE_HOLDER_STACK_CONSUMPTION_FOR_TYPE(validationType)
#define RESTORE_SO_TOLERANCE_STATE
#define HANDLE_STACKOVERFLOW_AFTER_CATCH \
    HandleStackOverflowAfterCatch()
#define DISABLE_BACKOUT_STACK_VALIDATION
#define BACKOUT_STACK_VALIDATION_VIOLATION
#define BEGIN_SO_INTOLERANT_CODE_NOPROBE                                                  
#define END_SO_INTOLERANT_CODE_NOPROBE                                                  
#define REMOVE_STACK_GUARD

#define BEGIN_SO_INTOLERANT_CODE_NO_THROW_CHECK_THREAD(ActionOnSO)                          \
    {                                                                                       \
        if (g_StackProbingEnabled && !g_fpDoProbe(ADJUST_PROBE(DEFAULT_ENTRY_PROBE_AMOUNT)))\
        {                                                                                   \
            ActionOnSO;                                                                     \
        } else {                                                                            \
            SOIntolerantTransitionHandler __soIntolerantTransitionHandler;                  \
            /* work around unreachable code warning */                                      \
            if (true)                                                                       \
            {                                                                               \
                DEBUG_ASSURE_NO_RETURN_BEGIN(SO_INTOLERANT)

#define BEGIN_SO_INTOLERANT_CODE_NO_THROW_CHECK_THREAD_FORCE_SO()                           \
    BEGIN_SO_INTOLERANT_CODE_NO_THROW_CHECK_THREAD(DontCallDirectlyForceStackOverflow());   \

#define END_SO_INTOLERANT_CODE                                                              \
                ;                                                                           \
                DEBUG_ASSURE_NO_RETURN_END(SO_INTOLERANT)                                   \
            }                                                                               \
            __soIntolerantTransitionHandler.SetNoException();                               \
        }                                                                                   \
    }

#endif

#endif // !DACCESS_COMPILE
#endif // FEATURE_STACK_PROBES

// if the feature is off or we are compiling for DAC, disable all the probes
#if !defined(FEATURE_STACK_PROBE) || defined(DACCESS_COMPILE)

#define VALIDATE_BACKOUT_STACK_CONSUMPTION
#define VALIDATE_BACKOUT_STACK_CONSUMPTION_FOR
#define UNSAFE_BEGIN_VALIDATE_BACKOUT_STACK_CONSUMPTION_NO_DISABLE
#define UNSAFE_BEGIN_VALIDATE_BACKOUT_STACK_CONSUMPTION_NO_DISABLE_FOR(numPages)
#define UNSAFE_END_VALIDATE_BACKOUT_STACK_CONSUMPTION_NO_DISABLE
#define VALIDATE_HOLDER_STACK_CONSUMPTION_FOR_TYPE(validationType)
#define BEGIN_SO_INTOLERANT_CODE_NO_THROW_CHECK_THREAD(ActionOnSO)
#define BEGIN_SO_INTOLERANT_CODE_NO_THROW_CHECK_THREAD_FORCE_SO()
#define END_SO_INTOLERANT_CODE
#define RESTORE_SO_TOLERANCE_STATE

#define HANDLE_STACKOVERFLOW_AFTER_CATCH

#define DISABLE_BACKOUT_STACK_VALIDATION
#define BACKOUT_STACK_VALIDATION_VIOLATION
#define BEGIN_SO_INTOLERANT_CODE_NOPROBE
#define END_SO_INTOLERANT_CODE_NOPROBE
#define REMOVE_STACK_GUARD

// Probe size is 0 as Stack Overflow probing is not enabled
#define DEFAULT_ENTRY_PROBE_AMOUNT 0

#define BACKOUT_CODE_STACK_LIMIT 0

#endif //!FEATURE_STACK_PROBE || DACCESS_COMPILE

#endif  // __GENERICSTACKPROBE_h__
