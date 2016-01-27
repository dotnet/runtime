// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
//
//
// Central spin logic used across the entire code-base.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Runtime.ConstrainedExecution;
using System.Security.Permissions;
using System.Threading;
using System.Diagnostics.Contracts;
using System.Diagnostics.CodeAnalysis;

namespace System.Threading
{
    // SpinWait is just a little value type that encapsulates some common spinning
    // logic. It ensures we always yield on single-proc machines (instead of using busy
    // waits), and that we work well on HT. It encapsulates a good mixture of spinning
    // and real yielding. It's a value type so that various areas of the engine can use
    // one by allocating it on the stack w/out unnecessary GC allocation overhead, e.g.:
    //
    //     void f() {
    //         SpinWait wait = new SpinWait();
    //         while (!p) { wait.SpinOnce(); }
    //         ...
    //     }
    //
    // Internally it just maintains a counter that is used to decide when to yield, etc.
    // 
    // A common usage is to spin before blocking. In those cases, the NextSpinWillYield
    // property allows a user to decide to fall back to waiting once it returns true:
    // 
    //     void f() {
    //         SpinWait wait = new SpinWait();
    //         while (!p) {
    //             if (wait.NextSpinWillYield) { /* block! */ }
    //             else { wait.SpinOnce(); }
    //         }
    //         ...
    //     }

    /// <summary>
    /// Provides support for spin-based waiting.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="SpinWait"/> encapsulates common spinning logic. On single-processor machines, yields are
    /// always used instead of busy waits, and on computers with Intel™ processors employing Hyper-Threading™
    /// technology, it helps to prevent hardware thread starvation. SpinWait encapsulates a good mixture of
    /// spinning and true yielding.
    /// </para>
    /// <para>
    /// <see cref="SpinWait"/> is a value type, which means that low-level code can utilize SpinWait without
    /// fear of unnecessary allocation overheads. SpinWait is not generally useful for ordinary applications.
    /// In most cases, you should use the synchronization classes provided by the .NET Framework, such as
    /// <see cref="System.Threading.Monitor"/>. For most purposes where spin waiting is required, however,
    /// the <see cref="SpinWait"/> type should be preferred over the <see
    /// cref="System.Threading.Thread.SpinWait"/> method.
    /// </para>
    /// <para>
    /// While SpinWait is designed to be used in concurrent applications, it is not designed to be
    /// used from multiple threads concurrently.  SpinWait's members are not thread-safe.  If multiple
    /// threads must spin, each should use its own instance of SpinWait.
    /// </para>
    /// </remarks>
    [HostProtection(Synchronization = true, ExternalThreading = true)]
    public struct SpinWait
    {

        // These constants determine the frequency of yields versus spinning. The
        // numbers may seem fairly arbitrary, but were derived with at least some
        // thought in the design document.  I fully expect they will need to change
        // over time as we gain more experience with performance.
        internal const int YIELD_THRESHOLD = 10; // When to switch over to a true yield.
        internal const int SLEEP_0_EVERY_HOW_MANY_TIMES = 5; // After how many yields should we Sleep(0)?
        internal const int SLEEP_1_EVERY_HOW_MANY_TIMES = 20; // After how many yields should we Sleep(1)?

        // The number of times we've spun already.
        private int m_count;

        /// <summary>
        /// Gets the number of times <see cref="SpinOnce"/> has been called on this instance.
        /// </summary>
        public int Count
        {
            get { return m_count; }
        }

        /// <summary>
        /// Gets whether the next call to <see cref="SpinOnce"/> will yield the processor, triggering a
        /// forced context switch.
        /// </summary>
        /// <value>Whether the next call to <see cref="SpinOnce"/> will yield the processor, triggering a
        /// forced context switch.</value>
        /// <remarks>
        /// On a single-CPU machine, <see cref="SpinOnce"/> always yields the processor. On machines with
        /// multiple CPUs, <see cref="SpinOnce"/> may yield after an unspecified number of calls.
        /// </remarks>
        public bool NextSpinWillYield
        {
            get { return m_count > YIELD_THRESHOLD || PlatformHelper.IsSingleProcessor; }
        }

