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

#if defined(FEATURE_TIERED_COMPILATION) && !defined(DACCESS_COMPILE)

// Called at AppDomain construction
TieredCompilationManager::TieredCompilationManager() :
    m_lock(CrstTieredCompilation),
    m_countOfMethodsToOptimize(0),
    m_isAppDomainShuttingDown(FALSE),
    m_countOptimizationThreadsRunning(0),
    m_countOfNewMethodsCalledDuringDelay(0),
    m_methodsPendingCountingForTier1(nullptr),
    m_tieringDelayTimerHandle(nullptr),
    m_tier1CallCountingCandidateMethodRecentlyRecorded(false)
{
    WRAPPER_NO_CONTRACT;
    // On Unix, we can reach here before EEConfig is initialized, so defer config-based initialization to Init()
}

// Called at AppDomain Init
void TieredCompilationManager::Init()
{
    CONTRACTL
    {
        GC_NOTRIGGER;
        CAN_TAKE_LOCK;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;
}

#endif // FEATURE_TIERED_COMPILATION && !DACCESS_COMPILE

NativeCodeVersion::OptimizationTier TieredCompilationManager::GetInitialOptimizationTier(PTR_MethodDesc pMethodDesc)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(pMethodDesc != NULL);

#ifdef FEATURE_TIERED_COMPILATION
    if (!pMethodDesc->IsEligibleForTieredCompilation())
    {
        // The optimization tier is not used
        return NativeCodeVersion::OptimizationTierOptimized;
    }

    if (pMethodDesc->RequestedAggressiveOptimization())
    {
        // Methods flagged with MethodImplOptions.AggressiveOptimization start with and stay at tier 1
        return NativeCodeVersion::OptimizationTier1;
    }

    if (!pMethodDesc->GetCallCounter()->IsCallCountingEnabled(pMethodDesc))
    {
        // Tier 0 call counting may have been disabled for several reasons, the intention is to start with and stay at an
        // optimized tier
        return NativeCodeVersion::OptimizationTierOptimized;
    }
#endif

    return NativeCodeVersion::OptimizationTier0;
}

#if defined(FEATURE_TIERED_COMPILATION) && !defined(DACCESS_COMPILE)

bool TieredCompilationManager::OnMethodCodeVersionCalledFirstTime(MethodDesc* pMethodDesc)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(pMethodDesc != nullptr);
    _ASSERTE(pMethodDesc->IsEligibleForTieredCompilation());
    _ASSERTE(pMethodDesc->GetCallCounter()->IsCallCountingEnabled(pMethodDesc));

    if (g_pConfig->TieredCompilation_CallCountingDelayMs() == 0)
    {
        return false;
    }

    while (true)
    {
        bool attemptedToInitiateDelay = false;
        if (!IsTieringDelayActive())
        {
            if (!TryInitiateTieringDelay())
            {
                return false;
            }
            attemptedToInitiateDelay = true;
        }

        CrstHolder holder(&m_lock);

        SArray<MethodDesc*>* methodsPendingCountingForTier1 = m_methodsPendingCountingForTier1;
        if (methodsPendingCountingForTier1 == nullptr)
        {
            // Timer tick callback race, try again
            continue;
        }

        // Record the method to resume counting later (see TieringDelayTimerCallback)
        bool success = false;
        EX_TRY
        {
            methodsPendingCountingForTier1->Append(pMethodDesc);
            success = true;
        }
        EX_CATCH
        {
        }
        EX_END_CATCH(RethrowTerminalExceptions);
        if (!success)
        {
            return false;
        }

        ++m_countOfNewMethodsCalledDuringDelay;

        if (!attemptedToInitiateDelay)
        {
            // Delay call counting for currently recoded methods further
            m_tier1CallCountingCandidateMethodRecentlyRecorded = true;
        }
        return true;
    }
}

