// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Linq;
using Xunit;
using System.Threading.Tasks;

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
        [PlatformSpecific(PlatformID.OSX)]
        public void TestStartTimeProperty_OSX()
        {
            using (Process p = Process.GetCurrentProcess())
            {
                ProcessThreadCollection threads = p.Threads;
                Assert.NotNull(threads);
                Assert.NotEmpty(threads);

                ProcessThread thread = threads[0];
                Assert.NotNull(thread);

                Assert.Throws<PlatformNotSupportedException>(() => thread.StartTime);
            }
        }

        [Fact]
        [PlatformSpecific(~PlatformID.OSX)] // OSX throws PNSE from StartTime
        public async Task TestStartTimeProperty()
        {
            TimeSpan allowedWindow = TimeSpan.FromSeconds(1);

            using (Process p = Process.GetCurrentProcess())
            {
                // Get the process' start time
                DateTime startTime = p.StartTime.ToUniversalTime();

                // Get the process' threads
                ProcessThreadCollection threads = p.Threads;
                Assert.NotNull(threads);
                Assert.NotEmpty(threads);

                // Get the current time
                DateTime curTime = DateTime.UtcNow;

                // Make sure each thread's start time is at least the process'
                // start time and not beyond the current time.
                Assert.All(
                    threads.Cast<ProcessThread>(),
                    t => Assert.InRange(t.StartTime.ToUniversalTime(), startTime - allowedWindow, curTime + allowedWindow));

                // Now add a thread, and from that thread, while it's still alive, verify
                // that there's at least one thread greater than the current time we previously grabbed.
                await Task.Factory.StartNew(() =>
                {
                    p.Refresh();
                    Assert.Contains(
                        p.Threads.Cast<ProcessThread>(),
                        t => t.StartTime.ToUniversalTime() >= curTime - allowedWindow);
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
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
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Assert.True((long)thread.StartAddress >= 0);
                    }
                    else
                    {
                        Assert.Equal(RuntimeInformation.IsOSPlatform(OSPlatform.OSX), thread.StartAddress == IntPtr.Zero);
                    }
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
