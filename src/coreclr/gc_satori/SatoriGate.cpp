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
// SatoriGate.cpp
//

#ifdef TARGET_WINDOWS

#include "common.h"
#include "windows.h"
#include "synchapi.h"
#include "SatoriGate.h"

SatoriGate::SatoriGate()
{
    m_state = s_blocking;
}

// If this gate is in blocking state, the thread will block
// until woken up, possibly spuriously.
void SatoriGate::Wait()
{
    uint32_t blocking = s_blocking;
    BOOL result = WaitOnAddress(&m_state, &blocking, sizeof(uint32_t), INFINITE);
    _ASSERTE(result == TRUE);
    m_state = s_blocking;
}

// If this gate is in blocking state, the thread will block
// until woken up, possibly spuriously.
// or until the wait times out. (in a case of timeout returns false)
bool SatoriGate::TimedWait(int timeout)
{
    uint32_t blocking = s_blocking;
    BOOL result = WaitOnAddress(&m_state, &blocking, sizeof(uint32_t), timeout);
    _ASSERTE(result == TRUE || GetLastError() == ERROR_TIMEOUT);

    bool woken = result == TRUE;
    if (woken)
    {
        // consume the wake
        m_state = s_blocking;
    }

    return woken;
}

// After this call at least one thread will go through the gate, either by waking up,
// or by going through Wait without blocking.
// If there are several racing wakes, one or more may take effect,
// but all wakes will see at least one thread going through the gate.
void SatoriGate::WakeOne()
{
    m_state = s_open;
    WakeByAddressSingle((PVOID)&m_state);
}

// Same as WakeOne, but if there are multiple waiters sleeping,
// all will be woken up and go through the gate.
void SatoriGate::WakeAll()
{
    m_state = s_open;
    WakeByAddressAll((PVOID)&m_state);
}

#else // TARGET_WINDOWS

#include "../gc/unix/globals.h"
#define _INC_PTHREADS
#include <pthread.h>
#include <errno.h>
#include <assert.h>
#include "SatoriGate.h"
#include <new>

#if defined(TARGET_LINUX)

#include <linux/futex.h>      /* Definition of FUTEX_* constants */
#include <sys/syscall.h>      /* Definition of SYS_* constants */
#include <unistd.h>

#ifndef  INT_MAX
#define INT_MAX 2147483647
#endif

SatoriGate::SatoriGate()
{
    m_state = s_blocking;
}

// returns true if was woken up. false if timed out
bool SatoriGate::TimedWait(int timeout)
{
    timespec t;
    uint64_t nanoseconds = (uint64_t)timeout * tccMilliSecondsToNanoSeconds;
    t.tv_sec = nanoseconds / tccSecondsToNanoSeconds;
    t.tv_nsec = nanoseconds % tccSecondsToNanoSeconds;

    long waitResult = syscall(SYS_futex, &m_state, FUTEX_WAIT_PRIVATE, s_blocking, &t, NULL, 0);

    // woken, not blocking, interrupted, timeout
    assert(waitResult == 0 || errno == EAGAIN || errno == ETIMEDOUT || errno == EINTR);

    bool woken = waitResult == 0 || errno != ETIMEDOUT;
    if (woken)
    {
        // consume the wake
        m_state = s_blocking;
    }

    return woken;
}

void SatoriGate::Wait()
{
    syscall(SYS_futex, &m_state, FUTEX_WAIT_PRIVATE, s_blocking, NULL, NULL, 0);
    m_state = s_blocking;
}

void SatoriGate::WakeAll()
{
    m_state = s_open;
    syscall(SYS_futex, &m_state, FUTEX_WAKE_PRIVATE, INT_MAX);
}

void SatoriGate::WakeOne()
{
    m_state = s_open;
    syscall(SYS_futex, &m_state, FUTEX_WAKE_PRIVATE, 1);
}
#else