bool TieredCompilationManager::OnMethodCodeVersionCalledSubsequently(MethodDesc* pMethodDesc)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(pMethodDesc != nullptr);
    _ASSERTE(pMethodDesc->GetCallCounter()->IsCallCountingEnabled(pMethodDesc));

    if (!IsTieringDelayActive() || g_pConfig->TieredCompilation_CallCountingDelayMs() == 0)
    {
        return false;
    }

    CrstHolder holder(&m_lock);

    if (!m_tier1CallCountingCandidateMethodRecentlyRecorded)
    {
        // This is to prevent a race where the method get recorded below, the delay timer callback resets the method's entry
        // point to begin call counting, and then the method's entry point is set by this thread to the tier 0 entry point. In
        // that case the method would not be counted or tiered-up anymore. So, stop call counting only when the delay timer will
        // be extended, the extra delay makes the issue near-impossible to occur. This is not a great solution and is temporary,
        // once the call counting scheme is changed this code and issue will disappear.
        return false;
    }

    SArray<MethodDesc*>* methodsPendingCountingForTier1 = m_methodsPendingCountingForTier1;
    if (methodsPendingCountingForTier1 == nullptr)
    {
        // Timer tick callback race
        _ASSERTE(!IsTieringDelayActive());
        return false;
    }

    // Record the method to resume counting later (see TieringDelayTimerCallback)
    bool success = false;
    EX_TRY
    {
        methodsPendingCountingForTier1->Append(pMethodDesc);
        success = true;
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(RethrowTerminalExceptions);
    return success;
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
            NativeCodeVersion::OptimizationTier optimizationTier = cur->GetOptimizationTier();
            if (optimizationTier == NativeCodeVersion::OptimizationTier1 ||
                optimizationTier == NativeCodeVersion::OptimizationTierOptimized)
            {
                // we've already promoted
                LOG((LF_TIEREDCOMPILATION, LL_INFO100000, "TieredCompilationManager::AsyncPromoteMethodToTier1 Method=0x%pM (%s::%s) ignoring already promoted method\n",
                    pMethodDesc, pMethodDesc->m_pszDebugClassName, pMethodDesc->m_pszDebugMethodName));
                return;
            }
        }

        HRESULT hr = S_OK;
        if (FAILED(hr = ilVersion.AddNativeCodeVersion(pMethodDesc, NativeCodeVersion::OptimizationTier1, &t1NativeCodeVersion)))
        {
            // optimization didn't work for some reason (presumably OOM)
            // just give up and continue on
            STRESS_LOG2(LF_TIEREDCOMPILATION, LL_WARNING, "TieredCompilationManager::AsyncPromoteMethodToTier1: "
                "AddNativeCodeVersion failed hr=0x%x, method=%pM\n",
                hr, pMethodDesc);
            return;
        }
    }

    // Insert the method into the optimization queue and trigger a thread to service
    // the queue if needed.
    //
    // Note an error here could affect concurrent threads running this
    // code. Those threads will observe m_countOptimizationThreadsRunning > 0 and return,
    // then QueueUserWorkItem fails on this thread lowering the count and leaves them
    // unserviced. Synchronous retries appear unlikely to offer any material improvement
    // and complicating the code to narrow an already rare error case isn't desirable.
    {
        SListElem<NativeCodeVersion>* pMethodListItem = new (nothrow) SListElem<NativeCodeVersion>(t1NativeCodeVersion);
        CrstHolder holder(&m_lock);
        if (pMethodListItem != NULL)
        {
            m_methodsToOptimize.InsertTail(pMethodListItem);
            ++m_countOfMethodsToOptimize;
        }

        LOG((LF_TIEREDCOMPILATION, LL_INFO10000, "TieredCompilationManager::AsyncPromoteMethodToTier1 Method=0x%pM (%s::%s), code version id=0x%x queued\n",
            pMethodDesc, pMethodDesc->m_pszDebugClassName, pMethodDesc->m_pszDebugMethodName,
            t1NativeCodeVersion.GetVersionId()));

        if (!IncrementWorkerThreadCountIfNeeded())
        {
            return;
        }
    }

    if (!TryAsyncOptimizeMethods())
    {
        CrstHolder holder(&m_lock);
        DecrementWorkerThreadCount();
    }
}

