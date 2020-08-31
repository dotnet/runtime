// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: debugger.h
//

//
// Header file for Runtime Controller classes of the COM+ Debugging Services.
//
//*****************************************************************************

#ifndef DEBUGGER_H_
#define DEBUGGER_H_

#include <windows.h>

#include <utilcode.h>

#include <metahost.h>

#if defined(_DEBUG) && !defined(DACCESS_COMPILE)
#define LOGGING
#endif

#include <log.h>

#include "cor.h"
#include "corpriv.h"
#include "daccess.h"

#include "common.h"
#include "winwrap.h"
#include "threads.h"
#include "threadsuspend.h"
#include "frames.h"

#include "appdomain.hpp"
#include "eedbginterface.h"
#include "dbginterface.h"
#include "corhost.h"


#include "corjit.h"
#include <dbgmeta.h> // <TODO>need to rip this out of here...</TODO>

#include "frameinfo.h"

#include "dllimportcallback.h"

#include "canary.h"

#undef ASSERT
#define CRASH(x)  _ASSERTE(!(x))
#define ASSERT(x) _ASSERTE(x)


#ifndef TRACE_MEMORY
#define TRACE_MEMORY 0
#endif

#if TRACE_MEMORY
#define TRACE_ALLOC(p)  LOG((LF_CORDB, LL_INFO10000, \
                       "--- Allocated %x at %s:%d\n", p, __FILE__, __LINE__));
#define TRACE_FREE(p)   LOG((LF_CORDB, LL_INFO10000, \
                       "--- Freed %x at %s:%d\n", p, __FILE__, __LINE__));
#else
#define TRACE_ALLOC(p)
#define TRACE_FREE(p)
#endif

typedef CUnorderedArray<void*,11> UnorderedPtrArray;

/* ------------------------------------------------------------------------ *
 * Forward class declarations
 * ------------------------------------------------------------------------ */

class DebuggerFrame;
class DebuggerModule;
class DebuggerModuleTable;
class Debugger;
class DebuggerBreakpoint;
class DebuggerPendingFuncEvalTable;
class DebuggerRCThread;
class DebuggerStepper;
class DebuggerMethodInfo;
class DebuggerJitInfo;
class DebuggerMethodInfoTable;
struct DebuggerControllerPatch;
class DebuggerEval;
class DebuggerControllerQueue;
class DebuggerController;
class Crst;

typedef CUnorderedArray<DebuggerControllerPatch *, 17> PATCH_UNORDERED_ARRAY;
template<class T> void DeleteInteropSafe(T *p);
template<class T> void DeleteInteropSafeExecutable(T *p);

typedef VPTR(class Debugger) PTR_Debugger;
typedef DPTR(struct DebuggerILToNativeMap) PTR_DebuggerILToNativeMap;
typedef DPTR(class DebuggerMethodInfo) PTR_DebuggerMethodInfo;
typedef VPTR(class DebuggerMethodInfoTable) PTR_DebuggerMethodInfoTable;
typedef DPTR(class DebuggerJitInfo) PTR_DebuggerJitInfo;
typedef DPTR(class DebuggerEval) PTR_DebuggerEval;
typedef DPTR(struct DebuggerIPCControlBlock) PTR_DebuggerIPCControlBlock;


/* ------------------------------------------------------------------------ *
 * Global variables
 * ------------------------------------------------------------------------ */

GPTR_DECL(Debugger,         g_pDebugger);
GPTR_DECL(EEDebugInterface, g_pEEInterface);
GVAL_DECL(ULONG,            CLRJitAttachState);
#ifndef TARGET_UNIX
GVAL_DECL(HANDLE,           g_hContinueStartupEvent);
#endif
extern DebuggerRCThread     *g_pRCThread;

//---------------------------------------------------------------------------------------
// Holder to ensure our calls to IncThreadsAtUnsafePlaces and DecThreadsAtUnsafePlaces
class AtSafePlaceHolder
{
public:
    AtSafePlaceHolder(Thread * pThread);

    // Clear the holder.
    ~AtSafePlaceHolder();

    // True if the holder is acquired.
    bool IsAtUnsafePlace();

    // Clear the holder (call DecThreadsAtUnsafePlaces if needed)
    void Clear();

private:
    // If this is non-null, then the holder incremented the unsafe counter and it needs
    // to decrement it.
    Thread * m_pThreadAtUnsafePlace;
};


template<BOOL COOPERATIVE, BOOL TOGGLE, BOOL IFTHREAD>
class GCHolderEEInterface
{
public:
    DEBUG_NOINLINE GCHolderEEInterface();
    DEBUG_NOINLINE ~GCHolderEEInterface();
};

#ifndef DACCESS_COMPILE
template<BOOL TOGGLE, BOOL IFTHREAD>
class GCHolderEEInterface<TRUE, TOGGLE, IFTHREAD>
{
private:
    bool startInCoop;

public:
    DEBUG_NOINLINE GCHolderEEInterface()
    {
        SCAN_SCOPE_BEGIN;
        STATIC_CONTRACT_MODE_COOPERATIVE;

        if (IFTHREAD && g_pEEInterface->GetThread() == NULL)
        {
            return;
        }

        startInCoop = false;

        if (g_pEEInterface->IsPreemptiveGCDisabled())
        {
            // we're starting in COOP, no need to switch
            startInCoop = true;
        }
        else
        {
            // we're starting in PREEMP, need to switch to COOP
            startInCoop = false;
            g_pEEInterface->DisablePreemptiveGC();
        }
    };

    DEBUG_NOINLINE ~GCHolderEEInterface()
    {
        SCAN_SCOPE_END;

        if (IFTHREAD && g_pEEInterface->GetThread() == NULL)
        {
            return;
        }

        _ASSERT(g_pEEInterface->IsPreemptiveGCDisabled());

        if (TOGGLE)
        {
            // We're in COOP, toggle to PREEMPTIVE and back to COOP
            // for synch purposes.
            g_pEEInterface->EnablePreemptiveGC();
            g_pEEInterface->DisablePreemptiveGC();

            // If we started in PREEMPTIVE switch back
            if (!startInCoop)
            {
                g_pEEInterface->EnablePreemptiveGC();
            }
        }
        else
        {
            // If we started in PREEMPTIVE switch back
            if (!startInCoop)
            {
                g_pEEInterface->EnablePreemptiveGC();
            }
        }
    };
};

template<BOOL TOGGLE, BOOL IFTHREAD>
class GCHolderEEInterface<FALSE, TOGGLE, IFTHREAD>
{
private:
    bool startInCoop;
    bool conditional;

    void EnterInternal(bool bStartInCoop, bool bConditional)
    {
        startInCoop = bStartInCoop;
        conditional = bConditional;

        if (!conditional || (IFTHREAD && g_pEEInterface->GetThread() == NULL))
        {
            return;
        }

        if (g_pEEInterface->IsPreemptiveGCDisabled())
        {
            // we're starting in COOP, we need to switch to PREEMP
            startInCoop = true;
            g_pEEInterface->EnablePreemptiveGC();
        }
        else
        {
            // We're starting in PREEMP, no need to switch
            startInCoop = false;
        }
    }

    void LeaveInternal()
    {
        if (!conditional || (IFTHREAD && g_pEEInterface->GetThread() == NULL))
        {
            return;
        }

        _ASSERTE(!g_pEEInterface->IsPreemptiveGCDisabled());

        if (TOGGLE)
        {
            // Explicitly toggle to COOP for eventin
            g_pEEInterface->DisablePreemptiveGC();

            // If we started in PREEMPTIVE switch back to PREEMPTIVE
            if (!startInCoop)
            {
                g_pEEInterface->EnablePreemptiveGC();
            }
        }
        else
        {
            // If we started in COOP, flip back to COOP at the end of the
            // scope, if we started in preemptive we should be fine.
            if (startInCoop)
            {
                g_pEEInterface->DisablePreemptiveGC();
            }
        }
    }

public:
    DEBUG_NOINLINE GCHolderEEInterface()
    {
        SCAN_SCOPE_BEGIN;
        STATIC_CONTRACT_MODE_PREEMPTIVE;

        this->EnterInternal(false, true);
    }

    DEBUG_NOINLINE GCHolderEEInterface(bool bConditional)
    {
        SCAN_SCOPE_BEGIN;
        if (bConditional)
        {
            STATIC_CONTRACT_MODE_PREEMPTIVE;
        }

        this->EnterInternal(false, bConditional);
    }

    DEBUG_NOINLINE ~GCHolderEEInterface()
    {
        SCAN_SCOPE_END;

        this->LeaveInternal();
    };
};
#endif //DACCESS_COMPILE

#define GCX_COOP_EEINTERFACE()                                          \
    GCHolderEEInterface<TRUE, FALSE, FALSE> __gcCoop_onlyOneAllowedPerScope

#define GCX_PREEMP_EEINTERFACE()                                        \
    GCHolderEEInterface<FALSE, FALSE, FALSE> __gcCoop_onlyOneAllowedPerScope

#define GCX_COOP_EEINTERFACE_TOGGLE()                                   \
    GCHolderEEInterface<TRUE, TRUE, FALSE> __gcCoop_onlyOneAllowedPerScope

#define GCX_PREEMP_EEINTERFACE_TOGGLE()                                 \
    GCHolderEEInterface<FALSE, TRUE, FALSE> __gcCoop_onlyOneAllowedPerScope

#define GCX_PREEMP_EEINTERFACE_TOGGLE_IFTHREAD()                        \
    GCHolderEEInterface<FALSE, TRUE, TRUE> __gcCoop_onlyOneAllowedPerScope

#define GCX_PREEMP_EEINTERFACE_TOGGLE_COND(cond)                        \
    GCHolderEEInterface<FALSE, TRUE, FALSE> __gcCoop_onlyOneAllowedPerScope((cond))

#define GCX_PREEMP_EEINTERFACE_TOGGLE_IFTHREAD_COND(cond)               \
    GCHolderEEInterface<FALSE, TRUE, TRUE> __gcCoop_onlyOneAllowedPerScope((cond))



// There are still some APIs that call new that we call from the helper thread.
// These are unsafe operations, so we wrap them here. Each of these is a potential hang.
inline DWORD UnsafeGetConfigDWORD_DontUse_(LPCWSTR name, DWORD defValue)
{
    SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE;
    return REGUTIL::GetConfigDWORD_DontUse_(name, defValue);
}

inline DWORD UnsafeGetConfigDWORD(const CLRConfig::ConfigDWORDInfo & info)
{
    SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE;
    return CLRConfig::GetConfigValue(info);
}

#define FILE_DEBUG INDEBUG(__FILE__) NOT_DEBUG(NULL)
#define LINE_DEBUG INDEBUG(__LINE__) NOT_DEBUG(0)

#define CORDBDebuggerSetUnrecoverableWin32Error(__d, __code, __w) \
    ((__d)->UnrecoverableError(HRESULT_FROM_WIN32(GetLastError()), \
                               (__code), FILE_DEBUG, LINE_DEBUG, (__w)), \
     HRESULT_FROM_GetLastError())

#define CORDBDebuggerSetUnrecoverableError(__d, __hr, __w) \
    (__d)->UnrecoverableError((__hr), \
                               (__hr), FILE_DEBUG, LINE_DEBUG, (__w))

#define CORDBUnrecoverableError(__d) ((__d)->m_unrecoverableError == TRUE)

/* ------------------------------------------------------------------------ *
 * Helpers used for contract preconditions.
 * ------------------------------------------------------------------------ */


bool ThisIsHelperThreadWorker(void);
bool ThisIsTempHelperThread();
bool ThisIsTempHelperThread(DWORD tid);

#ifdef _DEBUG

// Functions can be split up into 3 categories:
// 1.) Functions that must run on the helper thread.
//     Returns true if this is the helper thread (or the thread
//     doing helper-threadduty).

// 2.) Functions that can't run on the helper thread.
//     This is just !ThisIsHelperThread();

// 3.) Functions that may or may not run on the helper thread.
//     Note this is trivially true, but it's presences means that
//     we're not case #1 or #2, so it's still valuable.
inline bool ThisMaybeHelperThread() { return true; }

#endif


// These are methods for transferring information between a REGDISPLAY and
// a DebuggerREGDISPLAY.
extern void CopyREGDISPLAY(REGDISPLAY* pDst, REGDISPLAY* pSrc);
extern void SetDebuggerREGDISPLAYFromREGDISPLAY(DebuggerREGDISPLAY* pDRD, REGDISPLAY* pRD);

//
// PUSHED_REG_ADDR gives us NULL if the register still lives in the thread's context, or it gives us the address
// of where the register was pushed for this frame.
//
// This macro is used in CopyREGDISPLAY() and SetDebuggerREGDISPLAYFromREGDISPLAY().  We really should make
// DebuggerREGDISPLAY to be a class with these two methods, but unfortunately, the RS has no notion of REGDISPLAY.
inline LPVOID PushedRegAddr(REGDISPLAY* pRD, LPVOID pAddr)
{
    LIMITED_METHOD_CONTRACT;

#ifdef FEATURE_EH_FUNCLETS
    if ( ((UINT_PTR)(pAddr) >= (UINT_PTR)pRD->pCurrentContextPointers) &&
         ((UINT_PTR)(pAddr) <= ((UINT_PTR)pRD->pCurrentContextPointers + sizeof(T_KNONVOLATILE_CONTEXT_POINTERS))) )
#else
    if ( ((UINT_PTR)(pAddr) >= (UINT_PTR)pRD->pContext) &&
         ((UINT_PTR)(pAddr) <= ((UINT_PTR)pRD->pContext + sizeof(T_CONTEXT))) )
#endif
        return NULL;

    // (Microsoft 2/9/07 - putting this in an else clause confuses gcc for some reason, so I've moved
    //  it to here)
    return pAddr;
}

bool HandleIPCEventWrapper(Debugger* pDebugger, DebuggerIPCEvent *e);

HRESULT ValidateObject(Object *objPtr);

//-----------------------------------------------------------------------------
// Execution control needs several ways to get at the context of a thread
// stopped in mangaged code (stepping, setip, func-eval).
// We want to abstract away a few things:
// - active: this thread is stopped at a patch
// - inactive: this threads was managed suspended somewhere in jitted code
//             because of some other active thread.
//
// In general, execution control operations administered from the helper thread
// can occur on any managed thread (active or inactive).
// Intermediate triggers (eg, TriggerPatch) only occur on an active thread.
//
// Viewing the context in terms of Active vs. Inactive lets us abstract away
// filter context, redirected context, and interop hijacks.
//-----------------------------------------------------------------------------

// Get the context for a thread stopped (perhaps temporarily) in managed code.
// The process may be live or stopped.
// This thread could be 'active' (stopped at patch) or inactive.
// This context should always be in managed code and this context can be manipulated
// for execution control (setip, single-step, func-eval, etc)
// Returns NULL if not available.
CONTEXT * GetManagedStoppedCtx(Thread * pThread);

// Get the context for a thread live in or around managed code.
// Caller guarantees this is active.
// This ctx is just for a 'live' thread. This means that the ctx may include
// from a M2U hijack or from a Native patch (like .
// Never NULL.
CONTEXT * GetManagedLiveCtx(Thread * pThread);


#undef UtilMessageBoxCatastrophic
#undef UtilMessageBoxCatastrophicNonLocalized
#undef UtilMessageBoxCatastrophicVA
#undef UtilMessageBoxCatastrophicNonLocalizedVA
#undef UtilMessageBox
#undef UtilMessageBoxNonLocalized
#undef UtilMessageBoxVA
#undef UtilMessageBoxNonLocalizedVA
#undef WszMessageBox
#define UtilMessageBoxCatastrophic __error("Use g_pDebugger->MessageBox from inside the left side of the debugger")
#define UtilMessageBoxCatastrophicNonLocalized __error("Use g_pDebugger->MessageBox from inside the left side of the debugger")
#define UtilMessageBoxCatastrophicVA __error("Use g_pDebugger->MessageBox from inside the left side of the debugger")
#define UtilMessageBoxCatastrophicNonLocalizedVA __error("Use g_pDebugger->MessageBox from inside the left side of the debugger")
#define UtilMessageBox __error("Use g_pDebugger->MessageBox from inside the left side of the debugger")
#define UtilMessageBoxNonLocalized __error("Use g_pDebugger->MessageBox from inside the left side of the debugger")
#define UtilMessageBoxVA __error("Use g_pDebugger->MessageBox from inside the left side of the debugger")
#define UtilMessageBoxNonLocalizedVA __error("Use g_pDebugger->MessageBox from inside the left side of the debugger")
#define WszMessageBox __error("Use g_pDebugger->MessageBox from inside the left side of the debugger")


/* ------------------------------------------------------------------------ *
 * Module classes
 * ------------------------------------------------------------------------ */

// Once a module / appdomain is unloaded, all Right-side objects (such as breakpoints)
// in that appdomain will get neutered and will thus be prevented from accessing
// the unloaded appdomain.
//
// @dbgtodo jmc - This is now purely relegated to the LS. Eventually completely get rid of this
// by moving fields off to Module or getting rid of the fields completely.
typedef DPTR(class DebuggerModule) PTR_DebuggerModule;
class DebuggerModule
{
  public:
    DebuggerModule(Module * pRuntimeModule, DomainFile * pDomainFile, AppDomain * pAppDomain);

    // Do we have any optimized code in the module?
    // JMC-probes aren't emitted in optimized code,
    bool HasAnyOptimizedCode();

    // If the debugger updates things to allow/disallow optimized code, then we have to track that.
    void MarkAllowedOptimizedCode();
    void UnmarkAllowedOptimizedCode();


    BOOL ClassLoadCallbacksEnabled(void);
    void EnableClassLoadCallbacks(BOOL f);

    AppDomain* GetAppDomain();

    Module * GetRuntimeModule();


