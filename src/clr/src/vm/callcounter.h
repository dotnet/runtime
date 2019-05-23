// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: CallCounter.h
//
// ===========================================================================


#ifndef CALL_COUNTER_H
#define CALL_COUNTER_H

#ifdef FEATURE_TIERED_COMPILATION

// One entry in our dictionary mapping methods to the number of times they
// have been invoked
struct CallCounterEntry
{
    CallCounterEntry() {}
    CallCounterEntry(PTR_MethodDesc m, const int callCountLimit)
        : pMethod(m), callCountLimit(callCountLimit) {}

    PTR_MethodDesc pMethod;
    int callCountLimit;

#ifndef DACCESS_COMPILE
    static CallCounterEntry CreateWithCallCountingDisabled(MethodDesc *m);
#endif

    bool IsCallCountingEnabled() const
    {
        LIMITED_METHOD_CONTRACT;
        return callCountLimit != INT_MAX;
    }

#ifndef DACCESS_COMPILE
    void DisableCallCounting()
    {
        LIMITED_METHOD_CONTRACT;
        callCountLimit = INT_MAX;
    }
#endif
};

typedef DPTR(struct CallCounterEntry) PTR_CallCounterEntry;

class CallCounterHashTraits : public DefaultSHashTraits<CallCounterEntry>
{
public:
    typedef typename DefaultSHashTraits<CallCounterEntry>::element_t element_t;
    typedef typename DefaultSHashTraits<CallCounterEntry>::count_t count_t;

    typedef PTR_MethodDesc key_t;

    static key_t GetKey(element_t e)
    {
        LIMITED_METHOD_CONTRACT;
        return e.pMethod;
    }
    static BOOL Equals(key_t k1, key_t k2)
    {
        LIMITED_METHOD_CONTRACT;
        return k1 == k2;
    }
    static count_t Hash(key_t k)
    {
        LIMITED_METHOD_CONTRACT;
        return (count_t)dac_cast<TADDR>(k);
    }

    static const element_t Null() { LIMITED_METHOD_CONTRACT; return element_t(PTR_NULL, 0); }
    static const element_t Deleted() { LIMITED_METHOD_CONTRACT; return element_t((PTR_MethodDesc)-1, 0); }
    static bool IsNull(const element_t &e) { LIMITED_METHOD_CONTRACT; return e.pMethod == PTR_NULL; }
    static bool IsDeleted(const element_t &e) { return e.pMethod == (PTR_MethodDesc)-1; }
};

typedef SHash<NoRemoveSHashTraits<CallCounterHashTraits>> CallCounterHash;


// This is a per-appdomain cache of call counts for all code in that AppDomain.
// Each method invocation should trigger a call to OnMethodCalled (until it is disabled per-method)
// and the CallCounter will forward the call to the TieredCompilationManager including the
// current call count.
class CallCounter
{
public:
#ifdef DACCESS_COMPILE
    CallCounter() {}
#else
    CallCounter();
#endif

    static bool IsEligibleForCallCounting(PTR_MethodDesc pMethodDesc);
    bool IsCallCountingEnabled(PTR_MethodDesc pMethodDesc);
#ifndef DACCESS_COMPILE
    void DisableCallCounting(MethodDesc* pMethodDesc);
    bool WasCalledAtMostOnce(MethodDesc* pMethodDesc);
#endif

    void OnMethodCalled(MethodDesc* pMethodDesc, TieredCompilationManager *pTieredCompilationManager, BOOL* shouldStopCountingCallsRef, BOOL* wasPromotedToNextTierRef);

private:

    // fields protected by lock
    SpinLock m_lock;
    CallCounterHash m_methodToCallCount;
};

#endif // FEATURE_TIERED_COMPILATION

#endif // CALL_COUNTER_H
