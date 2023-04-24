// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#ifndef __SPINLOCK_H__
#define __SPINLOCK_H__

// #SwitchToThreadSpinning
//
// If you call __SwitchToThread in a loop waiting for a condition to be met,
// it is critical that you insert periodic sleeps.  This is because the thread
// you are waiting for to set that condition may need your CPU, and simply
// calling __SwitchToThread(0) will NOT guarantee that it gets a chance to run.
// If there are other runnable threads of higher priority, or even if there
// aren't and it is in another processor's queue, you will be spinning a very
// long time.
//
// To force all callers to consider this issue and to avoid each having to
// duplicate the same backoff code, __SwitchToThread takes a required second
// parameter.  If you want it to handle backoff for you, this parameter should
// be the number of successive calls you have made to __SwitchToThread (a loop
// count).  If you want to take care of backing off yourself, you can pass
// CALLER_LIMITS_SPINNING.  There are three valid cases for doing this:
//
//     - You count iterations and induce a sleep periodically
//     - The number of consecutive __SwitchToThreads is limited
//     - Your call to __SwitchToThread includes a non-zero sleep duration
//
// Lastly, to simplify this requirement for the following common coding pattern:
//
//     while (!condition)
//         SwitchToThread
//
// you can use the YIELD_WHILE macro.

#define YIELD_WHILE(condition)                                          \
    {                                                                   \
        uint32_t __dwSwitchCount = 0;                                     \
        while (condition)                                               \
        {                                                               \
            __SwitchToThread(0, ++__dwSwitchCount);                     \
        }                                                               \
    }

class SpinLock
{
private:
    enum LOCK_STATE
    {
        UNLOCKED = 0,
        LOCKED = 1
    };

    volatile int32_t m_lock;

    static void Lock(SpinLock& lock)
        { YIELD_WHILE (PalInterlockedExchange(&lock.m_lock, LOCKED) == LOCKED); }

    static void Unlock(SpinLock& lock)
        { PalInterlockedExchange(&lock.m_lock, UNLOCKED); }

public:
    SpinLock()
        : m_lock(UNLOCKED) { }

    typedef HolderNoDefaultValue<SpinLock&,
                                 SpinLock::Lock,
                                 SpinLock::Unlock>
        Holder;
};

#endif

