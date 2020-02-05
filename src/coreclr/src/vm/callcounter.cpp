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
#ifndef DACCESS_COMPILE

CallCounterEntry CallCounterEntry::CreateWithCallCountingDisabled(MethodDesc *m)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(m != nullptr);

    CallCounterEntry entry(m, INT_MAX);
    _ASSERTE(!entry.IsCallCountingEnabled());
    return entry;
}

CallCounter::CallCounter()
{
    LIMITED_METHOD_CONTRACT;

    m_lock.Init(LOCK_TYPE_DEFAULT);
}

#endif // !DACCESS_COMPILE

bool CallCounter::IsCallCountingEnabled(PTR_MethodDesc pMethodDesc)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(pMethodDesc != PTR_NULL);
    _ASSERTE(pMethodDesc->IsEligibleForTieredCompilation());

#ifndef DACCESS_COMPILE
    SpinLockHolder holder(&m_lock);
#endif

    PTR_CallCounterEntry entry =
        (PTR_CallCounterEntry)const_cast<CallCounterEntry *>(m_methodToCallCount.LookupPtr(pMethodDesc));
    return entry == PTR_NULL || entry->IsCallCountingEnabled();
}

#ifndef DACCESS_COMPILE

void CallCounter::DisableCallCounting(MethodDesc* pMethodDesc)
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(pMethodDesc != NULL);
    _ASSERTE(pMethodDesc->IsEligibleForTieredCompilation());

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
        existingEntry->DisableCallCounting();
        return;
    }

    // Typically, the entry would already exist because OnMethodCalled() would have been called before this function on the same
    // thread. With multi-core JIT, a function may be jitted before it is called, in which case the entry would not exist.
    m_methodToCallCount.Add(CallCounterEntry::CreateWithCallCountingDisabled(pMethodDesc));
}

NOINLINE bool CallCounter::OnMethodCodeVersionCalledSubsequently(NativeCodeVersion nativeCodeVersion, bool *doPublishRef)
{
    STANDARD_VM_CONTRACT;
    _ASSERTE(!nativeCodeVersion.IsNull());
    _ASSERTE(nativeCodeVersion.GetNativeCode() != NULL);
    _ASSERTE(doPublishRef != nullptr);
    _ASSERTE(*doPublishRef);

    MethodDesc *methodDesc = nativeCodeVersion.GetMethodDesc();
    if (!methodDesc->IsEligibleForTieredCompilation() ||
        nativeCodeVersion.GetOptimizationTier() != NativeCodeVersion::OptimizationTier0)
    {
        return false;
    }

    TieredCompilationManager *tieredCompilationManager = GetAppDomain()->GetTieredCompilationManager();
    if (tieredCompilationManager->OnMethodCodeVersionCalledSubsequently(methodDesc))
    {
        return true;
    }

    if (methodDesc->GetCallCounter()->IncrementCount(methodDesc))
    {
        *doPublishRef = false;
    }
    return true;
}

// This is called by the prestub each time the method is invoked in a particular
// AppDomain (the AppDomain for which AppDomain.GetCallCounter() == this). These
// calls continue until we backpatch the prestub to avoid future calls. This allows
// us to track the number of calls to each method and use it as a trigger for tiered
// compilation.
bool CallCounter::IncrementCount(MethodDesc* pMethodDesc)
{
    STANDARD_VM_CONTRACT;

    _ASSERTE(pMethodDesc->IsEligibleForTieredCompilation());

    if (!g_pConfig->TieredCompilation_CallCounting())
    {
        return false; // stop counting calls
    }

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

    int callCountLimit;
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
            callCountLimit = (int)g_pConfig->TieredCompilation_CallCountThreshold() - 1;
            _ASSERTE(callCountLimit >= 0);
            m_methodToCallCount.Add(CallCounterEntry(pMethodDesc, callCountLimit));
        }
        else if (pEntry->IsCallCountingEnabled())
        {
            callCountLimit = --pEntry->callCountLimit;
        }
        else
        {
            return false; // stop counting calls
        }
    }

    if (callCountLimit > 0)
    {
        return true; // continue counting calls
    }
    if (callCountLimit == 0)
    {
        GetAppDomain()->GetTieredCompilationManager()->AsyncPromoteMethodToTier1(pMethodDesc);
    }
    return false; // stop counting calls
}

#endif // !DACCESS_COMPILE
#endif // FEATURE_TIERED_COMPILATION
