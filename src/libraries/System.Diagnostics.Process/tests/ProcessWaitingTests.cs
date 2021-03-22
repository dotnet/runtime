// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Diagnostics.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/49568", typeof(PlatformDetection), nameof(PlatformDetection.IsMacOsAppleSilicon))]
    public class ProcessWaitingTests : ProcessTestBase
    {
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void MultipleProcesses_StartAllKillAllWaitAll()
        {
            const int Iters = 10;
            Process[] processes = Enumerable.Range(0, Iters).Select(_ => CreateProcessLong()).ToArray();

            foreach (Process p in processes) p.Start();
            foreach (Process p in processes) p.Kill();
            foreach (Process p in processes) Assert.True(p.WaitForExit(WaitInMS));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task MultipleProcesses_StartAllKillAllWaitAllAsync()
        {
            const int Iters = 10;
            Process[] processes = Enumerable.Range(0, Iters).Select(_ => CreateProcessLong()).ToArray();

            foreach (Process p in processes) p.Start();
            foreach (Process p in processes) p.Kill();
            foreach (Process p in processes)
            {
                using (var cts = new CancellationTokenSource(WaitInMS))
                {
                    await p.WaitForExitAsync(cts.Token);
                    Assert.True(p.HasExited);
                }
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void MultipleProcesses_SerialStartKillWait()
        {
            const int Iters = 10;
            for (int i = 0; i < Iters; i++)
            {
                Process p = CreateProcessLong();
                p.Start();
                p.Kill();
                Assert.True(p.WaitForExit(WaitInMS));
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task MultipleProcesses_SerialStartKillWaitAsync()
        {
            const int Iters = 10;
            for (int i = 0; i < Iters; i++)
            {
                Process p = CreateProcessLong();
                p.Start();
                p.Kill();
                using (var cts = new CancellationTokenSource(WaitInMS))
                {
                    await p.WaitForExitAsync(cts.Token);
                    Assert.True(p.HasExited);
                }
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task MultipleProcesses_ParallelStartKillWaitAsync()
        {
            const int Tasks = 4, ItersPerTask = 10;
            Func<Task> work = async () =>
            {
                for (int i = 0; i < ItersPerTask; i++)
                {
                    Process p = CreateProcessLong();
                    p.Start();
                    p.Kill();
                    using (var cts = new CancellationTokenSource(WaitInMS))
                    {
                        await p.WaitForExitAsync(cts.Token);
                        Assert.True(p.HasExited);
                    }
                }
            };

            await Task.WhenAll(Enumerable.Range(0, Tasks).Select(_ => Task.Run(work)).ToArray());
        }

        [Theory]
        [InlineData(0)]  // poll
        [InlineData(10)] // real timeout
        public void CurrentProcess_WaitNeverCompletes(int milliseconds)
        {
            Assert.False(Process.GetCurrentProcess().WaitForExit(milliseconds));
        }

        [Theory]
        [InlineData(0)]  // poll
        [InlineData(10)] // real timeout
        public async Task CurrentProcess_WaitAsyncNeverCompletes(int milliseconds)
        {
            using (var cts = new CancellationTokenSource(milliseconds))
            {
                CancellationToken token = cts.Token;
                Process process = Process.GetCurrentProcess();
                OperationCanceledException ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => process.WaitForExitAsync(token));
                Assert.Equal(token, ex.CancellationToken);
                Assert.False(process.HasExited);
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task SingleProcess_TryWaitAsyncMultipleTimesBeforeCompleting()
        {
            Process p = CreateProcessLong();
            p.Start();

            // Verify we can try to wait for the process to exit multiple times

            // First test with an already canceled token. Because the token is already canceled,
            // WaitForExitAsync should complete synchronously
            for (int i = 0; i < 2; i++)
            {
                var token = new CancellationToken(canceled: true);
                Task t = p.WaitForExitAsync(token);

                Assert.Equal(TaskStatus.Canceled, t.Status);

                OperationCanceledException ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);
                Assert.Equal(token, ex.CancellationToken);
                Assert.False(p.HasExited);
            }

            // Next, test with a token that is canceled after the task is created to
            // exercise event hookup and async cancellation
            using (var cts = new CancellationTokenSource())
            {
                CancellationToken token = cts.Token;
                Task t = p.WaitForExitAsync(token);
                cts.Cancel();

                OperationCanceledException ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t);
                Assert.Equal(token, ex.CancellationToken);
                Assert.False(p.HasExited);
            }

            // Then wait until it exits and concurrently kill it.
            // There's a race condition here, in that we really want to test
            // killing it while we're waiting, but we could end up killing it
            // before hand, in which case we're simply not testing exactly
            // what we wanted to test, but everything should still work.
            _ = Task.Delay(10).ContinueWith(_ => p.Kill());

            using (var cts = new CancellationTokenSource(WaitInMS))
            {
                await p.WaitForExitAsync(cts.Token);
                Assert.True(p.HasExited);
            }

            // Waiting on an already exited process should complete synchronously
            Assert.True(p.HasExited);
            Task task = p.WaitForExitAsync();
            Assert.Equal(TaskStatus.RanToCompletion, task.Status);
        }

        [SkipOnMono("Hangs on Mono, https://github.com/dotnet/runtime/issues/38943")]
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task SingleProcess_WaitAfterExited(bool addHandlerBeforeStart)
        {
            Process p = CreateProcessLong();
            p.EnableRaisingEvents = true;

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            if (addHandlerBeforeStart)
            {
                p.Exited += delegate { tcs.SetResult(); };
            }
            p.Start();
            if (!addHandlerBeforeStart)
            {
                p.Exited += delegate { tcs.SetResult(); };
            }

            p.Kill();
            await tcs.Task;

            Assert.True(p.WaitForExit(0));
            p.WaitForExit(); // wait for event handlers to complete
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task SingleProcess_WaitAsyncAfterExited(bool addHandlerBeforeStart)
        {
            Process p = CreateProcessLong();
            p.EnableRaisingEvents = true;

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            if (addHandlerBeforeStart)
            {
                p.Exited += delegate { tcs.SetResult(); };
            }
            p.Start();
            if (!addHandlerBeforeStart)
            {
                p.Exited += delegate { tcs.SetResult(); };
            }

            p.Kill();
            await tcs.Task;

            var token = new CancellationToken(canceled: true);
            await p.WaitForExitAsync(token);
            Assert.True(p.HasExited);

            await p.WaitForExitAsync();
            Assert.True(p.HasExited);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(127)]
        public async Task SingleProcess_EnableRaisingEvents_CorrectExitCode(int exitCode)
        {
            using (Process p = CreateProcessPortable(RemotelyInvokable.ExitWithCode, exitCode.ToString()))
            {
                var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                p.EnableRaisingEvents = true;
                p.Exited += delegate { tcs.SetResult(); };
                p.Start();
                await tcs.Task;
                Assert.Equal(exitCode, p.ExitCode);
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task SingleProcess_CopiesShareExitAsyncInformation()
        {
            using Process p = CreateProcessLong();
            p.Start();

            Process[] copies = Enumerable.Range(0, 3).Select(_ => Process.GetProcessById(p.Id)).ToArray();

            using (var cts = new CancellationTokenSource(millisecondsDelay: 0))
            {
                CancellationToken token = cts.Token;
                OperationCanceledException ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => p.WaitForExitAsync(token));
                Assert.Equal(token, ex.CancellationToken);
                Assert.False(p.HasExited);
            }
            p.Kill();
            using (var cts = new CancellationTokenSource(WaitInMS))
            {
                await p.WaitForExitAsync(cts.Token);
                Assert.True(p.HasExited);
            }

            using (var cts = new CancellationTokenSource(millisecondsDelay: 0))
            {
                foreach (Process copy in copies)
                {
                    // Since the process has already exited, waiting again does not throw (even if the token is canceled) because
                    // there's no work to do.
                    await copy.WaitForExitAsync(cts.Token);
                    Assert.True(copy.HasExited);
                }
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void WaitForPeerProcess()
        {
            Process child1 = CreateProcessLong();
            child1.Start();

            Process child2 = CreateProcess(peerId =>
            {
                Process peer = Process.GetProcessById(int.Parse(peerId));
                Console.WriteLine("Signal");
                Assert.True(peer.WaitForExit(WaitInMS));
                return RemoteExecutor.SuccessExitCode;
            }, child1.Id.ToString());
            child2.StartInfo.RedirectStandardOutput = true;
            child2.Start();
            char[] output = new char[6];
            child2.StandardOutput.Read(output, 0, output.Length);
            Assert.Equal("Signal", new string(output)); // wait for the signal before killing the peer

            child1.Kill();
            Assert.True(child1.WaitForExit(WaitInMS));
            Assert.True(child2.WaitForExit(WaitInMS));

            Assert.Equal(RemoteExecutor.SuccessExitCode, child2.ExitCode);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task WaitAsyncForPeerProcess()
        {
            using Process child1 = CreateProcessLong();
            child1.Start();

            using Process child2 = CreateProcess(async peerId =>
            {
                Process peer = Process.GetProcessById(int.Parse(peerId));
                Console.WriteLine("Signal");
                using (var cts = new CancellationTokenSource(WaitInMS))
                {
                    await peer.WaitForExitAsync(cts.Token);
                    Assert.True(peer.HasExited);
                }
                return RemoteExecutor.SuccessExitCode;
            }, child1.Id.ToString());
            child2.StartInfo.RedirectStandardOutput = true;
            child2.Start();
            char[] output = new char[6];
            child2.StandardOutput.Read(output, 0, output.Length);
            Assert.Equal("Signal", new string(output)); // wait for the signal before killing the peer

            child1.Kill();
            using (var cts = new CancellationTokenSource(WaitInMS))
            {
                await child1.WaitForExitAsync(cts.Token);
                Assert.True(child1.HasExited);
            }
            using (var cts = new CancellationTokenSource(WaitInMS))
            {
                await child2.WaitForExitAsync(cts.Token);
                Assert.True(child2.HasExited);
            }

            Assert.Equal(RemoteExecutor.SuccessExitCode, child2.ExitCode);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void WaitForSignal()
        {
            const string ExpectedSignal = "Signal";
            const string SuccessResponse = "Success";
            const int timeout = 30 * 1000; // 30 seconds, to allow for very slow machines

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

                    if (e.Data == ExpectedSignal)
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
                writer.WriteLine(SuccessResponse);
            }

            Assert.True(p.WaitForExit(timeout), "Process has not exited");
            p.WaitForExit(); // wait for event handlers to complete
            Assert.Equal(RemotelyInvokable.SuccessExitCode, p.ExitCode);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task WaitAsyncForSignal()
        {
            const string expectedSignal = "Signal";
            const string successResponse = "Success";
            const int timeout = 30 * 1000; // 30 seconds, to allow for very slow machines

            using Process p = CreateProcessPortable(RemotelyInvokable.WriteLineReadLine);
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            using var mre = new ManualResetEventSlim(false);

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
            Thread.Sleep(1);
            Assert.False(p.HasExited, "Process has prematurely exited");

            using (StreamWriter writer = p.StandardInput)
            {
                writer.WriteLine(successResponse);
            }

            using (var cts = new CancellationTokenSource(timeout))
            {
                await p.WaitForExitAsync(cts.Token);
                Assert.True(p.HasExited, "Process has not exited");
            }
            Assert.Equal(RemotelyInvokable.SuccessExitCode, p.ExitCode);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void WaitForExit_AfterProcessExit_ShouldConsumeOutputDataReceived()
        {
            const string message = "test";
            using Process p = CreateProcessPortable(RemotelyInvokable.Echo, message);

            int linesReceived = 0;
            p.OutputDataReceived += (_, e) => { if (e.Data is not null) linesReceived++; };
            p.StartInfo.RedirectStandardOutput = true;

            Assert.True(p.Start());

            // Give time for the process (cmd) to terminate
            while (!p.HasExited)
            {
                Thread.Sleep(20);
            }

            p.BeginOutputReadLine();
            p.WaitForExit();

            Assert.Equal(1, linesReceived);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task WaitForExitAsync_AfterProcessExit_ShouldConsumeOutputDataReceived()
        {
            const string message = "test";
            using Process p = CreateProcessPortable(RemotelyInvokable.Echo, message);

            int linesReceived = 0;
            p.OutputDataReceived += (_, e) => { if (e.Data is not null) linesReceived++; };
            p.StartInfo.RedirectStandardOutput = true;

            Assert.True(p.Start());

            // Give time for the process (cmd) to terminate
            while (!p.HasExited)
            {
                Thread.Sleep(20);
            }

            p.BeginOutputReadLine();
            await p.WaitForExitAsync();

            Assert.Equal(1, linesReceived);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void WaitChain()
        {
            Process root = CreateProcess(() =>
            {
                Process child1 = CreateProcess(() =>
                {
                    Process child2 = CreateProcess(() =>
                    {
                        Process child3 = CreateProcess(() => RemoteExecutor.SuccessExitCode);
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
            Assert.Equal(RemoteExecutor.SuccessExitCode, root.ExitCode);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task WaitAsyncChain()
        {
            Process root = CreateProcess(async () =>
            {
                Process child1 = CreateProcess(async () =>
                {
                    Process child2 = CreateProcess(async () =>
                    {
                        Process child3 = CreateProcess(() => RemoteExecutor.SuccessExitCode);
                        child3.Start();
                        using (var cts = new CancellationTokenSource(WaitInMS))
                        {
                            await child3.WaitForExitAsync(cts.Token);
                            Assert.True(child3.HasExited);
                        }

                        return child3.ExitCode;
                    });
                    child2.Start();
                    using (var cts = new CancellationTokenSource(WaitInMS))
                    {
                        await child2.WaitForExitAsync(cts.Token);
                        Assert.True(child2.HasExited);
                    }

                    return child2.ExitCode;
                });
                child1.Start();
                using (var cts = new CancellationTokenSource(WaitInMS))
                {
                    await child1.WaitForExitAsync(cts.Token);
                    Assert.True(child1.HasExited);
                }

                return child1.ExitCode;
            });
            root.Start();
            using (var cts = new CancellationTokenSource(WaitInMS))
            {
                await root.WaitForExitAsync(cts.Token);
                Assert.True(root.HasExited);
            }
            Assert.Equal(RemoteExecutor.SuccessExitCode, root.ExitCode);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void WaitForSelfTerminatingChild()
        {
            Process child = CreateProcessPortable(RemotelyInvokable.SelfTerminate);
            child.Start();
            Assert.True(child.WaitForExit(WaitInMS));
            Assert.NotEqual(RemoteExecutor.SuccessExitCode, child.ExitCode);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task WaitAsyncForSelfTerminatingChild()
        {
            Process child = CreateProcessPortable(RemotelyInvokable.SelfTerminate);
            child.Start();
            using (var cts = new CancellationTokenSource(WaitInMS))
            {
                await child.WaitForExitAsync(cts.Token);
                Assert.True(child.HasExited);
            }
            Assert.NotEqual(RemoteExecutor.SuccessExitCode, child.ExitCode);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public async Task WaitAsyncForProcess()
        {
            Process p = CreateDefaultProcess();

            Task processTask = p.WaitForExitAsync();
            Assert.False(p.HasExited);
            Assert.False(processTask.IsCompleted);

            p.Kill();
            await processTask;

            Assert.True(p.HasExited);
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

        [Fact]
        public async Task WaitForExitAsync_NotDirected_ThrowsInvalidOperationException()
        {
            var process = new Process();
            await Assert.ThrowsAsync<InvalidOperationException>(() => process.WaitForExitAsync());
        }
    }
}
