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
            catch (Exception e) when (e is Win32Exception || e is InvalidOperationException)
            {
                // Win32Exception is thrown when getting threadinfo fails, or
                // InvalidOperationException if it fails because the thread already exited.
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
        [PlatformSpecific(TestPlatforms.OSX)]
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

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // https://github.com/Microsoft/BashOnWindows/issues/974
        [PlatformSpecific(~TestPlatforms.OSX)] // OSX throws PNSE from StartTime
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
                int passed = 0;
                foreach (ProcessThread t in threads.Cast<ProcessThread>())
                {
                    try
                    {
                        Assert.InRange(t.StartTime.ToUniversalTime(), startTime - allowedWindow, curTime + allowedWindow);
                        passed++;
                    }
                    catch (InvalidOperationException)
                    {
                        // The thread may have gone away between our getting its info and attempting to access its StartTime
                    }
                }
                Assert.True(passed > 0, "Expected at least one thread to be valid for StartTime");

                // Now add a thread, and from that thread, while it's still alive, verify
                // that there's at least one thread greater than the current time we previously grabbed.
                await Task.Factory.StartNew(() =>
                {
                    p.Refresh();
                    try
                    {
                        Assert.Contains(p.Threads.Cast<ProcessThread>(), t => t.StartTime.ToUniversalTime() >= curTime - allowedWindow);
                    }
                    catch (InvalidOperationException)
                    {
                        // A thread may have gone away between our getting its info and attempting to access its StartTime
                    }
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
        }

        [Fact]
        public void TestStartAddressProperty()
        {
            using (Process p = Process.GetCurrentProcess())
            {
                ProcessThreadCollection threads = p.Threads;
                Assert.NotNull(threads);
                Assert.NotEmpty(threads);

                IntPtr startAddress = threads[0].StartAddress; 

                // There's nothing we can really validate about StartAddress, other than that we can get its value
                // without throwing.  All values (even zero) are valid on all platforms.
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
