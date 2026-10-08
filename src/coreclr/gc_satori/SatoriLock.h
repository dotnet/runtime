// Copyright (c) 2025 Vladimir Sadov
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
//
// SatoriLock.h
//

#ifndef __SATORI_LOCK_H__
#define __SATORI_LOCK_H__

#include "common.h"
#include "../gc.h"
#include "SatoriUtil.h"
#include "SatoriGate.h"

#if defined(TARGET_OSX)
#include <time.h>
#endif

class SatoriLock
{
private:
    // m_state layout:
    //
    // bit 0: True if the lock is held, false otherwise.
    //
    // bit 1: True if nonwaiters must not get ahead of waiters when acquiring a contended lock.
    //
    // sign bit: True if we've set the event to wake a waiting thread.  The waiter resets this to false when it
    //        wakes up.  This avoids the overhead of setting the event multiple times.
    //
    // everything else: A count of the number of threads waiting on the event.
    static const uint32_t Unlocked = 0;
    static const uint32_t Locked = 1;
    static const uint32_t YieldToWaiters = 2;
    static const uint32_t WaiterCountIncrement = 4;
    static const uint32_t WaiterWoken = 1u << 31;

    volatile uint32_t _state;
    volatile uint16_t _spinCount;
    volatile uint16_t _wakeWatchDog;
    volatile size_t _owningThreadId;

    SatoriGate* _gate;

private:
    FORCEINLINE
    static bool CompareExchangeAcq(uint32_t volatile* destination, uint32_t exchange, uint32_t comparand)
    {
#ifdef _MSC_VER
#if defined(TARGET_AMD64)
        return _InterlockedCompareExchange((long*)destination, exchange, comparand) == (long)comparand;
#else
        return _InterlockedCompareExchange_acq((long*)destination, exchange, comparand) == (long)comparand;
#endif
#else
        return __atomic_compare_exchange_n(destination, &comparand, exchange, false, __ATOMIC_ACQUIRE, __ATOMIC_RELAXED);
#endif
    }

    FORCEINLINE
    static uint32_t InterlockedDecRel(volatile uint32_t* arg)
    {
#ifdef _MSC_VER
#if defined(TARGET_AMD64)
        return (uint32_t)_InterlockedDecrement((long*)arg);
#else
        return (uint32_t)_InterlockedDecrement_rel((long*)arg);
#endif
#else
        return __atomic_sub_fetch(arg, 1, __ATOMIC_RELEASE);
#endif
    }

    static const uint16_t SpinCountNotInitialized = INT16_MIN;

    // While spinning is parameterized in terms of iterations,
    // the internal tuning operates with spin count at a finer scale.
    // One iteration is mapped to 64 spin count units.
    static const int SpinCountScaleShift = 6;

    static const uint16_t DefaultMaxSpinCount = 22 << SpinCountScaleShift;
    static const uint16_t DefaultMinSpinCount = 1 << SpinCountScaleShift;

    // We will use exponential backoff in rare cases when we need to change state atomically and cannot
    // make progress due to concurrent state changes by other threads.
    // While we cannot know the ideal amount of wait needed before making a successful attempt,
    // the exponential backoff will generally be not more than 2X worse than the perfect guess and
    // will do a lot less attempts than an simple retry. On multiprocessor machine fruitless attempts
    // will cause unnecessary sharing of the contended state which may make modifying the state more expensive.
    // To protect against degenerate cases we will cap the per-iteration wait to a few thousand spinwaits.
    static const uint32_t MaxExponentialBackoffBits = 12;

    // This lock is unfair and permits acquiring a contended lock by a nonwaiter in the presence of waiters.
    // It is possible for one thread to keep holding the lock long enough that waiters go to sleep and
    // then release and reacquire fast enough that waiters have no chance to get the lock.
    // In extreme cases one thread could keep retaking the lock starving everybody else.
    // If we see woken waiters not able to take the lock for too long we will ask nonwaiters to wait.
    static const uint32_t WaiterWatchdogTicks = 60;

public:
    void Initialize()
    {
        _state = 0;
        _spinCount = DefaultMinSpinCount;
        _wakeWatchDog = 0;
        _owningThreadId = 0;
        _gate = new (nothrow) SatoriGate();
    }

