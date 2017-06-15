// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//
//*****************************************************************************
//  genericstackprobe.cpp
//
//  This contains code for generic SO stack probes outside the VM, where we don't have a thread object
//
//*****************************************************************************

#include "stdafx.h"                     // Precompiled header key.
#include "utilcode.h"
#include "genericstackprobe.h"
#include "log.h"

#if defined(FEATURE_STACK_PROBE) && !defined(DACCESS_COMPILE)

#ifdef ENABLE_CONTRACTS_IMPL
BOOL g_EnableDefaultRWValidation = FALSE;
#endif

bool g_StackProbingEnabled;
void (*g_fpCheckForSOInSOIntolerantCode)();
void (*g_fpSetSOIntolerantTransitionMarker)();
BOOL (*g_fpDoProbe)(unsigned int n);
void (*g_fpHandleSoftStackOverflow)(BOOL fSkipDebugger);

// This function is used for NO_THROW probes that have no error return path.  In this
// case, we'll just force a stack overflow exception.  Do not call it directly - use
// one of the FORCE_SO macros.
void DontCallDirectlyForceStackOverflow()
{
#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable:26001) // "Suppress PREFast warning about underflows"
#endif

    UINT_PTR *sp = NULL;
    // we don't have access to GetCurrentSP from here, so just get an approximation
    sp = (UINT_PTR *)&sp;
    while (TRUE)
    {
        sp -= (GetOsPageSize() / sizeof(UINT_PTR));
        *sp = NULL;
    }

#ifdef _PREFAST_
#pragma warning(pop)
#endif
}

void (*g_fpHandleStackOverflowAfterCatch)() = 0;

// HandleStackOverflowAfterCatch 
//
void HandleStackOverflowAfterCatch()
{
    if (!g_fpHandleStackOverflowAfterCatch)
    {
        // If g_fpUnwindGuardChainTo has not been set, then we haven't called InitStackProbes
        // and we aren't probing, so bail.
        return;
    }

    // Reset the SO-tolerance state and restore the current guard
    g_fpHandleStackOverflowAfterCatch();
}

NOINLINE void SOIntolerantTransitionHandler::CtorImpl()
{
    m_exceptionOccurred = true;
    m_pPreviousHandler = ClrFlsGetValue(TlsIdx_SOIntolerantTransitionHandler);
    g_fpSetSOIntolerantTransitionMarker();
}

NOINLINE void SOIntolerantTransitionHandler::DtorImpl()
{
    // if we take a stack overflow exception in SO intolerant code, then we must
    // rip the process.  We check this by determining if the SP is beyond the calculated
    // limit.   Checking for the guard page being present is too much overhead during
    // exception handling (if you can believe that) and impacts perf.

    if (m_exceptionOccurred)
    {
        g_fpCheckForSOInSOIntolerantCode();
    }

    ClrFlsSetValue(TlsIdx_SOIntolerantTransitionHandler, m_pPreviousHandler);
}

#ifdef STACK_GUARDS_DEBUG

// If this is TRUE, we'll make the stack page that we put our stack marker in PAGE_NOACCESS so that you get an AV
// as soon as you go past the stack guard.
BOOL  g_ProtectStackPagesInDebugger = FALSE;

// This is the smallest size backout probe for which we will try to do a virtual protect for debugging. 
// If this number is too small, the 1 page ganularity of VirtualProtect becomes a problem. This number 
// should be less than or equal to the default backout probe size.
#define MINIMUM_PAGES_FOR_DEBUGGER_PROTECTION 4.0

void (*g_fpRestoreCurrentStackGuard)(BOOL fDisabled) = 0;
BOOL g_EnableBackoutStackValidation = FALSE;
BOOL (*g_fpShouldValidateSOToleranceOnThisThread)() = 0;
BOOL (*g_fp_BaseStackGuard_RequiresNStackPages)(BaseStackGuardGeneric *pGuard, unsigned int n, BOOL fThrowOnSO) = NULL;
void (*g_fp_BaseStackGuard_CheckStack)(BaseStackGuardGeneric *pGuard) = NULL;
BOOL (*g_fpCheckNStackPagesAvailable)(unsigned int n) = NULL;

// Always initialize g_EntryPointProbeAmount to a valid value as there could be a race where a 
// function probes with g_EntryPointProbeAmount's value before it is initialized in InitStackProbes.
DWORD g_EntryPointProbeAmount = DEFAULT_ENTRY_PROBE_SIZE;

// RestoreSOToleranceState 
//
// Restores the EE SO-tolerance state after a catch.  

