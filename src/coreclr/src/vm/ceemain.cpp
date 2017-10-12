// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: CEEMAIN.CPP
// ===========================================================================
//

// 
//
// The CLR code base uses a hyperlink feature of the HyperAddin plugin for Visual Studio. If you don't see
// 'HyperAddin' in your Visual Studio menu bar you don't have this support. To get it type 
// 
//     \\clrmain\tools\installCLRAddins
//     
//  After installing HyperAddin, your first run of VS should be as an administrator so HyperAddin can update 
//  some registry information.
//     
//  At this point the code: prefixes become hyperlinks in Visual Studio and life is good. See
//  http://mswikis/clr/dev/Pages/CLR%20Team%20Commenting.aspx for more information
//  
//  There is a bug associated with Visual Studio where it does not recognise the hyperlink if there is a ::
//  preceeding it on the same line. Since C++ uses :: as a namespace separator, this can often mean that the
//  second hyperlink on a line does not work. To work around this it is better to use '.' instead of :: as
//  the namespace separators in code: hyperlinks.
// 
// #StartHere
// #TableOfContents The .NET Runtime Table of contents
// 
// This comment is mean to be a nexus that allows you to jump quickly to various interesting parts of the
// runtime.
// 
// You can refer to product studio bugs using urls like the following
//     * http://bugcheck/bugs/DevDivBugs/2320.asp
//     * http://bugcheck/bugs/VSWhidbey/601210.asp
//     
//  Dev10 Bugs can be added with URLs like the following (for Dev10 bug 671409)
//     * http://tkbgitvstfat01:8090/wi.aspx?id=671409
// 
//*************************************************************************************************
//
// * Introduction to the runtime file:../../Documentation/botr/botr-faq.md
//
// #MajorDataStructures. The major data structures associated with the runtime are
//     * code:Thread (see file:threads.h#ThreadClass) - the additional thread state the runtime needs.
//     * code:AppDomain - The managed version of a process
//     * code:Assembly - The unit of deployment and versioning (may be several DLLs but often is only one).
//     * code:Module - represents a Module (DLL or EXE).
//     * code:MethodTable - represents the 'hot' part of a type (needed during normal execution)
//     * code:EEClass - represents the 'cold' part of a type (used during compilation, interop, ...)
//     * code:MethodDesc - represents a Method
//     * code:FieldDesc - represents a Field.
//     * code:Object - represents a object on the GC heap allocated with code:Alloc 
// 
// * ECMA specifications
//     * Partition I Concepts
//         http://download.microsoft.com/download/D/C/1/DC1B219F-3B11-4A05-9DA3-2D0F98B20917/Partition%20I%20Architecture.doc
//     * Partition II Meta Data
//         http://download.microsoft.com/download/D/C/1/DC1B219F-3B11-4A05-9DA3-2D0F98B20917/Partition%20II%20Metadata.doc
//     * Partition III IL
//         http://download.microsoft.com/download/D/C/1/DC1B219F-3B11-4A05-9DA3-2D0F98B20917/Partition%20III%20CIL.doc
//  
//  * Serge Liden (worked on the CLR and owned ILASM / ILDASM for a long time wrote a good book on IL
//     * Expert .NET 2.0 IL Assembler  http://www.amazon.com/Expert-NET-2-0-IL-Assembler/dp/1590596463
// 
// * This is also a pretty nice overview of what the CLR is at
//     http://msdn2.microsoft.com/en-us/netframework/aa497266.aspx
// 
// * code:EEStartup - This routine must be called before any interesting runtime services are used. It is
//     invoked as part of mscorwks's DllMain logic.
// * code:#EEShutDown - Code called before we shut down the EE.
//     
// * file:..\inc\corhdr.h#ManagedHeader - From a data structure point of view, this is the entry point into
//     the runtime. This is how all other data in the EXE are found.
//  
// * code:ICorJitCompiler#EEToJitInterface - This is the interface from the the EE to the Just in time (JIT)
//     compiler. The interface to the JIT is relatively simple (compileMethod), however the EE provides a
//     rich set of callbacks so the JIT can get all the information it needs. See also
//     file:../../Documentation/botr/ryujit-overview.md for general information on the JIT.
// 
// * code:VirtualCallStubManager - This is the main class that implements interface dispatch
// 
// * Precode - Every method needs entry point for other code to call even if that native code does not
//     actually exist yet. To support this methods can have code:Precode that is an entry point that exists
//     and will call the JIT compiler if the code does not yet exist.
//     
//  * NGEN - NGen stands for Native code GENeration and it is the runtime way of precomiling IL and IL
//      Meta-data into native code and runtime data structures. At compilation time the most
//      fundamental data structures is the code:ZapNode which represents something that needs to go into the
//      NGEN image.
//      
//   * What is cooperative / preemtive mode ? file:threads.h#CooperativeMode and
//       file:threads.h#SuspendingTheRuntime and file:../../Documentation/botr/threading.md
//   * Garbage collection - file:gc.cpp#Overview and file:../../Documentation/botr/garbage-collection.md
//   * code:AppDomain - The managed version of a process.
//   * Calling Into the runtime (FCALLs QCalls) file:../../Documentation/botr/mscorlib.md
//   * Exceptions - file:../../Documentation/botr/exceptions.md. The most important routine to start
//       with is code:COMPlusFrameHandler which is the routine that we hook up to get called when an unmanaged
//       exception happens.
//   * Assembly Loading file:../../Documentation/botr/type-loader.md
//   * Profiling file:../../Documentation/botr/profiling.md and file:../../Documentation/botr/profilability.md
//   * FCALLS QCALLS (calling into the runtime from managed code)
//       file:../../Documentation/botr/mscorlib.md
//   * Event Tracing for Windows
//     * file:../inc/eventtrace.h#EventTracing -
//     * This is the main file dealing with event tracing in CLR
//     * The implementation of this class is available in file:eventtrace.cpp
//     * file:../inc/eventtrace.h#CEtwTracer - This is the main class dealing with event tracing in CLR.
//         Follow the link for more information on how this feature has been implemented
//     * http://mswikis/clr/dev/Pages/CLR%20ETW%20Events%20Wiki.aspx - Follow the link for more information on how to
//         use this instrumentation feature.

// ----------------------------------------------------------------------------------------------------
// Features in the runtime that have been given hyperlinks
// 
// * code:Nullable#NullableFeature - the Nullable<T> type has special runtime semantics associated with
//     boxing this describes this feature.

#include "common.h"

#include "vars.hpp"
#include "log.h"
#include "ceemain.h"
#include "clsload.hpp"
#include "object.h"
#include "hash.h"
#include "ecall.h"
#include "ceemain.h"
#include "dllimport.h"
#include "syncblk.h"
#include "eeconfig.h"
#include "stublink.h"
#include "method.hpp"
#include "codeman.h"
#include "frames.h"
#include "threads.h"
#include "stackwalk.h"
#include "gcheaputilities.h"
#include "interoputil.h"
#include "fieldmarshaler.h"
#include "dbginterface.h"
#include "eedbginterfaceimpl.h"
#include "debugdebugger.h"
#include "cordbpriv.h"
#include "comdelegate.h"
#include "appdomain.hpp"
#include "perfcounters.h"
#ifdef FEATURE_IPCMAN
#include "ipcmanagerinterface.h"
#endif // FEATURE_IPCMAN
#include "eventtrace.h"
#include "corhost.h"
#include "binder.h"
#include "olevariant.h"
#include "comcallablewrapper.h"
#include "apithreadstress.h"
#include "ipcfunccall.h"
#include "perflog.h"
#include "../dlls/mscorrc/resource.h"
#ifdef FEATURE_USE_LCID
#include "nlsinfo.h"
#endif 
#include "util.hpp"
#include "shimload.h"
#include "comthreadpool.h"
#include "stackprobe.h"
#include "posterror.h"
#include "virtualcallstub.h"
#include "strongnameinternal.h"
#include "syncclean.hpp"
#include "typeparse.h"
#include "debuginfostore.h"
#include "mdaassistants.h"
#include "eemessagebox.h"
#include "finalizerthread.h"
#include "threadsuspend.h"
#include "disassembler.h"
#include "gcenv.ee.h"

#ifndef FEATURE_PAL
#include "dwreport.h"
#endif // !FEATURE_PAL

#include "stringarraylist.h"
#include "stubhelpers.h"
#include "perfdefaults.h"

#ifdef FEATURE_STACK_SAMPLING
#include "stacksampler.h"
#endif

#include <shlwapi.h>

#include "bbsweep.h"


#ifdef FEATURE_COMINTEROP
#include "runtimecallablewrapper.h"
#include "notifyexternals.h"
#include "mngstdinterfaces.h"
#include "rcwwalker.h"
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
#include "olecontexthelpers.h"
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT

#ifdef PROFILING_SUPPORTED
#include "proftoeeinterfaceimpl.h"
#include "profilinghelper.h"
#endif // PROFILING_SUPPORTED

#include "newapis.h"

#ifdef FEATURE_COMINTEROP
#include "synchronizationcontextnative.h"       // For SynchronizationContextNative::Cleanup
#endif

#ifdef FEATURE_INTERPRETER
#include "interpreter.h"
#endif // FEATURE_INTERPRETER

#include "../binder/inc/coreclrbindercommon.h"


#ifdef FEATURE_PERFMAP
#include "perfmap.h"
#endif

#include "eventpipe.h"

#ifndef FEATURE_PAL
// Included for referencing __security_cookie
#include "process.h"
#endif // !FEATURE_PAL

#ifdef FEATURE_GDBJIT
#include "gdbjit.h"
#endif // FEATURE_GDBJIT

#ifdef FEATURE_IPCMAN
static HRESULT InitializeIPCManager(void);
static void PublishIPCManager(void);
static void TerminateIPCManager(void);
#endif // FEATURE_IPCMAN

#ifndef CROSSGEN_COMPILE
static int GetThreadUICultureId(__out LocaleIDValue* pLocale);  // TODO: This shouldn't use the LCID.  We should rely on name instead

static HRESULT GetThreadUICultureNames(__inout StringArrayList* pCultureNames);
#endif // !CROSSGEN_COMPILE

HRESULT EEStartup(COINITIEE fFlags);


BOOL STDMETHODCALLTYPE ExecuteEXE(HMODULE hMod);
BOOL STDMETHODCALLTYPE ExecuteEXE(__in LPWSTR pImageNameIn);

#ifndef CROSSGEN_COMPILE
static void InitializeGarbageCollector();

#ifdef DEBUGGING_SUPPORTED
static void InitializeDebugger(void);
static void TerminateDebugger(void);
extern "C" HRESULT __cdecl CorDBGetInterface(DebugInterface** rcInterface);
#endif // DEBUGGING_SUPPORTED
#endif // !CROSSGEN_COMPILE




// Remember how the last startup of EE went.
HRESULT g_EEStartupStatus = S_OK;

// Flag indicating if the EE has been started.  This is set prior to initializing the default AppDomain, and so does not indicate that
// the EE is fully able to execute arbitrary managed code.  To ensure the EE is fully started, call EnsureEEStarted rather than just
// checking this flag.
Volatile<BOOL> g_fEEStarted = FALSE;

// Flag indicating if the EE should be suspended on shutdown.
BOOL    g_fSuspendOnShutdown = FALSE;

// Flag indicating if the finalizer thread should be suspended on shutdown.
BOOL    g_fSuspendFinalizerOnShutdown = FALSE;

// Flag indicating if the EE was started up by COM.
extern BOOL g_fEEComActivatedStartup;

// flag indicating that EE was not started up by IJW, Hosted, COM or my managed exe. 
extern BOOL g_fEEOtherStartup;

// The OS thread ID of the thread currently performing EE startup, or 0 if there is no such thread.
DWORD   g_dwStartupThreadId = 0;

// Event to synchronize EE shutdown.
static CLREvent * g_pEEShutDownEvent;

static DangerousNonHostedSpinLock g_EEStartupLock;

HRESULT InitializeEE(COINITIEE flags)
{
    WRAPPER_NO_CONTRACT;
#ifdef FEATURE_EVENT_TRACE
    if(!g_fEEComActivatedStartup)
        g_fEEOtherStartup = TRUE;
#endif // FEATURE_EVENT_TRACE
    return EnsureEEStarted(flags);
}

