// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: TieredCompilation.CPP
//
// ===========================================================================



#include "common.h"
#include "excep.h"
#include "log.h"
#include "win32threadpool.h"
#include "threadsuspend.h"
#include "tieredcompilation.h"

// TieredCompilationManager determines which methods should be recompiled and
// how they should be recompiled to best optimize the running code. It then
// handles logistics of getting new code created and installed.
//
//
// # Current feature state
//
// This feature is incomplete and currently experimental. To enable it
// you need to set COMPLUS_EXPERIMENTAL_TieredCompilation = 1. When the environment
// variable is unset the runtime should work as normal, but when it is set there are 
// anticipated incompatibilities and limited cross cutting test coverage so far.
//   Profiler - Anticipated incompatible with ReJIT, untested in general
//   ETW - Anticipated incompatible with the ReJIT id of the MethodJitted rundown events
//   Managed debugging - Anticipated incompatible with breakpoints/stepping that are
//                       active when a method is recompiled.
//   
//
// Testing that has been done so far largely consists of regression testing with
// the environment variable off + functional/perf testing of the Music Store ASP.Net
// workload as a basic example that the feature can work. Running the coreclr repo
// tests with the env var on generates about a dozen failures in JIT tests. The issues
// are likely related to assertions about optimization behavior but haven't been
// properly investigated yet.
//
// If you decide to try this out on a new workload and run into trouble a quick note
// on github is appreciated but this code may have high churn for a while to come and
// there will be no sense investing a lot of time investigating only to have it rendered 
// moot by changes. I aim to keep this comment updated as things change.
//
//
// # Important entrypoints in this code:
//
// 
// a) .ctor and Init(...) -  called once during AppDomain initialization
// b) OnMethodCalled(...) -  called when a method is being invoked. When a method
//                           has been called enough times this is currently the only
//                           trigger that initiates re-compilation.
// c) Shutdown() -           called during AppDomain::Exit() to begin the process
//                           of stopping tiered compilation. After this point no more
//                           background optimization work will be initiated but in-progress
//                           work still needs to complete.
// d) ShutdownAllDomains() - Called from EEShutdownHelper to block until all async work is
//                           complete. We must do this before we shutdown the JIT.
//
// # Overall workflow
//
// Methods initially call into OnMethodCalled() and once the call count exceeds
// a fixed limit we queue work on to our internal list of methods needing to
// be recompiled (m_methodsToOptimize). If there is currently no thread
// servicing our queue asynchronously then we use the runtime threadpool
// QueueUserWorkItem to recruit one. During the callback for each threadpool work
// item we handle as many methods as possible in a fixed period of time, then
// queue another threadpool work item if m_methodsToOptimize hasn't been drained.
//
// The background thread enters at StaticOptimizeMethodsCallback(), enters the
// appdomain, and then begins calling OptimizeMethod on each method in the
// queue. For each method we jit it, then update the precode so that future
// entrypoint callers will run the new code.
// 
// # Error handling
//
// The overall principle is don't swallow terminal failures that may have corrupted the
// process (AV for example), but otherwise for any transient issue or functional limitation
// that prevents us from optimizing log it for diagnostics and then back out gracefully,
// continuing to run the less optimal code. The feature should be constructed so that
// errors are limited to OS resource exhaustion or poorly behaved managed code
// (for example within an AssemblyResolve event or static constructor triggered by the JIT).

#ifdef FEATURE_TIERED_COMPILATION

// Called at AppDomain construction
TieredCompilationManager::TieredCompilationManager() :
    m_isAppDomainShuttingDown(FALSE),
    m_countOptimizationThreadsRunning(0),
    m_callCountOptimizationThreshhold(30),
    m_optimizationQuantumMs(50)
{
    LIMITED_METHOD_CONTRACT;
    m_lock.Init(LOCK_TYPE_DEFAULT);
}

// Called at AppDomain Init
void TieredCompilationManager::Init(ADID appDomainId)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CAN_TAKE_LOCK;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    SpinLockHolder holder(&m_lock);
    m_domainId = appDomainId;
    m_asyncWorkDoneEvent.CreateManualEvent(TRUE);
}

// Called each time code in this AppDomain has been run. This is our sole entrypoint to begin
// tiered compilation for now. Returns TRUE if no more notifications are necessary, but
// more notifications may come anyways.
//
// currentCallCount is pre-incremented, that is to say the value is 1 on first call for a given
//      method.
BOOL TieredCompilationManager::OnMethodCalled(MethodDesc* pMethodDesc, DWORD currentCallCount)
{
    STANDARD_VM_CONTRACT;

    if (currentCallCount < m_callCountOptimizationThreshhold)
    {
        return FALSE; // continue notifications for this method
    }
    else if (currentCallCount > m_callCountOptimizationThreshhold)
    {
        return TRUE; // stop notifications for this method
    }
    AsyncPromoteMethodToTier1(pMethodDesc);
    return TRUE;
}

