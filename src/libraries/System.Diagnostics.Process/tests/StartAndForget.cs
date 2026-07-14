// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class StartAndForgetTests : ProcessTestBase
    {
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public void StartAndForget_StartsProcessAndReturnsValidPid(bool useProcessStartInfo)
        {
            using Process template = CreateSleepProcess((int)TimeSpan.FromHours(1).TotalMilliseconds);
            int pid = useProcessStartInfo
                ? Process.StartAndForget(template.StartInfo)
                : Process.StartAndForget(template.StartInfo.FileName, Helpers.MapToArgumentList(template.StartInfo));

            Assert.True(pid > 0);

            using Process launched = Process.GetProcessById(pid);
            try
            {
                Assert.False(launched.HasExited);
            }
            finally
            {
                launched.Kill();
                launched.WaitForExit();
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void StartAndForget_WithStandardOutputHandle_CapturesOutput()
        {
            using Process template = CreateProcess(static () =>
            {
                Console.Write("hello");
                return RemoteExecutor.SuccessExitCode;
            });

            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle outputReadPipe, out SafeFileHandle outputWritePipe);

            using (outputReadPipe)
            using (outputWritePipe)
            {
                template.StartInfo.StandardOutputHandle = outputWritePipe;

                int pid = Process.StartAndForget(template.StartInfo);
                Assert.True(pid > 0);

                outputWritePipe.Close(); // close the parent copy of child handle

                using StreamReader streamReader = new(new FileStream(outputReadPipe, FileAccess.Read, bufferSize: 1, outputReadPipe.IsAsync));
                Assert.Equal("hello", streamReader.ReadToEnd());
            }
        }

        // This test does not use RemoteExecutor, but it's a simple way to filter to OSes that support Process.Start.
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void StartAndForget_WithNullArguments_StartsProcess()
        {
            // cmd is available on every Windows, including Nano. When run with no parameters, it displays the Windows version/copyright banner.
            // true is available on every Unix. When invoked with no arguments, it does nothing and exits successfully.
            int pid = Process.StartAndForget(OperatingSystem.IsWindows() ? "cmd.exe" : "true", null);

            Assert.True(pid > 0);
        }

        [Fact]
        public void StartAndForget_WithStartInfo_NullStartInfo_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("startInfo", () => Process.StartAndForget((ProcessStartInfo)null!));
        }

        [Fact]
        public void StartAndForget_WithFileName_NullFileName_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("fileName", () => Process.StartAndForget((string)null!));
        }

        [Theory]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, true)]
        public void StartAndForget_WithRedirectedStreams_ThrowsInvalidOperationException(
            bool redirectInput, bool redirectOutput, bool redirectError)
        {
            ProcessStartInfo startInfo = new("someprocess")
            {
                RedirectStandardInput = redirectInput,
                RedirectStandardOutput = redirectOutput,
                RedirectStandardError = redirectError,
            };

            Assert.Throws<InvalidOperationException>(() => Process.StartAndForget(startInfo));
        }

        [Fact]
        public void StartAndForget_WithUseShellExecute_ThrowsInvalidOperationException()
        {
            ProcessStartInfo startInfo = new("someprocess")
            {
                UseShellExecute = true,
            };

            Assert.Throws<InvalidOperationException>(() => Process.StartAndForget(startInfo));
        }


    }
}
