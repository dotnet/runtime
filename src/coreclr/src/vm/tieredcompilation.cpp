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

class TieredCompilationManager::AutoResetIsBackgroundWorkScheduled
{
private:
    TieredCompilationManager *m_tieredCompilationManager;

public:
    AutoResetIsBackgroundWorkScheduled(TieredCompilationManager *tieredCompilationManager)
        : m_tieredCompilationManager(tieredCompilationManager)
    {
        LIMITED_METHOD_CONTRACT;
        _ASSERTE(tieredCompilationManager == nullptr || tieredCompilationManager->m_isBackgroundWorkScheduled);
    }

    ~AutoResetIsBackgroundWorkScheduled()
    {
        WRAPPER_NO_CONTRACT;

        if (m_tieredCompilationManager == nullptr)
        {
            return;
        }

        LockHolder tieredCompilationLockHolder;

        _ASSERTE(m_tieredCompilationManager->m_isBackgroundWorkScheduled);
        m_tieredCompilationManager->m_isBackgroundWorkScheduled = false;
    }

    void Cancel()
    {
        LIMITED_METHOD_CONTRACT;
        m_tieredCompilationManager = nullptr;
    }
};

// Called at AppDomain construction
TieredCompilationManager::TieredCompilationManager() :
    m_countOfMethodsToOptimize(0),
    m_countOfNewMethodsCalledDuringDelay(0),
    m_methodsPendingCountingForTier1(nullptr),
    m_tieringDelayTimerHandle(nullptr),
    m_isBackgroundWorkScheduled(false),
    m_tier1CallCountingCandidateMethodRecentlyRecorded(false),
    m_isPendingCallCountingCompletion(false),
    m_recentlyRequestedCallCountingCompletionAgain(false)
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
    WRAPPER_NO_CONTRACT;
    _ASSERTE(pMethodDesc != nullptr);
    _ASSERTE(pMethodDesc->IsEligibleForTieredCompilation());
    _ASSERTE(g_pConfig->TieredCompilation_CallCountingDelayMs() != 0);

    // An exception here (OOM) would mean that the method's calls would not be counted and it would not be promoted. A
    // consideration is that an attempt can be made to reset the code entry point on exception (which can also OOM). Doesn't
    // seem worth it, the exception is propagated and there are other cases where a method may not be promoted due to OOM.
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
    }

    // Elsewhere, the tiered compilation lock is taken inside the code versioning lock. The code versioning lock is an unsafe
    // any-GC-mode lock, so the tiering lock is also that type of lock. Inside that type of lock, there is an implicit
    // GC_NOTRIGGER contract. So, the timer cannot be created inside the tiering lock since it may GC_TRIGGERS. At this point,
    // this is the only thread that may attempt creating the timer. If creating the timer fails, let the exception propagate,
    // but because the tiering lock was released above, first reset any recorded methods' code entry points and deactivate the
    // tiering delay so that timer creation may be attempted again.
    EX_TRY
    {
        NewHolder<ThreadpoolMgr::TimerInfoContext> timerContextHolder = new ThreadpoolMgr::TimerInfoContext();
        timerContextHolder->TimerId = 0;

        _ASSERTE(m_tieringDelayTimerHandle == nullptr);
        if (!ThreadpoolMgr::CreateTimerQueueTimer(
                &m_tieringDelayTimerHandle,
                TieringDelayTimerCallback,
                timerContextHolder,
                g_pConfig->TieredCompilation_CallCountingDelayMs(),
                (DWORD)-1 /* Period, non-repeating */,
                0 /* flags */))
        {
            _ASSERTE(m_tieringDelayTimerHandle == nullptr);
            ThrowOutOfMemory();
        }

        timerContextHolder.SuppressRelease(); // the timer context is automatically deleted by the timer infrastructure
    }
    EX_CATCH
    {
        // Since the tiering lock was released and reacquired, other methods may have been recorded in-between. Just deactivate
        // the tiering delay. Any methods that have been recorded would not have their calls be counted and would not be
        // promoted (due to the small window, there shouldn't be many of those). See consideration above in a similar exception
        // case.
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
    bool *scheduleTieringBackgroundWorkRef)
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
    _ASSERTE(scheduleTieringBackgroundWorkRef != nullptr);

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
    //
    // Note an error here could affect concurrent threads running this
    // code. Those threads will observe m_isBackgroundWorkScheduled == true and return,
    // then QueueUserWorkItem fails on this thread resetting the field to false and leaves them
    // unserviced. Synchronous retries appear unlikely to offer any material improvement
    // and complicating the code to narrow an already rare error case isn't desirable.
    SListElem<NativeCodeVersion>* pMethodListItem = new SListElem<NativeCodeVersion>(t1NativeCodeVersion);
    {
        LockHolder tieredCompilationLockHolder;

        m_methodsToOptimize.InsertTail(pMethodListItem);
        ++m_countOfMethodsToOptimize;

        LOG((LF_TIEREDCOMPILATION, LL_INFO10000, "TieredCompilationManager::AsyncPromoteToTier1 Method=0x%pM (%s::%s), code version id=0x%x queued\n",
            pMethodDesc, pMethodDesc->m_pszDebugClassName, pMethodDesc->m_pszDebugMethodName,
            t1NativeCodeVersion.GetVersionId()));

        if (m_isBackgroundWorkScheduled || IsTieringDelayActive())
        {
            return;
        }
    }

    // This function is called from a GC_NOTRIGGER scope and scheduling background work (creating a thread) may GC_TRIGGERS.
    // The caller needs to schedule background work after leaving the GC_NOTRIGGER scope. The contract is that the caller must
    // make an attempt to schedule background work in any normal path. In the event of an atypical exception (eg. OOM),
    // background work may not be scheduled and would have to be tried again the next time some background work is queued.
    if (!*scheduleTieringBackgroundWorkRef)
    {
        *scheduleTieringBackgroundWorkRef = true;
    }
}

