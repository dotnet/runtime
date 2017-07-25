// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using System.Threading;
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
            using (Process p = CreateProcessPortable(RemotelyInvokable.ExitWithCode, exitCode.ToString()))
            {
                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                p.EnableRaisingEvents = true;
                p.Exited += delegate
                { tcs.SetResult(true); };
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
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapNotUapAot, "Getting handle of child process running on UAP is not possible")]
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
            char[] output = new char[6];
            child2.StandardOutput.Read(output, 0, output.Length);
            Assert.Equal("Signal", new string(output)); // wait for the signal before killing the peer

            child1.Kill();
            Assert.True(child1.WaitForExit(WaitInMS));
            Assert.True(child2.WaitForExit(WaitInMS));

            Assert.Equal(SuccessExitCode, child2.ExitCode);
        }

        [Fact]
        public void WaitForSignal()
        {
            const string expectedSignal = "Signal";
            const string successResponse = "Success";
            const int timeout = 5 * 1000;

            Process p = CreateProcessPortable(RemotelyInvokable.WriteLineReadLine);
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            var mre = new ManualResetEventSlim(false);

            int linesReceived = 0;
            p.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    linesReceived++;

                    if (e.Data == expectedSignal)
                    {
                        mre.Set();
                    }
                }
            };

            p.Start();
            p.BeginOutputReadLine();

            Assert.True(mre.Wait(timeout));
            Assert.Equal(1, linesReceived);

            // Wait a little bit to make sure process didn't exit on itself
            Thread.Sleep(100);
            Assert.False(p.HasExited, "Process has prematurely exited");

            using (StreamWriter writer = p.StandardInput)
            {
                writer.WriteLine(successResponse);
            }

            Assert.True(p.WaitForExit(timeout), "Process has not exited");
            Assert.Equal(RemotelyInvokable.SuccessExitCode, p.ExitCode);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapNotUapAot, "Not applicable on uap - RemoteInvoke does not give back process handle")]
        [ActiveIssue(15844, TestPlatforms.AnyUnix)]
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
            Process child = CreateProcessPortable(RemotelyInvokable.SelfTerminate);
            child.Start();
            Assert.True(child.WaitForExit(WaitInMS));
            Assert.NotEqual(SuccessExitCode, child.ExitCode);
        }

        [Fact]
        public void WaitForInputIdle_NotDirected_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.WaitForInputIdle());
        }

        [Fact]
        public void WaitForExit_NotDirected_ThrowsInvalidOperationException()
        {
            var process = new Process();
            Assert.Throws<InvalidOperationException>(() => process.WaitForExit());
        }
    }
}