    // <TODO> (8/12/2002)
    // Currently we create a new DebuggerModules for each appdomain a shared
    // module lives in. We then pretend there aren't any shared modules.
    // This is bad. We need to move away from this.
    // Once we stop lying, then every module will be it's own PrimaryModule. :)
    //
    // Currently, Module* is 1:n w/ DebuggerModule.
    // We add a notion of PrimaryModule so that:
    // Module* is 1:1 w/ DebuggerModule::GetPrimaryModule();
    // This should help transition towards exposing shared modules.
    // If the Runtime module is shared, then this gives a common DM.
    // If the runtime module is not shared, then this is an identity function.
    //
    // The runtime has the notion of "DomainFile", which is 1:1 with DebuggerModule
    // and thus 1:1 with CordbModule.  The CordbModule hash table on the RS now uses
    // the DomainFile as the key instead of DebuggerModule.  This is a temporary
    // workaround to facilitate the removal of DebuggerModule.
    // </TODO>
    DebuggerModule * GetPrimaryModule();
    DomainFile * GetDomainFile()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_pRuntimeDomainFile;
    }

    // Called by DebuggerModuleTable to set our primary module
    void SetPrimaryModule(DebuggerModule * pPrimary);

    void SetCanChangeJitFlags(bool fCanChangeJitFlags);

  private:
    BOOL            m_enableClassLoadCallbacks;

    // First step in moving away from hiding shared modules.
    DebuggerModule* m_pPrimaryModule;

    PTR_Module     m_pRuntimeModule;
    PTR_DomainFile m_pRuntimeDomainFile;

    AppDomain*     m_pAppDomain;

    bool m_fHasOptimizedCode;

    void PickPrimaryModule();

    // Can we change jit flags on the module?
    // This is true during the Module creation
    bool           m_fCanChangeJitFlags;


};

/* ------------------------------------------------------------------------ *
 * Hash to hold pending func evals by thread id
 * ------------------------------------------------------------------------ */

struct DebuggerPendingFuncEval
{
    FREEHASHENTRY   entry;
    PTR_Thread          pThread;
    PTR_DebuggerEval    pDE;
};

typedef DPTR(struct DebuggerPendingFuncEval) PTR_DebuggerPendingFuncEval;

/* ------------------------------------------------------------------------ *
 * DebuggerRCThread class -- the Runtime Controller thread.
 * ------------------------------------------------------------------------ */

#define DRCT_CONTROL_EVENT  0
#define DRCT_RSEA           1
#define DRCT_FAVORAVAIL     2
#define DRCT_COUNT_INITIAL  3

#define DRCT_DEBUGGER_EVENT 3
#define DRCT_COUNT_FINAL    4






// Canary is used as way to have a runtime failure for the SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE
// contract violation.
// Have a macro which checks the canary and then uses the Suppress macro.
// We need this check to be a macro in order to chain to the Suppress_allocation macro.
#define CHECK_IF_CAN_TAKE_HELPER_LOCKS_IN_THIS_SCOPE(pHR, pCanary) \
    { \
        HelperCanary * __pCanary = (pCanary);  \
        if (!__pCanary->AreLocksAvailable()) { \
            (*pHR) = CORDBG_E_HELPER_MAY_DEADLOCK;  \
        } else  { \
            (*pHR) = S_OK; \
        } \
    } \
    SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE \
    ; \


// Mechanics for cross-thread call to helper thread (called "Favor").
class HelperThreadFavor
{
    // Only let RCThread access these fields.
    friend class DebuggerRCThread;

    HelperThreadFavor();
    // No dtor because we intentionally leak all shutdown.
    void Init();

protected:
    // Stuff for having the helper thread do function calls for a thread
    // that blew its stack
    FAVORCALLBACK m_fpFavor;
    void                           *m_pFavorData;
    HANDLE                          m_FavorReadEvent;
    Crst                            m_FavorLock;

    HANDLE                          m_FavorAvailableEvent;
};


// The *LazyInit classes represents storage that the debugger doesn't need until after it has started up.
// This is effectively an extension to the debugger class; but for perf reasons, we only
// want to instantiate it if we're actually debugging.

// Fields that are a logical extension of RCThread
class RCThreadLazyInit
{
    // Only let RCThread access these fields.
    friend class DebuggerRCThread;

public:
    RCThreadLazyInit() { }
    ~RCThreadLazyInit() { }

    void Init() { }
protected:



    HelperCanary m_Canary;
};

// Fields that are a logical extension of Debugger
class DebuggerLazyInit
{
    friend class Debugger;
public:
    DebuggerLazyInit();
    ~DebuggerLazyInit();

protected:
    void Init();

    DebuggerPendingFuncEvalTable *m_pPendingEvals;

    // The "debugger data lock" is a very small leaf lock used to protect debugger internal data structures (such
    // as DJIs, DMIs, module table). It is a GC-unsafe-anymode lock and so it can't trigger a GC while being held.
    // It also can't issue any callbacks into the EE or anycode that it does not directly control.
    // This is a separate lock from the the larger Debugger-lock / Controller lock, which allows regions under those
    // locks to access debugger datastructures w/o blocking each other.
    Crst                  m_DebuggerDataLock;
    HANDLE                m_CtrlCMutex;
    HANDLE                m_exAttachEvent;
    HANDLE                m_exUnmanagedAttachEvent;
    HANDLE                m_garbageCollectionBlockerEvent;

    BOOL                  m_DebuggerHandlingCtrlC;

    // Used by MapAndBindFunctionBreakpoints.  Note that this is thread-safe
    // only b/c we access it from within the DebuggerController::Lock
    SIZE_T_UNORDERED_ARRAY m_BPMappingDuplicates;

    UnorderedPtrArray     m_pMemBlobs;

    // Hang RCThread fields off DebuggerLazyInit to avoid an extra pointer.
    RCThreadLazyInit m_RCThread;
};
typedef DPTR(DebuggerLazyInit) PTR_DebuggerLazyInit;

class DebuggerRCThread
{
public:
    DebuggerRCThread(Debugger * pDebugger);
    virtual ~DebuggerRCThread();
    void CloseIPCHandles();

    //
    // You create a new instance of this class, call Init() to set it up,
    // then call Start() start processing events. Stop() terminates the
    // thread and deleting the instance cleans all the handles and such
    // up.
    //
    HRESULT Init(void);
    HRESULT Start(void);
    HRESULT AsyncStop(void);

    //
    // These are used by this thread to send IPC events to the Debugger
    // Interface side.
    //
    DebuggerIPCEvent* GetIPCEventSendBuffer()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

#ifdef LOGGING
        if(IsRCThreadReady()) {
            LOG((LF_CORDB, LL_EVERYTHING, "RCThread is ready\n"));
        }
#endif

        _ASSERTE(m_pDCB != NULL);
        // In case this turns into a continuation event
        GetRCThreadSendBuffer()->next = NULL;
        LOG((LF_CORDB,LL_EVERYTHING, "GIPCESBuffer: got event 0x%x\n", GetRCThreadSendBuffer()));

        return GetRCThreadSendBuffer();
    }

    // Send an IPCEvent once we're ready for sending. This should be done inbetween
    // SENDIPCEVENT_BEGIN & SENDIPCEVENT_END. See definition of SENDIPCEVENT_BEGIN
    // for usage pattern
    HRESULT SendIPCEvent();

    HRESULT EnsureRuntimeOffsetsInit(IpcTarget i); // helper function for SendIPCEvent
    void NeedRuntimeOffsetsReInit(IpcTarget i);

    DebuggerIPCEvent* GetIPCEventReceiveBuffer()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;
        _ASSERTE(m_pDCB != NULL);

        return GetRCThreadReceiveBuffer();
    }

    HRESULT SendIPCReply();

    //
    // Handle Favors - get the Helper thread to do a function call for us
    // because our thread can't (eg, we don't have the stack space)
    // DoFavor will call (*fp)(pData) and block until fp returns.
    // pData can store parameters, return value, and a this ptr (if we
    // need to call a member function)
    //
    void DoFavor(FAVORCALLBACK fp, void * pData);

    //
    // Convience routines
    //
    PTR_DebuggerIPCControlBlock GetDCB()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        // This may be called before we init or after we shutdown.

        return m_pDCB;
    }

    void WatchForStragglers(void);

    HRESULT SetupRuntimeOffsets(DebuggerIPCControlBlock *pDCB);

    bool HandleRSEA();
    void MainLoop();
    void TemporaryHelperThreadMainLoop();

    HANDLE GetHelperThreadCanGoEvent(void) {LIMITED_METHOD_CONTRACT;  return m_helperThreadCanGoEvent; }

    void EarlyHelperThreadDeath(void);

    void RightSideDetach(void);

    //
    //
    //
    void ThreadProc(void);
    static DWORD WINAPI ThreadProcStatic(LPVOID parameter);
    static DWORD WINAPI ThreadProcRemote(LPVOID parameter);

    DWORD GetRCThreadId()
    {
        LIMITED_METHOD_CONTRACT;

        return m_pDCB->m_helperThreadId;
    }

    // Return true if the Helper Thread up & initialized.
    bool IsRCThreadReady();

    HRESULT ReDaclEvents(PSECURITY_DESCRIPTOR securityDescriptor);
private:

    // The transport based communication protocol keeps the send and receive buffers outside of the DCB
    // to keep the DCB size down (since we send it over the wire).
    DebuggerIPCEvent * GetRCThreadReceiveBuffer()
    {
#if defined(FEATURE_DBGIPC_TRANSPORT_VM)
        return reinterpret_cast<DebuggerIPCEvent *>(&m_receiveBuffer[0]);
#else
        return reinterpret_cast<DebuggerIPCEvent *>(&m_pDCB->m_receiveBuffer[0]);
#endif
    }

    // The transport based communication protocol keeps the send and receive buffers outside of the DCB
    // to keep the DCB size down (since we send it over the wire).
    DebuggerIPCEvent * GetRCThreadSendBuffer()
    {
#if defined(FEATURE_DBGIPC_TRANSPORT_VM)
        return reinterpret_cast<DebuggerIPCEvent *>(&m_sendBuffer[0]);
#else  // FEATURE_DBGIPC_TRANSPORT_VM
        return reinterpret_cast<DebuggerIPCEvent *>(&m_pDCB->m_sendBuffer[0]);
#endif  // FEATURE_DBGIPC_TRANSPORT_VM
    }

    FAVORCALLBACK GetFavorFnPtr()           { return m_favorData.m_fpFavor; }
    void * GetFavorData()                   { return m_favorData.m_pFavorData; }

    void SetFavorFnPtr(FAVORCALLBACK fp, void * pData)
    {
        m_favorData.m_fpFavor = fp;
        m_favorData.m_pFavorData = pData;
    }
    Crst * GetFavorLock()                   { return &m_favorData.m_FavorLock; }

    HANDLE GetFavorReadEvent()              { return m_favorData.m_FavorReadEvent; }
    HANDLE GetFavorAvailableEvent()         { return m_favorData.m_FavorAvailableEvent; }

    HelperThreadFavor m_favorData;


    HelperCanary * GetCanary()              { return &GetLazyData()->m_Canary; }


    friend class Debugger;
    Debugger*                       m_debugger;

    // IPC_TARGET_* define default targets - if we ever want to do
    // multiple right sides, we'll have to switch to a OUTOFPROC + iTargetProcess scheme
    PTR_DebuggerIPCControlBlock     m_pDCB;

#ifdef FEATURE_DBGIPC_TRANSPORT_VM
    // These buffers move here out of the DebuggerIPCControlBlock since the block is not shared memory when
    // using the transport, but we do send its contents over the wire (and these buffers would greatly impact
    // the number of bytes sent without being useful in any way).
    BYTE                            m_receiveBuffer[CorDBIPC_BUFFER_SIZE];
    BYTE                            m_sendBuffer[CorDBIPC_BUFFER_SIZE];
#endif // FEATURE_DBGIPC_TRANSPORT_VM

    HANDLE                          m_thread;
    bool                            m_run;

    HANDLE                          m_threadControlEvent;
    HANDLE                          m_helperThreadCanGoEvent;
    bool                            m_rgfInitRuntimeOffsets[IPC_TARGET_COUNT];
    bool                            m_fDetachRightSide;

    RCThreadLazyInit *              GetLazyData();
#ifdef _DEBUG
    // Tracking to ensure that the helper thread only calls New() on the interop-safe heap.
    // We need a very light-weight way to track the helper b/c we need to check everytime somebody
    // calls operator new, which may occur during shutdown paths.
    static EEThreadId               s_DbgHelperThreadId;

public:
    // The OS ThreadId of the helper as determined from the CreateThread call.
    DWORD                           m_DbgHelperThreadOSTid;
private:
#endif

};

typedef DPTR(DebuggerRCThread) PTR_DebuggerRCThread;

/* ------------------------------------------------------------------------ *
 * Debugger Method Info struct and hash table
 * ------------------------------------------------------------------------ */

// class DebuggerMethodInfo: Struct to hold all the information
// necessary for a given function.
//
// m_module, m_token:   Method that this DMI applies to
//
const bool bOriginalToInstrumented = true;
const bool bInstrumentedToOriginal = false;

class DebuggerMethodInfo
{
    // This is the most recent version of the function based on the latest update and is
    // set in UpdateFunction. When a function is jitted, the version is copied from here
    // and stored in the corresponding DebuggerJitInfo structure so can always know the
    // version of a particular jitted function.
    SIZE_T          m_currentEnCVersion;

public:
    PTR_Module          m_module;
    mdMethodDef         m_token;

    PTR_DebuggerMethodInfo m_prevMethodInfo;
    PTR_DebuggerMethodInfo m_nextMethodInfo;


    // Enumerate DJIs
    // Expected usage:
    // DMI.InitDJIIterator(&it);
    // while(!it.IsAtEnd()) {
    //    f(it.Current()); it.Next();
    // }
    class DJIIterator
    {
        friend class DebuggerMethodInfo;

        DebuggerJitInfo* m_pCurrent;
        Module* m_pLoaderModuleFilter;
        MethodDesc* m_pMethodDescFilter;
    public:
        DJIIterator();

        bool IsAtEnd();
        DebuggerJitInfo * Current();
        void Next(BOOL fFirst = FALSE);

    };

    // Ensure the DJI cache is completely up to date. (This can be an expensive call, but
    // much less so if pMethodDescFilter is used).
    void CreateDJIsForNativeBlobs(AppDomain * pAppDomain, Module * pModuleFilter, MethodDesc * pMethodDescFilter);

    // Ensure the DJI cache is up to date for a particular closed method desc
    void CreateDJIsForMethodDesc(MethodDesc * pMethodDesc);

    // Get an iterator for all native blobs (accounts for Generics, Enc, + Prejiiting).
    // Must be stopped when we do this. This could be heavy weight.
    // This will call CreateDJIsForNativeBlobs() to ensure we have all DJIs available.
    // You may optionally pass pLoaderModuleFilter to restrict the DJIs iterated to
    // exist only on MethodDescs whose loader module matches the filter (pass NULL not
    // to filter by loader module).
    // You may optionally pass pMethodDescFilter to restrict the DJIs iterated to only
    // a single generic instantiation.
    void IterateAllDJIs(AppDomain * pAppDomain, Module * pLoaderModuleFilter, MethodDesc * pMethodDescFilter, DJIIterator * pEnum);

private:
    // The linked list of JIT's of this version of the method.   This will ALWAYS
    // contain one element except for code in generic classes or generic methods,
    // which may get JITted more than once under different type instantiations.
    //
    // We find the appropriate JitInfo by searching the list (nearly always this
    // will return the first element of course).
    //
    // The JitInfos contain back pointers to this MethodInfo.  They should never be associated
    // with any other MethodInfo.
    //
    // USE ACCESSOR FUNCTION GetLatestJitInfo(), as it does lazy init of this field.
    //

    PTR_DebuggerJitInfo m_latestJitInfo;

public:

    PTR_DebuggerJitInfo GetLatestJitInfo(MethodDesc *fd);

    DebuggerJitInfo * GetLatestJitInfo_NoCreate();


    // Find the DJI corresponding to the specified MD and native start address.
    DebuggerJitInfo * FindJitInfo(MethodDesc * pMD, TADDR addrNativeStartAddr);

    // Creating the Jit-infos.
    DebuggerJitInfo *FindOrCreateInitAndAddJitInfo(MethodDesc* fd, PCODE startAddr);
    DebuggerJitInfo *CreateInitAndAddJitInfo(NativeCodeVersion nativeCodeVersion, TADDR startAddr, BOOL* jitInfoWasCreated);


    void DeleteJitInfo(DebuggerJitInfo *dji);
    void DeleteJitInfoList(void);

    // Return true iff this has been jitted.
    // Since we can create DMIs freely, a DMI's existence doesn't mean that the method was jitted.
    bool HasJitInfos();

    // Return true iff this has been EnCed since the last time the function was jitted.
    bool HasMoreRecentEnCVersion();


    // Return true iif this is a JMC function, else false.
    bool IsJMCFunction();
    void SetJMCStatus(bool fStatus);


    DebuggerMethodInfo(Module *module, mdMethodDef token);
    ~DebuggerMethodInfo();

    // A profiler can remap the IL. We track the "instrumented" IL map here.
    void SetInstrumentedILMap(COR_IL_MAP * pMap, SIZE_T cEntries);
    bool HasInstrumentedILMap() {return m_fHasInstrumentedILMap; }

    // TranslateToInstIL will take offOrig, and translate it to the
    // correct IL offset if this code happens to be instrumented
    ULONG32 TranslateToInstIL(const InstrumentedILOffsetMapping * pMapping, ULONG32 offOrig, bool fOrigToInst);


    // We don't always have a debugger module. (Ex: we're tracking debug info,
    // but no debugger's attached). So this may return NULL alot.
    // If we can, we should use the RuntimeModule when ever possible.
    DebuggerModule* GetPrimaryModule();

