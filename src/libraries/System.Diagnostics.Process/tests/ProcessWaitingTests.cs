// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Diagnostics.ProcessTests
{
    public class ProcessWaitingTests : ProcessTestBase
    {
        [Fact]
        public void MultipleProcesses_StartAllKillAllWaitAll()
        {
            const int Iters = 50;
            Process[] processes = Enumerable.Range(0, Iters).Select(_ => CreateProcessInfinite()).ToArray();

            foreach (Process p in processes) p.Start();
            foreach (Process p in processes) p.Kill();
            foreach (Process p in processes) Assert.True(p.WaitForExit(WaitInMS));
        }

        [Fact]
        public void MultipleProcesses_SerialStartKillWait()
        {
            const int Iters = 50;
            for (int i = 0; i < Iters; i++)
            {
                Process p = CreateProcessInfinite();
                p.Start();
                p.Kill();
                p.WaitForExit(WaitInMS);
            }
        }

        [Fact]
        public void MultipleProcesses_ParallelStartKillWait()
        {
            const int Tasks = 4, ItersPerTask = 50;
            Action work = () =>
            {
                for (int i = 0; i < ItersPerTask; i++)
                {
                    Process p = CreateProcessInfinite();
                    p.Start();
                    p.Kill();
                    p.WaitForExit(WaitInMS);
                }
            };
            Task.WaitAll(Enumerable.Range(0, Tasks).Select(_ => Task.Run(work)).ToArray());
        }

        [Theory]
        [InlineData(0)]  // poll
        [InlineData(10)] // real timeout
        public void CurrentProcess_WaitNeverCompletes(int milliseconds)
        {
            Assert.False(Process.GetCurrentProcess().WaitForExit(milliseconds));
        }

        [Fact]
        public void SingleProcess_TryWaitMultipleTimesBeforeCompleting()
        {
            Process p = CreateProcessInfinite();
            p.Start();

            // Verify we can try to wait for the process to exit multiple times
            Assert.False(p.WaitForExit(0));
            Assert.False(p.WaitForExit(0));

            // Then wait until it exits and concurrently kill it.
            // There's a race condition here, in that we really want to test
            // killing it while we're waiting, but we could end up killing it
            // before hand, in which case we're simply not testing exactly
            // what we wanted to test, but everything should still work.
            Task.Delay(10).ContinueWith(_ => p.Kill());
            Assert.True(p.WaitForExit(WaitInMS));
            Assert.True(p.WaitForExit(0));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task SingleProcess_WaitAfterExited(bool addHandlerBeforeStart)
        {
            Process p = CreateProcessInfinite();
            p.EnableRaisingEvents = true;

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (addHandlerBeforeStart)
            {
                p.Exited += delegate { tcs.SetResult(true); };
            }
            p.Start();
            if (!addHandlerBeforeStart)
            {
                p.Exited += delegate { tcs.SetResult(true); };
            }

            p.Kill();
            Assert.True(await tcs.Task);

            Assert.True(p.WaitForExit(0));
        }

        [Fact]
        public void SingleProcess_CopiesShareExitInformation()
        {
            Process p = CreateProcessInfinite();
            p.Start();

            Process[] copies = Enumerable.Range(0, 3).Select(_ => Process.GetProcessById(p.Id)).ToArray();

            Assert.False(p.WaitForExit(0));
            p.Kill();
            Assert.True(p.WaitForExit(WaitInMS));

            foreach (Process copy in copies)
            {
                Assert.True(copy.WaitForExit(0));
            }
        }

    }
}
