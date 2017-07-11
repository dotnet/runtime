// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Xunit;

namespace System.Threading.Tests
{
    public class EventWaitHandleTests : RemoteExecutorTestBase
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
            AssertExtensions.Throws<ArgumentException>(null, () => new EventWaitHandle(true, (EventResetMode)12345));
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // names aren't supported on Unix
        [Fact]
        public void Ctor_InvalidNames()
        {
            AssertExtensions.Throws<ArgumentException>("name", null, () => new EventWaitHandle(true, EventResetMode.AutoReset, new string('a', 1000)));
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]  // names aren't supported on Unix
        [Fact]
        public void Ctor_NamesArentSupported_Unix()
        {
            Assert.Throws<PlatformNotSupportedException>(() => new EventWaitHandle(false, EventResetMode.AutoReset, "anything"));
            bool createdNew;
            Assert.Throws<PlatformNotSupportedException>(() => new EventWaitHandle(false, EventResetMode.AutoReset, "anything", out createdNew));
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
            using (var ewh = new EventWaitHandle(false, EventResetMode.AutoReset, name, out createdNew))
            {
                Assert.True(createdNew);
                using (new EventWaitHandle(false, EventResetMode.AutoReset, name, out createdNew))
                {
                    Assert.False(createdNew);
                }
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)] // named semaphores aren't supported on Unix
        [Theory]
        [InlineData(EventResetMode.AutoReset)]
        [InlineData(EventResetMode.ManualReset)]
        public void Ctor_NameUsedByOtherSynchronizationPrimitive_Windows(EventResetMode mode)
        {
            string name = Guid.NewGuid().ToString("N");
            using (Mutex m = new Mutex(false, name))
                Assert.Throws<WaitHandleCannotBeOpenedException>(() => new EventWaitHandle(false, mode, name));
        }

        [Fact]
        public void SetReset()
        {
            using (EventWaitHandle are = new EventWaitHandle(false, EventResetMode.AutoReset))
            {
                Assert.False(are.WaitOne(0));
                are.Set();
                Assert.True(are.WaitOne(0));
                Assert.False(are.WaitOne(0));
                are.Set();
                are.Reset();
                Assert.False(are.WaitOne(0));
            }

            using (EventWaitHandle mre = new EventWaitHandle(false, EventResetMode.ManualReset))
            {
                Assert.False(mre.WaitOne(0));
                mre.Set();
                Assert.True(mre.WaitOne(0));
                Assert.True(mre.WaitOne(0));
                mre.Set();
                Assert.True(mre.WaitOne(0));
                mre.Reset();
                Assert.False(mre.WaitOne(0));
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // OpenExisting not supported on Unix
        [Fact]
        public void OpenExisting_Windows()
        {
            string name = Guid.NewGuid().ToString("N");

            EventWaitHandle resultHandle;
            Assert.False(EventWaitHandle.TryOpenExisting(name, out resultHandle));
            Assert.Null(resultHandle);

            using (EventWaitHandle are1 = new EventWaitHandle(false, EventResetMode.AutoReset, name))
            {
                using (EventWaitHandle are2 = EventWaitHandle.OpenExisting(name))
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

                Assert.True(EventWaitHandle.TryOpenExisting(name, out resultHandle));
                Assert.NotNull(resultHandle);
                resultHandle.Dispose();
            }
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]  // OpenExisting not supported on Unix
        [Fact]
        public void OpenExisting_NotSupported_Unix()
        {
            Assert.Throws<PlatformNotSupportedException>(() => EventWaitHandle.OpenExisting("anything"));
            EventWaitHandle ewh;
            Assert.Throws<PlatformNotSupportedException>(() => EventWaitHandle.TryOpenExisting("anything", out ewh));
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // OpenExisting not supported on Unix
        [Fact]
        public void OpenExisting_InvalidNames_Windows()
        {
            AssertExtensions.Throws<ArgumentNullException>("name", () => EventWaitHandle.OpenExisting(null));
            AssertExtensions.Throws<ArgumentException>("name", null, () => EventWaitHandle.OpenExisting(string.Empty));
            AssertExtensions.Throws<ArgumentException>("name", null, () => EventWaitHandle.OpenExisting(new string('a', 10000)));
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // OpenExisting not supported on Unix
        [Fact]
        public void OpenExisting_UnavailableName_Windows()
        {
            string name = Guid.NewGuid().ToString("N");
            Assert.Throws<WaitHandleCannotBeOpenedException>(() => EventWaitHandle.OpenExisting(name));
            EventWaitHandle ignored;
            Assert.False(EventWaitHandle.TryOpenExisting(name, out ignored));
        }

        [PlatformSpecific(TestPlatforms.Windows)]  // OpenExisting not supported on Unix
        [Fact]
        public void OpenExisting_NameUsedByOtherSynchronizationPrimitive_Windows()
        {
            string name = Guid.NewGuid().ToString("N");
            using (Mutex mtx = new Mutex(true, name))
            {
                Assert.Throws<WaitHandleCannotBeOpenedException>(() => EventWaitHandle.OpenExisting(name));
                EventWaitHandle ignored;
                Assert.False(EventWaitHandle.TryOpenExisting(name, out ignored));
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)] // names aren't supported on Unix
        [Theory]
        [InlineData(EventResetMode.ManualReset)]
        [InlineData(EventResetMode.AutoReset)]
        [ActiveIssue(21275, TargetFrameworkMonikers.Uap)]
        public void PingPong(EventResetMode mode)
        {
            // Create names for the two events
            string outboundName = Guid.NewGuid().ToString("N");
            string inboundName = Guid.NewGuid().ToString("N");

            // Create the two events and the other process with which to synchronize
            using (var inbound = new EventWaitHandle(true, mode, inboundName))
            using (var outbound = new EventWaitHandle(false, mode, outboundName))
            using (var remote = RemoteInvoke(PingPong_OtherProcess, mode.ToString(), outboundName, inboundName))
            {
                // Repeatedly wait for one event and then set the other
                for (int i = 0; i < 10; i++)
                {
                    Assert.True(inbound.WaitOne(FailWaitTimeoutMilliseconds));
                    if (mode == EventResetMode.ManualReset)
                    {
                        inbound.Reset();
                    }
                    outbound.Set();
                }
            }
        }

        private static int PingPong_OtherProcess(string modeName, string inboundName, string outboundName)
        {
            EventResetMode mode = (EventResetMode)Enum.Parse(typeof(EventResetMode), modeName);

            // Open the two events
            using (var inbound = EventWaitHandle.OpenExisting(inboundName))
            using (var outbound = EventWaitHandle.OpenExisting(outboundName))
            {
                // Repeatedly wait for one event and then set the other
                for (int i = 0; i < 10; i++)
                {
                    Assert.True(inbound.WaitOne(FailWaitTimeoutMilliseconds));
                    if (mode == EventResetMode.ManualReset)
                    {
                        inbound.Reset();
                    }
                    outbound.Set();
                }
            }

            return SuccessExitCode;
        }

    }
}
