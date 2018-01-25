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
    CallCounterEntry(const MethodDesc* m, const int c)
        : pMethod(m), callCount(c) {}

    const MethodDesc* pMethod;
    int callCount;
};

class CallCounterHashTraits : public DefaultSHashTraits<CallCounterEntry>
{
public:
    typedef typename DefaultSHashTraits<CallCounterEntry>::element_t element_t;
    typedef typename DefaultSHashTraits<CallCounterEntry>::count_t count_t;

    typedef const MethodDesc* key_t;

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
        return (count_t)(size_t)k;
    }

    static const element_t Null() { LIMITED_METHOD_CONTRACT; return element_t(NULL, 0); }
    static const element_t Deleted() { LIMITED_METHOD_CONTRACT; return element_t((const MethodDesc*)-1, 0); }
    static bool IsNull(const element_t &e) { LIMITED_METHOD_CONTRACT; return e.pMethod == NULL; }
    static bool IsDeleted(const element_t &e) { return e.pMethod == (const MethodDesc*)-1; }
};

typedef SHash<NoRemoveSHashTraits<CallCounterHashTraits>> CallCounterHash;


// This is a per-appdomain cache of call counts for all code in that AppDomain.
// Each method invocation should trigger a call to OnMethodCalled (until it is disabled per-method)
// and the CallCounter will forward the call to the TieredCompilationManager including the
// current call count.
class CallCounter
{
public:
#if defined(DACCESS_COMPILE) || defined(CROSSGEN_COMPILE)
    CallCounter() {}
#else
    CallCounter();
#endif

    void OnMethodCalled(MethodDesc* pMethodDesc, TieredCompilationManager *pTieredCompilationManager, BOOL* shouldStopCountingCallsRef, BOOL* wasPromotedToTier1Ref);

private:

    // fields protected by lock
    SpinLock m_lock;
    CallCounterHash m_methodToCallCount;
};

#endif // FEATURE_TIERED_COMPILATION

#endif // CALL_COUNTER_H