void RestoreSOToleranceState()
{
    if (!g_fpRestoreCurrentStackGuard)
    {
        // If g_fpUnwindGuardChainTo has not been set, then we haven't called InitStackProbes
        // and we aren't probing, so bail.
        return;
    }

    // Reset the SO-tolerance state and restore the current guard
    g_fpRestoreCurrentStackGuard(FALSE);
}

//
// EnsureSOTolerant ASSERTS if we are not in an SO-tolerant mode
//
void EnsureSOTolerant()
{
#ifdef ENABLE_CONTRACTS_IMPL
    ClrDebugState *pClrDebugState = GetClrDebugState();
    _ASSERTE(! pClrDebugState || pClrDebugState->IsSOTolerant());
#endif
}

DEBUG_NOINLINE DebugSOIntolerantTransitionHandler::DebugSOIntolerantTransitionHandler() 
    : SOIntolerantTransitionHandler()
{
    SCAN_SCOPE_BEGIN;
    // This CANNOT be a STATIC_CONTRACT_SO_INTOLERANT b/c that isn't
    // really just a static contract, it is actually calls EnsureSOIntolerantOK
    // as well. Instead we just use the annotation.
    ANNOTATION_FN_SO_INTOLERANT;
#ifdef ENABLE_CONTRACTS_IMPL
    m_clrDebugState = GetClrDebugState();
    if (m_clrDebugState)
    {
        m_prevSOTolerantState = m_clrDebugState->BeginSOIntolerant();
    }
#endif
}

DEBUG_NOINLINE DebugSOIntolerantTransitionHandler::~DebugSOIntolerantTransitionHandler()
{
    SCAN_SCOPE_END;

    if (m_clrDebugState)
    {
        m_clrDebugState->SetSOTolerance(m_prevSOTolerantState);
    }
}

// This is effectively an implicit probe, because we are guaranteeing that we have
// enought stack to run and will not take an SO.  So we enter SO-intolerant code when
// we install one of these.
DEBUG_NOINLINE BaseStackMarker::BaseStackMarker(float numPages, BOOL fAllowDisabling) 
        : m_prevWasSOTolerant(FALSE)
        , m_pDebugState(
#ifdef ENABLE_CONTRACTS_IMPL
        CheckClrDebugState()
#else
        NULL
#endif  
        )
        , m_fMarkerSet(FALSE) 
        , m_fTemporarilyDisabled(FALSE), m_fAddedToStack(FALSE), m_pPrevious(NULL)
        , m_numPages(0.0), m_pMarker(NULL)
        , m_fProtectedStackPage(FALSE), m_fAllowDisabling(fAllowDisabling)
{
    SCAN_SCOPE_BEGIN;
    // This CANNOT be a STATIC_CONTRACT_SO_INTOLERANT b/c that isn't
    // really just a static contract, it is actually calls EnsureSOIntolerantOK
    // as well. Instead we just use the annotation.
    ANNOTATION_FN_SO_INTOLERANT;

    {
        DEBUG_ONLY_REGION();
        // If backout stack validation isn't enabled then we are done.
        if (!g_EnableBackoutStackValidation)
        {
            return;
        }
        
        // If we can't talk to other markers then the markers could get in each others way
        if (!m_pDebugState)
        {
            return;
        }

        // Allow only the lowest marker to be active at any one time. Yes, this means that
        // the stack will only ever have one element in it. However having multiple markers
        // is problematic for debugging and conflicts with the VirtualProtect option. It
        // adds little value, in that small backout checks stop happening in exception
        // codepaths, but these get plenty of coverage in success cases and the lowest
        // placed marked is the one that could actually indicate a stack overflow.
        if (!m_pDebugState->GetStackMarkerStack().IsEmpty())
        {
            return;
        }

        // Switch the SO tolerance mode
        m_prevWasSOTolerant = m_pDebugState->SetSOTolerance(FALSE);

        // If we have less then numPages left before the end of the stack then there is
        // no point in adding a marker since we will take an SO anyway if we use too much
        // stack. Putting the marker is actually very bad since it artificially forces an
        // SO in cases where it wouldn't normally occur if we use less than num pages of stack.
        if (g_fpCheckNStackPagesAvailable && 
            !g_fpCheckNStackPagesAvailable(numPages < 1 ? 1 : (unsigned int)numPages))
        {
            return;
        }

        if (m_fAllowDisabling) 
        {
            // Push ourselves on to the stack of stack markers on the CLR debug state.
            m_pDebugState->GetStackMarkerStack().PushStackMarker(this);
            m_fAddedToStack = TRUE;
        }

        // Set the actual stack guard marker if we have enough stack to do so.
        SetMarker(numPages);

        if (m_fMarkerSet && m_fAllowDisabling)
        {
            ProtectMarkerPageInDebugger();
        }
    }
}