// ---------------------------------------------------------------------------
// %%Function: EnsureEEStarted()
//
// Description: Ensure the CLR is started.
// ---------------------------------------------------------------------------
HRESULT EnsureEEStarted(COINITIEE flags)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        ENTRY_POINT;
    }
    CONTRACTL_END;

    if (g_fEEShutDown)
        return E_FAIL;

    HRESULT hr = E_FAIL;

    // On non x86 platforms, when we load mscorlib.dll during EEStartup, we will
    // re-enter _CorDllMain with a DLL_PROCESS_ATTACH for mscorlib.dll. We are
    // far enough in startup that this is allowed, however we don't want to
    // re-start the startup code so we need to check to see if startup has
    // been initiated or completed before we call EEStartup. 
    //
    // We do however want to make sure other threads block until the EE is started,
    // which we will do further down.
    if (!g_fEEStarted)
    {
        BEGIN_ENTRYPOINT_NOTHROW;

#if defined(FEATURE_APPX) && !defined(CROSSGEN_COMPILE)
        STARTUP_FLAGS startupFlags = CorHost2::GetStartupFlags();
        // On CoreCLR, the host is in charge of determining whether the process is AppX or not.
        AppX::SetIsAppXProcess(!!(startupFlags & STARTUP_APPX_APP_MODEL));
#endif

#ifndef FEATURE_PAL
        // The sooner we do this, the sooner we avoid probing registry entries.
        // (Perf Optimization for VSWhidbey:113373.)
        REGUTIL::InitOptionalConfigCache();
#endif


        BOOL bStarted=FALSE;

        {
            DangerousNonHostedSpinLockHolder lockHolder(&g_EEStartupLock);

            // Now that we've acquired the lock, check again to make sure we aren't in
            // the process of starting the CLR or that it hasn't already been fully started.
            // At this point, if startup has been inited we don't have anything more to do.
            // And if EEStartup already failed before, we don't do it again.
            if (!g_fEEStarted && !g_fEEInit && SUCCEEDED (g_EEStartupStatus))
            {
                g_dwStartupThreadId = GetCurrentThreadId();

                EEStartup(flags);
                bStarted=g_fEEStarted;
                hr = g_EEStartupStatus;

                g_dwStartupThreadId = 0;
            }
            else
            {
                hr = g_EEStartupStatus;
                if (SUCCEEDED(g_EEStartupStatus))
                {
                    hr = S_FALSE;
                }
            }
        }

#ifdef FEATURE_TESTHOOKS        
        if(bStarted)
            TESTHOOKCALL(RuntimeStarted(RTS_INITIALIZED));
#endif        
        END_ENTRYPOINT_NOTHROW;
    }
    else
    {
        //
        // g_fEEStarted is TRUE, but startup may not be complete since we initialize the default AppDomain
        // *after* setting that flag.  g_fEEStarted is set inside of g_EEStartupLock, and that lock is
        // not released until the EE is really started - so we can quickly check whether the EE is definitely
        // started by checking if that lock is currently held.  If it is not, then we know the other thread
        // (that is actually doing the startup) has finished startup.  If it is currently held, then we
        // need to wait for the other thread to release it, which we do by simply acquiring the lock ourselves.
        //
        // We do not want to do this blocking if we are the thread currently performing EE startup.  So we check
        // that first.
        //
        // Note that the call to IsHeld here is an "acquire" barrier, as is acquiring the lock.  And the release of 
        // the lock by the other thread is a "release" barrier, due to the volatile semantics in the lock's 
        // implementation.  This assures us that once we observe the lock having been released, we are guaranteed 
        // to observe a fully-initialized EE.
        //
        // A note about thread affinity here: we're using the OS thread ID of the current thread without
        // asking the host to pin us to this thread, as we did above.  We can get away with this, because we are
        // only interested in a particular thread ID (that of the "startup" thread) and *that* particular thread
        // is already affinitized by the code above.  So if we get that particular OS thread ID, we know for sure
        // we are really the startup thread.
        //
        if (g_EEStartupLock.IsHeld() && g_dwStartupThreadId != GetCurrentThreadId())
        {
            DangerousNonHostedSpinLockHolder lockHolder(&g_EEStartupLock);
        }

        hr = g_EEStartupStatus;
        if (SUCCEEDED(g_EEStartupStatus))
        {
            hr = S_FALSE;
        }
    }

    return hr;
}


#ifndef CROSSGEN_COMPILE

#ifndef FEATURE_PAL
// This is our Ctrl-C, Ctrl-Break, etc. handler.
static BOOL WINAPI DbgCtrlCHandler(DWORD dwCtrlType)
{
    WRAPPER_NO_CONTRACT;
    STATIC_CONTRACT_SO_TOLERANT;

#if defined(DEBUGGING_SUPPORTED)
    // Note that if a managed-debugger is attached, it's actually attached with the native
    // debugging pipeline and it will get a control-c notifications via native debug events.
    // However, if we let the native debugging pipeline handle the event and send the notification
    // to the debugger, then we break pre-V4 behaviour because we intercept handlers registered
    // in-process.  See Dev10 Bug 846455 for more information. 
    if (CORDebuggerAttached() &&
        (dwCtrlType == CTRL_C_EVENT || dwCtrlType == CTRL_BREAK_EVENT))
    {
        return g_pDebugInterface->SendCtrlCToDebugger(dwCtrlType);
    }
    else
#endif // DEBUGGING_SUPPORTED
    {         
        g_fInControlC = true;     // only for weakening assertions in checked build.
        return FALSE;             // keep looking for a real handler.
    }
}
#endif

// A host can specify that it only wants one version of hosting interface to be used.
BOOL g_singleVersionHosting;



void InitializeStartupFlags()
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    STARTUP_FLAGS flags = CorHost2::GetStartupFlags();


    if (flags & STARTUP_CONCURRENT_GC)
        g_IGCconcurrent = 1;
    else
        g_IGCconcurrent = 0;


    g_heap_type = (flags & STARTUP_SERVER_GC) == 0 ? GC_HEAP_WKS : GC_HEAP_SVR;
    g_IGCHoardVM = (flags & STARTUP_HOARD_GC_VM) == 0 ? 0 : 1;
}
#endif // CROSSGEN_COMPILE


#ifdef FEATURE_PREJIT
// BBSweepStartFunction is the first function to execute in the BBT sweeper thread.
// It calls WatchForSweepEvent where we wait until a sweep occurs.
DWORD __stdcall BBSweepStartFunction(LPVOID lpArgs)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    class CLRBBSweepCallback : public ICLRBBSweepCallback
    {
        virtual HRESULT WriteProfileData()
        {
            BEGIN_ENTRYPOINT_NOTHROW
            WRAPPER_NO_CONTRACT;
            Module::WriteAllModuleProfileData(false);
            END_ENTRYPOINT_NOTHROW;
            return S_OK;
        }
    } clrCallback;

    EX_TRY
    {
        g_BBSweep.WatchForSweepEvents(&clrCallback);
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(RethrowTerminalExceptions)

    return 0;
}
#endif // FEATURE_PREJIT


//-----------------------------------------------------------------------------

void InitGSCookie()
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;

    GSCookie * pGSCookiePtr = GetProcessGSCookiePtr();

    DWORD oldProtection;
    if(!ClrVirtualProtect((LPVOID)pGSCookiePtr, sizeof(GSCookie), PAGE_EXECUTE_READWRITE, &oldProtection))
    {
        ThrowLastError();
    }

#ifndef FEATURE_PAL
    // The GSCookie cannot be in a writeable page
    assert(((oldProtection & (PAGE_READWRITE|PAGE_WRITECOPY|PAGE_EXECUTE_READWRITE|
                              PAGE_EXECUTE_WRITECOPY|PAGE_WRITECOMBINE)) == 0));

    // Forces VC cookie to be initialized.
    void * pf = &__security_check_cookie;
    pf = NULL;

    GSCookie val = (GSCookie)(__security_cookie ^ GetTickCount());
#else // !FEATURE_PAL
    // REVIEW: Need something better for PAL...
    GSCookie val = (GSCookie)GetTickCount();
#endif // !FEATURE_PAL

#ifdef _DEBUG
    // In _DEBUG, always use the same value to make it easier to search for the cookie
    val = (GSCookie) WIN64_ONLY(0x9ABCDEF012345678) NOT_WIN64(0x12345678);
#endif

    // To test if it is initialized. Also for ICorMethodInfo::getGSCookie()
    if (val == 0)
        val ++;
    *pGSCookiePtr = val;

    if(!ClrVirtualProtect((LPVOID)pGSCookiePtr, sizeof(GSCookie), oldProtection, &oldProtection))
    {
        ThrowLastError();
    }
}

Volatile<BOOL> g_bIsGarbageCollectorFullyInitialized = FALSE;

void SetGarbageCollectorFullyInitialized()
{
    LIMITED_METHOD_CONTRACT;

    g_bIsGarbageCollectorFullyInitialized = TRUE;
}

// Tells whether the garbage collector is fully initialized
// Stronger than IsGCHeapInitialized
BOOL IsGarbageCollectorFullyInitialized()
{
    LIMITED_METHOD_CONTRACT;

    return g_bIsGarbageCollectorFullyInitialized;
}

// ---------------------------------------------------------------------------
// %%Function: EEStartupHelper
//
// Parameters:
//  fFlags                  - Initialization flags for the engine.  See the
//                              EEStartupFlags enumerator for valid values.
//
// Returns:
//  S_OK                    - On success
//
// Description:
//  Reserved to initialize the EE runtime engine explicitly.
// ---------------------------------------------------------------------------

