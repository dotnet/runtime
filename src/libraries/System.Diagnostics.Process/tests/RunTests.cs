// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class RunTests : ProcessTestBase
    {
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Run_ExitCodeIsReturned(bool useAsync)
        {
            using Process template = CreateProcess(static () => RemoteExecutor.SuccessExitCode);

            ProcessExitStatus exitStatus = useAsync
                ? await Process.RunAsync(template.StartInfo)
                : Process.Run(template.StartInfo);

            Assert.Equal(RemoteExecutor.SuccessExitCode, exitStatus.ExitCode);
            Assert.False(exitStatus.Canceled);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task Run_WithFileName_ExitCodeIsReturned(bool useAsync, bool silent)
        {
            using Process template = CreateProcess(static () => RemoteExecutor.SuccessExitCode);
            List<string>? arguments = Helpers.MapToArgumentList(template.StartInfo);

            ProcessExitStatus exitStatus = useAsync
                ? await Process.RunAsync(template.StartInfo.FileName, arguments, silent)
                : Process.Run(template.StartInfo.FileName, arguments, silent);

            Assert.Equal(RemoteExecutor.SuccessExitCode, exitStatus.ExitCode);
            Assert.False(exitStatus.Canceled);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task Run_WithFileName_OutputIsDiscardedWhenSilent(bool useAsync, bool silent)
        {
            // Process A starts process B that writes large output to stdout and stderr.
            // When silent is true, B's output is redirected to the null device.
            // A's stdout/stderr are redirected so we can verify whether B's output leaked through.
            using Process outerTemplate = CreateProcess((string silentArg) =>
            {
                bool silentValue = bool.Parse(silentArg);

                using Process innerTemplate = CreateProcess(() =>
                {
                    Console.Write(new string('a', 100_000));
                    Console.Error.Write(new string('b', 100_000));
                    return RemoteExecutor.SuccessExitCode;
                });

                List<string>? args = Helpers.MapToArgumentList(innerTemplate.StartInfo);
                ProcessExitStatus status = Process.Run(innerTemplate.StartInfo.FileName, args, silent: silentValue);

                return status.ExitCode;
            }, silent.ToString());

            outerTemplate.StartInfo.RedirectStandardOutput = true;
            outerTemplate.StartInfo.RedirectStandardError = true;

            ProcessTextOutput result = useAsync
                ? await Process.RunAndCaptureTextAsync(outerTemplate.StartInfo)
                : Process.RunAndCaptureText(outerTemplate.StartInfo);

            Assert.Equal(RemoteExecutor.SuccessExitCode, result.ExitStatus.ExitCode);
            Assert.False(result.ExitStatus.Canceled);
            Assert.Equal(silent ? string.Empty : new string('a', 100_000), result.StandardOutput);
            Assert.Equal(silent ? string.Empty : new string('b', 100_000), result.StandardError);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Run_WithTimeout_ExitCodeIsReturned(bool useAsync)
        {
            using Process template = CreateProcess(static () => RemoteExecutor.SuccessExitCode);

            ProcessExitStatus exitStatus;
            if (useAsync)
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
                exitStatus = await Process.RunAsync(template.StartInfo, cts.Token);
            }
            else
            {
                exitStatus = Process.Run(template.StartInfo, TimeSpan.FromMinutes(1));
            }

            Assert.Equal(RemoteExecutor.SuccessExitCode, exitStatus.ExitCode);
            Assert.False(exitStatus.Canceled);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Run_WithTimeoutOrCancellation_KillsLongRunningProcess(bool useAsync)
        {
            using Process template = CreateSleepProcess((int)TimeSpan.FromHours(1).TotalMilliseconds);

            ProcessExitStatus exitStatus;
            if (useAsync)
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
                exitStatus = await Process.RunAsync(template.StartInfo, cts.Token);
            }
            else
            {
                exitStatus = Process.Run(template.StartInfo, TimeSpan.FromMilliseconds(100));
            }

            Assert.True(exitStatus.Canceled);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public Task Run_WithTimeoutOrCancellation_KillsEntireProcessTree(bool useAsync, bool useFileName)
            => RunWithTimeoutOrCancellation_KillsEntireProcessTree(useAsync, useFileName, capture: false, closeOutput: false);

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false, false, false)]
        [InlineData(false, false, true)]
        [InlineData(false, true, false)]
        [InlineData(false, true, true)]
        [InlineData(true, false, false)]
        [InlineData(true, false, true)]
        [InlineData(true, true, false)]
        [InlineData(true, true, true)]
        public Task RunAndCaptureText_WithTimeoutOrCancellation_KillsEntireProcessTree(bool useAsync, bool useFileName, bool closeOutput)
            => RunWithTimeoutOrCancellation_KillsEntireProcessTree(useAsync, useFileName, capture: true, closeOutput);

        private async Task RunWithTimeoutOrCancellation_KillsEntireProcessTree(bool useAsync, bool useFileName, bool capture, bool closeOutput)
        {
            string pipeName = Guid.NewGuid().ToString();
            using NamedPipeServerStream pipe = new(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            using Process template = CreateProcess(RunProcessTree, pipeName, $"2;{closeOutput}");
            template.StartInfo.RedirectStandardOutput = capture;
            template.StartInfo.RedirectStandardError = capture;
            List<string>? arguments = Helpers.MapToArgumentList(template.StartInfo);
            using CancellationTokenSource cancellation = new();
            using CancellationTokenSource watchdog = new(TimeSpan.FromMilliseconds(Helpers.PassingTestTimeoutMilliseconds));

            Task run = Task.Run(async () =>
            {
                if (capture && !closeOutput)
                {
                    if (useAsync)
                    {
                        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => useFileName
                            ? Process.RunAndCaptureTextAsync(template.StartInfo.FileName, arguments, cancellation.Token)
                            : Process.RunAndCaptureTextAsync(template.StartInfo, cancellation.Token));
                    }
                    else
                    {
                        Assert.Throws<TimeoutException>(() => useFileName
                            ? Process.RunAndCaptureText(template.StartInfo.FileName, arguments, TimeSpan.FromMilliseconds(WaitInMS))
                            : Process.RunAndCaptureText(template.StartInfo, TimeSpan.FromMilliseconds(WaitInMS)));
                    }

                    return;
                }

                ProcessExitStatus exitStatus;
                if (capture)
                {
                    ProcessTextOutput output = useAsync
                        ? await (useFileName
                            ? Process.RunAndCaptureTextAsync(template.StartInfo.FileName, arguments, cancellation.Token)
                            : Process.RunAndCaptureTextAsync(template.StartInfo, cancellation.Token))
                        : useFileName
                            ? Process.RunAndCaptureText(template.StartInfo.FileName, arguments, TimeSpan.FromMilliseconds(WaitInMS))
                            : Process.RunAndCaptureText(template.StartInfo, TimeSpan.FromMilliseconds(WaitInMS));
                    Assert.Empty(output.StandardOutput);
                    Assert.Empty(output.StandardError);
                    exitStatus = output.ExitStatus;
                }
                else
                {
                    exitStatus = useAsync
                        ? await (useFileName
                            ? Process.RunAsync(template.StartInfo.FileName, arguments, cancellationToken: cancellation.Token)
                            : Process.RunAsync(template.StartInfo, cancellation.Token))
                        : useFileName
                            ? Process.Run(template.StartInfo.FileName, arguments, timeout: TimeSpan.FromMilliseconds(WaitInMS))
                            : Process.Run(template.StartInfo, TimeSpan.FromMilliseconds(WaitInMS));
                }

                Assert.True(exitStatus.Canceled);
            });

            try
            {
                for (int i = 0; i < 3; i++)
                {
                    await pipe.WaitForConnectionAsync(watchdog.Token);
                    using StreamReader reader = new(pipe, leaveOpen: true);
                    string processId = await reader.ReadLineAsync(watchdog.Token);
                    AddProcessForDispose(Process.GetProcessById(int.Parse(processId)));
                    pipe.Disconnect();
                }

                cancellation.Cancel();
                await run.WaitAsync(watchdog.Token);

                await Helpers.RetryWithBackoff(() =>
                {
                    foreach (Process process in _processes)
                    {
                        if (process != template)
                        {
                            Assert.True(process.HasExited, $"Process {process.Id} is still running.");
                        }
                    }
                });
            }
            finally
            {
                cancellation.Cancel();
                foreach (Process process in _processes)
                {
                    if (process != template)
                    {
                        process.Kill();
                    }
                }

                await run.WaitAsync(TimeSpan.FromMilliseconds(Helpers.PassingTestTimeoutMilliseconds));
            }
        }

        private static int RunProcessTree(string pipeName, string options)
        {
            string[] values = options.Split(';');
            int depth = int.Parse(values[0]);
            bool closeOutput = bool.Parse(values[1]);
            using SafeFileHandle nullHandle = File.OpenNullHandle();

            if (depth > 0)
            {
                using RemoteInvokeHandle child = RemoteExecutor.Invoke(RunProcessTree, pipeName, $"{depth - 1};{closeOutput}",
                    new RemoteInvokeOptions { Start = false, CheckExitCode = false });
                child.Process.StartInfo.InheritedHandles = [];
                if (closeOutput)
                {
                    child.Process.StartInfo.StandardOutputHandle = nullHandle;
                    child.Process.StartInfo.StandardErrorHandle = nullHandle;
                }

                child.Process.Start();
                child.Process = null;
            }

            if (closeOutput)
            {
                // Close the actual standard handles, not the non-owning Console wrappers, to signal EOF.
                using SafeFileHandle output = Console.OpenStandardOutputHandle();
                using SafeFileHandle error = Console.OpenStandardErrorHandle();
                new SafeFileHandle(output.DangerousGetHandle(), ownsHandle: true).Dispose();
                new SafeFileHandle(error.DangerousGetHandle(), ownsHandle: true).Dispose();
            }

            using NamedPipeClientStream pipe = new(".", pipeName, PipeDirection.Out);
            pipe.Connect(WaitInMS);
            using (StreamWriter writer = new(pipe, leaveOpen: true))
            {
                writer.WriteLine(Environment.ProcessId);
            }

            Thread.Sleep(Timeout.Infinite);
            return RemoteExecutor.SuccessExitCode;
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RunAndCaptureText_CapturesOutput(bool useAsync)
        {
            using Process template = CreateProcess(static () =>
            {
                Console.Write("hello");
                Console.Error.Write("world");
                return RemoteExecutor.SuccessExitCode;
            });

            template.StartInfo.RedirectStandardOutput = true;
            template.StartInfo.RedirectStandardError = true;

            ProcessTextOutput result = useAsync
                ? await Process.RunAndCaptureTextAsync(template.StartInfo)
                : Process.RunAndCaptureText(template.StartInfo);

            Assert.Equal(RemoteExecutor.SuccessExitCode, result.ExitStatus.ExitCode);
            Assert.False(result.ExitStatus.Canceled);
            Assert.Equal("hello", result.StandardOutput);
            Assert.Equal("world", result.StandardError);
            Assert.True(result.ProcessId > 0);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RunAndCaptureText_WithFileName_CapturesOutput(bool useAsync)
        {
            using Process template = CreateProcess(static () =>
            {
                Console.Write("output");
                Console.Error.Write("error");
                return RemoteExecutor.SuccessExitCode;
            });

            List<string>? arguments = Helpers.MapToArgumentList(template.StartInfo);

            ProcessTextOutput result = useAsync
                ? await Process.RunAndCaptureTextAsync(template.StartInfo.FileName, arguments)
                : Process.RunAndCaptureText(template.StartInfo.FileName, arguments);

            Assert.Equal(RemoteExecutor.SuccessExitCode, result.ExitStatus.ExitCode);
            Assert.Equal("output", result.StandardOutput);
            Assert.Equal("error", result.StandardError);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RunAndCaptureText_EmptyOutput(bool useAsync)
        {
            using Process template = CreateProcess(static () => RemoteExecutor.SuccessExitCode);

            template.StartInfo.RedirectStandardOutput = true;
            template.StartInfo.RedirectStandardError = true;

            ProcessTextOutput result = useAsync
                ? await Process.RunAndCaptureTextAsync(template.StartInfo)
                : Process.RunAndCaptureText(template.StartInfo);

            Assert.Equal(RemoteExecutor.SuccessExitCode, result.ExitStatus.ExitCode);
            Assert.Equal(string.Empty, result.StandardOutput);
            Assert.Equal(string.Empty, result.StandardError);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RunAndCaptureText_WithTimeoutOrCancellation_CapturesOutput(bool useAsync)
        {
            using Process template = CreateProcess(static () =>
            {
                Console.Write("captured");
                Console.Error.Write("errors");
                return RemoteExecutor.SuccessExitCode;
            });

            template.StartInfo.RedirectStandardOutput = true;
            template.StartInfo.RedirectStandardError = true;

            ProcessTextOutput result;
            if (useAsync)
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
                result = await Process.RunAndCaptureTextAsync(template.StartInfo, cts.Token);
            }
            else
            {
                result = Process.RunAndCaptureText(template.StartInfo, TimeSpan.FromMinutes(1));
            }

            Assert.Equal(RemoteExecutor.SuccessExitCode, result.ExitStatus.ExitCode);
            Assert.False(result.ExitStatus.Canceled);
            Assert.Equal("captured", result.StandardOutput);
            Assert.Equal("errors", result.StandardError);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RunAndCaptureText_WithTimeoutOrCancellation_KillsLongRunningProcess(bool useAsync)
        {
            using Process template = CreateSleepProcess((int)TimeSpan.FromHours(1).TotalMilliseconds);

            template.StartInfo.RedirectStandardOutput = true;
            template.StartInfo.RedirectStandardError = true;

            if (useAsync)
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                    await Process.RunAndCaptureTextAsync(template.StartInfo, cts.Token));
            }
            else
            {
                Assert.ThrowsAny<TimeoutException>(() =>
                    Process.RunAndCaptureText(template.StartInfo, TimeSpan.FromMilliseconds(100)));
            }
        }

        [Theory]
        [InlineData(false, false, true)]
        [InlineData(false, true, false)]
        [InlineData(false, false, false)]
        [InlineData(true, false, true)]
        [InlineData(true, true, false)]
        [InlineData(true, false, false)]
        public async Task RunAndCaptureText_NotRedirected_ThrowsInvalidOperationException(bool useAsync, bool redirectOutput, bool redirectError)
        {
            ProcessStartInfo startInfo = new("someprocess")
            {
                RedirectStandardOutput = redirectOutput,
                RedirectStandardError = redirectError
            };

            if (useAsync)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => Process.RunAndCaptureTextAsync(startInfo));
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => Process.RunAndCaptureText(startInfo));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Run_NullStartInfo_ThrowsArgumentNullException(bool useAsync)
        {
            if (useAsync)
            {
                await Assert.ThrowsAsync<ArgumentNullException>("startInfo", () => Process.RunAsync((ProcessStartInfo)null!));
            }
            else
            {
                AssertExtensions.Throws<ArgumentNullException>("startInfo", () => Process.Run((ProcessStartInfo)null!));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Run_NullFileName_ThrowsArgumentNullException(bool useAsync)
        {
            if (useAsync)
            {
                await Assert.ThrowsAsync<ArgumentNullException>("fileName", () => Process.RunAsync((string)null!));
            }
            else
            {
                AssertExtensions.Throws<ArgumentNullException>("fileName", () => Process.Run((string)null!));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Run_EmptyFileName_ThrowsArgumentException(bool useAsync)
        {
            if (useAsync)
            {
                await Assert.ThrowsAsync<ArgumentException>("fileName", () => Process.RunAsync(string.Empty));
            }
            else
            {
                AssertExtensions.Throws<ArgumentException>("fileName", () => Process.Run(string.Empty));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RunAndCaptureText_NullStartInfo_ThrowsArgumentNullException(bool useAsync)
        {
            if (useAsync)
            {
                await Assert.ThrowsAsync<ArgumentNullException>("startInfo", () => Process.RunAndCaptureTextAsync((ProcessStartInfo)null!));
            }
            else
            {
                AssertExtensions.Throws<ArgumentNullException>("startInfo", () => Process.RunAndCaptureText((ProcessStartInfo)null!));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RunAndCaptureText_NullFileName_ThrowsArgumentNullException(bool useAsync)
        {
            if (useAsync)
            {
                await Assert.ThrowsAsync<ArgumentNullException>("fileName", () => Process.RunAndCaptureTextAsync((string)null!));
            }
            else
            {
                AssertExtensions.Throws<ArgumentNullException>("fileName", () => Process.RunAndCaptureText((string)null!));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RunAndCaptureText_EmptyFileName_ThrowsArgumentException(bool useAsync)
        {
            if (useAsync)
            {
                await Assert.ThrowsAsync<ArgumentException>("fileName", () => Process.RunAndCaptureTextAsync(string.Empty));
            }
            else
            {
                AssertExtensions.Throws<ArgumentException>("fileName", () => Process.RunAndCaptureText(string.Empty));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Run_UseShellExecute_ThrowsInvalidOperationException(bool useAsync)
        {
            ProcessStartInfo startInfo = new("someprocess") { UseShellExecute = true };

            if (useAsync)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => Process.RunAsync(startInfo));
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => Process.Run(startInfo));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RunAndCaptureText_UseShellExecute_ThrowsInvalidOperationException(bool useAsync)
        {
            ProcessStartInfo startInfo = new("someprocess") { UseShellExecute = true };

            if (useAsync)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => Process.RunAndCaptureTextAsync(startInfo));
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => Process.RunAndCaptureText(startInfo));
            }
        }
    }
}