void TieredCompilationManager::AsyncPromoteMethodToTier1(MethodDesc* pMethodDesc)
{
    STANDARD_VM_CONTRACT;

    NativeCodeVersion t1NativeCodeVersion;

    // Add an inactive native code entry in the versioning table to track the tier1 
    // compilation we are going to create. This entry binds the compilation to a
    // particular version of the IL code regardless of any changes that may
    // occur between now and when jitting completes. If the IL does change in that
    // interval the new code entry won't be activated.
    {
        CodeVersionManager* pCodeVersionManager = pMethodDesc->GetCodeVersionManager();
        CodeVersionManager::TableLockHolder lock(pCodeVersionManager);
        ILCodeVersion ilVersion = pCodeVersionManager->GetActiveILCodeVersion(pMethodDesc);
        NativeCodeVersionCollection nativeVersions = ilVersion.GetNativeCodeVersions(pMethodDesc);
        for (NativeCodeVersionIterator cur = nativeVersions.Begin(), end = nativeVersions.End(); cur != end; cur++)
        {
            if (cur->GetOptimizationTier() == NativeCodeVersion::OptimizationTier1)
            {
                // we've already promoted
                return;
            }
        }

        if (FAILED(ilVersion.AddNativeCodeVersion(pMethodDesc, &t1NativeCodeVersion)))
        {
            // optimization didn't work for some reason (presumably OOM)
            // just give up and continue on
            return;
        }
        t1NativeCodeVersion.SetOptimizationTier(NativeCodeVersion::OptimizationTier1);
    }

    // Insert the method into the optimization queue and trigger a thread to service
    // the queue if needed.
    //
    // Terminal exceptions escape as exceptions, but all other errors should gracefully
    // return to the caller. Non-terminal error conditions should be rare (ie OOM,
    // OS failure to create thread) and we consider it reasonable for some methods
    // to go unoptimized or have their optimization arbitrarily delayed under these
    // circumstances. Note an error here could affect concurrent threads running this
    // code. Those threads will observe m_countOptimizationThreadsRunning > 0 and return,
    // then QueueUserWorkItem fails on this thread lowering the count and leaves them 
    // unserviced. Synchronous retries appear unlikely to offer any material improvement 
    // and complicating the code to narrow an already rare error case isn't desirable.
    {
        SListElem<NativeCodeVersion>* pMethodListItem = new (nothrow) SListElem<NativeCodeVersion>(t1NativeCodeVersion);
        SpinLockHolder holder(&m_lock);
        if (pMethodListItem != NULL)
        {
            m_methodsToOptimize.InsertTail(pMethodListItem);
        }

        if (0 == m_countOptimizationThreadsRunning && !m_isAppDomainShuttingDown)
        {
            // Our current policy throttles at 1 thread, but in the future we
            // could experiment with more parallelism.
            IncrementWorkerThreadCount();
        }
        else
        {
            return;
        }
    }

    EX_TRY
    {
        if (!ThreadpoolMgr::QueueUserWorkItem(StaticOptimizeMethodsCallback, this, QUEUE_ONLY, TRUE))
        {
            SpinLockHolder holder(&m_lock);
            DecrementWorkerThreadCount();
            STRESS_LOG1(LF_TIEREDCOMPILATION, LL_WARNING, "TieredCompilationManager::OnMethodCalled: "
                "ThreadpoolMgr::QueueUserWorkItem returned FALSE (no thread will run), method=%pM\n",
                pMethodDesc);
        }
    }
    EX_CATCH
    {
        SpinLockHolder holder(&m_lock);
        DecrementWorkerThreadCount();
        STRESS_LOG2(LF_TIEREDCOMPILATION, LL_WARNING, "TieredCompilationManager::OnMethodCalled: "
            "Exception queuing work item to threadpool, hr=0x%x, method=%pM\n",
            GET_EXCEPTION()->GetHR(), pMethodDesc);
    }
    EX_END_CATCH(RethrowTerminalExceptions);

    return;
}

// static
// called from EEShutDownHelper
void TieredCompilationManager::ShutdownAllDomains()
{
    STANDARD_VM_CONTRACT;

    AppDomainIterator domain(TRUE);
    while (domain.Next())
    {
        AppDomain * pDomain = domain.GetDomain();
        if (pDomain != NULL)
        {
            pDomain->GetTieredCompilationManager()->Shutdown(TRUE);
        }
    }
}