        /// <summary>
        /// Performs a single spin.
        /// </summary>
        /// <remarks>
        /// This is typically called in a loop, and may change in behavior based on the number of times a
        /// <see cref="SpinOnce"/> has been called thus far on this instance.
        /// </remarks>
        public void SpinOnce()
        {
            if (NextSpinWillYield)
            {
                //
                // We must yield.
                //
                // We prefer to call Thread.Yield first, triggering a SwitchToThread. This
                // unfortunately doesn't consider all runnable threads on all OS SKUs. In
                // some cases, it may only consult the runnable threads whose ideal processor
                // is the one currently executing code. Thus we occasionally issue a call to
                // Sleep(0), which considers all runnable threads at equal priority. Even this
                // is insufficient since we may be spin waiting for lower priority threads to
                // execute; we therefore must call Sleep(1) once in a while too, which considers
                // all runnable threads, regardless of ideal processor and priority, but may
                // remove the thread from the scheduler's queue for 10+ms, if the system is
                // configured to use the (default) coarse-grained system timer.
                //

#if !FEATURE_CORECLR
                CdsSyncEtwBCLProvider.Log.SpinWait_NextSpinWillYield();
#endif
                int yieldsSoFar = (m_count >= YIELD_THRESHOLD ? m_count - YIELD_THRESHOLD : m_count);

                if ((yieldsSoFar % SLEEP_1_EVERY_HOW_MANY_TIMES) == (SLEEP_1_EVERY_HOW_MANY_TIMES - 1))
                {
                    Thread.Sleep(1);
                }
                else if ((yieldsSoFar % SLEEP_0_EVERY_HOW_MANY_TIMES) == (SLEEP_0_EVERY_HOW_MANY_TIMES - 1))
                {
                    Thread.Sleep(0);
                }
                else
                {
                    Thread.Yield();
                }
            }
            else
            {
                //
                // Otherwise, we will spin.
                //
                // We do this using the CLR's SpinWait API, which is just a busy loop that
                // issues YIELD/PAUSE instructions to ensure multi-threaded CPUs can react
                // intelligently to avoid starving. (These are NOOPs on other CPUs.) We
                // choose a number for the loop iteration count such that each successive
                // call spins for longer, to reduce cache contention.  We cap the total
                // number of spins we are willing to tolerate to reduce delay to the caller,
                // since we expect most callers will eventually block anyway.
                //
                Thread.SpinWait(4 << m_count);
            }

            // Finally, increment our spin counter.
            m_count = (m_count == int.MaxValue ? YIELD_THRESHOLD : m_count + 1);
        }

        /// <summary>
        /// Resets the spin counter.
        /// </summary>
        /// <remarks>
        /// This makes <see cref="SpinOnce"/> and <see cref="NextSpinWillYield"/> behave as though no calls
        /// to <see cref="SpinOnce"/> had been issued on this instance. If a <see cref="SpinWait"/> instance
        /// is reused many times, it may be useful to reset it to avoid yielding too soon.
        /// </remarks>
        public void Reset()
        {
            m_count = 0;
        }

        #region Static Methods
        /// <summary>
        /// Spins until the specified condition is satisfied.
        /// </summary>
        /// <param name="condition">A delegate to be executed over and over until it returns true.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="condition"/> argument is null.</exception>
        public static void SpinUntil(Func<bool> condition)
        {
#if DEBUG
            bool result = 
#endif
            SpinUntil(condition, Timeout.Infinite);
#if DEBUG
            Contract.Assert(result);
#endif
        }

        /// <summary>
        /// Spins until the specified condition is satisfied or until the specified timeout is expired.
        /// </summary>
        /// <param name="condition">A delegate to be executed over and over until it returns true.</param>
        /// <param name="timeout">
        /// A <see cref="TimeSpan"/> that represents the number of milliseconds to wait, 
        /// or a TimeSpan that represents -1 milliseconds to wait indefinitely.</param>
        /// <returns>True if the condition is satisfied within the timeout; otherwise, false</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="condition"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="timeout"/> is a negative number
        /// other than -1 milliseconds, which represents an infinite time-out -or- timeout is greater than
        /// <see cref="System.Int32.MaxValue"/>.</exception>
        public static bool SpinUntil(Func<bool> condition, TimeSpan timeout)
        {
            // Validate the timeout
            Int64 totalMilliseconds = (Int64)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > Int32.MaxValue)
            {
                throw new System.ArgumentOutOfRangeException(
                    "timeout", timeout, Environment.GetResourceString("SpinWait_SpinUntil_TimeoutWrong"));
            }

            // Call wait with the timeout milliseconds
            return SpinUntil(condition, (int)timeout.TotalMilliseconds);
        }