    // We always have a runtime module.
    Module * GetRuntimeModule();

    // Set the latest EnC version number for this method
    // This doesn't mean we have a DJI for this version yet.
    void SetCurrentEnCVersion(SIZE_T currentEnCVersion)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(currentEnCVersion >= CorDB_DEFAULT_ENC_FUNCTION_VERSION);
        m_currentEnCVersion = currentEnCVersion;
    }

    SIZE_T GetCurrentEnCVersion()
    {
        LIMITED_METHOD_CONTRACT;
        SUPPORTS_DAC;

        return m_currentEnCVersion;
    }

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

protected:
    // JMC info. Each method can have its own JMC setting.
    bool m_fJMCStatus;

    // "Instrumented" IL map set by the profiler.
    // @dbgtodo  execution control - remove this when we do execution control from out-of-proc
    bool m_fHasInstrumentedILMap;
};

// ------------------------------------------------------------------------ *
// Executable code memory management for the debugger heap.
//
//     Rather than allocating memory that needs to be executable on the process heap (which
//     is forbidden on some flavors of SELinux and is generally a bad idea), we use the
//     allocator below. It will handle allocating and managing the executable memory in a
//     different part of the address space (not on the heap).
// ------------------------------------------------------------------------ */

#define DBG_MAX_EXECUTABLE_ALLOC_SIZE 48

// Forward declaration
struct DebuggerHeapExecutableMemoryPage;

// ------------------------------------------------------------------------ */
// DebuggerHeapExecutableMemoryChunk
//
// Each DebuggerHeapExecutableMemoryPage is divided into 64 of these chunks.
// The first chunk is a BookkeepingChunk used for bookkeeping information
// for the page, and the remaining ones are DataChunks and are handed out
// by the allocator when it allocates memory.
// ------------------------------------------------------------------------ */
union DECLSPEC_ALIGN(64) DebuggerHeapExecutableMemoryChunk {

    struct DataChunk
    {
        char data[DBG_MAX_EXECUTABLE_ALLOC_SIZE];

        DebuggerHeapExecutableMemoryPage *startOfPage;

        // The chunk number within the page.
        uint8_t chunkNumber;

    } data;

    struct BookkeepingChunk
    {
        DebuggerHeapExecutableMemoryPage *nextPage;

        uint64_t pageOccupancy;

    } bookkeeping;

    char _alignpad[64];
};

static_assert(sizeof(DebuggerHeapExecutableMemoryChunk) == 64, "DebuggerHeapExecutableMemoryChunk is expect to be 64 bytes.");

// ------------------------------------------------------------------------ */
// DebuggerHeapExecutableMemoryPage
//
// We allocate the size of DebuggerHeapExecutableMemoryPage each time we need
// more memory and divide each page into DebuggerHeapExecutableMemoryChunks for
// use. The pages are self describing; the first chunk contains information
// about which of the other chunks are used/free as well as a pointer to
// the next page.
// ------------------------------------------------------------------------ */
struct DECLSPEC_ALIGN(4096) DebuggerHeapExecutableMemoryPage
{
    inline DebuggerHeapExecutableMemoryPage* GetNextPage()
    {
        return chunks[0].bookkeeping.nextPage;
    }

    inline void SetNextPage(DebuggerHeapExecutableMemoryPage* nextPage)
    {
        chunks[0].bookkeeping.nextPage = nextPage;
    }

    inline uint64_t GetPageOccupancy() const
    {
        return chunks[0].bookkeeping.pageOccupancy;
    }

    inline void SetPageOccupancy(uint64_t newOccupancy)
    {
        // Can't unset first bit of occupancy!
        ASSERT((newOccupancy & 0x8000000000000000) != 0);

        chunks[0].bookkeeping.pageOccupancy = newOccupancy;
    }

    inline void* GetPointerToChunk(int chunkNum) const
    {
        return (char*)this + chunkNum * sizeof(DebuggerHeapExecutableMemoryChunk);
    }

    DebuggerHeapExecutableMemoryPage()
    {
        SetPageOccupancy(0x8000000000000000); // only the first bit is set.
        for (uint8_t i = 1; i < sizeof(chunks)/sizeof(chunks[0]); i++)
        {
            ASSERT(i != 0);
            chunks[i].data.startOfPage = this;
            chunks[i].data.chunkNumber = i;
        }
    }

private:
    DebuggerHeapExecutableMemoryChunk chunks[64];
};

// ------------------------------------------------------------------------ */
// DebuggerHeapExecutableMemoryAllocator class
// Handles allocation and freeing (and all necessary bookkeeping) for
// executable memory that the DebuggerHeap class needs. This is especially
// useful on systems (like SELinux) where having executable code on the
// heap is explicity disallowed for security reasons.
// ------------------------------------------------------------------------ */

class DebuggerHeapExecutableMemoryAllocator
{
public:
    DebuggerHeapExecutableMemoryAllocator()
    : m_pages(NULL)
    , m_execMemAllocMutex(CrstDebuggerHeapExecMemLock, (CrstFlags)(CRST_UNSAFE_ANYMODE | CRST_REENTRANCY | CRST_DEBUGGER_THREAD))
    { }

    ~DebuggerHeapExecutableMemoryAllocator();

    void* Allocate(DWORD numberOfBytes);
    void Free(void* addr);

private:
    enum class ChangePageUsageAction {ALLOCATE, FREE};

    DebuggerHeapExecutableMemoryPage* AddNewPage();
    bool CheckPageForAvailability(DebuggerHeapExecutableMemoryPage* page, /* _Out_ */ int* chunkToUse);
    void* ChangePageUsage(DebuggerHeapExecutableMemoryPage* page, int chunkNumber, ChangePageUsageAction action);

private:
    // Linked list of pages that have been allocated
    DebuggerHeapExecutableMemoryPage* m_pages;
    Crst m_execMemAllocMutex;
};

// ------------------------------------------------------------------------ *
// DebuggerHeap class
// For interop debugging, we need a heap that:
// - does not take any outside looks
// - returns memory which could be executed.
// ------------------------------------------------------------------------ */

#ifdef FEATURE_INTEROP_DEBUGGING
    #define USE_INTEROPSAFE_HEAP
#endif

class DebuggerHeap
{
public:
    DebuggerHeap();
    ~DebuggerHeap();

    bool IsInit();
    void Destroy();
    HRESULT Init(BOOL fExecutable);

    void *Alloc(DWORD size);
    void *Realloc(void *pMem, DWORD newSize, DWORD oldSize);
    void  Free(void *pMem);


protected:
#ifdef USE_INTEROPSAFE_HEAP
    HANDLE m_hHeap;
#endif
    BOOL m_fExecutable;

private:
#ifndef HOST_WINDOWS
    DebuggerHeapExecutableMemoryAllocator *m_execMemAllocator;
#endif
};

class DebuggerJitInfo;

#if defined(FEATURE_EH_FUNCLETS)
const int PARENT_METHOD_INDEX     = -1;
#endif // FEATURE_EH_FUNCLETS

class CodeRegionInfo
{
public:
    CodeRegionInfo() :
        m_addrOfHotCode(NULL),
        m_addrOfColdCode(NULL),
        m_sizeOfHotCode(0),
        m_sizeOfColdCode(0)
    {
        WRAPPER_NO_CONTRACT;
        SUPPORTS_DAC;
    }

    static CodeRegionInfo GetCodeRegionInfo(DebuggerJitInfo      * dji,
                                            MethodDesc           * md = NULL,
                                            PTR_CORDB_ADDRESS_TYPE addr = PTR_NULL);

    // Fills in the CodeRegoinInfo fields from the start address.
    void InitializeFromStartAddress(PCODE addr)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            SUPPORTS_DAC;
        }
        CONTRACTL_END;

        m_addrOfHotCode = addr;
        g_pEEInterface->GetMethodRegionInfo(addr,
            &m_addrOfColdCode,
            (size_t *) &m_sizeOfHotCode,
            (size_t *) &m_sizeOfColdCode);
    }

    // Converts an offset within a method to a code address
    PCODE OffsetToAddress(SIZE_T offset)
    {
        LIMITED_METHOD_CONTRACT;

        if (m_addrOfHotCode != NULL)
        {
            if (offset < m_sizeOfHotCode)
            {
                return m_addrOfHotCode + offset;
            }
            else
            {
                _ASSERTE(m_addrOfColdCode);
                _ASSERTE(offset <= m_sizeOfHotCode + m_sizeOfColdCode);

                return m_addrOfColdCode + (offset - m_sizeOfHotCode);
            }
        }
        else
        {
            return NULL;
        }
    }

    // Converts a code address to an offset within the method
    SIZE_T AddressToOffset(const BYTE *addr)
    {
        LIMITED_METHOD_CONTRACT;

        PCODE address = (PCODE)addr;

        if ((address >= m_addrOfHotCode) &&
            (address <  m_addrOfHotCode + m_sizeOfHotCode))
        {
            return address - m_addrOfHotCode;
        }
        else if ((address >= m_addrOfColdCode) &&
                 (address <  m_addrOfColdCode + m_sizeOfColdCode))
        {
            return address - m_addrOfColdCode + m_sizeOfHotCode;
        }

        _ASSERTE(!"addressToOffset called with invalid address");
        return NULL;
    }

    // Determines whether the address lies within the method
    bool IsMethodAddress(const BYTE *addr)
    {
        LIMITED_METHOD_CONTRACT;

        PCODE address = PINSTRToPCODE((TADDR)addr);
        return (((address >= m_addrOfHotCode) &&
                 (address <  m_addrOfHotCode + m_sizeOfHotCode)) ||
                ((address >= m_addrOfColdCode) &&
                 (address <  m_addrOfColdCode + m_sizeOfColdCode)));
    }

    // Determines whether the offset is in the hot section
    bool IsOffsetHot(SIZE_T offset)
    {
        LIMITED_METHOD_CONTRACT;

        return (offset < m_sizeOfHotCode);
    }

    PCODE getAddrOfHotCode()  {LIMITED_METHOD_DAC_CONTRACT; return m_addrOfHotCode;}
    PCODE getAddrOfColdCode() {LIMITED_METHOD_DAC_CONTRACT; return m_addrOfColdCode;}
    SIZE_T getSizeOfHotCode()  {LIMITED_METHOD_DAC_CONTRACT; return m_sizeOfHotCode;}
    SIZE_T getSizeOfColdCode() {LIMITED_METHOD_DAC_CONTRACT; return m_sizeOfColdCode;}
    SIZE_T getSizeOfTotalCode(){LIMITED_METHOD_DAC_CONTRACT; return m_sizeOfHotCode + m_sizeOfColdCode; }

private:

    PCODE                m_addrOfHotCode;
    PCODE                m_addrOfColdCode;
    SIZE_T               m_sizeOfHotCode;
    SIZE_T               m_sizeOfColdCode;
};

/* ------------------------------------------------------------------------ *
 * Debugger JIT Info struct
 * ------------------------------------------------------------------------ */

// class DebuggerJitInfo:   Struct to hold all the JIT information
// necessary for a given function.
// - DJIs are 1:1 w/ native codeblobs. They're almost 1:1 w/ Native Method Descs.
//    except that a MethodDesc only refers to the most recent EnC version of a method.
// - If 2 DJIs are different, they refer to different code-blobs.
// - DJIs are lazily created, and so you can't safely enumerate them b/c
// you can't rely on whether they're created or not.


//
// MethodDesc* m_fd:   MethodDesc of the method that this DJI applies to
//
// CORDB_ADDRESS m_addrOfCode:   Address of the code.  This will be read by
//      the right side (via ReadProcessMemory) to grab the actual  native start
//      address of the jitted method.
//
// SIZE_T m_sizeOfCode:   Pseudo-private variable: use the GetSkzeOfCode
//      method to get this value.
//
// bool m_jitComplete:   Set to true once JITComplete has been called.
//
// DebuggerILToNativeMap* m_sequenceMap:   This is the sequence map, which
//      is actually a collection of IL-Native pairs, where each IL corresponds
//      to a line of source code.  Each pair is refered to as a sequence map point.
//
// SIZE_T m_lastIL:   last nonEPILOG instruction
//
// unsigned int m_sequenceMapCount:   Count of the DebuggerILToNativeMaps
//      in m_sequenceMap.
//
// bool m_sequenceMapSorted:   Set to true once m_sequenceMapSorted is sorted
//      into ascending IL order (Debugger::setBoundaries, SortMap).
//

class DebuggerJitInfo
{
public:
    NativeCodeVersion        m_nativeCodeVersion;

    // Loader module is used to control life-time of DebufferJitInfo. Ideally, we would refactor the code to use LoaderAllocator here
    // instead because of it is what the VM actually uses to track the life time. It would make the debugger interface less chatty.
    PTR_Module               m_pLoaderModule;

    bool                     m_jitComplete;

#ifdef EnC_SUPPORTED
    // If this is true, then we've plastered the method with DebuggerEncBreakpoints
    // and the method has been EnC'd
    bool                     m_encBreakpointsApplied;
#endif //EnC_SUPPORTED

    PTR_DebuggerMethodInfo   m_methodInfo;

    CORDB_ADDRESS            m_addrOfCode;
    SIZE_T                   m_sizeOfCode;

    CodeRegionInfo           m_codeRegionInfo;

    PTR_DebuggerJitInfo      m_prevJitInfo;
    PTR_DebuggerJitInfo      m_nextJitInfo;

protected:
    // The jit maps are lazy-initialized.
    // They are always sorted.
    ULONG                    m_lastIL;
    PTR_DebuggerILToNativeMap m_sequenceMap;
    unsigned int             m_sequenceMapCount;
    PTR_DebuggerILToNativeMap m_callsiteMap;
    unsigned int             m_callsiteMapCount;
    bool                     m_sequenceMapSorted;

    PTR_NativeVarInfo        m_varNativeInfo;
    unsigned int             m_varNativeInfoCount;

    bool                     m_fAttemptInit;

#ifndef DACCESS_COMPILE
    void LazyInitBounds();
#else
    void LazyInitBounds() { LIMITED_METHOD_DAC_CONTRACT; }
#endif

public:
    unsigned int GetSequenceMapCount()
    {
        SUPPORTS_DAC;

        LazyInitBounds();
        return m_sequenceMapCount;
    }

    //@todo: this method could return NULL, but some callers are not handling the case
    PTR_DebuggerILToNativeMap GetSequenceMap()
    {
        SUPPORTS_DAC;

        LazyInitBounds();
        return m_sequenceMap;
    }

    unsigned int GetCallsiteMapCount()
    {
        SUPPORTS_DAC;

        LazyInitBounds();
        return m_callsiteMapCount;
    }

    PTR_DebuggerILToNativeMap GetCallSiteMap()
    {
        SUPPORTS_DAC;

        LazyInitBounds();
        return m_callsiteMap;
    }

    PTR_NativeVarInfo GetVarNativeInfo()
    {
        SUPPORTS_DAC;

        LazyInitBounds();
        return m_varNativeInfo;
    }

    unsigned int GetVarNativeInfoCount()
    {
        SUPPORTS_DAC;

        LazyInitBounds();
        return m_varNativeInfoCount;
    }


    // The version number of this jitted code
    SIZE_T                   m_encVersion;

#if defined(FEATURE_EH_FUNCLETS)
    DWORD                   *m_rgFunclet;
    int                      m_funcletCount;
#endif // FEATURE_EH_FUNCLETS

#ifndef DACCESS_COMPILE

    DebuggerJitInfo(DebuggerMethodInfo *minfo, NativeCodeVersion nativeCodeVersion);
    ~DebuggerJitInfo();

#endif // #ifdef DACCESS_COMPILE

    class ILToNativeOffsetIterator;

    // Usage of ILToNativeOffsetIterator:
    //
    // ILToNativeOffsetIterator it;
    // dji->InitILToNativeOffsetIterator(&it, ilOffset);
    // while (!it.IsAtEnd())
    // {
    //     nativeOffset = it.Current(&fExact);
    //     it.Next();
    // }
    struct ILOffset
    {
        friend class DebuggerJitInfo;
        friend class DebuggerJitInfo::ILToNativeOffsetIterator;

    private:
        SIZE_T m_ilOffset;
#ifdef FEATURE_EH_FUNCLETS
        int m_funcletIndex;
#endif
    };

    struct NativeOffset
    {
        friend class DebuggerJitInfo;
        friend class DebuggerJitInfo::ILToNativeOffsetIterator;

    private:
        SIZE_T m_nativeOffset;
        BOOL   m_fExact;
    };

    class ILToNativeOffsetIterator
    {
        friend class DebuggerJitInfo;

    public:
        ILToNativeOffsetIterator();

        bool   IsAtEnd();
        SIZE_T Current(BOOL* pfExact);
        SIZE_T CurrentAssertOnlyOne(BOOL* pfExact);
        void   Next();

    private:
        void   Init(DebuggerJitInfo* dji, SIZE_T ilOffset);

        DebuggerJitInfo* m_dji;
        ILOffset     m_currentILOffset;
        NativeOffset m_currentNativeOffset;
    };

    void InitILToNativeOffsetIterator(ILToNativeOffsetIterator &it, SIZE_T ilOffset);

    DebuggerILToNativeMap *MapILOffsetToMapEntry(SIZE_T ilOffset, BOOL *exact=NULL, BOOL fWantFirst = TRUE);
    void MapILRangeToMapEntryRange(SIZE_T ilStartOffset, SIZE_T ilEndOffset,
                                   DebuggerILToNativeMap **start,
                                   DebuggerILToNativeMap **end);
    NativeOffset MapILOffsetToNative(ILOffset ilOffset);