void TieredCompilationManager::Shutdown()
{
    STANDARD_VM_CONTRACT;

    CrstHolder holder(&m_lock);
    m_isAppDomainShuttingDown = TRUE;
}

bool TieredCompilationManager::IsTieringDelayActive()
{
    LIMITED_METHOD_CONTRACT;
    return m_methodsPendingCountingForTier1 != nullptr;
}

bool TieredCompilationManager::TryInitiateTieringDelay()
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(g_pConfig->TieredCompilation());
    _ASSERTE(g_pConfig->TieredCompilation_CallCountingDelayMs() != 0);

    NewHolder<SArray<MethodDesc*>> methodsPendingCountingHolder = new(nothrow) SArray<MethodDesc*>();
    if (methodsPendingCountingHolder == nullptr)
    {
        return false;
    }

    bool success = false;
    EX_TRY
    {
        methodsPendingCountingHolder->Preallocate(64);
        success = true;
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(RethrowTerminalExceptions);
    if (!success)
    {
        return false;
    }

    NewHolder<ThreadpoolMgr::TimerInfoContext> timerContextHolder = new(nothrow) ThreadpoolMgr::TimerInfoContext();
    if (timerContextHolder == nullptr)
    {
        return false;
    }
    timerContextHolder->TimerId = 0;

    {
        CrstHolder holder(&m_lock);

        if (IsTieringDelayActive())
        {
            return true;
        }

        // The timer is created inside the lock to avoid some unnecessary additional complexity that would otherwise arise from
        // there being a failure point after the timer is successfully created. For instance, if the timer is created outside
        // the lock and then inside the lock it is found that another thread beat us to it, there would be two active timers
        // that may tick before the extra timer is deleted, along with additional concurrency issues.
        _ASSERTE(m_tieringDelayTimerHandle == nullptr);
        success = false;
        EX_TRY
        {
            if (ThreadpoolMgr::CreateTimerQueueTimer(
                    &m_tieringDelayTimerHandle,
                    TieringDelayTimerCallback,
                    timerContextHolder,
                    g_pConfig->TieredCompilation_CallCountingDelayMs(),
                    (DWORD)-1 /* Period, non-repeating */,
                    0 /* flags */))
            {
                success = true;
            }
        }
        EX_CATCH
        {
        }
        EX_END_CATCH(RethrowTerminalExceptions);
        if (!success)
        {
            _ASSERTE(m_tieringDelayTimerHandle == nullptr);
            return false;
        }

        m_methodsPendingCountingForTier1 = methodsPendingCountingHolder.Extract();
        _ASSERTE(!m_tier1CallCountingCandidateMethodRecentlyRecorded);
        _ASSERTE(IsTieringDelayActive());
    }

    timerContextHolder.SuppressRelease(); // the timer context is automatically deleted by the timer infrastructure
    if (ETW::CompilationLog::TieredCompilation::Runtime::IsEnabled())
    {
        ETW::CompilationLog::TieredCompilation::Runtime::SendPause();
    }
    return true;
}

void WINAPI TieredCompilationManager::TieringDelayTimerCallback(PVOID parameter, BOOLEAN timerFired)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(timerFired);

    ThreadpoolMgr::TimerInfoContext* timerContext = (ThreadpoolMgr::TimerInfoContext*)parameter;
    EX_TRY
    {
        GCX_COOP();
        ManagedThreadBase::ThreadPool(TieringDelayTimerCallbackInAppDomain, nullptr);
    }
    EX_CATCH
    {
        STRESS_LOG1(LF_TIEREDCOMPILATION, LL_ERROR, "TieredCompilationManager::TieringDelayTimerCallback: "
            "Unhandled exception, hr=0x%x\n",
            GET_EXCEPTION()->GetHR());
    }
    EX_END_CATCH(RethrowTerminalExceptions);
}