// we have this so that the check of the global can be inlined
// and we don't make the call to CheckMarker unless we need to.
DEBUG_NOINLINE void BaseStackMarker::CheckForBackoutViolation()
{
    SCAN_SCOPE_END;

    // If backout stack validation isn't enabled then we are done.
    if (!g_EnableBackoutStackValidation)
    {
        return;
    }

    {
        DEBUG_ONLY_REGION()

        // The marker should always be re-enabled at this point.
        CONSISTENCY_CHECK_MSG(!m_fTemporarilyDisabled, "The stack guard was disabled but not properly re-enabled. This is a bug somewhere in the code called after this marker has been set up.");

        if (!m_pDebugState || m_fTemporarilyDisabled)
        {
            return;
        }

        // Reset the SO tolerance of the thread.
        m_pDebugState->SetSOTolerance(m_prevWasSOTolerant);

        if (m_fAddedToStack)
        {
            // Pop ourselves off of the stack of stack markers on the CLR debug state.
            CONSISTENCY_CHECK(m_pDebugState != NULL);
            BaseStackMarker *pPopResult = m_pDebugState->GetStackMarkerStack().PopStackMarker();
            
            CONSISTENCY_CHECK_MSG(pPopResult == this, "The marker we pop off the stack should always be the current marker.");
            CONSISTENCY_CHECK_MSG(m_pPrevious == NULL, "PopStackMarker should reset the current marker's m_pPrevious field to NULL.");
        }

        // Not cancellable markers should only be checked when no cancellable markers are present.
        if (!m_fAllowDisabling && !(m_pDebugState->GetStackMarkerStack().IsEmpty())) 
        {
            return;
        }

        if (m_fProtectedStackPage) 
        {
            UndoPageProtectionInDebugger();
        }

        if (m_fMarkerSet)
        {
            // Check to see if we overwrote the stack guard marker.
            CheckMarker();
        }
    }
}

void BaseStackMarker::SetMarker(float numPages)
{
    LIMITED_METHOD_CONTRACT;
    STATIC_CONTRACT_DEBUG_ONLY;
    
    m_numPages = numPages;

    // Use the address of the argument to get the current stack pointer. Note that this
    // won't be the exact SP; however it will be close enough.
    LPVOID pStack = &numPages;

    UINT_PTR *pMarker = (UINT_PTR*)pStack  - (int)(GetOsPageSize() / sizeof(UINT_PTR) * m_numPages);
    
    // We might not have committed our stack yet, so allocate the number of pages
    // we need so that they will be commited and we won't AV when we try to set the mark.
    _alloca( (int)(GetOsPageSize() * m_numPages) );
    m_pMarker = pMarker;
    *m_pMarker = STACK_COOKIE_VALUE;

    m_fMarkerSet = TRUE;

}

void BaseStackMarker::RareDisableMarker()
{
    LIMITED_METHOD_CONTRACT;
    STATIC_CONTRACT_DEBUG_ONLY;
        
    if (m_fProtectedStackPage) 
    {
        UndoPageProtectionInDebugger();
    }

    m_fTemporarilyDisabled = TRUE;
    
    if (m_fMarkerSet) 
    {
        *m_pMarker = DISABLED_STACK_COOKIE_VALUE;
    }
}

void BaseStackMarker::RareReEnableMarker()
{
    LIMITED_METHOD_CONTRACT;
    STATIC_CONTRACT_DEBUG_ONLY;
    
    m_fTemporarilyDisabled = FALSE;

    if (m_fMarkerSet) {    
        *m_pMarker = STACK_COOKIE_VALUE;
    }

    if (m_fProtectedStackPage) 
    {
        ProtectMarkerPageInDebugger();
    }
}

//-----------------------------------------------------------------------------
// Protect the page where we put the marker if a debugger is attached. That way, you get an AV right away
// when you go past the stack guard when running under a debugger.
//-----------------------------------------------------------------------------
void BaseStackMarker::ProtectMarkerPageInDebugger()
{
    WRAPPER_NO_CONTRACT;
    DEBUG_ONLY_FUNCTION;

    if (!g_ProtectStackPagesInDebugger)
    {
        return;
    }
    
    if (m_numPages < MINIMUM_PAGES_FOR_DEBUGGER_PROTECTION) 
    {
        return;
    }

    DWORD flOldProtect;

    LOG((LF_EH, LL_INFO100000, "BSM::PMP: m_pMarker 0x%p, value 0x%p\n", m_pMarker, *m_pMarker));

    // We cannot call into host for VirtualProtect. EEVirtualProtect will try to restore previous
    // guard, but the location has been marked with PAGE_NOACCESS.
#undef VirtualProtect
    BOOL fSuccess = ::VirtualProtect(m_pMarker, 1, PAGE_NOACCESS, &flOldProtect);
    _ASSERTE(fSuccess);

#define VirtualProtect(lpAddress, dwSize, flNewProtect, lpflOldProtect) \
        Dont_Use_VirtualProtect(lpAddress, dwSize, flNewProtect, lpflOldProtect)

    m_fProtectedStackPage = fSuccess;
}