    // MapSpecialToNative maps a CordDebugMappingResult to a native
    //      offset so that we can get the address of the prolog & epilog. which
    //      determines which epilog or prolog, if there's more than one.
    SIZE_T MapSpecialToNative(CorDebugMappingResult mapping,
                              SIZE_T which,
                              BOOL *pfAccurate);
#if defined(FEATURE_EH_FUNCLETS)
    void   MapSpecialToNative(int funcletIndex, DWORD* pPrologEndOffset, DWORD* pEpilogStartOffset);
    SIZE_T MapILOffsetToNativeForSetIP(SIZE_T offsetILTo, int funcletIndexFrom, EHRangeTree* pEHRT, BOOL* pExact);
#endif // FEATURE_EH_FUNCLETS

    // MapNativeOffsetToIL Takes a given nativeOffset, and maps it back
    //      to the corresponding IL offset, which it returns.  If mapping indicates
    //      that a the native offset corresponds to a special region of code (for
    //      example, the epilog), then the return value will be specified by
    //      ICorDebugILFrame::GetIP (see cordebug.idl)
    DWORD MapNativeOffsetToIL(SIZE_T nativeOffsetToMap,
                              CorDebugMappingResult *mapping,
                              DWORD *which,
                              BOOL skipPrologs=FALSE);

    // If a method has multiple copies of code (because of EnC or code-pitching),
    // this returns the DJI corresponding to 'pbAddr'
    DebuggerJitInfo *GetJitInfoByAddress(const BYTE *pbAddr );

    void Init(TADDR newAddress);

#if defined(FEATURE_EH_FUNCLETS)
    enum GetFuncletIndexMode
    {
        GFIM_BYOFFSET,
        GFIM_BYADDRESS,
    };

    void  InitFuncletAddress();
    DWORD GetFuncletOffsetByIndex(int index);
    int   GetFuncletIndex(CORDB_ADDRESS offset, GetFuncletIndexMode mode);
    int   GetFuncletCount() {return m_funcletCount;}
#endif // FEATURE_EH_FUNCLETS

    void SetVars(ULONG32 cVars, ICorDebugInfo::NativeVarInfo *pVars);
    void SetBoundaries(ULONG32 cMap, ICorDebugInfo::OffsetMapping *pMap);

    ICorDebugInfo::SourceTypes GetSrcTypeFromILOffset(SIZE_T ilOffset);

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif

    // Debug support
    CHECK Check() const;
    CHECK Invariant() const;
};

#if !defined(DACCESS_COMPILE)
// @dbgtodo Microsoft inspection: get rid of this class when IPC events are eliminated. It's been copied to
// dacdbistructures
/*
 * class MapSortIL:  A template class that will sort an array of DebuggerILToNativeMap.
 * This class is intended to be instantiated on the stack / in temporary storage, and used to reorder the sequence map.
 */
class MapSortIL : public CQuickSort<DebuggerILToNativeMap>
{
  public:
    //Constructor
    MapSortIL(DebuggerILToNativeMap *map,
              int count)
      : CQuickSort<DebuggerILToNativeMap>(map, count) {}

    inline int CompareInternal(DebuggerILToNativeMap *first,
                               DebuggerILToNativeMap *second)
    {
        LIMITED_METHOD_CONTRACT;

        if (first->nativeStartOffset == second->nativeStartOffset)
            return 0;
        else if (first->nativeStartOffset < second->nativeStartOffset)
            return -1;
        else
            return 1;
    }

    //Comparison operator
    int Compare(DebuggerILToNativeMap *first,
                DebuggerILToNativeMap *second)
    {
        LIMITED_METHOD_CONTRACT;

        const DWORD call_inst = (DWORD)ICorDebugInfo::CALL_INSTRUCTION;

        //PROLOGs go first
        if (first->ilOffset == (ULONG) ICorDebugInfo::PROLOG
            && second->ilOffset == (ULONG) ICorDebugInfo::PROLOG)
        {
            return CompareInternal(first, second);
        } else if (first->ilOffset == (ULONG) ICorDebugInfo::PROLOG)
        {
            return -1;
        } else if (second->ilOffset == (ULONG) ICorDebugInfo::PROLOG)
        {
            return 1;
        }
        // call_instruction goes at the very very end of the table.
        else if ((first->source & call_inst) == call_inst
            && (second->source & call_inst) == call_inst)
        {
            return CompareInternal(first, second);
        } else if ((first->source & call_inst) == call_inst)
        {
            return 1;
        } else if ((second->source & call_inst) == call_inst)
        {
            return -1;
        }
        //NO_MAPPING go last
        else if (first->ilOffset == (ULONG) ICorDebugInfo::NO_MAPPING
            && second->ilOffset == (ULONG) ICorDebugInfo::NO_MAPPING)
        {
            return CompareInternal(first, second);
        } else if (first->ilOffset == (ULONG) ICorDebugInfo::NO_MAPPING)
        {
            return 1;
        } else if (second->ilOffset == (ULONG) ICorDebugInfo::NO_MAPPING)
        {
            return -1;
        }
        //EPILOGs go next-to-last
        else if (first->ilOffset == (ULONG) ICorDebugInfo::EPILOG
            && second->ilOffset == (ULONG) ICorDebugInfo::EPILOG)
        {
            return CompareInternal(first, second);
        } else if (first->ilOffset == (ULONG) ICorDebugInfo::EPILOG)
        {
            return 1;
        } else if (second->ilOffset == (ULONG) ICorDebugInfo::EPILOG)
        {
            return -1;
        }
        //normal offsets compared otherwise
        else if (first->ilOffset < second->ilOffset)
            return -1;
        else if (first->ilOffset == second->ilOffset)
            return CompareInternal(first, second);
        else
            return 1;
    }
};

/*
 * class MapSortNative:  A template class that will sort an array of DebuggerILToNativeMap by the nativeStartOffset field.
 * This class is intended to be instantiated on the stack / in temporary storage, and used to reorder the sequence map.
 */
class MapSortNative : public CQuickSort<DebuggerILToNativeMap>
{
  public:
    //Constructor
    MapSortNative(DebuggerILToNativeMap *map,
                  int count)
      : CQuickSort<DebuggerILToNativeMap>(map, count)
    {
        WRAPPER_NO_CONTRACT;
    }


    //Returns -1,0,or 1 if first's nativeStartOffset is less than, equal to, or greater than second's
    int Compare(DebuggerILToNativeMap *first,
                DebuggerILToNativeMap *second)
    {
        LIMITED_METHOD_CONTRACT;

        if (first->nativeStartOffset < second->nativeStartOffset)
            return -1;
        else if (first->nativeStartOffset == second->nativeStartOffset)
            return 0;
        else
            return 1;
    }
};
#endif //!DACCESS_COMPILE

/* ------------------------------------------------------------------------ *
 * Import flares from assembly file
 * We rely on flares having unique addresses, and so we need to keeps them
 * from getting folded by the linker (Since they are identical code).
 * ------------------------------------------------------------------------ */

extern "C" void __stdcall SignalHijackStartedFlare(void);
extern "C" void __stdcall ExceptionForRuntimeHandoffStartFlare(void);
extern "C" void __stdcall ExceptionForRuntimeHandoffCompleteFlare(void);
extern "C" void __stdcall SignalHijackCompleteFlare(void);
extern "C" void __stdcall ExceptionNotForRuntimeFlare(void);
extern "C" void __stdcall NotifyRightSideOfSyncCompleteFlare(void);
extern "C" void __stdcall NotifySecondChanceReadyForDataFlare(void);

/* ------------------------------------------------------------------------ *
 * Debugger class
 * ------------------------------------------------------------------------ */


// Forward declare some parameter marshalling structs
struct ShouldAttachDebuggerParams;
struct EnsureDebuggerAttachedParams;
struct SendMDANotificationParams;

// class Debugger:  This class implements DebugInterface to provide
// the hooks to the Runtime directly.
//

class Debugger : public DebugInterface
{
    VPTR_VTABLE_CLASS(Debugger, DebugInterface);
public:

#ifndef DACCESS_COMPILE
    Debugger();
    virtual ~Debugger();
#else
    virtual ~Debugger() {}
#endif

    // If 0, then not yet initialized. If non-zero, then LS is initialized.
    LONG m_fLeftSideInitialized;

    // This flag controls the window where SetDesiredNGENCompilerFlags is allowed,
    // which is until Debugger::StartupPhase2 is complete. Typically it would be
    // set during the CreateProcess debug event but it could be set other times such
    // as module load for clr.dll.
    SVAL_DECL(BOOL, s_fCanChangeNgenFlags);

    friend class DebuggerLazyInit;
#ifdef TEST_DATA_CONSISTENCY
    friend class DataTest;
#endif

    // Checks if the JitInfos table has been allocated, and if not does so.
    HRESULT inline CheckInitMethodInfoTable();
    HRESULT inline CheckInitModuleTable();
    HRESULT CheckInitPendingFuncEvalTable();

#ifndef DACCESS_COMPILE
    DWORD GetRCThreadId()
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

        if (m_pRCThread)
            return m_pRCThread->GetRCThreadId();
        else
            return 0;
    }
#endif

    //
    // Methods exported from the Runtime Controller to the Runtime.
    // (These are the methods specified by DebugInterface.)
    //
    HRESULT Startup(void);

    HRESULT StartupPhase2(Thread * pThread);

    void CleanupTransportSocket();

    void InitializeLazyDataIfNecessary();

    void LazyInit(); // will throw
    HRESULT LazyInitWrapper(); // calls LazyInit and converts to HR.

    // Helper on startup to notify debugger
    void RaiseStartupNotification();

    // Send a raw managed debug event over the managed pipeline.
    void SendRawEvent(const DebuggerIPCEvent * pManagedEvent);

    // Message box API for the left side of the debugger. This API handles calls from the
    // debugger helper thread as well as from normal EE threads. It is the only one that
    // should be used from inside the debugger left side.
    int MessageBox(
                UINT uText,       // Resource Identifier for Text message
                UINT uCaption,    // Resource Identifier for Caption
                UINT uType,       // Style of MessageBox
                BOOL displayForNonInteractive,      // Display even if the process is running non interactive
                BOOL showFileNameInTitle,           // Flag to show FileName in Caption
                ...);             // Additional Arguments

    void SetEEInterface(EEDebugInterface* i);
    void StopDebugger(void);
    BOOL IsStopped(void)
    {
        LIMITED_METHOD_CONTRACT;
        // implements DebugInterface but also is called internally
        return m_stopped;
    }



    void ThreadCreated(Thread* pRuntimeThread);
    void ThreadStarted(Thread* pRuntimeThread);
    void DetachThread(Thread *pRuntimeThread);

    BOOL SuspendComplete(bool isEESuspendedForGC = false);

    void LoadModule(Module* pRuntimeModule,
                    LPCWSTR pszModuleName,
                    DWORD dwModuleName,
                    Assembly *pAssembly,
                    AppDomain *pAppDomain,
                    DomainFile * pDomainFile,
                    BOOL fAttaching);
    void LoadModuleFinished(Module* pRuntimeModule, AppDomain * pAppDomain);
    DebuggerModule * AddDebuggerModule(DomainFile * pDomainFile);


    void UnloadModule(Module* pRuntimeModule,
                      AppDomain *pAppDomain);
    void DestructModule(Module *pModule);

    void RemoveModuleReferences(Module * pModule);


    void SendUpdateModuleSymsEventAndBlock(Module * pRuntimeModule, AppDomain * pAppDomain);
    void SendRawUpdateModuleSymsEvent(Module * pRuntimeModule, AppDomain * pAppDomain);

    BOOL LoadClass(TypeHandle th,
                   mdTypeDef classMetadataToken,
                   Module* classModule,
                   AppDomain *pAppDomain);
    void UnloadClass(mdTypeDef classMetadataToken,
                     Module* classModule,
                     AppDomain *pAppDomain);

    void SendClassLoadUnloadEvent (mdTypeDef classMetadataToken,
                                   DebuggerModule *classModule,
                                   Assembly *pAssembly,
                                   AppDomain *pAppDomain,
                                   BOOL fIsLoadEvent);
    BOOL SendSystemClassLoadUnloadEvent (mdTypeDef classMetadataToken,
                                         Module *classModule,
                                         BOOL fIsLoadEvent);

    void SendCatchHandlerFound(Thread *pThread,
                               FramePointer fp,
                               SIZE_T nOffset,
                               DWORD  dwFlags);

    LONG NotifyOfCHFFilter(EXCEPTION_POINTERS* pExceptionPointers, PVOID pCatchStackAddr);


    bool FirstChanceNativeException(EXCEPTION_RECORD *exception,
                               T_CONTEXT *context,
                               DWORD code,
                               Thread *thread);

    bool IsJMCMethod(Module* pModule, mdMethodDef tkMethod);

    int GetMethodEncNumber(MethodDesc * pMethod);


    bool FirstChanceManagedException(Thread *pThread, SIZE_T currentIP, SIZE_T currentSP);

    void FirstChanceManagedExceptionCatcherFound(Thread *pThread,
                                                 MethodDesc *pMD, TADDR pMethodAddr,
                                                 BYTE *currentSP,
                                                 EE_ILEXCEPTION_CLAUSE *pEHClause);

    LONG LastChanceManagedException(EXCEPTION_POINTERS * pExceptionInfo,
                                    Thread *pThread,
                                    BOOL jitAttachRequested);

    void ManagedExceptionUnwindBegin(Thread *pThread);

    void DeleteInterceptContext(void *pContext);

    void ExceptionFilter(MethodDesc *fd, TADDR pMethodAddr, SIZE_T offset, BYTE *pStack);
    void ExceptionHandle(MethodDesc *fd, TADDR pMethodAddr, SIZE_T offset, BYTE *pStack);

    int NotifyUserOfFault(bool userBreakpoint, DebuggerLaunchSetting dls);

    SIZE_T GetArgCount(MethodDesc* md, BOOL *fVarArg = NULL);

    void FuncEvalComplete(Thread *pThread, DebuggerEval *pDE);

    DebuggerMethodInfo *CreateMethodInfo(Module *module, mdMethodDef md);
    void JITComplete(NativeCodeVersion nativeCodeVersion, TADDR newAddress);

    HRESULT RequestFavor(FAVORCALLBACK fp, void * pData);

#ifdef EnC_SUPPORTED
    HRESULT UpdateFunction(MethodDesc* pFD, SIZE_T encVersion);
    HRESULT AddFunction(MethodDesc* md, SIZE_T enCVersion);
    HRESULT UpdateNotYetLoadedFunction(mdMethodDef token, Module * pModule, SIZE_T enCVersion);

    HRESULT AddField(FieldDesc* fd, SIZE_T enCVersion);
    HRESULT RemapComplete(MethodDesc *pMd, TADDR addr, SIZE_T nativeOffset);

    HRESULT MapILInfoToCurrentNative(MethodDesc *pMD,
                                     SIZE_T ilOffset,
                                     TADDR nativeFnxStart,
                                     SIZE_T *nativeOffset);