void TieredCompilationManager::Shutdown(BOOL fBlockUntilAsyncWorkIsComplete)
{
    STANDARD_VM_CONTRACT;

    {
        SpinLockHolder holder(&m_lock);
        m_isAppDomainShuttingDown = TRUE;
    }
    if (fBlockUntilAsyncWorkIsComplete)
    {
        m_asyncWorkDoneEvent.Wait(INFINITE, FALSE);
    }
}

// This is the initial entrypoint for the background thread, called by
// the threadpool.
DWORD WINAPI TieredCompilationManager::StaticOptimizeMethodsCallback(void *args)
{
    STANDARD_VM_CONTRACT;

    TieredCompilationManager * pTieredCompilationManager = (TieredCompilationManager *)args;
    pTieredCompilationManager->OptimizeMethodsCallback();

    return 0;
}

//This method will process one or more methods from optimization queue
// on a background thread. Each such method will be jitted with code
// optimizations enabled and then installed as the active implementation
// of the method entrypoint.
// 
// We need to be carefuly not to work for too long in a single invocation
// of this method or we could starve the threadpool and force
// it to create unnecessary additional threads.
void TieredCompilationManager::OptimizeMethodsCallback()
{
    STANDARD_VM_CONTRACT;

    // This app domain shutdown check isn't required for correctness
    // but it should reduce some unneeded exceptions trying
    // to enter a closed AppDomain
    {
        SpinLockHolder holder(&m_lock);
        if (m_isAppDomainShuttingDown)
        {
            DecrementWorkerThreadCount();
            return;
        }
    }

    ULONGLONG startTickCount = CLRGetTickCount64();
    NativeCodeVersion nativeCodeVersion;
    EX_TRY
    {
        GCX_COOP();
        ENTER_DOMAIN_ID(m_domainId);
        {
            GCX_PREEMP();
            while (true)
            {
                {
                    SpinLockHolder holder(&m_lock); 
                    nativeCodeVersion = GetNextMethodToOptimize();
                    if (nativeCodeVersion.IsNull() ||
                        m_isAppDomainShuttingDown)
                    {
                        DecrementWorkerThreadCount();
                        break;
                    }
                    
                }
                OptimizeMethod(nativeCodeVersion);

                // If we have been running for too long return the thread to the threadpool and queue another event
                // This gives the threadpool a chance to service other requests on this thread before returning to
                // this work.
                ULONGLONG currentTickCount = CLRGetTickCount64();
                if (currentTickCount >= startTickCount + m_optimizationQuantumMs)
                {
                    if (!ThreadpoolMgr::QueueUserWorkItem(StaticOptimizeMethodsCallback, this, QUEUE_ONLY, TRUE))
                    {
                        SpinLockHolder holder(&m_lock);
                        DecrementWorkerThreadCount();
                        STRESS_LOG0(LF_TIEREDCOMPILATION, LL_WARNING, "TieredCompilationManager::OptimizeMethodsCallback: "
                            "ThreadpoolMgr::QueueUserWorkItem returned FALSE (no thread will run)\n");
                    }
                    break;
                }
            }
        }
        END_DOMAIN_TRANSITION;
    }
    EX_CATCH
    {
        STRESS_LOG2(LF_TIEREDCOMPILATION, LL_ERROR, "TieredCompilationManager::OptimizeMethodsCallback: "
            "Unhandled exception during method optimization, hr=0x%x, last method=%pM\n",
            GET_EXCEPTION()->GetHR(), nativeCodeVersion.GetMethodDesc());
    }
    EX_END_CATCH(RethrowTerminalExceptions);
}

// Jit compiles and installs new optimized code for a method.
// Called on a background thread.
void TieredCompilationManager::OptimizeMethod(NativeCodeVersion nativeCodeVersion)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(nativeCodeVersion.GetMethodDesc()->IsEligibleForTieredCompilation());
    if (CompileCodeVersion(nativeCodeVersion))
    {
        ActivateCodeVersion(nativeCodeVersion);
    }
}

// Compiles new optimized code for a method.
// Called on a background thread.
BOOL TieredCompilationManager::CompileCodeVersion(NativeCodeVersion nativeCodeVersion)
{
    STANDARD_VM_CONTRACT;

    PCODE pCode = NULL;
    MethodDesc* pMethod = nativeCodeVersion.GetMethodDesc();
    EX_TRY
    {
        pCode = pMethod->PrepareCode(nativeCodeVersion);
    }
    EX_CATCH
    {
        // Failing to jit should be rare but acceptable. We will leave whatever code already exists in place.
        STRESS_LOG2(LF_TIEREDCOMPILATION, LL_INFO10, "TieredCompilationManager::CompileMethod: Method %pM failed to jit, hr=0x%x\n", 
            pMethod, GET_EXCEPTION()->GetHR());
    }
    EX_END_CATCH(RethrowTerminalExceptions)

    return pCode != NULL;
}