    FORCEINLINE
    bool TryEnterOneShot()
    {
        uint32_t origState = _state;
        if ((origState & (YieldToWaiters | Locked)) == 0)
        {
            uint32_t newState = origState + Locked;
            if (CompareExchangeAcq(&_state, newState, origState))
            {
                _ASSERTE(_owningThreadId == 0);
                _owningThreadId = SatoriUtil::GetCurrentThreadTag();
                return true;
            }
        }

        return false;
    }

    FORCEINLINE
    bool TryEnter()
    {
        return TryEnterOneShot() ||
            EnterSlow(/*noBlock*/true);
    }

    FORCEINLINE
    void Enter()
    {
        if (!TryEnterOneShot())
        {
            bool entered = EnterSlow();
            _ASSERTE(entered);
        }
    }

    bool IsLocked()
    {
        return (_state & Locked) != 0;
    }

    FORCEINLINE
    void Leave()
    {
        _ASSERTE(IsLocked());
        _ASSERTE(_owningThreadId == SatoriUtil::GetCurrentThreadTag());

        _owningThreadId = 0;
        uint32_t state = InterlockedDecRel(&_state);
        if ((int32_t)state < (int32_t)WaiterCountIncrement) // true if have no waiters or WaiterWoken is set
        {
            return;
        }

        //
        // We have waiters; take the slow path.
        //
        AwakeWaiterIfNeeded();
    }

    static void CollisionBackoff(uint32_t collisions)
    {
        _ASSERTE(collisions > 0);
        if (collisions > MaxExponentialBackoffBits)
        {
            collisions = MaxExponentialBackoffBits;
        }

        // we will backoff for some random number of iterations that roughly grows as collisions^2
        // no need for much randomness here, randomness is "good to have", we could do without it,
        // so we will just hash in the stack location.
        uint32_t rand = (uint32_t)(size_t)&collisions * 2654435769u;
        // set the highmost bit to ensure minimum number of spins is exponentialy increasing
        // it basically guarantees that we spin at least 1, 2, 4, 8, 16, times, and so on
        rand |= (1u << 31);
        uint32_t spins = rand >> (uint8_t)(32 - collisions);

        for (int i = 0; i < (int)spins; i++)
        {
            YieldProcessor();
        }
    }

private:

    static uint16_t GetTickCount()
    {
        return (uint16_t)minipal_lowres_ticks();
    }

    // same idea as in CollisionBackoff, but with expected small range
    static void IterationBackoff(int iteration)
    {
        _ASSERTE(iteration > 0 && iteration < MaxExponentialBackoffBits);

        uint32_t rand = (uint32_t)(size_t)&iteration * 2654435769u;
        // set the highmost bit to ensure minimum number of spins is exponentialy increasing
        // it basically guarantees that we spin at least 1, 2, 4, 8, 16, times, and so on
        rand |= (1u << 31);
        uint32_t spins = rand >> (uint8_t)(32 - iteration);
        for (int i = 0; i < (int)spins; i++)
        {
            YieldProcessor();
        }
    }

    NOINLINE
    bool EnterSlow(bool noBlock = false);

    NOINLINE
    void AwakeWaiterIfNeeded();
};

class SatoriLockHolder : public Satori::StackOnly {
private:
    SatoriLock* const m_lock;

public:
    // Disallow copying
    SatoriLockHolder& operator=(const SatoriLockHolder&) = delete;
    SatoriLockHolder(const SatoriLockHolder&) = delete;

    SatoriLockHolder(SatoriLock* lock)
        : m_lock(lock)
    {
        m_lock->Enter();
    }

    SatoriLockHolder(SatoriLock* lock, bool isLocked)
        : m_lock(lock)
    {
        if (!isLocked)
            m_lock->Enter();
    }

    ~SatoriLockHolder()
    {
        m_lock->Leave();
    }
};

#endif
