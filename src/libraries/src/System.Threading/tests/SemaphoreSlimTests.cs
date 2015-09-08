// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        [Fact]
        [OuterLoop]
        public static void RunSemaphoreSlimCurrentTests()
        {
            RunSemaphoreSlimTest8_ConcWaitAndRelease
               (5, 1000, 50, 50, 50, 0, 5, 1000);
            RunSemaphoreSlimTest8_ConcWaitAndRelease
               (0, 1000, 50, 25, 25, 25, 0, 5000);
            RunSemaphoreSlimTest8_ConcWaitAndRelease
              (0, 1000, 50, 0, 0, 50, 0, 100);
            RunSemaphoreSlimTest8_ConcWaitAsyncAndRelease
               (5, 1000, 50, 50, 50, 0, 5, 1000);
            RunSemaphoreSlimTest8_ConcWaitAsyncAndRelease
               (0, 1000, 50, 25, 25, 25, 0, 5000);
            RunSemaphoreSlimTest8_ConcWaitAsyncAndRelease
              (0, 1000, 50, 0, 0, 50, 0, 100);
            TestConcurrentWaitAndWaitAsync(10, 10);
            TestConcurrentWaitAndWaitAsync(1, 10);
            TestConcurrentWaitAndWaitAsync(10, 1);
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
                if (semaphore.CurrentCount != initial)
                {
                    Assert.True(false, string.Format(methodFailed + "Constructor test failed, expected " + initial + " actual " + semaphore.CurrentCount));
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            // The code threw excption and it is not expected because the excyptionType param is null
            if (exceptionType == null && exception != null)
            {
                Assert.True(false, string.Format(methodFailed + "Constructor failed, the code threw an exception, and it is not supposed to."));
            }

            // Compare both exception types in case of the code threw exception
            if (exception != null && !Type.Equals(exception.GetType(), exceptionType))
            {
                Assert.True(false, string.Format(methodFailed + "Constructor failed, Excption types do not match"));
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
            string methodFailed = "RunSemaphoreSlimTest1_Wait(" + initial + "," + maximum + "," + timeout + "): FAILED.  ";
            Exception exception = null;
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

                if (result != returnValue ||
                (result && semaphore.CurrentCount != initial - 1))
                {
                    Assert.True(false, string.Format(methodFailed + "Wait failed, the method returned " + result + " and expected " + returnValue));
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            // The code threw excption and it is not expected because the excyptionType param is null
            if (exceptionType == null && exception != null)
            {
                Assert.True(false, string.Format(methodFailed + "Wait failed, the code threw an exception, and it is not supposed to."));
            }

            // Compare both exception types in case of the code threw exception
            if (exception != null && !Type.Equals(exception.GetType(), exceptionType))
            {
                Assert.True(false, string.Format(methodFailed + "Wait failed, Excption types do not match"));
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
            string methodFailed = "RunSemaphoreSlimTest1_WaitAsync(" + initial + "," + maximum + "," + timeout + "):  FAILED.  ";
            Exception exception = null;
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

                if (result != returnValue ||
                (result && semaphore.CurrentCount != initial - 1))
                {
                    Assert.True(false, string.Format(methodFailed + "WaitAsync failed, the method returned " + result + " and expected " + returnValue));
                }
            }
            catch (AggregateException ex)
            {
                exception = ex.InnerException;
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            // The code threw excption and it is not expected because the excyptionType param is null
            if (exceptionType == null && exception != null)
            {
                Assert.True(false, string.Format(methodFailed + "WaitAsync failed, the code threw an exception, and it is not supposed to."));
            }

            // Compare both exception types in case of the code threw exception
            if (exception != null && !Type.Equals(exception.GetType(), exceptionType))
            {
                Assert.True(false, string.Format(methodFailed + "WaitAsync failed, Excption types do not match"));
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

            Assert.True(!nonZeroObserved, "RunSemaphoreSlimTest1_WaitAsync2:  FAILED.  SemaphoreSlim.Release() seems to have synchronously invoked a continuation.");
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
            string methodFailed = "RunSemaphoreSlimTest2_Release(" + initial + "," + maximum + "," + releaseCount + "):  FAILED.  ";
            Exception exception = null;
            SemaphoreSlim semaphore = new SemaphoreSlim(initial, maximum);
            try
            {
                int oldCount = semaphore.Release(releaseCount);
                if (semaphore.CurrentCount != initial + releaseCount || oldCount != initial)
                {
                    Assert.True(false, string.Format(methodFailed + "Release failed, the method returned " + oldCount + " and expected " + initial));
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            // The code threw excption and it is not expected because the excyptionType param is null
            if (exceptionType == null && exception != null)
            {
                Assert.True(false, string.Format(methodFailed + "Release failed, the code threw an exception, and it is not supposed to."));
            }

            // Compare both exception types in case of the code threw exception
            if (exception != null && !Type.Equals(exception.GetType(), exceptionType))
            {
                Assert.True(false, string.Format(methodFailed + "Release failed, Excption types do not match"));
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
            Exception exception = null;
            SemaphoreSlim semaphore = new SemaphoreSlim(initial, maximum);
            try
            {
                semaphore.Dispose();
                CallSemaphoreAction(semaphore, action, null);
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            // The code threw excption and it is not expected because the excyptionType param is null
            if (exceptionType == null && exception != null)
            {
                string methodFailed = "RunSemaphoreSlimTest4_Dispose(" + initial + "," + maximum + "," + action + "): FAILED.  ";
                Assert.True(false, string.Format(methodFailed + "Dispose failed, the code threw an exception, and it is not supposed to."));
            }

            // Compare both exception types in case of the code threw exception
            if (exception != null && !Type.Equals(exception.GetType(), exceptionType))
            {
                string methodFailed = "RunSemaphoreSlimTest4_Dispose(" + initial + "," + maximum + "," + action + "): FAILED.  ";
                Assert.True(false, string.Format(methodFailed + "Dispose failed, Excption types do not match"));
            }
        }

        /// <summary>
        /// Test SemaphoreSlim CurrentCount property
        /// </summary>
        /// <param name="initial">The initial semaphore count</param>
        /// <param name="maximum">The maximum semaphore count</param>
        /// <param name="action">SemaphoreSlim action to be called before CurentCount</param>
        /// <returns>True if the test succeeded, false otherwise</returns>
        private static void RunSemaphoreSlimTest5_CurrentCount(int initial, int maximum, SemaphoreSlimActions? action)
        {
            SemaphoreSlim semaphore = new SemaphoreSlim(initial, maximum);
            try
            {
                CallSemaphoreAction(semaphore, action, null);
                if ((action == SemaphoreSlimActions.Wait && semaphore.CurrentCount != initial - 1)
                || (action == SemaphoreSlimActions.WaitAsync && semaphore.CurrentCount != initial - 1)
                || (action == SemaphoreSlimActions.Release && semaphore.CurrentCount != initial + 1))
                {
                    string methodFailed = "RunSemaphoreSlimTest5_CurrentCount(" + initial + "," + maximum + "," + action + "): FAILED.  ";
                    Assert.True(false, string.Format(methodFailed + "CurrentCount failed"));
                }
            }
            catch (Exception ex)
            {
                string methodFailed = "RunSemaphoreSlimTest5_CurrentCount(" + initial + "," + maximum + "," + action + "): FAILED.  ";
                Assert.True(false, string.Format(methodFailed + "CurrentCount failed, the code threw exception " + ex));
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
            try
            {
                CallSemaphoreAction(semaphore, action, null);
                if (semaphore.AvailableWaitHandle == null)
                {
                    string methodFailed = "RunSemaphoreSlimTest7_AvailableWaitHandle(" + initial + "," + maximum + "," + action + "): FAILED.  ";
                    Assert.True(false, string.Format(methodFailed + "AvailableWaitHandle failed, handle is null."));
                }
                if (semaphore.AvailableWaitHandle.WaitOne(0) != state)
                {
                    string methodFailed = "RunSemaphoreSlimTest7_AvailableWaitHandle(" + initial + "," + maximum + "," + action + "): FAILED.  ";
                    Assert.True(false, string.Format(methodFailed + "AvailableWaitHandle failed, expected " + state + " actual " + !state));
                }
            }
            catch (Exception ex)
            {
                string methodFailed = "RunSemaphoreSlimTest7_AvailableWaitHandle(" + initial + "," + maximum + "," + action + "): FAILED.  ";
                Assert.True(false, string.Format(methodFailed + "AvailableWaitHandle failed, the code threw exception " + ex));
            }
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
        private static void RunSemaphoreSlimTest8_ConcWaitAndRelease(int initial, int maximum,
            int waitThreads, int releaseThreads, int succeededWait, int failedWait, int finalCount, int timeout)
        {
            string methodFailed =
                "RunSemaphoreSlimTest8_ConcWaitAndRelease("
                + initial + "," + maximum + "," + waitThreads + ", " + releaseThreads + ", "
                + succeededWait + ", " + failedWait + ", " + finalCount + ", " + timeout + "): FAILED.  ";
            try
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
                if (succeeded != succeededWait || failed != failedWait || semaphore.CurrentCount != finalCount)
                {
                    Debug.WriteLine(methodFailed + "ConcurrentWaitRelease failed. This might not be a bug, if the system was unstable during the test.");
                    Assert.True(false, string.Format(methodFailed + "Expected succeeded={0}, failed={1}, count={2}, but got {3}, {4}, {5}",
                        succeededWait, failedWait, finalCount, succeeded, failed, semaphore.CurrentCount));
                }
            }
            catch (Exception ex)
            {
                Assert.True(false, string.Format(methodFailed + "ConcurrentWaitRelease failed, the code threw exception " + ex));
            }
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
        private static void RunSemaphoreSlimTest8_ConcWaitAsyncAndRelease(int initial, int maximum,
            int waitThreads, int releaseThreads, int succeededWait, int failedWait, int finalCount, int timeout)
        {
            string methodFailed =
                "RunSemaphoreSlimTest8_ConcWaitAsyncAndRelease(" + initial + "," + maximum + "," + waitThreads + ", " + releaseThreads + "):  FAILED. ";
            try
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

                //check the number of succeeded and failed wait
                if (succeeded != succeededWait || failed != failedWait || semaphore.CurrentCount != finalCount)
                {
                    Assert.True(false, string.Format(methodFailed + "ConcurrentWaitAsyncAndRelease failed. This might not be a bug, if the system was unstable during the test."));
                }
            }
            catch (Exception ex)
            {
                Assert.True(false, string.Format(methodFailed + "ConcurrentWaitAsyncAndRelease failed, the code threw exception " + ex));
            }
        }

        private static void TestConcurrentWaitAndWaitAsync(int syncWaiters, int asyncWaiters)
        {
            string methodFailed = "ConcurrentWaitAndWaitAsync(" + syncWaiters + "," + asyncWaiters + "): FAILED.";

            int totalWaiters = syncWaiters + asyncWaiters;
            if (totalWaiters < 2)
            {
                Assert.True(false, string.Format(methodFailed + " invalid waiter arguments... need at least 2 waiters"));
            }

            var semaphore = new SemaphoreSlim(0);
            Task[] tasks = new Task[totalWaiters];

            const int ITERS = 10;
            int randSeed = (int)DateTime.Now.Ticks;
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