        /// <summary>
        /// Spins until the specified condition is satisfied or until the specified timeout is expired.
        /// </summary>
        /// <param name="condition">A delegate to be executed over and over until it returns true.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or <see
        /// cref="System.Threading.Timeout.Infinite"/> (-1) to wait indefinitely.</param>
        /// <returns>True if the condition is satisfied within the timeout; otherwise, false</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="condition"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="millisecondsTimeout"/> is a
        /// negative number other than -1, which represents an infinite time-out.</exception>
        public static bool SpinUntil(Func<bool> condition, int millisecondsTimeout)
        {
            if (millisecondsTimeout < Timeout.Infinite)
            {
                throw new ArgumentOutOfRangeException(
                   "millisecondsTimeout", millisecondsTimeout, Environment.GetResourceString("SpinWait_SpinUntil_TimeoutWrong"));
            }
            if (condition == null)
            {
                throw new ArgumentNullException("condition", Environment.GetResourceString("SpinWait_SpinUntil_ArgumentNull"));
            }
            uint startTime = 0;
            if (millisecondsTimeout != 0 && millisecondsTimeout != Timeout.Infinite)
            {
                startTime = TimeoutHelper.GetTime();
            }
            SpinWait spinner = new SpinWait();
            while (!condition())
            {
                if (millisecondsTimeout == 0)
                {
                    return false;
                }

                spinner.SpinOnce();

                if (millisecondsTimeout != Timeout.Infinite && spinner.NextSpinWillYield)
                {
                    if (millisecondsTimeout <= (TimeoutHelper.GetTime() - startTime))
                    {
                        return false;
                    }
                }
            }
            return true;

        }
        #endregion

    }


    /// <summary>
    /// A helper class to get the number of processors, it updates the numbers of processors every sampling interval.
    /// </summary>
    internal static class PlatformHelper
    {
        private const int PROCESSOR_COUNT_REFRESH_INTERVAL_MS = 30000; // How often to refresh the count, in milliseconds.
        private static volatile int s_processorCount; // The last count seen.
        private static volatile int s_lastProcessorCountRefreshTicks; // The last time we refreshed.

        /// <summary>
        /// Gets the number of available processors
        /// </summary>
        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "Reviewed for thread safety")]
        internal static int ProcessorCount
        {
            get
            {
                int now = Environment.TickCount;
                int procCount = s_processorCount;
                if (procCount == 0 || (now - s_lastProcessorCountRefreshTicks) >= PROCESSOR_COUNT_REFRESH_INTERVAL_MS)
                {
                    s_processorCount = procCount = Environment.ProcessorCount;
                    s_lastProcessorCountRefreshTicks = now;
                }

                Contract.Assert(procCount > 0 && procCount <= 64,
                    "Processor count not within the expected range (1 - 64).");

                return procCount;
            }
        }

        /// <summary>
        /// Gets whether the current machine has only a single processor.
        /// </summary>
        internal static bool IsSingleProcessor
        {
            get { return ProcessorCount == 1; }
        }
    }

    /// <summary>
    /// A helper class to capture a start time using Environment.TickCout as a time in milliseconds, also updates a given timeout bu subtracting the current time from
    /// the start time
    /// </summary>
    internal static class TimeoutHelper
    {
        /// <summary>
        /// Returns the Environment.TickCount as a start time in milliseconds as a uint, TickCount tools over from postive to negative every ~ 25 days
        /// then ~25 days to back to positive again, uint is sued to ignore the sign and double the range to 50 days
        /// </summary>
        /// <returns></returns>
        public static uint GetTime()
        {
            return (uint)Environment.TickCount;
        }

        /// <summary>
        /// Helper function to measure and update the elapsed time
        /// </summary>
        /// <param name="startTime"> The first time (in milliseconds) observed when the wait started</param>
        /// <param name="originalWaitMillisecondsTimeout">The orginal wait timeoutout in milliseconds</param>
        /// <returns>The new wait time in milliseconds, -1 if the time expired</returns>
        public static int UpdateTimeOut(uint startTime, int originalWaitMillisecondsTimeout)
        {
            // The function must be called in case the time out is not infinite
            Contract.Assert(originalWaitMillisecondsTimeout != Timeout.Infinite);

            uint elapsedMilliseconds = (GetTime() - startTime);

            // Check the elapsed milliseconds is greater than max int because this property is uint
            if (elapsedMilliseconds > int.MaxValue)
            {
                return 0;
            }

            // Subtract the elapsed time from the current wait time
            int currentWaitTimeout = originalWaitMillisecondsTimeout - (int)elapsedMilliseconds; ;
            if (currentWaitTimeout <= 0)
            {
                return 0;
            }

            return currentWaitTimeout;
        }
    }

}
