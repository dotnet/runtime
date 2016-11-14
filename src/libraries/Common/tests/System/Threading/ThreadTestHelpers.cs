// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Threading.Tests
{
    public static class ThreadTestHelpers
    {
        public const int ExpectedTimeoutMilliseconds = 50;
        public const int UnexpectedTimeoutMilliseconds = 1000 * 30;

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
                        throw new Exception("Background thread exception", backgroundEx);
                    }
                };
            waitForThread =
                () =>
                {
                    Assert.True(t.Join(UnexpectedTimeoutMilliseconds));
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
                        throw new Exception("Background thread exception", backgroundEx);
                    }
                };
            waitForThread =
                () =>
                {
                    Assert.True(t.Join(UnexpectedTimeoutMilliseconds));
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

        public static void WaitForCondition(Func<bool> condition)
        {
            WaitForConditionWithCustomDelay(condition, () => Thread.Sleep(1));
        }

        public static void WaitForConditionWithoutBlocking(Func<bool> condition)
        {
            WaitForConditionWithCustomDelay(condition, () => Thread.Yield());
        }

        public static void WaitForConditionWithCustomDelay(Func<bool> condition, Action delay)
        {
            var startTime = DateTime.Now;
            while (!condition())
            {
                Assert.True((DateTime.Now - startTime).TotalMilliseconds < UnexpectedTimeoutMilliseconds);
                delay();
            }
        }

        public static void CheckedWait(this WaitHandle wh)
        {
            Assert.True(wh.WaitOne(UnexpectedTimeoutMilliseconds));
        }
    }
}
