// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Security;
using System.IO;
using Xunit;

namespace System.Diagnostics.ProcessTests
{
    public class ProcessThreadTests : ProcessTestBase
    {
        [Fact]
        public void TestCommonPriorityAndTimeProperties()
        {
            ProcessThreadCollection threadCollection = _process.Threads;
            Assert.True(threadCollection.Count > 0);

            ProcessThread thread = threadCollection[0];

            if (ThreadState.Terminated != thread.ThreadState)
            {
                Assert.Equal(_process.BasePriority, thread.BasePriority);
                Assert.True(thread.CurrentPriority >= 0, "Thread CurrentPriority");
                Assert.True(thread.PrivilegedProcessorTime.TotalSeconds >= 0, "Thread PrivilegedProcessorTime");
                Assert.True(thread.UserProcessorTime.TotalSeconds >= 0, "Thread UserProcessorTime");
                Assert.True(thread.TotalProcessorTime.TotalSeconds >= 0, "Thread TotalProcessorTime");
            }
        }

        [Fact]
        public void TestStartTimeProperty()
        {
            ProcessThreadCollection threadCollection = _process.Threads;
            Assert.True(threadCollection.Count > 0);

            ProcessThread thread = threadCollection[0];

            if (global::Interop.IsOSX)
            {
                Assert.Throws<PlatformNotSupportedException>(() => thread.StartTime);
                return;
            }

            if (ThreadState.Initialized != thread.ThreadState)
            {
                Assert.True(thread.StartTime.ToUniversalTime() <= DateTime.UtcNow, "Thread StartTime");
            }
        }

        [Fact]
        public void TestStartAddressProperty()
        {
            ProcessThread thread = _process.Threads[0];
            Assert.Equal(global::Interop.IsOSX, thread.StartAddress == IntPtr.Zero);
        }

        [Fact]
        public void TestPriorityLevelProperty()
        {
            ProcessThread thread = _process.Threads[0];

            if (global::Interop.IsOSX)
            {
                Assert.Throws<PlatformNotSupportedException>(() => thread.PriorityLevel);
                Assert.Throws<PlatformNotSupportedException>(() => thread.PriorityLevel = ThreadPriorityLevel.AboveNormal);
                return;
            }

            ThreadPriorityLevel originalPriority = thread.PriorityLevel;

            if (global::Interop.IsLinux)
            {
                Assert.Throws<PlatformNotSupportedException>(() => thread.PriorityLevel = ThreadPriorityLevel.AboveNormal);
                return;
            }

            try
            {
                thread.PriorityLevel = ThreadPriorityLevel.AboveNormal;
                Assert.Equal(ThreadPriorityLevel.AboveNormal, thread.PriorityLevel);
            }
            finally
            {
                thread.PriorityLevel = originalPriority;
                Assert.Equal(originalPriority, thread.PriorityLevel);
            }
        }

        [Fact]
        public void TestThreadStateProperty()
        {
            ProcessThread thread = _process.Threads[0];
            if (ThreadState.Wait != thread.ThreadState)
            {
                Assert.Throws<InvalidOperationException>(() => thread.WaitReason);
            }
        }
    }
}