bool TieredCompilationManager::IsTieringDelayActive()
{
    LIMITED_METHOD_CONTRACT;
    return m_methodsPendingCountingForTier1 != nullptr;
}

void WINAPI TieredCompilationManager::TieringDelayTimerCallback(PVOID parameter, BOOLEAN timerFired)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    _ASSERTE(timerFired);

    GetAppDomain()->GetTieredCompilationManager()->DeactivateTieringDelay();
}

void TieredCompilationManager::DeactivateTieringDelay()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_PREEMPTIVE;
    }
    CONTRACTL_END;

    HANDLE tieringDelayTimerHandle = nullptr;
    SArray<MethodDesc *> *methodsPendingCounting = nullptr;
    UINT32 countOfNewMethodsCalledDuringDelay = 0;
    bool doBackgroundWork = false;
    while (true)
    {
        {
            // It's possible for the timer to tick before it is recorded that the delay is in effect. This lock guarantees that
            // the delay is in effect.
            LockHolder tieredCompilationLockHolder;
            _ASSERTE(IsTieringDelayActive());

            tieringDelayTimerHandle = m_tieringDelayTimerHandle;
            if (m_tier1CallCountingCandidateMethodRecentlyRecorded)
            {
                m_tier1CallCountingCandidateMethodRecentlyRecorded = false;
            }
            else
            {
                // Exchange information into locals inside the lock

                methodsPendingCounting = m_methodsPendingCountingForTier1;
                _ASSERTE(methodsPendingCounting != nullptr);
                m_methodsPendingCountingForTier1 = nullptr;

                _ASSERTE(tieringDelayTimerHandle == m_tieringDelayTimerHandle);
                m_tieringDelayTimerHandle = nullptr;

                countOfNewMethodsCalledDuringDelay = m_countOfNewMethodsCalledDuringDelay;
                m_countOfNewMethodsCalledDuringDelay = 0;

                _ASSERTE(!IsTieringDelayActive());

                if (!m_isBackgroundWorkScheduled && (m_isPendingCallCountingCompletion || m_countOfMethodsToOptimize != 0))
                {
                    m_isBackgroundWorkScheduled = true;
                    doBackgroundWork = true;
                }

                break;
            }
        }

        // Reschedule the timer if there has been recent tier 0 activity (when a new eligible method is called the first
        // time) to further delay call counting
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

    AutoResetIsBackgroundWorkScheduled autoResetIsBackgroundWorkScheduled(doBackgroundWork ? this : nullptr);

    if (ETW::CompilationLog::TieredCompilation::Runtime::IsEnabled())
    {
        ETW::CompilationLog::TieredCompilation::Runtime::SendResume(countOfNewMethodsCalledDuringDelay);
    }

    // Install call counters
    {
        MethodDesc** methods = methodsPendingCounting->GetElements();
        COUNT_T methodCount = methodsPendingCounting->GetCount();
        CodeVersionManager *codeVersionManager = GetAppDomain()->GetCodeVersionManager();

        MethodDescBackpatchInfoTracker::PollForDebuggerSuspension();
        MethodDescBackpatchInfoTracker::ConditionalLockHolder slotBackpatchLockHolder;

        // Backpatching entry point slots requires cooperative GC mode, see
        // MethodDescBackpatchInfoTracker::Backpatch_Locked(). The code version manager's table lock is an unsafe lock that
        // may be taken in any GC mode. The lock is taken in cooperative GC mode on some other paths, so the same ordering
        // must be used here to prevent deadlock.
        GCX_COOP();
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
    ThreadpoolMgr::DeleteTimerQueueTimer(tieringDelayTimerHandle, nullptr);

    if (doBackgroundWork)
    {
        autoResetIsBackgroundWorkScheduled.Cancel(); // the call below will take care of it
        DoBackgroundWork();
    }
}

void TieredCompilationManager::AsyncCompleteCallCounting()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    {
        LockHolder tieredCompilationLockHolder;

        if (m_recentlyRequestedCallCountingCompletionAgain)
        {
            _ASSERTE(m_isPendingCallCountingCompletion);
        }
        else if (m_isPendingCallCountingCompletion)
        {
            // A potentially large number of methods may reach the call count threshold at about the same time or in bursts.
            // This field is used to coalesce a burst of pending completions, see the background work.
            m_recentlyRequestedCallCountingCompletionAgain = true;
        }
        else
        {
            m_isPendingCallCountingCompletion = true;
        }

        if (m_isBackgroundWorkScheduled || IsTieringDelayActive())
        {
            return;
        }
        m_isBackgroundWorkScheduled = true;
    }

    AutoResetIsBackgroundWorkScheduled autoResetIsBackgroundWorkScheduled(this);
    RequestBackgroundWork();
    autoResetIsBackgroundWorkScheduled.Cancel();
}

