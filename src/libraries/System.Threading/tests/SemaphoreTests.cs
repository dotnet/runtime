// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Tests
{
    public class SemaphoreTests : RemoteExecutorTestBase
    {
        private const int FailedWaitTimeout = 30000;

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 1)]
        [InlineData(1, 2)]
        [InlineData(0, int.MaxValue)]
        [InlineData(int.MaxValue, int.MaxValue)]
        public void Ctor_InitialAndMax(int initialCount, int maximumCount)
        {
            new Semaphore(initialCount, maximumCount).Dispose();
            new Semaphore(initialCount, maximumCount, null).Dispose();
        }

        [PlatformSpecific(PlatformID.Windows)]
        [Fact]
        public void Ctor_ValidName_Windows()
        {
            string name = Guid.NewGuid().ToString("N");

            new Semaphore(0, 1, name).Dispose();

            bool createdNew;
            new Semaphore(0, 1, name, out createdNew).Dispose();
            Assert.True(createdNew);
        }

        [PlatformSpecific(PlatformID.AnyUnix)]
        [Fact]
        public void Ctor_NamesArentSupported_Unix()
        {
            string name = Guid.NewGuid().ToString("N");

            Assert.Throws<PlatformNotSupportedException>(() => new Semaphore(0, 1, name));

            Assert.Throws<PlatformNotSupportedException>(() =>
            {
                bool createdNew;
                new Semaphore(0, 1, name, out createdNew).Dispose();
            });
        }

        [Fact]
        public void Ctor_InvalidArguments()
        {
            bool createdNew;

            Assert.Throws<ArgumentOutOfRangeException>("initialCount", () => new Semaphore(-1, 1));
            Assert.Throws<ArgumentOutOfRangeException>("initialCount", () => new Semaphore(-2, 1));
            Assert.Throws<ArgumentOutOfRangeException>("maximumCount", () => new Semaphore(0, 0));
            Assert.Throws<ArgumentException>(() => new Semaphore(2, 1));

            Assert.Throws<ArgumentOutOfRangeException>("initialCount", () => new Semaphore(-1, 1, null));
            Assert.Throws<ArgumentOutOfRangeException>("initialCount", () => new Semaphore(-2, 1, null));
            Assert.Throws<ArgumentOutOfRangeException>("maximumCount", () => new Semaphore(0, 0, null));
            Assert.Throws<ArgumentException>(() => new Semaphore(2, 1, null));

            Assert.Throws<ArgumentOutOfRangeException>("initialCount", () => new Semaphore(-1, 1, "CtorSemaphoreTest", out createdNew));
            Assert.Throws<ArgumentOutOfRangeException>("initialCount", () => new Semaphore(-2, 1, "CtorSemaphoreTest", out createdNew));
            Assert.Throws<ArgumentOutOfRangeException>("maximumCount", () => new Semaphore(0, 0, "CtorSemaphoreTest", out createdNew));
            Assert.Throws<ArgumentException>(() => new Semaphore(2, 1, "CtorSemaphoreTest", out createdNew));
        }

        [PlatformSpecific(PlatformID.Windows)]
        [Fact]
        public void Ctor_InvalidNames()
        {
            Assert.Throws<ArgumentException>(() => new Semaphore(0, 1, new string('a', 10000)));
            bool createdNew;
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
                            Assert.True(s.WaitOne(FailedWaitTimeout));
                        Assert.False(s.WaitOne(0));
                    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default),
                    Task.Factory.StartNew(() =>
                    {
                        for (int i = 0; i < NumItems; i++)
                            s.Release();
                    }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default));
        }
        }

        [PlatformSpecific(PlatformID.Windows)] // named semaphores aren't supported on Unix
        [Fact]
        public void NamedProducerConsumer()
        {
            string name = Guid.NewGuid().ToString("N");
            const int NumItems = 5;
            var b = new Barrier(2);
            Task.WaitAll(
                Task.Factory.StartNew(() =>
                {
                    using (var s = new Semaphore(0, int.MaxValue, name))
                    {
                        Assert.True(b.SignalAndWait(FailedWaitTimeout));
                        for (int i = 0; i < NumItems; i++)
                            Assert.True(s.WaitOne(FailedWaitTimeout));
                        Assert.False(s.WaitOne(0));
                    }
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default),
                Task.Factory.StartNew(() =>
                {
                    using (var s = new Semaphore(0, int.MaxValue, name))
                    {
                        Assert.True(b.SignalAndWait(FailedWaitTimeout));
                        for (int i = 0; i < NumItems; i++)
                            s.Release();
                    }
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default));
        }

        [PlatformSpecific(PlatformID.AnyUnix)]
        [Fact]
        public void OpenExisting_NotSupported_Unix()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Semaphore.OpenExisting(null));
            Assert.Throws<PlatformNotSupportedException>(() => Semaphore.OpenExisting(string.Empty));
            Assert.Throws<PlatformNotSupportedException>(() => Semaphore.OpenExisting("anything"));
            Semaphore semaphore;
            Assert.Throws<PlatformNotSupportedException>(() => Semaphore.TryOpenExisting("anything", out semaphore));
        }

        [PlatformSpecific(PlatformID.Windows)]
        [Fact]
        public void OpenExisting_InvalidNames_Windows()
        {
            Assert.Throws<ArgumentNullException>("name", () => Semaphore.OpenExisting(null));
            Assert.Throws<ArgumentException>(() => Semaphore.OpenExisting(string.Empty));
            Assert.Throws<ArgumentException>(() => Semaphore.OpenExisting(new string('a', 10000)));
        }

        [PlatformSpecific(PlatformID.Windows)] // named semaphores aren't supported on Unix
        [Fact]
        public void OpenExisting_UnavailableName_Windows()
        {
            string name = Guid.NewGuid().ToString("N");
            Assert.Throws<WaitHandleCannotBeOpenedException>(() => Semaphore.OpenExisting(name));
            Semaphore ignored;
            Assert.False(Semaphore.TryOpenExisting(name, out ignored));
        }

        [PlatformSpecific(PlatformID.Windows)] // named semaphores aren't supported on Unix
        [Fact]
        public void OpenExisting_NameUsedByOtherSynchronizationPrimitive_Windows()
        {
            string name = Guid.NewGuid().ToString("N");
            using (Mutex mtx = new Mutex(true, name))
            {
                Assert.Throws<WaitHandleCannotBeOpenedException>(() => Semaphore.OpenExisting(name));
                Semaphore ignored;
                Assert.False(Semaphore.TryOpenExisting(name, out ignored));
            }
        }

        [PlatformSpecific(PlatformID.Windows)] // named semaphores aren't supported on Unix
        [Fact]
        public void OpenExisting_SameAsOriginal_Windows()
        {
            string name = Guid.NewGuid().ToString("N");
            bool createdNew;
            using (Semaphore s1 = new Semaphore(0, Int32.MaxValue, name, out createdNew))
            {
                Assert.True(createdNew);

                using (Semaphore s2 = Semaphore.OpenExisting(name))
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
                Assert.True(Semaphore.TryOpenExisting(name, out s3));
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

        [PlatformSpecific(PlatformID.Windows)] // names aren't supported on Unix
        [Fact]
        public void PingPong()
        {
            // Create names for the two semaphores
            string outboundName = Guid.NewGuid().ToString("N");
            string inboundName = Guid.NewGuid().ToString("N");

            // Create the two semaphores and the other process with which to synchronize
            using (var inbound = new Semaphore(1, 1, inboundName))
            using (var outbound = new Semaphore(0, 1, outboundName))
            using (var remote = RemoteInvoke(PingPong_OtherProcess, outboundName, inboundName))
            {
                // Repeatedly wait for count in one semaphore and then release count into the other
                for (int i = 0; i < 10; i++)
                {
                    Assert.True(inbound.WaitOne(FailWaitTimeoutMilliseconds));
                    outbound.Release();
                }
            }
        }

        private static int PingPong_OtherProcess(string inboundName, string outboundName)
        {
            // Open the two semaphores
            using (var inbound = Semaphore.OpenExisting(inboundName))
            using (var outbound = Semaphore.OpenExisting(outboundName))
            {
                // Repeatedly wait for count in one sempahore and then release count into the other
                for (int i = 0; i < 10; i++)
                {
                    Assert.True(inbound.WaitOne(FailWaitTimeoutMilliseconds));
                    outbound.Release();
                }
            }

            return SuccessExitCode;
        }
    }
}
