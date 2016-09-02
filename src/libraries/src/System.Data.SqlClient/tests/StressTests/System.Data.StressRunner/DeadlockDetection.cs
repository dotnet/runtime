// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DPStressHarness
{
    public class DeadlockDetection
    {
        /// <summary>
        /// Information for a thread relating to deadlock detection. All of its information is stored in a reference object to make updating it easier.
        /// </summary>
        private class ThreadInfo
        {
            public ThreadInfo(long dueTime)
            {
                this.DueTime = dueTime;
            }

            /// <summary>
            /// The time (in ticks) when the thread should be completed
            /// </summary>
            public long DueTime;

            /// <summary>
            /// True if the thread should not be aborted
            /// </summary>
            public bool DisableAbort;

            /// <summary>
            /// The time when DisableAbort was set to true
            /// </summary>
            public long DisableAbortTime;
        }

        /// <summary>
        /// Maximum time that a test thread (i.e. a thread that is directly executing a [StressTest] method) can
        /// execute before it is considered to be deadlocked. This should be longer than the 
        /// TaskThreadDeadlockTimeoutTicks because if the test is waiting for a task then the test will always
        /// take longer to execute than the task.
        /// </summary>
        public const long TestThreadDeadlockTimeoutTicks = 20 * 60 * TimeSpan.TicksPerSecond;

        /// <summary>
        /// Maximum time that any Task can execute before it is considered to be deadlocked
        /// </summary>
        public const long TaskThreadDeadlockTimeoutTicks = 10 * 60 * TimeSpan.TicksPerSecond;

        /// <summary>
        /// Dictionary that maps Threads to the time (in ticks) when they should be completed. If they are not completed by that time then
        /// they are considered to be deadlocked.
        /// </summary>
        private static ConcurrentDictionary<Thread, ThreadInfo> s_threadDueTimes = null;

        /// <summary>
        /// Timer that scans through _threadDueTimes to find deadlocked threads
        /// </summary>
        private static Timer s_deadlockWatchdog = null;

        /// <summary>
        /// Interval of _deadlockWatchdog, in milliseconds
        /// </summary>
        private const int _watchdogIntervalMs = 60 * 1000;

        /// <summary>
        /// true if deadlock detection is enabled, otherwise false. Should be set only at process startup.
        /// </summary>
        private static bool s_isEnabled = false;

        /// <summary>
        /// Enables deadlock detection.
        /// </summary>
        public static void Enable()
        {
            // Switch out the default TaskScheduler. We must use reflection because it is private.
            FieldInfo defaultTaskScheduler = typeof(TaskScheduler).GetField("s_defaultTaskScheduler", BindingFlags.NonPublic | BindingFlags.Static);
            DeadlockDetectionTaskScheduler newTaskScheduler = new DeadlockDetectionTaskScheduler();
            defaultTaskScheduler.SetValue(null, newTaskScheduler);

            s_threadDueTimes = new ConcurrentDictionary<Thread, ThreadInfo>();
            s_deadlockWatchdog = new Timer(CheckForDeadlocks, null, _watchdogIntervalMs, _watchdogIntervalMs);

            s_isEnabled = true;
        }

        /// <summary>
        /// Adds the current Task execution thread to the tracked thread collection.
        /// </summary>
        public static void AddTaskThread()
        {
            if (s_isEnabled)
            {
                long dueTime = DateTime.UtcNow.Ticks + TaskThreadDeadlockTimeoutTicks;
                AddThread(dueTime);
            }
        }

        /// <summary>
        /// Adds the current Test execution thread (i.e. a thread that is directly executing a [StressTest] method) to the tracked thread collection.
        /// </summary>
        public static void AddTestThread()
        {
            if (s_isEnabled)
            {
                long dueTime = DateTime.UtcNow.Ticks + TestThreadDeadlockTimeoutTicks;
                AddThread(dueTime);
            }
        }

        private static void AddThread(long dueTime)
        {
            s_threadDueTimes.TryAdd(Thread.CurrentThread, new ThreadInfo(dueTime));
        }

        /// <summary>
        /// Removes the current thread from the tracked thread collection
        /// </summary>
        public static void RemoveThread()
        {
            if (s_isEnabled)
            {
                ThreadInfo unused;
                s_threadDueTimes.TryRemove(Thread.CurrentThread, out unused);
            }
        }

        /// <summary>
        /// Disables abort of current thread. Call this when the current thread is waiting on a task.
        /// </summary>
        public static void DisableThreadAbort()
        {
            if (s_isEnabled)
            {
                ThreadInfo threadInfo;
                if (s_threadDueTimes.TryGetValue(Thread.CurrentThread, out threadInfo))
                {
                    threadInfo.DisableAbort = true;
                    threadInfo.DisableAbortTime = DateTime.UtcNow.Ticks;
                }
            }
        }

        /// <summary>
        /// Enables abort of current thread after calling DisableThreadAbort(). The elapsed time since calling DisableThreadAbort() is added to the due time.
        /// </summary>
        public static void EnableThreadAbort()
        {
            if (s_isEnabled)
            {
                ThreadInfo threadInfo;
                if (s_threadDueTimes.TryGetValue(Thread.CurrentThread, out threadInfo))
                {
                    threadInfo.DueTime += DateTime.UtcNow.Ticks - threadInfo.DisableAbortTime;
                    threadInfo.DisableAbort = false;
                }
            }
        }

        /// <summary>
        /// Looks through the tracked thread collection and aborts any thread that is past its due time
        /// </summary>
        /// <param name="state">unused</param>
        private static void CheckForDeadlocks(object state)
        {
            if (s_isEnabled)
            {
                long now = DateTime.UtcNow.Ticks;

                // Find candidate threads
                foreach (var threadDuePair in s_threadDueTimes)
                {
                    if (!threadDuePair.Value.DisableAbort && now > threadDuePair.Value.DueTime)
                    {
                        // Abort the misbehaving thread and the return
                        // NOTE: We only want to abort a single thread at a time to allow the other thread in the deadlock pair to continue
                        Thread t = threadDuePair.Key;
                        Console.WriteLine("Deadlock detected on thread with managed thread id {0}", t.ManagedThreadId);
                        Debugger.Break();
                        t.Join();
                        return;
                    }
                }
            }
        }
    }
}
