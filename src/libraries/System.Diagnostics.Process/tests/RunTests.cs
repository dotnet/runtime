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
            using Process template = CreateProcess(RemotelyInvokable.Dummy);

            ProcessExitStatus exitStatus = useAsync
                ? await Process.RunAsync(template.StartInfo)
                : Process.Run(template.StartInfo);

            Assert.Equal(RemotelyInvokable.SuccessExitCode, exitStatus.ExitCode);
            Assert.False(exitStatus.Canceled);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Run_WithFileName_ExitCodeIsReturned(bool useAsync)
        {
            using Process template = CreateProcess(RemotelyInvokable.Dummy);
            List<string>? arguments = MapToArgumentList(template.StartInfo);

            ProcessExitStatus exitStatus = useAsync
                ? await Process.RunAsync(template.StartInfo.FileName, arguments)
                : Process.Run(template.StartInfo.FileName, arguments);

            Assert.Equal(RemotelyInvokable.SuccessExitCode, exitStatus.ExitCode);
            Assert.False(exitStatus.Canceled);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Run_WithTimeout_ExitCodeIsReturned(bool useAsync)
        {
            using Process template = CreateProcess(RemotelyInvokable.Dummy);

            ProcessExitStatus exitStatus = useAsync
                ? await Process.RunAsync(template.StartInfo)
                : Process.Run(template.StartInfo, TimeSpan.FromMinutes(1));

            Assert.Equal(RemotelyInvokable.SuccessExitCode, exitStatus.ExitCode);
            Assert.False(exitStatus.Canceled);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Run_WithTimeoutOrCancellation_KillsLongRunningProcess(bool useAsync)
        {
            using Process template = CreateSleepProcess((int)TimeSpan.FromHours(1).TotalMilliseconds);

            ProcessExitStatus exitStatus = useAsync
                ? await Process.RunAsync(template.StartInfo, new CancellationTokenSource(TimeSpan.FromMilliseconds(100)).Token)
                : Process.Run(template.StartInfo, TimeSpan.FromMilliseconds(100));

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

            List<string>? arguments = MapToArgumentList(template.StartInfo);

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
            using Process template = CreateProcess(RemotelyInvokable.Dummy);

            ProcessTextOutput result = useAsync
                ? await Process.RunAndCaptureTextAsync(template.StartInfo)
                : Process.RunAndCaptureText(template.StartInfo);

            Assert.Equal(RemotelyInvokable.SuccessExitCode, result.ExitStatus.ExitCode);
            Assert.Equal(string.Empty, result.StandardOutput);
            Assert.Equal(string.Empty, result.StandardError);
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
        public void Run_UseShellExecute_ThrowsInvalidOperationException(bool useAsync)
        {
            ProcessStartInfo startInfo = new("someprocess") { UseShellExecute = true };

            Assert.Throws<InvalidOperationException>(() =>
            {
                if (useAsync)
                {
                    Process.RunAsync(startInfo);
                }
                else
                {
                    Process.Run(startInfo);
                }
            });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void RunAndCaptureText_UseShellExecute_ThrowsInvalidOperationException(bool useAsync)
        {
            ProcessStartInfo startInfo = new("someprocess") { UseShellExecute = true };

            Assert.Throws<InvalidOperationException>(() =>
            {
                if (useAsync)
                {
                    Process.RunAndCaptureTextAsync(startInfo);
                }
                else
                {
                    Process.RunAndCaptureText(startInfo);
                }
            });
        }

        // RemoteExecutor populates ProcessStartInfo.Arguments, but the filename overloads
        // take an argument list, so this helper maps the serialized argument string.
        private static List<string>? MapToArgumentList(ProcessStartInfo startInfo)
        {
            string arguments = startInfo.Arguments;
            if (string.IsNullOrEmpty(arguments))
            {
                return null;
            }

            List<string> list = new();
            System.Text.StringBuilder builder = new();
            bool isQuoted = false;

            foreach (char c in arguments)
            {
                switch (c)
                {
                    case '"' when !isQuoted:
                        isQuoted = true;
                        break;
                    case ' ' when !isQuoted:
                    case '"' when isQuoted:
                        if (builder.Length > 0)
                        {
                            list.Add(builder.ToString());
                            builder.Clear();
                        }
                        isQuoted = false;
                        break;
                    default:
                        builder.Append(c);
                        break;
                }
            }

            if (builder.Length > 0)
            {
                list.Add(builder.ToString());
            }

            return list;
        }
    }
}
