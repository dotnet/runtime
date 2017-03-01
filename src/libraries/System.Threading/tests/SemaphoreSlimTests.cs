// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Tests
{
    /// <summary>
    /// SemaphoreSlim unit tests
    /// </summary>
    public class SemaphoreSlimTests
    {
        /// <summary>
        /// SemaphoreSlim public methods and properties to be tested
        /// </summary>
        private enum SemaphoreSlimActions
        {
            Constructor,
            Wait,
            WaitAsync,
            Release,
            Dispose,
            CurrentCount,
            AvailableWaitHandle
        }

        [Fact]
        public static void RunSemaphoreSlimTest0_Ctor()
        {
            RunSemaphoreSlimTest0_Ctor(0, 10, null);
            RunSemaphoreSlimTest0_Ctor(5, 10, null);
            RunSemaphoreSlimTest0_Ctor(10, 10, null);
        }

        [Fact]
        public static void RunSemaphoreSlimTest0_Ctor_Negative()
        {
            RunSemaphoreSlimTest0_Ctor(10, 0, typeof(ArgumentOutOfRangeException));
            RunSemaphoreSlimTest0_Ctor(10, -1, typeof(ArgumentOutOfRangeException));
            RunSemaphoreSlimTest0_Ctor(-1, 10, typeof(ArgumentOutOfRangeException));
        }

        [Fact]
        public static void RunSemaphoreSlimTest1_Wait()
        {
            // Infinite timeout
            RunSemaphoreSlimTest1_Wait(10, 10, -1, true, null);
            RunSemaphoreSlimTest1_Wait(1, 10, -1, true, null);

            // Zero timeout
            RunSemaphoreSlimTest1_Wait(10, 10, 0, true, null);
            RunSemaphoreSlimTest1_Wait(1, 10, 0, true, null);
            RunSemaphoreSlimTest1_Wait(0, 10, 0, false, null);

            // Positive timeout
            RunSemaphoreSlimTest1_Wait(10, 10, 10, true, null);
            RunSemaphoreSlimTest1_Wait(1, 10, 10, true, null);
            RunSemaphoreSlimTest1_Wait(0, 10, 10, false, null);
        }

        [Fact]
        public static void RunSemaphoreSlimTest1_Wait_NegativeCases()
        {
            // Invalid timeout
            RunSemaphoreSlimTest1_Wait(10, 10, -10, true, typeof(ArgumentOutOfRangeException));
            RunSemaphoreSlimTest1_Wait
               (10, 10, new TimeSpan(0, 0, Int32.MaxValue), true, typeof(ArgumentOutOfRangeException));
        }

        [Fact]
        public static void RunSemaphoreSlimTest1_WaitAsync()
        {
            // Infinite timeout
            RunSemaphoreSlimTest1_WaitAsync(10, 10, -1, true, null);
            RunSemaphoreSlimTest1_WaitAsync(1, 10, -1, true, null);

            // Zero timeout
            RunSemaphoreSlimTest1_WaitAsync(10, 10, 0, true, null);
            RunSemaphoreSlimTest1_WaitAsync(1, 10, 0, true, null);
            RunSemaphoreSlimTest1_WaitAsync(0, 10, 0, false, null);

            // Positive timeout
            RunSemaphoreSlimTest1_WaitAsync(10, 10, 10, true, null);
            RunSemaphoreSlimTest1_WaitAsync(1, 10, 10, true, null);
            RunSemaphoreSlimTest1_WaitAsync(0, 10, 10, false, null);
        }

        [Fact]
        public static void RunSemaphoreSlimTest1_WaitAsync_NegativeCases()
        {
            // Invalid timeout
            RunSemaphoreSlimTest1_WaitAsync(10, 10, -10, true, typeof(ArgumentOutOfRangeException));
            RunSemaphoreSlimTest1_WaitAsync
               (10, 10, new TimeSpan(0, 0, Int32.MaxValue), true, typeof(ArgumentOutOfRangeException));
            RunSemaphoreSlimTest1_WaitAsync2();
        }

        [Fact]
        public static void RunSemaphoreSlimTest2_Release()
        {
            // Valid release count
            RunSemaphoreSlimTest2_Release(5, 10, 1, null);
            RunSemaphoreSlimTest2_Release(0, 10, 1, null);
            RunSemaphoreSlimTest2_Release(5, 10, 5, null);
        }

        [Fact]
        public static void RunSemaphoreSlimTest2_Release_NegativeCases()
        {
            // Invalid release count
            RunSemaphoreSlimTest2_Release(5, 10, 0, typeof(ArgumentOutOfRangeException));
            RunSemaphoreSlimTest2_Release(5, 10, -1, typeof(ArgumentOutOfRangeException));

            // Semaphore Full
            RunSemaphoreSlimTest2_Release(10, 10, 1, typeof(SemaphoreFullException));
            RunSemaphoreSlimTest2_Release(5, 10, 6, typeof(SemaphoreFullException));
            RunSemaphoreSlimTest2_Release(int.MaxValue - 1, int.MaxValue, 10, typeof(SemaphoreFullException));
        }

        [Fact]
        public static void RunSemaphoreSlimTest4_Dispose()
        {
            RunSemaphoreSlimTest4_Dispose(5, 10, null, null);
            RunSemaphoreSlimTest4_Dispose(5, 10, SemaphoreSlimActions.CurrentCount, null);
            RunSemaphoreSlimTest4_Dispose
               (5, 10, SemaphoreSlimActions.Wait, typeof(ObjectDisposedException));
            RunSemaphoreSlimTest4_Dispose
               (5, 10, SemaphoreSlimActions.WaitAsync, typeof(ObjectDisposedException));
            RunSemaphoreSlimTest4_Dispose
              (5, 10, SemaphoreSlimActions.Release, typeof(ObjectDisposedException));
            RunSemaphoreSlimTest4_Dispose
              (5, 10, SemaphoreSlimActions.AvailableWaitHandle, typeof(ObjectDisposedException));
        }

        [Fact]
        public static void RunSemaphoreSlimTest5_CurrentCount()
        {
            RunSemaphoreSlimTest5_CurrentCount(5, 10, null);
            RunSemaphoreSlimTest5_CurrentCount(5, 10, SemaphoreSlimActions.Wait);
            RunSemaphoreSlimTest5_CurrentCount(5, 10, SemaphoreSlimActions.WaitAsync);
            RunSemaphoreSlimTest5_CurrentCount(5, 10, SemaphoreSlimActions.Release);
        }

        [Fact]
        public static void RunSemaphoreSlimTest7_AvailableWaitHandle()
        {
            RunSemaphoreSlimTest7_AvailableWaitHandle(5, 10, null, true);
            RunSemaphoreSlimTest7_AvailableWaitHandle(0, 10, null, false);

            RunSemaphoreSlimTest7_AvailableWaitHandle(5, 10, SemaphoreSlimActions.Wait, true);
            RunSemaphoreSlimTest7_AvailableWaitHandle(1, 10, SemaphoreSlimActions.Wait, false);
            RunSemaphoreSlimTest7_AvailableWaitHandle(5, 10, SemaphoreSlimActions.Wait, true);

            RunSemaphoreSlimTest7_AvailableWaitHandle(5, 10, SemaphoreSlimActions.WaitAsync, true);
            RunSemaphoreSlimTest7_AvailableWaitHandle(1, 10, SemaphoreSlimActions.WaitAsync, false);
            RunSemaphoreSlimTest7_AvailableWaitHandle(5, 10, SemaphoreSlimActions.WaitAsync, true);
            RunSemaphoreSlimTest7_AvailableWaitHandle(0, 10, SemaphoreSlimActions.Release, true);
        }

        /// <summary>
        /// Test SemaphoreSlim constructor
        /// </summary>
        /// <param name="initial">The initial semaphore count</param>
        /// <param name="maximum">The maximum semaphore count</param>
        /// <param name="exceptionType">The type of the thrown exception in case of invalid cases,
        /// null for valid cases</param>
        /// <returns>True if the test succeeded, false otherwise</returns>
        private static void RunSemaphoreSlimTest0_Ctor(int initial, int maximum, Type exceptionType)
        {
            string methodFailed = "RunSemaphoreSlimTest0_Ctor(" + initial + "," + maximum + "):  FAILED.  ";
            Exception exception = null;
            try
            {
                SemaphoreSlim semaphore = new SemaphoreSlim(initial, maximum);
                Assert.Equal(initial, semaphore.CurrentCount);
            }
            catch (Exception ex)
            {
                Assert.NotNull(exceptionType);
                Assert.IsType(exceptionType, ex);
                exception = ex;
            }
        }

        /// <summary>
        /// Test SemaphoreSlim Wait
        /// </summary>
        /// <param name="initial">The initial semaphore count</param>
        /// <param name="maximum">The maximum semaphore count</param>
        /// <param name="timeout">The timeout parameter for the wait method, it must be either int or TimeSpan</param>
        /// <param name="returnValue">The expected wait return value</param>
        /// <param name="exceptionType">The type of the thrown exception in case of invalid cases,
        /// null for valid cases</param>
        /// <returns>True if the test succeeded, false otherwise</returns>
        private static void RunSemaphoreSlimTest1_Wait
            (int initial, int maximum, object timeout, bool returnValue, Type exceptionType)
        {
            SemaphoreSlim semaphore = new SemaphoreSlim(initial, maximum);
            try
            {
                bool result = false;
                if (timeout is TimeSpan)
                {
                    result = semaphore.Wait((TimeSpan)timeout);
                }
                else
                {
                    result = semaphore.Wait((int)timeout);
                }
                Assert.Equal(returnValue, result);
                if (result)
                {
                    Assert.Equal(initial - 1, semaphore.CurrentCount);
                }
            }
            catch (Exception ex)
            {
                Assert.NotNull(exceptionType);
                Assert.IsType(exceptionType, ex);
            }
        }

        /// <summary>
        /// Test SemaphoreSlim WaitAsync
        /// </summary>
        /// <param name="initial">The initial semaphore count</param>
        /// <param name="maximum">The maximum semaphore count</param>
        /// <param name="timeout">The timeout parameter for the wait method, it must be either int or TimeSpan</param>
        /// <param name="returnValue">The expected wait return value</param>
        /// <param name="exceptionType">The type of the thrown exception in case of invalid cases,
        /// null for valid cases</param>
        /// <returns>True if the test succeeded, false otherwise</returns>
        private static void RunSemaphoreSlimTest1_WaitAsync
            (int initial, int maximum, object timeout, bool returnValue, Type exceptionType)
        {
            SemaphoreSlim semaphore = new SemaphoreSlim(initial, maximum);
            try
            {
                bool result = false;
                if (timeout is TimeSpan)
                {
                    result = semaphore.WaitAsync((TimeSpan)timeout).Result;
                }
                else
                {
                    result = semaphore.WaitAsync((int)timeout).Result;
                }
                Assert.Equal(returnValue, result);
                if (result)
                {
                    Assert.Equal(initial - 1, semaphore.CurrentCount);
                }
            }
            catch (Exception ex)
            {
                Assert.NotNull(exceptionType);
                Assert.IsType(exceptionType, ex);
            }
        }

        /// <summary>
        /// Test SemaphoreSlim WaitAsync
        /// The test verifies that SemaphoreSlim.Release() does not execute any user code synchronously.
        /// </summary>
        private static void RunSemaphoreSlimTest1_WaitAsync2()
        {
            SemaphoreSlim semaphore = new SemaphoreSlim(1);
            ThreadLocal<int> counter = new ThreadLocal<int>(() => 0);
            bool nonZeroObserved = false;

            const int asyncActions = 20;
            int remAsyncActions = asyncActions;
            ManualResetEvent mre = new ManualResetEvent(false);

            Action<int> doWorkAsync = async delegate (int i)
            {
                await semaphore.WaitAsync();
                if (counter.Value > 0)
                {
                    nonZeroObserved = true;
                }

                counter.Value = counter.Value + 1;
                semaphore.Release();
                counter.Value = counter.Value - 1;

                if (Interlocked.Decrement(ref remAsyncActions) == 0) mre.Set();
            };

            semaphore.Wait();
            for (int i = 0; i < asyncActions; i++) doWorkAsync(i);
            semaphore.Release();

            mre.WaitOne();

            Assert.False(nonZeroObserved, "RunSemaphoreSlimTest1_WaitAsync2:  FAILED.  SemaphoreSlim.Release() seems to have synchronously invoked a continuation.");
        }

        /// <summary>
        /// Test SemaphoreSlim Release
        /// </summary>
        /// <param name="initial">The initial semaphore count</param>
        /// <param name="maximum">The maximum semaphore count</param>
        /// <param name="releaseCount">The release count for the release method</param>
        /// <param name="exceptionType">The type of the thrown exception in case of invalid cases,
        /// null for valid cases</param>
        /// <returns>True if the test succeeded, false otherwise</returns>
        private static void RunSemaphoreSlimTest2_Release
           (int initial, int maximum, int releaseCount, Type exceptionType)
        {
            SemaphoreSlim semaphore = new SemaphoreSlim(initial, maximum);
            try
            {
                int oldCount = semaphore.Release(releaseCount);
                Assert.Equal(initial, oldCount);
                Assert.Equal(initial + releaseCount, semaphore.CurrentCount);
            }
            catch (Exception ex)
            {
                Assert.NotNull(exceptionType);
                Assert.IsType(exceptionType, ex);
            }
        }

        /// <summary>
        /// Call specific SemaphoreSlim method or property
        /// </summary>
        /// <param name="semaphore">The SemaphoreSlim instance</param>
        /// <param name="action">The action name</param>
        /// <param name="param">The action parameter, null if it takes no parameters</param>
        /// <returns>The action return value, null if the action returns void</returns>
        private static object CallSemaphoreAction
            (SemaphoreSlim semaphore, SemaphoreSlimActions? action, object param)
        {
            if (action == SemaphoreSlimActions.Wait)
            {
                if (param is TimeSpan)
                {
                    return semaphore.Wait((TimeSpan)param);
                }
                else if (param is int)
                {
                    return semaphore.Wait((int)param);
                }
                semaphore.Wait();
                return null;
            }
            else if (action == SemaphoreSlimActions.WaitAsync)
            {
                if (param is TimeSpan)
                {
                    return semaphore.WaitAsync((TimeSpan)param).Result;
                }
                else if (param is int)
                {
                    return semaphore.WaitAsync((int)param).Result;
                }
                semaphore.WaitAsync().Wait();
                return null;
            }
            else if (action == SemaphoreSlimActions.Release)
            {
                if (param != null)
                {
                    return semaphore.Release((int)param);
                }
                return semaphore.Release();
            }
            else if (action == SemaphoreSlimActions.Dispose)
            {
                semaphore.Dispose();
                return null;
            }
            else if (action == SemaphoreSlimActions.CurrentCount)
            {
                return semaphore.CurrentCount;
            }
            else if (action == SemaphoreSlimActions.AvailableWaitHandle)
            {
                return semaphore.AvailableWaitHandle;
            }

            return null;
        }

        /// <summary>
        /// Test SemaphoreSlim Dispose
        /// </summary>
        /// <param name="initial">The initial semaphore count</param>
        /// <param name="maximum">The maximum semaphore count</param>
        /// <param name="action">SemaphoreSlim action to be called after Dispose</param>
        /// <param name="exceptionType">The type of the thrown exception in case of invalid cases,
        /// null for valid cases</param>
        /// <returns>True if the test succeeded, false otherwise</returns>
        private static void RunSemaphoreSlimTest4_Dispose(int initial, int maximum, SemaphoreSlimActions? action, Type exceptionType)
        {
            SemaphoreSlim semaphore = new SemaphoreSlim(initial, maximum);
            try
            {
                semaphore.Dispose();
                CallSemaphoreAction(semaphore, action, null);
            }
            catch (Exception ex)
            {
                Assert.NotNull(exceptionType);
                Assert.IsType(exceptionType, ex);
            }
        }

        /// <summary>
        /// Test SemaphoreSlim CurrentCount property
        /// </summary>
        /// <param name="initial">The initial semaphore count</param>
        /// <param name="maximum">The maximum semaphore count</param>
        /// <param name="action">SemaphoreSlim action to be called before CurrentCount</param>
        /// <returns>True if the test succeeded, false otherwise</returns>
        private static void RunSemaphoreSlimTest5_CurrentCount(int initial, int maximum, SemaphoreSlimActions? action)
        {
            SemaphoreSlim semaphore = new SemaphoreSlim(initial, maximum);

            CallSemaphoreAction(semaphore, action, null);
            if (action == null)
            {
                Assert.Equal(initial, semaphore.CurrentCount);
            }
            else
            {
                Assert.Equal(initial + (action == SemaphoreSlimActions.Release ? 1 : -1), semaphore.CurrentCount);
            }
        }

        /// <summary>
        /// Test SemaphoreSlim AvailableWaitHandle property
        /// </summary>
        /// <param name="initial">The initial semaphore count</param>
        /// <param name="maximum">The maximum semaphore count</param>
        /// <param name="action">SemaphoreSlim action to be called before WaitHandle</param>
        /// <param name="state">The expected wait handle state</param>
        /// <returns>True if the test succeeded, false otherwise</returns>
        private static void RunSemaphoreSlimTest7_AvailableWaitHandle(int initial, int maximum, SemaphoreSlimActions? action, bool state)
        {
            SemaphoreSlim semaphore = new SemaphoreSlim(initial, maximum);

            CallSemaphoreAction(semaphore, action, null);
            Assert.NotNull(semaphore.AvailableWaitHandle);
            Assert.Equal(state, semaphore.AvailableWaitHandle.WaitOne(0));
        }

        /// <summary>
        /// Test SemaphoreSlim Wait and Release methods concurrently
        /// </summary>
        /// <param name="initial">The initial semaphore count</param>
        /// <param name="maximum">The maximum semaphore count</param>
        /// <param name="waitThreads">Number of the threads that call Wait method</param>
        /// <param name="releaseThreads">Number of the threads that call Release method</param>
        /// <param name="succeededWait">Number of succeeded wait threads</param>
        /// <param name="failedWait">Number of failed wait threads</param>
        /// <param name="finalCount">The final semaphore count</param>
        /// <returns>True if the test succeeded, false otherwise</returns>
        [Theory]
        [InlineData(5, 1000, 50, 50, 50, 0, 5, 1000)]
        [InlineData(0, 1000, 50, 25, 25, 25, 0, 500)]
        [InlineData(0, 1000, 50, 0, 0, 50, 0, 100)]
        public static void RunSemaphoreSlimTest8_ConcWaitAndRelease(int initial, int maximum,
            int waitThreads, int releaseThreads, int succeededWait, int failedWait, int finalCount, int timeout)
        {
            SemaphoreSlim semaphore = new SemaphoreSlim(initial, maximum);
            Task[] threads = new Task[waitThreads + releaseThreads];
            int succeeded = 0;
            int failed = 0;
            ManualResetEvent mre = new ManualResetEvent(false);
            // launch threads
            for (int i = 0; i < threads.Length; i++)
            {
                if (i < waitThreads)
                {
                    // We are creating the Task using TaskCreationOptions.LongRunning to
                    // force usage of another thread (which will be the case on the default scheduler
                    // with its current implementation).  Without this, the release tasks will likely get
                    // queued behind the wait tasks in the pool, making it very likely that the wait tasks
                    // will starve the very tasks that when run would unblock them.
                    threads[i] = new Task(delegate ()
                       {
                           mre.WaitOne();
                           if (semaphore.Wait(timeout))
                           {
                               Interlocked.Increment(ref succeeded);
                           }
                           else
                           {
                               Interlocked.Increment(ref failed);
                           }
                       }, TaskCreationOptions.LongRunning);
                }
                else
                {
                    threads[i] = new Task(delegate ()
                       {
                           mre.WaitOne();
                           semaphore.Release();
                       });
                }
                threads[i].Start(TaskScheduler.Default);
            }

            mre.Set();
            //wait work to be done;
            Task.WaitAll(threads);
            //check the number of succeeded and failed wait
            Assert.Equal(succeededWait, succeeded);
            Assert.Equal(failedWait, failed);
            Assert.Equal(finalCount, semaphore.CurrentCount);
        }

        /// <summary>
        /// Test SemaphoreSlim WaitAsync and Release methods concurrently
        /// </summary>
        /// <param name="initial">The initial semaphore count</param>
        /// <param name="maximum">The maximum semaphore count</param>
        /// <param name="waitThreads">Number of the threads that call Wait method</param>
        /// <param name="releaseThreads">Number of the threads that call Release method</param>
        /// <param name="succeededWait">Number of succeeded wait threads</param>
        /// <param name="failedWait">Number of failed wait threads</param>
        /// <param name="finalCount">The final semaphore count</param>
        /// <returns>True if the test succeeded, false otherwise</returns>
        [Theory]
        [InlineData(5, 1000, 50, 50, 50, 0, 5, 500)]
        [InlineData(0, 1000, 50, 25, 25, 25, 0, 500)]
        [InlineData(0, 1000, 50, 0, 0, 50, 0, 100)]
        private static void RunSemaphoreSlimTest8_ConcWaitAsyncAndRelease(int initial, int maximum,
            int waitThreads, int releaseThreads, int succeededWait, int failedWait, int finalCount, int timeout)
        {
            SemaphoreSlim semaphore = new SemaphoreSlim(initial, maximum);
            Task[] tasks = new Task[waitThreads + releaseThreads];
            int succeeded = 0;
            int failed = 0;
            ManualResetEvent mre = new ManualResetEvent(false);
            // launch threads
            for (int i = 0; i < tasks.Length; i++)
            {
                if (i < waitThreads)
                {
                    tasks[i] = Task.Run(async delegate
                    {
                        mre.WaitOne();
                        if (await semaphore.WaitAsync(timeout))
                        {
                            Interlocked.Increment(ref succeeded);
                        }
                        else
                        {
                            Interlocked.Increment(ref failed);
                        }
                    });
                }
                else
                {
                    tasks[i] = Task.Run(delegate
                    {
                        mre.WaitOne();
                        semaphore.Release();
                    });
                }
            }

            mre.Set();
            //wait work to be done;
            Task.WaitAll(tasks);

            Assert.Equal(succeededWait, succeeded);
            Assert.Equal(failedWait, failed);
            Assert.Equal(finalCount, semaphore.CurrentCount);
        }

        [Theory]
        [InlineData(10, 10)]
        [InlineData(1, 10)]
        [InlineData(10, 1)]
        public static void TestConcurrentWaitAndWaitAsync(int syncWaiters, int asyncWaiters)
        {
            int totalWaiters = syncWaiters + asyncWaiters;

            var semaphore = new SemaphoreSlim(0);
            Task[] tasks = new Task[totalWaiters];

            const int ITERS = 10;
            int randSeed = unchecked((int)DateTime.Now.Ticks);
            for (int i = 0; i < syncWaiters; i++)
            {
                tasks[i] = Task.Run(delegate
                {
                    //Random rand = new Random(Interlocked.Increment(ref randSeed));
                    for (int iter = 0; iter < ITERS; iter++)
                    {
                        semaphore.Wait();
                        semaphore.Release();
                    }
                });
            }
            for (int i = syncWaiters; i < totalWaiters; i++)
            {
                tasks[i] = Task.Run(async delegate
                {
                    //Random rand = new Random(Interlocked.Increment(ref randSeed));
                    for (int iter = 0; iter < ITERS; iter++)
                    {
                        await semaphore.WaitAsync();
                        semaphore.Release();
                    }
                });
            }

            semaphore.Release(totalWaiters / 2);
            Task.WaitAll(tasks);
        }
    }
}
