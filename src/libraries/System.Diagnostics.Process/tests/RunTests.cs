// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
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
        [InlineData(false)]
        [InlineData(true)]
        public async Task Run_WithFileName_ExitCodeIsReturned(bool useAsync)
        {
            using Process template = CreateProcess(static () => RemoteExecutor.SuccessExitCode);
            List<string>? arguments = Helpers.MapToArgumentList(template.StartInfo);

            ProcessExitStatus exitStatus = useAsync
                ? await Process.RunAsync(template.StartInfo.FileName, arguments)
                : Process.Run(template.StartInfo.FileName, arguments);

            Assert.Equal(RemoteExecutor.SuccessExitCode, exitStatus.ExitCode);
            Assert.False(exitStatus.Canceled);
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
