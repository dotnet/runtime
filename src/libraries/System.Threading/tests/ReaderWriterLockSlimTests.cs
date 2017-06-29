// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Tests
{
    public static class ReaderWriterLockSlimTests
    {
        [Fact]
        public static void Ctor()
        {
            ReaderWriterLockSlim rwls;

            using (rwls = new ReaderWriterLockSlim())
            {
                Assert.Equal(LockRecursionPolicy.NoRecursion, rwls.RecursionPolicy);
            }

            using (rwls = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion))
            {
                Assert.Equal(LockRecursionPolicy.NoRecursion, rwls.RecursionPolicy);
            }

            using (rwls = new ReaderWriterLockSlim((LockRecursionPolicy)12345))
            {
                Assert.Equal(LockRecursionPolicy.NoRecursion, rwls.RecursionPolicy);
            }

            using (rwls = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion))
            {
                Assert.Equal(LockRecursionPolicy.SupportsRecursion, rwls.RecursionPolicy);
            }
        }

        [Fact]
        public static void Dispose()
        {
            ReaderWriterLockSlim rwls;

            rwls = new ReaderWriterLockSlim();
            rwls.Dispose();
            Assert.Throws<ObjectDisposedException>(() => rwls.TryEnterReadLock(0));
            Assert.Throws<ObjectDisposedException>(() => rwls.TryEnterUpgradeableReadLock(0));
            Assert.Throws<ObjectDisposedException>(() => rwls.TryEnterWriteLock(0));
            rwls.Dispose();

            for (int i = 0; i < 3; i++)
            {
                rwls = new ReaderWriterLockSlim();
                switch (i)
                {
                    case 0: rwls.EnterReadLock(); break;
                    case 1: rwls.EnterUpgradeableReadLock(); break;
                    case 2: rwls.EnterWriteLock(); break;
                }
                Assert.Throws<SynchronizationLockException>(() => rwls.Dispose());
            }
        }

        [Fact]
        public static void EnterExit()
        {
            using (ReaderWriterLockSlim rwls = new ReaderWriterLockSlim())
            {
                Assert.False(rwls.IsReadLockHeld);
                rwls.EnterReadLock();
                Assert.True(rwls.IsReadLockHeld);
                rwls.ExitReadLock();
                Assert.False(rwls.IsReadLockHeld);

                Assert.False(rwls.IsUpgradeableReadLockHeld);
                rwls.EnterUpgradeableReadLock();
                Assert.True(rwls.IsUpgradeableReadLockHeld);
                rwls.ExitUpgradeableReadLock();
                Assert.False(rwls.IsUpgradeableReadLockHeld);

                Assert.False(rwls.IsWriteLockHeld);
                rwls.EnterWriteLock();
                Assert.True(rwls.IsWriteLockHeld);
                rwls.ExitWriteLock();
                Assert.False(rwls.IsWriteLockHeld);

                Assert.False(rwls.IsUpgradeableReadLockHeld);
                rwls.EnterUpgradeableReadLock();
                Assert.False(rwls.IsWriteLockHeld);
                Assert.True(rwls.IsUpgradeableReadLockHeld);
                rwls.EnterWriteLock();
                Assert.True(rwls.IsWriteLockHeld);
                rwls.ExitWriteLock();
                Assert.False(rwls.IsWriteLockHeld);
                Assert.True(rwls.IsUpgradeableReadLockHeld);
                rwls.ExitUpgradeableReadLock();
                Assert.False(rwls.IsUpgradeableReadLockHeld);

                Assert.True(rwls.TryEnterReadLock(0));
                rwls.ExitReadLock();

                Assert.True(rwls.TryEnterReadLock(Timeout.InfiniteTimeSpan));
                rwls.ExitReadLock();

                Assert.True(rwls.TryEnterUpgradeableReadLock(0));
                rwls.ExitUpgradeableReadLock();

                Assert.True(rwls.TryEnterUpgradeableReadLock(Timeout.InfiniteTimeSpan));
                rwls.ExitUpgradeableReadLock();

                Assert.True(rwls.TryEnterWriteLock(0));
                rwls.ExitWriteLock();

                Assert.True(rwls.TryEnterWriteLock(Timeout.InfiniteTimeSpan));
                rwls.ExitWriteLock();
            }
        }

        [Fact]
        public static void DeadlockAvoidance()
        {
            using (ReaderWriterLockSlim rwls = new ReaderWriterLockSlim())
            {
                rwls.EnterReadLock();
                Assert.Throws<LockRecursionException>(() => rwls.EnterReadLock());
                Assert.Throws<LockRecursionException>(() => rwls.EnterUpgradeableReadLock());
                Assert.Throws<LockRecursionException>(() => rwls.EnterWriteLock());
                rwls.ExitReadLock();

                rwls.EnterUpgradeableReadLock();
                rwls.EnterReadLock();
                Assert.Throws<LockRecursionException>(() => rwls.EnterReadLock());
                rwls.ExitReadLock();
                Assert.Throws<LockRecursionException>(() => rwls.EnterUpgradeableReadLock());
                rwls.EnterWriteLock();
                Assert.Throws<LockRecursionException>(() => rwls.EnterWriteLock());
                rwls.ExitWriteLock();
                rwls.ExitUpgradeableReadLock();

                rwls.EnterWriteLock();
                Assert.Throws<LockRecursionException>(() => rwls.EnterReadLock());
                Assert.Throws<LockRecursionException>(() => rwls.EnterUpgradeableReadLock());
                Assert.Throws<LockRecursionException>(() => rwls.EnterWriteLock());
                rwls.ExitWriteLock();
            }

            using (ReaderWriterLockSlim rwls = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion))
            {
                rwls.EnterReadLock();
                Assert.Throws<LockRecursionException>(() => rwls.EnterWriteLock());
                rwls.EnterReadLock();
                Assert.Throws<LockRecursionException>(() => rwls.EnterUpgradeableReadLock());
                rwls.ExitReadLock();
                rwls.ExitReadLock();

                rwls.EnterUpgradeableReadLock();
                rwls.EnterReadLock();
                rwls.EnterUpgradeableReadLock();
                rwls.ExitUpgradeableReadLock();
                rwls.EnterReadLock();
                rwls.ExitReadLock();
                rwls.ExitReadLock();
                rwls.EnterWriteLock();
                rwls.EnterWriteLock();
                rwls.ExitWriteLock();
                rwls.ExitWriteLock();
                rwls.ExitUpgradeableReadLock();

                rwls.EnterWriteLock();
                rwls.EnterReadLock();
                rwls.ExitReadLock();
                rwls.EnterUpgradeableReadLock();
                rwls.ExitUpgradeableReadLock();
                rwls.EnterWriteLock();
                rwls.ExitWriteLock();
                rwls.ExitWriteLock();
            }
        }

        [Theory]
        [InlineData(LockRecursionPolicy.NoRecursion)]
        [InlineData(LockRecursionPolicy.SupportsRecursion)]
        public static void InvalidExits(LockRecursionPolicy policy)
        {
            using (ReaderWriterLockSlim rwls = new ReaderWriterLockSlim(policy))
            {
                Assert.Throws<SynchronizationLockException>(() => rwls.ExitReadLock());
                Assert.Throws<SynchronizationLockException>(() => rwls.ExitUpgradeableReadLock());
                Assert.Throws<SynchronizationLockException>(() => rwls.ExitWriteLock());

                rwls.EnterReadLock();
                Assert.Throws<SynchronizationLockException>(() => rwls.ExitUpgradeableReadLock());
                Assert.Throws<SynchronizationLockException>(() => rwls.ExitWriteLock());
                rwls.ExitReadLock();

                rwls.EnterUpgradeableReadLock();
                Assert.Throws<SynchronizationLockException>(() => rwls.ExitReadLock());
                Assert.Throws<SynchronizationLockException>(() => rwls.ExitWriteLock());
                rwls.ExitUpgradeableReadLock();

                rwls.EnterWriteLock();
                Assert.Throws<SynchronizationLockException>(() => rwls.ExitReadLock());
                Assert.Throws<SynchronizationLockException>(() => rwls.ExitUpgradeableReadLock());
                rwls.ExitWriteLock();

                using (Barrier barrier = new Barrier(2))
                {
                    Task t = Task.Factory.StartNew(() =>
                    {
                        rwls.EnterWriteLock();
                        barrier.SignalAndWait();
                        barrier.SignalAndWait();
                        rwls.ExitWriteLock();
                    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

                    barrier.SignalAndWait();
                    Assert.Throws<SynchronizationLockException>(() => rwls.ExitWriteLock());
                    barrier.SignalAndWait();

                    t.GetAwaiter().GetResult();
                }
            }
        }

        [Fact]
        public static void InvalidTimeouts()
        {
            using (ReaderWriterLockSlim rwls = new ReaderWriterLockSlim())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => rwls.TryEnterReadLock(-2));
                Assert.Throws<ArgumentOutOfRangeException>(() => rwls.TryEnterUpgradeableReadLock(-3));
                Assert.Throws<ArgumentOutOfRangeException>(() => rwls.TryEnterWriteLock(-4));

                Assert.Throws<ArgumentOutOfRangeException>(() => rwls.TryEnterReadLock(TimeSpan.MaxValue));
                Assert.Throws<ArgumentOutOfRangeException>(() => rwls.TryEnterUpgradeableReadLock(TimeSpan.MinValue));
                Assert.Throws<ArgumentOutOfRangeException>(() => rwls.TryEnterWriteLock(TimeSpan.FromMilliseconds(-2)));
            }
        }

        [Fact]
        public static void WritersAreMutuallyExclusiveFromReaders()
        {
            using (Barrier barrier = new Barrier(2))
            using (ReaderWriterLockSlim rwls = new ReaderWriterLockSlim())
            {
                Task.WaitAll(
                    Task.Run(() =>
                    {
                        rwls.EnterWriteLock();
                        barrier.SignalAndWait();
                        Assert.True(rwls.IsWriteLockHeld);
                        barrier.SignalAndWait();
                        rwls.ExitWriteLock();
                    }),
                    Task.Run(() =>
                    {
                        barrier.SignalAndWait();
                        Assert.False(rwls.TryEnterReadLock(0));
                        Assert.False(rwls.IsReadLockHeld);
                        barrier.SignalAndWait();
                    }));
            }
        }

        [Fact]
        public static void WritersAreMutuallyExclusiveFromWriters()
        {
            using (Barrier barrier = new Barrier(2))
            using (ReaderWriterLockSlim rwls = new ReaderWriterLockSlim())
            {
                Task.WaitAll(
                    Task.Run(() =>
                    {
                        rwls.EnterWriteLock();
                        barrier.SignalAndWait();
                        Assert.True(rwls.IsWriteLockHeld);
                        barrier.SignalAndWait();
                        rwls.ExitWriteLock();
                    }),
                    Task.Run(() =>
                    {
                        barrier.SignalAndWait();
                        Assert.False(rwls.TryEnterWriteLock(0));
                        Assert.False(rwls.IsReadLockHeld);
                        barrier.SignalAndWait();
                    }));
            }
        }

        [Fact]
        public static void ReadersMayBeConcurrent()
        {
            using (Barrier barrier = new Barrier(2))
            using (ReaderWriterLockSlim rwls = new ReaderWriterLockSlim())
            {
                Assert.Equal(0, rwls.CurrentReadCount);
                Task.WaitAll(
                    Task.Run(() =>
                    {
                        rwls.EnterReadLock();
                        barrier.SignalAndWait(); // 1
                        Assert.True(rwls.IsReadLockHeld);
                        barrier.SignalAndWait(); // 2
                        Assert.Equal(2, rwls.CurrentReadCount);
                        barrier.SignalAndWait(); // 3
                        barrier.SignalAndWait(); // 4
                        rwls.ExitReadLock();
                    }),
                    Task.Run(() =>
                    {
                        barrier.SignalAndWait(); // 1
                        rwls.EnterReadLock();
                        barrier.SignalAndWait(); // 2
                        Assert.True(rwls.IsReadLockHeld);
                        Assert.Equal(0, rwls.WaitingReadCount);
                        barrier.SignalAndWait(); // 3
                        rwls.ExitReadLock();
                        barrier.SignalAndWait(); // 4
                    }));
                Assert.Equal(0, rwls.CurrentReadCount);
            }
        }

        [Fact]
        public static void WriterToWriterChain()
        {
            using (AutoResetEvent are = new AutoResetEvent(false))
            using (ReaderWriterLockSlim rwls = new ReaderWriterLockSlim())
            {
                rwls.EnterWriteLock();
                Task t = Task.Factory.StartNew(() =>
                {
                    Assert.False(rwls.TryEnterWriteLock(10));
                    Task.Run(() => are.Set()); // ideally this won't fire until we've called EnterWriteLock, but it's a benign race in that the test will succeed either way
                    rwls.EnterWriteLock();
                    rwls.ExitWriteLock();
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                are.WaitOne();
                rwls.ExitWriteLock();
                t.GetAwaiter().GetResult();
            }
        }

        [Fact]
        public static void WriterToReaderChain()
        {
            using (AutoResetEvent are = new AutoResetEvent(false))
            using (ReaderWriterLockSlim rwls = new ReaderWriterLockSlim())
            {
                rwls.EnterWriteLock();
                Task t = Task.Factory.StartNew(() =>
                {
                    Assert.False(rwls.TryEnterReadLock(TimeSpan.FromMilliseconds(10)));
                    Task.Run(() => are.Set()); // ideally this won't fire until we've called EnterReadLock, but it's a benign race in that the test will succeed either way
                    rwls.EnterReadLock();
                    rwls.ExitReadLock();
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                are.WaitOne();
                rwls.ExitWriteLock();
                t.GetAwaiter().GetResult();
            }
        }

        [Fact]
        public static void WriterToUpgradeableReaderChain()
        {
            using (AutoResetEvent are = new AutoResetEvent(false))
            using (ReaderWriterLockSlim rwls = new ReaderWriterLockSlim())
            {
                rwls.EnterWriteLock();
                Task t = Task.Factory.StartNew(() =>
                {
                    Assert.False(rwls.TryEnterUpgradeableReadLock(TimeSpan.FromMilliseconds(10)));
                    Task.Run(() => are.Set()); // ideally this won't fire until we've called EnterReadLock, but it's a benign race in that the test will succeed either way
                    rwls.EnterUpgradeableReadLock();
                    rwls.ExitUpgradeableReadLock();
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                are.WaitOne();
                rwls.ExitWriteLock();
                t.GetAwaiter().GetResult();
            }
        }

        [Fact]
        [OuterLoop]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Hangs in desktop, issue dotnet/corefx#3364 is not fixed there")]
        public static void ReleaseReadersWhenWaitingWriterTimesOut()
        {
            using (var rwls = new ReaderWriterLockSlim())
            {
                // Enter the read lock
                rwls.EnterReadLock();
                // Typical order of execution: 0

                Thread writeWaiterThread;
                using (var beforeTryEnterWriteLock = new ManualResetEvent(false))
                {
                    writeWaiterThread =
                        new Thread(() =>
                        {
                            // Typical order of execution: 1

                            // Add a writer to the wait list for enough time to allow successive readers to enter the wait list while this
                            // writer is waiting
                            beforeTryEnterWriteLock.Set();
                            if (rwls.TryEnterWriteLock(1000))
                            {
                                // The typical order of execution is not guaranteed, as sleep times are not guaranteed. For
                                // instance, before this write lock is added to the wait list, the two new read locks may be
                                // acquired. In that case, the test may complete before or while the write lock is taken.
                                rwls.ExitWriteLock();
                            }

                            // Typical order of execution: 4
                        });
                    writeWaiterThread.IsBackground = true;
                    writeWaiterThread.Start();
                    beforeTryEnterWriteLock.WaitOne();
                }
                Thread.Sleep(500); // wait for TryEnterWriteLock to enter the wait list

                // A writer should now be waiting, add readers to the wait list. Since a read lock is still acquired, the writer
                // should time out waiting, then these readers should enter and exit the lock.
                ThreadStart EnterAndExitReadLock = () =>
                {
                    // Typical order of execution: 2, 3
                    rwls.EnterReadLock();
                    // Typical order of execution: 5, 6
                    rwls.ExitReadLock();
                };
                var readerThreads =
                    new Thread[]
                    {
                        new Thread(EnterAndExitReadLock),
                        new Thread(EnterAndExitReadLock)
                    };
                foreach (var readerThread in readerThreads)
                {
                    readerThread.IsBackground = true;
                    readerThread.Start();
                }
                foreach (var readerThread in readerThreads)
                {
                    readerThread.Join();
                }

                rwls.ExitReadLock();
                // Typical order of execution: 7

                writeWaiterThread.Join();
            }
        }

        [Fact]
        [OuterLoop]
        public static void DontReleaseWaitingReadersWhenThereAreWaitingWriters()
        {
            using(var rwls = new ReaderWriterLockSlim())
            {
                rwls.EnterUpgradeableReadLock();
                rwls.EnterWriteLock();
                // Typical order of execution: 0

                // Add a waiting writer
                var threads = new Thread[2];
                using(var beforeEnterWriteLock = new ManualResetEvent(false))
                {
                    var thread =
                        new Thread(() =>
                        {
                            beforeEnterWriteLock.Set();
                            rwls.EnterWriteLock();
                            // Typical order of execution: 3
                            rwls.ExitWriteLock();
                        });
                    thread.IsBackground = true;
                    thread.Start();
                    threads[0] = thread;
                    beforeEnterWriteLock.WaitOne();
                }

                // Add a waiting reader
                using(var beforeEnterReadLock = new ManualResetEvent(false))
                {
                    var thread =
                        new Thread(() =>
                        {
                            beforeEnterReadLock.Set();
                            rwls.EnterReadLock();
                            // Typical order of execution: 4
                            rwls.ExitReadLock();
                        });
                    thread.IsBackground = true;
                    thread.Start();
                    threads[1] = thread;
                    beforeEnterReadLock.WaitOne();
                }

                // Wait for the background threads to block waiting for their locks
                Thread.Sleep(1000);

                // Typical order of execution: 1
                rwls.ExitWriteLock();
                // At this point there is still one reader and one waiting writer, so the reader-writer lock should not try to
                // release any of the threads waiting for a lock

                // Typical order of execution: 2
                rwls.ExitUpgradeableReadLock();
                // At this point, the waiting writer should be released, and the waiting reader should not

                foreach(var thread in threads)
                    thread.Join();
                // Typical order of execution: 5
            }
        }
    }
}