#endif // EnC_SUPPORTED

    void GetVarInfo(MethodDesc *       fd,         // [IN] method of interest
                    void *DebuggerVersionToken,    // [IN] which edit version
                    SIZE_T *           cVars,      // [OUT] size of 'vars'
                    const ICorDebugInfo::NativeVarInfo **vars     // [OUT] map telling where local vars are stored
                    );

    void getBoundariesHelper(MethodDesc * ftn,
                             unsigned int *cILOffsets, DWORD **pILOffsets);
    void getBoundaries(MethodDesc * ftn,
                       unsigned int *cILOffsets, DWORD **pILOffsets,
                       ICorDebugInfo::BoundaryTypes* implictBoundaries);

    void getVars(MethodDesc * ftn,
                 ULONG32 *cVars, ICorDebugInfo::ILVarInfo **vars,
                 bool *extendOthers);

    DebuggerMethodInfo *GetOrCreateMethodInfo(Module *pModule, mdMethodDef token);

    PTR_DebuggerMethodInfoTable GetMethodInfoTable() { return m_pMethodInfos; }

    // Gets the DJI for 'fd'
    // If 'pbAddr' is non-NULL and if the method has multiple copies of code
    // (because of EnC or code-pitching), this returns the DJI corresponding
    // to 'pbAddr'
    DebuggerJitInfo *GetJitInfo(MethodDesc *fd, const BYTE *pbAddr, DebuggerMethodInfo **pMethInfo = NULL);

    // Several ways of getting a DJI. DJIs are 1:1 w/ Native Code blobs.
    // Caller must guarantee good parameters.
    // DJIs can be lazily created; so the only way these will fail is in an OOM case.
    DebuggerJitInfo *GetJitInfoFromAddr(TADDR addr);

    // EnC trashes the methoddesc to point to the latest version. Thus given a method-desc,
    // we can get the most recent DJI.
    DebuggerJitInfo *GetLatestJitInfoFromMethodDesc(MethodDesc * pMethodDesc);


    HRESULT GetILToNativeMapping(PCODE pNativeCodeStartAddress, ULONG32 cMap, ULONG32 *pcMap,
                                 COR_DEBUG_IL_TO_NATIVE_MAP map[]);

    HRESULT GetILToNativeMappingIntoArrays(
        MethodDesc * pMethodDesc,
        PCODE pNativeCodeStartAddress,
        USHORT cMapMax,
        USHORT * pcMap,
        UINT ** prguiILOffset,
        UINT ** prguiNativeOffset);

    PRD_TYPE GetPatchedOpcode(CORDB_ADDRESS_TYPE *ip);
    BOOL CheckGetPatchedOpcode(CORDB_ADDRESS_TYPE *address, /*OUT*/ PRD_TYPE *pOpcode);

    void TraceCall(const BYTE *address);

    bool ThreadsAtUnsafePlaces(void);


    void PollWaitingForHelper();

    void IncThreadsAtUnsafePlaces(void)
    {
        LIMITED_METHOD_CONTRACT;
        InterlockedIncrement(&m_threadsAtUnsafePlaces);
    }

    void DecThreadsAtUnsafePlaces(void)
    {
        LIMITED_METHOD_CONTRACT;
        InterlockedDecrement(&m_threadsAtUnsafePlaces);
    }

    static StackWalkAction AtSafePlaceStackWalkCallback(CrawlFrame *pCF,
                                                        VOID* data);
    bool IsThreadAtSafePlaceWorker(Thread *thread);
    bool IsThreadAtSafePlace(Thread *thread);

    CorDebugUserState GetFullUserState(Thread *pThread);


    void Terminate();
    void Continue();

    bool HandleIPCEvent(DebuggerIPCEvent* event);

    DebuggerModule * LookupOrCreateModule(VMPTR_DomainFile vmDomainFile);
    DebuggerModule * LookupOrCreateModule(DomainFile * pDomainFile);
    DebuggerModule * LookupOrCreateModule(Module * pModule, AppDomain * pAppDomain);

    HRESULT GetAndSendInterceptCommand(DebuggerIPCEvent *event);

    //HRESULT GetAndSendJITFunctionData(DebuggerRCThread* rcThread,
    //                               mdMethodDef methodToken,
    //                               void* functionModuleToken);
    HRESULT GetFuncData(mdMethodDef funcMetadataToken,
                        DebuggerModule* pDebuggerModule,
                        SIZE_T nVersion,
                        DebuggerIPCE_FuncData *data);


    // The following four functions convert between type handles and the data that is
    // shipped for types to and from the right-side.
    //
    // I'm heading toward getting rid of the first two - they are almost never used.
    static HRESULT ExpandedTypeInfoToTypeHandle(DebuggerIPCE_ExpandedTypeData *data,
                                                unsigned int genericArgsCount,
                                                DebuggerIPCE_BasicTypeData *genericArgs,
                                                TypeHandle *pRes);
    static HRESULT BasicTypeInfoToTypeHandle(DebuggerIPCE_BasicTypeData *data,
                                             TypeHandle *pRes);
    void TypeHandleToBasicTypeInfo(AppDomain *pAppDomain,
                                   TypeHandle th,
                                   DebuggerIPCE_BasicTypeData *res);

    // TypeHandleToExpandedTypeInfo returns different DebuggerIPCE_ExpandedTypeData objects
    // depending on whether the object value that the TypeData corresponds to is
    // boxed or not.  Different parts of the API transfer objects in slightly different ways.
    // AllBoxed:
    //    For GetAndSendObjectData all values are boxed,
    //
    // StructsBoxed:
    //     When returning results from FuncEval only "true" structs
    //     get boxed, i.e. primitives are unboxed.
    //
    // NoSpecialBoxing:
    //     TypeHandleToExpandedTypeInfo is also used to report type parameters,
    //      and in this case none of the types are considered boxed (
    enum AreValueTypesBoxed { NoValueTypeBoxing, OnlyPrimitivesUnboxed, AllBoxed };

    void TypeHandleToExpandedTypeInfo(AreValueTypesBoxed boxed,
                                      AppDomain *pAppDomain,
                                      TypeHandle th,
                                      DebuggerIPCE_ExpandedTypeData *res);

    class TypeDataWalk
    {
        DebuggerIPCE_TypeArgData *m_curdata;
        unsigned int m_remaining;

    public:
        TypeDataWalk(DebuggerIPCE_TypeArgData *pData, unsigned int nData)
        {
            m_curdata = pData;
            m_remaining = nData;
        }


        // These are for type arguments in the funceval case.
        // They throw COMPLUS exceptions if they fail, so can only be used during funceval.
        void ReadTypeHandles(unsigned int nTypeArgs, TypeHandle *pRes);
        TypeHandle ReadInstantiation(Module *pModule, mdTypeDef tok, unsigned int nTypeArgs);
        TypeHandle ReadTypeHandle();

        BOOL Finished() { LIMITED_METHOD_CONTRACT; return m_remaining == 0; }
        DebuggerIPCE_TypeArgData *ReadOne() { LIMITED_METHOD_CONTRACT; if (m_remaining) { m_remaining--; return m_curdata++; } else return NULL; }

    };



    HRESULT GetMethodDescData(MethodDesc *pFD,
                              DebuggerJitInfo *pJITInfo,
                              DebuggerIPCE_JITFuncData *data);

    void GetAndSendTransitionStubInfo(CORDB_ADDRESS_TYPE *stubAddress);

    void SendBreakpoint(Thread *thread, T_CONTEXT *context,
                        DebuggerBreakpoint *breakpoint);
#ifdef FEATURE_DATABREAKPOINT
    void SendDataBreakpoint(Thread* thread, T_CONTEXT *context, DebuggerDataBreakpoint *breakpoint);
#endif // FEATURE_DATABREAKPOINT
    void SendStep(Thread *thread, T_CONTEXT *context,
                  DebuggerStepper *stepper,
                  CorDebugStepReason reason);

    void LockAndSendEnCRemapEvent(DebuggerJitInfo * dji, SIZE_T currentIP, SIZE_T *resumeIP);
    void LockAndSendEnCRemapCompleteEvent(MethodDesc *pFD);
    void SendEnCUpdateEvent(DebuggerIPCEventType eventType,
                            Module * pModule,
                            mdToken memberToken,
                            mdTypeDef classToken,
                            SIZE_T enCVersion);
    void LockAndSendBreakpointSetError(PATCH_UNORDERED_ARRAY * listUnbindablePatches);

    // helper for SendException
    void SendExceptionEventsWorker(
        Thread * pThread,
        bool firstChance,
        bool fIsInterceptable,
        bool continuable,
        SIZE_T currentIP,
        FramePointer framePointer,
        bool atSafePlace);

    // Main function to send an exception event, handle jit-attach if needed, etc
    HRESULT SendException(Thread *pThread,
                          bool fFirstChance,
                          SIZE_T currentIP,
                          SIZE_T currentSP,
                          bool fContinuable,
                          bool fAttaching,
                          bool fForceNonInterceptable,
                          EXCEPTION_POINTERS * pExceptionInfo);

    // Top-level function to handle sending a user-breakpoint, jit-attach, sync, etc.
    void SendUserBreakpoint(Thread * thread);

    // Send the user breakpoint and block waiting for a continue.
    void SendUserBreakpointAndSynchronize(Thread * pThread);

    // Just send the actual event.
    void SendRawUserBreakpoint(Thread *thread);



    void SendInterceptExceptionComplete(Thread *thread);

    HRESULT AttachDebuggerForBreakpoint(Thread *thread,
                                        __in_opt WCHAR *wszLaunchReason);


    void ThreadIsSafe(Thread *thread);

    void UnrecoverableError(HRESULT errorHR,
                            unsigned int errorCode,
                            const char *errorFile,
                            unsigned int errorLine,
                            bool exitThread);

    virtual BOOL IsSynchronizing(void)
    {
        LIMITED_METHOD_CONTRACT;

        return m_trappingRuntimeThreads;
    }

    //
    // The debugger mutex is used to protect any "global" Left Side
    // data structures. The RCThread takes it when handling a Right
    // Side event, and Runtime threads take it when processing
    // debugger events.
    //
#ifdef _DEBUG
    int m_mutexCount;
#endif

    // Helper function
    HRESULT AttachDebuggerForBreakpointOnHelperThread(Thread *pThread);

    // helper function to send Exception IPC event and Exception_CallBack2 event
    HRESULT SendExceptionHelperAndBlock(
        Thread      *pThread,
        OBJECTHANDLE exceptionHandle,
        bool        continuable,
        FramePointer framePointer,
        SIZE_T      nOffset,
        CorDebugExceptionCallbackType eventType,
        DWORD       dwFlags);


    // Helper function to send out LogMessage only. Can be either on helper thread or manager thread.
    void SendRawLogMessage(
        Thread                                    *pThread,
        AppDomain                                 *pAppDomain,
        int                                        iLevel,
        SString *   pCategory,
        SString *   pMessage);


    // Helper function to send MDA notification
    void SendRawMDANotification(SendMDANotificationParams * params);
    static void SendMDANotificationOnHelperThreadProxy(SendMDANotificationParams * params);

    // Returns a bitfield reflecting the managed debugging state at the time of
    // the jit attach.
    CLR_DEBUGGING_PROCESS_FLAGS GetAttachStateFlags();

    // Records that this thread is about to trigger jit attach and
    // resolves the race for which thread gets to trigger it
    BOOL PreJitAttach(BOOL willSendManagedEvent, BOOL willLaunchDebugger, BOOL explicitUserRequest);

    // Blocks until the debugger completes jit attach
    void WaitForDebuggerAttach();

    // Cleans up after jit attach is complete
    void PostJitAttach();

    // Main worker function to initiate, handle, and wait for a Jit-attach.
    void JitAttach(Thread * pThread, EXCEPTION_POINTERS * pExceptionInfo, BOOL willSendManagedEvent, BOOL explicitUserRequest);

private:
    void DoNotCallDirectlyPrivateLock(void);
    void DoNotCallDirectlyPrivateUnlock(void);

    // This function gets the jit debugger launched and waits for the native attach to complete
    // Make sure you called PreJitAttach and it returned TRUE before you call this
    HRESULT LaunchJitDebuggerAndNativeAttach(Thread * pThread, EXCEPTION_POINTERS * pExceptionInfo);

    // Helper to serialize metadata that has been updated by the profiler into
    // a buffer so that it can be read out-of-proc
    BYTE* SerializeModuleMetaData(Module * pModule, DWORD * countBytes);

    /// Wrapps fusion Module FusionCopyPDBs.
    HRESULT CopyModulePdb(Module* pRuntimeModule);

    // When attaching to a process, this is called to enumerate all of the
    // AppDomains currently in the process and allow modules pdbs to be copied over to the shadow dir maintaining out V2 in-proc behaviour.
    HRESULT IterateAppDomainsForPdbs();

#ifndef DACCESS_COMPILE
public:
    // Helper function to initialize JDI structure
    void InitDebuggerLaunchJitInfo(Thread * pThread, EXCEPTION_POINTERS * pExceptionInfo);

    // Helper function to retrieve JDI structure
    JIT_DEBUG_INFO * GetDebuggerLaunchJitInfo(void);

private:
    static JIT_DEBUG_INFO   s_DebuggerLaunchJitInfo;
    static EXCEPTION_RECORD s_DebuggerLaunchJitInfoExceptionRecord;
    static CONTEXT          s_DebuggerLaunchJitInfoContext;

    static void AcquireDebuggerLock(Debugger *c)
    {
        WRAPPER_NO_CONTRACT;
        c->DoNotCallDirectlyPrivateLock();
    }

    static void ReleaseDebuggerLock(Debugger *c)
    {
        WRAPPER_NO_CONTRACT;
        c->DoNotCallDirectlyPrivateUnlock();
    }
#else // DACCESS_COMPILE
    static void AcquireDebuggerLock(Debugger *c);
    static void ReleaseDebuggerLock(Debugger *c);
#endif // DACCESS_COMPILE


public:
    // define type for DebuggerLockHolder
    typedef DacHolder<Debugger *, Debugger::AcquireDebuggerLock, Debugger::ReleaseDebuggerLock> DebuggerLockHolder;

    void LockForEventSending(DebuggerLockHolder *dbgLockHolder);
    void UnlockFromEventSending(DebuggerLockHolder *dbgLockHolder);
    void SyncAllThreads(DebuggerLockHolder *dbgLockHolder);
    void SendSyncCompleteIPCEvent(bool isEESuspendedForGC = false);

    // Helper for sending a single pre-baked IPC event and blocking on the continue.
    // See definition of SENDIPCEVENT_BEGIN for usage pattern.
    void SendSimpleIPCEventAndBlock();

    void SendCreateProcess(DebuggerLockHolder * pDbgLockHolder);

    void IncrementClassLoadCallbackCount(void)
    {
        LIMITED_METHOD_CONTRACT;
        InterlockedIncrement(&m_dClassLoadCallbackCount);
    }

    void DecrementClassLoadCallbackCount(void)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(m_dClassLoadCallbackCount > 0);
        InterlockedDecrement(&m_dClassLoadCallbackCount);
    }


#ifdef _DEBUG_IMPL
    bool ThreadHoldsLock(void)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

        if (g_fProcessDetach)
            return true;

        BEGIN_GETTHREAD_ALLOWED;
        if (g_pEEInterface->GetThread())
        {
            return (GetThreadIdHelper(g_pEEInterface->GetThread()) == m_mutexOwner);
        }
        else
        {
            return (GetCurrentThreadId() == m_mutexOwner);
        }
        END_GETTHREAD_ALLOWED;
    }
#endif // _DEBUG_IMPL

#ifdef FEATURE_INTEROP_DEBUGGING
    static VOID M2UHandoffHijackWorker(
                             T_CONTEXT *pContext,
                             EXCEPTION_RECORD *pExceptionRecord);

    LONG FirstChanceSuspendHijackWorker(
                             T_CONTEXT *pContext,
                             EXCEPTION_RECORD *pExceptionRecord);
    static void GenericHijackFunc(void);
    static void SecondChanceHijackFunc(void);
    static void SecondChanceHijackFuncWorker(void);
    static void SignalHijackStarted(void);
    static void ExceptionForRuntimeHandoffStart(void);
    static void ExceptionForRuntimeHandoffComplete(void);
    static void SignalHijackComplete(void);
    static void ExceptionNotForRuntime(void);
    static void NotifyRightSideOfSyncComplete(void);
    static void NotifySecondChanceReadyForData(void);
#endif // FEATURE_INTEROP_DEBUGGING

    void UnhandledHijackWorker(T_CONTEXT * pContext, EXCEPTION_RECORD * pRecord);

    //
    // InsertToMethodInfoList puts the given DMI onto the DMI list.
    //
    HRESULT InsertToMethodInfoList(DebuggerMethodInfo *dmi);


    // MapBreakpoints will map any and all breakpoints (except EnC
    //      patches) from previous versions of the method into the current version.
    HRESULT MapAndBindFunctionPatches( DebuggerJitInfo *pJiNew,
        MethodDesc * fd,
        CORDB_ADDRESS_TYPE * addrOfCode);

    // MPTDJI takes the given patch (and djiFrom, if you've got it), and
    // does the IL mapping forwards to djiTo.  Returns
    // CORDBG_E_CODE_NOT_AVAILABLE if there isn't a mapping, which means that
    // no patch was placed.
    HRESULT MapPatchToDJI(DebuggerControllerPatch *dcp, DebuggerJitInfo *djiTo);

    HRESULT LaunchDebuggerForUser(Thread * pThread, EXCEPTION_POINTERS * pExceptionInfo,
        BOOL useManagedBPForManagedAttach, BOOL explicitUserRequest);

    void SendLogMessage (int iLevel,
                         SString * pSwitchName,
                         SString * pMessage);

    void SendLogSwitchSetting (int iLevel,
                               int iReason,
                               __in_z LPCWSTR pLogSwitchName,
                               __in_z LPCWSTR pParentSwitchName);

    bool IsLoggingEnabled (void)
    {
        LIMITED_METHOD_CONTRACT;

        if (m_LoggingEnabled)
            return true;
        return false;
    }

    // send a custom debugger notification to the RS
    void SendCustomDebuggerNotification(Thread * pThread, DomainFile * pDomain, mdTypeDef classToken);

    // Send an MDA notification. This ultimately translates to an ICorDebugMDA object on the Right-Side.
    void SendMDANotification(
        Thread * pThread, // may be NULL. Lets us send on behalf of other threads.
        SString * szName,
        SString * szDescription,
        SString * szXML,
        CorDebugMDAFlags flags,
        BOOL bAttach
    );


    void EnableLogMessages (bool fOnOff) {LIMITED_METHOD_CONTRACT;  m_LoggingEnabled = fOnOff;}
    bool GetILOffsetFromNative (MethodDesc *PFD, const BYTE *pbAddr,
                                DWORD nativeOffset, DWORD *ilOffset);

    DWORD GetHelperThreadID(void );


    HRESULT SetIP( bool fCanSetIPOnly,
                   Thread *thread,
                   Module *module,
                   mdMethodDef mdMeth,
                   DebuggerJitInfo* dji,
                   SIZE_T offsetILTo,
                   BOOL fIsIL);

    // Helper routines used by Debugger::SetIP

    // If we have a varargs function, we can't set the IP (we don't know how to pack/unpack the arguments), so if we
    // call SetIP with fCanSetIPOnly = true, we need to check for that.
    BOOL IsVarArgsFunction(unsigned int nEntries, PTR_NativeVarInfo varNativeInfo);

    HRESULT ShuffleVariablesGet(DebuggerJitInfo  *dji,
                                SIZE_T            offsetFrom,
                                T_CONTEXT          *pCtx,
                                SIZE_T          **prgVal1,
                                SIZE_T          **prgVal2,
                                BYTE           ***prgpVCs);

    HRESULT ShuffleVariablesSet(DebuggerJitInfo  *dji,
                             SIZE_T            offsetTo,
                             T_CONTEXT          *pCtx,
                             SIZE_T          **prgVal1,
                             SIZE_T          **prgVal2,
                             BYTE            **rgpVCs);

    HRESULT GetVariablesFromOffset(MethodDesc                 *pMD,
                                   UINT                        varNativeInfoCount,
                                   ICorDebugInfo::NativeVarInfo *varNativeInfo,
                                   SIZE_T                      offsetFrom,
                                   T_CONTEXT                    *pCtx,
                                   SIZE_T                     *rgVal1,
                                   SIZE_T                     *rgVal2,
                                   UINT                       uRgValSize, // number of element of the preallocated rgVal1 and rgVal2
                                   BYTE                     ***rgpVCs);

    HRESULT SetVariablesAtOffset(MethodDesc                 *pMD,
                                 UINT                        varNativeInfoCount,
                                 ICorDebugInfo::NativeVarInfo *varNativeInfo,
                                 SIZE_T                      offsetTo,
                                 T_CONTEXT                    *pCtx,
                                 SIZE_T                     *rgVal1,
                                 SIZE_T                     *rgVal2,
                                 BYTE                      **rgpVCs);

    BOOL IsThreadContextInvalid(Thread *pThread);

    // notification for SQL fiber debugging support
    void CreateConnection(CONNID dwConnectionId, __in_z WCHAR *wzName);
    void DestroyConnection(CONNID dwConnectionId);
    void ChangeConnection(CONNID dwConnectionId);

    //
    // This function is used to identify the helper thread.
    //
    bool ThisIsHelperThread(void);

    HRESULT ReDaclEvents(PSECURITY_DESCRIPTOR securityDescriptor);

