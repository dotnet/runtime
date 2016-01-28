// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Linq;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class ProcessThreadTests : ProcessTestBase
    {
        [Fact]
        public void TestCommonPriorityAndTimeProperties()
        {
            ProcessThreadCollection threadCollection = _process.Threads;
            Assert.True(threadCollection.Count > 0);
            ProcessThread thread = threadCollection[0];
            try
            {
                if (ThreadState.Terminated != thread.ThreadState)
                {
                    Assert.True(thread.Id >= 0);
                    Assert.Equal(_process.BasePriority, thread.BasePriority);
                    Assert.True(thread.CurrentPriority >= 0);
                    Assert.True(thread.PrivilegedProcessorTime.TotalSeconds >= 0);
                    Assert.True(thread.UserProcessorTime.TotalSeconds >= 0);
                    Assert.True(thread.TotalProcessorTime.TotalSeconds >= 0);
                }
            }
            catch (Win32Exception)
            {
                // Win32Exception is thrown when getting threadinfo fails. 
            }
        }

        [Fact]
        public void TestThreadCount()
        {
            int numOfThreads = 10;
            CountdownEvent counter = new CountdownEvent(numOfThreads);
            ManualResetEventSlim mre = new ManualResetEventSlim();
            for (int i = 0; i < numOfThreads; i++)
            {
                new Thread(() => { counter.Signal(); mre.Wait(); }) { IsBackground = true }.Start();
            }

            counter.Wait();

            try
            {
                Assert.True(Process.GetCurrentProcess().Threads.Count >= numOfThreads);
            }
            finally
            {
                mre.Set();
            }
        }

        [Fact]
        public void TestStartTimeProperty()
        {
            DateTime timeBeforeCreatingProcess = DateTime.UtcNow;
            Process p = CreateProcessLong();
            try
            {
                p.Start();

                ProcessThreadCollection threadCollection = p.Threads;
                Assert.True(threadCollection.Count > 0);

                ProcessThread thread = threadCollection[0];

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Assert.Throws<PlatformNotSupportedException>(() => thread.StartTime);
                    return;
                }

                if (ThreadState.Initialized != thread.ThreadState)
                {
                    // Time in unix, is measured in jiffies, which is incremented by one for every timer interrupt since the boot time.
                    // Thus, because there are HZ timer interrupts in a second, there are HZ jiffies in a second. Hence 1\HZ, will
                    // be the resolution of system timer. The lowest value of HZ on unix is 100, hence the timer resolution is 10 ms.
                    // On Windows, timer resolution is 15 ms from MSDN DateTime.Now. Hence, allowing error in 15ms [max(10,15)].
                    long intervalTicks = new TimeSpan(0, 0, 0, 0, 15).Ticks;
                    long beforeTicks = timeBeforeCreatingProcess.Ticks - intervalTicks;
                    long afterTicks = DateTime.UtcNow.Ticks + intervalTicks;
                    Assert.InRange(thread.StartTime.ToUniversalTime().Ticks, beforeTicks, afterTicks);
                }
            }
            finally
            {
                if (!p.HasExited)
                    p.Kill();

                Assert.True(p.WaitForExit(WaitInMS));
            }
        }

        [Fact]
        public void TestStartAddressProperty()
        {
            Process p = Process.GetCurrentProcess();
            try
            {
                if (p.Threads.Count != 0)
                {
                    ProcessThread thread = p.Threads[0];
                    Assert.Equal(RuntimeInformation.IsOSPlatform(OSPlatform.OSX), thread.StartAddress == IntPtr.Zero);
                }
            }
            finally
            {
                p.Dispose();
            }
        }

        [Fact]
        public void TestPriorityLevelProperty()
        {
            ProcessThread thread = _process.Threads[0];

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Assert.Throws<PlatformNotSupportedException>(() => thread.PriorityLevel);
                Assert.Throws<PlatformNotSupportedException>(() => thread.PriorityLevel = ThreadPriorityLevel.AboveNormal);
                return;
            }

            ThreadPriorityLevel originalPriority = thread.PriorityLevel;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
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