void TieredCompilationManager::TieringDelayTimerCallbackInAppDomain(LPVOID parameter)
{
    WRAPPER_NO_CONTRACT;

    GCX_PREEMP();
    GetAppDomain()->GetTieredCompilationManager()->TieringDelayTimerCallbackWorker();
}

void TieredCompilationManager::TieringDelayTimerCallbackWorker()
{
    WRAPPER_NO_CONTRACT;

    HANDLE tieringDelayTimerHandle;
    SArray<MethodDesc*>* methodsPendingCountingForTier1;
    UINT32 countOfNewMethodsCalledDuringDelay;
    bool optimizeMethods;
    while (true)
    {
        bool tier1CallCountingCandidateMethodRecentlyRecorded;
        {
            // It's possible for the timer to tick before it is recorded that the delay is in effect. This lock guarantees that
            // the delay is in effect.
            CrstHolder holder(&m_lock);
            _ASSERTE(IsTieringDelayActive());

            tieringDelayTimerHandle = m_tieringDelayTimerHandle;
            _ASSERTE(tieringDelayTimerHandle != nullptr);

            tier1CallCountingCandidateMethodRecentlyRecorded = m_tier1CallCountingCandidateMethodRecentlyRecorded;
            if (tier1CallCountingCandidateMethodRecentlyRecorded)
            {
                m_tier1CallCountingCandidateMethodRecentlyRecorded = false;
            }
            else
            {
                // Exchange information into locals inside the lock

                methodsPendingCountingForTier1 = m_methodsPendingCountingForTier1;
                _ASSERTE(methodsPendingCountingForTier1 != nullptr);
                m_methodsPendingCountingForTier1 = nullptr;

                _ASSERTE(tieringDelayTimerHandle == m_tieringDelayTimerHandle);
                m_tieringDelayTimerHandle = nullptr;

                countOfNewMethodsCalledDuringDelay = m_countOfNewMethodsCalledDuringDelay;
                m_countOfNewMethodsCalledDuringDelay = 0;

                _ASSERTE(!IsTieringDelayActive());
                optimizeMethods = IncrementWorkerThreadCountIfNeeded();

                break;
            }
        }

        // Reschedule the timer if there has been recent tier 0 activity (when a new eligible method is called the first time) to
        // further delay call counting
        if (tier1CallCountingCandidateMethodRecentlyRecorded)
        {
            bool success = false;
            EX_TRY
            {
                if (ThreadpoolMgr::ChangeTimerQueueTimer(
                        tieringDelayTimerHandle,
                        g_pConfig->TieredCompilation_CallCountingDelayMs(),
                        (DWORD)-1 /* Period, non-repeating */))
                {
                    success = true;
                }
            }
            EX_CATCH
            {
            }
            EX_END_CATCH(RethrowTerminalExceptions);
            if (success)
            {
                return;
            }
        }
    }

    if (ETW::CompilationLog::TieredCompilation::Runtime::IsEnabled())
    {
        ETW::CompilationLog::TieredCompilation::Runtime::SendResume(countOfNewMethodsCalledDuringDelay);
    }

    // Install call counters
    MethodDesc** methods = methodsPendingCountingForTier1->GetElements();
    COUNT_T methodCount = methodsPendingCountingForTier1->GetCount();
    for (COUNT_T i = 0; i < methodCount; ++i)
    {
        MethodDesc *methodDesc = methods[i];
        MethodDescBackpatchInfoTracker::ConditionalLockHolder lockHolder(methodDesc->MayHaveEntryPointSlotsToBackpatch());

        EX_TRY
        {
            methodDesc->ResetCodeEntryPoint();
        }
        EX_CATCH
        {
        }
        EX_END_CATCH(RethrowTerminalExceptions);
    }
    delete methodsPendingCountingForTier1;

    ThreadpoolMgr::DeleteTimerQueueTimer(tieringDelayTimerHandle, nullptr);

    if (optimizeMethods)
    {
        OptimizeMethods();
    }
}