#ifdef DACCESS_COMPILE
    virtual void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
    virtual void EnumMemoryRegionsIfFuncEvalFrame(CLRDataEnumMemoryFlags flags, Frame * pFrame);
#endif

    BOOL ShouldAutoAttach();
    BOOL FallbackJITAttachPrompt();
    HRESULT SetFiberMode(bool isFiberMode);

    HRESULT AddAppDomainToIPC (AppDomain *pAppDomain);
    HRESULT RemoveAppDomainFromIPC (AppDomain *pAppDomain);
    HRESULT UpdateAppDomainEntryInIPC (AppDomain *pAppDomain);

    void SendCreateAppDomainEvent(AppDomain * pAppDomain);

    // Notify the debugger that an assembly has been loaded
    void LoadAssembly(DomainAssembly * pDomainAssembly);

    // Notify the debugger that an assembly has been unloaded
    void UnloadAssembly(DomainAssembly * pDomainAssembly);

    HRESULT FuncEvalSetup(DebuggerIPCE_FuncEvalInfo *pEvalInfo, BYTE **argDataArea, DebuggerEval **debuggerEvalKey);
    HRESULT FuncEvalSetupReAbort(Thread *pThread, Thread::ThreadAbortRequester requester);
    HRESULT FuncEvalAbort(DebuggerEval *debuggerEvalKey);
    HRESULT FuncEvalRudeAbort(DebuggerEval *debuggerEvalKey);
    HRESULT FuncEvalCleanup(DebuggerEval *debuggerEvalKey);

    HRESULT SetReference(void *objectRefAddress, VMPTR_OBJECTHANDLE vmObjectHandle, void *newReference);
    HRESULT SetValueClass(void *oldData, void *newData, DebuggerIPCE_BasicTypeData *type);

    HRESULT SetILInstrumentedCodeMap(MethodDesc *fd,
                                     BOOL fStartJit,
                                     ULONG32 cILMapEntries,
                                     COR_IL_MAP rgILMapEntries[]);

    void EarlyHelperThreadDeath(void);

    void ShutdownBegun(void);

    void LockDebuggerForShutdown(void);

    void DisableDebugger(void);

    // Pid of the left side process that this Debugger instance is in.
    DWORD GetPid(void) { return m_processId; }

    HRESULT NameChangeEvent(AppDomain *pAppDomain, Thread *pThread);

    // send an event to the RS indicating that there's a Ctrl-C or Ctrl-Break
    BOOL SendCtrlCToDebugger(DWORD dwCtrlType);

    // Allows the debugger to keep an up to date list of special threads
    HRESULT UpdateSpecialThreadList(DWORD cThreadArrayLength, DWORD *rgdwThreadIDArray);

#ifndef DACCESS_COMPILE
    static void AcquireDebuggerDataLock(Debugger *pDebugger);

    static void ReleaseDebuggerDataLock(Debugger *pDebugger);

#else // DACCESS_COMPILE
    // determine whether the LS holds the data lock. If it does, we will assume the locked data is in an
    // inconsistent state and will throw an exception. The DAC will execute this if we are executing code
    // that takes the lock.
    static void AcquireDebuggerDataLock(Debugger *pDebugger);

    // unimplemented--nothing to do here
    static void ReleaseDebuggerDataLock(Debugger *pDebugger);

#endif // DACCESS_COMPILE

    // define type for DebuggerDataLockHolder
    typedef DacHolder<Debugger *, Debugger::AcquireDebuggerDataLock, Debugger::ReleaseDebuggerDataLock> DebuggerDataLockHolder;

#ifdef _DEBUG
    // Use for asserts
    bool HasDebuggerDataLock()
    {
        // If no lazy data yet, then can't possibly have the debugger-data lock.
        if (!g_pDebugger->HasLazyData())
        {
            return false;
        }
        return (g_pDebugger->GetDebuggerDataLock()->OwnedByCurrentThread()) != 0;
    }
#endif


    // For Just-My-Code (aka Just-User-Code).
    // The jit injects probes in debuggable managed methods that look like:
    // if (*pFlag != 0) call JIT_DbgIsJustMyCode.
    // pFlag is unique per-method constant determined by GetJMCFlagAddr.
    // JIT_DbgIsJustMyCode will get the ip & fp and call OnMethodEnter.

    // pIP is an ip within the method, right after the prolog.
#ifndef DACCESS_COMPILE
    virtual void OnMethodEnter(void * pIP);
    virtual DWORD* GetJMCFlagAddr(Module * pModule);
#endif

    // GetJMCFlagAddr provides a unique flag for each module. UpdateModuleJMCFlag
    // will go through all modules with user-code and set their flag to fStatus.
    void UpdateAllModuleJMCFlag(bool fStatus);
    void UpdateModuleJMCFlag(Module * pRuntime, bool fStatus);

    // Set the default JMC status of the specified module.  This function
    // also finds all the DMIs in the specified module and update their
    // JMC status as well.
    void SetModuleDefaultJMCStatus(Module * pRuntimeModule, bool fStatus);

#ifndef DACCESS_COMPILE
    static DWORD GetThreadIdHelper(Thread *pThread);
#endif // DACCESS_COMPILE

private:
    DebuggerJitInfo *GetJitInfoWorker(MethodDesc *fd, const BYTE *pbAddr, DebuggerMethodInfo **pMethInfo);

    // Save the necessary information for the debugger to recognize an IP in one of the thread redirection
    // functions.
    void InitializeHijackFunctionAddress();

    void InitDebugEventCounting();
    void    DoHelperThreadDuty();

    typedef enum
    {
        ATTACH_YES,
        ATTACH_NO,
        ATTACH_TERMINATE
    } ATTACH_ACTION;

    // Returns true if the debugger is not attached and DbgJITDebugLaunchSetting
    // is set to either ATTACH_DEBUGGER or ASK_USER and the user request attaching.
    ATTACH_ACTION ShouldAttachDebugger(bool fIsUserBreakpoint);
    ATTACH_ACTION ShouldAttachDebuggerProxy(bool fIsUserBreakpoint);
    friend void ShouldAttachDebuggerStub(ShouldAttachDebuggerParams * p);
    friend struct ShouldAttachDebuggerParams;

    void TrapAllRuntimeThreads();
    void ReleaseAllRuntimeThreads(AppDomain *pAppDomain);

#ifndef DACCESS_COMPILE
    // @dbgtodo  inspection -  eventually, all replies should be removed because requests will be DAC-ized.
    // Do not call this function unless you are getting ThreadId from RS
    void InitIPCReply(DebuggerIPCEvent *ipce,
                      DebuggerIPCEventType type)
    {
        LIMITED_METHOD_CONTRACT;

        _ASSERTE(ipce != NULL);
        ipce->type = type;
        ipce->hr = S_OK;

        ipce->processId = m_processId;
        ipce->threadId = 0;
        // AppDomain, Thread, are already initialized
    }

    void InitIPCEvent(DebuggerIPCEvent *ipce,
                      DebuggerIPCEventType type,
                      Thread *pThread,
                      AppDomain* pAppDomain)
    {
        WRAPPER_NO_CONTRACT;

        InitIPCEvent(ipce, type, pThread, VMPTR_AppDomain::MakePtr(pAppDomain));
    }

    // Let this function to figure out the unique Id that we will use for Thread.
    void InitIPCEvent(DebuggerIPCEvent *ipce,
                      DebuggerIPCEventType type,
                      Thread *pThread,
                      VMPTR_AppDomain vmAppDomain)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
        }
        CONTRACTL_END;

        _ASSERTE(ipce != NULL);
        ipce->type = type;
        ipce->hr = S_OK;
        ipce->processId = m_processId;
        ipce->threadId = pThread ? pThread->GetOSThreadId() : 0;
        ipce->vmAppDomain = vmAppDomain;
        ipce->vmThread.SetRawPtr(pThread);
    }

    void InitIPCEvent(DebuggerIPCEvent *ipce,
                      DebuggerIPCEventType type)
    {
        WRAPPER_NO_CONTRACT;

        _ASSERTE((type == DB_IPCE_SYNC_COMPLETE) ||
                 (type == DB_IPCE_TEST_CRST) ||
                 (type == DB_IPCE_TEST_RWLOCK));

        Thread *pThread = g_pEEInterface->GetThread();
        AppDomain *pAppDomain = NULL;

        if (pThread)
        {
            pAppDomain = pThread->GetDomain();
        }

        InitIPCEvent(ipce,
                     type,
                     pThread,
                     VMPTR_AppDomain::MakePtr(pAppDomain));
    }
#endif // DACCESS_COMPILE

    HRESULT GetFunctionInfo(Module *pModule,
                            mdToken functionToken,
                            BYTE **pCodeStart,
                            unsigned int *pCodeSize,
                            mdToken *pLocalSigToken);

    // Allocate a buffer and send it to the right side
    HRESULT GetAndSendBuffer(DebuggerRCThread* rcThread, ULONG bufSize);

    // Allocate a buffer in the left-side for use by the right-side
    HRESULT AllocateRemoteBuffer( ULONG bufSize, void **ppBuffer );

    // Releases a previously requested remote bufer and send reply
    HRESULT SendReleaseBuffer(DebuggerRCThread* rcThread, void *pBuffer);

public:
    // Release previously requested remmote buffer
    HRESULT ReleaseRemoteBuffer(void *pBuffer, bool removeFromBlobList);

private:
#ifdef EnC_SUPPORTED
    // Apply an EnC edit and send the result event to the RS
    HRESULT ApplyChangesAndSendResult(DebuggerModule * pDebuggerModule,
                                      DWORD cbMetadata,
                                      BYTE *pMetadata,
                                      DWORD cbIL,
                                      BYTE *pIL);
#endif // EnC_SUPPORTED

    bool GetCompleteDebuggerLaunchString(SString * pStrArgsBuf);

    // Launch a debugger for jit-attach
    void EnsureDebuggerAttached(Thread * pThread, EXCEPTION_POINTERS * pExceptionInfo, BOOL willSendManagedEvent, BOOL explicitUserRequest);
    HRESULT EDAHelper(PROCESS_INFORMATION * pProcessInfo);
    HRESULT EDAHelperProxy(PROCESS_INFORMATION * pProcessInfo);
    friend void EDAHelperStub(EnsureDebuggerAttachedParams * p);
    DebuggerLaunchSetting GetDbgJITDebugLaunchSetting();

public:
    HRESULT InitAppDomainIPC(void);
    HRESULT TerminateAppDomainIPC(void);

    bool ResumeThreads(AppDomain* pAppDomain);

    void ProcessAnyPendingEvals(Thread *pThread);

    bool HasLazyData();
    RCThreadLazyInit * GetRCThreadLazyData();

    // The module table is lazy init, and may be NULL. Callers must check.
    DebuggerModuleTable          * GetModuleTable();

    DebuggerHeap                 *GetInteropSafeHeap();
    DebuggerHeap                 *GetInteropSafeHeap_NoThrow();
    DebuggerHeap                 *GetInteropSafeExecutableHeap();
    DebuggerHeap                 *GetInteropSafeExecutableHeap_NoThrow();
    DebuggerLazyInit             *GetLazyData();
    HelperCanary * GetCanary();
    void MarkDebuggerAttachedInternal();
    void MarkDebuggerUnattachedInternal();

    HANDLE                GetAttachEvent()          { return  GetLazyData()->m_exAttachEvent; }

private:
#ifndef DACCESS_COMPILE
    void StartCanaryThread();
#endif
    DebuggerPendingFuncEvalTable *GetPendingEvals() { return GetLazyData()->m_pPendingEvals; }
    SIZE_T_UNORDERED_ARRAY * GetBPMappingDuplicates() { return &GetLazyData()->m_BPMappingDuplicates; }
    HANDLE                GetUnmanagedAttachEvent() { return  GetLazyData()->m_exUnmanagedAttachEvent; }
    BOOL                  GetDebuggerHandlingCtrlC() { return GetLazyData()->m_DebuggerHandlingCtrlC; }
    void                  SetDebuggerHandlingCtrlC(BOOL f) { GetLazyData()->m_DebuggerHandlingCtrlC = f; }
    HANDLE                GetCtrlCMutex()          { return GetLazyData()->m_CtrlCMutex; }
    UnorderedPtrArray*    GetMemBlobs()            { return &GetLazyData()->m_pMemBlobs; }


    PTR_DebuggerRCThread  m_pRCThread;
    DWORD                 m_processId; // our pid
    BOOL                  m_trappingRuntimeThreads;
    BOOL                  m_stopped;
    BOOL                  m_unrecoverableError;
    BOOL                  m_ignoreThreadDetach;
    PTR_DebuggerMethodInfoTable   m_pMethodInfos;


    // This is the main debugger lock. It is a large lock and used to synchronize complex operations
    // such as sending IPC events, debugger sycnhronization, and attach / detach.
    // The debugger effectively can't make any radical state changes without holding this lock.
    //
    //
    Crst                  m_mutex; // The main debugger lock.

    // Flag to track if the debugger Crst needs to go into "Shutdown for Finalizer" mode.
    // This means that only special shutdown threads (helper / finalizer / shutdown) can
    // take the lock, and all others will just block forever if they take it.
    bool                  m_fShutdownMode;

    //
    // Flag to track if the VM has told the debugger that it should block all threads
    // as soon as possible as it goes thru the debugger.  As of this writing, this is
    // done via the debugger Crst, anyone attempting to take the lock will block forever.
    //
    bool                  m_fDisabled;

#ifdef _DEBUG
    // Ownership tracking for debugging.
    DWORD                 m_mutexOwner;

    // Tid that last called LockForEventSending.
    DWORD                 m_tidLockedForEventSending;
#endif
    LONG                  m_threadsAtUnsafePlaces;
    Volatile<BOOL>        m_jitAttachInProgress;
    BOOL                  m_launchingDebugger;
    BOOL                  m_LoggingEnabled;
    AppDomainEnumerationIPCBlock    *m_pAppDomainCB;

    LONG                  m_dClassLoadCallbackCount;

    // Lazily initialized array of debugger modules
    // @dbgtodo module - eventually, DebuggerModule should go away,
    // and all such information should be stored in either the VM's module class or in the RS.
    DebuggerModuleTable          *m_pModules;

    // DacDbiInterfaceImpl needs to be able to write to private fields in the debugger class.
    friend class DacDbiInterfaceImpl;

    // Set OOP by RS to request a sync after a debug event.
    // Clear by LS when we sync.
    Volatile<BOOL>  m_RSRequestedSync;

    // send first chance/handler found callbacks for exceptions outside of JMC to the LS
    Volatile<BOOL>  m_sendExceptionsOutsideOfJMC;

    // represents different thead redirection functions recognized by the debugger
    enum HijackFunction
    {
        kUnhandledException = 0,
        kRedirectedForGCThreadControl,
        kRedirectedForDbgThreadControl,
        kRedirectedForUserSuspend,
        kRedirectedForYieldTask,
#if defined(HAVE_GCCOVER) && defined(TARGET_AMD64)
        kRedirectedForGCStress,
#endif // HAVE_GCCOVER && TARGET_AMD64
        kMaxHijackFunctions,
    };

    // static array storing the range of the thread redirection functions
    static MemoryRange s_hijackFunction[kMaxHijackFunctions];

    // Currently DAC doesn't support static array members.  This field is used to work around this limitation.
    ARRAY_PTR_MemoryRange m_rgHijackFunction;

public:

    // Sometimes we force all exceptions to be non-interceptable.
    // There are currently three cases where we set this field to true:
    //
    // 1) NotifyOfCHFFilter()
    //      - If the CHF filter is the first handler we encounter in the first pass, then there is no
    //        managed stack frame at which we can intercept the exception anyway.
    //
    // 2) LastChanceManagedException()
    //      - If Watson is launched for an unhandled exception, then the exception cannot be intercepted.
    //
    // 3) SecondChanceHijackFuncWorker()
    //      - The RS hijack the thread to this function to prevent the OS from killing the process at
    //        the end of the first pass.  (When a debugger is attached, the OS does not run a second pass.)
    //        This function ensures that the debugger gets a second chance notification.
    BOOL                          m_forceNonInterceptable;

    // When we are doing an early attach, the RS shim should not queue all the fake attach events for
    // the process, the appdomain, and the thread.  Otherwise we'll get duplicate events when these
    // entities are actually created.  This flag is used to mark whether we are doing an early attach.
    // There are still time windows where we can get duplicate events, but this flag closes down the
    // most common scenario.
    SVAL_DECL(BOOL, s_fEarlyAttach);

private:
    Crst *                 GetDebuggerDataLock() { SUPPORTS_DAC; return &GetLazyData()-> m_DebuggerDataLock; }

    // This is lazily inititalized. It's just a wrapper around a handle so we embed it here.
    DebuggerHeap                 m_heap;
    DebuggerHeap                 m_executableHeap;

    PTR_DebuggerLazyInit         m_pLazyData;


    // A list of all defines that affect layout of MD types
    typedef enum _Target_Defines
    {
        DEFINE__DEBUG = 1,
    } _Target_Defines;

    // A bitfield that has bits set at build time corresponding
    // to which defines are active
    static const int _defines = 0
#ifdef _DEBUG
        | DEFINE__DEBUG
