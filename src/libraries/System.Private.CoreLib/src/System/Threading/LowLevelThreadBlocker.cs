// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if TARGET_LINUX || TARGET_WINDOWS
// use OS-provided compare-and-wait API.
#else
// fallback to monitor (condvar+mutex)
#define USE_MONITOR
#endif

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Threading
{
    /// <summary>
    /// Portable lightweight API to block and wake threads.
    /// The blocker is low-level. It is not alertable by APCs and not interruptible by
    /// Thread.Interrupt.
    /// Functionally, the blocker is similar to a counting semaphore with MaxInt max count.
    /// We count Wakes and Wait consumes them or blocks until wake count is != 0.
    /// Waking policy is as fair as provided by the underlying APIs. That is typically
    /// FIFO or close to FIFO, but rarely documented explicitly.
    /// When OS provides a compare-and-wait API, such as futex, we use that.
    /// Otherwise we fallback to a heavier, but more portable condvar/mutex implementation.
    /// </summary>
    internal unsafe class LowLevelThreadBlocker : IDisposable
    {
        private int* _pState;

#if USE_MONITOR
        private LowLevelMonitor _monitor;
#endif

        public LowLevelThreadBlocker()
        {
            _pState = (int*)NativeMemory.AlignedAlloc(Internal.PaddingHelpers.CACHE_LINE_SIZE, Internal.PaddingHelpers.CACHE_LINE_SIZE);
            *_pState = 0;

#if USE_MONITOR
            _monitor.Initialize();
#endif
        }

        ~LowLevelThreadBlocker()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_pState == null)
            {
                return;
            }

            NativeMemory.AlignedFree(_pState);
            _pState = null;

#if USE_MONITOR
            _monitor.Dispose();
#endif

            GC.SuppressFinalize(this);
        }

#if USE_MONITOR
        internal void Wait()
        {
            _monitor.Acquire();

            int originalState = *_pState;
            while (originalState == 0)
            {
                _monitor.Wait();
                originalState = *_pState;
            }

            *_pState = originalState - 1;
            _monitor.Release();
        }

        internal bool TimedWait(int timeoutMs)
        {
            Debug.Assert(timeoutMs >= -1);
            if (timeoutMs == -1)
            {
                Wait();
                return true;
            }

            long deadline = Environment.TickCount64 + timeoutMs;
            _monitor.Acquire();

            int originalState = *_pState;
            while (originalState == 0)
            {
                if (!_monitor.Wait(timeoutMs) ||
                    (timeoutMs = (int)(deadline - Environment.TickCount64)) < 0)
                {
                    _monitor.Release();
                    return false;
                }

                originalState = *_pState;
            }

            *_pState = originalState - 1;
            _monitor.Release();
            return true;
        }

        internal void WakeOne()
        {
            _monitor.Acquire();
            *_pState = *_pState + 1;
            _monitor.Signal_Release();
        }
#else

        internal void Wait()
        {
            while (true)
            {
                int originalState = *_pState;
                while (originalState == 0)
                {
                    LowLevelFutex.WaitOnAddress(_pState, originalState);
                    originalState = *_pState;
                }

                if (Interlocked.CompareExchange(ref *_pState, originalState - 1, originalState) == originalState)
                {
                    return;
                }
            }
        }

        internal bool TimedWait(int timeoutMs)
        {
            Debug.Assert(timeoutMs >= -1);
            if (timeoutMs == -1)
            {
                Wait();
                return true;
            }

            long deadline = Environment.TickCount64 + timeoutMs;
            while (true)
            {
                int originalState = *_pState;
                while (originalState == 0)
                {
                    if (!LowLevelFutex.WaitOnAddressTimeout(_pState, originalState, timeoutMs))
                    {
                        return false;
                    }

                    timeoutMs = (int)(deadline - Environment.TickCount64);
                    if (timeoutMs < 0)
                    {
                        return false;
                    }

                    originalState = *_pState;
                }

                if (Interlocked.CompareExchange(ref *_pState, originalState - 1, originalState) == originalState)
                {
                    return true;
                }
            }
        }

        internal void WakeOne()
        {
            Interlocked.Increment(ref *_pState);
            LowLevelFutex.WakeByAddressSingle(_pState);
        }
#endif
    }
}
