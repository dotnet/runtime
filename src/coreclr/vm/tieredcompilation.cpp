// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
// a) .ctor -                called once during AppDomain initialization
// b) HandleCallCountingForFirstCall(...) - called when a method's code version is being
//                           invoked for the first time.
//
// # Overall workflow
//
// Methods initially call into HandleCallCountingForFirstCall() and once the call count exceeds
// a fixed limit we queue work on to our internal list of methods needing to
// be recompiled (m_methodsToOptimize). If there is currently no thread
// servicing our queue asynchronously then we use the runtime threadpool
// QueueUserWorkItem to recruit one. During the callback for each threadpool work
// item we handle as many methods as possible in a fixed period of time, then
// queue another threadpool work item if m_methodsToOptimize hasn't been drained.
//
// The background thread enters at StaticBackgroundWorkCallback(), enters the
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

CrstStatic TieredCompilationManager::s_lock;
#ifdef _DEBUG
Thread *TieredCompilationManager::s_backgroundWorkerThread = nullptr;
#endif
CLREvent TieredCompilationManager::s_backgroundWorkAvailableEvent;
bool TieredCompilationManager::s_isBackgroundWorkerRunning = false;
bool TieredCompilationManager::s_isBackgroundWorkerProcessingWork = false;

// Called at AppDomain construction
TieredCompilationManager::TieredCompilationManager() :
    m_countOfMethodsToOptimize(0),
    m_countOfNewMethodsCalledDuringDelay(0),
    m_methodsPendingCountingForTier1(nullptr),
    m_tier1CallCountingCandidateMethodRecentlyRecorded(false),
    m_isPendingCallCountingCompletion(false),
    m_recentlyRequestedCallCountingCompletion(false)
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

    if (!pMethodDesc->GetLoaderAllocator()->GetCallCountingManager()->IsCallCountingEnabled(NativeCodeVersion(pMethodDesc)))
    {
        // Tier 0 call counting may have been disabled for several reasons, the intention is to start with and stay at an
        // optimized tier
        return NativeCodeVersion::OptimizationTierOptimized;
    }

    return NativeCodeVersion::OptimizationTier0;
#else
    return NativeCodeVersion::OptimizationTierOptimized;
#endif
}

#if defined(FEATURE_TIERED_COMPILATION) && !defined(DACCESS_COMPILE)

void TieredCompilationManager::HandleCallCountingForFirstCall(MethodDesc* pMethodDesc)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    _ASSERTE(pMethodDesc != nullptr);
    _ASSERTE(pMethodDesc->IsEligibleForTieredCompilation());
    _ASSERTE(g_pConfig->TieredCompilation_CallCountingDelayMs() != 0);

    // An exception here (OOM) would mean that the method's calls would not be counted and it would not be promoted. A
    // consideration is that an attempt can be made to reset the code entry point on exception (which can also OOM). Doesn't
    // seem worth it, the exception is propagated and there are other cases where a method may not be promoted due to OOM.
    bool createBackgroundWorker;
    {
        LockHolder tieredCompilationLockHolder;

        SArray<MethodDesc *> *methodsPendingCounting = m_methodsPendingCountingForTier1;
        _ASSERTE((methodsPendingCounting != nullptr) == IsTieringDelayActive());
        if (methodsPendingCounting != nullptr)
        {
            methodsPendingCounting->Append(pMethodDesc);
            ++m_countOfNewMethodsCalledDuringDelay;

            if (!m_tier1CallCountingCandidateMethodRecentlyRecorded)
            {
                // Delay call counting for currently recoded methods further
                m_tier1CallCountingCandidateMethodRecentlyRecorded = true;
            }
            return;
        }

        NewHolder<SArray<MethodDesc *>> methodsPendingCountingHolder = new SArray<MethodDesc *>();
        methodsPendingCountingHolder->Preallocate(64);

        methodsPendingCountingHolder->Append(pMethodDesc);
        ++m_countOfNewMethodsCalledDuringDelay;

        m_methodsPendingCountingForTier1 = methodsPendingCountingHolder.Extract();
        _ASSERTE(!m_tier1CallCountingCandidateMethodRecentlyRecorded);
        _ASSERTE(IsTieringDelayActive());

        // The thread is in a GC_NOTRIGGER scope here. If the background worker is already running, we can schedule it inside
        // the same lock without triggering a GC.
        createBackgroundWorker = !TryScheduleBackgroundWorkerWithoutGCTrigger_Locked();
    }

    if (createBackgroundWorker)
    {
        // Elsewhere, the tiered compilation lock is taken inside the code versioning lock. The code versioning lock is an
        // unsafe any-GC-mode lock, so the tiering lock is also that type of lock. Inside that type of lock, there is an
        // implicit GC_NOTRIGGER contract. So, a thread cannot be created inside the tiering lock since it may GC_TRIGGERS. At
        // this point, this is the only thread that may attempt creating the background worker thread.
        EX_TRY
        {
            CreateBackgroundWorker();
        }
        EX_CATCH
        {
            // Since the tiering lock was released and reacquired, other methods may have been recorded in-between. Just
            // deactivate the tiering delay. Any methods that have been recorded would not have their calls be counted and
            // would not be promoted (due to the small window, there shouldn't be many of those). See consideration above in a
            // similar exception case.
            {
                LockHolder tieredCompilationLockHolder;

                _ASSERTE(IsTieringDelayActive());
                m_tier1CallCountingCandidateMethodRecentlyRecorded = false;
                _ASSERTE(m_methodsPendingCountingForTier1 != nullptr);
                delete m_methodsPendingCountingForTier1;
                m_methodsPendingCountingForTier1 = nullptr;
                _ASSERTE(!IsTieringDelayActive());
            }

            EX_RETHROW;
        }
        EX_END_CATCH(RethrowTerminalExceptions);
    }

    if (ETW::CompilationLog::TieredCompilation::Runtime::IsEnabled())
    {
        ETW::CompilationLog::TieredCompilation::Runtime::SendPause();
    }
}