#ifndef IfFailGotoLog
#define IfFailGotoLog(EXPR, LABEL) \
do { \
    hr = (EXPR);\
    if(FAILED(hr)) { \
        STRESS_LOG2(LF_STARTUP, LL_ALWAYS, "%s failed with code %x", #EXPR, hr);\
        goto LABEL; \
    } \
    else \
       STRESS_LOG1(LF_STARTUP, LL_ALWAYS, "%s completed", #EXPR);\
} while (0)
#endif

#ifndef IfFailGoLog
#define IfFailGoLog(EXPR) IfFailGotoLog(EXPR, ErrExit)
#endif

void EEStartupHelper(COINITIEE fFlags)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

#ifdef ENABLE_CONTRACTS_IMPL
    {
        extern void ContractRegressionCheck();
        ContractRegressionCheck();
    }
#endif

    HRESULT hr = S_OK;
    static ConfigDWORD breakOnEELoad;
    EX_TRY
    {
        g_fEEInit = true;

#ifndef CROSSGEN_COMPILE

#ifdef _DEBUG
        DisableGlobalAllocStore();
#endif //_DEBUG

#ifndef FEATURE_PAL
        ::SetConsoleCtrlHandler(DbgCtrlCHandler, TRUE/*add*/);
#endif

#endif // CROSSGEN_COMPILE

        // SString initialization
        // This needs to be done before config because config uses SString::Empty()
        SString::Startup();

        // Initialize EEConfig
        if (!g_pConfig)
        {
            IfFailGo(EEConfig::Setup());
        }

#ifndef CROSSGEN_COMPILE
        // Initialize Numa and CPU group information
        // Need to do this as early as possible. Used by creating object handle
        // table inside Ref_Initialization() before GC is initialized.
        NumaNodeInfo::InitNumaNodeInfo();
        CPUGroupInfo::EnsureInitialized();


        // Initialize global configuration settings based on startup flags
        // This needs to be done before the EE has started
        InitializeStartupFlags();

        InitThreadManager();
        STRESS_LOG0(LF_STARTUP, LL_ALWAYS, "Returned successfully from InitThreadManager");

#ifdef FEATURE_PERFTRACING
        // Initialize the event pipe.
        EventPipe::Initialize();
#endif // FEATURE_PERFTRACING

#ifdef FEATURE_GDBJIT
        // Initialize gdbjit
        NotifyGdb::Initialize();
#endif // FEATURE_GDBJIT

#ifdef FEATURE_EVENT_TRACE        
        // Initialize event tracing early so we can trace CLR startup time events.
        InitializeEventTracing();

        // Fire the EE startup ETW event
        ETWFireEvent(EEStartupStart_V1);
#endif // FEATURE_EVENT_TRACE

#ifdef FEATURE_IPCMAN
        // Give PerfMon a chance to hook up to us
        // Do this both *before* and *after* ipcman init so corperfmonext.dll
        // has a chance to release stale private blocks that IPCMan could collide with.
        // do this early to maximize window between perfmon refresh and ipc block creation.
        IPCFuncCallSource::DoThreadSafeCall();
#endif // FEATURE_IPCMAN

        InitGSCookie();

        Frame::Init();

#ifdef FEATURE_TESTHOOKS
        IfFailGo(CLRTestHookManager::CheckConfig());
#endif

#endif // CROSSGEN_COMPILE


#ifdef STRESS_LOG
        if (REGUTIL::GetConfigDWORD_DontUse_(CLRConfig::UNSUPPORTED_StressLog, g_pConfig->StressLog ()) != 0) {
            unsigned facilities = REGUTIL::GetConfigDWORD_DontUse_(CLRConfig::INTERNAL_LogFacility, LF_ALL);
            unsigned level = REGUTIL::GetConfigDWORD_DontUse_(CLRConfig::EXTERNAL_LogLevel, LL_INFO1000);
            unsigned bytesPerThread = REGUTIL::GetConfigDWORD_DontUse_(CLRConfig::UNSUPPORTED_StressLogSize, STRESSLOG_CHUNK_SIZE * 4);
            unsigned totalBytes = REGUTIL::GetConfigDWORD_DontUse_(CLRConfig::UNSUPPORTED_TotalStressLogSize, STRESSLOG_CHUNK_SIZE * 1024);
            StressLog::Initialize(facilities, level, bytesPerThread, totalBytes, GetModuleInst());
            g_pStressLog = &StressLog::theLog;
        }
#endif

#ifdef LOGGING
        InitializeLogging();
#endif

#ifdef ENABLE_PERF_LOG
        PerfLog::PerfLogInitialize();
#endif //ENABLE_PERF_LOG

#ifdef FEATURE_PERFMAP
        PerfMap::Initialize();
#endif

        STRESS_LOG0(LF_STARTUP, LL_ALWAYS, "===================EEStartup Starting===================");

#ifndef CROSSGEN_COMPILE
#ifndef FEATURE_PAL
        IfFailGoLog(EnsureRtlFunctions());
#endif // !FEATURE_PAL
        InitEventStore();
#endif

        // Fusion
        // Initialize the general Assembly Binder infrastructure
        IfFailGoLog(CCoreCLRBinderHelper::Init());

        if (g_pConfig != NULL)
        {
            IfFailGoLog(g_pConfig->sync());        
        }

        // Fire the runtime information ETW event
        ETW::InfoLog::RuntimeInformation(ETW::InfoLog::InfoStructs::Normal);

        if (breakOnEELoad.val(CLRConfig::UNSUPPORTED_BreakOnEELoad) == 1)
        {
#ifdef _DEBUG
            _ASSERTE(!"Start loading EE!");
#else
            DebugBreak();
#endif
        }

#ifdef ENABLE_STARTUP_DELAY
        PREFIX_ASSUME(NULL != g_pConfig);
        if (g_pConfig->StartupDelayMS())
        {
            ClrSleepEx(g_pConfig->StartupDelayMS(), FALSE);
        }
#endif
        
#if USE_DISASSEMBLER
        if ((g_pConfig->GetGCStressLevel() & (EEConfig::GCSTRESS_INSTR_JIT | EEConfig::GCSTRESS_INSTR_NGEN)) != 0)
        {
            Disassembler::StaticInitialize();
            if (!Disassembler::IsAvailable())
            {
                fprintf(stderr, "External disassembler is not available.\n");
                IfFailGo(E_FAIL);
            }
        }
#endif // USE_DISASSEMBLER

        // Monitors, Crsts, and SimpleRWLocks all use the same spin heuristics
        // Cache the (potentially user-overridden) values now so they are accessible from asm routines
        InitializeSpinConstants();

#ifndef CROSSGEN_COMPILE


#ifdef FEATURE_PREJIT
        // Initialize the sweeper thread.
        if (g_pConfig->GetZapBBInstr() != NULL)
        {
            DWORD threadID;
            HANDLE hBBSweepThread = ::CreateThread(NULL,
                                                   0,
                                                   (LPTHREAD_START_ROUTINE) BBSweepStartFunction,
                                                   NULL,
                                                   0,
                                                   &threadID);
            _ASSERTE(hBBSweepThread);
            g_BBSweep.SetBBSweepThreadHandle(hBBSweepThread);
        }
#endif // FEATURE_PREJIT

#ifdef FEATURE_IPCMAN
        // Initialize all our InterProcess Communications with COM+
        IfFailGoLog(InitializeIPCManager());
#endif // FEATURE_IPCMAN

#ifdef ENABLE_PERF_COUNTERS
        hr = PerfCounters::Init();
        _ASSERTE(SUCCEEDED(hr));
        IfFailGo(hr);
#endif

#ifdef FEATURE_IPCMAN
        // Marks the data in the IPC blocks as initialized so that readers know
        // that it is safe to read data from the blocks
        PublishIPCManager();
#endif //FEATURE_IPCMAN

#ifdef FEATURE_INTERPRETER
        Interpreter::Initialize();
#endif // FEATURE_INTERPRETER

        StubManager::InitializeStubManagers();

#ifndef FEATURE_PAL
        {
            // Record mscorwks geometry
            PEDecoder pe(g_pMSCorEE);

            g_runtimeLoadedBaseAddress = (SIZE_T)pe.GetBase();
            g_runtimeVirtualSize = (SIZE_T)pe.GetVirtualSize();
            InitCodeAllocHint(g_runtimeLoadedBaseAddress, g_runtimeVirtualSize, GetRandomInt(64));
        }
#endif // !FEATURE_PAL

#endif // CROSSGEN_COMPILE

        // Set up the cor handle map. This map is used to load assemblies in
        // memory instead of using the normal system load
        PEImage::Startup();

        AccessCheckOptions::Startup();

        MscorlibBinder::Startup();

        Stub::Init();
        StubLinkerCPU::Init();

#ifndef CROSSGEN_COMPILE

        InitializeGarbageCollector();

        // Initialize remoting

        if (!GCHandleUtilities::GetGCHandleManager()->Initialize())
        {
            IfFailGo(E_OUTOFMEMORY);
        }

        // Initialize contexts
        Context::Initialize();

        g_pEEShutDownEvent = new CLREvent();
        g_pEEShutDownEvent->CreateManualEvent(FALSE);

#ifdef FEATURE_IPCMAN
        // Initialize CCLRSecurityAttributeManager
        CCLRSecurityAttributeManager::ProcessInit();
#endif // FEATURE_IPCMAN

        VirtualCallStubManager::InitStatic();

        GCInterface::m_MemoryPressureLock.Init(CrstGCMemoryPressure);

#endif // CROSSGEN_COMPILE

        // Setup the domains. Threads are started in a default domain.

        // Static initialization
        PEAssembly::Attach();
        BaseDomain::Attach();
        SystemDomain::Attach();

        // Start up the EE intializing all the global variables
        ECall::Init();

        COMDelegate::Init();

        ExecutionManager::Init();

#ifndef CROSSGEN_COMPILE

#ifndef FEATURE_PAL
        // Watson initialization must precede InitializeDebugger() and InstallUnhandledExceptionFilter() 
        // because on CoreCLR when Waston is enabled, debugging service needs to be enabled and UEF will be used.
        if (!InitializeWatson(fFlags))
        {
            IfFailGo(E_FAIL);
        }
       
        // Note: In Windows 7, the OS will take over the job of error reporting, and so most 
        // of our watson code should not be used.  In such cases, we will however still need 
        // to provide some services to windows error reporting, such as computing bucket 
        // parameters for a managed unhandled exception.  
        if (RunningOnWin7() && IsWatsonEnabled() && !RegisterOutOfProcessWatsonCallbacks())
        {
            IfFailGo(E_FAIL);
        }
#endif // !FEATURE_PAL

#ifdef DEBUGGING_SUPPORTED
        if(!NingenEnabled())
        {
            // Initialize the debugging services. This must be done before any
            // EE thread objects are created, and before any classes or
            // modules are loaded.
            InitializeDebugger(); // throws on error
        }
#endif // DEBUGGING_SUPPORTED

#ifdef MDA_SUPPORTED
        ManagedDebuggingAssistants::EEStartupActivation();
#endif

#ifdef PROFILING_SUPPORTED
        // Initialize the profiling services.
        hr = ProfilingAPIUtility::InitializeProfiling();

        _ASSERTE(SUCCEEDED(hr));
        IfFailGo(hr);
#endif // PROFILING_SUPPORTED

        InitializeExceptionHandling();

        //
        // Install our global exception filter
        //
        if (!InstallUnhandledExceptionFilter())
        {
            IfFailGo(E_FAIL);
        }

        // throws on error
        SetupThread();

#ifdef DEBUGGING_SUPPORTED
        // Notify debugger once the first thread is created to finish initialization.
        if (g_pDebugInterface != NULL)
        {
            g_pDebugInterface->StartupPhase2(GetThread());
        }
#endif

#ifdef FEATURE_IPCMAN
        // Give PerfMon a chance to hook up to us
        // Do this both *before* and *after* ipcman init so corperfmonext.dll
        // has a chance to release stale private blocks that IPCMan could collide with.
        IPCFuncCallSource::DoThreadSafeCall();
        STRESS_LOG0(LF_STARTUP, LL_ALWAYS, "Returned successfully from second call to  IPCFuncCallSource::DoThreadSafeCall");
#endif // FEATURE_IPCMAN

        InitPreStubManager();

#ifdef FEATURE_COMINTEROP
        InitializeComInterop();
#endif // FEATURE_COMINTEROP

        StubHelpers::Init();
        NDirect::Init();

        // Before setting up the execution manager initialize the first part
        // of the JIT helpers.
        InitJITHelpers1();
        InitJITHelpers2();

        SyncBlockCache::Attach();

        // Set up the sync block
        SyncBlockCache::Start();

        StackwalkCache::Init();

        AppDomain::CreateADUnloadStartEvent();

        // In coreclr, clrjit is compiled into it, but SO work in clrjit has not been done.
#ifdef FEATURE_STACK_PROBE
        if (CLRHosted() && GetEEPolicy()->GetActionOnFailure(FAIL_StackOverflow) == eRudeUnloadAppDomain)
        {
            InitStackProbes();
        }
#endif

        // This isn't done as part of InitializeGarbageCollector() above because it
        // requires write barriers to have been set up on x86, which happens as part
        // of InitJITHelpers1.
        hr = g_pGCHeap->Initialize();
        IfFailGo(hr);

        // This isn't done as part of InitializeGarbageCollector() above because thread
        // creation requires AppDomains to have been set up.
        FinalizerThread::FinalizerThreadCreate();

        // Now we really have fully initialized the garbage collector
        SetGarbageCollectorFullyInitialized();

        InitializePinHandleTable();

#ifdef DEBUGGING_SUPPORTED
        // Make a call to publish the DefaultDomain for the debugger
        // This should be done before assemblies/modules are loaded into it (i.e. SystemDomain::Init)
        // and after its OK to switch GC modes and syncronize for sending events to the debugger.
        // @dbgtodo  synchronization: this can probably be simplified in V3
        LOG((LF_CORDB | LF_SYNC | LF_STARTUP, LL_INFO1000, "EEStartup: adding default domain 0x%x\n",
             SystemDomain::System()->DefaultDomain()));
        SystemDomain::System()->PublishAppDomainAndInformDebugger(SystemDomain::System()->DefaultDomain());
#endif

#ifdef FEATURE_PERFTRACING
        // Start the event pipe if requested.
        EventPipe::EnableOnStartup();
#endif // FEATURE_PERFTRACING

#endif // CROSSGEN_COMPILE

        SystemDomain::System()->Init();

#ifdef PROFILING_SUPPORTED
        // <TODO>This is to compensate for the DefaultDomain workaround contained in
        // SystemDomain::Attach in which the first user domain is created before profiling
        // services can be initialized.  Profiling services cannot be moved to before the
        // workaround because it needs SetupThread to be called.</TODO>

        SystemDomain::NotifyProfilerStartup();
#endif // PROFILING_SUPPORTED

#ifndef CROSSGEN_COMPILE
        if (CLRHosted()
#ifdef _DEBUG
            || ((fFlags & COINITEE_DLL) == 0 &&
                g_pConfig->GetHostTestADUnload())
#endif
           ) {
                // If we are hosted, a host may specify unloading AD when a managed allocation in
                // critical region fails.  We need to precreate a thread to unload AD.
                AppDomain::CreateADUnloadWorker();
        }
#endif // CROSSGEN_COMPILE

        g_fEEInit = false;

        SystemDomain::System()->DefaultDomain()->LoadSystemAssemblies();

        SystemDomain::System()->DefaultDomain()->SetupSharedStatics();

#ifdef _DEBUG
        APIThreadStress::SetThreadStressCount(g_pConfig->GetAPIThreadStressCount());
#endif
#ifdef FEATURE_STACK_SAMPLING
        StackSampler::Init();
#endif

#ifndef CROSSGEN_COMPILE
        if (!NingenEnabled())
        {
            // Perform any once-only SafeHandle initialization.
            SafeHandle::Init();
        }

#ifdef FEATURE_MINIMETADATA_IN_TRIAGEDUMPS
        // retrieve configured max size for the mini-metadata buffer (defaults to 64KB)
        g_MiniMetaDataBuffMaxSize = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_MiniMdBufferCapacity);
        // align up to GetOsPageSize(), with a maximum of 1 MB
        g_MiniMetaDataBuffMaxSize = (DWORD) min(ALIGN_UP(g_MiniMetaDataBuffMaxSize, GetOsPageSize()), 1024 * 1024);
        // allocate the buffer. this is never touched while the process is running, so it doesn't 
        // contribute to the process' working set. it is needed only as a "shadow" for a mini-metadata
        // buffer that will be set up and reported / updated in the Watson process (the 
        // DacStreamsManager class coordinates this)
        g_MiniMetaDataBuffAddress = (TADDR) ClrVirtualAlloc(NULL, 
                                                g_MiniMetaDataBuffMaxSize, MEM_COMMIT, PAGE_READWRITE);
#endif // FEATURE_MINIMETADATA_IN_TRIAGEDUMPS

#endif // CROSSGEN_COMPILE

        g_fEEStarted = TRUE;
        g_EEStartupStatus = S_OK;
        hr = S_OK;
        STRESS_LOG0(LF_STARTUP, LL_ALWAYS, "===================EEStartup Completed===================");

#if defined(_DEBUG) && !defined(CROSSGEN_COMPILE)

        //if g_fEEStarted was false when we loaded the System Module, we did not run ExpandAll on it.  In
        //this case, make sure we run ExpandAll here.  The rationale is that if we Jit before g_fEEStarted
        //is true, we can't initialize Com, so we can't jit anything that uses Com types.  Also, it's
        //probably not safe to Jit while g_fEEStarted is false.
        //
        //Also, if you run this it's possible we'll call CoInitialize, which defaults to MTA.  This might
        //mess up an application that uses STA.  However, this mode is only supported for certain limited
        //jit testing scenarios, so it can live with the limitation.
        if (g_pConfig->ExpandModulesOnLoad())
        {
            SystemDomain::SystemModule()->ExpandAll();
        }

        // Perform mscorlib consistency check if requested
        g_Mscorlib.CheckExtended();

#endif // _DEBUG && !CROSSGEN_COMPILE

ErrExit: ;
    }
    EX_CATCH
    {
#ifdef CROSSGEN_COMPILE
        // for minimal impact we won't update hr for regular builds
        hr = GET_EXCEPTION()->GetHR();
        _ASSERTE(FAILED(hr));
        StackSString exceptionMessage;
        GET_EXCEPTION()->GetMessage(exceptionMessage);
        fprintf(stderr, "%S\n", exceptionMessage.GetUnicode());
#endif // CROSSGEN_COMPILE
    }
    EX_END_CATCH(RethrowTerminalExceptionsWithInitCheck)

    if (!g_fEEStarted) {
        if (g_fEEInit)
            g_fEEInit = false;

        if (!FAILED(hr))
            hr = E_FAIL;

        g_EEStartupStatus = hr;
    }

    if (breakOnEELoad.val(CLRConfig::UNSUPPORTED_BreakOnEELoad) == 2)
    {
#ifdef _DEBUG
        _ASSERTE(!"Done loading EE!");
#else
        DebugBreak();
#endif
    }

}

LONG FilterStartupException(PEXCEPTION_POINTERS p, PVOID pv)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(p));
        PRECONDITION(CheckPointer(pv));
    } CONTRACTL_END;

    g_EEStartupStatus = (HRESULT)p->ExceptionRecord->ExceptionInformation[0];

    // Make sure we got a failure code in this case
    if (!FAILED(g_EEStartupStatus))
        g_EEStartupStatus = E_FAIL;

    // Initializations has failed so reset the g_fEEInit flag.
    g_fEEInit = false;

    if (p->ExceptionRecord->ExceptionCode == BOOTUP_EXCEPTION_COMPLUS)
    {
        // Don't ever handle the exception in a checked build
#ifndef _DEBUG
        return EXCEPTION_EXECUTE_HANDLER;
#endif
    }

    return EXCEPTION_CONTINUE_SEARCH;
}

// EEStartup is responcible for all the one time intialization of the runtime.  Some of the highlights of
// what it does include
//     * Creates the default and shared, appdomains. 
//     * Loads mscorlib.dll and loads up the fundamental types (System.Object ...)
// 
// see code:EEStartup#TableOfContents for more on the runtime in general. 
// see code:#EEShutdown for a analagous routine run during shutdown. 
// 
HRESULT EEStartup(COINITIEE fFlags)
{
    // Cannot use normal contracts here because of the PAL_TRY.
    STATIC_CONTRACT_NOTHROW;

    _ASSERTE(!g_fEEStarted && !g_fEEInit && SUCCEEDED (g_EEStartupStatus));

    PAL_TRY(COINITIEE *, pfFlags, &fFlags)
    {
#ifndef CROSSGEN_COMPILE
        InitializeClrNotifications();
#ifdef FEATURE_PAL
        InitializeJITNotificationTable();
        DacGlobals::Initialize();
#endif
#endif // CROSSGEN_COMPILE

        EEStartupHelper(*pfFlags);
    }
    PAL_EXCEPT_FILTER (FilterStartupException)
    {
        // The filter should have set g_EEStartupStatus to a failure HRESULT.
        _ASSERTE(FAILED(g_EEStartupStatus));
    }
    PAL_ENDTRY

#ifndef CROSSGEN_COMPILE
    if(SUCCEEDED(g_EEStartupStatus) && (fFlags & COINITEE_MAIN) == 0)
        g_EEStartupStatus = SystemDomain::SetupDefaultDomainNoThrow();
#endif

    return g_EEStartupStatus;
}


