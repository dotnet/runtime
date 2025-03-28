// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Threading.Tests
{
    public class SemaphoreTests
    {
        private const int FailedWaitTimeout = 30000;

        [Fact]
        public void ConstructorAndDisposeTest()
        {
            var s = new Semaphore(0, 1);
            Assert.False(s.WaitOne(0));
            s.Dispose();
            Assert.Throws<ObjectDisposedException>(() => s.Release());
            Assert.Throws<ObjectDisposedException>(() => s.WaitOne(0));

            s = new Semaphore(1, 1);
            Assert.True(s.WaitOne(0));
            s.Dispose();
        }

        [Fact]
        public void SignalAndUnsignalTest()
        {
            var s = new Semaphore(0, 1);
            Assert.Throws<ArgumentOutOfRangeException>(() => s.Release(0));

            s = new Semaphore(1, 1);
            Assert.True(s.WaitOne(0));
            Assert.False(s.WaitOne(0));
            Assert.False(s.WaitOne(0));
            Assert.Equal(0, s.Release());
            Assert.True(s.WaitOne(0));
            Assert.Throws<SemaphoreFullException>(() => s.Release(2));
            Assert.Equal(0, s.Release());
            Assert.Throws<SemaphoreFullException>(() => s.Release());

            s = new Semaphore(1, 2);
            Assert.Throws<SemaphoreFullException>(() => s.Release(2));
            Assert.Equal(1, s.Release(1));
            Assert.True(s.WaitOne(0));
            Assert.True(s.WaitOne(0));
            Assert.Throws<SemaphoreFullException>(() => s.Release(3));
            Assert.Equal(0, s.Release(2));
            Assert.Throws<SemaphoreFullException>(() => s.Release());
        }

        [Fact]
        public void WaitTest()
        {
            var s = new Semaphore(1, 2);
            s.CheckedWait();
            Assert.False(s.WaitOne(0));
            s.Release();
            s.CheckedWait();
            Assert.False(s.WaitOne(0));
            s.Release(2);
            s.CheckedWait();
            s.Release();
            s.CheckedWait();

            s = new Semaphore(0, 2);
            Assert.False(s.WaitOne(ThreadTestHelpers.ExpectedTimeoutMilliseconds));
        }

        [Fact]
        public void MultiWaitWithAllIndexesSignaledTest()
        {
            var ss =
                new Semaphore[]
                {
                    new Semaphore(1, 1),
                    new Semaphore(1, 1),
                    new Semaphore(1, 1),
                    new Semaphore(1, 1)
                };
            Assert.Equal(0, WaitHandle.WaitAny(ss, 0));
            for (int i = 0; i < ss.Length; ++i)
            {
                Assert.Equal(i > 0, ss[i].WaitOne(0));
                ss[i].Release();
            }
            Assert.Equal(0, WaitHandle.WaitAny(ss, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
            for (int i = 0; i < ss.Length; ++i)
            {
                Assert.Equal(i > 0, ss[i].WaitOne(0));
                ss[i].Release();
            }
            Assert.Equal(0, WaitHandle.WaitAny(ss));
            for (int i = 0; i < ss.Length; ++i)
            {
                Assert.Equal(i > 0, ss[i].WaitOne(0));
                ss[i].Release();
            }
            Assert.True(WaitHandle.WaitAll(ss, 0));
            for (int i = 0; i < ss.Length; ++i)
            {
                Assert.False(ss[i].WaitOne(0));
                ss[i].Release();
            }
            Assert.True(WaitHandle.WaitAll(ss, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
            for (int i = 0; i < ss.Length; ++i)
            {
                Assert.False(ss[i].WaitOne(0));
                ss[i].Release();
            }
            Assert.True(WaitHandle.WaitAll(ss));
            for (int i = 0; i < ss.Length; ++i)
            {
                Assert.False(ss[i].WaitOne(0));
            }
        }

        [Fact]
        public void MultiWaitWithInnerIndexesSignaled()
        {
            var ss =
                new Semaphore[]
                {
                    new Semaphore(0, 1),
                    new Semaphore(1, 1),
                    new Semaphore(1, 1),
                    new Semaphore(0, 1)
                };
            Assert.Equal(1, WaitHandle.WaitAny(ss, 0));
            for (int i = 0; i < ss.Length; ++i)
            {
                Assert.Equal(i == 2, ss[i].WaitOne(0));
            }
            ss[1].Release();
            ss[2].Release();
            Assert.Equal(1, WaitHandle.WaitAny(ss, ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
            for (int i = 0; i < ss.Length; ++i)
            {
                Assert.Equal(i == 2, ss[i].WaitOne(0));
            }
            ss[1].Release();
            ss[2].Release();
            Assert.False(WaitHandle.WaitAll(ss, 0));
            Assert.False(WaitHandle.WaitAll(ss, ThreadTestHelpers.ExpectedTimeoutMilliseconds));
            for (int i = 0; i < ss.Length; ++i)
            {
                Assert.Equal(i == 1 || i == 2, ss[i].WaitOne(0));
            }
        }

        [Fact]
        public void MultiWaitWithAllIndexesUnsignaled()
        {
            var ss =
                new Semaphore[]
                {
                    new Semaphore(0, 1),
                    new Semaphore(0, 1),
                    new Semaphore(0, 1),
                    new Semaphore(0, 1)
                };
            Assert.Equal(WaitHandle.WaitTimeout, WaitHandle.WaitAny(ss, 0));
            Assert.Equal(WaitHandle.WaitTimeout, WaitHandle.WaitAny(ss, ThreadTestHelpers.ExpectedTimeoutMilliseconds));
            Assert.False(WaitHandle.WaitAll(ss, 0));
            Assert.False(WaitHandle.WaitAll(ss, ThreadTestHelpers.ExpectedTimeoutMilliseconds));
            for (int i = 0; i < ss.Length; ++i)
            {
                Assert.False(ss[i].WaitOne(0));
            }
        }

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

        [PlatformSpecific(TestPlatforms.Windows)]  // named semaphores aren't supported on Unix
        [Theory]
        [MemberData(nameof(GetValidNames))]
        public void Ctor_ValidName_Windows(string name)
        {
            new Semaphore(0, 1, name, options: default).Dispose();

            bool createdNew;
            using var s = new Semaphore(0, 1, name, options: default, out createdNew);
            Assert.True(createdNew);
            new Semaphore(0, 1, name, options: default, out createdNew).Dispose();
            Assert.False(createdNew);
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]  // named semaphores aren't supported on Unix
        [ActiveIssue("https://github.com/mono/mono/issues/15161", TestRuntimes.Mono)]
        [Fact]
        public void Ctor_NamesArentSupported_Unix()
        {
            Assert.Throws<PlatformNotSupportedException>(() => new Semaphore(0, 1, "anything"));
            Assert.Throws<PlatformNotSupportedException>(() => new Semaphore(0, 1, "anything", options: default));
            Assert.Throws<PlatformNotSupportedException>(() => new Semaphore(0, 1, "anything", out _));
            Assert.Throws<PlatformNotSupportedException>(() => new Semaphore(0, 1, "anything", options: default, out _));
        }

        [Fact]
        public void Ctor_InvalidArguments()
        {
            bool createdNew;

            AssertExtensions.Throws<ArgumentOutOfRangeException>("initialCount", () => new Semaphore(-1, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("initialCount", () => new Semaphore(-2, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("maximumCount", () => new Semaphore(0, 0));
            AssertExtensions.Throws<ArgumentException>(null, () => new Semaphore(2, 1));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("initialCount", () => new Semaphore(-1, 1, null, options: default));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("initialCount", () => new Semaphore(-2, 1, null, options: default));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("maximumCount", () => new Semaphore(0, 0, null, options: default));
            AssertExtensions.Throws<ArgumentException>(null, () => new Semaphore(2, 1, null));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("initialCount", () => new Semaphore(-1, 1, "CtorSemaphoreTest", options: default, out createdNew));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("initialCount", () => new Semaphore(-2, 1, "CtorSemaphoreTest", options: default, out createdNew));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("maximumCount", () => new Semaphore(0, 0, "CtorSemaphoreTest", options: default, out createdNew));
            AssertExtensions.Throws<ArgumentException>(null, () => new Semaphore(2, 1, "CtorSemaphoreTest", options: default, out createdNew));
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
            using (Semaphore s = new Semaphore(0, int.MaxValue))
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49890", TestPlatforms.Android)]
        public void AnonymousProducerConsumer()
        {
            using (Semaphore s = new Semaphore(0, int.MaxValue))
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

        [PlatformSpecific(TestPlatforms.Windows)] // named semaphores aren't supported on Unix
        [Theory]
        [MemberData(nameof(MutexTests.NameOptionCombinations_MemberData), MemberType = typeof(MutexTests))]
        public void NamedProducerConsumer(bool currentUserOnly, bool currentSessionOnly)
        {
            string name = Guid.NewGuid().ToString("N");
            NamedWaitHandleOptions options =
                new() { CurrentUserOnly = currentUserOnly, CurrentSessionOnly = currentSessionOnly };
            const int NumItems = 5;
            var b = new Barrier(2);
            Task.WaitAll(
                Task.Factory.StartNew(() =>
                {
                    using (var s = new Semaphore(0, int.MaxValue, name, options))
                    {
                        Assert.True(b.SignalAndWait(FailedWaitTimeout));
                        for (int i = 0; i < NumItems; i++)
                            Assert.True(s.WaitOne(FailedWaitTimeout));
                        Assert.False(s.WaitOne(0));
                    }
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default),
                Task.Factory.StartNew(() =>
                {
                    using (var s = new Semaphore(0, int.MaxValue, name, options))
                    {
                        Assert.True(b.SignalAndWait(FailedWaitTimeout));
                        for (int i = 0; i < NumItems; i++)
                            s.Release();
                    }
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default));
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]  // named semaphores aren't supported on Unix
        [ActiveIssue("https://github.com/mono/mono/issues/15160", TestRuntimes.Mono)]
        [Fact]
        public void OpenExisting_NotSupported_Unix()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Semaphore.OpenExisting("anything"));
            Assert.Throws<PlatformNotSupportedException>(() => Semaphore.OpenExisting("anything", options: default));
            Assert.Throws<PlatformNotSupportedException>(() => Semaphore.TryOpenExisting("anything", out _));
            Assert.Throws<PlatformNotSupportedException>(() => Semaphore.TryOpenExisting("anything", options: default, out _));
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // named semaphores aren't supported on Unix
        [Fact]
        public void OpenExisting_InvalidNames_Windows()
        {
            AssertExtensions.Throws<ArgumentNullException>("name", () => Semaphore.OpenExisting(null));
            AssertExtensions.Throws<ArgumentNullException>("name", () => Semaphore.OpenExisting(null, options: default));
            AssertExtensions.Throws<ArgumentException>("name", null, () => Semaphore.OpenExisting(string.Empty));
            AssertExtensions.Throws<ArgumentException>("name", null, () => Semaphore.OpenExisting(string.Empty, options: default));
        }

        [PlatformSpecific(TestPlatforms.Windows)] // named semaphores aren't supported on Unix
        [Fact]
        public void OpenExisting_UnavailableName_Windows()
        {
            string name = Guid.NewGuid().ToString("N");
            Assert.Throws<WaitHandleCannotBeOpenedException>(() => Semaphore.OpenExisting(name, options: default));
            Semaphore s;
            Assert.False(Semaphore.TryOpenExisting(name, options: default, out s));
            Assert.Null(s);

            using (s = new Semaphore(0, 1, name, options: default)) { }
            Assert.Throws<WaitHandleCannotBeOpenedException>(() => Semaphore.OpenExisting(name, options: default));
            Assert.False(Semaphore.TryOpenExisting(name, options: default, out s));
            Assert.Null(s);
        }

        [PlatformSpecific(TestPlatforms.Windows)] // named semaphores aren't supported on Unix
        [Theory]
        [MemberData(nameof(MutexTests.NameOptionCombinations_MemberData), MemberType = typeof(MutexTests))]
        public void NameUsedByOtherSynchronizationPrimitive_Windows(bool currentUserOnly, bool currentSessionOnly)
        {
            string name = Guid.NewGuid().ToString("N");
            NamedWaitHandleOptions options =
                new() { CurrentUserOnly = currentUserOnly, CurrentSessionOnly = currentSessionOnly };
            using (Mutex mtx = new Mutex(true, name, options))
            {
                Assert.Throws<WaitHandleCannotBeOpenedException>(() => new Semaphore(initialCount: 1, maximumCount: 1, name, options));
                Assert.Throws<WaitHandleCannotBeOpenedException>(() => Semaphore.OpenExisting(name, options));
                Assert.False(Semaphore.TryOpenExisting(name, options, out _));
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)] // named semaphores aren't supported on Unix
        [Theory]
        [MemberData(nameof(GetValidNames))]
        public void OpenExisting_SameAsOriginal_Windows(string name)
        {
            bool createdNew;
            using (Semaphore s1 = new Semaphore(0, int.MaxValue, name, options: default, out createdNew))
            {
                Assert.True(createdNew);

                using (Semaphore s2 = Semaphore.OpenExisting(name, options: default))
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
                Assert.True(Semaphore.TryOpenExisting(name, options: default, out s3));
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

        [Theory]
        [MemberData(nameof(MutexTests.NamePrefixes_MemberData), MemberType = typeof(MutexTests))]
        [PlatformSpecific(TestPlatforms.Windows)] // names aren't supported on Unix
        public void NameOptionsApiCompatibilityTest(string namePrefix)
        {
            string name = Guid.NewGuid().ToString("N");
            string prefixedName = namePrefix + name;
            bool currentSessionOnly = namePrefix != @"Global\";

            using (var s = new Semaphore(initialCount: 1, maximumCount: 1, prefixedName, out bool createdNew))
            {
                Assert.True(createdNew);

                new Semaphore(
                    initialCount: 1,
                    maximumCount: 1,
                    name,
                    new() { CurrentUserOnly = false, CurrentSessionOnly = currentSessionOnly },
                    out createdNew).Dispose();
                Assert.False(createdNew);

                Semaphore.OpenExisting(
                    name,
                    new() { CurrentUserOnly = false, CurrentSessionOnly = currentSessionOnly }).Dispose();

                Assert.True(
                    Semaphore.TryOpenExisting(
                        name,
                        new() { CurrentUserOnly = false, CurrentSessionOnly = currentSessionOnly },
                        out Semaphore s2));
                s2.Dispose();
            }

            using (var s =
                new Semaphore(
                    initialCount: 1,
                    maximumCount: 1,
                    name,
                    new() { CurrentUserOnly = false, CurrentSessionOnly = currentSessionOnly },
                    out bool createdNew))
            {
                Assert.True(createdNew);

                new Semaphore(initialCount: 1, maximumCount: 1, prefixedName, out createdNew).Dispose();
                Assert.False(createdNew);

                Semaphore.OpenExisting(prefixedName).Dispose();

                Assert.True(Semaphore.TryOpenExisting(prefixedName, out Semaphore s2));
                s2.Dispose();
            }
        }

        [Theory]
        [MemberData(nameof(MutexTests.NamePrefixAndOptionsCompatibilityTest_MemberData), MemberType = typeof(MutexTests))]
        [PlatformSpecific(TestPlatforms.Windows)] // names aren't supported on Unix
        public void NamePrefixAndOptionsCompatibilityTest(bool currentUserOnly, bool currentSessionOnly, string namePrefix)
        {
            string name = namePrefix + Guid.NewGuid().ToString("N");
            bool currentSessionOnlyBasedOnPrefix = namePrefix != @"Global\";
            NamedWaitHandleOptions options =
                new() { CurrentUserOnly = currentUserOnly, CurrentSessionOnly = currentSessionOnly };

            if (string.IsNullOrEmpty(namePrefix) || currentSessionOnlyBasedOnPrefix == currentSessionOnly)
            {
                new Semaphore(initialCount: 1, maximumCount: 1, name, options).Dispose();
            }
            else
            {
                AssertExtensions.Throws<ArgumentException>("name",
                    () => new Semaphore(initialCount: 1, maximumCount: 1, name, options));
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoNorServerCore))] // Windows Nano Server and Server Core apparently use the same namespace for the Local\ and Global\ prefixes
        [MemberData(nameof(MutexTests.NameNamespaceTests_MemberData), MemberType = typeof(MutexTests))]
        [PlatformSpecific(TestPlatforms.Windows)] // names aren't supported on Unix
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

            using (var s = new Semaphore(initialCount: 1, maximumCount: 1, name, createOptions))
            {
                if (PlatformDetection.IsWindows &&
                    openOptions.CurrentSessionOnly == createOptions.CurrentSessionOnly &&
                    !createOptions.CurrentUserOnly &&
                    openOptions.CurrentUserOnly)
                {
                    Assert.Throws<WaitHandleCannotBeOpenedException>(
                        () => new Semaphore(initialCount: 1, maximumCount: 1, name, openOptions));
                    Assert.Throws<WaitHandleCannotBeOpenedException>(() => Semaphore.OpenExisting(name, openOptions));
                    Assert.False(Semaphore.TryOpenExisting(name, openOptions, out _));
                    return;
                }

                bool sameOptions =
                    openOptions.CurrentUserOnly == createOptions.CurrentUserOnly &&
                    openOptions.CurrentSessionOnly == createOptions.CurrentSessionOnly;
                bool expectedCreatedNew =
                    !sameOptions &&
                    (!PlatformDetection.IsWindows || openOptions.CurrentSessionOnly != createOptions.CurrentSessionOnly);

                new Semaphore(initialCount: 1, maximumCount: 1, name, openOptions, out bool createdNew).Dispose();
                Assert.Equal(expectedCreatedNew, createdNew);

                if (expectedCreatedNew)
                {
                    Assert.Throws<WaitHandleCannotBeOpenedException>(() => Semaphore.OpenExisting(name, openOptions));
                }
                else
                {
                    Semaphore.OpenExisting(name, openOptions).Dispose();
                }

                Assert.Equal(!expectedCreatedNew, Semaphore.TryOpenExisting(name, openOptions, out Semaphore s2));
                s2?.Dispose();
            }
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [MemberData(nameof(MutexTests.NameOptionCombinations_MemberData), MemberType = typeof(MutexTests))]
        [PlatformSpecific(TestPlatforms.Windows)] // names aren't supported on Unix
        public void PingPong(bool currentUserOnly, bool currentSessionOnly)
        {
            // Create names for the two semaphores
            string outboundName = Guid.NewGuid().ToString("N");
            string inboundName = Guid.NewGuid().ToString("N");

            NamedWaitHandleOptions options =
                new() { CurrentUserOnly = currentUserOnly, CurrentSessionOnly = currentSessionOnly };

            // Create the two semaphores and the other process with which to synchronize
            using (var inbound = new Semaphore(1, 1, inboundName, options))
            using (var outbound = new Semaphore(0, 1, outboundName, options))
            using (var remote =
                RemoteExecutor.Invoke(
                    PingPong_OtherProcess,
                    outboundName,
                    inboundName,
                    options.CurrentUserOnly ? "1" : "0",
                    options.CurrentSessionOnly ? "1" : "0"))
            {
                // Repeatedly wait for count in one semaphore and then release count into the other
                for (int i = 0; i < 10; i++)
                {
                    Assert.True(inbound.WaitOne(RemoteExecutor.FailWaitTimeoutMilliseconds));
                    outbound.Release();
                }
            }
        }

        private static void PingPong_OtherProcess(
            string inboundName,
            string outboundName,
            string currentUserOnlyStr,
            string currentSessionOnlyStr)
        {
            NamedWaitHandleOptions options =
                new()
                {
                    CurrentUserOnly = int.Parse(currentUserOnlyStr) != 0,
                    CurrentSessionOnly = int.Parse(currentSessionOnlyStr) != 0
                };

            // Open the two semaphores
            using (var inbound = Semaphore.OpenExisting(inboundName, options))
            using (var outbound = Semaphore.OpenExisting(outboundName, options))
            {
                // Repeatedly wait for count in one semaphore and then release count into the other
                for (int i = 0; i < 10; i++)
                {
                    Assert.True(inbound.WaitOne(RemoteExecutor.FailWaitTimeoutMilliseconds));
                    outbound.Release();
                }
            }
        }

        public static TheoryData<string> GetValidNames()
        {
            var names  =  new TheoryData<string>() { Guid.NewGuid().ToString("N") };
            names.Add(Guid.NewGuid().ToString("N") + new string('a', 1000));

            return names;
        }
    }
}