bool TieredCompilationManager::TrySetCodeEntryPointAndRecordMethodForCallCounting(MethodDesc* pMethodDesc, PCODE codeEntryPoint)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(pMethodDesc != nullptr);
    _ASSERTE(pMethodDesc->IsEligibleForTieredCompilation());
    _ASSERTE(codeEntryPoint != NULL);

    if (!IsTieringDelayActive())
    {
        return false;
    }

    LockHolder tieredCompilationLockHolder;

    if (!IsTieringDelayActive())
    {
        return false;
    }

    // Set the code entry point before recording the method for call counting to avoid a race. Otherwise, the tiering delay may
    // expire and enable call counting for the method before the entry point is set here, in which case calls to the method
    // would not be counted anymore.
    pMethodDesc->SetCodeEntryPoint(codeEntryPoint);
    _ASSERTE(m_methodsPendingCountingForTier1 != nullptr);
    m_methodsPendingCountingForTier1->Append(pMethodDesc);
    return true;
}

void TieredCompilationManager::AsyncPromoteToTier1(
    NativeCodeVersion tier0NativeCodeVersion,
    bool *createTieringBackgroundWorkerRef)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(CodeVersionManager::IsLockOwnedByCurrentThread());
    _ASSERTE(!tier0NativeCodeVersion.IsNull());
    _ASSERTE(tier0NativeCodeVersion.GetOptimizationTier() == NativeCodeVersion::OptimizationTier0);
    _ASSERTE(createTieringBackgroundWorkerRef != nullptr);

    NativeCodeVersion t1NativeCodeVersion;
    HRESULT hr;

    // Add an inactive native code entry in the versioning table to track the tier1
    // compilation we are going to create. This entry binds the compilation to a
    // particular version of the IL code regardless of any changes that may
    // occur between now and when jitting completes. If the IL does change in that
    // interval the new code entry won't be activated.
    MethodDesc *pMethodDesc = tier0NativeCodeVersion.GetMethodDesc();
    ILCodeVersion ilCodeVersion = tier0NativeCodeVersion.GetILCodeVersion();
    _ASSERTE(!ilCodeVersion.HasAnyOptimizedNativeCodeVersion(tier0NativeCodeVersion));
    hr = ilCodeVersion.AddNativeCodeVersion(pMethodDesc, NativeCodeVersion::OptimizationTier1, &t1NativeCodeVersion);
    if (FAILED(hr))
    {
        ThrowHR(hr);
    }

    // Insert the method into the optimization queue and trigger a thread to service
    // the queue if needed.
    SListElem<NativeCodeVersion>* pMethodListItem = new SListElem<NativeCodeVersion>(t1NativeCodeVersion);
    {
        LockHolder tieredCompilationLockHolder;

        m_methodsToOptimize.InsertTail(pMethodListItem);
        ++m_countOfMethodsToOptimize;

        LOG((LF_TIEREDCOMPILATION, LL_INFO10000, "TieredCompilationManager::AsyncPromoteToTier1 Method=0x%pM (%s::%s), code version id=0x%x queued\n",
            pMethodDesc, pMethodDesc->m_pszDebugClassName, pMethodDesc->m_pszDebugMethodName,
            t1NativeCodeVersion.GetVersionId()));

        // The thread is in a GC_NOTRIGGER scope here. If the background worker is already running, we can schedule it inside
        // the same lock without triggering a GC.
        if (TryScheduleBackgroundWorkerWithoutGCTrigger_Locked())
        {
            return;
        }
    }

    // This function is called from a GC_NOTRIGGER scope and creating the background worker (creating a thread) may GC_TRIGGERS.
    // The caller needs to create the background worker after leaving the GC_NOTRIGGER scope. The contract is that the caller
    // must make an attempt to create the background worker in any normal path. In the event of an atypical exception (eg. OOM),
    // the background worker may not be created and would have to be tried again the next time some background work is queued.
    *createTieringBackgroundWorkerRef = true;
}

