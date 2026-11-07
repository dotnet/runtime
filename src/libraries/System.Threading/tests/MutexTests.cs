// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Threading.Tests
{
    public class MutexTests : FileCleanupTestBase
    {
        [Fact]
        public void ConstructorAndDisposeTest()
        {
            var m = new Mutex();
            Assert.True(m.WaitOne(0));
            m.ReleaseMutex();
            m.Dispose();
            Assert.Throws<ObjectDisposedException>(() => m.ReleaseMutex());
            Assert.Throws<ObjectDisposedException>(() => m.WaitOne(0));

            m = new Mutex(false);
            Assert.True(m.WaitOne(0));
            m.ReleaseMutex();
            m.Dispose();

            m = new Mutex(true);
            Assert.True(m.WaitOne(0));
            m.ReleaseMutex();
            m.ReleaseMutex();
            m.Dispose();

            m = new Mutex(true);
            Assert.True(m.WaitOne(0));
            m.Dispose();
            Assert.Throws<ObjectDisposedException>(() => m.ReleaseMutex());
            Assert.Throws<ObjectDisposedException>(() => m.WaitOne(0));
        }

        [Fact]
        public void AcquireAndReleaseTest()
        {
            var m = new Mutex();
            Assert.Throws<ApplicationException>(() => m.ReleaseMutex());
            Assert.True(m.WaitOne(0));
            m.ReleaseMutex();
            Assert.Throws<ApplicationException>(() => m.ReleaseMutex());
            Assert.True(m.WaitOne(0));
            Assert.True(m.WaitOne(0));
            Assert.True(m.WaitOne(0));
            m.ReleaseMutex();
            m.ReleaseMutex();
            m.ReleaseMutex();
            Assert.Throws<ApplicationException>(() => m.ReleaseMutex());
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsCoreCLR), nameof(PlatformDetection.IsNotMultithreadingSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public void SingleThreadedNamedHandlesShareOwnershipAndLifetime(bool initiallyOwned)
        {
            string name = Guid.NewGuid().ToString("N");
            using Mutex original = new(initiallyOwned, name, out bool createdNew);
            Assert.True(createdNew);
            if (!initiallyOwned)
            {
                Assert.True(original.WaitOne(0));
            }

            using Mutex alias = new(initiallyOwned: true, name, out createdNew);
            Assert.False(createdNew);
            Assert.Throws<DuplicateWaitObjectException>(() => WaitHandle.WaitAll([original, alias], 0));
            Assert.Equal(0, WaitHandle.WaitAny([original, alias], 0));
            alias.ReleaseMutex();
            original.Dispose();

            alias.ReleaseMutex();
            Assert.Throws<ApplicationException>(() => alias.ReleaseMutex());
            using Mutex opened = Mutex.OpenExisting(name);
            alias.Dispose();
            Assert.True(opened.WaitOne(0));
            opened.ReleaseMutex();
            opened.Dispose();
            Assert.False(Mutex.TryOpenExisting(name, out _));

            using Mutex recreated = new(initiallyOwned: true, name, out createdNew);
            Assert.True(createdNew);
            recreated.ReleaseMutex();
            Assert.Throws<ApplicationException>(() => recreated.ReleaseMutex());
        }

        [Fact]
        public void WaitTest()
        {
            var m = new Mutex();
            m.CheckedWait();
            m.CheckedWait();
            m.ReleaseMutex();
            m.ReleaseMutex();
            Assert.True(m.WaitOne());
            Assert.True(m.WaitOne());
            m.ReleaseMutex();
            m.ReleaseMutex();
        }

        [Fact]
        public void MultiWaitWithAllIndexesUnlockedTest()
        {
            var ms =
                new Mutex[]
                {
                    new Mutex(),
                    new Mutex(),
                    new Mutex(),
                    new Mutex()
                };
            Assert.Equal(0, WaitHandle.WaitAny(ms, 0));
            ms[0].ReleaseMutex();
            for (int i = 1; i < ms.Length; ++i)
            {
                Assert.Throws<ApplicationException>(() => ms[i].ReleaseMutex());
            }
            Assert.Equal(0, WaitHandle.WaitAny(ms, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
            ms[0].ReleaseMutex();
            for (int i = 1; i < ms.Length; ++i)
            {
                Assert.Throws<ApplicationException>(() => ms[i].ReleaseMutex());
            }
            Assert.Equal(0, WaitHandle.WaitAny(ms));
            ms[0].ReleaseMutex();
            for (int i = 1; i < ms.Length; ++i)
            {
                Assert.Throws<ApplicationException>(() => ms[i].ReleaseMutex());
            }
            Assert.True(WaitHandle.WaitAll(ms, 0));
            for (int i = 0; i < ms.Length; ++i)
            {
                ms[i].ReleaseMutex();
            }
            Assert.True(WaitHandle.WaitAll(ms, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
            for (int i = 0; i < ms.Length; ++i)
            {
                ms[i].ReleaseMutex();
            }
            Assert.True(WaitHandle.WaitAll(ms));
            for (int i = 0; i < ms.Length; ++i)
            {
                ms[i].ReleaseMutex();
            }
        }

        [Fact]
        public void MultiWaitWithOuterIndexesLockedTest()
        {
            var ms =
                new Mutex[]
                {
                    new Mutex(true),
                    new Mutex(),
                    new Mutex(),
                    new Mutex(true)
                };
            Assert.Equal(0, WaitHandle.WaitAny(ms, 0));
            ms[0].ReleaseMutex();
            Assert.Throws<ApplicationException>(() => ms[1].ReleaseMutex());
            Assert.Throws<ApplicationException>(() => ms[2].ReleaseMutex());
            Assert.Equal(0, WaitHandle.WaitAny(ms, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
            ms[0].ReleaseMutex();
            Assert.Throws<ApplicationException>(() => ms[1].ReleaseMutex());
            Assert.Throws<ApplicationException>(() => ms[2].ReleaseMutex());
            Assert.Equal(0, WaitHandle.WaitAny(ms));
            ms[0].ReleaseMutex();
            Assert.Throws<ApplicationException>(() => ms[1].ReleaseMutex());
            Assert.Throws<ApplicationException>(() => ms[2].ReleaseMutex());
            Assert.True(WaitHandle.WaitAll(ms, 0));
            for (int i = 0; i < ms.Length; ++i)
            {
                ms[i].ReleaseMutex();
            }
            Assert.True(WaitHandle.WaitAll(ms, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
            for (int i = 0; i < ms.Length; ++i)
            {
                ms[i].ReleaseMutex();
            }
            Assert.True(WaitHandle.WaitAll(ms));
            for (int i = 0; i < ms.Length; ++i)
            {
                ms[i].ReleaseMutex();
            }
            ms[0].ReleaseMutex();
            Assert.Throws<ApplicationException>(() => ms[1].ReleaseMutex());
            Assert.Throws<ApplicationException>(() => ms[2].ReleaseMutex());
            ms[3].ReleaseMutex();
        }

        [Fact]
        public void MultiWaitWithAllIndexesLockedTest()
        {
            var ms =
                new Mutex[]
                {
                    new Mutex(true),
                    new Mutex(true),
                    new Mutex(true),
                    new Mutex(true)
                };
            Assert.Equal(0, WaitHandle.WaitAny(ms, 0));
            ms[0].ReleaseMutex();
            Assert.Equal(0, WaitHandle.WaitAny(ms, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
            ms[0].ReleaseMutex();
            Assert.Equal(0, WaitHandle.WaitAny(ms));
            ms[0].ReleaseMutex();
            Assert.True(WaitHandle.WaitAll(ms, 0));
            for (int i = 0; i < ms.Length; ++i)
            {
                ms[i].ReleaseMutex();
            }
            Assert.True(WaitHandle.WaitAll(ms, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
            for (int i = 0; i < ms.Length; ++i)
            {
                ms[i].ReleaseMutex();
            }
            Assert.True(WaitHandle.WaitAll(ms));
            for (int i = 0; i < ms.Length; ++i)
            {
                ms[i].ReleaseMutex();
            }
            for (int i = 0; i < ms.Length; ++i)
            {
                ms[i].ReleaseMutex();
                Assert.Throws<ApplicationException>(() => ms[i].ReleaseMutex());
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
        public void MutualExclusionTest()
        {
            var threadLocked = new AutoResetEvent(false);
            var continueThread = new AutoResetEvent(false);
            var m = new Mutex();
            Thread t = ThreadTestHelpers.CreateGuardedThread(out Action waitForThread, () =>
            {
                Assert.True(m.WaitOne(0));
                threadLocked.Set();
                continueThread.CheckedWait();
                m.ReleaseMutex();
            });
            t.IsBackground = true;
            t.Start();
            threadLocked.CheckedWait();
            Assert.False(m.WaitOne(0));
            Assert.False(m.WaitOne(ThreadTestHelpers.ExpectedTimeoutMilliseconds));
            continueThread.Set();
            waitForThread();
            Assert.True(m.WaitOne(0));
            m.ReleaseMutex();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMobile))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void Ctor_InvalidNames_Unix()
        {
            Assert.Throws<IOException>(() => new Mutex("Foo/Bar", options: default));
            Assert.Throws<IOException>(() => new Mutex("Global\\Foo/Bar", options: new NamedWaitHandleOptions { CurrentSessionOnly = false }));
        }

        [Theory]
        [MemberData(nameof(GetValidNames))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/117760",platforms: TestPlatforms.Android, runtimes: TestRuntimes.CoreCLR)]
        public void Ctor_ValidName(string name)
        {
            bool createdNew;
            using (Mutex m1 = new Mutex(false, name, options: default, out createdNew))
            {
                Assert.True(createdNew);
                using (Mutex m2 = new Mutex(false, name, options: default, out createdNew))
                {
                    Assert.False(createdNew);
                }
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // named semaphores aren't supported on Unix
        [Theory]
        [MemberData(nameof(NameOptionCombinations_MemberData))]
        public void NameUsedByOtherSynchronizationPrimitive_Windows(bool currentUserOnly, bool currentSessionOnly)
        {
            string name = Guid.NewGuid().ToString("N");
            NamedWaitHandleOptions options =
                new() { CurrentUserOnly = currentUserOnly, CurrentSessionOnly = currentSessionOnly };
            using (Semaphore s = new Semaphore(1, 1, name, options))
            {
                Assert.Throws<WaitHandleCannotBeOpenedException>(() => new Mutex(name, options));
                Assert.Throws<WaitHandleCannotBeOpenedException>(() => Mutex.OpenExisting(name, options));
                Assert.False(Mutex.TryOpenExisting(name, options, out _));
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInAppContainer))] // Can't create global objects in appcontainer
        [MemberData(nameof(NameOptionCombinations_MemberData))]
        public void Ctor_ImpersonateAnonymousAndTryCreateGlobalMutexTest(bool currentUserOnly, bool currentSessionOnly)
        {
            ThreadTestHelpers.RunTestInBackgroundThread(() =>
            {
                string initiallyCreatedName = Guid.NewGuid().ToString("N");
                string name = Guid.NewGuid().ToString("N");
                NamedWaitHandleOptions options =
                    new() { CurrentUserOnly = currentUserOnly, CurrentSessionOnly = currentSessionOnly };

                using var m = new Mutex(initiallyCreatedName, options);

                if (!ImpersonateAnonymousToken(GetCurrentThread()))
                {
                    // Impersonation is not allowed in the current context, this test is inappropriate in such a case
                    return;
                }

                bool reverted;
                try
                {
                    Assert.Throws<UnauthorizedAccessException>(() => new Mutex(name, options));
                    Assert.Throws<UnauthorizedAccessException>(() => new Mutex(initiallyCreatedName, options));
                    Assert.Throws<UnauthorizedAccessException>(() => new Mutex(initiallyCreatedName, options));
                    Assert.Throws<UnauthorizedAccessException>(() => Mutex.OpenExisting(initiallyCreatedName, options));
                    Assert.Throws<UnauthorizedAccessException>(
                        () => Mutex.TryOpenExisting(initiallyCreatedName, options, out _));
                }
                finally
                {
                    reverted = RevertToSelf();
                }

                Assert.True(reverted);
            });
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsInAppContainer))] // Can't create global objects in appcontainer
        [MemberData(nameof(NameOptionCombinations_MemberData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Ctor_TryCreateGlobalMutexTest_Uwp(bool currentUserOnly, bool currentSessionOnly)
        {
            NamedWaitHandleOptions options =
                new() { CurrentUserOnly = currentUserOnly, CurrentSessionOnly = currentSessionOnly };
            ThreadTestHelpers.RunTestInBackgroundThread(() =>
                Assert.Throws<UnauthorizedAccessException>(() => new Mutex(Guid.NewGuid().ToString("N"), options)));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
        [MemberData(nameof(GetValidNames))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/117760",platforms: TestPlatforms.Android, runtimes: TestRuntimes.CoreCLR)]
        public void OpenExisting(string name)
        {
            Mutex resultHandle;
            Assert.False(Mutex.TryOpenExisting(name, options: default, out resultHandle));

            using (Mutex m1 = new Mutex(name, options: default))
            {
                using (Mutex m2 = Mutex.OpenExisting(name, options: default))
                {
                    m1.CheckedWait();
                    Assert.False(Task.Factory.StartNew(() => m2.WaitOne(0), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).Result);
                    m1.ReleaseMutex();

                    m2.CheckedWait();
                    Assert.False(Task.Factory.StartNew(() => m1.WaitOne(0), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).Result);
                    m2.ReleaseMutex();
                }

                Assert.True(Mutex.TryOpenExisting(name, options: default, out resultHandle));
                Assert.NotNull(resultHandle);
                resultHandle.Dispose();
            }
        }

        [Fact]
        public void OpenExisting_InvalidNames()
        {
            AssertExtensions.Throws<ArgumentNullException>("name", () => Mutex.OpenExisting(null, options: default));
            AssertExtensions.Throws<ArgumentException>("name", null, () => Mutex.OpenExisting(string.Empty, options: default));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/117760",platforms: TestPlatforms.Android, runtimes: TestRuntimes.CoreCLR)]
        public void OpenExisting_UnavailableName()
        {
            string name = Guid.NewGuid().ToString("N");
            Assert.Throws<WaitHandleCannotBeOpenedException>(() => Mutex.OpenExisting(name, options: default));
            Mutex m;
            Assert.False(Mutex.TryOpenExisting(name, options: default, out m));
            Assert.Null(m);

            using (m = new Mutex(name, options: default)) { }
            Assert.Throws<WaitHandleCannotBeOpenedException>(() => Mutex.OpenExisting(name, options: default));
            Assert.False(Mutex.TryOpenExisting(name, options: default, out m));
            Assert.Null(m);
        }

        public enum WaitHandleWaitType
        {
            WaitOne,
            WaitAny,
            WaitAll
        }

        [Fact]
        public void NamedWaitHandleOptionsTest()
        {
            NamedWaitHandleOptions options = default;
            Assert.True(options.CurrentUserOnly);
            Assert.True(options.CurrentSessionOnly);

            options.CurrentUserOnly = false;
            Assert.False(options.CurrentUserOnly);
            options.CurrentSessionOnly = false;
            Assert.False(options.CurrentSessionOnly);
        }

        [Theory]
        [MemberData(nameof(NamePrefixes_MemberData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/117760",platforms: TestPlatforms.Android, runtimes: TestRuntimes.CoreCLR)]
        public void NameOptionsApiCompatibilityTest(string namePrefix)
        {
            string name = Guid.NewGuid().ToString("N");
            string prefixedName = namePrefix + name;
            bool currentSessionOnly = namePrefix != @"Global\";
            bool currentUserOnly = !PlatformDetection.IsWindows;

            using (var m = new Mutex(initiallyOwned: false, prefixedName, out bool createdNew))
            {
                Assert.True(createdNew);

                new Mutex(
                    initiallyOwned: false,
                    name,
                    new() { CurrentUserOnly = currentUserOnly, CurrentSessionOnly = currentSessionOnly },
                    out createdNew).Dispose();
                Assert.False(createdNew);

                Mutex.OpenExisting(
                    name,
                    new() { CurrentUserOnly = currentUserOnly, CurrentSessionOnly = currentSessionOnly }).Dispose();

                Assert.True(
                    Mutex.TryOpenExisting(
                        name,
                        new() { CurrentUserOnly = currentUserOnly, CurrentSessionOnly = currentSessionOnly },
                        out Mutex m2));
                m2.Dispose();
            }

            using (var m =
                new Mutex(
                    initiallyOwned: false,
                    name,
                    new() { CurrentUserOnly = currentUserOnly, CurrentSessionOnly = currentSessionOnly },
                    out bool createdNew))
            {
                Assert.True(createdNew);

                new Mutex(initiallyOwned: false, prefixedName, out createdNew).Dispose();
                Assert.False(createdNew);

                Mutex.OpenExisting(prefixedName).Dispose();

                Assert.True(Mutex.TryOpenExisting(prefixedName, out Mutex m2));
                m2.Dispose();
            }
        }

        public static IEnumerable<object[]> NamePrefixAndOptionsCompatibilityTest_MemberData()
        {
            foreach (NamedWaitHandleOptions options in GetNameOptionCombinations())
            {
                foreach (string namePrefix in GetNamePrefixes())
                {
                    yield return new object[] { options.CurrentUserOnly, options.CurrentSessionOnly, namePrefix };
                }
            }
        }

        [Theory]
        [MemberData(nameof(NamePrefixAndOptionsCompatibilityTest_MemberData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/117760",platforms: TestPlatforms.Android, runtimes: TestRuntimes.CoreCLR)]
        public void NamePrefixAndOptionsCompatibilityTest(bool currentUserOnly, bool currentSessionOnly, string namePrefix)
        {
            string name = namePrefix + Guid.NewGuid().ToString("N");
            bool currentSessionOnlyBasedOnPrefix = namePrefix != @"Global\";
            NamedWaitHandleOptions options =
                new() { CurrentUserOnly = currentUserOnly, CurrentSessionOnly = currentSessionOnly };

            if (string.IsNullOrEmpty(namePrefix) || currentSessionOnlyBasedOnPrefix == currentSessionOnly)
            {
                new Mutex(name, options).Dispose();
            }
            else
            {
                AssertExtensions.Throws<ArgumentException>("name", () => new Mutex(name, options));
            }
        }

        public static IEnumerable<object[]> NameNamespaceTests_MemberData()
        {
            foreach (NamedWaitHandleOptions o in GetNameOptionCombinations())
            {
                foreach (NamedWaitHandleOptions o2 in GetNameOptionCombinations())
                {
                    yield return
                        new object[] { o.CurrentUserOnly, o.CurrentSessionOnly, o2.CurrentUserOnly, o2.CurrentSessionOnly };
                }
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoNorServerCore))] // Windows Nano Server and Server Core apparently use the same namespace for the Local\ and Global\ prefixes
        [MemberData(nameof(NameNamespaceTests_MemberData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/117760",platforms: TestPlatforms.Android, runtimes: TestRuntimes.CoreCLR)]
        public void NameNamespaceTest(
            bool create_currentUserOnly,
            bool create_currentSessionOnly,
            bool open_currentUserOnly,
            bool open_currentSessionOnly)
        {
            string name = Guid.NewGuid().ToString("N");
            NamedWaitHandleOptions createOptions =
                new() { CurrentUserOnly = create_currentUserOnly, CurrentSessionOnly = create_currentSessionOnly };
            NamedWaitHandleOptions openOptions =
                new() { CurrentUserOnly = open_currentUserOnly, CurrentSessionOnly = open_currentSessionOnly };

            using (var m = new Mutex(name, createOptions))
            {
                if (PlatformDetection.IsWindows &&
                    openOptions.CurrentSessionOnly == createOptions.CurrentSessionOnly &&
                    !createOptions.CurrentUserOnly &&
                    openOptions.CurrentUserOnly)
                {
                    Assert.Throws<WaitHandleCannotBeOpenedException>(() => new Mutex(name, openOptions));
                    Assert.Throws<WaitHandleCannotBeOpenedException>(() => Mutex.OpenExisting(name, openOptions));
                    Assert.False(Mutex.TryOpenExisting(name, openOptions, out _));
                    return;
                }

                bool sameOptions =
                    openOptions.CurrentUserOnly == createOptions.CurrentUserOnly &&
                    openOptions.CurrentSessionOnly == createOptions.CurrentSessionOnly;
                bool expectedCreatedNew =
                    !sameOptions &&
                    (!PlatformDetection.IsWindows || openOptions.CurrentSessionOnly != createOptions.CurrentSessionOnly);

                new Mutex(initiallyOwned: false, name, openOptions, out bool createdNew).Dispose();
                Assert.Equal(expectedCreatedNew, createdNew);

                if (expectedCreatedNew)
                {
                    Assert.Throws<WaitHandleCannotBeOpenedException>(() => Mutex.OpenExisting(name, openOptions));
                }
                else
                {
                    Mutex.OpenExisting(name, openOptions).Dispose();
                }

                Assert.Equal(!expectedCreatedNew, Mutex.TryOpenExisting(name, openOptions, out Mutex m2));
                m2?.Dispose();
            }
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
                                    abandonDuringWait,
                                    true, // currentUserOnly
                                    true // currentSessionOnly
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
                                foreach (NamedWaitHandleOptions options in GetNameOptionCombinations())
                                {
                                    var newArgs = (object[])args.Clone();
                                    newArgs[0] = nameGuidStr;
                                    newArgs[6] = options.CurrentUserOnly;
                                    newArgs[7] = options.CurrentSessionOnly;
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

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
        [MemberData(nameof(AbandonExisting_MemberData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/117760",platforms: TestPlatforms.Android, runtimes: TestRuntimes.CoreCLR)]
        public void AbandonExisting(
            string name,
            WaitHandleWaitType waitType,
            int waitCount,
            int notAbandonedWaitIndex,
            bool isNotAbandonedWaitObjectSignaled,
            bool abandonDuringWait,
            bool currentUserOnly,
            bool currentSessionOnly)
        {
            ThreadTestHelpers.RunTestInBackgroundThread(() =>
            {
                NamedWaitHandleOptions options =
                    new() { CurrentUserOnly = currentUserOnly, CurrentSessionOnly = currentSessionOnly };

                using (var m = new Mutex(name, options))
                using (Mutex m2 = waitCount == 1 ? null : new Mutex(name == null ? null : name + "_2", options))
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
                                Assert.True(!isNotAbandonedWaitObjectSignaled || m2Index < notAbandonedWaitIndex);
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

        private static bool IsRemoteExecutorAndCrossProcessNamedMutexSupported =>
            RemoteExecutor.IsSupported && PlatformDetection.IsNotMobile;

        [ConditionalTheory(typeof(MutexTests), nameof(IsRemoteExecutorAndCrossProcessNamedMutexSupported))]
        [MemberData(nameof(NameOptionCombinations_MemberData))]
        public void CrossProcess_NamedMutex_ProtectedFileAccessAtomic(bool currentUserOnly, bool currentSessionOnly)
        {
            string fileName = GetTestFilePath();
            try
            {
                ThreadTestHelpers.RunTestInBackgroundThread(() =>
                {
                    string mutexName = Guid.NewGuid().ToString("N");
                    NamedWaitHandleOptions options =
                        new() { CurrentUserOnly = currentUserOnly, CurrentSessionOnly = currentSessionOnly };

                    Action<string, string, string, string> otherProcess =
                        (mutexName, fileName, currentUserOnlyStr, currentSessionOnlyStr) =>
                        {
                            NamedWaitHandleOptions options =
                                new()
                                {
                                    CurrentUserOnly = int.Parse(currentUserOnlyStr) != 0,
                                    CurrentSessionOnly = int.Parse(currentSessionOnlyStr) != 0
                                };

                            using (var mutex = Mutex.OpenExisting(mutexName, options))
                            {
                                mutex.CheckedWait();
                                try
                                { File.WriteAllText(fileName, "0"); }
                                finally { mutex.ReleaseMutex(); }

                                IncrementValueInFileNTimes(mutex, fileName, 10);
                            }
                        };

                    using (var mutex = new Mutex(mutexName, options))
                    using (var remote =
                        RemoteExecutor.Invoke(
                            otherProcess,
                            mutexName,
                            fileName,
                            options.CurrentUserOnly ? "1" : "0",
                            options.CurrentSessionOnly ? "1" : "0"))
                    {
                        SpinWait.SpinUntil(
                            () =>
                            {
                                mutex.CheckedWait();
                                try
                                { return File.Exists(fileName) && int.TryParse(File.ReadAllText(fileName), out _); }
                                finally { mutex.ReleaseMutex(); }
                            },
                            ThreadTestHelpers.UnexpectedTimeoutMilliseconds);

                        IncrementValueInFileNTimes(mutex, fileName, 10);
                    }

                    Assert.Equal(20, int.Parse(File.ReadAllText(fileName)));
                });
            }
            catch (Exception ex) when (File.Exists(fileName))
            {
                throw new AggregateException($"File contents: {File.ReadAllText(fileName)}", ex);
            }
        }

        [ConditionalTheory(typeof(MutexTests), nameof(IsRemoteExecutorAndCrossProcessNamedMutexSupported))]
        [MemberData(nameof(NameOptionCombinations_MemberData))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void CrossProcess_NamedMutex_ConcurrentCreateOrOpen(bool currentUserOnly, bool currentSessionOnly)
        {
            NamedWaitHandleOptions options =
                new() { CurrentUserOnly = currentUserOnly, CurrentSessionOnly = currentSessionOnly };

            Action<string, string> createMutexInOtherProcess =
                (mutexName, encodedArgs) =>
                {
                    string[] args = encodedArgs.Split('|');
                    NamedWaitHandleOptions options =
                        new()
                        {
                            CurrentUserOnly = int.Parse(args[0]) != 0,
                            CurrentSessionOnly = int.Parse(args[1]) != 0
                        };
                    string readyFilePath = args[2];
                    string startFilePath = args[3];
                    string resultFilePath = args[4];
                    string releaseFilePath = args[5];
                    int timeoutMs = int.Parse(args[6]);

                    File.WriteAllText(readyFilePath, "ready");
                    Assert.True(SpinWait.SpinUntil(() => File.Exists(startFilePath), timeoutMs));

                    using var mutex = new Mutex(false, mutexName, options, out bool createdNew);
                    File.WriteAllText(resultFilePath, createdNew ? "1" : "0");
                    Assert.True(SpinWait.SpinUntil(() => File.Exists(releaseFilePath), timeoutMs));
                };

            const int IterationCount = 10;
            for (int i = 0; i < IterationCount; ++i)
            {
                string mutexName = Guid.NewGuid().ToString("N");
                string readyFilePath1 = GetTestFilePath();
                string readyFilePath2 = GetTestFilePath();
                string startFilePath = GetTestFilePath();
                string releaseFilePath = GetTestFilePath();
                string resultFilePath1 = GetTestFilePath();
                string resultFilePath2 = GetTestFilePath();

                using (var remote1 =
                    RemoteExecutor.Invoke(
                        createMutexInOtherProcess,
                        mutexName,
                        $"{(options.CurrentUserOnly ? "1" : "0")}|{(options.CurrentSessionOnly ? "1" : "0")}|{readyFilePath1}|{startFilePath}|{resultFilePath1}|{releaseFilePath}|{RemoteExecutor.FailWaitTimeoutMilliseconds}"))
                using (var remote2 =
                    RemoteExecutor.Invoke(
                        createMutexInOtherProcess,
                        mutexName,
                        $"{(options.CurrentUserOnly ? "1" : "0")}|{(options.CurrentSessionOnly ? "1" : "0")}|{readyFilePath2}|{startFilePath}|{resultFilePath2}|{releaseFilePath}|{RemoteExecutor.FailWaitTimeoutMilliseconds}"))
                {
                    Assert.True(
                        SpinWait.SpinUntil(
                            () => File.Exists(readyFilePath1) && File.Exists(readyFilePath2),
                            RemoteExecutor.FailWaitTimeoutMilliseconds));

                    File.WriteAllText(startFilePath, "start");
                    Assert.True(
                        SpinWait.SpinUntil(
                            () => File.Exists(resultFilePath1) && File.Exists(resultFilePath2),
                            RemoteExecutor.FailWaitTimeoutMilliseconds));
                    File.WriteAllText(releaseFilePath, "release");
                }

                Assert.True(File.Exists(resultFilePath1));
                Assert.True(File.Exists(resultFilePath2));
                int createdNewCount =
                    int.Parse(File.ReadAllText(resultFilePath1)) +
                    int.Parse(File.ReadAllText(resultFilePath2));
                Assert.Equal(1, createdNewCount);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMobile), nameof(PlatformDetection.IsMultithreadingSupported))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public async Task NamedMutex_ConcurrentCreateOrOpen_CreatingThreadGetsInitialOwnership()
        {
            const int IterationCount = 10_000;
            string mutexName = Guid.NewGuid().ToString("N");
            using var barrier = new Barrier(2);
            using var cancellationTokenSource = new CancellationTokenSource();

            Task CreateOrOpenMutex()
            {
                return Task.Run(
                    () =>
                    {
                        try
                        {
                            for (int i = 0; i < IterationCount; i++)
                            {
                                barrier.SignalAndWait(cancellationTokenSource.Token);
                                using (var mutex = new Mutex(initiallyOwned: true, mutexName, out bool createdNew))
                                {
                                    if (createdNew)
                                    {
                                        barrier.SignalAndWait(cancellationTokenSource.Token);
                                        mutex.ReleaseMutex();
                                    }
                                    else
                                    {
                                        Assert.False(mutex.WaitOne(0));
                                        barrier.SignalAndWait(cancellationTokenSource.Token);
                                    }
                                }
                            }
                        }
                        catch
                        {
                            cancellationTokenSource.Cancel();
                            throw;
                        }
                    });
            }

            await Task.WhenAll(CreateOrOpenMutex(), CreateOrOpenMutex());
        }

        private static bool IsRemoteExecutorAndPThreadNamedMutexSupported =>
            IsRemoteExecutorAndCrossProcessNamedMutexSupported &&
            PlatformDetection.ManagedThreadCleanupOnFinalizerThread &&
            RuntimeInformation.ProcessArchitecture is not (Architecture.Arm or Architecture.Arm64);

        [ConditionalTheory(nameof(IsRemoteExecutorAndPThreadNamedMutexSupported))]
        [PlatformSpecific(TestPlatforms.Linux)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void NamedMutex_ReacquireBeforeAbandonedOwnerCleanup(bool recoverInOtherProcess, bool cleanupBeforeReacquire)
        {
            // Isolate the scenario because it deliberately stalls the child process's finalizer thread.
            RemoteExecutor.Invoke(
                (recoverInOtherProcessString, cleanupBeforeReacquireString) =>
                {
                    bool recoverInOtherProcess = bool.Parse(recoverInOtherProcessString);
                    bool cleanupBeforeReacquire = bool.Parse(cleanupBeforeReacquireString);
                    string name = Guid.NewGuid().ToString("N");
                    using var mutex = new Mutex(false, name);
                    using var finalizerStarted = new ManualResetEventSlim();
                    using var releaseFinalizer = new ManualResetEventSlim();
                    using var callbackCompleted = new ManualResetEventSlim();
                    NativeMutexThreadState ownerState = new(mutex, callbackCompleted);

                    try
                    {
                        // Prevent managed cleanup from retiring the dead owner's recursion count and ownership chain.
                        CreateFinalizerBlocker(finalizerStarted, releaseFinalizer);
                        GC.Collect();
                        finalizerStarted.CheckedWait();

                        // The foreign thread acquires once and exits without releasing. Unlike a managed Thread,
                        // its native mutex can be recovered before its managed ownership cleanup runs.
                        // Callback completion confirms acquisition; the recovery wait below observes native exit.
                        CreateNativeThread(ownerState);
                        callbackCompleted.CheckedWait();
                        Assert.Null(ownerState.CallbackException);
                        Assert.NotNull(ownerState.OwningThread);

                        if (recoverInOtherProcess)
                        {
                            // Another process consumes the abandonment notification and releases the native mutex,
                            // while this process still has the dead thread's local ownership bookkeeping.
                            RemoteExecutor.Invoke(
                                name =>
                                {
                                    using Mutex other = Mutex.OpenExisting(name);
                                    Assert.Throws<AbandonedMutexException>(() => other.WaitOne(ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
                                    other.ReleaseMutex();
                                },
                                name).Dispose();
                        }

                        // Exercise cleanup before reacquisition as well as reacquisition while cleanup is still blocked.
                        if (cleanupBeforeReacquire)
                        {
                            releaseFinalizer.Set();
                            Assert.True(ownerState.OwningThread.Join(ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
                        }

                        // Recovery elsewhere already consumed abandonment; direct recovery here must report it.
                        // Both paths must establish new ownership rather than reuse the dead thread's recursion count.
                        if (recoverInOtherProcess)
                        {
                            Assert.True(mutex.WaitOne(ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
                        }
                        else
                        {
                            Assert.Throws<AbandonedMutexException>(() => mutex.WaitOne(ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
                        }

                        // Releasing one recursive level must leave the original acquisition held.
                        Assert.True(mutex.WaitOne(0));
                        mutex.ReleaseMutex();

                        // Check native mutual exclusion, not just the process-local recursion count.
                        // Another process must remain excluded until the final release.
                        RemoteExecutor.Invoke(
                            name =>
                            {
                                using Mutex other = Mutex.OpenExisting(name);
                                Assert.False(other.WaitOne(0));
                            },
                            name).Dispose();

                        // Let any remaining dead-owner cleanup run while the new owner still holds the mutex.
                        // It must not disturb that ownership or prevent the new owner's final release.
                        releaseFinalizer.Set();
                        Assert.True(ownerState.OwningThread.Join(ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
                        mutex.ReleaseMutex();
                    }
                    finally
                    {
                        // An assertion failure must not leave the child process's finalizer blocked.
                        releaseFinalizer.Set();
                        GC.WaitForPendingFinalizers();
                    }

                    // Closing the last handle must retire the object. A leaked dead-owner reference would
                    // instead make this constructor reopen the previous object with createdNew == false.
                    mutex.Dispose();
                    using var recreated = new Mutex(initiallyOwned: true, name, out bool createdNew);
                    Assert.True(createdNew);
                    recreated.ReleaseMutex();
                },
                recoverInOtherProcess.ToString(),
                cleanupBeforeReacquire.ToString()).Dispose();
        }

        [ConditionalTheory(nameof(IsRemoteExecutorAndPThreadNamedMutexSupported))]
        [PlatformSpecific(TestPlatforms.Linux)]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void NamedMutex_DeferredAbandonment_DoesNotModifyRecoveredMutex(bool disposeOwningHandle, bool recordRecovery)
        {
            // The child process has the dead owner; a second process will hold the recovered native mutex
            // while the first process runs its deferred cleanup. Isolate the finalizer stall from the runner.
            RemoteExecutor.Invoke(
                (name, releaseFilePath, disposeOwningHandleString, recordRecoveryString) =>
                {
                    NamedWaitHandleOptions options = new() { CurrentUserOnly = false, CurrentSessionOnly = false };
                    using Mutex mutex = new(false, $"Global\\{name}", options);
                    // Keep the object open even in cases that close the original owning handle before cleanup.
                    using Mutex alias = Mutex.OpenExisting($"Global\\{name}", options);
                    using var finalizerStarted = new ManualResetEventSlim();
                    using var releaseFinalizer = new ManualResetEventSlim();
                    using var callbackCompleted = new ManualResetEventSlim();
                    NativeMutexThreadState ownerState = new(mutex, callbackCompleted);

                    try
                    {
                        // Hold back managed cleanup so native abandonment and recovery can happen first.
                        CreateFinalizerBlocker(finalizerStarted, releaseFinalizer);
                        GC.Collect();
                        finalizerStarted.CheckedWait();

                        // The foreign thread exits after its callback returns, abandoning the native mutex.
                        // Its managed ownership cleanup remains pending until the finalizer is released.
                        CreateNativeThread(ownerState);
                        callbackCompleted.CheckedWait();
                        Assert.Null(ownerState.CallbackException);
                        Assert.NotNull(ownerState.OwningThread);

                        // The recovery process either publishes its ownership through Mutex.WaitOne or holds
                        // the native mutex without publishing new owner IDs. Both cases keep it locked until signaled.
                        using (RemoteInvokeHandle recovery = RemoteExecutor.Invoke(RecoverPThreadMutex, name, releaseFilePath, recordRecoveryString))
                        {
                            try
                            {
                                // Snapshot the shared storage only after recovery has acquired the native mutex.
                                Assert.True(SpinWait.SpinUntil(
                                    () => File.Exists(releaseFilePath + ".ready"),
                                    ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
                                string sharedMemoryPath = $"{GlobalSharedMemoryDirectory}/{name}";
                                byte[] sharedData = File.ReadAllBytes(sharedMemoryPath);
                                // Cover deferred cleanup after the original owning handle has already been closed.
                                if (bool.Parse(disposeOwningHandleString))
                                {
                                    mutex.Dispose();
                                }

                                // Run the dead owner's managed cleanup while the recovery process still holds the mutex.
                                releaseFinalizer.Set();
                                Assert.True(ownerState.OwningThread.Join(ThreadTestHelpers.UnexpectedTimeoutMilliseconds));

                                // Cleanup may retire only local bookkeeping, even when shared IDs still name the dead owner.
                                // Shared bytes must remain unchanged, and the recovery process must keep us excluded.
                                Assert.Equal(sharedData, File.ReadAllBytes(sharedMemoryPath));
                                Assert.False(alias.WaitOne(0));
                            }
                            finally
                            {
                                // Let the recovery process release and exit even if an assertion fails.
                                File.WriteAllText(releaseFilePath, "release");
                            }
                        }

                        // Recovery has now released normally: acquiring must not report abandonment again,
                        // and recursive acquisition/release must still balance correctly.
                        Assert.True(alias.WaitOne(0));
                        Assert.True(alias.WaitOne(0));
                        alias.ReleaseMutex();
                        alias.ReleaseMutex();
                    }
                    finally
                    {
                        // Always unblock and drain finalization before leaving the isolated process.
                        releaseFinalizer.Set();
                        GC.WaitForPendingFinalizers();
                    }

                    // Neither the disposed handle nor the dead owner's ownership reference may keep the object alive.
                    mutex.Dispose();
                    alias.Dispose();
                    using Mutex recreated = new(initiallyOwned: true, $"Global\\{name}", options, out bool createdNew);
                    Assert.True(createdNew);
                    recreated.ReleaseMutex();
                },
                Guid.NewGuid().ToString("N"),
                GetTestFilePath(),
                disposeOwningHandle.ToString(),
                recordRecovery.ToString()).Dispose();
        }

        private static unsafe void RecoverPThreadMutex(string name, string releaseFilePath, string recordRecoveryString)
        {
            const int OwnerDead = 0x10041; // Interop.Error.EOWNERDEAD
            NamedWaitHandleOptions options = new() { CurrentUserOnly = false, CurrentSessionOnly = false };
            using Mutex mutex = Mutex.OpenExisting($"Global\\{name}", options);
            if (bool.Parse(recordRecoveryString))
            {
                // This exception reports successful acquisition of an abandoned mutex. Keep that ownership
                // until the original process finishes its cleanup and signals us to release.
                Assert.Throws<AbandonedMutexException>(() => mutex.WaitOne(ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
                try
                {
                    WaitForRelease();
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
                return;
            }

            // We explicitly map in the underlying mutex data structures so we can simulate the
            // "recover an abandoned mutex and acquire" and "cleanup ownership information of an abandoned mutex" race.
            // Force the gap between native recovery and managed ownership publication: acquisition makes
            // the pthread mutex consistent and holds it, but shared IDs still identify the dead owner.
            // Deferred cleanup must not interpret those stale IDs as permission to alter or unlock it.
            using MemoryMappedFile mapping = MemoryMappedFile.CreateFromFile(
                $"{GlobalSharedMemoryDirectory}/{name}", FileMode.Open, null, 0, MemoryMappedFileAccess.ReadWrite);
            using MemoryMappedViewAccessor view = mapping.CreateViewAccessor();
            byte* pointer = null;
            view.SafeMemoryMappedViewHandle.AcquirePointer(ref pointer);
            // The shared data follows the eight-byte SharedMemorySharedDataHeader.
            void* sharedData = pointer + view.PointerOffset + sizeof(ulong);
            bool acquired = false;
            try
            {
                int result = Interop.Sys.LowLevelCrossProcessMutex_Acquire(sharedData, ThreadTestHelpers.UnexpectedTimeoutMilliseconds);
                acquired = result is 0 or OwnerDead;
                Assert.Equal(OwnerDead, result);
                WaitForRelease();
            }
            finally
            {
                int releaseResult = acquired ? Interop.Sys.LowLevelCrossProcessMutex_Release(sharedData) : 0;
                view.SafeMemoryMappedViewHandle.ReleasePointer();
                Assert.Equal(0, releaseResult);
            }

            void WaitForRelease()
            {
                // The ready file lets the original process start cleanup only after acquisition; the release
                // file keeps this process's native ownership intact until that cleanup has been checked.
                File.WriteAllText(releaseFilePath + ".ready", "ready");
                Assert.True(SpinWait.SpinUntil(
                    () => File.Exists(releaseFilePath),
                    ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
            }
        }

        private sealed class NativeMutexThreadState(Mutex mutex, ManualResetEventSlim callbackCompleted)
        {
            public Mutex Mutex { get; } = mutex;
            public ManualResetEventSlim CallbackCompleted { get; } = callbackCompleted;
            public Thread OwningThread { get; set; }
            public Exception CallbackException { get; set; }
        }

        private static unsafe void CreateNativeThread(NativeMutexThreadState state)
        {
            GCHandle handle = GCHandle.Alloc(state);
            bool created = false;
            try
            {
                created = Interop.Sys.CreateThread(1536 * 1024, &AcquireMutexOnNativeThread, GCHandle.ToIntPtr(handle));
                Assert.True(created);
            }
            finally
            {
                if (!created)
                {
                    handle.Free();
                }
            }
        }

        [UnmanagedCallersOnly]
        private static IntPtr AcquireMutexOnNativeThread(IntPtr parameter)
        {
            GCHandle handle = GCHandle.FromIntPtr(parameter);
            NativeMutexThreadState state = (NativeMutexThreadState)handle.Target;
            try
            {
                state.OwningThread = Thread.CurrentThread;
                Assert.True(state.Mutex.WaitOne(ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
            }
            catch (Exception ex)
            {
                state.CallbackException = ex;
            }
            finally
            {
                handle.Free();
                state.CallbackCompleted.Set();
            }

            return IntPtr.Zero;
        }

        // Keep construction out of the caller's frame so the blocker can become unreachable before GC.Collect.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CreateFinalizerBlocker(ManualResetEventSlim started, ManualResetEventSlim release) =>
            _ = new FinalizerBlocker(started, release);

        private sealed class FinalizerBlocker(ManualResetEventSlim started, ManualResetEventSlim release)
        {
            ~FinalizerBlocker()
            {
                started.Set();
                if (!release.Wait(ThreadTestHelpers.UnexpectedTimeoutMilliseconds))
                {
                    Environment.FailFast("Timed out waiting to unblock the finalizer thread.");
                }
            }
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/96191", TestPlatforms.Browser)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/117760",platforms: TestPlatforms.Android, runtimes: TestRuntimes.CoreCLR)]
        public void NamedMutex_ThreadExitDisposeRaceTest()
        {
            var mutexName = Guid.NewGuid().ToString("N");
            NamedWaitHandleOptions options = new() { CurrentUserOnly = false, CurrentSessionOnly = true };

            for (int i = 0; i < 1000; ++i)
            {
                var m = new Mutex(mutexName, options);
                var startParallelTest = new ManualResetEvent(false);

                var t0Ready = new AutoResetEvent(false);
                Thread t0 = ThreadTestHelpers.CreateGuardedThread(out Action waitForT0, () =>
                {
                    m.CheckedWait();
                    t0Ready.Set();
                    startParallelTest.CheckedWait(); // after this, exit T0
                });
                t0.IsBackground = true;

                var t1Ready = new AutoResetEvent(false);
                Thread t1 = ThreadTestHelpers.CreateGuardedThread(out Action waitForT1, () =>
                {
                    using (var m2 = Mutex.OpenExisting(mutexName, options))
                    {
                        m.Dispose();
                        t1Ready.Set();
                        startParallelTest.CheckedWait(); // after this, close last handle to named mutex, exit T1
                    }
                });
                t1.IsBackground = true;

                t0.Start();
                t0Ready.CheckedWait(); // wait for T0 to acquire the mutex
                t1.Start();
                t1Ready.CheckedWait(); // wait for T1 to open the existing mutex in a new mutex object and dispose one of the two

                // Release both threads at the same time. T0 will be exiting the thread, perhaps trying to abandon the mutex
                // that is still locked by it. In parallel, T1 will be disposing the last mutex instance, which would try to
                // destroy the mutex.
                startParallelTest.Set();
                waitForT0();
                waitForT1();

                // Create a new mutex object with the same name and acquire it. There can be a delay between Thread.Join() above
                // returning and for T0 to abandon its mutex, keep trying to also verify that the mutex object is actually
                // destroyed and created new again.
                SpinWait.SpinUntil(() =>
                {
                    using (m = new Mutex(true, mutexName, options, out bool createdNew))
                    {
                        if (createdNew)
                        {
                            m.ReleaseMutex();
                        }
                        return createdNew;
                    }
                }, ThreadTestHelpers.UnexpectedTimeoutMilliseconds);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMultithreadingSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/117760",platforms: TestPlatforms.Android, runtimes: TestRuntimes.CoreCLR)]
        public void NamedMutex_DisposeWhenLockedRaceTest()
        {
            var mutexName = Guid.NewGuid().ToString("N");
            var mutex2Name = mutexName + "_2";
            NamedWaitHandleOptions options = new() { CurrentUserOnly = false, CurrentSessionOnly = true };

            var waitsForThread = new Action[Environment.ProcessorCount];
            for (int i = 0; i < waitsForThread.Length; ++i)
            {
                var t = ThreadTestHelpers.CreateGuardedThread(out waitsForThread[i], () =>
                {
                    for (int i = 0; i < 1000; ++i)
                    {
                        // Create or open two mutexes with different names, acquire the lock if created, and dispose without
                        // releasing the lock. What may occasionally happen is, one thread T0 will acquire the lock, another
                        // thread T1 will open the same mutex, T0 will dispose its mutex while the lock is held, and T1 will
                        // then release the last reference to the mutex. On some implementations T1 may not be able to destroy
                        // the mutex when it is still locked by T0, or there may be potential for races in the sequence. This
                        // test only looks for errors from race conditions.
                        using (var mutex = new Mutex(true, mutexName, options))
                        {
                        }
                        using (var mutex = new Mutex(true, mutex2Name, options))
                        {
                        }
                    }
                });
                t.IsBackground = true;
                t.Start();
            }

            foreach (var waitForThread in waitsForThread)
            {
                waitForThread();
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMobile))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void NamedMutex_OtherEvent_NotCompatible()
        {
            using Mutex m = new Mutex(Guid.NewGuid().ToString("N"), options: default);
            using ManualResetEvent mre = new(false);

            Assert.Throws<PlatformNotSupportedException>(() => WaitHandle.WaitAny(new WaitHandle[] { m, mre }, 0));
        }

        private const string GlobalSharedMemoryDirectory = $"/tmp/.dotnet/shm/global";
        private const UnixFileMode AllUsersRwx =
            UnixFileMode.UserRead
            | UnixFileMode.UserWrite
            | UnixFileMode.UserExecute
            | UnixFileMode.GroupRead
            | UnixFileMode.GroupWrite
            | UnixFileMode.GroupExecute
            | UnixFileMode.OtherRead
            | UnixFileMode.OtherWrite
            | UnixFileMode.OtherExecute;
        private const UnixFileMode OwnerUserRwx =
            UnixFileMode.UserRead
            | UnixFileMode.UserWrite
            | UnixFileMode.UserExecute;

        [ConditionalTheory(nameof(IsRemoteExecutorAndCrossProcessNamedMutexSupported))]
        [InlineData(OwnerUserRwx)]
        [InlineData(AllUsersRwx)]
        [PlatformSpecific(TestPlatforms.Linux)]
        [UnsupportedOSPlatform("windows")]
        public void NamedMutex_ExistingSharedMemoryDirectories_PermissionsAreUnchanged(UnixFileMode directoryMode)
        {
            string userRuntimeDirectory = GetTestFilePath();
            Directory.CreateDirectory(userRuntimeDirectory);

            RemoteInvokeOptions remoteInvokeOptions = new();
            remoteInvokeOptions.StartInfo.EnvironmentVariables["XDG_RUNTIME_DIR"] = userRuntimeDirectory;

            RemoteExecutor.Invoke(
                (userRuntimeDirectory, mode) =>
                {
                    UnixFileMode directoryMode = Enum.Parse<UnixFileMode>(mode);
                    string[] directories =
                    {
                        userRuntimeDirectory,
                        Path.Combine(userRuntimeDirectory, ".dotnet"),
                        Path.Combine(userRuntimeDirectory, ".dotnet", "shm"),
                        Path.Combine(userRuntimeDirectory, ".dotnet", "shm", "global")
                    };
                    foreach (string directory in directories)
                    {
                        Directory.CreateDirectory(directory);
                        File.SetUnixFileMode(directory, directoryMode);
                    }

                    string name = $"Global\\{Guid.NewGuid():N}";
                    using Mutex mutex = new(false, name, out bool createdNew);
                    Assert.True(createdNew);
                    using Mutex openedMutex = Mutex.OpenExisting(name);
                    Assert.True(Mutex.TryOpenExisting(name, out Mutex existingMutex));
                    existingMutex.Dispose();

                    foreach (string directory in directories)
                    {
                        Assert.Equal(directoryMode, File.GetUnixFileMode(directory));
                    }
                },
                userRuntimeDirectory,
                directoryMode.ToString(),
                remoteInvokeOptions).Dispose();
        }

        [ConditionalTheory(nameof(IsRemoteExecutorAndCrossProcessNamedMutexSupported))]
        [InlineData(null)]
        [InlineData(false)]
        [InlineData(true)]
        [PlatformSpecific(TestPlatforms.Linux)]
        public void NamedMutex_DefaultScope_UsesExpectedSharedMemoryPath(bool? unsafeDefaultMutexScopeGlobal)
        {
            string userRuntimeDirectory = GetTestFilePath();
            Directory.CreateDirectory(userRuntimeDirectory);

            RemoteInvokeOptions remoteInvokeOptions = new();
            remoteInvokeOptions.StartInfo.EnvironmentVariables["XDG_RUNTIME_DIR"] = userRuntimeDirectory;
            if (unsafeDefaultMutexScopeGlobal.HasValue)
            {
                remoteInvokeOptions.RuntimeConfigurationOptions["System.Threading.Mutex.UnsafeDefaultMutexScopeGlobal"] =
                    unsafeDefaultMutexScopeGlobal.Value;
            }

            RemoteExecutor.Invoke(
                (userRuntimeDirectory, useGlobalScope) =>
                {
                    string name = Guid.NewGuid().ToString("N");
                    string mutexName = $"Global\\{name}";
                    string userPath = Path.Combine(userRuntimeDirectory, ".dotnet", "shm", "global", name);
                    string globalPath = Path.Combine(GlobalSharedMemoryDirectory, name);
                    bool isGlobalScope = bool.Parse(useGlobalScope);

                    using Mutex mutex = new(false, mutexName, out bool createdNew);
                    Assert.True(createdNew);
                    Assert.Equal(!isGlobalScope, File.Exists(userPath));
                    Assert.Equal(isGlobalScope, File.Exists(globalPath));

                    using Mutex openedMutex = Mutex.OpenExisting(mutexName);
                    Assert.True(Mutex.TryOpenExisting(mutexName, out Mutex existingMutex));
                    existingMutex.Dispose();
                },
                userRuntimeDirectory,
                (unsafeDefaultMutexScopeGlobal == true).ToString(),
                remoteInvokeOptions).Dispose();
        }

        [ConditionalFact(nameof(IsRemoteExecutorAndCrossProcessNamedMutexSupported))]
        [PlatformSpecific(TestPlatforms.Linux)]
        [UnsupportedOSPlatform("windows")]
        public void NamedMutex_UserScopedSharedMemoryFallbackPathIsCreated()
        {
            string homeDirectory = GetTestFilePath();
            Directory.CreateDirectory(homeDirectory);

            RemoteInvokeOptions remoteInvokeOptions = new();
            remoteInvokeOptions.StartInfo.EnvironmentVariables["HOME"] = homeDirectory;
            remoteInvokeOptions.StartInfo.EnvironmentVariables["XDG_RUNTIME_DIR"] = string.Empty;

            RemoteExecutor.Invoke(
                homeDirectory =>
                {
                    string sharedMemoryBaseDirectory = Path.Combine(
                        homeDirectory,
                        ".dotnet",
                        "corefx",
                        "Runtime",
                        "SharedMemory");
                    string name = Guid.NewGuid().ToString("N");
                    string sharedMemoryDirectory = Path.Combine(sharedMemoryBaseDirectory, ".dotnet", "shm", "global");
                    NamedWaitHandleOptions options =
                        new() { CurrentUserOnly = true, CurrentSessionOnly = false };
                    using Mutex mutex = new($"Global\\{name}", options);
                    Assert.True(File.Exists(Path.Combine(sharedMemoryDirectory, name)));
                    Assert.Equal(OwnerUserRwx, File.GetUnixFileMode(Path.Combine(sharedMemoryBaseDirectory, ".dotnet")));
                    Assert.Equal(OwnerUserRwx, File.GetUnixFileMode(Path.Combine(sharedMemoryBaseDirectory, ".dotnet", "shm")));
                    Assert.Equal(OwnerUserRwx, File.GetUnixFileMode(sharedMemoryDirectory));
                },
                homeDirectory,
                remoteInvokeOptions).Dispose();
        }

        [ConditionalFact(nameof(IsRemoteExecutorAndCrossProcessNamedMutexSupported))]
        [PlatformSpecific(TestPlatforms.Linux)]
        [UnsupportedOSPlatform("windows")]
        public void NamedMutex_SharedMemoryDirectoryReplacementDoesNotRedirectFileAccess()
        {
            string userRuntimeDirectory = GetTestFilePath();
            Directory.CreateDirectory(userRuntimeDirectory);

            RemoteInvokeOptions remoteInvokeOptions = new();
            remoteInvokeOptions.StartInfo.EnvironmentVariables["XDG_RUNTIME_DIR"] = userRuntimeDirectory;

            RemoteExecutor.Invoke(
                userRuntimeDirectory =>
                {
                    NamedWaitHandleOptions options =
                        new() { CurrentUserOnly = true, CurrentSessionOnly = false };
                    string firstName = Guid.NewGuid().ToString("N");

                    using Mutex firstMutex = new($"Global\\{firstName}", options);

                    string runtimeDirectory = Path.Combine(userRuntimeDirectory, ".dotnet");
                    string sharedMemoryDirectory = Path.Combine(runtimeDirectory, "shm");
                    string originalSharedMemoryDirectory = Path.Combine(runtimeDirectory, "shm-original");
                    Directory.Move(sharedMemoryDirectory, originalSharedMemoryDirectory);
                    Directory.CreateDirectory(Path.Combine(sharedMemoryDirectory, "global"), OwnerUserRwx);

                    string secondName = Guid.NewGuid().ToString("N");
                    using Mutex secondMutex = new($"Global\\{secondName}", options);

                    Assert.True(File.Exists(Path.Combine(originalSharedMemoryDirectory, "global", secondName)));
                    Assert.False(File.Exists(Path.Combine(sharedMemoryDirectory, "global", secondName)));
                },
                userRuntimeDirectory,
                remoteInvokeOptions).Dispose();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMobile))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [UnsupportedOSPlatform("windows")]
        public void NamedMutex_InvalidSharedMemoryHeaderVersion()
        {
            string name = Guid.NewGuid().ToString("N");
            string path = $"{GlobalSharedMemoryDirectory}/{name}";

            Directory.CreateDirectory(GlobalSharedMemoryDirectory, AllUsersRwx);
            using (FileStream fs = new(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 4096))
            using (BinaryWriter bw = new(fs))
            {
                bw.Write((byte)1); // Write the shared memory type (mutex)
                bw.Write((byte)2); // Write an invalid version number
                // Make the file large enough for a valid named mutex file and divisible by page size (it should always be under one page).
                fs.SetLength(Environment.SystemPageSize);

                // Try opening a mutex when we still have the file locked.
                AssertExtensions.ThrowsAny<WaitHandleCannotBeOpenedException, InvalidDataException>(() => new Mutex($"Global\\{name}", new NamedWaitHandleOptions { CurrentSessionOnly = false, CurrentUserOnly = false }));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMobile))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [UnsupportedOSPlatform("windows")]
        public void NamedMutex_SharedMemoryFileAlreadyOpen()
        {
            string name = Guid.NewGuid().ToString("N");
            string path = $"{GlobalSharedMemoryDirectory}/{name}";

            Directory.CreateDirectory(GlobalSharedMemoryDirectory, AllUsersRwx);
            // Take an exclusive file lock of the global shared memory file.
            using (FileStream fs = new(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096))
            using (BinaryWriter bw = new(fs))
            {
                bw.Write((byte)1); // Write the shared memory type (mutex)
                bw.Write((byte)1); // Write  valid version number
                // Make the file large enough for a valid named mutex file and divisible by page size (it should always be under one page).
                fs.SetLength(Environment.SystemPageSize);

                // Try opening a mutex when we still have the file locked.
                Assert.Throws<IOException>(() => new Mutex($"Global\\{name}", new NamedWaitHandleOptions { CurrentSessionOnly = false, CurrentUserOnly = false }));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMobile))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [UnsupportedOSPlatform("windows")]
        public void NamedMutex_InvalidSharedMemoryHeaderKind()
        {
            string name = Guid.NewGuid().ToString("N");
            string path = $"{GlobalSharedMemoryDirectory}/{name}";

            Directory.CreateDirectory(GlobalSharedMemoryDirectory, AllUsersRwx);
            using (FileStream fs = new(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 4096))
            using (BinaryWriter bw = new(fs))
            {
                bw.Write((byte)2); // Write the shared memory type (invalid)
                bw.Write((byte)1); // Write a version number
                // Make the file large enough for a valid named mutex file and divisible by page size (it should always be under one page).
                fs.SetLength(Environment.SystemPageSize);
                // Try opening a mutex when we still have the file locked.
                AssertExtensions.ThrowsAny<WaitHandleCannotBeOpenedException, InvalidDataException>(() => new Mutex($"Global\\{name}", new NamedWaitHandleOptions { CurrentSessionOnly = false, CurrentUserOnly = false }));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMobile))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [UnsupportedOSPlatform("windows")]
        public void NamedMutex_TooSmallSharedMemoryFile()
        {
            string name = Guid.NewGuid().ToString("N");
            string path = $"{GlobalSharedMemoryDirectory}/{name}";

            Directory.CreateDirectory(GlobalSharedMemoryDirectory, AllUsersRwx);
            using (FileStream fs = new(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite, 4096))
            using (BinaryWriter bw = new(fs))
            {
                bw.Write((byte)1); // Write the shared memory type (mutex)
                bw.Write((byte)1); // Write a valid version number
                // Make the file large enough for a valid named mutex file but not divisible by page size.
                fs.SetLength(Environment.SystemPageSize - 1);
                // Try opening a mutex when we still have the file locked.
                AssertExtensions.ThrowsAny<WaitHandleCannotBeOpenedException, InvalidDataException>(() => new Mutex($"Global\\{name}", new NamedWaitHandleOptions { CurrentSessionOnly = false, CurrentUserOnly = false }));
            }
        }

        public static TheoryData<string> GetValidNames()
        {
            var names = new TheoryData<string>() { Guid.NewGuid().ToString("N") };

            // Windows native named mutexes and in-proc named mutexes support very long (1000+ char) names.
            // Non-Windows cross-process named mutexes are emulated using file system. It imposes limit
            // on maximum name length.
            if (PlatformDetection.IsWindows || PlatformDetection.IsMobile)
                names.Add(Guid.NewGuid().ToString("N") + new string('a', 1000));

            return names;
        }

        private static IEnumerable<string> GetNamePrefixes()
        {
            yield return string.Empty;
            yield return "Local\\";
            yield return "Global\\";
        }

        public static IEnumerable<object[]> NamePrefixes_MemberData()
        {
            foreach (var namePrefix in GetNamePrefixes())
            {
                yield return new object[] { namePrefix };
            }
        }

        public static IEnumerable<NamedWaitHandleOptions> GetNameOptionCombinations()
        {
            yield return new NamedWaitHandleOptions() { CurrentUserOnly = false, CurrentSessionOnly = false };
            yield return new NamedWaitHandleOptions() { CurrentUserOnly = false, CurrentSessionOnly = true };
            yield return new NamedWaitHandleOptions() { CurrentUserOnly = true, CurrentSessionOnly = false };
            yield return new NamedWaitHandleOptions() { CurrentUserOnly = true, CurrentSessionOnly = true };
        }

        public static IEnumerable<object[]> NameOptionCombinations_MemberData()
        {
            foreach (NamedWaitHandleOptions options in GetNameOptionCombinations())
            {
                yield return new object[] { options.CurrentUserOnly, options.CurrentSessionOnly };
            }
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