// TODO: This is a hack, we need to use real compile time detection
#define HAVE_CLOCK_GETTIME_NSEC_NP 1
// Convert nanoseconds to the timespec structure
// Parameters:
//  nanoseconds - time in nanoseconds to convert
//  t           - the target timespec structure
void NanosecondsToTimeSpec(uint64_t nanoseconds, timespec* t)
{
    t->tv_sec = nanoseconds / tccSecondsToNanoSeconds;
    t->tv_nsec = nanoseconds % tccSecondsToNanoSeconds;
}

SatoriGate::SatoriGate()
{
    m_cs = new (std::nothrow) pthread_mutex_t();
    m_cv = new (std::nothrow) pthread_cond_t();

    pthread_mutex_init(m_cs, NULL);
    pthread_condattr_t attrs;
    pthread_condattr_init(&attrs);
#if HAVE_PTHREAD_CONDATTR_SETCLOCK && !HAVE_CLOCK_GETTIME_NSEC_NP
    // Ensure that the pthread_cond_timedwait will use CLOCK_MONOTONIC
    pthread_condattr_setclock(&attrs, CLOCK_MONOTONIC);
#endif // HAVE_PTHREAD_CONDATTR_SETCLOCK && !HAVE_CLOCK_GETTIME_NSEC_NP
    pthread_cond_init(m_cv, &attrs);
    pthread_condattr_destroy(&attrs);
}

// returns true if was woken up
bool SatoriGate::TimedWait(int timeout)
{
    timespec endTime;
#if HAVE_CLOCK_GETTIME_NSEC_NP
    uint64_t endNanoseconds;
    uint64_t nanoseconds = (uint64_t)timeout * tccMilliSecondsToNanoSeconds;
    NanosecondsToTimeSpec(nanoseconds, &endTime);
    endNanoseconds = clock_gettime_nsec_np(CLOCK_UPTIME_RAW) + nanoseconds;
#elif HAVE_PTHREAD_CONDATTR_SETCLOCK
    clock_gettime(CLOCK_MONOTONIC, &endTime);
    TimeSpecAdd(&endTime, timeout);
#else
#error "Don't know how to perform timed wait on this platform"
#endif

    int waitResult = 0;
    pthread_mutex_lock(m_cs);
#if HAVE_CLOCK_GETTIME_NSEC_NP
    // Since OSX doesn't support CLOCK_MONOTONIC, we use relative variant of the timed wait.
    waitResult = m_state == s_open ?
        0 :
        pthread_cond_timedwait_relative_np(m_cv, m_cs, &endTime);
#else // HAVE_CLOCK_GETTIME_NSEC_NP
    waitResult = m_state == SatoriGate::s_open ?
        0 :
        pthread_cond_timedwait(m_cv, m_cs, &endTime);
#endif // HAVE_CLOCK_GETTIME_NSEC_NP
    pthread_mutex_unlock(m_cs);
    assert(waitResult == 0 || waitResult == ETIMEDOUT);

    bool woken = waitResult == 0;
    if (woken)
    {
        // consume the wake
        m_state = s_blocking;
    }

    return woken;
}

void SatoriGate::Wait()
{
    int waitResult;
    pthread_mutex_lock(m_cs);

    waitResult = m_state == SatoriGate::s_open ?
        0 :
        pthread_cond_wait(m_cv, m_cs);

    pthread_mutex_unlock(m_cs);
    assert(waitResult == 0);

    m_state = s_blocking;
}

void SatoriGate::WakeAll()
{
    m_state = SatoriGate::s_open;
    pthread_mutex_lock(m_cs);
    pthread_cond_broadcast(m_cv);
    pthread_mutex_unlock(m_cs);
}

void SatoriGate::WakeOne()
{
    m_state = SatoriGate::s_open;
    pthread_mutex_lock(m_cs);
    pthread_cond_signal(m_cv);
    pthread_mutex_unlock(m_cs);
}
#endif


#endif // TARGET_WINDOWS