void TieredCompilationManager::ScheduleBackgroundWork()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    {
        LockHolder tieredCompilationLockHolder;

        if (m_isBackgroundWorkScheduled ||
            (!m_isPendingCallCountingCompletion && m_countOfMethodsToOptimize == 0) ||
            IsTieringDelayActive())
        {
            return;
        }
        m_isBackgroundWorkScheduled = true;
    }

    AutoResetIsBackgroundWorkScheduled autoResetIsBackgroundWorkScheduled(this);
    RequestBackgroundWork();
    autoResetIsBackgroundWorkScheduled.Cancel();
}

void TieredCompilationManager::RequestBackgroundWork()
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(m_isBackgroundWorkScheduled);

    if (!ThreadpoolMgr::QueueUserWorkItem(StaticBackgroundWorkCallback, this, QUEUE_ONLY, TRUE))
    {
        ThrowOutOfMemory();
    }
}

// This is the initial entrypoint for the background thread, called by
// the threadpool.
DWORD WINAPI TieredCompilationManager::StaticBackgroundWorkCallback(void *args)
{
    STANDARD_VM_CONTRACT;

    TieredCompilationManager * pTieredCompilationManager = (TieredCompilationManager *)args;
    pTieredCompilationManager->DoBackgroundWork();
    return 0;
}