//-----------------------------------------------------------------------------
// Remove page protection installed for this probe
//-----------------------------------------------------------------------------
void BaseStackMarker::UndoPageProtectionInDebugger()
{
    WRAPPER_NO_CONTRACT;
    DEBUG_ONLY_FUNCTION;

    _ASSERTE(m_fProtectedStackPage);
    _ASSERTE(!m_fTemporarilyDisabled);

    DWORD flOldProtect;
    // EEVirtualProtect installs a BoundaryStackGuard.  To avoid recursion, we call
    // into OS for VirtualProtect instead.
#undef VirtualProtect
    BOOL fSuccess = ::VirtualProtect(m_pMarker, 1, PAGE_READWRITE, &flOldProtect);
    _ASSERTE(fSuccess);

    LOG((LF_EH, LL_INFO100000, "BSM::UMP m_pMarker 0x%p\n", m_pMarker));
    
#define VirtualProtect(lpAddress, dwSize, flNewProtect, lpflOldProtect) \
        Dont_Use_VirtualProtect(lpAddress, dwSize, flNewProtect, lpflOldProtect)
}

void BaseStackMarker::CheckMarker()
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_DEBUG_ONLY;
    
    if ( IsMarkerOverrun(m_pMarker) )
    {
        SOBackoutViolation(__FUNCTION__, __FILE__, __LINE__);
    }
}

AutoCleanupDisableBackoutStackValidation::AutoCleanupDisableBackoutStackValidation()
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_DEBUG_ONLY;
#ifdef ENABLE_CONTRACTS_IMPL
    m_fAlreadyDisabled = GetClrDebugState()->GetStackMarkerStack().IsDisabled();    
    if (!m_fAlreadyDisabled) 
    {
        GetClrDebugState()->GetStackMarkerStack().RareDisableStackMarkers();    
    }
#endif
}

AutoCleanupDisableBackoutStackValidation::~AutoCleanupDisableBackoutStackValidation()
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_DEBUG_ONLY;

#ifdef ENABLE_CONTRACTS_IMPL
    if (!m_fAlreadyDisabled) 
    {
        GetClrDebugState()->GetStackMarkerStack().RareReEnableStackMarkers();
    }
#endif
}

inline void StackMarkerStack::PushStackMarker(BaseStackMarker *pStackMarker)
{
    LIMITED_METHOD_CONTRACT;
    STATIC_CONTRACT_DEBUG_ONLY;

    pStackMarker->m_pPrevious = m_pTopStackMarker;       
    m_pTopStackMarker = pStackMarker;
}

BaseStackMarker *StackMarkerStack::PopStackMarker()
{
    LIMITED_METHOD_CONTRACT;
    STATIC_CONTRACT_DEBUG_ONLY;

    BaseStackMarker *pOldTop = m_pTopStackMarker;
    m_pTopStackMarker = pOldTop->m_pPrevious;
    pOldTop->m_pPrevious = NULL;
    return pOldTop;
}

void StackMarkerStack::RareDisableStackMarkers()
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_DEBUG_ONLY;

    // Walk up the stack of markers and disable them all.
    BaseStackMarker *pCurrentStackMarker = m_pTopStackMarker;
    while (pCurrentStackMarker)
    {
        pCurrentStackMarker->RareDisableMarker();
        pCurrentStackMarker = pCurrentStackMarker->m_pPrevious;
    }
    m_fDisabled = TRUE;
}

void StackMarkerStack::RareReEnableStackMarkers()
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_DEBUG_ONLY;

    // Walk up the stack of markers and re-enable them all.
    BaseStackMarker *pCurrentStackMarker = m_pTopStackMarker;
    while (pCurrentStackMarker)
    {
        pCurrentStackMarker->RareReEnableMarker();
        pCurrentStackMarker = pCurrentStackMarker->m_pPrevious;
    }
    m_fDisabled = FALSE;
}

#endif // STACK_GUARDS_DEBUG

#endif // FEATURE_STACK_PROBE && !DACCESS_COMPILE