#ifndef CROSSGEN_COMPILE

#ifdef FEATURE_COMINTEROP

void InnerCoEEShutDownCOM()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    static LONG AlreadyDone = -1;

    if (g_fEEStarted != TRUE)
        return;

    if (FastInterlockIncrement(&AlreadyDone) != 0)
        return;

    g_fShutDownCOM = true;

    // Release IJupiterGCMgr *
    RCWWalker::OnEEShutdown();

    // Release all of the RCWs in all contexts in all caches.
    ReleaseRCWsInCaches(NULL);

    // Release all marshaling data in all AppDomains
    AppDomainIterator i(TRUE);
    while (i.Next())
        i.GetDomain()->DeleteMarshalingData();

    // Release marshaling data  in shared domain as well
    SharedDomain::GetDomain()->DeleteMarshalingData();

#ifdef FEATURE_APPX    
    // Cleanup cached factory pointer in SynchronizationContextNative
    SynchronizationContextNative::Cleanup();
#endif    
}

// ---------------------------------------------------------------------------
// %%Function: CoEEShutdownCOM()
//
// Parameters:
//  none
//
// Returns:
//  Nothing
//
// Description:
//  COM Objects shutdown stuff should be done here
// ---------------------------------------------------------------------------
void STDMETHODCALLTYPE CoEEShutDownCOM()
{

    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        ENTRY_POINT;
    } CONTRACTL_END;

    if (g_fEEStarted != TRUE)
        return;

    HRESULT hr;
    BEGIN_EXTERNAL_ENTRYPOINT(&hr)

    InnerCoEEShutDownCOM();

    END_EXTERNAL_ENTRYPOINT;

    // API doesn't allow us to communicate a failure HRESULT.  MDAs can
    // be enabled to catch failure inside CanRunManagedCode.
    // _ASSERTE(SUCCEEDED(hr));
}

#endif // FEATURE_COMINTEROP

// ---------------------------------------------------------------------------
// %%Function: ForceEEShutdown()
//
// Description: Force the EE to shutdown now.
// 
// Note: returns when sca is SCA_ReturnWhenShutdownComplete.
// ---------------------------------------------------------------------------
void ForceEEShutdown(ShutdownCompleteAction sca)
{
    WRAPPER_NO_CONTRACT;

    // Don't bother to take the lock for this case.

    STRESS_LOG0(LF_STARTUP, INFO3, "EEShutdown invoked from ForceEEShutdown");
    EEPolicy::HandleExitProcess(sca);
}

//---------------------------------------------------------------------------
// %%Function: ExternalShutdownHelper
//
// Parameters:
//  int exitCode :: process exit code
//  ShutdownCompleteAction sca :: indicates whether ::ExitProcess() is
//                                called or if the function returns.
//
// Returns:
//  Nothing
//
// Description:
// This is a helper shared by CorExitProcess and ShutdownRuntimeWithoutExiting 
// which causes the runtime to shutdown after the appropriate checks. 
// ---------------------------------------------------------------------------
static void ExternalShutdownHelper(int exitCode, ShutdownCompleteAction sca)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        ENTRY_POINT;
    } CONTRACTL_END;

    CONTRACT_VIOLATION(GCViolation | ModeViolation | SOToleranceViolation);

    if (g_fEEShutDown || !g_fEEStarted)
        return;

    if (HasIllegalReentrancy())
    {
        return;
    }
    else
    if (!CanRunManagedCode())
    {
        return;
    }

    // The exit code for the process is communicated in one of two ways.  If the
    // entrypoint returns an 'int' we take that.  Otherwise we take a latched
    // process exit code.  This can be modified by the app via System.SetExitCode().
    SetLatchedExitCode(exitCode);


    ForceEEShutdown(sca);

    // @TODO: If we cannot run ManagedCode, BEGIN_EXTERNAL_ENTRYPOINT will skip
    // the shutdown.  We could call ::ExitProcess in that failure case, but that
    // would violate our hosting agreement.  We are supposed to go through EEPolicy::
    // HandleExitProcess().  Is this legal if !CanRunManagedCode()?

}

//---------------------------------------------------------------------------
// %%Function: void STDMETHODCALLTYPE CorExitProcess(int exitCode)
//
// Parameters:
//  int exitCode :: process exit code
//
// Returns:
//  Nothing
//
// Description:
//  COM Objects shutdown stuff should be done here
// ---------------------------------------------------------------------------
extern "C" void STDMETHODCALLTYPE CorExitProcess(int exitCode)
{
    WRAPPER_NO_CONTRACT;

    ExternalShutdownHelper(exitCode, SCA_ExitProcessWhenShutdownComplete);
}

//---------------------------------------------------------------------------
// %%Function: ShutdownRuntimeWithoutExiting
//
// Parameters:
//  int exitCode :: process exit code
//
// Returns:
//  Nothing
//
// Description:
// This is a helper used only by the v4+ Shim to shutdown this runtime and
// and return when the work has completed. It is exposed to the Shim via
// GetCLRFunction.
// ---------------------------------------------------------------------------
void ShutdownRuntimeWithoutExiting(int exitCode)
{
    WRAPPER_NO_CONTRACT;

    ExternalShutdownHelper(exitCode, SCA_ReturnWhenShutdownComplete);
}

//---------------------------------------------------------------------------
// %%Function: IsRuntimeStarted
//
// Parameters:
//  pdwStartupFlags: out parameter that is set to the startup flags if the
//                   runtime is started.
//
// Returns:
//  TRUE if the runtime has been started, FALSE otherwise.
//
// Description:
// This is a helper used only by the v4+ Shim to determine if this runtime
// has ever been started. It is exposed ot the Shim via GetCLRFunction.
// ---------------------------------------------------------------------------
BOOL IsRuntimeStarted(DWORD *pdwStartupFlags)
{
    LIMITED_METHOD_CONTRACT;

    if (pdwStartupFlags != NULL) // this parameter is optional
    {
        *pdwStartupFlags = 0;
    }
    return g_fEEStarted;
}

static bool WaitForEndOfShutdown_OneIteration()
{
    CONTRACTL{
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    // We are shutting down.  GC triggers does not have any effect now.
    CONTRACT_VIOLATION(GCViolation);

    // If someone calls EEShutDown while holding OS loader lock, the thread we created for shutdown
    // won't start running.  This is a deadlock we can not fix.  Instead, we timeout and continue the 
    // current thread.
    DWORD timeout = GetEEPolicy()->GetTimeout(OPR_ProcessExit);
    timeout *= 2;
    ULONGLONG endTime = CLRGetTickCount64() + timeout;
    bool done = false;

    EX_TRY
    {
        ULONGLONG curTime = CLRGetTickCount64();
        if (curTime > endTime)
        {
            done = true;
        }
        else
        {
#ifdef PROFILING_SUPPORTED
            if (CORProfilerPresent())
            {
                // A profiler is loaded, so just wait without timeout. This allows
                // profilers to complete potentially lengthy post processing, without the
                // CLR killing them off first. The Office team's server memory profiler,
                // for example, does a lot of post-processing that can exceed the 80
                // second imit we normally impose here. The risk of waiting without
                // timeout is that, if there really is a deadlock, shutdown will hang.
                // Since that will only happen if a profiler is loaded, that is a
                // reasonable compromise
                timeout = INFINITE;
            }
            else
#endif //PROFILING_SUPPORTED
            {
                timeout = static_cast<DWORD>(endTime - curTime);
            }
            DWORD status = g_pEEShutDownEvent->Wait(timeout,TRUE);
            if (status == WAIT_OBJECT_0 || status == WAIT_TIMEOUT)
            {
                done = true;
            }
            else
            {
                done = false;
            }
        }
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);
    return done;
}

void WaitForEndOfShutdown()
{
    CONTRACTL{
        NOTHROW;
        GC_NOTRIGGER;
        MODE_PREEMPTIVE;
    } CONTRACTL_END;

    // We are shutting down.  GC triggers does not have any effect now.
    CONTRACT_VIOLATION(GCViolation);

    Thread *pThread = GetThread();
    // After a thread is blocked in WaitForEndOfShutdown, the thread should not enter runtime again,
    // and block at WaitForEndOfShutdown again.
    if (pThread)
    {
        _ASSERTE(!pThread->HasThreadStateNC(Thread::TSNC_BlockedForShutdown));
        pThread->SetThreadStateNC(Thread::TSNC_BlockedForShutdown);
    }

    while (!WaitForEndOfShutdown_OneIteration());
}

// ---------------------------------------------------------------------------
// Function: EEShutDownHelper(BOOL fIsDllUnloading)
//
// The real meat of shut down happens here.  See code:#EEShutDown for details, including
// what fIsDllUnloading means.
//
void STDMETHODCALLTYPE EEShutDownHelper(BOOL fIsDllUnloading)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    // Used later for a callback.
    CEEInfo ceeInf;

    if(fIsDllUnloading)
    {
        ETW::EnumerationLog::ProcessShutdown();
    }

#ifdef FEATURE_PERFTRACING
    // Shutdown the event pipe.
    EventPipe::Shutdown();
#endif // FEATURE_PERFTRACING

#if defined(FEATURE_COMINTEROP)
    // Get the current thread.
    Thread * pThisThread = GetThread();
#endif

    // If the process is detaching then set the global state.
    // This is used to get around FreeLibrary problems.
    if(fIsDllUnloading)
        g_fProcessDetach = true;

    if (IsDbgHelperSpecialThread())
    {
        // Our debugger helper thread does not allow Thread object to be set up.
        // We should not run shutdown code on debugger helper thread.
        _ASSERTE(fIsDllUnloading);
        return;
    }

#ifdef _DEBUG
    // stop API thread stress
    APIThreadStress::SetThreadStressCount(0);
#endif

    STRESS_LOG1(LF_STARTUP, LL_INFO10, "EEShutDown entered unloading = %d", fIsDllUnloading);

#ifdef _DEBUG
    if (_DbgBreakCount)
        _ASSERTE(!"An assert was hit before EE Shutting down");

    if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_BreakOnEEShutdown))
        _ASSERTE(!"Shutting down EE!");
#endif

#ifdef DEBUGGING_SUPPORTED
    // This is a nasty, terrible, horrible thing. If we're being
    // called from our DLL main, then the odds are good that our DLL
    // main has been called as the result of some person calling
    // ExitProcess. That rips the debugger helper thread away very
    // ungracefully. This check is an attempt to recognize that case
    // and avoid the impending hang when attempting to get the helper
    // thread to do things for us.
    if ((g_pDebugInterface != NULL) && g_fProcessDetach)
        g_pDebugInterface->EarlyHelperThreadDeath();
