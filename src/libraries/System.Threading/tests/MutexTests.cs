// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Threading.Tests
{
    public class MutexTests : FileCleanupTestBase
    {
        [Fact]
        public void Ctor_ConstructWaitRelease()
        {
            using (Mutex m = new Mutex())
            {
                m.CheckedWait();
                m.ReleaseMutex();
            }

            using (Mutex m = new Mutex(false))
            {
                m.CheckedWait();
                m.ReleaseMutex();
            }

            using (Mutex m = new Mutex(true))
            {
                m.CheckedWait();
                m.ReleaseMutex();
                m.ReleaseMutex();
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void Ctor_InvalidNames_Unix()
        {
            AssertExtensions.Throws<ArgumentException>("name", null, () => new Mutex(false, new string('a', 1000), out bool createdNew));
        }

        [Theory]
        [MemberData(nameof(GetValidNames))]
        public void Ctor_ValidName(string name)
        {
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

        [PlatformSpecific(TestPlatforms.Windows)]  // named semaphores aren't supported on Unix
        [Fact]
        public void Ctor_NameUsedByOtherSynchronizationPrimitive_Windows()
        {
            string name = Guid.NewGuid().ToString("N");
            using (Semaphore s = new Semaphore(1, 1, name))
            {
                Assert.Throws<WaitHandleCannotBeOpenedException>(() => new Mutex(false, name));
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInAppContainer))] // Can't create global objects in appcontainer
        public void Ctor_ImpersonateAnonymousAndTryCreateGlobalMutexTest()
        {
            ThreadTestHelpers.RunTestInBackgroundThread(() =>
            {
                if (!ImpersonateAnonymousToken(GetCurrentThread()))
                {
                    // Impersonation is not allowed in the current context, this test is inappropriate in such a case
                    return;
                }

                Assert.Throws<UnauthorizedAccessException>(() => new Mutex(false, "Global\\" + Guid.NewGuid().ToString("N")));
                Assert.True(RevertToSelf());
            });
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsInAppContainer))] // Can't create global objects in appcontainer
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Ctor_TryCreateGlobalMutexTest_Uwp()
        {
            ThreadTestHelpers.RunTestInBackgroundThread(() =>
                Assert.Throws<UnauthorizedAccessException>(() => new Mutex(false, "Global\\" + Guid.NewGuid().ToString("N"))));
        }

        [Theory]
        [MemberData(nameof(GetValidNames))]
        public void OpenExisting(string name)
        {
            Mutex resultHandle;
            Assert.False(Mutex.TryOpenExisting(name, out resultHandle));

            using (Mutex m1 = new Mutex(false, name))
            {
                using (Mutex m2 = Mutex.OpenExisting(name))
                {
                    m1.CheckedWait();
                    Assert.False(Task.Factory.StartNew(() => m2.WaitOne(0), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).Result);
                    m1.ReleaseMutex();

                    m2.CheckedWait();
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
            AssertExtensions.Throws<ArgumentNullException>("name", () => Mutex.OpenExisting(null));
            AssertExtensions.Throws<ArgumentException>("name", null, () => Mutex.OpenExisting(string.Empty));
        }

        [Fact]
        public void OpenExisting_UnavailableName()
        {
            string name = Guid.NewGuid().ToString("N");
            Assert.Throws<WaitHandleCannotBeOpenedException>(() => Mutex.OpenExisting(name));
            Mutex ignored;
            Assert.False(Mutex.TryOpenExisting(name, out ignored));
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // named semaphores aren't supported on Unix
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

        public enum WaitHandleWaitType
        {
            WaitOne,
            WaitAny,
            WaitAll
        }

        private static IEnumerable<string> GetNamePrefixes()
        {
            yield return string.Empty;
            yield return "Local\\";
            yield return "Global\\";
        }

        public static IEnumerable<object[]> AbandonExisting_MemberData()
        {
            var nameGuidStr = Guid.NewGuid().ToString("N");
            foreach (WaitHandleWaitType waitType in Enum.GetValues(typeof(WaitHandleWaitType)))
            {
                foreach (int waitCount in new int[] { 1, 3 })
                {
                    if (waitType == WaitHandleWaitType.WaitOne && waitCount != 1)
                    {
                        continue;
                    }

                    for (int notAbandonedWaitIndex = 0; notAbandonedWaitIndex < waitCount; ++notAbandonedWaitIndex)
                    {
                        foreach (bool abandonDuringWait in new bool[] { false, true })
                        {
                            var args =
                                new object[]
                                {
                                    null, // name
                                    waitType,
                                    waitCount,
                                    notAbandonedWaitIndex,
                                    false, // isNotAbandonedWaitObjectSignaled
                                    abandonDuringWait
                                };

                            bool includeArgsForSignaledNotAbandonedWaitObject =
                                waitCount != 1 && (waitType == WaitHandleWaitType.WaitAll || !abandonDuringWait);

                            yield return (object[])args.Clone();
                            if (includeArgsForSignaledNotAbandonedWaitObject)
                            {
                                var newArgs = (object[])args.Clone();
                                newArgs[4] = true; // isNotAbandonedWaitObjectSignaled
                                yield return newArgs;
                            }

                            if (waitCount == 1 || PlatformDetection.IsWindows)
                            {
                                foreach (var namePrefix in GetNamePrefixes())
                                {
                                    var newArgs = (object[])args.Clone();
                                    newArgs[0] = namePrefix + nameGuidStr;
                                    yield return newArgs;
                                    if (includeArgsForSignaledNotAbandonedWaitObject)
                                    {
                                        newArgs = (object[])newArgs.Clone();
                                        newArgs[4] = true; // isNotAbandonedWaitObjectSignaled
                                        yield return newArgs;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(AbandonExisting_MemberData))]
        public void AbandonExisting(
            string name,
            WaitHandleWaitType waitType,
            int waitCount,
            int notAbandonedWaitIndex,
            bool isNotAbandonedWaitObjectSignaled,
            bool abandonDuringWait)
        {
            ThreadTestHelpers.RunTestInBackgroundThread(() =>
            {
                using (var m = new Mutex(false, name))
                using (Mutex m2 = waitCount == 1 ? null : new Mutex(false, name == null ? null : name + "_2"))
                using (ManualResetEvent e = waitCount == 1 ? null : new ManualResetEvent(isNotAbandonedWaitObjectSignaled))
                using (ManualResetEvent threadReadyForAbandon = abandonDuringWait ? new ManualResetEvent(false) : null)
                using (ManualResetEvent abandonSoon = abandonDuringWait ? new ManualResetEvent(false) : null)
                {
                    WaitHandle[] waitHandles = null;
                    if (waitType != WaitHandleWaitType.WaitOne)
                    {
                        waitHandles = new WaitHandle[waitCount];
                        if (waitCount == 1)
                        {
                            waitHandles[0] = m;
                        }
                        else
                        {
                            waitHandles[notAbandonedWaitIndex] = e;
                            waitHandles[notAbandonedWaitIndex == 0 ? 1 : 0] = m;
                            waitHandles[notAbandonedWaitIndex == 2 ? 1 : 2] = m2;
                        }
                    }

                    Thread t = ThreadTestHelpers.CreateGuardedThread(out Action waitForThread, () =>
                    {
                        Assert.True(m.WaitOne(0));
                        if (m2 != null)
                        {
                            Assert.True(m2.WaitOne(0));
                        }

                        if (abandonDuringWait)
                        {
                            threadReadyForAbandon.Set();
                            abandonSoon.CheckedWait();
                            Thread.Sleep(ThreadTestHelpers.ExpectedTimeoutMilliseconds);
                        }

                        // don't release the mutexes; abandon them on this thread
                    });
                    t.IsBackground = true;
                    t.Start();

                    if (abandonDuringWait)
                    {
                        threadReadyForAbandon.CheckedWait();
                        abandonSoon.Set();
                    }
                    else
                    {
                        waitForThread();
                    }

                    AbandonedMutexException ame;
                    switch (waitType)
                    {
                        case WaitHandleWaitType.WaitOne:
                            ame =
                                AssertExtensions.Throws<AbandonedMutexException, bool>(
                                    () => m.WaitOne(ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
                            Assert.Equal(-1, ame.MutexIndex);
                            Assert.Null(ame.Mutex);
                            break;

                        case WaitHandleWaitType.WaitAny:
                            if (waitCount != 1 && isNotAbandonedWaitObjectSignaled && notAbandonedWaitIndex == 0)
                            {
                                Assert.Equal(0, WaitHandle.WaitAny(waitHandles, 0));
                                AssertExtensions.Throws<AbandonedMutexException, bool>(
                                    () => m.WaitOne(ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
                                AssertExtensions.Throws<AbandonedMutexException, bool>(
                                    () => m2.WaitOne(ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
                                break;
                            }

                            if (waitCount != 1 && isNotAbandonedWaitObjectSignaled && notAbandonedWaitIndex != 0)
                            {
                                ame =
                                    Assert.Throws<AbandonedMutexException>(() =>
                                    {
                                        ThreadTestHelpers.WaitForCondition(() =>
                                        {
                                            // Actually expecting an exception from WaitAny(), but there may be a delay before
                                            // the mutex is actually released and abandoned. If there is no exception, the
                                            // WaitAny() must have succeeded due to the event being signaled.
                                            int r = WaitHandle.WaitAny(waitHandles, ThreadTestHelpers.UnexpectedTimeoutMilliseconds);
                                            Assert.Equal(notAbandonedWaitIndex, r);
                                            return false;
                                        });
                                    });
                            }
                            else
                            {
                                ame =
                                    AssertExtensions.Throws<AbandonedMutexException, int>(
                                        () => WaitHandle.WaitAny(waitHandles, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
                            }

                            // Due to a potential delay in abandoning mutexes, either mutex may have been seen to be
                            // abandoned first
                            Assert.True(ame.Mutex == m || (m2 != null && ame.Mutex == m2));
                            int mIndex = waitCount != 1 && notAbandonedWaitIndex == 0 ? 1 : 0;
                            int m2Index = waitCount != 1 && notAbandonedWaitIndex == 2 ? 1 : 2;
                            if (ame.Mutex == m)
                            {
                                Assert.Equal(mIndex, ame.MutexIndex);
                            }
                            else
                            {
                                Assert.True(m2Index < notAbandonedWaitIndex);
                                Assert.Equal(m2Index, ame.MutexIndex);
                            }

                            // Verify that the other mutex also gets abandoned
                            if (ame.MutexIndex == mIndex)
                            {
                                if (m2 != null)
                                {
                                    AssertExtensions.Throws<AbandonedMutexException, bool>(
                                        () => m2.WaitOne(ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
                                }
                            }
                            else
                            {
                                AssertExtensions.Throws<AbandonedMutexException, bool>(
                                    () => m.WaitOne(ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
                            }

                            break;

                        case WaitHandleWaitType.WaitAll:
                            if (waitCount != 1 && !isNotAbandonedWaitObjectSignaled)
                            {
                                Assert.False(WaitHandle.WaitAll(waitHandles, ThreadTestHelpers.ExpectedTimeoutMilliseconds * 2));
                                Assert.True(e.Set());
                            }

                            ame =
                                AssertExtensions.Throws<AbandonedMutexException, bool>(
                                    () => WaitHandle.WaitAll(waitHandles, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
                            Assert.Equal(-1, ame.MutexIndex);
                            Assert.Null(ame.Mutex);
                            break;
                    }

                    if (abandonDuringWait)
                    {
                        waitForThread();
                    }

                    m.ReleaseMutex();
                    m2?.ReleaseMutex();
                }
            });
        }

        public static IEnumerable<object[]> CrossProcess_NamedMutex_ProtectedFileAccessAtomic_MemberData()
        {
            var nameGuidStr = Guid.NewGuid().ToString("N");
            foreach (var namePrefix in GetNamePrefixes())
            {
                yield return new object[] { namePrefix + nameGuidStr };
            }
        }

        [ActiveIssue(34666)]
        [Theory]
        [MemberData(nameof(CrossProcess_NamedMutex_ProtectedFileAccessAtomic_MemberData))]
        public void CrossProcess_NamedMutex_ProtectedFileAccessAtomic(string prefix)
        {
            ThreadTestHelpers.RunTestInBackgroundThread(() =>
            {
                string mutexName = prefix + Guid.NewGuid().ToString("N");
                string fileName = GetTestFilePath();

                Action<string, string> otherProcess = (m, f) =>
                {
                    using (var mutex = Mutex.OpenExisting(m))
                    {
                        mutex.CheckedWait();
                        try
                        { File.WriteAllText(f, "0"); }
                        finally { mutex.ReleaseMutex(); }

                        IncrementValueInFileNTimes(mutex, f, 10);
                    }
                };

                using (var mutex = new Mutex(false, mutexName))
                using (var remote = RemoteExecutor.Invoke(otherProcess, mutexName, fileName))
                {
                    SpinWait.SpinUntil(() => File.Exists(fileName), ThreadTestHelpers.UnexpectedTimeoutMilliseconds);

                    IncrementValueInFileNTimes(mutex, fileName, 10);
                }

                Assert.Equal(20, int.Parse(File.ReadAllText(fileName)));
            });
        }

        private static void IncrementValueInFileNTimes(Mutex mutex, string fileName, int n)
        {
            for (int i = 0; i < n; i++)
            {
                mutex.CheckedWait();
                try
                {
                    int current = int.Parse(File.ReadAllText(fileName));
                    Thread.Sleep(10);
                    File.WriteAllText(fileName, (current + 1).ToString());
                }
                finally { mutex.ReleaseMutex(); }
            }
        }

        public static TheoryData<string> GetValidNames()
        {
            var names  =  new TheoryData<string>() { Guid.NewGuid().ToString("N") };

            if (PlatformDetection.IsWindows)
                names.Add(Guid.NewGuid().ToString("N") + new string('a', 1000));

            return names;
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentThread();

        [DllImport("advapi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ImpersonateAnonymousToken(IntPtr threadHandle);

        [DllImport("advapi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RevertToSelf();
    }
}
