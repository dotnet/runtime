// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Threading.Tests
{
    public static class ThreadTestHelpers
    {
        public const int ExpectedTimeoutMilliseconds = 50;
        public const int UnexpectedTimeoutMilliseconds = 1000 * 30;

        // Wait longer for a thread to time out, so that an unexpected timeout in the thread is more likely to expire first and
        // provide a better stack trace for the failure
        public const int UnexpectedThreadTimeoutMilliseconds =
            UnexpectedTimeoutMilliseconds + RemoteExecutor.FailWaitTimeoutMilliseconds;

        public static Thread CreateGuardedThread(out Action waitForThread, Action start)
        {
            Action checkForThreadErrors;
            return CreateGuardedThread(out checkForThreadErrors, out waitForThread, start);
        }

        public static Thread CreateGuardedThread(out Action checkForThreadErrors, out Action waitForThread, Action start)
        {
            Exception backgroundEx = null;
            var t =
                new Thread(() =>
                {
                    try
                    {
                        start();
                    }
                    catch (Exception ex)
                    {
                        backgroundEx = ex;
                        Interlocked.MemoryBarrier();
                    }
                });
            Action localCheckForThreadErrors = checkForThreadErrors = // cannot use ref or out parameters in lambda
                () =>
                {
                    Interlocked.MemoryBarrier();
                    if (backgroundEx != null)
                    {
                        throw new AggregateException(backgroundEx);
                    }
                };
            waitForThread =
                () =>
                {
                    Assert.True(t.Join(UnexpectedThreadTimeoutMilliseconds));
                    localCheckForThreadErrors();
                };
            return t;
        }

        public static Thread CreateGuardedThread(out Action waitForThread, Action<object> start)
        {
            Action checkForThreadErrors;
            return CreateGuardedThread(out checkForThreadErrors, out waitForThread, start);
        }

        public static Thread CreateGuardedThread(out Action checkForThreadErrors, out Action waitForThread, Action<object> start)
        {
            Exception backgroundEx = null;
            var t =
                new Thread(parameter =>
                {
                    try
                    {
                        start(parameter);
                    }
                    catch (Exception ex)
                    {
                        backgroundEx = ex;
                        Interlocked.MemoryBarrier();
                    }
                });
            Action localCheckForThreadErrors = checkForThreadErrors = // cannot use ref or out parameters in lambda
                () =>
                {
                    Interlocked.MemoryBarrier();
                    if (backgroundEx != null)
                    {
                        throw new AggregateException(backgroundEx);
                    }
                };
            waitForThread =
                () =>
                {
                    Assert.True(t.Join(UnexpectedThreadTimeoutMilliseconds));
                    localCheckForThreadErrors();
                };
            return t;
        }

        public static void RunTestInBackgroundThread(Action test)
        {
            Action waitForThread;
            var t = CreateGuardedThread(out waitForThread, test);
            t.IsBackground = true;
            t.Start();
            waitForThread();
        }

        public static void RunTestInBackgroundThread(Func<Task> test)
        {
            RunTestInBackgroundThread(() => test().Wait());
        }

        public static void WaitForCondition(Func<bool> condition)
        {
            WaitForConditionWithCustomDelay(condition, () => Thread.Sleep(1));
        }

        public static void WaitForConditionWithoutBlocking(Func<bool> condition)
        {
            WaitForConditionWithCustomDelay(condition, () => Thread.Yield());
        }

        public static void WaitForConditionWithoutRelinquishingTimeSlice(Func<bool> condition)
        {
            WaitForConditionWithCustomDelay(condition, () => Thread.SpinWait(1));
        }

        public static void WaitForConditionWithCustomDelay(Func<bool> condition, Action delay)
        {
            if (condition())
            {
                return;
            }

            int startTimeMs = Environment.TickCount;
            while (true)
            {
                delay();

                if (condition())
                {
                    return;
                }

                Assert.InRange(Environment.TickCount - startTimeMs, 0, UnexpectedTimeoutMilliseconds);
            }
        }

        public static void CheckedWait(this WaitHandle wh)
        {
            Assert.True(wh.WaitOne(UnexpectedTimeoutMilliseconds));
        }

        public static void CheckedWait(this ManualResetEventSlim e)
        {
            Assert.True(e.Wait(UnexpectedTimeoutMilliseconds));
        }

        public static void CheckedWait(this Task t)
        {
            Assert.True(t.Wait(UnexpectedTimeoutMilliseconds));
        }
    }
}
