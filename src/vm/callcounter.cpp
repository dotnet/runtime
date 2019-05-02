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

CallCounterEntry CallCounterEntry::CreateWithTier0CallCountingDisabled(const MethodDesc *m)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(m != nullptr);

    CallCounterEntry entry(m, INT_MAX);
    _ASSERTE(!entry.IsTier0CallCountingEnabled());
    return entry;
}

CallCounter::CallCounter()
{
    LIMITED_METHOD_CONTRACT;

    m_lock.Init(LOCK_TYPE_DEFAULT);
}

bool CallCounter::IsEligibleForTier0CallCounting(MethodDesc* pMethodDesc)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(pMethodDesc != NULL);
    _ASSERTE(pMethodDesc->IsEligibleForTieredCompilation());

    return g_pConfig->TieredCompilation_CallCounting() && !pMethodDesc->RequestedAggressiveOptimization();
}

bool CallCounter::IsTier0CallCountingEnabled(MethodDesc* pMethodDesc)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(pMethodDesc != NULL);
    _ASSERTE(pMethodDesc->IsEligibleForTieredCompilation());
    _ASSERTE(IsEligibleForTier0CallCounting(pMethodDesc));

    SpinLockHolder holder(&m_lock);

    const CallCounterEntry *entry = m_methodToCallCount.LookupPtr(pMethodDesc);
    return entry == nullptr || entry->IsTier0CallCountingEnabled();
}

void CallCounter::DisableTier0CallCounting(MethodDesc* pMethodDesc)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(pMethodDesc != NULL);
    _ASSERTE(pMethodDesc->IsEligibleForTieredCompilation());
    _ASSERTE(IsEligibleForTier0CallCounting(pMethodDesc));

    // Disabling call counting will affect the tier of the MethodDesc's first native code version. Callers must ensure that this
    // change is made deterministically and prior to or while jitting the first native code version such that the tier would not
    // be changed after it is already jitted. At that point, the call count threshold would already be initialized and the entry
    // would exist. To disable call counting at different points in time, it would be ok to do so if the method has not been
    // called yet (if the entry does not yet exist in the hash table), if necessary that could be a different function like
    // TryDisable...() that would fail to disable call counting if the method has already been called.

    SpinLockHolder holder(&m_lock);

    CallCounterEntry *existingEntry = const_cast<CallCounterEntry *>(m_methodToCallCount.LookupPtr(pMethodDesc));
    if (existingEntry != nullptr)
    {
        existingEntry->DisableTier0CallCounting();
        return;
    }

    // Typically, the entry would already exist because OnMethodCalled() would have been called before this function on the same
    // thread. With multi-core JIT, a function may be jitted before it is called, in which case the entry would not exist.
    m_methodToCallCount.Add(CallCounterEntry::CreateWithTier0CallCountingDisabled(pMethodDesc));
}

// This is called by the prestub each time the method is invoked in a particular
// AppDomain (the AppDomain for which AppDomain.GetCallCounter() == this). These
// calls continue until we backpatch the prestub to avoid future calls. This allows
// us to track the number of calls to each method and use it as a trigger for tiered
// compilation.
void CallCounter::OnMethodCalled(
    MethodDesc* pMethodDesc,
    TieredCompilationManager *pTieredCompilationManager,
    BOOL* shouldStopCountingCallsRef,
    BOOL* wasPromotedToNextTierRef)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(pMethodDesc->IsEligibleForTieredCompilation());
    _ASSERTE(pTieredCompilationManager != nullptr);
    _ASSERTE(shouldStopCountingCallsRef != nullptr);
    _ASSERTE(wasPromotedToNextTierRef != nullptr);

    // At the moment, call counting is only done for tier 0 code
    _ASSERTE(IsEligibleForTier0CallCounting(pMethodDesc));

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

    bool isFirstTier0Call = false;
    int tier0CallCountLimit;
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
            isFirstTier0Call = true;
            tier0CallCountLimit = (int)g_pConfig->TieredCompilation_CallCountThreshold() - 1;
            _ASSERTE(tier0CallCountLimit >= 0);
            m_methodToCallCount.Add(CallCounterEntry(pMethodDesc, tier0CallCountLimit));
        }
        else if (pEntry->IsTier0CallCountingEnabled())
        {
            pEntry->tier0CallCountLimit--;
            tier0CallCountLimit = pEntry->tier0CallCountLimit;
        }
        else
        {
            *shouldStopCountingCallsRef = true;
            *wasPromotedToNextTierRef = true;
            return;
        }
    }

    pTieredCompilationManager->OnTier0MethodCalled(
        pMethodDesc,
        isFirstTier0Call,
        tier0CallCountLimit,
        shouldStopCountingCallsRef,
        wasPromotedToNextTierRef);
}

#endif // FEATURE_TIERED_COMPILATION
