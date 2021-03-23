// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Summary:
// --------
//
// StackSampler is intended to identify methods where the process is spending most of its time
// and to re-JIT such methods in the background. Call these methods hot.
//
// Identifying hot methods:
// ========================
//
// There is no easy way to tell at a given point in execution whether in the future an unseen
// or un-hot method will become hot; So we track an evolving list of hot methods.
//
// We identify hot methods by suspending the runtime every "m" milliseconds. This operation
// freezes all the threads. We now get a list of threads that are executing and walk their
// stacks to get the managed method at the top of their stacks. The sampled list of methods
// for each thread constitute a single sample. Once we obtain a sample, the threads are thawed.
//
// The more a method is present in samples, it is clear that the process is spending its time
// in that method at several given points in time.
//
// We track this information on a per method basis, the count of its occurrences in each sample
// using a hash map.
//
// Note:
// =====
// o Using the above technique we have only identified top methods at a given point in the execution.
//   The list of hot methods keeps evolving as we get more samples. Only at the process end can we
//   say that the evolving list of hot methods is THE list of hot methods for the whole process.
// o Because we get the top managed method in the thread, this includes time spent by that method
//   in helper calls.
// o If GC is in progress it has suspended the threads, and we would not be able to suspend the threads.
//
// Future Consideration:
// =====================
// We could track "trending" methods, as methods decay out so we can keep only "trending" variants
// in the code manager and kick out "past" hot methods.
//
// Jitting in the background:
// ==========================
// Once we have the hot methods at a given point in time, we JIT them. The decision to JIT is configurable
// by configuring the number of times a method is seen in samples before we would JIT it.
// For example, if we are sampling every 10 msec and if we expect methods that spend at least 1 second
// to be hot, then the number of times to see this method is roughly, 100, it is best to be conservative
// with this number.
//
// Note that we JIT the evolving list of methods, without knowing ahead of time that the methods we JIT
// will be the final top "n" hot methods due to the lack of knowledge of when the process will end.
// This means we would over-JIT but this only yields results with false negatives.
//
// Currently we JIT in the background only once (with the current goal of getting a trace.)
//
// Note:
// =====
// o To run the JIT in the background, we try our best to JIT in the same app domain in which the original
//   JITting happened. But if we fail to acquire (ngen'ed method) or enter (unloaded domain) the original domain,
//   we then try to JIT it under the thread's app domain in which the method was last seen to be executing.
//
// o The JIT to use is configurable with COMPlus_AltJitName when COMPlus_StackSampling is enabled.
//
// o One use case is to collect traces as an .mc file from SuperPMI Shim JIT.
//
// Jitting parameters:
// ==========================
// The prestub tells us at JITting time using "RecordJittingInfo" to record the parameters used to JIT
// originally. We use these parameters to JIT in the background when we decide to JIT the method.
//


#include "common.h"
#include "corjit.h"
#include "stacksampler.h"
#include "threadsuspend.h"

#ifdef FEATURE_STACK_SAMPLING

// Global instance of the sampler
StackSampler* g_pStackSampler = nullptr;

// Create an instance of the stack sampler if sampling is enabled.
void StackSampler::Init()
{
    STANDARD_VM_CONTRACT;

    bool samplingEnabled = (CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_StackSamplingEnabled) != 0);
    if (samplingEnabled)
    {
        g_pStackSampler = new (nothrow) StackSampler();
    }
}

// ThreadProc for performing sampling and JITting.
/* static */
DWORD __stdcall StackSampler::SamplingThreadProc(void* arg)
{
    WRAPPER_NO_CONTRACT;

    StackSampler* pThis = (StackSampler*) arg;
    pThis->ThreadProc();
    return 0;
}

// Constructor
StackSampler::StackSampler()
    : m_crstJitInfo(CrstStackSampler, (CrstFlags)(CRST_UNSAFE_ANYMODE))
    , m_nSampleEvery(s_knDefaultSamplingIntervalMsec)
    , m_nSampleAfter(0)
    , m_nNumMethods(s_knDefaultNumMethods)
{
    // When to start sampling after the thread launch.
    int nSampleAfter = CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_StackSamplingAfter);
    if (nSampleAfter != INT_MAX && nSampleAfter >= 0)
    {
        m_nSampleAfter = nSampleAfter;
    }

    // How frequently to sample.
    int nSampleEvery = CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_StackSamplingEvery);
    if (nSampleEvery != INT_MAX && nSampleEvery > 0)
    {
        m_nSampleEvery = nSampleEvery;
    }

    // Max number of methods to track.
    int nNumMethods = CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_StackSamplingNumMethods);
    if (nNumMethods != INT_MAX && nNumMethods > 0)
    {
        m_nNumMethods = nNumMethods;
    }

    // Launch the thread.
    m_pThread = SetupUnstartedThread();
    m_pThread->SetBackground(TRUE);

    // Passing "this" to the thread in the constructor.
    if (m_pThread->CreateNewThread(1*1024*1024, SamplingThreadProc, this))
    {
        m_pThread->StartThread();
    }
}

