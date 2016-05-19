// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        [PlatformSpecific(PlatformID.Windows)]
        [Fact]
        public void Ctor_InvalidName()
        {
            Assert.Throws<ArgumentException>(() => new Mutex(false, new string('a', 1000)));
        }

        [PlatformSpecific(PlatformID.Windows)]
        [Fact]
        public void Ctor_ValidName_Windows()
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

        [PlatformSpecific(PlatformID.Windows)]
        [Fact]
        public void Ctor_NameUsedByOtherSynchronizationPrimitive_Windows()
        {
            string name = Guid.NewGuid().ToString("N");
            using (Semaphore s = new Semaphore(1, 1, name))
            {
                Assert.Throws<WaitHandleCannotBeOpenedException>(() => new Mutex(false, name));
            }
        }

        [PlatformSpecific(PlatformID.Windows)]
        [Fact]
        public void OpenExisting_Windows()
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

        [PlatformSpecific(PlatformID.Windows)]
        [Fact]
        public void OpenExisting_InvalidNames_Windows()
        {
            Assert.Throws<ArgumentNullException>("name", () => Mutex.OpenExisting(null));
            Assert.Throws<ArgumentException>(() => Mutex.OpenExisting(string.Empty));
            Assert.Throws<ArgumentException>(() => Mutex.OpenExisting(new string('a', 10000)));
        }

        [PlatformSpecific(PlatformID.Windows)]
        [Fact]
        public void OpenExisting_UnavailableName_Windows()
        {
            string name = Guid.NewGuid().ToString("N");
            Assert.Throws<WaitHandleCannotBeOpenedException>(() => Mutex.OpenExisting(name));
            Mutex ignored;
            Assert.False(Mutex.TryOpenExisting(name, out ignored));
        }

        [PlatformSpecific(PlatformID.Windows)]
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

        [Fact]
        public void AbandonExisting()
        {
            using (Mutex m = new Mutex())
            {
                Task t = Task.Factory.StartNew(() =>
                {
                    Assert.True(m.WaitOne(FailedWaitTimeout));
                // don't release the mutex; abandon it on this thread
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                t.Wait();
                Assert.Throws<AbandonedMutexException>(() => m.WaitOne(FailedWaitTimeout));
            }

            using (Mutex m = new Mutex())
            {
                Task t = Task.Factory.StartNew(() =>
                {
                    Assert.True(m.WaitOne(FailedWaitTimeout));
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                t.Wait();
                AbandonedMutexException ame = Assert.Throws<AbandonedMutexException>(() => WaitHandle.WaitAny(new[] { m }, FailedWaitTimeout));
                Assert.Equal(0, ame.MutexIndex);
                Assert.Equal(m, ame.Mutex);
            }
        }

        [PlatformSpecific(PlatformID.Windows)] // names aren't supported on Unix
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