bool TieredCompilationManager::TryScheduleBackgroundWorkerWithoutGCTrigger_Locked()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(IsLockOwnedByCurrentThread());

    if (s_isBackgroundWorkerProcessingWork)
    {
        _ASSERTE(s_isBackgroundWorkerRunning);
        return true;
    }

    if (s_isBackgroundWorkerRunning)
    {
        s_isBackgroundWorkerProcessingWork = true;
        s_backgroundWorkAvailableEvent.Set();
        return true;
    }

    s_isBackgroundWorkerRunning = true;
    s_isBackgroundWorkerProcessingWork = true;
    return false; // it's the caller's responsibility to call CreateBackgroundWorker() after leaving the GC_NOTRIGGER region
}

void TieredCompilationManager::CreateBackgroundWorker()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    _ASSERTE(!IsLockOwnedByCurrentThread());
    _ASSERTE(s_isBackgroundWorkerRunning);
    _ASSERTE(s_isBackgroundWorkerProcessingWork);
    _ASSERTE(s_backgroundWorkerThread == nullptr);

    EX_TRY
    {
        if (!s_backgroundWorkAvailableEvent.IsValid())
        {
            // An auto-reset event is used since it's a bit easier to manage and felt more natural in this case. It is also
            // possible to use a manual-reset event instead, though there doesn't appear to be anything to gain from doing so.
            s_backgroundWorkAvailableEvent.CreateAutoEvent(false);
        }

        Thread *newThread = SetupUnstartedThread();
        _ASSERTE(newThread != nullptr);
        INDEBUG(s_backgroundWorkerThread = newThread);
    #ifdef FEATURE_COMINTEROP
        newThread->SetApartment(Thread::AS_InMTA);
    #endif
        newThread->SetBackground(true);

        if (!newThread->CreateNewThread(0, BackgroundWorkerBootstrapper0, newThread, W(".NET Tiered Compilation Worker")))
        {
            newThread->DecExternalCount(false);
            ThrowOutOfMemory();
        }

        newThread->StartThread();
    }
    EX_CATCH
    {
        {
            LockHolder tieredCompilationLockHolder;

            s_isBackgroundWorkerProcessingWork = false;
            s_isBackgroundWorkerRunning = false;
            INDEBUG(s_backgroundWorkerThread = nullptr);
        }

        EX_RETHROW;
    }
    EX_END_CATCH(RethrowTerminalExceptions);
}

DWORD WINAPI TieredCompilationManager::BackgroundWorkerBootstrapper0(LPVOID args)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    _ASSERTE(args != nullptr);
    Thread *thread = (Thread *)args;
    _ASSERTE(s_backgroundWorkerThread == thread);

    if (!thread->HasStarted())
    {
        LockHolder tieredCompilationLockHolder;

        s_isBackgroundWorkerProcessingWork = false;
        s_isBackgroundWorkerRunning = false;
        INDEBUG(s_backgroundWorkerThread = nullptr);
        return 0;
    }

    _ASSERTE(GetThread() == thread);
    ManagedThreadBase::KickOff(BackgroundWorkerBootstrapper1, nullptr);

    GCX_PREEMP_NO_DTOR();

    DestroyThread(thread);
    return 0;
}

void TieredCompilationManager::BackgroundWorkerBootstrapper1(LPVOID)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_COOPERATIVE;
    }
    CONTRACTL_END;

    GCX_PREEMP();
    GetAppDomain()->GetTieredCompilationManager()->BackgroundWorkerStart();
}