bool TieredCompilationManager::TryAsyncOptimizeMethods()
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(DebugGetWorkerThreadCount() != 0);

    // Terminal exceptions escape as exceptions, but all other errors should gracefully
    // return to the caller. Non-terminal error conditions should be rare (ie OOM,
    // OS failure to create thread) and we consider it reasonable for some methods
    // to go unoptimized or have their optimization arbitrarily delayed under these
    // circumstances.
    bool success = false;
    EX_TRY
    {
        if (ThreadpoolMgr::QueueUserWorkItem(StaticOptimizeMethodsCallback, this, QUEUE_ONLY, TRUE))
        {
            success = true;
        }
        else
        {
            STRESS_LOG0(LF_TIEREDCOMPILATION, LL_WARNING, "TieredCompilationManager::OnMethodCalled: "
                "ThreadpoolMgr::QueueUserWorkItem returned FALSE (no thread will run)\n");
        }
    }
    EX_CATCH
    {
        STRESS_LOG1(LF_TIEREDCOMPILATION, LL_WARNING, "TieredCompilationManager::OnMethodCalled: "
            "Exception queuing work item to threadpool, hr=0x%x\n",
            GET_EXCEPTION()->GetHR());
    }
    EX_END_CATCH(RethrowTerminalExceptions);
    return success;
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

void TieredCompilationManager::OptimizeMethodsCallback()
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(DebugGetWorkerThreadCount() != 0);

    // This app domain shutdown check isn't required for correctness
    // but it should reduce some unneeded exceptions trying
    // to enter a closed AppDomain
    {
        CrstHolder holder(&m_lock);
        if (m_isAppDomainShuttingDown)
        {
            DecrementWorkerThreadCount();
            return;
        }
    }

    EX_TRY
    {
        GCX_COOP();
        OptimizeMethods();
    }
    EX_CATCH
    {
        STRESS_LOG1(LF_TIEREDCOMPILATION, LL_ERROR, "TieredCompilationManager::OptimizeMethodsCallback: "
            "Unhandled exception on domain transition, hr=0x%x\n",
            GET_EXCEPTION()->GetHR());
    }
    EX_END_CATCH(RethrowTerminalExceptions);
}

