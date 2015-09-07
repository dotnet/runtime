// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Tests
{
    /// <summary>
    /// SpinLock unit tests
    /// </summary>
    public class SpinLockTests
    {
        [Fact]
        public static void EnterExit()
        {
            var sl = new SpinLock();
            Assert.True(sl.IsThreadOwnerTrackingEnabled);

            for (int i = 0; i < 4; i++)
            {
                Assert.False(sl.IsHeld);
                Assert.False(sl.IsHeldByCurrentThread);

                bool lockTaken = false;
                if (i % 2 == 0)
                    sl.Enter(ref lockTaken);
                else
                    sl.TryEnter(ref lockTaken);
                Assert.True(lockTaken);
                Assert.True(sl.IsHeld);
                Assert.True(sl.IsHeldByCurrentThread);
                Task.Factory.StartNew(() =>
                {
                    Assert.True(sl.IsHeld);
                    Assert.False(sl.IsHeldByCurrentThread);
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).GetAwaiter().GetResult();
                sl.Exit();
            }
        }

        /// <summary>
        /// Run all SpinLock tests
        /// </summary>
        /// <returns>True if all tests passed, false if at least one test failed</returns>
        [Fact]
        [OuterLoop]
        public static void RunSpinLockTests()
        {
            for (int i = 0; i < 2; i++)
            {
                bool b;
                if (i == 0)
                {
                    Debug.WriteLine("NO THREAD IDS -- new SpinLock(true)");
                    b = true;
                }
                else
                {
                    Debug.WriteLine("WITH THREAD IDS -- new SpinLock(false)");
                    b = false;
                }

                RunSpinLockTest0_Enter(2, b);
                RunSpinLockTest0_Enter(128, b);
                RunSpinLockTest0_Enter(256, b);

                RunSpinLockTest1_TryEnter(2, b);
                RunSpinLockTest1_TryEnter(128, b);
                RunSpinLockTest1_TryEnter(256, b);

                RunSpinLockTest2_TryEnter(2, b);
                RunSpinLockTest2_TryEnter(128, b);
                RunSpinLockTest2_TryEnter(256, b);
            }
        }

        [Fact]
        public static void RunSpinLockTests_NegativeTests()
        {
            for (int i = 0; i < 2; i++)
            {
                bool b;
                if (i == 0)
                {
                    Debug.WriteLine("NO THREAD IDS -- new SpinLock(true)");
                    b = true;
                }
                else
                {
                    Debug.WriteLine("WITH THREAD IDS -- new SpinLock(false)");
                    b = false;
                }

                RunSpinLockTest3_TryEnter(b);
                RunSpinLockTest4_Exit(b);
            }
        }

        /// <summary>
        /// Test SpinLock.Enter by launching n threads that increment a variable inside a critical section
        /// the final count variable must be equal to n
        /// </summary>
        /// <param name="threadsCount">Number of threads that call enter/exit</param>
        /// <returns>True if succeeded, false otherwise</returns>
        private static void RunSpinLockTest0_Enter(int threadsCount, bool enableThreadIDs)
        {
            // threads array
            Task[] threads = new Task[threadsCount];
            //spinlock object
            SpinLock slock = new SpinLock(enableThreadIDs);
            // succeeded threads counter
            int succeeded = 0;
            // Semaphore used to make sure that there is no other threads in the critical section
            SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

            for (int i = 0; i < threadsCount; i++)
            {
                threads[i] = Task.Run(delegate ()
                {
                    bool lockTaken = false;
                    try
                    {
                        slock.Enter(ref lockTaken);
                        //use semaphore to make sure that no other thread inside the critical section
                        if (!semaphore.Wait(0))
                        {
                            // This mean that there is another thread in the critical section
                            return;
                        }
                        succeeded++;
                        if (slock.IsThreadOwnerTrackingEnabled && !slock.IsHeldByCurrentThread)
                        {
                            // lock is obtained successfully
                            succeeded--;
                        }
                    }
                    catch
                    {
                        // decrement the count in case of exception
                        succeeded--;
                    }
                    finally
                    {
                        semaphore.Release();
                        if (lockTaken)
                        {
                            slock.Exit();
                        }
                    }
                });
            }
            // wait all threads
            for (int i = 0; i < threadsCount; i++)
            {
                threads[i].Wait();
            }
            // count must be equal to the threads count
            if (succeeded != threadsCount)
            {
                Assert.True(false, string.Format("SpinLock.Enter() failed, actual count: " + succeeded + " expected: " + threadsCount));
            }
        }

        /// <summary>
        /// Test SpinLock.TryEnter() by launching n threads, each one calls TryEnter, the succeeded threads increment
        /// a counter variable and failed threads increment failed variable, count + failed must be equal to n
        /// </summary>
        /// <param name="threadsCount">Number of threads that call enter/exit</param>
        /// <returns>True if succeeded, false otherwise</returns>
        private static void RunSpinLockTest1_TryEnter(int threadsCount, bool enableThreadIDs)
        {
            for (int j = 0; j < 2; j++)
            {
                bool useMemoryBarrier = j == 0;
                Task[] threads = new Task[threadsCount];
                SpinLock slock = new SpinLock(enableThreadIDs);
                int succeeded = 0;
                int failed = 0;


                // Run threads
                for (int i = 0; i < threadsCount; i++)
                {
                    threads[i] = Task.Run(delegate ()
                    {
                        bool lockTaken = false;
                        slock.TryEnter(ref lockTaken);
                        if (lockTaken)
                        {
                            // Increment succeeded counter 
                            Interlocked.Increment(ref succeeded);
                            slock.Exit(useMemoryBarrier);
                        }
                        else
                        {
                            // Increment failed counter
                            Interlocked.Increment(ref failed);
                        }
                    });
                }
                // Wait all threads
                for (int i = 0; i < threadsCount; i++)
                {
                    threads[i].Wait();
                }
                // succeeded + failed must be equal to the threads count.
                if (succeeded + failed != threadsCount)
                {
                    Assert.True(false, string.Format("SpinLock.TryEnter() failed, actual count: " + (succeeded + failed) +
                        " expected :" + threadsCount));
                }
            }
        }

        /// <summary>
        /// Test SpinLock.TryEnter(Timespan) by generating random timespan milliseconds
        /// </summary>
        /// <param name="threadsCount">Number of threads that call enter/exit</param>
        /// <returns>True if succeeded, false otherwise</returns>
        private static void RunSpinLockTest2_TryEnter(int threadsCount, bool enableThreadIDs)
        {
            for (int j = 0; j < 2; j++)
            {
                bool useMemoryBarrier = j == 0;
                Task[] threads = new Task[threadsCount];
                SpinLock slock = new SpinLock(enableThreadIDs);
                int succeeded = 0;
                int failed = 0;

                // Run threads
                for (int i = 0; i < threadsCount; i++)
                {
                    threads[i] = new Task(delegate (object x)
                    {
                        // Generate random timespan
                        bool lockTaken = false;
                        TimeSpan time = TimeSpan.FromMilliseconds(20);
                        slock.TryEnter(time, ref lockTaken);
                        if (lockTaken)
                        {
                            // add some delay in the critical section
                            Task.WaitAll(Task.Delay(15));
                            Interlocked.Increment(ref succeeded);
                            slock.Exit(useMemoryBarrier);
                        }
                        else
                        {
                            // Failed to get the lock within the timeout
                            Interlocked.Increment(ref failed);
                        }
                    }, i);
                    threads[i].Start(TaskScheduler.Default);
                }
                // Wait all threads
                for (int i = 0; i < threadsCount; i++)
                {
                    threads[i].Wait();
                }
                // succeeded + failed must be equal to the threads count.
                if (succeeded + failed != threadsCount)
                {
                    Assert.True(false, string.Format("SpinLock.TryEnter() failed, actual count: " + (succeeded + failed) +
                        " expected :" + threadsCount));
                }
            }
        }

        /// <summary>
        /// Test TryEnter invalid cases
        /// </summary>
        /// <returns>True if succeeded, false otherwise</returns>
        private static void RunSpinLockTest3_TryEnter(bool enableThreadIDs)
        {
            Exception exception = null;
            SpinLock slock = new SpinLock(enableThreadIDs);
            bool lockTaken = false;

            #region Recursive lock
            if (enableThreadIDs) // only valid if thread IDs are on
            {
                // Test recursive locks
                slock.Enter(ref lockTaken);
                try
                {
                    if (lockTaken)
                    {
                        bool dummy = false;
                        // reacquire the lock
                        slock.Enter(ref dummy);
                    }
                }
                catch (Exception ex)
                {
                    // LockRecursionException must be thrown
                    exception = ex;
                }
                if (lockTaken)
                {
                    slock.Exit();
                    if (exception == null || exception.GetType() != typeof(LockRecursionException))
                    {
                        Assert.True(false, string.Format("SpinLock.TryEnter() failed, recursive locks without exception"));
                    }
                    if (slock.IsHeldByCurrentThread)
                    {
                        Assert.True(false, string.Format("SpinLock.TryEnter() failed, IsHeld is true after calling Exit"));
                    }
                }
                else
                {
                    Assert.True(false, string.Format("LockRecursionException was not thrown?"));
                }
            }
            #endregion

            #region timeout > int.max
            // Test invalid argument handling, too long timeout
            exception = null;
            try
            {
                lockTaken = false;
                slock.TryEnter(TimeSpan.MaxValue, ref lockTaken);
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            if (exception == null || exception.GetType() != typeof(ArgumentOutOfRangeException))
            {
                Assert.True(false, string.Format(@"SpinLock.TryEnter() failed, timeout.Totalmilliseconds > int.maxValue
                 without throwing ArgumentOutOfRangeException " + exception));
            }
            #endregion

            #region Timeout > int.max
            // Test invalid argument handling, timeout < -1
            exception = null;
            try
            {
                lockTaken = false;
                slock.TryEnter(-2, ref lockTaken);
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            if (exception == null || exception.GetType() != typeof(ArgumentOutOfRangeException))
            {
                Assert.True(false, string.Format(@"SpinLock.TryEnter() failed, timeout < -1
                 without throwing ArgumentOutOfRangeException"));
            }
            #endregion
        }

        /// <summary>
        /// Test Exit
        /// </summary>
        /// <returns>True if succeeded, false otherwise</returns>
        private static void RunSpinLockTest4_Exit(bool enableThreadIDs)
        {
            Exception exception = null;
            SpinLock slock = new SpinLock(enableThreadIDs);
            bool lockTaken = false;
            slock.Enter(ref lockTaken);
            slock.Exit();
            if (enableThreadIDs)
            {
                if (slock.IsHeldByCurrentThread)
                {
                    Assert.True(false, string.Format("SpinLock.Exit() failed, IsHeld is true after calling Exit"));
                }
            }
            else
            {
                if (slock.IsHeld)
                {
                    Assert.True(false, string.Format("SpinLock.Exit() failed, IsHeld is true after calling Exit"));
                }
            }

            for (int i = 0; i < 2; i++)
            {
                bool useBarrier = i == 0;
                // Calling Exit without owning the lock
                try
                {
                    slock.Exit(useBarrier);
                }
                catch (Exception ex)
                {
                    // SynchronizationLockException must be thrown
                    exception = ex;
                }
            }
            if (enableThreadIDs)
            {
                if (exception == null || exception.GetType() != typeof(SynchronizationLockException))
                {
                    Assert.True(false, string.Format(@"SpinLock.Exit() failed, calling Exit without owning the lock"));
                }
            }
        }

        [Fact]
        public static void RunSpinLockTestExceptions()
        {
            SpinLock slock = new SpinLock();
            bool isTaken = true;
            Assert.Throws<ArgumentException>(() => slock.Enter(ref isTaken));
            // Failure Case: Enter didn't throw AE when isTaken is true

            slock = new SpinLock(false);
            Assert.Throws<InvalidOperationException>(() => { bool iHeld = slock.IsHeldByCurrentThread; });
            // Failure Case: IsHeldByCurrentThread didn't throw IOE when the thread tracking is disabled
        }
    }
}