void TieredCompilationManager::BackgroundWorkerStart()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    _ASSERTE(s_backgroundWorkAvailableEvent.IsValid());

    DWORD timeoutMs = g_pConfig->TieredCompilation_BackgroundWorkerTimeoutMs();
    DWORD delayMs = g_pConfig->TieredCompilation_CallCountingDelayMs();

    int processorCount = GetCurrentProcessCpuCount();
    _ASSERTE(processorCount > 0);

    LARGE_INTEGER li;
    QueryPerformanceFrequency(&li);
    UINT64 ticksPerS = li.QuadPart;
    UINT64 maxWorkDurationTicks = ticksPerS * 50 / 1000; // 50 ms
    UINT64 minWorkDurationTicks = min(ticksPerS * processorCount / 1000, maxWorkDurationTicks); // <proc count> ms (capped)
    UINT64 workDurationTicks = minWorkDurationTicks;

    while (true)
    {
        _ASSERTE(s_isBackgroundWorkerRunning);
        _ASSERTE(s_isBackgroundWorkerProcessingWork);

        if (IsTieringDelayActive())
        {
            do
            {
                ClrSleepEx(delayMs, false);
            } while (!TryDeactivateTieringDelay());
        }

        // Don't want to perform background work as soon as it is scheduled if there is possibly more important work that could
        // be done. Some operating systems may also give a thread woken by a signal higher priority temporarily, which on a
        // CPU-limited environment may lead to rejitting a method as soon as it's promoted, effectively in the foreground.
        ClrSleepEx(0, false);

        if (IsTieringDelayActive())
        {
            continue;
        }

        if ((m_isPendingCallCountingCompletion || m_countOfMethodsToOptimize != 0) &&
            !DoBackgroundWork(&workDurationTicks, minWorkDurationTicks, maxWorkDurationTicks))
        {
            // Background work was interrupted due to the tiering delay being activated
            _ASSERTE(IsTieringDelayActive());
            continue;
        }

        {
            LockHolder tieredCompilationLockHolder;

            if (IsTieringDelayActive() || m_isPendingCallCountingCompletion || m_countOfMethodsToOptimize != 0)
            {
                continue;
            }

            s_isBackgroundWorkerProcessingWork = false;
        }

        // Wait for the worker to be scheduled again
        DWORD waitResult = s_backgroundWorkAvailableEvent.Wait(timeoutMs, false);
        if (waitResult == WAIT_OBJECT_0)
        {
            continue;
        }

        // The wait timed out, see if the worker can exit. When using the PAL, it may be possible to get WAIT_FAILED in some
        // shutdown scenarios, treat that as a timeout too since a signal would not have been observed anyway.

        LockHolder tieredCompilationLockHolder;

        if (s_isBackgroundWorkerProcessingWork)
        {
            // The background worker got scheduled again just as the wait timed out. The event would have been signaled just
            // after the wait had timed out, so reset it and continue processing work.
            s_backgroundWorkAvailableEvent.Reset();
            continue;
        }

        s_isBackgroundWorkerRunning = false;
        INDEBUG(s_backgroundWorkerThread = nullptr);
        return;
    }
}

bool TieredCompilationManager::IsTieringDelayActive()
{
    LIMITED_METHOD_CONTRACT;
    return m_methodsPendingCountingForTier1 != nullptr;
}