#endif // DEBUGGING_SUPPORTED

    BOOL fFinalizeOK = FALSE;

    EX_TRY
    {
        ClrFlsSetThreadType(ThreadType_Shutdown);

        if (!fIsDllUnloading)
        {
            ProcessEventForHost(Event_ClrDisabled, NULL);
        }
        else if (g_fEEShutDown)
        {
            // I'm in the final shutdown and the first part has already been run.
            goto part2;
        }

        // Indicate the EE is the shut down phase.
        g_fEEShutDown |= ShutDown_Start;

#ifdef FEATURE_TIERED_COMPILATION
        {
            GCX_PREEMP();
            TieredCompilationManager::ShutdownAllDomains();
        }
#endif

        fFinalizeOK = TRUE;

        // Terminate the BBSweep thread
        g_BBSweep.ShutdownBBSweepThread();

        // We perform the final GC only if the user has requested it through the GC class.
        // We should never do the final GC for a process detach
        if (!g_fProcessDetach && !g_fFastExitProcess)
        {
            g_fEEShutDown |= ShutDown_Finalize1;
            FinalizerThread::EnableFinalization();
            fFinalizeOK = FinalizerThread::FinalizerThreadWatchDog();
        }


        // Ok.  Let's stop the EE.
        if (!g_fProcessDetach)
        {
            // Convert key locks into "shutdown" mode. A lock in shutdown mode means:
            // - Only the finalizer/helper/shutdown threads will be able to take the the lock.
            // - Any other thread that tries takes it will just get redirected to an endless WaitForEndOfShutdown().
            //
            // The only managed code that should run after this point is the finalizers for shutdown.
            // We convert locks needed for running + debugging such finalizers. Since such locks may need to be
            // juggled between multiple threads (finalizer/helper/shutdown), no single thread can take the
            // lock and not give it up.
            //
            // Each lock needs its own shutdown flag (they can't all be converted at once).
            // To avoid deadlocks, we need to convert locks in order of crst level (biggest first).

            // Notify the debugger that we're going into shutdown to convert debugger-lock to shutdown.
            if (g_pDebugInterface != NULL)
            {
                g_pDebugInterface->LockDebuggerForShutdown();
            }

            // This call will convert the ThreadStoreLock into "shutdown" mode, just like the debugger lock above.
            g_fEEShutDown |= ShutDown_Finalize2;
            if (fFinalizeOK)
            {
                fFinalizeOK = FinalizerThread::FinalizerThreadWatchDog();
            }

            if (!fFinalizeOK)
            {
                // One of the calls to FinalizerThreadWatchDog failed due to timeout, so we need to prevent
                // any thread from running managed code, including the finalizer.
                ThreadSuspend::SuspendEE(ThreadSuspend::SUSPEND_FOR_SHUTDOWN);
                g_fSuspendOnShutdown = TRUE;
                g_fSuspendFinalizerOnShutdown = TRUE;
                ThreadStore::TrapReturningThreads(TRUE);
                ThreadSuspend::RestartEE(FALSE, TRUE);
            }
        }

#ifdef FEATURE_EVENT_TRACE
        // Flush managed object allocation logging data.
        // We do this after finalization is complete and returning threads have been trapped, so that
        // no there will be no more managed allocations and no more GCs which will manipulate the
        // allocation sampling data structures.
        ETW::TypeSystemLog::FlushObjectAllocationEvents();
#endif // FEATURE_EVENT_TRACE

#ifdef FEATURE_PERFMAP
        // Flush and close the perf map file.
        PerfMap::Destroy();
#endif

#ifdef FEATURE_PREJIT
        {
            // If we're doing basic block profiling, we need to write the log files to disk.

            static BOOL fIBCLoggingDone = FALSE;
            if (!fIBCLoggingDone)
            {
                if (g_IBCLogger.InstrEnabled())
                {
                    Thread * pThread = GetThread();
                    ThreadLocalIBCInfo* pInfo = pThread->GetIBCInfo();

                    // Acquire the Crst lock before creating the IBCLoggingDisabler object.
                    // Only one thread at a time can be processing an IBC logging event.
                    CrstHolder lock(g_IBCLogger.GetSync());
                    {
                        IBCLoggingDisabler disableLogging( pInfo );  // runs IBCLoggingDisabler::DisableLogging
                        
                        Module::WriteAllModuleProfileData(true);
                    }
                }
                fIBCLoggingDone = TRUE;
            }
        }

#endif // FEATURE_PREJIT

        ceeInf.JitProcessShutdownWork();  // Do anything JIT-related that needs to happen at shutdown.

#ifdef FEATURE_INTERPRETER
        // This will check a flag and do nothing if not enabled.
        Interpreter::PrintPostMortemData();
#endif // FEATURE_INTERPRETER
        
        FastInterlockExchange((LONG*)&g_fForbidEnterEE, TRUE);

        if (g_fProcessDetach)
        {
            ThreadStore::TrapReturningThreads(TRUE);
        }

        if (!g_fProcessDetach && !fFinalizeOK)
        {
            goto lDone;
        }

#ifdef PROFILING_SUPPORTED
        // If profiling is enabled, then notify of shutdown first so that the
        // profiler can make any last calls it needs to.  Do this only if we
        // are not detaching

        if (CORProfilerPresent())
        {
            // If EEShutdown is not being called due to a ProcessDetach event, so
            // the profiler should still be present
            if (!g_fProcessDetach)
            {
                BEGIN_PIN_PROFILER(CORProfilerPresent());
                GCX_PREEMP();
                g_profControlBlock.pProfInterface->Shutdown();
                END_PIN_PROFILER();
            }

            g_fEEShutDown |= ShutDown_Profiler;

            // Free the interface objects.
            ProfilingAPIUtility::TerminateProfiling();
        }
#endif // PROFILING_SUPPORTED


#ifdef _DEBUG
        g_fEEShutDown |= ShutDown_SyncBlock;
#endif
        {
            // From here on out we might call stuff that violates mode requirements, but we ignore these
            // because we are shutting down.
            CONTRACT_VIOLATION(ModeViolation);

#ifdef FEATURE_COMINTEROP
            // We need to call CoUninitialize in part one to ensure orderly shutdown of COM dlls.
            if (!g_fFastExitProcess)
            {
                if (pThisThread!= NULL)
                {
                    pThisThread->CoUninitialize();
                }
            }
#endif // FEATURE_COMINTEROP
        }

        // This is the end of Part 1.

part2:
        // If process shutdown is in progress and Crst locks to be used in shutdown phase 2
        // are already in use, then skip phase 2. This will happen only when those locks
        // are orphaned. In Vista, the penalty for attempting to enter such locks is 
        // instant process termination.
        if (g_fProcessDetach)
        {
            // The assert below is a bit too aggresive and has generally brought cases that have been race conditions
            // and not easily reproed to validate a bug. A typical race scenario is when there are two threads,
            // T1 and T2, with T2 having taken a lock (e.g. SystemDomain lock), the OS terminates
            // T2 for some reason. Later, when we enter the shutdown thread, we would assert on such
            // a lock leak, but there is not much we can do since the OS wont notify us prior to thread 
            // termination. And this is not even a user bug.
            //
            // Converting it to a STRESS LOG to reduce noise, yet keep things in radar if they need
            // to be investigated.
            //_ASSERTE_MSG(g_ShutdownCrstUsageCount == 0, "Some locks to be taken during shutdown may already be orphaned!");
            if (g_ShutdownCrstUsageCount > 0)
            {
                STRESS_LOG0(LF_STARTUP, LL_INFO10, "Some locks to be taken during shutdown may already be orphaned!");
                goto lDone;
            }
        }

        {
            CONTRACT_VIOLATION(ModeViolation);

            // On the new plan, we only do the tear-down under the protection of the loader
            // lock -- after the OS has stopped all other threads.
            if (fIsDllUnloading && (g_fEEShutDown & ShutDown_Phase2) == 0)
            {
                g_fEEShutDown |= ShutDown_Phase2;

                // Shutdown finalizer before we suspend all background threads. Otherwise we
                // never get to finalize anything. Obviously.

#ifdef _DEBUG
                if (_DbgBreakCount)
                    _ASSERTE(!"An assert was hit After Finalizer run");
#endif

                // No longer process exceptions
                g_fNoExceptions = true;

                //
                // Remove our global exception filter. If it was NULL before, we want it to be null now.
                //
                UninstallUnhandledExceptionFilter();

                // <TODO>@TODO: This does things which shouldn't occur in part 2.  Namely,
                // calling managed dll main callbacks (AppDomain::SignalProcessDetach), and
                // RemoveAppDomainFromIPC.
                //
                // (If we move those things to earlier, this can be called only if fShouldWeCleanup.)</TODO>
                if (!g_fFastExitProcess)
                {
                    SystemDomain::DetachBegin();
                }


#ifdef DEBUGGING_SUPPORTED
                // Terminate the debugging services.
                TerminateDebugger();
#endif // DEBUGGING_SUPPORTED

                StubManager::TerminateStubManagers();

#ifdef FEATURE_INTERPRETER
                Interpreter::Terminate();
#endif // FEATURE_INTERPRETER

#ifdef SHOULD_WE_CLEANUP
                if (!g_fFastExitProcess)
                {
                    GCHandleUtilities::GetGCHandleManager()->Shutdown();
                }
#endif /* SHOULD_WE_CLEANUP */

#ifdef ENABLE_PERF_COUNTERS
                // Terminate Perf Counters as late as we can (to get the most data)
                PerfCounters::Terminate();
#endif // ENABLE_PERF_COUNTERS

                //@TODO: find the right place for this
                VirtualCallStubManager::UninitStatic();

#ifdef FEATURE_IPCMAN
                // Terminate the InterProcess Communications with COM+
                TerminateIPCManager();
#endif // FEATURE_IPCMAN

#ifdef ENABLE_PERF_LOG
                PerfLog::PerfLogDone();
#endif //ENABLE_PERF_LOG

#ifdef FEATURE_IPCMAN
                // Give PerfMon a chance to hook up to us
                // Have perfmon resync list *after* we close IPC so that it will remove
                // this process
                IPCFuncCallSource::DoThreadSafeCall();
#endif // FEATURE_IPCMAN

                Frame::Term();

                if (!g_fFastExitProcess)
                {
                    SystemDomain::DetachEnd();
                }

                TerminateStackProbes();

                // Unregister our vectored exception and continue handlers from the OS.
                // This will ensure that if any other DLL unload (after ours) has an exception,
                // we wont attempt to process that exception (which could lead to various
                // issues including AV in the runtime).
                //
                // This should be done:
                //
                // 1) As the last action during the shutdown so that any unexpected AVs
                //    in the runtime during shutdown do result in FailFast in VEH.
                //
                // 2) Only when the runtime is processing DLL_PROCESS_DETACH. 
                CLRRemoveVectoredHandlers();

#if USE_DISASSEMBLER
                Disassembler::StaticClose();
#endif // USE_DISASSEMBLER

#ifdef _DEBUG
                if (_DbgBreakCount)
                    _ASSERTE(!"EE Shutting down after an assert");
#endif


#ifdef LOGGING
                extern unsigned FcallTimeHist[11];
#endif
                LOG((LF_STUBS, LL_INFO10, "FcallHist %3d %3d %3d %3d %3d %3d %3d %3d %3d %3d %3d\n",
                    FcallTimeHist[0], FcallTimeHist[1], FcallTimeHist[2], FcallTimeHist[3],
                    FcallTimeHist[4], FcallTimeHist[5], FcallTimeHist[6], FcallTimeHist[7],
                    FcallTimeHist[8], FcallTimeHist[9], FcallTimeHist[10]));

                WriteJitHelperCountToSTRESSLOG();

                STRESS_LOG0(LF_STARTUP, LL_INFO10, "EEShutdown shutting down logging");

#if 0       // Dont clean up the stress log, so that even at process exit we have a log (after all the process is going away
                if (!g_fFastExitProcess)
                    StressLog::Terminate(TRUE);
#endif

                if (g_pConfig != NULL)
                    g_pConfig->Cleanup();

#ifdef LOGGING
                ShutdownLogging();
#endif
            }
        }    

    lDone: ;
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);

    ClrFlsClearThreadType(ThreadType_Shutdown);
    if (!g_fProcessDetach)
    {
        g_pEEShutDownEvent->Set();
    }
}


#ifdef FEATURE_COMINTEROP

BOOL IsThreadInSTA()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // If ole32.dll is not loaded
    if (WszGetModuleHandle(W("ole32.dll")) == NULL)
    {
        return FALSE;
    }

    BOOL fInSTA = TRUE;
    // To be conservative, check if finalizer thread is around
    EX_TRY
    {
        Thread *pFinalizerThread = FinalizerThread::GetFinalizerThread();
        if (!pFinalizerThread || pFinalizerThread->Join(0, FALSE) != WAIT_TIMEOUT)
        {
            fInSTA = FALSE;
        }
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions);

    if (!fInSTA)
    {
        return FALSE;
    }

    THDTYPE type;
    HRESULT hr = S_OK;

    hr = GetCurrentThreadTypeNT5(&type);
    if (hr == S_OK)
    {
        fInSTA = (type == THDTYPE_PROCESSMESSAGES) ? TRUE : FALSE;

        // If we get back THDTYPE_PROCESSMESSAGES, we are guaranteed to
        // be an STA thread. If not, we are an MTA thread, however
        // we can't know if the thread has been explicitly set to MTA
        // (via a call to CoInitializeEx) or if it has been implicitly
        // made MTA (if it hasn't been CoInitializeEx'd but CoInitialize
        // has already been called on some other thread in the process.
    }
    else
    {
        // CoInitialize hasn't been called in the process yet so assume the current thread
        // is MTA. 
        fInSTA = FALSE;
    }

    return fInSTA;
}
#endif

BOOL g_fWeOwnProcess = FALSE;

static LONG s_ActiveShutdownThreadCount = 0;

// ---------------------------------------------------------------------------
// Function: EEShutDownProcForSTAThread(LPVOID lpParameter)
//
// Parameters:
//    LPVOID lpParameter: unused
//
// Description:
//    When EEShutDown decides that the shut down logic must occur on another thread,
//    EEShutDown creates a new thread, and this function acts as the thread proc. See
//    code:#STAShutDown for details.
// 
DWORD WINAPI EEShutDownProcForSTAThread(LPVOID lpParameter)
{
    STATIC_CONTRACT_SO_INTOLERANT;;


    ClrFlsSetThreadType(ThreadType_ShutdownHelper);

    EEShutDownHelper(FALSE);
    for (int i = 0; i < 10; i ++)
    {
        if (s_ActiveShutdownThreadCount)
        {
            return 0;
        }
        __SwitchToThread(20, CALLER_LIMITS_SPINNING);
    }

    EPolicyAction action = GetEEPolicy()->GetDefaultAction(OPR_ProcessExit, NULL);
    if (action < eRudeExitProcess)
    {
        action = eRudeExitProcess;
    }
    UINT exitCode;
    if (g_fWeOwnProcess)
    {
        exitCode = GetLatchedExitCode();
    }
    else
    {
        exitCode = HOST_E_EXITPROCESS_TIMEOUT;
    }
    EEPolicy::HandleExitProcessFromEscalation(action, exitCode);

    return 0;
}

