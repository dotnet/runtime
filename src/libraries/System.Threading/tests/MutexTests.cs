// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Threading.Tests
{
    public class MutexTests : RemoteExecutorTestBase
    {
        private const int FailedWaitTimeout = 30000;

        [Fact]
        public void Ctor_ConstructWaitRelease()
        {
            using (Mutex m = new Mutex())
            {
                Assert.True(m.WaitOne(FailedWaitTimeout));
                m.ReleaseMutex();
            }

            using (Mutex m = new Mutex(false))
            {
                Assert.True(m.WaitOne(FailedWaitTimeout));
                m.ReleaseMutex();
            }

            using (Mutex m = new Mutex(true))
            {
                Assert.True(m.WaitOne(FailedWaitTimeout));
                m.ReleaseMutex();
                m.ReleaseMutex();
            }
        }

        [Fact]
        public void Ctor_InvalidName()
        {
            Assert.Throws<ArgumentException>(() => new Mutex(false, new string('a', 1000)));
        }

        [Fact]
        public void Ctor_ValidName()
        {
            string name = Guid.NewGuid().ToString("N");
            bool createdNew;
            using (Mutex m1 = new Mutex(false, name, out createdNew))
            {
                Assert.True(createdNew);
                using (Mutex m2 = new Mutex(false, name, out createdNew))
                {
                    Assert.False(createdNew);
                }
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [Fact]
        public void Ctor_NameUsedByOtherSynchronizationPrimitive_Windows()
        {
            string name = Guid.NewGuid().ToString("N");
            using (Semaphore s = new Semaphore(1, 1, name))
            {
                Assert.Throws<WaitHandleCannotBeOpenedException>(() => new Mutex(false, name));
            }
        }

        [Fact]
        public void OpenExisting()
        {
            string name = Guid.NewGuid().ToString("N");

            Mutex resultHandle;
            Assert.False(Mutex.TryOpenExisting(name, out resultHandle));

            using (Mutex m1 = new Mutex(false, name))
            {
                using (Mutex m2 = Mutex.OpenExisting(name))
                {
                    Assert.True(m1.WaitOne(FailedWaitTimeout));
                    Assert.False(Task.Factory.StartNew(() => m2.WaitOne(0), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).Result);
                    m1.ReleaseMutex();

                    Assert.True(m2.WaitOne(FailedWaitTimeout));
                    Assert.False(Task.Factory.StartNew(() => m1.WaitOne(0), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).Result);
                    m2.ReleaseMutex();
                }

                Assert.True(Mutex.TryOpenExisting(name, out resultHandle));
                Assert.NotNull(resultHandle);
                resultHandle.Dispose();
            }
        }

        [Fact]
        public void OpenExisting_InvalidNames()
        {
            Assert.Throws<ArgumentNullException>("name", () => Mutex.OpenExisting(null));
            Assert.Throws<ArgumentException>(() => Mutex.OpenExisting(string.Empty));
            Assert.Throws<ArgumentException>(() => Mutex.OpenExisting(new string('a', 10000)));
        }

        [Fact]
        public void OpenExisting_UnavailableName()
        {
            string name = Guid.NewGuid().ToString("N");
            Assert.Throws<WaitHandleCannotBeOpenedException>(() => Mutex.OpenExisting(name));
            Mutex ignored;
            Assert.False(Mutex.TryOpenExisting(name, out ignored));
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [Fact]
        public void OpenExisting_NameUsedByOtherSynchronizationPrimitive_Windows()
        {
            string name = Guid.NewGuid().ToString("N");
            using (Semaphore sema = new Semaphore(1, 1, name))
            {
                Assert.Throws<WaitHandleCannotBeOpenedException>(() => Mutex.OpenExisting(name));
                Mutex ignored;
                Assert.False(Mutex.TryOpenExisting(name, out ignored));
            }
        }

        public static IEnumerable<object[]> AbandonExisting_MemberData()
        {
            var nameGuidStr = Guid.NewGuid().ToString("N");
            for (int waitType = 0; waitType < 2; ++waitType) // 0 == WaitOne, 1 == WaitAny
            {
                yield return new object[] { null, waitType };
                foreach (var namePrefix in new[] { string.Empty, "Local\\", "Global\\" })
                {
                    yield return new object[] { namePrefix + nameGuidStr, waitType };
                }
            }
        }

        [Theory]
        [MemberData(nameof(AbandonExisting_MemberData))]
        public void AbandonExisting(string name, int waitType)
        {
            using (var m = new Mutex(false, name))
            {
                Task t = Task.Factory.StartNew(() =>
                {
                    Assert.True(m.WaitOne(FailedWaitTimeout));
                    // don't release the mutex; abandon it on this thread
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                t.Wait();

                switch (waitType)
                {
                    case 0: // WaitOne
                        Assert.Throws<AbandonedMutexException>(() => m.WaitOne(FailedWaitTimeout));
                        break;

                    case 1: // WaitAny
                        AbandonedMutexException ame = Assert.Throws<AbandonedMutexException>(() => WaitHandle.WaitAny(new[] { m }, FailedWaitTimeout));
                        Assert.Equal(0, ame.MutexIndex);
                        Assert.Equal(m, ame.Mutex);
                        break;
                }
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData("Local\\")]
        [InlineData("Global\\")]
        public void CrossProcess_NamedMutex_ProtectedFileAccessAtomic(string prefix)
        {
            string mutexName = prefix + Guid.NewGuid().ToString("N");
            string fileName = GetTestFilePath();

            Func<string, string, int> otherProcess = (m, f) =>
            {
                using (var mutex = Mutex.OpenExisting(m))
                {
                    mutex.WaitOne();
                    try { File.WriteAllText(f, "0"); }
                    finally { mutex.ReleaseMutex(); }

                    IncrementValueInFileNTimes(mutex, f, 10);
                }
                return SuccessExitCode;
            };

            using (var mutex = new Mutex(false, mutexName))
            using (var remote = RemoteInvoke(otherProcess, mutexName, fileName))
            {
                SpinWait.SpinUntil(() => File.Exists(fileName));

                IncrementValueInFileNTimes(mutex, fileName, 10);
            }

            Assert.Equal(20, int.Parse(File.ReadAllText(fileName)));
        }

        private static void IncrementValueInFileNTimes(Mutex mutex, string fileName, int n)
        {
            for (int i = 0; i < n; i++)
            {
                mutex.WaitOne();
                try
                {
                    int current = int.Parse(File.ReadAllText(fileName));
                    Thread.Sleep(10);
                    File.WriteAllText(fileName, (current + 1).ToString());
                }
                finally { mutex.ReleaseMutex(); }
            }
        }
    }
}