bool TieredCompilationManager::TryDeactivateTieringDelay()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    _ASSERTE(GetThread() == s_backgroundWorkerThread);

    SArray<MethodDesc *> *methodsPendingCounting = nullptr;
    UINT32 countOfNewMethodsCalledDuringDelay = 0;
    {
        // It's possible for the timer to tick before it is recorded that the delay is in effect. This lock guarantees that
        // the delay is in effect.
        LockHolder tieredCompilationLockHolder;
        _ASSERTE(IsTieringDelayActive());

        if (m_tier1CallCountingCandidateMethodRecentlyRecorded)
        {
            m_tier1CallCountingCandidateMethodRecentlyRecorded = false;
            return false;
        }

        // Exchange information into locals inside the lock

        methodsPendingCounting = m_methodsPendingCountingForTier1;
        _ASSERTE(methodsPendingCounting != nullptr);
        m_methodsPendingCountingForTier1 = nullptr;

        countOfNewMethodsCalledDuringDelay = m_countOfNewMethodsCalledDuringDelay;
        m_countOfNewMethodsCalledDuringDelay = 0;

        _ASSERTE(!IsTieringDelayActive());
    }

    if (ETW::CompilationLog::TieredCompilation::Runtime::IsEnabled())
    {
        ETW::CompilationLog::TieredCompilation::Runtime::SendResume(countOfNewMethodsCalledDuringDelay);
    }

    // Install call counters
    {
        MethodDesc** methods = methodsPendingCounting->GetElements();
        COUNT_T methodCount = methodsPendingCounting->GetCount();
        CodeVersionManager *codeVersionManager = GetAppDomain()->GetCodeVersionManager();

        MethodDescBackpatchInfoTracker::ConditionalLockHolder slotBackpatchLockHolder;
        CodeVersionManager::LockHolder codeVersioningLockHolder;

        for (COUNT_T i = 0; i < methodCount; ++i)
        {
            MethodDesc *methodDesc = methods[i];
            _ASSERTE(codeVersionManager == methodDesc->GetCodeVersionManager());
            NativeCodeVersion activeCodeVersion =
                codeVersionManager->GetActiveILCodeVersion(methodDesc).GetActiveNativeCodeVersion(methodDesc);
            if (activeCodeVersion.IsNull())
            {
                continue;
            }

            EX_TRY
            {
                bool wasSet =
                    CallCountingManager::SetCodeEntryPoint(activeCodeVersion, activeCodeVersion.GetNativeCode(), false, nullptr);
                _ASSERTE(wasSet);
            }
            EX_CATCH
            {
                STRESS_LOG1(LF_TIEREDCOMPILATION, LL_WARNING, "TieredCompilationManager::DeactivateTieringDelay: "
                    "Exception in CallCountingManager::SetCodeEntryPoint, hr=0x%x\n",
                    GET_EXCEPTION()->GetHR());
            }
            EX_END_CATCH(RethrowTerminalExceptions);
        }
    }

    delete methodsPendingCounting;
    return true;
}

void TieredCompilationManager::AsyncCompleteCallCounting()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    {
        LockHolder tieredCompilationLockHolder;

        if (m_recentlyRequestedCallCountingCompletion)
        {
            _ASSERTE(m_isPendingCallCountingCompletion);
        }
        else
        {
            m_isPendingCallCountingCompletion = true;

            // A potentially large number of methods may reach the call count threshold at about the same time or in bursts.
            // This field is used to coalesce a burst of pending completions, see the background work.
            m_recentlyRequestedCallCountingCompletion = true;
        }

        // The thread is in a GC_NOTRIGGER scope here. If the background worker is already running, we can schedule it inside
        // the same lock without triggering a GC.
        if (TryScheduleBackgroundWorkerWithoutGCTrigger_Locked())
        {
            return;
        }
    }

    CreateBackgroundWorker(); // requires GC_TRIGGERS
}