// ---------------------------------------------------------------------------
// #EEShutDown
// 
// Function: EEShutDown(BOOL fIsDllUnloading)
//
// Parameters:
//    BOOL fIsDllUnloading:
//         * TRUE => Called from CLR's DllMain (DLL_PROCESS_DETACH). Not safe point for
//             full cleanup
//         * FALSE => Called some other way (e.g., end of the CLR's main). Safe to do
//             full cleanup.
//
// Description:
// 
//     All ee shutdown stuff should be done here. EEShutDown is generally called in one
//     of two ways:
//     * 1. From code:EEPolicy::HandleExitProcess (via HandleExitProcessHelper), with
//         fIsDllUnloading == FALSE. This code path is typically invoked by the CLR's
//         main just falling through to the end. Full cleanup can be performed when
//         EEShutDown is called this way.
//     * 2. From CLR's DllMain (DLL_PROCESS_DETACH), with fIsDllUnloading == TRUE. When
//         called this way, much cleanup code is unsafe to run, and is thus skipped.
// 
// Actual shut down logic is factored out to EEShutDownHelper which may be called
// directly by EEShutDown, or indirectly on another thread (see code:#STAShutDown).
//
// In order that callees may also know the value of fIsDllUnloading, EEShutDownHelper
// sets g_fProcessDetach = fIsDllUnloading, and g_fProcessDetach may then be retrieved
// via code:IsAtProcessExit.
//
// NOTE 1: Actually, g_fProcessDetach is set to TRUE if fIsDllUnloading is TRUE. But
// g_fProcessDetach doesn't appear to be explicitly set to FALSE. (Apparently
// g_fProcessDetach is implicitly initialized to FALSE as clr.dll is loaded.)
// 
// NOTE 2: EEDllMain(DLL_PROCESS_DETACH) already sets g_fProcessDetach to TRUE, so it
// appears EEShutDownHelper doesn't have to.
// 
void STDMETHODCALLTYPE EEShutDown(BOOL fIsDllUnloading)
{
    CONTRACTL {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
        SO_TOLERANT; // we don't need to cleanup 'cus we're shutting down
        PRECONDITION(g_fEEStarted);
    } CONTRACTL_END;

    // If we have not started runtime successfully, it is not safe to call EEShutDown.
    if (!g_fEEStarted || g_fFastExitProcess == 2)
    {
        return;
    }

    // Stop stack probing and asserts right away.  Once we're shutting down, we can do no more.
    // And we don't want to SO-protect anything at this point anyway. This really only has impact
    // on a debug build.
    TerminateStackProbes();

    // The process is shutting down.  No need to check SO contract.
    SO_NOT_MAINLINE_FUNCTION;

    // We only do the first part of the shutdown once.
    static LONG OnlyOne = -1;

    if (!fIsDllUnloading)
    {
        if (FastInterlockIncrement(&OnlyOne) != 0)
        {
            // I'm in a regular shutdown -- but another thread got here first.
            // It's a race if I return from here -- I'll call ExitProcess next, and
            // rip things down while the first thread is half-way through a
            // nice cleanup.  Rather than do that, I should just wait until the
            // first thread calls ExitProcess().  I'll die a nice death when that
            // happens.
            GCX_PREEMP_NO_DTOR();
            WaitForEndOfShutdown();
            return;
        }

#ifdef FEATURE_MULTICOREJIT
        if (!AppX::IsAppXProcess()) // When running as Appx, make the delayed timer driven writing be the only option
        {
            MulticoreJitManager::StopProfileAll();
        }
#endif
    }

#ifdef FEATURE_COMINTEROP
    if (!fIsDllUnloading && IsThreadInSTA())
    {
        // #STAShutDown
        // 
        // During shutdown, we may need to release STA interface on the shutdown thread.
        // It is possible that the shutdown thread may deadlock. During shutdown, all
        // threads are blocked, except the shutdown thread and finalizer thread. If a
        // lock is held by one of these suspended threads, it can deadlock the process if
        // the shutdown thread tries to enter the lock. To mitigate this risk, create
        // another thread (B) to do shutdown activities (i.e., EEShutDownHelper), while
        // this thread (A) waits. If B deadlocks, A will time out and immediately return
        // from EEShutDown. A will then eventually call the OS's ExitProcess, which will
        // kill the deadlocked thread (and all other threads).
        // 
        // Many Windows Forms-based apps will also execute the code below to shift shut
        // down logic to a separate thread, even if they don't use COM objects. Reason
        // being that they will typically use a main UI thread to pump all Windows
        // messages (including messages that facilitate cross-thread COM calls to STA COM
        // objects), and will set that thread up as an STA thread just in case there are
        // such cross-thread COM calls to contend with. In fact, when you use VS's
        // File.New.Project to make a new Windows Forms project, VS will mark Main() with
        // [STAThread]
        DWORD thread_id = 0;
        if (CreateThread(NULL,0,EEShutDownProcForSTAThread,NULL,0,&thread_id))
        {
            GCX_PREEMP_NO_DTOR();

            ClrFlsSetThreadType(ThreadType_Shutdown);
            WaitForEndOfShutdown();
            FastInterlockIncrement(&s_ActiveShutdownThreadCount);
            ClrFlsClearThreadType(ThreadType_Shutdown);
        }
    }
    else
        // Otherwise, this thread calls EEShutDownHelper directly.  First switch to
        // cooperative mode if this is a managed thread
#endif
    if (GetThread())
    {
        GCX_COOP();
        EEShutDownHelper(fIsDllUnloading);
        if (!fIsDllUnloading)
        {
            FastInterlockIncrement(&s_ActiveShutdownThreadCount);
        }
    }
    else
    {
        EEShutDownHelper(fIsDllUnloading);
        if (!fIsDllUnloading)
        {
            FastInterlockIncrement(&s_ActiveShutdownThreadCount);
        }
    }
}

// ---------------------------------------------------------------------------
// %%Function: IsRuntimeActive()
//
// Parameters:
//  none
//
// Returns:
//  TRUE or FALSE
//
// Description: Indicates if the runtime is active or not. "Active" implies
//              that the runtime has started and is in a position to run
//              managed code. If either of these conditions are false, the
//              function return FALSE.
//          
//              Why couldnt we add !g_fEEStarted check in CanRunManagedCode?
//
// 
//              ExecuteDLL in ceemain.cpp could start the runtime 
//             (due to DLL_PROCESS_ATTACH) after invoking CanRunManagedCode. 
//              If the function were to be modified, then this scenario could fail. 
//              Hence, I have built over CanRunManagedCode in IsRuntimeActive.

// ---------------------------------------------------------------------------
BOOL IsRuntimeActive()
{
    // If the runtime has started AND we can run managed code,
    // then runtime is considered "active".
    BOOL fCanRunManagedCode = CanRunManagedCode();
    return (g_fEEStarted && fCanRunManagedCode);
}

// ---------------------------------------------------------------------------
// %%Function: CanRunManagedCode()
//
// Parameters:
//  none
//
// Returns:
//  true or false
//
// Description: Indicates if one is currently allowed to run managed code.
// ---------------------------------------------------------------------------
NOINLINE BOOL CanRunManagedCodeRare(LoaderLockCheck::kind checkKind, HINSTANCE hInst /*= 0*/)
{
    CONTRACTL {
        NOTHROW;
        if (checkKind == LoaderLockCheck::ForMDA) { GC_TRIGGERS; } else { GC_NOTRIGGER; }; // because of the CustomerDebugProbe
        MODE_ANY;
        SO_TOLERANT;
    } CONTRACTL_END;

    // If we are shutting down the runtime, then we cannot run code.
    if (g_fForbidEnterEE)
        return FALSE;

    // If pre-loaded objects are not present, then no way.
    if (g_pPreallocatedOutOfMemoryException == NULL)
        return FALSE;

    // If we are finaling live objects or processing ExitProcess event,
    // we can not allow managed method to run unless the current thread
    // is the finalizer thread
    if ((g_fEEShutDown & ShutDown_Finalize2) && !FinalizerThread::IsCurrentThreadFinalizer())
        return FALSE;

#if defined(FEATURE_COMINTEROP) && defined(MDA_SUPPORTED)
    if ((checkKind == LoaderLockCheck::ForMDA) && (NULL == MDA_GET_ASSISTANT(LoaderLock))) 
        return TRUE;

    if (checkKind == LoaderLockCheck::None)
        return TRUE;

    // If we are checking whether the OS loader lock is held by the current thread, then
    // it better not be.  Note that ShouldCheckLoaderLock is a cached test for whether
    // we are checking this probe.  So we can call AuxUlibIsDLLSynchronizationHeld before
    // verifying that the probe is still enabled.
    //
    // What's the difference between ignoreLoaderLock & ShouldCheckLoaderLock?
    // ShouldCheckLoaderLock is a process-wide flag.  In a few places where we
    // *know* we are in the loader lock but haven't quite reached the dangerous
    // point, we call CanRunManagedCode suppressing/deferring this check.
    BOOL IsHeld;

    if (ShouldCheckLoaderLock(FALSE) &&
        AuxUlibIsDLLSynchronizationHeld(&IsHeld) &&
        IsHeld)
    {
        if (checkKind == LoaderLockCheck::ForMDA)
        {
            MDA_TRIGGER_ASSISTANT(LoaderLock, ReportViolation(hInst));
        }
        else
        {
            return FALSE;
        }
    }
#endif // defined(FEATURE_COMINTEROP) && defined(MDA_SUPPORTED)

    return TRUE;
}

#include <optsmallperfcritical.h>
BOOL CanRunManagedCode(LoaderLockCheck::kind checkKind, HINSTANCE hInst /*= 0*/)
{
    CONTRACTL {
        NOTHROW;
        if (checkKind == LoaderLockCheck::ForMDA) { GC_TRIGGERS; } else { GC_NOTRIGGER; }; // because of the CustomerDebugProbe
        MODE_ANY;
        SO_TOLERANT;
    } CONTRACTL_END;

    // Special-case the common success cases
    //  (Try not to make any calls here so that we don't have to spill our incoming arg regs)
    if (!g_fForbidEnterEE 
        && (g_pPreallocatedOutOfMemoryException != NULL)
        && !(g_fEEShutDown & ShutDown_Finalize2)
        && (((checkKind == LoaderLockCheck::ForMDA) 
#ifdef MDA_SUPPORTED
             && (NULL == MDA_GET_ASSISTANT(LoaderLock))
#endif // MDA_SUPPORTED
            ) || (checkKind == LoaderLockCheck::None)))
    {
        return TRUE;
    }

    // Then call a helper for everything else.
    return CanRunManagedCodeRare(checkKind, hInst);
}
#include <optdefault.h>


// ---------------------------------------------------------------------------
// %%Function: CoInitializeEE(DWORD fFlags)
//
// Parameters:
//  fFlags                  - Initialization flags for the engine.  See the
//                              COINITIEE enumerator for valid values.
//
// Returns:
//  Nothing
//
// Description:
//  Initializes the EE if it hasn't already been initialized. This function
//  no longer maintains a ref count since the EE doesn't support being
//  unloaded and re-loaded. It simply ensures the EE has been started.
// ---------------------------------------------------------------------------
HRESULT STDAPICALLTYPE CoInitializeEE(DWORD fFlags)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
        SO_TOLERANT;
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;
    BEGIN_ENTRYPOINT_NOTHROW;
    hr = InitializeEE((COINITIEE)fFlags);
    END_ENTRYPOINT_NOTHROW;

    return hr;
}

// ---------------------------------------------------------------------------
// %%Function: CoUninitializeEE
//
// Parameters:
//  BOOL fIsDllUnloading :: is it safe point for full cleanup
//
// Returns:
//  Nothing
//
// Description:
//  Must be called by client on shut down in order to free up the system.
// ---------------------------------------------------------------------------
void STDAPICALLTYPE CoUninitializeEE(BOOL fIsDllUnloading)
{
    LIMITED_METHOD_CONTRACT;
    //BEGIN_ENTRYPOINT_VOIDRET;

    // This API is unfortunately publicly exported so we cannot get rid
    // of it. However since the EE doesn't currently support being unloaded
    // and re-loaded, it is useless to do any ref counting here or to pretend
    // to unload it. The proper way to shutdown the EE is to call CorExitProcess.
    //END_ENTRYPOINT_VOIDRET;

}

//*****************************************************************************
BOOL ExecuteDLL_ReturnOrThrow(HRESULT hr, BOOL fFromThunk)
{
    CONTRACTL {
        if (fFromThunk) THROWS; else NOTHROW;
        WRAPPER(GC_TRIGGERS);
        MODE_ANY;
        SO_TOLERANT;
    } CONTRACTL_END;

    // If we have a failure result, and we're called from a thunk,
    // then we need to throw an exception to communicate the error.
    if (FAILED(hr) && fFromThunk)
    {
        COMPlusThrowHR(hr);
    }
    return SUCCEEDED(hr);
}

//
// Initialize the Garbage Collector
//

// Prototype for the function that initialzes the garbage collector.
// Should only be called once: here, during EE startup.
// Returns true if the initialization was successful, false otherwise.
//
// When using a standalone GC, this function is loaded dynamically using
// GetProcAddress.
extern "C" bool InitializeGarbageCollector(IGCToCLR* clrToGC, IGCHeap** gcHeap, IGCHandleManager** gcHandleManager, GcDacVars* gcDacVars);

#ifdef FEATURE_STANDALONE_GC

void LoadGarbageCollector()
{
    CONTRACTL {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    TCHAR *standaloneGc = nullptr;
    CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_GCStandaloneLocation, &standaloneGc);
    HMODULE hMod;
    if (!standaloneGc)
    {
#ifdef FEATURE_STANDALONE_GC_ONLY
        // if the user has set GCUseStandalone but has not given us a standalone location,
        // try and load the initialization symbol from the current module.
        hMod = GetModuleInst();
#else
        ThrowHR(E_FAIL);
#endif // FEATURE_STANDALONE_GC_ONLY
    }
    else
    {
        hMod = CLRLoadLibrary(standaloneGc);
    }

    if (!hMod)
    {
        ThrowHR(E_FAIL);
    }

    InitializeGarbageCollectorFunction igcf = (InitializeGarbageCollectorFunction)GetProcAddress(hMod, INITIALIZE_GC_FUNCTION_NAME);
    if (!igcf)
    {
        ThrowHR(E_FAIL);
    }

    // at this point we are committing to using the standalone GC
    // given to us.
    IGCToCLR* gcToClr = new (nothrow) standalone::GCToEEInterface();
    if (!gcToClr)
    {
        ThrowOutOfMemory();
    }

    IGCHandleManager *pGcHandleManager;
    IGCHeap *pGCHeap;
    if (!igcf(gcToClr, &pGCHeap, &pGcHandleManager, &g_gc_dac_vars))
    {
        ThrowOutOfMemory();
    }

    assert(pGCHeap != nullptr);
    assert(pGcHandleManager != nullptr);
    g_pGCHeap = pGCHeap;
    g_pGCHandleManager = pGcHandleManager;
    g_gcDacGlobals = &g_gc_dac_vars;
}

#endif // FEATURE_STANDALONE_GC