//This method will process one or more methods from optimization queue
// on a background thread. Each such method will be jitted with code
// optimizations enabled and then installed as the active implementation
// of the method entrypoint.
void TieredCompilationManager::DoBackgroundWork()
{
    WRAPPER_NO_CONTRACT;

    AutoResetIsBackgroundWorkScheduled autoResetIsBackgroundWorkScheduled(this);

    // We need to be careful not to work for too long in a single invocation of this method or we could starve the thread pool
    // and force it to create unnecessary additional threads. We will JIT for a minimum of this quantum, then schedule another
    // work item to the thread pool and return this thread back to the pool.
    const DWORD OptimizationQuantumMs = 50;

    if (ETW::CompilationLog::TieredCompilation::Runtime::IsEnabled())
    {
        UINT32 countOfMethodsToOptimize = m_countOfMethodsToOptimize;
        if (m_isPendingCallCountingCompletion)
        {
            countOfMethodsToOptimize += CallCountingManager::GetCountOfCodeVersionsPendingCompletion();
        }
        ETW::CompilationLog::TieredCompilation::Runtime::SendBackgroundJitStart(countOfMethodsToOptimize);
    }

    bool allMethodsJitted = false;
    UINT32 jittedMethodCount = 0;
    DWORD startTickCount = GetTickCount();
    while (true)
    {
        bool completeCallCounting = false;
        NativeCodeVersion nativeCodeVersionToOptimize;
        {
            LockHolder tieredCompilationLockHolder;

            if (IsTieringDelayActive())
            {
                m_isBackgroundWorkScheduled = false;
                autoResetIsBackgroundWorkScheduled.Cancel();
                break;
            }

            bool wasPendingCallCountingCompletion = m_isPendingCallCountingCompletion;
            if (wasPendingCallCountingCompletion)
            {
                if (m_recentlyRequestedCallCountingCompletionAgain)
                {
                    // A potentially large number of methods may reach the call count threshold at about the same time or in
                    // bursts. To coalesce a burst of pending completions a bit, if another method has reached the call count
                    // threshold since the last time it was checked here, don't complete call counting yet. Coalescing
                    // call counting completions a bit helps to avoid blocking foreground threads due to lock contention as
                    // methods are continuing to reach the call count threshold.
                    m_recentlyRequestedCallCountingCompletionAgain = false;
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
                        _ASSERTE(!m_recentlyRequestedCallCountingCompletionAgain);
                        completeCallCounting = true;
                    }
                    else
                    {
                        m_isBackgroundWorkScheduled = false;
                        autoResetIsBackgroundWorkScheduled.Cancel();
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
        }
        else
        {
            OptimizeMethod(nativeCodeVersionToOptimize);
            ++jittedMethodCount;
        }

        // If we have been running for too long return the thread to the threadpool and queue another event
        // This gives the threadpool a chance to service other requests on this thread before returning to
        // this work.
        DWORD currentTickCount = GetTickCount();
        if (currentTickCount - startTickCount >= OptimizationQuantumMs)
        {
            bool success = false;
            EX_TRY
            {
                RequestBackgroundWork();
                success = true;
            }
            EX_CATCH
            {
                STRESS_LOG1(LF_TIEREDCOMPILATION, LL_WARNING, "TieredCompilationManager::DoBackgroundWork: "
                    "Exception in RequestBackgroundWork, hr=0x%x\n",
                    GET_EXCEPTION()->GetHR());
            }
            EX_END_CATCH(RethrowTerminalExceptions);
            if (success)
            {
                autoResetIsBackgroundWorkScheduled.Cancel();
                break;
            }

            startTickCount = currentTickCount;
        }
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

#if defined(TARGET_X86)
        // Deferring X86 support until a need is observed or
        // time permits investigation into all the potential issues.
        // https://github.com/dotnet/runtime/issues/33582
#else
        // This is a recompiling request which means the caller was
        // in COOP mode since the code already ran.
        _ASSERTE(!pMethod->HasUnmanagedCallersOnlyAttribute());
#endif
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
        if (mayHaveEntryPointSlotsToBackpatch)
        {
            MethodDescBackpatchInfoTracker::PollForDebuggerSuspension();
        }
        MethodDescBackpatchInfoTracker::ConditionalLockHolder slotBackpatchLockHolder(mayHaveEntryPointSlotsToBackpatch);

        // Backpatching entry point slots requires cooperative GC mode, see
        // MethodDescBackpatchInfoTracker::Backpatch_Locked(). The code version manager's table lock is an unsafe lock that
        // may be taken in any GC mode. The lock is taken in cooperative GC mode on some other paths, so the same ordering
        // must be used here to prevent deadlock.
        GCX_MAYBE_COOP(mayHaveEntryPointSlotsToBackpatch);
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

// Dequeues the next method in the optmization queue.
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

    // Determine the optimization tier for the default code version (slightly faster common path during startup compared to
    // below), and disable call counting and set the optimization tier if it's not going to be tier 0 (this is used in other
    // places for the default code version where necessary to avoid the extra expense of GetOptimizationTier()).
    if (nativeCodeVersion.IsDefaultVersion())
    {
        NativeCodeVersion::OptimizationTier newOptimizationTier;
        if (!methodDesc->RequestedAggressiveOptimization())
        {
            if (g_pConfig->TieredCompilation_QuickJit())
            {
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
            // fall through
#endif

        case NativeCodeVersion::OptimizationTier1:
            flags.Set(CORJIT_FLAGS::CORJIT_FLAG_TIER1);
            // fall through

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

CrstStatic TieredCompilationManager::s_lock;

#ifdef _DEBUG
bool TieredCompilationManager::IsLockOwnedByCurrentThread()
{
    WRAPPER_NO_CONTRACT;
    return !!s_lock.OwnedByCurrentThread();
}
#endif // _DEBUG

#endif // FEATURE_TIERED_COMPILATION && !DACCESS_COMPILE
