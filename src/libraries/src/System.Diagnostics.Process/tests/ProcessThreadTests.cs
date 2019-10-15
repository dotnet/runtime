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
    public partial class ProcessThreadTests : ProcessTestBase
    {
        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Retrieving information about local processes is not supported on uap")]
        public void TestCommonPriorityAndTimeProperties()
        {
            CreateDefaultProcess();

            ProcessThreadCollection threadCollection = _process.Threads;
            Assert.InRange(threadCollection.Count, 1, int.MaxValue);
            ProcessThread thread = threadCollection[0];
            try
            {
                if (ThreadState.Terminated != thread.ThreadState)
                {
                    // On OSX, thread id is a 64bit unsigned value. We truncate the ulong to int
                    // due to .NET API surface area. Hence, on overflow id can be negative while
                    // casting the ulong to int.
                    if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        Assert.InRange(thread.Id, 0, int.MaxValue);
                    }
                    Assert.Equal(_process.BasePriority, thread.BasePriority);
                    Assert.InRange(thread.CurrentPriority, 0, int.MaxValue);
                    Assert.InRange(thread.PrivilegedProcessorTime.TotalSeconds, 0, int.MaxValue);
                    Assert.InRange(thread.UserProcessorTime.TotalSeconds, 0, int.MaxValue);
                    Assert.InRange(thread.TotalProcessorTime.TotalSeconds, 0, int.MaxValue);
                }
            }
            catch (Exception e) when (e is Win32Exception || e is InvalidOperationException)
            {
                // Win32Exception is thrown when getting threadinfo fails, or
                // InvalidOperationException if it fails because the thread already exited.
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Retrieving information about local processes is not supported on uap")]
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
        [PlatformSpecific(TestPlatforms.OSX|TestPlatforms.FreeBSD)] // OSX and FreeBSD throw PNSE from StartTime
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
        [PlatformSpecific(TestPlatforms.Linux|TestPlatforms.Windows)] // OSX and FreeBSD throw PNSE from StartTime
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Retrieving information about local processes is not supported on uap")]
        public async Task TestStartTimeProperty()
        {
            TimeSpan allowedWindow = TimeSpan.FromSeconds(2);

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
                Assert.InRange(passed, 1, int.MaxValue);

                // Now add a thread, and from that thread, while it's still alive, verify
                // that there's at least one thread greater than the current time we previously grabbed.
                await Task.Factory.StartNew(() =>
                {
                    p.Refresh();
                    try
                    {
                        var newest = p.Threads.Cast<ProcessThread>().OrderBy(t => t.StartTime.ToUniversalTime()).Last();
                        Assert.InRange(newest.StartTime.ToUniversalTime(), curTime - allowedWindow, DateTime.Now.ToUniversalTime() + allowedWindow);
                    }
                    catch (InvalidOperationException)
                    {
                        // A thread may have gone away between our getting its info and attempting to access its StartTime
                    }
                }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Retrieving information about local processes is not supported on uap")]
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
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Retrieving information about local processes is not supported on uap")]
        public void TestThreadStateProperty()
        {
            CreateDefaultProcess();

            ProcessThread thread = _process.Threads[0];
            if (ThreadState.Wait != thread.ThreadState)
            {
                Assert.Throws<InvalidOperationException>(() => thread.WaitReason);
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Retrieving information about local processes is not supported on uap")]
        public void Threads_GetMultipleTimes_ReturnsSameInstance()
        {
            CreateDefaultProcess();

            Assert.Same(_process.Threads, _process.Threads);
        }

        [Fact]
        public void Threads_GetNotStarted_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.Threads);
        }
    }
}
