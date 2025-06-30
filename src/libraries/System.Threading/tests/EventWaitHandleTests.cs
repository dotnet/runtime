// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Security;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Threading.Tests
{
    public class EventWaitHandleTests
    {
        [Theory]
        [InlineData(false, EventResetMode.AutoReset)]
        [InlineData(false, EventResetMode.ManualReset)]
        [InlineData(true, EventResetMode.AutoReset)]
        [InlineData(true, EventResetMode.ManualReset)]
        public void Ctor_StateMode(bool initialState, EventResetMode mode)
        {
            using (var ewh = new EventWaitHandle(initialState, mode))
                Assert.Equal(initialState, ewh.WaitOne(0));
        }

        [Fact]
        public void Ctor_InvalidMode()
        {
            AssertExtensions.Throws<ArgumentException>("mode", null, () => new EventWaitHandle(true, (EventResetMode)12345));
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // names aren't supported on Unix
        [Theory]
        [MemberData(nameof(GetValidNames))]
        public void Ctor_ValidNames(string name)
        {
            bool createdNew;
            using (var ewh = new EventWaitHandle(true, EventResetMode.AutoReset, name, options: default, out createdNew))
            {
                Assert.True(createdNew);
            }
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]  // names aren't supported on Unix
        [Fact]
        public void Ctor_NamesArentSupported_Unix()
        {
            Assert.Throws<PlatformNotSupportedException>(() => new EventWaitHandle(false, EventResetMode.AutoReset, "anything"));
            Assert.Throws<PlatformNotSupportedException>(() => new EventWaitHandle(false, EventResetMode.AutoReset, "anything", options: default));
            Assert.Throws<PlatformNotSupportedException>(() => new EventWaitHandle(false, EventResetMode.AutoReset, "anything", out _));
            Assert.Throws<PlatformNotSupportedException>(() => new EventWaitHandle(false, EventResetMode.AutoReset, "anything", options: default, out _));
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // names aren't supported on Unix
        [Theory]
        [InlineData(false, EventResetMode.AutoReset)]
        [InlineData(false, EventResetMode.ManualReset)]
        [InlineData(true, EventResetMode.AutoReset)]
        [InlineData(true, EventResetMode.ManualReset)]
        public void Ctor_StateModeNameCreatedNew_Windows(bool initialState, EventResetMode mode)
        {
            string name = Guid.NewGuid().ToString("N");
            bool createdNew;
            using (var ewh = new EventWaitHandle(initialState, mode, name, options: default, out createdNew))
            {
                Assert.True(createdNew);
                using (new EventWaitHandle(initialState, mode, name, options: default, out createdNew))
                {
                    Assert.False(createdNew);
                }
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)] // names aren't supported on Unix
        [Theory]
        [MemberData(nameof(MutexTests.NameOptionCombinations_MemberData), MemberType = typeof(MutexTests))]
        public void NameUsedByOtherSynchronizationPrimitive_Windows(bool currentUserOnly, bool currentSessionOnly)
        {
            string name = Guid.NewGuid().ToString("N");
            NamedWaitHandleOptions options =
                new() { CurrentUserOnly = currentUserOnly, CurrentSessionOnly = currentSessionOnly };
            using (Mutex m = new Mutex(name, options))
            {
                Assert.Throws<WaitHandleCannotBeOpenedException>(() => new EventWaitHandle(false, EventResetMode.AutoReset, name, options));
                Assert.Throws<WaitHandleCannotBeOpenedException>(() => new EventWaitHandle(false, EventResetMode.ManualReset, name, options));
                Assert.Throws<WaitHandleCannotBeOpenedException>(() => EventWaitHandle.OpenExisting(name, options));
                Assert.False(EventWaitHandle.TryOpenExisting(name, options, out _));
            }
        }

        [Fact]
        public void SetReset()
        {
            using (EventWaitHandle are = new EventWaitHandle(false, EventResetMode.AutoReset))
            {
                Assert.False(are.WaitOne(0));
                Assert.False(are.WaitOne(ThreadTestHelpers.ExpectedTimeoutMilliseconds));
                are.Set();
                Assert.True(are.WaitOne(0));
                Assert.False(are.WaitOne(0));
                Assert.False(are.WaitOne(ThreadTestHelpers.ExpectedTimeoutMilliseconds));
                are.Set();
                are.Reset();
                Assert.False(are.WaitOne(0));
                Assert.False(are.WaitOne(ThreadTestHelpers.ExpectedTimeoutMilliseconds));
            }

            using (EventWaitHandle mre = new EventWaitHandle(false, EventResetMode.ManualReset))
            {
                Assert.False(mre.WaitOne(0));
                Assert.False(mre.WaitOne(ThreadTestHelpers.ExpectedTimeoutMilliseconds));
                mre.Set();
                Assert.True(mre.WaitOne(0));
                Assert.True(mre.WaitOne(0));
                mre.Set();
                Assert.True(mre.WaitOne(0));
                mre.Reset();
                Assert.False(mre.WaitOne(0));
                Assert.False(mre.WaitOne(ThreadTestHelpers.ExpectedTimeoutMilliseconds));
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // OpenExisting not supported on Unix
        [Theory]
        [MemberData(nameof(GetValidNames))]
        public void OpenExisting_Windows(string name)
        {
            EventWaitHandle resultHandle;
            Assert.False(EventWaitHandle.TryOpenExisting(name, options: default, out resultHandle));
            Assert.Null(resultHandle);

            using (EventWaitHandle are1 = new EventWaitHandle(false, EventResetMode.AutoReset, name, options: default))
            {
                using (EventWaitHandle are2 = EventWaitHandle.OpenExisting(name, options: default))
                {
                    are1.Set();
                    Assert.True(are2.WaitOne(0));
                    Assert.False(are1.WaitOne(0));
                    Assert.False(are2.WaitOne(0));

                    are2.Set();
                    Assert.True(are1.WaitOne(0));
                    Assert.False(are2.WaitOne(0));
                    Assert.False(are1.WaitOne(0));
                }

                Assert.True(EventWaitHandle.TryOpenExisting(name, options: default, out resultHandle));
                Assert.NotNull(resultHandle);
                resultHandle.Dispose();
            }
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]  // OpenExisting not supported on Unix
        [Fact]
        public void OpenExisting_NotSupported_Unix()
        {
            Assert.Throws<PlatformNotSupportedException>(() => EventWaitHandle.OpenExisting("anything"));
            Assert.Throws<PlatformNotSupportedException>(() => EventWaitHandle.OpenExisting("anything", options: default));
            Assert.Throws<PlatformNotSupportedException>(() => EventWaitHandle.TryOpenExisting("anything", out _));
            Assert.Throws<PlatformNotSupportedException>(() => EventWaitHandle.TryOpenExisting("anything", options: default, out _));
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // OpenExisting not supported on Unix
        [Fact]
        public void OpenExisting_InvalidNames_Windows()
        {
            AssertExtensions.Throws<ArgumentNullException>("name", () => EventWaitHandle.OpenExisting(null));
            AssertExtensions.Throws<ArgumentNullException>("name", () => EventWaitHandle.OpenExisting(null, options: default));
            AssertExtensions.Throws<ArgumentException>("name", null, () => EventWaitHandle.OpenExisting(string.Empty));
            AssertExtensions.Throws<ArgumentException>("name", null, () => EventWaitHandle.OpenExisting(string.Empty, options: default));
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // OpenExisting not supported on Unix
        [Fact]
        public void OpenExisting_UnavailableName_Windows()
        {
            string name = Guid.NewGuid().ToString("N");
            Assert.Throws<WaitHandleCannotBeOpenedException>(() => EventWaitHandle.OpenExisting(name, options: default));
            EventWaitHandle e;
            Assert.False(EventWaitHandle.TryOpenExisting(name, options: default, out e));
            Assert.Null(e);

            using (e = new EventWaitHandle(false, EventResetMode.AutoReset, name, options: default)) { }
            Assert.Throws<WaitHandleCannotBeOpenedException>(() => EventWaitHandle.OpenExisting(name, options: default));
            Assert.False(EventWaitHandle.TryOpenExisting(name, options: default, out e));
            Assert.Null(e);
        }

        [Theory]
        [MemberData(nameof(MutexTests.NamePrefixes_MemberData), MemberType = typeof(MutexTests))]
        [PlatformSpecific(TestPlatforms.Windows)] // names aren't supported on Unix
        public void NameOptionsApiCompatibilityTest(string namePrefix)
        {
            string name = Guid.NewGuid().ToString("N");
            string prefixedName = namePrefix + name;
            bool currentSessionOnly = namePrefix != @"Global\";

            using (var e =
                new EventWaitHandle(initialState: false, EventResetMode.AutoReset, prefixedName, out bool createdNew))
            {
                Assert.True(createdNew);

                new EventWaitHandle(
                    initialState: false,
                    EventResetMode.AutoReset,
                    name,
                    new() { CurrentUserOnly = false, CurrentSessionOnly = currentSessionOnly },
                    out createdNew).Dispose();
                Assert.False(createdNew);

                EventWaitHandle.OpenExisting(
                    name,
                    new() { CurrentUserOnly = false, CurrentSessionOnly = currentSessionOnly }).Dispose();

                Assert.True(
                    EventWaitHandle.TryOpenExisting(
                        name,
                        new() { CurrentUserOnly = false, CurrentSessionOnly = currentSessionOnly },
                        out EventWaitHandle e2));
                e2.Dispose();
            }

            using (var e =
                new EventWaitHandle(
                    initialState: false,
                    EventResetMode.AutoReset,
                    name,
                    new() { CurrentUserOnly = false, CurrentSessionOnly = currentSessionOnly },
                    out bool createdNew))
            {
                Assert.True(createdNew);

                new EventWaitHandle(initialState: false, EventResetMode.AutoReset, prefixedName, out createdNew).Dispose();
                Assert.False(createdNew);

                EventWaitHandle.OpenExisting(prefixedName).Dispose();

                Assert.True(EventWaitHandle.TryOpenExisting(prefixedName, out EventWaitHandle e2));
                e2.Dispose();
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
                new EventWaitHandle(initialState: false, EventResetMode.AutoReset, name, options).Dispose();
            }
            else
            {
                AssertExtensions.Throws<ArgumentException>("name",
                    () => new EventWaitHandle(initialState: false, EventResetMode.AutoReset, name, options));
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

            using (var e = new EventWaitHandle(initialState: false, EventResetMode.AutoReset, name, createOptions))
            {
                if (PlatformDetection.IsWindows &&
                    openOptions.CurrentSessionOnly == createOptions.CurrentSessionOnly &&
                    !createOptions.CurrentUserOnly &&
                    openOptions.CurrentUserOnly)
                {
                    Assert.Throws<WaitHandleCannotBeOpenedException>(
                        () => new EventWaitHandle(initialState: false, EventResetMode.AutoReset, name, openOptions));
                    Assert.Throws<WaitHandleCannotBeOpenedException>(() => EventWaitHandle.OpenExisting(name, openOptions));
                    Assert.False(EventWaitHandle.TryOpenExisting(name, openOptions, out _));
                    return;
                }

                bool sameOptions =
                    openOptions.CurrentUserOnly == createOptions.CurrentUserOnly &&
                    openOptions.CurrentSessionOnly == createOptions.CurrentSessionOnly;
                bool expectedCreatedNew =
                    !sameOptions &&
                    (!PlatformDetection.IsWindows || openOptions.CurrentSessionOnly != createOptions.CurrentSessionOnly);

                new EventWaitHandle(
                    initialState: false,
                    EventResetMode.AutoReset,
                    name,
                    openOptions,
                    out bool createdNew).Dispose();
                Assert.Equal(expectedCreatedNew, createdNew);

                if (expectedCreatedNew)
                {
                    Assert.Throws<WaitHandleCannotBeOpenedException>(() => EventWaitHandle.OpenExisting(name, openOptions));
                }
                else
                {
                    EventWaitHandle.OpenExisting(name, openOptions).Dispose();
                }

                Assert.Equal(!expectedCreatedNew, EventWaitHandle.TryOpenExisting(name, openOptions, out EventWaitHandle e2));
                e2?.Dispose();
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)] // names aren't supported on Unix
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [MemberData(nameof(NameOptionsAndEventResetModeCombinations_MemberData))]
        public void PingPong(bool currentUserOnly, bool currentSessionOnly, EventResetMode mode)
        {
            // Create names for the two events
            string outboundName = Guid.NewGuid().ToString("N");
            string inboundName = Guid.NewGuid().ToString("N");

            NamedWaitHandleOptions options =
                new() { CurrentUserOnly = currentUserOnly, CurrentSessionOnly = currentSessionOnly };

            // Create the two events and the other process with which to synchronize
            using (var inbound = new EventWaitHandle(true, mode, inboundName, options))
            using (var outbound = new EventWaitHandle(false, mode, outboundName, options))
            using (var remote =
                RemoteExecutor.Invoke(
                    PingPong_OtherProcess,
                    mode.ToString(),
                    outboundName,
                    inboundName,
                    options.CurrentUserOnly ? "1" : "0",
                    options.CurrentSessionOnly ? "1" : "0"))
            {
                // Repeatedly wait for one event and then set the other
                for (int i = 0; i < 10; i++)
                {
                    Assert.True(inbound.WaitOne(RemoteExecutor.FailWaitTimeoutMilliseconds));
                    if (mode == EventResetMode.ManualReset)
                    {
                        inbound.Reset();
                    }
                    outbound.Set();
                }
            }
        }

        private static void PingPong_OtherProcess(
            string modeName,
            string inboundName,
            string outboundName,
            string currentUserOnlyStr,
            string currentSessionOnlyStr)
        {
            EventResetMode mode = (EventResetMode)Enum.Parse(typeof(EventResetMode), modeName);
            NamedWaitHandleOptions options =
                new()
                {
                    CurrentUserOnly = int.Parse(currentUserOnlyStr) != 0,
                    CurrentSessionOnly = int.Parse(currentSessionOnlyStr) != 0
                };

            // Open the two events
            using (var inbound = EventWaitHandle.OpenExisting(inboundName, options))
            using (var outbound = EventWaitHandle.OpenExisting(outboundName, options))
            {
                // Repeatedly wait for one event and then set the other
                for (int i = 0; i < 10; i++)
                {
                    Assert.True(inbound.WaitOne(RemoteExecutor.FailWaitTimeoutMilliseconds));
                    if (mode == EventResetMode.ManualReset)
                    {
                        inbound.Reset();
                    }
                    outbound.Set();
                }
            }
        }

        public static TheoryData<string> GetValidNames()
        {
            var names  =  new TheoryData<string>() { Guid.NewGuid().ToString("N") };
            names.Add(Guid.NewGuid().ToString("N") + new string('a', 1000));

            return names;
        }

        public static IEnumerable<object[]> NameOptionsAndEventResetModeCombinations_MemberData()
        {
            foreach (NamedWaitHandleOptions options in MutexTests.GetNameOptionCombinations())
            {
                foreach (EventResetMode mode in Enum.GetValues<EventResetMode>())
                {
                    yield return new object[] { options.CurrentUserOnly, options.CurrentSessionOnly, mode };
                }
            }
        }
    }
}
