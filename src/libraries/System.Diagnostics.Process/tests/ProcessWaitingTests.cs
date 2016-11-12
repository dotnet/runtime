// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class ProcessWaitingTests : ProcessTestBase
    {
        [Fact]
        public void MultipleProcesses_StartAllKillAllWaitAll()
        {
            const int Iters = 10;
            Process[] processes = Enumerable.Range(0, Iters).Select(_ => CreateProcessLong()).ToArray();

            foreach (Process p in processes) p.Start();
            foreach (Process p in processes) p.Kill();
            foreach (Process p in processes) Assert.True(p.WaitForExit(WaitInMS));
        }

        [Fact]
        public void MultipleProcesses_SerialStartKillWait()
        {
            const int Iters = 10;
            for (int i = 0; i < Iters; i++)
            {
                Process p = CreateProcessLong();
                p.Start();
                p.Kill();
                p.WaitForExit(WaitInMS);
            }
        }

        [Fact]
        public void MultipleProcesses_ParallelStartKillWait()
        {
            const int Tasks = 4, ItersPerTask = 10;
            Action work = () =>
            {
                for (int i = 0; i < ItersPerTask; i++)
                {
                    Process p = CreateProcessLong();
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
            Process p = CreateProcessLong();
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
            Process p = CreateProcessLong();
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

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(127)]
        public async Task SingleProcess_EnableRaisingEvents_CorrectExitCode(int exitCode)
        {
            using (Process p = RemoteInvoke(exitCodeStr => int.Parse(exitCodeStr), exitCode.ToString(), new RemoteInvokeOptions { Start = false }).Process)
            {
                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                p.EnableRaisingEvents = true;
                p.Exited += delegate { tcs.SetResult(true); };
                p.Start();
                Assert.True(await tcs.Task);
                Assert.Equal(exitCode, p.ExitCode);
            }
        }

        [Fact]
        public void SingleProcess_CopiesShareExitInformation()
        {
            Process p = CreateProcessLong();
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

        [Fact]
        public void WaitForPeerProcess()
        {
            Process child1 = CreateProcessLong();
            child1.Start();

            Process child2 = CreateProcess(peerId =>
            {
                Process peer = Process.GetProcessById(int.Parse(peerId));
                Console.WriteLine("Signal");
                Assert.True(peer.WaitForExit(WaitInMS));
                return SuccessExitCode;
            }, child1.Id.ToString());
            child2.StartInfo.RedirectStandardOutput = true;
            child2.Start();
            Assert.Equal("Signal", child2.StandardOutput.ReadLine()); // wait for the signal before killing the peer

            child1.Kill();
            Assert.True(child1.WaitForExit(WaitInMS));
            Assert.True(child2.WaitForExit(WaitInMS));

            Assert.Equal(SuccessExitCode, child2.ExitCode);
        }

        [Fact]
        public void WaitChain()
        {
            Process root = CreateProcess(() =>
            {
                Process child1 = CreateProcess(() =>
                {
                    Process child2 = CreateProcess(() =>
                    {
                        Process child3 = CreateProcess(() => SuccessExitCode);
                        child3.Start();
                        Assert.True(child3.WaitForExit(WaitInMS));
                        return child3.ExitCode;
                    });
                    child2.Start();
                    Assert.True(child2.WaitForExit(WaitInMS));
                    return child2.ExitCode;
                });
                child1.Start();
                Assert.True(child1.WaitForExit(WaitInMS));
                return child1.ExitCode;
            });
            root.Start();
            Assert.True(root.WaitForExit(WaitInMS));
            Assert.Equal(SuccessExitCode, root.ExitCode);
        }

        [Fact]
        public void WaitForSelfTerminatingChild()
        {
            Process child = CreateProcess(() =>
            {
                Process.GetCurrentProcess().Kill();
                throw new ShouldNotBeInvokedException();
            });
            child.Start();
            Assert.True(child.WaitForExit(WaitInMS));
            Assert.NotEqual(SuccessExitCode, child.ExitCode);
        }
    }
}
