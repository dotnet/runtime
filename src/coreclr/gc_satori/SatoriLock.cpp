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
// SatoriLock.cpp
//

#include "common.h"

#include "gcenv.h"
#include "../env/gcenv.os.h"
#include "SatoriLock.h"

NOINLINE
bool SatoriLock::EnterSlow(bool noBlock)
{
    bool hasWaited = false;
    // we will retry after waking up
    while (true)
    {
        int iteration = 1;

        // We will count when we failed to change the state of the lock and increase pauses
        // so that bursts of activity are better tolerated. This should not happen often.
        // after waking up we restart collision and iteration counters.
        int collisions = 0;

        // We will track the changes of ownership while we are trying to acquire the lock.
        size_t oldOwner = _owningThreadId;
        uint32_t ownerChanged = 0;

        int iterationLimit = _spinCount >> SpinCountScaleShift;
        // inner loop where we try acquiring the lock or registering as a waiter
        while (true)
        {
            //
            // Try to grab the lock.  We may take the lock here even if there are existing waiters.  This creates the possibility
            // of starvation of waiters, but it also prevents lock convoys and preempted waiters from destroying perf.
            // However, if we do not see _wakeWatchDog cleared for long enough, we go into YieldToWaiters mode to ensure some
            // waiter progress.
            //
            uint32_t oldState = _state;
            bool canAcquire = ((oldState & Locked) == Unlocked) &&
                (hasWaited || ((oldState & YieldToWaiters) == 0));

            if (canAcquire)
            {
                uint32_t newState = oldState | Locked;
                if (hasWaited)
                    newState = (newState - WaiterCountIncrement) & ~(WaiterWoken | YieldToWaiters);

                if (CompareExchangeAcq(&_state, newState, oldState))
                {
                    // GOT THE LOCK!!
                    _ASSERTE((_state | Locked) != 0);
                    _ASSERTE(_owningThreadId == 0);
                    _owningThreadId = SatoriUtil::GetCurrentThreadTag();

                    if (hasWaited)
                        _wakeWatchDog = 0;

                    // now we can estimate how busy the lock is and adjust spinning accordingly
                    uint16_t spinLimit = _spinCount;
                    if (ownerChanged != 0)
                    {
                        // The lock has changed ownership while we were trying to acquire it.
                        // It is a signal that we might want to spin less next time.
                        // Pursuing a lock that is being "stolen" by other threads is inefficient
                        // due to cache misses and unnecessary sharing of state that keeps invalidating.
                        if (spinLimit > DefaultMinSpinCount)
                        {
                            _spinCount = (uint16_t)(spinLimit - 1);
                        }
                    }
                    else if (spinLimit < DefaultMaxSpinCount &&
                        iteration >= (spinLimit >> SpinCountScaleShift))
                    {
                        // we used all of allowed iterations, but the lock does not look very contested,
                        // we can allow a bit more spinning.
                        //
                        // NB: if we acquired the lock while registering a waiter, and owner did not change it still counts.
                        //     (however iteration does not grow beyond the iterationLimit)
                        _spinCount = (uint16_t)(spinLimit + 1);
                    }

                    return true;
                }
            }

            size_t newOwner = _owningThreadId;
            if (newOwner != 0 && newOwner != oldOwner)
            {
                if (oldOwner != 0)
                    ownerChanged++;

                oldOwner = newOwner;
            }

            if (iteration < iterationLimit)
            {
                // We failed to acquire the lock and want to retry after a pause.
                // Ideally we will retry right when the lock becomes free, but we cannot know when that will happen.
                // We will use a pause that doubles up on every iteration. It will not be more than 2x worse
                // than the ideal guess, while minimizing the number of retries.
                // We will allow pauses up to 64~128 spinwaits.
                IterationBackoff(min(iteration, 6));
                iteration++;
                continue;
            }
            else if (!canAcquire)
            {
                // We reached our spin limit, and need to wait.

                if (noBlock)
                    return false;

                // If waiter was awaken spuriously, it may acquire the lock before wake watchdog is set.
                // If there are no more waiters for a long time, the watchdog could hang around for a while too.
                // When a new waiter enters the system, it may look like we had no waiter progress for all that time.
                // To avoid this, if it looks like we have no waiters and will be the first new one,
                // clear the watchdog.
                // It is ok to clear even if we will not end up the first one.
                // We will self-correct on the next wake and reestablish a new watchdog.
                if (oldState < WaiterCountIncrement && _wakeWatchDog !=0)
                    _wakeWatchDog = 0;
                    
                // Increment the waiter count.
                // Note that we do not do any overflow checking on this increment.  In order to overflow,
                // we'd need to have about 1 billion waiting threads, which is inconceivable anytime in the
                // foreseeable future.
                uint32_t newState = oldState + WaiterCountIncrement;
                if (hasWaited)
                    newState = (newState - WaiterCountIncrement) & ~WaiterWoken;

                if (Interlocked::CompareExchange(&_state, newState, oldState) == oldState)
                    break;
            }
            else
            {
                // We are over the iteration limit, but the lock was open, we tried and failed.
                // It was a collision.
            }

            CollisionBackoff(++collisions);
        }

        //
        // Now we wait.
        //
        _ASSERTE(_state >= WaiterCountIncrement);
        _gate->Wait();
        _ASSERTE(_state >= WaiterCountIncrement);

        // this was either real or spurious wake.
        // either way try acquire again.
        hasWaited = true;
    }
}

NOINLINE
void SatoriLock::AwakeWaiterIfNeeded()
{
    int collisions = 0;
    while (true)
    {
        uint32_t oldState = _state;
        if ((int32_t)oldState >= (int32_t)WaiterCountIncrement) // false if WaiterWoken is set
        {
            // there are waiters, and nobody has woken one.
            uint32_t newState = oldState | WaiterWoken;

            uint16_t lastWakeTicks = _wakeWatchDog;
            if (lastWakeTicks != 0)
            {
                uint16_t currentTicks = GetTickCount();
                if ((int16_t)currentTicks - (int16_t)lastWakeTicks > (int16_t)WaiterWatchdogTicks)
                {
                    //printf("Last: %i ", (int)lastWakeTicks);
                    //printf("Current: %i \n", (int)currentTicks);
                    newState |= YieldToWaiters;
                }
            }

            if (Interlocked::CompareExchange(&_state, newState, oldState) == oldState)
            {
                if (lastWakeTicks == 0)
                {
                    // Sometimes timestamp will be 0.
                    // It is harmless. We will try again on the next wake
                    _wakeWatchDog = GetTickCount();
                }

                _gate->WakeOne();
                return;
            }
        }
        else
        {
            // no need to wake a waiter.
            return;
        }

        CollisionBackoff(++collisions);
    }
}