//This method will process one or more methods from optimization queue
// on a background thread. Each such method will be jitted with code
// optimizations enabled and then installed as the active implementation
// of the method entrypoint.
bool TieredCompilationManager::DoBackgroundWork(
    UINT64 *workDurationTicksRef,
    UINT64 minWorkDurationTicks,
    UINT64 maxWorkDurationTicks)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(GetThread() == s_backgroundWorkerThread);
    _ASSERTE(m_isPendingCallCountingCompletion || m_countOfMethodsToOptimize != 0);
    _ASSERTE(workDurationTicksRef != nullptr);
    _ASSERTE(minWorkDurationTicks <= maxWorkDurationTicks);

    UINT64 workDurationTicks = *workDurationTicksRef;
    _ASSERTE(workDurationTicks >= minWorkDurationTicks);
    _ASSERTE(workDurationTicks <= maxWorkDurationTicks);

    if (ETW::CompilationLog::TieredCompilation::Runtime::IsEnabled())
    {
        UINT32 countOfMethodsToOptimize = m_countOfMethodsToOptimize;
        if (m_isPendingCallCountingCompletion)
        {
            countOfMethodsToOptimize += CallCountingManager::GetCountOfCodeVersionsPendingCompletion();
        }
        ETW::CompilationLog::TieredCompilation::Runtime::SendBackgroundJitStart(countOfMethodsToOptimize);
    }

    bool sendStopEvent = true;
    bool allMethodsJitted = false;
    UINT32 jittedMethodCount = 0;
    LARGE_INTEGER li;
    QueryPerformanceCounter(&li);
    UINT64 startTicks = li.QuadPart;
    UINT64 previousTicks = startTicks;

    do
    {
        bool completeCallCounting = false;
        NativeCodeVersion nativeCodeVersionToOptimize;
        {
            LockHolder tieredCompilationLockHolder;

            if (IsTieringDelayActive())
            {
                break;
            }

            bool wasPendingCallCountingCompletion = m_isPendingCallCountingCompletion;
            if (wasPendingCallCountingCompletion)
            {
                if (m_recentlyRequestedCallCountingCompletion)
                {
                    // A potentially large number of methods may reach the call count threshold at about the same time or in
                    // bursts. To coalesce a burst of pending completions a bit, if another method has reached the call count
                    // threshold since the last time it was checked here, don't complete call counting yet. Coalescing
                    // call counting completions a bit helps to avoid blocking foreground threads due to lock contention as
                    // methods are continuing to reach the call count threshold.
                    m_recentlyRequestedCallCountingCompletion = false;
                }
                else
                {
                    m_isPendingCallCountingCompletion = false;
                    completeCallCounting = true;
                }
            }

            if (!completeCallCounting)
            {
                nativeCodeVersionToOptimize = GetNextMethodToOptimize();
                if (nativeCodeVersionToOptimize.IsNull())
                {
                    // Ran out of methods to JIT
                    if (wasPendingCallCountingCompletion)
                    {
                        // If call counting completions are pending and delayed above for coalescing, complete call counting
                        // now, as that will add more methods to be rejitted
                        m_isPendingCallCountingCompletion = false;
                        _ASSERTE(!m_recentlyRequestedCallCountingCompletion);
                        completeCallCounting = true;
                    }
                    else
                    {
                        allMethodsJitted = true;
                        break;
                    }
                }
            }
        }

        _ASSERTE(completeCallCounting == !!nativeCodeVersionToOptimize.IsNull());
        if (completeCallCounting)
        {
            EX_TRY
            {
                CallCountingManager::CompleteCallCounting();
            }
            EX_CATCH
            {
                STRESS_LOG1(LF_TIEREDCOMPILATION, LL_WARNING, "TieredCompilationManager::DoBackgroundWork: "
                    "Exception in CallCountingManager::CompleteCallCounting, hr=0x%x\n",
                    GET_EXCEPTION()->GetHR());
            }
            EX_END_CATCH(RethrowTerminalExceptions);

            continue;
        }

        OptimizeMethod(nativeCodeVersionToOptimize);
        ++jittedMethodCount;

        // Yield the thread periodically to give preference to possibly more important work

        QueryPerformanceCounter(&li);
        UINT64 currentTicks = li.QuadPart;
        if (currentTicks - startTicks < workDurationTicks)
        {
            previousTicks = currentTicks;
            continue;
        }
        if (currentTicks - previousTicks >= maxWorkDurationTicks)
        {
            // It's unlikely that one iteration above would have taken that long, more likely this thread got scheduled out for
            // a while, in which case there is no need to yield again. Discount the time taken for the previous iteration and
            // continue processing work.
            startTicks += currentTicks - previousTicks;
            previousTicks = currentTicks;
            continue;
        }

        if (ETW::CompilationLog::TieredCompilation::Runtime::IsEnabled())
        {
            UINT32 countOfMethodsToOptimize = m_countOfMethodsToOptimize;
            if (m_isPendingCallCountingCompletion)
            {
                countOfMethodsToOptimize += CallCountingManager::GetCountOfCodeVersionsPendingCompletion();
            }
            ETW::CompilationLog::TieredCompilation::Runtime::SendBackgroundJitStop(countOfMethodsToOptimize, jittedMethodCount);
        }

        UINT64 beforeSleepTicks = currentTicks;
        ClrSleepEx(0, false);

        QueryPerformanceCounter(&li);
        currentTicks = li.QuadPart;

        // Depending on how oversubscribed thread usage is on the system, the sleep may have caused this thread to not be
        // scheduled for a long time. Yielding the thread too frequently may significantly slow down the background work, which
        // may significantly delay how long it takes to reach steady-state performance. On the other hand, yielding the thread
        // too infrequently may cause the background work to monopolize the available CPU resources and prevent more important
        // foreground work from occurring. So the sleep duration is measured and for the next batch of background work, at least
        // a portion of that measured duration is used (within the min and max to keep things sensible). Since the background
        // work duration is capped to a maximum and since a long sleep delay is likely to repeat, to avoid going back to
        // too-frequent yielding too quickly, the background work duration is decayed back to the minimum if the sleep duration
        // becomes consistently short.
        UINT64 newWorkDurationTicks = (currentTicks - beforeSleepTicks) / 4;
        UINT64 decayedWorkDurationTicks = (workDurationTicks + workDurationTicks / 2) / 2;
        workDurationTicks = newWorkDurationTicks < decayedWorkDurationTicks ? decayedWorkDurationTicks : newWorkDurationTicks;
        if (workDurationTicks < minWorkDurationTicks)
        {
            workDurationTicks = minWorkDurationTicks;
        }
        else if (workDurationTicks > maxWorkDurationTicks)
        {
            workDurationTicks = maxWorkDurationTicks;
        }

        if (IsTieringDelayActive())
        {
            sendStopEvent = false;
            break;
        }

        if (ETW::CompilationLog::TieredCompilation::Runtime::IsEnabled())
        {
            UINT32 countOfMethodsToOptimize = m_countOfMethodsToOptimize;
            if (m_isPendingCallCountingCompletion)
            {
                countOfMethodsToOptimize += CallCountingManager::GetCountOfCodeVersionsPendingCompletion();
            }
            ETW::CompilationLog::TieredCompilation::Runtime::SendBackgroundJitStart(countOfMethodsToOptimize);
        }

        jittedMethodCount = 0;
        startTicks = previousTicks = currentTicks;
    } while (!IsTieringDelayActive());

    if (ETW::CompilationLog::TieredCompilation::Runtime::IsEnabled() && sendStopEvent)
    {
        UINT32 countOfMethodsToOptimize = m_countOfMethodsToOptimize;
        if (m_isPendingCallCountingCompletion)
        {
            countOfMethodsToOptimize += CallCountingManager::GetCountOfCodeVersionsPendingCompletion();
        }
        ETW::CompilationLog::TieredCompilation::Runtime::SendBackgroundJitStop(countOfMethodsToOptimize, jittedMethodCount);
    }

    if (allMethodsJitted)
    {
        EX_TRY
        {
            CallCountingManager::StopAndDeleteAllCallCountingStubs();
        }
        EX_CATCH
        {
            STRESS_LOG1(LF_TIEREDCOMPILATION, LL_WARNING, "TieredCompilationManager::DoBackgroundWork: "
                "Exception in CallCountingManager::StopAndDeleteAllCallCountingStubs, hr=0x%x\n",
                GET_EXCEPTION()->GetHR());
        }
        EX_END_CATCH(RethrowTerminalExceptions);
    }

    *workDurationTicksRef = workDurationTicks;
    return allMethodsJitted;
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

        // This is a recompiling request which means the caller was
        // in COOP mode since the code already ran.
        _ASSERTE(!pMethod->HasUnmanagedCallersOnlyAttribute());
        config->SetCallerGCMode(CallerGCMode::Coop);
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

    // If the ilParent version is active this will activate the native code version now.
    // Otherwise if the ilParent version becomes active again in the future the native
    // code version will activate then.
    ILCodeVersion ilParent;
    HRESULT hr = S_OK;
    {
        bool mayHaveEntryPointSlotsToBackpatch = pMethod->MayHaveEntryPointSlotsToBackpatch();
        MethodDescBackpatchInfoTracker::ConditionalLockHolder slotBackpatchLockHolder(mayHaveEntryPointSlotsToBackpatch);
        CodeVersionManager::LockHolder codeVersioningLockHolder;

        // As long as we are exclusively using any non-JumpStamp publishing for tiered compilation
        // methods this first attempt should succeed
        ilParent = nativeCodeVersion.GetILCodeVersion();
        hr = ilParent.SetActiveNativeCodeVersion(nativeCodeVersion);
        LOG((LF_TIEREDCOMPILATION, LL_INFO10000, "TieredCompilationManager::ActivateCodeVersion Method=0x%pM (%s::%s), code version id=0x%x. SetActiveNativeCodeVersion ret=0x%x\n",
            pMethod, pMethod->m_pszDebugClassName, pMethod->m_pszDebugMethodName,
            nativeCodeVersion.GetVersionId(),
            hr));
    }
    if (FAILED(hr))
    {
        STRESS_LOG2(LF_TIEREDCOMPILATION, LL_INFO10, "TieredCompilationManager::ActivateCodeVersion: "
            "Method %pM failed to publish native code for native code version %d\n",
            pMethod, nativeCodeVersion.GetVersionId());
    }
}

