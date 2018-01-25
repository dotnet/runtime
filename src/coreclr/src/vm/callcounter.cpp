// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: CallCounter.CPP
//
// ===========================================================================



#include "common.h"
#include "excep.h"
#include "log.h"
#include "tieredcompilation.h"
#include "callcounter.h"

#ifdef FEATURE_TIERED_COMPILATION

CallCounter::CallCounter()
{
    LIMITED_METHOD_CONTRACT;

    m_lock.Init(LOCK_TYPE_DEFAULT);
}

// This is called by the prestub each time the method is invoked in a particular
// AppDomain (the AppDomain for which AppDomain.GetCallCounter() == this). These
// calls continue until we backpatch the prestub to avoid future calls. This allows
// us to track the number of calls to each method and use it as a trigger for tiered
// compilation.
//
// Returns TRUE if no future invocations are needed (we reached the count we cared about)
// and FALSE otherwise. It is permissible to keep calling even when TRUE was previously
// returned and multi-threaded race conditions will surely cause this to occur.
void CallCounter::OnMethodCalled(
    MethodDesc* pMethodDesc,
    TieredCompilationManager *pTieredCompilationManager,
    BOOL* shouldStopCountingCallsRef,
    BOOL* wasPromotedToTier1Ref)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(pMethodDesc->IsEligibleForTieredCompilation());
    _ASSERTE(pTieredCompilationManager != nullptr);
    _ASSERTE(shouldStopCountingCallsRef != nullptr);
    _ASSERTE(wasPromotedToTier1Ref != nullptr);

    // PERF: This as a simple to implement, but not so performant, call counter
    // Currently this is only called until we reach a fixed call count and then
    // disabled. Its likely we'll want to improve this at some point but
    // its not as bad as you might expect. Allocating a counter inline in the
    // MethodDesc or at some location computable from the MethodDesc should
    // eliminate 1 pointer per-method (the MethodDesc* key) and the CPU
    // overhead to acquire the lock/search the dictionary. Depending on where it
    // is we may also be able to reduce it to 1 byte counter without wasting the
    // following bytes for alignment. Further work to inline the OnMethodCalled
    // callback directly into the jitted code would eliminate CPU overhead of 
    // leaving the prestub unpatched, but may not be good overall as it increases
    // the size of the jitted code.


    TieredCompilationManager* pCallCounterSink = NULL;
    int callCount;
    {
        //Be careful if you convert to something fully lock/interlocked-free that
        //you correctly handle what happens when some N simultaneous calls don't
        //all increment the counter. The slight drift is probably neglible for tuning
        //but TieredCompilationManager::OnMethodCalled() doesn't expect multiple calls
        //each claiming to be exactly the threshhold call count needed to trigger
        //optimization.
        SpinLockHolder holder(&m_lock);
        CallCounterEntry* pEntry = const_cast<CallCounterEntry*>(m_methodToCallCount.LookupPtr(pMethodDesc));
        if (pEntry == NULL)
        {
            callCount = 1;
            m_methodToCallCount.Add(CallCounterEntry(pMethodDesc, callCount));
        }
        else
        {
            pEntry->callCount++;
            callCount = pEntry->callCount;
        }
    }

    pTieredCompilationManager->OnMethodCalled(pMethodDesc, callCount, shouldStopCountingCallsRef, wasPromotedToTier1Ref);
}

#endif // FEATURE_TIERED_COMPILATION