#ifndef FEATURE_STANDALONE_GC_ONLY
void LoadStaticGarbageCollector()
{
    CONTRACTL{
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    IGCHandleManager *pGcHandleManager;
    IGCHeap *pGCHeap;

    if (!InitializeGarbageCollector(nullptr, &pGCHeap, &pGcHandleManager, &g_gc_dac_vars)) 
    {
        ThrowOutOfMemory();
    }

    assert(pGCHeap != nullptr);
    assert(pGcHandleManager != nullptr);
    g_pGCHeap = pGCHeap;
    g_pGCHandleManager = pGcHandleManager;
    g_gcDacGlobals = &g_gc_dac_vars;
}
#endif // FEATURE_STANDALONE_GC_ONLY


void InitializeGarbageCollector()
{
    CONTRACTL{
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    HRESULT hr;

    // Build the special Free Object used by the Generational GC
    _ASSERT(g_pFreeObjectMethodTable == NULL);
    g_pFreeObjectMethodTable = (MethodTable *) new BYTE[sizeof(MethodTable)];
    ZeroMemory(g_pFreeObjectMethodTable, sizeof(MethodTable));

    // As the flags in the method table indicate there are no pointers
    // in the object, there is no gc descriptor, and thus no need to adjust
    // the pointer to skip the gc descriptor.

    g_pFreeObjectMethodTable->SetBaseSize(ObjSizeOf (ArrayBase));
    g_pFreeObjectMethodTable->SetComponentSize(1);

#ifdef FEATURE_STANDALONE_GC
    if (CLRConfig::GetConfigValue(CLRConfig::EXTERNAL_GCUseStandalone)
#ifdef FEATURE_STANDALONE_GC_ONLY
        || true
#endif // FEATURE_STANDALONE_GC_ONLY
        )
    {
        LoadGarbageCollector();
    }
    else
#endif // FEATURE_STANDALONE_GC
    {
#ifndef FEATURE_STANDALONE_GC_ONLY
        LoadStaticGarbageCollector();
#endif // FEATURE_STANDALONE_GC_ONLY
    }

    // Apparently the Windows linker removes global variables if they are never
    // read from, which is a problem for g_gcDacGlobals since it's expected that
    // only the DAC will read from it. This forces the linker to include
    // g_gcDacGlobals.
    volatile void* _dummy = g_gcDacGlobals;
}

/*****************************************************************************/
/* This is here only so that if we get an exception we stop before we catch it */
LONG DllMainFilter(PEXCEPTION_POINTERS p, PVOID pv)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(!"Exception happened in mscorwks!DllMain!");
    return EXCEPTION_EXECUTE_HANDLER;
}

//*****************************************************************************
// This is the part of the old-style DllMain that initializes the
// stuff that the EE team works on. It's called from the real DllMain
// up in MSCOREE land. Separating the DllMain tasks is simply for
// convenience due to the dual build trees.
//*****************************************************************************
BOOL STDMETHODCALLTYPE EEDllMain( // TRUE on success, FALSE on error.
    HINSTANCE   hInst,             // Instance handle of the loaded module.
    DWORD       dwReason,          // Reason for loading.
    LPVOID      lpReserved)        // Unused.
{
    STATIC_CONTRACT_NOTHROW;
    STATIC_CONTRACT_GC_TRIGGERS;

    // this runs at the top of a thread, SO is not a concern here...
    STATIC_CONTRACT_SO_NOT_MAINLINE;


    // HRESULT hr;
    // BEGIN_EXTERNAL_ENTRYPOINT(&hr);
    // EE isn't spun up enough to use this macro

    struct Param
    {
        HINSTANCE hInst;
        DWORD dwReason;
        LPVOID lpReserved;
        void **pTlsData;
    } param;
    param.hInst = hInst;
    param.dwReason = dwReason;
    param.lpReserved = lpReserved;
    param.pTlsData = NULL;

    // Can't use PAL_TRY/EX_TRY here as they access the ClrDebugState which gets blown away as part of the
    // PROCESS_DETACH path. Must use special PAL_TRY_FOR_DLLMAIN, passing the reason were in the DllMain.
    PAL_TRY_FOR_DLLMAIN(Param *, pParam, &param, pParam->dwReason)
    {

    switch (pParam->dwReason)
        {
            case DLL_PROCESS_ATTACH:
            {
                // We cache the SystemInfo for anyone to use throughout the
                // life of the DLL.
                GetSystemInfo(&g_SystemInfo);

                // Remember module instance
                g_pMSCorEE = pParam->hInst;


                // Set callbacks so that LoadStringRC knows which language our
                // threads are in so that it can return the proper localized string.
            // TODO: This shouldn't rely on the LCID (id), but only the name
                SetResourceCultureCallbacks(GetThreadUICultureNames,
                                            GetThreadUICultureId);

                InitEEPolicy();

                break;
            }

            case DLL_PROCESS_DETACH:
            {
                // lpReserved is NULL if we're here because someone called FreeLibrary
                // and non-null if we're here because the process is exiting.
                // Since nobody should ever be calling FreeLibrary on mscorwks.dll, lpReserved
                // should always be non NULL.
                _ASSERTE(pParam->lpReserved || !g_fEEStarted);
                g_fProcessDetach = TRUE;

#if defined(ENABLE_CONTRACTS_IMPL) && defined(FEATURE_STACK_PROBE)
                // We are shutting down process.  No need to check SO contract.
                // And it is impossible to enforce SO contract in global dtor, like ModIntPairList.
                g_EnableDefaultRWValidation = FALSE;
#endif

                if (g_fEEStarted)
                {
                    // GetThread() may be set to NULL for Win9x during shutdown.
                    Thread *pThread = GetThread();
                    if (GCHeapUtilities::IsGCInProgress() &&
                        ( (pThread && (pThread != ThreadSuspend::GetSuspensionThread() ))
                            || !g_fSuspendOnShutdown))
                    {
                        g_fEEShutDown |= ShutDown_Phase2;
                        break;
                    }

                    LOG((LF_STARTUP, INFO3, "EEShutDown invoked from EEDllMain"));
                    EEShutDown(TRUE); // shut down EE if it was started up
                }
                else
                {
                    CLRRemoveVectoredHandlers();
                }
                break;
            }

            case DLL_THREAD_DETACH:
            {
                // Don't destroy threads here if we're in shutdown (shutdown will
                // clean up for us instead).

                // Store the TLS data; we'll need it later and we might NULL the slot in DetachThread.
                // This would be problematic because we can't depend on the FLS still existing.
                pParam->pTlsData = CExecutionEngine::CheckThreadStateNoCreate(0
#ifdef _DEBUG
                 // When we get here, OS has destroyed FLS, so FlsGetValue returns NULL now.
                 // We have validation code in CExecutionEngine::CheckThreadStateNoCreate to ensure that
                 // our TLS and FLS data are consistent, but since FLS has been destroyed, we need
                 // to silent the check there.  The extra arg for check build is for this purpose.
                                                                                         , TRUE
#endif
                                                                                         );
                Thread* thread = GetThread();
                if (thread)
                {
#ifdef FEATURE_COMINTEROP
                    // reset the CoInitialize state
                    // so we don't call CoUninitialize during thread detach
                    thread->ResetCoInitialized();
#endif // FEATURE_COMINTEROP
                    // For case where thread calls ExitThread directly, we need to reset the
                    // frame pointer. Otherwise stackwalk would AV. We need to do it in cooperative mode.
                    // We need to set m_GCOnTransitionsOK so this thread won't trigger GC when toggle GC mode
                    if (thread->m_pFrame != FRAME_TOP)
                    {
#ifdef _DEBUG
                        thread->m_GCOnTransitionsOK = FALSE;
#endif
                        GCX_COOP_NO_DTOR();
                        thread->m_pFrame = FRAME_TOP;
                        GCX_COOP_NO_DTOR_END();
                    }
                    thread->DetachThread(TRUE);
                }
            }
        }

    }
    PAL_EXCEPT_FILTER(DllMainFilter)
    {
    }
    PAL_ENDTRY;

    if (dwReason == DLL_THREAD_DETACH || dwReason == DLL_PROCESS_DETACH)
    {
        CExecutionEngine::ThreadDetaching(param.pTlsData);
    }
    return TRUE;
}


#ifdef FEATURE_IPCMAN
extern CCLRSecurityAttributeManager s_CLRSecurityAttributeManager;
#endif // FEATURE_IPCMAN


#ifdef DEBUGGING_SUPPORTED
//
// InitializeDebugger initialized the Runtime-side COM+ Debugging Services
//
static void InitializeDebugger(void)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Ensure that if we throw, we'll call TerminateDebugger to cleanup.
    // This makes our Init more atomic by avoiding partially-init states.
    class EnsureCleanup {
        BOOL    fNeedCleanup;
    public:
        EnsureCleanup()
        {
            fNeedCleanup = TRUE;
        }

        void SuppressCleanup()
        {
            fNeedCleanup  = FALSE;
        }

        ~EnsureCleanup()
        {
             STATIC_CONTRACT_NOTHROW;
             STATIC_CONTRACT_GC_NOTRIGGER;
             STATIC_CONTRACT_MODE_ANY;

            if (fNeedCleanup) 
            { 
                TerminateDebugger();
            }
        }
    } hCleanup;

    HRESULT hr = S_OK;

    LOG((LF_CORDB, LL_INFO10, "Initializing left-side debugging services.\n"));

    FARPROC gi = (FARPROC) &CorDBGetInterface;

    // Init the interface the EE provides to the debugger,
    // ask the debugger for its interface, and if all goes
    // well call Startup on the debugger.
    EEDbgInterfaceImpl::Init();
    _ASSERTE(g_pEEDbgInterfaceImpl != NULL); // throws on OOM

    // This allocates the Debugger object.
    typedef HRESULT __cdecl CORDBGETINTERFACE(DebugInterface**);
    hr = ((CORDBGETINTERFACE*)gi)(&g_pDebugInterface);
    IfFailThrow(hr);

    g_pDebugInterface->SetEEInterface(g_pEEDbgInterfaceImpl);

    {
        hr = g_pDebugInterface->Startup(); // throw on error
        _ASSERTE(SUCCEEDED(hr));

        // 
        // If the debug pack is not installed, Startup will return S_FALSE
        // and we should cleanup and proceed without debugging support.
        //
        if (hr != S_OK)
        {
            return;
        }
    }


    LOG((LF_CORDB, LL_INFO10, "Left-side debugging services setup.\n"));

    hCleanup.SuppressCleanup();

    return;
}


//
// TerminateDebugger shuts down the Runtime-side COM+ Debugging Services
// InitializeDebugger will call this if it fails.
// This may be called even if the debugger is partially initialized.
// This can be called multiple times.
//
static void TerminateDebugger(void)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    LOG((LF_CORDB, LL_INFO10, "Shutting down left-side debugger services.\n"));

    // If initialized failed really early, then we didn't even get the Debugger object.
    if (g_pDebugInterface != NULL)
    {
        // Notify the out-of-process debugger that shutdown of the in-process debugging support has begun. This is only
        // really used in interop debugging scenarios.
        g_pDebugInterface->ShutdownBegun();

        // This will kill the helper thread, delete the Debugger object, and free all resources.
        g_pDebugInterface->StopDebugger();
    }

    g_CORDebuggerControlFlags = DBCF_NORMAL_OPERATION;

}


#ifdef FEATURE_IPCMAN
// ---------------------------------------------------------------------------
// Initialize InterProcess Communications for COM+
// 1. Allocate an IPCManager Implementation and hook it up to our interface *
// 2. Call proper init functions to activate relevant portions of IPC block
// ---------------------------------------------------------------------------
static HRESULT InitializeIPCManager(void)
{
    CONTRACTL{
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    } CONTRACTL_END;

    HRESULT hr = S_OK;
    HINSTANCE hInstIPCBlockOwner = 0;

    DWORD pid = 0;
    // Allocate the Implementation. Everyone else will work through the interface
    g_pIPCManagerInterface = new (nothrow) IPCWriterInterface();

    if (g_pIPCManagerInterface == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto errExit;
    }

    pid = GetCurrentProcessId();


    // Do general init
    hr = g_pIPCManagerInterface->Init();

    if (!SUCCEEDED(hr))
    {
        goto errExit;
    }

    // Generate private IPCBlock for our PID. Note that for the other side of the debugger,
    // they'll hook up to the debuggee's pid (and not their own). So we still
    // have to pass the PID in.
    EX_TRY
    {
        // <TODO>This should go away in the future.</TODO>
        hr = g_pIPCManagerInterface->CreateLegacyPrivateBlockTempV4OnPid(pid, FALSE, &hInstIPCBlockOwner);
    }
    EX_CATCH_HRESULT(hr);

    if (hr == HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS))
    {
        // We failed to create the IPC block because it has already been created. This means that
        // two mscoree's have been loaded into the process.
        PathString strFirstModule;
        PathString strSecondModule;
        EX_TRY
        {
            // Get the name and path of the first loaded MSCOREE.DLL.
            if (!hInstIPCBlockOwner || !WszGetModuleFileName(hInstIPCBlockOwner, strFirstModule))
                strFirstModule.Set(W("<Unknown>"));

            // Get the name and path of the second loaded MSCOREE.DLL.
            if (!WszGetModuleFileName(g_pMSCorEE, strSecondModule))
               strSecondModule.Set(W("<Unknown>"));
        }
        EX_CATCH_HRESULT(hr);
        // Load the format strings for the title and the message body.
        EEMessageBoxCatastrophic(IDS_EE_TWO_LOADED_MSCOREE_MSG, IDS_EE_TWO_LOADED_MSCOREE_TITLE, strFirstModule, strSecondModule);
        goto errExit;
    }
    else
    {
        PathString temp;
        if (!WszGetModuleFileName(GetModuleInst(),
                                  temp
                                  ))
        {
            hr = HRESULT_FROM_GetLastErrorNA();
        }
        else
        {
            EX_TRY
            {
                if (temp.GetCount() + 1 > MAX_LONGPATH)
                {
                    hr = E_FAIL;
                }
                else
                {
                    wcscpy_s((PWSTR)g_pIPCManagerInterface->GetInstancePath(),temp.GetCount() + 1,temp);
                }
            }
            EX_CATCH_HRESULT(hr);
        }
    }

    // Generate public IPCBlock for our PID.
    EX_TRY
    {
        hr = g_pIPCManagerInterface->CreateSxSPublicBlockOnPid(pid);
    }
    EX_CATCH_HRESULT(hr);


errExit:
    // If any failure, shut everything down.
    if (!SUCCEEDED(hr))
        TerminateIPCManager();

    return hr;
}
#endif // FEATURE_IPCMAN

#endif // DEBUGGING_SUPPORTED