//This method will process one or more methods from optimization queue
// on a background thread. Each such method will be jitted with code
// optimizations enabled and then installed as the active implementation
// of the method entrypoint.
void TieredCompilationManager::OptimizeMethods()
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(DebugGetWorkerThreadCount() != 0);

    // We need to be careful not to work for too long in a single invocation of this method or we could starve the thread pool
    // and force it to create unnecessary additional threads. We will JIT for a minimum of this quantum, then schedule another
    // work item to the thread pool and return this thread back to the pool.
    const DWORD OptimizationQuantumMs = 50;

    if (ETW::CompilationLog::TieredCompilation::Runtime::IsEnabled())
    {
        ETW::CompilationLog::TieredCompilation::Runtime::SendBackgroundJitStart(m_countOfMethodsToOptimize);
    }

    UINT32 jittedMethodCount = 0;
    DWORD startTickCount = GetTickCount();
    NativeCodeVersion nativeCodeVersion;
    EX_TRY
    {
        GCX_PREEMP();
        while (true)
        {
            {
                CrstHolder holder(&m_lock);

                if (IsTieringDelayActive() || m_isAppDomainShuttingDown)
                {
                    DecrementWorkerThreadCount();
                    break;
                }

                nativeCodeVersion = GetNextMethodToOptimize();
                if (nativeCodeVersion.IsNull())
                {
                    DecrementWorkerThreadCount();
                    break;
                }
            }

            OptimizeMethod(nativeCodeVersion);
            ++jittedMethodCount;

            // If we have been running for too long return the thread to the threadpool and queue another event
            // This gives the threadpool a chance to service other requests on this thread before returning to
            // this work.
            DWORD currentTickCount = GetTickCount();
            if (currentTickCount - startTickCount >= OptimizationQuantumMs)
            {
                if (!TryAsyncOptimizeMethods())
                {
                    CrstHolder holder(&m_lock);
                    DecrementWorkerThreadCount();
                }
                break;
            }
        }
    }
    EX_CATCH
    {
        {
            CrstHolder holder(&m_lock);
            DecrementWorkerThreadCount();
        }
        STRESS_LOG2(LF_TIEREDCOMPILATION, LL_ERROR, "TieredCompilationManager::OptimizeMethods: "
            "Unhandled exception during method optimization, hr=0x%x, last method=%p\n",
            GET_EXCEPTION()->GetHR(), nativeCodeVersion.GetMethodDesc());
    }
    EX_END_CATCH(RethrowTerminalExceptions);

    if (ETW::CompilationLog::TieredCompilation::Runtime::IsEnabled())
    {
        ETW::CompilationLog::TieredCompilation::Runtime::SendBackgroundJitStop(m_countOfMethodsToOptimize, jittedMethodCount);
    }
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
        PrepareCodeConfigBuffer configBuffer(nativeCodeVersion);
        PrepareCodeConfig *config = configBuffer.GetConfig();
        pCode = pMethod->PrepareCode(config);
        LOG((LF_TIEREDCOMPILATION, LL_INFO10000, "TieredCompilationManager::CompileCodeVersion Method=0x%pM (%s::%s), code version id=0x%x, code ptr=0x%p\n",
            pMethod, pMethod->m_pszDebugClassName, pMethod->m_pszDebugMethodName,
            nativeCodeVersion.GetVersionId(),
            pCode));

        if (config->JitSwitchedToMinOpt())
        {
            // The JIT decided to switch to min-opts, likely due to the method being very large or complex. The rejitted code
            // may be slower if the method had been prejitted. Ignore the rejitted code and continue using the tier 0 entry
            // point.
            // TODO: In the future, we should get some feedback from images containing pregenerated code and from tier 0 JIT
            // indicating that the method would not benefit from a rejit and avoid the rejit altogether.
            pCode = NULL;
        }
    }
    EX_CATCH
    {
        // Failing to jit should be rare but acceptable. We will leave whatever code already exists in place.
        STRESS_LOG2(LF_TIEREDCOMPILATION, LL_INFO10, "TieredCompilationManager::CompileCodeVersion: Method %pM failed to jit, hr=0x%x\n",
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
    bool mayHaveEntryPointSlotsToBackpatch = pMethod->MayHaveEntryPointSlotsToBackpatch();
    MethodDescBackpatchInfoTracker::ConditionalLockHolder lockHolder(mayHaveEntryPointSlotsToBackpatch);

    {
        // Backpatching entry point slots requires cooperative GC mode, see
        // MethodDescBackpatchInfoTracker::Backpatch_Locked(). The code version manager's table lock is an unsafe lock that
        // may be taken in any GC mode. The lock is taken in cooperative GC mode on some other paths, so the same ordering
        // must be used here to prevent deadlock.
        GCX_MAYBE_COOP(mayHaveEntryPointSlotsToBackpatch);
        CodeVersionManager::TableLockHolder lock(pCodeVersionManager);

        // As long as we are exclusively using any non-JumpStamp publishing for tiered compilation
        // methods this first attempt should succeed
        ilParent = nativeCodeVersion.GetILCodeVersion();
        hr = ilParent.SetActiveNativeCodeVersion(nativeCodeVersion, FALSE);
        LOG((LF_TIEREDCOMPILATION, LL_INFO10000, "TieredCompilationManager::ActivateCodeVersion Method=0x%pM (%s::%s), code version id=0x%x. SetActiveNativeCodeVersion ret=0x%x\n",
            pMethod, pMethod->m_pszDebugClassName, pMethod->m_pszDebugMethodName,
            nativeCodeVersion.GetVersionId(),
            hr));
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
            // Backpatching entry point slots requires cooperative GC mode, see
            // MethodDescBackpatchInfoTracker::Backpatch_Locked(). The code version manager's table lock is an unsafe lock that
            // may be taken in any GC mode. The lock is taken in cooperative GC mode on some other paths, so the same ordering
            // must be used here to prevent deadlock.
            GCX_MAYBE_COOP(mayHaveEntryPointSlotsToBackpatch);
            CodeVersionManager::TableLockHolder lock(pCodeVersionManager);

            hr = ilParent.SetActiveNativeCodeVersion(nativeCodeVersion, TRUE);
            LOG((LF_TIEREDCOMPILATION, LL_INFO10000, "TieredCompilationManager::ActivateCodeVersion Method=0x%pM (%s::%s), code version id=0x%x. [Suspended] SetActiveNativeCodeVersion ret=0x%x\n",
                pMethod, pMethod->m_pszDebugClassName, pMethod->m_pszDebugMethodName,
                nativeCodeVersion.GetVersionId(),
                hr));
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
        --m_countOfMethodsToOptimize;
        return nativeCodeVersion;
    }
    return NativeCodeVersion();
}

bool TieredCompilationManager::IncrementWorkerThreadCountIfNeeded()
{
    WRAPPER_NO_CONTRACT;
    // m_lock should be held

    if (0 == m_countOptimizationThreadsRunning &&
        !m_isAppDomainShuttingDown &&
        !m_methodsToOptimize.IsEmpty() &&
        !IsTieringDelayActive())
    {
        // Our current policy throttles at 1 thread, but in the future we
        // could experiment with more parallelism.
        m_countOptimizationThreadsRunning++;
        return true;
    }
    return false;
}

void TieredCompilationManager::DecrementWorkerThreadCount()
{
    STANDARD_VM_CONTRACT;
    // m_lock should be held
    _ASSERTE(m_countOptimizationThreadsRunning != 0);

    m_countOptimizationThreadsRunning--;
}

#ifdef _DEBUG
DWORD TieredCompilationManager::DebugGetWorkerThreadCount()
{
    WRAPPER_NO_CONTRACT;

    CrstHolder holder(&m_lock);
    return m_countOptimizationThreadsRunning;
}
#endif

//static
CORJIT_FLAGS TieredCompilationManager::GetJitFlags(NativeCodeVersion nativeCodeVersion)
{
    LIMITED_METHOD_CONTRACT;

    CORJIT_FLAGS flags;
    MethodDesc *methodDesc = nativeCodeVersion.GetMethodDesc();
    if (!methodDesc->IsEligibleForTieredCompilation())
    {
#ifdef FEATURE_INTERPRETER
        flags.Set(CORJIT_FLAGS::CORJIT_FLAG_MAKEFINALCODE);
#endif
        return flags;
    }

    if (nativeCodeVersion.IsDefaultVersion()) // slightly faster common path during startup compared to below
    {
        if (!methodDesc->RequestedAggressiveOptimization())
        {
            if (g_pConfig->TieredCompilation_QuickJit())
            {
                flags.Set(CORJIT_FLAGS::CORJIT_FLAG_TIER0);
                return flags;
            }
        }
        else
        {
            flags.Set(CORJIT_FLAGS::CORJIT_FLAG_TIER1);
        }

    #ifdef FEATURE_INTERPRETER
        flags.Set(CORJIT_FLAGS::CORJIT_FLAG_MAKEFINALCODE);
    #endif
        return flags;
    }

    switch (nativeCodeVersion.GetOptimizationTier())
    {
        case NativeCodeVersion::OptimizationTier0:
            if (!g_pConfig->TieredCompilation_QuickJit())
            {
                goto OptTierOptimized;
            }
            flags.Set(CORJIT_FLAGS::CORJIT_FLAG_TIER0);
            break;

        case NativeCodeVersion::OptimizationTier1:
            flags.Set(CORJIT_FLAGS::CORJIT_FLAG_TIER1);
            // fall through

        case NativeCodeVersion::OptimizationTierOptimized:
        OptTierOptimized:
#ifdef FEATURE_INTERPRETER
            flags.Set(CORJIT_FLAGS::CORJIT_FLAG_MAKEFINALCODE);
#endif
            break;

        default:
            UNREACHABLE();
    }
    return flags;
}

#endif // FEATURE_TIERED_COMPILATION && !DACCESS_COMPILE