#endif
        ;

public:
    DWORD m_defines;
    DWORD m_mdDataStructureVersion;
#ifndef DACCESS_COMPILE
    virtual void SuspendForGarbageCollectionStarted();
    virtual void SuspendForGarbageCollectionCompleted();
    virtual void ResumeForGarbageCollectionStarted();
#endif
    BOOL m_isBlockedOnGarbageCollectionEvent;
    BOOL m_willBlockOnGarbageCollectionEvent;
    BOOL m_isGarbageCollectionEventsEnabled;
    // this latches m_isGarbageCollectionEventsEnabled in BeforeGarbageCollection so we can
    // guarantee the corresponding AfterGC event is sent even if the events are disabled during GC.
    BOOL m_isGarbageCollectionEventsEnabledLatch;
private:
    HANDLE GetGarbageCollectionBlockerEvent() { return  GetLazyData()->m_garbageCollectionBlockerEvent; }

};



extern "C" {
void STDCALL FuncEvalHijack(void);
void * STDCALL FuncEvalHijackWorker(DebuggerEval *pDE);

void STDCALL ExceptionHijack(void);
void STDCALL ExceptionHijackEnd(void);
void STDCALL ExceptionHijackWorker(T_CONTEXT * pContext, EXCEPTION_RECORD * pRecord, EHijackReason::EHijackReason reason, void * pData);

void RedirectedHandledJITCaseForGCThreadControl_Stub();
void RedirectedHandledJITCaseForGCThreadControl_StubEnd();

void RedirectedHandledJITCaseForDbgThreadControl_Stub();
void RedirectedHandledJITCaseForDbgThreadControl_StubEnd();

void RedirectedHandledJITCaseForUserSuspend_Stub();
void RedirectedHandledJITCaseForUserSuspend_StubEnd();

#if defined(HAVE_GCCOVER) && defined(TARGET_AMD64)
void RedirectedHandledJITCaseForGCStress_Stub();
void RedirectedHandledJITCaseForGCStress_StubEnd();
#endif // HAVE_GCCOVER && TARGET_AMD64
};


// CNewZeroData is the allocator used by the all the hash tables that the helper thread could possibly alter. It uses
// the interop safe allocator.
class CNewZeroData
{
public:
#ifndef DACCESS_COMPILE
    static BYTE *Alloc(int iSize, int iMaxSize)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            PRECONDITION(g_pDebugger != NULL);
        }
        CONTRACTL_END;

        DebuggerHeap * pHeap = g_pDebugger->GetInteropSafeHeap_NoThrow();
        if (pHeap == NULL)
        {
            return NULL;
        }

        BYTE *pb = (BYTE *) pHeap->Alloc(iSize);
        if (pb == NULL)
        {
            return NULL;
        }

        memset(pb, 0, iSize);
        return pb;
    }
    static void Free(BYTE *pPtr, int iSize)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            PRECONDITION(g_pDebugger != NULL);
        }
        CONTRACTL_END;


        DebuggerHeap * pHeap = g_pDebugger->GetInteropSafeHeap_NoThrow();
        _ASSERTE(pHeap != NULL); // should already exist

        pHeap->Free(pPtr);
    }
    static BYTE *Grow(BYTE *&pPtr, int iCurSize)
    {
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            PRECONDITION(g_pDebugger != NULL);
        }
        CONTRACTL_END;

        void *p;

        DebuggerHeap* pHeap = g_pDebugger->GetInteropSafeHeap_NoThrow();
        _ASSERTE(pHeap != NULL); // should already exist

        PREFIX_ASSUME( iCurSize >= 0 );
        S_UINT32 iNewSize = S_UINT32( iCurSize ) + S_UINT32( GrowSize(iCurSize) );
        if( iNewSize.IsOverflow() )
        {
            return NULL;
        }
        p = pHeap->Realloc(pPtr, iNewSize.Value(), iCurSize);
        if (p == NULL)
        {
            return NULL;
        }

        memset((BYTE*)p+iCurSize, 0, GrowSize(iCurSize));
        return (pPtr = (BYTE *)p);
    }

    // A hashtable may recycle memory. We need to zero it out again.
    static void Clean(BYTE * pData, int iSize)
    {
        LIMITED_METHOD_CONTRACT;

        memset(pData, 0, iSize);
    }
#endif // DACCESS_COMPILE

    static int RoundSize(int iSize)
    {
        LIMITED_METHOD_CONTRACT;

        return (iSize);
    }
    static int GrowSize(int iCurSize)
    {
        LIMITED_METHOD_CONTRACT;
        int newSize = (3 * iCurSize) / 2;
        return (newSize < 256) ? 256 : newSize;
    }
};

class DebuggerPendingFuncEvalTable : private CHashTableAndData<CNewZeroData>
{
  public:
    virtual ~DebuggerPendingFuncEvalTable() = default;

  private:

    BOOL Cmp(SIZE_T k1, const HASHENTRY * pc2)
    {
        LIMITED_METHOD_DAC_CONTRACT;

#if defined(DACCESS_COMPILE)
        // This function hasn't been tested yet in the DAC build.  Make sure the DACization is correct.
        DacNotImpl();
#endif // DACCESS_COMPILE

        Thread * pThread1 = reinterpret_cast<Thread *>(k1);
        Thread * pThread2 = dac_cast<PTR_DebuggerPendingFuncEval>(const_cast<HASHENTRY *>(pc2))->pThread;

        return (pThread1 != pThread2);
    }

    ULONG HASH(Thread* pThread)
    {
        LIMITED_METHOD_CONTRACT;
        return (ULONG)((SIZE_T)pThread);   // only use low 32-bits if 64-bit
    }


    SIZE_T KEY(Thread * pThread)
    {
        LIMITED_METHOD_CONTRACT;
        return (SIZE_T)pThread;
    }

  public:

#ifndef DACCESS_COMPILE
    DebuggerPendingFuncEvalTable() : CHashTableAndData<CNewZeroData>(11)
    {
        WRAPPER_NO_CONTRACT;

        SUPPRESS_ALLOCATION_ASSERTS_IN_THIS_SCOPE;
        NewInit(11, sizeof(DebuggerPendingFuncEval), 11);
    }

    void AddPendingEval(Thread *pThread, DebuggerEval *pDE)
    {
        WRAPPER_NO_CONTRACT;

        _ASSERTE((pThread != NULL) && (pDE != NULL));

        DebuggerPendingFuncEval *pfe = (DebuggerPendingFuncEval*)Add(HASH(pThread));
        pfe->pThread = pThread;
        pfe->pDE = pDE;
    }

    void RemovePendingEval(Thread* pThread)
    {
        WRAPPER_NO_CONTRACT;

        _ASSERTE(pThread != NULL);

        DebuggerPendingFuncEval *entry = (DebuggerPendingFuncEval*)Find(HASH(pThread), KEY(pThread));
        Delete(HASH(pThread), (HASHENTRY*)entry);
   }

#endif // #ifndef DACCESS_COMPILE

    DebuggerPendingFuncEval *GetPendingEval(Thread* pThread)
    {
        WRAPPER_NO_CONTRACT;

        DebuggerPendingFuncEval *entry = (DebuggerPendingFuncEval*)Find(HASH(pThread), KEY(pThread));
        return entry;
    }
};

struct DebuggerModuleEntry
{
    FREEHASHENTRY   entry;
    PTR_DebuggerModule  module;
};

typedef DPTR(struct DebuggerModuleEntry) PTR_DebuggerModuleEntry;

class DebuggerModuleTable : private CHashTableAndData<CNewZeroData>
{
#ifdef DACCESS_COMPILE
  public:
    virtual ~DebuggerModuleTable() = default;
#endif

  private:

    BOOL Cmp(SIZE_T k1, const HASHENTRY * pc2)
    {
        LIMITED_METHOD_DAC_CONTRACT;

#if defined(DACCESS_COMPILE)
        // This function hasn't been tested yet in the DAC build.  Make sure the DACization is correct.
        DacNotImpl();
#endif // DACCESS_COMPILE

        Module * pModule1 = reinterpret_cast<Module *>(k1);
        Module * pModule2 =
            dac_cast<PTR_DebuggerModuleEntry>(const_cast<HASHENTRY *>(pc2))->module->GetRuntimeModule();

        return (pModule1 != pModule2);
    }

    ULONG HASH(Module* module)
    {
        LIMITED_METHOD_CONTRACT;
        return (ULONG)((SIZE_T)module);   // only use low 32-bits if 64-bit
    }

    SIZE_T KEY(Module * pModule)
    {
        LIMITED_METHOD_CONTRACT;
        return (SIZE_T)pModule;
    }

#ifdef _DEBUG
    bool ThreadHoldsLock();
#endif

public:

#ifndef DACCESS_COMPILE

    DebuggerModuleTable();
    virtual ~DebuggerModuleTable();

    void AddModule(DebuggerModule *module);

    void RemoveModule(Module* module, AppDomain *pAppDomain);


    void Clear();

    //
    // RemoveModules removes any module loaded into the given appdomain from the hash.  This is used when we send an
    // ExitAppdomain event to ensure that there are no leftover modules in the hash. This can happen when we have shared
    // modules that aren't properly accounted for in the CLR. We miss sending UnloadModule events for those modules, so
    // we clean them up with this method.
    //
    void RemoveModules(AppDomain *pAppDomain);
#endif // #ifndef DACCESS_COMPILE

    DebuggerModule *GetModule(Module* module);

    // We should never look for a NULL Module *
    DebuggerModule *GetModule(Module* module, AppDomain* pAppDomain);
    DebuggerModule *GetFirstModule(HASHFIND *info);
    DebuggerModule *GetNextModule(HASHFIND *info);
};

// struct DebuggerMethodInfoKey:   Key for each of the method info hash table entries.
// Module * m_pModule:  This and m_token make up the key
// mdMethodDef m_token:  This and m_pModule make up the key
//
// Note: This is used for hashing, so the structure must be totally blittable.
typedef DPTR(struct DebuggerMethodInfoKey) PTR_DebuggerMethodInfoKey;
struct DebuggerMethodInfoKey
{
    PTR_Module          pModule;
    mdMethodDef         token;
} ;

// struct DebuggerMethodInfoEntry:  Entry for the JIT info hash table.
// FREEHASHENTRY entry:   Needed for use by the hash table
// DebuggerMethodInfo * ji:   The actual DebuggerMethodInfo to
//          hash.  Note that DMI's will be hashed by MethodDesc.
typedef DPTR(struct DebuggerMethodInfoEntry) PTR_DebuggerMethodInfoEntry;
struct DebuggerMethodInfoEntry
{
    FREEHASHENTRY          entry;
    DebuggerMethodInfoKey  key;
    SIZE_T                 nVersion;
    SIZE_T                 nVersionLastRemapped;
    PTR_DebuggerMethodInfo   mi;

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif
};

// class DebuggerMethodInfoTable:   Hash table to hold all the non-JIT related
// info for each method we see.  The JIT infos live in a seperate table
// keyed by MethodDescs - there may be multiple
// JITted realizations of each MethodDef, e.g. under different generic
// assumptions.  Hangs off of the Debugger object.
// INVARIANT: There is only one DebuggerMethodInfo per method
// in the table. Note that DMI's will be hashed by MethodDesc.
//
class DebuggerMethodInfoTable : private CHashTableAndData<CNewZeroData>
{
    VPTR_BASE_CONCRETE_VTABLE_CLASS(DebuggerMethodInfoTable);

  public:
    virtual ~DebuggerMethodInfoTable() = default;

  private:
    BOOL Cmp(SIZE_T k1, const HASHENTRY * pc2)
    {
        LIMITED_METHOD_DAC_CONTRACT;

        // This is the inverse of the KEY() function.
        DebuggerMethodInfoKey * pDjik = reinterpret_cast<DebuggerMethodInfoKey *>(k1);

        DebuggerMethodInfoEntry * pDjie = dac_cast<PTR_DebuggerMethodInfoEntry>(const_cast<HASHENTRY *>(pc2));

        return (pDjik->pModule != pDjie->key.pModule) ||
               (pDjik->token != pDjie->key.token);
    }

    ULONG HASH(DebuggerMethodInfoKey* pDjik)
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return HashPtr( pDjik->token, pDjik->pModule );
    }

    SIZE_T KEY(DebuggerMethodInfoKey * pDjik)
    {
        // This is casting a host pointer to a SIZE_T. So that key is restricted to the host address space.
        // This key is just passed to Cmp(), which will cast it back to a DebuggerMethodInfoKey*.
        LIMITED_METHOD_DAC_CONTRACT;
        return (SIZE_T)pDjik;
    }

//#define _DEBUG_DMI_TABLE

#ifdef _DEBUG_DMI_TABLE
public:
    ULONG CheckDmiTable();

#define CHECK_DMI_TABLE (CheckDmiTable())
#define CHECK_DMI_TABLE_DEBUGGER (m_pMethodInfos->CheckDmiTable())

#else

#define CHECK_DMI_TABLE
#define CHECK_DMI_TABLE_DEBUGGER

#endif // _DEBUG_DMI_TABLE

  public:

#ifndef DACCESS_COMPILE

    DebuggerMethodInfoTable();

    HRESULT AddMethodInfo(Module *pModule,
                       mdMethodDef token,
                       DebuggerMethodInfo *mi);

    HRESULT OverwriteMethodInfo(Module *pModule,
                             mdMethodDef token,
                             DebuggerMethodInfo *mi,
                             BOOL fOnlyIfNull);

    // pModule is being unloaded - remove any entries that belong to it.  Why?
    // (a) Correctness: the module can be reloaded at the same address,
    //      which will cause accidental matches with our hashtable (indexed by
    //      {Module*,mdMethodDef}
    // (b) Perf: don't waste the memory!
    void ClearMethodsOfModule(Module *pModule);
    void DeleteEntryDMI(DebuggerMethodInfoEntry *entry);

#endif // #ifndef DACCESS_COMPILE

    DebuggerMethodInfo *GetMethodInfo(Module *pModule, mdMethodDef token);
    DebuggerMethodInfo *GetFirstMethodInfo(HASHFIND *info);
    DebuggerMethodInfo *GetNextMethodInfo(HASHFIND *info);

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif
};

class DebuggerEvalBreakpointInfoSegment
{
public:
    // DebuggerEvalBreakpointInfoSegment contains just the breakpoint
    // instruction and a pointer to the associated DebuggerEval. It makes
    // it easy to go from the instruction to the corresponding DebuggerEval
    // object. It has been separated from the rest of the DebuggerEval
    // because it needs to be in a section of memory that's executable,
    // while the rest of DebuggerEval does not. By having it separate, we
    // don't need to have the DebuggerEval contents in executable memory.
    BYTE          m_breakpointInstruction[CORDbg_BREAK_INSTRUCTION_SIZE];
    DebuggerEval *m_associatedDebuggerEval;

    DebuggerEvalBreakpointInfoSegment(DebuggerEval* dbgEval)
    : m_associatedDebuggerEval(dbgEval)
    {
        ASSERT(dbgEval != NULL);
    }
};

/* ------------------------------------------------------------------------ *
 * DebuggerEval class
 *
 * Note that arguments get passsed in a block allocated when
 * the func-eval is set up.  The setup phase passes the total count of arguments.
 *
 * In some situations type arguments must also be passed, e.g.
 * when performing a "newarr" operation or calling a generic function with a
 * "funceval". In the setup phase we pass a count of the number of
 * nodes in the "flattened" type expressions for the type arguments, if any.
 * e.g. for calls to non-generic code this is 0.
 *    - for "newobj List<int>" this is 1: there is one type argument "int".
 *    - for "newobj Dict<string,int>" this is 2: there are two
 *      type arguments "string" and "int".
 *    - for "newobj Dict<string,List<int>>" this is 3: there are two
        type arguments but the second contains two nodes (one for List and one for int).
 * The type argument will get placed in the allocated argument block,
 * the order being determined by the order they occur in the tree, i.e.
 * left-to-right, top-to-bottom in the type expressions tree, e.g. for
 * type arguments <string,List<int>> you get string followed by List followed by int.
 * ------------------------------------------------------------------------ */

class DebuggerEval
{
public:

    //
    // Used as a bit field.
    //
    enum FUNC_EVAL_ABORT_TYPE
    {
        FE_ABORT_NONE = 0,
        FE_ABORT_NORMAL = 1,
        FE_ABORT_RUDE = 2
    };

    T_CONTEXT                          m_context;
    Thread                            *m_thread;
    DebuggerIPCE_FuncEvalType          m_evalType;
    mdMethodDef                        m_methodToken;
    mdTypeDef                          m_classToken;
    PTR_DebuggerModule                 m_debuggerModule;     // Only valid if AD is still around
    RSPTR_CORDBEVAL                    m_funcEvalKey;
    bool                               m_successful;        // Did the eval complete successfully
    Debugger::AreValueTypesBoxed       m_retValueBoxing;        // Is the return value boxed?
    unsigned int                       m_argCount;
    unsigned int                       m_genericArgsCount;
    unsigned int                       m_genericArgsNodeCount;
    SIZE_T                             m_stringSize;
    BYTE                              *m_argData;
    MethodDesc                        *m_md;
    PCODE                              m_targetCodeAddr;
    ARG_SLOT                           m_result[NUMBER_RETURNVALUE_SLOTS];
    TypeHandle                         m_resultType;
    SIZE_T                             m_arrayRank;
    FUNC_EVAL_ABORT_TYPE               m_aborting;          // Has an abort been requested, and what type.
    bool                               m_aborted;           // Was this eval aborted
    bool                               m_completed;          // Is the eval complete - successfully or by aborting
    bool                               m_evalDuringException;
    bool                               m_rethrowAbortException;
    Thread::ThreadAbortRequester       m_requester;         // For aborts, what kind?
    VMPTR_OBJECTHANDLE                 m_vmObjectHandle;
    TypeHandle                         m_ownerTypeHandle;
    DebuggerEvalBreakpointInfoSegment* m_bpInfoSegment;

