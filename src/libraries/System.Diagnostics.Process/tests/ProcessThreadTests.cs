// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Threading;
using System.Linq;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using System.Threading.Tasks;

namespace System.Diagnostics.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/49568", typeof(PlatformDetection), nameof(PlatformDetection.IsMacOsAppleSilicon))]
    public partial class ProcessThreadTests : ProcessTestBase
    {
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
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
                    if (!OperatingSystem.IsMacOS())
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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void ThreadsAreDisposedWhenProcessIsDisposed()
        {
            Process process = CreateDefaultProcess();

            ProcessThreadCollection threadCollection = process.Threads;
            int expectedCount = 0;
            int disposedCount = 0;
            foreach (ProcessThread processThread in threadCollection)
            {
                expectedCount += 1;
                processThread.Disposed += (_, __) => disposedCount += 1;
            }

            KillWait(process);
            Assert.Equal(0, disposedCount);

            process.Dispose();
            Assert.Equal(expectedCount, disposedCount);
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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestThreadStateProperty()
        {
            CreateDefaultProcess();

            ProcessThread thread = _process.Threads[0];
            if (ThreadState.Wait != thread.ThreadState)
            {
                Assert.Throws<InvalidOperationException>(() => thread.WaitReason);
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
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