// Is "pMD" a good method, that is suitable for tracking as HOT and
// JITting in the background.
bool IsGoodMethodDesc(MethodDesc* pMD)
{
    LIMITED_METHOD_CONTRACT;
    return !(pMD == nullptr || !pMD->IsIL() || pMD->IsUnboxingStub() || pMD->GetMethodTable()->Collectible());
}

//
// An opportunity to record the parameters passed to the JIT at the time of JITting this method.
/* static */
void StackSampler::RecordJittingInfo(MethodDesc* pMD, CORJIT_FLAGS flags)
{
    WRAPPER_NO_CONTRACT;
    if (g_pStackSampler == nullptr)
    {
        return;
    }
    // Skip if this is not a good method desc.
    if (!IsGoodMethodDesc(pMD))
    {
        return;
    }
    // Record in the hash map.
    g_pStackSampler->RecordJittingInfoInternal(pMD, flags);
}

void StackSampler::RecordJittingInfoInternal(MethodDesc* pMD, CORJIT_FLAGS flags)
{
    JitInfoHashEntry entry(pMD, flags);

    // Record the domain in the hash map.
    {
        CrstHolder ch(&m_crstJitInfo);
        m_jitInfo.AddOrReplace(entry);
    }
}

// Stack walk callback data.
struct WalkInfo
{
    StackSampler* pThis;

    // The thread in which the walk is happening and the method is executing.
    // Used to obtain the app domain.
    Thread* pMdThread;
};

// Visitor for stack walk callback.
StackWalkAction StackSampler::StackWalkCallback(CrawlFrame* pCf, VOID* data)
{
    WRAPPER_NO_CONTRACT;

    WalkInfo* info = (WalkInfo*) data;
    return ((StackSampler*) info->pThis)->CrawlFrameVisitor(pCf, info->pMdThread);
}

// Stack walk visitor helper to maintain the hash map of method desc, their count
// and the thread's domain in which the method is executing.
StackWalkAction StackSampler::CrawlFrameVisitor(CrawlFrame* pCf, Thread* pMdThread)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    MethodDesc* pMD = pCf->GetFunction();

    // Filter out methods we don't care about
    if (!IsGoodMethodDesc(pMD))
    {
        return SWA_CONTINUE;
    }

    // Lookup the method desc and obtain info.
    CountInfo info;
    m_countInfo.Lookup(pMD, &info);

    // Record the current domain ID of the method's thread, i.e.,
    // the method is last known to be executing.
    info.uCount++;

    // Put the info back.
    m_countInfo.AddOrReplace(CountInfoHashEntry(pMD, info));

    // We got the top good one, skip.
    return SWA_ABORT;
}

// Thread routine that suspends the runtime, walks the other threads' stacks to get the
// top managed method. Restarts the runtime after samples are collected. Identifies top
// methods from the samples and re-JITs them in the background.
void StackSampler::ThreadProc()
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Complete the thread init.
    if (!m_pThread->HasStarted())
    {
        return;
    }

    // User asked us to sample after certain time.
    m_pThread->UserSleep(m_nSampleAfter);

    WalkInfo info = { this, nullptr };

    while (true)
    {
        EX_TRY
        {
            // Suspend the runtime.
            ThreadSuspend::SuspendEE(ThreadSuspend::SUSPEND_OTHER);

            // Walk all other threads.
            Thread* pThread = nullptr;
            while ((pThread = ThreadStore::GetThreadList(pThread)) != nullptr)
            {
                if (pThread == m_pThread)
                {
                    continue;
                }
                // TODO: Detect if thread is suspended by user before we suspended and skip.

                info.pMdThread = pThread;

                // Walk the frames.
                pThread->StackWalkFrames(StackWalkCallback, &info, FUNCTIONSONLY | ALLOW_ASYNC_STACK_WALK);
            }

            // Restart the runtime.
            ThreadSuspend::RestartEE(FALSE, TRUE);

            // JIT the methods that frequently occur in samples.
            JitFrequentMethodsInSamples();
        }
        EX_CATCH
        {
        }
        EX_END_CATCH(SwallowAllExceptions);

        // User asked us to sample every few seconds.
        // TODO: Measure time to JIT using CycleTimer and subtract from the time we sleep every time.
        m_pThread->UserSleep(m_nSampleEvery);
    }
}