    DebuggerEval(T_CONTEXT * pContext, DebuggerIPCE_FuncEvalInfo * pEvalInfo, bool fInException);

    // This constructor is only used when setting up an eval to re-abort a thread.
    DebuggerEval(T_CONTEXT * pContext, Thread * pThread, Thread::ThreadAbortRequester requester);

    bool Init()
    {
        _ASSERTE(DbgIsExecutable(&m_bpInfoSegment->m_breakpointInstruction, sizeof(m_bpInfoSegment->m_breakpointInstruction)));
        return true;
    }

    // The m_argData buffer holds both the type arg data (for generics) and the main argument data.
    //
    // For DB_IPCE_FET_NEW_STRING it holds the data specifying the string to create.
    DebuggerIPCE_TypeArgData *GetTypeArgData()
    {
        LIMITED_METHOD_CONTRACT;
        return (DebuggerIPCE_TypeArgData *) (m_argData);
    }

    DebuggerIPCE_FuncEvalArgData *GetArgData()
    {
        LIMITED_METHOD_CONTRACT;
        return (DebuggerIPCE_FuncEvalArgData*) (m_argData + m_genericArgsNodeCount * sizeof(DebuggerIPCE_TypeArgData));
    }

    WCHAR *GetNewStringArgData()
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(m_evalType == DB_IPCE_FET_NEW_STRING);
        return (WCHAR*)m_argData;
    }

    ~DebuggerEval()
    {
        WRAPPER_NO_CONTRACT;

        // Clean up any temporary buffers used to send the argument type information.  These were allocated
        // in respnse to a GET_BUFFER message
        DebuggerIPCE_FuncEvalArgData *argData = GetArgData();
        for (unsigned int i = 0; i < m_argCount; i++)
        {
            if (argData[i].fullArgType != NULL)
            {
                _ASSERTE(g_pDebugger != NULL);
                g_pDebugger->ReleaseRemoteBuffer((BYTE*)argData[i].fullArgType, true);
            }
        }

        // Clean up the array of argument information.  This was allocated as part of Func Eval setup.
        if (m_argData)
        {
            DeleteInteropSafe(m_argData);
        }

#ifdef _DEBUG
        // Set flags to strategic values in case we access deleted memory.
        m_completed = false;
        m_rethrowAbortException = true;
#endif
    }
};

/* ------------------------------------------------------------------------ *
 * New/delete overrides to use the debugger's private heap
 * ------------------------------------------------------------------------ */

class InteropSafe {};
extern InteropSafe interopsafe;

class InteropSafeExecutable {};
extern InteropSafeExecutable interopsafeEXEC;

#ifndef DACCESS_COMPILE
inline void * __cdecl operator new(size_t n, const InteropSafe&)
{
    CONTRACTL
    {
        THROWS; // throw on OOM
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(g_pDebugger != NULL);
    void *result = g_pDebugger->GetInteropSafeHeap()->Alloc((DWORD)n);
    if (result == NULL) {
        ThrowOutOfMemory();
    }
    return result;
}

inline void * __cdecl operator new[](size_t n, const InteropSafe&)
{
    CONTRACTL
    {
        THROWS; // throw on OOM
        GC_NOTRIGGER;
    }
    CONTRACTL_END;
    _ASSERTE(g_pDebugger != NULL);
    void *result = g_pDebugger->GetInteropSafeHeap()->Alloc((DWORD)n);
    if (result == NULL) {
        ThrowOutOfMemory();
    }
    return result;
}

inline void * __cdecl operator new(size_t n, const InteropSafe&, const NoThrow&) throw()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(g_pDebugger != NULL);
    DebuggerHeap * pHeap = g_pDebugger->GetInteropSafeHeap_NoThrow();
    if (pHeap == NULL)
    {
        return NULL;
    }
    void *result = pHeap->Alloc((DWORD)n);
    return result;
}

inline void * __cdecl operator new[](size_t n, const InteropSafe&, const NoThrow&) throw()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(g_pDebugger != NULL);
    DebuggerHeap * pHeap = g_pDebugger->GetInteropSafeHeap_NoThrow();
    if (pHeap == NULL)
    {
        return NULL;
    }
    void *result = pHeap->Alloc((DWORD)n);
    return result;
}

// Note: there is no C++ syntax for manually invoking this, but if a constructor throws an exception I understand that
// this delete operator will be invoked automatically to destroy the object.
inline void __cdecl operator delete(void *p, const InteropSafe&)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (p != NULL)
    {
        _ASSERTE(g_pDebugger != NULL);
        DebuggerHeap * pHeap = g_pDebugger->GetInteropSafeHeap_NoThrow();
        _ASSERTE(pHeap != NULL); // should have had heap around if we're deleting
        pHeap->Free(p);
    }
}

// Note: there is no C++ syntax for manually invoking this, but if a constructor throws an exception I understand that
// this delete operator will be invoked automatically to destroy the object.
inline void __cdecl operator delete[](void *p, const InteropSafe&)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (p != NULL)
    {
        _ASSERTE(g_pDebugger != NULL);
        DebuggerHeap * pHeap = g_pDebugger->GetInteropSafeHeap_NoThrow();
        _ASSERTE(pHeap != NULL); // should have had heap around if we're deleting

        pHeap->Free(p);
    }
}

//
// Interop safe delete to match the interop safe new's above. There is no C++ syntax for actually invoking those interop
// safe delete operators above, so we use this method to accomplish the same thing.
//
template<class T> void DeleteInteropSafe(T *p)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // Don't stop a thread that may hold the Interop-safe heap lock.
    // It may be in preemptive, but it's still "inside" the CLR and so inside the "Can't-Stop-Region"
    CantStopHolder hHolder;

    if (p != NULL)
    {
        p->~T();

        _ASSERTE(g_pDebugger != NULL);
        DebuggerHeap * pHeap = g_pDebugger->GetInteropSafeHeap_NoThrow();
        _ASSERTE(pHeap != NULL); // should have had heap around if we're deleting

        pHeap->Free(p);
    }
}

inline void * __cdecl operator new(size_t n, const InteropSafeExecutable&)
{
    CONTRACTL
    {
        THROWS; // throw on OOM
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(g_pDebugger != NULL);
    void *result = g_pDebugger->GetInteropSafeExecutableHeap()->Alloc((DWORD)n);
    if (result == NULL) {
        ThrowOutOfMemory();
    }
    return result;
}

inline void * __cdecl operator new(size_t n, const InteropSafeExecutable&, const NoThrow&) throw()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    _ASSERTE(g_pDebugger != NULL);
    DebuggerHeap * pHeap = g_pDebugger->GetInteropSafeExecutableHeap_NoThrow();
    if (pHeap == NULL)
    {
        return NULL;
    }
    void *result = pHeap->Alloc((DWORD)n);
    return result;
}

// Note: there is no C++ syntax for manually invoking this, but if a constructor throws an exception I understand that
// this delete operator will be invoked automatically to destroy the object.
inline void __cdecl operator delete(void *p, const InteropSafeExecutable&)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    if (p != NULL)
    {
        _ASSERTE(g_pDebugger != NULL);
        DebuggerHeap * pHeap = g_pDebugger->GetInteropSafeExecutableHeap_NoThrow();
        _ASSERTE(pHeap != NULL); // should have had heap around if we're deleting
        pHeap->Free(p);
    }
}

//
// Interop safe delete to match the interop safe new's above. There is no C++ syntax for actually invoking those interop
// safe delete operators above, so we use this method to accomplish the same thing.
//
template<class T> void DeleteInteropSafeExecutable(T *p)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    // Don't stop a thread that may hold the Interop-safe heap lock.
    // It may be in preemptive, but it's still "inside" the CLR and so inside the "Can't-Stop-Region"
    CantStopHolder hHolder;

    if (p != NULL)
    {
        p->~T();

        _ASSERTE(g_pDebugger != NULL);
        DebuggerHeap * pHeap = g_pDebugger->GetInteropSafeExecutableHeap_NoThrow();
        _ASSERTE(pHeap != NULL); // should have had heap around if we're deleting

        pHeap->Free(p);
    }
}
#endif // DACCESS_COMPILE


#if _DEBUG
#define DBG_RUNTIME_MAX ((DB_IPCE_RUNTIME_LAST&0xff)+1)
#define DBG_DEBUGGER_MAX ((DB_IPCE_DEBUGGER_LAST&0xff)+1)

#define DbgLog(event) DbgLogHelper(event)
void DbgLogHelper(DebuggerIPCEventType event);
#else
#define DbgLog(event)
#endif // _DEBUG

//-----------------------------------------------------------------------------
// Helpers for cleanup
// These are various utility functions, mainly where we factor out code.
//-----------------------------------------------------------------------------
void GetPidDecoratedName(__out_ecount(cBufSizeInChars) WCHAR * pBuf,
                         int cBufSizeInChars,
                         const WCHAR * pPrefix);

// Specify type of Win32 event
enum EEventResetType {
    kManualResetEvent = TRUE,
    kAutoResetEvent = FALSE
};

HANDLE CreateWin32EventOrThrow(
    LPSECURITY_ATTRIBUTES lpEventAttributes,
    EEventResetType eType,
    BOOL bInitialState
);

HANDLE OpenWin32EventOrThrow(
    DWORD dwDesiredAccess,
    BOOL bInheritHandle,
    LPCWSTR lpName
);

#define SENDIPCEVENT_RAW_BEGIN_EX(pDbgLockHolder, gcxStmt)                            \
  {                                                                                   \
    ThreadStoreLockHolderWithSuspendReason tsld(ThreadSuspend::SUSPEND_FOR_DEBUGGER); \
    Debugger::DebuggerLockHolder *__pDbgLockHolder = pDbgLockHolder;                  \
    gcxStmt;                                                                          \
    g_pDebugger->LockForEventSending(__pDbgLockHolder);

#define SENDIPCEVENT_RAW_END_EX                                                       \
    g_pDebugger->UnlockFromEventSending(__pDbgLockHolder);                            \
  }

#define SENDIPCEVENT_RAW_BEGIN(pDbgLockHolder)                  \
    SENDIPCEVENT_RAW_BEGIN_EX(pDbgLockHolder, GCX_PREEMP_EEINTERFACE_TOGGLE_COND(CORDebuggerAttached()))

#define SENDIPCEVENT_RAW_END SENDIPCEVENT_RAW_END_EX

// Suspend-aware SENDIPCEVENT macros:
// Check whether __thread has been suspended by the debugger via SetDebugState().
// If this thread has been suspended, it shouldn't send any event to the RS because the
// debugger may not be expecting it.  Instead, just leave the lock and retry.
// When we leave, we'll enter coop mode first and get suspended if a suspension is in progress.
// Afterwards, we'll transition back into preemptive mode, and we'll block because this thread
// has been suspended by the debugger (see code:Thread::RareEnablePreemptiveGC).
#define SENDIPCEVENT_BEGIN_EX(pDebugger, thread, gcxStmt)                                 \
  {                                                                                       \
    FireEtwDebugIPCEventStart();                                                          \
    bool __fRetry = true;                                                                 \
    do                                                                                    \
    {                                                                                     \
      {                                                                                   \
        Debugger::DebuggerLockHolder __dbgLockHolder(pDebugger, FALSE);                   \
        Debugger::DebuggerLockHolder *__pDbgLockHolder = &__dbgLockHolder;                \
        gcxStmt;                                                                          \
        ThreadStoreLockHolderWithSuspendReason tsld(ThreadSuspend::SUSPEND_FOR_DEBUGGER); \
        g_pDebugger->LockForEventSending(__pDbgLockHolder);                               \
        /* Check if the thread has been suspended by the debugger via SetDebugState(). */ \
        if (thread != NULL && thread->HasThreadStateNC(Thread::TSNC_DebuggerUserSuspend)) \
        {                                                                                 \
            /* Just leave the lock and retry (see comment above for explanation */        \
        }                                                                                 \
        else                                                                              \
        {                                                                                 \
            __fRetry = false;                                                             \

#define SENDIPCEVENT_END_EX                                     \
            ;                                                   \
        }                                                       \
        g_pDebugger->UnlockFromEventSending(__pDbgLockHolder);  \
      } /* ~gcxStmt & ~DebuggerLockHolder & ~tsld */            \
    } while (__fRetry);                                         \
    FireEtwDebugIPCEventEnd();                                  \
  }


// The typical SENDIPCEVENT - toggles the GC mode...
#define SENDIPCEVENT_BEGIN(pDebugger, thread) \
    SENDIPCEVENT_BEGIN_EX(pDebugger, thread, GCX_PREEMP_EEINTERFACE_TOGGLE_IFTHREAD_COND(CORDebuggerAttached()))

// Convenience macro to match SENDIPCEVENT_BEGIN
#define SENDIPCEVENT_END SENDIPCEVENT_END_EX


// Use this if you need to access the DebuggerLockHolder set up by SENDIPCEVENT_BEGIN.
// This is valid only between the SENDIPCEVENT_BEGIN / SENDIPCEVENT_END macros
#define SENDIPCEVENT_PtrDbgLockHolder __pDbgLockHolder


// Common contract for sending events.
// Used inbetween SENDIPCEVENT_BEGIN & _END.
//
// Can't GC trigger b/c if we're sycning we'll deadlock:
// - We'll block at the GC toggle (b/c we're syncing).
// - But we're holding the LockForEventSending "lock", so we'll block the helper trying to send a
//   SuspendComplete
//
// @todo- we could also assert that:
// - m_tidLockedForEventSending = GetCurrentThreadId();
#define SENDEVENT_CONTRACT_ITEMS \
    GC_NOTRIGGER; \
    MODE_PREEMPTIVE; \
    PRECONDITION(g_pDebugger->ThreadHoldsLock()); \
    PRECONDITION(!g_pDebugger->IsStopped()); \


//-----------------------------------------------------------------------------
// Sample usage for sending IPC _Notification_ events.
// This is different then SendIPCReply (which is used to reply to events
// initiated by the RS).
//-----------------------------------------------------------------------------

// Thread *pThread = g_pEEInterface->GetThread();
// SENDIPCEVENT_BEGIN(g_pDebugger, pThread); // or use "this" if inside a Debugger method
// _ASSERTE(ThreadHoldsLock()); // we now hold the debugger lock.
// // debugger may have detached while we were blocked above.
//
// if (CORDebuggerAttached()) {
//      // Send as many IPC events as we wish.
//      SendIPCEvent(....);
//      SendIPCEvent(....);
//      SendIPCEvent(....);
//
//      if (we sent an event) {
//          TrapAllRuntimeThreads();
//      }
// }
//
// // We block here while the debugger responds to the event.
// SENDIPCEVENT_END;

// Or if we just want to send a single IPC event and block, we can do this:
//
//  < ... Init IPC Event ...>
// SendSimpleIPCEventAndBlock(); <-- this will block
//
// Note we don't have to call SENDIPCEVENT_BEGIN / END in this case.

// @todo - further potential cleanup to the IPC sending:
// - Make SendIPCEvent + TrapAllRuntimeThreads check for CORDebuggerAttached() so that we
// can always call them after SENDIPCEVENT_BEGIN
// - Assert that SendIPCEVent is only called inbetween a Begin/End pair
// - count if we actually send any IPCEvents inbetween a Begin/End pair, and then have
// SendIPCEvent_END call TrapAllRuntimeThreads automatically for us.


// Include all of the inline stuff now.
#include "debugger.inl"


//
//
//
//  The below contract defines should only be used (A) if they apply, and (B) they are the LEAST
// definitive for the function you are contracting.  The below defines represent the baseline contract
// for each case.
//
// e.g. If a function FOO() throws, always, you should use THROWS, not any of the below.
//
//
//
#if _DEBUG

#define MAY_DO_HELPER_THREAD_DUTY_THROWS_CONTRACT \
      if ((m_pRCThread == NULL) || !m_pRCThread->IsRCThreadReady()) { THROWS; } else { NOTHROW; }

#define MAY_DO_HELPER_THREAD_DUTY_GC_TRIGGERS_CONTRACT \
      if ((m_pRCThread == NULL) || !m_pRCThread->IsRCThreadReady() || (GetThread() != NULL)) { GC_TRIGGERS; } else { GC_NOTRIGGER; }

#define GC_TRIGGERS_FROM_GETJITINFO if (GetThreadNULLOk() != NULL) { GC_TRIGGERS; } else { GC_NOTRIGGER; }

//
// The DebuggerDataLock lock is UNSAFE_ANYMODE, which means that we cannot
// take a GC while someone is holding it.  Unfortunately this means that
// we cannot contract for a "possible" GC trigger statically, and must
// rely on runtime coverage to find any code path that may cause a GC.
//
#define CALLED_IN_DEBUGGERDATALOCK_HOLDER_SCOPE_MAY_GC_TRIGGERS_CONTRACT WRAPPER(GC_TRIGGERS)

#else

#define MAY_DO_HELPER_THREAD_DUTY_THROWS_CONTRACT
#define MAY_DO_HELPER_THREAD_DUTY_GC_TRIGGERS_CONTRACT
#define CALLED_IN_DEBUGGERDATALOCK_HOLDER_SCOPE_MAY_GC_TRIGGERS_CONTRACT

#define GC_TRIGGERS_FROM_GETJITINFO

#endif

// Returns true if the specified IL offset has a special meaning (eg. prolog, etc.)
bool DbgIsSpecialILOffset(DWORD offset);

#if !defined(TARGET_X86)
void FixupDispatcherContext(T_DISPATCHER_CONTEXT* pDispatcherContext, T_CONTEXT* pContext, T_CONTEXT* pOriginalContext, PEXCEPTION_ROUTINE pUnwindPersonalityRoutine = NULL);
#endif

#endif /* DEBUGGER_H_ */