// Updates the MethodDesc and precode so that future invocations of a method will
// execute the native code pointed to by pCode.
// Called on a background thread.
void TieredCompilationManager::ActivateCodeVersion(NativeCodeVersion nativeCodeVersion)
{
    STANDARD_VM_CONTRACT;

    MethodDesc* pMethod = nativeCodeVersion.GetMethodDesc();
    CodeVersionManager* pCodeVersionManager = pMethod->GetCodeVersionManager();

    // If the ilParent version is active this will activate the native code version now.
    // Otherwise if the ilParent version becomes active again in the future the native
    // code version will activate then.
    ILCodeVersion ilParent;
    HRESULT hr = S_OK;
    {
        // As long as we are exclusively using precode publishing for tiered compilation
        // methods this first attempt should succeed
        CodeVersionManager::TableLockHolder lock(pCodeVersionManager);
        ilParent = nativeCodeVersion.GetILCodeVersion();
        hr = ilParent.SetActiveNativeCodeVersion(nativeCodeVersion, FALSE);
    }
    if (hr == CORPROF_E_RUNTIME_SUSPEND_REQUIRED)
    {
        // if we start using jump-stamp publishing for tiered compilation, the first attempt
        // without the runtime suspended will fail and then this second attempt will
        // succeed.
        // Even though this works performance is likely to be quite bad. Realistically
        // we are going to need batched updates to makes tiered-compilation + jump-stamp
        // viable. This fallback path is just here as a proof-of-concept.
        ThreadSuspend::SuspendEE(ThreadSuspend::SUSPEND_FOR_REJIT);
        {
            CodeVersionManager::TableLockHolder lock(pCodeVersionManager);
            hr = ilParent.SetActiveNativeCodeVersion(nativeCodeVersion, TRUE);
        }
        ThreadSuspend::RestartEE(FALSE, TRUE);
    }
    if (FAILED(hr))
    {
        STRESS_LOG2(LF_TIEREDCOMPILATION, LL_INFO10, "TieredCompilationManager::ActivateCodeVersion: Method %pM failed to publish native code for native code version %d\n",
            pMethod, nativeCodeVersion.GetVersionId());
    }
}

// Dequeues the next method in the optmization queue.
// This should be called with m_lock already held and runs
// on the background thread.
NativeCodeVersion TieredCompilationManager::GetNextMethodToOptimize()
{
    STANDARD_VM_CONTRACT;

    SListElem<NativeCodeVersion>* pElem = m_methodsToOptimize.RemoveHead();
    if (pElem != NULL)
    {
        NativeCodeVersion nativeCodeVersion = pElem->GetValue();
        delete pElem;
        return nativeCodeVersion;
    }
    return NativeCodeVersion();
}

void TieredCompilationManager::IncrementWorkerThreadCount()
{
    STANDARD_VM_CONTRACT;
    //m_lock should be held

    m_countOptimizationThreadsRunning++;
    m_asyncWorkDoneEvent.Reset();
}

void TieredCompilationManager::DecrementWorkerThreadCount()
{
    STANDARD_VM_CONTRACT;
    //m_lock should be held
    
    m_countOptimizationThreadsRunning--;
    if (m_countOptimizationThreadsRunning == 0)
    {
        m_asyncWorkDoneEvent.Set();
    }
}

//static
CORJIT_FLAGS TieredCompilationManager::GetJitFlags(NativeCodeVersion nativeCodeVersion)
{
    LIMITED_METHOD_CONTRACT;

    CORJIT_FLAGS flags;
    if (!nativeCodeVersion.GetMethodDesc()->IsEligibleForTieredCompilation())
    {
#ifdef FEATURE_INTERPRETER
        flags.Set(CORJIT_FLAGS::CORJIT_FLAG_MAKEFINALCODE);
#endif
        return flags;
    }
    
    if (nativeCodeVersion.GetOptimizationTier() == NativeCodeVersion::OptimizationTier0)
    {
        flags.Set(CORJIT_FLAGS::CORJIT_FLAG_TIER0);
    }
    else
    {
        flags.Set(CORJIT_FLAGS::CORJIT_FLAG_TIER1);
#ifdef FEATURE_INTERPRETER
        flags.Set(CORJIT_FLAGS::CORJIT_FLAG_MAKEFINALCODE);
#endif
    }
    return flags;
}

#endif // FEATURE_TIERED_COMPILATION