// ---------------------------------------------------------------------------
// Marks the IPC block as initialized so that other processes know that the
// block is safe to read
// ---------------------------------------------------------------------------
#ifdef FEATURE_IPCMAN
static void PublishIPCManager(void)
{
    CONTRACTL{
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    if (g_pIPCManagerInterface != NULL)
        g_pIPCManagerInterface->Publish();
}
#endif // FEATURE_IPCMAN



#ifdef FEATURE_IPCMAN
// ---------------------------------------------------------------------------
// Terminate all InterProcess operations
// ---------------------------------------------------------------------------
static void TerminateIPCManager(void)
{
    CONTRACTL{
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    } CONTRACTL_END;

    if (g_pIPCManagerInterface != NULL)
    {
        g_pIPCManagerInterface->Terminate();
        delete g_pIPCManagerInterface;
        g_pIPCManagerInterface = NULL;
    }

}
#endif // FEATURE_IPCMAN

#ifndef LOCALE_SPARENT
#define LOCALE_SPARENT 0x0000006d
#endif

// ---------------------------------------------------------------------------
// Impl for UtilLoadStringRC Callback: In VM, we let the thread decide culture
// copy culture name into szBuffer and return length
// ---------------------------------------------------------------------------
static HRESULT GetThreadUICultureNames(__inout StringArrayList* pCultureNames)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pCultureNames));
        SO_INTOLERANT;
    } 
    CONTRACTL_END;

    HRESULT hr = S_OK;

    EX_TRY
    {
        InlineSString<LOCALE_NAME_MAX_LENGTH> sCulture;
        InlineSString<LOCALE_NAME_MAX_LENGTH> sParentCulture;


        Thread * pThread = GetThread();

        if (pThread != NULL) {

            // Switch to cooperative mode, since we'll be looking at managed objects
            // and we don't want them moving on us.
            GCX_COOP();

            THREADBASEREF pThreadBase = (THREADBASEREF)pThread->GetExposedObjectRaw();

            if (pThreadBase != NULL)
            {
                CULTUREINFOBASEREF pCurrentCulture = pThreadBase->GetCurrentUICulture();

                if (pCurrentCulture != NULL)
                {
                    STRINGREF cultureName = pCurrentCulture->GetName();

                    if (cultureName != NULL)
                    {
                        sCulture.Set(cultureName->GetBuffer(),cultureName->GetStringLength());
                    }

                    CULTUREINFOBASEREF pParentCulture = pCurrentCulture->GetParent();

                    if (pParentCulture != NULL)
                    {
                        STRINGREF parentCultureName = pParentCulture->GetName();

                        if (parentCultureName != NULL)
                        {
                            sParentCulture.Set(parentCultureName->GetBuffer(),parentCultureName->GetStringLength());
                        }

                    }
                }
            }
        }
        // If the lazily-initialized cultureinfo structures aren't initialized yet, we'll
        // need to do the lookup the hard way.
        if (sCulture.IsEmpty() || sParentCulture.IsEmpty())
        {
            LocaleIDValue id ;
            int tmp; tmp = GetThreadUICultureId(&id);   // TODO: We should use the name instead
            _ASSERTE(tmp!=0 && id != UICULTUREID_DONTCARE);
            SIZE_T cchParentCultureName=LOCALE_NAME_MAX_LENGTH;
#ifdef FEATURE_USE_LCID 
            SIZE_T cchCultureName=LOCALE_NAME_MAX_LENGTH;
            if (!NewApis::LCIDToLocaleName(id, sCulture.OpenUnicodeBuffer(static_cast<COUNT_T>(cchCultureName)), static_cast<int>(cchCultureName), 0))
            {
                hr = HRESULT_FROM_GetLastError();
            }
            sCulture.CloseBuffer();
#else
            sCulture.Set(id);
#endif

#ifndef FEATURE_PAL
            if (!NewApis::GetLocaleInfoEx((LPCWSTR)sCulture, LOCALE_SPARENT, sParentCulture.OpenUnicodeBuffer(static_cast<COUNT_T>(cchParentCultureName)),static_cast<int>(cchParentCultureName)))
            {
                hr = HRESULT_FROM_GetLastError();
            }
            sParentCulture.CloseBuffer();
#else // !FEATURE_PAL            
            sParentCulture = sCulture;
#endif // !FEATURE_PAL            
        }
        // (LPCWSTR) to restrict the size to null terminated size 
        pCultureNames->AppendIfNotThere((LPCWSTR)sCulture);
        // Disabling for Dev10 for consistency with managed resource lookup (see AppCompat bug notes in ResourceFallbackManager.cs)
        // Also, this is in the wrong order - put after the parent culture chain.
        //AddThreadPreferredUILanguages(pCultureNames);
        pCultureNames->AppendIfNotThere((LPCWSTR)sParentCulture);
        pCultureNames->Append(SString::Empty());
    }
    EX_CATCH
    {
        hr=E_OUTOFMEMORY;
    }
    EX_END_CATCH(SwallowAllExceptions);

    return hr;
}

// The exit code for the process is communicated in one of two ways.  If the
// entrypoint returns an 'int' we take that.  Otherwise we take a latched
// process exit code.  This can be modified by the app via System.SetExitCode().
static INT32 LatchedExitCode;
    
void SetLatchedExitCode (INT32 code)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;

    STRESS_LOG1(LF_SYNC, LL_INFO10, "SetLatchedExitCode = %d\n", code);
    LatchedExitCode = code;
}

INT32 GetLatchedExitCode (void)
{
    LIMITED_METHOD_CONTRACT;
    return LatchedExitCode;
}

    
// ---------------------------------------------------------------------------
// Impl for UtilLoadStringRC Callback: In VM, we let the thread decide culture
// Return an int uniquely describing which language this thread is using for ui.
// ---------------------------------------------------------------------------
// TODO: Callers should use names, not LCIDs
#ifdef FEATURE_USE_LCID
static int GetThreadUICultureId(__out LocaleIDValue* pLocale)
{
    CONTRACTL{
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_INTOLERANT;;
    } CONTRACTL_END;



    int Result = UICULTUREID_DONTCARE;

    Thread * pThread = GetThread();

    if (pThread != NULL) {

        // Switch to cooperative mode, since we'll be looking at managed objects
        // and we don't want them moving on us.
        GCX_COOP();

        THREADBASEREF pThreadBase = (THREADBASEREF)pThread->GetExposedObjectRaw();
        if (pThreadBase != NULL)
        {
            CULTUREINFOBASEREF pCurrentCulture = pThreadBase->GetCurrentUICulture();

            if (pCurrentCulture != NULL)
            {
                STRINGREF cultureName = pCurrentCulture->GetName();
                _ASSERT(cultureName != NULL);

                if ((Result = NewApis::LocaleNameToLCID(cultureName->GetBuffer(), 0)) == 0)
                    Result = (int)UICULTUREID_DONTCARE;
            }
        }
    }

    if (Result == (int)UICULTUREID_DONTCARE)
    {
        // This thread isn't set up to use a non-default culture. Let's grab the default
        // one and return that.

        Result = COMNlsInfo::CallGetUserDefaultUILanguage();

        if (Result == 0 || Result == (int)UICULTUREID_DONTCARE)
            Result = GetUserDefaultLangID();

        _ASSERTE(Result != 0);
        if (Result == 0)
        {
            Result = (int)UICULTUREID_DONTCARE;
        }

    }
    *pLocale=Result;
    return Result;
}
#else
// TODO: Callers should use names, not LCIDs
static int GetThreadUICultureId(__out LocaleIDValue* pLocale)
{
    CONTRACTL{
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        SO_INTOLERANT;;
    } CONTRACTL_END;

    _ASSERTE(sizeof(LocaleIDValue)/sizeof(WCHAR) >= LOCALE_NAME_MAX_LENGTH);

    int Result = 0;

    Thread * pThread = GetThread();

    if (pThread != NULL) {

        // Switch to cooperative mode, since we'll be looking at managed objects
        // and we don't want them moving on us.
        GCX_COOP();

        THREADBASEREF pThreadBase = (THREADBASEREF)pThread->GetExposedObjectRaw();
        if (pThreadBase != NULL)
        {
            CULTUREINFOBASEREF pCurrentCulture = pThreadBase->GetCurrentUICulture();

            if (pCurrentCulture != NULL)
            {
                STRINGREF currentCultureName = pCurrentCulture->GetName();

                if (currentCultureName != NULL)
                {
                    int cchCurrentCultureNameResult = currentCultureName->GetStringLength();
                    if (cchCurrentCultureNameResult < LOCALE_NAME_MAX_LENGTH)
                    {
                        memcpy(*pLocale, currentCultureName->GetBuffer(), cchCurrentCultureNameResult*sizeof(WCHAR));
                        (*pLocale)[cchCurrentCultureNameResult]='\0';
                        Result=cchCurrentCultureNameResult;
                    }
                }

            }
        }
    }
    if (Result == 0)
    {
#ifndef FEATURE_PAL
        // This thread isn't set up to use a non-default culture. Let's grab the default
        // one and return that.

        Result = NewApis::GetUserDefaultLocaleName(*pLocale, LOCALE_NAME_MAX_LENGTH);

        _ASSERTE(Result != 0);
#else // !FEATURE_PAL
        static const WCHAR enUS[] = W("en-US");
        memcpy(*pLocale, enUS, sizeof(enUS));
        Result = sizeof(enUS);
#endif // !FEATURE_PAL
    }
    return Result;
}

#endif // FEATURE_USE_LCID
// ---------------------------------------------------------------------------
// Export shared logging code for JIT, et.al.
// ---------------------------------------------------------------------------
#ifdef _DEBUG

extern VOID LogAssert( LPCSTR szFile, int iLine, LPCSTR expr);
extern "C"
//__declspec(dllexport)
VOID STDMETHODCALLTYPE LogHelp_LogAssert( LPCSTR szFile, int iLine, LPCSTR expr)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        ENTRY_POINT;            
        PRECONDITION(CheckPointer(szFile));
        PRECONDITION(CheckPointer(expr));
    }  CONTRACTL_END;

    BEGIN_ENTRYPOINT_VOIDRET;
    LogAssert(szFile, iLine, expr);
    END_ENTRYPOINT_VOIDRET;

}

extern "C"
//__declspec(dllexport)
BOOL STDMETHODCALLTYPE LogHelp_NoGuiOnAssert()
{
    LIMITED_METHOD_CONTRACT;
    BOOL fRet = FALSE;
    BEGIN_ENTRYPOINT_VOIDRET;
    fRet = NoGuiOnAssert();
    END_ENTRYPOINT_VOIDRET;
    return fRet;
}

extern "C"
//__declspec(dllexport)
VOID STDMETHODCALLTYPE LogHelp_TerminateOnAssert()
{
    LIMITED_METHOD_CONTRACT;
    BEGIN_ENTRYPOINT_VOIDRET;
//  __asm int 3;
    TerminateOnAssert();
    END_ENTRYPOINT_VOIDRET;

}

#else // !_DEBUG

extern "C"
//__declspec(dllexport)
VOID STDMETHODCALLTYPE LogHelp_LogAssert( LPCSTR szFile, int iLine, LPCSTR expr) {
    LIMITED_METHOD_CONTRACT;

    //BEGIN_ENTRYPOINT_VOIDRET;
    //END_ENTRYPOINT_VOIDRET;
}

extern "C"
//__declspec(dllexport)
BOOL STDMETHODCALLTYPE LogHelp_NoGuiOnAssert() {
    LIMITED_METHOD_CONTRACT;

    //BEGIN_ENTRYPOINT_VOIDRET;
    //END_ENTRYPOINT_VOIDRET;

    return FALSE;
}

extern "C"
//__declspec(dllexport)
VOID STDMETHODCALLTYPE LogHelp_TerminateOnAssert() {
    LIMITED_METHOD_CONTRACT;

    //BEGIN_ENTRYPOINT_VOIDRET;
    //END_ENTRYPOINT_VOIDRET;

}

#endif // _DEBUG


#ifndef ENABLE_PERF_COUNTERS
//
// perf counter stubs for builds which don't have perf counter support
// These are needed because we export these functions in our DLL


Perf_Contexts* STDMETHODCALLTYPE GetPrivateContextsPerfCounters()
{
    LIMITED_METHOD_CONTRACT;

    //BEGIN_ENTRYPOINT_VOIDRET;
    //END_ENTRYPOINT_VOIDRET;

    return NULL;
}

#endif


#ifdef ENABLE_CONTRACTS_IMPL

// Returns TRUE if any contract violation suppressions are in effect.
BOOL AreAnyViolationBitsOn()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        SO_TOLERANT;
        MODE_ANY;
    }
    CONTRACTL_END;
    UINT_PTR violationMask = GetClrDebugState()->ViolationMask();
    violationMask &= ~((UINT_PTR)CanFreeMe);  //CanFreeMe is a borrowed bit and has nothing to do with violations
    if (violationMask & ((UINT_PTR)BadDebugState))
    {
        return FALSE;
    }

    return violationMask != 0;
}


// This function is intentionally invoked inside a big CONTRACT_VIOLATION that turns on every violation
// bit on the map. The dynamic contract at the beginning *should* turn off those violation bits. 
// The body of this function tests to see that it did exactly that. This is to prevent the VSWhidbey B#564831 fiasco
// from ever recurring.
void ContractRegressionCheckInner()
{
    // DO NOT TURN THIS CONTRACT INTO A STATIC CONTRACT!!! The very purpose of this function
    // is to ensure that dynamic contracts disable outstanding contract violation bits.
    // This code only runs once at process startup so it's not going pooch the checked build perf.
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        FORBID_FAULT;
        LOADS_TYPE(CLASS_LOAD_BEGIN);
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END

    if (AreAnyViolationBitsOn())
    {
        // If we got here, the contract above FAILED to turn off one or more violation bits. This is a 
        // huge diagnostics hole and must be fixed immediately.
        _ASSERTE(!("WARNING: mscorwks has detected an internal error that may indicate contracts are"
                   " being silently disabled across the runtime. Do not ignore this assert!"));
    }
}

// This function executes once per process to ensure our CONTRACT_VIOLATION() mechanism 
// is properly scope-limited by nested contracts.
void ContractRegressionCheck()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;
 
    {
        // DO NOT "FIX" THIS CONTRACT_VIOLATION!!!
        // The existence of this CONTRACT_VIOLATION is not a bug. This is debug-only code specifically written
        // to test the CONTRACT_VIOLATION mechanism itself. This is needed to prevent a regression of 
        // B#564831 (which left a huge swath of contracts silently disabled for over six months)
        PERMANENT_CONTRACT_VIOLATION(ThrowsViolation
                                   | GCViolation
                                   | FaultViolation
                                   | LoadsTypeViolation
                                   | TakesLockViolation
                                   , ReasonContractInfrastructure
                                    );
        {
            FAULT_NOT_FATAL();
            ContractRegressionCheckInner();
        }
    }

    if (AreAnyViolationBitsOn())
    {
        // If we got here, the CONTRACT_VIOLATION() holder left one or more violation bits turned ON
        // after we left its scope. This is a huge diagnostic hole and must be fixed immediately.
        _ASSERTE(!("WARNING: mscorwks has detected an internal error that may indicate contracts are"
                   " being silently disabled across the runtime. Do not ignore this assert!"));
    }

}

#endif // ENABLE_CONTRACTS_IMPL

#endif // CROSSGEN_COMPILE