// Find the most frequent method in the samples and JIT them.
void StackSampler::JitFrequentMethodsInSamples()
{
    struct Count
    {
        MethodDesc* pMD;
        CountInfo info;

        static int __cdecl Decreasing(const void* e1, const void* e2)
        {
            return ((Count*) e2)->info.uCount - ((Count*) e1)->info.uCount;
        }
    };

    // We want to keep a max-heap of the top frequent methods in the samples.
    NewHolder<Count> freq(new (nothrow) Count[m_nNumMethods]);

    //
    // For each element in the samples, call it incoming, add to the "frequent" list
    // if the list has space to hold the incoming element.
    //
    // If the list doesn't have space, replace the min frequent element in the list
    // with the incoming element, if the latter is more frequent.
    //
    unsigned uLength = 0;
    for (CountInfoHash::Iterator iter = m_countInfo.Begin(), end = m_countInfo.End(); iter != end; iter++)
    {
        Count c = { (*iter).Key(), (*iter).Value() };

        // Is the list full? Drop the min element if incoming is more frequent.
        if (uLength == m_nNumMethods)
        {
            // Find the min element and the min index.
            unsigned uMinIndex = 0;
            unsigned uMin = freq[0].info.uCount;
            for (unsigned i = 1; i < uLength; ++i)
            {
                if (uMin > freq[i].info.uCount)
                {
                    uMin = freq[i].info.uCount;
                    uMinIndex = i;
                }
            }
            if (uMin < c.info.uCount)
            {
                freq[uMinIndex] = c;
            }
        }
        // List is not full, just add the incoming element.
        else
        {
            freq[uLength] = c;
            uLength++;
        }
    }

    // Sort by most frequent element first.
    qsort(freq, uLength, sizeof(Count), Count::Decreasing);

#ifdef _DEBUG
    LOG((LF_JIT, LL_INFO100000, "-----------HOT METHODS-------\n"));
    for (unsigned i = 0; i < uLength; ++i)
    {
        // printf("%s:%s, %u\n", freq[i].pMD->GetMethodTable()->GetClass()->GetDebugClassName(), freq[i].pMD->GetName(), freq[i].info.uCount);
        LOG((LF_JIT, LL_INFO100000, "%s:%s, %u\n", freq[i].pMD->GetMethodTable()->GetClass()->GetDebugClassName(), freq[i].pMD->GetName(), freq[i].info.uCount));
    }
    LOG((LF_JIT, LL_INFO100000, "-----------------------------\n"));
#endif

    // Do the JITting.
    for (unsigned i = 0; i < uLength; ++i)
    {
        // If not already JITted and the method is frequent enough to be important.
        if (!freq[i].info.fJitted && freq[i].info.uCount > s_knDefaultCountForImportance)
        {
            // Try to get the original app domain ID in which the method was JITTed, if not
            // use the app domain ID the method was last seen executing.
            JitAndCollectTrace(freq[i].pMD);
        }
    }
}

// Invoke the JIT for the method desc. Switch to the appropriate domain.
void StackSampler::JitAndCollectTrace(MethodDesc* pMD)
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;

    // Indicate to the JIT or the JIT interface that we are JITting
    // in the background for stack sampling.
    CORJIT_FLAGS flags(CORJIT_FLAGS::CORJIT_FLAG_SAMPLING_JIT_BACKGROUND);

    _ASSERTE(pMD->IsIL());

    EX_TRY
    {
        {
            GCX_PREEMP();

            COR_ILMETHOD_DECODER::DecoderStatus status;
            NewHolder<COR_ILMETHOD_DECODER> pDecoder(
                    new COR_ILMETHOD_DECODER(pMD->GetILHeader(),
                                            pMD->GetMDImport(),
                                            &status));

#ifdef _DEBUG
            LOG((LF_JIT, LL_INFO100000, "Jitting the hot method desc using SuperPMI in the background thread -> "));
            LOG((LF_JIT, LL_INFO100000, "%s:%s\n", pMD->GetMethodTable()->GetClass()->GetDebugClassName(), pMD->GetName()));
#endif
            NativeCodeVersion natCodeVer(pMD);
            PrepareCodeConfigBuffer cfgBuffer(natCodeVer);
            PCODE pCode = UnsafeJitFunction(cfgBuffer.GetConfig(), pDecoder, flags);
        }

        // Update that this method has been already JITted.
        CountInfo info;
        m_countInfo.Lookup(pMD, &info);
        info.fJitted = true;
        m_countInfo.AddOrReplace(CountInfoHashEntry(pMD, info));
    }
    EX_CATCH
    {
    }
    EX_END_CATCH(SwallowAllExceptions)

}

#endif // FEATURE_STACK_SAMPLING