// Dequeues the next method in the optimization queue.
// This runs on the background thread.
NativeCodeVersion TieredCompilationManager::GetNextMethodToOptimize()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    _ASSERTE(IsLockOwnedByCurrentThread());

    SListElem<NativeCodeVersion>* pElem = m_methodsToOptimize.RemoveHead();
    if (pElem != NULL)
    {
        NativeCodeVersion nativeCodeVersion = pElem->GetValue();
        delete pElem;
        _ASSERTE(m_countOfMethodsToOptimize != 0);
        --m_countOfMethodsToOptimize;
        return nativeCodeVersion;
    }
    return NativeCodeVersion();
}

//static
CORJIT_FLAGS TieredCompilationManager::GetJitFlags(PrepareCodeConfig *config)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(config != nullptr);
    _ASSERTE(
        !config->WasTieringDisabledBeforeJitting() ||
        config->GetCodeVersion().GetOptimizationTier() != NativeCodeVersion::OptimizationTier0);

    CORJIT_FLAGS flags;

    // Determine the optimization tier for the default code version (slightly faster common path during startup compared to
    // below), and disable call counting and set the optimization tier if it's not going to be tier 0 (this is used in other
    // places for the default code version where necessary to avoid the extra expense of GetOptimizationTier()).
    NativeCodeVersion nativeCodeVersion = config->GetCodeVersion();
    if (nativeCodeVersion.IsDefaultVersion() && !config->WasTieringDisabledBeforeJitting())
    {
        MethodDesc *methodDesc = nativeCodeVersion.GetMethodDesc();
        if (!methodDesc->IsEligibleForTieredCompilation())
        {
            _ASSERTE(nativeCodeVersion.GetOptimizationTier() == NativeCodeVersion::OptimizationTierOptimized);
        #ifdef FEATURE_INTERPRETER
            flags.Set(CORJIT_FLAGS::CORJIT_FLAG_MAKEFINALCODE);
        #endif
            return flags;
        }

        NativeCodeVersion::OptimizationTier newOptimizationTier;
        if (!methodDesc->RequestedAggressiveOptimization())
        {
            if (g_pConfig->TieredCompilation_QuickJit())
            {
                _ASSERTE(nativeCodeVersion.GetOptimizationTier() == NativeCodeVersion::OptimizationTier0);
                flags.Set(CORJIT_FLAGS::CORJIT_FLAG_TIER0);
                return flags;
            }

            newOptimizationTier = NativeCodeVersion::OptimizationTierOptimized;
        }
        else
        {
            newOptimizationTier = NativeCodeVersion::OptimizationTier1;
            flags.Set(CORJIT_FLAGS::CORJIT_FLAG_TIER1);
        }

        methodDesc->GetLoaderAllocator()->GetCallCountingManager()->DisableCallCounting(nativeCodeVersion);
        nativeCodeVersion.SetOptimizationTier(newOptimizationTier);
    #ifdef FEATURE_INTERPRETER
        flags.Set(CORJIT_FLAGS::CORJIT_FLAG_MAKEFINALCODE);
    #endif
        return flags;
    }

    switch (nativeCodeVersion.GetOptimizationTier())
    {
        case NativeCodeVersion::OptimizationTier0:
            if (g_pConfig->TieredCompilation_QuickJit())
            {
                flags.Set(CORJIT_FLAGS::CORJIT_FLAG_TIER0);
                break;
            }

            nativeCodeVersion.SetOptimizationTier(NativeCodeVersion::OptimizationTierOptimized);
            goto Optimized;

#ifdef FEATURE_ON_STACK_REPLACEMENT
        case NativeCodeVersion::OptimizationTier1OSR:
            flags.Set(CORJIT_FLAGS::CORJIT_FLAG_OSR);
            FALLTHROUGH;
#endif

        case NativeCodeVersion::OptimizationTier1:
            flags.Set(CORJIT_FLAGS::CORJIT_FLAG_TIER1);
            FALLTHROUGH;

        case NativeCodeVersion::OptimizationTierOptimized:
        Optimized:
#ifdef FEATURE_INTERPRETER
            flags.Set(CORJIT_FLAGS::CORJIT_FLAG_MAKEFINALCODE);
#endif
            break;

        default:
            UNREACHABLE();
    }
    return flags;
}

#ifdef _DEBUG
bool TieredCompilationManager::IsLockOwnedByCurrentThread()
{
    WRAPPER_NO_CONTRACT;
    return !!s_lock.OwnedByCurrentThread();
}
#endif // _DEBUG

#endif // FEATURE_TIERED_COMPILATION && !DACCESS_COMPILE
