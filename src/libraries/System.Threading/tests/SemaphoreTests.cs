// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Test
{
    public class SemaphoreTests
    {
        [Fact]
        public void Ctor()
        {
            new Semaphore(0, 1).Dispose();
            new Semaphore(1, 1).Dispose();
            new Semaphore(1, 2).Dispose();
            new Semaphore(Int32.MaxValue, Int32.MaxValue).Dispose();
            new Semaphore(1, 1, null).Dispose();

            new Semaphore(0, 1, "SemaphoreTestsName").Dispose();

            bool createdNew;
            new Semaphore(0, 1, "SemaphoreTestsName", out createdNew).Dispose();
            Assert.True(createdNew);

            Assert.Throws<ArgumentOutOfRangeException>("initialCount", () => new Semaphore(-1, 1));
            Assert.Throws<ArgumentOutOfRangeException>("initialCount", () => new Semaphore(-2, 1));
            Assert.Throws<ArgumentOutOfRangeException>("maximumCount", () => new Semaphore(0, 0));
            Assert.Throws<ArgumentException>(() => new Semaphore(2, 1));

            Assert.Throws<ArgumentOutOfRangeException>("initialCount", () => new Semaphore(-1, 1, null));
            Assert.Throws<ArgumentOutOfRangeException>("initialCount", () => new Semaphore(-2, 1, null));
            Assert.Throws<ArgumentOutOfRangeException>("maximumCount", () => new Semaphore(0, 0, null));
            Assert.Throws<ArgumentException>(() => new Semaphore(2, 1, null));
            Assert.Throws<ArgumentException>(() => new Semaphore(0, 1, new string('a', 10000)));

            Assert.Throws<ArgumentOutOfRangeException>("initialCount", () => new Semaphore(-1, 1, "CtorSemaphoreTest", out createdNew));
            Assert.Throws<ArgumentOutOfRangeException>("initialCount", () => new Semaphore(-2, 1, "CtorSemaphoreTest", out createdNew));
            Assert.Throws<ArgumentOutOfRangeException>("maximumCount", () => new Semaphore(0, 0, "CtorSemaphoreTest", out createdNew));
            Assert.Throws<ArgumentException>(() => new Semaphore(2, 1, "CtorSemaphoreTest", out createdNew));
            Assert.Throws<ArgumentException>(() => new Semaphore(0, 1, new string('a', 10000)));
            Assert.Throws<ArgumentException>(() => new Semaphore(0, 1, new string('a', 10000), out createdNew));
        }

        [Fact]
        public void CanWaitWithoutBlockingUntilNoCount()
        {
            const int InitialCount = 5;
            using (Semaphore s = new Semaphore(InitialCount, InitialCount))
            {
                for (int i = 0; i < InitialCount; i++)
                    Assert.True(s.WaitOne(0));
                Assert.False(s.WaitOne(0));
            }
        }

        [Fact]
        public void CanWaitWithoutBlockingForReleasedCount()
        {
            using (Semaphore s = new Semaphore(0, Int32.MaxValue))
            {
                for (int counts = 1; counts < 5; counts++)
                {
                    Assert.False(s.WaitOne(0));

                    if (counts % 2 == 0)
                    {
                        for (int i = 0; i < counts; i++)
                            s.Release();
                    }
                    else
                    {
                        s.Release(counts);
                    }

                    for (int i = 0; i < counts; i++)
                    {
                        Assert.True(s.WaitOne(0));
                    }

                    Assert.False(s.WaitOne(0));
                }
            }
        }

        [Fact]
        public void Release()
        {
            using (Semaphore s = new Semaphore(1, 1))
            {
                Assert.Throws<SemaphoreFullException>(() => s.Release());
            }

            using (Semaphore s = new Semaphore(0, 10))
            {
                Assert.Throws<SemaphoreFullException>(() => s.Release(11));
                Assert.Throws<ArgumentOutOfRangeException>("releaseCount", () => s.Release(-1));
            }

            using (Semaphore s = new Semaphore(0, 10))
            {
                for (int i = 0; i < 10; i++)
                {
                    Assert.Equal(i, s.Release());
                }
            }
        }

        [Fact]
        public void AnonymousProducerConsumer()
        {
            using (Semaphore s = new Semaphore(0, Int32.MaxValue))
            {
                const int NumItems = 5;
                Task.WaitAll(
                    Task.Factory.StartNew(() =>
                    {
                        for (int i = 0; i < NumItems; i++)
                            s.WaitOne();
                        Assert.False(s.WaitOne(0));
                    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default),
                    Task.Factory.StartNew(() =>
                    {
                        for (int i = 0; i < NumItems; i++)
                            s.Release();
                    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default));
        }
        }

        [Fact]
        public void NamedProducerConsumer()
        {
            const string Name = "NamedProducerConsumerSemaphoreTest";
            const int NumItems = 5;
            Task.WaitAll(
                Task.Factory.StartNew(() =>
                {
                    using (Semaphore s = new Semaphore(0, Int32.MaxValue, Name))
                    {
                        for (int i = 0; i < NumItems; i++)
                            s.WaitOne();
                        Assert.False(s.WaitOne(0));
                    }
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default),
                Task.Factory.StartNew(() =>
                {
                    using (Semaphore s = new Semaphore(0, Int32.MaxValue, Name))
                    {
                        for (int i = 0; i < NumItems; i++)
                            s.Release();
                    }
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default));
        }

        [Fact]
        public void OpenExisting()
        {
            const string Name = "OpenExistingSemaphoreTestName";
            Semaphore ignored;

            Assert.Throws<ArgumentNullException>("name", () => Semaphore.OpenExisting(null));
            Assert.Throws<ArgumentException>(() => Semaphore.OpenExisting(string.Empty));
            Assert.Throws<WaitHandleCannotBeOpenedException>(() => Semaphore.OpenExisting(Name));
            Assert.Throws<ArgumentException>(() => Semaphore.OpenExisting(new string('a', 10000)));
            Assert.False(Semaphore.TryOpenExisting(Name, out ignored));

            using (Mutex mtx = new Mutex(true, Name))
            {
                Assert.Throws<WaitHandleCannotBeOpenedException>(() => Semaphore.OpenExisting(Name));
                Assert.False(Semaphore.TryOpenExisting(Name, out ignored));
            }

            bool createdNew;
            using (Semaphore s1 = new Semaphore(0, Int32.MaxValue, Name, out createdNew))
            {
                Assert.True(createdNew);

                using (Semaphore s2 = Semaphore.OpenExisting(Name))
                {
                    Assert.False(s1.WaitOne(0));
                    Assert.False(s2.WaitOne(0));
                    s1.Release();
                    Assert.True(s2.WaitOne(0));
                    Assert.False(s2.WaitOne(0));
                    s2.Release();
                    Assert.True(s1.WaitOne(0));
                    Assert.False(s1.WaitOne(0));
                }

                Semaphore s3;
                Assert.True(Semaphore.TryOpenExisting(Name, out s3));
                using (s3)
                {
                    Assert.False(s1.WaitOne(0));
                    Assert.False(s3.WaitOne(0));
                    s1.Release();
                    Assert.True(s3.WaitOne(0));
                    Assert.False(s3.WaitOne(0));
                    s3.Release();
                    Assert.True(s1.WaitOne(0));
                    Assert.False(s1.WaitOne(0));
                }
            }
        }
    }
}